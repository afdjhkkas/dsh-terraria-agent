# Bridge protocol (v0.2.0)

Transport: TCP `127.0.0.1:7777`, one JSON object per line, UTF-8, `\n` terminated.

- Request: `{"cmd":"<name>","params":{...},"req":<optional id>}`
- Success: `{"ok":true,"data":{...},"req":<echoed id>}`
- Failure: `{"ok":false,"error":"<human readable>","req":<echoed id>}`

`req` is optional. When present the bridge echoes it, so a client can discard any
line that is not the answer to its own request. The current client always sends one.

A failure means **the command did not do the thing** and the message explains why in
terms the caller can act on (out of bounds, not in a world, insufficient materials…).
It does not mean the connection is broken.

## When commands run

- **In a world** requests are queued to the game's main thread and drained from the
  world update hooks. If the main thread does not reach the request within 1500 ms the
  request is dropped and the error says whether it had already started (see Errors).
- **At the main menu** there are no world hooks, so requests are handed to the game's
  own main-thread queue instead; `ping`/`state` therefore work before a world is loaded.
- **While paused** (`Main.gamePaused`: the window is unfocused, or the inventory is open
  with auto-pause on) reading commands still work, but anything that needs the world to
  advance refuses with an explicit error instead of pretending to work.

## Tile encoding (changed in 0.2.0)

`type` is `-1` for **empty** and the Terraria tile id otherwise.

> Why: Terraria's `TileID.Dirt == 0`. Encoding "no tile" as `0` made dirt
> indistinguishable from air in `scan`, and made `mine` refuse to dig dirt
> ("this cell is already empty"). Every tile-reading command uses `-1` for empty now.

## Coordinates

Tile coordinates. 1 tile = 16 px. `x` grows right, `y` grows down.
Player position is derived from `Player.Center`.

## Commands

### `ping`
`{"pong":true,"version":"0.2.0","inWorld":bool,"tick":n,"paused":bool,"hasFocus":bool}`

`paused` is `Main.gamePaused`; `hasFocus` is `Main.hasFocus`. Tick-driven commands
refuse to run while `paused` is true, because the world is not simulating.

### `state`
`{inWorld,version,paused,hasFocus,screen,mouseWorld,mouseTile,world,player,attack}`

`world`: `{name,width,height,dayTime,time,raining,bloodMoon,eclipse,hardmode,tick}`
`player`: `{tile,pos,velocity,itemAnimation,itemAnimationMax,itemTime,life,lifeMax,mana,manaMax,dead,direction,grounded,selected,held,buffs}`
`held`: `{type,name,stack}`
`attack`: see `attack`.

`grounded` is true only when the player is at rest vertically this tick **and** last tick.

### `scan`
params: `x?`, `y?` (default: player), `radius?` (1..48, default 12), `walls?` (default true),
`format?` (`"rows"` default | `"ascii"` | `"rle"`)

Common fields: `x0,y0,w,h,center,playerTile,format,legend`.

`format:"rows"` (default, full fidelity):
- `stride` = `walls ? 4 : 2`, `fields` = `["type","wall","liquid","shape"]` or `["type","liquid"]`.
- `tiles` is an **array of rows**: `tiles[j]` is the row at `y = y0 + j`; the cell at
  `x = x0 + i` occupies `tiles[j][i*stride .. i*stride+stride-1]`.
  It is *not* a flat array of tiles.

`format:"ascii"` (compact, for big radii):
- `map`: `h` newline-separated lines, one char per tile:
  `.` air · `#` solid · `~` water · `L` lava · `^` half-brick or slope
- `w`,`h`,`x0`,`y0` still give the geometry; exact ids come from `tile`.

`format:"rle"` (most compact, still exact):
- `map`: `h` strings, each a run-length list `"<count>x<type>,<count>x<type>…"`
  reading left to right, e.g. `"12x-1,3x0,40x-1"`.

`shape`: `0` solid · `1` half brick · `2..5` the four slopes
(`2`=down-left, `3`=down-right, `4`=up-left, `5`=up-right).
`liquid`: 0..255. `wall`: 0 = no wall.

### `tile`
params: `x`,`y` (**required**; missing → error, never a default coordinate).

`{x,y,hasTile,type,name,wall,liquid,slope,halfBlock,actuator,frameX,frameY}`

`type` is `-1` when `hasTile` is false.

### `mine`
params: `x`,`y` (required), `noItem?` (default false), `walls?` (default false)

Removes the tile; only removes the background wall when `walls:true`.
`{ok,x,y,removedTile,removedWall,before,after,wallBefore,wall}`
`before`/`after` use the `-1` empty convention. `noItem` suppresses item drops for the tile
(and for the wall when `walls:true`).

### `place`
params: `x`,`y`,`type` (required, `1..TileID.Count-1`), `style?`, `forced?`

`{ok,placed,worldGenOk,x,y,requested,after}` — `after` is the type actually present afterwards
(`-1` = empty) and `ok`/`placed` are true only when the requested type is really there now.
`worldGenOk` is Terraria's own return value, which is true even when nothing changed
(e.g. it refused to replace an existing tree).

### `move`
params: `dir` (`left|right|up|down`), `ms?` (1..5000, default 500)

Returns immediately (never blocks the game thread): `{dir,ms,from:[x,y],untilTick,tick}`
`from` is the player centre in tile coordinates rounded to 0.1.

