using System.Text;
using Terraria;

namespace DshAiMini
{
    /// <summary>
    /// 把"现在的游戏状态"压成一行字，塞进 system 提示词。
    ///
    /// 为什么直接读 Main.player 而不是走桥：这个 mod 和游戏在同一个进程里，
    /// 读字段是零成本的本地操作；走桥反而要开 socket、要序列化、还要依赖另一个 mod。
    /// 单 mod 版的意义就在这里——不依赖任何外部进程。
    ///
    /// 只给会改变决策的字段。背包全清单不给：太长，而且九成和当前问题无关，
    /// 会让模型把注意力放在无关物品上。玩家点名问背包时再由调用方补。
    /// </summary>
    internal static class AiGameState
    {
        public static string Describe() => Describe(null);

        public static string Describe(AiConfig cfg)
        {
            if (Main.dedServ || Main.gameMenu) return "";

            // Bot 是另一个 tModLoader 客户端；这里不能把宿主客户端的坐标
            // 冒充成 Bot 的坐标。Bot 的真实状态必须由它自己的 Bridge status 回读。
            if (cfg?.ActiveActor != null && cfg.ActiveActor.IsBot)
            {
                string who = string.IsNullOrWhiteSpace(cfg.ActiveActor.PlayerName)
                    ? cfg.ActiveActor.DisplayName : cfg.ActiveActor.PlayerName;
                return $"当前执行对象是队友 Bot「{who}」（{AiBotRuntime.StatusText(cfg.ActiveActor)}）；"
                    + "这里不提供本机玩家的位置，必须先调用 status 读取 Bot 自己的状态。";
            }

            Player p = Main.player[Main.myPlayer];
            if (p == null || !p.active) return "";

            var sb = new StringBuilder();

            // 位置：脚底格比 pos.y 好懂，pos.y 是头顶
            int footY = (int)(p.position.Y / 16f) + 3;
            int tileX = (int)(p.Center.X / 16f);
            sb.Append($"位置 ({tileX},{footY})；");

            sb.Append($"生命 {p.statLife}/{p.statLifeMax2}；魔力 {p.statMana}/{p.statManaMax2}；");

            var held = p.inventory[p.selectedItem];
            if (held != null && !held.IsAir)
                sb.Append($"手持 {held.Name} x{held.stack}；");
            else
                sb.Append("手上空着；");

            if (p.dead) sb.Append("**已死亡**；");
            if (p.mount.Active) sb.Append($"骑乘中（{p.mount.Type}）；");
            if (p.wet) sb.Append("在水里；");
            if (p.lavaWet) sb.Append("**在岩浆里**；");

            // 深度分区：决定"这附近能挖到什么"
            float depth = p.Center.Y / (float)Main.maxTilesY;
            sb.Append(depth < 0.28f ? "地表；" : depth < 0.45f ? "地下；" : depth < 0.72f ? "洞穴；" : "地狱附近；");

            // 附近威胁：只数活的、敌对的、不太远的，不列名字（省字数）
            int hostiles = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || n.friendly || n.townNPC) continue;
                if (n.Distance(p.Center) > 40f * 16f) continue;
                hostiles++;
            }
            sb.Append(hostiles > 0 ? $"附近 {hostiles} 个敌人。" : "附近没有敌人。");

            return sb.ToString();
        }
    }
}
