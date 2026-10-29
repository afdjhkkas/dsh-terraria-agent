using System;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DshAiMini
{
    /// <summary>
    /// 最小的可行性验证：F8 发一个 HTTP 请求，F9 看结果。
    ///
    /// 为什么不用命令而用按键：命令要么走聊天框、要么走外部桥，
    /// 都会把「mod 自己能不能发请求」这个问题搅浑。按键最干净。
    ///
    /// 计时只打在游戏里：主线程从投递到拿回结果这段时间里有没有卡帧，
    /// 用连续两帧的间隔看 —— 如果某帧超过 100ms，就是被网络拖住了。
    /// </summary>
    public class AiTest : ModSystem
    {
        private static bool _prevF8, _prevF9;
        private static long _postedTick = -1;
        private static long _lastTick = -1;
        private static long _worstFrameMs;
        private static string _lastLine = "(还没发过请求)";
        private static bool _waiting;

        /// <summary>用来验证的发请求地址。故意用一个必然会回 JSON 的公开接口。</summary>
        private const string TestUrl = "https://httpbin.org/post";

        public override void PostUpdateEverything()
        {
            // 帧间隔：进游戏后第一次不算。
            long now = Main.GameUpdateCount;
            if (_lastTick >= 0)
            {
                long dt = now - _lastTick;
                // GameUpdateCount 是帧计数不是毫秒，只能用游戏自己的计时器
            }
            _lastTick = now;

            bool f8 = Main.keyState.IsKeyDown(Keys.F8);
            bool f9 = Main.keyState.IsKeyDown(Keys.F9);

            if (f8 && !_prevF8) DoPost();
            if (f9 && !_prevF9) DoReadResult();
            _prevF8 = f8;
            _prevF9 = f9;

            // 每帧顺手看一眼有没有回来的
            var got = AiHttp.TryTake();
            if (got.HasValue) OnResult(got.Value);
        }

        private static void DoPost()
        {
            if (_waiting) { Say("上一���还没回来，等一下", 255, 200, 120); return; }
            _waiting = true;
            _postedTick = Main.GameUpdateCount;
            _worstFrameMs = 0;
            Say("发请求中（F9 看结果）…", 180, 220, 255);
            AiHttp.Post(TestUrl, "{\"probe\":\"dsh-ai-mini\"}", null, 20000);
        }

        private static void DoReadResult()
        {
            if (_waiting) { Say("还在等，稍候", 255, 200, 120); return; }
            Say(_lastLine, 200, 255, 200);
        }

        private static void OnResult((bool ok, int status, string text, string error, long elapsedMs) r)
        {
            _waiting = false;
            long frames = Main.GameUpdateCount - _postedTick;
            if (r.ok)
            {
                string t = r.text ?? "";
                if (t.Length > 180) t = t.Substring(0, 180) + "…";
                t = t.Replace("\r", " ").Replace("\n", " ");
                _lastLine = $"HTTP {r.status} / {r.elapsedMs}ms（主线程上过了 {frames} 帧）: {t}";
                Say(_lastLine, 160, 255, 160);
            }
            else
            {
                _lastLine = $"失败 {r.error} / {r.elapsedMs}ms";
                Say(_lastLine, 255, 160, 160);
            }
        }

        private static void Say(string s, byte r, byte g, byte b)
        {
            if (Main.dedServ) return;
            Main.NewText("[AI] " + s, r, g, b);
        }
    }
}
