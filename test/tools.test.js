import assert from 'node:assert/strict'
import net from 'node:net'
import test from 'node:test'
import { apply } from '../index.js'

/**
 * A stand-in bridge that records the exact request bodies the tools produce and
 * answers `{"ok":true,"data":{}}`. This is how we pin the argument mapping
 * without a game: every assertion below is about what actually goes on the wire.
 */
async function withBridge (run) {
  const seen = []
  const server = net.createServer(socket => {
    let buffer = ''
    socket.setEncoding('utf8')
    socket.on('data', chunk => {
      buffer += chunk
      let index
      while ((index = buffer.indexOf('\n')) >= 0) {
        const line = buffer.slice(0, index)
        buffer = buffer.slice(index + 1)
        if (!line.trim()) continue
        const request = JSON.parse(line)
        seen.push(request)
        const reply = { ok: true, data: {}, ...(request.req === undefined ? {} : { req: request.req }) }
        socket.write(JSON.stringify(reply) + '\n')
      }
    })
  })
  await new Promise((resolve, reject) => {
    server.once('error', reject)
    server.listen(0, '127.0.0.1', resolve)
  })
  const { port } = server.address()
  const registered = []
  const dispose = apply({
    tools: { register: tool => registered.push(tool) },
  }, { host: '127.0.0.1', port, timeoutMs: 3000 })
  const tool = name => registered.find(entry => entry.name === name)
  const exec = (name, args) => tool(name).execute(args, {})
  try {
    await run({ seen, exec, tool, registered })
  } finally {
    dispose()
    await new Promise(resolve => server.close(resolve))
  }
}

test('registers every documented tool with a supported schema', async () => {
  await withBridge(({ registered }) => {
    const names = registered.map(tool => tool.name).sort()
    for (const expected of [
      'terraria_status', 'terraria_observe', 'terraria_scan', 'terraria_tile', 'terraria_inventory',
      'terraria_npcs', 'terraria_move', 'terraria_jump', 'terraria_use_item', 'terraria_attack',
      'terraria_stop', 'terraria_follow', 'terraria_idle', 'terraria_players',
      'terraria_mine', 'terraria_place', 'terraria_craft', 'terraria_chat',
    ]) assert.ok(names.includes(expected), `missing ${expected}`)
  })
})

test('required coordinates are refused instead of defaulting to 0', async () => {
  await withBridge(async ({ exec, seen }) => {
    // The tool-def shim validates `required` before execute() runs, so the message
    // comes from there; the point is that nothing reaches the game with x/y missing.
    await assert.rejects(() => exec('terraria_tile', {}), /required property "x"/)
    await assert.rejects(() => exec('terraria_tile', { x: 5 }), /required property "y"/)
    await assert.rejects(() => exec('terraria_mine', { y: 3 }), /required property "x"/)
    await assert.rejects(() => exec('terraria_place', { x: 1, y: 2 }), /required property "type"/)
    // None of those may reach the game.
    assert.equal(seen.length, 0, `unexpected requests: ${JSON.stringify(seen)}`)
  })
})

test('cheat tools are gone: no spawn/teleport/give surface', async () => {
  await withBridge(({ registered }) => {
    const names = registered.map(tool => tool.name)
    for (const banned of ['terraria_spawn_npc', 'terraria_teleport', 'terraria_give_item'])
      assert.ok(!names.includes(banned), `${banned} must not be registered`)
  })
})

test('ids are validated before they reach the game', async () => {
  await withBridge(async ({ exec, seen }) => {
    await assert.rejects(() => exec('terraria_follow', { player: -1 }), /player must be between 0/)
    await assert.rejects(() => exec('terraria_place', { x: 1, y: 2, type: -5 }), /type must be between 0/)
    await assert.rejects(() => exec('terraria_craft', { item: 0 }), /item must be between 1/)
    await assert.rejects(() => exec('terraria_chat', { text: '   ' }), /must not be blank/)
    await assert.rejects(() => exec('terraria_move', { direction: 'sideways' }), /"direction" must be one of/)
    assert.equal(seen.length, 0)
  })
})

