using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DshAiMini
{
    internal sealed class ChatTurn
    {
        public string Role;   // "system" | "user" | "assistant"
        public string Content;
        public ChatTurn(string role, string content) { Role = role; Content = content; }
    }

    /// <summary>
    /// 对话：只做「拼请求 → 交出去 → 解析回包」三件事，不碰 UI、不碰线程。
    ///
    /// 用 OpenAI 兼容的 chat/completions 形状，因为它同时覆盖 DeepSeek、
    /// OpenAI、以及绝大多数第三方中转。换供应商只改 BaseUrl 和 Model。
    /// </summary>
    internal static class AiChat
    {
        public static readonly List<ChatTurn> History = new();

        /// <summary>把一段对话序列化成请求体。单独拆出来，方便离线看报文长什么样。</summary>
        public static string BuildBody(AiConfig cfg, IList<ChatTurn> turns, bool stream = false)
        {
            using var ms = new System.IO.MemoryStream();
            using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                w.WriteStartObject();
                w.WriteString("model", cfg.Model);
                w.WriteStartArray("messages");
                foreach (var t in turns)
                {
                    w.WriteStartObject();
                    w.WriteString("role", t.Role);
                    w.WriteString("content", t.Content ?? "");
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteNumber("temperature", cfg.Temperature);
                if (cfg.MaxTokens > 0) w.WriteNumber("max_tokens", cfg.MaxTokens);
                if (stream) w.WriteBoolean("stream", true);
                w.WriteEndObject();
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        /// <summary>从回包里取正文。认 OpenAI 形状，也容忍个别中转的变体。</summary>
        public static string ExtractText(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "";
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var err))
                {
                    string m = err.ValueKind == JsonValueKind.Object && err.TryGetProperty("message", out var mEl)
                        ? mEl.GetString() : err.ToString();
                    return "[错误] " + m;
                }

                if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
                {
                    var c0 = choices[0];
                    if (c0.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content))
                        return content.GetString() ?? "";
                    if (c0.TryGetProperty("text", out var text))
                        return text.GetString() ?? "";
                }
                if (root.TryGetProperty("content", out var flat) && flat.ValueKind == JsonValueKind.Array)
                {
                    var sb = new StringBuilder();
                    foreach (var part in flat.EnumerateArray())
                        if (part.TryGetProperty("text", out var t)) sb.Append(t.GetString());
                    return sb.ToString();
                }
            }
            catch { }
            return json.Length > 400 ? json.Substring(0, 400) + "…" : json;
        }

        /// <summary>测试连接：发一条最短的消息，只看能不能通、密钥对不对。</summary>
        public static void TestAsync(AiConfig cfg, Action<bool, string> done)
        {
            var turns = new List<ChatTurn> { new ChatTurn("user", "ping") };
            var body = BuildBody(cfg, turns);
            AiHttp.Post(cfg.BaseUrl, body, cfg.ApiKey, Math.Min(cfg.TimeoutMs, 20000),
                (ok, status, text, error) =>
                {
                    if (!ok)
                    {
                        done(false, string.IsNullOrEmpty(error) ? ("HTTP " + status + " " + Trim(text)) : error);
                        return;
                    }
                    string reply = ExtractText(text);
                    if (reply.StartsWith("[错误]")) { done(false, reply.Substring(4)); return; }
                    done(true, "模型回了一句「" + Trim(reply) + "」");
                });
        }

        /// <summary>正常一轮对话：把历史带上，拿回复，把回复也记进历史。</summary>
        public static void SendAsync(AiConfig cfg, string systemPrompt, string userText, Action<bool, string> done)
        {
            var turns = new List<ChatTurn>();
            if (!string.IsNullOrWhiteSpace(systemPrompt)) turns.Add(new ChatTurn("system", systemPrompt));
            turns.AddRange(History);
            turns.Add(new ChatTurn("user", userText));

            var body = BuildBody(cfg, turns);
            AiHttp.Post(cfg.BaseUrl, body, cfg.ApiKey, cfg.TimeoutMs,
                (ok, status, text, error) =>
                {
                    if (!ok)
                    {
                        done(false, string.IsNullOrEmpty(error) ? ("HTTP " + status + " " + Trim(text)) : error);
                        return;
                    }
                    string reply = ExtractText(text);
                    if (reply.StartsWith("[错误]")) { done(false, reply.Substring(4)); return; }

                    History.Add(new ChatTurn("user", userText));
                    History.Add(new ChatTurn("assistant", reply));
                    // 只留最近 20 轮，避免请求体越滚越大
                    while (History.Count > 40) History.RemoveAt(0);
                    done(true, reply);
                });
        }

        private static string Trim(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ").Trim();
            return s.Length > 120 ? s.Substring(0, 120) + "…" : s;
        }
    }
}
