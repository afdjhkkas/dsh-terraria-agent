using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace DshAiMini
{
    /// <summary>独立 Bot 客户端的生命周期和桥健康检查。</summary>
    internal static class AiBotRuntime
    {
        internal enum BotState { Configured, Starting, BridgeReady, InWorld, Failed, Stopped }
        sealed class Runtime
        {
            public Process Process;
            public AiActorBinding Actor;
            public BotState State = BotState.Configured;
            public string Error = "";
            public long LastProbe;
            public long StartedTick;
            public bool ProbeBusy;
        }

        static readonly Dictionary<string, Runtime> Runtimes = new();
        static readonly object Gate = new();

        public static string StatusText(AiActorBinding actor)
        {
            if (actor == null || !actor.IsBot) return "本机";
            lock (Gate)
            {
                if (!Runtimes.TryGetValue(actor.Id, out var r)) return "未启动";
                return r.State switch
                {
                    BotState.InWorld => "已进世界",
                    BotState.BridgeReady => "桥已连接，等待进世界",
                    BotState.Starting => "启动中",
                    BotState.Failed => "失败：" + Trim(r.Error, 42),
                    BotState.Stopped => "已停止",
                    _ => "已配置",
                };
            }
        }

        public static bool IsReady(AiActorBinding actor)
        {
            if (actor == null || !actor.IsBot) return false;
            lock (Gate) return Runtimes.TryGetValue(actor.Id, out var r) && r.State == BotState.InWorld;
        }

        public static string SuggestPlayerName()
        {
            string active = Main.ActivePlayerFileData?.Name ?? "";
            string other = AvailablePlayerNames().FirstOrDefault(name =>
                !string.Equals(name, active, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(other)) return other;
            return active;
        }

        /// <summary>
        /// 返回本机真正可用于 -plr 的角色文件名。
        /// Main.PlayerList 通常已经有这些条目，但在游戏内打开面板时它可能还没有
        /// 完成刷新；因此同时从 Main.PlayerPath 读取 .plr，并让 Player.GetFileData
        /// 给出角色显示名。只读，不加载或修改任何角色状态。
        /// </summary>
        public static List<string> AvailablePlayerNames()
        {
            var names = new List<string>();
            void Add(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                value = value.Trim();
                if (!names.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase)))
                    names.Add(value);
            }

            try
            {
                if (Main.PlayerList != null)
                    foreach (var p in Main.PlayerList)
                        if (p != null) Add(p.Name);
            }
            catch { }

            try
            {
                string path = Main.PlayerPath;
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    foreach (string file in Directory.EnumerateFiles(path, "*.plr"))
                    {
                        try
                        {
                            var data = Player.GetFileData(file, cloudSave: false);
                            if (data != null) Add(data.Name);
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        static string ResolveTModLoaderDirectory(string requested)
        {
            var candidates = new List<string>();
            void Add(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                try
                {
                    string full = Path.GetFullPath(value.Trim().Trim('"'));
                    if (!candidates.Contains(full, StringComparer.OrdinalIgnoreCase)) candidates.Add(full);
                }
                catch { }
            }

            Add(requested);
            Add(Environment.GetEnvironmentVariable("TMODLOADER_DIR"));
            Add(AppContext.BaseDirectory);
            try { Add(Path.GetDirectoryName(typeof(AiBotRuntime).Assembly.Location)); } catch { }

            // A mod can be loaded from a temporary Mods directory. Walk a few
            // parents so the normal game directory is still found without a
            // machine-specific Steam path.
            foreach (string seed in candidates.ToArray())
            {
                string current = seed;
                for (int i = 0; i < 5 && !string.IsNullOrWhiteSpace(current); i++)
                {
                    Add(current);
                    try { current = Directory.GetParent(current)?.FullName; } catch { current = null; }
                }
            }

            // Last-resort discovery for a normal Windows Steam install. This is
            // deliberately bounded to common library layouts and never scans a
            // user's entire disk.
            try
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    foreach (string relative in new[]
                    {
                        @"Steam\steamapps\common\tModLoader",
                        @"SteamLibrary\steamapps\common\tModLoader",
                        @"Games\Steam\steamapps\common\tModLoader",
                        @"Program Files (x86)\Steam\steamapps\common\tModLoader",
                        @"Program Files\Steam\steamapps\common\tModLoader",
                    }) Add(Path.Combine(drive.RootDirectory.FullName, relative));
                }
            }
            catch { }

            return candidates.FirstOrDefault(c =>
                File.Exists(Path.Combine(c, "tModLoader.dll"))
                && File.Exists(Path.Combine(c, "dotnet", "dotnet.exe"))) ?? "";
        }

        public static void Start(AiActorBinding actor)
        {
            if (actor == null || !actor.IsBot) return;
            lock (Gate)
            {
                if (Runtimes.TryGetValue(actor.Id, out var existing) && existing.Process != null && !existing.Process.HasExited)
                    return;
                var r = new Runtime { State = BotState.Starting, StartedTick = (long)Main.GameUpdateCount };
                r.Actor = actor.Clone();
                Runtimes[actor.Id] = r;
                try
                {
                    string launcher = actor.LauncherPath;
                    string working = actor.WorkingDirectory;
                    if (string.IsNullOrWhiteSpace(launcher))
                    {
                        working = ResolveTModLoaderDirectory(working);
                        if (string.IsNullOrWhiteSpace(working))
                            throw new InvalidOperationException("找不到 tModLoader 安装目录；请在 Bot 配置中填写路径，或设置 TMODLOADER_DIR");
                        launcher = Path.Combine(working, "dotnet", "dotnet.exe");
                    }
                    if (!File.Exists(launcher)) throw new FileNotFoundException("找不到 Bot 启动器", launcher);
                    if (string.IsNullOrWhiteSpace(working)) working = Path.GetDirectoryName(launcher);
                    string dll = Path.Combine(working, "tModLoader.dll");
                    if (!File.Exists(dll)) throw new FileNotFoundException("找不到 tModLoader.dll", dll);
                    string playerName = string.IsNullOrWhiteSpace(actor.PlayerName) ? SuggestPlayerName() : actor.PlayerName.Trim();
                    if (string.IsNullOrWhiteSpace(playerName))
                        throw new InvalidOperationException("没有可用于 Bot 的角色；先创建角色，或在 Bot 行填写已有角色名");
                    bool playerExists = AvailablePlayerNames().Any(name =>
                        string.Equals(name, playerName, StringComparison.OrdinalIgnoreCase));
                    if (!playerExists)
                        throw new InvalidOperationException("本机找不到角色“" + playerName + "”；请填写角色选择页中已有的角色名");
                    r.Actor.PlayerName = playerName;
                    var args = new StringBuilder();
                    args.Append(Quote(dll));
                    // 走 tModLoader 原生客户端启动参数。它会在 ModLoader 成功加载后
                    // 调 Main.AutoJoin，并自动选择 -plr 指定的角色；这比在 mod.Load
                    // 里临时注册回调可靠，也避免“桥已启动但客户端仍停在菜单”。
                    string host = string.IsNullOrWhiteSpace(actor.GameHost) ? "127.0.0.1" : actor.GameHost.Trim();
                    int gamePort = actor.GamePort <= 0 ? 7777 : actor.GamePort;
                    args.Append(" -j ").Append(Quote(host + ":" + gamePort));
                    args.Append(" -plr ").Append(Quote(playerName));
                    if (!string.IsNullOrWhiteSpace(actor.GamePassword)) args.Append(" -pass ").Append(Quote(actor.GamePassword));
                    args.Append(" -dsh-bridge-port ").Append(actor.BridgePort);
                    if (!string.IsNullOrWhiteSpace(actor.ExtraArgs)) args.Append(" ").Append(actor.ExtraArgs);
                    var psi = new ProcessStartInfo(launcher, args.ToString())
                    {
                        WorkingDirectory = working,
                        // 不经过 shell 启动，避免 Explorer/Steam 在进程创建瞬间激活 Bot 窗口。
                        // 保留正常窗口：用户可以随后切到 Bot 客机手动操作；启动代码不调用
                        // SetForegroundWindow/ShowWindow/SendInput，因此不会主动抢当前焦点。
                        // tModLoader is launched through dotnet.exe.  Suppress only the
                        // launcher console; the MonoGame client creates its own normal,
                        // interactive game window below.
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Normal,
                    };
                    // 通过环境变量标记这是由本桥启动的 Bot 客户端。命令行参数会被
                    // tModLoader 自己解析，不能塞入一个它不认识的开关；环境变量不会影响
                    // 游戏参数，也让桥端能选择性隔离后台物理鼠标输入。
                    psi.Environment["DSH_BOT_CLIENT"] = "1";
                    r.Process = Process.Start(psi);
                    if (r.Process == null) throw new InvalidOperationException("无法启动 Bot 进程");
                }
                catch (Exception ex)
                {
                    r.State = BotState.Failed;
                    r.Error = ex.Message;
                }
            }
        }

        public static void Stop(AiActorBinding actor)
        {
            if (actor == null || !actor.IsBot) return;
            lock (Gate)
            {
                if (!Runtimes.TryGetValue(actor.Id, out var r)) return;
                try { if (r.Process != null && !r.Process.HasExited) r.Process.Kill(true); } catch { }
                r.State = BotState.Stopped;
            }
        }

        public static void Tick()
        {
            Runtime[] runtimes;
            lock (Gate) runtimes = new List<Runtime>(Runtimes.Values).ToArray();
            foreach (var r in runtimes)
            {
                var actor = r.Actor;
                if (actor == null || !actor.IsBot || !actor.Enabled) continue;
                long now = (long)Main.GameUpdateCount;
                lock (Gate)
                {
                    if (r.Process != null && r.Process.HasExited && r.State != BotState.InWorld)
                    {
                        r.State = BotState.Failed;
                        if (string.IsNullOrWhiteSpace(r.Error)) r.Error = "Bot 客户端已退出（未进世界）";
                        continue;
                    }
                }
                lock (Gate)
                {
                    if (r.ProbeBusy || now - r.LastProbe < 30) continue;
                    r.ProbeBusy = true;
                    r.LastProbe = now;
                }
                ProbeAsync(actor, r, now);
            }
        }

        static async void ProbeAsync(AiActorBinding actor, Runtime runtime, long probeTick)
        {
            try
            {
                string ping = await RequestAsync(actor, "ping", new Dictionary<string, object>());
                using var pdoc = JsonDocument.Parse(ping);
                var data = pdoc.RootElement.TryGetProperty("data", out var d) ? d : pdoc.RootElement;
                bool inWorld = data.TryGetProperty("inWorld", out var iw) && iw.ValueKind == JsonValueKind.True;
                lock (Gate) { runtime.State = inWorld ? BotState.InWorld : BotState.BridgeReady; runtime.Error = ""; }
            }
            catch (Exception ex)
            {
                lock (Gate)
                {
                    bool alive = runtime.Process != null && !runtime.Process.HasExited;
                    // tModLoader 客户端首次启动要加载资源和模组，可能需要几分钟；
                    // 10 秒会把“仍在加载”误报成失败。
                    bool timedOut = alive && probeTick - runtime.StartedTick >= 18000;
                    runtime.State = alive && !timedOut ? BotState.Starting : BotState.Failed;
                    runtime.Error = timedOut
                        ? "启动 5 分钟仍未连接桥；检查主机、端口和版本"
                        : alive ? ex.Message : "客户端已退出";
                }
            }
            finally { lock (Gate) runtime.ProbeBusy = false; }
        }

        public static Task<string> RequestAsync(AiActorBinding actor, string cmd, object parameters)
        {
            return Task.Run(async () =>
            {
                using var client = new TcpClient();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await client.ConnectAsync("127.0.0.1", actor.BridgePort, timeout.Token);
                using NetworkStream stream = client.GetStream();
                using var reader = new StreamReader(stream, new UTF8Encoding(false));
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                string json = JsonSerializer.Serialize(new { cmd, @params = parameters });
                await writer.WriteLineAsync(json.AsMemory(), timeout.Token);
                string line = await reader.ReadLineAsync(timeout.Token);
                if (string.IsNullOrWhiteSpace(line)) throw new IOException("Bot Bridge 没有返回响应");
                return line;
            });
        }

        public static void StopAll()
        {
            lock (Gate)
            {
                foreach (var r in Runtimes.Values)
                    try { if (r.Process != null && !r.Process.HasExited) r.Process.Kill(true); } catch { }
                Runtimes.Clear();
            }
        }

        static string Quote(string s) => "\"" + (s ?? "").Replace("\"", "\\\"") + "\"";
        static string Trim(string s, int n) => string.IsNullOrEmpty(s) ? "" : s.Length > n ? s.Substring(0, n) + "…" : s;
    }
}
