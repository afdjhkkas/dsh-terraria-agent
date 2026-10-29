using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace DshAiMini
{
    /// <summary>
    /// 面板的开关与挂载。
    ///
    /// 快捷键用边沿检测而不是电平：按住不放只算一次，否则一帧开关几十次。
    /// 面板本身挂在 "Vanilla: Mouse Text" 之前，这样它盖在游戏 UI 之上、
    /// 又不会挡住鼠标指针。
    /// </summary>
    public class AiSystem : ModSystem
    {
        public static readonly Keys PanelKey = Keys.F7;

        private static bool _prevPanelKey;
        private static UserInterface _ui;
        private static AiPanel _panel;
        public static bool IsOpen { get; private set; }

        // ── 游戏聊天框监听 ─────────────────────────────────────────────────
        // 面板关着的时候，玩家在聊天框里说的那句话也得有人接。
        // 判据用"回车上升沿 + 上一帧的 chatText"：
        //   · 不能用"chatText 变空" —— 输入法 mod（InputMethodFixer）在拼音组合期
        //     会反复清空/重填 chatText，用它会把 n / ni / nih 每个中间态都当一句话
        //     （桥那边就吃过这个亏，一条消息记成 725 条）。
        //   · 不能用当帧 chatText —— 原版发完就把它清空了，读到的是空串。
        private static bool _prevEnter;
        private static string _prevChat = "";
        private static string _lastSent = "";
        private static long _lastSentTick;

        public override void Load()
        {
            if (Main.dedServ) return;
            _panel = new AiPanel();
            _ui = new UserInterface();
            _ui.SetState(null);
        }

        public override void Unload()
        {
            _panel = null;
            _ui = null;
        }

        public static void TogglePanel(bool? force = null)
        {
            bool want = force ?? !IsOpen;
            if (want == IsOpen) return;
            IsOpen = want;
            if (!want)
            {
                // 关面板必须把输入法桥一起收掉：否则原版聊天框还挂着，
                // 玩家一按回车会把没发出去的问题打到公屏。
                try { AiPanel.Current?.BlurAll(); } catch { }
            }
            _ui?.SetState(want ? _panel : null);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.dedServ) return;

            bool down = Main.keyState.IsKeyDown(PanelKey);
            if (down && !_prevPanelKey) TogglePanel();
            _prevPanelKey = down;

            if (IsOpen) _ui?.Update(gameTime);
        }

        /// <summary>
        /// 工具状态机要每帧推进。放在 PostUpdateEverything：
        /// 此时本帧的玩家更新已经跑完，读写 world/player 都是本帧的最终值。
        /// </summary>
        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gameMenu) return;
            AiToolExec.Tick();
            PollGameChat();
        }

        /// <summary>面板关着时，把游戏聊天框里说的话当成问题送给 AI。</summary>
        static void PollGameChat()
        {
            bool enterEdge = Main.inputTextEnter && !_prevEnter;
            string prev = _prevChat;
            string cur = Main.chatText ?? "";

            // 面板开着的时候聊天框被输入法桥占用（AiInput.ImeBridge），这段必须让开，
            // 否则玩家在面板输入框里打的字会被当成"对 AI 的提问"再发一次。
            if (!IsOpen && enterEdge)
            {
                string q = (prev ?? "").Trim();
                long now = (long)Main.GameUpdateCount;
                bool dup = q.Length > 0 && q == _lastSent && now - _lastSentTick <= 4;
                if (q.Length > 0 && !dup)
                {
                    _lastSent = q;
                    _lastSentTick = now;
                    try { AiPanel.Current?.AskFromGameChat(q); }
                    catch (System.Exception ex) { ModContent.GetInstance<DshAiMini>()?.Logger?.Warn("[ai-mini] 聊天框提问失败: " + ex.Message); }
                }
            }

            _prevEnter = Main.inputTextEnter;
            _prevChat = cur;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Main.dedServ || _ui == null) return;

            int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (idx < 0) idx = layers.Count;

            layers.Insert(idx, new LegacyGameInterfaceLayer(
                "DshAiMini: Panel",
                () =>
                {
                    if (IsOpen) _ui.Draw(Main.spriteBatch, Main.gameTimeCache);
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
