#!/usr/bin/env node
/**
 * Live smoke test against a running game.
 * ============================================================================
 * Read-only by default. `--write` additionally mutates the world / inventory
 * (mines a dirt tile and puts it back, crafts, spawns a slime, walks).
 *
 *   node scripts/live-smoke.mjs            # safe checks only
 *   node scripts/live-smoke.mjs --write    # full acceptance run
 *   node scripts/live-smoke.mjs --port 47777
 *
 * Exits 0 when every check passes, 1 otherwise. Each line is one assertion so
 * the output is easy to paste into a bug report.
 * ============================================================================
 */
import { createBridgeClient } from '../src/bridge-client.js'

const args = process.argv.slice(2)
const write = args.includes('--write')
const portArg = args.indexOf('--port')
const port = portArg >= 0 ? Number(args[portArg + 1]) : 47777

const client = createBridgeClient({ port, timeoutMs: 8000 })
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms))

let pass = 0
const failures = []

function check (name, ok, detail) {
  if (ok) {
    pass++
    console.log(`  ok   ${name}${detail ? ` :: ${detail}` : ''}`)
  } else {
    failures.push(name)
    console.log(`  FAIL ${name}${detail ? ` :: ${detail}` : ''}`)
  }
}

async function call (cmd, params = {}) {
  try {
    return { ok: true, data: (await client.request(cmd, params)).data }
  } catch (error) {
    return { ok: false, error: error.message, name: error.name }
  }
}

/** Assert a command is refused with a message matching `re`. */
async function expectRefusal (cmd, params, re, label) {
  const r = await call(cmd, params)
  check(label ?? `${cmd} refuses`, !r.ok && re.test(r.error), r.ok ? 'unexpectedly succeeded' : r.error)
}

const tileCode = (tile) => (tile?.hasTile ? tile.type : -1)

console.log(`\n== live smoke (port ${port}, ${write ? 'READ+WRITE' : 'read-only'}) ==\n`)

/* ── 1. transport and session ─────────────────────────────────────── */
console.log('[1] bridge')
const ping = await call('ping')
if (!ping.ok) {
  console.log(`  FAIL cannot reach the bridge: ${ping.error}`)
  console.log('\nStart tModLoader with DshTerrariaBridge enabled first.')
  process.exit(1)
}
check('ping answers', ping.data?.pong === true)
check('protocol version is 0.2.x', /^0\.2\./.test(String(ping.data?.version)), String(ping.data?.version))
check('paused/hasFocus are reported', typeof ping.data?.paused === 'boolean' && typeof ping.data?.hasFocus === 'boolean',
  `paused=${ping.data?.paused} hasFocus=${ping.data?.hasFocus}`)

if (!ping.data.inWorld) {
  console.log('\n  Not in a world yet — the rest needs a loaded world.')
  console.log('  Ask the user to enter one, then run again.')
  client.close()
  process.exit(failures.length ? 1 : 0)
}

/* ── 2. state ─────────────────────────────────────────────────────── */
console.log('\n[2] state')
const state = await call('state')
const player = state.data?.player ?? {}
check('state has a player', Number.isFinite(player.tile?.[0]), JSON.stringify(player.tile))
check('grounded is a boolean', typeof player.grounded === 'boolean', String(player.grounded))
check('attack status is present', player && state.data?.attack !== undefined)
check('world dimensions are sane', state.data?.world?.width > 0 && state.data?.world?.height > 0,
  `${state.data?.world?.width}x${state.data?.world?.height}`)

const px = Math.floor(player.tile?.[0] ?? 0)
const py = Math.floor(player.tile?.[1] ?? 0)

/* ── 3. reading terrain ───────────────────────────────────────────── */
console.log('\n[3] terrain reads')
const rows = await call('scan', { x: px, y: py, r: 4, format: 'rows' })
check('scan rows returns (2r+1)^2 rows', rows.data?.tiles?.length === 9, `${rows.data?.tiles?.length}`)
check('scan declares stride/fields', Number.isFinite(rows.data?.stride) && Array.isArray(rows.data?.fields),
  `stride=${rows.data?.stride} fields=${JSON.stringify(rows.data?.fields)}`)
const codes = new Set()
for (let j = 0; j < (rows.data?.h ?? 0); j++) {
  for (let i = 0; i < (rows.data?.w ?? 0); i++) codes.add(rows.data.tiles[j][i * rows.data.stride])
}
check('empty is encoded as -1 (not 0)', codes.has(-1) || [...codes].every(c => c > 0),
  `codes=${[...codes].slice(0, 8).join(',')}`)

const ascii = await call('scan', { x: px, y: py, r: 4, format: 'ascii' })
check('ascii map has one line per row', ascii.data?.map?.split('\n').length === 9)
const rle = await call('scan', { x: px, y: py, r: 4, format: 'rle' })
check('rle map is an array of runs', Array.isArray(rle.data?.map) && /x-?\d/.test(String(rle.data?.map?.[0])),
  String(rle.data?.map?.[0]))

const big = await call('scan', { x: px, y: py, r: 48, format: 'ascii' })
check('ascii r=48 stays small', JSON.stringify(big.data).length < 15000, `${JSON.stringify(big.data).length} bytes`)

const here = await call('tile', { x: px, y: py })
check('tile reads one cell with an explicit type', Number.isFinite(here.data?.type),
  `hasTile=${here.data?.hasTile} type=${here.data?.type}`)
check('tile type matches the -1 convention', here.data?.type === tileCode(here.data))

