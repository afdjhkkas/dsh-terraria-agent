import test from 'node:test'
import assert from 'node:assert/strict'
import { createPrimitives, isTransportFault } from '../src/primitives.js'

/**
 * Scripted stand-in for the Terraria bridge.
 *
 * The real bridge answers a command immediately and the game applies it on a
 * later frame, so every case here can decide *when* an effect becomes visible
 * (`applyAfter`), which is what the primitives' polling has to cope with.
 */
function makeFakeBridge({
  tiles = {},
  inventory = [],
  chest = [],
  chestPos = { x: 100, y: 50 },
  applyAfter = 0,
  placeConsumes = 1,
  craftGain = null,
  equipWorks = true,
  offScreen = false,
} = {}) {
  const clock = { t: 0 }
  const pending = []
  const state = {
    tiles: { ...tiles },
    inventory: inventory.map(it => ({ ...it })),
    chest: chest.map(it => ({ ...it })),
    calls: [],
  }
  const key = (x, y) => `${x},${y}`
  const flush = () => {
    if (clock.t >= applyAfter) {
      while (pending.length) pending.shift()()
    }
  }
  const countItem = type => state.inventory.filter(it => it.type === type).reduce((n, it) => n + it.stack, 0)

  async function request(command, params = {}) {
    state.calls.push({ command, params })
    flush()
    switch (command) {
      case 'tile': {
        const cell = state.tiles[key(params.x, params.y)]
        return { data: cell ?? { x: params.x, y: params.y, hasTile: false, type: -1, wall: 0 } }
      }
      case 'mine': {
        const k = key(params.x, params.y)
        const cell = state.tiles[k]
        apply(() => {
          if (cell && cell.breaksAfter !== undefined) {
            cell.breaksAfter -= 1
            if (cell.breaksAfter <= 0) state.tiles[k] = air(params.x, params.y)
          }
        })
        return { data: { held: 'Iron Pickaxe', onScreen: !offScreen } }
      }
      case 'place': {
        const idx = state.inventory.findIndex(it => it.type === params.type)
        const entry = idx >= 0 ? state.inventory[idx] : null
        const evidence = { slot: entry?.slot ?? -1, stackBefore: entry?.stack ?? 0, name: `tile-${params.type}`, item: entry ? 'item' : null }
        apply(() => {
          if (!entry || entry.stack <= 0) return
          // A 3x2 furniture (the furnace) really occupies root..root+2/root+1,
          // and the player aimed at root + origin (1,1).
          if (params.type === 33) {
            for (let dx = -1; dx <= 1; dx += 1) {
              for (let dy = -1; dy <= 0; dy += 1) {
                const tx = params.x + dx, ty = params.y + dy
                state.tiles[key(tx, ty)] = { x: tx, y: ty, hasTile: true, type: params.type, wall: 0 }
              }
            }
          } else {
            state.tiles[key(params.x, params.y)] = { x: params.x, y: params.y, hasTile: true, type: params.type, wall: 0 }
          }
          entry.stack -= placeConsumes
          if (entry.stack <= 0) state.inventory.splice(idx, 1)
        })
        return { data: evidence }
      }
      case 'tiledata': {
        const data = params.type === 33
          ? { hasObjectData: true, width: 3, height: 2, originX: 1, originY: 1, styleWrapLimit: 0 }
          : { hasObjectData: true, width: 1, height: 2, originX: 0, originY: 0, styleWrapLimit: 0 }
        return { data }
      }
      case 'inventory':
        return {
          data: {
            items: state.inventory.filter(it => it.stack > 0).map((it, i) => ({ slot: it.slot ?? i, type: it.type, name: it.name ?? `item-${it.type}`, stack: it.stack })),
            equipment: state.equipment ?? [],
            defense: state.defense ?? 0,
          },
        }
      case 'craft': {
        const gain = craftGain ?? { crafted: params.times, dropped: 0, perCraft: 1, name: 'thing' }
        apply(() => {
          if (gain.crafted > 0) {
            const produced = gain.crafted * (gain.perCraft ?? 1)
            const existing = state.inventory.find(it => it.type === params.item)
            if (existing) existing.stack += produced
            else state.inventory.push({ type: params.item, stack: produced, name: gain.name, slot: 20 + state.inventory.length })
          }
        })
        return { data: gain }
      }
      case 'equip': {
        const idx = state.inventory.findIndex(it => it.slot === params.slot)
        const item = idx >= 0 ? state.inventory[idx] : null
        apply(() => {
          if (!item || !equipWorks) return
          state.equipment = [{ slot: 0, kind: 'armor', type: item.type, name: item.name, defense: 3 }]
          state.defense = 3
          state.inventory.splice(idx, 1)
        })
        return { data: { equipped: item?.name ?? null } }
      }
      case 'chest': {
        if (params.action === 'list') {
          return { data: { chest: chestPos, items: state.chest.filter(it => it.stack > 0).map(it => ({ slot: it.slot, type: it.type, name: it.name, stack: it.stack })) } }
        }
        if (params.action === 'put') {
          const idx = state.inventory.findIndex(it => it.slot === params.invSlot)
          const item = idx >= 0 ? state.inventory[idx] : null
          const chestSlot = state.chest.length
          apply(() => {
            if (!item) return
            state.chest.push({ slot: chestSlot, type: item.type, name: item.name, stack: item.stack })
            state.inventory.splice(idx, 1)
          })
          return { data: { put: item?.name ?? null, chestSlot } }
        }
        if (params.action === 'take') {
          const entry = state.chest.find(it => it.slot === params.slot)
          const moved = entry ? (params.count > 0 ? Math.min(params.count, entry.stack) : entry.stack) : 0
          apply(() => {
            if (!entry) return
            entry.stack -= moved
            const bag = state.inventory.find(it => it.type === entry.type)
            if (bag) bag.stack += moved
            else state.inventory.push({ slot: 30 + state.inventory.length, type: entry.type, name: entry.name, stack: moved })
          })
          return { data: { took: entry?.name ?? null, count: moved, slot: params.slot } }
        }
        throw new Error(`unexpected chest action ${params.action}`)
      }
      default:
        throw new Error(`unexpected command ${command}`)
    }
  }

  function apply(fn) {
    pending.push(fn)
    return undefined
  }

  const sleep = async () => { clock.t += 120 }
  const primitives = createPrimitives({ request, sleep, now: () => clock.t })
  return { request, primitives, state, clock }
}

