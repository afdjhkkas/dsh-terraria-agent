using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria;
using Terraria.ModLoader;

namespace DshAiMini
{
    /// <summary>
    /// 配置：存成普通 JSON 放在 ModConfigs 目录下，人能直接看、直接改。
    ///
    /// 不用 tModLoader 的 ModConfig，是因为那个会自动生成一套 UI，
    /// 而我们要的是自己排布的面板（模型 / 地址 / key / 提示词 分组显示）。
    /// </summary>
    internal sealed class AiConfig
    {
        /// <summary>官方 / 第三方。只是给界面分组用，实际请求只看 BaseUrl 和 Model。</summary>
        public string Provider { get; set; } = "official";

        /// <summary>OpenAI 兼容的 chat/completions 完整地址。</summary>
        public string BaseUrl { get; set; } = "https://api.deepseek.com/v1/chat/completions";

        /// <summary>模型名，例如 deepseek-chat。</summary>
        public string Model { get; set; } = "deepseek-chat";

        /// <summary>API 密钥。存本地明文，只在请求头里用。</summary>
        public string ApiKey { get; set; } = "";

        /// <summary>温度，越大越发散。</summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>单次回复上限 token。0 表示不传。</summary>
        public int MaxTokens { get; set; } = 800;

        /// <summary>请求超时（毫秒）。</summary>
        public int TimeoutMs { get; set; } = 60000;

        /// <summary>是否把游戏状态（血量/位置/时间）附在每轮对话前面。</summary>
        public bool IncludeGameState { get; set; } = true;

        /// <summary>是否加载知识库（docs/kb 下的要点会拼进 system prompt）。</summary>
        public bool UseKnowledge { get; set; } = true;

        [JsonIgnore]
        public bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);

        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static string Dir
        {
            get
            {
                string d = Path.Combine(Main.SavePath, "ModConfigs");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        public static string FilePath => Path.Combine(Dir, "DshAiMini.json");

        private static void Warn(string msg) => ModContent.GetInstance<DshAiMini>()?.Logger?.Warn(msg);

        public static AiConfig Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var text = File.ReadAllText(FilePath);
                    var cfg = JsonSerializer.Deserialize<AiConfig>(text);
                    if (cfg != null) return cfg;
                }
            }
            catch (Exception ex)
            {
                Warn("[ai-mini] 读配置失败，用默认值: " + ex.Message);
            }
            return new AiConfig();
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Opt));
            }
            catch (Exception ex)
            {
                Warn("[ai-mini] 写配置失败: " + ex.Message);
            }
        }
    }
}
