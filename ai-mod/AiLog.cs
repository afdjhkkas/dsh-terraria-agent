using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace DshAiMini
{
    /// <summary>
    /// 对话消息流。参考 DSH 会话页的做法：一行一条，左边一个小标记说明这条是谁说的，
    /// 用户/思考/工具/回答四种用不同颜色区分，最新的在最下面。
    ///
    /// 这里刻意不做真滚动条：Terraria 的 UIScrollablePanel 和自绘文本混用很容易错位，
    /// 而玩家真正要看的是"最近几轮"，所以固定显示尾部 N 条、靠自动贴底达到同样效果。
    /// </summary>
    internal sealed class AiLog : UIElement
    {
        internal enum Kind { User, Think, Tool, Answer, Error }

        internal sealed class Line
        {
            public Kind Kind;
            public string Text;
            public List<string> Wrapped;   // 按像素宽度折好的行，绘制时直接用
        }

        private readonly List<Line> _lines = new();

        /// <summary>当前显示区域能放几条（Rebuild 时按高度算）。</summary>
        public int Visible = 12;

        public float LineHeight = 20f;

        public void Add(Kind kind, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _lines.Add(new Line { Kind = kind, Text = text });
            if (_lines.Count > 200) _lines.RemoveRange(0, _lines.Count - 200);
            Wrap();
        }

        public void Clear()
        {
            _lines.Clear();
        }

        public int Count => _lines.Count;

        /// <summary>宽度变化后要重折（Rebuild、窗口缩放都要调）。</summary>
        public void Wrap()
        {
            float w = GetInnerDimensions().Width - 16f;
            if (w < 60f) w = 60f;

            foreach (var l in _lines)
            {
                l.Wrapped = WrapText(l.Text, w);
            }
        }

        /// <summary>
        /// 按像素宽度折行。中文没有空格，只能按字符一个个试，
        /// 遇到 \n 强制换行；遇到英文单词尽量不切断（在空格处断）。
        /// </summary>
        private static List<string> WrapText(string text, float maxWidth)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) { result.Add(""); return result; }

            var font = FontAssets.MouseText.Value;
            foreach (var raw in text.Split('\n'))
            {
                if (raw.Length == 0) { result.Add(""); continue; }

                int start = 0;
                int lastSpace = -1;
                for (int i = 0; i < raw.Length; i++)
                {
                    string piece = raw.Substring(start, i - start + 1);
                    if (font.MeasureString(piece).X > maxWidth && i > start)
                    {
                        int cut = lastSpace > start ? lastSpace : i;
                        result.Add(raw.Substring(start, cut - start));
                        start = cut < i ? cut + 1 : i;
                        lastSpace = -1;
                    }
                    if (raw[i] == ' ') lastSpace = i;
                }
                if (start < raw.Length) result.Add(raw.Substring(start));
            }
            if (result.Count == 0) result.Add("");
            return result;
        }

        public override void Draw(SpriteBatch sb)
        {
            base.Draw(sb);

            var dim = GetInnerDimensions();
            var font = FontAssets.MouseText.Value;
            float y = dim.Y + 4f;

            // 取尾部能放下的部分（自动贴底）
            var flat = new List<(Line Line, string Row)>();
            for (int i = _lines.Count - 1; i >= 0 && flat.Count < Visible; i--)
            {
                var l = _lines[i];
                if (l.Wrapped == null) continue;
                for (int j = l.Wrapped.Count - 1; j >= 0 && flat.Count < Visible; j--)
                {
                    flat.Add((l, l.Wrapped[j]));
                    if (j == 0 && l.Wrapped.Count > 1) { /* 段落之间留空由下一轮处理 */ }
                }
            }
            flat.Reverse();

            foreach (var (line, row) in flat)
            {
                Color col = ColorOf(line.Kind);
                string mark = MarkOf(line.Kind);
                if (mark != null)
                    Terraria.Utils.DrawBorderString(sb, mark, new Vector2(dim.X + 4f, y), col * 0.75f, 0.82f);

                Terraria.Utils.DrawBorderString(sb, row, new Vector2(dim.X + 22f, y), col, 0.86f);
                y += LineHeight;
                if (y > dim.Y + dim.Height - LineHeight) break;
            }
        }

        private static Color ColorOf(Kind k) => k switch
        {
            Kind.User => new Color(180, 220, 255),
            Kind.Think => new Color(140, 140, 150),
            Kind.Tool => new Color(120, 200, 255),
            Kind.Answer => new Color(235, 235, 235),
            _ => new Color(255, 140, 140),
        };

        private static string MarkOf(Kind k) => k switch
        {
            Kind.User => "你",
            Kind.Think => "·",
            Kind.Tool => ">",
            Kind.Answer => "AI",
            _ => "!",
        };
    }
}
