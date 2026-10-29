# Terraria mechanics cheat sheet (for this agent)

Everything here is either **verified on this machine** (measured against tModLoader
`2026.8.3.0` / Terraria 1.4.4 with the bridge live) or **cited from the official wiki**.
`[measured]` = observed in-game through the bridge; `[wiki]` = from terraria.wiki.gg.

Keep this file next to the code: every future test session needs these numbers.

## Coordinates and world sizes

- 1 tile = 16 px. `x` grows right, `y` grows **down**. Player centre = `Player.Center`.
- World sizes `[wiki]`: small 4200×1200, medium 6400×1800, large 8400×2400.
  A small world is 4200×1200 tiles (measured in this repo's test run); the values
  are a property of the world, not of the project.
- `Main.maxTilesX/maxTilesY` are the authoritative bounds; `Main.tile[x, y]` is
  invalid outside them.

## Tile encoding (the convention this bridge uses)

- **`type = -1` means empty.** `type = 0` is **Dirt**.
  > This is the single most important thing to get right. Terraria's `TileID.Dirt == 0`,
  > so "0 = air" silently makes dirt invisible to scans and unmineable.
- `shape`: `0` solid · `1` half brick · `2..5` slopes
  (`SlopeType`: Solid=0, SlopeDownLeft=1, SlopeDownRight=2, SlopeUpLeft=3, SlopeUpRight=4).
  Watch out: a naive `SlopeType + 1` collides half bricks (5) with `SlopeUpRight` (5).
- `liquid`: `LiquidAmount` 0..255; `LiquidType` water/lava/honey/shimmer.
- `wall`: 0 = no wall. Walls are a **separate** layer: they drive housing validity and
  are not removed by breaking the tile in front of them.

Common tile ids `[wiki]`: 0 Dirt · 1 Stone · 2 Grass · 3 Plants · 4 Torch · 5 Tree ·
6 Iron Ore · 7 Copper Ore · 8 Gold Ore · 9 Silver Ore · 10 Closed Door · 30 Wood? —
always confirm with `tile` rather than trusting a table from memory.

## Player physics `[measured]`

- Horizontal movement and jumping must be measured from real held-key input. Earlier
  velocity-forcing measurements are historical debugging data, not permitted operations.
- Gravity, friction, collision, slopes, platforms, mounts and status effects remain owned
  by vanilla; the bridge emits direction/jump triggers and reads the resulting state.
- **Wings** `[wiki]`: with wings equipped, *holding jump after the jump ends starts flight*.
  A long `jump` on a winged character is legitimate flight, not a bug. The bridge sets
  `controlJump` and lets vanilla decide; it does not fake flight.
- Directly overriding `velocity` is forbidden by the bridge contract because it bypasses
  engine effects and makes an action impossible to verify as a player input.

## Combat `[wiki]`

- Net damage = `floor(attack - defense × f)`, `f` = 0.5 classic / 0.75 expert / 1.0 master;
  **minimum 1** whenever a hit lands.
- Crits: chance from `GetTotalCritChance(DamageClass)`, multiplier ×2.
- Real damage uses the player pipeline: `GetTotalDamage(class).ApplyTo(item.damage)`
  (armor, accessories, buffs, prefixes). Passing raw `item.damage` ignores all of it.
- **Invincibility frames**: 10 ticks (≈167 ms) of *global* invincibility is the default
  after a hit from a piercing source. Melee hits are **affected by** global i-frames but do
  not apply them. A hit inside the i-frame window does 0 damage — so count hits by comparing
  `npc.life` before/after, not by counting attempts.
- `NPC.CanBeChasedBy()` means "damageable and chaseable", which is what auto-target should
  use; it is `false` for invulnerable phases (e.g. some boss states), so it is *not* a
  reliable "is hostile" test on its own.

## Item use `[measured]`

- `use` sets `itemAnimation`/`itemTime` immediately; with an iron shortsword
  `itemAnimation` went 8 → 4 → 0 over ~150 ms, i.e. `useAnimation ≈ 8-10` ticks.
- `useTime` gates how often an item can be used; ignoring it turns any weapon into a
  no-cooldown auto-attack.

### tModLoader hook pitfall `[measured]`

`ModPlayer.PreItemCheck()` returns a bool and **false cancels `ItemCheck_Inner`**. Returning
false "to be safe" silently disables every item use in the game: no swings, no animations,
no axe chopping, and no drops (mining still appeared to work only if it bypassed ItemCheck
through `Player.PickTile`). Return **true** to let the item check run.

Ordering worth knowing when driving a player from a mod: `Player.Update` calls
`PreUpdate`, `ResetControls`, then (only when `Main.hasFocus`) `TriggersSet.CopyInto` and
`ModPlayer.SetControls`, then `ItemCheckWrapped -> ItemCheck -> PreItemCheck ->
ItemCheck_Inner`. So `PreItemCheck` is the last hook that can still influence the vanilla
item use, and in single player the whole frame is skipped with `gamePaused = true` when the
window loses focus.

For bridge movement, the measured safe sequence is `ModSystem.PostUpdateInput` recording the
native `Triggers.Current` state, then `DshControlPlayer.SetControls` resolving path/walk intents
and writing the player's `controlLeft/controlRight/controlJump` flags. Calling the movement
resolver from `PostUpdateInput` or relying on Triggers alone is too early/ineffective in this
build: the trigger diagnostic is true while the control flags remain false and the player does
not move.

## Recipes `[measured]`

- Lookup by output type works: `Main.recipe[i].createItem.type == itemType`.
- Station check: `recipe.requiredTile` (list of tile ids) must be nearby (vanilla uses
  `adjTile` range, the bridge searched ±5 tiles).
- **Recipe groups** (`recipe.acceptedGroups`, e.g. "any wood"): a recipe that accepts a
  group must consume *any* member of the group. Wooden Arrows (item 40) are a group recipe —
  rejecting groups makes the most basic ammo uncraftable.
- **Conditions** (`recipe.Conditions`, 1.4.4): e.g. near water, in graveyard, hardmode,
  time of night. Ignoring them lets the caller craft things the game would not allow.
- `craft` clamps `times` to 20 in the mod; the tool accepts up to 99.

## Pausing and focus `[wiki + measured]`

- Terraria has settings **"Pause when unfocused"** (Settings → General; single-player only)
  and **"auto pause"** (pauses while the inventory / NPC dialogue is open). Either sets
  `Main.gamePaused`, and the world stops ticking.
- The bridge keeps answering while paused, so a tick-driven command would report success and
  do nothing. This is why every tick-driven command checks `Main.gamePaused` first and why
  `state`/`ping` expose `paused` and `hasFocus`.
- Measured: with this machine's settings, **minimising** the window did **not** pause — tick
  kept advancing and movement still worked. Pause state is user-configurable, so always ask
  `ping`/`state` rather than assuming.

## Journey mode `[measured]`

In Journey mode (`GameMode: 3`) duplication, a difficulty slider and time/weather control are
available, and creative item/enemy spawning is allowed. That makes it convenient for tests —
but a Journey run is not the same as a normal playthrough, so do not assume those affordances
exist when judging whether an action was legitimately earned.

## Placing and digging in play `[measured]`

- **Support**: a block can only be placed where it touches an existing block, or on
  a solid/platform surface. Mid-air placement is refused. The game's own answer to
  "is this support?" is `Main.tileSolid[type]` (surfaced by the bridge as `tile.solid`):
  plants, grass tufts, tombstones and furniture are tiles but are **not** support.
- **Reach**: tile interaction is about 5 tiles from the player centre; farther asks
  are refused rather than silently ignored.
- **Furniture** (workbench, chair, door, table) is a multi-tile object: it must be
  placed through the object placement path (`WorldGen.PlaceObject`), not the
  single-tile one (`WorldGen.PlaceTile`).
- **Player body**: the hitbox is 20x42 px, i.e. it occupies **four tile rows**
  around the centre (2 above, 1 below). Movement checks that only look at
  `round(centre.y)` miss the tile above the head, which is the classic
  "I am walking into a wall and do not know it" failure.
- **Digging a path**: a player clears the blocking tiles of their body rows,
  top-down, then walks. A move that produces no displacement is almost always a
  solid tile in one of those rows, not a broken game.

## Which tool a tile needs is a game fact: `Main.tileAxe` `[measured]`

Chopping trees with the pickaxe does nothing, and the agent concluded "this tile cannot be
broken". The real cause: tool selection only treated `TileID.Trees`/`PalmTree` as axe work, so
vanity trees and other tree-like tiles were hit with a pickaxe forever. The game already exposes
the answer - `Main.tileAxe[tileType]` - and using it makes every tree choppable.

When a tile will not break, check in this order: is the right tool in hand (log the held item the
swing actually used), is the cell within reach, is the aim actually on that cell (`state.input.aim`
vs `target`), and only then suspect the tile itself.

## Underground: light first, then dig `[measured]`

Digging without light is not just unpleasant, it removes the agent's only way to notice danger:
the screen is black and a screenshot shows nothing. A player carries torches and puts one down
every few tiles; the agent must do the same, or it cannot see water, lava, or a cave with
monsters in it.

Survival readings that must be watched while working, not after:

- `breath` (drowning under water) - `state` now exposes `breath/breathMax`,
- `lavaWet`, `wet`,
- `hpPercent` and `dead`.

Reactive-only watching (waiting for hp to drop) is too late for lava and for drowning; check the
readings *and* look at the screen between steps.

## Dig away from the base `[measured]`

Digging straight down from inside a house punches through its floor and voids the room's housing
check. Any mining shaft starts at least ~15 tiles from the nearest player-built wall; repair
immediately (place the missing floor tiles back, then confirm `housing` reports true again).

## Never add an API that can create items `[measured]`

A `WorldGen.PlaceObject` shortcut was added to place furniture reliably. It worked - and it also
placed a **Demon Altar**, an object no player can obtain, because it never checks the inventory.
That is cheating with extra steps and it destroys trust in every result afterwards. It was
removed; the rule that replaced it:

- **placing** must go through the player's own item use (item in inventory, consumed, reach,
  support), and
