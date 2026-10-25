import { defineTool } from './src/tool-def.mjs'
import { createBridgeClient } from './src/bridge-client.js'
import { createPrimitives } from './src/primitives.js'

export const name = 'dsh-terraria-agent'
export const inject = ['tools']

/**
 * Shared rules for the model. These used to live only in the README, which the model
 * never sees, so the essential operating knowledge now travels with every description.
 */
const RULES = [
  'Tiles: 1 tile = 16 px, x grows right, y grows DOWN.',
  'Tile ids: -1 = empty, 0 = Dirt, 1 = Stone (use terraria_tile to confirm).',
  'Tool results are JSON strings; read them, do not guess.',
  'After every action, verify with terraria_observe / terraria_scan before the next step.',
  'A {"ok":false} answer is a normal game-side refusal with a reason, not a broken bridge.',
].join(' ')

const textOutput = {
  schema: { type: 'string' },
  render: (_args, value) => [{ type: 'text', text: String(value) }],
}

// The host's schema DSL only accepts `required: true` on a property and rejects
// numeric `minimum`/`maximum`, so ranges are enforced in the mappers below.
const integer = (description, { required = false } = {}) => ({
  type: 'integer',
  ...(required ? { required: true } : {}),
  description,
})

const string = (description, { required = false, enum: values } = {}) => ({
  type: 'string',
  ...(required ? { required: true } : {}),
  description,
  ...(values ? { enum: values } : {}),
})

const boolean = description => ({ type: 'boolean', description })

function bounded(value, min, max, fallback) {
  const number = Number(value)
  return Number.isFinite(number) ? Math.min(max, Math.max(min, Math.trunc(number))) : fallback
}

/** Required coordinate/id: refuse instead of silently substituting 0. */
function mustInt(value, name, min = 0, max = 1e9) {
  const number = Number(value)
  if (!Number.isFinite(number)) throw new Error(`${name} is required and must be a number.`)
  const truncated = Math.trunc(number)
  if (truncated < min || truncated > max) throw new Error(`${name} must be between ${min} and ${max}.`)
  return truncated
}

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms))

function makeSerialRunner(client) {
  const queues = new Map()

  return (command, params, exec) => {
    const key = String(exec?.agent?.id ?? 'default')
    const previous = queues.get(key) ?? Promise.resolve()
    const result = previous.then(() => {
      if (exec?.signal?.aborted) {
        const error = new Error('Terraria tool call was cancelled before it reached the game.')
        error.name = 'AbortError'
        throw error
      }
      return client.request(command, params, { signal: exec?.signal })
    })
    const tail = result.then(() => undefined, () => undefined)
    queues.set(key, tail)
    tail.then(() => {
      if (queues.get(key) === tail) queues.delete(key)
    })
    return result
  }
}

/**
 * Compact rendering. `JSON.stringify(value, null, 2)` was turning a radius-48 scan into
 * 340 KB (~97k tokens); the same payload without indentation is ~4x smaller, and the
 * scan tool additionally offers ascii/rle summaries.
 */
function displayResult(result) {
  return JSON.stringify(result.data ?? result)
}

function playerVec(state) {
  const tile = state?.player?.tile
  return Array.isArray(tile) ? tile : null
}

/**
 * Turn the raw flags into the next action the caller should take. The most common
 * dead end is Terraria's "pause when unfocused": the user is typing in DSH, the game
 * loses focus, the world freezes and every action is refused.
 */
const HINTS = {
  paused: 'The game is PAUSED (Main.gamePaused), so the world is not simulating and movement/jump/use/attack will be refused. '
    + 'Ask the user to turn off Settings > General > "Pause when unfocused", or to click the game window. '
    + 'Reading commands (status/observe/scan/tile/inventory/npcs) still work.',
  menu: 'Still on the main menu: ask the user to pick a character and enter a world, then call terraria_status again.',
}

function withHints(data) {
  if (!data || typeof data !== 'object') return data
  const out = { ...data }
  if (out.inWorld === false) out.hint = HINTS.menu
  else if (out.paused === true) out.hint = HINTS.paused
  return out
}

