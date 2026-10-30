using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
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
            public long LastWindowSweep;
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

        public static void Start(AiActorBinding actor)
        {
            if (actor == null || !actor.IsBot) return;
            lock (Gate)
            {
                if (Runtimes.TryGetValue(actor.Id, out var existing) && existing.Process != null && !existing.Process.HasExited)
                    return;
                var r = new Runtime { State = BotState.Starting };
                r.Actor = actor.Clone();
                Runtimes[actor.Id] = r;
                try
                {
                    string launcher = actor.LauncherPath;
                    string working = actor.WorkingDirectory;
                    if (string.IsNullOrWhiteSpace(launcher))
                    {
                        working = string.IsNullOrWhiteSpace(working) ? @"D:\STEAM\steamapps\common\tModLoader" : working;
                        launcher = Path.Combine(working, "dotnet", "dotnet.exe");
                    }
                    if (!File.Exists(launcher)) throw new FileNotFoundException("找不到 Bot 启动器", launcher);
                    if (string.IsNullOrWhiteSpace(working)) working = Path.GetDirectoryName(launcher);
                    string dll = Path.Combine(working, "tModLoader.dll");
                    var args = new StringBuilder();
                    args.Append(Quote(dll)).Append(" -j ").Append(Quote(string.IsNullOrWhiteSpace(actor.GameHost) ? "127.0.0.1" : actor.GameHost));
                    args.Append(" -p ").Append(actor.GamePort <= 0 ? 7777 : actor.GamePort);
                    if (!string.IsNullOrWhiteSpace(actor.GamePassword)) args.Append(" -password ").Append(Quote(actor.GamePassword));
                    args.Append(" -dsh-bridge-port ").Append(actor.BridgePort);
                    if (!string.IsNullOrWhiteSpace(actor.ExtraArgs)) args.Append(" ").Append(actor.ExtraArgs);
                    var psi = new ProcessStartInfo(launcher, args.ToString())
                    {
                        WorkingDirectory = working,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                    };
                    r.Process = Process.Start(psi);
                    if (r.Process == null) throw new InvalidOperationException("无法启动 Bot 进程");
                    HideProcessWindows(r.Process.Id);
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
                if (now - r.LastWindowSweep >= 15)
                {
                    r.LastWindowSweep = now;
                    try
                    {
                        if (r.Process != null && !r.Process.HasExited)
                            HideProcessWindows(r.Process.Id);
                    }
                    catch { }
                }
                lock (Gate)
                {
                    if (r.ProbeBusy || now - r.LastProbe < 30) continue;
                    r.ProbeBusy = true;
                    r.LastProbe = now;
                }
                ProbeAsync(actor, r);
            }
        }

        static async void ProbeAsync(AiActorBinding actor, Runtime runtime)
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
                    runtime.State = runtime.Process != null && !runtime.Process.HasExited ? BotState.Starting : BotState.Failed;
                    runtime.Error = ex.Message;
                }
            }
            finally { lock (Gate) runtime.ProbeBusy = false; }
        }

        public static Task<string> RequestAsync(AiActorBinding actor, string cmd, object parameters)
        {
            return Task.Run(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", actor.BridgePort);
                using NetworkStream stream = client.GetStream();
                using var reader = new StreamReader(stream, new UTF8Encoding(false));
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                string json = JsonSerializer.Serialize(new { cmd, @params = parameters });
                await writer.WriteLineAsync(json);
                string line = await reader.ReadLineAsync();
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

        delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hwnd, int command);

        static void HideProcessWindows(int processId)
        {
            EnumWindows((hwnd, _) =>
            {
                GetWindowThreadProcessId(hwnd, out uint owner);
                if (owner == (uint)processId) ShowWindow(hwnd, 0);
                return true;
            }, IntPtr.Zero);
        }

        static string Quote(string s) => "\"" + (s ?? "").Replace("\"", "\\\"") + "\"";
        static string Trim(string s, int n) => string.IsNullOrEmpty(s) ? "" : s.Length > n ? s.Substring(0, n) + "…" : s;
    }
}