- **crafting** may use the recipe API, but only when the player really carries the ingredients.

Multi-tile furniture anchors, measured: a 3x2 object (furnace, altar) is placed with the
**bottom-centre** tile as the cursor target, a 2x1 object (workbench, anvil) with the
**bottom-left**.

## One product, several recipes `[measured]`

An iron bar has three recipes (from ore, from iron fence, from wrought iron fence). Crafting code
that takes the *first* recipe for a product reports "missing 铁栅栏 x4" while nine iron ore sit in
the bag. Always pick the first recipe whose ingredients are actually present.

Item ids that were wrong by guesswork and are now looked up: 铁锭 22 (铁矿 x3), 铁砧 35
(铁锭 x5), 熔炉 item 33 / tile 17, 木桌 32, 工作台 36 / tile 18, 木椅 34 / tile 15.

## Persistence: only the game's own path survives `[measured]`

Two independent things silently destroyed work in this project, both worth checking first
when "it worked a second ago and now it is gone":

1. **Forced placement is not real.** `WorldGen.PlaceTile(..., forced: true)` (and
   `WorldGen.PlaceWall` called directly) put a tile in the map, and it even reads back
   correctly, but after the world is reloaded the tile **is gone**. Placing through the
   player's own item use (`ItemCheck` -> `PlaceThing` -> `WorldGen.PlaceTile`) produces a
   properly framed, valid tile that persists. Measured: 3 blocks + 1 wall placed via the item
   path were all still there after a restart; 2 blocks placed forced were both gone.