test('numeric strings are rejected up front instead of reaching the game', async () => {
  // Documents the current contract: the schema says integer, so "100" is refused
  // with a clear message rather than silently coerced. (loosenNumberParams exists
  // for the day we decide to accept strings; it is deliberately unwired.)
  await withBridge(async ({ exec, seen }) => {
    await assert.rejects(() => exec('terraria_tile', { x: '100', y: '50' }), /must be an integer/)
    assert.equal(seen.length, 0)
  })
})

test('coordinates travel as numbers once accepted', async () => {
  await withBridge(async ({ exec, seen }) => {
    await exec('terraria_tile', { x: 100, y: 50 })
    assert.equal(seen.length, 1)
    assert.deepEqual(seen[0].cmd, 'tile')
    assert.deepEqual(seen[0].params, { x: 100, y: 50 })
  })
})

test('scan picks a cheap format above radius 24 and stays exact below it', async () => {
  await withBridge(async ({ exec, seen }) => {
    await exec('terraria_scan', { radius: 12 })
    await exec('terraria_scan', { radius: 30 })
    await exec('terraria_scan', { radius: 999 })
    await exec('terraria_scan', { radius: 30, format: 'rows' })
    assert.equal(seen[0].params.format, 'rows')
    assert.equal(seen[1].params.format, 'ascii')
    assert.equal(seen[2].params.r, 48, 'radius is clamped to the bridge limit')
    assert.equal(seen[3].params.format, 'rows', 'an explicit format wins')
  })
})

test('bridge results are rendered compactly, not pretty-printed', async () => {
  await withBridge(async ({ exec }) => {
    const text = await exec('terraria_state_unknown_guard' in {} ? 'x' : 'terraria_status', {})
    const parsed = JSON.parse(text)
    assert.equal(parsed.ok, true)
    assert.equal(text.includes('\n'), false, 'output must stay on one line')
  })
})

test('a paused game gets an actionable hint', async () => {
  // The fake bridge answers with data:{} so nothing is paused; assert instead that
  // the hint helper is wired by simulating a paused status reply.
  const server = net.createServer(socket => {
    socket.setEncoding('utf8')
    socket.on('data', () => {
      socket.write(JSON.stringify({ ok: true, data: { pong: true, version: '0.2.0', inWorld: true, paused: true, hasFocus: false } }) + '\n')
    })
  })
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve))
  const { port } = server.address()
  const registered = []
  const dispose = apply({ tools: { register: tool => registered.push(tool) } }, { port, timeoutMs: 3000 })
  try {
    const status = registered.find(tool => tool.name === 'terraria_status')
    const parsed = JSON.parse(await status.execute({}, {}))
    assert.equal(parsed.data.paused, true)
    assert.match(parsed.data.hint, /Pause when unfocused/)
  } finally {
    dispose()
    await new Promise(resolve => server.close(resolve))
  }
})
test('tile type 0 (Dirt) is a legal placement', async () => {
  await withBridge(async ({ exec, seen }) => {
    await exec('terraria_place', { x: 10, y: 20, type: 0 })
    // Player mode places, then re-reads the tile to verify, so there is more than one call.
    assert.equal(seen[0].cmd, 'place')
    assert.equal(seen[0].params.type, 0, 'dirt must survive argument mapping')
    assert.equal(seen[1].cmd, 'tile')
  })
})

test('the wall command reaches the bridge with its wall id', async () => {
  await withBridge(async ({ exec, seen }) => {
    await exec('terraria_wall', { x: 3, y: 4, type: 2 })
    assert.deepEqual(seen[0].cmd, 'wall')
    assert.deepEqual(seen[0].params, { x: 3, y: 4, type: 2 })
    await assert.rejects(() => exec('terraria_wall', { x: 3, y: 4, type: 0 }), /type must be between 1/)
  })
})
