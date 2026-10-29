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

        private AiConfig _cfg;
        private string _mode;            // official | thirdparty
        private string _vendor;
        private int _modelIndex;

        private string _page = "chat";   // chat | config

        private UIText _status;
        private UIPanel _body;           // 模式相关的那一块，切换时整体重建
        private AiInput _url, _model, _key;

        private AiLog _log;              // 对话页的消息流
        private AiInput _ask;            // 对话页的输入框
        private bool _busy;              // 正在等回复（防重复发）

        public override void OnActivate()
        {
            base.OnActivate();
            Current = this;
            _cfg = AiConfig.Load();
            _mode = _cfg.Provider == "thirdparty" ? "thirdparty" : "official";
            _vendor = string.IsNullOrEmpty(_cfg.Vendor) ? "deepseek" : _cfg.Vendor;
            _modelIndex = 0;

            var preset = Providers.Find(_vendor);
            if (preset != null)
                for (int i = 0; i < preset.Models.Length; i++)
                    if (preset.Models[i] == _cfg.Model) _modelIndex = i;

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

            var lab = new UIText("来源", 0.85f);
            lab.Left.Set(240, 0f); lab.Top.Set(y + 7, 0f);
            bg.Append(lab);

            MakeTab(bg, "官方", _mode == "official", 300, y, () => { _mode = "official"; Rebuild(); });
            MakeTab(bg, "第三方", _mode == "thirdparty", 410, y, () => { _mode = "thirdparty"; Rebuild(); });

            y += 46f;

            // 模式相关区
            _body = new UIPanel();
            _body.Width.Set(724, 0f);
            _body.Height.Set(250, 0f);
            _body.Left.Set(18, 0f); _body.Top.Set(y, 0f);
            _body.BackgroundColor = new Color(18, 22, 32) * 0.6f;
            _body.BorderColor = new Color(60, 70, 100);
            bg.Append(_body);

            if (_mode == "official") BuildOfficial();
            else BuildThirdParty();

            y += 258f;

            // 密钥（两种模式共用）
            var klab = new UIText("API 密钥", 0.85f);
            klab.Left.Set(18, 0f); klab.Top.Set(y + 7, 0f);
            bg.Append(klab);

            _key = new AiInput("粘贴或输入 key（Ctrl+V）");
            _key.Password = true;
            _key.MaxLength = 400;
            _key.Text = _cfg.ApiKey ?? "";
            _key.Width.Set(560, 0f);
            _key.Left.Set(120, 0f); _key.Top.Set(y, 0f);
            bg.Append(_key);
            _inputs.Add(_key);

            y += 42f;

            _status = new UIText("", 0.8f);
            _status.Left.Set(18, 0f); _status.Top.Set(y, 0f);
            _status.TextColor = new Color(160, 200, 255);
            bg.Append(_status);

            y += 26f;

            MakeButton(bg, "保存", 18, y, 110, new Color(50, 90, 60), () => Save());
            MakeButton(bg, "测试连接", 140, y, 130, new Color(50, 70, 110), () => TestConn());
            MakeButton(bg, "关闭 (F7)", 282, y, 130, new Color(90, 50, 50), () => AiSystem.TogglePanel(false));

            var note = new UIText(
                "官方档：先选厂商，再点模型名。第三方档：填完整的 chat/completions 地址。\n" +
                "配置存在 ModConfigs/DshAiMini.json，可直接改文件。密钥只在本机请求头里用。",
                0.76f);
            note.Left.Set(18, 0f); note.Top.Set(y + 46, 0f);
            note.TextColor = new Color(140, 148, 165);
            bg.Append(note);
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
            if (string.IsNullOrWhiteSpace(cfg.BaseUrl)) { Say("先去配置页填接口地址", new Color(255, 170, 150)); return; }
            if (string.IsNullOrWhiteSpace(cfg.ApiKey)) { Say("先去配置页填 API 密钥", new Color(255, 170, 150)); return; }

            _ask.Text = "";
            _busy = true;

            _log.Add(AiLog.Kind.User, q);
            _log.Wrap();
            ChatStore.Append("user", q);

            bool full = string.Equals(cfg.KnowledgeMode, "full", System.StringComparison.OrdinalIgnoreCase);
            string trace = "思考… " + cfg.Model
                         + (cfg.UseKnowledge ? (full ? " · 知识库全文" : " · 知识库按题命中") : " · 无知识库")
                         + (cfg.IncludeGameState ? " · 带实时状态" : "");
            _log.Add(AiLog.Kind.Think, trace);
            _log.Wrap();
            Say("已发出，等回复…", new Color(180, 220, 255));

            AiChat.SendAsync(cfg, null, q, (ok, msg) =>
            {
                _busy = false;
                if (ok)
                {
                    _log.Add(AiLog.Kind.Answer, msg);
                    ChatStore.Append("assistant", msg);
                    Say("回复完成（" + msg.Length + " 字）", new Color(150, 255, 170));
                }
                else
                {
                    _log.Add(AiLog.Kind.Error, msg);
                    Say("失败：" + msg, new Color(255, 150, 150));
                }
                _log.Wrap();
            });
        }

        private void BuildOfficial()
        {
            var p0 = new UIText("厂商", 0.82f);
            p0.Left.Set(12, 0f); p0.Top.Set(12, 0f);
            _body.Append(p0);

            float yy = 34f;
            foreach (var preset in Providers.Official)
            {
                var key = preset.Key;
                MakeButton(_body, preset.Label, 12, yy, 200, 
                    _vendor == key ? new Color(60, 100, 150) : new Color(40, 46, 60),
                    () => { _vendor = key; _modelIndex = 0; Rebuild(); });
                yy += 34f;
            }

            var p1 = new UIText("模型", 0.82f);
            p1.Left.Set(240, 0f); p1.Top.Set(12, 0f);
            _body.Append(p1);

            var cur = Providers.Find(_vendor);
            if (cur != null)
            {
                float my = 34f;
                for (int i = 0; i < cur.Models.Length; i++)
                {
                    int idx = i;
                    MakeButton(_body, cur.Models[i], 240, my, 200,
                        _modelIndex == idx ? new Color(60, 100, 150) : new Color(40, 46, 60),
                        () => { _modelIndex = idx; Rebuild(); });
                    my += 34f;
                }

                // 地址行必须落在"两列里更长的那个"下面，否则会压在厂商按钮上。
                // 厂商 4 个按钮占 34..170，模型可能只有 2 个占 34..102，
                // 早先按模型那列算 my+8=110，正好盖住第三个厂商按钮。
                float vendorBottom = 34f + Providers.Official.Length * 34f;
                float addrTop = Math.Max(vendorBottom, my) + 10f;
                var addr = new UIText("地址：" + cur.BaseUrl, 0.72f);
                addr.Left.Set(12, 0f); addr.Top.Set(addrTop, 0f);
                addr.TextColor = new Color(140, 150, 170);
                _body.Append(addr);
            }
        }

        private void BuildThirdParty()
        {
            float y = 12f;

            var p0 = new UIText("接口地址（完整 chat/completions）", 0.82f);
            p0.Left.Set(12, 0f); p0.Top.Set(y, 0f);
            _body.Append(p0);

            _url = new AiInput("https://…/v1/chat/completions");
            _url.Text = _cfg.BaseUrl ?? "";
            _url.Width.Set(660, 0f);
            _url.Left.Set(12, 0f); _url.Top.Set(y + 24, 0f);
            _body.Append(_url);
            _inputs.Add(_url);

            y += 72f;

            var p1 = new UIText("模型名", 0.82f);
            p1.Left.Set(12, 0f); p1.Top.Set(y, 0f);
            _body.Append(p1);

            _model = new AiInput("中转那边要求的模型 id");
            _model.Text = _cfg.Model ?? "";
            _model.Width.Set(660, 0f);
            _model.Left.Set(12, 0f); _model.Top.Set(y + 24, 0f);
            _body.Append(_model);
            _inputs.Add(_model);

            y += 72f;

            var tip = new UIText("大多数中转都兼容 OpenAI 的 /v1/chat/completions，地址填到 completions 为止。", 0.74f);
            tip.Left.Set(12, 0f); tip.Top.Set(y, 0f);
            tip.TextColor = new Color(140, 148, 165);
            _body.Append(tip);
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

        private AiConfig Collect()
        {
            _cfg.Provider = _mode;
            _cfg.Vendor = _vendor;
            _cfg.ApiKey = (_key?.Text ?? "").Trim();

            if (_mode == "official")
            {
                var p = Providers.Find(_vendor);
                if (p != null)
                {
                    _cfg.BaseUrl = p.BaseUrl;
                    _cfg.Model = p.Models[Math.Clamp(_modelIndex, 0, p.Models.Length - 1)];
                }
            }
            else
            {
                _cfg.BaseUrl = (_url?.Text ?? "").Trim();
                _cfg.Model = (_model?.Text ?? "").Trim();
            }
            return _cfg;
        }

        private void Save()
        {
            var cfg = Collect();
            cfg.Save();
            Say("已保存：" + cfg.Model + " @ " + ShortUrl(cfg.BaseUrl), new Color(150, 255, 170));
        }

        private void TestConn()
        {
            var cfg = Collect();
            cfg.Save();
            if (string.IsNullOrWhiteSpace(cfg.BaseUrl)) { Say("先把接口地址填上", new Color(255, 170, 150)); return; }
            if (string.IsNullOrWhiteSpace(cfg.Model)) { Say("先选/填模型名", new Color(255, 170, 150)); return; }
            if (string.IsNullOrWhiteSpace(cfg.ApiKey)) { Say("先把 API 密钥填上", new Color(255, 170, 150)); return; }

            Say("正在连接 " + cfg.Model + " …", new Color(180, 220, 255));
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
    }
}