2. **Killing the host process loses the world.** `CloseMainWindow()` on the `dotnet.exe`
   console host terminates the process before the game saves. Post `WM_CLOSE` to the **game
   window** (the `SDL_app` window of that process) instead, and let it exit by itself; then
   tile changes persist.

Rules that follow from the above:

- Every world change (mine, place, wall) goes through the game's item use; never edit tiles
  directly except for throwaway tests.
- A placement needs support: blocks need an adjacent block/solid ground, walls need an
  adjacent block or wall. Mid-air requests are refused (and are not charged).
- After building something worth keeping, close the game window properly before restarting.

## Housing (what makes an NPC move in) `[wiki]`

A room is valid when: interior area is at least 60 tiles (and not absurdly huge),
every interior tile has a **player-placed** background wall, the space is enclosed
by blocks with a door or platform entrance, and it contains a light source
(torch), a comfort item (chair/bed/bench) and a flat surface item
(table/dresser/work bench).

## Doors `[measured]`

A door needs, at its column:

- a **solid block directly below** the bottom door tile (the floor),
- **three free tiles** for the door itself (bottom, middle, top),
- a **solid block directly above** the top door tile.

Miss the block above and placement fails **silently**: the swing plays, nothing is consumed,
nothing appears. This cost an hour of blind retries before it was pointed out; it is the
single most useful "common sense" rule for housing.

