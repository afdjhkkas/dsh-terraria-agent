using Terraria.ModLoader;

namespace DshAiMini
{
    /// <summary>
    /// DSH AI Mini：把「配模型 + 对话」搬进游戏本体，不再依赖外部 DSH。
    ///
    /// 现在这一步只做验证：先在游戏里按 F8 发一个 HTTP 请求，确认
    /// ①mod 里能发网络请求 ②发的时候游戏不卡 ③回包能拿到。
    /// 三件事都成立，才值得往上盖面板和提示词。
    /// </summary>
    public class DshAiMini : Mod
    {
        public const string Version = "0.1.0";

        public override void Load()
        {
            Logger.Info($"[ai-mini] DSH AI Mini {Version} loaded");
        }
    }
}
