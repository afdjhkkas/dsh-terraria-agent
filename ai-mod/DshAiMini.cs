using Terraria.ModLoader;
using System;
using System.Collections.Generic;
using Terraria;

namespace DshAiMini
{
    /// <summary>
    /// DSH AI Mini：把「配模型 + 对话」搬进游戏本体，不再依赖外部 DSH。
    ///
    /// 面板、对话、真实 Bot 客户端入服都从这里加载；网络回调仍由 AiSystem
    /// 在游戏主线程排空，避免 UI 和世界状态被后台线程直接改写。
    /// </summary>
    public class DshAiMini : Mod
    {
        public const string Version = "0.1.0";
        private bool _autoJoinScheduled;

        public override void Load()
        {
            Logger.Info($"[ai-mini] DSH AI Mini {Version} loaded");
            TryAutoJoinFromLaunchArgs();
        }

        public override void PostSetupContent()
        {
            TryAutoJoinFromLaunchArgs();
        }

        private void TryAutoJoinFromLaunchArgs()
        {
            // BotRuntime 现在使用 tModLoader 原生的 -j/-plr/-pass 参数。
            // 如果启动参数已经包含原生入服，就不要再注册第二个 AutoJoin 回调。
            string[] rawArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < rawArgs.Length; i++)
                if (string.Equals(rawArgs[i], "-j", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(rawArgs[i], "-join", StringComparison.OrdinalIgnoreCase))
                    return;

            var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] raw = rawArgs;
            for (int i = 0; i < raw.Length; i++)
            {
                string key = raw[i];
                if (string.IsNullOrWhiteSpace(key) || !key.StartsWith("-dsh-autojoin-", StringComparison.OrdinalIgnoreCase)) continue;
                string value = (i + 1 < raw.Length && !raw[i + 1].StartsWith("-", StringComparison.Ordinal)) ? raw[++i] : "";
                args[key] = value;
            }

            if (_autoJoinScheduled || !args.TryGetValue("-dsh-autojoin-host", out string host) || string.IsNullOrWhiteSpace(host)) return;
            if (!args.TryGetValue("-dsh-autojoin-port", out string port) || !int.TryParse(port, out int portNumber) || portNumber < 1 || portNumber > 65535)
            {
                Logger.Warn("[ai-mini] Bot 自动入服参数无效：端口=" + port);
                return;
            }

            args.TryGetValue("-dsh-autojoin-player", out string player);
            args.TryGetValue("-dsh-autojoin-password", out string password);
            try
            {
                if (Main.instance == null) throw new InvalidOperationException("Main.instance 尚未建立");
                if (password != null) Netplay.ServerPassword = password;
                Main.instance.AutoJoin(host.Trim() + ":" + portNumber, string.IsNullOrWhiteSpace(player) ? null : player.Trim());
                _autoJoinScheduled = true;
                Logger.Info("[ai-mini] 已安排真实客户端入服：" + host.Trim() + ":" + portNumber);
            }
            catch (Exception ex)
            {
                Logger.Error("[ai-mini] 安排 Bot 入服失败：" + ex.Message);
            }
        }
    }
}
