using System;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace DshAiMini
{
    /// <summary>
    /// 把 AiToolExec 的控制意图真的送进引擎。
    ///
    /// 时机是关键：原版 Player.Update 的顺序大致是
    ///   ResetControls() → 读键盘 → 结算输入 → 算速度/位移 → PostUpdateRunSpeeds()
    /// 所以写在 PostUpdateRunSpeeds 里的 controlX，是引擎这一帧算位移之前最后看到的版本。
    /// 写在更早的地方会被 ResetControls 清掉；写在 PostUpdate 里已经晚了，要等下一帧。
    ///
    /// 快捷召唤（上/下坐骑）走的是输入层的边沿：原版在 Player.Update 里读
    /// PlayerInput.Triggers.Current.QuickMount，失焦时那一整段不执行，
    /// 所以这里在需要时临时把 Main.hasFocus 抬起来，按完再放开。
    /// </summary>
    internal class AiPlayer : ModPlayer
    {
        /// <summary>AiToolExec 设它，本类负责把这个意图变成一次真实按键边沿。</summary>
        internal static bool RequestQuickMount;

        // 边沿状态：按下 → 保持 2 帧 → 松开
        static int _quickHeld;

        public override void PostUpdateRunSpeeds()
        {
            // ── 方向 / 跳 / 上 / 下 / 使用 ──
            if (AiToolExec.WantLeft) Player.controlLeft = true;
            if (AiToolExec.WantRight) Player.controlRight = true;
            if (AiToolExec.WantJump) Player.controlJump = true;
            if (AiToolExec.WantUp) Player.controlUp = true;
            if (AiToolExec.WantDown) Player.controlDown = true;
            if (AiToolExec.WantUseItem)
            {
                Player.controlUseItem = true;
                Main.mouseLeft = true;
            }

            // ── 快捷召唤：必须走输入层，且有按下/松开两态 ──
            if (RequestQuickMount && _quickHeld == 0)
            {
                _quickHeld = 1;
                if (!Main.hasFocus) Main.hasFocus = true;   // 失焦时输入层整段不执行
            }

            if (_quickHeld > 0)
            {
                PlayerInput.Triggers.Current.QuickMount = true;
                _quickHeld++;
                if (_quickHeld > 3)
                {
                    PlayerInput.Triggers.Current.QuickMount = false;
                    _quickHeld = 0;
                    RequestQuickMount = false;
                }
            }
        }

        public override void PostUpdate()
        {
            // 抬起来的焦点只在按键那几帧有效，别留着影响玩家自己操作
            if (_quickHeld == 0 && !RequestQuickMount && Main.hasFocus && !Main.instance.IsActive)
                Main.hasFocus = false;
        }
    }
}
