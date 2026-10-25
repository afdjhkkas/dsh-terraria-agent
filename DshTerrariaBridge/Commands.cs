using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria.GameContent.Creative;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

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
                case "scan": return Scan(p);
                case "tile": return TileAt(p);
                case "mine": return Mine(p);
                case "mirror": return MirrorHome(p);
                case "accessory": return Accessory(p);
                case "walkto": return WalkTo(p);
                case "path": return PathTo(p);
                case "research": return Research(p);
                case "find": return FindTile(p);
                case "duplicate": return Duplicate(p);
                case "fill": return FillArea(p);
                case "housing": return Housing(p);
                case "needs": return RoomDiagnostics(p);
                case "items": return DroppedItems(p);
                case "save": return SaveWorld(p);
                case "removeblock": return RemoveBlock(p);
                case "recipes": return Recipes(p);
                case "api": return ApiLookup(p);
                case "talk": return TalkTo(p);
                case "shop": return ShopList(p);
                case "shopdebug": return ShopDebug(p);
                case "shops": return ShopFind(p);
                case "buy": return ShopBuy(p);
                case "sell": return ShopSell(p);
                case "chest": return ChestCmd(p);
                case "drop": return DropItem(p);
                case "heal": return HealSelf(p);
                case "boss": return BossStatus(p);
                case "event": return EventStatus(p);
                case "equip": return EquipArmor(p);
                case "place": return Place(p);
                case "move": return Move(p);
                case "jump": return Jump(p);
                case "use": return Use(p);
                case "attack": return Attack(p);
                case "spawn_npc": return SpawnNpc(p);
                case "stop":
                    BridgeSystem.StopPath("cancelled");
                    BridgeSystem.StopWalk("cancelled");
                    BridgeSystem.StopMining();
                    BridgeSystem.ClearControls();
                    return Json.Ok(new { stopped = true });
                case "teleport": return Teleport(p);
                case "wall": return Wall(p);
                case "inventory": return Inventory();
                case "craft": return Craft(p);
                case "give": return Give(p);
                case "npc": return Npcs(p);
                case "chat": return Chat(p);
                case "help": return Help();
                default: return Json.Error($"unknown command: {cmd}（可用 ping/state/scan/tile/mine/place/move/jump/use/attack/spawn_npc/stop/teleport/wall/inventory/craft/give/npc/chat）");
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

        private static string Help() => Json.Ok(new
        {
            commands = new[]
            {
                "ping", "state", "scan{x,y,r}", "tile{x,y}", "mine{x,y,noItem}",
                "place{x,y,type,style,forced}", "move{dir,ms}", "jump{ms}", "use{slot,right,ms}",
                "attack{who,r,ms,interval,slot}", "spawn_npc{type,x,y}",
                "stop", "teleport{x,y}", "inventory", "craft{item,times}", "give{type,stack}",
                "npc{r}", "chat{text}",
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
                    tick = Main.GameUpdateCount,
                };
                data["player"] = new
                {
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
                // Diagnostics: when a player-mode action "swings but nothing happens",
                // this says whether the synthetic input was actually active.
                data["input"] = new
                {
                    hasFocus = Main.hasFocus,
                    lastClickOk = BridgeSystem.LastClickOk,
                    lastClickError = BridgeSystem.LastClickError,
                    lastClickTick = BridgeSystem.LastClickTick,
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
                    list.Add(new { type = pl.buffType[i], name = BuffID.Search.GetName(pl.buffType[i]), time = pl.buffTime[i] });
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
                int stride = withWalls ? 4 : 2;
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
                            row[k + 3] = ShapeCode(t);
                        }
                        else
                        {
                            row[k + 1] = t.LiquidAmount;
                        }
                    }
                    rows.Add(row);
                }
                data["stride"] = stride;
                data["fields"] = withWalls ? new[] { "type", "wall", "liquid", "shape" } : new[] { "type", "liquid" };
                data["tiles"] = rows;
                data["legend"] = "type=-1 空，其余是 Terraria 方块 ID（0=土块）；"
                    + "tiles[j] 是 y=y0+j 这一行，第 i 格在 tiles[j][i*stride .. i*stride+stride-1]；"
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
                liquid = t.LiquidAmount,
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

        /* ── 拟人操作：找工具/材料，掏出来，瞄准，挥动 ─────────────────── */

        private static int FindPickaxe(Player pl)
        {
            for (int i = 0; i < pl.inventory.Length; i++)
                if (pl.inventory[i] != null && pl.inventory[i].type > 0 && pl.inventory[i].pick > 0) return i;
            return -1;
        }

        private static int FindAxe(Player pl)
        {
            for (int i = 0; i < pl.inventory.Length; i++)
                if (pl.inventory[i] != null && pl.inventory[i].type > 0 && pl.inventory[i].axe > 0) return i;
            return -1;
        }

        private static int FindItemForTile(Player pl, int tileType)
        {
            for (int i = 0; i < pl.inventory.Length; i++)
            {
                Item it = pl.inventory[i];
                if (it != null && it.type > 0 && it.stack > 0 && it.createTile == tileType) return i;
            }
            return -1;
        }

        private static int FindItemForWall(Player pl, int wallType)
        {
            for (int i = 0; i < pl.inventory.Length; i++)
            {
                Item it = pl.inventory[i];
                if (it != null && it.type > 0 && it.stack > 0 && it.createWall == wallType) return i;
            }
            return -1;
        }

        private static string StartSwing(int slot, int x, int y, int ms, bool right, bool os)
        {
            Player pl = P;
            pl.selectedItem = slot;
            BridgeSystem.SetAim(x, y);
            bool onScreen = BridgeSystem.ApplyAim();
            ulong until = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            BridgeSystem.AimUntil = until + 40;

            if (os)
            {
                // Real mouse: visible cursor, and it needs the game to be foreground.
                BridgeSystem.InjectClickAsync(x, y, right, ms);
            }
            else
            {
                // Synthetic input, applied from PreItemCheck: no focus needed, the user's
                // cursor is untouched, so the agent can act while the player plays.
                if (right) { BridgeSystem.UseTileUntil = until; BridgeSystem.UseTileJustStarted = true; }
                else { BridgeSystem.UseUntil = until; BridgeSystem.UseJustStarted = true; }
            }

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
                os,
                hint = os
                    ? "真实鼠标注入：光标已移过去并按住；需要游戏在前台。"
                    : "合成输入：不需要前台，也不会动你的鼠标；等一下用 tile / inventory 确认结果。",
            });
        }

        /// <summary>Arm a swing with whatever tool the tile needs.</summary>
        private static string BeginPlayerMine(int x, int y, int ms, bool os, bool useInput)
        {
            Player pl = P;
            if (pl.dead) return "玩家已死亡，请先复活";
            if (!BridgeSystem.InReach(pl, x, y)) return "够不到：离这一格超过 7 格，先走过去再挖";
            Tile t = Main.tile[x, y];
            // Which tool a tile needs is a game fact, not a guess: Main.tileAxe marks every
            // tree-like tile (palm, vanity trees, cactus...). Checking only TileID.Trees made the
            // agent swing a pickaxe at a vanity tree and conclude the tile was unbreakable.
            bool needsAxe = t.HasTile && Main.tileAxe[t.TileType];
            int slot = needsAxe ? FindAxe(pl) : FindPickaxe(pl);
            if (slot < 0) return needsAxe ? "背包里没有斧头，砍树需要斧头" : "背包里没有镐子，挖方块需要镐子";

            // Default: synthetic control flags injected from PreItemCheck, so the game's own
            // ItemCheck runs - real swing animation and real drops, with no focus stealing.
            //  - os:true   real mouse (visible cursor, needs the game in the foreground)
            //  - job:true  the Player.PickTile job (works even when ItemCheck cannot run)
            if (os || useInput) return StartSwing(slot, x, y, ms, false, os);

            // Focus-free mining: the job calls Player.PickTile (what a swing calls internally)
            // on the main thread each swing interval, with the tool selected and swinging.
            BridgeSystem.StartMining(x, y, slot, ms + 400);
            Item tool = pl.inventory[slot];
            return Json.Ok(new
            {
                mode = "player",
                x,
                y,
                slot,
                held = new { type = tool.type, name = tool.Name, pick = tool.pick, axe = tool.axe },
                ms,
                direct = true,
                hint = "已开始挥动（不需要前台）；等一下用 tile 确认是不是挖掉了。",
            });
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
                for (int i = 0; i < pl.inventory.Length; i++)
                {
                    Item inv = pl.inventory[i];
                    if (inv == null) continue;
                    if (inv.type == taken.type && inv.stack + n <= inv.maxStack) { target = i; break; }
                }
                if (target < 0)
                    for (int i = 0; i < pl.inventory.Length; i++)
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

        /* ── 丢东西 / 喝药 / Boss 与事件状态 / 穿护甲 ────────────────── */

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
            Item dropped = it.Clone();
            dropped.stack = n;
            Item.NewItem(new EntitySource_DropAsItem(pl), pl.Center, Vector2.Zero, dropped.type, n);
            it.stack -= n;
            if (it.stack <= 0) { pl.inventory[slot] = new Item(); pl.inventory[slot].SetDefaults(0); }
            return Json.Ok(new { dropped = dropped.Name, count = n });
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
            for (int i = 0; i < pl.inventory.Length; i++)
            {
                Item it = pl.inventory[i];
                if (it != null && it.type == stock.type && it.stack < it.maxStack) { slotInBag = i; break; }
            }
            if (slotInBag < 0)
                for (int i = 0; i < pl.inventory.Length; i++)
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
                    for (int i = 0; i < pl.inventory.Length; i++)
                    {
                        Item inv = pl.inventory[i];
                        if (inv != null && inv.type == coinType && inv.stack + chunk <= inv.maxStack) { slot2 = i; break; }
                    }
                    if (slot2 < 0)
                        for (int i = 0; i < pl.inventory.Length; i++)
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
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("removeblock 需要 x,y");
            int before = Main.tile[x, y].HasTile ? (int)Main.tile[x, y].TileType : -1;
            WorldGen.KillTile(x, y, false, false, true);
            int after = Main.tile[x, y].HasTile ? (int)Main.tile[x, y].TileType : -1;
            return Json.Ok(new { x, y, before, after, removed = before != after });
        }

        private static string SaveWorld(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            try
            {
                WorldFile.SaveWorld(false);
                return Json.Ok(new
                {
                    saved = true,
                    world = Main.worldName,
                    spawn = new { x = Main.spawnTileX, y = Main.spawnTileY },
                });
            }
            catch (Exception e)
            {
                return Json.Error("存档失败：" + e.Message);
            }
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
            for (int i = 0; i < pl.inventory.Length; i++)
            {
                Item it = pl.inventory[i];
                if (it != null && it.type > 0 && it.stack > 0 && it.createTile == type) { slot = i; break; }
            }
            if (slot < 0) return Json.Error($"背包里没有能放成方块 {type} 的物品");

            int placed = 0, skipped = 0, outOfRange = 0;
            for (int x = loX; x <= hiX; x++)
            {
                for (int y = loY; y <= hiY; y++)
                {
                    Tile t = Main.tile[x, y];
                    if (t.HasTile) { skipped++; continue; }
                    if (Vector2.Distance(pl.Center, new Vector2(x * 16f + 8f, y * 16f + 8f)) > 30 * 16f) { outOfRange++; continue; }
                    if (pl.inventory[slot].stack <= 0) break;
                    if (WorldGen.PlaceTile(x, y, type, false, false, -1, pl.whoAmI))
                    {
                        pl.inventory[slot].stack--;
                        placed++;
                    }
                    else skipped++;
                    if (pl.inventory[slot].stack <= 0) { pl.inventory[slot].SetDefaults(0); break; }
                }
            }
            return Json.Ok(new { placed, skipped, outOfRange, left = pl.inventory[slot]?.stack ?? 0 });
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
            if (type <= 0) return Json.Error("find 需要 type");
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
                for (int i = 0; i < pl.inventory.Length; i++)
                {
                    Item inv = pl.inventory[i];
                    if (inv != null && inv.type == type && inv.stack + chunk <= inv.maxStack) { slot = i; break; }
                }
                if (slot < 0)
                    for (int i = 0; i < pl.inventory.Length; i++)
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
            var path = BridgeSystem.ComputePath(px, py, x, y, out string why);
            BridgeSystem.PathAllowDig = true;
            if (path == null) return Json.Error(why);
            BridgeSystem.BeginPath(path, BridgeSystem.Now + BridgeSystem.Ticks(ms), allowDig);
            var head = new List<object>();
            for (int i = 0; i < Math.Min(6, path.Count); i++) head.Add(new { x = path[i].x, y = path[i].y });
            return Json.Ok(new { from = new { x = px, y = py }, to = new { x, y }, length = path.Count, head });
        }

        private static string WalkTo(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("walkto 需要 x,y");
            int tol = Json.Int(p, "tol", 1);
            int ms = Json.Int(p, "ms", 15000);
            BridgeSystem.BeginWalk(x, y, tol, BridgeSystem.Now + BridgeSystem.Ticks(ms));
            return Json.Ok(new { target = new { x, y }, tol, ms });
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
                    for (int i = 0; i < pl.inventory.Length; i++)
                    {
                        if (pl.inventory[i] != null && pl.inventory[i].type > 0) continue;
                        pl.inventory[i] = it.Clone();
                        pl.armor[a] = new Item();
                        pl.armor[a].SetDefaults(0);
                        return Json.Ok(new { action = "unequip", from = a, to = i, item = it.Name });
                    }
                }
                return Json.Error("没有可卸的饰品，或者背包满了");
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
            for (int i = 0; i < pl.inventory.Length; i++)
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
            bool admin = Json.Bool(p, "admin", false);
            if (!admin)
            {
                // Player mode: take out a pickaxe/axe, aim at the tile and swing. The game
                // decides whether it breaks, drops items and consumes tool durability.
                if (!HasPlayer(out string perr)) return Json.Error(perr);
                // 默认 1300ms：丛林草/冰雪/石头在铁镐下要 1.2~1.3 秒，默认 900 会让"看起来挖了其实没掉"
                // 反复踩过三次坑（100ms、800ms 都白挖），所以把默认值调到够用，下限提到 300。
                string started = BeginPlayerMine(x, y, Math.Clamp(Json.Int(p, "ms", 1300), 300, 4000), Json.Bool(p, "os", false), !Json.Bool(p, "job", false));
                if (started != null && started.StartsWith("{")) return started;
                if (started != null) return Json.Error(started);
                return Json.Error("empty player-mode response");
            }
            bool noItem = Json.Bool(p, "noItem", false);
            // Walls are a separate decision: WorldGen.KillWall ignores noItem and walls drive
            // housing validity, so removing them must be opt-in.
            bool walls = Json.Bool(p, "walls", false);
            Tile tile = Main.tile[x, y];
            bool hadTile = tile.HasTile;
            int before = TileCode(tile);
            int wallBefore = tile.WallType;
            if (!hadTile && wallBefore == 0)
                return Json.Ok(new { ok = false, x, y, before, after = EmptyTile, removedTile = false, removedWall = false, note = "这一格本来就是空的" });
            // 1.4.4 的 WorldGen.KillTile 返回 void：成败靠前后回读判断
            if (hadTile) WorldGen.KillTile(x, y, false, false, noItem);
            Tile tileAfter = Main.tile[x, y];
            bool removedTile = hadTile && !tileAfter.HasTile;
            bool removedWall = false;
            if (walls && wallBefore != 0 && tileAfter.WallType != 0)
            {
                WorldGen.KillWall(x, y, false);
                tileAfter = Main.tile[x, y];
                removedWall = wallBefore != 0 && tileAfter.WallType == 0;
            }
            return Json.Ok(new
            {
                ok = removedTile || removedWall,
                x,
                y,
                removedTile,
                removedWall,
                before,
                after = TileCode(tileAfter),
                wallBefore,
                wall = tileAfter.WallType,
            });
        }

        private static string Place(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            int type = Json.Int(p, "type", -1);
            int style = Json.Int(p, "style", 0);
            bool forced = Json.Bool(p, "forced", true);
            bool admin = Json.Bool(p, "admin", false);
            if (x < 0 || y < 0) return Json.Error("place 需要 x,y");
            if (type < 0) return Json.Error("place 需要 type（方块类型 ID）");
            if (!ValidTileType(type)) return Json.Error($"invalid tile type {type} (0..{TileID.Count - 1}; 0 is Dirt)");
            if (!InBounds(x, y)) return Json.Error($"out of world bounds: {x},{y}");
            if (!admin)
            {
                // Player mode: the block has to exist in the inventory, we select it and aim.
                if (!HasPlayer(out string perr)) return Json.Error(perr);
                Player pl = P;
                if (pl.dead) return Json.Error("玩家已死亡，请先复活");
                if (!BridgeSystem.InReach(pl, x, y)) return Json.Error("够不到：离这一格超过 7 格，先走过去");
                int slot = FindItemForTile(pl, type);
                if (slot < 0) return Json.Error($"背包里没有能放置 {TileID.Search.GetName(type)} 的物品");
                // Place through the game's own item use (left click), the same path a player
                // uses: it frames the tile, registers it as a solid, consumes the item and
                // respects support. Placing with WorldGen.PlaceTile(forced:true) produced tiles
                // that looked fine but did not behave like real blocks (mobs walked through).
                int ms = Math.Clamp(Json.Int(p, "ms", 320), 100, 2000);
                return StartSwing(slot, x, y, ms, false, Json.Bool(p, "os", false));
            }
            bool ok = WorldGen.PlaceTile(x, y, type, false, forced, -1, style);
            int after = TileCode(Main.tile[x, y]);
            // WorldGen.PlaceTile returns true in cases where nothing actually changed
            // (an existing tile it refused to replace). Report what is really there.
            bool placed = after == type;
            return Json.Ok(new { ok = placed, placed, worldGenOk = ok, x, y, requested = type, after });
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
            bool admin = Json.Bool(p, "admin", false);
            if (!admin)
            {
                if (!HasPlayer(out string perr)) return Json.Error(perr);
                Player pl = P;
                if (pl.dead) return Json.Error("玩家已死亡，请先复活");
                if (!BridgeSystem.InReach(pl, x, y)) return Json.Error("够不到：离这一格超过 7 格，先走过去");
                int slot;
                if (remove)
                {
                    // Removing a wall in vanilla needs a hammer.
                    slot = -1;
                    for (int i = 0; i < pl.inventory.Length; i++)
                        if (pl.inventory[i] != null && pl.inventory[i].hammer > 0) { slot = i; break; }
                    if (slot < 0) return Json.Error("背包里没有锤子，拆墙需要锤子");
                }
                else
                {
                    slot = FindItemForWall(pl, type);
                    if (slot < 0) return Json.Error($"背包里没有能放置该墙的物品（wall id {type}）");
                }
                int ms = Math.Clamp(Json.Int(p, "ms", 320), 100, 2000);
                // Walls are placed (and hammered away) with the LEFT button in vanilla.
                return StartSwing(slot, x, y, ms, false, Json.Bool(p, "os", false));
            }
            Tile t = Main.tile[x, y];
            int before = t.WallType;
            if (remove)
            {
                if (before != 0) WorldGen.KillWall(x, y, false);
            }
            else
            {
                if (type <= 0 || type >= WallID.Count) return Json.Error($"invalid wall type {type} (1..{WallID.Count - 1})");
                if (t.HasTile) return Json.Error("这一格有方块挡着，先 mine 掉再放墙");
                WorldGen.PlaceWall(x, y, type, false);
            }
            int after = Main.tile[x, y].WallType;
            bool ok = remove ? after == 0 : after == type;
            return Json.Ok(new { ok, x, y, remove, requested = remove ? 0 : type, before, after });
        }

        private static string Teleport(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            float x = Json.Float(p, "x", float.NaN), y = Json.Float(p, "y", float.NaN);
            if (float.IsNaN(x) || float.IsNaN(y)) return Json.Error("teleport 需要 x,y（方块坐标）");
            if (!float.IsFinite(x) || !float.IsFinite(y)) return Json.Error("x,y must be finite");
            if (x < 1 || y < 1 || x > Main.maxTilesX - 2 || y > Main.maxTilesY - 2)
                return Json.Error($"out of world bounds: {x},{y}（世界是 {Main.maxTilesX}x{Main.maxTilesY}）");
            Player pl = P;
            var from = new[] { MathF.Round(pl.Center.X / 16f, 1), MathF.Round(pl.Center.Y / 16f, 1) };
            pl.position = new Vector2(x * 16f, y * 16f - pl.height);
            pl.velocity = Vector2.Zero;
            pl.fallStart = pl.fallStart2 = (int)(pl.position.Y / 16f);
            // Landing inside solid rock would leave the player stuck; lift until free.
            int adjusted = 0;
            while (adjusted < 40 && Collision.SolidTiles(pl.position, pl.width, pl.height, false))
            {
                pl.position.Y -= 16f;
                adjusted++;
            }
            pl.fallStart = pl.fallStart2 = (int)(pl.position.Y / 16f);
            return Json.Ok(new
            {
                x,
                y,
                from,
                adjusted,
                tile = new[] { MathF.Round(pl.Center.X / 16f, 1), MathF.Round(pl.Center.Y / 16f, 1) },
            });
        }

        /* ── 动作 ─────────────────────────────────────────────────────── */

        private static string Move(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            string dir = (Json.Str(p, "dir", "right") ?? "right").ToLowerInvariant();
            int ms = Math.Clamp(Json.Int(p, "ms", 500), 1, 5000);
            ulong until = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            var from = new[] { MathF.Round(P.Center.X / 16f, 1), MathF.Round(P.Center.Y / 16f, 1) };
            switch (dir)
            {
                // A new direction replaces its opposite. Without this,
                // sequential left/right calls can overlap and cancel out.
                case "left": BridgeSystem.LeftUntil = until; BridgeSystem.RightUntil = 0; break;
                case "right": BridgeSystem.RightUntil = until; BridgeSystem.LeftUntil = 0; break;
                case "up": BridgeSystem.UpUntil = until; BridgeSystem.DownUntil = 0; break;
                case "down": BridgeSystem.DownUntil = until; BridgeSystem.UpUntil = 0; break;
                default: return Json.Error($"dir 只能是 left/right/up/down，收到 {dir}");
            }
            // Apply once immediately so a short command is not lost between
            // the request tick and the next Player update.
            BridgeSystem.ApplyMovement();
            return Json.Ok(new { dir, ms, from, untilTick = until, tick = BridgeSystem.Now });
        }

        private static string Jump(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            int ms = Math.Clamp(Json.Int(p, "ms", 150), 1, 2000);
            // Optional height in tiles: the bridge solves the impulse, because the game's
            // variable-height jump cannot be driven without real key input.
            float height = 5.5f;
            var hp = p.TryGetProperty("height", out var hv) ? hv : default;
            if (hp.ValueKind == JsonValueKind.Number) height = (float)hp.GetDouble();
            BridgeSystem.BeginJump(BridgeSystem.Now + BridgeSystem.Ticks(ms), height);
            return Json.Ok(new
            {
                ms,
                from = new[] { MathF.Round(pl.Center.X / 16f, 1), MathF.Round(pl.Center.Y / 16f, 1) },
                grounded = pl.velocity.Y == 0f && pl.oldVelocity.Y == 0f,
                untilTick = BridgeSystem.JumpUntil,
                tick = BridgeSystem.Now,
            });
        }

        private static string Use(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            if (!CanTick(out string perr2)) return Json.Error(perr2);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡，请先复活");
            int slot = Json.Int(p, "slot", -1);
            if (slot >= 0 && slot < pl.inventory.Length) pl.selectedItem = slot;
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
                if (held != null && held.type > 0 && held.useAnimation > 0)
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
                // Evidence that something actually started, instead of just "ok".
                anim = pl.itemAnimation,
            });
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
            if (slot >= 0 && slot < pl.inventory.Length) pl.selectedItem = slot;

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

        private static string SpawnNpc(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int type = Json.Int(p, "type", NPCID.BlueSlime);
            if (!ValidNpcType(type)) return Json.Error($"invalid NPC type {type} (1..{NPCID.Count - 1})");
            Player pl = P;
            float x = Json.Float(p, "x", float.NaN);
            float y = Json.Float(p, "y", float.NaN);
            if (float.IsNaN(x)) x = pl.Center.X / 16f + 5f;
            if (float.IsNaN(y)) y = pl.Center.Y / 16f;
            int who = NPC.NewNPC(new EntitySource_Misc("dsh-bridge"), (int)(x * 16f), (int)(y * 16f), type, 0, 0f, 0f, 0f, 0f, pl.whoAmI);
            if (who < 0 || who >= Main.npc.Length || Main.npc[who] == null || !Main.npc[who].active)
                return Json.Error("NPC 生成失败");
            NPC n = Main.npc[who];
            return Json.Ok(new { who, type = n.type, netId = n.netID, name = n.FullName, life = n.life, tile = new[] { MathF.Round(n.Center.X / 16f, 1), MathF.Round(n.Center.Y / 16f, 1) } });
        }

        /* ── 背包 / 合成 / 生成 ───────────────────────────────────────── */

        private static string Inventory()
        {
            if (!InWorld(out string err)) return Json.Error(err);
            Player pl = P;
            var slots = new List<object>();
            for (int i = 0; i < pl.inventory.Length; i++)
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
            return Json.Ok(new
            {
                selected = pl.selectedItem,
                count = slots.Count,
                items = slots.ToArray(),
                equipment = equip.ToArray(),
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
                for (int i = 0; i < pl.inventory.Length && left > 0; i++)
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
            for (int i = 0; i < pl.inventory.Length && stack > 0; i++)
            {
                Item it = pl.inventory[i];
                if (it.type == type && it.stack > 0 && it.stack < maxStack)
                {
                    int can = Math.Min(maxStack - it.stack, stack);
                    it.stack += can; stack -= can; added += can;
                }
            }
            for (int i = 0; i < pl.inventory.Length && stack > 0; i++)
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

        private static string Give(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int type = Json.Int(p, "type", 0);
            int stack = Math.Clamp(Json.Int(p, "stack", 1), 1, 9999);
            if (!ValidItemType(type)) return Json.Error($"invalid item type {type}: 不是有效的物品 ID");
            Player pl = P;
            // Put it in the inventory. QuickSpawnItem drops it on the floor and relies on the
            // pickup delay, so "give then craft" used to fail intermittently.
            int added = AddToInventory(pl, type, stack);
            int dropped = stack - added;
            if (dropped > 0) pl.QuickSpawnItem(new EntitySource_Misc("dsh-bridge"), type, dropped);
            return Json.Ok(new { type, stack, name = Lang.GetItemNameValue(type), added, dropped });
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
                });
            }
            return Json.Ok(new { radius = r, count = near.Count, npcs = near.ToArray() });
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
