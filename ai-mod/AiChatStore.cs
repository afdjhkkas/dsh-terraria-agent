using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Terraria;
using Terraria.ModLoader;

namespace DshAiMini
{
    /// <summary>
    /// 对话历史。存成 JSON 放在 ModConfigs 下，退出游戏再进来还能接上。
    ///
    /// 为什么要落盘：对话页看着像"临时的"，但玩家问过的结论（比如"我现在该去打谁"）
    /// 往往过一会儿还要回头看。落在磁盘上才算真的记住。
    /// </summary>
    internal static class ChatStore
    {
        internal sealed class Turn
        {
            public string Role { get; set; }   // user | assistant
            public string Text { get; set; }
            public long At { get; set; }
        }

        internal sealed class ChatFile
        {
            public List<Turn> Turns { get; set; } = new();
        }

        /// <summary>最多留这么多条（一问一答算两条），再多从最旧的丢。</summary>
        const int KeepMax = 120;

        static List<Turn> _turns;
        static readonly JsonSerializerOptions Opt = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        static string Dir
        {
            get
            {
                string d = Path.Combine(Main.SavePath, "ModConfigs");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        public static string FilePath => Path.Combine(Dir, "DshAiMini-chat.json");

        static void Warn(string msg) => ModContent.GetInstance<DshAiMini>()?.Logger?.Warn(msg);

        public static List<Turn> Turns
        {
            get
            {
                if (_turns == null) Load();
                return _turns;
            }
        }

        public static void Load()
        {
            _turns = new List<Turn>();
            try
            {
                if (!File.Exists(FilePath)) return;
                var f = JsonSerializer.Deserialize<ChatFile>(File.ReadAllText(FilePath));
                if (f?.Turns != null) _turns = f.Turns;
            }
            catch (Exception ex)
            {
                Warn("[ai-mini] 读对话历史失败，从空开始: " + ex.Message);
            }
        }

        static void Flush()
        {
            try
            {
                var f = new ChatFile { Turns = _turns ?? new List<Turn>() };
                File.WriteAllText(FilePath, JsonSerializer.Serialize(f, Opt));
            }
            catch (Exception ex)
            {
                Warn("[ai-mini] 写对话历史失败: " + ex.Message);
            }
        }

        public static void Append(string role, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var list = Turns;
            list.Add(new Turn { Role = role, Text = text, At = DateTimeOffset.Now.ToUnixTimeMilliseconds() });
            while (list.Count > KeepMax) list.RemoveAt(0);
            Flush();
        }

        public static void Clear()
        {
            _turns = new List<Turn>();
            Flush();
        }

        /// <summary>给面板底部那行说明用。</summary>
        public static string KnowledgeModeText()
        {
            var cfg = AiConfig.Load();
            if (!cfg.UseKnowledge) return "不注入知识库";
            bool full = string.Equals(cfg.KnowledgeMode, "full", StringComparison.OrdinalIgnoreCase);
            return full ? "全文（约 20 万字符）" : "按题命中";
        }
    }
}
