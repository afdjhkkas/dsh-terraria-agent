# 生成物：mod 命令与插件工具清单

> 本文件由 `node scripts/kb-sync.mjs` 从 `DshTerrariaBridge/Commands.cs` 与
> `index.js` 生成。**不要手改**；要改就改代码再重新生成。

## mod 桥命令（99 个）

| 命令 | 参数 |
|---|---|
| `accessory` | `off` `slot` `from` |
| `api` | `type` `member` |
| `attack` | `who` `r` `ms` `interval` `slot` |
| `blood` | — |
| `bloodmoon` | — |
| `bobber` | — |
| `boss` | — |
| `buy` | `slot` `shopId` |
| `capture` | `x` `y` `who` `ms` |
| `castline` | `x` `y` `ms` |
| `chat` | `text` |
| `chatpoll` | `since` `max` |
| `chatpost` | `text` |
| `chest` | `action` `slot` `count` `invSlot` |
| `climb` | `dir` `ms` `lay` |
| `craft` | `item` `times` |
| `down` | — |
| `drop` | `slot` `count` |
| `duplicate` | `item` `count` |
| `eclipse` | — |
| `equip` | `slot` |
| `event` | — |
| `fill` | `x1` `y1` `x2` `y2` `type` |
| `find` | `type` `x1` `x2` `y1` `y2` `max` |
| `fly` | `ms` |
| `follow` | `off` `player` `guard` `range` |
| `frost` | — |
| `frostmoon` | — |
| `goblin` | — |
| `grab` | `r` |
| `grapple` | `x` `y` `ms` |
| `gravity` | — |
| `guard` | `off` `ms` `r` |
| `heal` | — |
| `help` | — |
| `homeroom` | `x` `y` `who` |
| `hook` | — |
| `hookdebug` | — |
| `housediag` | `x` `y` |
| `housing` | `x` `y` |
| `humaninput` | — |
| `idle` | — |
| `idmap` | `path` |
| `interact` | `x` `y` |
| `inventory` | — |
| `items` | `r` |
| `jump` | `ms` |
| `keyevents` | `since` `max` `clear` |
| `left` | — |
| `liquidop` | `x` `y` `ms` |
| `map` | `step` |
| `martian` | — |
| `mine` | `x` `y` `ms` |
| `minion` | `slot` |
| `mirror` | — |
| `mount` | `action` `slot` |
| `move` | `dir` `ms` |
| `needs` | `x` `y` |
| `nodig` | `action` `x1` `y1` `x2` `y2` |
| `npc` | `r` |
| `path` | `x` `y` `ms` `dig` |
| `ping` | — |
| `pirate` | — |
| `place` | `x` `y` `type` `ms` |
| `players` | — |
| `pumpkin` | — |
| `pumpkinmoon` | — |
| `quest` | — |
| `recipes` | `q` `limit` |
| `reel` | `ms` |
| `removeblock` | `x` `y` `ms` |
| `research` | `slot` |
| `right` | — |
| `roomcheck` | `x` `y` `npc` `mode` |
| `save` | — |
| `scan` | `x` `y` `r` `walls` `format` |
| `select` | `slot` `item` |
| `sell` | `slot` |
| `shoot` | `x` `y` `slot` `ms` |
| `shop` | `shopId` `npc` |
| `shopdebug` | `npc` |
| `shops` | `want` |
| `snowman` | — |
| `snowmoon` | — |
| `splitstack` | `slot` `count` |
| `startevent` | `event` |
| `state` | — |
| `stop` | — |
| `talk` | `npc` |
| `tile` | `x` `y` |
| `tiledata` | `type` `style` `alternate` |
| `trash` | `slot` `count` |
| `travel` | `stop` `dir` `distance` `ms` `dig` |
| `travelstatus` | — |
| `unhook` | — |
| `up` | — |
| `use` | `slot` `right` `ms` `x` `y` |
| `walkto` | `x` `y` `tol` `ms` |
| `wall` | `x` `y` `type` `remove` `ms` |

## DSH 插件工具（27 个）

| 工具 | 作用 |
|---|---|
| `terraria_attack` | Attack a hostile NPC for a while. Omit who to pick the nearest one.  |
| `terraria_bridge` | Call ANY bridge command by name with a raw params object. Use this for capabilities that have no dedicated tool  |
| `terraria_chat` | Print a message in the local client chat (Main.NewText only: it is not broadcast to a server). Truncated at 400 characte |
| `terraria_chat_post` | Post a line into the local Terraria chat box so the player reads it in game (Main.NewText, client only).  |
| `terraria_chat_wait` | Block until a NEW line arrives in the Terraria chat box (i.e. something the player typed), or until timeoutMs.  |
| `terraria_chest` | Work with the chest you are standing next to (within 6 tiles):  |
| `terraria_craft` | Craft by output item id using the inventory and nearby stations (workbench / furnace / anvil must be within ~1 tile).  |
| `terraria_equip` | Equip the armour or accessory sitting in one bag slot, then read the equipment slots and defence back.  |
| `terraria_exception` | 全局异常处理体系（与任务无关，所有任务每一步都要走一遍）。  |
| `terraria_follow` | Follow another player: walk while further than range tiles, stand still when close.  |
| `terraria_idle` | Stand still: cancel follow, paths, walks, movement and attack holds. Nothing is erased from the world. |
| `terraria_inventory` | Read the inventory, the equipped armor/accessories (so you can tell whether you are armoured or have wings) and the sele |
| `terraria_jump` | Jump once from the ground, then report the height change. Airborne it only holds the jump key, so wings  |
| `terraria_kb` | Knowledge base for the Terraria agent.  |
| `terraria_mine` | Mine or chop one tile the way a player does: it selects the pickaxe (axe for trees), aims at the tile and  |
| `terraria_move` | Hold a movement direction, then report how far the player actually went (from/to/dx/dy).  |
| `terraria_npcs` | List active NPCs near the player with who/type/name/tile/life/friendly/boss/hostile.  |
| `terraria_observe` | Read the current world and player: position, health, mana, held item, buffs, weather, time and the live/last attack stat |
| `terraria_place` | Place a tile the way a player does: it finds that block in your inventory, selects it, aims and places it -  |
| `terraria_place_object` | Place a multi-cell furniture (chest, furnace, anvil, table, chair, door, bed) at its ROOT cell - the top-left cell of it |
| `terraria_players` | List every active player in the world: index, name, dead, hp, tile position, team, and whether it is the local player or |
| `terraria_scan` | Read a square region of tiles around a tile coordinate.  |
| `terraria_stop` | Clear every movement, jump, use-item and attack hold issued through the bridge.  |
| `terraria_tile` | Inspect exactly one tile: hasTile, type (-1 = empty), name, wall, liquid, slope, half-brick, actuator.  |
| `terraria_tiledata` | Furniture footprint and anchor, straight from the game (TileObjectData).  |
| `terraria_use_item` | Use the selected item for a moment, optionally selecting a slot first (right:true for right-click / tile interaction).  |
| `terraria_wall` | Place or remove a background wall like a player: it takes the wall item (or a hammer when removing), aims and uses it.  |
