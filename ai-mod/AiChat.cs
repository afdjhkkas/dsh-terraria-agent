using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DshAiMini
{
    internal sealed class ChatTurn
    {
        public string Role;   // "system" | "user" | "assistant" | "tool"
        public string Content;
        public string ToolCallId;       // role=="tool" 时用
        public List<ToolCall> Calls;    // role=="assistant" 且这一轮是工具调用时用
        public ChatTurn(string role, string content) { Role = role; Content = content; }
    }

    internal sealed class ToolCall
    {
        public string Id = "";
        public string Name = "";
        public string Args = "{}";
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
        public static string BuildBody(AiConfig cfg, IList<ChatTurn> turns, bool stream = false, bool withTools = false)
        {
            using var ms = new System.IO.MemoryStream();
            using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                w.WriteStartObject();
                w.WriteString("model", cfg.ModelResolved);
                w.WriteStartArray("messages");
                foreach (var t in turns)
                {
                    w.WriteStartObject();
                    w.WriteString("role", t.Role);

                    // assistant 那一轮如果是在要工具，content 必须是 null（OpenAI 的规矩）
                    if (t.Calls != null && t.Calls.Count > 0)
                    {
                        w.WriteNull("content");
                        w.WriteStartArray("tool_calls");
                        foreach (var c in t.Calls)
                        {
                            w.WriteStartObject();
                            w.WriteString("id", c.Id);
                            w.WriteString("type", "function");
                            w.WriteStartObject("function");
                            w.WriteString("name", c.Name);
                            w.WriteString("arguments", c.Args);
                            w.WriteEndObject();
                            w.WriteEndObject();
                        }
                        w.WriteEndArray();
                    }
                    else
                    {
                        w.WriteString("content", t.Content ?? "");
                        if (t.Role == "tool" && !string.IsNullOrEmpty(t.ToolCallId))
                            w.WriteString("tool_call_id", t.ToolCallId);
                    }
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteNumber("temperature", cfg.Temperature);
                if (cfg.MaxTokens > 0) w.WriteNumber("max_tokens", cfg.MaxTokens);
                if (stream) w.WriteBoolean("stream", true);
                if (withTools)
                {
                    w.WritePropertyName("tools");
                    w.WriteRawValue(AiTools.Json(), skipInputValidation: true);
                    w.WriteString("tool_choice", "auto");
                }
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

        /// <summary>
        /// 从回包里取"它想调用哪些工具"。
        /// 空列表 = 它这一轮直接给答案了；非空 = 它要先动手再回答。
        /// </summary>
        public static List<ToolCall> ExtractToolCalls(string json)
        {
            var list = new List<ToolCall>();
            if (string.IsNullOrWhiteSpace(json)) return list;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("choices", out var ch)) return list;
                if (ch.ValueKind != JsonValueKind.Array || ch.GetArrayLength() == 0) return list;
                if (!ch[0].TryGetProperty("message", out var msg)) return list;
                if (!msg.TryGetProperty("tool_calls", out var tc) || tc.ValueKind != JsonValueKind.Array) return list;
                foreach (var c in tc.EnumerateArray())
                {
                    var call = new ToolCall();
                    if (c.TryGetProperty("id", out var id)) call.Id = id.GetString() ?? "";
                    if (c.TryGetProperty("function", out var fn))
                    {
                        if (fn.TryGetProperty("name", out var nm)) call.Name = nm.GetString() ?? "";
                        if (fn.TryGetProperty("arguments", out var ag))
                            call.Args = ag.ValueKind == JsonValueKind.String ? (ag.GetString() ?? "{}") : ag.ToString();
                    }
                    if (!string.IsNullOrEmpty(call.Name)) list.Add(call);
                }
            }
            catch { }
            return list;
        }

        /// <summary>测试连接：发一条最短的消息，只看能不能通、密钥对不对。</summary>
        public static void TestAsync(AiConfig cfg, Action<bool, string> done)
        {
            var turns = new List<ChatTurn> { new ChatTurn("user", "ping") };
            var body = BuildBody(cfg, turns);
            AiHttp.Post(cfg.BaseUrlResolved, body, cfg.ApiKeyResolved, Math.Min(cfg.TimeoutMs, 20000),
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

        /// <summary>
        /// 正常一轮对话：把历史带上，拿回复，把回复也记进历史。
        ///
        /// system 不是原样发出去的，而是临场拼成三段：
        ///   ① 固定人格与实测数值（SystemPrompt.Text）
        ///   ② 知识库 —— KnowledgeMode="full" 时塞编进 dll 的全部 16 篇原文，
        ///      否则只塞这一问命中的那几条（Knowledge.ForQuery）
        ///   ③ 实时游戏状态（AiGameState.Describe，可关）
        /// 这样同一个模型在不同处境下拿到的是不同的上下文，而不是一段万能长文。
        /// </summary>
        public static void SendAsync(AiConfig cfg, string systemPrompt, string userText, Action<bool, string> done)
        {
            var sb = new StringBuilder();
            sb.Append(string.IsNullOrWhiteSpace(systemPrompt) ? SystemPrompt.Text : systemPrompt);

            if (cfg.UseKnowledge)
            {
                bool full = string.Equals(cfg.KnowledgeMode, "full", System.StringComparison.OrdinalIgnoreCase);
                string kb = full ? Knowledge.All() : Knowledge.ForQuery(userText);
                if (!string.IsNullOrWhiteSpace(kb))
                {
                    sb.Append("\n\n").Append(full
                        ? "## 知识库全文（内部资料，回答时优先采信；与你的记忆冲突时以此为准）\n"
                        : "## 相关知识库\n").Append(kb);
                }
            }
            if (cfg.IncludeGameState)
            {
                string st = AiGameState.Describe();
                if (!string.IsNullOrWhiteSpace(st)) sb.Append("\n\n## 玩家当前状态（实时）\n").Append(st);
            }

            var turns = new List<ChatTurn>();
            turns.Add(new ChatTurn("system", sb.ToString()));
            turns.AddRange(History);
            turns.Add(new ChatTurn("user", userText));

            var body = BuildBody(cfg, turns);
            AiHttp.Post(cfg.BaseUrlResolved, body, cfg.ApiKeyResolved, cfg.TimeoutMs,
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