## Reach is smaller than it looks `[measured]`

Tile interaction is refused beyond roughly five tiles from the player centre. The bridge may
accept a request up to seven; the game then silently does nothing (no swing result, no item
consumed). Build and dig within **four** tiles and walk between phases.

## Building layout: leave a walkway `[measured]`

Rooms placed back-to-back share a wall, and a door in that wall opens into the neighbour's
wall: the player gets trapped in the doorway, and any hole punched to escape **merges the two
rooms into one space**, which costs a house slot. Leave about three tiles of walkway between
buildings and put each door on the walkway side.

## Jumping beats tunnelling - check the head clearance first `[measured]`

A normal jump clears about five tiles. If a jump seems to fail, the usual cause is a low
ceiling (often blocks the agent itself placed): measure the tile rows above the player before
concluding "cannot jump". Digging a tunnel to get around a two-tile step is not what a player
does and wastes a lot of time.

## Solid blocks block, platforms pass `[wiki]`

Wood/stone blocks are collision-solid: the player cannot walk or jump *through* them, so a
staircase made of blocks in a three-tile-wide walkway wedges the player in place. Platforms
(`tile 19`) are passable from below and standable on top, which is why they are the right
material for ladders, roof entrances and anything the player must pass.

## Clear the plot before building `[measured]`

Trees and plants inside or in front of a plot are not decoration: a tree trunk is solid, it
blocks walking (so distant cells fall out of reach and every placement fails "for no reason"),
and it occupies interior tiles. Chop every tree in and around the plot first, then clear
plants, then build.

## A platform in the roof is a cheap entrance `[measured]`

Platforms (`tile 19`) count as a room entrance for housing and can be jumped through from
below, so a couple of platform tiles in the ceiling give the agent a way in and out without
touching the door. That is much more reliable than navigating doorways, especially when
buildings are close together.

## You cannot place a block where you are standing `[measured]`

A placement whose target cell overlaps the player's own hitbox is refused silently. Step one or
two tiles aside first; this looks identical to a support failure until you notice the player is
standing in the cell.

## "I cannot move" is almost always un-dug terrain `[measured]`

A player is 20x42 px, i.e. four tile rows (two above the centre, one below). When a move
produces no displacement, the cause is a solid tile in one of those rows **in the direction of
travel, including the diagonal above it** (a two-tile step needs both the step and the head
clear). Dig every one of them before moving again; clearing a single column is not enough and
looks like a physics bug when it is not.

## NPCs move in during the day `[wiki]`

A valid room is not enough at night: town NPCs arrive while `dayTime` is true. So "the room
is valid but nobody came" is usually just the clock.

## Progression route to the end `[wiki]`

