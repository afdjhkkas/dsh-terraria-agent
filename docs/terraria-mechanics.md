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

- Horizontal: forcing `velocity.X = ±2.8` px/tick gives ≈ 11 tiles/s; 800 ms of `right`
  moved ≈ 8.8 tiles, then friction brings it to rest within ~100 ms.
- Jump: one impulse of `velocity.Y = -5.6` raises the player ≈ 2.3 tiles, airborne ≈ 350 ms.
- Gravity/friction still run on top of any forced velocity, so a forced velocity does not
  survive once the hold ends.
- **Wings** `[wiki]`: with wings equipped, *holding jump after the jump ends starts flight*.
  A long `jump` on a winged character is legitimate flight, not a bug. The bridge sets
  `controlJump` and lets vanilla decide; it does not fake flight.
- Consequences of overriding `velocity` directly: knockback, dash, mounts, conveyor belts,
  ice skid, wind and `confused` are all bypassed. Collision still applies because
  position integration happens after the override.

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