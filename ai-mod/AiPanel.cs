using System.Linq;
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

        /// <summary>
        /// 走不走「原版聊天框」这条输入通道。
        ///
        /// 为什么要这么绕：中文输入法的合成结果（拼音 → 汉字）根本不会经过
        /// Main.keyState —— 键盘上只会看到 n/i/h/a/o 这几个字母键。
        /// IME 合成后的文本只有一处能拿到：原版聊天框自己维护的 Main.chatText
        /// （装了 InputMethodFixer 这类模组后它才真的有中文）。
        /// 所以想让玩家在这个面板里直接打中文，就只能借用原版聊天框当输入法桥：
        /// 聚焦时把聊天框打开、文字同步过来，失焦/回车时再关掉。
        /// </summary>
        public bool ImeBridge;


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
            if (ImeBridge)
            {
                // 借用原版聊天框：通过原版入口打开输入状态，
                // 这样 GetInputText/IME 才会在下一帧接管 Main.chatText。
                Main.chatText = Text ?? "";
                try { Main.OpenPlayerChat(); }
                catch
                {
                    // 某些菜单加载阶段原版入口可能暂不可用，保留兼容回退。
                    Main.drawingPlayerChat = true;
                }
                Main.chatRelease = false;
            }
        }

        public bool IsFocused => _focused;

        public void Blur()
        {
            if (!_focused) return;
            _focused = false;
            if (ImeBridge)
            {
                // 关桥。聊天内容已经同步进 Text，不要让它继续挂在游戏聊天框上，
                // 否则玩家关面板后一按回车就把问题发到公屏了。
                Main.chatText = "";
                try { Main.ClosePlayerChat(); }
                catch { Main.drawingPlayerChat = false; }
                Main.chatRelease = false;
            }
            Prev.Clear();
        }

        private void Submit()
        {
            // 提交时再同步一次原版聊天框，避免输入法最后一帧尚未写入 Text。
            if (ImeBridge)
            {
                string live = Main.chatText ?? "";
                if (live.Length > MaxLength) live = live.Substring(0, MaxLength);
                if (live != Text) { Text = live; OnChanged?.Invoke(); }
            }
            // 先释放原版聊天框，再把内容交给面板。
            // 否则同一帧里 PollGameChat 还能看到旧的 Main.chatText，
            // 轻则重复发送，重则把问题发到游戏公屏。
            if (ImeBridge) Blur();
            OnSubmit?.Invoke();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!_focused) return;

            var ks = Main.keyState;

            if (ImeBridge)
            {
                // 文字完全交给原版聊天框（输入法才有活路），这里只做两件事：
                // 1) 把 Main.chatText 同步过来；2) 接住回车/Esc。
                string live = Main.chatText ?? "";
                if (live.Length > MaxLength) live = live.Substring(0, MaxLength);
                if (live != Text) { Text = live; OnChanged?.Invoke(); }

                if (ks.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter) && !Prev.Contains(Microsoft.Xna.Framework.Input.Keys.Enter))
                {
                    Prev.Add(Microsoft.Xna.Framework.Input.Keys.Enter);
                    Submit();
                }
                else if (ks.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape) && !Prev.Contains(Microsoft.Xna.Framework.Input.Keys.Escape))
                {
                    Prev.Add(Microsoft.Xna.Framework.Input.Keys.Escape);
                    Blur();
                }

                var still2 = new HashSet<Microsoft.Xna.Framework.Input.Keys>(ks.GetPressedKeys());
                Prev.RemoveWhere(k => !still2.Contains(k));
                return;
            }

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
                    if (OnSubmit != null) Submit();
                    else _focused = false;
                }
                else if (k == Microsoft.Xna.Framework.Input.Keys.Escape)
                {
                    Blur();
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

        // 面板默认是关闭的，但游戏聊天入口也要能工作。
        // 之前 Current 只在 OnActivate 赋值，玩家不先按 F7 打开一次面板时，
        // AiSystem.PollGameChat 找不到接收者，消息就静默丢掉了。
        public AiPanel()
        {
            Current = this;
        }

        /// <summary>当前获得焦点的输入框（没有就 null）。</summary>
        public AiInput FocusedInput
        {
            get
            {
                foreach (var i in _inputs) if (i.IsFocused) return i;
                return null;
            }
        }
        private readonly List<AiInput> _inputs = new();

        private AiConfig _cfg;           // 磁盘上的真配置（只读用途）
        private AiConfig _sync;          // 面板上正在编辑的副本

        private string _page = "chat";   // chat | config | model

        private UIText _status;

        private AiLog _log;              // 对话页的消息流
        private AiInput _ask;            // 对话页的输入框
        private readonly List<(UIText label, AiActorBinding bot)> _botStatusLabels = new();
        private string _modelNotice = "";
        private Color _modelNoticeColor = new Color(140, 150, 176);
        const double BusyTimeout = 120;  // 秒。超过就当这次请求死了
        private bool _busy;              // 正在等回复（防重复发）
        private double _busySince;       // 开始等的时刻（秒）。超时自动放开，否则一次没回调就能把面板锁死

        public override void OnActivate()
        {
            base.OnActivate();
            Current = this;
            _cfg = AiConfig.Load();
            _sync = _cfg.Clone();   // 面板上改的是副本，点保存才写回
            Rebuild();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_page != "model" || _botStatusLabels.Count == 0 || Main.GameUpdateCount % 30 != 0) return;
            foreach (var entry in _botStatusLabels)
            {
                string role = string.IsNullOrWhiteSpace(entry.bot.PlayerName) ? "角色未设置" : "角色 " + entry.bot.PlayerName;
                entry.label.SetText(AiBotRuntime.StatusText(entry.bot) + " · " + role);
            }
        }

        private void Rebuild()
        {
            _inputs.Clear();
            _botStatusLabels.Clear();
            RemoveAllChildren();

            var bg = new UIPanel();
            bg.Width.Set(820, 0f);
            bg.Height.Set(_page == "chat" ? 520 : 620, 0f);
            bg.HAlign = 0.5f;
            bg.VAlign = 0.5f;
            bg.BackgroundColor = Theme.BgOuter * 0.97f;
            bg.BorderColor = Theme.Line;
            Append(bg);

            var title = new UIText("DSH AI Mini", 1.02f, true);
            title.Left.Set(18, 0f); title.Top.Set(12, 0f);
            title.TextColor = Theme.Title;
            bg.Append(title);

            // 三个页面：对话 / 配置 / 模型与 Bot。默认停在对话页。
            float y = 52f;
            MakeTab(bg, "对话", _page == "chat", 18, y, () => { _page = "chat"; Rebuild(); });
            MakeTab(bg, "配置", _page == "config", 128, y, () => { _page = "config"; Rebuild(); });
            MakeTab(bg, "模型 / Bot", _page == "model", 238, y, () => { _page = "model"; Rebuild(); });

            if (_page == "chat") { BuildChat(bg); return; }
            if (_page == "config") { BuildConfig(bg); return; }
            BuildModelPage(bg);
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
            box.BackgroundColor = Theme.BgInset;
            box.BorderColor = Theme.LineSoft;
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
            _ask = new AiInput("输入问题，回车发送（中文直接打，走游戏聊天框）");
            _ask.ImeBridge = true;
            _ask.MaxLength = 500;
            _ask.Width.Set(600, 0f);
            _ask.Left.Set(18, 0f); _ask.Top.Set(y, 0f);
            _ask.OnSubmit = Send;
            bg.Append(_ask);
            _inputs.Add(_ask);

            MakeBtn(bg, "发送", 628, y + 2, 114, 30, BtnKind.Primary, Send);

            y += 42f;

            _status = new UIText("", 0.8f);
            _status.Left.Set(18, 0f); _status.Top.Set(y, 0f);
            _status.TextColor = new Color(160, 200, 255);
            bg.Append(_status);

            y += 26f;

            MakeBtn(bg, "清空历史", 18, y, 110, 28, BtnKind.Ghost, () =>
            {
                ChatStore.Clear();
                AiChat.ClearHistory();
                _log.Clear();
                _log.Add(AiLog.Kind.Think, "历史已清空。");
                Say("已清空", new Color(255, 220, 160));
            });
            MakeBtn(bg, "关闭 (F7)", 140, y, 110, 28, BtnKind.Danger, () => AiSystem.TogglePanel(false));

            var note = new UIText(
                "知识库已编进 mod（16 篇）。当前模式：" + (ChatStore.KnowledgeModeText()),
                0.75f);
            note.Left.Set(264, 0f); note.Top.Set(y + 4, 0f);
            note.TextColor = new Color(140, 148, 165);
            bg.Append(note);

            var chatHint = new UIText("面板关闭时也可按 Enter 打开原版聊天框提问；回答会回到聊天记录。", 0.7f);
            chatHint.Left.Set(18, 0f); chatHint.Top.Set(y + 30, 0f);
            chatHint.TextColor = new Color(120, 135, 165);
            bg.Append(chatHint);
        }

        /// <summary>
        /// 发一轮对话。这里不 await，全部走 AiHttp 的回调，
        /// 因为游戏主线程不能等网络 —— 等的时候画面还得照常跑。
        /// </summary>
        private void Send()
        {
            if (_ask == null) return;
            string q = (_ask.Text ?? "").Trim();
            if (_ask.ImeBridge && string.IsNullOrWhiteSpace(q))
                q = (Main.chatText ?? "").Trim();
            _ask.Blur();
            if (string.IsNullOrWhiteSpace(q))
            {
                Say("请输入内容后再发送。", new Color(255, 190, 140));
                return;
            }
            Ask(q, false);
        }

        /// <summary>
        /// 从游戏聊天框来的一句话。面板可以关着，所以这里不强依赖 _log / _ask：
        /// 有面板就画进消息区，没有就用原版 NewText 把回答打到聊天框。
        ///
        /// 为什么要有这条：玩家关掉面板之后说的是"我走了"，不是"我要开面板"。
        /// 让他为了问一句话专门按 F7，等于把这个 AI 变成必须开窗口才能用的东西。
        /// </summary>
        public void AskFromGameChat(string q)
        {
            Ask(q, true);
        }

        private void Ask(string q, bool fromGameChat)
        {
            // 卡住自愈：只要回调没来（网络断、key 失效、异常被吞），_busy 就会一直是 true，
            // 之后面板里发什么都没反应，看上去像"界面坏了"。超过 BusyTimeout 直接放开。
            if (_busy && Main.gameTimeCache.TotalGameTime.TotalSeconds - _busySince > BusyTimeout)
            {
                _busy = false;
                AddLog(AiLog.Kind.Error, "上一次请求超过 " + (int)BusyTimeout + " 秒没回来，已强制放弃");
            }
            if (_busy) { Say("上一句还在等回复…", new Color(255, 200, 150)); return; }
            if (q.Length == 0) return;

            var cfg = AiConfig.Load();
            if (string.IsNullOrWhiteSpace(cfg.RuntimeBaseUrlResolved)) { Say("先去配置页填接口地址", new Color(255, 170, 150)); return; }
            if (string.IsNullOrWhiteSpace(cfg.RuntimeApiKeyResolved)) { Say("先去配置页填 API 密钥", new Color(255, 170, 150)); return; }

            if (_ask != null) _ask.Text = "";
            _busy = true;
            _busySince = Main.gameTimeCache.TotalGameTime.TotalSeconds;

            AddLog(AiLog.Kind.User, q);
            ChatStore.Append("user", q);

            bool full = string.Equals(cfg.KnowledgeMode, "full", System.StringComparison.OrdinalIgnoreCase);
            string trace = "思考… " + cfg.RuntimeModelResolved + " → " + cfg.ActiveActorDescription
                         + (cfg.UseKnowledge ? (full ? " · 知识库全文" : " · 知识库按题命中") : " · 无知识库")
                         + (cfg.IncludeGameState ? " · 带实时状态" : "");
            AddLog(AiLog.Kind.Think, trace);
            Say("已发出，等回复…", new Color(180, 220, 255));

            AiAgent.Ask(cfg, q,
                (kind, line) =>
                {
                    // 过程实时推上来：它在做什么，玩家当场就能看到
                    AddLog(kind, line);
                    Say(AiToolExec.Busy ? ("执行中：" + AiToolExec.Note) : "思考中…", new Color(180, 220, 255));
                },
                msg =>
                {
                    _busy = false;
                    bool bad = msg.StartsWith("[") || msg.StartsWith("失败");
                    AddLog(bad ? AiLog.Kind.Error : AiLog.Kind.Answer, msg);
                    ChatStore.Append("assistant", msg);
                    Say(bad ? ("失败：" + Short(msg)) : ("回复完成（" + msg.Length + " 字）"),
                        bad ? new Color(255, 150, 150) : new Color(150, 255, 170));

                    // 从聊天框问的，回答也回到聊天框 —— 玩家没开面板，不能让他去翻面板
                    if (fromGameChat)
                    {
                        Color c = bad ? new Color(255, 150, 150) : new Color(150, 220, 255);
                        foreach (string ln in ClampLines(msg, 3, 110))
                            Main.NewText(ln, c.R, c.G, c.B);
                    }
                });
        }

        /// <summary>没有面板时 _log 是 null，这里统一兜住。</summary>
        void AddLog(AiLog.Kind kind, string line)
        {
            if (_log == null) return;
            _log.Add(kind, line);
            _log.Wrap();
        }

        /// <summary>把长回答按行数与行宽切开，免得一口气刷满聊天框。</summary>
        static List<string> ClampLines(string text, int maxLines, int maxLen)
        {
            var outp = new List<string>();
            if (string.IsNullOrEmpty(text)) return outp;
            string t = text.Replace("\r", "");
            var raw = t.Split('\n');
            foreach (string one in raw)
            {
                string line = one.Trim();
                while (line.Length > maxLen)
                {
                    outp.Add(line.Substring(0, maxLen));
                    line = line.Substring(maxLen);
                    if (outp.Count >= maxLines) return outp;
                }
                if (line.Length > 0) outp.Add(line);
                if (outp.Count >= maxLines) return outp;
            }
            return outp;
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
            box.BackgroundColor = Theme.BgCard;
            box.BorderColor = Theme.LineSoft;
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
                row.BackgroundColor = on ? Theme.AccentDim : Theme.BgInset;
                row.BorderColor = on ? Theme.Accent : Theme.LineSoft;
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

            MakeBtn(box, "+ 添加提供商", 8, 246, 190, 30, BtnKind.Success, () => { AddProvider(); Rebuild(); });

            if (_sync.Active != null) BuildProviderDetail(bg, 232, top);

            float by = 408f;
            MakeBtn(bg, "保存", 18, by, 100, 30, BtnKind.Primary, Save);
            MakeBtn(bg, "测试连接", 126, by, 118, 30, BtnKind.Ghost, TestConn);
            MakeBtn(bg, "关闭 (F7)", 252, by, 110, 30, BtnKind.Danger, () => AiSystem.TogglePanel(false));

            var note = new UIText(
                "每套提供商单独存地址、协议、密钥和模型目录，切换不用重填。\n" +
                "配置在 ModConfigs/DshAiMini.json，可直接改文件。密钥只在本机请求头里用。",
                0.74f);
            note.Left.Set(18, 0f); note.Top.Set(by + 46, 0f);
            note.TextColor = new Color(140, 148, 165);
            bg.Append(note);
        }

        /// <summary>
        /// 模型与队友页：模型是“脑子”，Actor 是“这个脑子要操控谁”。
        /// Bot 使用真实的第二个 tModLoader 客户端；只有 Bridge 确认进世界才算加入成功。
        /// </summary>
        private void BuildModelPage(UIElement bg)
        {
            _sync.EnsureActors();
            // 给说明/状态条留出独立的一行，避免覆盖左右两张卡片的边框。
            float top = 124f;

            var intro = new UIText(
                "Bot 使用真实 tModLoader 客户端加入当前世界；只有“已进世界”才算成功。先填写已有角色名，再点“启动”。",
                0.78f);
            intro.Left.Set(18, 0f); intro.Top.Set(top - 28, 0f);
            intro.TextColor = Theme.Muted;
            bg.Append(intro);

            if (!string.IsNullOrWhiteSpace(_modelNotice))
            {
                var notice = new UIText(_modelNotice, 0.72f);
                notice.Left.Set(18, 0f); notice.Top.Set(top - 8, 0f);
                notice.TextColor = _modelNoticeColor;
                bg.Append(notice);
            }

            var left = new UIPanel();
            left.Width.Set(382, 0f); left.Height.Set(438, 0f);
            left.Left.Set(18, 0f); left.Top.Set(top, 0f);
            left.BackgroundColor = Theme.BgCard;
            left.BorderColor = Theme.LineSoft;
            bg.Append(left);

            var right = new UIPanel();
            right.Width.Set(382, 0f); right.Height.Set(438, 0f);
            right.Left.Set(412, 0f); right.Top.Set(top, 0f);
            right.BackgroundColor = Theme.BgCard;
            right.BorderColor = Theme.LineSoft;
            bg.Append(right);

            var lh = new UIText("可用模型", 0.9f, true);
            lh.Left.Set(12, 0f); lh.Top.Set(10, 0f); lh.TextColor = Theme.Title;
            left.Append(lh);
            var rh = new UIText("执行对象", 0.9f, true);
            rh.Left.Set(12, 0f); rh.Top.Set(10, 0f); rh.TextColor = Theme.Title;
            right.Append(rh);
            MakeBtn(right, "刷新 Bot 状态", 238, 8, 112, 26, BtnKind.Ghost, () => Rebuild());

            var options = ModelOptions();
            var modelList = new UIList { ListPadding = 5f };
            modelList.Width.Set(328, 0f); modelList.Height.Set(370, 0f);
            modelList.Left.Set(12, 0f); modelList.Top.Set(42, 0f);
            left.Append(modelList);
            var modelScroll = new UIScrollbar();
            modelScroll.Height.Set(370, 0f); modelScroll.Left.Set(346, 0f); modelScroll.Top.Set(42, 0f);
            left.Append(modelScroll); modelList.SetScrollbar(modelScroll);
            if (options.Count == 0)
            {
                var empty = new UIText("先在“配置”页添加一个模型。", 0.78f);
                empty.Left.Set(4, 0f); empty.Top.Set(8, 0f); empty.TextColor = Theme.Warn;
                modelList.Add(empty);
            }
            else
            {
                foreach (var opt in options)
                {
                    var row = new UIPanel();
                    row.Width.Set(318, 0f); row.Height.Set(42, 0f);
                    row.Left.Set(0, 0f);
                    row.BackgroundColor = Theme.BgInset;
                    row.BorderColor = Theme.LineSoft;
                    modelList.Add(row);

                    string title = (opt.provider.DisplayName ?? "提供商") + " · " + DisplayModel(opt.model);
                    var tx = new UIText(title, 0.64f);
                    tx.Left.Set(8, 0f); tx.Top.Set(5, 0f); tx.TextColor = Theme.Text;
                    row.Append(tx);

                    MakeBtn(row, "操控我", 192, 11, 72, 28, BtnKind.Primary,
                        () => AssignSelf(opt.provider, opt.model));
                    MakeBtn(row, "+ Bot", 270, 11, 72, 28, BtnKind.Success,
                        () => AddBot(opt.provider, opt.model));
                }
            }

            // 同一场 Host & Play 默认连本机地址；可在这里改目标主机与端口，之后新增的 Bot 会沿用。
            var existingBot = _sync.Actors.FirstOrDefault(a => a != null && a.IsBot);
            var hostInput = new AiInput("Host 地址");
            hostInput.MaxLength = 128;
            hostInput.Text = existingBot?.GameHost ?? "127.0.0.1";
            hostInput.Width.Set(208, 0f); hostInput.Left.Set(12, 0f); hostInput.Top.Set(40, 0f);
            hostInput.OnChanged = () =>
            {
                foreach (var bot in _sync.Actors.Where(a => a != null && a.IsBot)) bot.GameHost = hostInput.Text.Trim();
            };
            right.Append(hostInput); _inputs.Add(hostInput);

            var portInput = new AiInput("端口");
            portInput.MaxLength = 5;
            portInput.Text = (existingBot?.GamePort ?? 7777).ToString();
            portInput.Width.Set(92, 0f); portInput.Left.Set(230, 0f); portInput.Top.Set(40, 0f);
            portInput.OnChanged = () =>
            {
                if (int.TryParse(portInput.Text, out int port) && port > 0 && port <= 65535)
                    foreach (var bot in _sync.Actors.Where(a => a != null && a.IsBot)) bot.GamePort = port;
            };
            right.Append(portInput); _inputs.Add(portInput);

            int selfIndex = _sync.Actors.FindIndex(a => a != null && a.IsSelf);
            if (selfIndex < 0) selfIndex = 0;
            var self = _sync.Actors[selfIndex];
            float sy = 82f;
            var selfRow = new UIPanel();
            selfRow.Width.Set(358, 0f); selfRow.Height.Set(48, 0f);
            selfRow.Left.Set(12, 0f); selfRow.Top.Set(sy, 0f);
            selfRow.BackgroundColor = _sync.ActiveActorIndex == selfIndex ? Theme.AccentDim : Theme.BgInset;
            selfRow.BorderColor = _sync.ActiveActorIndex == selfIndex ? Theme.Accent : Theme.LineSoft;
            right.Append(selfRow);
            var selfText = new UIText("我的角色\n" + BindingModelText(self), 0.72f);
            selfText.Left.Set(10, 0f); selfText.Top.Set(5, 0f); selfText.TextColor = Color.White;
            selfRow.Append(selfText);
            MakeBtn(selfRow, _sync.ActiveActorIndex == selfIndex ? "当前" : "使用", 284, 10, 58, 28,
                _sync.ActiveActorIndex == selfIndex ? BtnKind.Success : BtnKind.Ghost,
                () => { _sync.ActiveActorIndex = selfIndex; SaveAndRebuild(); });
            sy += 56f;

            var bots = _sync.Actors.Where(a => a != null && a.IsBot).ToList();
            var botList = new UIList { ListPadding = 5f };
            botList.Width.Set(328, 0f); botList.Height.Set(280, 0f);
            botList.Left.Set(12, 0f); botList.Top.Set(140, 0f);
            right.Append(botList);
            var botScroll = new UIScrollbar();
            botScroll.Height.Set(280, 0f); botScroll.Left.Set(346, 0f); botScroll.Top.Set(140, 0f);
            right.Append(botScroll); botList.SetScrollbar(botScroll);
            if (bots.Count == 0)
            {
                var empty = new UIText("还没有队友 Bot。\n从左侧模型行点“+ Bot”添加并启动。", 0.74f);
                empty.Left.Set(4, 0f); empty.Top.Set(8, 0f); empty.TextColor = Theme.Muted;
                botList.Add(empty);
            }
            else
            {
                foreach (var bot in bots)
                {
                    int botIndex = _sync.Actors.IndexOf(bot);
                    var row = new UIPanel();
                    row.Width.Set(318, 0f); row.Height.Set(112, 0f);
                    row.Left.Set(0, 0f);
                    row.BackgroundColor = _sync.ActiveActorIndex == botIndex ? Theme.AccentDim : Theme.BgInset;
                    row.BorderColor = _sync.ActiveActorIndex == botIndex ? Theme.Accent : Theme.LineSoft;
                    botList.Add(row);

                    var name = new AiInput("队友名称");
                    name.MaxLength = 32;
                    name.Text = bot.DisplayName ?? "";
                    name.Width.Set(190, 0f); name.Left.Set(8, 0f); name.Top.Set(5, 0f);
                    name.OnChanged = () => bot.DisplayName = name.Text;
                    row.Append(name); _inputs.Add(name);

                    var role = new AiInput("游戏角色名（已有）");
                    role.MaxLength = 80;
                    role.Text = bot.PlayerName ?? "";
                    role.Width.Set(116, 0f); role.Left.Set(194, 0f); role.Top.Set(5, 0f);
                    role.OnChanged = () => bot.PlayerName = role.Text.Trim();
                    row.Append(role); _inputs.Add(role);

                    string roleText = string.IsNullOrWhiteSpace(bot.PlayerName) ? "角色名未设置" : "角色：" + bot.PlayerName;
                    var statusText = new UIText(AiBotRuntime.StatusText(bot) + " · " + roleText, 0.62f);
                    statusText.Left.Set(8, 0f); statusText.Top.Set(39, 0f); statusText.TextColor = Theme.Muted;
                    row.Append(statusText);

                    var portText = new UIText("桥 " + bot.BridgePort + " · " + BindingModelText(bot), 0.58f);
                    portText.Left.Set(8, 0f); portText.Top.Set(57, 0f); portText.TextColor = Theme.Muted;
                    row.Append(portText);
                    _botStatusLabels.Add((statusText, bot));

                    MakeBtn(row, _sync.ActiveActorIndex == botIndex ? "当前" : "使用", 8, 78, 58, 25,
                        _sync.ActiveActorIndex == botIndex ? BtnKind.Success : BtnKind.Ghost,
                        () => { _sync.ActiveActorIndex = botIndex; SaveAndRebuild(); });
                    MakeBtn(row, "换模型", 70, 78, 64, 25, BtnKind.Ghost,
                        () => CycleBotModel(bot));
                    MakeBtn(row, "启动", 138, 78, 62, 25, BtnKind.Primary,
                        () => StartBot(bot));
                    MakeBtn(row, "停止", 204, 78, 62, 25, BtnKind.Ghost,
                        () => StopBot(bot));
                    MakeBtn(row, "删", 270, 78, 42, 25, BtnKind.Danger,
                        () => RemoveBot(bot));
                }
            }

            MakeBtn(bg, "保存", 18, 566, 100, 30, BtnKind.Primary, Save);
            MakeBtn(bg, "关闭 (F7)", 126, 566, 110, 30, BtnKind.Danger, () => AiSystem.TogglePanel(false));
            var status = new UIText("当前选择：" + _sync.ActiveActorDescription, 0.76f);
            status.Left.Set(252, 0f); status.Top.Set(572, 0f); status.TextColor = Theme.Muted;
            bg.Append(status);
        }

        private List<(int providerIndex, AiProvider provider, AiModel model)> ModelOptions()
        {
            var list = new List<(int providerIndex, AiProvider provider, AiModel model)>();
            for (int i = 0; i < _sync.Providers.Count; i++)
            {
                var p = _sync.Providers[i];
                foreach (var m in p.Models ?? new List<AiModel>())
                    if (!string.IsNullOrWhiteSpace(m.Id)) list.Add((i, p, m));
            }
            return list;
        }

        private void AssignSelf(AiProvider provider, AiModel model)
        {
            _sync.EnsureActors();
            int index = _sync.Actors.FindIndex(a => a != null && a.IsSelf);
            if (index < 0) return;
            var self = _sync.Actors[index];
            self.ProviderKey = provider.Key;
            self.ModelId = model.Id;
            _sync.ActiveIndex = _sync.Providers.IndexOf(provider);
            _sync.ActiveActorIndex = index;
            // 这里必须落盘：对话每次从磁盘重新加载配置，只有刷新界面而不保存
            // 会造成 UI 显示“操控我”，实际请求仍然打到旧模型/旧对象。
            SaveAndRebuild();
        }

        private void AddBot(AiProvider provider, AiModel model)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _modelNotice = "当前不是 Host & Play 客户端，无法加入队友 Bot。请先以多人模式开世界。";
                _modelNoticeColor = Theme.Warn;
                Rebuild();
                return;
            }
            _sync.EnsureActors();
            int n = _sync.Actors.Count(a => a != null && a.IsBot) + 1;
            var bot = new AiActorBinding
            {
                Id = "bot-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Kind = "bot",
                DisplayName = "队友 Bot " + n,
                ProviderKey = provider.Key,
                ModelId = model.Id,
                PlayerName = AiBotRuntime.SuggestPlayerName(),
                GameHost = _sync.Actors.FirstOrDefault(a => a != null && a.IsBot)?.GameHost ?? "127.0.0.1",
                GamePort = _sync.Actors.FirstOrDefault(a => a != null && a.IsBot)?.GamePort ?? 7777,
                BridgePort = 47779 + n - 1,
                Enabled = true,
            };
            _sync.Actors.Add(bot);
            // 默认沿用当前角色并立即启动，保持“点 + Bot 就开始加入”的直觉。
            // 如果角色名需要改，条目仍会保留；改完再点一次“启动”即可重试。
            PersistAndStartBot(bot);
            _modelNotice = "已添加并启动 " + bot.DisplayName + "；请等待状态变为“已进世界”。";
            _modelNoticeColor = Theme.Good;
            Rebuild();
        }

        private void PersistAndStartBot(AiActorBinding bot)
        {
            if (bot == null) return;
            _sync.Save();
            _cfg = AiConfig.Load();
            var saved = _cfg.Actors.FirstOrDefault(a => a != null && a.Id == bot.Id);
            if (saved != null)
            {
                AiBotRuntime.Start(saved);
                _modelNotice = "正在启动 " + saved.DisplayName + "…";
                _modelNoticeColor = Theme.Accent;
            }
        }

        private void StartBot(AiActorBinding bot)
        {
            if (bot == null) return;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _modelNotice = "当前不是 Host & Play 客户端，Bot 无法连接这个世界。";
                _modelNoticeColor = Theme.Warn;
                Rebuild();
                return;
            }
            if (string.IsNullOrWhiteSpace(bot.GameHost)) bot.GameHost = "127.0.0.1";
            if (bot.GamePort < 1 || bot.GamePort > 65535)
            {
                bot.GamePort = 7777;
                _modelNotice = "端口无效，已恢复为 7777。";
                _modelNoticeColor = Theme.Warn;
                Rebuild();
                return;
            }
            if (string.IsNullOrWhiteSpace(bot.PlayerName))
            {
                bot.PlayerName = AiBotRuntime.SuggestPlayerName();
                if (string.IsNullOrWhiteSpace(bot.PlayerName))
                {
                    _modelNotice = "没有可用角色名。请先在角色选择页创建角色，或填写已有角色名。";
                    _modelNoticeColor = Theme.Warn;
                    Rebuild();
                    return;
                }
            }
            PersistAndStartBot(bot);
            Rebuild();
        }

        private void StopBot(AiActorBinding bot)
        {
            AiBotRuntime.Stop(bot);
            _modelNotice = "已停止 " + (bot?.DisplayName ?? "Bot") + "。";
            _modelNoticeColor = Theme.Muted;
            Rebuild();
        }

        private void RemoveBot(AiActorBinding bot)
        {
            if (bot == null) return;
            AiBotRuntime.Stop(bot);
            int oldIndex = _sync.Actors.IndexOf(bot);
            _sync.Actors.Remove(bot);
            if (_sync.ActiveActorIndex > oldIndex) _sync.ActiveActorIndex--;
            else if (_sync.ActiveActorIndex == oldIndex) _sync.ActiveActorIndex = 0;
            _sync.Save();
            _modelNotice = "已删除队友条目。";
            _modelNoticeColor = Theme.Muted;
            Rebuild();
        }

        private void SaveAndRebuild()
        {
            _sync.Save();
            _cfg = AiConfig.Load();
            Rebuild();
        }

        private void CycleBotModel(AiActorBinding bot)
        {
            var options = ModelOptions();
            if (options.Count == 0) return;
            int cur = -1;
            for (int i = 0; i < options.Count; i++)
                if (options[i].provider.Key == bot.ProviderKey && options[i].model.Id == bot.ModelId) { cur = i; break; }
            var next = options[(cur + 1 + options.Count) % options.Count];
            bot.ProviderKey = next.provider.Key;
            bot.ModelId = next.model.Id;
            _sync.Save();
            Rebuild();
        }

        private string BindingModelText(AiActorBinding binding)
        {
            if (binding == null) return "未分配模型";
            var p = _sync.Providers.FirstOrDefault(x => x.Key == binding.ProviderKey);
            var m = p?.Models?.FirstOrDefault(x => x.Id == binding.ModelId);
            return p == null || m == null ? "未分配模型" : p.DisplayName + " · " + DisplayModel(m);
        }

        private static string DisplayModel(AiModel model)
        {
            if (model == null) return "未命名模型";
            return string.IsNullOrWhiteSpace(model.Label) ? model.Id : model.Label;
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
            ky.Width.Set(408, 0f);
            ky.Left.Set(x + 96, 0f); ky.Top.Set(y, 0f);
            ky.OnChanged = () => pv.ApiKey = ky.Text;
            bg.Append(ky); _inputs.Add(ky);
            y += 40f;

            Lab(bg, "显示名称", x, y + 9);
            var nmIn = new AiInput("给这套配置起个名");
            nmIn.MaxLength = 40;
            nmIn.Text = pv.DisplayName ?? "";
            nmIn.Width.Set(408, 0f);
            nmIn.Left.Set(x + 96, 0f); nmIn.Top.Set(y, 0f);
            nmIn.OnChanged = () => pv.DisplayName = nmIn.Text;
            bg.Append(nmIn); _inputs.Add(nmIn);
            y += 40f;

            Lab(bg, "API 地址", x, y + 9);
            var url = new AiInput("https://api.example.com/v1");
            url.MaxLength = 300;
            url.Text = pv.BaseUrl ?? "";
            url.Width.Set(408, 0f);
            url.Left.Set(x + 96, 0f); url.Top.Set(y, 0f);
            url.OnChanged = () => pv.BaseUrl = url.Text;
            bg.Append(url); _inputs.Add(url);
            y += 40f;

            Lab(bg, "API 协议", x, y + 9);
            MakeBtn(bg, pv.Protocol == "anthropic" ? "Anthropic Messages" : "OpenAI Chat Completions",
                x + 96, y, 250, 28, BtnKind.Ghost, () =>
                {
                    pv.Protocol = pv.Protocol == "anthropic" ? "openai" : "anthropic";
                    Rebuild();
                });
            y += 42f;

            Lab(bg, "模型目录", x, y + 6);
            // 「获取可用模型」：真的去请求这个 key 在这个地址下的 /models。
            // 放在左边、恢复默认放右边——先"能选什么"，再"回到原样"。
            MakeLink(bg, "获取可用模型", x + 216, y + 4, FetchModels);
            MakeLink(bg, "恢复默认", x + 344, y + 4, () =>
            {
                var d = DefaultModelsFor(pv.Key);
                if (d.Count > 0) { pv.Models = d; pv.ActiveModel = d[0].Id; }
                Rebuild();
            });
            y += 28f;

            for (int i = 0; i < pv.Models.Count; i++)
            {
                int mi = i;
                var m = pv.Models[i];

                // 斑马纹：偶数行给一条极淡的底条，让相邻两行的模型 id 不至于糊在一起。
                if ((i & 1) == 1)
                {
                    var stripe = new UIPanel();
                    stripe.Width.Set(476, 0f); stripe.Height.Set(32, 0f);
                    stripe.Left.Set(x - 4, 0f); stripe.Top.Set(y - 2, 0f);
                    stripe.BackgroundColor = new Color(255, 255, 255, 0.025f);
                    stripe.BorderColor = Color.Transparent;
                    bg.Append(stripe);
                }

                var idIn = new AiInput("模型 id");
                idIn.MaxLength = 120;
                idIn.Text = m.Id ?? "";
                idIn.Width.Set(176, 0f);
                idIn.Left.Set(x, 0f); idIn.Top.Set(y, 0f);
                idIn.OnChanged = () => m.Id = idIn.Text;
                bg.Append(idIn); _inputs.Add(idIn);

                var lbIn = new AiInput("显示名");
                lbIn.MaxLength = 60;
                lbIn.Text = m.Label ?? "";
                lbIn.Width.Set(176, 0f);
                lbIn.Left.Set(x + 184, 0f); lbIn.Top.Set(y, 0f);
                lbIn.OnChanged = () => m.Label = lbIn.Text;
                bg.Append(lbIn); _inputs.Add(lbIn);

                bool isCur = pv.ActiveModel == m.Id;
                MakeBtn(bg, isCur ? "当前" : "用这个", x + 368, y, 56, 28,
                    isCur ? BtnKind.Success : BtnKind.Ghost,
                    () => { pv.ActiveModel = m.Id; Rebuild(); });

                MakeBtn(bg, "删", x + 430, y, 38, 28, BtnKind.Danger, () =>
                {
                    if (mi < pv.Models.Count) pv.Models.RemoveAt(mi);
                    if (pv.ActiveModel == m.Id)
                        pv.ActiveModel = pv.Models.Count > 0 ? pv.Models[0].Id : "";
                    Rebuild();
                });

                y += 36f;
            }

            MakeBtn(bg, "+ 添加模型", x, y, 118, 28, BtnKind.Success, () =>
            {
                pv.Models.Add(new AiModel("", ""));
                Rebuild();
            });
        }

        /// <summary>
        /// 「获取可用模型」：拿当前正在编辑的地址和密钥，请求 {base}/models。
        ///
        /// 注意请求体用的是 _sync（面板上的副本）而不是已保存的 _cfg ——
        /// 用户很可能刚改完地址就想试试这个地址有哪些模型，还没点保存。
        /// 回调时按 Key 找回那一条提供商再写回，因为期间可能已经 Rebuild 过。
        /// </summary>
        void FetchModels()
        {
            var pv = _sync.Active;
            if (pv == null) { Say("先选一个提供商", Theme.Warn); return; }
            if (string.IsNullOrWhiteSpace(pv.BaseUrl)) { Say("先填 API 地址，再拉模型列表", Theme.Warn); return; }
            string key = pv.Key;
            Say("正在拉取模型列表…", Theme.Muted);

            AiChat.ListModelsAsync(_sync.Clone(), (ok, msg, ids) =>
            {
                if (!ok) { Say("拉取失败：" + msg, Theme.Bad); return; }
                var hit = _sync.Providers.Find(q => q.Key == key);
                if (hit == null) { Say("提供商已不在了，拉到的 " + ids.Count + " 个模型丢弃", Theme.Warn); return; }
                int added = 0;
                foreach (var id in ids)
                {
                    bool has = false;
                    foreach (var m in hit.Models) if (m.Id == id) { has = true; break; }
                    if (!has) { hit.Models.Add(new AiModel(id, id)); added++; }
                }
                if (added > 0 && string.IsNullOrWhiteSpace(hit.ActiveModel))
                    hit.ActiveModel = hit.Models[0].Id;
                Say("拿到 " + ids.Count + " 个模型，新增 " + added + " 个", Theme.Good);
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

        // ── 配色 ────────────────────────────────────────────────────────────
        // 全部提成常量：原来色值散在十几处，改一次要翻全文件，而且相邻两处
        // 常常差 2~3 个色阶，看着"脏"。现在只有这几个来源。
        static class Theme
        {
            public static readonly Color BgOuter = new Color(18, 21, 31);       // 面板底
            public static readonly Color BgCard = new Color(26, 31, 45);        // 卡片/内框
            public static readonly Color BgInset = new Color(13, 16, 24);       // 凹陷（消息区、输入框）
            public static readonly Color Line = new Color(58, 70, 100);         // 常规描边
            public static readonly Color LineSoft = new Color(40, 48, 68);      // 弱描边
            public static readonly Color Title = new Color(232, 238, 250);      // 主标题
            public static readonly Color Text = new Color(198, 208, 228);       // 正文
            public static readonly Color Muted = new Color(140, 150, 176);      // 次要/标签
            public static readonly Color Accent = new Color(96, 150, 232);      // 主色（蓝）
            public static readonly Color AccentDim = new Color(52, 78, 122);
            public static readonly Color Good = new Color(120, 200, 140);       // 绿点/成功
            public static readonly Color Warn = new Color(224, 190, 110);       // 黄
            public static readonly Color Bad = new Color(206, 112, 112);        // 红
        }

        /// <summary>按钮等级。一级主操作、二级常规、三级危险，视觉权重必须能分开。</summary>
        private enum BtnKind { Primary, Ghost, Danger, Success }

        private void MakeBtn(UIElement parent, string text, float x, float y, float w, float h,
                             BtnKind kind, Action onClick)
        {
            Color fill, edge, ink;
            switch (kind)
            {
                case BtnKind.Primary: fill = Theme.AccentDim; edge = Theme.Accent; ink = Color.White; break;
                case BtnKind.Success: fill = new Color(38, 66, 46); edge = new Color(86, 148, 100); ink = Theme.Good; break;
                case BtnKind.Danger:  fill = new Color(64, 34, 36); edge = new Color(132, 66, 66); ink = Theme.Bad; break;
                default:              fill = Theme.BgCard; edge = Theme.LineSoft; ink = Theme.Text; break;
            }

            var b = new UIPanel();
            b.Width.Set(w, 0f); b.Height.Set(h, 0f);
            b.Left.Set(x, 0f); b.Top.Set(y, 0f);
            b.BackgroundColor = fill;
            b.BorderColor = edge;
            parent.Append(b);

            var t = new UIText(text, 0.78f);
            t.HAlign = 0.5f; t.VAlign = 0.5f;
            t.TextColor = ink;
            b.Append(t);

            // 悬停时把底色提亮一档。没有反馈的按钮在深色面板上根本分不清能不能点。
            b.OnMouseOver += (_, __) => { b.BackgroundColor = fill * 1.45f; t.TextColor = Color.White; };
            b.OnMouseOut  += (_, __) => { b.BackgroundColor = fill; t.TextColor = ink; };
            b.OnLeftClick += (_, __) => onClick();
        }

        /// <summary>可点的文字链接（"获取可用模型""恢复默认模型"这类轻操作）。</summary>
        private void MakeLink(UIElement parent, string text, float x, float y, Action onClick)
        {
            var t = new UIText(text, 0.74f);
            t.Left.Set(x, 0f); t.Top.Set(y, 0f);
            t.TextColor = new Color(132, 176, 232);
            t.OnMouseOver += (_, __) => t.TextColor = new Color(190, 216, 255);
            t.OnMouseOut  += (_, __) => t.TextColor = new Color(132, 176, 232);
            t.OnLeftClick += (_, __) => onClick();
            parent.Append(t);
        }

        void Lab(UIElement bg, string s, float x, float y)
        {
            var t = new UIText(s, 0.8f);
            t.Left.Set(x, 0f); t.Top.Set(y, 0f);
            t.TextColor = Theme.Muted;
            bg.Append(t);
        }

        private void MakeTab(UIElement parent, string text, bool active, float x, float y, Action onClick)
        {
            var t = new UIPanel();
            t.Width.Set(96, 0f); t.Height.Set(30, 0f);
            t.Left.Set(x, 0f); t.Top.Set(y, 0f);
            t.BackgroundColor = active ? Theme.AccentDim : Theme.BgCard;
            t.BorderColor = active ? Theme.Accent : Theme.LineSoft;
            parent.Append(t);

            var tx = new UIText(text, 0.8f);
            tx.HAlign = 0.5f; tx.VAlign = 0.5f;
            tx.TextColor = active ? Color.White : Theme.Muted;
            t.Append(tx);

            if (!active)
            {
                t.OnMouseOver += (_, __) => { t.BackgroundColor = Theme.LineSoft; tx.TextColor = Theme.Text; };
                t.OnMouseOut  += (_, __) => { t.BackgroundColor = Theme.BgCard; tx.TextColor = Theme.Muted; };
            }
            t.OnLeftClick += (_, __) => onClick();
        }

        private void MakeButton(UIElement parent, string text, float x, float y, float w, Color bgc, Action onClick)
        {
            // 旧签名保留给仍按色值传参的调用点；新代码请直接用 MakeBtn(BtnKind)。
            var b = new UIPanel();
            b.Width.Set(w, 0f); b.Height.Set(28, 0f);
            b.Left.Set(x, 0f); b.Top.Set(y, 0f);
            b.BackgroundColor = bgc;
            b.BorderColor = Theme.LineSoft;
            parent.Append(b);

            var t = new UIText(text, 0.76f);
            t.HAlign = 0.5f; t.VAlign = 0.5f;
            t.TextColor = Theme.Text;
            b.Append(t);

            b.OnMouseOver += (_, __) => { b.BackgroundColor = bgc * 1.45f; b.BorderColor = Theme.Accent; t.TextColor = Color.White; };
            b.OnMouseOut  += (_, __) => { b.BackgroundColor = bgc; b.BorderColor = Theme.LineSoft; t.TextColor = Theme.Text; };
            b.OnLeftClick += (_, __) => onClick();
        }

        public void BlurAll()
        {
            foreach (var i in _inputs) i.Blur();
        }

        private void Say(string s, Color c)
        {
            if (_status != null)
            {
                _status.SetText(s);
                _status.TextColor = c;
                return;
            }

            // 面板关闭时，对话入口仍然可用；错误和准备提示不能静默消失。
            if (!string.IsNullOrWhiteSpace(s) && !AiSystem.IsOpen)
                Main.NewText("[DSH AI] " + s, c.R, c.G, c.B);
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
            var ap = cfg.ActiveActorProvider ?? cfg.Active;
            if (ap == null) { Say("先添加一个提供商", new Color(255, 170, 150)); return; }
            Say("已保存：" + ap.DisplayName + " · " + cfg.RuntimeModelResolved + " → " + cfg.ActiveActorDescription,
                new Color(150, 255, 170));
        }

        private void TestConn()
        {
            var cfg = Commit();
            if (string.IsNullOrWhiteSpace(cfg.RuntimeBaseUrlResolved)) { Say("先把 API 地址填上", new Color(255, 170, 150)); return; }
            if (string.IsNullOrWhiteSpace(cfg.RuntimeModelResolved))   { Say("先在模型目录里填一个模型 id", new Color(255, 170, 150)); return; }
            if (string.IsNullOrWhiteSpace(cfg.RuntimeApiKeyResolved))  { Say("先把 API 密钥填上", new Color(255, 170, 150)); return; }

            Say("正在连接 " + cfg.RuntimeModelResolved + " …", new Color(180, 220, 255));
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