Wood and workbench -> wooden/iron gear -> valid house -> NPCs -> life crystals ->
Eye of Cthulhu -> Eater of Worlds / Brain of Cthulhu -> Skeletron -> Queen Bee ->
Wall of Flesh -> hardmode mechanical bosses -> Plantera -> Golem -> lunar events
(4 pillars) -> **Moon Lord**. Optional side bosses (King Slime, Queen Slime,
Duke Fishron, Empress of Light, Deerclops) can be skipped or done for gear.

## Testing notes for this repo

- Launch straight into a world, no menus:
  `D:\STEAM\steamapps\common\tModLoader\dotnet\dotnet.exe tModLoader.dll -world <abs .wld> -player <abs .plr>`
  with the tModLoader folder as cwd. Note: on this install the `-world` argument was
  **ignored** and the last-played world loaded instead — always confirm with `state.world.name`.
- Launching that way does not need Steam; the log says "Launched outside steam client" and
  the game runs anyway.
- The bridge listens on `127.0.0.1:47777` as soon as mods finish loading (menu is enough for
  `ping`, but `state`/`scan` need a loaded world).
- tModLoader worlds and players live in
  `Documents/My Games/Terraria/tModLoader/{Worlds,Players}` — **not** the vanilla
  `Documents/My Games/Terraria/{Worlds,Players}` folder.
- The mod must be rebuilt from `ModSources/DshTerrariaBridge` with the .NET 8 SDK
  (`%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe build -c Release`) and the game must be
  **closed** first (tModLoader locks the .tmod).

## Sources

- Wings / flight: https://terraria.wiki.gg/wiki/Wings
- Invincibility frames: https://terraria.wiki.gg/wiki/Invincibility_frame
- Defense formula: https://terraria.wiki.gg/wiki/Defense
- World size: https://terraria.wiki.gg/wiki/World_size
- Settings (pause options): https://terraria.wiki.gg/wiki/Settings
- Tile IDs: https://terraria.wiki.gg/wiki/Tile_IDs
- Recipe groups / conditions: https://github.com/tModLoader/tModLoader/wiki/Basic-Recipes
## 顶墙判据（本档实测）

- `move right` 800ms → 坐标完全不变，但 `velocity` 仍是 `[2.8, 0]`：速度有、位移被碰撞吃掉 = **正顶着墙**。
- 同位置 `jump` 180ms + `move right` 700ms → 位移 6.1 格（越过那格石头后恢复正常行走）。

## 建房实测记录（本档，2026-10-26）

- 结构尝试 3 处：内部方块被占 15~26 格，`mine` 4 连成功率仅 2/15 → 玩家进不去是主因。
- 铺墙成功率：内部**全空**时 19~20/32；内部有方块时 0/32（全部报「方块挡着」）。
- 家具：椅子/火把/工作台在"下方有支撑 + 距离 ≤ 3 格"时都能放下（各 2/2）。
- `housediag`：直到 `enclosed:true` 才值得看后面的门/光/舒适度；否则一律 `tiles:9001`（洪水泄到边界）。

## 跳跃曲线（本档实测，2026-10-26，同一块平地反复跳）

| 按住 | 顶点高度（格） |
|---|---|
| 20ms | 1.8 |
| 40ms | 2.1 |
| 60ms | 2.4 |
| 100ms | 3.0 |
| 150ms | 3.8 |
| 200ms | 4.7 |
| 250ms | 5.6 |
| 400ms 及以上 | 6.1（封顶） |

- 真人同场演示的满跳也是 6.1 格（`275.7 → 269.6`），与桥的原生跳跃一致。
- 桥的 `jump{tiles:N}` 用这张表反查时长（表在 `Commands.MsForTiles`），并加一 tick 余量；
  实测 `tiles:2→1.8`、`3→3.0`、`4→4.1`、`5→5.3`。
- **窗口失焦时整列为 0 格**：同一份代码在前台跳出上表数值、后台一格不跳（输入层被引擎忽略）。

