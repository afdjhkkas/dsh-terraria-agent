using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.ModLoader;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;

namespace DshTerrariaBridge
{
    /// <summary>
    /// 本地桥：127.0.0.1:47777，一行一个 JSON 请求 / 响应。
    /// 与 dsh-terraria-agent 的 win.ps1 共用同一套协议，宿主侧只换传输层。
    ///
    /// 线程模型：
    ///   - 接受连接 / 读请求：后台线程
    ///   - 真正读写游戏状态：主线程（PostUpdateEverything 里排空队列）
    /// 这样上层永远不用关心 Terraria 的单线程约束。
    /// </summary>
    public class BridgeSystem : ModSystem
    {
        /// <summary>客户端/单人的桥端口。</summary>
        public const int ClientPort = 47777;
        /// <summary>专用服务端的桥端口（世界归属于它，存盘只能在它这边做）。</summary>
        public const int ServerPort = 47778;

        /// <summary>
        /// 本进程是专用服务端吗？Host&Play 会开两个 tModLoader 进程：一个 -server 持世界，
        /// 一个无参的客户端。两边都会加载本 mod，所以端口必须分开，否则只有一个能绑上，
        /// 而"谁能存盘"恰好取决于连的是哪个进程。
        /// </summary>
        private static bool IsDedicatedServer()
        {
            if (Main.dedServ) return true;
            try
            {
                foreach (string a in Environment.GetCommandLineArgs())
                    if (a == "-server") return true;
            }
            catch { }
            return false;
        }

        // The Bot client is a second tModLoader process.  Keep the marker out of the
        // tModLoader command line (unknown switches are version-dependent) and use an
        // environment variable instead.  This lets us isolate physical mouse input while
        // still allowing bridge-generated input to run in a background window.
        internal static bool IsBotClient => string.Equals(
            Environment.GetEnvironmentVariable("DSH_BOT_CLIENT"), "1", StringComparison.Ordinal);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private static bool IsThisProcessForeground()
        {
            if (!OperatingSystem.IsWindows()) return true;
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return false;
                GetWindowThreadProcessId(hwnd, out uint pid);
                return pid == (uint)Environment.ProcessId;
            }
            catch { return true; }
        }

