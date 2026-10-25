using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
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
        public const int Port = 47777;

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
                    if (!Main.gameMenu) Main.hasFocus = true;
                }
                catch { /* game not ready */ }
                Thread.Sleep(8);
            }
        }

        public override void PostUpdateEverything()
        {
            TickMining();
            TickTravel();
            DrainPending();
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
        internal static bool PathAllowDig = true;
        private static ulong PathNextAct;

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
                if (!TileAir(x, y + k)) return true;
            return false;
        }

        internal static List<(int x, int y)> ComputePath(int sx, int sy, int tx, int ty, out string why)
        {
            why = "";
            int x0 = Math.Max(1, Math.Min(sx, tx) - 10), x1 = Math.Min(Main.maxTilesX - 2, Math.Max(sx, tx) + 10);
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
                if (!TileAir(x, y)) return PathAllowDig ? 8f : float.MaxValue;   // 要挖（禁挖时不可通行）
                return FloorWithin(x, y, 2) ? 1f : 20f;        // 有地=1；悬空=20（代价高到会主动绕开，只有别无选择才用）
            }

            int si = sx - x0, sj = sy - y0, ti = tx - x0, tj = ty - y0;
            if (si < 0 || sj < 0 || si >= w || sj >= h || ti < 0 || tj < 0 || ti >= w || tj >= h) { why = "起点/终点在包围盒外"; return null; }

            var open = new List<(int i, int j)>();
            cost[si, sj] = 0;
            open.Add((si, sj));
            int guard = 0;
            while (open.Count > 0 && guard++ < 200000)
            {
                int bi = 0; float best = float.MaxValue;
                for (int k = 0; k < open.Count; k++)
                {
                    float f = cost[open[k].i, open[k].j] + Math.Abs(open[k].i - ti) + Math.Abs(open[k].j - tj);
                    if (f < best) { best = f; bi = k; }
                }
                var (ci, cj) = open[bi];
                open.RemoveAt(bi);
                if (ci == ti && cj == tj) break;
                if (done[ci, cj]) continue;
                done[ci, cj] = true;

                foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int ni = ci + di, nj = cj + dj;
                    if (ni < 0 || nj < 0 || ni >= w || nj >= h) continue;
                    float sc = StepCost(x0 + ni, y0 + nj);
                    if (sc >= float.MaxValue) continue;
                    float nc = cost[ci, cj] + sc;
                    if (nc < cost[ni, nj]) { cost[ni, nj] = nc; prev[ni, nj] = (ci, cj); open.Add((ni, nj)); }
                }
            }
            if (cost[ti, tj] >= float.MaxValue) { why = "没有可行路径"; return null; }

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
            if (p.wet) { p.velocity.Y = Math.Min(p.velocity.Y, -3.0f); return; }
            if (Now < PathNextAct) return;

            int px = (int)(p.Center.X / 16f), py = (int)(p.Center.Y / 16f);
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
                if (PathAllowDig) for (int k = -2; k <= 1; k++) if (!TileAir(px + sign, py + k)) WalkMine(px + sign, py + k);
                if (TileAir(px + sign, py + 2) && TileAir(px + sign, py + 3)) PlaceWalkBlock(px + sign, py + 2);
                if (TileAir(px + sign, py + 2)) WalkJump(2.4f);      // 台阶就跳
                p.controlLeft |= sign < 0;
                p.controlRight |= sign > 0;
                p.velocity.X = sign * 2.9f;
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
                if (PathAllowDig && !TileAir(px, py + 2)) WalkMine(px, py + 2);
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
            p.selectedItem = slot;
            if (WorldGen.PlaceTile(x, y, p.inventory[slot].createTile, false, false, -1, p.whoAmI))
            {
                p.inventory[slot].stack--;
                if (p.inventory[slot].stack <= 0) p.inventory[slot].SetDefaults(0);
                return true;
            }
            return false;
        }

        /* ── walkto：朝目标坐标自己走 ─────────────────────────────────────
         * 之前只有 move(方向,时长)，代理只能一味朝一个方向推，掉洞、卡墙、淹死全靠运气。
         * 这里做成逐帧的闭环：算方向、遇墙挖、遇坑跳、泡水就上浮、碰到岩浆或死亡就停。
         */
        internal static bool WalkActive;
        internal static int WalkX, WalkY, WalkTol = 1;
        internal static string WalkStop = "";
        internal static ulong WalkDeadline;
        private static ulong WalkNextJump, WalkNextMine;
        private static int WalkLastPx, WalkStuck;

        internal static void BeginWalk(int x, int y, int tol, ulong until)
        {
            WalkActive = true; WalkX = x; WalkY = y;
            WalkTol = Math.Max(0, tol); WalkDeadline = until;
            WalkStop = ""; WalkNextJump = 0; WalkNextMine = 0; WalkStuck = 0; WalkLastPx = -99999;
        }

        internal static void StopWalk(string reason)
        {
            if (!WalkActive) return;
            WalkActive = false; WalkStop = reason;
            Player p = Main.LocalPlayer;
            if (p != null) { p.controlLeft = false; p.controlRight = false; p.controlJump = false; }
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
            float g = p.gravity > 0.05f ? p.gravity : 0.4f;
            p.velocity.Y = -(float)Math.Sqrt(2.0 * g * tiles * 16.0);
            p.controlJump = true;
            WalkNextJump = Now + 6;
        }

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
            p.selectedItem = slot;
            if (Main.tileAxe[t.TileType]) WorldGen.KillTile(x, y, false, false, false);
            else p.PickTile(x, y, p.inventory[slot].pick);
            p.SetItemAnimation(Math.Max(1, p.inventory[slot].useAnimation));
            p.SetItemTime(Math.Max(1, p.inventory[slot].useTime));
            p.direction = x * 16f >= p.Center.X ? 1 : -1;
            WalkNextMine = Now + 7;
        }

        private static void TickWalk()
        {
            if (!WalkActive) return;
            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead) { StopWalk("dead"); return; }
            if (Now > WalkDeadline) { StopWalk("timeout"); return; }
            if (p.lavaWet) { StopWalk("lava"); return; }
            if (p.wet) { p.velocity.Y = Math.Min(p.velocity.Y, -3.0f); return; }

            int px = (int)(p.Center.X / 16f), py = (int)(p.Center.Y / 16f);
            int dx = WalkX - px, dy = WalkY - py;
            if (Math.Abs(dx) <= WalkTol && Math.Abs(dy) <= WalkTol) { StopWalk("arrived"); return; }
            // 向下挖时容易一掉就冲过头（目标 y=347 却掉到 y=406）：超过目标就停
            if (py > WalkY + 2) { StopWalk("overshoot"); return; }

            int sign = Math.Sign(dx);
            if (sign == 0) return;

            // 卡住自救：位置连续多帧几乎不动，就把前方 3 行挖开再跳
            if (Math.Abs(px - WalkLastPx) <= 0) WalkStuck++;
            else { WalkStuck = 0; WalkLastPx = px; }
            if (WalkStuck > 10)
            {
                for (int dy2 = -1; dy2 <= 1; dy2++)
                    if (TileSolid(px + sign, py + dy2)) WalkMine(px + sign, py + dy2);
                WalkJump(3.2f);
                WalkStuck = 0;
            }

            // 悬崖保护：前方 4 格内没有地面就不要往前冲（以前会直接掉几十格）
            int floorDepth = 0;
            bool floorFound = false;
            for (int k = 1; k <= 4; k++)
            {
                if (TileSolid(px + sign, py + k)) { floorFound = true; floorDepth = k; break; }
            }
            if (!floorFound)
            {
                // 前方是深坑：停在原地，把决定权交回调用方（不要盲跳）
                StopWalk("cliff");
                return;
            }
            bool gapAhead = floorDepth >= 2;

            bool bodyBlocked = TileSolid(px + sign, py);
            bool headBlocked = TileSolid(px + sign, py - 1);
            bool stepBlocked = TileSolid(px + sign, py + 1);

            if (headBlocked) WalkMine(px + sign, py - 1);
            else if (bodyBlocked)
            {
                if (stepBlocked) WalkMine(px + sign, py);       // 一堵墙，挖
                else WalkJump(2.6f);                            // 只有台阶，跳
            }
            if (gapAhead) WalkJump(2.6f);                        // 1-3 格的小缺口，跳

            p.controlLeft |= sign < 0;
            p.controlRight |= sign > 0;
            p.velocity.X = sign * 2.9f;
            p.direction = sign;
            if (dy < -1) { p.controlUp = true; WalkJump(2.8f); }
            else if (dy > 2)
            {
                // 目标在下面：挖脚下的方块往下走
                if (TileSolid(px, py + 2)) WalkMine(px, py + 2);
            }
        }

        /// <summary>
        /// 移动兜底：control 标志在 Player.Update 里会被真实按键状态覆盖，
        /// 所以除设置标志外，每帧直接把速度推向目标方向（碰撞/重力仍由游戏解算）。
        /// </summary>
        internal static void ApplyMovement()
        {
            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead) return;
            ulong now = Main.GameUpdateCount;

            if (PathActive) { TickPath(); return; }
            if (WalkActive) { TickWalk(); return; }

            float dir = 0f;
            if (LeftUntil > now) dir -= 1f;
            if (RightUntil > now) dir += 1f;
            if (dir != 0f)
            {
                p.controlLeft |= LeftUntil > now;
                p.controlRight |= RightUntil > now;
                p.velocity.X = dir * 2.8f;
            }

            if (UpUntil > now) { p.controlUp = true; p.velocity.Y = Math.Min(p.velocity.Y, -2.4f); }
            if (DownUntil > now) { p.controlDown = true; p.velocity.Y = Math.Max(p.velocity.Y, 1.2f); }
            if (JumpUntil > now)
            {
                p.controlJump = true;
                // One impulse per command, and only from the ground. The old code
                // re-impulsed at every apex, which turned a 2 s jump into an
                // unpowered climb; wings and auto-jump are left to vanilla.
                if (!JumpIssued && p.velocity.Y == 0f && p.oldVelocity.Y == 0f)
                {
                    p.velocity.Y = JumpImpulse;
                    JumpIssued = true;
                }
            }
            if (UseUntil > now) p.controlUseItem = true;
            if (UseTileUntil > now) p.controlUseTile = true;
            // Drop the aim once the action is over, so a stale target does not steer the
            // next thing the player does.
            if (AimUntil != 0 && now > AimUntil) { AimTileX = -1; AimTileY = -1; AimUntil = 0; }
        }

        /* ── 拟人瞄准：把游戏内光标指到某个方块 ───────────────────────────
         * Terraria 每帧都会从物理鼠标重算 Main.mouseX/MouseWorld，所以瞄准必须
         * 在 Player.Update 之前（ModPlayer.PreUpdate）持续写入，否则会被覆盖。
         * 这样物品使用（挖掘/放置/攻击）走的就是原版逻辑：工具、动画、冷却、耗材。
         */
        internal static volatile int AimTileX = -1, AimTileY = -1;
        internal static ulong AimUntil;
        internal static volatile bool UseJustStarted, UseTileJustStarted;
        internal static long PreItemCalls, SetControlCalls, SynthCalls;

        internal static void SetAim(int x, int y) { AimTileX = x; AimTileY = y; }
        internal static void ClearAim() { AimTileX = -1; AimTileY = -1; }

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
            Main.mouseX = (int)screenX;
            Main.mouseY = (int)screenY;
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
                if (UseTileJustStarted) { Main.mouseRightRelease = true; UseTileJustStarted = false; }
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

        /* ── 真实输入注入 ────────────────────────────────────────────────
         * Terraria 每帧用物理鼠标状态覆盖玩家的控制标记，所以在 ModPlayer 钩子里写
         * controlUseItem / Main.mouseLeft 会被冲掉（实测 itemAnimation 恒为 0）。
         * 唯一可靠的办法就是走操作系统那条路：把光标移到目标格，按下、等、松开。
         * 这也正是"像玩家一样操作"：游戏自己的挖掘/放置/消耗/掉落逻辑全部生效。
         * 注入必须在后台线程做——命令本身跑在主线程，睡在那里会卡住整个游戏。
         */
        [StructLayout(LayoutKind.Sequential)] private struct MOUSEPOS { public int X, Y; }

        [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out MOUSEPOS p);
        [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, IntPtr extra);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref MOUSEPOS p);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern void keybd_event(byte vk, byte scan, uint flags, IntPtr extra);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr pid);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassNameW(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int pid);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// The visible FNA window of THIS process. Main.instance.Window.Handle is not the
        /// window Windows reports as foreground, so comparing against it always failed.
        /// </summary>
        private static IntPtr FindOwnWindow()
        {
            int myPid = System.Diagnostics.Process.GetCurrentProcess().Id;
            IntPtr found = IntPtr.Zero;
            EnumWindows((IntPtr h, IntPtr l) =>
            {
                if (!IsWindowVisible(h)) return true;
                var name = new StringBuilder(256);
                GetClassNameW(h, name, 256);
                if (name.ToString() != "SDL_app") return true;
                GetWindowThreadProcessId(h, out int pid);
                if (pid != myPid) return true;
                found = h;
                return false;
            }, IntPtr.Zero);
            return found;
        }

        /// <summary>Is the foreground window ours? Compare process ids, not handles.</summary>
        private static bool OwnProcessIsForeground()
        {
            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            GetWindowThreadProcessId(foreground, out int pid);
            return pid == System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;


        /// <summary>
        /// Windows only lets the foreground process take focus, so a background thread's
        /// SetForegroundWindow is refused. Two documented ways round it: send an input event
        /// first (that grants the sender foreground rights), and AttachThreadInput to the
        /// current foreground thread. Try both, then verify - we never click without focus.
        /// </summary>
        private static bool WaitForForeground(IntPtr hwnd, int timeoutMs)
        {
            int start = Environment.TickCount;
            while (Environment.TickCount - start < timeoutMs)
            {
                if (OwnProcessIsForeground()) return true;
                Thread.Sleep(20);
            }
            return OwnProcessIsForeground();
        }

        private static bool TryFocusGame(IntPtr hwnd)
        {
            if (OwnProcessIsForeground()) return true;

            ShowWindow(hwnd, 9);                       // un-minimise
            Thread.Sleep(40);

            // 1) tap Alt so this process owns the last input event, then ask for the window
            keybd_event(0x12, 0, 0, IntPtr.Zero);
            Thread.Sleep(15);
            keybd_event(0x12, 0, 2, IntPtr.Zero);
            Thread.Sleep(40);
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
            if (WaitForForeground(hwnd, 300)) return true;

            // 2) attach to the current foreground thread and try again
            IntPtr foreground = GetForegroundWindow();
            uint foregroundThread = GetWindowThreadProcessId(foreground, IntPtr.Zero);
            uint myThread = GetCurrentThreadId();
            bool attached = foregroundThread != 0 && AttachThreadInput(myThread, foregroundThread, true);
            try
            {
                BringWindowToTop(hwnd);
                SetForegroundWindow(hwnd);
            }
            finally
            {
                if (attached) AttachThreadInput(myThread, foregroundThread, false);
            }
            return WaitForForeground(hwnd, 400);
        }

        /// <summary>OS-level click on a tile, mirroring what a human does with the mouse.</summary>
        internal static void InjectClick(int tileX, int tileY, bool right, int ms)
        {
            try
            {
                IntPtr hwnd = FindOwnWindow();
                if (hwnd == IntPtr.Zero) hwnd = Main.instance.Window.Handle;
                TryFocusGame(hwnd);
                // Safety: if the game still is not the foreground window, do NOT click.
                // The cursor would land on whatever window is actually there (the user's
                // editor, the chat...) and we would be clicking their work.
                if (!OwnProcessIsForeground())
                {
                    LastClickError = "游戏没有拿到前台，为避免误点你正在用的窗口，这次操作已取消";
                    modRef?.Logger.Warn("[dsh] injection skipped: game is not the foreground window");
                    return;
                }

                MOUSEPOS target = new()
                {
                    X = (int)(tileX * 16f + 8f - Main.screenPosition.X),
                    Y = (int)(tileY * 16f + 8f - Main.screenPosition.Y),
                };
                ClientToScreen(hwnd, ref target);
                GetCursorPos(out MOUSEPOS previous);
                SetCursorPos(target.X, target.Y);
                Thread.Sleep(70);                    // let the game see the cursor on the tile
                mouse_event(right ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
                Thread.Sleep(Math.Max(30, ms));
                mouse_event(right ? MOUSEEVENTF_RIGHTUP : MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                Thread.Sleep(40);
                SetCursorPos(previous.X, previous.Y); // put the user's cursor back
                LastClickTick = Main.GameUpdateCount;
                LastClickOk = true;
            }
            catch (Exception e)
            {
                LastClickError = e.Message;
                modRef?.Logger.Warn($"[dsh] input injection failed: {e.Message}");
            }
        }

        /// <summary>Fire the injection on a background thread so the game thread never blocks.</summary>
        internal static void InjectClickAsync(int tileX, int tileY, bool right, int ms)
        {
            LastClickOk = false;
            LastClickError = null;
            var t = new Thread(() => InjectClick(tileX, tileY, right, ms)) { IsBackground = true };
            t.Start();
        }

        internal static volatile bool LastClickOk;
        internal static volatile string LastClickError;
        internal static ulong LastClickTick;

        /// <summary>
        /// Point the cursor at the armed tile and pretend the mouse button is held. Called
        /// from PreItemCheck, so vanilla's own use/placement/mining logic runs.
        /// </summary>
        internal static void ApplySyntheticInput()
        {
            SynthCalls++;
            if (AimTileX >= 0) ApplyAim();
            ulong now = Now;
            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return;

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

        /* ── 直接挖掘任务 ────────────────────────────────────────────────
         * 不用按键、不抢焦点：每帧在主线程调用原版挥镐时用的 Player.PickTile
         * （它有耐久/硬度/掉落逻辑），按物品 useTime 控制节奏，并播放挥动动画。
         * 这样玩家在同一窗口里打怪、AI 在后台挖矿，互不抢输入。
         */
        internal struct MiningJob
        {
            public bool Active;
            public int X, Y, Slot, Hits;
            public ulong NextHit, Deadline;
        }

        internal static MiningJob Mine;

        internal static void StartMining(int x, int y, int slot, int ms)
        {
            Mine = new MiningJob
            {
                Active = true,
                X = x,
                Y = y,
                Slot = slot,
                Hits = 0,
                NextHit = Now,
                Deadline = Now + Ticks(ms),
            };
        }

        internal static void StopMining() => Mine.Active = false;

        private static void TickMining()
        {
            if (!Mine.Active) return;
            Tile t = Main.tile[Mine.X, Mine.Y];
            if (!t.HasTile || Now > Mine.Deadline) { Mine.Active = false; return; }
            if (Now < Mine.NextHit) return;

            Player p = Main.LocalPlayer;
            if (p == null || !p.active || p.dead) { Mine.Active = false; return; }
            Item tool = p.inventory[Mine.Slot];
            if (tool == null || tool.type <= 0) { Mine.Active = false; return; }

            int power = tool.pick;
            if (power > 0)
            {
                p.PickTile(Mine.X, Mine.Y, power);
            }
            else
            {
                // Axes (trees) and hammers do not go through PickTile.
                WorldGen.KillTile(Mine.X, Mine.Y, false, false, false);
            }
            p.selectedItem = Mine.Slot;
            if (tool.useAnimation > 0)
            {
                p.SetItemAnimation(Math.Max(1, tool.useAnimation));
                p.SetItemTime(Math.Max(1, tool.useTime));
            }
            p.direction = Mine.X * 16f >= p.Center.X ? 1 : -1;
            Mine.Hits++;
            Mine.NextHit = Now + (ulong)Math.Max(6, (int)(tool.useTime * 0.6f));
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
            public int Mined, Filled, Steps;
            public string StopReason;
        }

        internal static TravelJob Travel;

        internal static void StartTravel(int dir, int targetX, int ms)
        {
            Travel = new TravelJob
            {
                Active = true,
                Dir = dir,
                TargetX = targetX,
                Deadline = Now + Ticks(ms),
                NextMine = Now,
                StopReason = "",
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
                dir = Travel.Dir > 0 ? "right" : "left",
                targetX = Travel.TargetX,
                x = px,
                y = py,
                mine = Travel.Mined,
                fill = Travel.Filled,
                steps = Travel.Steps,
                stopReason = Travel.StopReason,
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
            // 到达
            if (Travel.Dir > 0 ? px >= Travel.TargetX : px <= Travel.TargetX) { StopTravel("reached"); return; }
            // 血低 / 有敌人：交还给脚本层
            if (p.statLife < p.statLifeMax2 * 0.45f) { StopTravel("lowhp"); return; }
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
            if (Travel.Dir > 0) { RightUntil = until; LeftUntil = 0; }
            else { LeftUntil = until; RightUntil = 0; }

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

            // 1) 先补脚下地板（前方 2 格）
            if (Travel.Steps % 3 == 0)
            {
                for (int d = 1; d <= 2; d++)
                {
                    int fx = px + Travel.Dir * d, fy = py + 2;
                    if (!WorldGen.InWorld(fx, fy, 2)) continue;
                    Tile ft = Main.tile[fx, fy];
                    if (ft.HasTile) continue;
                    // 需要邻居支撑：检查脚下那层是否有实心
                    bool support = false;
                    for (int ddx = -1; ddx <= 1; ddx++)
                    {
                        int sx = fx + ddx;
                        if (!WorldGen.InWorld(sx, fy, 2)) continue;
                        if (Main.tile[sx, fy].HasTile) { support = true; break; }
                    }
                    if (!support) continue;
                    WorldGen.PlaceTile(fx, fy, TileID.Stone, false, false);
                    if (Main.tile[fx, fy].HasTile) Travel.Filled++;
                }
            }

            // 2) 再挖挡路格（前方 1 列，身体高度 3 行）
            int bx = px + Travel.Dir;
            for (int dy = -1; dy <= 1; dy++)
            {
                int by = py + dy;
                if (!WorldGen.InWorld(bx, by, 2)) continue;
                Tile t = Main.tile[bx, by];
                if (!t.HasTile) continue;
                p.selectedItem = pick;
                p.PickTile(bx, by, p.inventory[pick].pick);
                if (!Main.tile[bx, by].HasTile) Travel.Mined++;
            }
            Travel.Steps++;
        }

        /// <summary>Arm one jump for the given deadline (a single ground impulse).</summary>
        internal static void BeginJump(ulong until, float heightTiles = 5.5f)
        {
            JumpUntil = until;
            JumpIssued = false;
            // A fixed impulse made every jump identical (measured 2.30 tiles no matter how long
            // the key was held, because the game's variable jump height is never driven).
            // Solve the impulse for the requested apex instead: h = v^2 / (2g).
            float g = Main.LocalPlayer?.gravity is > 0.05f ? Main.LocalPlayer.gravity : 0.4f;
            float tiles = Math.Clamp(heightTiles, 1f, 12f);
            JumpImpulse = -(float)Math.Sqrt(2.0 * g * tiles * 16.0);
        }

        internal static float JumpImpulse = -8.4f;

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
        internal static ulong LeftUntil, RightUntil, UpUntil, DownUntil, JumpUntil, UseUntil, UseTileUntil;
        internal static ulong AttackUntil, AttackNextTick;
        internal static volatile int AttackTarget = -1, AttackRadius = 40, AttackIntervalTicks = 10;
        internal static volatile bool AttackAutoTarget, AttackFinished, AttackFinishedCleanly, AttackKilled, AttackBlockedByTerrain, JumpIssued;
        internal static volatile int AttackHits, AttackLastTarget = -1, AttackLastLife, AttackLifeBefore, LastHitDamage;
        internal static ulong AttackFinishedTick;
        internal static volatile string AttackName = "";

        public static void ApplyControls(Player player)
        {
            ulong now = Main.GameUpdateCount;
            if (LeftUntil > now) player.controlLeft = true;
            if (RightUntil > now) player.controlRight = true;
            if (UpUntil > now) player.controlUp = true;
            if (DownUntil > now) player.controlDown = true;
            if (JumpUntil > now) player.controlJump = true;
            if (UseUntil > now) player.controlUseItem = true;
            if (UseTileUntil > now) player.controlUseTile = true;
        }

    internal static void ClearControls()
    {
        LeftUntil = RightUntil = UpUntil = DownUntil = JumpUntil = UseUntil = UseTileUntil = 0;
        ClearAttack();
    }

        internal static ulong Now => Main.GameUpdateCount;
        internal static ulong Ticks(int ms) => (ulong)Math.Max(1, ms / 16.67);
    }

    public class DshControlPlayer : ModPlayer
    {
        public override void PreUpdate()
        {
            BridgeSystem.ApplyAim();
            BridgeSystem.ApplyControls(Player);
        }

        // 真正逐帧生效的地方：Player.Update 结束时再推一次速度/标志
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
        }

        public override void PostUpdate()
        {
            // ModPlayer hooks run for every player; without this guard the local player
            // would be advanced once per player slot in multiplayer.
            if (Player.whoAmI != Main.myPlayer) return;
            BridgeSystem.ApplyMovement();
            BridgeSystem.ApplyAttack();
        }
    }
}