## 真人对照：挖掘每格耗时与走速（本档实测，2026-10-26）

用 `humaninput` 以 45ms/帧录制真人操作（2491 帧 / 154 秒），按"按住左键的连续段"切
分，段内按鼠标目标格再切分，得到单格耗时：

| 操作 | 实测 | 备注 |
|---|---|---|
| 铁镐挖一格土/石 | **430–550ms**（26–33 tick） | 单格段 433ms / 466ms；连续挖时每格均值 ~500ms |
| 连续挖（按住不放） | 一格接一格，中间几乎不抬键 | 1.97 秒的段里换了 4 个目标格 |
| 走路（陆地，平地/微坡） | **7–9 格/秒**（中位 ~8） | 22 段样本，快的 10–12 段多为下坡或落体 |
| 铁镐能挖的方块 | 土、石都能挖 | 手持 `heldType=1`（铁镐） |

对照桥自己的参数：

- `mineTile` 默认 `ms=1300`，是真人单格耗时的 **约 3 倍**；盲挖循环用 `ms=90` 靠多帧累积，
  单格实际落在 3–5 次调用。
- 走路速度 6.3–11 格/秒，与真人 7–9 一致，**不需要调**。
- 真人挖一格的速度几乎不随"土/石"变化（430 与 466ms 同量级），说明铁镐对这两种方块都是
  2–3 下；想快只能换镐，不是调参数。

改完的实测（同一块石头，铁镐）：`mineTile` 单格 **966ms → 750ms**，仍是一次成功。
差的 350ms 原来只是 `sleep(ms+350)` 的固定余量——桥的 `mine` 是"按住 ms 再松手"，
松手后 1–2 tick 就结算，多等的那段纯属自费。

### 右键交互（同一份真人录像，19 次点击）

| 量 | 实测 |
|---|---|
| 单次右键按住时长 | **50–118ms**（3–7 tick），5 次只有 1 帧（≤45ms） |
| 鼠标到角色的距离 | **0.3–2.7 格**，中位 ~0.8 格，全部贴在目标格上 |
| 手持物 | 铁镐或铁宽剑都照样交互成功，不需要空手 |

所以：交互就是"走到旁边、点一下"。桥的交互命令按点按时长发即可，不需要长按，
也不需要为了交互先卸下工具。

### 在水里怎么出来（真人演示，本轮录像 2720 帧）

| 观察 | 数值 |
|---|---|
| 出水那一下按的键 | **跳跃**（`jump=true`），起跳竖直速度 -5.81，与陆地起跳完全一样 |
| 水里能挖吗 | **能**，而且这是从水袋出来的正规做法：连续挖了十几秒开出一条向上通道 |
| 水里挖一格耗时 | **1.4–1.6 秒**（陆地 0.43–0.55 秒，慢约 3 倍） |
| 水里能不能原地往上浮 | 能，但只靠上浮**出不了水面以上的岸**——岸比水面高时要靠跳跃或挖台阶 |

结论：**水里和陆地的操作机制没有区别，只是多了一个窒息计时**。所以"卡在水里"不是水的问题，
是高度的问题：够高就跳，不够就先在水里挖出阶梯或斜通道，一级一级往上，最后一步跳。

## 左键挥动节奏（真机实测）

采集方式：`keyevents` 按 tick 记录按下/松开边沿，`atomizeEvents` 按 tick 差值切分（60 tick/s）。

| 档位 | 样本 | 按住 tick | 毫秒 | 中位 |
|---|---|---|---|---|
| 轻点（单次） | 9 | 5–7 | 83–117 | 100ms |
| 按住（作业） | 8 | 21–33 | 350–550 | 517ms |

两簇之间空档很大（7 → 21 tick），没有中间样本，所以分界取 14 tick。

其它实测：轻点跳 5–11 tick（83–183ms，中位 133）；长按跳 31–36 tick（517–600ms）；
移动 7–39 tick（117–650ms，中位 267/283）。