const stone = (x, y, extra = {}) => ({ x, y, hasTile: true, type: 1, wall: 0, ...extra })
const air = (x, y) => ({ x, y, hasTile: false, type: -1, wall: 0 })

test('mineTile keeps swinging until the tile is gone', async () => {
  const fake = makeFakeBridge({ tiles: { '5,5': stone(5, 5, { breaksAfter: 2 }) } })
  const result = await fake.primitives.mineTile({ x: 5, y: 5, ms: 100 })
  assert.equal(result.ok, true)
  assert.equal(result.removed, true)
  assert.equal(result.attempts, 2)
  assert.equal(fake.state.tiles['5,5'].hasTile, false)
})

test('mineTile fails immediately when the target is off screen', async () => {
  const fake = makeFakeBridge({ tiles: { '5,5': stone(5, 5) }, offScreen: true })
  const result = await fake.primitives.mineTile({ x: 5, y: 5, ms: 10, tries: 3 })
  assert.equal(result.ok, false)
  assert.match(result.reason, /屏幕内/)
  assert.equal(result.attempts, 1)
})

test('mineTile reports a tile that refuses to break', async () => {
  const fake = makeFakeBridge({ tiles: { '5,5': stone(5, 5) } })
  const result = await fake.primitives.mineTile({ x: 5, y: 5, ms: 10, tries: 2 })
  assert.equal(result.ok, false)
  assert.equal(result.attempts, 2)
  assert.match(result.reason, /还没挖掉/)
})

test('mineTile treats an already empty cell as done', async () => {
  const fake = makeFakeBridge()
  const result = await fake.primitives.mineTile({ x: 1, y: 1 })
  assert.equal(result.ok, true)
  assert.equal(result.alreadyEmpty, true)
  assert.equal(fake.state.calls.length, 1)
})

test('placeTile waits for the tile and proves the stack was consumed', async () => {
  const fake = makeFakeBridge({ inventory: [{ slot: 3, type: 1, stack: 5 }], applyAfter: 240 })
  const result = await fake.primitives.placeTile({ x: 7, y: 7, type: 1 })
  assert.equal(result.ok, true)
  assert.equal(result.placed, true)
  assert.equal(result.consumed, 1)
  assert.equal(fake.state.tiles['7,7'].type, 1)
})

test('placeTile refuses an occupied cell without swinging', async () => {
  const fake = makeFakeBridge({ tiles: { '7,7': { x: 7, y: 7, hasTile: true, type: 2, wall: 0 } }, inventory: [{ slot: 3, type: 1, stack: 5 }] })
  const result = await fake.primitives.placeTile({ x: 7, y: 7, type: 1 })
  assert.equal(result.ok, false)
  assert.match(result.reason, /已经有/)
  assert.equal(fake.state.calls.some(c => c.command === 'place'), false)
})

test('placeTile flags a tile that appeared without the item being consumed', async () => {
  const fake = makeFakeBridge({ inventory: [{ slot: 3, type: 1, stack: 5 }], placeConsumes: 0 })
  const result = await fake.primitives.placeTile({ x: 8, y: 8, type: 1 })
  assert.equal(result.ok, false)
  assert.equal(result.placed, true)
  assert.match(result.reason, /物品流/)
})

