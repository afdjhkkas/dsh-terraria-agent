import { defineTool } from './src/tool-def.mjs'
import { createBridgeClient } from './src/bridge-client.js'

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

  register({
    name: 'terraria_spawn_npc',
    description: 'Spawn an NPC near the player for testing. Creative/admin action - use it to practise combat, not while exploring.',
    parameters: {
      type: integer('Terraria NPC type id (default BlueSlime = 1).'),
      x: integer('World tile X (default five tiles right of the player).'),
      y: integer('World tile Y (default the player).'),
    },
    command: 'spawn_npc',
    mapArgs: ({ type, x, y }) => ({
      type: type === undefined ? 1 : mustInt(type, 'type', 1, 100000),
      ...(x === undefined ? {} : { x: mustInt(x, 'x') }),
      ...(y === undefined ? {} : { y: mustInt(y, 'y') }),
    }),
  })

  /* ── 世界写入 ─────────────────────────────────────────────────── */

  register({
    name: 'terraria_mine',
    description: 'Mine a tile the way a player does: it selects your pickaxe (or axe for trees), aims at the tile and swings. '
      + 'The game decides whether it breaks, drops the item and wears the tool. Read removed/after to see the result; '
      + 'if removed is false, walk closer or swing longer (ms), or use a better tool. '
      + 'admin:true skips the tool and edits the world directly - that is cheating, use it only for tests. '
      + `${RULES}`,
    parameters: {
      x: integer('World tile X (required).', { required: true }),
      y: integer('World tile Y (required).', { required: true }),
      ms: integer('How long to swing, 100..4000 (default 900).'),
      admin: boolean('Cheat: edit the world directly without a tool.'),
    },
    command: 'mine',
    timeout: 15000,
    async run(args, exec) {
      const x = mustInt(args.x, 'x')
      const y = mustInt(args.y, 'y')
      const ms = bounded(args.ms, 100, 4000, 900)
      if (args.admin === true) {
        const direct = await request('mine', { x, y, admin: true }, exec)
        return JSON.stringify({ ok: true, data: { ...direct.data, mode: 'admin' } })
      }
      const before = await request('tile', { x, y }, exec)
      const swing = await request('mine', { x, y, ms }, exec)
      await sleep(ms + 320)
      const after = await request('tile', { x, y }, exec)
      const removed = after.data?.hasTile === false
      const data = {
        x,
        y,
        mode: 'player',
        tool: swing.data?.held,
        aimed: swing.data?.onScreen,
        before: before.data?.type,
        after: after.data?.type,
        removed,
      }
      if (!data.aimed) data.hint = '目标不在屏幕内：先走近一点再挖'
      else if (!removed) data.hint = '还没挖掉：增大 ms 多挥几下，或者换更好的镐子'
      return JSON.stringify({ ok: true, data })
    },
  })

  register({
    name: 'terraria_place',
    description: 'Place a tile the way a player does: it finds that block in your inventory, selects it, aims and places it - '
      + 'so the stack is actually consumed and you need the block in hand. after is what is really there afterwards '
      + '(-1 = empty). admin:true edits the world directly without using an item (cheating, tests only). '
      + `Tile id 0 is Dirt, not "empty". ${RULES}`,
    parameters: {
      x: integer('World tile X (required).', { required: true }),
      y: integer('World tile Y (required).', { required: true }),
      type: integer('Tile id to place (required; 0 = Dirt, 1 = Stone).', { required: true }),
      style: integer('Tile style, usually 0.'),
      admin: boolean('Cheat: place without consuming an item.'),
    },
    command: 'place',
    timeout: 15000,
    async run(args, exec) {
      const x = mustInt(args.x, 'x')
      const y = mustInt(args.y, 'y')
      const type = mustInt(args.type, 'type', 0, 100000)
      if (args.admin === true) {
        const direct = await request('place', { x, y, type, style: args.style ?? 0, admin: true }, exec)
        return JSON.stringify({ ok: true, data: { ...direct.data, mode: 'admin' } })
      }
      const placeReq = await request('place', { x, y, type, ms: 300 }, exec)
      await sleep(520)
      const after = await request('tile', { x, y }, exec)
      const placed = after.data?.hasTile === true && after.data?.type === type
      const data = { x, y, type, mode: 'player', item: placeReq.data?.held, placed, after: after.data?.type }
      if (!placed) data.hint = after.data?.hasTile ? '这一格已经有别的方块了' : '没放上：确认背包里有这种方块，且目标格有支撑'
      return JSON.stringify({ ok: true, data })
    },
  })

  register({
    name: 'terraria_wall',
    description: 'Place or remove a background wall like a player: it takes the wall item (or a hammer when removing), aims and uses it. '
      + 'Walls decide whether a room counts as housing and every NPC needs a valid room, so this is what makes "build a house" possible. '
      + 'The cell must have no block in it. admin:true edits directly (cheating, tests only). '
      + `${RULES}`,
    parameters: {
      x: integer('World tile X (required).', { required: true }),
      y: integer('World tile Y (required).', { required: true }),
      type: integer('Wall id to place (default 2 = wood wall).'),
      remove: boolean('Remove the wall (needs a hammer).'),
      admin: boolean('Cheat: edit the wall directly.'),
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
        ...(args.admin === true ? { admin: true } : {}),
      }
      const started = await request('wall', payload, exec)
      if (args.admin === true) return JSON.stringify({ ok: true, data: { ...started.data, mode: 'admin' } })
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
    name: 'terraria_teleport',
    description: 'Teleport to a tile coordinate. Rejects non-finite or out-of-world coordinates, and lifts the player out of solid rock '
      + '(adjusted = tiles lifted). Teleporting into lava or next to enemies is still possible - observe right after. '
      + `${RULES}`,
    parameters: {
      x: integer('Destination tile X (required).', { required: true }),
      y: integer('Destination tile Y (required).', { required: true }),
    },
    command: 'teleport',
    mapArgs: ({ x, y }) => ({ x: mustInt(x, 'x'), y: mustInt(y, 'y') }),
  })

  register({
    name: 'terraria_craft',
    description: 'Craft by output item id using the inventory and nearby stations. Handles recipe groups ("any wood") and recipe conditions. '
      + 'crafted counts items that reached the inventory; dropped counts items that fell on the floor. '
      + `times is capped at 20 per call. ${RULES}`,
    parameters: {
      item: integer('Output item type id (required).', { required: true }),
      times: integer('Number of crafts, 1..20.'),
    },
    command: 'craft',
    mapArgs: ({ item, times }) => ({ item: mustInt(item, 'item', 1, 100000), times: bounded(times, 1, 20, 1) }),
  })

  register({
    name: 'terraria_give_item',
    description: 'Put an item stack into the inventory (added = what fit; dropped = what fell on the floor because the inventory was full). Creative/admin action for testing; prefer terraria_mine / terraria_craft while playing.',
    parameters: {
      type: integer('Terraria item type id (required).', { required: true }),
      stack: integer('Stack size, 1..9999.'),
    },
    command: 'give',
    mapArgs: ({ type, stack }) => ({ type: mustInt(type, 'type', 1, 100000), stack: bounded(stack, 1, 9999, 1) }),
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