The *tool* `terraria_move` adds `wait` (default true): it sleeps `ms`, re-reads `state` and
reports `from`, `to`, `dx`, `dy`, `moved`. Doing the wait in the plugin keeps the main thread
free, and turns "I asked it to walk" into "it walked 4.2 tiles".

### `jump`
params: `ms?` (1..2000, default 150)

One impulse, applied only when the player is standing (`velocity.Y == 0` both this and the
previous tick). While airborne only `controlJump` is held, so **wings** (hold jump → jump turns
into flight) and auto-jump still work exactly as in vanilla — the bridge does not fight them.

`{ms,from:[x,y],untilTick,tick}` — the tool adds `wait` and reports `dY`.

### `use`
params: `slot?`, `right?`, `ms?` (1..2000, default 220)

`{slot,right,ms,held,anim}` — `anim` is `itemAnimation` right after the request, so the
caller can see that something actually started.

### `attack`
params: `who?`, `radius?` (1..200 tiles, default 40), `durationMs?` (100..15000, default 3000),
`intervalMs?` (30..2000, default 180), `slot?`, `wait?` (default false)

Auto-targets the nearest hostile NPC when `who` is omitted. Hits require line of sight,
use the player's real damage/crit/use-speed, and never fire more often than the weapon's
own use time. Returns immediately unless `wait:true`, in which case it holds for
`durationMs` and reports the final summary.

`state.attack` mirrors the live task **and keeps the last finished attempt**:
`{active,target,autoTarget,hits,lastTarget,lastLife,untilTick,last:{target,name,hits,lifeBefore,lifeAfter,killed,endedTick}}`
`last` survives `stop` and the end of the task, so a caller can verify an attack after the fact.

### `stop`
`{stopped:true}` — clears movement/jump/use/attack holds. Does not erase `attack.last`.

### `teleport`
params: `x`,`y` (required, finite, inside the world)

`{x,y,from,tile,adjusted}` — pushes the player out of solid tiles upward before returning.
Rejects non-finite or out-of-world coordinates.

### `inventory`
`{selected,count,items:[{slot,hotbar,type,name,stack,prefix,rare}],equipment:[{slot,kind,type,name,defense,wingSlot}],canFly,defense}`

`items` covers the inventory array only. Armor and accessories live in a separate array,
so `equipment` is what tells you whether the character is armoured or winged
(`canFly`), and `defense` is the net number.

### `craft`
params: `item` (required), `times?` (1..20)

Handles recipe groups ("any wood"), recipe conditions and station requirements.
`{item,name,crafted,times,perCraft,reason?}` — `crafted` counts only items that actually
landed in the inventory; anything dropped on the floor is reported separately as `dropped`.

### `give`
params: `type` (required, must be a real item id), `stack?` (1..9999)

`{type,stack,name,added,dropped}`

### `spawn_npc`
params: `type?` (must be a real NPC id, default `NPCID.BlueSlime`), `x?`, `y?`

`{who,type,netId,name,life,tile}`

### `npc`
params: `r?` (1..200 tiles, default 40)

`{radius,count,npcs:[{who,type,name,tile,life,lifeMax,friendly,boss,hostile}]}`
`hostile` means "damaging and attackable" — it is what `attack` will select.

### `chat`
params: `text` (required, ≤400 chars, not blank)

`{sent,length,truncated}` — local client echo only (`Main.NewText`), not a server broadcast.

### `help`
`{commands:[...]}`

## Errors

Free-form but actionable. Current messages include:

- `not in a world (still on the menu or no world loaded)`
- `no local player on this instance (dedicated server?)`
- `x,y must be finite`
- `out of world bounds: x,y`
- `invalid tile type 9999 (1..692)`
- `the game is paused (Main.gamePaused); tick-driven commands will not run until it resumes`
- `main thread did not process the request within 1500 ms` — **the command may still take effect**;
  the message says whether it started.

Side-effecting commands (`mine`, `place`, `give`, `craft`, `spawn_npc`, `teleport`) are
**not** idempotent. A caller that sees a timeout must call `state`/`inventory`/`tile`
before retrying.
## Change log

### 0.2.0
- **Empty tiles are `-1`.** `0` is Dirt; the old "0 means empty" encoding made dirt
  invisible to `scan` and made `mine` refuse to dig it.
- `scan` gained `format` (`rows`|`ascii`|`rle`), and `shape` codes now match their legend
  (half brick is `1`, slopes are `2..5`).
- `tile` requires `x`,`y`; `teleport` rejects non-finite/out-of-world positions and lifts
  the player out of solid rock; `give`/`place`/`spawn_npc` validate their ids;
  `place` reports whether anything actually changed.
- `mine` reports `removedTile`/`removedWall` separately and only removes the wall when
  `walls:true`.
- `attack` uses real damage/crit, requires line of sight, respects use speed and
  invincibility frames, and keeps `attack.last` after the task ends (and after `stop`).
- `jump` applies a single ground impulse; wings are left to vanilla.
- `inventory` exposes equipment and `canFly`.
- `ping`/`state` expose `paused` and `hasFocus`; tick-driven commands refuse while paused.
- Requests may carry `req` and get it echoed.
- The 1500 ms timeout error distinguishes "already started, it WILL happen" from
  "dropped, safe to retry". Requests at the main menu no longer time out.
- `state.player.grounded` requires rest on two consecutive ticks.