export function apply(ctx, rawConfig = {}) {
  const config = { ...(rawConfig ?? {}) }
  const host = String(config.host ?? '127.0.0.1').trim().toLowerCase() || '127.0.0.1'
  if (host !== '127.0.0.1' && host !== 'localhost') {
    // Do not take the whole plugin down for a typo: warn and stay on loopback.
    ctx.logger?.warn?.(`[terraria] ignoring host "${config.host}": only loopback is allowed`)
  }
  const port = bounded(config.port, 1, 65535, 47777)
  const timeoutMs = bounded(config.timeoutMs, 500, 30000, 5000)
  const client = createBridgeClient({ host: '127.0.0.1', port, timeoutMs })
  const request = makeSerialRunner(client)
  // Verified primitives: they poll the world instead of sleeping and hoping,
  // so "placed: true" means the tile is really there (see src/primitives.js).
  const primitives = createPrimitives({
    request: (command, params, options) => request(command, params, { signal: options?.signal }),
  })

  const register = ({ name: toolName, description, parameters = {}, command, timeout, mapArgs = () => ({}), run }) => {
    ctx.tools.register(defineTool({
      name: toolName,
      description,
      parameters,
      output: textOutput,
      ...(timeout ? { timeoutMs: timeout } : {}),
      async execute(args, exec) {
        try {
          if (run) return run(args ?? {}, exec)
          const result = await request(command, mapArgs(args ?? {}), exec)
          return displayResult(result)
        } catch (error) {
          if (error?.name === 'AbortError') {
            // A cancelled move/attack would otherwise keep steering the character.
            client.stopBestEffort()
            throw new Error(`${error.message} The bridge was told to stop all movement, jump, use and attack holds.`)
          }
          throw error
        }
      },
    }))
  }

  /* ── 状态 ─────────────────────────────────────────────────────── */

  register({
    name: 'terraria_status',
    description: `Check the local tModLoader bridge: reachable, in a world, paused, window focus, tick. ${RULES}`,
    command: 'ping',
    async run(args, exec) {
      const result = await request('ping', {}, exec)
      return JSON.stringify({ ok: true, data: withHints(result.data) })
    },
  })

  register({
    name: 'terraria_observe',
    description: 'Read the current world and player: position, health, mana, held item, buffs, weather, time and the live/last attack state. '
      + 'paused=true means the world is not simulating (Terraria pauses when unfocused or when the inventory is open), so movement, jump, use and attack will refuse. '
      + `Call this before and after every action. ${RULES}`,
    command: 'state',
    async run(args, exec) {
      const result = await request('state', {}, exec)
      return JSON.stringify({ ok: true, data: withHints(result.data) })
    },
  })

  /* ── 世界读取 ─────────────────────────────────────────────────── */

  register({
    name: 'terraria_scan',
    description: 'Read a square region of tiles around a tile coordinate. '
      + 'format:"ascii" returns a compact map (. air, # solid, ^ half/slope, ~ liquid, L lava) — use it for large areas; '
      + 'format:"rle" returns "<count>x<type>" runs; the default format:"rows" returns exact data where tiles[j] is row y0+j and cell i occupies tiles[j][i*stride .. i*stride+stride-1]. '
      + 'type is -1 for empty. radius 1..48 costs context fast: r=12 ≈ 6 KB compact, r=48 ≈ 76 KB; prefer ascii/rle above r=24. '
      + RULES,
    parameters: {
      x: integer('Center tile X. Defaults to the player.'),
      y: integer('Center tile Y. Defaults to the player.'),
      radius: integer('Scan radius in tiles, 1..48 (default 12).'),
      walls: boolean('Include wall, liquid and shape values (rows format only).'),
      format: string('Output shape: rows (exact), ascii (compact map), rle (run-length).', { enum: ['rows', 'ascii', 'rle'] }),
    },
    command: 'scan',
    timeout: 15000,
    mapArgs: args => ({
      ...(args.x === undefined ? {} : { x: mustInt(args.x, 'x') }),
      ...(args.y === undefined ? {} : { y: mustInt(args.y, 'y') }),
      r: bounded(args.radius, 1, 48, 12),
      ...(args.walls === undefined ? {} : { walls: args.walls }),
      format: args.format ?? (bounded(args.radius, 1, 48, 12) > 24 ? 'ascii' : 'rows'),
    }),
  })

  register({
    name: 'terraria_tile',
    description: 'Inspect exactly one tile: hasTile, type (-1 = empty), name, wall, liquid, slope, half-brick, actuator. '
      + `x and y are required. Use it to confirm what terraria_scan shows. ${RULES}`,
    parameters: {
      x: integer('World tile X (required).', { required: true }),
      y: integer('World tile Y (required).', { required: true }),
    },
    command: 'tile',
    mapArgs: ({ x, y }) => ({ x: mustInt(x, 'x'), y: mustInt(y, 'y') }),
  })

  register({
    name: 'terraria_inventory',
    description: 'Read the inventory, the equipped armor/accessories (so you can tell whether you are armoured or have wings) and the selected slot.',
    command: 'inventory',
  })

  register({
    name: 'terraria_npcs',
    description: 'List active NPCs near the player with who/type/name/tile/life/friendly/boss/hostile. '
      + 'hostile means damageable and chaseable, which is what terraria_attack will target. '
      + `Pass the returned who to terraria_attack. ${RULES}`,
    parameters: { radius: integer('Search radius in tiles, 1..200 (default 40).') },
    command: 'npc',
    mapArgs: args => ({ r: bounded(args.radius, 1, 200, 40) }),
  })

  /* ── 动作 ─────────────────────────────────────────────────────── */

  register({
    name: 'terraria_move',
    description: 'Hold a movement direction, then report how far the player actually went (from/to/dx/dy). '
      + 'direction is required. 250-1000 ms is one useful step; long holds overshoot pits and enemies. '
      + 'movement works even when the window is unfocused. '
      + `Read "moved": false as "nothing happened" and re-check the world. ${RULES}`,
    parameters: {
      direction: string('Direction to hold.', { required: true, enum: ['left', 'right', 'up', 'down'] }),
      ms: integer('Duration in milliseconds, 1..5000 (default 500).'),
    },
    command: 'move',
    timeout: 15000,
    async run(args, exec) {
      const dir = String(args.direction ?? '')
      if (!['left', 'right', 'up', 'down'].includes(dir)) throw new Error('direction must be left, right, up or down.')
      const ms = bounded(args.ms, 1, 5000, 500)
      const started = await request('move', { dir, ms }, exec)
      const from = started.data?.from ?? null
      // Wait out the hold on this side: sleeping inside the game's main thread would
      // freeze the world. Then measure what really happened.
      await sleep(ms + 220)
      const after = await request('state', {}, exec)
      const to = playerVec(after.data)
      const state = { ...started.data }
      state.to = to
      if (Array.isArray(from) && Array.isArray(to)) {
        state.dx = Math.round((to[0] - from[0]) * 10) / 10
        state.dy = Math.round((to[1] - from[1]) * 10) / 10
        state.moved = Math.abs(state.dx) + Math.abs(state.dy) > 0.2
      }
      return JSON.stringify({ ok: true, data: state })
    },
  })

  register({
    name: 'terraria_jump',
    description: 'Jump once from the ground, then report the height change. Airborne it only holds the jump key, so wings '
      + '(hold jump to fly after the jump) still behave as in vanilla. '
      + `One impulse per call - call it again to jump again. ${RULES}`,
    parameters: { ms: integer('How long to hold jump, 1..2000 (default 150).') },
    command: 'jump',
    timeout: 15000,
    async run(args, exec) {
      const ms = bounded(args.ms, 1, 2000, 150)
      const started = await request('jump', { ms }, exec)
      const from = started.data?.from ?? null
      const grounded = started.data?.grounded
      await sleep(ms + 380)
      const after = await request('state', {}, exec)
      const to = playerVec(after.data)
      const data = { ...started.data, to }
      if (Array.isArray(from) && Array.isArray(to)) {
        data.dY = Math.round((to[1] - from[1]) * 10) / 10
        data.jumped = data.dY < -0.2
        if (!data.jumped) data.note = grounded === false ? 'not standing on the ground, so no jump impulse was applied' : 'no height change observed'
      }
      return JSON.stringify({ ok: true, data })
    },
  })

  register({
    name: 'terraria_use_item',
    description: 'Use the selected item for a moment, optionally selecting a slot first (right:true for right-click / tile interaction). '
      + 'anim > 0 in the answer proves the swing started. '
      + `Mining is faster with terraria_mine. ${RULES}`,
    parameters: {
      slot: integer('Inventory slot to select first.'),
      right: boolean('Use the alternate / right-click action.'),
      ms: integer('Duration in milliseconds, 1..2000 (default 220).'),
      x: integer('Aim at this tile X before using (for swings, bows, tools).'),
      y: integer('Aim at this tile Y before using.'),
    },
    command: 'use',
    mapArgs: args => ({
      ...(args.slot === undefined ? {} : { slot: mustInt(args.slot, 'slot', 0, 200) }),
      ...(args.right === undefined ? {} : { right: args.right }),
      ...(args.x === undefined ? {} : { x: mustInt(args.x, 'x') }),
      ...(args.y === undefined ? {} : { y: mustInt(args.y, 'y') }),
      ms: bounded(args.ms, 1, 2000, 220),
    }),
  })

  register({
    name: 'terraria_attack',
    description: 'Attack a hostile NPC for a while. Omit who to pick the nearest one. '
      + 'Hits need line of sight (targets within ~2 tiles are always reachable), use your real damage/crit/use-speed, and respect invincibility frames. '
      + 'The task keeps running in the background; keep attacking is reported through state.attack, and state.attack.last keeps the '
      + 'hit count and the target\'s remaining life after the task ends (and even after terraria_stop), so verify with terraria_observe. '
      + `The held item must deal damage. ${RULES}`,
    parameters: {
      who: integer('NPC who index from terraria_npcs. Omit to auto-target the nearest hostile.'),
      radius: integer('Auto-target radius in tiles, 1..200 (default 40).'),
      durationMs: integer('How long to keep attacking, 100..15000 ms (default 3000).'),
      intervalMs: integer('Delay between hits, 30..2000 ms (default 180).'),
      slot: integer('Inventory slot holding the weapon.'),
    },
    command: 'attack',
    mapArgs: args => ({
      ...(args.who === undefined ? {} : { who: mustInt(args.who, 'who') }),
      r: bounded(args.radius, 1, 200, 40),
      ms: bounded(args.durationMs, 100, 15000, 3000),
      interval: bounded(args.intervalMs, 30, 2000, 180),
      ...(args.slot === undefined ? {} : { slot: mustInt(args.slot, 'slot', 0, 200) }),
    }),
  })

  register({
    name: 'terraria_stop',
    description: 'Clear every movement, jump, use-item and attack hold issued through the bridge. '
      + 'Use it when something runs away, and after cancelling a call. It does not erase state.attack.last.',
    command: 'stop',
  })


  /* ── 世界写入 ─────────────────────────────────────────────────── */

  register({
    name: 'terraria_follow',
    description: 'Follow another player: walk while further than range tiles, stand still when close. '
      + 'guard:true also fights: it prefers the NPC the teammate is attacking and stops attacking below 30% health. '
      + 'Only control flags and velocity are written - the game still resolves collisions, gravity, stairs and doors. '
      + 'Only meaningful when another player is in the world. ${RULES}',
    parameters: {
      player: integer('Player index to follow (see terraria_players). Defaults to the nearest other player.'),
      range: integer('Stop distance in tiles, 1..30 (default 4.5, rounded).'),
      guard: boolean('Also fight: prefer the teammate\'s target, retreat below 30% health.'),
      off: boolean('Stop following.'),
    },
    command: 'follow',
    mapArgs: ({ player, range, guard, off }) => ({
      ...(player === undefined ? {} : { player: mustInt(player, 'player', 0, 254) }),
      ...(range === undefined ? {} : { range: bounded(range, 1, 30, 4.5) }),
      ...(guard === undefined ? {} : { guard: Boolean(guard) }),
      ...(off === undefined ? {} : { off: Boolean(off) }),
    }),
  })

  register({
    name: 'terraria_idle',
    description: 'Stand still: cancel follow, paths, walks, movement and attack holds. Nothing is erased from the world.',
    command: 'idle',
  })

  register({
    name: 'terraria_players',
    description: 'List every active player in the world: index, name, dead, hp, tile position, team, and whether it is the local player or the follow target.',
    command: 'players',
  })

  register({
    name: 'terraria_mine',
    description: 'Mine a tile the way a player does: it selects your pickaxe (or axe for trees), aims at the tile and swings. '
      + 'The game decides whether it breaks, drops the item and wears the tool. Read removed/after to see the result; '
      + 'if removed is false, walk closer or swing longer (ms), or use a better tool. '
      + `${RULES}`,
    parameters: {
      x: integer('World tile X (required).', { required: true }),
      y: integer('World tile Y (required).', { required: true }),
      ms: integer('How long to swing, 100..4000 (default 900).'),
    },
    command: 'mine',
    timeout: 25000,
    async run(args, exec) {
      const x = mustInt(args.x, 'x')
      const y = mustInt(args.y, 'y')
      const ms = bounded(args.ms, 100, 4000, 1300)
      const result = await primitives.mineTile({ x, y, ms, tries: bounded(args.tries, 1, 6, 3), signal: exec?.signal })
      return JSON.stringify({ ok: result.ok, data: { ...result, mode: 'player' } })
    },
  })

  register({
    name: 'terraria_place',
    description: 'Place a tile the way a player does: it finds that block in your inventory, selects it, aims and places it - '
      + 'so the stack is actually consumed and you need the block in hand. after is what is really there afterwards '
      + '(-1 = empty). '
      + `Tile id 0 is Dirt, not "empty". ${RULES}`,
    parameters: {
      x: integer('World tile X (required).', { required: true }),
      y: integer('World tile Y (required).', { required: true }),
      type: integer('Tile id to place (required; 0 = Dirt, 1 = Stone).', { required: true }),
      style: integer('Tile style, usually 0.'),
    },
    command: 'place',
    timeout: 20000,
    async run(args, exec) {
      const x = mustInt(args.x, 'x')
      const y = mustInt(args.y, 'y')
      const type = mustInt(args.type, 'type', 0, 100000)
      const result = await primitives.placeTile({ x, y, type, style: bounded(args.style, 0, 1000, 0), deadlineMs: 4000, signal: exec?.signal })
      return JSON.stringify({ ok: result.ok, data: { ...result, mode: 'player' } })
    },
  })

  register({
    name: 'terraria_wall',
    description: 'Place or remove a background wall like a player: it takes the wall item (or a hammer when removing), aims and uses it. '
      + 'Walls decide whether a room counts as housing and every NPC needs a valid room, so this is what makes "build a house" possible. '
      + 'The cell must have no block in it. '
      + `${RULES}`,
    parameters: {
      x: integer('World tile X (required).', { required: true }),
      y: integer('World tile Y (required).', { required: true }),
      type: integer('Wall id to place (default 2 = wood wall).'),
      remove: boolean('Remove the wall (needs a hammer).'),
    },
    command: 'wall',
    timeout: 12000,
    async run(args, exec) {
      const x = mustInt(args.x, 'x')
      const y = mustInt(args.y, 'y')
      const remove = args.remove === true
      const payload = {
        x,
        y,
        ...(args.type === undefined ? {} : { type: mustInt(args.type, 'type', 1, 100000) }),
        ...(remove ? { remove: true } : {}),
      }
      const started = await request('wall', payload, exec)
      await sleep(560)
      const after = await request('tile', { x, y }, exec)
      const wanted = payload.type ?? 2
      const applied = remove ? after.data?.wall === 0 : after.data?.wall === wanted
      const data = { x, y, mode: 'player', item: started.data?.held, remove, wall: after.data?.wall, applied }
      if (!applied) data.hint = remove ? '墙还在：需要锤子，并且要挥动够久' : '墙没放上：确认背包里有这种墙，且这格没有方块'
      return JSON.stringify({ ok: true, data })
    },
  })

  register({
    name: 'terraria_craft',
    description: 'Craft by output item id using the inventory and nearby stations (workbench / furnace / anvil must be within ~1 tile). '
      + 'Handles recipe groups ("any wood") and recipe conditions. It waits until the products are really in the bag and reports '
      + 'gained/expected; a full bag shows up as a dropped>0 warning, not as "no materials". '
      + `This takes an ITEM id (not a recipe id). times is capped at 20 per call. ${RULES}`,
    parameters: {
      item: integer('Output item type id (required).', { required: true }),
      times: integer('Number of crafts, 1..20.'),
    },
    command: 'craft',
    timeout: 20000,
    async run(args, exec) {
      const item = mustInt(args.item, 'item', 1, 100000)
      const times = bounded(args.times, 1, 20, 1)
      const result = await primitives.craftItem({ item, times, signal: exec?.signal })
      return JSON.stringify({ ok: result.ok, data: result })
    },
  })

  register({
    name: 'terraria_tiledata',
    description: 'Furniture footprint and anchor, straight from the game (TileObjectData). '
      + 'width/height are the cells the object occupies and origin is the cell inside the footprint that the cursor must be on. '
      + 'Use it before placing furniture: a 3x2 furnace with origin (1,1) is placed by aiming one right and one down from its top-left cell. '
      + `${RULES}`,
    parameters: { type: integer('Tile id of the furniture (required).', { required: true }), style: integer('Style, usually 0.') },
    command: 'tiledata',
    async run(args, exec) {
      const type = mustInt(args.type, 'type', 0, 100000)
      const info = await primitives.objectInfo(type, { style: bounded(args.style, 0, 1000, 0), signal: exec?.signal })
      return JSON.stringify({ ok: true, data: info })
    },
  })

  register({
    name: 'terraria_place_object',
    description: 'Place a multi-cell furniture (chest, furnace, anvil, table, chair, door, bed) at its ROOT cell - the top-left cell of its footprint. '
      + 'It looks the anchor up instead of guessing, aims at root+origin, then checks every footprint cell and reports filled/total. '
      + 'Use terraria_tiledata first if you want to see the footprint. '
      + `${RULES}`,
    parameters: {
      x: integer('Root cell X (top-left of the footprint, required).', { required: true }),
      y: integer('Root cell Y (top-left of the footprint, required).', { required: true }),
      type: integer('Tile id of the furniture (required).', { required: true }),
      style: integer('Style, usually 0.'),
    },
    command: 'place',
    timeout: 25000,
    async run(args, exec) {
      const x = mustInt(args.x, 'x')
      const y = mustInt(args.y, 'y')
      const type = mustInt(args.type, 'type', 0, 100000)
      const result = await primitives.placeObject({ x, y, type, style: bounded(args.style, 0, 1000, 0), signal: exec?.signal })
      return JSON.stringify({ ok: result.ok, data: { ...result, mode: 'player' } })
    },
  })

  register({
    name: 'terraria_equip',
    description: 'Equip the armour or accessory sitting in one bag slot, then read the equipment slots and defence back. '
      + 'armour slots are 0=head 1=body 2=legs; accessories follow. '
      + `${RULES}`,
    parameters: { slot: integer('Bag slot holding the item (required).', { required: true }) },
    command: 'equip',
    timeout: 12000,
    async run(args, exec) {
      const slot = mustInt(args.slot, 'slot', 0, 58)
      const result = await primitives.equipFromSlot({ slot, signal: exec?.signal })
      return JSON.stringify({ ok: result.ok, data: result })
    },
  })

  register({
    name: 'terraria_chest',
    description: 'Work with the chest you are standing next to (within 6 tiles): '
      + 'action="list" shows its slots, "put" moves a bag slot into it, "take" moves a chest slot into the bag. '
      + 'Every action is verified on both sides (chest contents and bag count) before it reports success. '
      + `${RULES}`,
    parameters: {
      action: string('list | put | take (required).', { required: true, enum: ['list', 'put', 'take'] }),
      invSlot: integer('Bag slot to move into the chest (action=put).'),
      slot: integer('Chest slot to take from (action=take).'),
      count: integer('How many to take (action=take, default all).'),
    },
    command: 'chest',
    timeout: 15000,
    async run(args, exec) {
      const action = String(args.action ?? 'list')
      if (action === 'list') {
        const result = await primitives.chestList({ signal: exec?.signal })
        return JSON.stringify({ ok: result.ok, data: result })
      }
      if (action === 'put') {
        const result = await primitives.chestPut({ invSlot: mustInt(args.invSlot, 'invSlot', 0, 58), signal: exec?.signal })
        return JSON.stringify({ ok: result.ok, data: result })
      }
      if (action === 'take') {
        const result = await primitives.chestTake({
          slot: mustInt(args.slot, 'slot', 0, 39),
          ...(args.count === undefined ? {} : { count: bounded(args.count, 1, 999, 1) }),
          signal: exec?.signal,
        })
        return JSON.stringify({ ok: result.ok, data: result })
      }
      throw new Error('action must be list, put or take.')
    },
  })

    register({
    name: 'terraria_chat',
    description: 'Print a message in the local client chat (Main.NewText only: it is not broadcast to a server). Truncated at 400 characters.',
    parameters: { text: string('Message to send (required).', { required: true }) },
    command: 'chat',
    mapArgs: ({ text }) => {
      const value = String(text ?? '')
      if (!value.trim()) throw new Error('text is required and must not be blank.')
      return { text: value.slice(0, 400) }
    },
  })

  return () => client.close()
}