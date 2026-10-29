using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terraria;
using Terraria.ModLoader;

namespace DshAiMini
{
    /// <summary>模型目录里的一行：接口用的 ID + 给人看的名字。</summary>
    /// <summary>
    /// 一个模型实际要操控的对象。
    ///
    /// self = 当前这个 Terraria 客户端里的本机玩家；
    /// bot  = 未来由独立 Terraria 客户端加入世界的队友。
    ///
    /// Bot 配置先落盘，运行时只有 self 能执行，直到对应的第二客户端接入。
    /// 这样切换模型不会有"界面选了队友，实际上却把用户角色动了"的歧义。
    /// </summary>
    internal sealed class AiActorBinding
    {
        public string Id { get; set; } = "self";
        public string Kind { get; set; } = "self";
        public string DisplayName { get; set; } = "我的角色";
        public string PlayerName { get; set; } = "";
        public string ProviderKey { get; set; } = "";
        public string ModelId { get; set; } = "";
        public int BridgePort { get; set; } = 47779;
        public bool Enabled { get; set; } = true;

        [JsonIgnore] public bool IsSelf => string.Equals(Kind, "self", StringComparison.OrdinalIgnoreCase);
        [JsonIgnore] public bool IsBot => string.Equals(Kind, "bot", StringComparison.OrdinalIgnoreCase);

        public AiActorBinding Clone() => new AiActorBinding
        {
            Id = Id,
            Kind = Kind,
            DisplayName = DisplayName,
            PlayerName = PlayerName,
            ProviderKey = ProviderKey,
            ModelId = ModelId,
            BridgePort = BridgePort,
            Enabled = Enabled,
        };
    }

    internal sealed class AiModel
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";

        public AiModel() { }
        public AiModel(string id, string label) { Id = id; Label = label; }

