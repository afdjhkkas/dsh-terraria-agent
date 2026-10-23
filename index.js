import { defineTool } from './src/tool-def.mjs'
import { createBridgeClient } from './src/bridge-client.js'

export const name = 'dsh-terraria-agent'
export const inject = ['tools']

const textOutput = {
  schema: { type: 'string' },
  render: (_args, value) => [{ type: 'text', text: String(value) }],
}

// DSH's schema DSL deliberately accepts a small JSON-Schema subset.  In
// particular, `required` is only valid when it is true and numeric
// `minimum`/`maximum` keywords are not supported.  Keep range enforcement in
// the bridge argument mappers below, and only emit supported schema fields.
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

function displayResult(result) {
  return JSON.stringify(result.data ?? result, null, 2)
}

export function apply(ctx, rawConfig = {}) {
  const config = { ...(rawConfig ?? {}) }
  const host = config.host ?? '127.0.0.1'
  if (host !== '127.0.0.1') {
    throw new TypeError('dsh-terraria-agent only connects to 127.0.0.1.')
  }
  const port = bounded(config.port, 1, 65535, 7777)
  const timeoutMs = bounded(config.timeoutMs, 500, 20000, 5000)
  const client = createBridgeClient({ host, port, timeoutMs })
  const request = makeSerialRunner(client)

  const register = ({ name: toolName, description, parameters = {}, command, mapArgs = () => ({}) }) => {
    ctx.tools.register(defineTool({
      name: toolName,
      description,
      parameters,
      output: textOutput,
      async execute(args, exec) {
        const result = await request(command, mapArgs(args), exec)
        return displayResult(result)
      },
    }))
  }

  register({
    name: 'terraria_status',
    description: 'Check whether the local tModLoader bridge is reachable and whether Terraria has loaded a world.',
    command: 'ping',
  })
  register({
    name: 'terraria_observe',
    description: 'Read the current world, player position, health, mana, held item, buffs, weather, and time.',
    command: 'state',
  })
  register({
    name: 'terraria_scan',
    description: 'Read a square region of Terraria tiles around a tile coordinate. The radius is limited to 48 tiles.',
    parameters: {
      x: integer('Center tile X. Defaults to the player or world spawn.', { minimum: 0 }),
      y: integer('Center tile Y. Defaults to the player or world spawn.', { minimum: 0 }),
      radius: integer('Scan radius in tiles, from 1 to 48.', { minimum: 1, maximum: 48 }),
      walls: boolean('Include wall, liquid, and shape values.'),
    },
    command: 'scan',
    mapArgs: args => ({
      ...(args.x === undefined ? {} : { x: args.x }),
      ...(args.y === undefined ? {} : { y: args.y }),
      r: bounded(args.radius, 1, 48, 12),
      ...(args.walls === undefined ? {} : { walls: args.walls }),
    }),
  })
  register({
    name: 'terraria_tile',
    description: 'Inspect the tile, wall, liquid, slope, and actuator state at one world tile coordinate.',
    parameters: {
      x: integer('World tile X.', { required: true, minimum: 0 }),
      y: integer('World tile Y.', { required: true, minimum: 0 }),
    },
    command: 'tile',
    mapArgs: ({ x, y }) => ({ x, y }),
  })
  register({
    name: 'terraria_inventory',
    description: 'Read the local player inventory and currently selected slot.',
    command: 'inventory',
  })
  register({
    name: 'terraria_npcs',
    description: 'List active NPCs near the player or world spawn.',
    parameters: { radius: integer('Search radius in tiles.', { minimum: 1, maximum: 200 }) },
    command: 'npc',
    mapArgs: args => ({ r: bounded(args.radius, 1, 200, 80) }),
  })
  register({
    name: 'terraria_move',
    description: 'Hold a movement direction briefly. The bridge caps each move at 5 seconds.',
    parameters: {
      direction: string('Direction to hold.', { required: true, enum: ['left', 'right', 'up', 'down'] }),
      ms: integer('Duration in milliseconds, from 1 to 5000.', { minimum: 1, maximum: 5000 }),
    },
    command: 'move',
    mapArgs: ({ direction, ms }) => ({ dir: direction, ms: bounded(ms, 1, 5000, 500) }),
  })
  register({
    name: 'terraria_jump',
    description: 'Hold jump briefly. The bridge caps the hold at 2 seconds.',
    parameters: { ms: integer('Duration in milliseconds, from 1 to 2000.', { minimum: 1, maximum: 2000 }) },
    command: 'jump',
    mapArgs: args => ({ ms: bounded(args.ms, 1, 2000, 150) }),
  })
  register({
    name: 'terraria_use_item',
    description: 'Use the selected inventory item briefly, optionally selecting a slot first. Set right=true for alternate/right-click use.',
    parameters: {
      slot: integer('Inventory slot to select before use.'),
      right: boolean('Use alternate/right-click action.'),
      ms: integer('Duration in milliseconds, from 1 to 2000.', { minimum: 1, maximum: 2000 }),
    },
    command: 'use',
    mapArgs: args => ({
      ...(args.slot === undefined ? {} : { slot: args.slot }),
      ...(args.right === undefined ? {} : { right: args.right }),
      ms: bounded(args.ms, 1, 2000, 220),
    }),
  })
  register({
    name: 'terraria_attack',
    description: 'Continuously attack the nearest hostile NPC, or a specific NPC who index. The bridge keeps attacking for the requested duration and retargets nearby enemies when auto-targeting.',
    parameters: {
      who: integer('NPC who index from terraria_npcs. Omit to choose the nearest hostile NPC.', { minimum: 0 }),
      radius: integer('Auto-target radius in tiles.', { minimum: 1, maximum: 200 }),
      durationMs: integer('How long to keep attacking, from 100 to 15000 milliseconds.', { minimum: 100, maximum: 15000 }),
      intervalMs: integer('Delay between hits, from 30 to 2000 milliseconds.', { minimum: 30, maximum: 2000 }),
      slot: integer('Inventory slot containing the weapon to use.'),
    },
    command: 'attack',
    mapArgs: args => ({
      ...(args.who === undefined ? {} : { who: args.who }),
      r: bounded(args.radius, 1, 200, 40),
      ms: bounded(args.durationMs, 100, 15000, 3000),
      interval: bounded(args.intervalMs, 30, 2000, 180),
      ...(args.slot === undefined ? {} : { slot: args.slot }),
    }),
  })
  register({
    name: 'terraria_stop',
    description: 'Clear all movement, jump, use-item, and attack holds issued through the bridge.',
    command: 'stop',
  })
  register({
    name: 'terraria_spawn_npc',
    description: 'Spawn an NPC near the local player for controlled testing. This is a creative/admin action.',
    parameters: {
      type: integer('Terraria NPC type ID.', { minimum: 1 }),
      x: integer('World tile X. Defaults to five tiles right of the player.'),
      y: integer('World tile Y. Defaults to the player.'),
    },
    command: 'spawn_npc',
    mapArgs: ({ type, x, y }) => ({
      type: type ?? 1,
      ...(x === undefined ? {} : { x }),
      ...(y === undefined ? {} : { y }),
    }),
  })
  register({
    name: 'terraria_mine',
    description: 'Mine the tile at world tile coordinates. This changes the world and may drop items.',
    parameters: {
      x: integer('World tile X.', { required: true, minimum: 0 }),
      y: integer('World tile Y.', { required: true, minimum: 0 }),
      noItem: boolean('Suppress item drops.'),
    },
    command: 'mine',
    mapArgs: ({ x, y, noItem }) => ({ x, y, ...(noItem === undefined ? {} : { noItem }) }),
  })
  register({
    name: 'terraria_place',
    description: 'Place a tile at world tile coordinates using normal placement rules. This changes the world.',
    parameters: {
      x: integer('World tile X.', { required: true, minimum: 0 }),
      y: integer('World tile Y.', { required: true, minimum: 0 }),
      type: integer('Terraria tile type ID.', { required: true, minimum: 1 }),
      style: integer('Tile style, usually 0.', { minimum: 0 }),
    },
    command: 'place',
    mapArgs: ({ x, y, type, style }) => ({ x, y, type, style: style ?? 0, forced: false }),
  })
  register({
    name: 'terraria_teleport',
    description: 'Teleport the local player to a world tile coordinate.',
    parameters: {
      x: integer('Destination tile X.', { required: true, minimum: 0 }),
      y: integer('Destination tile Y.', { required: true, minimum: 0 }),
    },
    command: 'teleport',
    mapArgs: ({ x, y }) => ({ x, y }),
  })
  register({
    name: 'terraria_craft',
    description: 'Craft an item by Terraria item type ID using the local player inventory and nearby crafting stations.',
    parameters: {
      item: integer('Output item type ID.', { required: true, minimum: 1 }),
      times: integer('Number of crafts.', { minimum: 1, maximum: 99 }),
    },
    command: 'craft',
    mapArgs: ({ item, times }) => ({ item, times: bounded(times, 1, 99, 1) }),
  })
  register({
    name: 'terraria_give_item',
    description: 'Spawn an item stack into the local player inventory. This is a creative/admin action.',
    parameters: {
      type: integer('Terraria item type ID.', { required: true, minimum: 1 }),
      stack: integer('Stack size.', { minimum: 1, maximum: 9999 }),
    },
    command: 'give',
    mapArgs: ({ type, stack }) => ({ type, stack: bounded(stack, 1, 9999, 1) }),
  })
  register({
    name: 'terraria_chat',
    description: 'Send a message through the local Terraria client chat.',
    parameters: { text: string('Message to send.', { required: true }) },
    command: 'chat',
    mapArgs: ({ text }) => ({ text: String(text).slice(0, 400) }),
  })

  return () => client.close()
}
