using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
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
                case "place": return Place(p);
                case "move": return Move(p);
                case "jump": return Jump(p);
                case "use": return Use(p);
                case "attack": return Attack(p);
                case "spawn_npc": return SpawnNpc(p);
                case "stop": BridgeSystem.ClearControls(); return Json.Ok(new { stopped = true });
                case "teleport": return Teleport(p);
                case "inventory": return Inventory();
                case "craft": return Craft(p);
                case "give": return Give(p);
                case "npc": return Npcs(p);
                case "chat": return Chat(p);
                case "help": return Help();
                default: return Json.Error($"unknown command: {cmd}（可用 ping/state/scan/tile/mine/place/move/jump/use/attack/spawn_npc/stop/teleport/inventory/craft/give/npc/chat）");
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

        private static string Ping() => Json.Ok(new
        {
            pong = true,
            version = DshTerrariaBridge.BridgeVersion,
            inWorld = !Main.gameMenu,
            tick = Main.GameUpdateCount,
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
                ["screen"] = new { w = Main.screenWidth, h = Main.screenHeight },
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
                    grounded = pl.velocity.Y == 0f,
                    selected = pl.selectedItem,
                    held = new { type = pl.HeldItem.type, name = pl.HeldItem.Name, stack = pl.HeldItem.stack },
                    buffs = Buffs(pl),
                };
                data["attack"] = BridgeSystem.AttackStatus();
            }
            else
            {
                data["hint"] = "还在主菜单：先用菜单进世界（单人模式 → 选人物 → 选世界），之后 world/player 才有值。";
            }

            return Json.Ok(data);
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

            int x0 = cx - r, y0 = cy - r, w = r * 2 + 1, h = r * 2 + 1;
            var rows = new List<int[]>(h);
            for (int j = 0; j < h; j++)
            {
                var row = new int[w * (withWalls ? 4 : 2)];
                for (int i = 0; i < w; i++)
                {
                    int x = x0 + i, y = y0 + j;
                    if (!InBounds(x, y)) continue;
                    Tile t = Main.tile[x, y];
                    int k = i * (withWalls ? 4 : 2);
                    row[k] = t.HasTile ? t.TileType : 0;
                    if (withWalls)
                    {
                        row[k + 1] = t.WallType;
                        row[k + 2] = t.LiquidAmount;
                        row[k + 3] = (t.Slope == SlopeType.Solid ? (t.IsHalfBlock ? 5 : 0) : (int)t.Slope + 1);
                    }
                    else
                    {
                        row[k + 1] = t.LiquidAmount;
                    }
                }
                rows.Add(row);
            }

            return Json.Ok(new
            {
                x0,
                y0,
                w,
                h,
                stride = withWalls ? 4 : 2,
                fields = withWalls ? new[] { "type", "wall", "liquid", "shape" } : new[] { "type", "liquid" },
                center = new[] { cx, cy },
                playerTile = pl != null && pl.active ? new[] { (int)(pl.Center.X / 16f), (int)(pl.Center.Y / 16f) } : null,
                tiles = rows,
                legend = "type=0 空；shape 0=实心/1=半砖/2..5=斜坡；liquid 0-255",
            });
        }

        private static string TileAt(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", 0), y = Json.Int(p, "y", 0);
            if (!InBounds(x, y)) return Json.Error($"out of world bounds: {x},{y}");
            Tile t = Main.tile[x, y];
            return Json.Ok(new
            {
                x,
                y,
                hasTile = t.HasTile,
                type = t.HasTile ? t.TileType : 0,
                name = t.HasTile ? TileID.Search.GetName(t.TileType) : "",
                wall = t.WallType,
                liquid = t.LiquidAmount,
                slope = (int)t.Slope,
                halfBlock = t.IsHalfBlock,
                actuator = t.HasActuator,
                frameX = t.TileFrameX,
                frameY = t.TileFrameY,
            });
        }

        private static string Mine(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            if (x < 0 || y < 0) return Json.Error("mine 需要 x,y（方块坐标）");
            if (!InBounds(x, y)) return Json.Error($"out of world bounds: {x},{y}");
            bool noItem = Json.Bool(p, "noItem", false);
            Tile tile = Main.tile[x, y];
            int before = tile.HasTile ? tile.TileType : 0;
            int wallBefore = tile.WallType;
            if (before == 0 && wallBefore == 0)
                return Json.Ok(new { ok = false, x, y, before, after = 0, removed = false, note = "这一格本来就是空的" });
            // 1.4.4 的 WorldGen.KillTile 返回 void：成败靠前后回读判断
            if (before != 0) WorldGen.KillTile(x, y, false, false, noItem);
            if (wallBefore != 0 && Main.tile[x, y].WallType != 0) WorldGen.KillWall(x, y, false);
            Tile tileAfter = Main.tile[x, y];
            int after = tileAfter.HasTile ? tileAfter.TileType : 0;
            bool removedTile = before != 0 && after == 0;
            bool removedWall = wallBefore != 0 && tileAfter.WallType == 0;
            return Json.Ok(new { ok = removedTile || removedWall, x, y, before, after, wallBefore, wall = tileAfter.WallType, removed = removedTile || removedWall });
        }

        private static string Place(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            int x = Json.Int(p, "x", -1), y = Json.Int(p, "y", -1);
            int type = Json.Int(p, "type", TileID.Dirt);
            int style = Json.Int(p, "style", 0);
            bool forced = Json.Bool(p, "forced", true);
            if (x < 0 || y < 0) return Json.Error("place 需要 x,y");
            if (!InBounds(x, y)) return Json.Error($"out of world bounds: {x},{y}");
            bool ok = WorldGen.PlaceTile(x, y, type, false, forced, -1, style);
            int after = Main.tile[x, y].HasTile ? Main.tile[x, y].TileType : 0;
            return Json.Ok(new { ok, x, y, requested = type, after });
        }

        private static string Teleport(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            float x = Json.Float(p, "x", float.NaN), y = Json.Float(p, "y", float.NaN);
            if (float.IsNaN(x) || float.IsNaN(y)) return Json.Error("teleport 需要 x,y（方块坐标）");
            Player pl = P;
            pl.position = new Vector2(x * 16f, y * 16f - pl.height);
            pl.velocity = Vector2.Zero;
            pl.fallStart = (int)(pl.position.Y / 16f);
            return Json.Ok(new { x, y, tile = new[] { MathF.Round(pl.Center.X / 16f, 1), MathF.Round(pl.Center.Y / 16f, 1) } });
        }

        /* ── 动作 ─────────────────────────────────────────────────────── */

        private static string Move(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            string dir = (Json.Str(p, "dir", "right") ?? "right").ToLowerInvariant();
            int ms = Math.Clamp(Json.Int(p, "ms", 500), 1, 15000);
            ulong until = BridgeSystem.Now + BridgeSystem.Ticks(ms);
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
            return Json.Ok(new { dir, ms, untilTick = until, tick = BridgeSystem.Now });
        }

        private static string Jump(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int ms = Math.Clamp(Json.Int(p, "ms", 150), 1, 5000);
            BridgeSystem.JumpUntil = BridgeSystem.Now + BridgeSystem.Ticks(ms);
            return Json.Ok(new { jumpMs = ms, untilTick = BridgeSystem.JumpUntil });
        }

        private static string Use(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            Player pl = P;
            if (pl.dead) return Json.Error("玩家已死亡，请先复活");
            int slot = Json.Int(p, "slot", -1);
            if (slot >= 0 && slot < pl.inventory.Length) pl.selectedItem = slot;
            bool right = Json.Bool(p, "right", false);
            int ms = Math.Clamp(Json.Int(p, "ms", 220), 1, 5000);
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
            return Json.Ok(new { slot = pl.selectedItem, right, ms, held = new { type = pl.HeldItem.type, name = pl.HeldItem.Name } });
        }

        private static string Attack(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
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
                npc = target == null ? null : new { name = target.FullName, life = target.active ? target.life : 0, lifeMax = target.lifeMax },
            });
        }

        private static string SpawnNpc(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int type = Json.Int(p, "type", NPCID.BlueSlime);
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
            return Json.Ok(new { selected = pl.selectedItem, count = slots.Count, items = slots.ToArray() });
        }

        private static int CountItem(Player pl, int type)
        {
            int n = 0;
            foreach (Item it in pl.inventory)
                if (it != null && it.type == type) n += it.stack;
            return n;
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
            int times = Math.Clamp(Json.Int(p, "times", 1), 1, 20);

            Player pl = P;
            Recipe.FindRecipes();
            Recipe recipe = null;
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                if (Main.recipe[i].createItem.type == itemType) { recipe = Main.recipe[i]; break; }
            }
            if (recipe == null) return Json.Error($"没有找到产物为 {itemType} 的配方");

            // 配方组（任意木板/任意铁锭之类）暂不支持：宁可明确拒绝，也不猜错材料
            if (recipe.acceptedGroups != null && recipe.acceptedGroups.Count > 0)
                return Json.Error("这个配方含'配方组'（任意木材/任意矿石之类），暂不支持自动合成，请用游戏内界面合成");

            // 统计需求（同一物品可能出现在多个槽位）
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
            for (int round = 0; round < times; round++)
            {
                foreach (var kv in need)
                {
                    if (CountItem(pl, kv.Key) < kv.Value)
                        return crafted > 0
                            ? Json.Ok(new { item = itemType, name = Lang.GetItemNameValue(itemType), crafted, reason = "材料用完了" })
                            : Json.Error($"材料不足：需要 {Lang.GetItemNameValue(kv.Key)} x{kv.Value}，背包里有 {CountItem(pl, kv.Key)}");
                }
                if (!HasStation(pl, recipe))
                    return crafted > 0
                        ? Json.Ok(new { item = itemType, name = Lang.GetItemNameValue(itemType), crafted, reason = "附近没有工作站" })
                        : Json.Error("附近没有这个配方需要的工作站（工作台/熔炉/铁砧…）");

                // 扣材料
                foreach (var kv in need)
                {
                    int left = kv.Value;
                    for (int i = 0; i < pl.inventory.Length && left > 0; i++)
                    {
                        Item it = pl.inventory[i];
                        if (it.type != kv.Key) continue;
                        int take = Math.Min(it.stack, left);
                        it.stack -= take; left -= take;
                        if (it.stack <= 0) it.SetDefaults(0);
                    }
                }

                int per = Math.Max(1, recipe.createItem.stack);
                int added = AddToInventory(pl, itemType, per);
                if (added <= 0) { pl.QuickSpawnItem(new EntitySource_Misc("dsh-bridge"), itemType, per); added = per; }
                crafted += added;
            }

            return Json.Ok(new
            {
                item = itemType,
                name = Lang.GetItemNameValue(itemType),
                crafted,
                times,
                perCraft = Math.Max(1, recipe.createItem.stack),
            });
        }

        private static string Give(JsonElement p)
        {
            if (!InWorld(out string err)) return Json.Error(err);
            if (!HasPlayer(out string perr)) return Json.Error(perr);
            int type = Json.Int(p, "type", 0);
            int stack = Math.Clamp(Json.Int(p, "stack", 1), 1, 9999);
            if (type <= 0) return Json.Error("give 需要 type");
            P.QuickSpawnItem(new EntitySource_Misc("dsh-bridge"), type, stack);
            return Json.Ok(new { type, stack, name = Lang.GetItemNameValue(type) });
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