**重要前提**：以上轻点样本是操作者"配合测试"刻意做出来的，不代表自然习惯。
自然挖掘、砍伐、连续放置都是**按住不放**；轻点只用于单次试探。
采集真人数据时必须先声明"按平时的方式操作"，否则刻意配合会污染规律。


## 跳远：起跳时长 × 横移时长 → 落点（真机逐帧实测）

同一块平地、反复试，记录起跳点与落点：

| 起跳按住 | 横移按住 | 水平位移 | 顶点升高 |
|---|---|---|---|
| 150ms | 200ms | 4.2 格 | 1.4~2.7 格 |
| 150ms | 400ms | 6.2 格 | 1.4~2.7 格 |
| 250ms | 320ms | 5.6 格 | 3.8 格 |
| 250ms | 520ms | 7.5 格 | 3.8 格 |
| 400ms | 320ms | 5.5 格 | 4.1 格 |
| 400ms | 520ms | 7.5~7.6 格（多次复现） | 4.1 格 |

要点：① 单独 jump 不横移＝原地垂直起落，跳不上海拔差；② 横移必须与起跳同帧，落地后再走已经晚了；③ 头顶压 1 格方块时整条曲线被削成 0.2~0.4 格。

## 基地楼层结构（第 194 轮实测，本世界数据）

`scan (4260,455) r=30 ascii` 的逐行读数（x=4248..4272，y=443..467）：

| 层 | y | 内容 |
|---|---|---|
| 楼板 | 443 | 整层实心 |
| 空腔 | 444–447 | 4 格 |
| 楼板 | 448 | 整层实心 |
| 空腔 | 449–458 | 10 格，中间有成排柱子：每 3 格空 1 格（`##...##...`），y=454 那行还有一道横向半砖（`##.#.##.#.##`） |
| 楼板 | 459 | 整层实心 PalmWood(322) |
| 空腔 | 460–463 | **4 格 —— 玩家现在站的就是这层**（脚 y=464，角色中心 y=462.7） |
| 楼板 | 464 | 木平台(19)，`ropeContact.tileFeetType:19` |

**楼层之间没有任何楼梯、门或梯子**，只能挖或搭。上级楼层归属：

- 爆破专家 Oten（who4, type38）在 (4260.7, 457.8)，脚踩 y=459 → 在 **449–458 那层**
- 玩家在 460–463 那层 → 两人中间隔着一整层实心楼板，`talk` 必然失败（视线被挡）

**上二层（460–463 → 449–458）本轮试过四种办法，全部失败**：

1. `jump {ms:250}` / `{ms:300}` / `{ms:520}` —— 原地起落，位置一格不动（头顶 459 是实心楼板）
2. 挖开 (4253,459)、(4254,459) 得到 2 格宽的洞，`jump` 仍上不去：玩家身体 2 格宽，
   洞必须**至少 2 格宽且完全覆盖身体**；挖开后单次跳跃只上升 3.6 格（462.7→459.1 是
   中心位移），脚到不了 459 楼板上表面
3. 同格 `jump` 两次连按 —— 第二次在空中无效（没有 `IntentJump` 保持，翅膀不开）
4. `hook {x:4253,y:456}` 只把角色从 462.7 拉到 461.3（上 1.4 格）；
   再 `hook {x:4253,y:450}`（`grapCountBefore:4` 说明钩住了）位置完全不动 —— 钩绳穿过洞口，
   但角色身体被 459 楼板挡住，拉不上去

**(4252,459) 挖不掉**：`tile` 显示 `type:322 PalmWood, solid:true, solidTop:false,
frameX:108, frameY:72` —— frameX 非 0 说明它不是普通木块而是多格结构的一部分（门框/家具），
`mine` 反复挥动无效。挖洞时要避开这种格子。

结论：**在这套结构下，不去改动更多方块就到不了上一层**。可行方向是把洞挖到 3 格宽、
或者干脆搭一段木平台楼梯上去；`fly` 修好后也可以直接飞过（本次 `fly` 有 bug，见 STATUS）。
