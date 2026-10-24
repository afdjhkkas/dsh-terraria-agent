using Terraria.ModLoader;

namespace DshTerrariaBridge
{
    /// <summary>
    /// DSH Terraria Bridge：把游戏的读写能力通过 127.0.0.1:7777 暴露给 dsh-terraria-agent。
    /// 只服务本机回环地址，不监听外网。
    /// </summary>
    public class DshTerrariaBridge : Mod
    {
        public const string BridgeVersion = "0.2.0";

        public override void Load()
        {
            Logger.Info($"[dsh] DSH Terraria Bridge {BridgeVersion} loaded");
        }
    }
}