        private static int CommandLinePort()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i + 1 < args.Length; i++)
                    if (string.Equals(args[i], "-dsh-bridge-port", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(args[i + 1], out int port) && port >= 1024 && port <= 65535)
                        return port;
            }
            catch { }
            return 0;
        }

        public static int Port { get; private set; } = ClientPort;

        /// <summary>Logger reachable from the static connection threads.</summary>
        private static Mod modRef;
        private static TcpListener listener;
        private static Thread acceptThread;
        private static volatile bool running;

        private sealed class Pending
        {
            public string Json;
            public string Req;
            public string Response;
            public int Cancelled;
            public int Started;
            public readonly ManualResetEventSlim Done = new(false);
        }

        /// <summary>Pull the caller's optional correlation id out of a request line.</summary>
        private static string ReadReq(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("req", out var e)) return null;
                return e.ValueKind switch
                {
                    JsonValueKind.Number => e.GetRawText(),
                    JsonValueKind.String => e.GetString(),
                    _ => null,
                };
            }
            catch { return null; }
        }

        private static readonly Queue<Pending> pending = new();

        public override void Load()
        {
            modRef = Mod;
            keepFocusThread = new Thread(KeepFocusLoop) { IsBackground = true, Name = "dsh-keepfocus" };
            keepFocusThread.Start();
            // Note: single player freezes when the window loses focus (measured: paused =
            // true, nothing ticks), so co-op play uses Host & Play, which never pauses.
            Port = CommandLinePort();
            if (Port == 0) Port = IsDedicatedServer() ? ServerPort : ClientPort;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, Port);
                listener.Start();
                running = true;
                acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "dsh-bridge" };
                acceptThread.Start();
                modRef?.Logger.Info($"[dsh] bridge listening on 127.0.0.1:{Port}");
            }
            catch (Exception e)
            {
                running = false;
                // A silent failure here looks exactly like "the tools are broken", so say
                // what happened and why: this is almost always another program on the port.
                modRef?.Logger.Error($"[dsh] bridge could not listen on 127.0.0.1:{Port}: {e.Message}. "
                    + "Another program may already use that port; DSH Terraria tools will be unavailable this session.");
            }
        }

        public override void Unload()
        {
            running = false;
            try { listener?.Stop(); } catch { }
            listener = null;
            lock (pending)
            {
                while (pending.Count > 0)
                {
                    var item = pending.Dequeue();
                    Interlocked.Exchange(ref item.Cancelled, 1);
                    item.Done.Set();
                }
            }
        }

        private static void AcceptLoop()
        {
            while (running)
            {
                TcpClient client;
                try { client = listener.AcceptTcpClient(); }
                catch { break; }

                var t = new Thread(() => Serve(client)) { IsBackground = true };
                t.Start();
            }
        }

        private static void Serve(TcpClient client)
        {
            try
            {
                client.NoDelay = true;
                using NetworkStream stream = client.GetStream();
                using var reader = new StreamReader(stream, new UTF8Encoding(false));
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                string line;
                while (running && (line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    // Anything that is not our JSON envelope gets closed immediately and
                    // quietly. Terraria's own netcode speaks a binary/text handshake, so a
                    // stray client (or a port scan) must not see a stack trace, and must
                    // never be answered as if it were a command.
                    if (line[0] != '{')
                    {
                        modRef?.Logger.Debug($"[dsh] bridge: ignoring non-JSON client input ({line.Length} bytes)");
                        break;
                    }
                    string response;
                    try
                    {
                        // Echo the caller's correlation id (if any) so it can discard
                        // lines that are not the answer to its own request.
                        response = ExecuteOnMainThread(line, ReadReq(line));
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        response = Json.Error(e.Message);
                    }
                    writer.WriteLine(response ?? Json.Error("empty response"));
                }
            }
            catch { }
            finally
            {
                try { client.Close(); } catch { }
            }
        }

        /// <summary>
        /// 把命令丢到主线程执行。
        /// 世界内：走 tick 队列（PostUpdateEverything/UpdateUI 排空），主线程被占住时请求会过期并被跳过，
        /// 避免超时后重复执行有副作用的命令。
        /// 主菜单：tModLoader 不跑世界钩子，队列永远不会被排空（连 ping 都会超时），
        /// 此时没有第二个排空者，直接在主线程上执行即可。
        /// </summary>
        internal static string ExecuteOnMainThread(string json, string req)
        {
            if (Main.gameMenu)
            {
                // Still must run ON the main thread (game state is single-threaded); the menu
                // just has no tick-driven drainer, so hand the work to the game's own queue.
                Json.CurrentReq = req;
                try
                {
                    var task = Main.RunOnMainThread(() => Commands.Handle(json));
                    // Bounded wait: .Result on a wedged main thread would pin this socket
                    // thread forever and leak it one call at a time.
                    if (!task.Wait(4000))
                        return Json.Error("主线程 4 秒内没有响应（游戏可能卡住或正在加载）");
                    return task.Result;
                }
                catch (Exception e)
                {
                    return Json.Error(e.InnerException?.Message ?? e.Message);
                }
                finally
                {
                    Json.CurrentReq = null;
                }
            }

            var item = new Pending { Json = json, Req = req };
            lock (pending) pending.Enqueue(item);
            if (item.Done.Wait(1500)) return item.Response;

            // The request is still queued or is running right now. Say which, because
            // "did not process" is not the same thing as "did not happen": a command that
            // already started WILL take effect, and retrying it would apply it twice.
            Interlocked.Exchange(ref item.Cancelled, 1);
            bool started = Volatile.Read(ref item.Started) != 0;
            // This error is produced on the socket thread, so set the id here too.
            Json.CurrentReq = req;
            return started
                ? Json.Error("命令已在游戏主线程开始执行，但 1500 ms 内没有结束。它【会】生效，不要直接重试；"
                    + "先用 state/inventory/tile 查一下当前状态再决定。")
                : Json.Error("命令在 1500 ms 内没有轮到主线程执行，已被丢弃，不会产生任何效果；可以安全重试。");
        }

        private static void DrainPending()
        {
            while (true)
            {
                Pending item = null;
                lock (pending)
                {
                    if (pending.Count > 0) item = pending.Dequeue();
                }
                if (item == null) return;
                if (Volatile.Read(ref item.Cancelled) != 0)
                {
                    item.Response = Json.Error("request expired before main-thread execution");
                    item.Done.Set();
                    continue;
                }
                Interlocked.Exchange(ref item.Started, 1);
                // Handlers build their envelope on this (main) thread; give it the id.
                Json.CurrentReq = item.Req;
                try { item.Response = Commands.Handle(item.Json); }
                catch (Exception e) { item.Response = Json.Error(e.Message); }
                finally { Json.CurrentReq = null; }
                item.Done.Set();
            }
        }

        /* ── 后台运行 ────────────────────────────────────────────────────
         * 单人模式下 Main.DoUpdate 会在 !hasFocus && netMode==0 时直接
         * gamePaused=true; return，整帧不做世界更新——所以任何钩子都不会被调用。
         * 由于这个早退发生在所有模组钩子之前，只能在另一个线程里把 hasFocus
         * 顶住（每 8ms 写一次），让世界照常 tick，而键盘/鼠标仍是真实状态。
         */
        private static Thread keepFocusThread;
        private static volatile bool keepFocusRun;

        private static void KeepFocusLoop()
        {
            while (running)
            {
                try
                {
                    // A network client keeps ticking while unfocused.  Forcing hasFocus on
                    // the Bot made Terraria sample the desktop mouse even when the Bot
                    // window was behind the host window.  Only single-player needs this
                    // legacy keep-alive; the Bot is explicitly a network client.
                    if (!IsBotClient && !Main.gameMenu && Main.netMode == 0) Main.hasFocus = true;
                }
                catch { /* game not ready */ }
                Thread.Sleep(8);
            }
        }

        /// <summary>
        /// 把"按住跳跃"记录为输入意图；最终在 SetControls 写入 player.controlJump。
        /// 原因：引擎的变高跳跃由输入上升沿驱动（releaseJump / Triggers），只在 PreUpdate 里
        /// 覆盖 controlJump 时引擎根本不跳（实测 4 次 jump 全 0 格），而直接写 velocity 又只能
        /// 恒 5.2 格。写 Triggers 才是"按下跳跃键"，引擎自己算出 1.4~6.1 格的变高。
        /// </summary>
        // ==== 按键边沿事件（tick 级） ====
        // 轮询 humaninput 只能看到采样点，短按往往只落在一帧上（实测出现过 0ms 的跳跃）。
        // 这里按 tick 记“哪一 tick 按下/松开了哪个键”，外部拿事件流就能算出真实按住时长。
        internal sealed class KeyEvent { public long Tick; public string Key; public bool Down; }
        private static readonly List<KeyEvent> KeyEvents = new List<KeyEvent>();
        private const int KeyEventCap = 6000;
        private static readonly Dictionary<string, bool> KeyState = new Dictionary<string, bool>();
        private static readonly string[] WatchedKeys = { "jump", "left", "right", "up", "down", "inventory", "mouseLeft", "mouseRight" };

        // Bridge-generated keys need the same edge semantics as a physical key.  Terraria
        // uses Current for held input and JustPressed/JustReleased for jump and other
        // actions.  Keeping our own previous state lets a command begin with one real
        // press edge, remain held for its requested duration, and end with one release.
        // 多段跳（云朵瓶/暴雪瓶/泰拉靴这类 extraJump）靠的是**每次都要新的按下边沿**。
        // 实测（Bot 客户端，装备云朵瓶 + 泰拉闪耀靴）：连发三次 jump 升 14.98 格，单跳只有 6.09。
        // 所以一次调用要跳 N 段，就必须在段与段之间让 controlJump 真正回落一两帧，
        // 否则 ApplyBridgeTrigger 的 wasHeld 一直是 true，永远产生不了新边沿。
        internal static int JumpExtraLeft;
        internal static ulong JumpNextAt;
        internal static ulong JumpCascadeUntil;
        internal static int JumpExtraMs;
        internal static ulong JumpGapUntil;
        private static bool BridgeJumpHeld, BridgeLeftHeld, BridgeRightHeld, BridgeUpHeld, BridgeDownHeld, BridgeInventoryHeld, BridgeMountHeld;
        // Movement decisions are made before Player.Update.  Keep them as input intents so
        // PostUpdateInput can present the same key state to vanilla in the correct phase.
        // internal 而不是 private：Commands.cs 的 state 回包要只读地暴露这五个意图位，
        // 用来区分「意图没置上」和「意图置上了但 control 没跟随」这两种完全不同的故障。
        internal static bool IntentLeft, IntentRight, IntentUp, IntentDown, IntentJump;
        private static ulong MovementAppliedTick = ulong.MaxValue;

        // These helpers are the actual keyboard-level output.  Keep the intent bit for the
        // next control hook, and set Player.control* immediately when a player is available;
        // the game owns all resulting physics and collision.
        private static void RequestLeft(bool value = true)
        {
            if (!value) return;
            IntentLeft = true;
            var p = Main.LocalPlayer;
            if (p != null && p.active && p.whoAmI == Main.myPlayer) p.controlLeft = true;
        }
        private static void RequestRight(bool value = true)
        {
            if (!value) return;
            IntentRight = true;
            var p = Main.LocalPlayer;
            if (p != null && p.active && p.whoAmI == Main.myPlayer) p.controlRight = true;
        }
        private static void RequestUp(bool value = true)
        {
            if (!value) return;
            IntentUp = true;
            var p = Main.LocalPlayer;
            if (p != null && p.active && p.whoAmI == Main.myPlayer) p.controlUp = true;
        }
        private static void RequestDown(bool value = true)
        {
            if (!value) return;
            IntentDown = true;
            var p = Main.LocalPlayer;
            if (p != null && p.active && p.whoAmI == Main.myPlayer) p.controlDown = true;
        }
        private static void RequestJump(bool value = true)
        {
            if (!value) return;
            IntentJump = true;
            var p = Main.LocalPlayer;
            if (p != null && p.active && p.whoAmI == Main.myPlayer) p.controlJump = true;
        }

        private static void ApplyBridgeTrigger(bool active, ref bool wasHeld, Action<TriggersSet> setCurrent, Action<TriggersSet> setPressed, Action<TriggersSet> setReleased)
        {
            var current = PlayerInput.Triggers.Current;
            if (active)
            {
                setCurrent(current);
                if (!wasHeld) setPressed(PlayerInput.Triggers.JustPressed);
            }
            else if (wasHeld)
            {
                setReleased(PlayerInput.Triggers.JustReleased);
            }
            wasHeld = active;
        }

        /// <summary>
        /// Feed a hook target into the same pre-zoom mouse path that vanilla uses.
        /// Do not use `target - screenPosition` here: DoUpdate_HandleInput calls
        /// SetZoom_Unscaled/CacheMousePositionForZoom/SetZoom_MouseInWorld after
        /// PostUpdateInput, and that chain rebuilds Main.mouseX/Y from the private
        /// original mouse coordinates.  The inverse of vanilla's formula is:
        ///
        ///   final = halfScreen + (raw - halfScreen) / zoom
        ///   raw   = halfScreen + (final - halfScreen) * zoom
        ///
        /// This only supplies mouse input.  Projectile creation and grappling remain
        /// entirely in Player.QuickGrapple.
        /// </summary>
        private static void ApplyHookAimBeforeZoom()
        {
            ulong now = Main.GameUpdateCount;
            if (!HookHeld || HookUntil <= now) return;

            float zoom = Main.GameViewMatrix != null ? Main.GameViewMatrix.Zoom.X : 1f;
            if (!float.IsFinite(zoom) || zoom <= 0f) zoom = 1f;

            float finalX = HookAimWorld.X - Main.screenPosition.X;
            float finalY = HookAimWorld.Y - Main.screenPosition.Y;
            Player p = Main.LocalPlayer;
            // Main.MouseWorld mirrors the vanilla gravDir=-1 conversion.  Solve for
            // the pre-conversion screen Y so upside-down gravity aims at the same world
            // point as normal gravity.
            if (p != null && p.gravDir == -1f)
                finalY = Main.screenHeight - finalY;

            float halfX = Main.screenWidth / 2f;
            float halfY = Main.screenHeight / 2f;
            int rawX = (int)(halfX + (finalX - halfX) * zoom);
            int rawY = (int)(halfY + (finalY - halfY) * zoom);

            DbgHookRawMouseX = rawX;
            DbgHookRawMouseY = rawY;
            DbgHookFinalMouseX = (int)finalX;
            DbgHookFinalMouseY = (int)finalY;
            DbgHookZoom = zoom;
            DbgHookScreenX = Main.screenPosition.X;
            DbgHookScreenY = Main.screenPosition.Y;

            // These fields are exactly what SetZoom_Unscaled copies into Main.mouseX/Y
            // on the next step.  Keep a direct fallback for a loader build that changes
            // the private field names; the later post-zoom write still handles it.
            try
            {
                OriginalMouseXField?.SetValue(null, rawX);
                OriginalMouseYField?.SetValue(null, rawY);
            }
            catch { }
            Main.mouseX = rawX;
            Main.mouseY = rawY;
        }

        /// <summary>
        /// Feed a bridge-owned world point through the same raw-mouse cache that vanilla
        /// uses before SetZoom_MouseInWorld.  This is shared by hook and NPC interaction:
        /// writing world - screenPosition directly is only correct after zoom conversion.
        /// </summary>
        private static void ApplyWorldAimBeforeZoom(Vector2 world)
        {
            float zoom = Main.GameViewMatrix != null ? Main.GameViewMatrix.Zoom.X : 1f;
            if (!float.IsFinite(zoom) || zoom <= 0f) zoom = 1f;

            float finalX = world.X - Main.screenPosition.X;
            float finalY = world.Y - Main.screenPosition.Y;
            Player p = Main.LocalPlayer;
            if (p != null && p.gravDir == -1f)
                finalY = Main.screenHeight - finalY;

            float halfX = Main.screenWidth / 2f;
            float halfY = Main.screenHeight / 2f;
            int rawX = (int)MathF.Round(halfX + (finalX - halfX) * zoom);
            int rawY = (int)MathF.Round(halfY + (finalY - halfY) * zoom);
            try
            {
                OriginalMouseXField?.SetValue(null, rawX);
                OriginalMouseYField?.SetValue(null, rawY);
            }
            catch { }
            Main.mouseX = rawX;
            Main.mouseY = rawY;
        }

        /// <summary>Write the already-zoomed screen coordinate immediately before QuickGrapple.</summary>
        private static void ApplyHookAimAfterZoom()
        {
            float finalX = HookAimWorld.X - Main.screenPosition.X;
            float finalY = HookAimWorld.Y - Main.screenPosition.Y;
            Player p = Main.LocalPlayer;
            if (p != null && p.gravDir == -1f)
                finalY = Main.screenHeight - finalY;
            Main.mouseX = (int)finalX;
            Main.mouseY = (int)finalY;
            DbgHookFinalMouseX = Main.mouseX;
            DbgHookFinalMouseY = Main.mouseY;
            DbgHookScreenX = Main.screenPosition.X;
            DbgHookScreenY = Main.screenPosition.Y;
        }

        private static void ApplyTalkAimBeforeZoom()
        {
            if (TalkTargetNpc < 0 || TalkUntil <= Main.GameUpdateCount) return;
            ApplyWorldAimBeforeZoom(TalkAimWorld);
            float finalX = TalkAimWorld.X - Main.screenPosition.X;
            float finalY = TalkAimWorld.Y - Main.screenPosition.Y;
            Player p = Main.LocalPlayer;
            if (p != null && p.gravDir == -1f)
                finalY = Main.screenHeight - finalY;
            DbgTalkFinalMouseX = (int)MathF.Round(finalX);
            DbgTalkFinalMouseY = (int)MathF.Round(finalY);
            DbgTalkRawMouseX = Main.mouseX;
            DbgTalkRawMouseY = Main.mouseY;
            DbgTalkScreenX = Main.screenPosition.X;
            DbgTalkScreenY = Main.screenPosition.Y;
            DbgTalkZoom = Main.GameViewMatrix != null ? Main.GameViewMatrix.Zoom.X : 1f;
        }

        /// <summary>
        /// Finish or expire a pending talk request.  The actual mouse state is supplied
        /// by ApplyTalkInputBeforeMouseOver, immediately before vanilla DrawMouseOver.
        /// </summary>
        private static void ApplyTalkInputAfterZoom()
        {
            if (TalkTargetNpc < 0) return;

            ulong now = Main.GameUpdateCount;
            Player p = Main.LocalPlayer;
            if (p != null && p.talkNPC == TalkTargetNpc && !string.IsNullOrWhiteSpace(Main.npcChatText))
            {
                TalkTargetNpc = -1;
                TalkUntil = 0;
                TalkPhase = 0;
                DbgTalkMouseRight = false;
                DbgTalkChatRelease = false;
                Main.mouseRight = false;
                Main.mouseRightRelease = true;
                return;
            }

            if (TalkUntil <= now)
            {
                TalkTargetNpc = -1;
                TalkUntil = 0;
                TalkPhase = 0;
                DbgTalkMouseRight = false;
                DbgTalkChatRelease = false;
                Main.mouseRight = false;
                Main.mouseRightRelease = true;
                return;
            }
        }

        /// <summary>
        /// Run in the interface layer immediately before vanilla's "Mouse Over" layer.
        /// DrawMouseOver itself calls SetZoom_Unscaled/SetZoom_MouseInWorld and then
        /// HoverOverNPCs.  Seeding the private raw mouse cache here makes that vanilla
        /// conversion use the current camera position, rather than the camera from
        /// PostUpdateInput earlier in the tick.
        /// </summary>
        /// <summary>
        /// talkoption 的跨帧状态机。每帧只做一件事：
        ///   扫描阶段：把 Main.mouseX/mouseY 挪到下一个候选点，同时检查上一帧那个点有没有让
        ///   npcChatFocusN 变真（原版每帧绘制对话按钮时读鼠标位置判悬停，一帧只结算一个点）；
        ///   命中之后：把鼠标钉在按钮上，走一次「释放 → 按下」的真实左键边沿。
        /// 只写鼠标输入，不碰任何游戏状态（§0.55）。
        /// </summary>
        private static bool ApplyTalkInputBeforeMouseOver()
        {
            // talkfocus {x,y} 的单帧探针由 GUIChatDrawInner 钩子消费。
            // 这里不能提前写 Main.mouseX/Y：Vanilla: Player Chat 随后会重新 SetZoom_UI。
            if (ProbeMouseX >= 0 && ProbeMouseY >= 0)
            {
                return true;
            }

            // talkoption is driven by the GUIChatDrawInner detour below.  The
            // interface layer is intentionally a no-op for it: writing here is too
            // early because Vanilla: Player Chat calls SetZoom_UI again before it
            // reads Main.mouseX/Y.
            if (TalkOptPhase > 0 && TalkOptPhase < 5) return true;
            if (TalkTargetNpc < 0) return true;
            ulong now = Main.GameUpdateCount;
            if (TalkUntil <= now) return true;

            // DrawInterface starts in UI zoom. Restore the original screen dimensions
            // before calculating the raw input that DrawMouseOver will convert.
            try { PlayerInput.SetZoom_Unscaled(); } catch { }
            ApplyTalkAimBeforeZoom();

            if (TalkPhase == 1)
            {
                // Release/arming frame. DrawInterface_41 will publish npcChatRelease=true
                // after this layer, so the next frame has a real right-button edge.
                Main.mouseRight = false;
                Main.mouseRightRelease = true;
                TalkPhase = 2;
            }
            else
            {
                // Press frame. HoverOverNPCs consumes mouseRight && npcChatRelease and
                // performs the complete vanilla conversation setup.
                Main.mouseRight = true;
                Main.mouseRightRelease = true;
                DbgTalkMouseRight = true;
                DbgTalkChatRelease = Main.npcChatRelease;
            }
            return true;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Main.dedServ) return;
            // 插入一个界面层占位，不能把 talkoption 的鼠标写入放在这里：
            // Vanilla: Player Chat 自己会再次 SetZoom_UI，真正的写入由
            // GUIChatDrawInner detour 完成。
            // 原版 DrawInterface 的层序是
            //   ... Vanilla: Chat -> Vanilla: Player Chat（对话按钮，读 Main.mouseX 判悬停）
            //       -> Vanilla: Mouse Text -> Vanilla: Mouse Over -> ...
            // 保留在 Player Chat 之前只是为了让桥的 talk 右键输入不被后续层覆盖；
            // talkoption / talkfocus 不依赖这个层的坐标时序。
            int idx = layers.FindIndex(layer => layer.Name == "Vanilla: Player Chat");
            if (idx < 0) idx = layers.FindIndex(layer => layer.Name == "Vanilla: Chat");
            if (idx < 0) idx = 0;
            layers.Insert(idx, new LegacyGameInterfaceLayer(
                "DshTerrariaBridge: Talk Input",
                ApplyTalkInputBeforeMouseOver,
                // DrawInterface starts in UI zoom and Vanilla: Player Chat is a UI layer.
                // Using Game here changes Main.mouseX/Y to world-space zoom before the
                // probe runs, so a logical UI point is interpreted in the wrong place.
                InterfaceScaleType.UI));
        }

        /// <summary>
        /// Copy the bridge's movement intent into Terraria's per-player control flags in
        /// ModPlayer.SetControls, after vanilla ResetControls/TriggersSet.CopyInto.  This
        /// tModLoader build does not copy mod-written Triggers.Current into control* (the
        /// diagnostic showed pi.right=true while controlRight stayed false), so Triggers are
        /// diagnostic/edge state while control* is the actual movement output.
        /// These are input flags only; physics, collision, jump impulse, and rope state remain
        /// entirely owned by the vanilla Player.Update implementation.
        /// </summary>
        /// <summary>当前游戏 tick，命令线程取时间窗时用（与 ApplyNativeControlFlags 同源）。</summary>
        internal static ulong NowTick() => Main.GameUpdateCount;

        /// <summary>
        /// 坐骑键的输入闸门：<see cref="Main.hasFocus"/> 为假时，原版会跳过
        /// <c>TriggersSet.CopyInto</c>，我们写进 <c>Triggers.Current.QuickMount</c> 的值全部丢弃。
        /// 本方法只在坐骑时间窗内把闸门临时置真，窗口结束就交还给引擎（<c>Main.Update</c>
        /// 每帧都会用 <c>IsActive</c> 重新赋值，所以不会残留）。
        /// </summary>
        internal static void OpenMountInputGate()
        {
            if (Main.dedServ || Main.gameMenu) return;
            ulong now = Main.GameUpdateCount;
            // 预开闸窗口也要开：那几帧不按坐骑键（controlMount 保持 false），
            // 引擎才有机会执行 else { releaseMount = true; }，把下骑的前置条件备好。
            if ((MountUntil > now || MountPreOpenUntil > now || InventoryUntil > now || InventoryPreOpenUntil > now) && !Main.hasFocus) Main.hasFocus = true;
        }

        internal static void ApplyNativeControlFlags(Player player)
        {
            if (player == null || player.whoAmI != Main.myPlayer) return;
            ulong now = Main.GameUpdateCount;
            var triggers = PlayerInput.Triggers.Current;
            // Intent flags are generated by ApplyMovement in the same SetControls callback.
            // Keep the trigger terms only for compatibility with a real physical key; this
            // environment has proven that trigger-only output does not reach control*.
            // 多段跳推进：**按时机**，不是按固定间隔。
            // 云朵瓶/暴雪瓶/泰拉靴这类 extraJump 的收益全在"上一段快到顶时接上"——
            // 实测轨迹：起跳 vy −4.6 → 收窄到 −1.8（快到顶）→ 这时按下去立刻回到 −4.6。
            // 所以判据是「在空中 且 vy 已经大于 −2.0」（快没劲了），不是「等了 200ms」。
            // 超时保护：离起跳超过 90 帧（约 1.5 秒）还没到顶点就不追了，避免落地后乱跳。
            bool onGround = player.velocity.Y == 0f && player.oldVelocity.Y == 0f;
            if (JumpExtraLeft > 0 && !onGround && player.velocity.Y > -1.5f
                && now >= JumpNextAt && now <= JumpCascadeUntil)
            {
                JumpExtraLeft--;
                // 顺序要紧：BeginJump 内部会把 JumpGapUntil 清 0，所以"松两帧"必须在它之后设，
                // 否则 JumpGapUntil 恒为 0，ApplyBridgeTrigger 的 wasHeld 一直是 true，
                // 第二段永远没有新的按下边沿 —— 表现就是"三段跳只接上一段"。
                BeginJump(now + (ulong)Math.Max(2, JumpExtraMs / 16));
                JumpGapUntil = now + 2;
                JumpNextAt = now + 2;                // 下一段仍然等 vy 判据重新满足
            }
            bool bridgeLeft = IntentLeft || (triggers.Left && LeftUntil > now);
            bool bridgeRight = IntentRight || (triggers.Right && RightUntil > now);
            // Vanilla clears both directions when it sees both held at once.  A bridge
            // command is directional, so release the opposite input before applying the
            // requested one; this is still an input flag, never a physics write.
            if (bridgeLeft && !bridgeRight) player.controlRight = false;
            if (bridgeRight && !bridgeLeft) player.controlLeft = false;
            player.controlLeft |= bridgeLeft;
            player.controlRight |= bridgeRight;
            // 上下不能只靠 triggers：实测 `climb dir:down` 时 triggers.Down 为真、intentDown 为假，
            // 于是这一行原本算出来是 false，control 一次都没置上、人一格都不降。
            // 上下这条链上真正带时间窗的是 UpUntil/DownUntil/ClimbUpUntil/ClimbDownUntil，
            // 它们必须直接参与判断，不能躲在 triggers 后面。
            player.controlUp |= IntentUp || UpUntil > now || ClimbUpUntil > now;
            player.controlDown |= IntentDown || DownUntil > now || ClimbDownUntil > now;
            // jump 与上下同一个病：triggers.Jump 只有键盘真的按住才为真。桥发的跳跃意图全部落在
            // JumpUntil 这个时间窗里，躲在 triggers 后面就等于"后台控制时 controlJump 恒 false"。
            // 实测证据（同一存档、同一命令、只换脚下地形）：
            //   站在实心方块上 fly 能一路升 22 格，因为那一次 ApplyMovement 还没跑过、IntentJump 还在；
            //   站在 type 19 木平台上 fly/jump 全部 velY:0、jumped:false，
            //   因为 ApplyMovement 开头（2069）已经把 IntentJump 清掉，只剩被 triggers 短路的那一半。
            player.controlJump |= IntentJump || JumpUntil > now;
            if (InventoryPreOpenUntil <= now)
                player.controlInv |= InventoryUntil > now;
            // 坐骑不在这里写。实测：在 SetControls 里写 controlMount=true（mountFired 已是 true），
            // 到 PostUpdateMiscEffects 读回来仍是 false —— 原版在 SetControls 之后会用输入层结果
            // 重建 controlMount，写在早阶段的 true 当场被覆盖。这和 controlHook 是同一个病
            // （见 ApplySyntheticInput 里抓钩那段注释）。真正的写入点在 ApplySyntheticInput
            // （PreItemCheck / PostUpdateMiscEffects 阶段），那里晚于重建。
            // 诊断：只在这个函数返回前把 control 的当帧值记下来。命令线程读到 control* 时
            // Player.Update 早已跑完、原版已经重置，所以 humaninput 里的 control 不能当证据。
            DbgFlagsTick = (long)now;
            DbgFlagsLeft = player.controlLeft;
            DbgFlagsRight = player.controlRight;
            DbgFlagsUp = player.controlUp;
            DbgFlagsDown = player.controlDown;
            DbgFlagsJump = player.controlJump;
            DbgFlagsIntentRight = IntentRight;
            DbgFlagsIntentJump = IntentJump;
            DbgFlagsRightUntilLive = RightUntil > now;
            DbgFlagsJumpUntilLive = JumpUntil > now;
        }

        // 诊断快照：Player.Update 结束后（引擎已算完移动）最终采信的输入。
        internal static long DbgAfterTick;
        internal static long DbgPreTick;
        internal static bool DbgPreCtrlLeft;
        internal static bool DbgPreCtrlRight;
        internal static bool DbgPreCtrlUp;
        internal static bool DbgPreCtrlDown;
        internal static bool DbgPreCtrlJump;
        internal static float DbgPreVelX;
        internal static bool DbgAfterCtrlLeft, DbgAfterCtrlRight, DbgAfterCtrlUp, DbgAfterCtrlDown, DbgAfterCtrlJump;
        internal static bool DbgAfterTrigLeft, DbgAfterTrigRight, DbgAfterTrigUp, DbgAfterTrigDown, DbgAfterTrigJump;
        internal static float DbgAfterVelX;

        // 诊断快照：PostUpdateMiscEffects 返回后，原版绳索/抓钩/ItemCheck 还没消费输入。
        internal static long DbgPrePhysicsTick;
        internal static bool DbgPrePhysicsUp, DbgPrePhysicsDown, DbgPrePhysicsHook;
        internal static bool DbgPrePhysicsMount, DbgPrePhysicsReleaseMount;
        internal static bool DbgPrePhysicsUseItem, DbgPrePhysicsUseTile;

        // 诊断快照：ApplyNativeControlFlags 结束时 control* 的真实值。
        internal static long DbgFlagsTick;
        internal static bool DbgFlagsLeft, DbgFlagsRight, DbgFlagsUp, DbgFlagsDown, DbgFlagsJump;
        internal static bool DbgFlagsIntentRight, DbgFlagsIntentJump;
        internal static bool DbgFlagsRightUntilLive, DbgFlagsJumpUntilLive;

        // Bot/client input provenance.  These are read-only diagnostics: they make it
        // possible to tell a real desktop click from a bridge action in a two-window test.
        internal static bool DbgBotClient;
        internal static bool DbgBotWindowForeground;
        internal static bool DbgBotMouseSuppressed;
        internal static bool DbgPhysicalMouseLeft, DbgPhysicalMouseRight;
        internal static bool DbgBridgeMouseLeft, DbgBridgeMouseRight;

        private static bool ReadKey(string key)
        {
            try
            {
                var t = PlayerInput.Triggers.Current;
                switch (key)
                {
                    case "jump": return t.Jump;
                    case "left": return t.Left;
                    case "right": return t.Right;
                    case "up": return t.Up;
                    case "down": return t.Down;
                    case "inventory": return t.Inventory;
                    case "mouseLeft": return Main.mouseLeft;
                    case "mouseRight": return Main.mouseRight;
                    default: return false;
                }
            }
            catch { return false; }
        }

        /// <summary>把 sinceTick 之后的事件按时间正序交出去（只读，最多 max 条）。</summary>
        internal static List<KeyEvent> SnapshotKeyEvents(long sinceTick, int max)
        {
            var outp = new List<KeyEvent>();
            lock (KeyEvents)
            {
                for (int i = KeyEvents.Count - 1; i >= 0 && outp.Count < max; i--)
                {
                    if (KeyEvents[i].Tick < sinceTick) break;
                    outp.Add(KeyEvents[i]);
                }
            }
            outp.Reverse();
            return outp;
        }

        internal static void ClearKeyEvents() { lock (KeyEvents) { KeyEvents.Clear(); KeyState.Clear(); } }

        internal static long LastKeyEventTick()
        {
            lock (KeyEvents) return KeyEvents.Count > 0 ? KeyEvents[KeyEvents.Count - 1].Tick : 0;
        }

        private static void SuppressBackgroundBotMouse()
        {
            DbgBotClient = IsBotClient;
            DbgBotWindowForeground = !IsBotClient || IsThisProcessForeground();
            DbgBotMouseSuppressed = false;
            DbgPhysicalMouseLeft = Main.mouseLeft;
            DbgPhysicalMouseRight = Main.mouseRight;
            DbgBridgeMouseLeft = false;
            DbgBridgeMouseRight = false;

            if (!IsBotClient || DbgBotWindowForeground) return;

            ulong now = Main.GameUpdateCount;
            // These actions are generated by the bridge later in the same tick.  Clearing
            // the physical sample here does not cancel them: ApplySyntheticInput and the
            // chat/button detours write their own mouse state at their actual vanilla read
            // points.  It only prevents a click in the host window from becoming a Bot use.
            bool bridgeLeft = UseUntil > now || (HookHeld && HookUntil > now);
            bool bridgeRight = UseTileUntil > now || TalkTargetNpc >= 0 || TalkOptPhase > 0;
            DbgBridgeMouseLeft = bridgeLeft;
            DbgBridgeMouseRight = bridgeRight;

            var current = PlayerInput.Triggers.Current;
            if (!bridgeLeft)
            {
                current.MouseLeft = false;
                PlayerInput.Triggers.JustPressed.MouseLeft = false;
                PlayerInput.Triggers.JustReleased.MouseLeft = true;
                Main.mouseLeft = false;
                Main.mouseLeftRelease = true;
            }
            if (!bridgeRight)
            {
                current.MouseRight = false;
                PlayerInput.Triggers.JustPressed.MouseRight = false;
                PlayerInput.Triggers.JustReleased.MouseRight = true;
                Main.mouseRight = false;
                Main.mouseRightRelease = true;
            }
            DbgBotMouseSuppressed = true;
        }

        // 注意：ModSystem 没有 PreUpdateInput 这个可重写钩子（写了会 CS0115 编不过，m17982 实测）。
        // 注入点只能留在 PostUpdateInput —— 它本身就在 Main.UpdateInput 之后、Player.Update 之前。
        public override void PostUpdateInput()
        {
            if (Main.gameMenu || Main.dedServ) return;
            SuppressBackgroundBotMouse();
            ulong now = Main.GameUpdateCount;
            Player player = Main.LocalPlayer;
            if (ClimbUpUntil > now || ClimbDownUntil > now)
                UpdateClimbTarget(player);

            bool bridgeUp = IntentUp || UpUntil > now || ClimbUpUntil > now;
            bool bridgeDown = IntentDown || DownUntil > now || ClimbDownUntil > now;
            // 平台下穿不是跳跃：原版要求 controlDown 按住且 controlJump 为 false，
            // 让引擎自己一格一格往下掉。以前把 down 并进 jump，按下键就变成起跳。
            bool bridgeJump = (IntentJump || JumpUntil > now) && now >= JumpGapUntil;

            // Supply native game input triggers. Player.Update rebuilds controlUp/down from
            // these triggers, so control flags written earlier by ModPlayer are overwritten.
            ApplyBridgeTrigger(bridgeUp, ref BridgeUpHeld, t => t.Up = true, t => t.Up = true, t => t.Up = true);
            ApplyBridgeTrigger(bridgeDown, ref BridgeDownHeld, t => t.Down = true, t => t.Down = true, t => t.Down = true);
            ApplyBridgeTrigger(bridgeJump, ref BridgeJumpHeld, t => t.Jump = true, t => t.Jump = true, t => t.Jump = true);
            // Triggers 保留真人按键的边沿/诊断语义；桥的实际移动输出在 SetControls
            // 由 ApplyNativeControlFlags 写入 controlLeft/controlRight。
            ApplyBridgeTrigger(IntentLeft || LeftUntil > now, ref BridgeLeftHeld, t => t.Left = true, t => t.Left = true, t => t.Left = true);
            ApplyBridgeTrigger(IntentRight || RightUntil > now, ref BridgeRightHeld, t => t.Right = true, t => t.Right = true, t => t.Right = true);
            // QuickMount 由原版 TriggersSet.CopyInto 复制进 Player.controlMount，
            // 时机就在 Player.Update 处理 controlMount/releaseMount 边沿之前。
            // 不要在更晚的阶段自己合成 controlMount —— 它的读点(23783)早于所有晚阶段钩子。
            // （2026-10-29 曾被改成直写 controlMount 而失效，此处按 0c93b55 恢复）
            // 预开闸期内（MountPreOpenUntil > now）刻意不按键：那几帧要留给引擎执行
            // else { releaseMount = true; }，否则 releaseMount 一直是 false，QuickMount 不进。
            ApplyBridgeTrigger(MountUntil > now && MountPreOpenUntil <= now, ref BridgeMountHeld, t => t.QuickMount = true, t => t.QuickMount = true, t => t.QuickMount = true);
            // Inventory is Terraria's semantic Escape key. Keep the real trigger and
            // its press/release edges; ApplyNativeControlFlags mirrors the same input
            // into controlInv for this loader build's Player.Update consumer.
            // 预开闸期内（InventoryPreOpenUntil > now）刻意不按键：那几帧要留给引擎把
            // releaseInventory 置回 true，否则 ToggleInv 永远看不到"刚按下"这个边沿。
            if (InventoryPreOpenUntil > now)
            {
                ApplyBridgeTrigger(false, ref BridgeInventoryHeld,
                    t => t.Inventory = true, t => t.Inventory = true, t => t.Inventory = true);
            }
            else
            {
                ApplyBridgeTrigger(InventoryUntil > now, ref BridgeInventoryHeld,
                    t => t.Inventory = true, t => t.Inventory = true, t => t.Inventory = true);
            }
            var triggers = PlayerInput.Triggers.Current;
            DbgInputTriggerTick = (long)now;
            DbgInputTriggerPlayer = player.whoAmI;
            DbgInputTriggerLeft = triggers.Left;
            DbgInputTriggerRight = triggers.Right;
            DbgInputTriggerUp = triggers.Up;
            DbgInputTriggerDown = triggers.Down;
            DbgInputTriggerJump = triggers.Jump;
            DbgInputTriggerMount = triggers.QuickMount;
            DbgInputTriggerInventory = triggers.Inventory;
            // Compatibility field now follows the native trigger, which is the actual input source.
            DbgCtrlUp = triggers.Up;
            // 采集边沿事件：只记“变化”，同一状态不重复记。
            long tick = (long)now;
            // 本模组自己下发的输入不算人的操作，否则示范库里会混进脚本自己的动作。
            bool bridgeUse = UseUntil > (ulong)tick || UseTileUntil > (ulong)tick;
            foreach (var key in WatchedKeys)
            {
                if (bridgeJump && key == "jump") continue;
                if (bridgeUp && key == "up") continue;
                if (bridgeDown && key == "down") continue;
                if (bridgeUse && (key == "mouseLeft" || key == "mouseRight")) continue;
                if (InventoryUntil > now && key == "inventory") continue;
                bool keyDownNow = ReadKey(key);
                bool was = KeyState.TryGetValue(key, out var prev) && prev;
                if (keyDownNow == was) continue;
                KeyState[key] = keyDownNow;
                lock (KeyEvents)
                {
                    KeyEvents.Add(new KeyEvent { Tick = tick, Key = key, Down = keyDownNow });
                    if (KeyEvents.Count > KeyEventCap) KeyEvents.RemoveRange(0, KeyEvents.Count - KeyEventCap);
                }
            }
            // This callback runs immediately before vanilla's zoom conversion.  Feed
            // the inverse coordinate now; ApplySyntheticInput writes the final screen
            // coordinate again in the player update phase before QuickGrapple consumes it.
            ApplyHookAimBeforeZoom();
            ApplyTalkAimBeforeZoom();
            // ApplyMovement is intentionally not called here.  It must run from
            // DshControlPlayer.PreUpdate, after these triggers are present, so vanilla does
            // not rebuild controlLeft/controlRight/controlJump over the bridge's input.
        }

        private static bool IsStandingOnPlatform(Player player)
        {
            if (player == null || !player.active || player.velocity.Y < 0f) return false;
            int x = (int)(player.Center.X / 16f);
            int y = (int)((player.position.Y + player.height + 6f) / 16f);
            if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return false;
            Tile tile = Main.tile[x, y];
            return tile != null && tile.HasTile && TileID.Sets.Platforms[tile.TileType];
        }

        public override void PostUpdateEverything()
        {
            InstallChatHook();
            InstallTalkOptionHook();
            TickTravel();
            DrainPending();
            ApplyTalkInputAfterZoom();
        }


        /* ── 聊天收件箱：把你在游戏聊天框里敲的话交给 agent ─────────────
         * 原理：挂 Main.DoUpdate_HandleChat（按回车真正发消息的那一步）的前置，
         *       把 Main.chatText 抄一份进收件箱。只读复制，不改游戏行为。
         * 不用按键注入、不抢焦点、不碰用户桌面。
         */
        private static readonly object ChatLock = new object();
        private static readonly List<string> ChatInbox = new List<string>();
        // ChatPending 只用于诊断当前输入框内容；它不再承担“变空即发送”的判定。
        private static string ChatPending = "";
        private static ulong ChatPendingTick;
        // 上限与"丢了多少"：ChatPoll 的 since 是绝对序号，丢了旧的就要把序号补回去，
        // 否则调用方手里的 since 会指向错位的内容。
        private const int ChatInboxCap = 500;
        private static int ChatDropped;
        private static ulong ChatHookCalls;
        private static ulong ChatLastSendTick;
        private static string ChatLastSentText = "";
        private static ulong ChatLastCancelTick;
        private static string ChatLastEvent = "";
        private static string ChatLastCandidate = "";
        internal static bool ChatHookInstalled;
        internal static string ChatHookError = "";
        private static bool ChatHookTried;

        // talkoption must write the mouse after GameInterfaceLayer.Draw has selected
        // the UI zoom and immediately before GUIChatDrawInner reads it.  A separate
        // interface layer runs too early: the subsequent Vanilla: Player Chat layer
        // calls SetZoom_UI again and restores the OS mouse coordinates.
        private static bool TalkOptionHookTried;
        internal static bool TalkOptionHookInstalled;
        internal static string TalkOptionHookError = "";
        internal static ulong TalkOptionHookCalls;

        internal static void InstallChatHook()
        {
            if (ChatHookTried) return;
            ChatHookTried = true;
            try
            {
                var mi = typeof(Main).GetMethod("DoUpdate_HandleChat",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (mi == null) { ChatHookError = "找不到 Main.DoUpdate_HandleChat"; return; }
                // 镜像原版 DoUpdate_HandleChat 的发送分支：
                //   inputTextEnter && chatRelease && chatText.Length > 0
                // 但 inputTextEnter 是在 orig() 内部由 GetInputText() 计算的，
                // 所以必须保存 orig 前的文本/释放状态，再在 orig 后用当前帧的
                // inputTextEnter + chatText 清空结果确认“真的按回车发送”。
                Terraria.ModLoader.MonoModHooks.Add(mi, (Action<Action>)(orig =>
                {
                    string beforeText = Main.chatText ?? "";
                    string trimmed = beforeText.Trim();
                    bool wasChatOpen = Main.drawingPlayerChat;
                    bool releaseBefore = Main.chatRelease;
                    lock (ChatLock)
                    {
                        ChatHookCalls++;
                        if (wasChatOpen && trimmed.Length > 0)
                        {
                            ChatPending = trimmed;
                            ChatPendingTick = Main.GameUpdateCount;
                        }
                    }

                    // 绝不吞掉原版异常，也绝不跳过原版聊天处理。
                    orig();

                    try
                    {
                        ulong now = Main.GameUpdateCount;
                        // 同一句话会被判成「已发送」两次：原版发送并清空 chatText 之后，
                        // 只要回车还按着，下一帧 inputTextEnter 与 chatRelease 仍为真，
                        // 而 chatText 此刻又是空的——三个条件原地重成立，于是重复入队。
                        // 用「文本 + 时间窗」去重：同一文本 4 tick 内只记一条。
                        // 真连打两句同样的话只隔几百毫秒，所以窗口必须小于 4 tick（约 66ms）。
                        bool duplicate = trimmed == ChatLastSentText &&
                            ChatLastSendTick != 0 && now - ChatLastSendTick <= 4;
                        bool sent = wasChatOpen && releaseBefore && Main.inputTextEnter &&
                            trimmed.Length > 0 && (Main.chatText ?? "").Length == 0 && !duplicate;
                        bool cancelled = wasChatOpen && !sent && Main.inputTextEscape;
                        if (sent)
                        {
                            bool command = trimmed.StartsWith("/", StringComparison.Ordinal);
                            lock (ChatLock)
                            {
                                ChatLastCandidate = trimmed;
                                ChatLastEvent = command ? "command" : "sent";
                                if (!command)
                                {
                                    ChatInbox.Add(trimmed);
                                    while (ChatInbox.Count > ChatInboxCap)
                                    {
                                        ChatInbox.RemoveAt(0);
                                        ChatDropped++;
                                    }
                                    ChatLastSendTick = now;
                                    ChatLastSentText = trimmed;
                                }
                                ChatPending = "";
                                ChatPendingTick = 0;
                            }
                        }
                        else if (cancelled)
                        {
                            lock (ChatLock)
                            {
                                ChatLastCandidate = trimmed;
                                ChatLastEvent = "cancel";
                                ChatLastCancelTick = now;
                                ChatPending = "";
                                ChatPendingTick = 0;
                            }
                        }
                    }
                    catch { }
                }));
                ChatHookInstalled = true;
            }
            catch (Exception ex) { ChatHookError = ex.GetType().Name + ": " + ex.Message; }
        }

        internal static void InstallTalkOptionHook()
        {
            if (TalkOptionHookTried) return;
            TalkOptionHookTried = true;
            try
            {
                var mi = typeof(Main).GetMethod("GUIChatDrawInner",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (mi == null) { TalkOptionHookError = "找不到 Main.GUIChatDrawInner"; return; }

                // For an instance parameterless method the orig delegate still
                // receives the target instance.  Passing Action here silently
                // creates an incompatible detour: the command enters phase 1,
                // but GUIChatDrawInner never reaches the scanner.
                Terraria.ModLoader.MonoModHooks.Add(mi, (Action<Action<Main>, Main>)((orig, self) =>
                {
                    TalkOptionHookCalls++;
                    try
                    {
                        PrepareTalkOptionInput();
                        orig(self);
                    }
                    finally
                    {
                        FinishTalkOptionInput();
                    }
                }));
                TalkOptionHookInstalled = true;
            }
            catch (Exception ex) { TalkOptionHookError = ex.GetType().Name + ": " + ex.Message; }
        }

        private static bool TalkOptionFocusHit()
        {
            return TalkOptIndex == 1 ? Main.npcChatFocus1
                : TalkOptIndex == 2 ? Main.npcChatFocus2
                : TalkOptIndex == 3 ? Main.npcChatFocus3
                : TalkOptIndex == 4 && Main.npcChatFocus4;
        }

        private static void PrepareTalkOptionInput()
        {
            // talkfocus is a one-shot probe.  It is handled at the same point as
            // talkoption so Vanilla: Player Chat cannot overwrite the coordinate.
            if (ProbeMouseX >= 0 && ProbeMouseY >= 0)
            {
                Main.mouseX = ProbeMouseX;
                Main.mouseY = ProbeMouseY;
                ProbeMouseX = -1;
                ProbeMouseY = -1;
            }

            if (TalkOptPhase == 0) return;
            Player pl = Main.LocalPlayer;
            if (pl == null || pl.talkNPC < 0)
            {
                TalkOptPhase = 5;
                TalkOptMsg = "对话已经结束，没法点按钮";
                return;
            }

            if (TalkOptPhase == 1)
            {
                Main.mouseX = TalkOptX;
                Main.mouseY = TalkOptY;
                Main.mouseLeft = false;
                Main.mouseLeftRelease = true;
                return;
            }

            // Keep the hit point and feed the same release/press edge that a real
            // mouse click produces.  GUIChatDrawInner will recompute focus from this
            // point and then run its normal button callback.
            Main.mouseX = TalkOptHitX;
            Main.mouseY = TalkOptHitY;
            if (TalkOptPhase == 2)
            {
                Main.mouseLeft = false;
                Main.mouseLeftRelease = true;
            }
            else if (TalkOptPhase == 3)
            {
                Main.mouseLeft = true;
                Main.mouseLeftRelease = true;
            }
            else if (TalkOptPhase == 4)
            {
                Main.mouseLeft = false;
                Main.mouseLeftRelease = true;
            }
        }

        private static void FinishTalkOptionInput()
        {
            if (TalkOptPhase == 0) return;
            TalkOptLastIgnoreMouseInterface = PlayerInput.IgnoreMouseInterface;
            TalkOptLastItemAnimation = Main.LocalPlayer?.itemAnimation ?? -1;
            TalkOptLastUsingGamepad = PlayerInput.UsingGamepad;
            TalkOptLastMouseX = Main.mouseX;
            TalkOptLastMouseY = Main.mouseY;
            if (PlayerInput.IgnoreMouseInterface)
            {
                TalkOptBlocked++;
                TalkOptMsg = "等待原版允许鼠标界面后继续扫描/点击";
                return;
            }

            if (TalkOptPhase == 1)
            {
                if (TalkOptionFocusHit())
                {
                    TalkOptHitX = TalkOptX;
                    TalkOptHitY = TalkOptY;
                    TalkOptPhase = 2;
                    TalkOptMsg = $"找到按钮 {TalkOptIndex} 在 ({TalkOptHitX},{TalkOptHitY})";
                    return;
                }

                TalkOptScanned++;
                int w = Main.screenWidth, h = Main.screenHeight;
                int x0 = 100, x1 = Math.Max(x0 + 10, w - 100);
                int y0 = 80, y1 = Math.Max(y0 + 10, h / 2);
                const int step = 8;
                TalkOptX += step;
                if (TalkOptX > x1) { TalkOptX = x0; TalkOptY += step; }
                if (TalkOptY > y1)
                {
                    TalkOptPhase = 5;
                    TalkOptMsg = $"扫完 {TalkOptScanned} 个点都没有按钮 {TalkOptIndex}";
                }
                return;
            }

            if (TalkOptPhase == 2)
            {
                TalkOptPhase = 3;
                return;
            }
            if (TalkOptPhase == 3)
            {
                TalkOptPhase = 4;
                TalkOptMsg = $"已发送原版左键输入到按钮 {TalkOptIndex} @ ({TalkOptHitX},{TalkOptHitY})；请回读对话/商店状态确认效果";
                return;
            }
            if (TalkOptPhase == 4)
            {
                TalkOptPhase = 6;
            }
        }

        internal static object ChatPoll(int since, int max)
        {
            lock (ChatLock)
            {
                if (since < 0) since = 0;
                // since 是绝对序号；缓冲里丢过 ChatDropped 条旧的，先换算成下标。
                // 丢了又不补，调用方手里的 since 会指到别的消息上（读出来的内容悄悄错位）。
                int rel = since - ChatDropped;
                if (rel < 0) rel = 0;
                if (rel > ChatInbox.Count) rel = ChatInbox.Count;
                int take = Math.Min(max, ChatInbox.Count - rel);
                var lines = ChatInbox.GetRange(rel, take);
                int next = ChatDropped + rel + take;
                return new
                {
                    hooked = ChatHookInstalled,
                    hookError = ChatHookError.Length > 0 ? ChatHookError : null,
                    next = next,
                    pending = ChatDropped + ChatInbox.Count - next,
                    dropped = ChatDropped,
                    hookCalls = ChatHookCalls,
                    lastSendTick = ChatLastSendTick,
                    lastCancelTick = ChatLastCancelTick,
                    lastEvent = ChatLastEvent,
                    lastCandidate = ChatLastCandidate,
                    pendingSinceTick = ChatPendingTick,
                    // 诊断：pendingText 非空说明玩家仍在输入框里打字，还没有按回车发送。
                    pendingText = ChatPending,
                    lines = lines.ToArray(),
                };
            }
        }

        internal static object ChatPost(string text)
        {
            Main.NewText(text, new Microsoft.Xna.Framework.Color(120, 220, 255));
            return new { posted = text, hooked = ChatHookInstalled };
        }

        /* ── 寻路（A*）＋ 逐格执行 ────────────────────────────────────────
         * 前面所有"朝目标推"的走法都会掉坑或卡死，因为从不看全局。
         * 这里做真正的寻路：在包围盒里用 A* 算一条代价最小的路，再一格一格执行
         * （实心就挖、悬空就搭、往下就挖穿、往上就跳）。
         * 代价模型：空气+脚下有地=1；空气但悬空=3；实心=8（要挖）；水/岩浆=不可通行。
         */
        internal static bool PathActive;
        internal static List<(int x, int y)> PathCells = new();
        internal static int PathIndex;
        internal static string PathStop = "";
        internal static ulong PathDeadline;
        // 默认不挖：挖是破坏性动作，必须由上层显式 dig:true 才允许。
        // 事故：默认 true 时 walkto 一靠近房子就把周围土石刨了一遍。
        internal static bool PathAllowDig = false;
        /// <summary>A* 算不出路时（要跳上屋顶、门在反方向这类三维动作）退回"朝目标贪心走"。</summary>
        internal static bool PathGreedy;
        // 自己造的东西不拿去当路挖（实测事故：从自建的 NPC 房出来时，寻路直接把墙挖穿，房子当场漏气）。
        // 做法是先把这些方块当不可通行，让 A* 绕路；真绕不过去再由调用方显式放开。
        internal static bool PathProtectBuilds = true;
        private static readonly HashSet<int> ProtectTiles = new() { 4, 10, 11, 15, 16, 17, 18, 19, 21, 26, 30, 31, 50, 51, 63, 72, 73, 77, 85, 86 };

        // 人自己认下来的“不许挖”矩形（自己的房子、NPC 房、箱区…）。
        // 只靠方块类型判断不够：自建房屋的地板常常就是普通的土/石，按类型看和天然地形一模一样。
        // 真正的判据是“这块地方是我造的/我要留的”，只能由建造方登记。
        internal static List<(int x0, int y0, int x1, int y1)> NoDigZones = new();

        internal static bool InNoDigZone(int x, int y)
        {
            for (int i = 0; i < NoDigZones.Count; i++)
            {
                var z = NoDigZones[i];
                if (x >= z.x0 && x <= z.x1 && y >= z.y0 && y <= z.y1) return true;
            }
            return false;
        }

        internal static bool IsProtectedBuild(int x, int y)
        {
            if (InNoDigZone(x, y)) return true;
            Tile t = Main.tile[x, y];
            return t.HasTile && ProtectTiles.Contains(t.TileType);
        }
        private static ulong PathNextAct;
        private static int PathLastIndex = -1, PathIdxStuck;
        // 位置连续若干 tick 没变才算“真被挡住”。实测教训：无条件往前挥镐，
        // 人还在自建房里走的时候就把地板、墙一起刨了。
        private static int PathStillPx, PathStillPy, PathStillTicks;

        private static bool TileAir(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return false;
            Tile t = Main.tile[x, y];
            return !t.HasTile || !Main.tileSolid[t.TileType];
        }

        private static bool TileLiquid(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return false;
            Tile t = Main.tile[x, y];
            return t.LiquidAmount > 40;
        }

        private static bool FloorWithin(int x, int y, int depth)
        {
            for (int k = 1; k <= depth; k++)
                if (TileStand(x, y + k)) return true;
            return false;
        }

        /// <summary>最近一次寻路的诊断串（只读，path/travelstatus 回传）</summary>
        internal static string LastPathWhy = "";
        internal static bool DbgPulley, DbgCtrlUp, DbgPlayerControlUp, DbgRopeTile;
        internal static float DbgVelY;
        // Input timing snapshots: native Triggers after PostUpdateInput and controls after engine processing.
        internal static long DbgInputTriggerTick = -1, DbgInputControlTick = -1;
        internal static int DbgInputTriggerPlayer = -1, DbgInputControlPlayer = -1;
        internal static bool DbgInputTriggerLeft, DbgInputTriggerRight, DbgInputTriggerUp, DbgInputTriggerDown, DbgInputTriggerJump, DbgInputTriggerMount, DbgInputTriggerInventory;
        internal static bool DbgInputControlLeft, DbgInputControlRight, DbgInputControlUp, DbgInputControlDown, DbgInputControlJump;

        internal static List<(int x, int y)> ComputePath(int sx, int sy, int tx, int ty, out string why, int margin = 10)
        {
            why = "";
            int requestedTx = tx, requestedTy = ty;
            ty = NormalizePathTargetY(tx, ty, 4);
            // 包围盒放宽到 ±26 试过（想让 A* 看到身后的门），结果更早卡住（2160 就 stuck）：
            // 框一大，A* 反而更容易挑到"绕远但代价低"的路，走到门口又过不去。先回到 ±10。
            int x0 = Math.Max(1, Math.Min(sx, tx) - margin), x1 = Math.Min(Main.maxTilesX - 2, Math.Max(sx, tx) + margin);
            int y0 = Math.Max(1, Math.Min(sy, ty) - 14), y1 = Math.Min(Main.maxTilesY - 2, Math.Max(sy, ty) + 18);
            int w = x1 - x0 + 1, h = y1 - y0 + 1;
            if (w <= 0 || h <= 0) { why = "包围盒无效"; return null; }

            var cost = new float[w, h];
            var prev = new (int, int)[w, h];
            var done = new bool[w, h];
            for (int i = 0; i < w; i++) for (int j = 0; j < h; j++) { cost[i, j] = float.MaxValue; prev[i, j] = (-1, -1); }

            float StepCost(int x, int y)
            {
                if (TileLiquid(x, y)) return float.MaxValue;
                if (!TileAir(x, y))
                {
                    // 门是“走过去就开”的通道，不是墙：A* 必须允许穿过门，否则自建 NPC 房会被
                    // 判成完全封闭、整条路算不出来（表现为 walkto 一启动就 stuck）。
                    var tt = Main.tile[x, y];
                    if (tt.HasTile && (tt.TileType == TileID.ClosedDoor || tt.TileType == TileID.OpenDoor)) return 1f;
                    if (PathProtectBuilds && IsProtectedBuild(x, y)) return float.MaxValue;   // 自己造的：绕路
                    return PathAllowDig ? 8f : float.MaxValue;   // 要挖（禁挖时不可通行）
                }
                // 深度取 3：房内地板比玩家中心低 2~3 格，只看 2 格会把整间房判成"悬空"，
                // A* 于是宁愿选更贵的绕路也不进房（m17158 卡在 2166 的直接原因）。
                return FloorWithin(x, y, 3) ? 1f : 20f;        // 有地=1；悬空=20（高价，只有别无选择才用）
            }

            int si = sx - x0, sj = sy - y0, ti = tx - x0, tj = ty - y0;
            if (si < 0 || sj < 0 || si >= w || sj >= h || ti < 0 || tj < 0 || ti >= w || tj >= h) { why = "起点/终点在包围盒外"; return null; }
            // 失败诊断：把起点/终点/目标格的判定一次性回传，不然只能靠猜（m17649 no-path 反复排查）。
            LastPathWhy = $"s=({sx},{sy}) reqT=({requestedTx},{requestedTy}) t=({tx},{ty}) box=[{x0},{x1}]x[{y0},{y1}] "
                        + $"bodyS={PathBodyFree(sx, sy)} floorS={PathFloorAt(sx, sy)} standS={TileStand(sx, sy + 1)} "
                        + $"bodyT={PathBodyFree(tx, ty)} floorT={PathFloorAt(tx, ty)} standT={TileStand(tx, ty + 1)}";

            var open = new List<(int i, int j)>();
            cost[si, sj] = 0;
            open.Add((si, sj));
            int guard = 0, openedCount = 0;
            while (open.Count > 0 && guard++ < 600000)
            {
                int bi = 0; float best = float.MaxValue;
                for (int k = 0; k < open.Count; k++)
                {
                    float f = cost[open[k].i, open[k].j] + Math.Abs(open[k].i - ti) + Math.Abs(open[k].j - tj) * 0.5f;
                    if (f < best) { best = f; bi = k; }
                }
                var (ci, cj) = open[bi];
                open.RemoveAt(bi);
                if (ci == ti && cj == tj) break;
                if (done[ci, cj]) continue;
                done[ci, cj] = true;
                openedCount++;

                // ── 垂直层：走 / 跨 / 跳 / 落 四类边 ──────────────────────────
                // 只有「走」和「下坠」时 A* 才看不见"跳上屋顶再下来"这条路：
                // 自建房一旦把地面通道堵死，平面图上就成了完全封闭（m17291 卡在房 A 右墙前）。
                int cx = x0 + ci, cy = y0 + cj;
                void TryEdge(int nx, int ny, float w8)
                {
                    int ni = nx - x0, nj = ny - y0;
                    if (ni < 0 || nj < 0 || ni >= w || nj >= h) return;
                    float sc = StepCost(nx, ny);
                    if (sc >= float.MaxValue) return;
                    float nc = cost[ci, cj] + w8;
                    if (nc < cost[ni, nj]) { cost[ni, nj] = nc; prev[ni, nj] = (ci, cj); open.Add((ni, nj)); }
                }

                // 同列垂直跳：真实玩家可以在没有水平位移时原地跳上上方平台。
                // 旧图只有左右邻居，目标正好在同列时永远不会生成这条边，
                // 于是 TickWalk 虽然知道 dy<0，却拿不到需要执行的路点。
                // 仍要求身体净空和脚下可站立，跳跃高度沿用原版满跳上限。
                for (int up = 1; up <= 6; up++)
                {
                    int jy = cy - up;
                    if (!PathColumnClear(cx, cy, jy)) break;
                    if (!PathBodyFree(cx, jy)) continue;
                    if (!PathFloorAt(cx, jy)) continue;
                    TryEdge(cx, jy, 4f + up * 0.3f);
                }

                for (int d = -1; d <= 1; d += 2)
                {
                    // 走 / 跨（原版能自己上的 1~2 格台阶）
                    for (int up = 0; up <= 2; up++)
                    {
                        int nx = cx + d, ny = cy - up;
                        if (!PathBodyFree(nx, ny)) continue;
                        if (!PathFloorAt(nx, ny)) continue;
                        TryEdge(nx, ny, up == 0 ? 1f : 1.2f * up);
                    }
                    // 跳：水平 1~3、上升 0~6（满跳 6.1 格）；同高横跳只允许跨 2 格以上
                    // 水平跨距 4 格：3 格宽的坑要"从左沿跳到右沿"，正好 4 格；
                    // 上限定 3 时这类坑在图上就是死路（离线用例 gap3 抓到）。
                    for (int step = 1; step <= 4; step++)
                    {
                        int jx = cx + d * step;
                        for (int up = 0; up <= 6; up++)   // 净空充足时满跳 6.1（头顶被压会被削，净空另行判定）
                        {
                            if (up == 0 && step == 1) continue;
                            int jy = cy - up;
                            if (!PathColumnClear(cx, cy, jy)) break;
                            if (!PathBodyFree(jx, jy)) continue;
                            if (!PathFloorAt(jx, jy)) continue;
                            // 横移发生在最高点（落点高度），只查那一层的身体两格。
                            // 查 cy..jy 整段会把"门框上方留了空、人可以跳过去再从屋顶横移"直接判死：
                            // 门框 226/227 是实心，2174 那面墙从地面到屋顶，从屋外只能靠"跳到屋顶高度再横移"绕过去（m17619）。
                            if (!PathHorizClear(cx, jx, jy - 1, jy)) continue;
                            TryEdge(jx, jy, 4f + step + up * 0.3f);
                            break;
                        }
                    }
                    // 落：水平 1~3、下降 1~14（超过 14 格按摔伤处理，不当通路）
                    for (int step = 1; step <= 4; step++)
                    {
                        int jx = cx + d * step;
                        for (int down = 1; down <= 14; down++)
                        {
                            int jy = cy + down;
                            if (!PathBodyFree(jx, jy)) break;
                            if (!PathFloorAt(jx, jy)) continue;
                            if (!PathHorizClear(cx, jx, cy, jy)) continue;
                            TryEdge(jx, jy, 2f + step * 0.5f + down * 0.2f);
                            break;
                        }
                    }
                }
            }
            if (cost[ti, tj] >= float.MaxValue) { why = "没有可行路径 | " + LastPathWhy + $" opened={openedCount}"; return null; }

            var path = new List<(int x, int y)>();
            var cur = (i: ti, j: tj);
            int safety = 0;
            while (!(cur.i == si && cur.j == sj) && safety++ < 5000)
            {
                path.Add((x0 + cur.i, y0 + cur.j));
                cur = prev[cur.i, cur.j];
                if (cur.i < 0) break;
            }
            path.Reverse();
            return path;
        }

        internal static void BeginPath(List<(int x, int y)> cells, ulong until, bool allowDig = true)
        {
            PathAllowDig = allowDig;
            PathCells = cells ?? new List<(int x, int y)>();
            PathIndex = 0; PathActive = PathCells.Count > 0;
            PathStop = PathActive ? "" : "空路径";
            PathDeadline = until; PathNextAct = 0;
            PathLastIndex = -1; PathIdxStuck = 0;
        }

        internal static void StopPath(string reason)
        {
            if (!PathActive) return;
            PathActive = false; PathStop = reason;
        }

        private static void TickPath()
        {
            if (!PathActive) return;
            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead) { StopPath("dead"); return; }
            if (Now > PathDeadline) { StopPath("timeout"); return; }
            if (p.lavaWet) { StopPath("lava"); return; }
            if (p.wet) { RequestJump(); return; }
            if (Now < PathNextAct) return;

            int px = (int)(p.Center.X / 16f), py = (int)(p.Center.Y / 16f);

            if (px == PathStillPx && py == PathStillPy) PathStillTicks++;
            else { PathStillTicks = 0; PathStillPx = px; PathStillPy = py; }

            // 死锁看门狗：实测在空腔竖井里 A* 会原地上下跳 60 秒、一格不动，
            // 而 timeout 要等满 60 秒才触发。
            // 用"路径下标不再前进"当判据，而不是坐标：人在原地上下抖 1 格时
            // 整数 py 会在 305/306 之间来回变，坐标判据永远不会触发（实测踩过）。
            if (PathIndex == PathLastIndex) PathIdxStuck++;
            else { PathIdxStuck = 0; PathLastIndex = PathIndex; }
            if (PathIdxStuck > 90) { StopPath("no-progress"); return; }

            while (PathIndex < PathCells.Count)
            {
                var c = PathCells[PathIndex];
                if (Math.Abs(c.x - px) <= 0 && Math.Abs(c.y - py) <= 1) { PathIndex++; continue; }
                break;
            }
            if (PathIndex >= PathCells.Count) { StopPath("arrived"); return; }

            var next = PathCells[PathIndex];
            int dx = next.x - px, dy = next.y - py;

            // 优先水平推进：路径格常常只比人低一格（站在地板上），那不是"要往下挖"
            if (dx != 0)
            {
                int sign = Math.Sign(dx);
                // 只在“真被挡住”（位置连续几 tick 没变）时才挖：实测无条件挥镐会在自建房里把地板和墙刨掉。
                bool blocked = !TileAir(px + sign, py) || !TileAir(px + sign, py + 1);
                if (TileAir(px + sign, py + 2) && TileAir(px + sign, py + 3)) PlaceWalkBlock(px + sign, py + 2);
                // 障碍处理顺序：先跳（1 格台阶跳过去就行）→ 还不行才动镐。
                // 事故背景：原来的顺序是“看到挡路就先挥镐”，结果在自建房里把地板、墙和门一起刨了。
                int rise = py - next.y;
                if (blocked)
                {
                    bool canHop = TileAir(px + sign, py + 2) && rise <= 5 && PathStillTicks < 5;
                    if (canHop) WalkJump(2.4f);
                    else if (PathStillTicks >= 5 && PathAllowDig)
                        for (int k = -2; k <= 1; k++) if (!TileAir(px + sign, py + k)) WalkMine(px + sign, py + k);
                }
                // 没被挡就只走路，不跳：用户指出“明明走就可以，为什么要边走边跳”（m16540）。
                // 需要上台阶时，原版会自动跨 1 格；真跨不上去（rise 大）才跳，且按满跳量一次跳到位。
                if (sign < 0) RequestLeft(); else if (sign > 0) RequestRight();
                // 不硬改 velocity：原版靠 control 标志自己算速度，才会触发台阶/斜坡/门的自动处理。
                // 事故背景：之前为了追横向速度直接写 velocity.X，结果走到自建房的关门前推不动（原版自动开门逻辑被绕过）。
                p.direction = sign;
            }
            else if (dy <= -1)
            {
                if (PathAllowDig && !TileAir(px, py - 2)) WalkMine(px, py - 2);
                else if (PathAllowDig && !TileAir(px, py - 3)) WalkMine(px, py - 3);
                else WalkJump(2.8f);
            }
            else if (dy >= 1)
            {
                // 往下挖：角色碰撞盒占 px-1 和 px 两格（桥报的 px 是中心点），
                // 只挖 px 会留一格实心在脚下，人一格都下不去——这正是之前竖井"跳 60 秒不动"的真因。
                if (PathAllowDig && !TileAir(px, py + 2)) WalkMine(px, py + 2);
                if (PathAllowDig && !TileAir(px - 1, py + 2)) WalkMine(px - 1, py + 2);
                if (PathAllowDig && TileAir(px, py + 2) && TileAir(px - 1, py + 2) && !TileAir(px, py + 3)) WalkMine(px, py + 3);
            }
            PathNextAct = Now + 3;
        }

        /// <summary>走路垫砖：用背包里的方块。找不到方块就不放（返回 false）。</summary>
        private static bool PlaceWalkBlock(int x, int y)
        {
            Player p = Main.LocalPlayer;
            if (p == null) return false;
            int slot = -1;
            for (int i = 0; i < p.inventory.Length; i++)
            {
                Item it = p.inventory[i];
                if (it != null && it.type > 0 && it.stack > 0 && it.createTile > 0 && it.placeStyle >= 0) { slot = i; break; }
            }
            if (slot < 0) return false;
            // 真实放置：原来这里 WorldGen.PlaceTile + 手动 stack--，等于绕过物品流改世界。
            // 现在只压准星按住使用，支撑判定和扣物品都由原版 ItemCheck 做。
            BeginItemUse(slot, x, y, 140);
            return true;
        }

        /* ── walkto：朝目标坐标自己走 ─────────────────────────────────────
         * 之前只有 move(方向,时长)，代理只能一味朝一个方向推，掉洞、卡墙、淹死全靠运气。
         * 这里做成逐帧的闭环：算方向、遇墙挖、遇坑跳、泡水就上浮、碰到岩浆或死亡就停。
         */
        internal static bool WalkActive;
        internal static int WalkX, WalkY, WalkTol = 1;
        // 中途被挡会自己重新找路（用户要求：走路不能一次算完就照做）
        internal static int WalkRepaths;
        internal static int WalkStuckHard;
        internal static int WalkRepathSame;
        internal static ulong WalkDoorUntil;
        /// <summary>前方地形扫描缓存（每 10 tick 重扫；索引 1..8 = 前方第 n 列第一块实心的 y，int.MinValue=无地面）。</summary>
        private static int[] WalkScan;
        private static ulong WalkScanAt;
        internal static int WalkRepathPx = -99999;
        internal static int WalkWpX, WalkWpY;
        internal static bool WalkHasWp;
        internal static string WalkStop = "";
        internal static ulong WalkDeadline;
        private static ulong WalkNextJump, WalkNextMine;
        private static int WalkLastPx, WalkStuck;
        internal static int WalkStuckTicks => WalkStuck;

        internal static void BeginWalk(int x, int y, int tol, ulong until)
        {
            WalkActive = true; WalkX = x; WalkY = y;
            WalkTol = Math.Max(0, tol); WalkDeadline = until;
            WalkStop = ""; WalkNextJump = 0; WalkNextMine = 0; WalkStuck = 0; WalkLastPx = -99999;
            WalkRepaths = 0; WalkStuckHard = 0; WalkRepathSame = 0; WalkRepathPx = -99999; WalkHasWp = false; WalkWpX = 0; WalkWpY = 0; WalkDetour = 0;
        }

        internal static void StopWalk(string reason)
        {
            if (!WalkActive) return;
            WalkActive = false; WalkStop = reason; WalkHasWp = false;
            // The next PostUpdateInput frame will emit no bridge movement triggers.
        }

        private static bool TileSolid(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return true;
            Tile t = Main.tile[x, y];
            return t.HasTile && Main.tileSolid[t.TileType];
        }

        private static void WalkJump(float tiles = 2.4f)
        {
            if (Now < WalkNextJump) return;
            Player p = Main.LocalPlayer;
            if (p == null) return;
            if (!(p.velocity.Y == 0f && p.oldVelocity.Y == 0f)) return;
            RequestJump();
            // 必须把 controlJump 持续到顶点：原版跳跃是"可变跳"，只给一帧 = 松手，
            // 角色只升 1~2 格就往下掉。实测 climb-slope 想跳 5 格墙，每次都只到 2.1 格（m17852）。
            // 按"满跳 6.1 格 ≈ 24 tick"线性折算按住时长。
            JumpUntil = Now + (ulong)Math.Max(4, (int)(tiles / 6.0f * 24.0f));
            WalkNextJump = Now + 6;
        }

        // 用户实测：我在自己盖的 NPC 房里用 walkto 脱困，寻路“看到挡路就挖”，把墙挖穿了（m15575）。
        // 玩家造物一律不进挖掘清单——宁可绕路或报 stuck，也不能拆自己盖的房子。
        private static readonly HashSet<int> BuildTiles = new HashSet<int> { 4, 10, 11, 15, 16, 17, 18, 19, 21, 30 };
        private static bool IsBuildTile(int type) => BuildTiles.Contains(type);
        private static void WalkMine(int x, int y)
        {
            if (Now < WalkNextMine) return;
            Player p = Main.LocalPlayer;
            if (p == null) return;
            int slot = -1;
            for (int i = 0; i < p.inventory.Length; i++)
            {
                Item it = p.inventory[i];
                if (it != null && it.type > 0 && it.pick > 0) { slot = i; break; }
            }
            if (slot < 0) return;
            Tile t = Main.tile[x, y];
            if (!t.HasTile) return;
            if (IsBuildTile(t.TileType)) return;   // 自家建材/家具/门/平台/火把/箱子：不挖
            // 砍树和锤墙也走原版使用路径（原来这里是 WorldGen.KillTile 直接删格子）。
            BeginItemUse(slot, x, y, 140);
            p.direction = x * 16f >= p.Center.X ? 1 : -1;
            WalkNextMine = Now + 7;
        }

        /// <summary>
        /// 重新找路，取近距离的一个点当临时路点。
        /// 取太远（第 8 步）会变成“原地往上跳 8 格”，跳不上去白算。
        /// 预算按进展算：从上次重算位置又往前挪 2 格以上，说明在推进，次数重新给满。
        /// </summary>
        /// <summary>
        /// 区域预判：读前方 w 列、py-3..py+8 行的地形，返回每列第一块实心方块的 y。
        /// 一次读一片，比逐格摸墙多了"隔壁几列能不能站"的信息——门口、台阶、屋顶边缘都靠它区分。
        /// </summary>
        // 平台（type 19）在 Main.tileSolid 里是 false、只在 tileSolidTop 里为 true。
        // 之前所有“有没有地面/是不是悬崖”都只看 tileSolid，于是站在木平台上会被判成悬空、
        // 前方整条平台路被判成悬崖（m16944/m17008 两次 cliff 都是这个原因）。
        private static bool TileStand(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return true;
            Tile t = Main.tile[x, y];
            if (!t.HasTile) return false;
            int ty = t.TileType;
            return Main.tileSolid[ty] || Main.tileSolidTop[ty] || TileID.Sets.Platforms[ty];
        }

        // ── 寻路的四个判定（与 src/path-model.js 一一对应，改一处必须同步改另一处） ──
        /// <summary>身体两格都能站人（门算能过）</summary>
        private static bool PathBodyFree(int x, int y)
        {
            bool ok(int yy)
            {
                if (TileAir(x, yy)) return true;
                Tile t = Main.tile[x, yy];
                return t.HasTile && (t.TileType == TileID.ClosedDoor || t.TileType == TileID.OpenDoor);
            }
            return ok(y) && ok(y - 1);
        }

        /// <summary>脚下有地板（平台也算）。
        /// 门必须单独放行：门占 3 格高且自身不算可站立面，站在门格里时
        /// 脚下 1~2 格都还是门，真正的地板在门下方 —— 只查 y+1 会让所有门口
        /// 变成"无地可站"，于是 A* 连"从房里原路走出门"都算不出来（m17619 离线复现 NO PATH）。</summary>
        private static bool PathFloorAt(int x, int y)
        {
            for (int k = 1; k <= 4; k++)
            {
                if (TileStand(x, y + k)) return true;
                Tile t = Main.tile[x, y + k];
                bool isDoor = t.HasTile && (t.TileType == TileID.ClosedDoor || t.TileType == TileID.OpenDoor);
                // 空格要继续往下看：人在空中/下落、或站在比地板高 1 格的位置时，
                // 脚下第 1 格就是空的（实测 2105,233 空、234 才是草地），
                // 旧写法在这里直接 return false，于是起点被判“无地可站” →
                // 整个 A* 一开始就 no-path（"bodyS=True floorS=False" 就是这个）。
                if (!isDoor && t.HasTile) return false;
            }
            return false;
        }

        /// <summary>
        /// 把跟随/走路目标落到同一列最近的可站立格。
        /// 玩家在跳跃、飞行或刚落地的那几帧，Center.Y 对应的是空中格；
        /// 直接把这个格交给 A* 会得到“目标没有地面”的假 no-path。
        /// 这里只读碰撞箱和地形，不改变任何游戏状态。
        /// </summary>
        private static int NormalizePathTargetY(int x, int y, int maxDown = 4)
        {
            if (x < 1 || x >= Main.maxTilesX - 1) return y;
            int start = Math.Max(1, y);
            int end = Math.Min(Main.maxTilesY - 2, start + Math.Max(0, maxDown));
            for (int candidate = start; candidate <= end; candidate++)
                if (PathBodyFree(x, candidate) && PathFloorAt(x, candidate)) return candidate;
            return y;
        }

        /// <summary>
        /// 跟随目标的保持距离点可能落在平台边外（尤其是窄平台或屋顶）。
        /// 在目标附近寻找最近的可站立列，避免把 A* 终点设在空气里。
        /// </summary>
        private static (int x, int y) NormalizeFollowTarget(int x, int y, int radius = 8, int maxDown = 8, int maxUp = 24)
        {
            // 只在"y 往下 8 格"里找站立点，是"队友一换层就不跟"的根因：
            // 队友在头顶两层楼上时 rawGoalY 落在空中，向下找不到地板，
            // 函数原样返回空中坐标 → A* 找不到可站立终点 → 一步都不走。
            // 现在上下都搜（向上给到 24 格），并按"与队友实际高度的差"打分，
            // 这样同层优先、上下层也能接上。
            int bestX = x, bestY = y;
            float bestScore = float.MaxValue;
            for (int dx = -radius; dx <= radius; dx++)
            {
                int candidateX = x + dx;
                if (candidateX < 1 || candidateX >= Main.maxTilesX - 1) continue;
                int startY = Math.Max(1, y - Math.Max(0, maxUp));
                int endY = Math.Min(Main.maxTilesY - 2, y + Math.Max(0, maxDown));
                for (int candidateY = startY; candidateY <= endY; candidateY++)
                {
                    if (!PathBodyFree(candidateX, candidateY) || !PathFloorAt(candidateX, candidateY)) continue;
                    float score = Math.Abs(dx) * 1.5f + Math.Abs(candidateY - y);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestX = candidateX;
                        bestY = candidateY;
                    }
                }
            }
            return (bestX, bestY);
        }

        /// <summary>从 y 往上到 ny 的竖直通道是否清空</summary>
        private static bool PathColumnClear(int x, int y, int ny)
        {
            for (int yy = y - 1; yy >= ny; yy--) if (IsBlockingSolid(x, yy)) return false;
            return true;
        }

        /// <summary>横跨时中间格必须过得去（否则会算出"跳过一堵墙"的假路）</summary>
        private static bool PathHorizClear(int x0, int x1, int yTop, int yBot)
        {
            int dir = Math.Sign(x1 - x0);
            for (int x = x0 + dir; x != x1; x += dir)
                for (int y = yTop - 1; y <= yBot; y++)
                    if (IsBlockingSolid(x, y)) return false;
            return true;
        }

        private static int[] ScanGround(int px, int py, int sign, int w)
        {
            var g = new int[w + 1];
            for (int i = 1; i <= w; i++)
            {
                g[i] = int.MinValue;
                // 向下必须扫够深：跳跃中 py 会临时高出地面 6 格，只看 +8 会把脚下地板漏掉，
                // 于是"人在空中"被误判成悬崖（m16944 那次 cliff 就是这么来的）。
                for (int k = -3; k <= 16; k++)
                {
                    if (TileStand(px + sign * i, py + k)) { g[i] = py + k; break; }
                }
            }
            return g;
        }

        /// <summary>
        /// 算不出路时找一个“能穿过去的接口”：附近的门，或者障碍上方能站人的缺口。
        /// 这是硬规则 3 的落地——绕行/找门必须是“撞上了才实时找”，不能出发时预先写死。
        /// </summary>
        private static bool FindGate(int px, int py, int tx, out int gx, out int gy)
        {
            gx = px; gy = py;
            int best = int.MaxValue;
            // 双向搜：卡在自建房右墙时，唯一的出口（门）在身后，只朝目标方向搜永远找不到。
            for (int d = 1; d <= 26; d++)
            {
                foreach (int sgn in new[] { 1, -1 })
                {
                    int x = px + sgn * d;
                    if (x < 1 || x >= Main.maxTilesX - 1) continue;
                    for (int y = py - 6; y <= py + 8; y++)
                    {
                        if (y < 1 || y >= Main.maxTilesY - 1) continue;
                        Tile t = Main.tile[x, y];
                        if (!t.HasTile) continue;
                        if (t.TileType != TileID.ClosedDoor && t.TileType != TileID.OpenDoor) continue;
                        if (!FloorWithin(x, y, 4)) continue;      // 门前得站得住
                        int cost = Math.Abs(x - px);
                        if (cost < best) { best = cost; gx = x; gy = y; }
                    }
                }
            }
            return best != int.MaxValue;
        }

        private static bool TryRepath(int px, int py)
        {
            // 只有“位置真的前进了”才清零重算计数；否则连续 3 次重算不出进展就认输。
            // （旧代码里 WalkRepathSame 那段是错的：第一行刚把 WalkRepathPx 设成 px，
            //   第二行必然相等，计到第 7 次就永久返回 false，表现为一启动就 stuck。）
            if (Math.Abs(px - WalkRepathPx) >= 2) { WalkRepaths = 0; WalkRepathPx = px; }
            if (WalkRepaths >= 3) return false;
            string why;
            // 滚动局部规划：ComputePath 的包围盒本身只覆盖 ±10/±14/±18 格（见其内部实现），
            // 已经天然是"近距离规划"。曾在外面再套一层 16 格截断，结果把目标点截到
            // 平台中间、路点来回振荡，反而从能走到 2173 退化成卡在 2108（m17052 实测）。
            // 先用窄框（±10）：它快、也不会挑"绕远但便宜"的怪路。
            // 窄框算不出时再放一次宽框（±28）—— 唯一出口（比如自家房子的门）常在 10 格之外，
            // 窄框会把"必须往回走才出得去"直接判成无路（m17400 卡在房 A 右墙前就是这个）。
            var path = ComputePath(px, py, WalkX, WalkY, out why)
                       ?? ComputePath(px, py, WalkX, WalkY, out why, 28);
            if (path == null || path.Count == 0)
            {
                // FindGate 两版都试过（单向找/双向找门），都把结果变差（2133 / 2160 就 stuck）。
                // 关掉，等有"门的确切可达性判定"再开——不能再靠猜。
                WalkRepaths++;          // 只有真的算不出路才记一次失败
                return false;
            }
            // 只跳过"和脚下同一格"的路点，绝不跳过"往回一格"的路点。
            // 原实现取"距离 >= 3 格的第一个路点"，会把起跳点/站位点整段吞掉：
            // 自建房屋顶那段 A* 给的是 (2166,230) → (2167,224)，人站在 (2167,229)，
            // 起跳点 (2166,230) 只有 2 格距离被判"太近"跳过，于是直接去执行头顶 5 格的路点，
            // 而正上方是实心屋顶 → 原地卡到超时（m17790 实测）。
            // ComputePath 的返回值不含起点（回溯循环的条件就是"走到起点就停"），
            // 所以 path[0] 就是第一步。原来从 i=1 起找，等于把第一步永久丢弃：
            // 起点 (2167,229) 的第一格是"退回 (2166,230) 的起跳点"，被丢掉后
            // 人只能去执行头顶 5 格的屋顶路点，而正上方是实心屋顶 → 卡到超时（m17790/m17809 两次实测）。
            int k = 0;
            for (int i = 0; i < path.Count && i <= 12; i++)
            {
                if (path[i].x != px || path[i].y != py) { k = i; break; }
            }
            if (k < 0 || k >= path.Count) return false;
            WalkWpX = path[k].x; WalkWpY = path[k].y; WalkHasWp = true;
            // 成功必须清零：以前这里也 ++，结果每成功 3 次就到阈值，之后 TryRepath 永久返回 false
            // （表现为走到半路突然"detour-build"直接停，位置一动不动）。
            WalkRepaths = 0; WalkStuck = 0;
            return true;
        }

        // ═══════════════ 四层行为系统：Sense → KB 检索 → Evaluate → Act ═══════════════
        // 规范：行为规则只允许定义在知识库（docs/kb/actor-terrain-rules.md），代码只做调度。
        // 这里把 KB 的条目写成数据表（与文档一一对应），改规则先改文档、再同步此表。
        // 早期版本是十几个 if 顺序 return，改一条连带坏三条；现在同一 tick 只允许
        // 最高优先级的那一条生效，其余即使条件成立也不执行。

        internal struct ActorSense
        {
            public int px, py, dir;
            public bool dead, lava, drowning;
            public bool arrived;
            public bool blockingSelf;     // 身体两格都是「阻挡方块」（门/平台不算）
            public bool stalled;
            public bool cliffAhead;
            public bool doorAhead;
            public bool buildAhead;
            public bool gapAhead;
            public bool onGround;
            public int wallH;
        }

        internal struct ActorAct
        {
            public string rule; public int priority; public string kind; public float jumpTiles;
        }

        internal static ActorAct ActorNew, ActorOld;

        /// <summary>isSolidTile：瓦片底层固态标记，只描述数据本身。</summary>
        private static bool IsSolidTile(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return true;
            Tile t = Main.tile[x, y];
            return t.HasTile && Main.tileSolid[t.TileType];
        }

        /// <summary>
        /// isBlockingSolid：业务判定——是否「不可穿过」的实心阻挡。
        /// 门和平台虽然可能 tileSolid 为 true，但人能穿过去，**不算阻挡**。
        /// 卡住脱困必须用这个，用 IsSolidTile 会让人站在平台上原地乱蹦。
        /// </summary>
        private static bool IsBlockingSolid(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return true;
            Tile t = Main.tile[x, y];
            if (!t.HasTile) return false;
            int ty = t.TileType;
            if (ty == TileID.ClosedDoor || ty == TileID.OpenDoor) return false;
            if (TileID.Sets.Platforms[ty]) return false;
            return Main.tileSolid[ty];
        }

        /// <summary>感知层：只读，只输出观测值，不做任何决策。每个检测器互相独立。</summary>
        private static ActorSense SenseWorld(bool ignoreWp = false)
        {
            var s = new ActorSense();
            Player p = Main.LocalPlayer;
            if (p == null) return s;
            s.px = (int)(p.Center.X / 16f);
            s.py = (int)(p.Center.Y / 16f);
            s.dead = p.dead || !p.active;
            s.lava = p.lavaWet;
            s.drowning = p.breath == 0 && p.wet;
            s.arrived = Math.Abs(s.px - WalkX) <= WalkTol && Math.Abs(s.py - WalkY) <= WalkTol;
            // 方向必须按"当前路点"算，不能按最终目标：绕路时路点常在人身后（往回走出房门），
            // 用最终目标会算出朝右，于是人顶着右墙一遍遍满跳（m17462 实测 control.right 恒 true、路点却在左边）。
            // 反应式执行器（WalkReactive）不追路点：方向一律按最终目标算，
            // 否则会继承上一套路点留下的方向，表现成"朝旧方向顶墙"。
            s.dir = Math.Sign((ignoreWp || !WalkHasWp ? WalkX : WalkWpX) - s.px);
            if (s.dir == 0) s.dir = 1;
            s.blockingSelf = IsBlockingSolid(s.px, s.py) && IsBlockingSolid(s.px, s.py - 1);
            s.onGround = p.velocity.Y == 0f;
            s.stalled = Math.Abs(s.px - WalkLastPx) <= 0 && WalkStuck >= 6;
            // 停顿持续计数：位置一变就归零，所以这个数只在"卡住"期间增长。
            if (s.stalled) StallJumps++; else StallJumps = 0;
            int sign = s.dir;
            // 前方扫一片：门 / 自建 / 坑 / 墙高 / 悬崖
            var scan = ScanGround(s.px, s.py, sign, 8);
            int depth = scan[1] == int.MinValue ? 99 : Math.Max(0, scan[1] - s.py);
            // 悬崖 vs 坑：只看第 1 列会把「一格宽的坑」误判成悬崖（离线用例 test/sense.test.js
            // 抓到的第一个真缺陷）。改成前 3 列全都够不着地才算悬崖；第 1 列空但后面有地是坑。
            bool nearBlind = true;
            for (int i = 1; i <= 3; i++)
            {
                if (scan[i] != int.MinValue && scan[i] - s.py <= NavRules.T.CliffMaxDrop) { nearBlind = false; break; }
            }
            s.cliffAhead = nearBlind;
            for (int k = 0; k <= 2; k++)
            {
                Tile t = Main.tile[s.px + sign, s.py + k];
                if (t.HasTile && (t.TileType == TileID.ClosedDoor || t.TileType == TileID.OpenDoor)) { s.doorAhead = true; break; }
            }
            int h = 0;
            for (int k = 0; k < 12; k++)
            {
                if (IsBlockingSolid(s.px + sign, s.py - k)) h = k + 1; else break;
            }
            s.wallH = h;
            // 只有「真的挡路」才算自建障碍：1 格高原版会自动跨过去，此时若还报 buildAhead，
            // 会在地板上方 1 格的屋梁/家具边上反复 detour（m17253 房内 2166 停住就是这个）。
            if (h >= 2)
            {
                Tile bt = Main.tile[s.px + sign, s.py - (h - 1)];
                s.buildAhead = bt.HasTile && IsBuildTile(bt.TileType);
            }
            // 坑必须是「眼前这一格脚下是空的」：只比 depth 会把平台上的正常行走也算成坑
            // （m17158 对比里 2117~2152 全程被误判 jump-gap，而旧版一直正常 go-right）。
            // 必须「站在地上」才谈得上踩空：跳跃途中 py 本来就高于地面，
            // 那时 TileStand(px+dir, py) 必然是空的，只按那一格判会把每一次跳都当成坑。
            // 对岸取第 2~4 列里第一个落得下脚的：只拿 depth（第 1 列）判深会把坑判成悬崖。
            int landAfter = int.MinValue;
            for (int i = 2; i <= 4; i++) { if (scan[i] != int.MinValue) { landAfter = scan[i]; break; } }
            int landDelta = landAfter == int.MinValue ? 99 : Math.Abs(landAfter - s.py);
            s.gapAhead = s.onGround && !s.cliffAhead && !s.doorAhead && h == 0
                         && !TileStand(s.px + sign, s.py) && !TileStand(s.px + sign, s.py + 1)
                         && landDelta >= 2 && landDelta <= 6;
            return s;
        }

        /// <summary>评估层：严格按优先级，只返回一条。顺序来自 KB 的优先级表。</summary>
        private static ActorAct Evaluate(ActorSense s)
        {
            ActorAct a = new ActorAct { rule = "move-waypoint", priority = NavRules.P.MoveWaypoint, kind = "go", jumpTiles = 0 };
            if (s.dead)      return new ActorAct { rule = "escape-danger", priority = NavRules.P.EscapeDanger, kind = "stop" };
            if (s.lava)      return new ActorAct { rule = "escape-danger", priority = NavRules.P.EscapeDanger, kind = "escape" };
            if (s.drowning)  return new ActorAct { rule = "escape-danger", priority = NavRules.P.EscapeDanger, kind = "upfloat" };
            if (s.arrived)   return new ActorAct { rule = "arrive", priority = NavRules.P.Arrive, kind = "stop" };
            if (s.blockingSelf) return new ActorAct { rule = "unstick-blocked", priority = NavRules.P.UnstickBlocked, kind = "jump", jumpTiles = NavRules.T.JumpTilesMax };
            // 停顿自救：先按满跳跳一次（高差在满跳范围内的路点只有靠跳才上得去；
            // 直接重规划会算出同一个路点，位置照旧不动，于是无限重规划、人永远站着 —— m17558 实测 70 帧零控制）。
            if (s.stalled)
            {
                // 8 帧 ≈ 260ms 长按，正好是满跳；太短就只到两三格，根本够不着屋顶。
                if (StallJumps <= NavRules.T.StallJumpsMax) return new ActorAct { rule = "stall-jump", priority = NavRules.P.StallJump, kind = "jump", jumpTiles = NavRules.T.StallJumpTiles };
                return new ActorAct { rule = "stall-recover", priority = NavRules.P.StallJump, kind = "replan" };
            }
            if (s.cliffAhead && !s.doorAhead) return new ActorAct { rule = "avoid-cliff", priority = NavRules.P.AvoidCliff, kind = "replan" };
            // 门：必须显式开门。原版"走过去自动开"依赖真实键盘输入，手写 control 时不会触发，
            // 所以这里出一个独立动作，由 ActActor 发一次原版右键交互输入。
            if (s.doorAhead) return new ActorAct { rule = "pass-door", priority = NavRules.P.PassDoor, kind = "open-door" };
            // 只在"没有路点可走"时才因建材绕行。A* 已经算出一条绕开房子的路时，
            // 贴身处是墙是正常的（正走在门口），不能再触发 replan —— 否则每帧重算、
            // 路点被反复改写成"往回退 4 格"，人就永远钉在墙前（m17430 实测）。
            if (s.buildAhead && !WalkHasWp) return new ActorAct { rule = "detour-build", priority = NavRules.P.DetourBuild, kind = "replan" };
            if (s.wallH >= 1 && s.wallH <= NavRules.T.WallMaxJump) return new ActorAct { rule = "climb-slope", priority = NavRules.P.ClimbSlope, kind = "jump", jumpTiles = (float)Math.Min(NavRules.T.JumpTilesMax, s.wallH + NavRules.T.ClimbExtraTiles) };
            if (s.gapAhead)  return new ActorAct { rule = "jump-gap", priority = NavRules.P.JumpGap, kind = "jump", jumpTiles = NavRules.T.GapJumpTiles };
            return a;
        }

        private static int WalkDetour;   // detour-build 自己的重试预算（与 TryRepath 的 WalkRepaths 分开）
        private static int StallJumps;   // 同一位置连续“停顿自救”的次数：前两跳满跳，第三跳才重规划

        internal static bool ActorUseNew;   // false=旧逻辑驱动，true=四层驱动（重构切换开关）
        internal static bool WalkReactive;  // true=反应式执行器（不追路点）；false=旧 walkto 节点追踪
        internal static ActorAct ActReactive;

        /// <summary>动作层：只把评估结果翻译成控制，不再做任何环境判断。</summary>
        private static void ActActor(ActorAct a, Player p, int sign)
        {
            if (a.kind == "stop") { StopWalk(a.rule); return; }
            // 开门后必须继续走：只在开门时 return 会让人站着不动，下一帧又判"前方是门"，
            // 于是每 20 tick 开一次门、位置永远不动（m17852 实测卡在 2160.4）。
            if (a.kind == "open-door")
            {
                int dpx = (int)(p.Center.X / 16f), dpy = (int)(p.Center.Y / 16f);
                for (int k = 0; k <= 2; k++)
                {
                    Tile t = Main.tile[dpx + sign, dpy + k];
                    if (t.HasTile && t.TileType == TileID.ClosedDoor && Now > WalkDoorUntil)
                    {
                        BeginTileInteraction(dpx + sign, dpy + k);
                        WalkDoorUntil = Now + 20;
                        break;
                    }
                }
                // 不 return：继续走下面的横移逻辑，穿过这扇已经推开的门。
            }
            if (a.kind == "escape") { RequestJump(); return; }
            if (a.kind == "upfloat") { RequestUp(); return; }
            if (a.kind == "replan")
            {
                int rpx = (int)(p.Center.X / 16f), rpy = (int)(p.Center.Y / 16f);
                if (!TryRepath(rpx, rpy))
                {
                    // A* 的包围盒只覆盖当前位置到目标之间（±10），当绕行需要“往回走到门/缺口”
                    // 时它看不见那条路，于是直接判无路。这里先朝目标反方向挪一小段再重算：
                    // 退一步，门就落进包围盒里了。退 2 次还不成，就按预算停。
                    // 独立计数：不能和 TryRepath 的 WalkRepaths 共用，否则一次卡住 3 帧就用光预算。
                    if (++WalkDetour < 8) { WalkWpX = rpx - sign * 4; WalkWpY = rpy; WalkHasWp = true; }
                    else StopWalk(a.rule);
                }
                return;
            }
            if (sign > 0) RequestRight(); else if (sign < 0) RequestLeft();
            // 横移只发方向键意图；PostUpdateInput 会把它翻译成原生 Triggers，
            // 速度、碰撞和台阶完全交给原版物理。
            if (a.kind == "jump") WalkJump(a.jumpTiles > 0 ? a.jumpTiles : 2.4f);
        }

        internal static bool ActorDualRun = true;

        /// <summary>把旧逻辑实际写下去的 control 翻译成行为名，用于同帧对比。</summary>
        private static ActorAct ObserveOldAction(Player p)
        {
            var a = new ActorAct { rule = "old", priority = 0, kind = "none" };
            if (WalkActive == false) { a.kind = "stop"; return a; }
            if (p.controlJump) { a.kind = "jump"; return a; }
            if (p.controlLeft) { a.kind = "go-left"; return a; }
            if (p.controlRight) { a.kind = "go-right"; return a; }
            // 没写方向键，但上一帧可能刚停：用 stopReason 区分停/重算
            a.kind = string.IsNullOrEmpty(WalkStop) ? "idle" : "stop";   // WalkStop 由 StopWalk 写入
            return a;
        }

        /// <summary>反应式执行器：每帧 Sense → KB检索(Evaluate) → Act，不追路点。
        /// A* 只在启动时判一次可达性、提供宏观方向，不再把节点序列交给执行层。
        /// 用户规格（m17835）：旧 walkto 的节点追踪保留隔离、并行开发、同场景对比。</summary>
        private static void TickWalkReactive()
        {
            NavRules.EnsureFresh();   // 决策参数热更：改 nav-rules.json 存盘即生效，不用重启
            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead) { StopWalk(p == null || !p.active ? "no-player" : "dead"); return; }
            if (Now > WalkDeadline) { StopWalk("timeout"); return; }
            ActorSense s = SenseWorld(ignoreWp: true);
            if (Math.Abs(s.px - WalkX) <= WalkTol && Math.Abs(s.py - WalkY) <= WalkTol) { StopWalk("arrived"); return; }
            ActReactive = Evaluate(s);
            if (ActReactive.rule == null) { StopWalk("no-rule"); return; }
            ActActor(ActReactive, p, s.dir);
        }

        private static void TickWalk()
        {
            // 两套执行器并存：reactive=每帧临场决策（新），否则=追 A* 路点（旧，隔离保留）。
            if (WalkReactive) { TickWalkReactive(); return; }
            if (!WalkActive) return;
            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead) { StopWalk("dead"); return; }
            if (Now > WalkDeadline) { StopWalk("timeout"); return; }
            if (p.lavaWet) { StopWalk("lava"); return; }
            if (p.wet) { RequestJump(); return; }

            int px = (int)(p.Center.X / 16f), py = (int)(p.Center.Y / 16f);
            if (Math.Abs(WalkX - px) <= WalkTol && Math.Abs(WalkY - py) <= WalkTol) { StopWalk("arrived"); return; }
            // 有路点时朝路点走；走到就丢掉，回到朝总目标走
            // 用浮点中心比较、阈值 0.6 格：以前用整数格 + "<=1" 判到达，人在 2170.0 就把
            // 2169 的路点当成已到达并丢掉，下一个路点还是 2169 → 原地死循环（m17490 实测）。
            float fpx = p.Center.X / 16f, fpy = p.Center.Y / 16f;
            // 横向阈值必须紧：0.8 格时人在 2167.1 就把 2166 的路点判成到达，于是"从平台起跳"
            // 那一步被吞掉，下一个路点在头顶 5 格、正下方是实心屋顶 → 只能原地卡到超时（m17686 实测）。
            // 纵向可以松（下落/上升本来就有惯性），横向必须先把人摆到起跳点那一格上。
            if (WalkHasWp && Math.Abs(WalkWpX + 0.5f - fpx) <= 0.42f && Math.Abs(WalkWpY + 0.5f - fpy) <= 1.2f)
            {
                // 到点必须刹车：路点常常是"起跳点"，带着 2.9 格/秒的惯性冲过去 3 格，
                // 下一个路点就变成头顶正上方、而正上方是实心屋顶 —— 规划好的起跳点白算了（m17699 实测冲到 2167.9 后卡死）。
                // Release the direction at the waypoint and let vanilla friction settle the
                // player; the bridge never edits velocity directly.
                WalkHasWp = false;
            }
            // 没有路点就立刻重算，绝不"直接朝最终目标顶墙"：
            // 贪心回退不走路点：路点由 A* 生成，贪心模式下两者方向会打架。
            if (PathGreedy) WalkHasWp = false;
            // 以前会在路点用完后拿 WalkX 当方向，于是人在自建房右墙前一遍遍朝右满跳（m17436 实测 30 秒原地跳）。
            if (!WalkHasWp)
            {
                if (!TryRepath(px, py)) { StopWalk("no-path"); return; }
                if (!WalkHasWp) { StopWalk("no-path"); return; }
            }
            int gx = WalkHasWp ? WalkWpX : WalkX;
            int gy = WalkHasWp ? WalkWpY : WalkY;
            int dx = gx - px, dy = gy - py;
            // 向下挖时容易一掉就冲过头（目标 y=347 却掉到 y=406）：超过目标就停
            if (py > WalkY + 2) { StopWalk("overshoot"); return; }

            int sign = Math.Sign(dx);
            // 垂直路点不能直接 return：高处平台的第一步经常与当前角色同列，
            // dx=0 但 dy<0，这时必须发一次真实跳跃才能进入上方平台。
            // 旧代码在这里提前 return，后面的 WalkJump 永远到不了，表现为
            // “水平面能跟，玩家一上高处 Bot 就停”。
            if (sign == 0)
            {
                if (dy < 0) WalkJump(Math.Min(5.9f, -dy + 1.2f));
                return;
            }

            // 卡进实心格（跳跃落进墙里）：原地怎么走都出不来，先往上跳并清掉身内那一格
            // 必须“身体两格都实心”才算嵌进方块。原来只看 py 一格，而人站地上时 py 恰好就是
            // 脚踩的那块地面 → 判定成立 → 每帧满跳 → 表现为一路跳着走（m16900/m16944 两次）。
            if (TileSolid(px, py) && TileSolid(px, py - 1))
            {
                WalkJump(6.0f);
                if (++WalkStuckHard > 24) { StopWalk("embedded"); }
                return;
            }
            WalkStuckHard = 0;

            // 卡住自救：位置连续多帧几乎不动，就把前方 3 行挖开再跳
            if (Math.Abs(px - WalkLastPx) <= 0) WalkStuck++;
            else { WalkStuck = 0; WalkLastPx = px; }
            if (WalkStuck > 6)
            {
                // 注意：重算之后不能 return——否则每 6 帧重算一次、永远执行不到下面的移动代码，
                // 表现就是“原地不动但没停”（m16540 那次卡在门格就是这么来的）。
                if (TryRepath(px, py)) { WalkStuck = 0; }
                if (WalkRepaths >= 3) { StopWalk("stuck"); return; }
                WalkJump(5.5f);   // 只跳，不挖：挖是破坏性动作，不能塞在"自救"里自动发生
                WalkStuck = 0;
            }

            // 悬崖保护：前方 4 格内没有地面就不要往前冲（以前会直接掉几十格）
            int floorDepth = 0;
            bool floorFound = false;
            // 原来只看 4 格，2~4 格的小坑也被当悬崖停住；现在看到 8 格，≤6 格走下去（摔不死）
            // 区域预判（用户规格：先扫一遍再决定，走的时候逐格问，到要判断的点再扫）。
            // 每 10 tick 把前方 8 列的地面高度整体扫一次，缓存起来逐格使用；
            // 单看一列会在门口/台阶/屋顶边缘误判，扫一片才能分出"能走 / 要跳 / 是坑 / 是门"。
            if (Now - WalkScanAt >= 10 || WalkScan == null)
            {
                WalkScan = ScanGround(px, py, sign, 8);
                WalkScanAt = Now;
            }
            floorFound = WalkScan[1] != int.MinValue;
            floorDepth = floorFound ? Math.Max(0, WalkScan[1] - py) : 99;
            // 前方是门/房内：地板在门后面，从外侧探不到，“无地面”判据在这里会误报悬崖。
            bool doorAhead = false;
            for (int k = 0; k <= 2; k++)
            {
                Tile da = Main.tile[px + sign, py + k];
                if (da.HasTile && (da.TileType == TileID.ClosedDoor || da.TileType == TileID.OpenDoor)) { doorAhead = true; break; }
            }
            // 落差 7~14 格不是悬崖，是该跳下去：真人走断头平台/屋顶边缘就是直接下跳，掉 10 格
            // 只掉十来点血，而判成 cliff 会让整条路永远走不通（m17008 就卡在平台尽头）。
            // 只有"前方 16 格都探不到地面"或落差超过 14 格才真的停。
            if ((!floorFound || floorDepth > 14) && !doorAhead)
            {
                if (TryRepath(px, py)) return;
                StopWalk("cliff");
                return;
            }
            // 用户原话：「明明走就可以，为什么要边走边跳？」——1~2 格落差直接走，3 格以上才起跳
            bool gapAhead = floorDepth >= 3;

            bool bodyBlocked = TileSolid(px + sign, py);

            // 用户原话：「先判断再考虑大跳还是小跳」「为什么会莫名其妙挥镐子」。
            // 所以：量出前方障碍多高、缺口多宽，再决定跳多高；只有高墙且是天然地形才动镐。
            int wallH = 0;
            for (int k = 0; k < 8; k++)
            {
                if (TileSolid(px + sign, py - k)) wallH = k + 1; else break;
            }
            int gapW = 0;
            if (gapAhead)
            {
                for (int k = 1; k <= 5; k++)
                {
                    bool floor = false;
                    for (int d = 1; d <= 3; d++) if (TileSolid(px + sign * k, py + d)) { floor = true; break; }
                    if (floor) break;
                    gapW++;
                }
            }

            // 前方是关着的门：用原版右键交互把它打开（人走过去门会自己开，但这里靠推速度，
            // 原版那套自动开门不会触发，所以显式按一次“使用方块”）。
            if (bodyBlocked)
            {
                Tile dt = Main.tile[px + sign, py];
                if (dt.HasTile && dt.TileType == TileID.ClosedDoor && Now > WalkDoorUntil)
                {
                    // 开门不是“挥镐”：挥镐只会把门敲坏。原版走这套是 Player.TileInteract，
                    // 由真人右键触发；桥只压准星并按住右键，让原版自己处理门。
                    BeginTileInteraction(px + sign, py);
                    WalkDoorUntil = Now + 25;
                    PathNextAct = Now + 6;
                    return;
                }
            }

            // 自己造的建材（木块/平台/门/工作台…）：一律不跳、不挖，整条路绕开。
            // 事故背景：walkto 穿自建 NPC 房时，右墙被当成“矮墙”去跳，跳不过就卡在墙根；
            // 墙是玩家自己造的，正确做法是从门进出或绕路，绝不是翻自己的房。
            if (!bodyBlocked && !gapAhead)
            {
                // 平地 / 小落差：什么都不做，直接走
            }
            else if (gapAhead)
            {
                // 缺口：跳多高按落差给，越深跳越高（上限约满跳）
                WalkJump(Math.Min(5.9f, floorDepth + 0.8f));
            }
            else if (wallH <= 1)
            {
                // 就一格台阶：原版自己会跨，什么都不用做
            }
            else if (wallH <= 5)
            {
                // 矮墙：跳多高按墙高给，不用定值
                WalkJump(Math.Min(5.9f, wallH + 1.5f));
            }
            else
            {
                Tile bt = Main.tile[px + sign, py];
                if (bt.HasTile && !IsBuildTile(bt.TileType))
                {
                    // 事故：这里原来"天然地形就自动挖"，结果一靠近房子就把周围土石刨了。
                    // 挖是破坏性动作，必须由上层显式决定；walkto 只负责走，走不过去就停。
                    StopWalk("wall-ahead");
                }
                else
                {
                    // 自己造的墙：不挖。但"不挖"不等于"过不去" —— 先按满跳试跨（墙另一侧是空地就过去了），
                    // 跨不过去才重新规划绕路，规划不出才停。事故：以前直接 TryRepath，A* 就地在原地打转。
                    // A* 算不出（要跳上屋顶/门在反方向）就退回贪心：朝目标走，遇障按"走→跳→停"处理。
                    if (!TryRepath(px, py))
                    {
                        if (WalkRepaths >= 3) { StopWalk("blocked-build"); return; }
                        PathGreedy = true;
                    }
                    return;
                }
            }

            if (sign < 0) RequestLeft(); else if (sign > 0) RequestRight();
            p.direction = sign;
            // 横移只发方向键意图；门另走主动右键交互，速度与碰撞由原版处理。
            // 事故：这里原来每帧都调 WalkJump，"目标比当前高"就一路跳着走，看着像抽筋。
            // 原版自己会跨 1 格台阶；只有明确要跨 2..5 格时才由上层显式 jump。
            // A* 的跳跃边可能落在一段开阔空间（前方没有立刻的墙），这时只发 Up
            // 不会起跳；跟随高处队友就会永远在水平面边跳边走。路点本身已经是
            // 可站立的垂直目标，按目标高差发一次真实跳跃，冲量/碰撞仍由原版计算。
            if (dy < -1) WalkJump(Math.Min(5.9f, -dy + 1.2f));
            // 目标在下方（dy>2）：不挖，靠"走过边缘自然下落"。地形本来就有起伏，
            // 事故：这里原来判 TileSolid(px,py+2) 就当成"要往下挖"直接停，而 py+2 往往就是自己脚踩的那块地。

        }

        /// <summary>
        /// 移动意图在 PostUpdateInput 阶段写入原生方向触发器，
        /// 速度、碰撞、重力和台阶全部由游戏解算。
        /// </summary>

        // ==== 跟随 / 待命 / 护卫（和人一起玩的基础） ====
        // 设计：只写原生方向触发器，碰撞/重力/门/台阶全交给原版解算。
        // 不做瞬移、不改方块——后随的人走不过去的地方，就等着或者喊人。

        internal static volatile bool FollowActive, FollowGuard;
        internal static volatile int FollowTargetIdx = -1;
        internal static volatile float FollowStopTiles = 4.5f;
        internal static ulong FollowNextThink;
        // 跟随只负责把队友的实时位置转换成 walkto 目标；具体寻路、越障和
        // 输入时序全部复用既有 WalkActive/ComputePath 执行器，不维护 Bot 专属规则。
        private static int FollowPathTargetX = int.MinValue, FollowPathTargetY = int.MinValue;
        private static ulong FollowNextRepath;

        internal static void ClearFollow()
        {
            FollowActive = false;
            FollowGuard = false;
            FollowTargetIdx = -1;
            FollowNextThink = 0;
            FollowPathTargetX = FollowPathTargetY = int.MinValue;
            FollowNextRepath = 0;
            ClearAttack();
        }

        internal static Player FindFollowTarget(Player me)
        {
            if (FollowTargetIdx >= 0 && FollowTargetIdx < Main.maxPlayers)
            {
                Player q = Main.player[FollowTargetIdx];
                if (q != null && q.active && !q.dead && q.whoAmI != me.whoAmI) return q;
            }
            // 没指定就找最近的另一个活着的玩家
            Player best = null; float bestD = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player q = Main.player[i];
                if (q == null || !q.active || q.dead || q.whoAmI == me.whoAmI) continue;
                float d = Vector2.DistanceSquared(q.Center, me.Center);
                if (d < bestD) { bestD = d; best = q; }
            }
            return best;
        }

        /// <summary>找玩家正在打的目标；直接读 NPC.target（原版就是这么记仇恨的）。</summary>
        internal static int FindPlayerTargetNPC(Player mate, float maxTiles)
        {
            float maxD = maxTiles * 16f;
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0) continue;
                if (!n.CanBeChasedBy()) continue;
                if (n.target != mate.whoAmI) continue;
                float d = Vector2.DistanceSquared(Main.LocalPlayer.Center, n.Center);
                if (d <= maxD * maxD && d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        internal static void TickFollow()
        {
            Player me = Main.LocalPlayer;
            if (me == null || !me.active || me.dead) return;
            Player mate = FindFollowTarget(me);
            if (mate == null) { ClearFollow(); return; }

            float dx = (mate.Center.X - me.Center.X) / 16f;
            float dy = (mate.Center.Y - me.Center.Y) / 16f;
            float adx = Math.Abs(dx);

            // 护卫：血低于 30% 先别打，喝了药再上；否则优先打玩家正在打的目标。
            bool lowHp = me.statLife < me.statLifeMax * 0.3f;
            if (FollowGuard)
            {
                if (lowHp) { if (AttackUntil > 0) ClearAttack(); }
                else if (Now >= FollowNextThink)
                {
                    FollowNextThink = Now + Ticks(250);
                    if (AttackUntil == 0)
                    {
                        int tgt = FindPlayerTargetNPC(mate, 26f);
                        if (tgt < 0) tgt = FindNearestHostile(me, 14);
                        if (tgt >= 0) BeginAttack(tgt, false, 26, 1200, 90);
                    }
                }
            }

            // 够近就停止既有 walkto；超过保持距离时，把目标玩家的实时位置
            // 转成一个保持距离的 walkto 终点，并直接调用同一套执行器。
            bool needMove = adx > FollowStopTiles || Math.Abs(dy) > 1.5f;
            if (!needMove)
            {
                StopWalk("follow-range");
                return;
            }

            // 同一列但高差很大时，不能强行把目标横移 FollowStopTiles 格：
            // 那会把“跳到队友所在平台”变成“先绕到平台旁边的空气点”，A* 常常
            // 找不到可站立目标，表现为只在原地水平移动。只有确实有水平距离时
            // 才保持安全间隔；纯垂直跟随直接瞄准队友所在列。
            int sign = Math.Abs(dx) > 0.25f ? Math.Sign(dx) : 0;
            int goalX = (int)MathF.Round(mate.Center.X / 16f - sign * FollowStopTiles);
            int rawGoalY = (int)MathF.Round(mate.Center.Y / 16f);
            (goalX, int goalY) = NormalizeFollowTarget(goalX, rawGoalY, 8, 8);
            bool targetMoved = FollowPathTargetX == int.MinValue
                || Math.Abs(goalX - FollowPathTargetX) >= 2
                || Math.Abs(goalY - FollowPathTargetY) >= 2;
            // 不要按固定 250ms 重置正在执行的路径。高处路线往往需要先走到起跳列，
            // 再跳到上层；频繁 BeginWalk 会把路点清掉，Bot 永远走不到跳跃那一步。
            // 目标移动两格以上或当前任务结束时才重开路径；同一局部区域内只更新终点，
            // 让 TickWalk 自己完成现有路点并在受阻时重规划。
            // 加 250ms 冷却：A* 找不到路时 BeginWalk 会立刻失败，若不节流就变成
            // 每帧重跑一次全图搜索，帧率会被拖垮，而且仍是一步不动。
            if ((!WalkActive || targetMoved) && Now >= FollowNextRepath)
            {
                PathAllowDig = false;
                PathGreedy = false;
                PathProtectBuilds = true;
                BeginWalk(goalX, goalY, 1, Now + Ticks(3000));
                FollowPathTargetX = goalX;
                FollowPathTargetY = goalY;
                FollowNextRepath = Now + Ticks(250);
            }
            else
            {
                // 目标在同一局部区域内移动时更新终点，但保留当前路点；
                // 原有 WalkActive 会在路点完成/受阻时重新规划。
                WalkX = goalX;
                WalkY = goalY;
            }
            TickWalk();
        }

        /// <summary>精确横向移动：按目标格坐标比例控制速度，到位即停（误差 0.06 格）。</summary>
        internal static bool ExactActive;
        internal static float ExactTargetX;
        internal static int ExactUntil;
        // 精确移动必须提前松手：Terraria 会在没有方向键后继续按原版摩擦滑行，
        // 直到 velocity 自己衰减。这里记录的是控制阶段和诊断，不写速度。
        // 0=加速，1=滑行，2=反向修正。
        internal static int ExactPhase;
        internal static int ExactStuckTicks;
        internal static float ExactLastX;
        internal static float ExactBrakeDistance;
        internal static float ExactRemaining;
        internal static string ExactStopReason = "";

        // 真机测得的“松手后惯性距离”查表。velocity.X 是 Terraria 原生速度单位，
        // 返回值是格。分段插值只用于查表之间的速度；绝不把它写回 Player.velocity。
        private static readonly (float speed, float tiles)[] ExactCoastTable =
        {
            (0.20f, 0.03f),
            (1.94f, 0.71f),
            (2.71f, 1.32f),
            (3.48f, 2.12f),
            (3.57f, 2.22f),
        };

        internal static float EstimateExactCoast(float speed)
        {
            speed = Math.Abs(speed);
            if (speed <= ExactCoastTable[0].speed) return ExactCoastTable[0].tiles;
            for (int i = 1; i < ExactCoastTable.Length; i++)
            {
                var (s1, d1) = ExactCoastTable[i];
                var (s0, d0) = ExactCoastTable[i - 1];
                if (speed <= s1)
                {
                    float f = (speed - s0) / (s1 - s0);
                    return d0 + (d1 - d0) * f;
                }
            }
            return ExactCoastTable[ExactCoastTable.Length - 1].tiles;
        }

        internal static void ApplyMovement()
        {
            if (MovementAppliedTick == Now) return;
            MovementAppliedTick = Now;
            IntentLeft = IntentRight = IntentUp = IntentDown = IntentJump = false;
            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead) return;
            ulong now = Main.GameUpdateCount;

            // Vanilla deliberately suppresses horizontal movement and jumping
            // while the feet are on a rope.  Release that state with a genuine
            // Down input before starting a queued move/jump; never clear pulley
            // or manufacture position/velocity.
            if (TickRopeRelease(p, now)) return;

            if (ExactActive)
            {
                // Exact movement owns horizontal direction windows while it is active.
                // Clear stale move/tiles windows so an earlier command cannot fight the
                // controller or keep pressing after the target has been reached.
                LeftUntil = 0;
                RightUntil = 0;
                float cur = p.position.X / 16f;
                float dx = ExactTargetX - cur;
                float speed = p.velocity.X;
                float remaining = Math.Abs(dx);
                float coast = EstimateExactCoast(speed);
                ExactRemaining = remaining;
                ExactBrakeDistance = coast;

                if (now > (ulong)ExactUntil)
                {
                    // 到位不再手动清速度：靠原版摩擦自然停下（真人松手也是滑一小段）。
                    ExactActive = false;
                    ExactStopReason = "timeout";
                    ExactPhase = 1;
                    return;
                }

                if (remaining <= 0.08f && Math.Abs(speed) <= 0.12f)
                {
                    // 只有位置和速度都回读到稳定，才算真正到位；命令层可以用 state
                    // 的 exact.active/stopReason 判断，而不是相信“按键到期”。
                    ExactActive = false;
                    ExactStopReason = "arrived";
                    ExactPhase = 1;
                    return;
                }

                if (Math.Abs(cur - ExactLastX) < 0.002f && remaining > 0.12f)
                    ExactStuckTicks++;
                else
                    ExactStuckTicks = 0;
                ExactLastX = cur;
                if (ExactStuckTicks > 45)
                {
                    ExactActive = false;
                    ExactStopReason = "blocked";
                    ExactPhase = 1;
                    return;
                }

                int wanted = Math.Sign(dx);
                // speedToward > 0 means the current inertia is already carrying the
                // player toward the target. Once the measured coast distance covers the
                // remaining gap, release both keys and let vanilla friction do the braking.
                float speedToward = speed * wanted;
                if (speedToward > 0.05f && remaining <= coast + 0.04f)
                {
                    ExactPhase = 1;
                    return;
                }

                // If the player crossed the target, or is still drifting away from it,
                // press the target-side key. The engine then decelerates/reverses using
                // its own horizontal movement code; no velocity is manufactured here.
                ExactPhase = speedToward < -0.05f ? 2 : 0;
                if (wanted < 0) RequestLeft();
                else if (wanted > 0) RequestRight();
                return;
            }
            // 跟随由既有 walkto 执行器驱动；它必须先于普通 WalkActive 分支，
            // 这样每帧都能把队友的最新位置交给同一套寻路逻辑。
            if (FollowActive) { TickFollow(); return; }
            if (PathActive) { TickPath(); return; }
            if (WalkActive)
            {
                // 双分支并行：新版四层只算不执行（对比用），旧版照常驱动，行为不变。
                ActorNew = Evaluate(SenseWorld());
                if (ActorUseNew)
                {
                    var sn = SenseWorld();
                    ActActor(ActorNew, p, sn.dir);
                }
                else if (ActorDualRun)
                {
                    TickWalk();
                    ActorOld = ObserveOldAction(p);
                }
                else TickWalk();
                return;
            }
            float dir = 0f;
            if (LeftUntil > now) dir -= 1f;
            if (RightUntil > now) dir += 1f;
            if (dir != 0f)
            {
                if (LeftUntil > now) RequestLeft();
                if (RightUntil > now) RequestRight();
            }

            // Up/Down/Climb and platform-drop input is emitted in PostUpdateInput so the
            // vanilla game receives native triggers before Player.Update.
            if (JumpUntil > now)
            {
                RequestJump();
                // One impulse per command, and only from the ground. The old code
                // re-impulsed at every apex, which turned a 2 s jump into an
                // unpowered climb; wings and auto-jump are left to vanilla.
                // 原生模式下这里一个字都不写：引擎自己的 Player.Jump() 才是"按住更高"的来源。
                if (!JumpIssued && p.velocity.Y == 0f && p.oldVelocity.Y == 0f)
                {
                    // The vanilla engine owns the jump impulse and variable-height timing.
                    // If it does not accept the trigger, report that through state instead of
                    // manufacturing a velocity fallback.
                }
            }
            // 平台下穿只靠 controlDown，不再在这里请求跳跃（见 ApplyNativeControlFlags）。
            // 行走结果驱动：走够格数就松手，不管花了多久、中间被挡过几次。
            if (MoveStopTiles > 0f)
            {
                float curX = p.Center.X / 16f, curY = p.Center.Y / 16f;
                float moved = MoveStopAxis == 0 ? Math.Abs(curX - MoveFromX) : Math.Abs(curY - MoveFromY);
                if (moved >= MoveStopTiles)
                {
                    LeftUntil = 0; RightUntil = 0; UpUntil = 0; DownUntil = 0;
                    MoveStopTiles = 0f;
                }
            }
            if (UseUntil > now)
            {
                p.controlUseItem = true;
                if (UseStopWhenClear && LastUseX >= 0 && LastUseY >= 0
                    && !Main.tile[LastUseX, LastUseY].HasTile)
                {
                    EndItemUse();
                }
                else if (UseStopWhenPlaced && LastUseX >= 0 && LastUseY >= 0
                    && Main.tile[LastUseX, LastUseY].HasTile)
                {
                    EndItemUse();
                }
            }
            if (UseTileUntil > now) p.controlUseTile = true;
            // 只要有桥的动作还在跑，准星就归桥；一旦全停，立刻把准星还给真人。
            // 旧写法只在 AimUntil 非 0 时才清理，而大部分调用点（mine/place/hook…）只调
            // SetAim 不设 AimUntil —— 结果 AimTileX 永久残留，PreItemCheck 每帧把准星压回
            // 那个旧格子，真人挥镐/放方块全打在空格上，表现为"挖不了、放不了"。这是实测事故。
            bool bridgeBusy = UseUntil > now || UseTileUntil > now || (HookHeld && HookUntil > now);
            if (!bridgeBusy && AimTileX >= 0) { AimTileX = -1; AimTileY = -1; AimUntil = 0; }
            else if (AimUntil != 0 && now > AimUntil) { AimTileX = -1; AimTileY = -1; AimUntil = 0; }
        }

        /* ── 拟人瞄准：把游戏内光标指到某个方块 ───────────────────────────
         * Terraria 每帧都会从物理鼠标重算 Main.mouseX/MouseWorld，所以瞄准必须
         * 在 Player.Update 之前（ModPlayer.PreUpdate）持续写入，否则会被覆盖。
         * 这样物品使用（挖掘/放置/攻击）走的就是原版逻辑：工具、动画、冷却、耗材。
         */
        internal static volatile int AimTileX = -1, AimTileY = -1;
        internal static ulong AimUntil;
        internal static volatile bool UseJustStarted, UseTileJustStarted;
        internal static ulong UseTileLastPulse;
        internal static int LastUseSlot = -1, LastUseX = -1, LastUseY = -1;
        internal static long PreItemCalls, SetControlCalls, SynthCalls;

        internal static void SetAim(int x, int y) { AimTileX = x; AimTileY = y; }
        internal static void ClearAim() { AimTileX = -1; AimTileY = -1; }

        internal static void BeginTileInteraction(int tileX, int tileY, int ms = 120)
        {
            SetAim(tileX, tileY);
            UseTileJustStarted = true;
            UseTileUntil = Now + Ticks(Math.Clamp(ms, 1, 2000));
        }

        /// <summary>
        /// 让游戏自己把"使用物品"这件事做一遍：选中真实槽位、准星压到目标格、按住使用 ms 毫秒。
        /// 真正的放置/挖掘/消耗全部由原版 ItemCheck 完成——支撑判定、家具锚点、扣物品、刷 frame
        /// 都是游戏自己的逻辑，这里不碰 Main.tile。
        /// 调用方必须回读确认，因为原版使用是异步的。
        /// </summary>
        internal static void BeginItemUse(int slot, int tileX, int tileY, int ms)
        {
            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return;
            if (slot >= 0 && slot < p.inventory.Length) p.selectedItem = slot;
            // 只有"换成另一件事"才重新起手。持续挖同一格时每一帧都置 UseJustStarted 会让
            // ApplyAim 每帧塞一次 mouseLeftRelease，原版就把挥动动画不断重置——玩家在屏幕上
            // 看不到正常的挥镐动作，土方却还在消失，完全像指令挖。实测这就是"看着不像真人"的根因。
            // 上一次挥动已经播完（空手等待 / 动作结束）时，必须允许重新起手——否则连续射击同一目标
            // 时第二箭永远不发（实测：对着同一格连射 6 次，箭一支没少）。这就是真人"按住左键连挥"。
            bool animDone = p.itemAnimation == 0 && p.itemTime == 0;
            bool sameTarget = slot == LastUseSlot && tileX == LastUseX && tileY == LastUseY && UseUntil > Now && !animDone;
            SetAim(tileX, tileY);
            if (!sameTarget) UseJustStarted = true;
            LastUseSlot = slot; LastUseX = tileX; LastUseY = tileY;
            UseUntil = Now + Ticks(ms);
        }

        internal static void EndItemUse()
        {
            UseUntil = 0;
            UseTileUntil = 0;
            LastUseSlot = -1; LastUseX = -1; LastUseY = -1;
            UseStopWhenClear = false;
            UseStopWhenPlaced = false;
            ClearAim();
        }

        /// <summary>
        /// Point the in-game cursor at a tile AND synthesize the mouse button state.
        /// Setting controlUseItem alone is not enough: Terraria rebuilds the player's control
        /// flags from the physical mouse every frame, so an armed action needs Main.mouseLeft /
        /// Main.mouseRight to look pressed for that frame as well.
        /// Returns false when the tile is off-screen.
        /// </summary>
        internal static bool ApplyAim()
        {
            int tx = AimTileX, ty = AimTileY;
            if (tx < 0 || ty < 0) return false;
            float worldX = tx * 16f + 8f;
            float worldY = ty * 16f + 8f;
            float screenX = worldX - Main.screenPosition.X;
            float screenY = worldY - Main.screenPosition.Y;
            // Main.MouseWorld is read-only in 1.4.4 (derived from the mouse position), so the
            // cursor is driven through the raw coordinates; tile interactions read
            // Player.tileTargetX/Y, which are static and are what placement/mine actually use.
            // ApplyAim can run both before and after PlayerInput.SetZoom_MouseInWorld.  Seed
            // the private raw cache for the former case and write the final screen point for
            // the latter; either phase therefore resolves to the requested world tile.
            ApplyWorldAimBeforeZoom(new Vector2(worldX, worldY));
            Main.mouseX = (int)MathF.Round(screenX);
            if (Main.LocalPlayer != null && Main.LocalPlayer.gravDir == -1f)
                screenY = Main.screenHeight - screenY;
            Main.mouseY = (int)MathF.Round(screenY);
            // 这一版 tModLoader 没有可写的智能光标开关（Main 只有只读的 SmartCursorWanted /
            // SmartCursorIsUsed），所以不去碰设置；准星落点靠 mouseX/Y + tileTargetX/Y 直接压住，
            // 实测 mine/place 回读的格子就是传入的那一格。
            Player.tileTargetX = tx;
            Player.tileTargetY = ty;

            ulong now = Main.GameUpdateCount;
            if (UseUntil > now)
            {
                Main.mouseLeft = true;
                if (UseJustStarted) { Main.mouseLeftRelease = true; UseJustStarted = false; }
            }
            if (UseTileUntil > now)
            {
                Main.mouseRight = true;
                // 右键“按下边沿”原来只在第一帧给一次，实测原版开箱子抓不到（界面不弹、openChest 恒 -1）。
                // 改成每 4 帧补一次释放边沿，让引擎在任何相位读取时都能看到一次完整“按下”。
                if (UseTileJustStarted || now - UseTileLastPulse >= 4)
                {
                    Main.mouseRightRelease = true;
                    UseTileLastPulse = now;
                    UseTileJustStarted = false;
                }
            }
            return screenX >= 0 && screenY >= 0 && screenX < Main.screenWidth && screenY < Main.screenHeight;
        }

        /// <summary>Is that tile inside the player's reach (~7 tiles, like a human)?</summary>
        internal static bool InReach(Player p, int tx, int ty, int tiles = 7)
        {
            if (p == null || !p.active) return false;
            float cx = p.Center.X / 16f, cy = p.Center.Y / 16f;
            return Math.Abs(cx - tx) <= tiles && Math.Abs(cy - ty) <= tiles;
        }

        /* 只走"后台合成输入"这一条路：从 PreItemCheck 里按下使用标记，由原版 ItemCheck
         * 执行挥动/放置/挖矿。早先那套 SetCursorPos+mouse_event 的 OS 级注入已删除——
         * 它会移动用户的真实光标、抢焦点，违反"不要直接控制我的电脑"这条规则。
         */

        /// <summary>
        /// Point the cursor at the armed tile and pretend the mouse button is held. Called
        /// from PreItemCheck, so vanilla's own use/placement/mining logic runs.
        /// </summary>
        internal static void ApplySyntheticInput()
        {
            SynthCalls++;
            ulong now = Now;
            // 只在桥自己正在挖/放/抓的时候压准星。其余时间一律不碰 Main.mouseX/Y 与
            // tileTargetX/Y —— 真人接管键盘时，准星必须是他的鼠标，否则挖掘与放置全部失效。
            // 放绳：绳子只能铺在自己头顶，所以目标格必须每 tick 跟着人走，
            // 用上一次挖/放留下的旧准星会铺到别处（实测：laying:true 但一格没铺出来）。
            if (LayRopeUntil > now)
            {
                Player lp = Main.LocalPlayer;
                if (lp != null)
                {
                    AimTileX = (int)(lp.Center.X / 16f);
                    AimTileY = (int)(lp.Center.Y / 16f) - 2;
                }
            }
            bool bridging = UseUntil > now || UseTileUntil > now || (HookHeld && HookUntil > now) || LayRopeUntil > now;
            if (bridging && AimTileX >= 0) ApplyAim();
            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return;

            ApplyGuard(p, now);
            // 坐骑输出不在这里：见 PostUpdateInput 的 ApplyBridgeTrigger(QuickMount)。
            if (UseUntil > now)
            {
                Main.mouseLeft = true;
                p.controlUseItem = true;
                if (UseJustStarted)
                {
                    Main.mouseLeftRelease = true;
                    p.releaseUseItem = true;
                    UseJustStarted = false;
                }
            }
            if (LayRopeUntil > now)
            {
                Main.mouseLeft = true;
                p.controlUseItem = true;
            }
            if (HookHeld && HookUntil > now)
            {
                // 只置 controlHook。以前这里顺手把 Main.mouseLeft 也置真，结果原版把这一帧
                // 当成"使用当前物品"，抓钩一次都没飞出去（实测 grapCount 连续 3 秒停在 0）。
                p.controlHook = true;
                // 光写 controlHook 没用：Player.Update 里会用输入层结果重建它
                // （`controlHook = Main.mouseLeft`），写在 SetControls 的值当场被覆盖。
                // 所以按住左键本身也要在输入层为真——抓钩在装备槽，此时手上不是攻击物品，
                // 不会像以前那样把这一帧变成"使用当前物品"。
                Main.mouseLeft = true;
                Main.mouseLeftRelease = false;
                // 这里已经过了 PlayerInput 的 zoom 转换，写入最终屏幕坐标；
                // 目标在输入前阶段则由 ApplyHookAimBeforeZoom 负责逆变换。
                ApplyHookAimAfterZoom();
                // 抓钩发射要「刚按下」这一沿。反编译 Player.Update 确认原版逻辑是：
                //   if (controlHook) { if (releaseHook) QuickGrapple(); releaseHook = false; }
                //   else releaseHook = true;
                // 也就是说 **controlHook 与 releaseHook 同时为真** 才会发射。
                // 以前这里写 false（误以为 QuickGrapple 开头是 if (releaseHook) return），
                // 结果 releaseHook 恒假 → QuickGrapple 一次都没被调用（grapCount 恒 0）。
                // 每帧都置 true 是安全的：QuickGrapple 内部对"已有同型弹幕"直接 return，
                // 不会重复发射，等钩子收回后才允许再发。
                p.releaseHook = true;
            }
            if (HookReleasePending)
            {
                p.controlHook = false;
                p.releaseHook = true;
                HookReleasePending = false;
            }
            // Unhook is represented only by a released hook input.  The vanilla projectile
            // lifecycle decides whether the attachment ends; the bridge never kills a
            // projectile or edits grappling/pulley state directly.
            if (UseTileUntil > now)
            {
                Main.mouseRight = true;
                p.controlUseTile = true;
                if (UseTileJustStarted)
                {
                    Main.mouseRightRelease = true;
                    UseTileJustStarted = false;
                }
            }
        }



        /* ── 直线旅行任务 ────────────────────────────────────────────────
         * 把脚本层验证过无数次的“挖-铺-走”循环搬进游戏内：每帧自己挖掉前方挡路格、
         * 补上脚下地板、按住方向键前进。遇敌/血低自动停下交还给脚本层做决策——
         * C# 只干最耗时的地形活，判断仍留在外面。
         */
        internal struct TravelJob
        {
            public bool Active;
            public int Dir;          // -1 西, +1 东
            public int TargetX;
            public ulong Deadline;
            public ulong NextMine;
            public int Mined, Filled, Steps, Swings;
            public int ArmedX, ArmedY;     // 上一帧压准星的目标格（-1 = 无）
            public bool Dig;               // false = 只走路，绝不碰方块
            public string StopReason;
            public int LastPx, LastPy, Stuck;
        }

        internal static TravelJob Travel;

        internal static void StartTravel(int dir, int targetX, int ms, bool dig = true)
        {
            Travel = new TravelJob
            {
                Active = true,
                Dig = dig,
                Dir = dir,
                TargetX = targetX,
                Deadline = Now + Ticks(ms),
                NextMine = Now,
                ArmedX = -1,
                StopReason = "",
                LastPx = -99999, LastPy = -99999, Stuck = 0,
            };
        }

        internal static void StopTravel(string reason)
        {
            if (!Travel.Active) return;
            Travel.Active = false;
            Travel.StopReason = reason ?? "stopped";
            LeftUntil = 0; RightUntil = 0;
        }

        internal static string TravelStatus()
        {
            Player p = Main.LocalPlayer;
            int px = p == null ? -1 : (int)MathF.Floor(p.Center.X / 16f);
            int py = p == null ? -1 : (int)MathF.Floor(p.Center.Y / 16f);
            return Json.Ok(new
            {
                active = Travel.Active,
                dir = Travel.Dir == 3 ? "up" : (Travel.Dir == 2 ? "down" : (Travel.Dir > 0 ? "right" : "left")),
                targetX = Travel.TargetX,
                x = px,
                y = py,
                mine = Travel.Mined,
                fill = Travel.Filled,
                steps = Travel.Steps,
                swings = Travel.Swings,
                aiming = Travel.ArmedX < 0 ? null : (object)new { x = Travel.ArmedX, y = Travel.ArmedY },
                stopReason = Travel.StopReason,
                // walkto 自己的状态：卡住时才能看出走的是哪条路、为什么停
                walk = new
                {
                    active = WalkActive,
                    x = WalkX, y = WalkY, tol = WalkTol,
                    stop = WalkStop,
                    repaths = WalkRepaths,
                    hasWp = WalkHasWp,
                    wpX = WalkWpX, wpY = WalkWpY,
                    greedy = PathGreedy,
                    reactive = WalkReactive,
                    rule = ActReactive.rule ?? "",
                    kind = ActReactive.kind ?? "",
                    // 计时器残留是"控制标志与路点反向"的头号嫌疑：这两个值还剩多少 ms 是直接证据。
                    lu = (long)LeftUntil > (long)Main.GameUpdateCount ? (long)LeftUntil - (long)Main.GameUpdateCount : 0,
                    ru = (long)RightUntil > (long)Main.GameUpdateCount ? (long)RightUntil - (long)Main.GameUpdateCount : 0,
                },
                hp = p == null ? 0 : (int)(100f * p.statLife / Math.Max(1, p.statLifeMax2)),
            });
        }

        private static void TickTravel()
        {
            if (!Travel.Active) return;
            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead) { StopTravel(p == null || !p.active ? "no-player" : "dead"); return; }
            if (Now > Travel.Deadline) { StopTravel("timeout"); return; }

            int px = (int)MathF.Floor(p.Center.X / 16f);
            int py = (int)MathF.Floor(p.Center.Y / 16f);
            // 到达：横向看 x，纵向（Dir==2 向下）看 y
            bool vertical = Travel.Dir == 2;
            bool upward = Travel.Dir == 3;
            int reachedNow;
            if (upward) reachedNow = py <= Travel.TargetX ? 1 : 0;
            else if (vertical) reachedNow = py >= Travel.TargetX ? 1 : 0;
            else reachedNow = Travel.Dir > 0 ? (px >= Travel.TargetX ? 1 : 0) : (px <= Travel.TargetX ? 1 : 0);
            if (reachedNow == 1) { StopTravel("reached"); return; }

            // 卡死看门狗：位置连续 45 帧不动就停（以前只能等超时，脚本层干等 60 秒）
            if (px == Travel.LastPx && py == Travel.LastPy) Travel.Stuck++;
            else { Travel.Stuck = 0; Travel.LastPx = px; Travel.LastPy = py; }
            if (Travel.Stuck > 45) { StopTravel("stuck"); return; }
            // 血低 / 溺水 / 有敌人：交还给脚本层
            if (p.statLife < p.statLifeMax2 * 0.45f) { StopTravel("lowhp"); return; }
            if (p.wet && p.breath <= 0) { StopTravel("drowning"); return; }
            // 前方 3 格内有水就停：继续挖只会引水灌进来
            {
                int ax = Travel.Dir == 3 ? px : (vertical ? px : px + Travel.Dir);
                bool water = false;
                if (upward)
                {
                    for (int dx = -1; dx <= 1 && !water; dx++)
                        for (int dy = -4; dy <= -1 && !water; dy++)
                        {
                            int wx = px + dx, wy = py + dy;
                            if (!WorldGen.InWorld(wx, wy, 2)) continue;
                            if (Main.tile[wx, wy].LiquidAmount >= 200) water = true;
                        }
                }
                else if (vertical)
                {
                    for (int dx = -1; dx <= 1 && !water; dx++)
                        for (int dy = 2; dy <= 5 && !water; dy++)
                        {
                            int wx = px + dx, wy = py + dy;
                            if (!WorldGen.InWorld(wx, wy, 2)) continue;
                            if (Main.tile[wx, wy].LiquidAmount >= 200) water = true;
                        }
                }
                else
                {
                    // 横向赶路：前方 1~2 列、从头顶到脚下 5 格全扫，水和深坑都要停。
                    // 原来只查前方 1 列的 dy -1..+2，实测在 x≈2220 的湖盆上没拦住：
                    // 水在脚下第 3 格（dy+3），travel 一路走过去直接掉进去溺水身亡。
                    for (int step = 1; step <= 2 && !water; step++)
                    {
                        int wx = px + Travel.Dir * step;
                        for (int dy = -1; dy <= 5 && !water; dy++)
                        {
                            int wy = py + dy;
                            if (!WorldGen.InWorld(wx, wy, 2)) continue;
                            if (Main.tile[wx, wy].LiquidAmount >= 200) water = true;
                        }
                    }
                }
                // 上浮例外：已经泡在水里时，向上游是唯一出路，不能因为"前方有水"就停。
                // （实测井底积水后 travel up 直接 stop=water，人困死在里面）
                if (water && !(upward && p.wet)) { StopTravel("water"); return; }
            }
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.friendly || n.lifeMax <= 5) continue;
                if (n.townNPC) continue;
                if (Math.Abs(n.Center.X - p.Center.X) / 16f > 9f) continue;
                if (Math.Abs(n.Center.Y - p.Center.Y) / 16f > 9f) continue;
                StopTravel("enemy");
                return;
            }

            // 每 tick 持续推进方向键
            ulong until = Now + 40;
            // up 方向只按上键，**不按跳跃**。
            // 原来这里同时设了 JumpUntil：跳跃键按住会让原版连续起跳，
            // 于是"往上"表现成凭空一路飞升（用户 m22312：根本没绳子）。
            if (upward) { UpUntil = until; DownUntil = 0; }
            else if (vertical) { DownUntil = until; UpUntil = 0; }
            else if (Travel.Dir > 0) { RightUntil = until; LeftUntil = 0; }
            else { LeftUntil = until; RightUntil = 0; }

            // dig:false 时只推方向键，一格都不挖、也不补地板。
            if (!Travel.Dig) return;
            if (Now < Travel.NextMine) return;
            Travel.NextMine = Now + 4;

            // 找镐
            int pick = -1;
            for (int i = 0; i < 10; i++)
            {
                Item it = p.inventory[i];
                if (it != null && it.type > 0 && it.pick > 0) { pick = i; break; }
            }
            if (pick < 0) { StopTravel("no-pickaxe"); return; }

            // 先算这一帧要挖哪一格；有格子要挖就只挥镐，不再顺手铺地板——
            // 每 tick 换两次选中物品会让原版反复打断挥动，实测把掘进速度砍掉一半。
            int tx = px + Travel.Dir, ty = py;
            bool hasTarget = false;
            if (upward)
            {
                tx = px; ty = py - 1;
                for (int dy = -3; dy <= -1 && !hasTarget; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (WorldGen.InWorld(px + dx, py + dy, 2) && Main.tile[px + dx, py + dy].HasTile)
                        { tx = px + dx; ty = py + dy; hasTarget = true; break; }
            }
            else if (vertical)
            {
                tx = px; ty = py + 2;
                for (int dy = 2; dy <= 3 && !hasTarget; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (WorldGen.InWorld(px + dx, py + dy, 2) && Main.tile[px + dx, py + dy].HasTile)
                        { tx = px + dx; ty = py + dy; hasTarget = true; break; }
            }
            else
            {
                // py-2..py+1 四行：角色高 2.6 格，只挖 3 行会漏掉头顶那格
                for (int dy = -2; dy <= 1; dy++)
                {
                    int by = py + dy;
                    if (!WorldGen.InWorld(tx, by, 2)) continue;
                    if (Main.tile[tx, by].HasTile) { ty = by; hasTarget = true; break; }
                }
            }

            // 上一帧压的那一格如果已经空了，就记一次真实挖穿（回读证据，不是计数器自我汇报）
            if (Travel.ArmedX >= 0 && WorldGen.InWorld(Travel.ArmedX, Travel.ArmedY, 2)
                && !Main.tile[Travel.ArmedX, Travel.ArmedY].HasTile)
            {
                Travel.Mined++;
                Travel.ArmedX = -1;
            }

            if (hasTarget)
            {
                // 真实挥镐：让原版 ItemCheck 自己结算硬度/镐力/掉落。
                BeginItemUse(pick, tx, ty, 140);
                Travel.ArmedX = tx; Travel.ArmedY = ty; Travel.Swings++;
            }
            else if (Travel.Steps % 2 == 0)
            {
                // 没有格子要挖，才补脚下地板（真实放置，方块从背包扣）。
                for (int d = 1; d <= 2; d++)
                {
                    int fx = px + Travel.Dir * d, fy = py + 2;
                    if (!WorldGen.InWorld(fx, fy, 2)) continue;
                    if (Main.tile[fx, fy].HasTile) continue;
                    int bslot = -1;
                    for (int i = 0; i < 10; i++)
                    {
                        Item it = p.inventory[i];
                        if (it != null && it.type > 0 && it.stack > 0 && it.createTile > 0) { bslot = i; break; }
                    }
                    if (bslot < 0) break;
                    BeginItemUse(bslot, fx, fy, 140);
                    if (Main.tile[fx, fy].HasTile) Travel.Filled++;
                    break;
                }
            }

            Travel.Steps++;
        }

        /// <summary>Arm one jump for the given deadline (a single ground impulse).</summary>
        internal static void BeginJump(ulong until)
        {
            JumpUntil = until;
            JumpIssued = false;
            JumpGapUntil = 0;
            // Keep the first press edge alive even when the command arrives between two
            // PostUpdateInput callbacks.  The next callback will emit the native trigger;
            // SetControls then writes the final controlJump flag at the vanilla decision point.
            IntentJump = true;
        }

        private static void ArmRopeRelease(string kind)
        {
            RopeDeferredKind = kind;
            RopeReleasePending = true;
            RopeReleaseFailed = false;
            RopeReleaseFailureTick = 0;
            RopeReleaseUntil = Now + Ticks(1200);
            // Keep only the native down input alive while the old action waits.
            LeftUntil = RightUntil = UpUntil = JumpUntil = 0;
            DownUntil = RopeReleaseUntil;
            ExactActive = false;
            MoveStopTiles = 0f;
            IntentLeft = IntentRight = IntentUp = IntentDown = IntentJump = false;
            MovementAppliedTick = ulong.MaxValue;
        }

        internal static bool QueueRopeReleaseMove(string dir, int ms, float tiles)
        {
            if (!OnRopeNow(Main.LocalPlayer)) return false;
            RopeDeferredDir = dir;
            RopeDeferredMs = ms;
            RopeDeferredTiles = tiles;
            RopeDeferredTargetX = 0f;
            ArmRopeRelease("move");
            return true;
        }

        internal static bool QueueRopeReleaseExactMove(float targetX, int ms)
        {
            if (!OnRopeNow(Main.LocalPlayer)) return false;
            RopeDeferredDir = "";
            RopeDeferredMs = ms;
            RopeDeferredTiles = 0f;
            RopeDeferredTargetX = targetX;
            ArmRopeRelease("moveto");
            return true;
        }

        internal static bool QueueRopeReleaseJump(int ms, string dir, int moveMs)
        {
            if (!OnRopeNow(Main.LocalPlayer)) return false;
            RopeDeferredDir = dir ?? "";
            RopeDeferredMs = ms;
            RopeDeferredMoveMs = moveMs;
            RopeDeferredTiles = 0f;
            RopeDeferredTargetX = 0f;
            ArmRopeRelease("jump");
            return true;
        }

        private static void ActivateDeferredRopeAction(Player p)
        {
            string kind = RopeDeferredKind;
            string dir = RopeDeferredDir;
            int ms = RopeDeferredMs;
            int moveMs = RopeDeferredMoveMs > 0 ? RopeDeferredMoveMs : ms;
            float tiles = RopeDeferredTiles;
            float targetX = RopeDeferredTargetX;
            RopeDeferredKind = "";
            RopeDeferredDir = "";
            RopeDeferredMs = RopeDeferredMoveMs = 0;
            RopeDeferredTiles = RopeDeferredTargetX = 0f;
            RopeReleasePending = false;
            DownUntil = 0;

            ulong until = Now + Ticks(ms);
            if (kind == "moveto")
            {
                ExactActive = true;
                ExactTargetX = targetX;
                ExactUntil = (int)until;
                ExactPhase = 0;
                ExactStuckTicks = 0;
                ExactLastX = p.position.X / 16f;
                ExactBrakeDistance = 0f;
                ExactRemaining = Math.Abs(targetX - ExactLastX);
                ExactStopReason = "";
                return;
            }
            if (kind == "jump")
            {
                BeginJump(until);
                if (dir == "left") LeftUntil = Now + Ticks(moveMs);
                else if (dir == "right") RightUntil = Now + Ticks(moveMs);
                return;
            }
            if (kind == "move")
            {
                if (tiles > 0f)
                {
                    MoveFromX = p.Center.X / 16f;
                    MoveFromY = p.Center.Y / 16f;
                    MoveStopTiles = tiles;
                    MoveStopAxis = (dir == "up" || dir == "down") ? 1 : 0;
                    LeftUntil = RightUntil = UpUntil = DownUntil = 0;
                    if (dir == "left") LeftUntil = ulong.MaxValue;
                    else if (dir == "right") RightUntil = ulong.MaxValue;
                    else if (dir == "up") UpUntil = ulong.MaxValue;
                    else if (dir == "down") DownUntil = ulong.MaxValue;
                }
                else if (dir == "left") { LeftUntil = until; RightUntil = 0; }
                else if (dir == "right") { RightUntil = until; LeftUntil = 0; }
                else if (dir == "up") { UpUntil = until; DownUntil = 0; }
                else if (dir == "down") { DownUntil = until; UpUntil = 0; }
            }
        }

        private static bool TickRopeRelease(Player p, ulong now)
        {
            if (!RopeReleasePending) return false;
            if (!OnRopeNow(p))
            {
                ActivateDeferredRopeAction(p);
                return false;
            }
            // 这里原来是"先按住下键脱绳，等 OnRopeNow 变假再执行跳跃"。
            // 实测证明这个设计是错的：按住下键时原版让角色**握着绳子往下滑**，
            // pulley 一直是 true，OnRopeNow 就一直是 true，于是永远等不到"脱离"，
            // 1.2 秒窗口耗尽后被判失败——而玩家看到的现象就是"按跳却一路滑到绳底"。
            // 正确做法是**在绳上直接按跳**：原版 PulleyMovement 看到跳跃输入会先把
            // pulley 解除再走正常起跳，这一帧不需要任何中间状态。
            ActivateDeferredRopeAction(p);
            return false;
        }


        /// <summary>
        /// Performs a real player-owned hit against a hostile NPC.  This uses
        /// Player.ApplyDamageToNPC so normal damage modifiers, hit hooks,
        /// combat text, loot and multiplayer synchronization still run.
        /// The command starts a short-lived loop; it must never block the
        /// Terraria main thread waiting for the attack to finish.
        /// </summary>
        internal static void ApplyAttack()
        {
            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead)
            {
                FinishAttack(false);
                return;
            }
            ulong now = Main.GameUpdateCount;
            if (AttackUntil <= now)
            {
                FinishAttack(false);
                return;
            }
            if (now < AttackNextTick) return;

            NPC target = null;
            if (AttackTarget >= 0 && AttackTarget < Main.npc.Length)
            {
                NPC candidate = Main.npc[AttackTarget];
                if (candidate != null && candidate.active && candidate.CanBeChasedBy())
                    target = candidate;
            }

            if (target == null && AttackAutoTarget)
            {
                int who = FindNearestHostile(p, AttackRadius);
                if (who >= 0)
                {
                    AttackTarget = who;
                    target = Main.npc[who];
                }
            }

            if (target == null)
            {
                // Nothing left to hit: keep the summary, then stop.
                FinishAttack(true);
                return;
            }

            float maxDistance = Math.Max(1, AttackRadius) * 16f;
            if (Vector2.Distance(p.Center, target.Center) > maxDistance)
            {
                if (AttackAutoTarget)
                {
                    int who = FindNearestHostile(p, AttackRadius);
                    if (who < 0) return;
                    AttackTarget = who;
                    target = Main.npc[who];
                }
                else
                {
                    FinishAttack(true);
                    return;
                }
            }

            Item weapon = p.HeldItem;
            if (weapon == null || weapon.damage <= 0)
            {
                FinishAttack(true);
                return;
            }

            int direction = target.Center.X >= p.Center.X ? 1 : -1;
            p.direction = direction;

            // A real swing cannot reach through a wall: without this the bridge is a
            // damage beam that ignores terrain and hitboxes.
            if (!Collision.CanHitLine(p.position, p.width, p.height, target.position, target.width, target.height))
            {
                AttackNextTick = now + (ulong)Math.Max(1, AttackIntervalTicks);
                AttackBlockedByTerrain = true;
                return;
            }

            // Keep the held weapon's swing state visible while the direct hit
            // below applies real player-owned damage.  SetDummyItemTime also
            // updates itemTimeMax, which the vanilla animation code expects.
            p.SetDummyItemTime(Math.Max(1, weapon.useAnimation));

            // Use the player's real damage pipeline (armor, accessories, buffs, prefixes)
            // instead of the bare item damage, and roll a real crit.
            int damage = (int)p.GetTotalDamage(weapon.DamageType).ApplyTo(weapon.damage);
            bool crit = Main.rand.NextFloat() < p.GetTotalCritChance(weapon.DamageType) / 100f;
            LastHitDamage = damage;

            int lifeBefore = target.life;
            p.ApplyDamageToNPC(target, damage, weapon.knockBack, direction, crit, weapon.DamageType, true);
            // Global invincibility frames mean a hit inside the i-frame window does no
            // damage; counting it would overstate the attack.
            if (target.life < lifeBefore) AttackHits++;
            AttackLastTarget = target.whoAmI;
            AttackLastLife = target.active ? target.life : 0;
            AttackName = target.FullName;
            AttackKilled = !target.active || target.life <= 0;
            AttackBlockedByTerrain = false;

            AttackNextTick = now + (ulong)Math.Max(1, AttackIntervalTicks);
        }

        /// <summary>Freeze the last attempt's result so it survives the end of the task.</summary>
        internal static void FinishAttack(bool hadTarget)
        {
            if (!AttackFinished && (AttackHits > 0 || AttackLastTarget >= 0))
            {
                AttackFinished = true;
                AttackFinishedTick = Main.GameUpdateCount;
                AttackFinishedCleanly = hadTarget;
            }
            AttackUntil = 0;
            AttackTarget = -1;
            AttackAutoTarget = false;
            AttackNextTick = 0;
        }

        internal static int FindNearestHostile(Player p, int radiusTiles)
        {
            if (p == null || !p.active) return -1;
            float maxDistance = Math.Max(1, radiusTiles) * 16f;
            float best = float.MaxValue;
            int bestWho = -1;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = Vector2.DistanceSquared(p.Center, n.Center);
                if (d <= maxDistance * maxDistance && d < best)
                {
                    best = d;
                    bestWho = i;
                }
            }
            return bestWho;
        }

        internal static void BeginAttack(int target, bool autoTarget, int radiusTiles, int durationMs, int intervalMs)
        {
            AttackTarget = target;
            AttackAutoTarget = autoTarget;
            AttackRadius = Math.Max(1, radiusTiles);
            AttackUntil = Now + Ticks(durationMs);
            AttackIntervalTicks = Math.Max(1, (int)Math.Round(intervalMs / 16.67));
            AttackNextTick = Now;
            AttackHits = 0;
            AttackLastTarget = -1;
            AttackLastLife = 0;
            AttackKilled = false;
            AttackBlockedByTerrain = false;
            AttackFinished = false;
            AttackFinishedTick = 0;
            AttackFinishedCleanly = false;
            LastHitDamage = 0;
            AttackName = target >= 0 && target < Main.npc.Length && Main.npc[target] != null ? Main.npc[target].FullName : "";
            AttackLifeBefore = target >= 0 && target < Main.npc.Length && Main.npc[target] != null ? Main.npc[target].life : 0;
        }

        internal static void ClearAttack()
        {
            AttackUntil = 0;
            AttackTarget = -1;
            AttackAutoTarget = false;
            AttackNextTick = 0;
        }

        internal static object AttackStatus() => new
        {
            active = AttackUntil > Now,
            target = AttackTarget,
            autoTarget = AttackAutoTarget,
            hits = AttackHits,
            lastTarget = AttackLastTarget,
            lastLife = AttackLastLife,
            untilTick = AttackUntil,
            blockedByTerrain = AttackBlockedByTerrain,
            // Survives stop() and the end of the task, so an attack can be verified
            // after the fact instead of showing zeros.
            last = AttackFinished ? new
            {
                target = AttackLastTarget,
                name = AttackName,
                hits = AttackHits,
                lifeBefore = AttackLifeBefore,
                lifeAfter = AttackLastLife,
                killed = AttackKilled,
                endedTick = AttackFinishedTick,
                endedByTargetGone = AttackFinishedCleanly,
            } : null,
        };

        public override void UpdateUI(Microsoft.Xna.Framework.GameTime gameTime) => DrainPending();

        /* ── 控制状态：给"按住走 N 毫秒"用，按 tick 存截止时间 ───────────── */

        // Written from the bridge thread, read from the main thread: keep the
        // published values volatile instead of relying on x64 store atomicity.
        internal static ulong LeftUntil, RightUntil, UpUntil, DownUntil, JumpUntil, UseUntil, UseTileUntil, InventoryUntil;
        // 坐骑键的时间窗。输出在 ApplyNativeControlFlags 里直写 Player.controlMount，
        // 不用 Triggers：TriggersSet.CopyInto 只在 Main.hasFocus 时运行，失焦下写
        // Triggers.Current.QuickMount 是空转（实测 mount:true / controlMount:false）。
        internal static ulong MountUntil;
        // 预开闸窗口：原版 Player.Update 里 releaseMount 唯一的置真处是
        //     else { releaseMount = true; }        // 条件是 controlMount == false
        // 而那一整段挂在 if (Main.hasFocus) 里。本环境常态失焦，releaseMount 就永远
        // 停在字段默认值 false → QuickMount() 永不进入 → 能骑上（骑上走 ItemCheck
        // 那条路）却下不来（反编译 Player.cs:2071 / 23778-23790 证实）。
        // 解法：真正按下之前先开几帧闸但不按，让引擎自己把 releaseMount 置真。
        // 窗口过后引擎每帧会用 IsActive 重新赋值 hasFocus，不残留。
        internal static ulong MountPreOpenUntil;
        // 背包键和坐骑是同一个病：原版处理 controlInv 的那段是
        //   if (controlInv) { if (releaseInventory) ToggleInv(); releaseInventory = false; }
        //   else { releaseInventory = true; }
        // 而整段挂在 if (Main.hasFocus) 里。失焦时 else 分支不执行，releaseInventory 永远是初值 false，
        // 于是我们写 controlInv=true 也进不去 ToggleInv（实测：key inventory 回 ok:true，playerInventory 恒 false）。
        // 预开闸这几帧只抬 hasFocus、不按键，把时间留给引擎执行 else 分支把 releaseInventory 置 true。
        internal static ulong InventoryPreOpenUntil;
        // 边沿闸门：原版 controlMount 是"刚按下"的复制，持续为真会每帧反复上下坐骑，
        // 所以一个时间窗只允许发一次 true。
        internal static bool MountFired;
        // 结果驱动：目标格一挖空就收手，不靠“猜要按多久”。不同方块硬度差很多（土 1 下、
        // 石头 3 下、更硬的要更多），任何固定时长都必然在某些方块上不够、在另一些上白按。
        internal static bool UseStopWhenClear;
        // 放置/敲墙同样按结果停：格子真的变成实体（放置）或真的空了（挖掘）才收手。
        internal static bool UseStopWhenPlaced;
        // 行走也按结果停：给的是"要走几格"，不是"按多久"。
        // 被墙挡住、脚下滑坡、水里慢，都会让同样的时长走出完全不同距离。
        internal static float MoveFromX, MoveFromY;
        internal static float MoveStopTiles;
        internal static int MoveStopAxis;   // 0=X, 1=Y
        // 攀爬：在 PostUpdateInput 中只按原生 Up/Down trigger，让原版绳索物理自己处理位移。
        // 不写 controlUp/controlDown 的最终结果，也不写 velocity、position 或 pulley。
        internal static ulong ClimbUpUntil, ClimbDownUntil, LayRopeUntil;
        internal static int ClimbTargetY;
        // 横移/跳跃在原版挂绳状态下会被有意忽略。命令层看到脚底是真绳时，
        // 先排队发一小段真实 Down 输入，等脚底离开绳子后再开始原动作。
        // 这里保存的是待执行的输入参数，不保存或制造任何物理结果。
        internal static bool RopeReleasePending, RopeReleaseFailed;
        internal static ulong RopeReleaseUntil, RopeReleaseFailureTick;
        private static string RopeDeferredKind = "";
        private static string RopeDeferredDir = "";
        private static int RopeDeferredMs, RopeDeferredMoveMs;
        private static float RopeDeferredTiles, RopeDeferredTargetX;
        // 抓钩：原版是 Player.controlHook（真人按住左键时由输入层置位）驱动的。
        // 之前只置 mouseLeft 没用——钩子根本不会飞出去（实测 grapCount 一直 0）。
        // 自动防守：真持武器、真瞄准、真松开，伤害全部由原版弹幕/挥击产生。
        // 用户的原话是"你一直在被怪打"——角色不能站着挨打。
        internal static ulong GuardUntil, GuardRadius = 14, GuardNextScan, GuardTargets;
        internal static int GuardLastTarget = -1, GuardWeaponSlot = -1;
        // 真挥了几刀：itemAnimation 从 >0 回到 0 就是"这一次挥击播完了"。
        internal static int GuardSwings, GuardPrevAnim, GuardSavedSlot = -1;
        // 默认关：用户明确要求删掉"自动打怪"这种会干扰实验的自动行为。
        // 需要打架就显式发 guard {ms}，它只在这段时间内生效，不会自己续命。
        internal static volatile bool GuardDefault = false;
        internal static bool GuardEngaged;
        internal static volatile bool HookHeld;
        // 钩子弹幕朝**鼠标世界坐标**飞（原版用 Main.MouseWorld 算方向），不是朝 tileTargetX/Y。
        // 之前只 SetAim 不改鼠标，光标还停在别处，钩子永远没有合法落点。
        internal static Vector2 HookAimWorld;
        // PlayerInput.SetZoom_MouseInWorld 在 PostUpdateInput 之后会把
        // `_originalMouseX/Y` 重新换算成 Main.mouseX/Y。抓钩若只写最终坐标，
        // 下一步会被物理鼠标覆盖；这里缓存原版私有字段，把目标反解回原始屏幕坐标。
        private static readonly FieldInfo OriginalMouseXField =
            typeof(PlayerInput).GetField("_originalMouseX", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo OriginalMouseYField =
            typeof(PlayerInput).GetField("_originalMouseY", BindingFlags.Static | BindingFlags.NonPublic);
        internal static volatile int DbgHookRawMouseX, DbgHookRawMouseY;
        internal static volatile int DbgHookFinalMouseX, DbgHookFinalMouseY;
        internal static volatile float DbgHookZoom;
        internal static volatile float DbgHookScreenX, DbgHookScreenY;
        // 诊断：Player.Update 跑完之后这些控制位到底是什么——抓钩逻辑看到的就是这份。
        internal static volatile bool DbgPostControlHook, DbgPostReleaseHook, DbgPostMouseLeft;
        internal static volatile bool DbgPostControlMount, DbgPostReleaseMount;
        internal static volatile int DbgProjHookCount;
        internal static ulong HookUntil;
        // 脱钩要走原版的 releaseHook 一帧标志（和 mouseLeftRelease 同类的机制）。
        // 只清 controlHook 不够：实测 grapCount 一直停在 1，角色被钉在原地。
        internal static volatile bool HookReleasePending, HookJustStarted;
        // NPC 对话：只保存目标与右键输入时间窗，NPC 选中、商店和台词全部由原版 HoverOverNPCs 结算。
        internal static volatile int TalkTargetNpc = -1;
        internal static Vector2 TalkAimWorld;
        internal static ulong TalkUntil;
        internal static volatile int TalkPhase; // 0=idle, 1=release/arm, 2=press
        internal static volatile bool DbgTalkMouseRight, DbgTalkChatRelease;
        internal static volatile int DbgTalkFinalMouseX, DbgTalkFinalMouseY;
        internal static volatile int DbgTalkRawMouseX, DbgTalkRawMouseY;
        internal static volatile float DbgTalkScreenX, DbgTalkScreenY, DbgTalkZoom;
        // ── talkoption：靠 npcChatFocus1..4 反推对话按钮矩形，再发一次真实左键 ──
        // 原版每帧绘制对话按钮时读 Main.mouseX/mouseY 判断悬停，把结果写进 npcChatFocusN。
        // 所以「设一个点 → 下一帧读焦点位」是唯一可行的探测方式：一帧只能测一个点。
        internal static volatile int TalkOptIndex;      // 0=idle, 1..4=目标按钮序号
        internal static volatile int TalkOptPhase;      // 1=扫描 2=释放 3=按下 4=点击后释放 5=失败 6=输入已发送
        internal static volatile int TalkOptX, TalkOptY;
        internal static volatile int TalkOptHitX = -1, TalkOptHitY = -1;
        internal static ulong TalkOptStart;
        internal static volatile int TalkOptScanned;
        internal static volatile int TalkOptBlocked;
        internal static volatile bool TalkOptLastIgnoreMouseInterface;
        internal static volatile bool TalkOptLastUsingGamepad;
        internal static volatile int TalkOptLastItemAnimation = -1;
        internal static volatile int TalkOptLastMouseX, TalkOptLastMouseY;
        internal static volatile string TalkOptMsg = "";
        // talkfocus 的单帧探针：>=0 表示这一帧要把鼠标写到这里，写完立刻置 -1
        internal static volatile int ProbeMouseX = -1, ProbeMouseY = -1;
        internal static ulong AttackUntil, AttackNextTick;
        internal static volatile int AttackTarget = -1, AttackRadius = 40, AttackIntervalTicks = 10;
        internal static volatile bool AttackAutoTarget, AttackFinished, AttackFinishedCleanly, AttackKilled, AttackBlockedByTerrain, JumpIssued;
        internal static volatile int AttackHits, AttackLastTarget = -1, AttackLastLife, AttackLifeBefore, LastHitDamage;
        internal static ulong AttackFinishedTick;
        internal static volatile string AttackName = "";

        public static void ApplyControls(Player player)
        {
            ulong now = Main.GameUpdateCount;

            // 换世界 / 回菜单 / 死亡：把上一次留下的所有"会自己动"的状态清干净。
            // 静态字段跨世界不会归零，这正是"角色一直往右走、一直自动打怪"的来源。
            if (Main.gameMenu)
            {
                if (KnownWorldId != -2) { ResetMotion("menu"); KnownWorldId = -2; }
                return;
            }
            if (KnownWorldId != Main.worldID)
            {
                int id = Main.worldID;
                KnownWorldId = id;
                ResetMotion("world#" + id);
            }
            if (player != null && player.active && player.dead && GuardUntil > 0)
                ResetMotion("death");

            // Movement and jump are emitted from PostUpdateInput as native triggers.
            // Item/tile use is emitted in PreItemCheck, the phase vanilla ItemCheck reads.
        }

    /// <summary>
    /// 贴在绳子上吗？原版把"附近有绳子"记在 Player.ropeCount 里；这里同时兜一层
    /// 直接查方块，避免 ropeCount 还没刷新的那一帧判错。
    /// </summary>
    internal static bool OnRopeNow(Player p)
    {
        if (p == null) return false;
        // `ropeCount` and a rope tile anywhere in the body are only contact
        // diagnostics.  Vanilla ignores horizontal movement/jump while the
        // player's feet are on a rope, so the command layer must use the same
        // foot-cell test.  A rope crossing the torso while the player stands on
        // soil must not be reported as "on rope".
        int tx = (int)((p.position.X + p.width * 0.5f) / 16f);
        int feetY = (int)((p.position.Y + p.height) / 16f);
        return IsRopeTile(tx, feetY);
    }

    /// <summary>Checks rope tiles intersecting the player's current hitbox without using ropeCount.</summary>
    internal static bool HasRopeTileInPlayerBody(Player p)
    {
        if (p == null) return false;
        int left = (int)MathF.Floor((p.position.X + 1f) / 16f);
        int right = (int)MathF.Floor((p.position.X + p.width - 1f) / 16f);
        int top = (int)MathF.Floor((p.position.Y + 1f) / 16f);
        int bottom = (int)MathF.Floor((p.position.Y + p.height - 1f) / 16f);
        for (int x = left; x <= right; x++)
            for (int y = top; y <= bottom; y++)
                if (IsRopeTile(x, y)) return true;
        return false;
    }

    /// <summary>
    /// 到达 climb 命令指定的目标格后，释放方向输入。
    /// </summary>
    internal static void UpdateClimbTarget(Player p)
    {
        if (p == null) return;
        if (ClimbTargetY > 0)
        {
            int py = (int)(p.Center.Y / 16f);
            bool up = ClimbUpUntil > Now;
            if (up && py <= ClimbTargetY) { ClimbUpUntil = 0; }
            if (!up && py >= ClimbTargetY) { ClimbDownUntil = 0; }
        }
    }

    internal static bool IsRopeTile(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return false;
        Tile t = Main.tile[x, y];
        if (t == null || !t.HasTile) return false;
        int type = t.TileType;
        return type == 213 || type == 353 || type == 365 || type == 366 || type == 504;
    }

    internal static void ClearControls()
    {
        LeftUntil = RightUntil = UpUntil = DownUntil = JumpUntil = UseUntil = UseTileUntil = InventoryUntil = 0;
        ClimbUpUntil = ClimbDownUntil = LayRopeUntil = 0; ClimbTargetY = 0;
        IntentLeft = IntentRight = IntentUp = IntentDown = IntentJump = false;
        BridgeJumpHeld = BridgeLeftHeld = BridgeRightHeld = BridgeUpHeld = BridgeDownHeld = BridgeInventoryHeld = BridgeMountHeld = false;
        // 坐骑边沿闸门也要清：否则 ClearControls 之后再 mount 会被上一轮的 MountFired 拦住。
        MountFired = false;
        RopeReleasePending = RopeReleaseFailed = false;
        RopeReleaseUntil = RopeReleaseFailureTick = 0;
        RopeDeferredKind = RopeDeferredDir = "";
        RopeDeferredMs = RopeDeferredMoveMs = 0;
        RopeDeferredTiles = RopeDeferredTargetX = 0f;
        // 抓钩也算"会自己动"的状态：不清的话松手后角色还被挂在钩上。
        HookHeld = false; HookUntil = 0; HookReleasePending = true;
        TalkTargetNpc = -1; TalkUntil = 0; TalkPhase = 0;
        DbgTalkMouseRight = false; DbgTalkChatRelease = false;
        ClearAttack();
    }

    /// <summary>
    /// 把所有"会自己动"的状态一次清干净。
    /// 静态字段在换世界/重开客户端时不会自动归零，之前就是靠这个静默地一直往前走、
    /// 一直自动打怪——用户在旁边看着像"有两个脚本在干扰"。换世界和死亡时必须清。
    /// </summary>
    internal static void ResetMotion(string why)
    {
        ClearControls();
        StopPath("reset:" + why);
        StopWalk("reset:" + why);
        FollowActive = false;
        Travel.Active = false;
        GuardUntil = 0;
        GuardEngaged = false;
        GuardTargets = 0;
        HookHeld = false;
        HookUntil = 0;
        HookReleasePending = false;
        LastResetReason = why;
        ResetCount++;
    }

    internal static volatile string LastResetReason = "";
    internal static int ResetCount;
    internal static int KnownWorldId = -1;

        /// <summary>
        /// 自动防守：每 tick 找最近的敌对 NPC，视线可达就换武器、瞄准它、按住使用键。
        /// 伤害由原版挥击/弹幕产生（这里只管"手怎么动"，不直接扣血、不动世界）。
        /// </summary>
        private static void ApplyGuard(Player p, ulong now)
        {
            // 手上有明确的活（挖/放）或者正被手动方向键驱动时不抢手：
            // 否则 guard 每帧把 controlLeft/Right 写回 false，手动 move/right/left 全部失效。
            if (UseUntil > now || UseTileUntil > now) return;
            if (LeftUntil > now || RightUntil > now || UpUntil > now || DownUntil > now) return;
            // 不再自动续期：guard 只在命令给的时长内有效（用户要求删掉自动打怪）。
            if (GuardUntil <= now)
            {
                if (GuardEngaged) { GuardEngaged = false; GuardLastTarget = -1; }
                GuardPrevAnim = 0;
                return;
            }
            if (GuardPrevAnim > 0 && p.itemAnimation == 0) GuardSwings++;
            GuardPrevAnim = p.itemAnimation;
            if (now < GuardNextScan)
            {
                if (GuardEngaged && GuardLastTarget >= 0 && GuardLastTarget < Main.npc.Length)
                {
                    NPC hold = Main.npc[GuardLastTarget];
                    if (hold != null && hold.active && hold.CanBeChasedBy())
                    {
                        SetAim((int)(hold.Center.X / 16f), (int)(hold.Center.Y / 16f));
                        Main.mouseLeft = true;
                        p.controlUseItem = true;
                        p.direction = hold.Center.X >= p.Center.X ? 1 : -1;
                    }
                }
                return;
            }
            GuardNextScan = now + 6;

            NPC best = null;
            float bestDist = GuardRadius * 16f;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = Vector2.Distance(p.Center, n.Center);
                if (d > bestDist) continue;
                if (!Collision.CanHitLine(p.position, p.width, p.height, n.position, n.width, n.height)) continue;
                best = n; bestDist = d;
            }
            if (best == null)
            {
                if (GuardEngaged)
                {
                    GuardEngaged = false; GuardLastTarget = -1;
                    // 打完了换回原来的格子，别把正在建房子的方块/镐子顶掉
                    if (GuardSavedSlot >= 0 && GuardSavedSlot < p.inventory.Length
                        && p.inventory[GuardSavedSlot].type > 0) p.selectedItem = GuardSavedSlot;
                    GuardSavedSlot = -1;
                }
                return;
            }
            GuardEngaged = true;
            GuardLastTarget = best.whoAmI;
            GuardTargets++;
            int slot = PickWeaponSlot(p);
            if (slot >= 0 && slot != p.selectedItem)
            {
                if (!GuardEngaged) GuardSavedSlot = p.selectedItem;
                p.selectedItem = slot;
            }
            SetAim((int)(best.Center.X / 16f), (int)(best.Center.Y / 16f));
            p.direction = best.Center.X >= p.Center.X ? 1 : -1;
            Main.mouseLeft = true;
            p.controlUseItem = true;
        }

        // 手动动作开始时，取消仍在后台运行的寻路/旅行/精确移动任务。
        // 否则旧任务会在 ApplyMovement 的提前返回分支里吞掉新的 Up/Down 意图。
        internal static void StopAutomatedMovement(string reason)
        {
            // 手动动作或新的明确命令应终止跟随，避免 FollowActive 在更早的
            // 分支里重新覆盖用户刚发出的方向/跳跃输入。
            if (FollowActive) ClearFollow();
            StopPath(reason);
            StopWalk(reason);
            Travel.Active = false;
            ExactActive = false;
            MoveStopTiles = 0f;
            ExactPhase = 0;
            ExactStuckTicks = 0;
            ExactStopReason = reason ?? "stopped";
        }

        /// <summary>优先有弹药的远程武器，其次近战；都不行就用当前手持。</summary>
        private static int PickWeaponSlot(Player p)
        {
            int melee = -1;
            for (int i = 0; i < 10; i++)
            {
                Item it = p.inventory[i];
                if (it == null || it.type <= 0 || it.stack <= 0 || it.damage <= 0) continue;
                if (it.shoot > 0 && it.ammo == 0 && it.mana == 0 && HasAmmoFor(p, it)) { GuardWeaponSlot = i; return i; }
                if (melee < 0 && it.useStyle > 0 && it.ammo == 0) melee = i;
            }
            GuardWeaponSlot = melee >= 0 ? melee : GuardWeaponSlot;
            return GuardWeaponSlot;
        }

        private static bool HasAmmoFor(Player p, Item weapon)
        {
            for (int i = 0; i < p.inventory.Length; i++)
            {
                Item a = p.inventory[i];
                if (a != null && a.ammo > 0 && a.stack > 0 && ItemLoader.CanChooseAmmo(weapon, a, p)) return true;
            }
            return false;
        }

        internal static ulong Now => Main.GameUpdateCount;
        internal static ulong Ticks(int ms) => (ulong)Math.Max(1, ms / 16.67);
    }

    public class DshControlPlayer : ModPlayer
    {
        public override void PreUpdate()
        {
            // 坐骑快捷召唤的输入闸门。原版 Player.Update 的顺序是
            //   ResetControls → if (Main.hasFocus) { TriggersSet.CopyInto(this) } → 处理 controlMount
            // 失焦时第二段整段跳过，往 Triggers 写 QuickMount 一个字节都到不了 controlMount；
            // 而晚阶段直写 controlMount 又晚于它的读点。唯一能穿过去的窗口就在这两行之间：
            // 本钩子（PreUpdate）在 ResetControls 之前跑，所以在这里把闸门临时置真，
            // 让原版自己的 CopyInto 干活。只改"输入闸门开不开"，不碰任何物理量。
            BridgeSystem.OpenMountInputGate();
            BridgeSystem.ApplyAim();
            BridgeSystem.ApplyControls(Player);
            // Keep the proven PreUpdate path as well as the SetControls path.  In this
            // multiplayer build the hook order differs between the local and host player;
            // both hooks are input flags only, so whichever is last before Player.Update wins.
            BridgeSystem.ApplyMovement();
            BridgeSystem.ApplyNativeControlFlags(Player);
            // 诊断：ModPlayer.PreUpdate 返回时、原版 ResetControls 尚未开始前的输入快照。
            BridgeSystem.DbgPreTick = (long)Main.GameUpdateCount;
            BridgeSystem.DbgPreCtrlLeft = Player.controlLeft;
            BridgeSystem.DbgPreCtrlRight = Player.controlRight;
            BridgeSystem.DbgPreCtrlUp = Player.controlUp;
            BridgeSystem.DbgPreCtrlDown = Player.controlDown;
            BridgeSystem.DbgPreCtrlJump = Player.controlJump;
            BridgeSystem.DbgPreVelX = Player.velocity.X;
        }

        // 真正逐帧生效的地方：物品使用在 ItemCheck 前，移动输入在 PostUpdateInput。
        /// <summary>
        /// Runs immediately before the vanilla item check. Everything written here is what
        /// ItemCheck actually reads - writing it in PreUpdate/PostUpdate is overwritten by
        /// the physical mouse before ItemCheck runs (measured: itemAnimation stayed 0).
        /// This is what lets the agent mine and place while the window is unfocused, with
        /// no OS input injection and no cursor movement.
        /// </summary>
        public override bool PreItemCheck()
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                BridgeSystem.PreItemCalls++;
                BridgeSystem.ApplySyntheticInput();
            }
            // TRUE = let the vanilla item check run. Returning false CANCELS ItemCheck_Inner,
            // which silently disabled every item use in the game (no swings, no wood drops).
            return true;
        }

        /// <summary>
        /// tModLoader calls this after the vanilla input has been read and before the player
        /// acts on it, which is the documented place to override control flags.
        /// </summary>
        public override void SetControls()
        {
            if (Player.whoAmI != Main.myPlayer) return;
            BridgeSystem.SetControlCalls++;
            BridgeSystem.ApplySyntheticInput();
            // This is the effective output path in this tModLoader build.  Triggers.Current
            // is useful for diagnostics, but it does not populate Player.control* here.
            BridgeSystem.ApplyMovement();
            BridgeSystem.ApplyNativeControlFlags(Player);
        }

        /// <summary>
        /// The remaining vanilla consumers read their control flags after SetControls:
        /// rope movement, QuickGrapple, and ItemCheck.  Re-apply only input flags here,
        /// immediately before those consumers run.  This does not write velocity,
        /// position, pulley, projectiles, or tiles.
        /// </summary>
        public override void PostUpdateMiscEffects()
        {
            if (Player.whoAmI != Main.myPlayer) return;
            BridgeSystem.ApplyNativeControlFlags(Player);
            BridgeSystem.ApplySyntheticInput();
            BridgeSystem.DbgPrePhysicsTick = (long)Main.GameUpdateCount;
            BridgeSystem.DbgPrePhysicsUp = Player.controlUp;
            BridgeSystem.DbgPrePhysicsDown = Player.controlDown;
            BridgeSystem.DbgPrePhysicsHook = Player.controlHook;
            BridgeSystem.DbgPrePhysicsMount = Player.controlMount;
            BridgeSystem.DbgPrePhysicsReleaseMount = Player.releaseMount;
            BridgeSystem.DbgPrePhysicsUseItem = Player.controlUseItem;
            BridgeSystem.DbgPrePhysicsUseTile = Player.controlUseTile;
        }

        public override void PostUpdateRunSpeeds()
        {
            if (Player.whoAmI != Main.myPlayer) return;
            // In this tModLoader build vanilla performs additional input normalization
            // after SetControls and immediately before HorizontalMovement.  Re-apply the
            // same input flags at this last input-only hook so Player.Update consumes the
            // bridge command in the same frame.  No velocity/position/pulley is touched.
            BridgeSystem.ApplyNativeControlFlags(Player);
            // 诊断：原版算完重力后、位置积分前，把关键状态留一份给 state 回读。
            BridgeSystem.DbgPulley = Player.pulley; BridgeSystem.DbgVelY = Player.velocity.Y; BridgeSystem.DbgPlayerControlUp = Player.controlUp;
            BridgeSystem.DbgRopeTile = BridgeSystem.HasRopeTileInPlayerBody(Player);
            BridgeSystem.DbgInputControlTick = (long)Main.GameUpdateCount;
            BridgeSystem.DbgInputControlPlayer = Player.whoAmI;
            BridgeSystem.DbgInputControlLeft = Player.controlLeft;
            BridgeSystem.DbgInputControlRight = Player.controlRight;
            BridgeSystem.DbgInputControlUp = Player.controlUp;
            BridgeSystem.DbgInputControlDown = Player.controlDown;
            BridgeSystem.DbgInputControlJump = Player.controlJump;
        }

        public override void PostUpdate()
        {
            // ModPlayer hooks run for every player; without this guard the local player
            // would be advanced once per player slot in multiplayer.
            if (Player.whoAmI != Main.myPlayer) return;
            BridgeSystem.ApplyAttack();
            // 诊断：Update 跑完之后这些控制位是什么——抓钩逻辑看到的正是这份
            BridgeSystem.DbgPostControlHook = Player.controlHook;
            BridgeSystem.DbgPostControlMount = Player.controlMount;
            BridgeSystem.DbgPostReleaseMount = Player.releaseMount;
            BridgeSystem.DbgPostReleaseHook = Player.releaseHook;
            // 诊断：Player.Update 全部跑完之后，引擎最终采信的输入是什么。
            // 这一份才是「我写的值有没有被采纳」的唯一证据（ApplyNativeControlFlags 里的快照太早）。
            var _t = PlayerInput.Triggers.Current;
            BridgeSystem.DbgAfterTick = (long)Main.GameUpdateCount;
            BridgeSystem.DbgAfterCtrlLeft = Player.controlLeft;
            BridgeSystem.DbgAfterCtrlRight = Player.controlRight;
            BridgeSystem.DbgAfterCtrlUp = Player.controlUp;
            BridgeSystem.DbgAfterCtrlDown = Player.controlDown;
            BridgeSystem.DbgAfterCtrlJump = Player.controlJump;
            BridgeSystem.DbgAfterTrigLeft = _t.Left;
            BridgeSystem.DbgAfterTrigRight = _t.Right;
            BridgeSystem.DbgAfterTrigUp = _t.Up;
            BridgeSystem.DbgAfterTrigDown = _t.Down;
            BridgeSystem.DbgAfterTrigJump = _t.Jump;
            BridgeSystem.DbgAfterVelX = Player.velocity.X;
            BridgeSystem.DbgPostMouseLeft = Main.mouseLeft;
            int hookProj = 0;
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr != null && pr.active && pr.owner == Player.whoAmI && pr.type > 0 && pr.type < Main.projHook.Length && Main.projHook[pr.type]) hookProj++;
            }
            BridgeSystem.DbgProjHookCount = hookProj;
        }
    }
}