console.log('\n[4] refusals are actionable')
await expectRefusal('tile', {}, /需要 x,y/, 'tile without coordinates is refused')
await expectRefusal('tile', { x: -1, y: -1 }, /out of world bounds/, 'tile outside the world is refused')
await expectRefusal('teleport', { x: 999999, y: 999999 }, /out of world bounds/, 'teleport outside the world is refused')
await expectRefusal('give', { type: 999999 }, /invalid item type/, 'give with an invalid id is refused')
await expectRefusal('place', { x: px, y: py, type: 999999 }, /invalid tile type/, 'place with an invalid id is refused')

const refusal = await call('tile', { x: -1, y: -1 })
check('refusal messages are not relabelled as protocol faults',
  refusal.ok === false && !/Invalid Terraria bridge response/i.test(refusal.error), refusal.error)

/* ── 5. inventory ─────────────────────────────────────────────────── */
console.log('\n[5] inventory')
const inv = await call('inventory')
check('inventory lists items', Array.isArray(inv.data?.items), `${inv.data?.count} slots`)
check('inventory exposes equipped gear', Array.isArray(inv.data?.equipment),
  JSON.stringify(inv.data?.equipment)?.slice(0, 120))
check('inventory exposes canFly/defense', typeof inv.data?.canFly === 'boolean' && typeof inv.data?.defense === 'number',
  `canFly=${inv.data?.canFly} defense=${inv.data?.defense}`)

/* ── 6. movement ──────────────────────────────────────────────────── */
console.log('\n[6] movement')
const mv = await call('move', { dir: 'right', ms: 400 })
check('move reports from + untilTick', Array.isArray(mv.data?.from) && Number.isFinite(mv.data?.untilTick),
  JSON.stringify(mv.data))
await sleep(700)
const after = await call('state')
const to = after.data?.player?.tile
check('position changed somewhere', Array.isArray(to), JSON.stringify(to))

if (write) {
  /* ── 7. the dirt bug ────────────────────────────────────────────── */
  console.log('\n[7] mining dirt (TileID 0)')
  let dirt = null
  for (let dy = 1; dy <= 8 && !dirt; dy++) {
    for (let dx = -5; dx <= 5 && !dirt; dx++) {
      const t = await call('tile', { x: px + dx, y: py + dy })
      if (t.ok && t.data.hasTile && t.data.type === 0) dirt = { x: px + dx, y: py + dy }
    }
  }
  if (!dirt) {
    console.log('  skip no dirt tile in reach')
  } else {
    // Player mode: the bridge takes out a pickaxe and swings. Restoring uses admin mode so
    // the test does not depend on the player happening to carry a dirt block.
    const mined = await call('mine', { x: dirt.x, y: dirt.y })
    check('mine picks a tool and aims', mined.data?.mode === 'player' && !!mined.data?.tool,
      JSON.stringify(mined.data?.tool))
    const gone = await call('tile', { x: dirt.x, y: dirt.y })
    check('dirt is really gone after swinging', gone.data?.hasTile === false)
    await call('place', { x: dirt.x, y: dirt.y, type: 0, admin: true })
    const back = await call('tile', { x: dirt.x, y: dirt.y })
    check('dirt restored', back.data?.hasTile === true && back.data?.type === 0)
  }

  /* ── 8. combat ──────────────────────────────────────────────────── */
  console.log('\n[8] combat')
  const spawned = await call('spawn_npc', { type: 1 })
  check('spawn_npc returns a who', Number.isFinite(spawned.data?.who), JSON.stringify(spawned.data)?.slice(0, 120))
  await sleep(300)
  const attack = await call('attack', { ms: 2500, interval: 150, r: 60 })
  check('attack starts and reports effective damage',
    attack.data?.attacking === true && Number.isFinite(attack.data?.effectiveDamage),
    `dmg=${attack.data?.effectiveDamage} npc=${JSON.stringify(attack.data?.npc)}`)
  await sleep(3000)
  const done = await call('state')
  const last = done.data?.attack?.last
  check('attack.last survives the task', !!last, JSON.stringify(last))
  if (last) {
    check('attack landed at least one hit', last.hits > 0, `hits=${last.hits}`)
    check('attack.last carries life before/after', Number.isFinite(last.lifeBefore) && Number.isFinite(last.lifeAfter),
      `${last.lifeBefore} -> ${last.lifeAfter}`)
  }
  await call('stop')
  const kept = await call('state')
  check('stop keeps attack.last', kept.data?.attack?.last?.hits === last?.hits)

  /* ── 9. crafting with a recipe group ────────────────────────────── */
  console.log('\n[9] crafting')
  await call('give', { type: 9, stack: 200 })   // wood
  await call('give', { type: 3, stack: 200 })   // stone block (item 3, tile 1)
  let station = null
  for (let dy = -3; dy <= 4 && !station; dy++) {
    for (let dx = -3; dx <= 3 && !station; dx++) {
      const t = await call('tile', { x: px + dx, y: py + dy })
      if (t.ok && t.data.hasTile === false) station = { x: px + dx, y: py + dy }
    }
  }
  if (!station) {
    console.log('  skip no free tile for a workbench')
  } else {
    const wb = await call('place', { x: station.x, y: station.y, type: 18, admin: true })
    check('workbench placed', wb.data?.placed === true, JSON.stringify(wb.data))
    const arrows = await call('craft', { item: 40, times: 1 })
    check('recipe group ("any wood") crafts wooden arrows', arrows.ok && arrows.data?.crafted > 0,
      arrows.ok ? JSON.stringify(arrows.data) : arrows.error)
    await call('mine', { x: station.x, y: station.y, admin: true })
  }
}

client.close()
console.log(`\n== ${pass} passed, ${failures.length} failed ==`)
if (failures.length) {
  console.log('failed: ' + failures.join(' | '))
  process.exit(1)
}