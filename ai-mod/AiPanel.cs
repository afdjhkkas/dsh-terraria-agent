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
                else if (k == Microsoft.Xna.Framework.Input.Keys.Enter || k == Microsoft.Xna.Framework.Input.Keys.Escape)
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

    /// <summary>面板本体。左列是配置项，右列是说明与操作。</summary>
    internal sealed class AiPanel : UIState
    {
        public static AiPanel Current;
        private readonly List<AiInput> _inputs = new();

        private AiInput _url, _model, _key;
        private UIText _status;

        public override void OnActivate()
        {
            base.OnActivate();
            Current = this;
            _inputs.Clear();
            RemoveAllChildren();

            var bg = new UIPanel();
            bg.Width.Set(720, 0f);
            bg.Height.Set(430, 0f);
            bg.HAlign = 0.5f;
            bg.VAlign = 0.5f;
            bg.BackgroundColor = new Color(24, 28, 40) * 0.96f;
            bg.BorderColor = new Color(90, 110, 160);
            Append(bg);

            var title = new UIText("DSH AI Mini — 模型配置", 1.05f, true);
            title.Left.Set(18, 0f); title.Top.Set(12, 0f);
            bg.Append(title);

            var cfg = AiConfig.Load();

            float y = 56f;
            y = Row(bg, "接口地址", cfg.BaseUrl, "https://…/v1/chat/completions", y, out _url);
            y = Row(bg, "模型名", cfg.Model, "deepseek-chat / gpt-4o-mini …", y, out _model);
            y = Row(bg, "API 密钥", cfg.ApiKey, "粘贴 key（Ctrl+V 可用）", y, out _key);
            _key.Password = true;

            _status = new UIText("", 0.82f);
            _status.Left.Set(18, 0f); _status.Top.Set(y + 6, 0f);
            _status.TextColor = new Color(160, 200, 255);
            bg.Append(_status);

            var save = new UITextPanel<string>("保存", 0.9f) { BackgroundColor = new Color(50, 90, 60) };
            save.Width.Set(110, 0f); save.Height.Set(34, 0f);
            save.Left.Set(18, 0f); save.Top.Set(y + 40, 0f);
            save.OnLeftClick += (_, __) => Save();
            bg.Append(save);

            var test = new UITextPanel<string>("测试连接", 0.9f) { BackgroundColor = new Color(50, 70, 110) };
            test.Width.Set(130, 0f); test.Height.Set(34, 0f);
            test.Left.Set(140, 0f); test.Top.Set(y + 40, 0f);
            test.OnLeftClick += (_, __) => TestConn();
            bg.Append(test);

            var close = new UITextPanel<string>("关闭 (F7)", 0.9f) { BackgroundColor = new Color(90, 50, 50) };
            close.Width.Set(130, 0f); close.Height.Set(34, 0f);
            close.Left.Set(282, 0f); close.Top.Set(y + 40, 0f);
            close.OnLeftClick += (_, __) => AiSystem.TogglePanel(false);
            bg.Append(close);

            var note = new UIText(
                "配置存在 ModConfigs/DshAiMini.json，可直接改文件。\n" +
                "密钥只在本机请求头里用，不写进日志。\n" +
                "输入框点一下才能打字；Esc 或回车结束输入。", 0.78f);
            note.Left.Set(18, 0f); note.Top.Set(y + 88, 0f);
            note.TextColor = new Color(150, 155, 170);
            bg.Append(note);

            Say("填好地址和密钥后点「测试连接」", new Color(150, 200, 255));
        }

        private float Row(UIElement parent, string label, string value, string hint, float y, out AiInput input)
        {
            var t = new UIText(label, 0.85f);
            t.Left.Set(18, 0f); t.Top.Set(y + 7, 0f);
            parent.Append(t);

            input = new AiInput(hint);
            input.Text = value ?? "";
            input.Width.Set(520, 0f);
            input.Left.Set(120, 0f);
            input.Top.Set(y, 0f);
            parent.Append(input);
            _inputs.Add(input);
            return y + 42f;
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
            var cfg = AiConfig.Load();
            cfg.BaseUrl = _url.Text.Trim();
            cfg.Model = _model.Text.Trim();
            cfg.ApiKey = _key.Text.Trim();
            return cfg;
        }

        private void Save()
        {
            var cfg = Collect();
            cfg.Save();
            Say("已保存到 " + AiConfig.FilePath, new Color(150, 255, 170));
        }

        private void TestConn()
        {
            var cfg = Collect();
            cfg.Save();
            if (string.IsNullOrWhiteSpace(cfg.BaseUrl)) { Say("先把接口地址填上", new Color(255, 170, 150)); return; }
            if (string.IsNullOrWhiteSpace(cfg.ApiKey)) { Say("先把 API 密钥填上", new Color(255, 170, 150)); return; }

            Say("正在连接…", new Color(180, 220, 255));
            AiChat.TestAsync(cfg, (ok, msg) =>
            {
                Say(ok ? "连接正常：" + msg : "失败：" + msg, ok ? new Color(150, 255, 170) : new Color(255, 150, 150));
            });
        }
    }
}
