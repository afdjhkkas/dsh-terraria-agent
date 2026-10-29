using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace DshAiMini
{
    /// <summary>
    /// 输入框：自己接管键盘。
    ///
    /// 为什么不用现成的：tModLoader 里那个 UIInputTextField 是 internal，
    /// UITextBox 又不处理按键。而这里的输入全是 ASCII（URL / key / 模型名），
    /// 不需要输入法，自己映射键码反而最省事、最好控。
    /// </summary>
    internal sealed class AiInput : UIElement
    {
        public string Text = "";
        public string Hint;
        public int MaxLength = 300;
        public bool Password;
        public Action OnChanged;

        /// <summary>按回车时调用。设了它就代表这个框是"输入框 + 提交"，回车直接发出。</summary>
        public Action OnSubmit;

        private bool _focused;
        private static readonly HashSet<Microsoft.Xna.Framework.Input.Keys> Prev = new();

        public AiInput(string hint) { Hint = hint; Height.Set(34, 0f); }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            Focus();
        }

        public void Focus()
        {
            if (AiPanel.Current != null) AiPanel.Current.BlurAll();
            _focused = true;
            Prev.Clear();
        }

        public void Blur() => _focused = false;

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!_focused) return;

            var ks = Main.keyState;
            bool shift = ks.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift)
                      || ks.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);

            foreach (var k in ks.GetPressedKeys())
            {
                if (Prev.Contains(k)) continue;
                Prev.Add(k);

                if (k == Microsoft.Xna.Framework.Input.Keys.Back)
                {
                    if (Text.Length > 0) Text = Text.Substring(0, Text.Length - 1);
                }
                else if (k == Microsoft.Xna.Framework.Input.Keys.V
                         && (ks.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl)
                          || ks.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl)))
                {
                    string clip = AiClipboard.GetText();
                    if (!string.IsNullOrEmpty(clip))
                    {
                        clip = clip.Replace("\r", "").Replace("\n", "").Trim();
                        int room = MaxLength - Text.Length;
                        if (room > 0) Text += clip.Length <= room ? clip : clip.Substring(0, room);
                    }
                }
                else if (k == Microsoft.Xna.Framework.Input.Keys.Enter)
                {
                    if (OnSubmit != null) OnSubmit();
                    else _focused = false;
                }
                else if (k == Microsoft.Xna.Framework.Input.Keys.Escape)
                {
                    _focused = false;
                }
                else
                {
                    char? c = Map(k, shift);
                    if (c.HasValue && Text.Length < MaxLength) Text += c.Value;
                }
                OnChanged?.Invoke();
            }

            // 松开过的键从"上一帧按下"里移除
            var still = new HashSet<Microsoft.Xna.Framework.Input.Keys>(ks.GetPressedKeys());
            Prev.RemoveWhere(k => !still.Contains(k));
        }

        private static char? Map(Microsoft.Xna.Framework.Input.Keys k, bool shift)
        {
            if (k >= Microsoft.Xna.Framework.Input.Keys.A && k <= Microsoft.Xna.Framework.Input.Keys.Z)
            {
                char c = (char)('a' + (k - Microsoft.Xna.Framework.Input.Keys.A));
                return shift ? char.ToUpperInvariant(c) : c;
            }
            if (k >= Microsoft.Xna.Framework.Input.Keys.D0 && k <= Microsoft.Xna.Framework.Input.Keys.D9)
            {
                char d = (char)('0' + (k - Microsoft.Xna.Framework.Input.Keys.D0));
                if (!shift) return d;
                return d switch { '1' => '!', '2' => '@', '3' => '#', '4' => '$', '5' => '%',
                                  '6' => '^', '7' => '&', '8' => '*', '9' => '(', '0' => ')', _ => d };
            }
            if (k == Microsoft.Xna.Framework.Input.Keys.OemMinus) return shift ? '_' : '-';
            if (k == Microsoft.Xna.Framework.Input.Keys.OemPlus) return shift ? '+' : '=';
            if (k == Microsoft.Xna.Framework.Input.Keys.OemOpenBrackets) return shift ? '{' : '[';
            if (k == Microsoft.Xna.Framework.Input.Keys.OemCloseBrackets) return shift ? '}' : ']';
            if (k == Microsoft.Xna.Framework.Input.Keys.OemPipe) return shift ? '|' : '\\';
            if (k == Microsoft.Xna.Framework.Input.Keys.OemSemicolon) return shift ? ':' : ';';
            if (k == Microsoft.Xna.Framework.Input.Keys.OemQuotes) return shift ? '"' : '\'';
            if (k == Microsoft.Xna.Framework.Input.Keys.OemComma) return shift ? '<' : ',';
            if (k == Microsoft.Xna.Framework.Input.Keys.OemPeriod) return shift ? '>' : '.';
            if (k == Microsoft.Xna.Framework.Input.Keys.OemQuestion) return shift ? '?' : '/';
            if (k == Microsoft.Xna.Framework.Input.Keys.Space) return ' ';
            return null;
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);
            var dims = GetDimensions().ToRectangle();

            Terraria.Utils.DrawInvBG(sb, dims, _focused ? new Color(60, 80, 120) * 0.9f : new Color(40, 44, 60) * 0.85f);
            var px = TextureAssets.MagicPixel.Value;
            Color edge = _focused ? Color.Yellow : new Color(90, 100, 130);
            sb.Draw(px, new Rectangle(dims.X, dims.Y, dims.Width, 2), edge);
            sb.Draw(px, new Rectangle(dims.X, dims.Bottom - 2, dims.Width, 2), edge);
            sb.Draw(px, new Rectangle(dims.X, dims.Y, 2, dims.Height), edge);
            sb.Draw(px, new Rectangle(dims.Right - 2, dims.Y, 2, dims.Height), edge);

            string shown = Password && Text.Length > 0 ? new string('*', Math.Min(Text.Length, 40)) : Text;
            string line = string.IsNullOrEmpty(shown) ? Hint : shown;
            Color col = string.IsNullOrEmpty(shown) ? new Color(140, 140, 150) : Color.White;

            // 超长就显示尾部，让人看得见刚敲的字
            float scale = 0.85f;
            var font = FontAssets.MouseText.Value;
            float maxW = dims.Width - 16f;
            string render = line;
            while (render.Length > 1 && font.MeasureString(render).X * scale > maxW)
                render = render.Substring(1);

            Terraria.Utils.DrawBorderString(sb, render, new Vector2(dims.X + 8f, dims.Y + 7f), col, scale);

            if (_focused && (int)(Main.GlobalTimeWrappedHourly * 2f) % 2 == 0)
            {
                float w = font.MeasureString(render).X * scale;
                sb.Draw(px, new Rectangle((int)(dims.X + 8f + w), dims.Y + 7, 2, dims.Height - 14), Color.White);
            }
        }
    }


    /// <summary>
    /// 官方与第三方的预设。列表本身没有魔法，只是把常见地址和模型名摊开，
    /// 免得每次都要背 URL。第三方那一档故意留空，交给用户自己填。
    /// </summary>
    internal static class Providers
    {
        internal sealed class Preset
        {
            public string Key, Label, BaseUrl;
            public string[] Models;
        }

        public static readonly Preset[] Official =
        {
            new Preset { Key = "deepseek", Label = "DeepSeek 官方",
                BaseUrl = "https://api.deepseek.com/v1/chat/completions",
                Models = new[] { "deepseek-chat", "deepseek-reasoner" } },
            new Preset { Key = "openai", Label = "OpenAI 官方",
                BaseUrl = "https://api.openai.com/v1/chat/completions",
                Models = new[] { "gpt-4o-mini", "gpt-4o", "o3-mini" } },
            new Preset { Key = "dashscope", Label = "阿里通义（兼容模式）",
                BaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
                Models = new[] { "qwen-plus", "qwen-max", "qwen-turbo" } },
            new Preset { Key = "moonshot", Label = "月之暗面 Kimi",
                BaseUrl = "https://api.moonshot.cn/v1/chat/completions",
                Models = new[] { "moonshot-v1-8k", "moonshot-v1-32k" } },
        };

        public static Preset Find(string key)
        {
            foreach (var p in Official) if (p.Key == key) return p;
            return null;
        }

        public static Preset Guess(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) return null;
            foreach (var p in Official)
                if (baseUrl.IndexOf(p.Key, System.StringComparison.OrdinalIgnoreCase) >= 0) return p;
            return null;
        }
    }

    /// <summary>
    /// 面板本体。按「官方 / 第三方」两档组织，和 DSH 自己的模型配置一个思路：
    /// 预设能覆盖的就不让人手打，覆盖不到的才留输入框。
    /// </summary>
    internal sealed class AiPanel : UIState
    {
        public static AiPanel Current;
        private readonly List<AiInput> _inputs = new();

        private AiConfig _cfg;           // 磁盘上的真配置（只读用途）
        private AiConfig _sync;          // 面板上正在编辑的副本

        private string _page = "chat";   // chat | config

        private UIText _status;

        private AiLog _log;              // 对话页的消息流
        private AiInput _ask;            // 对话页的输入框
        private bool _busy;              // 正在等回复（防重复发）

        public override void OnActivate()
        {
            base.OnActivate();
            Current = this;
            _cfg = AiConfig.Load();
            _sync = _cfg.Clone();   // 面板上改的是副本，点保存才写回
            Rebuild();
        }

        private void Rebuild()
        {
            _inputs.Clear();
            RemoveAllChildren();

            var bg = new UIPanel();
            bg.Width.Set(760, 0f);
            bg.Height.Set(470, 0f);
            bg.HAlign = 0.5f;
            bg.VAlign = 0.5f;
            bg.BackgroundColor = new Color(22, 26, 38) * 0.97f;
            bg.BorderColor = new Color(90, 110, 160);
            Append(bg);

            var title = new UIText("DSH AI Mini", 1.02f, true);
            title.Left.Set(18, 0f); title.Top.Set(12, 0f);
            bg.Append(title);

            // 两个页面：对话 / 配置。默认停在对话页 —— 打开就想问东西，不是想改配置。
            float y = 52f;
            MakeTab(bg, "对话", _page == "chat", 18, y, () => { _page = "chat"; Rebuild(); });
            MakeTab(bg, "配置", _page == "config", 128, y, () => { _page = "config"; Rebuild(); });

            if (_page == "chat") { BuildChat(bg); return; }

            BuildConfig(bg);
        }

        /// <summary>
        /// 对话页。上面是消息流，下面一行输入框 + 发送。
        ///
        /// 行为对齐我在 DSH 里跟人对话的样子：问完立刻回显"你：xxx"，
        /// 然后出现一行动作提示（用了哪个模型、是否带了知识库和实时状态），
        /// 最后是回答。这样任何一步卡住都能看出卡在哪。
        /// </summary>
        private void BuildChat(UIElement bg)
        {
            float y = 96f;

            // 消息区
            var box = new UIPanel();
            box.Width.Set(724, 0f);
            box.Height.Set(300, 0f);
            box.Left.Set(18, 0f); box.Top.Set(y, 0f);
            box.BackgroundColor = new Color(14, 17, 26) * 0.9f;
            box.BorderColor = new Color(60, 70, 100);
            bg.Append(box);

            _log = new AiLog();
            _log.Width.Set(708, 0f);
            _log.Height.Set(292, 0f);
            _log.Left.Set(8, 0f); _log.Top.Set(4, 0f);
            _log.LineHeight = 19f;
            _log.Visible = 15;
            box.Append(_log);

            // 之前聊过的恢复出来
            for (int i = 0; i < ChatStore.Turns.Count; i++)
            {
                var t = ChatStore.Turns[i];
                _log.Add(t.Role == "user" ? AiLog.Kind.User : AiLog.Kind.Answer, t.Text);
            }
            if (ChatStore.Turns.Count == 0)
            {
                _log.Add(AiLog.Kind.Think, "问点什么试试。例如：我现在该干什么 / 怎么挖矿 / 这个 boss 打得过吗");
            }
            _log.Wrap();

            y += 310f;

            // 输入框 + 发送
            _ask = new AiInput("输入问题，回车发送");
            _ask.MaxLength = 500;
            _ask.Width.Set(600, 0f);
            _ask.Left.Set(18, 0f); _ask.Top.Set(y, 0f);
            _ask.OnSubmit = Send;
            bg.Append(_ask);
            _inputs.Add(_ask);

            MakeButton(bg, "发送", 628, y + 3, 114, new Color(50, 90, 60), Send);

            y += 42f;

            _status = new UIText("", 0.8f);
            _status.Left.Set(18, 0f); _status.Top.Set(y, 0f);
            _status.TextColor = new Color(160, 200, 255);
            bg.Append(_status);

            y += 26f;

            MakeButton(bg, "清空历史", 18, y, 110, new Color(70, 60, 50), () =>
            {
                ChatStore.Clear();
                _log.Clear();
                _log.Add(AiLog.Kind.Think, "历史已清空。");
                Say("已清空", new Color(255, 220, 160));
            });
            MakeButton(bg, "关闭 (F7)", 140, y, 110, new Color(90, 50, 50), () => AiSystem.TogglePanel(false));

            var note = new UIText(
                "知识库已编进 mod（16 篇）。当前模式：" + (ChatStore.KnowledgeModeText()),
                0.75f);
            note.Left.Set(264, 0f); note.Top.Set(y + 4, 0f);
            note.TextColor = new Color(140, 148, 165);
            bg.Append(note);
        }

        /// <summary>
        /// 发一轮对话。这里不 await，全部走 AiHttp 的回调，
        /// 因为游戏主线程不能等网络 —— 等的时候画面还得照常跑。
        /// </summary>
        private void Send()
        {
            if (_busy) { Say("上一句还在等回复…", new Color(255, 200, 150)); return; }
            if (_log == null || _ask == null) return;

            string q = (_ask.Text ?? "").Trim();
            if (q.Length == 0) { Say("先说一句想问的", new Color(255, 200, 150)); return; }

            var cfg = AiConfig.Load();
            if (string.IsNullOrWhiteSpace(cfg.BaseUrlResolved)) { Say("先去配置页填接口地址", new Color(255, 170, 150)); return; }
            if (string.IsNullOrWhiteSpace(cfg.ApiKeyResolved)) { Say("先去配置页填 API 密钥", new Color(255, 170, 150)); return; }

            _ask.Text = "";
            _busy = true;

            _log.Add(AiLog.Kind.User, q);
            _log.Wrap();
            ChatStore.Append("user", q);

            bool full = string.Equals(cfg.KnowledgeMode, "full", System.StringComparison.OrdinalIgnoreCase);
            string trace = "思考… " + cfg.ModelResolved
                         + (cfg.UseKnowledge ? (full ? " · 知识库全文" : " · 知识库按题命中") : " · 无知识库")
                         + (cfg.IncludeGameState ? " · 带实时状态" : "");
            _log.Add(AiLog.Kind.Think, trace);
            _log.Wrap();
            Say("已发出，等回复…", new Color(180, 220, 255));

            AiAgent.Ask(cfg, q,
                (kind, line) =>
                {
                    // 过程实时推上来：它在做什么，玩家当场就能看到
                    _log.Add(kind, line);
                    _log.Wrap();
                    Say(AiToolExec.Busy ? ("执行中：" + AiToolExec.Note) : "思考中…", new Color(180, 220, 255));
                },
                msg =>
                {
                    _busy = false;
                    bool bad = msg.StartsWith("[") || msg.StartsWith("失败");
                    _log.Add(bad ? AiLog.Kind.Error : AiLog.Kind.Answer, msg);
                    _log.Wrap();
                    ChatStore.Append("assistant", msg);
                    Say(bad ? ("失败：" + Short(msg)) : ("回复完成（" + msg.Length + " 字）"),
                        bad ? new Color(255, 150, 150) : new Color(150, 255, 170));
                _log.Wrap();
            });
        }

        // ===== 配置页：提供商列表 + 详情 =====
        // 照 DSH 那套：左边一列提供商（点一下切换），右边是它的全部设置。
        // 之所以不是一个扁平表单：真实使用里人手上会有"官方 key 一个、中转一个、公司代理一个"，
        // 扁平表单每换一次都要重填地址，很容易把 A 的 key 配到 B 的地址上。
        private void BuildConfig(UIElement bg)
        {
            float top = 104f;

            var listLab = new UIText("提供商", 0.82f);
            listLab.Left.Set(18, 0f); listLab.Top.Set(top - 22, 0f);
            listLab.TextColor = new Color(150, 160, 185);
            bg.Append(listLab);

            var box = new UIPanel();
            box.Width.Set(206, 0f); box.Height.Set(290, 0f);
            box.Left.Set(18, 0f); box.Top.Set(top, 0f);
            box.BackgroundColor = new Color(18, 22, 32) * 0.75f;
            box.BorderColor = new Color(60, 70, 100);
            bg.Append(box);

            float yy = 8f;
            for (int i = 0; i < _sync.Providers.Count; i++)
            {
                int idx = i;
                var pv = _sync.Providers[i];
                bool on = idx == _sync.ActiveIndex;

                var row = new UIPanel();
                row.Width.Set(190, 0f); row.Height.Set(30, 0f);
                row.Left.Set(8, 0f); row.Top.Set(yy, 0f);
                row.BackgroundColor = on ? new Color(48, 78, 126) : new Color(28, 34, 48);
                row.BorderColor = on ? new Color(90, 140, 210) : new Color(48, 56, 76);
                row.OnLeftClick += (_, __) => { _sync.ActiveIndex = idx; Rebuild(); };
                box.Append(row);

                var nm = new UIText(string.IsNullOrEmpty(pv.DisplayName) ? "(未命名)" : pv.DisplayName, 0.8f);
                nm.Left.Set(10, 0f); nm.Top.Set(7, 0f);
                nm.TextColor = on ? Color.White : new Color(190, 198, 215);
                row.Append(nm);

                var kd = new UIText(pv.IsOfficial ? "官方" : "自定义", 0.68f);
                kd.Left.Set(pv.IsOfficial ? 132 : 120, 0f); kd.Top.Set(9, 0f);
                kd.TextColor = new Color(130, 140, 165);
                row.Append(kd);

                // 有 key 才点亮那个小绿点 —— 一眼看出哪套是能用的
                if (pv.HasKey)
                {
                    var dot = new UIText("●", 0.75f);
                    dot.Left.Set(172, 0f); dot.Top.Set(7, 0f);
                    dot.TextColor = new Color(90, 220, 120);
                    row.Append(dot);
                }

                yy += 34f;
            }

            MakeButton(box, "+ 添加提供商", 8, 248, 190, new Color(40, 70, 50), () => { AddProvider(); Rebuild(); });

            if (_sync.Active != null) BuildProviderDetail(bg, 240, top);

            float by = 408f;
            MakeButton(bg, "保存", 18, by, 110, new Color(50, 90, 60), Save);
            MakeButton(bg, "测试连接", 140, by, 130, new Color(50, 70, 110), TestConn);
            MakeButton(bg, "关闭 (F7)", 282, by, 130, new Color(90, 50, 50), () => AiSystem.TogglePanel(false));

            var note = new UIText(
                "每套提供商单独存地址、协议、密钥和模型目录，切换不用重填。\n" +
                "配置在 ModConfigs/DshAiMini.json，可直接改文件。密钥只在本机请求头里用。",
                0.74f);
            note.Left.Set(18, 0f); note.Top.Set(by + 46, 0f);
            note.TextColor = new Color(140, 148, 165);
            bg.Append(note);
        }

        private void BuildProviderDetail(UIElement bg, float x, float top)
        {
            var pv = _sync.Active;
            if (pv == null) return;

            var hd = new UIText((string.IsNullOrEmpty(pv.DisplayName) ? "(未命名)" : pv.DisplayName)
                                + (pv.IsOfficial ? "   · 官方" : "   · 自定义"), 0.92f);
            hd.Left.Set(x, 0f); hd.Top.Set(top - 24, 0f);
            hd.TextColor = pv.HasKey ? new Color(150, 230, 160) : new Color(205, 195, 155);
            bg.Append(hd);

            float y = top;

            Lab(bg, "API 密钥", x, y + 9);
            var ky = new AiInput("留空使用已保存凭据");
            ky.Password = true; ky.MaxLength = 400;
            ky.Text = pv.ApiKey ?? "";
            ky.Width.Set(488, 0f);
            ky.Left.Set(x + 96, 0f); ky.Top.Set(y, 0f);
            ky.OnChanged = () => pv.ApiKey = ky.Text;
            bg.Append(ky); _inputs.Add(ky);
            y += 40f;

            Lab(bg, "显示名称", x, y + 9);
            var nmIn = new AiInput("给这套配置起个名");
            nmIn.MaxLength = 40;
            nmIn.Text = pv.DisplayName ?? "";
            nmIn.Width.Set(400, 0f);
            nmIn.Left.Set(x + 96, 0f); nmIn.Top.Set(y, 0f);
            nmIn.OnChanged = () => pv.DisplayName = nmIn.Text;
            bg.Append(nmIn); _inputs.Add(nmIn);
            y += 40f;

            Lab(bg, "API 地址", x, y + 9);
            var url = new AiInput("https://api.example.com/v1");
            url.MaxLength = 300;
            url.Text = pv.BaseUrl ?? "";
            url.Width.Set(488, 0f);
            url.Left.Set(x + 96, 0f); url.Top.Set(y, 0f);
            url.OnChanged = () => pv.BaseUrl = url.Text;
            bg.Append(url); _inputs.Add(url);
            y += 40f;

            Lab(bg, "API 协议", x, y + 9);
            MakeButton(bg, pv.Protocol == "anthropic" ? "Anthropic Messages" : "OpenAI Chat Completions",
                x + 96, y, 250, new Color(36, 44, 64), () =>
                {
                    pv.Protocol = pv.Protocol == "anthropic" ? "openai" : "anthropic";
                    Rebuild();
                });
            y += 42f;

            Lab(bg, "模型目录", x, y + 6);
            var rst = new UIText("恢复默认模型", 0.74f);
            rst.Left.Set(x + 380, 0f); rst.Top.Set(y + 6, 0f);
            rst.TextColor = new Color(140, 190, 240);
            rst.OnLeftClick += (_, __) =>
            {
                var d = DefaultModelsFor(pv.Key);
                if (d.Count > 0) { pv.Models = d; pv.ActiveModel = d[0].Id; }
                Rebuild();
            };
            bg.Append(rst);
            y += 26f;

            for (int i = 0; i < pv.Models.Count; i++)
            {
                int mi = i;
                var m = pv.Models[i];

                var idIn = new AiInput("模型 id");
                idIn.MaxLength = 120;
                idIn.Text = m.Id ?? "";
                idIn.Width.Set(206, 0f);
                idIn.Left.Set(x, 0f); idIn.Top.Set(y, 0f);
                idIn.OnChanged = () => m.Id = idIn.Text;
                bg.Append(idIn); _inputs.Add(idIn);

                var lbIn = new AiInput("显示名");
                lbIn.MaxLength = 60;
                lbIn.Text = m.Label ?? "";
                lbIn.Width.Set(206, 0f);
                lbIn.Left.Set(x + 214, 0f); lbIn.Top.Set(y, 0f);
                lbIn.OnChanged = () => m.Label = lbIn.Text;
                bg.Append(lbIn); _inputs.Add(lbIn);

                bool isCur = pv.ActiveModel == m.Id;
                MakeButton(bg, isCur ? "当前" : "用这个", x + 428, y, 56,
                    isCur ? new Color(50, 90, 60) : new Color(36, 44, 64),
                    () => { pv.ActiveModel = m.Id; Rebuild(); });

                MakeButton(bg, "删", x + 490, y, 38, new Color(92, 48, 48), () =>
                {
                    if (mi < pv.Models.Count) pv.Models.RemoveAt(mi);
                    if (pv.ActiveModel == m.Id)
                        pv.ActiveModel = pv.Models.Count > 0 ? pv.Models[0].Id : "";
                    Rebuild();
                });

                y += 36f;
            }

            MakeButton(bg, "+ 添加模型", x, y, 120, new Color(40, 70, 50), () =>
            {
                pv.Models.Add(new AiModel("", ""));
                Rebuild();
            });
        }

        static List<AiModel> DefaultModelsFor(string key)
        {
            foreach (var d in AiConfig.Defaults())
                if (d.Key == key)
                {
                    var copy = new List<AiModel>();
                    foreach (var m in d.Models) copy.Add(m.Clone());
                    return copy;
                }
            return new List<AiModel>();
        }

        void AddProvider()
        {
            _sync.Providers.Add(new AiProvider
            {
                Key = "custom-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                DisplayName = "新提供商 " + (_sync.Providers.Count + 1),
                Kind = "custom",
                BaseUrl = "",
                Protocol = "openai",
                Models = new List<AiModel> { new AiModel("", "") },
            });
            _sync.ActiveIndex = _sync.Providers.Count - 1;
        }

        void Lab(UIElement bg, string s, float x, float y)
        {
            var t = new UIText(s, 0.8f);
            t.Left.Set(x, 0f); t.Top.Set(y, 0f);
            t.TextColor = new Color(160, 168, 190);
            bg.Append(t);
        }

        private void MakeTab(UIElement parent, string text, bool active, float x, float y, Action onClick)
        {
            var t = new UITextPanel<string>(text, 0.86f)
            { BackgroundColor = active ? new Color(60, 100, 150) : new Color(38, 44, 58) };
            t.Width.Set(100, 0f); t.Height.Set(32, 0f);
            t.Left.Set(x, 0f); t.Top.Set(y, 0f);
            t.OnLeftClick += (_, __) => onClick();
            parent.Append(t);
        }

        private void MakeButton(UIElement parent, string text, float x, float y, float w, Color bgc, Action onClick)
        {
            var b = new UITextPanel<string>(text, 0.8f) { BackgroundColor = bgc };
            b.Width.Set(w, 0f); b.Height.Set(28, 0f);
            b.Left.Set(x, 0f); b.Top.Set(y, 0f);
            b.OnLeftClick += (_, __) => onClick();
            parent.Append(b);
        }

        public void BlurAll()
        {
            foreach (var i in _inputs) i.Blur();
        }

        private void Say(string s, Color c)
        {
            if (_status == null) return;
            _status.SetText(s);
            _status.TextColor = c;
        }

        /// <summary>把面板上的副本落成真配置。编辑期一律改 _sync，点保存才写回文件。</summary>
        private AiConfig Commit()
        {
            _sync.Save();
            _cfg = AiConfig.Load();
            return _cfg;
        }

        private void Save()
        {
            var cfg = Commit();
            var ap = cfg.Active;
            if (ap == null) { Say("先添加一个提供商", new Color(255, 170, 150)); return; }
            Say("已保存：" + ap.DisplayName + " · " + cfg.ModelResolved + " @ " + ShortUrl(cfg.BaseUrlResolved),
                new Color(150, 255, 170));
        }

        private void TestConn()
        {
            var cfg = Commit();
            if (string.IsNullOrWhiteSpace(cfg.BaseUrlResolved)) { Say("先把 API 地址填上", new Color(255, 170, 150)); return; }
            if (string.IsNullOrWhiteSpace(cfg.ModelResolved))   { Say("先在模型目录里填一个模型 id", new Color(255, 170, 150)); return; }
            if (string.IsNullOrWhiteSpace(cfg.ApiKeyResolved))  { Say("先把 API 密钥填上", new Color(255, 170, 150)); return; }

            Say("正在连接 " + cfg.ModelResolved + " …", new Color(180, 220, 255));
            AiChat.TestAsync(cfg, (ok, msg) =>
            {
                Say(ok ? "连接正常：" + msg : "失败：" + msg,
                    ok ? new Color(150, 255, 170) : new Color(255, 150, 150));
            });
        }

        private static string ShortUrl(string u)
        {
            if (string.IsNullOrEmpty(u)) return "";
            try { var uri = new Uri(u); return uri.Host; } catch { return u; }
        }
    
        /// <summary>截短一行放状态条。</summary>
        static string Short(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ").Trim();
            return s.Length > 60 ? s.Substring(0, 60) + "…" : s;
        }
}
}
