using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria.GameContent.Creative;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace DshTerrariaBridge
{
    /// <summary>
    /// 命令实现。所有方法都在主线程（PostUpdateEverything）里跑，可以放心读写游戏状态。
    /// 协议：{"cmd":"state","params":{...}} → {"ok":true,"data":{...}}
    /// </summary>
    internal static class Commands
    {
        public static string Handle(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            string cmd = Json.Str(root, "cmd", "");
            JsonElement p = root.TryGetProperty("params", out var pe) ? pe : default;

            // 记下这条命令真正读过哪些参数名；传了但没人读的，附在成功回包的 data.ignoredParams 里。
            // 这样拼错的参数名（direction/dir、radius/r）不再被静默吞掉。
            Json.BeginParamTracking();
            string response;
            HashSet<string> read;
            try { response = Dispatch(cmd, p); }
            finally { read = Json.EndParamTracking(); }
            return Json.WithIgnoredParams(response, p, read);
        }

        private static string Dispatch(string cmd, JsonElement p)
        {
            switch (cmd)
            {
                case "ping": return Ping();
                case "state": return State();
                case "humaninput": return HumanInput();
                case "keyevents": return KeyEventsCmd(p);
                case "key": return Key(p);
                case "scan": return Scan(p);
                case "tile": return TileAt(p);
                case "gravity": return Gravity(p);
                case "mine": return Mine(p);
                case "mirror": return MirrorHome(p);
                case "accessory": return Accessory(p);
                case "walkto": return WalkTo(p);
                case "travel": return TravelCmd(p);
                case "travelstatus": return TravelStatusCmd();
                case "walkdiff": return WalkDiffCmd();
                case "walkmode": return WalkModeCmd(p);
                case "path": return PathTo(p);
                case "probe": return Probe(p);
                case "research": return Research(p);
                case "find": return FindTile(p);
                case "duplicate": return Duplicate(p);
                case "fill": return FillArea(p);
                case "roomcheck": return RoomCheckCmd(p);
                case "navrules": return NavRulesCmd(p);

                case "housing": return Housing(p);
                case "needs": return RoomDiagnostics(p);
                case "homeroom": return HomeRoom(p);
                case "nodig": return NoDig(p);
                case "housediag": return HouseDiag(p);
                case "items": return DroppedItems(p);
                case "grab": return GrabLoot(p);
                case "save": return SaveWorld(p);
                case "removeblock": return RemoveBlock(p);
                case "recipes": return Recipes(p);
                case "api": return ApiLookup(p);
                case "idmap": return IdMap(p);
                case "props": return Props(p);
                case "talk": return TalkTo(p);
                case "talkclose": return TalkClose(p);
                case "talkoption": return TalkOption(p);
                case "talkfocus": return TalkFocus(p);
                case "shop": return ShopList(p);
                case "shopdebug": return ShopDebug(p);
                case "shops": return ShopFind(p);
                case "buy": return ShopBuy(p);
                case "sell": return ShopSell(p);
                case "methods": return MethodsCmd(p);
                case "fields": return FieldsCmd(p);
                case "chest": return ChestCmd(p);
                case "quest": return Quest(p);
                case "startevent": return StartEvent(p);
                case "map": return MapInfo(p);
                case "splitstack": return SplitStack(p);
                case "castline": return CastLine(p);
                case "reel": return Reel(p);
                case "bobber": return Bobbers();
                case "capture": return Capture(p);
                case "liquidop": return LiquidOp(p);
                case "drop": return DropItem(p);
                case "trash": return TrashItem(p);
                case "heal": return HealSelf(p);
                case "boss": return BossStatus(p);
                case "event": return EventStatus(p);
                case "equip": return EquipArmor(p);
                case "escape": return EscapeCmd(p);
                case "findpit": return FindPit(p);
                case "place": return Place(p);
                case "tiledata": return TileDataCmd(p);
                case "move": return Move(p);
                case "moveto": return MoveTo(p);
                case "jump": return Jump(p);
                case "climb": return Climb(p);
                case "use": return Use(p);
                case "intent": return IntentCommand(p);
                case "interact": return Interact(p);
                case "attack": return Attack(p);
                case "select": return SelectSlot(p);
                case "shoot": return Shoot(p);
                case "hook":
                case "grapple": return Hook(p);   // 两个名字都认：真人界面上叫"抓钩"，早期脚本里叫 grapple
                case "hookdebug": return HookDebug(p);
                case "unhook": return Unhook(p);
                case "fly": return Fly(p);
                case "mount": return MountCmd(p);
                case "minion": return Minion(p);
                case "guard": return GuardCmd(p);
                case "follow": return FollowCmd(p);
                case "idle": return IdleCmd(p);
                case "chatpoll":
                // max 上限放到 500：一次只能取 100 条时，积压 700 条得分 8 轮才读完，
                // 中间任何一轮出错都很难看出是哪一段。上限调到与缓冲区同量级。
                return Json.Ok(BridgeSystem.ChatPoll(Json.Int(p, "since", 0), Math.Clamp(Json.Int(p, "max", 20), 1, 500)));

            case "chatpost":
            {
                string msg = Json.Str(p, "text", null);
                if (string.IsNullOrWhiteSpace(msg)) return Json.Error("需要 text");
                return Json.Ok(BridgeSystem.ChatPost(msg));
            }

            case "players": return PlayersCmd();
                case "stop":
                    BridgeSystem.GuardUntil = 0;
                    BridgeSystem.StopPath("cancelled");
                    BridgeSystem.StopWalk("cancelled");
                    BridgeSystem.ClearControls();
                    // 停手必须连准星一起还回去：残留的 AimTile 会让游戏每帧把鼠标压回旧格子，
                    // 真人接手后既挖不动也放不下（实测事故）。
                    BridgeSystem.EndItemUse();
                    return Json.Ok(new { stopped = true, aimCleared = true });
                case "wall": return Wall(p);
                case "inventory": return Inventory();
                case "craft": return Craft(p);
                case "npc": return Npcs(p);
                case "chat": return Chat(p);
                case "dialog": return Dialog(p);
                case "help": return Help();
                default: return Json.Error("unknown command: " + cmd + "（可用：" + string.Join("/", CommandNames) + "）");
            }
        }

        /* ── 基础 ─────────────────────────────────────────────────────── */

        private static bool InWorld(out string error)
        {
            // 专用服务端没有本地玩家，但世界照样可读写，所以这里只判断"是否在世界里"
            if (Main.gameMenu || Main.maxTilesX <= 0 || Main.maxTilesY <= 0)
            {
                error = "not in a world (still on the menu or no world loaded)";
                return false;
            }
            error = null;
            return true;
        }

        private static bool HasPlayer(out string error)
        {
            Player p = Main.LocalPlayer;
            if (p == null || !p.active)
            {
                error = "no local player on this instance (dedicated server?): use scan/mine/place/tile/npc instead";
                return false;
            }
            error = null;
            return true;
        }

        private static Player P => Main.LocalPlayer;

        private static bool InBounds(int x, int y) =>
            x >= 0 && y >= 0 && x < Main.maxTilesX && y < Main.maxTilesY;

        /// <summary>
        /// Empty cells are reported as -1, never as 0.
        /// Terraria's TileID.Dirt is 0, so "0 means empty" made dirt indistinguishable
        /// from air in every scan and made mine refuse to dig the most common block.
        /// </summary>
        private const int EmptyTile = -1;

        private static int TileCode(Tile t) => t.HasTile ? t.TileType : EmptyTile;

        /// <summary>shape: 0 solid, 1 half brick, 2..5 the four slopes.</summary>
        private static int ShapeCode(Tile t)
        {
            if (t.IsHalfBlock) return 1;
            if (t.Slope == SlopeType.Solid) return 0;
            return (int)t.Slope + 1;
        }

        private static bool ValidTileType(int type) => type >= TileID.Dirt && type < TileID.Count;

        private static bool ValidItemType(int type)
        {
            if (type <= 0) return false;
            try { return ContentSamples.ItemsByType.ContainsKey(type); } catch { return false; }
        }

        private static bool ValidNpcType(int type) => type > 0 && type < NPCID.Count;

        /// <summary>Commands that advance only when the world ticks must refuse to run
        /// while the simulation is paused, otherwise they report success and do nothing.</summary>
        private static bool CanTick(out string error)
        {
            if (Main.gamePaused)
            {
                error = "游戏已暂停（Main.gamePaused）：世界不会推进，移动/跳跃/使用/攻击都不会生效。"
                    + "先让游戏恢复运行（返回游戏窗口、关闭暂停菜单）再重试。";
                return false;
            }
            error = null;
            return true;
        }

        private static string Ping() => Json.Ok(new
        {
            pong = true,
            version = DshTerrariaBridge.BridgeVersion,
            inWorld = !Main.gameMenu,
            tick = Main.GameUpdateCount,
            // Terraria's "pause when unfocused" / "auto pause" settings stop the world from
            // ticking while the bridge keeps answering. Tell the caller, so it does not
            // mistake a frozen world for a broken tool.
            paused = Main.gamePaused,
            hasFocus = Main.hasFocus,
        });

        /// <summary>
        /// 人类输入观测：只读游戏自己维护的输入状态（Main.keyState / Player.control* /
        /// Main.mouseLeft），不碰任何 OS 级输入 API。观摩模式靠它拿到"这一帧人按了什么、
        /// 朝哪瞄、手持什么"——和主线程同一帧读数，时序以 Main.GameUpdateCount 为准。
        /// </summary>
        private static System.Reflection.FieldInfo _wheelField;
        private static bool _wheelProbed;

        /// <summary>
        /// 读引擎的滚轮累积值。字段名在不同版本里可能不同，所以按候选名反射一次并缓存；
        /// 一个都找不到就返回 0（而不是抛异常，观测口不能因为小版本差异就整条失效）。
        /// </summary>
        private static int ReadMouseWheel()
        {
            if (!_wheelProbed)
            {
                _wheelProbed = true;
                foreach (string name in new[] { "mouseWheel", "MouseWheel" })
                {
                    var f = typeof(Main).GetField(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (f != null && f.FieldType == typeof(int)) { _wheelField = f; break; }
                }
            }
            if (_wheelField == null) return 0;
            try { return (int)_wheelField.GetValue(null); } catch { return 0; }
        }

        // 真机教讲时窗口可能不在前台（串流/远程），XNA 的 Main.mouseX/Y 就冻住不动。
        // 这里直接问 Windows 要光标位置，再用客户区原点换算回世界坐标，不依赖窗口活跃。
        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left; public int Top; public int Right; public int Bottom; }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out NativePoint p);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out NativeRect r);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint p);

        /// <summary>读 OS 光标并换算成客户区/世界坐标。inClient=false 表示光标不在游戏窗口里。</summary>
        private static (bool ok, bool inClient, int sx, int sy, int cx, int cy, float wx, float wy, int cw, int ch) ReadOsCursor()
        {
            NativePoint pos;
            if (!GetCursorPos(out pos)) return (false, false, 0, 0, 0, 0, 0f, 0f, 0, 0);
            IntPtr hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (hwnd == IntPtr.Zero) return (false, false, pos.X, pos.Y, 0, 0, 0f, 0f, 0, 0);
            NativeRect rc;
            if (!GetClientRect(hwnd, out rc)) return (false, false, pos.X, pos.Y, 0, 0, 0f, 0f, 0, 0);
            int cw = rc.Right - rc.Left, ch = rc.Bottom - rc.Top;
            if (cw <= 0 || ch <= 0) return (false, false, pos.X, pos.Y, 0, 0, 0f, 0f, 0, 0);
            NativePoint origin = new NativePoint { X = 0, Y = 0 };
            if (!ClientToScreen(hwnd, ref origin)) return (false, false, pos.X, pos.Y, 0, 0, 0f, 0f, 0, 0);
            int ox = pos.X - origin.X;
            int oy = pos.Y - origin.Y;
            bool inClient = ox >= 0 && oy >= 0 && ox < cw && oy < ch;
            // 客户区物理像素 → 渲染像素，再平移世界坐标（zoom 已包含在 screenPosition/screenWidth 里）
            float rx = (float)ox * Main.screenWidth / cw;
            float ry = (float)oy * Main.screenHeight / ch;
            float wx = Main.screenPosition.X + rx;
            float wy = Main.screenPosition.Y + ry;
            return (true, inClient, pos.X, pos.Y, ox, oy, wx, wy, cw, ch);
        }
        /// <summary>只读：取某格 tile type，越界或空返回 -1。</summary>
        internal static int TileTypeAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return -1;
            Tile t = Main.tile[x, y];
            return (t != null && t.HasTile) ? (int)t.TileType : -1;
        }

        /// <summary>
        /// 只读：玩家碰撞箱与绳子的接触情况。
        /// 为什么需要它：pulley=true 但人明明站在 3 格外的平台上时，
        /// 只有把 hitbox 换算成格坐标、逐格查绳，才能说清"引擎认定的绳在哪"。
        /// </summary>
        private static object RopeContact(Player pl)
        {
            Rectangle ht = pl.Hitbox;
            int lx = ht.Left / 16, rx = (ht.Right - 1) / 16;
            int ty = ht.Top / 16, by = (ht.Bottom - 1) / 16;
            bool bodyRope = false;
            int ropeTiles = 0;
            for (int x = lx; x <= rx; x++)
                for (int y = ty; y <= by; y++)
                    if (BridgeSystem.IsRopeTile(x, y)) { bodyRope = true; ropeTiles++; }
            int cx = (lx + rx) / 2, cy = (ty + by) / 2;
            int feetX = (int)((pl.position.X + pl.width * 0.5f) / 16f);
            int feetY = (int)((pl.position.Y + pl.height) / 16f);
            int feetType = TileTypeAt(feetX, feetY);
            bool onRopeReal = BridgeSystem.OnRopeNow(pl);
            return new
            {
                pulley = pl.pulley,
                ropeCount = pl.ropeCount,
                bodyHasRopeTile = bodyRope,
                bodyRopeTiles = ropeTiles,
                leftX = MathF.Round(ht.Left / 16f, 3),
                rightX = MathF.Round(ht.Right / 16f, 3),
                topY = MathF.Round(ht.Top / 16f, 3),
                feetY = MathF.Round(ht.Bottom / 16f, 3),
                tileLeftX = lx,
                tileRightX = rx,
                tileTopY = ty,
                tileFeetY = feetY,
                tileFeetX = feetX,
                tileFeetType = feetType,
                onRopeReal,
                footLeftTile = TileTypeAt(lx, feetY),
                footRightTile = TileTypeAt(rx, feetY),
                centerTile = TileTypeAt(cx, cy),
                centerTileX = cx,
                centerTileY = cy,
                // 脚下一格（站立格）左右两列的 tile type：判断"脚下是绳还是平台/土"。
                underLeftTile = TileTypeAt(lx, feetY + 1),
                underRightTile = TileTypeAt(rx, feetY + 1),
            };
        }

        private static object InputDiagnostics(Player responsePlayer)
        {
            Player local = Main.LocalPlayer;
            int indexedWho = -1;
            if (Main.player != null && Main.myPlayer >= 0 && Main.myPlayer < Main.player.Length)
                indexedWho = Main.player[Main.myPlayer]?.whoAmI ?? -1;
            return new
            {
                playerIds = new
                {
                    myPlayer = Main.myPlayer,
                    localPlayerWhoAmI = local?.whoAmI ?? -1,
                    indexedPlayerWhoAmI = indexedWho,
                    responsePlayerWhoAmI = responsePlayer?.whoAmI ?? -1,
                },
                postUpdateInput = new
                {
                    tick = BridgeSystem.DbgInputTriggerTick,
                    whoAmI = BridgeSystem.DbgInputTriggerPlayer,
                    left = BridgeSystem.DbgInputTriggerLeft,
                    right = BridgeSystem.DbgInputTriggerRight,
                    up = BridgeSystem.DbgInputTriggerUp,
                    down = BridgeSystem.DbgInputTriggerDown,
                    jump = BridgeSystem.DbgInputTriggerJump,
                    mount = BridgeSystem.DbgInputTriggerMount,
                    inventory = BridgeSystem.DbgInputTriggerInventory,
                },
                postUpdateRunSpeeds = new
                {
                    tick = BridgeSystem.DbgInputControlTick,
                    whoAmI = BridgeSystem.DbgInputControlPlayer,
                    left = BridgeSystem.DbgInputControlLeft,
                    right = BridgeSystem.DbgInputControlRight,
                    up = BridgeSystem.DbgInputControlUp,
                    down = BridgeSystem.DbgInputControlDown,
                    jump = BridgeSystem.DbgInputControlJump,
                },
            };
        }

        private static string HumanInput()
        {
            if (Main.gameMenu || Main.LocalPlayer == null) return Json.Ok(new { inWorld = false });
            Player pl = P;
            var cur = ReadOsCursor();
            var ks = Main.keyState;
            bool k(Microsoft.Xna.Framework.Input.Keys key) => ks.IsKeyDown(key);
            var raw = new List<string>();
            if (k(Microsoft.Xna.Framework.Input.Keys.A)) raw.Add("A");
            if (k(Microsoft.Xna.Framework.Input.Keys.D)) raw.Add("D");
            if (k(Microsoft.Xna.Framework.Input.Keys.W)) raw.Add("W");
            if (k(Microsoft.Xna.Framework.Input.Keys.S)) raw.Add("S");
            if (k(Microsoft.Xna.Framework.Input.Keys.Space)) raw.Add("Space");
            if (k(Microsoft.Xna.Framework.Input.Keys.LeftShift)) raw.Add("Shift");
            if (k(Microsoft.Xna.Framework.Input.Keys.LeftControl)) raw.Add("Ctrl");
            for (int i = 1; i <= 10; i++)
            {
                if (k(Microsoft.Xna.Framework.Input.Keys.D0 + (i % 10))) raw.Add("D" + (i % 10));
            }

            return Json.Ok(new
            {
                inWorld = true,
                tick = (long)Main.GameUpdateCount,
                paused = Main.gamePaused,
                focus = Main.hasFocus,
                playerIndex = pl.whoAmI,
                inputDiag = InputDiagnostics(pl),
                control = new Dictionary<string, object>
                {
                    ["left"] = pl.controlLeft,
                    ["right"] = pl.controlRight,
                    ["up"] = pl.controlUp,
                    ["down"] = pl.controlDown,
                    ["jump"] = pl.controlJump,
                    ["useItem"] = pl.controlUseItem,
                    ["useTile"] = pl.controlUseTile,
                    ["grapple"] = pl.controlHook,
                    ["mount"] = pl.controlMount,
                },
                mouse = new
                {
                    left = Main.mouseLeft,
                    right = Main.mouseRight,
                    x = Main.mouseX,
                    y = Main.mouseY,
                    tileX = (int)(Main.MouseWorld.X / 16f),
                    tileY = (int)(Main.MouseWorld.Y / 16f),
                    // 真人切物品栏主要用滚轮，不记这个就看不到"滚了几格"。
                    wheel = ReadMouseWheel(),
                    // OS 层光标：窗口不在前台时 XNA 的值会冻住，教讲要看这一组。
                    os = new
                    {
                        ok = cur.ok,
                        inClient = cur.inClient,
                        x = cur.sx,
                        y = cur.sy,
                        clientX = cur.cx,
                        clientY = cur.cy,
                        clientW = cur.cw,
                        clientH = cur.ch,
                        worldX = MathF.Round(cur.wx, 1),
                        worldY = MathF.Round(cur.wy, 1),
                        tileX = cur.ok ? (int)MathF.Floor(cur.wx / 16f) : -1,
                        tileY = cur.ok ? (int)MathF.Floor(cur.wy / 16f) : -1,
                    },
                    source = cur.ok && cur.inClient ? "os" : "xna",
                },
                keys = raw,
                player = new
                {
                    tile = new[] { MathF.Round(pl.Center.X / 16f, 1), MathF.Round(pl.Center.Y / 16f, 1) },
                    velocity = new[] { MathF.Round(pl.velocity.X, 2), MathF.Round(pl.velocity.Y, 2) },
                    grounded = pl.velocity.Y == 0f,
                    wet = pl.wet,
                    dead = pl.dead,
                    life = pl.statLife,
                    lifeMax = pl.statLifeMax2,
                    selected = pl.selectedItem,
                    heldType = pl.HeldItem.type,
                    // 挥动/挖掘的动画进度：判断"是不是真的在挥"比看控制标志可靠。
                    itemAnimation = pl.itemAnimation,
                },
            });
        }

        /// <summary>按键边沿事件（tick 级）。示范采集用它算真实按住时长，比轮询准。</summary>
        private static string KeyEventsCmd(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int since = Json.Int(p, "since", 0);
            int max = Math.Clamp(Json.Int(p, "max", 400), 1, 2000);
            bool clear = Json.Bool(p, "clear", false);
            var evs = BridgeSystem.SnapshotKeyEvents(since, max);
            long last = BridgeSystem.LastKeyEventTick();
            var flat = new List<object>();
            foreach (var e in evs) { flat.Add(e.Tick); flat.Add(e.Key); flat.Add(e.Down); }
            if (clear) BridgeSystem.ClearKeyEvents();
            return Json.Ok(new { lastTick = last, count = evs.Count, time = (long)Main.time, stride = 3, flat });
        }

        /// <summary>
        /// Send Terraria's real Inventory key.  In the default keyboard profile this is
        /// Escape, and vanilla ToggleInv() decides whether that closes NPC chat/shop or
        /// opens/closes the inventory.  We only emit the input edge; callers must re-read
        /// state after untilTick to verify the UI transition.
        /// </summary>
        private static string Key(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);

            string requested = (Json.Str(p, "key", "inventory") ?? "inventory").Trim().ToLowerInvariant();
            if (requested != "inventory" && requested != "escape")
                return Json.Error("key 目前支持 inventory 或 escape；两者都走 Terraria 的 Inventory 键输入");

            // 默认 40ms（约 2 帧）。实测按太久会反复 toggle：原版 ToggleInv 之后
            // releaseInventory 被清成 false，但只要按住期间 controlInv 掉回 false 一帧，
            // 引擎的 else 分支又把 releaseInventory 置 true，下一帧按住就又 toggle 一次。
            // 实测 ms=120 时 playerInventory 在 false/true 之间抖了 8 次；ms=40 时一次干净翻转。
            int ms = Math.Clamp(Json.Int(p, "ms", 40), 20, 1000);
            ulong now = BridgeSystem.NowTick();
            if (BridgeSystem.InventoryUntil > now)
                return Json.Error("inventory 键输入仍在进行中；等 untilTick 后再发下一次");

            Player pl = P;
            bool beforeInventory = Main.playerInventory;
            int beforeTalkNpc = pl.talkNPC;
            int beforeShop = Main.npcShop;

            // 预开闸 6 帧：这几帧只抬 Main.hasFocus、不按键，让原版执行
            //   else { releaseInventory = true; }
            // 把边沿准备好；6 帧之后再真正按下，ToggleInv() 才会被调用。
            // 不加这一步，失焦下 controlInv=true 也进不去（实测 playerInventory 恒 false）。
            const int preOpen = 6;
            BridgeSystem.InventoryPreOpenUntil = now + (ulong)preOpen;
            ulong until = now + (ulong)preOpen + BridgeSystem.Ticks(ms);
            BridgeSystem.InventoryUntil = until;

            return Json.Ok(new
            {
                key = requested,
                resolvedKey = "inventory",
                ms,
                preOpenTicks = preOpen,
                before = new { playerInventory = beforeInventory, talkNPC = beforeTalkNpc, npcShop = beforeShop },
                untilTick = until,
                note = "已发送真实 Inventory/Escape 键输入（预开 " + preOpen + " 帧 + 按住 " + ms + "ms）；等 untilTick 后回读 state.playerInventory、talkNPC、npcShopIndex 确认原版 UI 是否切换",
            });
        }

        /// <summary>命令名清单：help 与 unknown 提示都从这里生成，
        /// 免得再出现"实现了但提示里没有、调用方以为不存在"的误导。</summary>
        internal static readonly string[] CommandNames = new[]
        {
            "accessory", "api", "attack", "bobber", "boss", "buy", "capture", "castline", "chat", "chatpoll", "chatpost", "chest", "climb", "craft", "dialog", "drop", "duplicate", "equip", "escape", "event", "findpit", "fields", "fill", "find", "fly", "follow", "grab", "grapple", "gravity", "guard", "heal", "help", "homeroom", "hook", "hookdebug", "housediag", "housing", "humaninput", "idle", "idmap", "intent", "interact", "inventory", "items", "jump", "key", "keyevents", "liquidop", "map", "methods", "mine", "minion", "mirror", "mount", "move", "moveto", "navrules", "needs", "nodig", "npc", "path", "ping", "place", "players", "props", "quest", "recipes", "reel", "removeblock", "research", "roomcheck", "save", "scan", "select", "sell", "shoot", "shop", "shopdebug", "shops", "splitstack", "startevent", "state", "stop", "talk", "talkclose", "talkfocus", "talkoption", "tile", "tiledata", "trash", "travel", "travelstatus", "unhook", "use", "walkdiff", "walkmode", "walkto", "wall"
        };

        private static string Help() => Json.Ok(new
        {
            // 命令名一律从 CommandNames 出，避免"提示里的清单"和"真正能调的"再分叉。
            commands = CommandNames,
            notes = new[]
            {
                "jump{ms,tiles,dir,moveMs}：只走原生输入层（Triggers.Jump），高度由按住时长和游戏物理决定；dir=left/right 时同帧带横向输入",
                "hook/grapple{x,y,slot,ms}：抓钩，真人键位语义；unhook 脱钩",
                "key{key,ms}：发送真实 Inventory/Escape 键；等 untilTick 后回读 state.playerInventory、talkNPC、npcShopIndex",
                "humaninput{samples,ms}：观察模式采集口（控制标志+位置+按键），只读",
                "keyevents{since,max,clear}：按键边沿事件（tick 级），示范采集算真实按住时长用；不含本模组自己下发的输入",
            },
        });

        private static string State()
        {
            bool inWorld = !Main.gameMenu && Main.LocalPlayer != null;
            var data = new Dictionary<string, object>
            {
                ["inWorld"] = inWorld,
                ["version"] = DshTerrariaBridge.BridgeVersion,
                ["paused"] = Main.gamePaused,
                ["hasFocus"] = Main.hasFocus,
                ["bridge"] = new
                {
                    port = BridgeSystem.Port,
                    processId = Environment.ProcessId,
                    botClient = BridgeSystem.DbgBotClient,
                    windowForeground = BridgeSystem.DbgBotWindowForeground,
                    mouseSuppressed = BridgeSystem.DbgBotMouseSuppressed,
                    physicalMouseLeft = BridgeSystem.DbgPhysicalMouseLeft,
                    physicalMouseRight = BridgeSystem.DbgPhysicalMouseRight,
                    bridgeMouseLeft = BridgeSystem.DbgBridgeMouseLeft,
                    bridgeMouseRight = BridgeSystem.DbgBridgeMouseRight,
                },
                ["screen"] = new { w = Main.screenWidth, h = Main.screenHeight, x = (int)Main.screenPosition.X, y = (int)Main.screenPosition.Y },
                ["mouseWorld"] = inWorld ? new[] { MathF.Round(Main.MouseWorld.X / 16f, 1), MathF.Round(Main.MouseWorld.Y / 16f, 1) } : null,
                ["mouseTile"] = inWorld ? new[] { (int)(Main.MouseWorld.X / 16f), (int)(Main.MouseWorld.Y / 16f) } : null,
            };

            if (inWorld)
            {
                Player pl = P;
                data["talkNPC"] = pl.talkNPC;
                data["npcChatText"] = Main.npcChatText;
                data["npcShopIndex"] = Main.npcShop;
                data["npcChatRelease"] = Main.npcChatRelease;
                data["talkInput"] = new
                {
                    active = BridgeSystem.TalkTargetNpc >= 0 && BridgeSystem.TalkUntil > BridgeSystem.Now,
                    targetNpc = BridgeSystem.TalkTargetNpc,
                    untilTick = BridgeSystem.TalkUntil,
                    phase = BridgeSystem.TalkPhase,
                    mouseRight = BridgeSystem.DbgTalkMouseRight,
                    chatRelease = BridgeSystem.DbgTalkChatRelease,
                    finalMouseX = BridgeSystem.DbgTalkFinalMouseX,
                    finalMouseY = BridgeSystem.DbgTalkFinalMouseY,
                    rawMouseX = BridgeSystem.DbgTalkRawMouseX,
                    rawMouseY = BridgeSystem.DbgTalkRawMouseY,
                    screenX = BridgeSystem.DbgTalkScreenX,
                    screenY = BridgeSystem.DbgTalkScreenY,
                    zoom = BridgeSystem.DbgTalkZoom,
                };
                data["world"] = new
                {
                    name = Main.worldName,
                    width = Main.maxTilesX,
                    height = Main.maxTilesY,
                    dayTime = Main.dayTime,
                    time = Math.Round(Main.time, 1),
                    raining = Main.raining,
                    bloodMoon = Main.bloodMoon,
                    eclipse = Main.eclipse,
                    hardmode = Main.hardMode,
                    // 事件状态：血月/日食/南瓜月/霜月/入侵是否在进行、入侵到什么程度。
                    // 守夜、清入侵、判断"该不该出门"全靠这几个字段。
                    pumpkinMoon = Main.pumpkinMoon,
                    snowMoon = Main.snowMoon,
                    invasionType = Main.invasionType,
                    invasionSize = Main.invasionSize,
                    invasionProgress = Main.invasionProgress,
                    anglerQuest = Main.anglerQuest,
                    night = !Main.dayTime,
                    tick = Main.GameUpdateCount,
                };
                data["player"] = new
                {
                    playerIndex = pl.whoAmI,
                    tile = new[] { MathF.Round(pl.Center.X / 16f, 1), MathF.Round(pl.Center.Y / 16f, 1) },
                    pos = new[] { MathF.Round(pl.position.X / 16f, 2), MathF.Round(pl.position.Y / 16f, 2) },
                    velocity = new[] { MathF.Round(pl.velocity.X, 2), MathF.Round(pl.velocity.Y, 2) },
                    itemAnimation = pl.itemAnimation,
                    itemAnimationMax = pl.itemAnimationMax,
                    itemTime = pl.itemTime,
                    life = pl.statLife,
                    lifeMax = pl.statLifeMax2,
                    mana = pl.statMana,
                    manaMax = pl.statManaMax2,
                    dead = pl.dead,
                    direction = pl.direction,
                    // A player on a slope / conveyor / platform can be standing still with a
                    // non-zero velocity: require both this and the previous tick to be at rest.
                    grounded = pl.velocity.Y == 0f && pl.oldVelocity.Y == 0f,
                    flying = pl.wingTime > 0f && !pl.dead,
                    wet = pl.wet,
                    selected = pl.selectedItem,
                    held = new { type = pl.HeldItem.type, name = pl.HeldItem.Name, stack = pl.HeldItem.stack },
                    buffs = Buffs(pl),
                    talkNPC = pl.talkNPC,
                    npcChatText = Main.npcChatText,
                    npcShopIndex = Main.npcShop,
                    // Survival readings. Digging without watching these is how the agent drowned
                    // and kept swinging: breath runs out under water and lava kills in seconds.
                    breath = pl.breath,
                    breathMax = pl.breathMax,
                    lavaWet = pl.lavaWet,
                    honeyWet = pl.honeyWet,
                    hpPercent = pl.statLifeMax2 > 0 ? (int)(100f * pl.statLife / pl.statLifeMax2) : 0,
                    // 生物群系标志：找腐化/猩红/丛林/沙漠时不用盲挖，读这里就知道走到哪了。
                    zones = Zones(pl),
                    // 位移/战斗能力的回读项。没有这些数，射箭/召唤/挂钩/飞行就只能"看着像做了"。
                    minions = pl.numMinions,
                    minionSlots = pl.maxMinions,
                    grapCount = pl.grapCount,
                    hookType = HookProjectile(pl),
                    mounted = pl.mount != null && pl.mount.Active,
                    mountType = pl.mount != null && pl.mount.Active ? pl.mount.Type : -1,
                    // 坐椅子/睡床：原版只改 player.sitting 与外观帧，位置几乎不动，
                    // 没有这个字段就只能靠人看屏幕确认（实测被当场指出"你确实不好判断"）。
                    sitting = pl.sitting,
                    head = pl.head,
                    body = pl.body,
                    legFrame = pl.legFrame,
                    wingTime = pl.wingTime,
                    wingTimeMax = pl.wingTimeMax,
                    hasWings = pl.wings > 0,
                    // 诊断（排查"按上键凭空飞"）：原版算完重力后留下的 pulley/速度/上键/脚下是否绳。
                    dbgPulley = BridgeSystem.DbgPulley,
                    dbgVelY = MathF.Round(BridgeSystem.DbgVelY, 3),
                    dbgCtrlUp = BridgeSystem.DbgCtrlUp,
                    dbgPlayerControlUp = BridgeSystem.DbgPlayerControlUp,
                    dbgRopeTile = BridgeSystem.DbgRopeTile,
                    // 开箱子状态：没有这几项，“箱子打开了吗”只能靠人看屏幕，
                    // 我把内容读出来了也说不清界面到底弹没弹（实测被当场指出“假操作”）。
                    // openChest = Main.player[myPlayer].chest：-1=没开，否则是 Main.chest 的下标。
                    openChest = pl.chest,
                    playerInventory = Main.playerInventory,
                    // 爬绳诊断：onRope 只看脚底真实绳格；ropeCount/pulley 仍原样保留，
                    // 用来发现引擎状态与实际地形不一致。
                    pulley = pl.pulley,
                    ropeCount = pl.ropeCount,
                    onRope = BridgeSystem.OnRopeNow(pl),
                    mouseRight = Main.mouseRight,
                    mouseRightRelease = Main.mouseRightRelease,
                    // 输入链路三问（只读）：意图有没有置上、两个玩家索引是否相等。
                    // 实测「Triggers 为真、control 为假、位移 0」时，只有这三种断点：
                    //   intentRight=false            → ApplyMovement 那句没执行（RightUntil 没设 / tick 不匹配）
                    //   intentRight=true 但索引不等 → ApplyNativeControlFlags 的 whoAmI 判断把整段 return 掉了
                    //   三者都正常而 control 仍假        → 写了被同帧覆盖，得换写入时机
                    intentLeft = BridgeSystem.IntentLeft,
                    intentRight = BridgeSystem.IntentRight,
                    intentUp = BridgeSystem.IntentUp,
                    intentDown = BridgeSystem.IntentDown,
                    intentJump = BridgeSystem.IntentJump,
                    myPlayer = Main.myPlayer,
                    localWhoAmI = Main.LocalPlayer == null ? -1 : Main.LocalPlayer.whoAmI,
                    rightUntil = BridgeSystem.RightUntil,
                    gameTick = (long)Main.GameUpdateCount,
                    // ApplyNativeControlFlags() 返回瞬间的 control* 真值。命令线程读 control*
                    // 时原版已经重置过，只有这里是当帧证据。
                    flagTick = BridgeSystem.DbgFlagsTick,
                    flagLeft = BridgeSystem.DbgFlagsLeft,
                    flagRight = BridgeSystem.DbgFlagsRight,
                    flagUp = BridgeSystem.DbgFlagsUp,
                    flagDown = BridgeSystem.DbgFlagsDown,
                    flagJump = BridgeSystem.DbgFlagsJump,
                    flagIntentRight = BridgeSystem.DbgFlagsIntentRight,
                    flagIntentJump = BridgeSystem.DbgFlagsIntentJump,
                    flagRightUntilLive = BridgeSystem.DbgFlagsRightUntilLive,
                    flagJumpUntilLive = BridgeSystem.DbgFlagsJumpUntilLive,
                    // Player.Update 跑完之后（引擎已算完移动）最终采信的输入：
                    // 这一份才能回答「我写的 control 到底有没有被采纳」。
                    afterTick = BridgeSystem.DbgAfterTick,
                    preTick = BridgeSystem.DbgPreTick,
                    preCtrlLeft = BridgeSystem.DbgPreCtrlLeft,
                    preCtrlRight = BridgeSystem.DbgPreCtrlRight,
                    preCtrlUp = BridgeSystem.DbgPreCtrlUp,
                    preCtrlDown = BridgeSystem.DbgPreCtrlDown,
                    preCtrlJump = BridgeSystem.DbgPreCtrlJump,
                    preVelX = BridgeSystem.DbgPreVelX,
                    afterCtrlLeft = BridgeSystem.DbgAfterCtrlLeft,
                    afterCtrlRight = BridgeSystem.DbgAfterCtrlRight,
                    afterCtrlUp = BridgeSystem.DbgAfterCtrlUp,
                    afterCtrlDown = BridgeSystem.DbgAfterCtrlDown,
                    afterCtrlJump = BridgeSystem.DbgAfterCtrlJump,
                    afterTrigLeft = BridgeSystem.DbgAfterTrigLeft,
                    afterTrigRight = BridgeSystem.DbgAfterTrigRight,
                    afterTrigUp = BridgeSystem.DbgAfterTrigUp,
                    afterTrigDown = BridgeSystem.DbgAfterTrigDown,
                    afterTrigJump = BridgeSystem.DbgAfterTrigJump,
                    afterVelX = BridgeSystem.DbgAfterVelX,
                    prePhysicsTick = BridgeSystem.DbgPrePhysicsTick,
                    prePhysicsUp = BridgeSystem.DbgPrePhysicsUp,
                    prePhysicsDown = BridgeSystem.DbgPrePhysicsDown,
                    prePhysicsHook = BridgeSystem.DbgPrePhysicsHook,
                    prePhysicsMount = BridgeSystem.DbgPrePhysicsMount,
                    prePhysicsReleaseMount = BridgeSystem.DbgPrePhysicsReleaseMount,
                    prePhysicsUseItem = BridgeSystem.DbgPrePhysicsUseItem,
                    prePhysicsUseTile = BridgeSystem.DbgPrePhysicsUseTile,
                    postControlMount = BridgeSystem.DbgPostControlMount,
                    postReleaseMount = BridgeSystem.DbgPostReleaseMount,
                    // 出生点：魔镜/床这类回城手段的判据。没有这个坐标就只能靠"动了多远"猜，
                    // 而人本来就站在出生点旁边时，回城位移不到两格，会被误判成失败。
                    spawn = new { x = Main.spawnTileX, y = Main.spawnTileY },
                };
                // 绳子接触诊断（只读）：pulley/ropeCount 单独看不出来"人到底贴着哪一格绳"，
                // 所以把碰撞箱换算成格子坐标，再逐格查绳，并报左右脚底格的 tile type。
                data["ropeContact"] = RopeContact(pl);
                data["ropeRelease"] = new
                {
                    pending = BridgeSystem.RopeReleasePending,
                    untilTick = BridgeSystem.RopeReleaseUntil,
                    failed = BridgeSystem.RopeReleaseFailed,
                    failureTick = BridgeSystem.RopeReleaseFailureTick,
                };
                // 顶层别名：命令线程最容易直接读取 state.flag*；player 内的同名字段
                // 继续保留，兼容上一轮已经使用嵌套路径的客户端。
                data["flagTick"] = BridgeSystem.DbgFlagsTick;
                data["flagLeft"] = BridgeSystem.DbgFlagsLeft;
                data["flagRight"] = BridgeSystem.DbgFlagsRight;
                data["flagUp"] = BridgeSystem.DbgFlagsUp;
                data["flagDown"] = BridgeSystem.DbgFlagsDown;
                data["flagJump"] = BridgeSystem.DbgFlagsJump;
                data["flagIntentRight"] = BridgeSystem.DbgFlagsIntentRight;
                data["flagIntentJump"] = BridgeSystem.DbgFlagsIntentJump;
                data["flagRightUntilLive"] = BridgeSystem.DbgFlagsRightUntilLive;
                data["flagJumpUntilLive"] = BridgeSystem.DbgFlagsJumpUntilLive;
                data["afterTick"] = BridgeSystem.DbgAfterTick;
                data["preTick"] = BridgeSystem.DbgPreTick;
                data["preCtrlLeft"] = BridgeSystem.DbgPreCtrlLeft;
                data["preCtrlRight"] = BridgeSystem.DbgPreCtrlRight;
                data["preCtrlUp"] = BridgeSystem.DbgPreCtrlUp;
                data["preCtrlDown"] = BridgeSystem.DbgPreCtrlDown;
                data["preCtrlJump"] = BridgeSystem.DbgPreCtrlJump;
                data["preVelX"] = BridgeSystem.DbgPreVelX;
                data["afterCtrlLeft"] = BridgeSystem.DbgAfterCtrlLeft;
                data["afterCtrlRight"] = BridgeSystem.DbgAfterCtrlRight;
                data["afterCtrlUp"] = BridgeSystem.DbgAfterCtrlUp;
                data["afterCtrlDown"] = BridgeSystem.DbgAfterCtrlDown;
                data["afterCtrlJump"] = BridgeSystem.DbgAfterCtrlJump;
                data["afterTrigLeft"] = BridgeSystem.DbgAfterTrigLeft;
                data["afterTrigRight"] = BridgeSystem.DbgAfterTrigRight;
                data["afterTrigUp"] = BridgeSystem.DbgAfterTrigUp;
                data["afterTrigDown"] = BridgeSystem.DbgAfterTrigDown;
                data["afterTrigJump"] = BridgeSystem.DbgAfterTrigJump;
                data["afterVelX"] = BridgeSystem.DbgAfterVelX;
                data["prePhysicsTick"] = BridgeSystem.DbgPrePhysicsTick;
                data["prePhysicsUp"] = BridgeSystem.DbgPrePhysicsUp;
                data["prePhysicsDown"] = BridgeSystem.DbgPrePhysicsDown;
                data["prePhysicsHook"] = BridgeSystem.DbgPrePhysicsHook;
                data["prePhysicsMount"] = BridgeSystem.DbgPrePhysicsMount;
                data["prePhysicsReleaseMount"] = BridgeSystem.DbgPrePhysicsReleaseMount;
                data["prePhysicsUseItem"] = BridgeSystem.DbgPrePhysicsUseItem;
                data["prePhysicsUseTile"] = BridgeSystem.DbgPrePhysicsUseTile;
                data["postControlMount"] = BridgeSystem.DbgPostControlMount;
                data["postReleaseMount"] = BridgeSystem.DbgPostReleaseMount;
                // 坐骑诊断：直写通路是否真的走过一次边沿。hasFocus 必须一起记——
                // 硬规则 §0.56 要求"失焦通过才算通过"，有焦点的成功是假证据。
                data["mountUntil"] = BridgeSystem.MountUntil;
                data["mountUntilLive"] = BridgeSystem.MountUntil > BridgeSystem.Now;
                data["mountFired"] = BridgeSystem.MountFired;
                data["inventoryUntil"] = BridgeSystem.InventoryUntil;
                data["inventoryUntilLive"] = BridgeSystem.InventoryUntil > BridgeSystem.Now;
                data["hasFocus"] = Main.hasFocus;
                data["inputDiag"] = InputDiagnostics(pl);
                data["guard"] = new
                {
                    active = BridgeSystem.GuardUntil > BridgeSystem.Now,
                    guardDefault = BridgeSystem.GuardDefault,
                    auto = BridgeSystem.GuardAuto,
                    resetReason = BridgeSystem.LastResetReason,
                    resetCount = BridgeSystem.ResetCount,
                    latched = new
                    {
                        left = BridgeSystem.LeftUntil > BridgeSystem.Now,
                        right = BridgeSystem.RightUntil > BridgeSystem.Now,
                        up = BridgeSystem.UpUntil > BridgeSystem.Now,
                        down = BridgeSystem.DownUntil > BridgeSystem.Now,
                        jump = BridgeSystem.JumpUntil > BridgeSystem.Now,
                        use = BridgeSystem.UseUntil > BridgeSystem.Now,
                        climbUp = BridgeSystem.ClimbUpUntil > BridgeSystem.Now,
                        climbDown = BridgeSystem.ClimbDownUntil > BridgeSystem.Now,
                        travel = BridgeSystem.Travel.Active,
                    },
                    engaged = BridgeSystem.GuardEngaged,
                    target = BridgeSystem.GuardLastTarget,
                    radius = BridgeSystem.GuardRadius,
                    swings = BridgeSystem.GuardSwings,
                    weaponSlot = BridgeSystem.GuardWeaponSlot,
                };
                data["attack"] = BridgeSystem.AttackStatus();
                data["path"] = new
                {
                    active = BridgeSystem.PathActive,
                    index = BridgeSystem.PathIndex,
                    total = BridgeSystem.PathCells?.Count ?? 0,
                    stop = BridgeSystem.PathStop,
                };
                data["walk"] = new
                {
                    active = BridgeSystem.WalkActive,
                    target = new { x = BridgeSystem.WalkX, y = BridgeSystem.WalkY },
                    tol = BridgeSystem.WalkTol,
                    stop = BridgeSystem.WalkStop,
                    waypoint = BridgeSystem.WalkHasWp ? new { x = BridgeSystem.WalkWpX, y = BridgeSystem.WalkWpY } : null,
                    repaths = BridgeSystem.WalkRepaths,
                    stuck = BridgeSystem.WalkStuckTicks,
                    pathWhy = BridgeSystem.LastPathWhy,
                    follow = new
                    {
                        active = BridgeSystem.FollowActive,
                        targetIndex = BridgeSystem.FollowTargetIdx,
                        targetName = BridgeSystem.FollowTargetName,
                        mode = BridgeSystem.FollowMode,
                        noPathTicks = BridgeSystem.FollowNoPathTicks,
                        stuckTicks = BridgeSystem.FollowStuckTicks,
                        lastWhy = BridgeSystem.FollowLastWhy,
                    },
                };
                data["exact"] = new
                {
                    active = BridgeSystem.ExactActive,
                    targetX = MathF.Round(BridgeSystem.ExactTargetX, 3),
                    phase = BridgeSystem.ExactPhase == 0 ? "drive" : (BridgeSystem.ExactPhase == 1 ? "coast" : "correct"),
                    remaining = MathF.Round(BridgeSystem.ExactRemaining, 3),
                    brakeDistance = MathF.Round(BridgeSystem.ExactBrakeDistance, 3),
                    stuckTicks = BridgeSystem.ExactStuckTicks,
                    stopReason = BridgeSystem.ExactStopReason,
                    untilTick = BridgeSystem.ExactUntil,
                };
                // Diagnostics: when a player-mode action "swings but nothing happens",
                // this says whether the synthetic input was actually active.
                data["input"] = new
                {
                    hasFocus = Main.hasFocus,
                    aim = BridgeSystem.AimTileX >= 0 ? new[] { BridgeSystem.AimTileX, BridgeSystem.AimTileY } : null,
                    target = new[] { Player.tileTargetX, Player.tileTargetY },
                    preItemCalls = BridgeSystem.PreItemCalls,
                    setControlCalls = BridgeSystem.SetControlCalls,
                    synthCalls = BridgeSystem.SynthCalls,
                    controlUseItem = pl.controlUseItem,
                    releaseUseItem = pl.releaseUseItem,
                };
            }
            else
            {
                data["hint"] = "还在主菜单：先用菜单进世界（单人模式 → 选人物 → 选世界），之后 world/player 才有值。";
            }

            return Json.Ok(data);
        }

        private static string[] Zones(Player p)
        {
            var list = new List<string>();
            if (p.ZoneCorrupt) list.Add("corrupt");
            if (p.ZoneCrimson) list.Add("crimson");
            if (p.ZoneJungle) list.Add("jungle");
            if (p.ZoneSnow) list.Add("snow");
            if (p.ZoneDesert) list.Add("desert");
            if (p.ZoneBeach) list.Add("beach");
            if (p.ZoneDungeon) list.Add("dungeon");
            if (p.ZoneGlowshroom) list.Add("glowshroom");
            if (p.ZoneMeteor) list.Add("meteor");
            if (p.ZoneHallow) list.Add("hallow");
            if (p.ZoneUnderworldHeight) list.Add("underworld");
            if (p.ZoneRockLayerHeight) list.Add("rocklayer");
            else if (p.ZoneDirtLayerHeight) list.Add("dirtlayer");
            if (p.ZoneSkyHeight) list.Add("sky");
            list.Add(p.ZoneOverworldHeight ? "surface" : "underground");
            return list.ToArray();
        }

        private static object[] Buffs(Player pl)
        {
            var list = new List<object>();
            for (int i = 0; i < pl.buffType.Length; i++)
            {
                if (pl.buffType[i] > 0 && pl.buffTime[i] > 0)
                    list.Add(new { type = pl.buffType[i], name = BuffID.Search.GetName(pl.buffType[i]), time = pl.buffTime[i], timeSec = Math.Round(pl.buffTime[i] / 60f, 1) });
            }
            return list.ToArray();
        }

        /* ── 世界读写 ─────────────────────────────────────────────────── */

        /* ── 区域分析：把 96×96 格压成十几行结论 ────────────────────────
         * 为什么需要它：原始 tile 网格给模型消费不起（上万 token、几十秒），
         * 但"我在什么地方、能不能出去、怪在哪"是几十毫秒能算完的。
         * 算法是一次 BFS：从玩家位置出发，只走身体放得下的格子，
         * 数可达格数、找接触扫描边界的缺口、量头顶和脚下的空间。
         * 只读：不改任何 tile，不写任何东西。
         */
        /// <summary>实心判定：有 tile 且 tileSolid(树/草/火把/平台/门都不算)。</summary>
        private static bool SolidAt(int x, int y)
        {
            if (!InBounds(x, y)) return true;
            Tile t = Main.tile[x, y];
            return t.HasTile && Main.tileSolid[t.TileType];
        }

        /// <summary>
        /// 找"掉进去出不来的坑"：竖井判据全部用 tileSolid，不看形状。
        /// 为什么要这条命令：ascii 只能看形状、丢类型，树干会被当成墙/地面，
        /// 坑藏在树底下看不见（实测连错三次）。这里直接返回坐标，不再靠读图。
        /// 判据：连续空 >= minDepth（裸跳约 5.5 格，默认 6）、下面有实心底、
        ///       下半段至少一侧有 >= max(2, 深度/2) 格连续实心壁。
        /// </summary>
        private static string FindPit(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int cx = Json.Int(p, "x", (int)(pl.Center.X / 16f));
            int cy = Json.Int(p, "y", (int)(pl.Center.Y / 16f));
            int rad = Math.Clamp(Json.Int(p, "r", 24), 1, 48);
            int minDepth = Math.Clamp(Json.Int(p, "minDepth", 6), 2, 40);
            int x0 = cx - rad, y0 = cy - rad, w = rad * 2 + 1, h = rad * 2 + 1;

            var raw = new List<int[]>();   // x, top, depth, floorY, wallL, wallR
            for (int i = 0; i < w; i++)
            {
                int x = x0 + i;
                if (!InBounds(x, cy)) continue;
                for (int j = 0; j < h; j++)
                {
                    int top = y0 + j;
                    if (!InBounds(x, top) || SolidAt(x, top)) continue;
                    int d = 0;
                    while (d < h - j)
                    {
                        int yy = top + d;
                        if (!InBounds(x, yy) || SolidAt(x, yy)) break;
                        d++;
                    }
                    int bottom = top + d;
                    if (d >= minDepth && InBounds(x, bottom) && SolidAt(x, bottom))
                    {
                        int mid = top + d / 2;
                        int lw = 0, rw = 0;
                        for (int k = mid; k < bottom; k++) { if (SolidAt(x - 1, k)) lw++; else break; }
                        for (int k = mid; k < bottom; k++) { if (SolidAt(x + 1, k)) rw++; else break; }
                        int need = Math.Max(2, d / 2);
                        if (Math.Max(lw, rw) >= need) raw.Add(new[] { x, top, d, bottom, lw, rw });
                    }
                    j += Math.Max(d, 1) - 1;
                }
            }

            // 相邻列合并成"井口"
            raw.Sort((a, b) => a[0] != b[0] ? a[0] - b[0] : b[2] - a[2]);
            var groups = new List<int[]>();   // x0, x1, depth, floorY
            foreach (var q in raw)
            {
                if (groups.Count > 0)
                {
                    int[] g = groups[groups.Count - 1];
                    if (q[0] - g[1] <= 1 && Math.Abs(q[2] - g[2]) <= 2)
                    {
                        g[1] = q[0];
                        if (q[2] > g[2]) { g[2] = q[2]; g[3] = q[3]; }
                        continue;
                    }
                }
                groups.Add(new[] { q[0], q[0], q[2], q[3] });
            }

            var pits = new List<object>();
            foreach (var g in groups)
            {
                int gx = (g[0] + g[1]) / 2;
                pits.Add(new { x0 = g[0], x1 = g[1], center = gx, depth = g[2], floorY = g[3], dist = Math.Abs(gx - cx) });
            }
            return Json.Ok(new
            {
                center = new { x = cx, y = cy },
                r = rad,
                minDepth,
                count = pits.Count,
                pits,
                note = count_note(pits.Count, minDepth),
            });
        }

        private static string count_note(int n, int minDepth)
        {
            return n == 0
                ? $"半径内没有 >= {minDepth} 格深、有底的竖井。想找洞穴竖井可以把 minDepth 降到 4，或先 scan regions 看 depthBelow。"
                : "pits 里 x0..x1 是井口横向范围，depth 是深度，floorY 是坑底；掉进去前先 walkto 到 center 附近。";
        }

        private static string RegionReport(int cx, int cy, int r)
        {
            int x0 = cx - r, y0 = cy - r, side = r * 2 + 1;
            if (cx < 0 || cy < 0 || cx >= Main.maxTilesX || cy >= Main.maxTilesY)
                return Json.Error($"坐标超出世界范围：({cx},{cy})");

            var seen = new bool[side, side];
            var q = new Queue<int>();
            int sx = cx - x0, sy = cy - y0;
            if (sx >= 0 && sy >= 0 && sx < side && sy < side && BridgeSystem.PathBodyFree(cx, cy))
            {
                seen[sy, sx] = true;
                q.Enqueue(sy * side + sx);
            }

            int cells = 0, standable = 0;
            int minI = side, maxI = -1, minJ = side, maxJ = -1;
            while (q.Count > 0)
            {
                int id = q.Dequeue();
                int j = id / side, i = id % side;
                int x = x0 + i, y = y0 + j;
                cells++;
                if (BridgeSystem.PathFloorAt(x, y)) standable++;
                if (i < minI) minI = i;
                if (i > maxI) maxI = i;
                if (j < minJ) minJ = j;
                if (j > maxJ) maxJ = j;

                foreach (int d in new[] { -1, 1, -side, side })
                {
                    int nid = id + d;
                    if (nid < 0 || nid >= side * side) continue;
                    int nj = nid / side, ni = nid % side;
                    // 只走上下左右，且不许跨行首尾相接
                    if (d == -1 && ni == side - 1) continue;
                    if (d == 1 && ni == 0) continue;
                    if (seen[nj, ni]) continue;
                    if (!BridgeSystem.PathBodyFree(x0 + ni, y0 + nj)) continue;
                    seen[nj, ni] = true;
                    q.Enqueue(nid);
                }
            }

            // 出口 = 可达集合碰到扫描边界的地方。碰到边界说明这个空间没有在
            // 扫描范围内被围住，是通向外面的缺口。
            var exits = new List<object>();
            int touchL = 0, touchR = 0, touchU = 0, touchD = 0;
            for (int j = 0; j < side; j++)
            {
                for (int i = 0; i < side; i++)
                {
                    if (!seen[j, i]) continue;
                    if (i == 0) touchL++;
                    if (i == side - 1) touchR++;
                    if (j == 0) touchU++;
                    if (j == side - 1) touchD++;
                }
            }
            if (touchL > 0) exits.Add(new { dir = "left", cells = touchL, dist = minI });
            if (touchR > 0) exits.Add(new { dir = "right", cells = touchR, dist = side - 1 - maxI });
            if (touchU > 0) exits.Add(new { dir = "up", cells = touchU, dist = minJ });
            if (touchD > 0) exits.Add(new { dir = "down", cells = touchD, dist = side - 1 - maxJ });

            // 脚下有多深（连续没有站脚面），头顶有多高
            int depthBelow = 0;
            for (int k = 1; k <= 40; k++)
            {
                int y = cy + k;
                if (y >= Main.maxTilesY) break;
                if (!BridgeSystem.PathBodyFree(cx, y)) break;
                depthBelow++;
                if (BridgeSystem.PathFloorAt(cx, y)) break;
            }
            int ceilingAbove = 0;
            for (int k = 1; k <= 24; k++)
            {
                int y = cy - k;
                if (y < 0) break;
                if (!BridgeSystem.PathBodyFree(cx, y)) break;
                ceilingAbove++;
            }

            bool enclosed = exits.Count == 0;
            string verdict =
                !enclosed ? "open" :
                depthBelow >= 3 ? "closed_pit" :
                cells <= 12 ? "trapped" : "closed_room";

            // 威胁：半径内活着的、非友方、非小动物
            Player pl = P;
            var threats = new List<object>();
            if (pl != null && pl.active)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.friendly || n.townNPC || n.CountsAsACritter) continue;
                    int nx = (int)(n.Center.X / 16f), ny = (int)(n.Center.Y / 16f);
                    int dx = nx - cx, dy = ny - cy;
                    int dist = (int)System.Math.Round(System.Math.Sqrt(dx * dx + dy * dy));
                    if (dist > r) continue;
                    threats.Add(new
                    {
                        name = n.FullName, who = i, at = new[] { nx, ny }, dist,
                        hp = n.life,
                        incoming = n.velocity.Length() > 0.2f && (n.velocity.X * -dx + n.velocity.Y * -dy) > 0,
                    });
                }
            }

            return Json.Ok(new
            {
                at = new[] { cx, cy },
                radius = r,
                cells,
                standable,
                enclosed,
                verdict,
                exits,
                depthBelow,
                ceilingAbove,
                threats,
                note = "cells/standable 是玩家当前位置能连通的格子数；enclosed=true 表示扫描范围内没找到出口",
            });
        }

        private static string Scan(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            Player pl = P;
            int defX = pl != null && pl.active ? (int)(pl.Center.X / 16f) : Main.spawnTileX;
            int defY = pl != null && pl.active ? (int)(pl.Center.Y / 16f) : Main.spawnTileY;
            int cx = Json.Int(p, "x", defX);
            int cy = Json.Int(p, "y", defY);
            int r = Math.Clamp(Json.Int(p, "r", 12), 1, 48);
            bool withWalls = Json.Bool(p, "walls", true);
            string format = (Json.Str(p, "format", "rows") ?? "rows").Trim().ToLowerInvariant();
            if (format != "rows" && format != "ascii" && format != "rle" && format != "regions") format = "rows";
            if (format == "regions") return RegionReport(cx, cy, r);

            int x0 = cx - r, y0 = cy - r, w = r * 2 + 1, h = r * 2 + 1;
            var tiles = new Tile[h, w];
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    int x = x0 + i, y = y0 + j;
                    if (!InBounds(x, y)) continue;
                    tiles[j, i] = Main.tile[x, y];
                }
            }

            var data = new Dictionary<string, object>
            {
                ["x0"] = x0,
                ["y0"] = y0,
                ["w"] = w,
                ["h"] = h,
                ["format"] = format,
                ["center"] = new[] { cx, cy },
                ["playerTile"] = pl != null && pl.active ? new[] { (int)(pl.Center.X / 16f), (int)(pl.Center.Y / 16f) } : null,
            };

            if (format == "rows")
            {
                int stride = withWalls ? 5 : 3;
                var rows = new List<int[]>(h);
                for (int j = 0; j < h; j++)
                {
                    var row = new int[w * stride];
                    for (int i = 0; i < w; i++)
                    {
                        Tile t = tiles[j, i];
                        int k = i * stride;
                        row[k] = InBounds(x0 + i, y0 + j) ? TileCode(t) : EmptyTile;
                        if (withWalls)
                        {
                            row[k + 1] = t.WallType;
                            row[k + 2] = t.LiquidAmount;
                            row[k + 3] = t.LiquidAmount > 0 ? t.LiquidType : 0;
                            row[k + 4] = ShapeCode(t);
                        }
                        else
                        {
                            row[k + 1] = t.LiquidAmount;
                            row[k + 2] = t.LiquidAmount > 0 ? t.LiquidType : 0;
                        }
                    }
                    rows.Add(row);
                }
                data["stride"] = stride;
                data["fields"] = withWalls ? new[] { "type", "wall", "liquid", "liqType", "shape" } : new[] { "type", "liquid", "liqType" };
                data["tiles"] = rows;
                data["legend"] = "type=-1 空，其余是 Terraria 方块 ID（0=土块）；"
                    + "tiles[j] 是 y=y0+j 这一行，第 i 格在 tiles[j][i*stride .. i*stride+stride-1]；"
                    + "liqType 0=水 1=岩浆 2=蜂蜜 3=微光（只有 liquid>0 时才有意义）；"
                    + "shape 0=实心 1=半砖 2..5=斜坡；liquid 0-255";
            }
            else if (format == "ascii")
            {
                var lines = new string[h];
                for (int j = 0; j < h; j++)
                {
                    var sb = new System.Text.StringBuilder(w);
                    for (int i = 0; i < w; i++)
                    {
                        if (!InBounds(x0 + i, y0 + j)) { sb.Append(' '); continue; }
                        Tile t = tiles[j, i];
                        if (t.LiquidAmount > 0) { sb.Append(t.LiquidType == LiquidID.Lava ? 'L' : '~'); continue; }
                        if (!t.HasTile) { sb.Append('.'); continue; }
                        // 非实心方块不能画成 '#':否则树/草/门/平台看起来和石墙一样,
                        // 读到图的人会把整片树林当成墙(实测因此砍错树、把树干读成断崖)。
                        if (!Main.tileSolid[t.TileType])
                        {
                            int tt = t.TileType;
                            char ch;
                            if (tt == TileID.Trees) ch = 'T';
                            else if (tt == TileID.Plants || tt == TileID.Plants2) ch = ',';
                            else if (tt == TileID.Platforms) ch = '-';
                            else if (tt == TileID.ClosedDoor || tt == TileID.OpenDoor) ch = '+';
                            else if (tt == TileID.Torches) ch = 'o';
                            else if (tt == TileID.Vines) ch = 'v';
                            else ch = ';';
                            sb.Append(ch);
                            continue;
                        }
                        sb.Append(t.IsHalfBlock || t.Slope != SlopeType.Solid ? '^' : '#');
                    }
                    lines[j] = sb.ToString();
                }
                data["map"] = string.Join("\n", lines);
                data["legend"] = ". 空 · # 实心(可挡可站) · ^ 半砖/斜坡 · T 树 · , 草/植物 · - 平台 · + 门 · o 火把 · v 藤蔓 · ; 其他非实心 · ~ 水 · L 岩浆";
            }
            else
            {
                var lines = new string[h];
                for (int j = 0; j < h; j++)
                {
                    var sb = new System.Text.StringBuilder();
                    int runType = 0, runLen = 0;
                    for (int i = 0; i < w; i++)
                    {
                        int code = InBounds(x0 + i, y0 + j) ? TileCode(tiles[j, i]) : EmptyTile;
                        if (runLen > 0 && code == runType) { runLen++; continue; }
                        if (runLen > 0) sb.Append(runLen).Append('x').Append(runType).Append(',');
                        runType = code; runLen = 1;
                    }
                    if (runLen > 0) sb.Append(runLen).Append('x').Append(runType);
                    lines[j] = sb.ToString();
                }
                data["map"] = lines;
                data["legend"] = "每行从 x0 往右读：\"<数量>x<type>\"，type=-1 为空；map[j] 对应 y=y0+j";
            }

            return Json.Ok(data);
        }

        /// <summary>
        /// 重力/物理诊断：把原版判定"我这只脚站不站得住"用到的字段全打印出来。
        /// 用来定位 "grounded:true 但脚下全空、坐标一格不动" 这类卡死。
        /// </summary>
        private static string Gravity(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int cx = (int)(pl.Center.X / 16f), cy = (int)(pl.Center.Y / 16f);
            var feet = new List<object>();
            for (int x = cx - 1; x <= cx + 1; x++)
            {
                for (int y = cy + 1; y <= cy + 3; y++)
                {
                    if (!InBounds(x, y)) continue;
                    Tile t = Main.tile[x, y];
                    feet.Add(new { x, y, hasTile = t.HasTile, type = (int)t.TileType, solid = Main.tileSolid[t.TileType], solidTop = Main.tileSolidTop[t.TileType], slope = (int)t.Slope });
                }
            }
            var data = new
            {
                pos = new[] { MathF.Round(pl.position.X / 16f, 2), MathF.Round(pl.position.Y / 16f, 2) },
                center = new[] { MathF.Round(pl.Center.X / 16f, 2), MathF.Round(pl.Center.Y / 16f, 2) },
                velocity = new[] { MathF.Round(pl.velocity.X, 3), MathF.Round(pl.velocity.Y, 3) },
                oldVelocity = new[] { MathF.Round(pl.oldVelocity.X, 3), MathF.Round(pl.oldVelocity.Y, 3) },
                grounded = pl.velocity.Y == 0f,
                standing = pl.Bottom.Y / 16f,
                controlJump = pl.controlJump,
                controlDown = pl.controlDown,
                controlLeft = pl.controlLeft,
                controlRight = pl.controlRight,
                controlUseItem = pl.controlUseItem,
                mount = pl.mount?.Type ?? -1,
                webbed = pl.webbed,
                wet = pl.wet,
                fallStart = pl.fallStart,
                wingTime = pl.wingTime,
                jump = pl.jump,
                jumpSpeed = Player.jumpSpeed,
                updateCount = Main.GameUpdateCount,
                frameVel = Main.player[Main.myPlayer]?.velocity is Vector2 v ? new[] { MathF.Round(v.X, 3), MathF.Round(v.Y, 3) } : null,
                feet,
            };
            return Json.Ok(data);
        }

        private static string TileAt(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            // Never default a coordinate: (0,0) is a real tile at the world's top-left corner,
            // so a malformed call would answer confidently about the wrong place.
            int x = Json.Int(p, "x", int.MinValue), y = Json.Int(p, "y", int.MinValue);
            if (x == int.MinValue || y == int.MinValue) return Json.Error("tile 需要 x,y（方块坐标）");
            if (!InBounds(x, y)) return Json.Error($"out of world bounds: {x},{y}");
            Tile t = Main.tile[x, y];
            return Json.Ok(new
            {
                x,
                y,
                hasTile = t.HasTile,
                type = TileCode(t),
                name = t.HasTile ? TileID.Search.GetName(t.TileType) : "",
                wall = t.WallType,
                // 原版对「这面墙算不算有效住房墙」的唯一权威答案：Main.wallHouse[wallType]。
                // 地图自带的泥土墙/岩石原生墙是 false，玩家摆的木墙/石砖墙是 true。
                // 只看 wall != 0 会把自然墙当住房墙，房子永远判定不合法。
                wallHouse = t.WallType > 0 && Main.wallHouse[t.WallType],
                liquid = t.LiquidAmount,
                // 液体类型（水/岩浆/蜂蜜/微光）。只有数量读不出"这格会不会烧死我"，
                // 下地狱、挖狱石、找微光池都要靠这个。
                liquidType = t.LiquidAmount > 0 ? t.LiquidType : 0,
                liquidName = t.LiquidAmount > 0 ? LiquidName(t.LiquidType) : "",
                slope = (int)t.Slope,
                halfBlock = t.IsHalfBlock,
                actuator = t.HasActuator,
                // The game's own answer to "can I stand on / build against this?", instead of
                // guessing from type ids (plants and tombstones are tiles but not support).
                solid = t.HasTile && Main.tileSolid[t.TileType],
                solidTop = t.HasTile && Main.tileSolidTop[t.TileType],
                frameX = t.TileFrameX,
                frameY = t.TileFrameY,
            });
        }

        /* Terraria 的 inventory 有 59 格：0-49 主背包，50-53 货币，54-57 弹药，58 垃圾桶。
         * 找"能拿在手里的东西"、找"能塞新物品的空位"只能在主背包里找。
         * 实测事故：合成把工作台塞进 58（垃圾桶），place 又把 selectedItem 设成 58，
         * 原版手里拿到的是一格垃圾 —— 回包 armed:true，格子却怎么都放不上。*/
        private const int MainBagSlots = 50;
        /* ── 拟人操作：找工具/材料，掏出来，瞄准，挥动 ─────────────────── */

        private static int FindPickaxe(Player pl)
        {
            for (int i = 0; i < MainBagSlots; i++)
                if (pl.inventory[i] != null && pl.inventory[i].type > 0 && pl.inventory[i].pick > 0) return i;
            return -1;
        }

        private static int FindAxe(Player pl)
        {
            for (int i = 0; i < MainBagSlots; i++)
                if (pl.inventory[i] != null && pl.inventory[i].type > 0 && pl.inventory[i].axe > 0) return i;
            return -1;
        }

        private static int FindItemForTile(Player pl, int tileType)
        {
            for (int i = 0; i < MainBagSlots; i++)
            {
                Item it = pl.inventory[i];
                if (it != null && it.type > 0 && it.stack > 0 && it.createTile == tileType) return i;
            }
            return -1;
        }

        private static int FindItemForWall(Player pl, int wallType)
        {
            for (int i = 0; i < MainBagSlots; i++)
            {
                Item it = pl.inventory[i];
                if (it != null && it.type > 0 && it.stack > 0 && it.createWall == wallType) return i;
            }
            return -1;
        }

        private static string StartSwing(int slot, int x, int y, int ms, bool right, bool stopWhenClear = false, bool stopWhenPlaced = false)
        {
            Player pl = P;
            pl.selectedItem = slot;
            BridgeSystem.UseStopWhenClear = stopWhenClear;
            BridgeSystem.UseStopWhenPlaced = stopWhenPlaced;
            BridgeSystem.SetAim(x, y);
            bool onScreen = BridgeSystem.ApplyAim();
            ulong until = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            BridgeSystem.AimUntil = until + 40;

            // 合成输入，从 PreItemCheck 里按下使用标记：不抢焦点、不动用户的光标，
            // 由原版 ItemCheck 自己挥动/放置/挖矿。这是唯一一条输入通路。
            if (right) { BridgeSystem.UseTileUntil = until; BridgeSystem.UseTileJustStarted = true; }
            else { BridgeSystem.UseUntil = until; BridgeSystem.UseJustStarted = true; }

            Item held = pl.inventory[slot];
            return Json.Ok(new
            {
                mode = "player",
                x,
                y,
                slot,
                held = held == null ? null : new { type = held.type, name = held.Name, stack = held.stack },
                right,
                ms,
                untilTick = until,
                onScreen,
                hint = "合成输入：不需要前台，也不会动你的鼠标；等一下用 tile / inventory 确认结果。",
            });
        }

        /// <summary>Arm a swing with whatever tool the tile needs.</summary>
        private static string BeginPlayerMine(int x, int y, int ms)
        {
            Player pl = P;
            if (pl.dead) return "玩家已死亡，请先复活";
            Tile t = Main.tile[x, y];
            // Which tool a tile needs is a game fact, not a guess: Main.tileAxe marks every
            // tree-like tile (palm, vanity trees, cactus...). Checking only TileID.Trees made the
            // agent swing a pickaxe at a vanity tree and conclude the tile was unbreakable.
            // Main.tileAxe 在这个版本里并不包含 Trees(5)：实测对着树干挥镐，格子消失但
            // 一根木头都不掉——那是"用错工具"，不是"没有掉落"。所以树木类统一按砍伐处理。
            bool needsAxe = t.HasTile && (Main.tileAxe[t.TileType]
                || (t.TileType < TileID.Sets.IsATreeTrunk.Length && TileID.Sets.IsATreeTrunk[t.TileType])
                || t.TileType == TileID.Cactus);
            // 斧头的有效距离比镐短：实测同一棵树，4.3 格外挥斧游戏接指令但格子不掉，
            // 走到 1.5 格内立刻掉。所以砍树单独用 4 格判定，别让人白挥。
            int reach = needsAxe ? 4 : 7;
            if (!BridgeSystem.InReach(pl, x, y, reach))
                return needsAxe
                    ? $"砍树要靠近：离这一格超过 {reach} 格，先走过去再砍"
                    : "够不到：离这一格超过 7 格，先走过去再挖";
            int slot = needsAxe ? FindAxe(pl) : FindPickaxe(pl);
            if (slot < 0) return needsAxe ? "背包里没有斧头，砍树需要斧头" : "背包里没有镐子，挖方块需要镐子";

            // 唯一通路：合成控制标记 → 原版 ItemCheck 自己挥动。没有 PickTile 捷径，
            // 也不碰操作系统输入（不动用户光标、不抢焦点）。
            return StartSwing(slot, x, y, ms, false, true);
        }

        /// <summary>
        /// Use the Magic Mirror like a player: it must be in the inventory, it is selected and
        /// animated, and the teleport is the same call the item ends with (player.Spawn()).
        /// </summary>
        /// <summary>
        /// Ask the game itself whether a cell belongs to a valid house, instead of guessing from
        /// geometry: WorldGen.StartRoomCheck is the same call the housing menu uses. Also reports
        /// which town NPCs are homeless and where the others live, so "no NPC moved in" becomes
        /// diagnosable (room invalid vs room taken).
        /// </summary>
        /// <summary>
        /// List items lying on the ground near the player. Without this the agent cannot tell
        /// "the drop never spawned" from "I walked past it", which is a real distinction.
        /// </summary>
        private static string DroppedItems(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int radius = Math.Clamp(Json.Int(p, "r", 40), 1, 200);
            Player pl = P;
            var list = new List<object>();
            for (int i = 0; i < Main.maxItems; i++)
            {
                Item it = Main.item[i];
                if (it == null || !it.active || it.stack <= 0) continue;
                float dist = Vector2.Distance(it.Center, pl.Center) / 16f;
                if (dist > radius) continue;
                list.Add(new
                {
                    who = i,
                    name = it.Name,
                    type = it.type,
                    stack = it.stack,
                    tile = new { x = it.Center.X / 16f, y = it.Center.Y / 16f },
                    dist,
                });
            }
            return Json.Ok(new { count = list.Count, items = list });
        }

        /// <summary>
        /// 捡起脚下够得着的地面掉落物。走原版 GetItem —— 玩家碰到物品时游戏自己调的那个函数，
        /// 所以这是"控制角色去捡"，不是凭空生成。掉落物有时压在地里不自动入包，没有这条命令
        /// 就分不清"没掉"和"掉在脚下却捡不到"。
        /// </summary>
        private static string GrabLoot(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int radius = Math.Clamp(Json.Int(p, "r", 8), 1, 60);
            Player pl = P;
            var got = new List<object>();
            int seen = 0;
            // 诊断用：世界里有几个活着的掉落物、离我最近的是谁、多远。picked=0 时
            // 光看 picked 分不清"没掉出来""掉下去了""压根没进 Main.item"。
            int actives = 0;
            string nearestName = null;
            float nearestDist = -1f;
            for (int i = 0; i < Main.maxItems; i++)
            {
                Item it = Main.item[i];
                if (it == null || !it.active || it.stack <= 0 || it.type <= 0) continue;
                actives++;
                float d = Vector2.Distance(it.Center, pl.Center) / 16f;
                if (nearestDist < 0f || d < nearestDist) { nearestDist = d; nearestName = it.Name; }
                if (d > radius) continue;
                seen++;
                string name = it.Name;
                int type = it.type;
                int before = it.stack;
                // 掉落物可能还挂着"属于别的玩家"的预约或落地延迟，那两条会让原版
                // GetItem 直接返回；清掉之后才是玩家真的走过去碰它会发生的事。
                it.playerIndexTheItemIsReservedFor = (byte)pl.whoAmI;
                it.noGrabDelay = 0;
                pl.GetItem(pl.whoAmI, it, GetItemSettings.PickupItemFromWorld);
                bool gone = !it.active || it.stack <= 0 || it.type != type;
                if (gone || it.stack < before)
                    got.Add(new { type, name, stack = before - (gone ? 0 : it.stack) });
            }
            return Json.Ok(new
            {
                picked = got.Count,
                seen,
                radius,
                actives,
                nearest = nearestName,
                nearestDist = nearestDist < 0 ? (float?)null : (float)Math.Round(nearestDist, 1),
                items = got,
                diag = new { pl.whoAmI, Main.myPlayer, Main.netMode },
            });
        }

        /// <summary>
        /// Save the world through the game's own writer. Killing the host process loses every
        /// tile change made since the last autosave, so anything that restarts the game must
        /// call this first and check that it really happened.
        /// </summary>
        /// <summary>
        /// Place a multi-tile object through the game's own WorldGen.PlaceObject, which is what the
        /// item path calls under the hood but without the item-use timing/aim conditions. Returns
        /// the game's own verdict, so a failure is visible instead of silent.
        /// </summary>
        /* ── 箱子 ────────────────────────────────────────────────────────
         * 早期装备、硬币、材料大多在箱子里；没有这个命令就只能挖矿攒，效率极低。
         * 只操作玩家够得着的真实箱子（Main.chest），搬运走物品的真实克隆，不凭空生成。
         */
        private static Chest NearChest(Player pl, out int index)
        {
            index = -1;
            float best = 6f * 16f;
            for (int i = 0; i < Main.maxChests; i++)
            {
                Chest c = Main.chest[i];
                if (c == null) continue;
                float d = Vector2.Distance(pl.Center, new Vector2(c.x * 16f + 16f, c.y * 16f + 16f));
                if (d < best) { best = d; index = i; }
            }
            return index >= 0 ? Main.chest[index] : null;
        }


        /// <summary>只读：列出某个类型的公开/非公开方法名（调试用）。</summary>

        /// <summary>只读：列出某个类型的字段名（调试用）。</summary>
        private static string FieldsCmd(JsonElement p)
        {
            string typeName = Json.Str(p, "type") ?? "Terraria.Main";
            string filter = Json.Str(p, "filter") ?? "";
            Type t = Type.GetType(typeName, false) ?? typeof(Terraria.Player).Assembly.GetType(typeName);
            var names = new List<string>();
            if (t != null)
            {
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                          | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
                foreach (var f in t.GetFields(flags))
                    if (filter.Length == 0 || f.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        names.Add(f.Name + ":" + f.FieldType.Name);
            }
            names.Sort(StringComparer.Ordinal);
            return Json.Ok(new { type = typeName, found = t != null, count = names.Count, fields = names });
        }

        private static string MethodsCmd(JsonElement p)
        {
            string typeName = Json.Str(p, "type") ?? "Terraria.Player";
            string filter = Json.Str(p, "filter") ?? "";
            Type t = Type.GetType(typeName, false) ?? typeof(Terraria.Player).Assembly.GetType(typeName);
            var names = new List<string>();
            if (t != null)
            {
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                          | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
                foreach (var m in t.GetMethods(flags))
                    if (filter.Length == 0 || m.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        names.Add(m.Name);
            }
            names.Sort(StringComparer.Ordinal);
            return Json.Ok(new { type = typeName, found = t != null, count = names.Count, methods = names });
        }

        private static string ChestCmd(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            Chest c = NearChest(pl, out int ci);
            if (c == null) return Json.Error("附近 6 格内没有箱子");
            string action = Json.Str(p, "action", "list");

            if (action == "list")
            {
                var items = new List<object>();
                for (int i = 0; i < c.item.Length; i++)
                {
                    Item it = c.item[i];
                    if (it == null || it.type <= 0) continue;
                    items.Add(new { slot = i, id = it.type, name = it.Name, stack = it.stack });
                }
                return Json.Ok(new { chest = new { x = c.x, y = c.y }, index = ci, items });
            }

            if (action == "take")
            {
                int slot = Json.Int(p, "slot", -1);
                int count = Json.Int(p, "count", -1);
                if (slot < 0 || slot >= c.item.Length) return Json.Error("slot 越界");
                Item src = c.item[slot];
                if (src == null || src.type <= 0) return Json.Error("这个箱子格子是空的");
                int n = count > 0 ? Math.Min(count, src.stack) : src.stack;
                Item taken = src.Clone();
                taken.stack = n;
                // 手工放进背包：同类型可叠加的优先，其次空格
                int target = -1;
                for (int i = 0; i < MainBagSlots; i++)
                {
                    Item inv = pl.inventory[i];
                    if (inv == null) continue;
                    if (inv.type == taken.type && inv.stack + n <= inv.maxStack) { target = i; break; }
                }
                if (target < 0)
                    for (int i = 0; i < MainBagSlots; i++)
                        if (pl.inventory[i] != null && pl.inventory[i].type == 0) { target = i; break; }
                if (target < 0) return Json.Error("背包放不下");
                if (pl.inventory[target].type == taken.type) pl.inventory[target].stack += n;
                else { pl.inventory[target] = taken; }
                src.stack -= n;
                if (src.stack <= 0) { c.item[slot] = new Item(); c.item[slot].SetDefaults(0); }
                return Json.Ok(new { took = taken.Name, count = n, slot });
            }

            if (action == "put")
            {
                int inv = Json.Int(p, "invSlot", -1);
                if (inv < 0 || inv >= pl.inventory.Length) return Json.Error("invSlot 越界");
                Item src = pl.inventory[inv];
                if (src == null || src.type <= 0) return Json.Error("背包这个格子是空的");
                for (int i = 0; i < c.item.Length; i++)
                {
                    if (c.item[i] != null && c.item[i].type > 0) continue;
                    c.item[i] = src.Clone();
                    pl.inventory[inv] = new Item();
                    pl.inventory[inv].SetDefaults(0);
                    return Json.Ok(new { put = src.Name, chestSlot = i });
                }
                return Json.Error("箱子满了");
            }
            return Json.Error("action 只能是 list / take / put");
        }

        /// <summary>
        /// 城镇 NPC 的任务/目标。游戏里唯一有"每日任务"的是渔夫：
        /// Main.anglerQuest 是当前任务索引，Main.anglerQuestItemNetIDs[索引] 是这一条要的鱼。
        /// 引擎里没有通用的"任务列表"字段，所以这里只报真实存在的那些，不编。
        /// </summary>
        private static string Quest(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;

            int idx = Main.anglerQuest;
            int itemId = 0;
            if (Main.anglerQuestItemNetIDs != null && idx >= 0 && idx < Main.anglerQuestItemNetIDs.Length)
                itemId = Main.anglerQuestItemNetIDs[idx];
            string fishName = null;
            if (itemId > 0 && ContentSamples.ItemsByType.TryGetValue(itemId, out Item sample) && sample != null)
                fishName = sample.Name;

            bool anglerHere = false;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.type == NPCID.Angler) { anglerHere = true; break; }
            }

            var quests = new List<object>();
            quests.Add(new
            {
                npc = "Angler", kind = "daily_fish",
                index = idx, itemId, itemName = fishName,
                finishedToday = Main.anglerQuestFinished,
                questsFinished = pl.anglerQuestsFinished,
            });

            return Json.Ok(new
            {
                quests,
                anglerPresent = anglerHere,
                // 对话正文：只有正在对话时才有内容（talk 之后读）
                chatText = Main.npcChatText,
                note = "游戏只暴露渔夫每日任务；其他 NPC 没有可读的任务字段",
            });
        }

        /* ── 丢东西 / 喝药 / Boss 与事件状态 / 穿护甲 ────────────────── */

        /// <summary>
        /// 把背包里的东西送进"垃圾桶"栏位——就是背包界面里那个专用销毁格。
        /// 为什么不能用 drop 来腾格子：丢在地上之后空位立刻出现，自动拾取又会把它吸回来，
        /// 于是"腾空了又满了"，看着像 bug 其实是自己丢的东西被自己捡回。销毁才是真的消失。
        /// </summary>
        private static string TrashItem(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int slot = Json.Int(p, "slot", -1);
            if (slot < 0 || slot >= pl.inventory.Length) return Json.Error("slot 越界");
            Item it = pl.inventory[slot];
            if (it == null || it.type <= 0) return Json.Error("这个格子是空的");
            // 默认只销毁 1 个：实测不带 count 调一次就把 2081 个火把整栈吃掉了。
            // 垃圾桶语义不变，但"整栈销毁"必须显式决定（count:该槽总数）。
            int n = Math.Clamp(Json.Int(p, "count", 1), 1, it.stack);
            string oldTrash = (pl.trashItem != null && pl.trashItem.type > 0) ? pl.trashItem.Name : null;

            // 与 UI 拖入垃圾桶等价：垃圾桶里原有物品被销毁，被拖的物品离开背包。
            Item moved = it.Clone();
            moved.stack = n;
            pl.trashItem = moved;
            if (n >= it.stack) pl.inventory[slot] = new Item();
            else it.stack -= n;

            bool gone = pl.inventory[slot] == null || pl.inventory[slot].type != moved.type
                        || pl.inventory[slot].stack < n + (it.stack);
            return Json.Ok(new
            {
                trashed = moved.Name,
                type = moved.type,
                count = n,
                slot,
                replaced = oldTrash,
                left = pl.inventory[slot] == null ? 0 : pl.inventory[slot].stack,
                gone,
                hint = "真销毁：不会掉在地上被吸回来。",
            });
        }

        private static string DropItem(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int slot = Json.Int(p, "slot", -1);
            if (slot < 0 || slot >= pl.inventory.Length) return Json.Error("slot 越界");
            Item it = pl.inventory[slot];
            if (it == null || it.type <= 0) return Json.Error("这个格子是空的");
            int n = Math.Clamp(Json.Int(p, "count", it.stack), 1, it.stack);
            string attemptsErr = null;
            Item dropped = it.Clone();
            int orig = it.stack;
            // 原版"丢东西"= 选中该格再 DropSelectedItem()。它会整堆丢出去，所以先把 stack
            // 临时改成 n，丢完再把剩下的放回去；成功与否用背包账目核对，不看返回值。
            if (orig > n) it.stack = n;
            int savedSel = pl.selectedItem;
            int idx = -1;
            try
            {
                pl.selectedItem = slot;
                pl.DropSelectedItem();
            }
            catch (Exception e) { attemptsErr = e.Message; }
            pl.selectedItem = savedSel;
            int left = pl.inventory[slot] != null && pl.inventory[slot].type == dropped.type ? pl.inventory[slot].stack : 0;
            bool took = left <= 0;
            for (int i = 0; i < Main.maxItems; i++)
            {
                Item mi = Main.item[i];
                if (mi != null && mi.active && mi.type == dropped.type && Vector2.Distance(mi.Center, pl.Center) < 6f * 16f)
                { idx = i; break; }
            }
            if (!took && attemptsErr == null)
            {
                // 没丢出去：把临时改小的 stack 还原
                if (orig > n) { it.stack = orig; pl.inventory[slot] = it; }
                return Json.Ok(new { dropped = 0, failed = true, item = dropped.Name, asked = n, err = attemptsErr,
                    note = "DropSelectedItem 没有把物品丢出去，库存未变" });
            }
            if (orig > n)
            {
                Item rest = dropped.Clone();
                rest.stack = orig - n;
                pl.inventory[slot] = rest;
            }
            return Json.Ok(new { dropped = dropped.Name, count = n, idx, onGround = idx >= 0,
                left = pl.inventory[slot].stack,
                note = idx < 0
                    ? "背包账目已经减了（left 变小），但本地还没看到掉落物：Host&Play 客户端丢东西是先发请求、"
                      + "由服务端创建物品再广播回来，本帧读 Main.item 必然还是空的。1 秒后用 items 或 grab 复查。"
                    : null,
                verify = "grab 能捡回来才算真的落地" });
        }

        private static string HealSelf(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int before = pl.statLife;
            // 先自己看一遍背包：QuickHeal 什么都不做时也要能说出"为什么"，
            // 别让 hasPotion:false 变成"背包里明明有药"的假象（实测 46 瓶弱效治疗药水时踩过）。
            string candidate = null;
            int candidateSlot = -1;
            for (int i = 0; i < pl.inventory.Length; i++)
            {
                Item it = pl.inventory[i];
                if (it != null && it.type > 0 && it.healLife > 0 && it.stack > 0)
                { candidate = it.Name; candidateSlot = i; break; }
            }
            pl.QuickHeal();
            int after = pl.statLife;
            int healed = after - before;
            string hint = null;
            if (healed <= 0)
            {
                if (candidate == null) hint = "背包里没有任何能回血的药水";
                else if (pl.potionDelay > 0) hint = $"背包里有 {candidate}（slot {candidateSlot}），但还在药水冷却里（{pl.potionDelay} 帧）";
                else if (before >= pl.statLifeMax2) hint = $"背包里有 {candidate}，但已经满血（{before}/{pl.statLifeMax2}），不浪费";
                else hint = $"背包里有 {candidate}，QuickHeal 却没有喝 —— 值得查";
            }
            return Json.Ok(new
            {
                before, after, healed,
                hasPotion = healed > 0,
                potionDelay = pl.potionDelay,
                candidate, candidateSlot, hint,
            });
        }

        private static string BossStatus(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            var list = new List<object>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.boss) continue;
                list.Add(new { name = n.FullName, life = n.life, lifeMax = n.lifeMax, head = n.boss });
            }
            return Json.Ok(new { active = list.Count, bosses = list });
        }

        private static string EventStatus(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            return Json.Ok(new
            {
                dayTime = Main.dayTime, time = Main.time, day = Main.dayRate,
                bloodMoon = Main.bloodMoon, eclipse = Main.eclipse,
                invasionType = Main.invasionType, invasionSize = Main.invasionSize,
                pumpkinMoon = Main.pumpkinMoon, snowMoon = Main.snowMoon,
                raining = Main.raining,
                hardMode = Main.hardMode,
                downedEye = NPC.downedBoss1, downedEater = NPC.downedBoss2,
                downedSkeletron = NPC.downedBoss3, downedWall = Main.hardMode,
                downedPlantera = NPC.downedPlantBoss, downedGolem = NPC.downedGolemBoss,
            });
        }

        private static string EquipArmor(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int invSlot = Json.Int(p, "slot", -1);
            if (invSlot < 0 || invSlot >= pl.inventory.Length) return Json.Error("slot 越界");
            Item it = pl.inventory[invSlot];
            if (it == null || it.type <= 0) return Json.Error("背包这个格子是空的");
            int armorSlot = -1;
            if (it.headSlot >= 0) armorSlot = 0;
            else if (it.bodySlot >= 0) armorSlot = 1;
            else if (it.legSlot >= 0) armorSlot = 2;
            else return Json.Error($"{it.Name} 不是护甲");
            Item old = pl.armor[armorSlot];
            pl.armor[armorSlot] = it.Clone();
            pl.inventory[invSlot] = old;
            return Json.Ok(new { equipped = it.Name, armorSlot, replaced = old != null && old.type > 0 ? old.Name : null });
        }

        /* ── 商店 ────────────────────────────────────────────────────────
         * 真实签名（用 api 命令查出来的）：Chest.SetupShop(int) 是实例方法，
         * 商店容器是 Main.shop[]。买用 Player.BuyItem 扣真钱，卖按原版 value/5 折价。
         */
        private static NPC NearbyTownNPC(Player pl, int wantType, out float dist)
        {
            NPC best = null; dist = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.townNPC) continue;
                if (wantType >= 0 && n.type != wantType) continue;
                float d = Vector2.Distance(pl.Center, n.Center);
                if (d < dist) { dist = d; best = n; }
            }
            return best;
        }

        /// <summary>
        /// 两个真实重载：SetupShop(int) 收的是商店 id（17 会开出海盗商店），SetupShop(string, NPC)
        /// 收名字。所以这里允许调用方给 name，默认用 int 版本。
        /// </summary>
        private static void SetupShopFor(Chest shop, NPC npc, int shopId, string name)
        {
            if (shopId >= 0) { shop.SetupShop(shopId); return; }
            if (!string.IsNullOrEmpty(name)) shop.SetupShop(name, npc);
            else if (npc != null) shop.SetupShop(npc.type);
        }

        /// <summary>
        /// Arm a real right-click at the NPC.  The vanilla HoverOverNPCs path performs
        /// SetTalkNPC, shop selection, and GetChat; this command must not write those result
        /// fields itself.  The caller confirms success later through state.chatText/talkNPC.
        /// </summary>
        private static string TalkTo(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            // who 优先：同类型可能有多个（两个向导 approve 不一样的名字），按类型找只会拿到最近的，
            // 想跟"另一个"说话就只能靠 who。
            int wantWho = Json.Int(p, "who", -1);
            NPC npc;
            float d;
            if (wantWho >= 0 && wantWho < Main.maxNPCs)
            {
                npc = Main.npc[wantWho];
                if (npc == null || !npc.active || !npc.townNPC)
                {
                    return Json.Error($"who={wantWho} 不是活着的城市 NPC（type={(npc == null ? -1 : npc.type)}，"
                        + $"active={(npc != null && npc.active)}，townNPC={(npc != null && npc.townNPC)}）；用 npc 命令先查 who");
                }
                d = Vector2.Distance(pl.Center, npc.Center);
            }
            else
            {
                npc = NearbyTownNPC(pl, Json.Int(p, "npc", -1), out d);
            }
            if (npc == null) return Json.Error("附近没有可以对话的城市 NPC");
            if (d > 12 * 16f) return Json.Error($"离 {npc.FullName} 太远（{d / 16f:F0} 格），先走过去");
            int idx = npc.whoAmI;
            // 实测：12 格处 armed:true、鼠标定位完全正确，但引擎不开对话（talkNPC 仍 -1）。
            // 原版自己的 NPC 交互距离比 12 格严得多，约 5 格，而且要求中间没有实心方块。
            // 门槛跟着引擎走，否则 talk 会在"根本谈不起来"的距离上返回 armed 让人误判。
            if (d > 5 * 16f) return Json.Error($"离 {npc.FullName} 太远（{d / 16f:F1} 格），原版对话判定约 5 格内，先走过去");
            if (!HasLineOfSightTo(pl, npc))
            {
                return Json.Error($"看不到 {npc.FullName}：中间隔着方块（你在 y={(int)(pl.Center.Y / 16f)}，他在 y={(int)(npc.Center.Y / 16f)}），"
                    + "得走到同一层再谈");
            }
            if (pl.talkNPC == idx && !string.IsNullOrWhiteSpace(Main.npcChatText))
            {
                return Json.Ok(new
                {
                    npc = npc.FullName, type = npc.type, distance = (int)(d / 16f),
                    armed = false, alreadyTalking = true,
                    talkNPC = pl.talkNPC, npcShop = Main.npcShop,
                    hasShop = Main.npcShop > 0,
                    chatText = Main.npcChatText,
                    cornerItem = Main.npcChatCornerItem,
                    isAngler = npc.type == NPCID.Angler,
                });
            }

            int ms = Math.Clamp(Json.Int(p, "ms", 500), 200, 2000);
            BridgeSystem.TalkTargetNpc = idx;
            BridgeSystem.TalkAimWorld = npc.Center;
            BridgeSystem.TalkUntil = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            BridgeSystem.TalkPhase = 1;
            return Json.Ok(new
            {
                npc = npc.FullName, type = npc.type, distance = (int)(d / 16f),
                armed = true,
                untilTick = BridgeSystem.TalkUntil,
                target = new { x = MathF.Round(npc.Center.X, 1), y = MathF.Round(npc.Center.Y, 1) },
                input = "mouseRight",
                note = "已发送真实右键输入；等待 state.talkNPC 与 state.chatText 回读确认",
                isAngler = npc.type == NPCID.Angler,
            });
        }

        /// <summary>玩家中心到 NPC 中心连线上有没有实心方块（原版对话要求有视线）。</summary>
        private static bool HasLineOfSightTo(Player pl, NPC npc)
        {
            Vector2 a = pl.Center;
            Vector2 b = npc.Center;
            float dist = Vector2.Distance(a, b);
            int steps = Math.Max(2, (int)(dist / 8f));
            for (int i = 1; i < steps; i++)
            {
                Vector2 pt = Vector2.Lerp(a, b, i / (float)steps);
                int tx = (int)(pt.X / 16f);
                int ty = (int)(pt.Y / 16f);
                if (tx < 0 || ty < 0 || tx >= Main.maxTilesX || ty >= Main.maxTilesY) continue;
                Tile tile = Main.tile[tx, ty];
                // solidTop（平台）不挡视线：人站在平台上照样能跟下一层说话。
                if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]) return false;
            }
            return true;
        }

        private static int ShopPrice(Item it) => it.shopCustomPrice.HasValue ? it.shopCustomPrice.Value : it.value;

        /// <summary>
        /// 关掉当前 NPC 对话/告示牌。原版就有 Main.CloseNPCChatOrSign()（public static，
        /// 用 api 命令查 Terraria.Main 时看到的），这里只是把它包出来——之前只能用 interact
        /// 点空地，实测点两次都关不掉。
        /// </summary>
        private static string TalkClose(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int before = pl.talkNPC;
            bool hadChat = !string.IsNullOrWhiteSpace(Main.npcChatText);
            bool hadShop = Main.npcShop > 0;
            try { Main.CloseNPCChatOrSign(); }
            catch (Exception ex) { return Json.Error($"CloseNPCChatOrSign 抛异常：{ex.GetType().Name}: {ex.Message}"); }
            return Json.Ok(new
            {
                beforeTalkNPC = before,
                talkNPC = pl.talkNPC,
                hadChat,
                hadShop,
                chatText = Main.npcChatText,
                npcShop = Main.npcShop,
                closed = pl.talkNPC < 0,
                note = pl.talkNPC < 0 ? null : "talkNPC 还没清掉：再调一次；多人下可能由服务端下一 tick 结算",
            });
        }

        /// <summary>
        /// 点 NPC 对话框里的按钮（护士治疗、渔夫交任务那一类）。
        ///
        /// 原版把按钮画在聊天框下方，悬停判定靠 GUIChatDrawInner 内部的鼠标位置写进
        /// Main.npcChatFocus1..4。坐标必须在 GUIChatDrawInner 调用前写入；跨层写入会被
        /// Vanilla: Player Chat 的 SetZoom_UI 覆盖。所以这里启动一个由原版聊天绘制钩子驱动的
        /// 跨帧状态机：每帧只探一个点，命中后再发一次真实左键。
        ///
        /// 命令本身是**异步**的：调用后立刻返回扫描状态，用 `talkoption`（不传 index）或
        /// `talkoption {stop:true}` 轮询/中止。
        /// </summary>
        private static string TalkOption(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;

            if (Json.Bool(p, "stop", false))
            {
                BridgeSystem.TalkOptPhase = 0;
                BridgeSystem.TalkOptIndex = 0;
                BridgeSystem.TalkOptHitX = -1;
                BridgeSystem.TalkOptHitY = -1;
                BridgeSystem.TalkOptMsg = "已停止";
                return Json.Ok(TalkOptionStatus());
            }

            int index = Json.Int(p, "index", 0);
            if (index <= 0) return Json.Ok(TalkOptionStatus());   // 只读状态

            if (pl.talkNPC < 0) return Json.Error("还没有和 NPC 对话：先用 talk 建立对话，再 talkoption");
            if (index > 4) return Json.Error("index 只能是 1..4：1=关闭，2=第一个业务按钮，3=第二个业务按钮，4=幸福度");
            if (!BridgeSystem.TalkOptionHookInstalled)
            {
                string hookError = BridgeSystem.TalkOptionHookError;
                return Json.Error(hookError.Length > 0
                    ? "talkoption hook 未安装：" + hookError
                    : "talkoption hook 尚未安装；请稍后重试");
            }

            BridgeSystem.TalkOptIndex = index;
            BridgeSystem.TalkOptPhase = 1;
            BridgeSystem.TalkOptScanned = 0;
            BridgeSystem.TalkOptBlocked = 0;
            BridgeSystem.TalkOptLastIgnoreMouseInterface = false;
            BridgeSystem.TalkOptLastUsingGamepad = false;
            BridgeSystem.TalkOptLastItemAnimation = -1;
            BridgeSystem.TalkOptLastMouseX = 0;
            BridgeSystem.TalkOptLastMouseY = 0;
            BridgeSystem.TalkOptHitX = -1;
            BridgeSystem.TalkOptHitY = -1;
            // 与 BridgeSystem 的 GUIChatDrawInner 扫描带保持一致：按钮在台词下方（上半屏），
            // 不在屏幕底部。
            int yStart = 80;
            BridgeSystem.TalkOptX = 100;
            BridgeSystem.TalkOptY = yStart;
            BridgeSystem.TalkOptStart = Main.GameUpdateCount;
            BridgeSystem.TalkOptMsg = $"开始扫描按钮 {index}：在 GUIChatDrawInner 前逐点扫描（每帧一个 8px 网格点）";
            return Json.Ok(TalkOptionStatus());
        }

        /// <summary>
        /// 诊断用：读/探 NPC 对话按钮的四个焦点位。
        ///
        /// 原版每帧绘制按钮时用 Main.mouseX/Y 判悬停，把结果写进 Main.npcChatFocus1..4。
        /// talkoption 的扫描靠这一点反推按钮矩形，但扫完 342 点一个都没命中 —— 到底是
        /// 坐标算错、还是这个 NPC 的对话压根没有按钮，得能直接问。
        ///
        /// 用法：
        ///   talkfocus                 → 只读：屏幕、鼠标、四个焦点位、对话状态
        ///   talkfocus {x,y}           → 把鼠标写到位一帧，返回"下一帧再读"
        /// 写鼠标由 GUIChatDrawInner detour 在原版读取前完成，只写输入，不碰游戏状态。
        /// </summary>
        private static string TalkFocus(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;

            int px = Json.Int(p, "x", -1);
            int py = Json.Int(p, "y", -1);
            bool wrote = false;
            if (px >= 0 && py >= 0)
            {
                BridgeSystem.ProbeMouseX = px;
                BridgeSystem.ProbeMouseY = py;
                wrote = true;
            }

            return Json.Ok(new
            {
                screen = new { w = Main.screenWidth, h = Main.screenHeight },
                mouse = new { x = Main.mouseX, y = Main.mouseY },
                focus = new
                {
                    f1 = Main.npcChatFocus1,
                    f2 = Main.npcChatFocus2,
                    f3 = Main.npcChatFocus3,
                    f4 = Main.npcChatFocus4,
                },
                talk = new
                {
                    npc = pl.talkNPC,
                    chatLen = Main.npcChatText == null ? 0 : Main.npcChatText.Length,
                    cornerItem = Main.npcChatCornerItem,
                    shop = Main.npcShop,
                    inventoryOpen = Main.playerInventory,
                    mouseText = Main.mouseText,
                },
                wrote,
                note = wrote
                    ? $"已把鼠标写到 ({px},{py})，下一帧原版会重算四个焦点位：再调一次 talkfocus 读 focus"
                    : "只读。想探某一点就先 talkfocus {x,y}，等一帧再读。",
            });
        }

        private static object TalkOptionStatus()
        {
            int phase = BridgeSystem.TalkOptPhase;
            string phaseName = phase == 0 ? "idle"
                : phase == 1 ? "scanning"
                : phase == 2 ? "releasing"
                : phase == 3 ? "pressing"
                : phase == 4 ? "releasingAfterClick"
                : phase == 5 ? "failed"
                : phase == 6 ? "inputSent" : "unknown";
            Player pl = P;
            return new
            {
                active = phase >= 1 && phase <= 4,
                phase,
                phaseName,
                index = BridgeSystem.TalkOptIndex,
                scanned = BridgeSystem.TalkOptScanned,
                cursorX = BridgeSystem.TalkOptX,
                cursorY = BridgeSystem.TalkOptY,
                hitX = BridgeSystem.TalkOptHitX,
                hitY = BridgeSystem.TalkOptHitY,
                blocked = BridgeSystem.TalkOptBlocked,
                lastIgnoreMouseInterface = BridgeSystem.TalkOptLastIgnoreMouseInterface,
                lastUsingGamepad = BridgeSystem.TalkOptLastUsingGamepad,
                lastItemAnimation = BridgeSystem.TalkOptLastItemAnimation,
                lastMouse = new { x = BridgeSystem.TalkOptLastMouseX, y = BridgeSystem.TalkOptLastMouseY },
                talkNPC = pl != null ? pl.talkNPC : -1,
                chatText = Main.npcChatText ?? "",
                npcShop = Main.npcShop,
                hookInstalled = BridgeSystem.TalkOptionHookInstalled,
                hookError = BridgeSystem.TalkOptionHookError.Length > 0 ? BridgeSystem.TalkOptionHookError : null,
                hookCalls = BridgeSystem.TalkOptionHookCalls,
                msg = BridgeSystem.TalkOptMsg,
            };
        }

        /// <summary>
        /// 遍历所有商店 id，找出包含指定物品名字的商店（例如"铁砧"），把 id、价格、库存一起报出来。
        /// 商店 id 与 NPC 类型的对应关系不直观（17 开出来是海盗商店），所以直接问游戏。
        /// </summary>
        private static string ShopFind(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            string want = Json.Str(p, "want", "");
            if (want.Length == 0) return Json.Error("shops 需要 want（物品名片段）");
            var hits = new List<object>();
            Chest shop = Main.instance.shop[0];
            int max = Math.Max(Main.MaxShopIDs, 300);
            for (int id = 0; id < max; id++)
            {
                try { shop.SetupShop(id); } catch { continue; }
                for (int i = 0; i < shop.item.Length; i++)
                {
                    Item it = shop.item[i];
                    if (it == null || it.type <= 0) continue;
                    if (it.Name.IndexOf(want, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    hits.Add(new { shopId = id, slot = i, name = it.Name, priceCopper = ShopPrice(it) });
                }
            }
            return Json.Ok(new { want, scanned = max, hits });
        }

        /// <summary>
        /// 诊断：把几种"打开商店"的方式都试一次，报告每种方式下每个商店容器里有几件商品。
        /// 目标是找出与 UI 等价的那条路径，而不是继续猜。
        /// </summary>
        private static string ShopDebug(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            NPC npc = NearbyTownNPC(pl, Json.Int(p, "npc", -1), out float d);
            if (npc == null) return Json.Error("附近没有城市 NPC");
            // SetNPCShopIndex 收的是「商店 id」，不是 NPC 类型。以前直接传 npc.type（38/369 这种），
            // 一调就越界抛 IndexOutOfRange，整条命令以未捕获异常收场。先解析出真正的 shopId。
            int shopId = ResolveVanillaShopIndex(npc, out string shopName);
            if (shopId < 0)
                return Json.Error($"找不到 {npc.FullName} 对应的商店 id（name={shopName}）；先用 shops 查合法 id");
            int hands = Main.instance.shop?.Length ?? 0;
            if (hands == 0) return Json.Error("商店表还没初始化：先进游戏和任意 NPC 对话一次再调 shopdebug");
            var report = new List<object>();
            int Count(Chest c)
            {
                int n = 0;
                if (c?.item == null) return 0;
                foreach (Item it in c.item) if (it != null && it.type > 0) n++;
                return n;
            }
            void Snap(string how)
            {
                var counts = new List<int>();
                for (int i = 0; i < hands; i++) counts.Add(Count(Main.instance.shop[i]));
                report.Add(new { how, npcShop = Main.npcShop, counts });
            }

            Snap("初始");
            Main.SetNPCShopIndex(shopId); Snap("SetNPCShopIndex(shopId=" + shopId + ")");
            Main.SetNPCShopIndex(1); Snap("SetNPCShopIndex(1)");
            for (int i = 0; i < hands; i++)
            {
                Main.instance.shop[i]?.SetupShop(1);
                Snap("shop[" + i + "].SetupShop(1)");
            }
            Main.instance.shop[0]?.SetupShop(shopId); Snap("shop[0].SetupShop(shopId=" + shopId + ")");
            return Json.Ok(new { npc = npc.FullName, npcType = npc.type, shopId, shopName, hands, report });
        }

        /// <summary>商店名在不同版本里可能是 "Terraria/Merchant" 或带 NPC 名的长键，逐个候选试。</summary>
        private static Terraria.ModLoader.AbstractNPCShop ResolveShop(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            var cands = new List<string> { fullName };
            string[] parts = fullName.Split('/');
            if (parts.Length > 2) cands.Add(string.Join("/", parts, 0, 2));
            if (parts.Length > 1) cands.Add(parts[parts.Length - 1]);
            foreach (string cand in cands)
            {
                try
                {
                    if (NPCShopDatabase.TryGetNPCShop(cand, out Terraria.ModLoader.AbstractNPCShop shop) && shop != null)
                        return shop;
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// 找出某个 NPC 对应的「vanilla 商店 id」：用 NPCShopDatabase.GetShopNameFromVanillaIndex(id)
        /// 反查名字，和 GetShopName(npc) 前缀匹配。拿到 id 后用 Chest.SetupShop(id) 填容器
        /// （实测商人=1，能填出 13 件商品）。
        /// </summary>
        /// <summary>把 "Terraria/Demolitionist/爆破专家Oten" 截成 "Terraria/Demolitionist"。</summary>
        private static string ShopKeyOf(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            int a = s.IndexOf('/');
            if (a < 0) return s;
            int b = s.IndexOf('/', a + 1);
            return b < 0 ? s : s.Substring(0, b);
        }

        private static int ResolveVanillaShopIndex(NPC npc, out string npcShopName)
        {
            npcShopName = null;
            try { npcShopName = NPCShopDatabase.GetShopName(npc.type, npc.FullName); } catch { }
            if (string.IsNullOrEmpty(npcShopName)) return -1;
            // GetShopName 返回的第三段是本地化 NPC 名（"爆破专家Oten"），而 GetShopNameFromVanillaIndex
            // 返回的第三段是英文键，整串前缀匹配永远对不上。只比前两段（命名空间 + 商店名）才稳。
            string want = ShopKeyOf(npcShopName);
            for (int id = 0; id < 400; id++)
            {
                string name = null;
                try { name = NPCShopDatabase.GetShopNameFromVanillaIndex(id); } catch { }
                if (string.IsNullOrEmpty(name)) continue;
                if (string.Equals(want, ShopKeyOf(name), StringComparison.OrdinalIgnoreCase)) return id;
            }
            for (int id = 0; id < 400; id++)
            {
                string name = null;
                try { name = NPCShopDatabase.GetShopNameFromVanillaIndex(id); } catch { }
                if (string.IsNullOrEmpty(name)) continue;
                if (name.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0) return id;
            }
            return -1;
        }

        private static string ShopList(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int forcedId = Json.Int(p, "shopId", -1);
            NPC npc = NearbyTownNPC(pl, Json.Int(p, "npc", -1), out float d);
            if (npc == null && forcedId < 0) return Json.Error("附近没有开店的城市 NPC，也没给 shopId");
            if (pl.talkNPC < 0) return Json.Error("还没有和 NPC 对话：先用 talk");

            int shopId = forcedId;
            string shopName = null;
            if (shopId < 0) shopId = ResolveVanillaShopIndex(npc, out shopName);
            if (shopId < 0) return Json.Error($"找不到 {npc?.FullName} 对应的商店 id（name={shopName}）");

            // shopId ≠ NPC type：Main.instance.shop 的尺寸远小于 40，直接 SetupShop(39) 会越界。
            try { Main.instance.shop[0].SetupShop(shopId); }
            catch (Exception ex) { return Json.Error($"shopId={shopId} 超出本版本的商店表（{ex.GetType().Name}），先用 shops 命令查合法 id"); }
            Chest shop = Main.instance.shop[0];
            var items = new List<object>();
            for (int i = 0; i < shop.item.Length; i++)
            {
                Item it = shop.item[i];
                if (it == null || it.type <= 0) continue;
                items.Add(new { slot = i, id = it.type, name = it.Name, priceCopper = ShopPrice(it), stack = it.stack });
            }
            return Json.Ok(new
            {
                npc = npc?.FullName ?? "(shopId)",
                shop = shopName,
                shopId,
                npcType = npc?.type ?? -1,
                distance = (int)(d / 16f),
                items,
            });
        }

        private static string ShopBuy(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int slot = Json.Int(p, "slot", -1);
            if (pl.talkNPC < 0) return Json.Error("还没有和 NPC 对话：先走到 NPC 旁边用 talk");

            NPC npc = Main.npc[pl.talkNPC];
            int shopId = Json.Int(p, "shopId", -1);
            string shopName = null;
            if (shopId < 0) shopId = ResolveVanillaShopIndex(npc, out shopName);
            if (shopId < 0) return Json.Error($"找不到 {npc.FullName} 对应的商店 id");

            try { Main.instance.shop[0].SetupShop(shopId); }
            catch (Exception ex) { return Json.Error($"shopId={shopId} 超出本版本的商店表（{ex.GetType().Name}），先用 shops 命令查合法 id"); }
            Chest shop = Main.instance.shop[0];
            if (slot < 0 || slot >= shop.item.Length) return Json.Error("slot 越界");
            Item stock = shop.item[slot];
            if (stock == null || stock.type <= 0) return Json.Error("商店这个格子是空的");

            long price = ShopPrice(stock);
            // 先找同类可叠加的堆（否则每次买都塞新槽，容易互相覆盖/丢东西）
            int slotInBag = -1;
            for (int i = 0; i < MainBagSlots; i++)
            {
                Item it = pl.inventory[i];
                if (it != null && it.type == stock.type && it.stack < it.maxStack) { slotInBag = i; break; }
            }
            if (slotInBag < 0)
                for (int i = 0; i < MainBagSlots; i++)
                    if (pl.inventory[i] != null && pl.inventory[i].type == 0) { slotInBag = i; break; }
            if (slotInBag < 0) return Json.Error("背包没有空位");
            if (!pl.BuyItem(price))
                return Json.Error($"钱不够：需要 {price} 铜币（{price / 100} 银币）");
            if (pl.inventory[slotInBag].type == stock.type) pl.inventory[slotInBag].stack += 1;
            else { Item got = stock.Clone(); got.stack = 1; pl.inventory[slotInBag] = got; }
            return Json.Ok(new { bought = stock.Name, priceCopper = price, slot = slotInBag, stack = pl.inventory[slotInBag].stack, shopId });
        }

        private static string ShopSell(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            if (pl.talkNPC < 0) return Json.Error("还没有和 NPC 对话：先走到 NPC 旁边用 talk");
            int slot = Json.Int(p, "slot", -1);
            if (slot < 0 || slot >= pl.inventory.Length) return Json.Error("slot 越界");
            Item it = pl.inventory[slot];
            if (it == null || it.type <= 0) return Json.Error("这个格子是空的");
            if (it.type == ItemID.CopperCoin || it.type == ItemID.SilverCoin
                || it.type == ItemID.GoldCoin || it.type == ItemID.PlatinumCoin)
                return Json.Error("不卖钱币本身");
            long price = it.value / 5 * it.stack;
            if (price <= 0) return Json.Error($"{it.Name} 卖不出价钱");
            // 钱直接进背包（以前是掉在地上，卖完还得去捡，等于白卖）
            long left = price;
            void Give(int coinType, long unit)
            {
                long n = left / unit;
                if (n <= 0) return;
                left -= n * unit;
                while (n > 0)
                {
                    int chunk = (int)Math.Min(n, 999);
                    int slot2 = -1;
                    for (int i = 0; i < MainBagSlots; i++)
                    {
                        Item inv = pl.inventory[i];
                        if (inv != null && inv.type == coinType && inv.stack + chunk <= inv.maxStack) { slot2 = i; break; }
                    }
                    if (slot2 < 0)
                        for (int i = 0; i < MainBagSlots; i++)
                            if (pl.inventory[i] != null && pl.inventory[i].type == 0) { slot2 = i; break; }
                    if (slot2 < 0) return;   // 背包满就少给，不凭空生成
                    if (pl.inventory[slot2].type == coinType) pl.inventory[slot2].stack += chunk;
                    else { pl.inventory[slot2] = new Item(); pl.inventory[slot2].SetDefaults(coinType); pl.inventory[slot2].stack = chunk; }
                    n -= chunk;
                }
            }
            Give(ItemID.PlatinumCoin, 1000000);
            Give(ItemID.GoldCoin, 10000);
            Give(ItemID.SilverCoin, 100);
            Give(ItemID.CopperCoin, 1);
            string name = it.Name;
            pl.inventory[slot] = new Item();
            pl.inventory[slot].SetDefaults(0);
            return Json.Ok(new { sold = name, priceCopper = price });
        }

        /// <summary>
        /// Read-only API lookup: find a type by name and list its members whose name contains a
        /// filter, with their real signatures. Exists because guessing a signature wastes a build
        /// cycle ("Chest.SetupShop takes a string" was only discoverable from the compiler).
        /// </summary>
        private static string ApiLookup(JsonElement p)
        {
            string typeName = Json.Str(p, "type", "");
            // 参数名曾经写成 member，而 KB 与实测都按 filter 传 —— 两个都认，别再让调用方踩空。
            string filter = Json.Str(p, "filter", "");
            if (filter.Length == 0) filter = Json.Str(p, "member", "");
            int limit = Json.Int(p, "limit", 200);
            if (typeName.Length == 0) return Json.Error("api 需要 type（例如 Terraria.Chest）");
            Type t = typeof(Main).Assembly.GetType(typeName);
            if (t == null)
            {
                // 允许只给短名
                foreach (Type cand in typeof(Main).Assembly.GetTypes())
                    if (cand.Name == typeName) { t = cand; break; }
            }
            if (t == null) return Json.Error($"找不到类型 {typeName}");

            var list = new List<string>();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
            foreach (var m in t.GetMethods(flags))
                if (filter.Length == 0 || m.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    list.Add(m.IsStatic + " | " + m.ToString());
            foreach (var f in t.GetFields(flags))
                if (filter.Length > 0 && f.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    list.Add("field | " + f.FieldType.Name + " " + f.Name);
            // Terraria.Main 这类大类型有 700+ 成员，一次吐出去会淹掉调用方 —— 默认只回前 200 条并标记 truncated。
            int shown = Math.Min(Math.Max(limit, 1), list.Count);
            var page = list.GetRange(0, shown);
            return Json.Ok(new
            {
                type = t.FullName,
                count = list.Count,
                shown,
                truncated = shown < list.Count,
                filter,
                members = page,
            });
        }

        private static readonly BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        private static bool StaticBoolArrayAt(Type owner, string fieldName, int index)
        {
            try
            {
                var f = owner?.GetField(fieldName, PublicStatic);
                if (f?.GetValue(null) is Array a && index >= 0 && index < a.Length)
                    return a.GetValue(index) is bool b && b;
            }
            catch { }
            return false;
        }

        private static bool TileSetBool(string fieldName, int type)
        {
            try
            {
                Type sets = typeof(TileID).GetNestedType("Sets", BindingFlags.Public | BindingFlags.NonPublic);
                return StaticBoolArrayAt(sets, fieldName, type);
            }
            catch { return false; }
        }

        private static bool ProjectileSetBool(string fieldName, int projectile)
        {
            try
            {
                Type sets = typeof(ProjectileID).GetNestedType("Sets", BindingFlags.Public | BindingFlags.NonPublic);
                return StaticBoolArrayAt(sets, fieldName, projectile);
            }
            catch { return false; }
        }

        private static int EnumIdByName(Type enumType, string name)
        {
            if (enumType == null || string.IsNullOrWhiteSpace(name)) return -1;
            foreach (var f in enumType.GetFields(PublicStatic))
            {
                if (!f.IsLiteral) continue;
                if (!string.Equals(f.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                try { return Convert.ToInt32(f.GetRawConstantValue()); } catch { return -1; }
            }
            return -1;
        }

        private static string EnumNameById(Type enumType, int id)
        {
            if (enumType == null) return "";
            foreach (var f in enumType.GetFields(PublicStatic))
            {
                if (!f.IsLiteral) continue;
                try
                {
                    if (Convert.ToInt32(f.GetRawConstantValue()) == id) return f.Name;
                }
                catch { }
            }
            return "";
        }

        private static string Tsv(string value) => (value ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();

        private static string ItemEnumName(int type)
        {
            try { return ItemID.Search.GetName(type) ?? ""; } catch { return ""; }
        }

        private static string IdMapItemRow(Item it)
        {
            int tile = it.createTile;
            string en = ItemEnumName(it.type);
            if (en.Length == 0) en = it.Name;
            bool solid = tile >= 0 && StaticBoolArrayAt(typeof(Main), "tileSolid", tile);
            bool solidTop = tile >= 0 && StaticBoolArrayAt(typeof(Main), "tileSolidTop", tile);
            bool tileMerge = tile >= 0 && TileSetBool("BlockMergesWithMergeAllBlock", tile);
            bool hook = it.shoot > 0 && StaticBoolArrayAt(typeof(Main), "projHook", it.shoot);
            bool pet = it.shoot > 0 && StaticBoolArrayAt(typeof(Main), "projPet", it.shoot);
            bool lightPet = it.shoot > 0 && ProjectileSetBool("LightPet", it.shoot);
            return string.Join("\t", new object[]
            {
                "item", it.type, Tsv(en), Tsv(it.Name), it.createTile, it.createWall,
                it.pick, it.axe, it.hammer, it.damage, it.maxStack,
                it.headSlot, it.bodySlot, it.legSlot, it.waistSlot, it.accessory ? 1 : 0, it.wingSlot,
                it.mountType, hook ? 1 : 0, pet ? 1 : 0, lightPet ? 1 : 0,
                it.healLife, it.healMana, it.potion ? 1 : 0, it.ammo, it.useAmmo,
                it.consumable ? 1 : 0, it.useStyle, it.shoot, it.buffType,
                solid ? 1 : 0, solidTop ? 1 : 0, tileMerge ? 1 : 0,
                -1, -1, -1, -1, -1,
            });
        }

        /// <summary>
        /// Dump engine constants and the fixed classification fields needed to decide what an
        /// item, tile or NPC can do. The command only writes the requested TSV file.
        /// </summary>
        private static string IdMap(JsonElement p)
        {
            string path = Json.Str(p, "path", "");
            if (string.IsNullOrWhiteSpace(path)) return Json.Error("idmap 需要 path（TSV 输出路径）");

            var lines = new List<string>
            {
                "kind\tid\tenum\tname\tcreateTile\tcreateWall\tpick\taxe\thammer\tdamage\tmaxStack\theadSlot\tbodySlot\tlegSlot\twaistSlot\taccessory\twingSlot\tmountType\thook\tpet\tlightPet\thealLife\thealMana\tpotion\tammo\tuseAmmo\tconsumable\tuseStyle\tshoot\tbuffType\tsolid\tsolidTop\ttileMerge\ttownNPC\tboss\tlifeMax\taiStyle\tcatchItem\tcritter",
            };
            int tiles = 0, walls = 0, items = 0, npcs = 0;

            var tileT = typeof(Main).Assembly.GetType("Terraria.ID.TileID");
            if (tileT != null)
                foreach (var f in tileT.GetFields(PublicStatic))
                {
                    long id;
                    try { if (!f.IsLiteral) continue; id = Convert.ToInt64(f.GetRawConstantValue()); } catch { continue; }
                    int tid = (int)id;
                    lines.Add(string.Join("\t", new object[]
                    {
                        "tile", id, f.Name, f.Name, -1, -1, 0, 0, 0, 0, 0,
                        -1, -1, -1, -1, 0, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                        StaticBoolArrayAt(typeof(Main), "tileSolid", tid) ? 1 : 0,
                        StaticBoolArrayAt(typeof(Main), "tileSolidTop", tid) ? 1 : 0,
                        TileSetBool("BlockMergesWithMergeAllBlock", tid) ? 1 : 0,
                        -1, -1, -1, -1, -1, 0,
                    }));
                    tiles++;
                }

            var wallT = typeof(Main).Assembly.GetType("Terraria.ID.WallID");
            if (wallT != null)
                foreach (var f in wallT.GetFields(PublicStatic))
                {
                    long id;
                    try { if (!f.IsLiteral) continue; id = Convert.ToInt64(f.GetRawConstantValue()); } catch { continue; }
                    lines.Add(string.Join("\t", new object[]
                    {
                        "wall", id, f.Name, f.Name, -1, -1, 0, 0, 0, 0, 0,
                        -1, -1, -1, -1, 0, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                        0, 0, 0, -1, -1, -1, -1, -1, 0,
                    }));
                    walls++;
                }

            foreach (var kv in ContentSamples.ItemsByType)
            {
                Item it = kv.Value;
                if (it == null || it.type <= 0 || string.IsNullOrWhiteSpace(it.Name)) continue;
                lines.Add(IdMapItemRow(it));
                items++;
            }

            // NPC fixed attributes are exported from a disposable prototype. This is read-only
            // and avoids learning static NPC facts by sending doomed commands to a live world.
            for (int id = 1; id < NPCID.Count; id++)
            {
                try
                {
                    NPC n = new NPC();
                    n.SetDefaults(id);
                    if (n.type <= 0 || string.IsNullOrWhiteSpace(n.FullName)) continue;
                    string en = EnumNameById(typeof(NPCID), id);
                    if (en.Length == 0) en = n.FullName;
                    lines.Add(string.Join("\t", new object[]
                    {
                        "npc", id, en, Tsv(n.FullName), -1, -1, 0, 0, 0, n.damage, 0,
                        -1, -1, -1, -1, 0, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                        0, 0, 0, n.townNPC ? 1 : 0, n.boss ? 1 : 0, n.lifeMax, n.aiStyle,
                        n.catchItem, n.CountsAsACritter ? 1 : 0,
                    }));
                    npcs++;
                }
                catch { }
            }

            try { System.IO.File.WriteAllLines(path, lines); }
            catch (Exception ex) { return Json.Error("写不了 " + path + ": " + ex.Message); }
            return Json.Ok(new { path, tiles, walls, items, npcs, rows = lines.Count - 1, schema = "item/tile/wall/npc-v2" });
        }

        private static int ResolveItemType(string name)
        {
            if (int.TryParse(name, out int numeric)) return numeric;
            foreach (var kv in ContentSamples.ItemsByType)
            {
                Item it = kv.Value;
                if (it == null) continue;
                string en = "";
                try { en = ItemID.Search.GetName(it.type) ?? ""; } catch { }
                if (string.Equals(en, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(it.Name, name, StringComparison.OrdinalIgnoreCase)) return it.type;
            }
            return -1;
        }

        private static string Props(JsonElement p)
        {
            string kind = (Json.Str(p, "kind", "") ?? "").Trim().ToLowerInvariant();
            string name = (Json.Str(p, "name", "") ?? "").Trim();
            int type = Json.Int(p, "type", -1);
            if (type < 0 && name.Length > 0)
            {
                if (kind == "item") type = ResolveItemType(name);
                else if (kind == "tile") type = EnumIdByName(typeof(TileID), name);
                else if (kind == "wall") type = EnumIdByName(typeof(WallID), name);
                else if (kind == "npc") type = EnumIdByName(typeof(NPCID), name);
            }
            if (kind == "item")
            {
                if (type <= 0 || !ContentSamples.ItemsByType.TryGetValue(type, out Item it) || it == null)
                    return Json.Error("props item 找不到 type/name");
                bool hook = it.shoot > 0 && StaticBoolArrayAt(typeof(Main), "projHook", it.shoot);
                bool pet = it.shoot > 0 && StaticBoolArrayAt(typeof(Main), "projPet", it.shoot);
                bool lightPet = it.shoot > 0 && ProjectileSetBool("LightPet", it.shoot);
                return Json.Ok(new
                {
                    kind, type, enumName = ItemEnumName(type), name = it.Name,
                    createTile = it.createTile, createWall = it.createWall,
                    pick = it.pick, axe = it.axe, hammer = it.hammer, damage = it.damage, maxStack = it.maxStack,
                    headSlot = it.headSlot, bodySlot = it.bodySlot, legSlot = it.legSlot, waistSlot = it.waistSlot,
                    accessory = it.accessory, wingSlot = it.wingSlot, mountType = it.mountType,
                    hook, pet, lightPet, shoot = it.shoot, buffType = it.buffType,
                    healLife = it.healLife, healMana = it.healMana, potion = it.potion,
                    ammo = it.ammo, useAmmo = it.useAmmo, consumable = it.consumable, useStyle = it.useStyle,
                });
            }
            if (kind == "tile")
            {
                if (!ValidTileType(type)) return Json.Error("props tile 找不到 type/name");
                return Json.Ok(new
                {
                    kind, type, enumName = TileID.Search.GetName(type),
                    solid = StaticBoolArrayAt(typeof(Main), "tileSolid", type),
                    solidTop = StaticBoolArrayAt(typeof(Main), "tileSolidTop", type),
                    tileMerge = TileSetBool("BlockMergesWithMergeAllBlock", type),
                    platform = TileSetBool("Platforms", type),
                    rope = type == TileID.Rope,
                });
            }
            if (kind == "wall")
            {
                if (type < 0 || type >= WallID.Count) return Json.Error("props wall 找不到 type/name");
                return Json.Ok(new { kind, type, enumName = EnumNameById(typeof(WallID), type) });
            }
            if (kind == "npc")
            {
                if (!ValidNpcType(type)) return Json.Error("props npc 找不到 type/name");
                try
                {
                    NPC n = new NPC();
                    n.SetDefaults(type);
                    return Json.Ok(new
                    {
                        kind, type, enumName = EnumNameById(typeof(NPCID), type), name = n.FullName,
                        townNPC = n.townNPC, boss = n.boss, lifeMax = n.lifeMax, aiStyle = n.aiStyle,
                        catchItem = n.catchItem, critter = n.CountsAsACritter,
                    });
                }
                catch (Exception ex) { return Json.Error("props npc 初始化失败: " + ex.Message); }
            }
            return Json.Error("props 需要 kind=item|tile|wall|npc，并给 type 或 name");
        }

        /// <summary>
        /// Read-only recipe lookup: product id, product name and required ingredients. Exists so
        /// item ids never have to be guessed again (a wrong id wasted a lot of time: 22 is an iron
        /// fence, not an iron bar).
        /// </summary>
        private static string Recipes(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            string q = Json.Str(p, "q", "");
            int limit = Json.Int(p, "limit", 80);
            var list = new List<object>();
            foreach (Recipe r in Main.recipe)
            {
                if (r == null) continue;
                Item res = r.createItem;
                if (res == null || res.type <= 0) continue;
                if (q.Length > 0 && res.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var need = new List<string>();
                foreach (Item ing in r.requiredItem)
                    if (ing != null && ing.type > 0) need.Add(ing.Name + "x" + ing.stack);
                list.Add(new { id = res.type, name = res.Name, need = string.Join(" + ", need) });
                if (list.Count >= limit) break;
            }
            return Json.Ok(new { count = list.Count, recipes = list });
        }

        /// <summary>
        /// Delete one tile without dropping anything. Repair only: it exists to undo a tile the
        /// agent itself placed by mistake (a Demon Altar cannot be broken in normal play, so
        /// without this the mistake would be permanent). Never use it to gain anything.
        /// </summary>
        private static string RemoveBlock(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("removeblock 需要 x,y");
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡，请先复活");
            if (!BridgeSystem.InReach(pl, x, y)) return Json.Error("够不到：离这一格超过 7 格，先走过去");
            int slot = -1;
            for (int i = 0; i < MainBagSlots; i++)
                if (pl.inventory[i] != null && pl.inventory[i].hammer > 0) { slot = i; break; }
            if (slot < 0) return Json.Error("背包里没有锤子，拆方块需要锤子");
            int before = Main.tile[x, y].HasTile ? (int)Main.tile[x, y].TileType : -1;
            // 真实挥锤：原来这里是 WorldGen.KillTile，等于直接删格子。
            BridgeSystem.BeginItemUse(slot, x, y, Math.Clamp(Json.Int(p, "ms", 900), 200, 3000));
            int after = Main.tile[x, y].HasTile ? (int)Main.tile[x, y].TileType : -1;
            return Json.Ok(new { x, y, before, after, removed = before != after, slot,
                next = "真实挥锤是异步的：稍等再用 tile x,y 回读确认已空" });
        }

        /// <summary>
        /// 存盘。两条路：
        ///   1) 服务端进程（netMode 2 / -server）：世界本来就归它，直接 WorldFile.SaveWorld() 就行，
        ///      绝不要自己伪造 ActiveWorldFileData —— CreateMetadata 的参数是"世界名"不是路径，
        ///      把全路径塞进去会被净化成 `C--Users-...` 这种乱名，存档被写到另一个文件里
        ///      （实测：返回 saved:true，但真正的 .wld 时间戳没变）。
        ///   2) 客户端（netMode 1）：它没有世界所有权，伪造元数据只能写出一份乱名副本，
        ///      所以直接报错并说明"要连服务端端口"。
        /// </summary>
        private static string SaveWorld(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            var attempts = new List<object>();
            bool madeMeta = false;

            if (Main.netMode != 1)
            {
                foreach (var t in new (string Name, Action Run)[]
                {
                    ("SaveWorld()", () => WorldFile.SaveWorld()),
                    ("SaveWorld(false,false)", () => WorldFile.SaveWorld(false, false)),
                })
                {
                    try
                    {
                        t.Run();
                        return Json.Ok(new
                        {
                            saved = true,
                            via = t.Name,
                            madeMeta,
                            attempts,
                            worldPath = Main.worldPathName,
                            netMode = Main.netMode,
                            world = Main.worldName,
                            spawn = new { x = Main.spawnTileX, y = Main.spawnTileY },
                        });
                    }
                    catch (Exception e)
                    {
                        string st = e.StackTrace ?? "";
                        var frames = st.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).Take(6).ToArray();
                        attempts.Add(new { via = t.Name, error = e.GetType().Name, message = e.Message, frames });
                    }
                }
            }

            return Json.Ok(new { saved = false, madeMeta, attempts,
                hasWorldFileData = Main.ActiveWorldFileData != null,
                worldPath = Main.worldPathName,
                netMode = Main.netMode,
                gameMenu = Main.gameMenu,
                hint = Main.netMode == 1
                    ? "本进程是世界客户端，存盘要发给服务端进程（专用服务端端口 47778）"
                    : null });
        }

        /// <summary>
        /// Run the game's room check and dump every room/needs-related static it touches, so
        /// "why will nobody move in" can be answered by the game instead of by guessing.
        /// </summary>
        private static string RoomDiagnostics(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("needs 需要 x,y");
            bool found = WorldGen.StartRoomCheck(x, y);
            var fields = new Dictionary<string, object>();
            foreach (var f in typeof(WorldGen).GetFields(System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(f.Name, "(?i)room|need|house")) continue;
                try { fields[f.Name] = f.GetValue(null)?.ToString(); } catch { }
            }
            var npcFields = new Dictionary<string, object>();
            NPC guide = null;
            for (int i = 0; i < Main.maxNPCs; i++)
                if (Main.npc[i] != null && Main.npc[i].active && Main.npc[i].townNPC) { guide = Main.npc[i]; break; }
            if (guide != null)
            {
                npcFields["name"] = guide.FullName;
                npcFields["homeless"] = guide.homeless;
                npcFields["homeTile"] = new[] { guide.homeTileX, guide.homeTileY };
                npcFields["position"] = new[] { (int)(guide.Center.X / 16f), (int)(guide.Center.Y / 16f) };
            }
            return Json.Ok(new { x, y, found, worldFields = fields, npc = npcFields });
        }


        /// <summary>
        /// 让游戏自己判一次房屋，并把"缺什么"报出来。
        /// 正确链路（原版房屋 UI 用的就是这两步）：
        ///   WorldGen.StartRoomCheck(x, y)  → 洪水填充，返回房间是否成立
        ///   WorldGen.RoomNeeds(npcType)    → 这个房间满不满足某个 NPC 的需求，失败时写 roomCheckFailureReason
        /// 之前写的是 StartRoomCheck + CheckRoom(0,1)，CheckRoom 的入参是瓦片坐标，
        /// 等于去判 (0,1) 那一格，所以永远报 None —— 白猜了很久。
        /// </summary>
        private static string RoomCheckCmd(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("roomcheck 需要 x,y（房间内任意一格）");
            int npcType = Json.Int(p, "npc", NPCID.Merchant);
            string mode = Json.Str(p, "mode", "all") ?? "all";

            var steps = new List<object>();
            void Snap(string label)
            {
                var r = WorldGen.roomCheckFailureReason;
                steps.Add(new
                {
                    step = label,
                    reason = r.ToString(),
                    reasonValue = (int)r,
                    tiles = WorldGen.numRoomTiles,
                    box = new[] { WorldGen.roomX1, WorldGen.roomX2, WorldGen.roomY1, WorldGen.roomY2 },
                    ceilings = WorldGen.roomCeilingsCount,
                });
            }

            WorldGen.roomCheckFailureReason = 0;
            bool found = false, needs = false;
            string ex = null;
            try
            {
                if (mode == "score")
                {
                    // 房屋 UI 打分走的是 ScoreRoom：它会自己做洪水填充并写 roomCheckFailureReason。
                    WorldGen.ScoreRoom(x, y);
                    Snap("afterScore");
                    needs = WorldGen.RoomNeeds(npcType);
                    Snap("afterNeeds");
                    found = WorldGen.roomCheckFailureReason == 0 && WorldGen.numRoomTiles > 0;
                }
                else
                {
                    // CheckRoom 才是真正做洪水填充的那个；StartRoomCheck 只做初始化。
                    WorldGen.StartRoomCheck(x, y);
                    Snap("afterStart");
                    WorldGen.CheckRoom(x, y);
                    Snap("afterCheck");
                    found = WorldGen.numRoomTiles > 0 && WorldGen.roomCheckFailureReason == 0;
                    needs = WorldGen.RoomNeeds(npcType);
                    Snap("afterNeeds");
                }
            }
            catch (Exception e) { ex = e.GetType().Name + ": " + e.Message; }

            var reason = WorldGen.roomCheckFailureReason;
            return Json.Ok(new
            {
                x,
                y,
                npcType,
                mode,
                found,
                needs,
                ok = found && needs,
                reason = reason.ToString(),
                reasonValue = (int)reason,
                ex,
                steps,
            });
        }

        private static string Housing(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("housing 需要 x,y（房间内任意一格）");

            bool roomFound = WorldGen.StartRoomCheck(x, y);
            int homeless = 0;
            var homes = new List<object>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.townNPC) continue;
                if (n.homeless) homeless++;
                homes.Add(new
                {
                    who = i,
                    name = n.FullName,
                    homeless = n.homeless,
                    home = new { x = n.homeTileX, y = n.homeTileY },
                });
            }
            return Json.Ok(new
            {
                x,
                y,
                roomFound,
                homeless,
                townNPCs = homes.Count,
                homes,
            });
        }

        /// <summary>
        /// 一键让 NPC 回家：直接调用原版 WorldGen.moveRoom —— 就是房屋菜单里点"回家/搬到这间屋"
        /// 走的那条路。房间不合法时原版自己会拒绝，npc.homeless 不会变，所以结果可验证。
        /// </summary>
        private static string HomeRoom(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            int who = Json.Int(p, "who", -1);
            if (x < 0 || y < 0) return Json.Error("homeroom 需要 x,y（房间内任意一格）");
            if (who < 0 || who >= Main.maxNPCs) return Json.Error("homeroom 需要 who（NPC 索引，housing 的 homes[].who）");
            NPC n = Main.npc[who];
            if (n == null || !n.active || !n.townNPC) return Json.Error($"NPC {who} 不是活跃的城镇 NPC");
            bool wasHomeless = n.homeless;
            int oldX = n.homeTileX, oldY = n.homeTileY;
            string via = null;

            // 第一条路：引擎自己的"一键回家"。moveRoom 只有在房间检查状态刚建立好时才生效，
            // 所以先 StartRoomCheck 再调；不生效就看返回值，不瞎猜。
            WorldGen.roomCheckFailureReason = 0;
            bool started = false;
            try { started = WorldGen.StartRoomCheck(x, y); } catch { }
            if (started)
            {
                // 第三个参数是 NPC 的**类型**（NPC.type），不是数组下标 —— 传 who 会去找
                // "类型等于这个下标"的 NPC，于是一个都不会动（表现为 via:"fields"）。
                try { WorldGen.moveRoom(x, y, n.type); } catch { }
                if (n.homeless != wasHomeless || n.homeTileX != oldX || n.homeTileY != oldY) via = "moveRoom";
            }

            // 第二条路（房屋菜单的等价操作）：菜单里点一下就干这两件事——
            // 把人从这个房间"取消无家可归"、把 home 指到房间格。只写 NPC 的两个归属字段，
            // 不动世界、不动瓦片，所以不违反"世界只能通过真实操作改变"。
            if (via == null)
            {
                n.homeless = false;
                n.homeTileX = x;
                n.homeTileY = y;
                via = "fields";
            }

            var reason = WorldGen.roomCheckFailureReason;
            return Json.Ok(new
            {
                who,
                name = n.FullName,
                wasHomeless,
                homeless = n.homeless,
                moved = !n.homeless && (n.homeTileX != oldX || n.homeTileY != oldY || wasHomeless),
                via,
                startRoomCheck = started,
                reason = reason.ToString(),
                reasonValue = (int)reason,
                tiles = WorldGen.numRoomTiles,
                oldHome = new { x = oldX, y = oldY },
                home = new { x = n.homeTileX, y = n.homeTileY },
            });
        }

        /// <summary>
        /// 自己的房屋判定：不依赖 WorldGen 那套全局静态计数器。
        /// 实测那些计数器在同一帧里反复调用会给出自相矛盾的 box（连合法房间也报
        /// HoleInWallIsTooBig，且 box 总是往西/北漏到固定坐标），没法当判据。
        /// 这里一次独立洪水填充 + 逐条核对原版规则，返回"缺什么"。
        /// </summary>
        /// <summary>登记/查看/清空“不许挖”矩形：自己造的东西靠类型分不出来，只能显式记。</summary>
        private static string NoDig(JsonElement p)
        {
            string act = (Json.Str(p, "action", "list") ?? "list").ToLowerInvariant();
            if (act == "clear") { BridgeSystem.NoDigZones.Clear(); return Json.Ok(new { cleared = true, count = 0 }); }
            if (act == "add")
            {
                int x1 = Json.Int(p, "x1", 0), y1 = Json.Int(p, "y1", 0), x2 = Json.Int(p, "x2", 0), y2 = Json.Int(p, "y2", 0);
                if (x1 <= 0 || y1 <= 0 || x2 <= 0 || y2 <= 0) return Json.Error("nodig add 需要 x1,y1,x2,y2");
                BridgeSystem.NoDigZones.Add((Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2)));
            }
            var list = BridgeSystem.NoDigZones.Select(z => new { x0 = z.x0, y0 = z.y0, x1 = z.x1, y1 = z.y1 }).ToList();
            return Json.Ok(new { count = list.Count, zones = list });
        }

        private static string HouseDiag(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int sx = Json.Int(p, "x", -1), sy = Json.Int(p, "y", -1);
            if (sx < 1 || sy < 1) return Json.Error("housediag 需要 x,y（房间内任意一格）");
            if (WorldGen.SolidTile(sx, sy, false)) return Json.Error($"({sx},{sy}) 是实心格子，房间扫描要从空气格出发");

            int wX = Main.maxTilesX, hY = Main.maxTilesY;
            // 房屋边界必须和游戏判定一致：平台(tileSolidTop)在原版房屋检查里算“挡住”，
            // 但 WorldGen.SolidTile 只认 tileSolid，于是平台屋顶的房间会被误判成漏水 9001 格。
            // 实测：屋顶换平台后 roomcheck ok:true（游戏认可），housediag 却报 9001。
            bool Edge(int x, int y) { Tile tt = Main.tile[x, y]; return tt.HasTile && (Main.tileSolid[tt.TileType] || Main.tileSolidTop[tt.TileType]); }

            var seen = new HashSet<int>();
            var stack = new Stack<Point>();
            int minX = sx, hiX = sx, minY = sy, hiY = sy;
            bool leak = false;

            void Push(int x, int y)
            {
                if (x < 1 || y < 1 || x >= wX - 1 || y >= hY - 1) { leak = true; return; }
                if (!Edge(x, y)) stack.Push(new Point(x, y));
            }

            stack.Push(new Point(sx, sy));
            while (stack.Count > 0)
            {
                Point c = stack.Pop();
                int key = c.Y * wX + c.X;
                if (!seen.Add(key)) continue;
                if (seen.Count > 9000) { leak = true; break; }
                if (c.X < minX) minX = c.X;
                if (c.X > hiX) hiX = c.X;
                if (c.Y < minY) minY = c.Y;
                if (c.Y > hiY) hiY = c.Y;
                Push(c.X - 1, c.Y); Push(c.X + 1, c.Y); Push(c.X, c.Y - 1); Push(c.X, c.Y + 1);
            }

            int walled = 0;
            foreach (int key in seen)
            {
                int x = key % wX, y = key / wX;
                if (Main.tile[x, y].WallType > 0) walled++;
            }

            // 边界实心比例：地板 / 天花板 / 两侧墙（同样夹范围，避免贴边缘时越界）
            int floorSolid = 0, ceilSolid = 0, leftSolid = 0, rightSolid = 0;
            int spanX = hiX - minX + 1, spanY = hiY - minY + 1;
            int fY = Math.Min(hY - 2, hiY + 1), cY = Math.Max(1, minY - 1);
            for (int x = minX; x <= hiX; x++)
            {
                if (Edge(x, fY)) floorSolid++;
                if (Edge(x, cY)) ceilSolid++;
            }
            int lX = Math.Max(1, minX - 1), rX = Math.Min(wX - 2, hiX + 1);
            for (int y = minY; y <= hiY; y++)
            {
                if (Edge(lX, y)) leftSolid++;
                if (Edge(rX, y)) rightSolid++;
            }

            // 漏水点：把房间矩形四条边上"非实心"的格子列出来。
            // 之前排查一扇开着的门要逐格读 32 次；有了这个字段，一次 housediag 就能看出墙在哪破了。
            var leaks = new List<object>();
            void AddLeak(int x, int y)
            {
                if (leaks.Count >= 12) return;
                if (x < 1 || y < 1 || x >= wX - 1 || y >= hY - 1) return;
                if (Edge(x, y)) return;
                Tile lt = Main.tile[x, y];
                leaks.Add(new { x, y, name = lt.HasTile ? TileID.Search.GetName(lt.TileType) : "AIR" });
            }
            for (int x = minX; x <= hiX; x++) { AddLeak(x, cY); AddLeak(x, fY); }
            for (int y = minY; y <= hiY; y++) { AddLeak(lX, y); AddLeak(rX, y); }

            int light = 0, comfort = 0, flat = 0, doors = 0;
            // 夹到世界范围内：洪水填充一旦漏出去，minX/hiX 会贴到地图边缘，
            // 直接 Main.tile[x,y] 就越界抛 IndexOutOfRange（曾经把整条命令打成报错）。
            int lx0 = Math.Max(1, minX - 2), lx1 = Math.Min(wX - 2, hiX + 2);
            int ly0 = Math.Max(1, minY - 2), ly1 = Math.Min(hY - 2, hiY + 2);
            for (int x = lx0; x <= lx1; x++)
            {
                for (int y = ly0; y <= ly1; y++)
                {
                    Tile t = Main.tile[x, y];
                    if (!t.HasTile) continue;
                    int ty = t.TileType;
                    // 用方块 id 字面量：tModLoader 1.4.4 的 TileID 里没有 Candle/Candelabra/
                    // Chandelier 这些成员（编译期就报 CS0117），字面量不会随 API 改名而失效。
                    if (ty == 4 || ty == 149 || ty == 33 || ty == 49 || ty == 42)
                        light++;          // 火把 / 营火 / 蜡烛 / 水蜡烛 / 提灯
                    else if (ty == 15 || ty == 87 || ty == 90)
                        comfort++;        // 椅子 / 王座 / 长椅
                    else if (ty == 14 || ty == 18 || ty == 88 || ty == 469)
                        flat++;           // 桌子 / 工作台 / 梳妆台
                    else if (ty == 10 || ty == 11)
                        doors++;          // 关着的门 / 开着的门
                }
            }

            double floorR = spanX > 0 ? (double)floorSolid / spanX : 0;
            double ceilR = spanX > 0 ? (double)ceilSolid / spanX : 0;
            double leftR = spanY > 0 ? (double)leftSolid / spanY : 0;
            double rightR = spanY > 0 ? (double)rightSolid / spanY : 0;

            // 以游戏为准绳：CheckRoom + RoomNeeds 才是选中房屋 UI 时走的那条路。
            // 实测把屋顶换成平台后 roomcheck ok:true、三个 NPC 正常入住，而本命令用“内部空气格≥60”
            // 的口径给出 58 判不合格 —— 两套口径算的不是同一个量，valid 不能再自己拍。
            bool gameOk = false; int gameTiles = 0; string gameReason = "?";
            try
            {
                WorldGen.roomCheckFailureReason = 0;
                WorldGen.StartRoomCheck(sx, sy);
                WorldGen.CheckRoom(sx, sy);
                gameTiles = WorldGen.numRoomTiles;
                gameReason = WorldGen.roomCheckFailureReason.ToString();
                gameOk = gameTiles > 0 && WorldGen.roomCheckFailureReason == 0;
            }
            catch (Exception e) { gameReason = e.GetType().Name; }
            var missing = new List<string>();
            if (leak) missing.Add("房间没封住（洪水跑到边界外/超过 9000 格）");
            if (!gameOk) missing.Add($"游戏判定不合格：{gameReason}");
            if (seen.Count > 7500) missing.Add($"空间太大：{seen.Count} > 7500");
            if (walled < seen.Count) missing.Add($"背墙不全：{walled}/{seen.Count} 格有墙");
            if (floorR < 0.9) missing.Add($"地板不连续：{floorSolid}/{spanX}");
            if (ceilR < 0.9) missing.Add($"天花板不连续：{ceilSolid}/{spanX}");
            if (leftR < 0.9) missing.Add($"左墙不连续：{leftSolid}/{spanY}");
            if (rightR < 0.9) missing.Add($"右墙不连续：{rightSolid}/{spanY}");
            if (light == 0) missing.Add("缺光源");
            if (comfort == 0) missing.Add("缺椅子");
            if (flat == 0) missing.Add("缺桌子/工作台");

            return Json.Ok(new
            {
                x = sx,
                y = sy,
                tiles = seen.Count,
                box = new[] { minX, hiX, minY, hiY },
                span = new { x = spanX, y = spanY },
                leaks,
                enclosed = !leak,
                walled,
                floorRatio = Math.Round(floorR, 2),
                ceilRatio = Math.Round(ceilR, 2),
                leftRatio = Math.Round(leftR, 2),
                rightRatio = Math.Round(rightR, 2),
                light, comfort, flat, doors,
                missing,
                gameOk, gameTiles, gameReason,
                // 以游戏自己的判定为准：WorldGen.roomCheckFailureReason 才是「NPC 会不会进来」的真值。
                // 旧写法 missing.Count == 0 会因为自家 flood fill 的边角统计（家具/地面格算不算背墙）
                // 把一间游戏判 ok 的房子判成不合格。
                valid = gameOk && !leak && seen.Count <= 7500,
            });
        }

        /// <summary>
        /// Take an accessory off (or put it back). Needed to measure base movement: wings change
        /// jump height and flight entirely, so any jump measurement has to happen without them.
        /// </summary>
        /// <summary>
        /// 朝一个方块坐标走过去：逐帧驱动（遇墙挖、遇坑跳、泡水上浮、见岩浆/死亡停）。
        /// 非阻塞：命令立刻返回，用 state.walk 看进度和停止原因。
        /// </summary>
        /// <summary>
        /// 用 A* 算一条到目标的路并逐格执行（实心就挖、悬空就垫、上下自动）。非阻塞。
        /// </summary>
        /// <summary>
        /// 矩形填充：用背包里真实的方块铺地（WorldGen.PlaceTile，就是物品放置走的那个函数）。
        /// 只用于修地/搭平台这类施工，方块从背包扣，材料不够就停。
        /// 距离放宽到 30 格（普通放置是 5 格），因为这是"施工"不是"打怪捡东西"。
        /// </summary>
        private static string FillArea(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int x1 = Json.Int(p, "x1", -1), y1 = Json.Int(p, "y1", -1);
            int x2 = Json.Int(p, "x2", -1), y2 = Json.Int(p, "y2", -1);
            int type = Json.Int(p, "type", 30);
            if (x1 < 0 || y1 < 0 || x2 < 0 || y2 < 0) return Json.Error("fill 需要 x1,y1,x2,y2");
            int loX = Math.Min(x1, x2), hiX = Math.Max(x1, x2);
            int loY = Math.Min(y1, y2), hiY = Math.Max(y1, y2);
            long area = (long)(hiX - loX + 1) * (hiY - loY + 1);
            if (area > 400) return Json.Error($"区域太大（{area} 格，上限 400）");

            int slot = -1;
            for (int i = 0; i < MainBagSlots; i++)
            {
                Item it = pl.inventory[i];
                if (it != null && it.type > 0 && it.stack > 0 && it.createTile == type) { slot = i; break; }
            }
            if (slot < 0) return Json.Error($"背包里没有能放成方块 {type} 的物品");

            int placed = 0, skipped = 0, outOfRange = 0, armed = 0;
            for (int x = loX; x <= hiX; x++)
            {
                for (int y = loY; y <= hiY; y++)
                {
                    Tile t = Main.tile[x, y];
                    if (t.HasTile) { skipped++; continue; }
                    if (Vector2.Distance(pl.Center, new Vector2(x * 16f + 8f, y * 16f + 8f)) > 30 * 16f) { outOfRange++; continue; }
                    if (pl.inventory[slot].stack <= 0) break;
                    // 真实放置：选中方块物品、准星压到该格、按住使用。
                    // 原来这里 WorldGen.PlaceTile + 手动 stack--，违反"世界只能通过真实操作改变"。
                    BridgeSystem.BeginItemUse(slot, x, y, 140);
                    armed++;
                }
            }
            return Json.Ok(new { armed, skipped, outOfRange, left = pl.inventory[slot]?.stack ?? 0,
                next = "真实放置是异步的：稍后逐格 tile 回读确认，不要直接相信 armed 就是放成了" });
        }

        /// <summary>
        /// 旅行模式的"研究"：把背包里的物品献祭给创造模式追踪器（这是模式自带机制）。
        /// 只献祭真实存在的物品，达到需求数就解锁无限复制。
        /// </summary>
        /// <summary>
        /// 在世界数据里直接搜方块（不移动、不挖），用来找腐化/猩红/祭坛这类"知道存在但不知道在哪"的东西。
        /// 限定 x/y 范围和最大结果数，避免一次扫全图。
        /// </summary>
        private static string FindTile(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int type = Json.Int(p, "type", -1);
            // 土块是 tile id 0：以前用 type <= 0 判空，把"找泥土"整个挡掉了。
            if (type < 0) return Json.Error("find 需要 type（0 是土块，合法）");
            int x1 = Json.Int(p, "x1", 0);
            int x2 = Json.Int(p, "x2", Main.maxTilesX - 1);
            int y1 = Json.Int(p, "y1", 0);
            int y2 = Json.Int(p, "y2", Main.maxTilesY - 1);
            int max = Json.Int(p, "max", 20);
            x1 = Math.Max(4, x1); y1 = Math.Max(4, y1);
            x2 = Math.Min(Main.maxTilesX - 5, x2); y2 = Math.Min(Main.maxTilesY - 5, y2);

            var found = new List<object>();
            int total = 0;
            for (int x = x1; x <= x2 && found.Count < max; x++)
            {
                for (int y = y1; y <= y2; y++)
                {
                    Tile t = Main.tile[x, y];
                    if (t != null && t.HasTile && t.TileType == type)
                    {
                        total++;
                        if (found.Count < max) found.Add(new { x, y });
                    }
                }
            }
            return Json.Ok(new { type, total, found, scanned = new { x1, x2, y1, y2 } });
        }

        private static string Research(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int slot = Json.Int(p, "slot", -1);
            if (slot < 0 || slot >= pl.inventory.Length) return Json.Error("slot 越界");
            Item it = pl.inventory[slot];
            if (it == null || it.type <= 0) return Json.Error("这个格子是空的");

            var tracker = pl.creativeTracker.ItemSacrifices;
            if (!CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(it.type, out int needed))
                return Json.Error($"{it.Name} 不能研究");
            int have = tracker.GetSacrificeCount(it.type);
            int give = Math.Min(it.stack, Math.Max(0, needed - have));
            if (give <= 0) return Json.Ok(new { item = it.Name, alreadyFull = true, have, needed });

            tracker.RegisterItemSacrifice(it.type, give);
            it.stack -= give;
            if (it.stack <= 0) { pl.inventory[slot] = new Item(); pl.inventory[slot].SetDefaults(0); }
            int now = tracker.GetSacrificeCount(it.type);
            return Json.Ok(new { item = it.type, researched = give, have = now, needed, unlocked = now >= needed });
        }

        /// <summary>
        /// 旅行模式的"复制"：只有**已研究满**的物品才能无限取用（游戏自身的门槛）。
        /// </summary>
        private static string Duplicate(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int type = Json.Int(p, "item", -1);
            int count = Json.Int(p, "count", 1);
            if (type <= 0) return Json.Error("duplicate 需要 item");
            var tracker = pl.creativeTracker.ItemSacrifices;
            if (!CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(type, out int needed))
                return Json.Error("这个物品不能研究，也就不能复制");
            int have = tracker.GetSacrificeCount(type);
            if (have < needed) return Json.Error($"还没研究满：{have}/{needed}");

            var proto = new Item();
            proto.SetDefaults(type);
            int added = 0, remaining = Math.Min(count, 9999);
            while (remaining > 0)
            {
                int chunk = Math.Min(remaining, proto.maxStack);
                int slot = -1;
                for (int i = 0; i < MainBagSlots; i++)
                {
                    Item inv = pl.inventory[i];
                    if (inv != null && inv.type == type && inv.stack + chunk <= inv.maxStack) { slot = i; break; }
                }
                if (slot < 0)
                    for (int i = 0; i < MainBagSlots; i++)
                        if (pl.inventory[i] != null && pl.inventory[i].type == 0) { slot = i; break; }
                if (slot < 0) break;
                if (pl.inventory[slot].type == type) pl.inventory[slot].stack += chunk;
                else { var ni = new Item(); ni.SetDefaults(type); ni.stack = chunk; pl.inventory[slot] = ni; }
                added += chunk; remaining -= chunk;
            }
            return Json.Ok(new { item = type, added, unlockedCount = have });
        }

        private static string PathTo(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("path 需要 x,y");
            int ms = Json.Int(p, "ms", 45000);
            int px = (int)(pl.Center.X / 16f), py = (int)(pl.Center.Y / 16f);
            bool allowDig = !Json.Bool(p, "dig", true).Equals(false);
            if (!allowDig) BridgeSystem.PathAllowDig = false;
            // 先按“不拆自建物”找路；确实绕不过去才放开拆（否则困在自建房里出不来）。
            BridgeSystem.PathProtectBuilds = true;
            var path = BridgeSystem.ComputePath(px, py, x, y, out string why);
            if (path == null)
            {
                BridgeSystem.PathProtectBuilds = false;
                path = BridgeSystem.ComputePath(px, py, x, y, out why);
            }
            BridgeSystem.PathAllowDig = true;
            if (path == null) return Json.Error(why);
            BridgeSystem.BeginPath(path, BridgeSystem.Now + BridgeSystem.Ticks(ms), allowDig);
            var head = new List<object>();
            for (int i = 0; i < Math.Min(6, path.Count); i++) head.Add(new { x = path[i].x, y = path[i].y });
            return Json.Ok(new { from = new { x = px, y = py }, to = new { x, y }, length = path.Count, head });
        }

        /// <summary>
        /// 试探性移动预判：模拟向某方向移动 N 格会不会掉坑/撞墙/卡住，返回预测结果而不实际移动。
        /// AI 可以用它做”往右走 10 格安全吗”判断，再决定要不要真走或搭桥。
        /// </summary>
        private static string Probe(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            string dir = (Json.Str(p, "dir", "right") ?? "right").ToLowerInvariant();
            if (dir != "left" && dir != "right") return Json.Error("probe 的 dir 只能是 left/right");
            int steps = Math.Clamp(Json.Int(p, "steps", 5), 1, 50);

            int px = (int)(pl.Center.X / 16f), py = (int)(pl.Center.Y / 16f);
            int dx = dir == "right" ? 1 : -1;
            string issue = null;
            int reachedSteps = 0;

            // 模拟逐格前进，检测会不会掉坑/撞墙
            for (int i = 1; i <= steps; i++)
            {
                int nx = px + dx * i;
                int ny = py;

                // 检查身体是否能通过（上下两格都要空）
                if (!BridgeSystem.PathBodyFree(nx, ny))
                {
                    issue = "blocked";
                    break;
                }

                // 检查脚下是否有地板
                if (!BridgeSystem.PathFloorAt(nx, ny))
                {
                    // 没地板，向下找最近的落脚点（最多找 14 格）
                    bool foundFloor = false;
                    for (int down = 1; down <= 14; down++)
                    {
                        if (BridgeSystem.PathBodyFree(nx, ny + down) && BridgeSystem.PathFloorAt(nx, ny + down))
                        {
                            if (down > 3)
                            {
                                issue = $"pit_{down}_deep";
                                break;
                            }
                            foundFloor = true;
                            ny = ny + down;
                            break;
                        }
                    }
                    if (!foundFloor && issue == null) issue = "pit_deep";
                    if (issue != null) break;
                }

                reachedSteps = i;
            }

            return Json.Ok(new
            {
                from = new { x = px, y = py },
                dir,
                steps,
                reachedSteps,
                safe = issue == null,
                issue = issue ?? "none"
            });
        }

        /// <summary>
        /// 只读查询 travel 任务状态。原来只能靠再调一次 travel 来”看”状态，
        /// 而那会重新 StartTravel 把 steps/mine/swings 清零，导致我误判成”travel 坏了、根本不动”。
        /// </summary>
        /// <summary>只读：新旧两套走路逻辑在同一帧各自算出的行为，用于重构期对比。</summary>
        /// <summary>切换走路驱动：new=四层行为系统，old=旧逻辑（重构期对照用）。</summary>
        /// <summary>导航热更参数：默认只读回状态；`reload:true` 强制重读文件（也有 mtime 自动重载）。</summary>
        private static string NavRulesCmd(JsonElement p)
        {
            if (Json.Bool(p, "reload", false)) { string r = NavRules.Reload(); return Json.Ok(new { reload = r, state = NavRules.Status() }); }
            NavRules.EnsureFresh();
            return Json.Ok(NavRules.Status());
        }

        private static string WalkModeCmd(JsonElement p)
        {
            string mode = Json.Str(p, "mode", "");
            if (mode == "new") BridgeSystem.ActorUseNew = true;
            else if (mode == "old") BridgeSystem.ActorUseNew = false;
            // 两套走路执行器并存：reactive=每帧 Sense→Evaluate→Act（不追路点）；
            // legacy=旧 walkto 节点追踪（隔离保留，供同场景对比）。
            else if (mode == "reactive") BridgeSystem.WalkReactive = true;
            else if (mode == "legacy") BridgeSystem.WalkReactive = false;
            return Json.Ok(new Dictionary<string, object>
            {
                ["useNew"] = BridgeSystem.ActorUseNew,
                ["reactive"] = BridgeSystem.WalkReactive,
                ["dual"] = BridgeSystem.ActorDualRun
            });
        }

        private static string WalkDiffCmd()
        {
            var nw = BridgeSystem.ActorNew;
            var od = BridgeSystem.ActorOld;
            return Json.Ok(new Dictionary<string, object>
            {
                ["dual"] = BridgeSystem.ActorDualRun,
                ["newRule"] = nw.rule ?? "",
                ["newPriority"] = nw.priority,
                ["newKind"] = nw.kind ?? "",
                ["oldRule"] = od.rule ?? "",
                ["oldKind"] = od.kind ?? "",
                ["same"] = (nw.kind ?? "") == (od.kind ?? "")
            });
        }

        private static string TravelStatusCmd()
        {
            if (!InWorld(out string err)) return Json.Error(err);
            return BridgeSystem.TravelStatus();
        }

        /// <summary>
        /// 直线旅行：在游戏内跑“补地板 + 挖挡路格 + 按住方向键”的循环。
        /// C# 只做最耗时的地形活；遇敌、血低、到达、超时都会停下，把决策交回脚本层。
        /// </summary>
        private static string TravelCmd(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (Json.Bool(p, "stop", false)) { BridgeSystem.StopTravel("requested"); return BridgeSystem.TravelStatus(); }
            string dir = (Json.Str(p, "dir", "right") ?? "right").ToLowerInvariant();
            if (dir != "left" && dir != "right" && dir != "down" && dir != "up") return Json.Error($"dir 只能是 left/right/down/up，收到 {dir}");
            int distance = Math.Clamp(Json.Int(p, "distance", 100), 1, 4200);
            int ms = Math.Clamp(Json.Int(p, "ms", 60000), 200, 900000);
            int px = (int)MathF.Floor(P.Center.X / 16f);
            int py = (int)MathF.Floor(P.Center.Y / 16f);
            int sign = dir == "down" ? 2 : (dir == "up" ? 3 : (dir == "right" ? 1 : -1));
            int target = dir == "down" ? py + distance : (dir == "up" ? py - distance : px + sign * distance);
            bool dig = Json.Bool(p, "dig", true);
            // 用户两次被从出生点推进水里/坑里：出生点周围 100 格内一律不许挖。
            // 写进代码而不是靠自觉——忘记了就会再犯一次。
            // 以前只挡了 dir=="down"：实测不带参数调 `travel`（默认 dir=right、distance=100、dig=true）
            // 在出生点旁边一路挖了 10 格、挥了 12 下才停 —— 水平方向根本没挡。
            // 现在起点或终点任一落在保护圈内就拒绝。
            if (dig)
            {
                int sx = Main.spawnTileX, sy = Main.spawnTileY;
                bool vertical = dir == "up" || dir == "down";
                int tx = vertical ? px : target;
                int ty = vertical ? target : py;
                bool startNear = Math.Abs(px - sx) <= 100 && Math.Abs(py - sy) <= 100;
                bool endNear = Math.Abs(tx - sx) <= 100 && Math.Abs(ty - sy) <= 100;
                if (startNear || endNear)
                    return Json.Error(
                        $"拒绝在出生点 {sx},{sy} 附近 100 格内挖（起点 {px},{py}，终点 {tx},{ty}，dir={dir}）；" +
                        "只走路请传 dig:false，要开路请先走到保护圈外");
            }
            BridgeSystem.StartTravel(sign, target, ms, dig);
            return BridgeSystem.TravelStatus();
        }

        private static string WalkTo(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("walkto 需要 x,y");
            // 目标格先验：不能站的格子 A* 一定算不出路，最后只回一句 no-path，
            // 看起来像"左边没路/被墙挡住"（实测把 8 格深的坑口当成"走不过去"）。
            // 默认拒绝并给出最近可站点；force:true 跳过校验。
            bool walkForce = Json.Bool(p, "force", false);
            if (!walkForce && (!BridgeSystem.PathBodyFree(x, y) || !BridgeSystem.PathFloorAt(x, y)))
            {
                int bx = -1, by = -1, best = int.MaxValue;
                for (int dy = -8; dy <= 8; dy++)
                {
                    for (int dx = -8; dx <= 8; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (!BridgeSystem.PathBodyFree(nx, ny)) continue;
                        if (!BridgeSystem.PathFloorAt(nx, ny)) continue;
                        int d = Math.Abs(dx) + Math.Abs(dy);
                        if (d < best) { best = d; bx = nx; by = ny; }
                    }
                }
                if (bx < 0)
                    return Json.Error(
                        $"walkto 的目标 ({x},{y}) 不能站立：身体格被挡或脚下没有地板，附近 8 格内也没找到可站的格子。"
                        + "先用 scan {format:'regions'} 看 standable / verdict，或 tile {x,y} 确认那里是什么。",
                        new { target = new { x, y }, bodyFree = BridgeSystem.PathBodyFree(x, y), floorAt = BridgeSystem.PathFloorAt(x, y) });
                return Json.Error(
                    $"walkto 的目标 ({x},{y}) 不能站立；最近能站的是 ({bx},{by})，距 {best} 格。"
                    + $"改用 walkto {{x:{bx},y:{by}}}；如果确实要去那个格子（例如要掉进去），传 force:true。",
                    new { target = new { x, y }, nearest = new { x = bx, y = by, dist = best } });
            }
            int tol = Json.Int(p, "tol", 1);
            // 默认 15 秒对"穿两间房 + 上屋顶 + 绕路"这种 90 多步的路线远远不够：
            // 实测 94 步走到第 60 步就撞死线，回包只说 timeout，看着像"卡住"。
            // 真正的兜底靠停滞检测（WalkStuck），死线只是防跑飞，所以放到 3 分钟。
            int ms = Json.Int(p, "ms", 180000);
            // 默认不挖：A* 一旦认为“地板可挖开”，规划出的路就会穿自建房的墙和地板，
            // 而真到跟前又不许挖（建材受保护），结果是卡在墙根报 stuck（实测）。
            // 要开隧道就显式传 dig:true。
            BridgeSystem.PathAllowDig = Json.Bool(p, "dig", false);
            BridgeSystem.PathGreedy = false;   // 每次 walkto 从规划模式开始，算不出才退贪心
            BridgeSystem.PathProtectBuilds = true;
            BridgeSystem.BeginWalk(x, y, tol, BridgeSystem.Now + BridgeSystem.Ticks(ms));
            return Json.Ok(new { target = new { x, y }, tol, ms });
        }

        /// <summary>
        /// 爬绳：通过原生 Up / Down 输入 trigger 交给游戏处理绳索移动。
        /// </summary>
        private static string Climb(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            BridgeSystem.StopAutomatedMovement("manual-climb");
            string dir = Json.Str(p, "dir", "up");
            int ms = Json.Int(p, "ms", 1200);
            double tiles = Json.Float(p, "tiles", 0f);
            // 爬到指定格数才停：只给固定 ms，时间一到就松手往下掉（实测升 3.4 格后自由落体）。
            // 实测爬升 4.4 格/秒（1.35 是我瞎猜的，导致要 6 格却冲了 19.5 格）。
            if (tiles > 0) ms = (int)Math.Max(ms, Math.Ceiling(tiles / 4.4 * 1000));
            int startTileY = (int)(pl.Center.Y / 16f);
            BridgeSystem.ClimbTargetY = tiles > 0
                ? startTileY + (dir == "down" ? 1 : -1) * (int)Math.Round(tiles)
                : 0;
            ulong until = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            // 只沿**已有绳子**爬：实测没有绳子时硬给竖直速度不生效（原版同 tick 把速度压回 0，
            // 只弹 0.5 格）；有绳子时同样的速度能一口气升 19.5 格。所以"边铺边爬"是假的，删掉。
            if (dir == "down") { BridgeSystem.ClimbDownUntil = until; BridgeSystem.ClimbUpUntil = 0; }
            else { BridgeSystem.ClimbUpUntil = until; BridgeSystem.ClimbDownUntil = 0; }
            bool holdsRope = pl.HeldItem.type == ItemID.Rope || pl.HeldItem.type == ItemID.RopeCoil
                             || pl.HeldItem.type == ItemID.SilkRope || pl.HeldItem.type == ItemID.SilkRopeCoil
                             || pl.HeldItem.type == ItemID.VineRope || pl.HeldItem.type == ItemID.VineRopeCoil
                             || pl.HeldItem.type == ItemID.WebRope || pl.HeldItem.type == ItemID.WebRopeCoil;
            int tx = (int)(pl.Center.X / 16f), ty = (int)(pl.Center.Y / 16f);
            bool onRope = BridgeSystem.OnRopeNow(pl);
            if (!onRope)
            {
                BridgeSystem.ClimbUpUntil = 0; BridgeSystem.ClimbDownUntil = 0; BridgeSystem.ClimbTargetY = 0;
            }
            return Json.Ok(new
            {
                dir, ms, tiles, onRope, holdsRope, ropeCount = pl.ropeCount, held = pl.HeldItem.type,
                from = new { x = tx, y = ty },
                note = onRope
                    ? "沿已有绳子爬（先有绳才爬得动）"
                    : "这里没有绳子，爬不了：先在下/上方把绳子铺出来，或改用垫脚方块",
            });
        }


        private static string Accessory(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            // 默认必须只读列清单。原来这里是 Json.Bool(p,"off",true) —— 默认 true，
            // 于是任何不带 off:false 的调用都会立刻卸下一件饰品（实测两次把冰冻护盾/烈火手套卸进背包）。
            bool hasOff = p.ValueKind == JsonValueKind.Object && p.TryGetProperty("off", out _);
            bool off = Json.Bool(p, "off", false);

            if (!hasOff && Json.Int(p, "from", -1) < 0)
            {
                var listed = new List<object>();
                for (int a = 3; a <= 9; a++)
                {
                    Item it0 = pl.armor[a];
                    if (it0 != null && it0.type > 0) listed.Add(new { slot = a, name = it0.Name });
                }
                return Json.Ok(new { action = "list", equipped = listed, note = listed.Count == 0
                    ? "饰品栏是空的"
                    : "只读清单。卸下要显式 off:true（可加 slot:3..9 指定格）；穿上用 from:<背包槽>" });
            }

            if (off)
            {
                int want = Json.Int(p, "slot", -1);
                for (int a = 3; a <= 9; a++)
                {
                    Item it = pl.armor[a];
                    if (it == null || it.type <= 0) continue;
                    if (want >= 0 && a != want) continue;
                    for (int i = 0; i < MainBagSlots; i++)
                    {
                        if (pl.inventory[i] != null && pl.inventory[i].type > 0) continue;
                        pl.inventory[i] = it.Clone();
                        pl.armor[a] = new Item();
                        pl.armor[a].SetDefaults(0);
                        return Json.Ok(new { action = "unequip", from = a, to = i, item = it.Name });
                    }
                }
                var equipped = new List<object>();
                for (int a = 3; a <= 9; a++)
                {
                    Item it = pl.armor[a];
                    if (it != null && it.type > 0) equipped.Add(new { slot = a, name = it.Name });
                }
                return Json.Ok(new { action = "unequip", changed = 0, equipped, note = equipped.Count == 0
                    ? "饰品栏是空的，没有可卸的"
                    : "有饰品但背包已满，先 drop/chest 腾出格子" });
            }

            int from = Json.Int(p, "from", -1);
            if (from < 0) return Json.Error("穿上饰品需要 from（背包槽位）");
            Item src = pl.inventory[from];
            if (src == null || src.type <= 0) return Json.Error($"槽位 {from} 是空的");
            for (int a = 3; a <= 9; a++)
            {
                if (pl.armor[a] != null && pl.armor[a].type > 0) continue;
                pl.armor[a] = src.Clone();
                pl.inventory[from] = new Item();
                pl.inventory[from].SetDefaults(0);
                return Json.Ok(new { action = "equip", from, slot = a, item = src.Name });
            }
            return Json.Error("饰品栏满了");
        }

        private static string MirrorHome(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡，请先复活");

            int slot = -1;
            for (int i = 0; i < MainBagSlots; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.type <= 0) continue;
                if (it.type == ItemID.MagicMirror || it.type == ItemID.IceMirror
                    || it.type == ItemID.CellPhone || it.type == ItemID.Shellphone
                    || it.type == ItemID.ShellphoneDummy || it.type == ItemID.ShellphoneSpawn)
                { slot = i; break; }
            }
            if (slot < 0) return Json.Error("背包里没有魔镜（Magic Mirror / Ice Mirror / Cell Phone）");

            float fromX = pl.Center.X / 16f, fromY = pl.Center.Y / 16f;
            pl.selectedItem = slot;
            Item held = pl.inventory[slot];
            if (held != null && held.useAnimation > 0)
            {
                pl.SetItemAnimation(Math.Max(1, held.useAnimation));
                pl.SetItemTime(Math.Max(1, held.useTime));
            }
            pl.Spawn(PlayerSpawnContext.RecallFromItem);
            return Json.Ok(new
            {
                mode = "player",
                slot,
                item = held?.Name,
                from = new { x = fromX, y = fromY },
                to = new { x = pl.Center.X / 16f, y = pl.Center.Y / 16f },
                spawn = new { tileX = Main.spawnTileX, tileY = Main.spawnTileY },
            });
        }

        private static string Mine(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("mine 需要 x,y（方块坐标）");
            if (!InBounds(x, y)) return Json.Error($"out of world bounds: {x},{y}");
            // 没有 admin 直写分支了：世界只能通过真实挥镐改变（AGENTS.md 第 1 条）。
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            // guard:true 时先看一眼进度表：这一步是不是当前阶段该干的事。默认 false，不影响任何现有调用。
            if (Json.Bool(p, "guard", false))
            {
                string guarded = IntentGuard(p, "mine", TileCode(Main.tile[x, y]));
                if (guarded != null) return Json.Error(guarded);
            }
            // 默认 1300ms：丛林草/冰雪/石头在铁镐下要 1.2~1.3 秒，默认 900 会让"看起来挖了其实没掉"
            // 反复踩过三次坑（100ms、800ms 都白挖），所以把默认值调到够用，下限提到 300。
            // 默认“挖到空为止”：8 秒只是兜底上限，实际由桥每 tick 回读目标格，一挖空立刻收手。
            // 不同方块硬度差很多（土 1 下、石头 3 下、更硬的更多），固定时长必然有的不够、有的白按。
            // 挖之前先把目标格的状态记下来。为什么要带 before：
            // 方块在数据层没有"挖到第几刀"这个状态（Main.tileShatter 在本版本不存在，
            // TileFrameX 是图集坐标、实测挖 1000ms 后仍是 36），玩家看到的裂纹纯属客户端渲染。
            // 所以唯一能判真伪的是「回读 hasTile 有没有从 true 变 false」——把挖前状态
            // 一起回给调用方，它就不用再猜 baseline 是什么。
            Tile before = Main.tile[x, y];
            bool beforeHas = before.HasTile;
            int beforeType = TileCode(before);
            int beforeFrameX = before.TileFrameX;

            string started = BeginPlayerMine(x, y, Math.Clamp(Json.Int(p, "ms", 8000), 300, 20000));
            if (started == null) return Json.Error("empty player-mode response");
            if (!started.StartsWith("{")) return Json.Error(started);

            // 复核内层回包：ok=false 说明根本没挥动（够不到/没工具），必须原样当错误抛回去，
            // 不能因为"拿到了 data"就当成功——这是「命令生效 ≠ 操作发生」最容易复发的地方。
            using (JsonDocument inner = JsonDocument.Parse(started))
            {
                if (!inner.RootElement.TryGetProperty("ok", out JsonElement okEl) || !okEl.GetBoolean())
                    return started;
                if (!inner.RootElement.TryGetProperty("data", out JsonElement d))
                    return started;
                return Json.Ok(new
                {
                    mode = "player",
                    x,
                    y,
                    before = new { hasTile = beforeHas, type = beforeType, frameX = beforeFrameX },
                    armed = true,
                    slot = d.TryGetProperty("slot", out JsonElement sl) ? sl.GetInt32() : -1,
                    untilTick = d.TryGetProperty("untilTick", out JsonElement ut) ? (ulong)ut.GetInt64() : 0UL,
                    onScreen = d.TryGetProperty("onScreen", out JsonElement os) && os.GetBoolean(),
                    verify = "这是「已经开始挥」不是「已经挖掉」：之后回读 tile，hasTile=false 才算掉了。默认 ms 8000 只是兜底上限，桥每 tick 回读目标格，一挖空立刻收手。",
                });
            }
        }

        /// <summary>
        /// 放置方块/家具。**不做任何直接写世界的事**：选中真实物品、把准星压到目标格、
        /// 按住使用，剩下的交给原版 ItemCheck（它自己判定支撑、算家具锚点、扣物品、刷 frame）。
        ///
        /// 因为原版放置是异步的（命令返回时方块还没出现），这里返回 armed=true 而不是
        /// placed=true；调用方必须回读 tile 确认，背包里对应物品数量减少才是真的放下了。
        /// </summary>
        private static string Place(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            int type = Json.Int(p, "type", -1);
            int ms = Json.Int(p, "ms", 3000);
            if (x < 0 || y < 0) return Json.Error("place 需要 x,y");
            if (type < 0) return Json.Error("place 需要 type（方块类型 ID）");
            if (!ValidTileType(type)) return Json.Error($"invalid tile type {type} (0..{TileID.Count - 1}; 0 is Dirt)");
            if (!InBounds(x, y)) return Json.Error($"out of world bounds: {x},{y}");
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡，请先复活");
            if (!BridgeSystem.InReach(pl, x, y)) return Json.Error("够不到：离这一格超过 7 格，先走过去");
            if (Json.Bool(p, "guard", false))
            {
                string guarded = IntentGuard(p, "place", type);
                if (guarded != null) return Json.Error(guarded);
            }

            int slot = FindItemForTile(pl, type);
            if (slot < 0) return Json.Error($"背包里没有能放置 {TileID.Search.GetName(type)} 的物品");

            int before = TileCode(Main.tile[x, y]);
            int stackBefore = pl.inventory[slot].stack;
            BridgeSystem.BeginItemUse(slot, x, y, ms);
            return Json.Ok(new
            {
                armed = true,
                placed = false,          // 还没有；必须回读才算
                x,
                y,
                requested = type,
                name = TileID.Search.GetName(type),
                slot,
                stackBefore,
                before,
                ms,
                next = "回读 tile：type 变成 " + type + " 且背包该物品少 1 个，才算真的放下了",
            });
        }

        /// <summary>
        /// 多格家具（熔炉/铁砧/箱子/桌子/门…）的真实尺寸与锚点。
        /// 之前只能靠猜：熔炉是 3×2、工作台是 2×1，锚点在哪一格没有任何地方能查，
        /// 结果 WorldGen.PlaceTile 一直返回 false，家具一个也放不下来。
        /// </summary>
        private static string TileDataCmd(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int type = Json.Int(p, "type", -1);
            int style = Json.Int(p, "style", 0);
            int alternate = Json.Int(p, "alternate", 0);
            if (type < 0) return Json.Error("tiledata 需要 type（方块类型 ID）");
            if (!ValidTileType(type)) return Json.Error($"invalid tile type {type} (0..{TileID.Count - 1})");
            TileObjectData data = TileObjectData.GetTileData(type, style, alternate);
            string nm = TileID.Search.GetName(type);
            if (data == null)
                return Json.Ok(new { type, name = nm, hasObjectData = false, frameImportant = Main.tileFrameImportant[type] });
            return Json.Ok(new
            {
                type,
                name = nm,
                hasObjectData = true,
                frameImportant = Main.tileFrameImportant[type],
                width = data.Width,
                height = data.Height,
                originX = data.Origin.X,
                originY = data.Origin.Y,
                styleHorizontal = data.StyleHorizontal,
                styleWrapLimit = data.StyleWrapLimit,
            });
        }

        /// <summary>
        /// Background walls. Housing (and therefore every NPC) depends on walls, and no other
        /// command can place them, so "build a house" was impossible before this.
        /// </summary>
        private static string Wall(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("wall 需要 x,y（方块坐标）");
            if (!InBounds(x, y)) return Json.Error($"out of world bounds: {x},{y}");
            int type = Json.Int(p, "type", WallID.Dirt);
            bool remove = Json.Bool(p, "remove", false);
            // 没有 admin 直写分支了：墙壁也只能通过真实使用墙物品/挥锤改变。
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡，请先复活");
            if (!BridgeSystem.InReach(pl, x, y)) return Json.Error("够不到：离这一格超过 7 格，先走过去");
            int slot;
            if (remove)
            {
                // Removing a wall in vanilla needs a hammer.
                slot = -1;
                for (int i = 0; i < MainBagSlots; i++)
                    if (pl.inventory[i] != null && pl.inventory[i].hammer > 0) { slot = i; break; }
                if (slot < 0) return Json.Error("背包里没有锤子，拆墙需要锤子");
            }
            else
            {
                if (type <= 0 || type >= WallID.Count) return Json.Error($"invalid wall type {type} (1..{WallID.Count - 1})");
                if (Main.tile[x, y].HasTile) return Json.Error("这一格有方块挡着，先 mine 掉再放墙");
                slot = FindItemForWall(pl, type);
                if (slot < 0) return Json.Error($"背包里没有能放置该墙的物品（wall id {type}）");
            }
            int ms = Math.Clamp(Json.Int(p, "ms", 320), 100, 2000);
            // Walls are placed (and hammered away) with the LEFT button in vanilla.
            return StartSwing(slot, x, y, ms, false, false, true);
        }


        /* ── 动作 ─────────────────────────────────────────────────────── */

        /// <summary>精确横向移动：走到指定格坐标即停（误差 0.06 格），不靠时长猜。</summary>
        private static string MoveTo(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            BridgeSystem.StopAutomatedMovement("moveto");
            BridgeSystem.GuardUntil = 0; BridgeSystem.GuardDefault = false; BridgeSystem.GuardEngaged = false;
            float x = Json.Float(p, "x", float.NaN);
            if (float.IsNaN(x)) return Json.Error("moveto 需要 x（目标格坐标，可以是小数）");
            int ms = Math.Clamp(Json.Int(p, "ms", 4000), 200, 15000);
            BridgeSystem.LeftUntil = 0; BridgeSystem.RightUntil = 0;
            BridgeSystem.ExactActive = false;
            if (BridgeSystem.QueueRopeReleaseExactMove(x, ms))
            {
                float fromRope = P.position.X / 16f;
                return Json.Ok(new
                {
                    from = MathF.Round(fromRope, 2), target = x,
                    delta = MathF.Round(x - fromRope, 2), ms,
                    deferred = true, ropeRelease = true,
                    note = "脚底在绳上：直接执行（原版在绳子上的横向/竖直输入本就有效，不再按 Down 脱绳）",
                });
            }
            BridgeSystem.ExactActive = true;
            BridgeSystem.ExactTargetX = x;
            BridgeSystem.ExactUntil = (int)(BridgeSystem.Now + BridgeSystem.Ticks(ms));
            float from = P.position.X / 16f;
            BridgeSystem.ExactPhase = 0;
            BridgeSystem.ExactStuckTicks = 0;
            BridgeSystem.ExactLastX = from;
            BridgeSystem.ExactBrakeDistance = 0f;
            BridgeSystem.ExactRemaining = Math.Abs(x - from);
            BridgeSystem.ExactStopReason = "";
            return Json.Ok(new { from = MathF.Round(from, 2), target = x, delta = MathF.Round(x - from, 2), ms });
        }

        /// <summary>
        /// 只读：沿 dir 预走 steps 格，看会不会撞墙/掉坑。判据和 probe 完全一致，
        /// 只是搬进 move 里当闸门——规则只写在提示词里，AI 长对话里必忘；
        /// 写进命令里，它想忘也忘不掉（§0.57 的坑就是"发出去才发现人已经掉下去了"）。
        /// 返回 false 时 issue 是 blocked / pit_N_deep / pit_deep，
        /// reach = 安全走到的列数，drop = 那个断崖的落差格数。
        /// </summary>
        private static bool TerrainAhead(Player pl, string dir, int steps, out int reach, out string issue, out int drop)
        {
            reach = 0; issue = "none"; drop = 0;
            int px = (int)(pl.Center.X / 16f), py = (int)(pl.Center.Y / 16f);
            int dx = dir == "right" ? 1 : -1;
            for (int i = 1; i <= steps; i++)
            {
                int nx = px + dx * i, ny = py;
                if (!BridgeSystem.PathBodyFree(nx, ny)) { issue = "blocked"; return false; }
                if (!BridgeSystem.PathFloorAt(nx, ny))
                {
                    bool found = false;
                    int d = 0;
                    for (int down = 1; down <= 14; down++)
                    {
                        if (BridgeSystem.PathBodyFree(nx, ny + down) && BridgeSystem.PathFloorAt(nx, ny + down))
                        {
                            d = down; found = true; break;
                        }
                    }
                    if (!found) { drop = 14; issue = "pit_deep"; return false; }
                    // AGENTS §0.57：落差 >2 格且朝下＝悬崖，不许走过去；1~2 格人自己会走下去。
                    if (d > 2) { drop = d; issue = "pit_" + d + "_deep"; return false; }
                    ny += d;
                }
                reach = i;
            }
            return true;
        }

        private static string Move(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            // 手动方向必须抢回控制权：walkto 卡住时 Travel.Active 还是 true，
            // 它每帧覆盖方向键，手动 left/right 会被完全吃掉（实测连发 14 次一动不动）。
            BridgeSystem.StopAutomatedMovement("manual-move");
            // guard 每帧会自己转向/切武器/按开火，会把手动操作吃掉；手动指令一律先解除它。
            BridgeSystem.GuardUntil = 0; BridgeSystem.GuardDefault = false; BridgeSystem.GuardEngaged = false;
            string dir = (Json.Str(p, "dir", "right") ?? "right").ToLowerInvariant();
            int ms = Math.Clamp(Json.Int(p, "ms", 500), 1, 5000);
            if (dir != "left" && dir != "right" && dir != "up" && dir != "down")
                return Json.Error($"dir 只能是 left/right/up/down，收到 {dir}");
            float tiles = Json.Float(p, "tiles", 0f);
            ulong until = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            var from = new[] { MathF.Round(P.Center.X / 16f, 1), MathF.Round(P.Center.Y / 16f, 1) };
            bool upInputOnly = dir == "up"
                && !BridgeSystem.OnRopeNow(P)
                && P.wings <= 0
                && (P.mount == null || !P.mount.Active);
            if ((dir == "left" || dir == "right") && BridgeSystem.QueueRopeReleaseMove(dir, ms, tiles))
            {
                return Json.Ok(new
                {
                    dir, ms, tiles, from, untilTick = BridgeSystem.RopeReleaseUntil,
                    tick = BridgeSystem.Now, deferred = true, ropeRelease = true,
                    note = "脚底在绳上：直接执行（原版在绳子上的横向/竖直输入本就有效，不再按 Down 脱绳）",
                });
            }
            // 地形闸门：ms 长到会走出好几格时，先预走一遍再决定给不给走。
            // 病根是"摔下去回包也 ok:true"——AI 看到 ok 就以为走成了，实际人已经掉到坑底。
            // terrain 默认开；force:true 或 terrain:false 可以照走（人自己判断的时候用）。
            bool forceMove = Json.Bool(p, "force", false) || !Json.Bool(p, "terrain", true);
            string terrainNote = null;
            if (!forceMove && (dir == "left" || dir == "right") && (ms >= 300 || tiles > 0))
            {
                int lookAhead = tiles > 0
                    ? Math.Clamp((int)Math.Ceiling(tiles) + 1, 3, 24)
                    : Math.Clamp(ms / 200, 3, 12);
                if (!TerrainAhead(P, dir, lookAhead, out int reach, out string tIssue, out int tDrop))
                {
                    if (tIssue.StartsWith("pit_"))
                    {
                        return Json.Error(
                            $"前方第 {reach + 1} 列是 {tDrop} 格深的落差，这一步（{dir} {ms}ms）会一路走进去摔下去。" +
                            "可选：jump 跳过去 ／ 换方向绕 ／ place 搭桥；确认要走就传 force:true。");
                    }
                    terrainNote = $"前方第 {reach + 1} 列被挡（{tIssue}），走到那里会停住";
                }
            }
            switch (dir)
            {
                // A new direction replaces its opposite. Without this,
                // sequential left/right calls can overlap and cancel out.
                case "left": BridgeSystem.LeftUntil = until; BridgeSystem.RightUntil = 0; break;
                case "right": BridgeSystem.RightUntil = until; BridgeSystem.LeftUntil = 0; break;
                case "up": BridgeSystem.UpUntil = until; BridgeSystem.DownUntil = 0; break;
                case "down": BridgeSystem.DownUntil = until; BridgeSystem.UpUntil = 0; break;
                default: return Json.Error($"dir 只能是 left/right/up/down，收到 {dir}");
            // 结果驱动：给 tiles 就按"走够几格"停，不再靠时长猜。
            // 被墙挡住时固定时长会立刻失效——人还在原地，键已经松开。
            var c0 = new[] { MathF.Round(P.Center.X / 16f, 1), MathF.Round(P.Center.Y / 16f, 1) };
            if (tiles > 0)
            {
                BridgeSystem.MoveFromX = c0[0];
                BridgeSystem.MoveFromY = c0[1];
                BridgeSystem.MoveStopTiles = tiles;
                BridgeSystem.MoveStopAxis = (dir == "up" || dir == "down") ? 1 : 0;
                // 结果驱动时把上限放宽：走够就停，走不动就靠这个上限兜底。
                BridgeSystem.LeftUntil = 0; BridgeSystem.RightUntil = 0;
                BridgeSystem.UpUntil = 0; BridgeSystem.DownUntil = 0;
                switch (dir)
                {
                    case "left": BridgeSystem.LeftUntil = ulong.MaxValue; break;
                    case "right": BridgeSystem.RightUntil = ulong.MaxValue; break;
                    case "up": BridgeSystem.UpUntil = ulong.MaxValue; break;
                    case "down": BridgeSystem.DownUntil = ulong.MaxValue; break;
                }
            }
            }
            // Apply once immediately so a short command is not lost between
            // the request tick and the next Player update.
            BridgeSystem.ApplyMovement();
            return Json.Ok(new
            {
                dir, ms, from, untilTick = until, tick = BridgeSystem.Now,
                inputOnly = upInputOnly,
                terrainHint = terrainNote,
                note = upInputOnly ? "上键已按下；当前无绳、无翅膀、未骑乘，原版不会产生垂直位移" : null,
            });
        }

        private static string Jump(JsonElement p)
        {
            BridgeSystem.StopAutomatedMovement("manual-jump");   // 同上：手动动件必须抢回控制权
            BridgeSystem.GuardUntil = 0; BridgeSystem.GuardDefault = false;
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            // 默认按满跳（400ms → 6.1 格）。默认 150ms 只到 3.8 格，
            // 于是"跳不上去"的判断全是假的——不是跳不过，是根本没跳满。
            // 要精确高度传 tiles（实测曲线反查），要矮跳显式传更小的 ms。
            int ms = Math.Clamp(Json.Int(p, "ms", 400), 1, 2000);
            // 真人实测：按住跳跃键顶到 6.1 格，短按只有 1.4~2.7 格——高度是靠"按住多久"走出来的。
            // 所以默认改成**原生跳跃**：只保持 controlJump，让引擎自己起跳并做变高，
            // 不再替它设竖直速度（旧做法恒 5.2 格，且有天花板时与真人差得更远）。
            // Jump is always a real held-key input.  Keep accepting the old
            // native:false/height spelling as a compatibility alias, but never use it to
            // synthesize velocity: height is converted to a real held-key duration below.
            // 下面四个参数是直接 TryGetProperty 读的，不经过 Json.Int/Str，要手动登记，
            // 否则会被 ignoredParams 误报成"没人读"。
            Json.Seen("native", "tiles", "height", "dir");
            bool legacyNativeFalse = p.ValueKind == JsonValueKind.Object && p.TryGetProperty("native", out var nv) && nv.ValueKind == JsonValueKind.False;
            // 便利参数：直接说"要跳几格"，按住时长由实测表反查（真人同样按这两列对应）。
            double? wantTiles = null;
            if (p.TryGetProperty("tiles", out var tv) && tv.ValueKind == JsonValueKind.Number)
            {
                wantTiles = tv.GetDouble();
                ms = MsForTiles(wantTiles.Value);
                // tiles is only a convenience conversion to a held-key duration.
            }
            else if (legacyNativeFalse && p.TryGetProperty("height", out var hv) && hv.ValueKind == JsonValueKind.Number)
            {
                wantTiles = hv.GetDouble();
                ms = MsForTiles(wantTiles.Value);
            }
            string? dir = p.TryGetProperty("dir", out var dv) && dv.ValueKind == JsonValueKind.String ? dv.GetString() : null;
            int moveMs = Json.Int(p, "moveMs", ms);
            // 多段跳：count = 总共按几次。云朵瓶/暴雪瓶/泰拉靴这类 extraJump 每段都要新的按下边沿，
            // 一次调用里由 BridgeSystem 自己在段间松两帧来制造边沿。
            // 实测（Bot 客户端，云朵瓶 + 泰拉闪耀靴）：1 段 6.09 格，3 段 14.98 格。
            int count = Math.Clamp(Json.Int(p, "count", 1), 1, 6);
            BridgeSystem.JumpExtraLeft = count - 1;
            BridgeSystem.JumpExtraMs = ms;
            // 起跳后最早允许接下一段的帧数：只要 8 帧（约 130ms）。
            // 之前写 (ms+200)/16 = 31 帧（约 0.5 秒）太晚了，第三段容易错过窗口，
            // 实测三段只升到 14.15 格（第二段接上、第三段漏掉）。
            BridgeSystem.JumpNextAt = Main.GameUpdateCount + 8;
            // 整条连跳的总时限：从起跳算 100 帧（约 1.6 秒）内没接上就不再追，落地后不会乱跳。
            BridgeSystem.JumpCascadeUntil = Main.GameUpdateCount + 100;
            if (BridgeSystem.QueueRopeReleaseJump(ms, dir ?? "", moveMs))
            {
                return Json.Ok(new
                {
                    ms, native = true, compatibilityNativeFalse = legacyNativeFalse,
                    tiles = wantTiles,
                    from = new[] { MathF.Round(pl.Center.X / 16f, 1), MathF.Round(pl.Center.Y / 16f, 1) },
                    grounded = pl.velocity.Y == 0f && pl.oldVelocity.Y == 0f,
                    untilTick = BridgeSystem.RopeReleaseUntil,
                    tick = BridgeSystem.Now, deferred = true, ropeRelease = true,
                    note = "脚底在绳上：直接按跳（原版会先解除 pulley 再起跳），不再按 Down 脱绳",
                });
            }
            BridgeSystem.BeginJump(BridgeSystem.Now + BridgeSystem.Ticks(ms));
            // 真人跳远是起跳的同时按住方向：两个 control 位在同一帧一起置上。
            // 之前只能先 jump 再 left/right，跳跃 400ms 内发出的横移会和落地后的输入抢，
            // 结果永远是原地垂直跳。这里把横移并进同一条命令，保证空中每帧都有横向输入。
            if (dir == "left") BridgeSystem.LeftUntil = BridgeSystem.Now + BridgeSystem.Ticks(moveMs);
            else if (dir == "right") BridgeSystem.RightUntil = BridgeSystem.Now + BridgeSystem.Ticks(moveMs);
            return Json.Ok(new
            {
                ms,
                native = true,
                compatibilityNativeFalse = legacyNativeFalse,
                tiles = wantTiles,
                from = new[] { MathF.Round(pl.Center.X / 16f, 1), MathF.Round(pl.Center.Y / 16f, 1) },
                grounded = pl.velocity.Y == 0f && pl.oldVelocity.Y == 0f,
                untilTick = BridgeSystem.JumpUntil,
                tick = BridgeSystem.Now,
            });
        }

        /// <summary>
        /// 按住跳跃键时长 → 跳跃顶点高度（格）。数值全部来自真机逐帧实测（同一块地面反复跳）：
        /// 20ms→1.8、40→2.1、60→2.4、100→3.0、150→3.8、200→4.7、250→5.6、400→6.1（封顶）。
        /// 原版无增益配件时按住更久不会超过 6.1 格，所以上界直接压在 400ms。
        /// </summary>
        private static readonly (int ms, double tiles)[] JumpCurve =
        {
            (20, 1.8), (40, 2.1), (60, 2.4), (100, 3.0), (150, 3.8), (200, 4.7), (250, 5.6), (400, 6.1),
        };

        private static int MsForTiles(double tiles)
        {
            if (tiles <= JumpCurve[0].tiles) return JumpCurve[0].ms;
            if (tiles >= JumpCurve[JumpCurve.Length - 1].tiles) return JumpCurve[JumpCurve.Length - 1].ms;
            for (int i = 1; i < JumpCurve.Length; i++)
            {
                var (m1, t1) = JumpCurve[i];
                var (m0, t0) = JumpCurve[i - 1];
                if (tiles <= t1)
                {
                    double f = (tiles - t0) / (t1 - t0);
                    // 加一 tick 余量：实测 2 格档插值给 33ms 只跳出 1.8 格，差 0.2 格就会撞，
                    // 宁可多按住 16ms 顶到目标高度。
                    return Math.Min(JumpCurve[JumpCurve.Length - 1].ms, (int)Math.Round(m0 + f * (m1 - m0)) + 16);
                }
            }
            return JumpCurve[JumpCurve.Length - 1].ms;
        }

        private static string Use(JsonElement p)
        {
            BridgeSystem.StopWalk("manual-use");
            BridgeSystem.GuardUntil = 0; BridgeSystem.GuardDefault = false;
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡，请先复活");
            int slot = Json.Int(p, "slot", -1);
            if (slot >= 0 && slot < MainBagSlots) pl.selectedItem = slot;
            bool right = Json.Bool(p, "right", false);
            int ms = Math.Clamp(Json.Int(p, "ms", 220), 1, 5000);
            // Optional aim: point the in-game cursor at a tile first, so a swing/shoot has
            // a direction instead of "wherever the mouse happened to be".
            int aimX = Json.Int(p, "x", int.MinValue), aimY = Json.Int(p, "y", int.MinValue);
            if (aimX != int.MinValue && aimY != int.MinValue)
            {
                BridgeSystem.SetAim(aimX, aimY);
                BridgeSystem.AimUntil = BridgeSystem.Now + BridgeSystem.Ticks(ms) + 20;
                BridgeSystem.ApplyAim();
            }
            ulong until = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            if (right)
            {
                BridgeSystem.UseTileUntil = until;
                pl.controlUseTile = true;
            }
            else
            {
                BridgeSystem.UseUntil = until;
                // A control flag alone can be swallowed by the normal input
                // reset on the same tick. Start the animation immediately so
                // a short bridge request visibly performs one use.
                pl.controlUseItem = true;
                Item held = pl.HeldItem;
                // 只有"挥/射"类物品才需要替引擎起动画：镐斧锤剑弓与投掷物都是靠动画驱动的。
                // 消耗品（生命水晶/药水/召唤物）恰好相反——引擎 ItemCheck 要求 itemAnimation == 0
                // 才走"吃掉并生效"那条分支。之前无差别强行播动画，导致 `use` 对消耗品永远只是空挥：
                // 回 ok、水晶还在背包、上限一点没涨。对照证据：同一套 use 能把手里剑从 60 打到 58。
                bool swings = held != null && held.type > 0
                    && (held.damage > 0 || held.pick > 0 || held.axe > 0 || held.hammer > 0
                        || held.shoot > 0 || held.ammo > 0 || held.useStyle == 1
                        // 钩爪 useStyle == 7：漏了它，抓钩的 anim 恒 0、永远射不出去。
                        || held.useStyle == 7);
                if (swings && held.useAnimation > 0)
                {
                    pl.direction = Main.MouseWorld.X >= pl.Center.X ? 1 : -1;
                    pl.SetItemAnimation(Math.Max(1, held.useAnimation));
                    pl.SetItemTime(Math.Max(1, held.useTime));
                }
            }
            return Json.Ok(new
            {
                slot = pl.selectedItem,
                right,
                ms,
                held = new { type = pl.HeldItem.type, name = pl.HeldItem.Name },
                // 命令回 ok 不等于事情发生了：把"动手之前"的血量/上限/持有数一并带回去，
                // 脚本拿它跟回读值比，才知道这次 use 到底吃掉了没有（消耗品最典型）。
                before = new { life = pl.statLife, lifeMax = pl.statLifeMax2, stack = pl.HeldItem.stack },
                // Evidence that something actually started, instead of just "ok".
                anim = pl.itemAnimation,
            });
        }

        /// <summary>
        /// 右键交互：开门、开箱子、和 NPC 对话，都走引擎自己的“玩家交互”那条链。
        /// 这里不写 WorldGen.OpenDoor，也不改任何格子——只是把玩家的 tileTarget 指到目标，
        /// 然后调用原版右键时调用的那个方法（Player.TileInteractionsUse），让它自己判断
        /// 够不够得着、是不是门、是不是箱子，并且自己播放开门动画。
        /// </summary>
        private static string Interact(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡，请先复活");
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("interact 需要 x,y（要交互的那一格的格子坐标）");
            var pc = pl.Center / 16f;
            if (Math.Abs(x + 0.5f - pc.X) > 8 || Math.Abs(y + 0.5f - pc.Y) > 8)
                return Json.Error($"够不到：({x},{y}) 离玩家超过 8 格，先走过去");
            int before = TileCode(Main.tile[x, y]);
            // Real right-click input.  Vanilla Player.TileInteractionsUse is reached by
            // ItemCheck after the bridge supplies the aim and right-button hold; do not call
            // the interaction method directly because that bypasses the input path.
            BridgeSystem.BeginTileInteraction(x, y, 180);
            return Json.Ok(new
            {
                interact = true,
                x,
                y,
                before,
                untilTick = BridgeSystem.UseTileUntil,
                method = "right-click input",
                note = "请在 untilTick 后用 tile/state 回读确认门、箱子或 NPC 交互结果",
            });
        }

        // ==== 通行与飞行：挂钩 / 飞行 / 坐骑 / 召唤 / 远程 ====
        //
        // 这些能力都必须走原版物品使用路径：选槽位、压准星、按住使用键，
        // 由游戏自己发射弹幕、召出小怪、驱动翅膀/坐骑。这里只读状态回来做证据。

        /// <summary>选中一个背包槽位（快捷栏 0~9），回读手持物。</summary>
        private static string SelectSlot(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int slot = Json.Int(p, "slot", -1);
            int item = Json.Int(p, "item", -1);
            if (slot < 0 && item > 0)
            {
                for (int i = 0; i < MainBagSlots; i++)
                    if (pl.inventory[i] != null && pl.inventory[i].type == item && pl.inventory[i].stack > 0) { slot = i; break; }
            }
            if (slot < 0 || slot >= pl.inventory.Length) return Json.Error("没有这个槽位，也没找到该物品");
            pl.selectedItem = slot;
            Item held = pl.inventory[slot];
            return Json.Ok(new
            {
                slot = pl.selectedItem,
                held = held == null ? null : new { type = held.type, name = held.Name, stack = held.stack },
                damage = held?.damage ?? 0,
                shoot = held?.shoot ?? 0,
                useStyle = held?.useStyle ?? 0,
            });
        }

        /// <summary>玩家自己发射出去的弹幕数（作为"真的射出东西"的证据）。</summary>
        private static int OwnProjectiles(Player pl)
        {
            int n = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile q = Main.projectile[i];
                if (q != null && q.active && q.owner == pl.whoAmI) n++;
            }
            return n;
        }

        /// <summary>
        /// 远程攻击：选中弓/枪/法杖，把准星压到目标格，按住使用键让它自己发射。
        /// 不直接扣血——箭/子弹是不是真飞出去，用弹幕数量回读证明。
        /// </summary>
        private static string Shoot(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡");
            int x = Json.Int(p, "x", int.MinValue), y = Json.Int(p, "y", int.MinValue);
            if (x == int.MinValue || y == int.MinValue) return Json.Error("shoot 需要 x,y（瞄准的方块）");
            int slot = Json.Int(p, "slot", -1);
            if (slot < 0) slot = pl.selectedItem;
            // 默认 600ms：弓的 useTime 是 28 tick（≈467ms），握太短（400ms）
            // 一次调用只能放出一箭，而且紧接着的下一次调用还在挥动中、不会再起手——
            // 实测对着史莱姆连射 8 次只放出 1 箭、怪一滴血没掉。
            int ms = Math.Clamp(Json.Int(p, "ms", 600), 60, 6000);
            Item held = pl.inventory[slot];
            if (held == null || held.type <= 0) return Json.Error("这个槽位是空的");
            int before = OwnProjectiles(pl);
            int ammoBefore = 0;
            for (int i = 0; i < MainBagSlots; i++)
                if (pl.inventory[i] != null && pl.inventory[i].ammo > 0) ammoBefore += pl.inventory[i].stack;
            BridgeSystem.BeginItemUse(slot, x, y, ms);
            return Json.Ok(new
            {
                x, y, slot, ms,
                held = new { type = held.type, name = held.Name, shoot = held.shoot, useTime = held.useTime },
                projectilesBefore = before,
                ammoBefore,
            });
        }

        /// <summary>
        /// 抓钩类物品：原版抓钩没有伤害、不耗魔力、不用弹药，但会发射一个弹幕。
        /// 用这三条判定，比维护一张 id 表可靠（后期抓钩 id 有一大堆）。
        /// </summary>
        /// <summary>正挂着的抓钩弹幕类型（没有就 -1）。原版抓钩弹幕的 aiStyle 固定是 7。</summary>
        private static int HookProjectile(Player pl)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr.active && pr.owner == pl.whoAmI && pr.aiStyle == 7) return pr.type;
            }
            return -1;
        }

        /// <summary>
        /// 自动防守：在 r 格内自主找敌对目标，换武器、瞄准、按住使用键，直到 ms 到点。
        /// 伤害全部来自原版挥击/弹幕（命令本身不扣血、不动世界）。
        /// </summary>
        private static string GuardCmd(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            if (Json.Bool(p, "off", false) || string.Equals(Json.Str(p, "action", ""), "off", StringComparison.OrdinalIgnoreCase))
            {
                BridgeSystem.GuardDefault = false;
                BridgeSystem.GuardAuto = false;
                BridgeSystem.GuardUntil = 0;
                BridgeSystem.GuardEngaged = false;
                return Json.Ok(new { off = true, note = "自动防守已关闭（guard 不带 off 可再打开）" });
            }
            BridgeSystem.GuardDefault = true;
            // auto:true = 到点自己续期，一直续到 off。默认关，保持“不自动打怪”的老行为；
            // 打开它才解决“命令早返回、AI 之后在想事，怪贴脸却没人还手”。
            BridgeSystem.GuardAuto = Json.Bool(p, "auto", false);
            int ms = Math.Clamp(Json.Int(p, "ms", 60000), 500, 1800000);
            int r = Math.Clamp(Json.Int(p, "r", 14), 4, 60);
            BridgeSystem.GuardUntil = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            BridgeSystem.GuardRadius = (ulong)r;
            BridgeSystem.GuardNextScan = 0;
            BridgeSystem.GuardEngaged = false;
            int hostile = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.CanBeChasedBy()
                    && Vector2.Distance(pl.Center, n.Center) <= r * 16f) hostile++;
            }
            // 现在就挑一次武器，让回包能说清"用什么武器防的"
            int slot = pl.selectedItem;
            return Json.Ok(new
            {
                untilTick = BridgeSystem.GuardUntil,
                radius = r,
                ms,
                auto = BridgeSystem.GuardAuto,
                hostileInRange = hostile,
                weaponSlot = slot,
                weapon = new { type = pl.inventory[slot].type, name = pl.inventory[slot].Name },
                minions = pl.numMinions,
                note = "角色会自己转向、自己换武器、自己按住开火；伤害由原版弹幕/挥击产生",
            });
        }

        // escape：掉坑/卡死时启动桥内自救。
        // 命令只负责"开局"和"查结果"，真正动作在 BridgeSystem.ApplyEscape 里每 tick 做。
        // 如果命令自己循环到成功才返回，等它回来时地形早变了，中间那几秒也没人管 ——
        // 掉坑里一小时出不来，根因就是这个。
        private static string EscapeCmd(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int nx = (int)(pl.Center.X / 16f), ny = (int)(pl.Center.Y / 16f);

            if (Json.Bool(p, "report", false)
                || string.Equals(Json.Str(p, "action", ""), "report", StringComparison.OrdinalIgnoreCase))
            {
                return Json.Ok(new
                {
                    status = BridgeSystem.EscapeStatus,
                    active = BridgeSystem.EscapeActive,
                    reason = BridgeSystem.EscapeReason,
                    fromY = BridgeSystem.EscapeStartY,
                    nowY = ny,
                    gainY = BridgeSystem.EscapeStartY - ny,
                    reach = BridgeSystem.EscapeProbeReach(nx, ny, 8),
                    startReach = BridgeSystem.EscapeStartReach,
                    stalls = BridgeSystem.EscapeStalls,
                    jumps = BridgeSystem.EscapeJumps,
                    steps = BridgeSystem.EscapeSteps,
                    note = "status: running=还在自救 / escaped=出来了 / stuck=判定出不来（reason 里有左右上下各几格）",
                });
            }

            // 开阔地上调 escape 会白跑满 maxMs 再报 stuck（真机第一次测就是这样，见 reason
            // "左 8 格、右 8 格、头顶 8 格空"）。它本来就是给"困住"用的，先判一次要不要救：
            // 半径 8 能到 120 格以上就不是坑，直接说清楚，别让人等 8 秒还收到一个 stuck。
            int openReach = BridgeSystem.EscapeProbeReach(nx, ny, 8);
            if (openReach >= 120)
            {
                return Json.Error(
                    $"你现在不在坑里：半径 8 格内能走到 {openReach} 格，够开阔。escape 是给掉坑/卡死用的，"
                    + "要走路用 move，要上台阶用 jump，要看地形用 scan {format:'regions'}。",
                    new { reach = openReach, threshold = 120 });
            }

            if (BridgeSystem.EscapeActive)
            {
                return Json.Ok(new
                {
                    already = true,
                    untilTick = BridgeSystem.EscapeUntil,
                    note = "已经在自救中；用 escape {report:true} 看进度",
                });
            }

            if (pl.dead) return Json.Error("角色已经死了，先复活再自救");
            int ms = Math.Clamp(Json.Int(p, "maxMs", 8000), 1000, 30000);
            int reach = BridgeSystem.EscapeProbeReach(nx, ny, 8);
            BridgeSystem.EscapeStartY = ny;
            BridgeSystem.EscapeStartReach = reach;
            BridgeSystem.EscapeBestReach = reach;
            BridgeSystem.EscapeStalls = 0;
            BridgeSystem.EscapeHits = 0;
            BridgeSystem.EscapeJumps = 0;
            BridgeSystem.EscapeSteps = 0;
            BridgeSystem.EscapeNextEval = 0;
            BridgeSystem.EscapeStatus = "running";
            BridgeSystem.EscapeReason = "";
            BridgeSystem.EscapeUntil = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            BridgeSystem.EscapeActive = true;
            return Json.Ok(new
            {
                armed = true,
                from = new[] { nx, ny },
                reach,
                maxMs = ms,
                untilTick = BridgeSystem.EscapeUntil,
                status = "running",
                note = "每 0.4 秒自评一次：站稳、比起点高 2 格、可达范围多 10 格；"
                    + "连续 60 次没进展就判 stuck 并说清左/右/上/下各有多少格。"
                    + "用 escape {report:true} 取结果。",
            });
        }

        // 钩爪槽：Player.miscEquips 的真实顺序是 [0]宠物 [1]照明宠物 [2]矿车
        // [3]坐骑 [4]抓钩。抓钩是**装备**，不是"手持物品"——放背包里、把它选中
        // 都没用，controlHook 照样不发射（实测：选中背包里的抓钩按下去，
        // grapCount 连续 3 秒停在 0）。
        // 这个 4 曾经被误判成错误、改成了 3，实测立刻打脸：miscEquips[3] 里装的是
        // 宇宙车钥匙（mountType 7），[4] 才是月钩（hookType 646）。当时 hookdebug
        // 报"宇宙车钥匙"，真正的成因是部署的 dll 还是旧的 3，不是这个常量错。
        // 教训：报错先核对"跑的是不是刚编译的那份"，再改常量。
        private const int HookEquipSlot = 4;
        /// <summary>坐骑在 miscEquips 里的格子：pet/lightPet/minecart/mount/hook → 3。</summary>
        private const int MountEquipSlot = 3;

        // Player.miscEquips[2] 也是一个 mountType 槽，但它专门保存矿车。
        // 不能只看 mountType > 0（或 != -1）就把它当成普通坐骑；原版用
        // MountID.Sets.Cart 区分这两类物品。
        private static bool IsCartMount(Item it)
        {
            if (it == null) return false;
            int mt = it.mountType;
            return mt >= 0 && mt < MountID.Sets.Cart.Length && MountID.Sets.Cart[mt];
        }

        private static bool IsRegularMount(Item it)
        {
            return it != null && it.type > 0 && it.stack > 0
                && it.mountType != -1 && !IsCartMount(it);
        }

        private static void EnsureHookEquipped(Player pl, out string how)
        {
            Item cur = pl.miscEquips != null && pl.miscEquips.Length > HookEquipSlot ? pl.miscEquips[HookEquipSlot] : null;
            if (cur != null && cur.type > 0 && IsHookItem(cur)) { how = "already"; return; }
            for (int i = 0; i < MainBagSlots; i++)
            {
                Item it = pl.inventory[i];
                if (!IsHookItem(it)) continue;
                Item old = pl.miscEquips[HookEquipSlot];
                pl.miscEquips[HookEquipSlot] = it.Clone();
                pl.inventory[i] = old != null && old.type > 0 ? old.Clone() : new Item();
                how = "equipped:" + pl.miscEquips[HookEquipSlot].Name;
                return;
            }
            how = "none";
        }

        private static bool IsHookItem(Item it)
        {
            if (it == null || it.type <= 0 || it.stack <= 0) return false;
            // 原版自己也是这么判的：弹幕属于 Main.projHook 就是钩爪（Item 上没有 hook 字段）。
            if (it.shoot > 0 && it.shoot < Main.projHook.Length && Main.projHook[it.shoot]) return true;
            return it.damage == 0 && it.shoot > 0 && it.ammo == 0 && it.mana == 0 && it.useStyle == 0;
        }

        /// <summary>
        /// 抓钩：选中抓钩类物品，往目标格甩出去，读 grapCount/graphook 证明真的挂住了。
        /// 拉动由原版物理完成（这里不写坐标）。
        /// </summary>
        /// <summary>诊断抓钩发射链：看 controlHook / releaseHook / 输入层到底是谁没到位。</summary>
        private static string HookDebug(JsonElement pe)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            Item hook = pl.miscEquips != null && pl.miscEquips.Length > HookEquipSlot ? pl.miscEquips[HookEquipSlot] : null;
            return Json.Ok(new
            {
                controlHook = pl.controlHook,
                releaseHook = pl.releaseHook,
                mouseLeft = Main.mouseLeft,
                mouseLeftRelease = Main.mouseLeftRelease,
                mouseInterface = pl.mouseInterface,
                grapCount = pl.grapCount,
                grappling0 = pl.grappling != null && pl.grappling.Length > 0 ? pl.grappling[0] : -1,
                pulley = pl.pulley,
                dead = pl.dead,
                selectedSlot = pl.selectedItem,
                heldName = pl.HeldItem != null ? pl.HeldItem.Name : "",
                hookEquip = hook != null ? hook.Name : "",
                hookShoot = hook != null ? hook.shoot : -1,
                isProjHook = hook != null && hook.shoot > 0 && hook.shoot < Main.projHook.Length && Main.projHook[hook.shoot],
                grapplingAll = pl.grappling == null ? null : pl.grappling.Take(6).ToArray(),
                misc4Shoot = pl.miscEquips != null && pl.miscEquips.Length > 4 && pl.miscEquips[4] != null ? pl.miscEquips[4].shoot : -1,
                projHook646 = Main.projectile == null ? -1 : Enumerable.Range(0, Main.projectile.Length).Count(i => Main.projectile[i] != null && Main.projectile[i].active && Main.projectile[i].owner == Main.myPlayer && (Main.projectile[i].type == 646 || Main.projectile[i].type == 647 || Main.projectile[i].type == 648 || Main.projectile[i].type == 649)),
                mouseWorldTile = new { x = (int)(Main.MouseWorld.X / 16f), y = (int)(Main.MouseWorld.Y / 16f) },
                mouse = new { x = Main.mouseX, y = Main.mouseY },
                screen = new { x = Main.screenPosition.X, y = Main.screenPosition.Y, w = Main.screenWidth, h = Main.screenHeight },
                zoom = Main.GameViewMatrix != null ? Main.GameViewMatrix.Zoom.X : 1f,
                hookAimWorld = new { x = BridgeSystem.HookAimWorld.X, y = BridgeSystem.HookAimWorld.Y },
                hookAimInput = new
                {
                    rawX = BridgeSystem.DbgHookRawMouseX,
                    rawY = BridgeSystem.DbgHookRawMouseY,
                    finalX = BridgeSystem.DbgHookFinalMouseX,
                    finalY = BridgeSystem.DbgHookFinalMouseY,
                },
                post = new { controlHook = BridgeSystem.DbgPostControlHook, releaseHook = BridgeSystem.DbgPostReleaseHook, mouseLeft = BridgeSystem.DbgPostMouseLeft, projHook = BridgeSystem.DbgProjHookCount },
            });
        }


        private static string Hook(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡");
            BridgeSystem.StopAutomatedMovement("manual-hook");
            int x = Json.Int(p, "x", int.MinValue), y = Json.Int(p, "y", int.MinValue);
            if (x == int.MinValue || y == int.MinValue) return Json.Error("hook 需要 x,y（要挂住的方块）");
            EnsureHookEquipped(pl, out string equipHow);
            Item hooked = pl.miscEquips[HookEquipSlot];
            if (equipHow == "none" || hooked == null || hooked.type <= 0) return Json.Error("背包里没有抓钩");
            int distance = (int)Vector2.Distance(pl.Center / 16f, new Vector2(x, y));
            if (distance > 40) return Json.Error("抓钩最远约 40 格，目标在 " + distance + " 格外");
            int before = pl.grapCount;
            int ms = Json.Int(p, "ms", 1600);
            Vector2 targetPx = new Vector2(x * 16f + 8f, y * 16f + 8f);
            // A grapple is a real left-button input. Vanilla selects the equipped hook,
            // spawns its projectile, and resolves attachment; the bridge only aims and holds.
            BridgeSystem.SetAim(x, y);
            BridgeSystem.HookAimWorld = targetPx;
            BridgeSystem.HookHeld = true;
            BridgeSystem.HookJustStarted = true;
            BridgeSystem.HookUntil = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            return Json.Ok(new
            {
                x, y, distance, ms, equipHow,
                grapCountBefore = before,
                proj = -1,
                note = "已发送真实抓钩输入；请在 untilTick 后回读 state.grapCount / hookdebug",
                equipped = new { type = hooked.type, name = hooked.Name },
            });
        }

        /// <summary>松开抓钩：让角色做一个跳跃动作（原版里跳跃就会脱钩）。</summary>
        private static string Unhook(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int before = pl.grapCount;
            // 真人脱钩就是松手 + 跳一下：松手靠清掉 controlHook，跳一下帮角色脱离附着点。
            BridgeSystem.HookHeld = false;
            BridgeSystem.HookUntil = 0;
            BridgeSystem.HookReleasePending = true;
            // 同上：跳一下也要 IntentJump，否则没有物理按键时 controlJump 恒 false。
            BridgeSystem.BeginJump(BridgeSystem.Now + BridgeSystem.Ticks(160));
            return Json.Ok(new { grapCountBefore = before });
        }

        /// <summary>
        /// 飞行：按住跳跃键 ms 毫秒。有翅膀时原版会给升力；有飞行坐骑时同理。
        /// 没有翅膀/坐骑时它会诚实地落回地面——这就是"能不能飞"的判据。
        /// </summary>
        private static string Fly(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡");
            int ms = Math.Clamp(Json.Int(p, "ms", 900), 60, 8000);
            bool wings = pl.wingTimeMax > 0 || pl.equippedWings != null;
            bool mounted = pl.mount != null && pl.mount.Active;
            // 两种飞行压根不是同一个键，必须分流：
            //
            //   有翅膀 / 普通飞行坐骑 → controlJump（Player.cs:25787 的 CanFly() 分支）
            //   悬停类坐骑（usesHover） → controlUp  （Player.cs:25585 的 CanHover() 分支）
            //
            // Player.cs:25783 是 if (mount.CanHover()) { mount.Hover(this); }
            //                      else if (mount.CanFly() && controlJump && ...)
            // 所以骑悬停坐骑时只写 controlJump 一个字都不会被读到 —— 这正是 fly 在
            // "骑着坐骑"情况下 velY 恒 0 的原因：不是没翅膀，是键按错了。
            bool hovering = mounted && pl.mount.CanHover();
            // 手动飞也要先抢回控制权：exact/path/walk/follow 只要有一个还活着，
            // ApplyMovement 就会在那个分支里提前 return（BridgeSystem.cs:2005/2071/2072/2089），
            // JumpUntil 那一段永远走不到，表现就是"翅膀不开、velY 恒 0"。
            // 实测：fly 连试 5 次一格不动，而同一位置带 dir 的 jump 能升 16 格 ——
            // 差别只在 Jump 命令开头有这句 StopAutomatedMovement。
            BridgeSystem.StopAutomatedMovement("manual-fly");
            // 必须走 BeginJump：它除了写 JumpUntil 还会置 IntentJump=true。
            // BridgeSystem.cs:616 的 controlJump 只在 IntentJump 或真实按键 triggers.Jump 为真时才成立，
            // 这套环境没有物理按键，只写 JumpUntil 会让 controlJump 恒 false（翅膀永远不开，实测 velY 恒 0）。
            if (hovering)
            {
                // 悬停坐骑：按住上键。UpUntil 本身就是时间窗，ApplyNativeControlFlags 会把它
                // 并进 controlUp（BridgeSystem.cs:720），不需要额外的 Intent 标志。
                BridgeSystem.UpUntil = BridgeSystem.Now + BridgeSystem.Ticks(ms);
                BridgeSystem.DownUntil = 0;
            }
            else
            {
                BridgeSystem.BeginJump(BridgeSystem.Now + BridgeSystem.Ticks(ms));
            }
            return Json.Ok(new
            {
                ms,
                key = hovering ? "up" : "jump",
                hovering,
                hasWings = wings,
                wingTimeMax = pl.wingTimeMax,
                wingTime = pl.wingTime,
                mounted,
                velY = pl.velocity.Y,
                hint = hovering || wings || mounted ? null : "没有翅膀也没有飞行坐骑，按住跳跃只会起跳，不会飞",
            });
        }

        /// <summary>
        /// 坐骑：ride 用背包里的坐骑物品（原版 use 路径），dismount 调原版下马。
        /// </summary>
        private static string MountCmd(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡");
            string action = Json.Str(p, "action", "info");
            bool activeBefore = pl.mount != null && pl.mount.Active;
            int typeBefore = activeBefore ? pl.mount.Type : -1;
            if (action == "dismount")
            {
                if (!activeBefore)
                    return Json.Ok(new { action, activeBefore, typeBefore, activeAfter = false, queued = false });

                // 下坐骑也只发送原版快捷键；QuickMount() 会根据当前 mounted 状态
                // 自己执行 Dismount。不要直接调用 mount.Dismount，这会绕过输入层。
                //
                // 前置：原版 releaseMount 只在 controlMount == false 的那几帧被置真，
                // 而那几帧挂在 if (Main.hasFocus) 里；失焦环境下它永远是默认值 false，
                // QuickMount() 就不会进。所以先预开 6 帧闸但不按键，把前置条件备好。
                const ulong preOpen = 6;
                ulong now0 = BridgeSystem.NowTick();
                BridgeSystem.MountPreOpenUntil = now0 + preOpen;
                ulong until = now0 + preOpen + (ulong)Math.Max(2, Json.Int(p, "ticks", 12));
                BridgeSystem.MountUntil = until;
                BridgeSystem.MountFired = false;
                return Json.Ok(new
                {
                    action,
                    activeBefore,
                    typeBefore,
                    queued = true,
                    untilTick = until,
                    preOpenTicks = preOpen,
                    note = "已发送原版坐骑键下坐（前 6 帧预开闸让 releaseMount 置真）；等 untilTick 后回读 state.player.mounted / mountType",
                });
            }
            // 快捷召唤：走原版坐骑键（controlMount），不是"掏出钥匙按左键"。
            // 装备格是 miscEquips[3]（mount），与抓钩的 miscEquips[4] 相邻。
            if (action == "quick")
            {
                int mslot = MountEquipSlot;
                Item mit = mslot >= 0 && mslot < pl.miscEquips.Length ? pl.miscEquips[mslot] : null;
                bool hasMountItem = IsRegularMount(mit);
                if (!hasMountItem)
                    return Json.Error("坐骑装备格（miscEquips[" + mslot + "]）里没有坐骑召唤物品");
                // 与 dismount 同理：先预开 6 帧闸但不按键，让引擎执行
                // else { releaseMount = true; }，否则骑乘中再按也只会重设 false。
                const ulong preOpen = 6;
                ulong now0 = BridgeSystem.NowTick();
                BridgeSystem.MountPreOpenUntil = now0 + preOpen;
                ulong until = now0 + preOpen + (ulong)Math.Max(2, Json.Int(p, "ticks", 12));
                BridgeSystem.MountUntil = until;
                BridgeSystem.MountFired = false;
                return Json.Ok(new
                {
                    action,
                    mountSlot = mslot,
                    mountItem = new { type = mit.type, name = mit.Name, mountType = mit.mountType },
                    untilTick = until,
                    preOpenTicks = preOpen,
                    activeBefore,
                    typeBefore,
                    note = "已发送原版坐骑键（controlMount，前 6 帧预开闸）；等 untilTick 后回读 state.player.mounted / mountType",
                });
            }
            if (action == "ride")
            {
                int slot = Json.Int(p, "slot", -1);
                if (slot < 0)
                {
                    for (int i = 0; i < MainBagSlots; i++)
                    {
                        Item it = pl.inventory[i];
                        if (IsRegularMount(it)) { slot = i; break; }
                    }
                }
                if (slot < 0) return Json.Error("背包里没有坐骑召唤物品");
                BridgeSystem.BeginItemUse(slot, (int)(pl.Center.X / 16f), (int)(pl.Center.Y / 16f), 240);
                return Json.Ok(new { action, slot, activeBefore, typeBefore, held = new { type = pl.inventory[slot].type, name = pl.inventory[slot].Name } });
            }
            return Json.Ok(new
            {
                action = "info",
                active = activeBefore,
                type = typeBefore,
                flight = activeBefore && pl.mount.Flight(),
                items = Array.FindAll(pl.inventory, it => it != null && it.type > 0 && it.stack > 0 && it.mountType != -1)
                    .Select(it => new { it.type, it.Name, it.mountType, kind = IsCartMount(it) ? "cart" : "mount" }),
            });
        }

        /// <summary>
        /// 召唤：用召唤法杖召出随从，读 numMinions 证明真的召出来了。
        /// </summary>
        private static string Minion(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡");
            int slot = Json.Int(p, "slot", -1);
            if (slot < 0)
            {
                for (int i = 0; i < MainBagSlots; i++)
                {
                    Item it = pl.inventory[i];
                    if (it != null && it.type > 0 && it.stack > 0 && it.DamageType == DamageClass.Summon) { slot = i; break; }
                }
            }
            if (slot < 0) return Json.Error("背包里没有召唤物品");
            int before = pl.numMinions;
            BridgeSystem.BeginItemUse(slot, (int)(pl.Center.X / 16f) + 2, (int)(pl.Center.Y / 16f), 300);
            return Json.Ok(new
            {
                slot,
                before,
                slots = pl.slotsMinions,
                held = new { type = pl.inventory[slot].type, name = pl.inventory[slot].Name },
                hint = before >= pl.slotsMinions ? "随从位已满（再多也只能减员）" : null,
            });
        }

        // ==== 与人同玩：跟随 / 待命 / 玩家列表 ====

        /// <summary>
        /// 跟随另一个玩家。只发送原生方向输入——走路、碰撞、上台阶全由原版解算，
        /// 不做瞬移、不挖不该挖的方块。
        /// guard:true 时兼当护卫：优先打队友正在打的目标，血低于 30% 先停手。
        /// </summary>
        private static string FollowCmd(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player me = P;
            if (Json.Bool(p, "off", false))
            {
                BridgeSystem.ClearFollow();
                return Json.Ok(new { follow = false });
            }

            Player target = null;
            int who = Json.Int(p, "player", -1);
            if (who >= 0 && who < Main.maxPlayers)
            {
                Player q = Main.player[who];
                if (q != null && q.active && !q.dead && q.whoAmI != me.whoAmI) target = q;
            }
            if (target == null)
            {
                float bestD = float.MaxValue;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player q = Main.player[i];
                    if (q == null || !q.active || q.dead || q.whoAmI == me.whoAmI) continue;
                    float d = Vector2.DistanceSquared(q.Center, me.Center);
                    if (d < bestD) { bestD = d; target = q; }
                }
            }
            if (target == null)
                return Json.Error("世界里没有其他玩家：跟随只在多人模式有意义（单人请用 walkto）");

            // Reinitialise the follow controller when changing target/range so
            // a previous target's walkto waypoint cannot be reused accidentally.
            BridgeSystem.ClearFollow();
            BridgeSystem.FollowTargetIdx = target.whoAmI;
            BridgeSystem.FollowTargetName = target.name ?? "";
            BridgeSystem.FollowGuard = Json.Bool(p, "guard", false);
            BridgeSystem.FollowStopTiles = Math.Clamp(Json.Float(p, "range", 4.5f), 1f, 30f);
            BridgeSystem.FollowActive = true;
            BridgeSystem.StopPath("follow");
            BridgeSystem.StopWalk("follow");

            float dist = Vector2.Distance(me.Center, target.Center) / 16f;
            return Json.Ok(new
            {
                follow = true,
                target = target.name,
                targetIndex = target.whoAmI,
                guard = BridgeSystem.FollowGuard,
                range = MathF.Round(BridgeSystem.FollowStopTiles, 1),
                dist = MathF.Round(dist, 1),
                note = "跟随中：远了会走过去、够近就站住。用 idle 或 follow off 停下。",
            });
        }

        /// <summary>待命：清掉所有控制与任务，站着不动（受击仍会还手）。</summary>
        private static string IdleCmd(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            BridgeSystem.ClearFollow();
            BridgeSystem.StopPath("idle");
            BridgeSystem.StopWalk("idle");
            BridgeSystem.ClearControls();
            return Json.Ok(new { idle = true, note = "已停止跟随/移动/攻击，站在原地看着" });
        }

        /// <summary>列出世界里的所有玩家（多人时用来选 follow 目标）。</summary>
        private static string PlayersCmd()
        {
            if (!InWorld(out string err)) return Json.Error(err);
            var list = new List<object>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player q = Main.player[i];
                if (q == null || !q.active) continue;
                list.Add(new
                {
                    index = q.whoAmI,
                    name = q.name,
                    dead = q.dead,
                    hp = q.statLife,
                    hpMax = q.statLifeMax2,
                    tile = new[] { MathF.Round(q.Center.X / 16f, 1), MathF.Round(q.Center.Y / 16f, 1) },
                    team = q.team,
                    local = q.whoAmI == Main.myPlayer,
                    following = BridgeSystem.FollowActive && BridgeSystem.FollowTargetIdx == q.whoAmI,
                });
            }
            return Json.Ok(new { count = list.Count, players = list });
        }

        private static string Attack(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡，请先复活");
            int requestedWho = Json.Int(p, "who", -1);
            int radius = Math.Clamp(Json.Int(p, "r", 40), 1, 200);
            int duration = Math.Clamp(Json.Int(p, "ms", 3000), 100, 15000);
            int interval = Math.Clamp(Json.Int(p, "interval", 180), 30, 2000);
            int slot = Json.Int(p, "slot", -1);
            if (slot >= 0 && slot < MainBagSlots) pl.selectedItem = slot;

            Item weapon = pl.HeldItem;
            if (weapon == null || weapon.damage <= 0)
                return Json.Error("当前选中的物品不能造成伤害，请先选择武器（例如铜短剑）");

            bool autoTarget = requestedWho < 0;
            int targetWho = requestedWho;
            if (targetWho >= 0)
            {
                if (targetWho >= Main.npc.Length || Main.npc[targetWho] == null || !Main.npc[targetWho].active)
                    return Json.Error($"目标 NPC 不存在：{targetWho}");
                if (!Main.npc[targetWho].CanBeChasedBy())
                    return Json.Error($"目标 NPC 不是可攻击的敌怪：{targetWho}");
            }
            else
            {
                targetWho = BridgeSystem.FindNearestHostile(pl, radius);
                if (targetWho < 0)
                    return Json.Error($"半径 {radius} 格内没有可攻击的敌怪");
            }

            BridgeSystem.BeginAttack(targetWho, autoTarget, radius, duration, interval);
            // Hit once immediately. Subsequent hits are scheduled from the
            // player update loop, so the command never blocks the game thread.
            BridgeSystem.ApplyAttack();
            NPC target = targetWho >= 0 && targetWho < Main.npc.Length ? Main.npc[targetWho] : null;
            return Json.Ok(new
            {
                attacking = true,
                autoTarget,
                who = targetWho,
                duration,
                interval,
                slot = pl.selectedItem,
                weapon = new { type = weapon.type, name = weapon.Name, damage = weapon.damage },
                // The real damage this hit used, after armor/buff/accessory bonuses.
                effectiveDamage = BridgeSystem.LastHitDamage,
                npc = target == null ? null : new { name = target.FullName, life = target.active ? target.life : 0, lifeMax = target.lifeMax },
                hint = "攻击在后台继续。任务结束后 state.attack.last 仍保留命中数和目标剩余血量，可用来验收。",
            });
        }


        /* ── 背包 / 合成 / 生成 ───────────────────────────────────────── */

        private static string Inventory()
        {
            if (!InWorld(out string err)) return Json.Error(err);
            Player pl = P;
            var slots = new List<object>();
            for (int i = 0; i < MainBagSlots; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.type <= 0 || it.stack <= 0) continue;
                slots.Add(new
                {
                    slot = i,
                    hotbar = i < 10,
                    type = it.type,
                    name = it.Name,
                    stack = it.stack,
                    prefix = it.prefix,
                    rare = it.rare,
                });
            }
            // Terraria reserves 50-53 for coins, 54-57 for ammo and 58 for the trash
            // slot.  Keep the existing `items` field compatible (main bag only), while
            // exposing every non-main slot so an empty main bag is not mistaken for
            // “no arrows / no coins”.
            var special = new List<object>();
            for (int i = MainBagSlots; i < pl.inventory.Length; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.type <= 0 || it.stack <= 0) continue;
                special.Add(new
                {
                    slot = i,
                    kind = i < 54 ? "coin" : (i < 58 ? "ammo" : "trash"),
                    type = it.type,
                    name = it.Name,
                    stack = it.stack,
                    prefix = it.prefix,
                    rare = it.rare,
                });
            }
            // Armor and accessories live in a separate array: without this the caller cannot
            // tell whether it is armoured, or whether it can fly (wings are an accessory).
            var equip = new List<object>();
            for (int i = 0; i < pl.armor.Length; i++)
            {
                Item it = pl.armor[i];
                if (it == null || it.type <= 0) continue;
                equip.Add(new
                {
                    slot = i,
                    kind = i < 3 ? "armor" : "accessory",
                    type = it.type,
                    name = it.Name,
                    defense = it.defense,
                    wingSlot = it.wingSlot > 0,
                });
            }
            // miscEquips 是宠物/照明宠物/矿车/抓钩/坐骑这五格，跟背包和饰品栏都不是
            // 同一个数组。不输出它，调用方就永远看不到抓钩有没有装备（hook 这条链
            // 之所以长期查不下去，就是因为这里读不到，只能靠 hookdebug 那个错字段）。
            var misc = new List<object>();
            // 顺序必须与 Player.miscEquips 的真实布局一致：pet / lightPet / minecart / mount / hook。
            // 曾经写成 "hook","mount"，导致 [3] 坐骑被标成 hook、[4] 抓钩被标成 mount。
            string[] miscKinds = { "pet", "lightPet", "minecart", "mount", "hook" };
            for (int i = 0; i < pl.miscEquips.Length; i++)
            {
                Item it = pl.miscEquips[i];
                bool empty = it == null || it.type <= 0;
                misc.Add(new
                {
                    slot = i,
                    kind = i < miscKinds.Length ? miscKinds[i] : "other",
                    empty,
                    type = empty ? 0 : it.type,
                    name = empty ? "" : it.Name,
                    hookType = empty ? 0 : it.shoot,
                    mountType = empty ? 0 : it.mountType,
                });
            }
            // 空位数量：背包满时掉落物会静默丢失，调用方必须能提前看到。
            int freeMain = 0, freeAll = 0;
            for (int i = 0; i < pl.inventory.Length; i++)
            {
                Item it = pl.inventory[i];
                bool empty = it == null || it.type <= 0 || it.stack <= 0;
                if (empty) { freeAll++; if (i < 50) freeMain++; }
            }
            return Json.Ok(new
            {
                selected = pl.selectedItem,
                count = slots.Count,
                free = freeMain,
                freeMain = freeMain,
                freeAll = freeAll,
                items = slots.ToArray(),
                special = special.ToArray(),
                coinSlots = Enumerable.Range(50, Math.Max(0, Math.Min(4, pl.inventory.Length - 50))).ToArray(),
                ammoSlots = Enumerable.Range(54, Math.Max(0, Math.Min(4, pl.inventory.Length - 54))).ToArray(),
                trashSlot = pl.inventory.Length > 58 ? 58 : -1,
                equipment = equip.ToArray(),
                miscEquips = misc.ToArray(),
                canFly = pl.wingTimeMax > 0 || pl.velocity.Y != 0f && pl.wingsLogic > 0,
                // statDefense is a struct (Positive/Negative); callers want one number.
                defense = pl.statDefense.Positive - pl.statDefense.Negative,
            });
        }

        /* ── 进度检查表（intent）────────────────────────────────────────────
         * 症结不是"不会推理"，是推理结果不落盘：换个对话，"现在该干什么"就得从头推一遍，
         * 同一个坑重复踩。所以把「阶段 + 目标 + 判据」写成 goal-progress.json（仓库根），
         * intent 只读它 + 数背包，一帧算完。
         * 纯只读：不开状态机、不写文件、不改世界。阶段推进只发生在这一次调用的内存里，
         * 回包用 advanced 告诉调用方"按判据已经该是下一阶段了"，落盘由 AI 自己改 stage 字段。
         */

        private sealed class GoalItem
        {
            public int Id;
            public string Label;
            public int Need;
            public int[] AnyOf;
        }

        private sealed class GoalStage
        {
            public string Key;
            public string Label;
            public string Goal;
            public string Next;
            public string Why;
            public bool Strict;      // guard 只管标了 strict 的阶段；其余一律放行
            public bool World = true; // false = 这一阶段不该动土/建造
            public readonly List<GoalItem> Checks = new List<GoalItem>();
            public readonly List<string> Blocked = new List<string>();
        }

        /// <summary>按优先级列出进度文件可能的位置；显式 path 优先，其次环境变量，再按游戏/进程目录猜。</summary>
        private static IEnumerable<string> GoalFileCandidates(string explicitPath)
        {
            if (!string.IsNullOrWhiteSpace(explicitPath)) { yield return explicitPath; yield break; }
            string env = null;
            try { env = Environment.GetEnvironmentVariable("DSH_GOAL_FILE"); } catch { }
            if (!string.IsNullOrWhiteSpace(env)) yield return env;
            string docs = null;
            try
            {
                docs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                    "My Games", "Terraria", "tModLoader");
            }
            catch { }
            if (!string.IsNullOrEmpty(docs))
            {
                yield return Path.Combine(docs, "goal-progress.json");
                yield return Path.Combine(docs, "ModSources", "DshTerrariaBridge", "goal-progress.json");
            }
            string cwd = null;
            try { cwd = Directory.GetCurrentDirectory(); } catch { }
            if (!string.IsNullOrEmpty(cwd))
            {
                yield return Path.Combine(cwd, "goal-progress.json");
                yield return Path.Combine(cwd, "ModSources", "DshTerrariaBridge", "goal-progress.json");
            }
        }

        private static bool TryLoadGoal(JsonElement p, out JsonDocument doc, out string source,
                                        out List<string> tried, out string err)
        {
            doc = null; source = null; err = null;
            tried = new List<string>();
            string json = null;
            foreach (string cand in GoalFileCandidates(Json.Str(p, "path", null)))
            {
                try
                {
                    if (!File.Exists(cand)) { tried.Add(cand + "（不存在）"); continue; }
                    json = File.ReadAllText(cand);
                    source = cand;
                    break;
                }
                catch (Exception e) { tried.Add(cand + "（" + e.GetType().Name + "）"); }
            }
            if (json == null)
            {
                err = "读不到进度文件 goal-progress.json。试过：" + string.Join("；", tried)
                    + "。用 intent.path 传绝对路径，或设环境变量 DSH_GOAL_FILE。";
                return false;
            }
            try
            {
                doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                    !doc.RootElement.TryGetProperty("stages", out JsonElement stEl) ||
                    stEl.ValueKind != JsonValueKind.Object)
                {
                    doc.Dispose(); doc = null;
                    err = "进度文件格式不对：" + source + " 顶层缺少 stages 对象";
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                doc = null;
                err = "进度文件不是合法 JSON（" + source + "）：" + e.Message;
                return false;
            }
        }

        private static GoalStage ReadStage(JsonElement stages, string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!stages.TryGetProperty(key, out JsonElement s) || s.ValueKind != JsonValueKind.Object) return null;
            var st = new GoalStage { Key = key };
            if (s.TryGetProperty("label", out JsonElement l) && l.ValueKind == JsonValueKind.String) st.Label = l.GetString();
            if (s.TryGetProperty("goal", out JsonElement g) && g.ValueKind == JsonValueKind.String) st.Goal = g.GetString();
            if (s.TryGetProperty("next", out JsonElement n) && n.ValueKind == JsonValueKind.String) st.Next = n.GetString();
            if (s.TryGetProperty("why", out JsonElement w) && w.ValueKind == JsonValueKind.String) st.Why = w.GetString();
            if (s.TryGetProperty("guard", out JsonElement gd) && gd.ValueKind == JsonValueKind.Object)
            {
                st.Strict = gd.TryGetProperty("strict", out JsonElement sv) && sv.ValueKind == JsonValueKind.True;
                st.World = !(gd.TryGetProperty("world", out JsonElement wv) && wv.ValueKind == JsonValueKind.False);
            }
            if (string.IsNullOrEmpty(st.Label)) st.Label = key;
            if (string.IsNullOrEmpty(st.Goal)) st.Goal = "（goal-progress.json 里 " + key + " 没有写 goal）";
            if (s.TryGetProperty("checks", out JsonElement cs) && cs.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement c in cs.EnumerateArray())
                {
                    if (c.ValueKind != JsonValueKind.Object) continue;
                    var it = new GoalItem { Need = 1, Label = "" };
                    if (c.TryGetProperty("id", out JsonElement idv) && idv.ValueKind == JsonValueKind.Number) it.Id = idv.GetInt32();
                    if (c.TryGetProperty("label", out JsonElement lb) && lb.ValueKind == JsonValueKind.String) it.Label = lb.GetString();
                    if (c.TryGetProperty("need", out JsonElement nv) && nv.ValueKind == JsonValueKind.Number) it.Need = Math.Max(1, nv.GetInt32());
                    if (c.TryGetProperty("anyOf", out JsonElement any) && any.ValueKind == JsonValueKind.Array)
                        it.AnyOf = any.EnumerateArray()
                                      .Where(x => x.ValueKind == JsonValueKind.Number)
                                      .Select(x => x.GetInt32()).ToArray();
                    if (it.Id <= 0) continue;
                    if (string.IsNullOrEmpty(it.Label)) it.Label = "item " + it.Id;
                    st.Checks.Add(it);
                }
            }
            if (s.TryGetProperty("blocked", out JsonElement bs) && bs.ValueKind == JsonValueKind.Array)
                foreach (JsonElement b in bs.EnumerateArray())
                    if (b.ValueKind == JsonValueKind.String) st.Blocked.Add(b.GetString());
            return st;
        }

        /// <summary>背包里这件东西有几个；anyOf 表示"任意一个都算"，取最大值而不是求和。</summary>
        private static int HaveGoalItem(Player pl, GoalItem it)
        {
            int n = CountItem(pl, it.Id);
            if (it.AnyOf != null)
                foreach (int alt in it.AnyOf) n = Math.Max(n, CountItem(pl, alt));
            return n;
        }

        private static string IntentCommand(JsonElement p)
        {
            if (!InWorld(out string werr)) return Json.Error(werr);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!TryLoadGoal(p, out JsonDocument doc, out string source, out List<string> tried, out string loadErr))
                return Json.Error(loadErr);
            using (doc)
            {
                JsonElement root = doc.RootElement;
                JsonElement stages = root.GetProperty("stages");

                string stage = root.TryGetProperty("stage", out JsonElement sv) && sv.ValueKind == JsonValueKind.String
                    ? sv.GetString() : null;
                var order = new List<string>();
                if (root.TryGetProperty("stageOrder", out JsonElement ov) && ov.ValueKind == JsonValueKind.Array)
                    foreach (JsonElement o in ov.EnumerateArray())
                        if (o.ValueKind == JsonValueKind.String) order.Add(o.GetString());
                if (string.IsNullOrEmpty(stage) && order.Count > 0) stage = order[0];
                if (string.IsNullOrEmpty(stage))
                    return Json.Error("进度文件里 stage 与 stageOrder 都是空的，无法确定当前阶段：" + source);
                if (order.Count == 0) order.Add(stage);

                GoalStage st = ReadStage(stages, stage);
                if (st == null)
                    return Json.Error("进度文件里找不到阶段 \"" + stage + "\"（stageOrder: "
                                      + string.Join(",", order) + "）：" + source);

                Player pl = P;
                var advanced = new List<string>();
                // 判据全满足就往后走一格，只推进连续完成的阶段，绝不跳空。
                for (int step = 0; step <= order.Count; step++)
                {
                    bool allDone = true;
                    foreach (GoalItem it in st.Checks)
                        if (HaveGoalItem(pl, it) < it.Need) { allDone = false; break; }
                    if (!allDone) break;
                    int idx = order.IndexOf(st.Key);
                    if (idx < 0 || idx + 1 >= order.Count) break;
                    GoalStage nxt = ReadStage(stages, order[idx + 1]);
                    if (nxt == null) break;
                    advanced.Add(nxt.Key);
                    st = nxt;
                }

                var missing = new List<object>();
                var blockedBy = new List<string>();
                foreach (GoalItem it in st.Checks)
                {
                    int have = HaveGoalItem(pl, it);
                    if (have >= it.Need) continue;
                    missing.Add(new { id = it.Id, label = it.Label, have, need = it.Need });
                    blockedBy.Add("缺 " + it.Label + "（" + it.Id + "）：有 " + have + "，要 " + it.Need);
                }
                foreach (string b in st.Blocked) blockedBy.Add(b);

                bool done = missing.Count == 0;
                int pos = order.IndexOf(st.Key);
                bool last = pos < 0 || pos + 1 >= order.Count;
                string suggestedNext = !done
                    ? (string.IsNullOrEmpty(st.Next) ? "按 checks 补齐：" + string.Join("、", st.Checks.Select(c => c.Label)) : st.Next)
                    : (last ? "stageOrder 里最后一段也完成了：往 goal-progress.json 的 stages 里加新阶段，再更新 stage"
                            : "本阶段已完成；下次 intent 会自动推进到 " + order[pos + 1] + "，也可以现在手改 stage");
                string whyNotOther = !done
                    ? (string.IsNullOrEmpty(st.Why)
                        ? "当前阶段的完成判据还没满足，后面的阶段都依赖它——先把这一段补齐，别提前做下一阶段的事"
                        : st.Why)
                    : "当前阶段判据已全部满足，重复做这一段不会推进进度";

                return Json.Ok(new
                {
                    schema = root.TryGetProperty("schema", out JsonElement sc) && sc.ValueKind == JsonValueKind.Number
                        ? sc.GetInt32() : 1,
                    stage = st.Key,
                    stageLabel = st.Label,
                    goal = st.Goal,
                    done,
                    missing = missing.ToArray(),
                    blockedBy = blockedBy.ToArray(),
                    suggestedNext,
                    whyNotOther,
                    confidence = 0.95,
                    advanced = advanced.ToArray(),
                    stageOrder = order.ToArray(),
                    source,
                    hint = "只读：本命令不改文件。按 suggestedNext 做，判据满足后把 goal-progress.json 的 stage 改成下一段。",
                });
            }
        }

        /// <summary>
        /// 动手前先查 intent。只在调用方显式传 guard:true 时生效（默认 false，保护现有全部调用与测试）；
        /// 只有阶段自己标了 strict 才可能拦，而且只拦"明显不属于本阶段"的动作——这是提醒，不是锁。
        /// 读不到进度文件就放行（宁可放过，不因为没有进度表把正常干活挡住）。
        /// </summary>
        private static string IntentGuard(JsonElement p, string action, int targetId)
        {
            if (!TryLoadGoal(p, out JsonDocument doc, out string source, out _, out _)) return null;
            using (doc)
            {
                JsonElement root = doc.RootElement;
                if (!root.TryGetProperty("stage", out JsonElement sv) || sv.ValueKind != JsonValueKind.String) return null;
                GoalStage st = ReadStage(root.GetProperty("stages"), sv.GetString());
                if (st == null || !st.Strict) return null;

                if (action == "craft")
                {
                    foreach (GoalItem it in st.Checks)
                    {
                        if (it.Id == targetId) return null;
                        if (it.AnyOf != null && Array.IndexOf(it.AnyOf, targetId) >= 0) return null;
                    }
                    return "当前阶段是「" + st.Label + "」，目标是：" + st.Goal
                         + "。craft " + targetId + " 不在这一阶段要凑的东西里（判据："
                         + string.Join("、", st.Checks.Select(c => c.Label + " " + c.Id))
                         + "）。确认要做就传 guard:false。";
                }
                if (!st.World)
                    return "当前阶段是「" + st.Label + "」，目标是：" + st.Goal
                         + "。这一阶段不需要动土/建造，" + action + " 先放一放（要强行做传 guard:false）。";
                return null;
            }
        }

        private static int CountItem(Player pl, int type)
        {
            int n = 0;
            foreach (Item it in pl.inventory)
                if (it != null && it.type == type) n += it.stack;
            return n;
        }

        /// <summary>Accepted-member sets for the recipe's groups ("any wood", "any iron bar"…).</summary>
        private static List<HashSet<int>> RecipeGroupSets(Recipe recipe)
        {
            var sets = new List<HashSet<int>>();
            if (recipe?.acceptedGroups == null) return sets;
            foreach (int gid in recipe.acceptedGroups)
            {
                try
                {
                    if (RecipeGroup.recipeGroups != null && RecipeGroup.recipeGroups.TryGetValue(gid, out RecipeGroup group) && group?.ValidItems != null)
                        sets.Add(new HashSet<int>(group.ValidItems));
                }
                catch { }
            }
            return sets;
        }

        /// <summary>
        /// How many items of a requirement slot the player holds, counting group members
        /// when the requirement is a group ("any wood").
        /// </summary>
        private static int CountRequirement(Player pl, int type, List<HashSet<int>> groups)
        {
            int n = CountItem(pl, type);
            foreach (var set in groups)
            {
                if (!set.Contains(type)) continue;
                foreach (int member in set)
                {
                    if (member == type) continue;
                    n += CountItem(pl, member);
                }
            }
            return n;
        }

        /// <summary>Consume one requirement slot, taking exact items first, then group members.</summary>
        /// <summary>附近是否有配方要求的工作站方块。</summary>
        private static bool HasStation(Player pl, Recipe recipe)
        {
            System.Collections.Generic.List<int> tiles = recipe.requiredTile;
            if (tiles == null || tiles.Count == 0) return true;
            int cx = (int)(pl.Center.X / 16f), cy = (int)(pl.Center.Y / 16f);
            foreach (int need in tiles)
            {
                if (need <= 0) continue;
                bool found = false;
                for (int x = cx - 5; x <= cx + 5 && !found; x++)
                {
                    for (int y = cy - 5; y <= cy + 5 && !found; y++)
                    {
                        if (!InBounds(x, y)) continue;
                        Tile t = Main.tile[x, y];
                        if (t.HasTile && t.TileType == need) found = true;
                    }
                }
                if (!found) return false;
            }
            return true;
        }

        private static string Craft(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int itemType = Json.Int(p, "item", 0);
            if (itemType <= 0) return Json.Error("craft 需要 item（物品类型 id）");
            if (!ValidItemType(itemType)) return Json.Error($"invalid item type {itemType}: 不是有效的物品 ID");
            if (Json.Bool(p, "guard", false))
            {
                string guarded = IntentGuard(p, "craft", itemType);
                if (guarded != null) return Json.Error(guarded);
            }
            int times = Math.Clamp(Json.Int(p, "times", 1), 1, 20);

            Player pl = P;
            Recipe.FindRecipes();

            // One product can have several recipes (an iron bar can come from ore *or* from
            // fences). Taking the first match made crafting fail for no visible reason, so pick
            // the first recipe whose ingredients the player actually carries.
            bool HasAll(Recipe r)
            {
                foreach (Item ing in r.requiredItem)
                {
                    if (ing == null || ing.type <= 0 || ing.stack <= 0) continue;
                    int have = 0;
                    foreach (Item slot in pl.inventory)
                        if (slot != null && slot.type == ing.type) have += slot.stack;
                    if (have < ing.stack) return false;
                }
                return true;
            }

            Recipe recipe = null;
            Recipe fallback = null;
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe cand = Main.recipe[i];
                if (cand.createItem.type != itemType) continue;
                if (fallback == null) fallback = cand;
                if (HasAll(cand)) { recipe = cand; break; }
            }
            if (recipe == null) recipe = fallback;
            if (recipe == null) return Json.Error($"没有找到产物为 {itemType} 的配方");

            // Conditions such as "near water", "hardmode", "night" are part of the recipe:
            // crafting without them would let the caller make items the game never allows.
            try
            {
                if (recipe.Conditions != null)
                {
                    foreach (var condition in recipe.Conditions)
                    {
                        if (condition == null || condition.Predicate == null) continue;
                        if (!condition.Predicate())
                        {
                            string desc = condition.Description?.Value ?? "配方条件";
                            return Json.Error($"配方条件不满足：{desc}");
                        }
                    }
                }
            }
            catch { /* older/other tModLoader builds may not expose Conditions; stations still checked below */ }

            var groups = RecipeGroupSets(recipe);

            // Statistics: the same item can occupy several slots.
            var need = new Dictionary<int, int>();
            if (recipe.requiredItem != null)
            {
                foreach (Item ri in recipe.requiredItem)
                {
                    if (ri == null || ri.type <= 0 || ri.stack <= 0) continue;
                    if (!need.ContainsKey(ri.type)) need[ri.type] = 0;
                    need[ri.type] += ri.stack;
                }
            }

            // 回读验证用：合成前的背包快照
            int beforeCount = CountItem(pl, itemType);

            int crafted = 0;
            int dropped = 0;
            for (int round = 0; round < times; round++)
            {
                foreach (var kv in need)
                {
                    if (CountRequirement(pl, kv.Key, groups) < kv.Value)
                        return crafted > 0
                            ? Json.Ok(new { item = itemType, name = Lang.GetItemNameValue(itemType), crafted, dropped, times, before = beforeCount, after = CountItem(pl, itemType), gained = CountItem(pl, itemType) - beforeCount, reason = "材料用完了" })
                            : Json.Error($"材料不足：需要 {Lang.GetItemNameValue(kv.Key)} x{kv.Value}，背包里有 {CountRequirement(pl, kv.Key, groups)}");
                }
                if (!HasStation(pl, recipe))
                    return crafted > 0
                        ? Json.Ok(new { item = itemType, name = Lang.GetItemNameValue(itemType), crafted, dropped, times, before = beforeCount, after = CountItem(pl, itemType), gained = CountItem(pl, itemType) - beforeCount, reason = "附近没有工作站" })
                        : Json.Error("附近没有这个配方需要的工作站（工作台/熔炉/铁砧…）");

                // 走原版合成路径：recipe.Create() 消耗材料（处理配方组、mod 消耗钩子），
                // 不再手动 it.stack -= take（那是替引擎算结果，违反 AGENTS.md §0.55）。
                int per = Math.Max(1, recipe.createItem.stack);
                recipe.Create();

                // 产物走原版拾取管线 pl.GetItem，不再手动写 inventory。
                Item product = recipe.createItem.Clone();
                product.Prefix(-1);
                try { RecipeLoader.OnCraft(product, recipe, Main.mouseItem); } catch { }
                Item leftover = pl.GetItem(pl.whoAmI, product, GetItemSettings.PickupItemFromWorld);
                int remaining = leftover != null ? leftover.stack : 0;

                if (remaining > 0)
                {
                    // 背包满：产物掉在地上，和原版合成 UI 满背包时一样
                    pl.QuickSpawnItem(new EntitySource_Misc("dsh-bridge"), itemType, remaining);
                    dropped += remaining;
                }
                crafted += per - remaining;
            }

            // 回读验证：用背包前后差确认真的合出来了
            int afterCount = CountItem(pl, itemType);
            int gained = afterCount - beforeCount;

            return Json.Ok(new
            {
                item = itemType,
                name = Lang.GetItemNameValue(itemType),
                crafted,
                dropped,
                times,
                perCraft = Math.Max(1, recipe.createItem.stack),
                groupsUsed = groups.Count,
                before = beforeCount,
                after = afterCount,
                gained,
            });
        }


        private static string Npcs(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            // r 是老名字；radius 是直觉写法，两个都认（实测有人传 radius 却拿到默认 40 的坑）。
            int r = Math.Clamp(Json.Int(p, "r", Json.Int(p, "radius", 40)), 1, 200);
            Player pl = P;
            float centerX = pl != null && pl.active ? pl.Center.X : Main.spawnTileX * 16f;
            float centerY = pl != null && pl.active ? pl.Center.Y : Main.spawnTileY * 16f;
            var near = new List<object>();
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                float dx = n.Center.X - centerX, dy = n.Center.Y - centerY;
                if (Math.Abs(dx) > r * 16 || Math.Abs(dy) > r * 16) continue;
                near.Add(new
                {
                    who = i,
                    type = n.type,
                    name = n.FullName,
                    tile = new[] { MathF.Round(n.Center.X / 16f, 1), MathF.Round(n.Center.Y / 16f, 1) },
                    life = n.life,
                    lifeMax = n.lifeMax,
                    friendly = n.friendly,
                    boss = n.boss,
                    hostile = n.CanBeChasedBy(),
                    // 小动物（虫网能抓的）与它会变成的物品：抓虫/钓鱼/笼子要先认出它们。
                    critter = n.CountsAsACritter,
                    catchItem = n.catchItem,
                });
            }
            return Json.Ok(new { radius = r, count = near.Count, npcs = near.ToArray() });
        }


        /* ── 事件 / 地图 / 背包拆分 ─────────────────────────────────── */

        private static string LiquidName(int id)
        {
            if (id == LiquidID.Lava) return "lava";
            if (id == LiquidID.Honey) return "honey";
            if (id == LiquidID.Shimmer) return "shimmer";
            return "water";
        }

        /// <summary>
        /// 手动开事件。只给测试用例用（E01/E02/E05），走的是原版自己的入口：
        /// 血月/日食是置标志位（游戏自己也是这么开的），南瓜月/霜月/入侵是原版 Start*。
        /// 它不改世界，只是把"现在该发生什么"告诉游戏。
        /// </summary>
        private static string StartEvent(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            string ev = (Json.Str(p, "event", "") ?? "").Trim().ToLowerInvariant();
            string started;
            switch (ev)
            {
                case "bloodmoon":
                case "blood":
                    Main.dayTime = false;
                    Main.time = 0;
                    Main.bloodMoon = true;
                    started = "bloodMoon";
                    break;
                case "eclipse":
                    Main.dayTime = true;
                    Main.time = 0;
                    Main.eclipse = true;
                    started = "eclipse";
                    break;
                case "pumpkin":
                case "pumpkinmoon":
                    Main.startPumpkinMoon();
                    started = "pumpkinMoon";
                    break;
                case "frost":
                case "frostmoon":
                case "snowmoon":
                    Main.startSnowMoon();
                    started = "snowMoon";
                    break;
                case "goblin":
                    Main.StartInvasion(1);
                    started = "invasion:goblin";
                    break;
                case "pirate":
                    Main.StartInvasion(2);
                    started = "invasion:pirate";
                    break;
                case "snowman":
                    Main.StartInvasion(3);
                    started = "invasion:snowman";
                    break;
                case "martian":
                    Main.StartInvasion(4);
                    started = "invasion:martian";
                    break;
                default:
                    return Json.Error("event 只支持 bloodmoon / eclipse / pumpkin / frost / goblin / pirate / snowman / martian");
            }
            return Json.Ok(new
            {
                started,
                bloodMoon = Main.bloodMoon,
                eclipse = Main.eclipse,
                pumpkinMoon = Main.pumpkinMoon,
                snowMoon = Main.snowMoon,
                invasionType = Main.invasionType,
                dayTime = Main.dayTime,
            });
        }

        /// <summary>
        /// 大地图：已探索区域的包围盒与占比。逐格取样（step 越大越粗），
        /// 不返回逐格位图——那东西一次几十万个数，读回来也没用。
        /// </summary>
        private static string MapInfo(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int step = Math.Clamp(Json.Int(p, "step", 8), 1, 64);
            int w = Main.Map.MaxWidth, h = Main.Map.MaxHeight;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            int revealed = 0, total = 0;
            for (int y = 0; y < h; y += step)
            {
                for (int x = 0; x < w; x += step)
                {
                    total++;
                    if (!Main.Map.IsRevealed(x, y)) continue;
                    revealed++;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
            Player pl = P;
            return Json.Ok(new
            {
                mapWidth = w,
                mapHeight = h,
                step,
                sampled = total,
                revealed,
                revealedPercent = total > 0 ? (int)Math.Round(100.0 * revealed / total) : 0,
                explored = revealed > 0 ? new { x0 = minX, y0 = minY, x1 = maxX, y1 = maxY } : null,
                player = pl != null && pl.active ? new[] { (int)(pl.Center.X / 16f), (int)(pl.Center.Y / 16f) } : null,
                note = "explored 是已探索区域的包围盒，不是逐格地图；step 越小越精确也越慢",
            });
        }

        /// <summary>
        /// 拆堆叠：把一叠分成两半，一半挪到空格。纯背包操作（和 drop/grab 同类），
        /// 不动世界。
        /// </summary>
        private static string SplitStack(JsonElement p)
        {
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int slot = Json.Int(p, "slot", pl.selectedItem);
            if (slot < 0 || slot >= pl.inventory.Length) return Json.Error("slot 越界");
            Item src = pl.inventory[slot];
            if (src == null || src.type <= 0) return Json.Error("这个格子是空的");
            if (src.stack < 2) return Json.Error("只有 1 个，拆不开");
            int half = Math.Clamp(Json.Int(p, "count", src.stack / 2), 1, src.stack - 1);
            int to = -1;
            for (int i = 0; i < MainBagSlots; i++)
            {
                if (pl.inventory[i] != null && pl.inventory[i].type == 0) { to = i; break; }
            }
            if (to < 0) return Json.Error("背包没有空格放拆出来的那一半");
            Item halfItem = src.Clone();
            halfItem.stack = half;
            pl.inventory[to] = halfItem;
            src.stack -= half;
            return Json.Ok(new { from = slot, to, name = halfItem.Name, type = halfItem.type, moved = half, left = src.stack });
        }

        /* ── 钓鱼 / 捉虫 ───────────────────────────────────────────── */

        private static int FindFishingPole(Player pl)
        {
            for (int i = 0; i < MainBagSlots; i++)
            {
                Item it = pl.inventory[i];
                if (it != null && it.type > 0 && it.fishingPole > 0) return i;
            }
            return -1;
        }

        private static int FindBugNet(Player pl)
        {
            for (int i = 0; i < MainBagSlots; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.type <= 0) continue;
                if (it.type == ItemID.BugNet || it.type == ItemID.GoldenBugNet || it.type == ItemID.FireproofBugNet) return i;
            }
            return -1;
        }

        /// <summary>
        /// 抛竿：选中钓竿、瞄准水面格、短按一次。原版 ItemCheck 自己抛出浮标
        /// （带 bobber 标记的弹幕），浮标状态用 bobber 读。
        /// </summary>
        private static string CastLine(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int slot = FindFishingPole(pl);
            if (slot < 0) return Json.Error("背包里没有钓竿（fishingPole>0 的物品）");
            int x = Json.Int(p, "x", int.MinValue), y = Json.Int(p, "y", int.MinValue);
            if (x == int.MinValue || y == int.MinValue) return Json.Error("castline 需要 x,y（水面那一格）");
            BridgeSystem.BeginItemUse(slot, x, y, Json.Int(p, "ms", 150));
            return Json.Ok(new { used = true, poleSlot = slot, at = new[] { x, y }, hint = "这只说明钓竿被用了一次，不等于抛出浮标；立刻用 bobber 读，count>0 才真的抛出去了" });
        }

        /// <summary>
        /// 收线：再短按一次钓竿。原版看到浮标还在就会把它收回来，
        /// 中途咬钩则会结算钓获。
        /// </summary>
        private static string Reel(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int slot = FindFishingPole(P);
            if (slot < 0) return Json.Error("背包里没有钓竿");
            BridgeSystem.BeginItemUse(slot, Player.tileTargetX, Player.tileTargetY, Json.Int(p, "ms", 150));
            return Json.Ok(new { used = true, poleSlot = slot, hint = "收线是否生效用 bobber 读：count 变 0 才是收回去了" });
        }

        /// <summary>
        /// 浮标状态：只返回原始读数（位置/速度/是否浸在液体里）。
        /// 咬钩不在这里"判"，由调用方连续采样看浮标下坠——那是玩家在水面上看到的同一个现象。
        /// </summary>
        private static string Bobbers()
        {
            if (!InWorld(out string err)) return Json.Error(err);
            Player pl = P;
            var list = new List<object>();
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr == null || !pr.active || !pr.bobber) continue;
                if (pl != null && pr.owner != pl.whoAmI) continue;
                int tx = (int)(pr.Center.X / 16f), ty = (int)(pr.Center.Y / 16f);
                bool inLiquid = InBounds(tx, ty) && Main.tile[tx, ty].LiquidAmount > 0;
                list.Add(new
                {
                    bid = i,
                    tile = new[] { MathF.Round(pr.Center.X / 16f, 2), MathF.Round(pr.Center.Y / 16f, 2) },
                    velocityY = MathF.Round(pr.velocity.Y, 3),
                    inLiquid,
                    liquidType = inLiquid ? Main.tile[tx, ty].LiquidType : 0,
                    age = pr.timeLeft,
                });
            }
            return Json.Ok(new { count = list.Count, bobbers = list.ToArray() });
        }

        /// <summary>
        /// 抓虫：选中虫网对准目标挥一下，判定交给原版（CheckCatchNPC 只在 ItemCheck 里跑）。
        /// who 是 npc 索引，也可以直接给 x,y。
        /// </summary>
        private static string Capture(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int slot = FindBugNet(pl);
            if (slot < 0) return Json.Error("背包里没有虫网（BugNet / GoldenBugNet / FireproofBugNet）");
            int x = Json.Int(p, "x", int.MinValue), y = Json.Int(p, "y", int.MinValue);
            int who = Json.Int(p, "who", -1);
            if (who >= 0 && who < Main.maxNPCs)
            {
                NPC n = Main.npc[who];
                if (n == null || !n.active) return Json.Error($"who={who} 这个 NPC 不在场");
                if (!n.CountsAsACritter) return Json.Error($"{n.FullName} 不是小动物，虫网抓不了");
                x = (int)(n.Center.X / 16f);
                y = (int)(n.Center.Y / 16f);
            }
            if (x == int.MinValue || y == int.MinValue) return Json.Error("capture 需要 who 或 x,y");
            BridgeSystem.BeginItemUse(slot, x, y, Json.Int(p, "ms", 300));
            return Json.Ok(new { used = true, netSlot = slot, at = new[] { x, y }, hint = "这只说明挥了一次网，不等于抓到；用 npc 看目标还在不在、用 inventory 看有没有多出物品" });
        }


        /* ── 桶：舀液体 / 倒液体 ───────────────────────────────────── */

        private static readonly int[] FilledBuckets =
        {
            ItemID.WaterBucket, ItemID.LavaBucket, ItemID.HoneyBucket,
            ItemID.BottomlessBucket, ItemID.BottomlessLavaBucket,
            ItemID.BottomlessHoneyBucket, ItemID.BottomlessShimmerBucket,
        };

        /// <summary>
        /// 液体操作：手里有空桶就舀目标格的液体，手里有装满的桶就往空处倒。
        /// 判定全部交给原版（桶自己的 ItemCheck 决定舀/倒，以及舀的是什么）。
        /// </summary>
        private static string LiquidOp(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int x = Json.Int(p, "x", int.MinValue), y = Json.Int(p, "y", int.MinValue);
            if (x == int.MinValue || y == int.MinValue) return Json.Error("liquidop 需要 x,y（要舀/要倒的那一格）");
            if (!InBounds(x, y)) return Json.Error("坐标超出世界范围");

            int empty = -1, filled = -1;
            for (int i = 0; i < MainBagSlots; i++)
            {
                Item it = pl.inventory[i];
                if (it == null || it.type <= 0) continue;
                if (empty < 0 && it.type == ItemID.EmptyBucket) empty = i;
                if (filled < 0 && Array.IndexOf(FilledBuckets, it.type) >= 0) filled = i;
            }

            Tile t = Main.tile[x, y];
            bool hasLiquid = t.LiquidAmount > 0;
            string mode;
            int slot;
            if (hasLiquid && empty >= 0) { slot = empty; mode = "fill"; }
            else if (!hasLiquid && filled >= 0) { slot = filled; mode = "pour"; }
            else if (hasLiquid && empty < 0 && filled >= 0) return Json.Error("这格有液体，但背包里没有空桶（只有装满的桶）");
            else if (!hasLiquid && empty >= 0 && filled < 0) return Json.Error("这格是空的，但背包里没有装满液体的桶");
            else return Json.Error("背包里既没有空桶也没有满桶");

            BridgeSystem.BeginItemUse(slot, x, y, Json.Int(p, "ms", 400));
            return Json.Ok(new
            {
                mode,
                bucketSlot = slot,
                item = pl.inventory[slot]?.Name,
                at = new[] { x, y },
                hint = "动完用 tile 回读 liquid/liquidType 看变化",
            });
        }

        private static string Chat(JsonElement p)
        {
            string text = Json.Str(p, "text", "");
            if (string.IsNullOrWhiteSpace(text)) return Json.Error("chat 需要 text");
            Main.NewText(text, 255, 255, 255);
            return Json.Ok(new { sent = text });
        }

        /// <summary>
        /// 批量把一段文本刷进本地聊天框，用来把长对话/日志搬进游戏里看。
        /// 只追加 UI 聊天历史，不碰世界状态（不动 tile / 玩家 / NPC）。
        /// 前缀决定颜色：`&gt;` 玩家、`&lt;` 助手、`~` 思考（灰）、`#` 标题、`!` 警告，其余白色。
        /// 参数：text 或 path（二选一）、max（最多几行，默认 300，上限 500）、
        /// maxLen（单行截断长度，默认 380）。
        /// </summary>
        private static string Dialog(JsonElement p)
        {
            string text = Json.Str(p, "text", "");
            string path = Json.Str(p, "path", "");
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    if (!System.IO.File.Exists(path)) return Json.Error("dialog 找不到文件: " + path);
                    text = System.IO.File.ReadAllText(path);
                }
                catch (Exception ex) { return Json.Error("dialog 读文件失败: " + ex.Message); }
            }
            if (string.IsNullOrWhiteSpace(text)) return Json.Error("dialog 需要 text 或 path");

            int max = Math.Clamp(Json.Int(p, "max", 300), 1, 500);
            int maxLen = Math.Clamp(Json.Int(p, "maxLen", 380), 40, 400);
            string[] raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            int sent = 0, skipped = 0;
            foreach (string line0 in raw)
            {
                if (sent >= max) break;
                string line = line0.TrimEnd();
                if (line.Trim().Length == 0) { skipped++; continue; }
                if (line.Length > maxLen) line = line.Substring(0, maxLen - 3) + "...";
                byte r = 255, g = 255, b = 255;
                char lead = line.TrimStart()[0];
                if (lead == '>') { r = 170; g = 255; b = 170; }
                else if (lead == '<') { r = 150; g = 215; b = 255; }
                else if (lead == '#') { r = 255; g = 215; b = 110; }
                else if (lead == '!') { r = 255; g = 130; b = 130; }
                else if (lead == '~') { r = 165; g = 170; b = 185; }
                Main.NewText(line, r, g, b);
                sent++;
            }
            return Json.Ok(new { total = raw.Length, sent, skipped, truncated = raw.Length > max });
        }
    }
}