        public AiModel Clone() => new AiModel(Id, Label);
    }

    /// <summary>
    /// 一个提供商：地址 + 协议 + 密钥 + 一串模型。
    ///
    /// 为什么不像原来那样"官方一套字段、第三方另一套字段"：
    /// 两边其实只差要不要显示名称和协议选择，数据是同一种东西。
    /// 分成两套字段会让"切换"变成搬字段，非常容易漏。
    /// </summary>
    internal sealed class AiProvider
    {
        /// <summary>内部唯一键。官方预置用固定名（deepseek-official），自定义用用户给的显示名。</summary>
        public string Key { get; set; } = "";

        /// <summary>显示名称。官方预置写"DeepSeek"，自定义写用户填的。</summary>
        public string DisplayName { get; set; } = "";

        /// <summary>official = 内置预置；custom = 自己加的。</summary>
        public string Kind { get; set; } = "custom";

        public string BaseUrl { get; set; } = "";

        /// <summary>openai = /chat/completions；anthropic = /messages。目前只实现了 openai。</summary>
        public string Protocol { get; set; } = "openai";

        public string ApiKey { get; set; } = "";

        public List<AiModel> Models { get; set; } = new();

        /// <summary>当前选中的模型 Id。</summary>
        public string ActiveModel { get; set; } = "";

        [JsonIgnore] public bool IsOfficial => string.Equals(Kind, "official", StringComparison.OrdinalIgnoreCase);
        [JsonIgnore] public bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);
        [JsonIgnore] public AiModel Active =>
            Models.FirstOrDefault(m => m.Id == ActiveModel) ?? Models.FirstOrDefault();

        public AiProvider Clone() => new AiProvider
        {
            Key = Key, DisplayName = DisplayName, Kind = Kind, BaseUrl = BaseUrl,
            Protocol = Protocol, ApiKey = ApiKey, ActiveModel = ActiveModel,
            Models = Models.Select(m => m.Clone()).ToList(),
        };
    }

    /// <summary>
    /// 配置：存成普通 JSON 放在 ModConfigs 目录下，人能直接看、直接改。
    ///
    /// 不用 tModLoader 的 ModConfig，是因为那个会自动生成一套 UI，
    /// 而我们要的是自己排布的面板（提供商列表 + 详情 + 模型目录）。
    /// </summary>
    internal sealed class AiConfig
    {
        public List<AiProvider> Providers { get; set; } = new();
        public int ActiveActorIndex { get; set; }

        /// <summary>当前用第几个提供商。</summary>
        public int ActiveIndex { get; set; } = 0;

        /// <summary>温度，越大越发散。</summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>单次回复上限 token。0 表示不传。</summary>
        public int MaxTokens { get; set; } = 800;

        /// <summary>请求超时（毫秒）。</summary>
        public int TimeoutMs { get; set; } = 60000;

        /// <summary>是否把游戏状态（血量/位置/时间）附在每轮对话前面。</summary>
        public bool IncludeGameState { get; set; } = true;

        /// <summary>是否加载知识库（编进 dll 的那些要点）。</summary>
        public bool UseKnowledge { get; set; } = true;

        /// <summary>
        /// 知识库注入方式：
        ///   "match" = 只塞这一问命中的那几条（省 token，默认）
        ///   "full"  = 把编进 dll 的全部 16 篇原文塞进去（最能干也最贵）
        /// </summary>
        public string KnowledgeMode { get; set; } = "match";

        // ---- 兼容旧版单提供商字段：读到就迁移成一条 provider ----
        public string Provider { get; set; }
        public string Vendor { get; set; }
        public string BaseUrl { get; set; }
        public string Model { get; set; }
        public string ApiKey { get; set; }

        [JsonIgnore] public AiProvider Active =>
            ActiveIndex >= 0 && ActiveIndex < Providers.Count ? Providers[ActiveIndex] : null;

        [JsonIgnore]
        public string BaseUrlResolved
        {
            get
            {
                var p = Active;
                if (p == null) return "";
                string u = (p.BaseUrl ?? "").Trim();
                if (u.Length == 0) return "";
                // 允许只填到 /v1，自动补上 chat/completions
                if (!u.Contains("/chat/completions") && !u.Contains("/messages"))
                {
                    if (u.EndsWith("/")) u = u.TrimEnd('/');
                    u += "/chat/completions";
                }
                return u;
            }
        }

        [JsonIgnore] public string ModelResolved => Active?.Active?.Id ?? "";
        [JsonIgnore] public string ApiKeyResolved => Active?.ApiKey ?? "";
        [JsonIgnore] public bool HasKey => !string.IsNullOrWhiteSpace(ApiKeyResolved);

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

        /// <summary>全新安装时的预置：DeepSeek 官方 + 一个空的"自定义"。密钥留空，等用户填。</summary>
        public static List<AiProvider> Defaults()
        {
            var list = new List<AiProvider>();

            list.Add(new AiProvider
            {
                Key = "deepseek-official",
                DisplayName = "DeepSeek",
                Kind = "official",
                BaseUrl = "https://api.deepseek.com/v1",
                Protocol = "openai",
                Models = new List<AiModel>
                {
                    new AiModel("deepseek-chat", "DeepSeek Chat"),
                    new AiModel("deepseek-reasoner", "DeepSeek Reasoner"),
                },
                ActiveModel = "deepseek-chat",
            });

            list.Add(new AiProvider
            {
                Key = "openai-official",
                DisplayName = "OpenAI",
                Kind = "official",
                BaseUrl = "https://api.openai.com/v1",
                Protocol = "openai",
                Models = new List<AiModel>
                {
                    new AiModel("gpt-4o-mini", "GPT-4o mini"),
                    new AiModel("gpt-4o", "GPT-4o"),
                },
                ActiveModel = "gpt-4o-mini",
            });

            return list;
        }

        public static AiConfig Load()
        {
            AiConfig cfg = null;
            try
            {
                if (File.Exists(FilePath))
                    cfg = JsonSerializer.Deserialize<AiConfig>(File.ReadAllText(FilePath));
            }
            catch (Exception ex)
            {
                Warn("[ai-mini] 读配置失败，用默认值: " + ex.Message);
            }

            if (cfg == null) cfg = new AiConfig();

            // 旧版配置迁移：只有单套字段、没有 Providers
            if (cfg.Providers == null || cfg.Providers.Count == 0)
            {
                cfg.Providers = Defaults();
                var first = cfg.Providers[0];
                string oldUrl = cfg.BaseUrl;
                string oldModel = cfg.Model;
                string oldKey = cfg.ApiKey;
                if (!string.IsNullOrWhiteSpace(oldUrl)) first.BaseUrl = oldUrl;
                if (!string.IsNullOrWhiteSpace(oldKey)) first.ApiKey = oldKey;
                if (!string.IsNullOrWhiteSpace(oldModel))
                {
                    if (!first.Models.Any(m => m.Id == oldModel))
                        first.Models.Insert(0, new AiModel(oldModel, oldModel));
                    first.ActiveModel = oldModel;
                }
                // 迁移完把旧字段清掉，存盘时不再写它们
                cfg.BaseUrl = cfg.Model = cfg.ApiKey = null;
            }

            if (cfg.ActiveIndex < 0 || cfg.ActiveIndex >= cfg.Providers.Count) cfg.ActiveIndex = 0;
            foreach (var p in cfg.Providers)
            {
                p.Models ??= new List<AiModel>();
                if (string.IsNullOrEmpty(p.ActiveModel) && p.Models.Count > 0) p.ActiveModel = p.Models[0].Id;
            }
            return cfg;
        }

        /// <summary>
        /// 执行对象绑定表。第一版只有 self（本机玩家）；bot 是给"第二个客户端加入世界"预留的，
        /// 现在配好也没有执行者，所以运行时只有 self 会被真正驱动。
        /// </summary>
        public List<AiActorBinding> Actors { get; set; } = new List<AiActorBinding>();

        /// <summary>保证表里至少有一个 self，并把 ActiveActorIndex 夹到合法范围。</summary>
        public void EnsureActors()
        {
            Actors ??= new List<AiActorBinding>();
            if (Actors.Count == 0)
                Actors.Add(new AiActorBinding { Id = "self", Kind = "self", DisplayName = "我的角色" });
            if (ActiveActorIndex < 0 || ActiveActorIndex >= Actors.Count) ActiveActorIndex = 0;
        }

        /// <summary>当前选中的执行对象（永远非 null，必要时现场补一个 self）。</summary>
        [JsonIgnore]
        public AiActorBinding ActiveActor
        {
            get
            {
                EnsureActors();
                return Actors[ActiveActorIndex];
            }
        }

        /// <summary>
        /// 给提示词用的一句话：模型要知道"我现在是在操控谁"。
        /// bot 还没接入时明确写"未接入"，免得模型以为它能动队友。
        /// </summary>
        [JsonIgnore]
        public string ActiveActorDescription
        {
            get
            {
                var a = ActiveActor;
                if (a == null || a.IsSelf) return "我的角色（本机玩家，可以直接操作）";
                string who = string.IsNullOrWhiteSpace(a.PlayerName) ? a.DisplayName : a.PlayerName;
                return who + "（队友角色，当前尚未接入执行器，只能读取状态、不能操作）";
            }
        }

        public void Save()
        {
            try
            {
                EnsureActors();
                if (ActiveIndex < 0 || ActiveIndex >= Providers.Count) ActiveIndex = 0;
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Opt));
            }
            catch (Exception ex)
            {
                Warn("[ai-mini] 写配置失败: " + ex.Message);
            }
        }

        /// <summary>拿一份深拷贝，给面板编辑用 —— 点取消就不写回真配置。</summary>
        public AiConfig Clone() => new AiConfig
        {
            Providers = Providers.Select(p => p.Clone()).ToList(),
            ActiveIndex = ActiveIndex,
            Temperature = Temperature,
            MaxTokens = MaxTokens,
            TimeoutMs = TimeoutMs,
            IncludeGameState = IncludeGameState,
            UseKnowledge = UseKnowledge,
            KnowledgeMode = KnowledgeMode,
        };
    }
}
