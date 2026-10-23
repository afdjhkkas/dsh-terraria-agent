using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace DshTerrariaBridge
{
    /// <summary>
    /// 本地桥：127.0.0.1:7777，一行一个 JSON 请求 / 响应。
    /// 与 dsh-terraria-agent 的 win.ps1 共用同一套协议，宿主侧只换传输层。
    ///
    /// 线程模型：
    ///   - 接受连接 / 读请求：后台线程
    ///   - 真正读写游戏状态：主线程（PostUpdateEverything 里排空队列）
    /// 这样上层永远不用关心 Terraria 的单线程约束。
    /// </summary>
    public class BridgeSystem : ModSystem
    {
        public const int Port = 7777;

        private static TcpListener listener;
        private static Thread acceptThread;
        private static volatile bool running;

        private sealed class Pending
        {
            public string Json;
            public string Response;
            public int Cancelled;
            public readonly ManualResetEventSlim Done = new(false);
        }

        private static readonly Queue<Pending> pending = new();

        public override void Load()
        {
            try
            {
                listener = new TcpListener(IPAddress.Loopback, Port);
                listener.Start();
                running = true;
                acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "dsh-bridge" };
                acceptThread.Start();
                Mod.Logger.Info($"[dsh] bridge listening on 127.0.0.1:{Port}");
            }
            catch (Exception e)
            {
                running = false;
                Mod.Logger.Warn($"[dsh] bridge failed to start on {Port}: {e.Message}");
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
                    string response;
                    try
                    {
                        response = ExecuteOnMainThread(line);
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
    /// 把命令丢到主线程执行。请求只通过 tick 队列排空；如果主线程长期不排空，
    /// 请求会过期并被跳过，避免超时后重复执行有副作用的命令。
        /// </summary>
        internal static string ExecuteOnMainThread(string json)
        {
            var item = new Pending { Json = json };
            lock (pending) pending.Enqueue(item);
            if (item.Done.Wait(1500)) return item.Response;   // 菜单阶段 tick 钩子可能不跑，1.5s 后走兜底
            // 不在超时后重复执行同一个命令：如果它尚未出队，DrainPending 会跳过；
            // 如果已经开始执行，则它只会完成这一次，调用方收到超时错误。
            Interlocked.Exchange(ref item.Cancelled, 1);
            return Json.Error("main thread did not process the request within 1500 ms");
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
                try { item.Response = Commands.Handle(item.Json); }
                catch (Exception e) { item.Response = Json.Error(e.Message); }
                item.Done.Set();
            }
        }

        public override void PostUpdateEverything()
        {
            DrainPending();
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
                if (p.velocity.Y >= -0.1f && p.velocity.Y <= 0.1f) p.velocity.Y = -5.6f;
            }
            if (UseUntil > now) p.controlUseItem = true;
            if (UseTileUntil > now) p.controlUseTile = true;
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
                ClearAttack();
                return;
            }
            if (AttackUntil <= Main.GameUpdateCount) return;
            ulong now = Main.GameUpdateCount;
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
                if (!AttackAutoTarget) ClearAttack();
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
                    ClearAttack();
                    return;
                }
            }

            Item weapon = p.HeldItem;
            if (weapon == null || weapon.damage <= 0)
            {
                ClearAttack();
                return;
            }

            int direction = target.Center.X >= p.Center.X ? 1 : -1;
            p.direction = direction;
            // Keep the held weapon's swing state visible while the direct hit
            // below applies real player-owned damage.  SetDummyItemTime also
            // updates itemTimeMax, which the vanilla animation code expects.
            p.SetDummyItemTime(Math.Max(1, weapon.useAnimation));
            p.ApplyDamageToNPC(target, weapon.damage, weapon.knockBack, direction, false, weapon.DamageType, true);
            AttackHits++;
            AttackLastTarget = target.whoAmI;
            AttackLastLife = target.active ? target.life : 0;

            int interval = AttackIntervalTicks > 0 ? AttackIntervalTicks : 10;
            AttackNextTick = now + (ulong)interval;
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
        }

        internal static void ClearAttack()
        {
            AttackUntil = 0;
            AttackTarget = -1;
            AttackAutoTarget = false;
            AttackNextTick = 0;
            AttackHits = 0;
            AttackLastTarget = -1;
            AttackLastLife = 0;
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
        };

        public override void UpdateUI(Microsoft.Xna.Framework.GameTime gameTime) => DrainPending();

        /* ── 控制状态：给"按住走 N 毫秒"用，按 tick 存截止时间 ───────────── */

        internal static ulong LeftUntil, RightUntil, UpUntil, DownUntil, JumpUntil, UseUntil, UseTileUntil;
        internal static ulong AttackUntil, AttackNextTick;
        internal static int AttackTarget = -1, AttackRadius = 40, AttackIntervalTicks = 10;
        internal static bool AttackAutoTarget;
        internal static int AttackHits, AttackLastTarget = -1, AttackLastLife;

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
        public override void PreUpdate() => BridgeSystem.ApplyControls(Player);

        // 真正逐帧生效的地方：Player.Update 结束时再推一次速度/标志
        public override void PostUpdate()
        {
            BridgeSystem.ApplyMovement();
            BridgeSystem.ApplyAttack();
        }
    }
}