test('placeObject aims at root + origin and checks every footprint cell', async () => {
  const fake = makeFakeBridge({ inventory: [{ slot: 2, type: 33, stack: 1 }] })
  const result = await fake.primitives.placeObject({ x: 20, y: 30, type: 33 })
  assert.equal(result.ok, true)
  assert.equal(result.complete, true)
  assert.equal(result.filled, 6)
  // 3x2 furnace with origin (1,1) is placed by aiming one right and one down.
  const placed = fake.state.calls.filter(c => c.command === 'place')
  assert.deepEqual(placed.map(c => [c.params.x, c.params.y]), [[21, 31]])
})

test('placeObject reports a partial footprint as a failure', async () => {
  const fake = makeFakeBridge({ inventory: [{ slot: 2, type: 33, stack: 1 }] })
  // A responder that only ever fills the aimed cell.
  const oneCell = async (command, params = {}) => {
    if (command === 'place') {
      fake.state.tiles[`${params.x},${params.y}`] = { x: params.x, y: params.y, hasTile: true, type: params.type }
      fake.state.inventory = fake.state.inventory.filter(it => it.type !== params.type)
      return { data: { slot: 2, stackBefore: 1, name: 'furnace' } }
    }
    return fake.request(command, params)
  }
  const primitives = createPrimitives({ request: oneCell, sleep: async () => {}, now: () => 0 })
  const result = await primitives.placeObject({ x: 20, y: 30, type: 33 })
  assert.equal(result.ok, false)
  assert.equal(result.complete, false)
  assert.ok(result.filled < 6)
  assert.match(result.reason, /锚点或支撑/)
})

test('craftItem waits for the products and reports the gain', async () => {
  const fake = makeFakeBridge({ inventory: [{ slot: 0, type: 9, stack: 50 }], craftGain: { crafted: 1, dropped: 0, perCraft: 6, name: 'wall' }, applyAfter: 120 })
  const result = await fake.primitives.craftItem({ item: 30, times: 1 })
  assert.equal(result.ok, true)
  assert.equal(result.gained, 6)
  assert.equal(result.expected, 6)
})

test('craftItem warns when the products fell on the floor because the bag is full', async () => {
  const fake = makeFakeBridge({ craftGain: { crafted: 0, dropped: 1, perCraft: 1, name: 'chair' } })
  const result = await fake.primitives.craftItem({ item: 34, times: 1 })
  assert.equal(result.ok, true)
  assert.match(result.warning, /背包放不下/)
})

test('craftItem explains a craft that produced nothing', async () => {
  const fake = makeFakeBridge({ craftGain: { crafted: 0, dropped: 0, perCraft: 1, name: 'anvil' } })
  const result = await fake.primitives.craftItem({ item: 35, times: 1 })
  assert.equal(result.ok, false)
  assert.match(result.reason, /材料不够|工作台/)
})

test('equipFromSlot verifies the armour slot and the defence it grants', async () => {
  const fake = makeFakeBridge({ inventory: [{ slot: 4, type: 11, stack: 1, name: 'Iron Helmet' }], applyAfter: 120 })
  const result = await fake.primitives.equipFromSlot({ slot: 4 })
  assert.equal(result.ok, true)
  assert.equal(result.defenseAfter, 3)
  assert.equal(result.defenseBefore, 0)
})

test('equipFromSlot fails when the item never reaches an equipment slot', async () => {
  const fake = makeFakeBridge({ inventory: [{ slot: 4, type: 11, stack: 1, name: 'Iron Helmet' }], equipWorks: false })
  const result = await fake.primitives.equipFromSlot({ slot: 4 })
  assert.equal(result.ok, false)
  assert.match(result.reason, /装备栏/)
})

test('chestPut moves the item and proves both sides changed', async () => {
  const fake = makeFakeBridge({ inventory: [{ slot: 6, type: 19, stack: 8, name: 'Bomb' }], applyAfter: 120 })
  const result = await fake.primitives.chestPut({ invSlot: 6 })
  assert.equal(result.ok, true)
  assert.equal(result.chestItemsAfter, 1)
  assert.equal(fake.state.chest[0].type, 19)
})

test('chestTake returns the count that really arrived in the bag', async () => {
  const fake = makeFakeBridge({ chest: [{ slot: 0, type: 19, name: 'Bomb', stack: 5 }], applyAfter: 120 })
  const result = await fake.primitives.chestTake({ slot: 0, count: 3 })
  assert.equal(result.ok, true)
  assert.equal(result.count, 3)
  assert.equal(result.bagAfter, 3)
  assert.equal(fake.state.chest[0].stack, 2)
})

test('isTransportFault separates a dead bridge from a gameplay answer', () => {
  assert.equal(isTransportFault(Object.assign(new Error('x'), { code: 'ETIMEDOUT' })), true)
  assert.equal(isTransportFault(new Error('Terraria bridge is not running at 127.0.0.1:47777.')), true)
  assert.equal(isTransportFault(new Error('背包里没有能放置 Stone 的物品')), false)
})