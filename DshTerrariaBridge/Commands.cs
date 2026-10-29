using System;
using System.Collections.Generic;
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

            switch (cmd)
            {
                case "ping": return Ping();
                case "state": return State();
                case "humaninput": return HumanInput();
                case "keyevents": return KeyEventsCmd(p);
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
                case "talk": return TalkTo(p);
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
                case "place": return Place(p);
                case "tiledata": return TileDataCmd(p);
                case "move": return Move(p);
                case "moveto": return MoveTo(p);
                case "jump": return Jump(p);
                case "climb": return Climb(p);
                case "use": return Use(p);
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
                return Json.Ok(BridgeSystem.ChatPoll(Json.Int(p, "since", 0), Math.Clamp(Json.Int(p, "max", 20), 1, 100)));

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

        /// <summary>命令名清单：help 与 unknown 提示都从这里生成，
        /// 免得再出现"实现了但提示里没有、调用方以为不存在"的误导。</summary>
        internal static readonly string[] CommandNames = new[]
        {
            "accessory", "api", "attack", "blood", "bloodmoon", "bobber", "boss", "buy", "capture", "castline", "chat", "chatpoll", "chatpost", "chest", "climb", "craft", "down", "drop", "duplicate", "eclipse", "equip", "event", "fill", "find", "fly", "follow", "frost", "frostmoon", "goblin", "grab", "gravity", "guard", "grapple", "heal", "help", "homeroom", "hook", "housediag", "housing", "humaninput", "idle", "idmap", "interact", "inventory", "items", "jump", "keyevents", "left", "liquidop", "map", "martian", "mine", "minion", "mirror", "mount", "move", "needs", "npc", "path", "ping", "pirate", "place", "players", "pumpkin", "pumpkinmoon", "quest", "recipes", "reel", "removeblock", "research", "right", "roomcheck", "save", "scan", "select", "sell", "shoot", "shop", "shopdebug", "shops", "snowman", "snowmoon", "splitstack", "startevent", "state", "stop", "talk", "tile", "tiledata", "trash", "travel", "travelstatus", "unhook", "up", "use", "walkto", "wall"
        };

        private static string Help() => Json.Ok(new
        {
            // 命令名一律从 CommandNames 出，避免"提示里的清单"和"真正能调的"再分叉。
            commands = CommandNames,
            notes = new[]
            {
                "jump{ms,tiles,dir,moveMs}：只走原生输入层（Triggers.Jump），高度由按住时长和游戏物理决定；dir=left/right 时同帧带横向输入",
                "hook/grapple{x,y,slot,ms}：抓钩，真人键位语义；unhook 脱钩",
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
                ["screen"] = new { w = Main.screenWidth, h = Main.screenHeight, x = (int)Main.screenPosition.X, y = (int)Main.screenPosition.Y },
                ["mouseWorld"] = inWorld ? new[] { MathF.Round(Main.MouseWorld.X / 16f, 1), MathF.Round(Main.MouseWorld.Y / 16f, 1) } : null,
                ["mouseTile"] = inWorld ? new[] { (int)(Main.MouseWorld.X / 16f), (int)(Main.MouseWorld.Y / 16f) } : null,
            };

            if (inWorld)
            {
                Player pl = P;
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
                data["inputDiag"] = InputDiagnostics(pl);
                data["guard"] = new
                {
                    active = BridgeSystem.GuardUntil > BridgeSystem.Now,
                    guardDefault = BridgeSystem.GuardDefault,
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
            if (format != "rows" && format != "ascii" && format != "rle") format = "rows";

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
                        if (t.LiquidAmount > 0) sb.Append(t.LiquidType == LiquidID.Lava ? 'L' : '~');
                        else if (t.HasTile) sb.Append(t.IsHalfBlock || t.Slope != SlopeType.Solid ? '^' : '#');
                        else sb.Append('.');
                    }
                    lines[j] = sb.ToString();
                }
                data["map"] = string.Join("\n", lines);
                data["legend"] = ". 空 · # 实心 · ^ 半砖/斜坡 · ~ 水/其他液体 · L 岩浆（方块类型请用 terraria_tile）";
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
            int n = Math.Clamp(Json.Int(p, "count", it.stack), 1, it.stack);
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
                left = pl.inventory[slot].stack, verify = "grab 能捡回来才算真的落地" });
        }

        private static string HealSelf(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            int before = pl.statLife;
            pl.QuickHeal();
            int after = pl.statLife;
            return Json.Ok(new { before, after, healed = after - before, hasPotion = after > before });
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
        /// 走到 NPC 旁边并真实对话（Player.SetTalkNPC），商店界面由游戏自己打开。
        /// 之后 shop / buy 只读已经打开的商店，不再自己 SetupShop——那样等于凭空开店。
        /// </summary>
        private static string TalkTo(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            NPC npc = NearbyTownNPC(pl, Json.Int(p, "npc", -1), out float d);
            if (npc == null) return Json.Error("附近没有可以对话的城市 NPC");
            if (d > 12 * 16f) return Json.Error($"离 {npc.FullName} 太远（{d / 16f:F0} 格），先走过去");
            int idx = npc.whoAmI;
            pl.SetTalkNPC(idx, true);
            // 游戏自己打开商店的入口：SetNPCShopIndex 会填好 Main.instance.shop 并设 Main.npcShop
            try { Main.SetNPCShopIndex(npc.type); } catch { }
            return Json.Ok(new
            {
                npc = npc.FullName, type = npc.type, distance = (int)(d / 16f),
                talkNPC = pl.talkNPC, npcShop = Main.npcShop,
                hasShop = Main.npcShop > 0,
                // 对话正文：SetTalkNPC 之后游戏自己把这一句填进 Main.npcChatText，
                // 这是唯一能读到的"NPC 说了什么"（选项按钮的文案是绘制时算的，读不到）。
                chatText = Main.npcChatText,
                cornerItem = Main.npcChatCornerItem,
                isAngler = npc.type == NPCID.Angler,
            });
        }

        private static int ShopPrice(Item it) => it.shopCustomPrice.HasValue ? it.shopCustomPrice.Value : it.value;

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
            int hands = Main.instance.shop.Length;
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
            Main.SetNPCShopIndex(npc.type); Snap("SetNPCShopIndex(npc.type)");
            Main.SetNPCShopIndex(1); Snap("SetNPCShopIndex(1)");
            for (int i = 0; i < hands; i++)
            {
                Main.instance.shop[i].SetupShop(1);
                Snap("shop[" + i + "].SetupShop(1)");
            }
            Main.instance.shop[0].SetupShop(npc.type); Snap("shop[0].SetupShop(npc.type)");
            return Json.Ok(new { npc = npc.FullName, npcType = npc.type, hands, report });
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
        private static int ResolveVanillaShopIndex(NPC npc, out string npcShopName)
        {
            npcShopName = null;
            try { npcShopName = NPCShopDatabase.GetShopName(npc.type, npc.FullName); } catch { }
            if (string.IsNullOrEmpty(npcShopName)) return -1;
            for (int id = 0; id < 400; id++)
            {
                string name = null;
                try { name = NPCShopDatabase.GetShopNameFromVanillaIndex(id); } catch { }
                if (string.IsNullOrEmpty(name)) continue;
                if (npcShopName.StartsWith(name, StringComparison.OrdinalIgnoreCase)) return id;
                if (npcShopName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return id;
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

            Main.instance.shop[0].SetupShop(shopId);
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

            Main.instance.shop[0].SetupShop(shopId);
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
            string filter = Json.Str(p, "member", "");
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
            return Json.Ok(new { type = t.FullName, count = list.Count, members = list });
        }

        /// <summary>
        /// Dump the engine constant tables to a TSV file so the knowledge base can be generated
        /// instead of hand-typed. These numbers are program constants: the same id points at the
        /// same object in every world, which is why they belong in the KB (user m05433).
        ///
        /// Two kinds of rows:
        ///   tile/wall  → id + enum name (from the ID classes)
        ///   item       → id + name + what it places + its tool/damage stats
        /// Read-only with respect to the world; it only writes the file you name.
        /// </summary>
        private static string IdMap(JsonElement p)
        {
            string path = Json.Str(p, "path", "");
            if (string.IsNullOrWhiteSpace(path)) return Json.Error("idmap 需要 path（TSV 输出路径）");

            var lines = new List<string>
            {
                "kind\tid\tenum\tname\tcreateTile\tcreateWall\tpick\taxe\thammer\tdamage\tmaxStack",
            };
            int tiles = 0, walls = 0, items = 0;

            var tileT = typeof(Main).Assembly.GetType("Terraria.ID.TileID");
            if (tileT != null)
                foreach (var f in tileT.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    long id;
                    try { id = Convert.ToInt64(f.GetRawConstantValue()); } catch { continue; }
                    lines.Add("tile\t" + id + "\t" + f.Name + "\t" + f.Name + "\t-1\t-1\t0\t0\t0\t0\t0");
                    tiles++;
                }

            var wallT = typeof(Main).Assembly.GetType("Terraria.ID.WallID");
            if (wallT != null)
                foreach (var f in wallT.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    long id;
                    try { id = Convert.ToInt64(f.GetRawConstantValue()); } catch { continue; }
                    lines.Add("wall\t" + id + "\t" + f.Name + "\t" + f.Name + "\t-1\t-1\t0\t0\t0\t0\t0");
                    walls++;
                }

            // 物品行用游戏自己的原型表：名称、放置后变成哪个物块/墙、镐斧锤力、伤害、堆叠上限。
            foreach (var kv in ContentSamples.ItemsByType)
            {
                Item it = kv.Value;
                if (it == null || it.type <= 0) continue;
                string nm = (it.Name ?? "").Replace('\t', ' ').Replace('\n', ' ').Trim();
                if (nm.Length == 0) continue;
                string en = "";
                try { en = ItemID.Search.GetName(it.type) ?? ""; } catch { en = ""; }
                if (en.Length == 0) en = nm;
                lines.Add("item\t" + it.type + "\t" + en + "\t" + nm + "\t" + it.createTile + "\t" + it.createWall
                          + "\t" + it.pick + "\t" + it.axe + "\t" + it.hammer + "\t" + it.damage
                          + "\t" + it.maxStack);
                items++;
            }

            try { System.IO.File.WriteAllLines(path, lines); }
            catch (Exception ex) { return Json.Error("写不了 " + path + ": " + ex.Message); }
            return Json.Ok(new { path, tiles, walls, items, rows = lines.Count - 1 });
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
        /// 只读查询 travel 任务状态。原来只能靠再调一次 travel 来“看”状态，
        /// 而那会重新 StartTravel 把 steps/mine/swings 清零，导致我误判成“travel 坏了、根本不动”。
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
            if (dig && dir == "down")
            {
                int sx = Main.spawnTileX, sy = Main.spawnTileY;
                if (Math.Abs(px - sx) <= 100 && Math.Abs(py - sy) <= 100)
                    return Json.Error($"拒绝在出生点 {sx},{sy} 附近 100 格内向下挖（当前 {px},{py}）；要走路请用 dig:false，要挖矿请先走远");
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
            bool off = Json.Bool(p, "off", true);

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
            // 默认 1300ms：丛林草/冰雪/石头在铁镐下要 1.2~1.3 秒，默认 900 会让"看起来挖了其实没掉"
            // 反复踩过三次坑（100ms、800ms 都白挖），所以把默认值调到够用，下限提到 300。
            // 默认“挖到空为止”：8 秒只是兜底上限，实际由桥每 tick 回读目标格，一挖空立刻收手。
            // 不同方块硬度差很多（土 1 下、石头 3 下、更硬的更多），固定时长必然有的不够、有的白按。
            string started = BeginPlayerMine(x, y, Math.Clamp(Json.Int(p, "ms", 8000), 300, 20000));
            if (started != null && started.StartsWith("{")) return started;
            if (started != null) return Json.Error(started);
            return Json.Error("empty player-mode response");
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
                    note = "脚底在绳上：先按下脱绳，离开真实绳格后再执行 moveto",
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
                    note = "脚底在绳上：先按下脱绳，离开真实绳格后再执行水平移动",
                });
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
            bool legacyNativeFalse = p.TryGetProperty("native", out var nv) && nv.ValueKind == JsonValueKind.False;
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
                    note = "脚底在绳上：先按下脱绳，离开真实绳格后再执行跳跃",
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
                BridgeSystem.GuardUntil = 0;
                BridgeSystem.GuardEngaged = false;
                return Json.Ok(new { off = true, note = "自动防守已关闭（guard 不带 off 可再打开）" });
            }
            BridgeSystem.GuardDefault = true;
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
                hostileInRange = hostile,
                weaponSlot = slot,
                weapon = new { type = pl.inventory[slot].type, name = pl.inventory[slot].Name },
                minions = pl.numMinions,
                note = "角色会自己转向、自己换武器、自己按住开火；伤害由原版弹幕/挥击产生",
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
            BridgeSystem.JumpUntil = BridgeSystem.Now + BridgeSystem.Ticks(160);
            BridgeSystem.JumpIssued = false;
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
            BridgeSystem.JumpUntil = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            BridgeSystem.JumpIssued = false;
            // 飞/脱钩都是"按住跳跃键"的语义：走原生输入。
            return Json.Ok(new
            {
                ms,
                hasWings = wings,
                wingTimeMax = pl.wingTimeMax,
                wingTime = pl.wingTime,
                mounted,
                velY = pl.velocity.Y,
                hint = wings || mounted ? null : "没有翅膀也没有飞行坐骑，按住跳跃只会起跳，不会飞",
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
                if (activeBefore) pl.mount.Dismount(pl);
                return Json.Ok(new { action, activeBefore, typeBefore, activeAfter = pl.mount != null && pl.mount.Active });
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
                ulong until = BridgeSystem.NowTick() + (ulong)Math.Max(2, Json.Int(p, "ticks", 12));
                BridgeSystem.MountUntil = until;
                return Json.Ok(new
                {
                    action,
                    mountSlot = mslot,
                    mountItem = new { type = mit.type, name = mit.Name, mountType = mit.mountType },
                    untilTick = until,
                    activeBefore,
                    typeBefore,
                    note = "已发送原版坐骑键（controlMount）；等 untilTick 后回读 state.player.mounted / mountType",
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

            BridgeSystem.FollowTargetIdx = target.whoAmI;
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
        private static int ConsumeRequirement(Player pl, int type, int count, List<HashSet<int>> groups)
        {
            int left = count;
            var order = new List<int> { type };
            foreach (var set in groups)
            {
                if (!set.Contains(type)) continue;
                foreach (int member in set) if (member != type) order.Add(member);
            }
            foreach (int want in order)
            {
                for (int i = 0; i < MainBagSlots && left > 0; i++)
                {
                    Item it = pl.inventory[i];
                    if (it.type != want) continue;
                    int take = Math.Min(it.stack, left);
                    it.stack -= take; left -= take;
                    if (it.stack <= 0) it.SetDefaults(0);
                }
                if (left <= 0) break;
            }
            return count - left;
        }

        /// <summary>把物品直接塞进背包（先叠加再空格），返回实际放入数量。</summary>
        private static int AddToInventory(Player pl, int type, int stack)
        {
            int maxStack = 9999;
            try { Item sample = ContentSamples.ItemsByType[type]; if (sample != null && sample.maxStack > 0) maxStack = sample.maxStack; } catch { }
            int added = 0;
            for (int i = 0; i < MainBagSlots && stack > 0; i++)
            {
                Item it = pl.inventory[i];
                if (it.type == type && it.stack > 0 && it.stack < maxStack)
                {
                    int can = Math.Min(maxStack - it.stack, stack);
                    it.stack += can; stack -= can; added += can;
                }
            }
            for (int i = 0; i < MainBagSlots && stack > 0; i++)
            {
                Item it = pl.inventory[i];
                if (it.type <= 0)
                {
                    it.SetDefaults(type);
                    it.stack = Math.Min(stack, maxStack);
                    stack -= it.stack; added += it.stack;
                }
            }
            return added;
        }

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

            int crafted = 0;
            int dropped = 0;
            for (int round = 0; round < times; round++)
            {
                foreach (var kv in need)
                {
                    if (CountRequirement(pl, kv.Key, groups) < kv.Value)
                        return crafted > 0
                            ? Json.Ok(new { item = itemType, name = Lang.GetItemNameValue(itemType), crafted, dropped, times, reason = "材料用完了" })
                            : Json.Error($"材料不足：需要 {Lang.GetItemNameValue(kv.Key)} x{kv.Value}，背包里有 {CountRequirement(pl, kv.Key, groups)}");
                }
                if (!HasStation(pl, recipe))
                    return crafted > 0
                        ? Json.Ok(new { item = itemType, name = Lang.GetItemNameValue(itemType), crafted, dropped, times, reason = "附近没有工作站" })
                        : Json.Error("附近没有这个配方需要的工作站（工作台/熔炉/铁砧…）");

                // 扣材料（配方组按"任意成员"结算）
                foreach (var kv in need) ConsumeRequirement(pl, kv.Key, kv.Value, groups);

                int per = Math.Max(1, recipe.createItem.stack);
                int added = AddToInventory(pl, itemType, per);
                // Do not claim the inventory received what actually fell on the floor.
                if (added < per)
                {
                    int spill = per - Math.Max(0, added);
                    pl.QuickSpawnItem(new EntitySource_Misc("dsh-bridge"), itemType, spill);
                    dropped += spill;
                    added = Math.Max(0, added);
                }
                crafted += added;
            }

            return Json.Ok(new
            {
                item = itemType,
                name = Lang.GetItemNameValue(itemType),
                crafted,
                dropped,
                times,
                perCraft = Math.Max(1, recipe.createItem.stack),
                groupsUsed = groups.Count,
            });
        }


        private static string Npcs(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int r = Math.Clamp(Json.Int(p, "r", 40), 1, 200);
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
    }
}
