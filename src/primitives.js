/**
 * Verified world/inventory primitives.
 * ============================================================================
 * The bridge commands are fire-and-forget: `mine` arms a swing, `place` arms a
 * use, and the command answers before the game has processed a single frame.
 * A tool that just `sleep(500)` and reads once reports "placed: true" whenever
 * timing was lucky and "failed" when it was not - which is exactly the failure
 * mode that made every earlier build attempt look broken-but-random.
 *
 * Every helper here therefore owns one guarantee: **it does not return until
 * the world (or the inventory) shows the effect, or it ran out of its own
 * deadline and says so.** Poll intervals and deadlines are arguments, not
 * constants, so a slow pickaxe and a fast one can both be described.
 *
 * Failure is a value, not an exception: `{ ok: false, reason }` with the
 * evidence that produced it. Tools surface that; only transport faults throw.
 * ============================================================================
 */

import { MAX_SUSTAINED_ATTEMPTS } from './action-loop.js'

/** Default poll cadence while waiting for the game to apply an action (ms). */
const POLL_MS = 120
const defaultSleep = ms => new Promise(resolve => setTimeout(resolve, ms))

/** One tile cell as the bridge reports it. */
const isEmptyCell = tile => tile != null && tile.hasTile === false

/** Was `err` a transport/timeout fault rather than a gameplay answer? */
export function isTransportFault(err) {
  return err?.code === 'ETIMEDOUT' || /bridge is not running|closed the connection|Invalid Terraria bridge/i.test(String(err?.message))
}

/**
 * @param {object} deps
 * @param {(command:string, params?:object, options?:{signal?:AbortSignal}) => Promise<{data:any}>} deps.request
 * @param {(ms:number) => Promise<void>} [deps.sleep]
 * @param {() => number} [deps.now]
 */
export function createPrimitives({ request, sleep = defaultSleep, now = Date.now } = {}) {
  if (typeof request !== 'function') throw new TypeError('createPrimitives needs a request() function')

  const readTile = async (x, y, signal) => (await request('tile', { x, y }, { signal })).data ?? {}

  /** Poll `probe` until it returns truthy or the deadline passes. */
  async function pollUntil(probe, { deadlineMs = 3000, intervalMs = POLL_MS, signal } = {}) {
    const deadline = now() + deadlineMs
    let attempts = 0
    for (;;) {
      attempts += 1
      const value = await probe(attempts)
      if (value) return { hit: true, attempts, value }
      if (now() >= deadline) return { hit: false, attempts, value }
      await sleep(intervalMs)
    }
  }

  /* ── mining ─────────────────────────────────────────────────────────── */

  /**
   * Swing until the tile is actually gone.
   *
   * A tile that needs two swings, or a swing the game dropped because the cell
   * was out of reach for a frame, now costs one more attempt instead of a false
   * "removed: false" the caller has to interpret.
   *
   * @param {{x:number,y:number,ms?:number,tries?:number,signal?:AbortSignal}} args
   */
  async function mineTile({ x, y, ms = 1300, tries = MAX_SUSTAINED_ATTEMPTS, signal } = {}) {
    const before = await readTile(x, y, signal)
    if (isEmptyCell(before)) return { ok: true, removed: false, alreadyEmpty: true, x, y, before: before.type }

    const swings = []
    for (let attempt = 1; attempt <= tries; attempt += 1) {
      const armed = (await request('mine', { x, y, ms }, { signal })).data ?? {}
      await sleep(ms + 350)
      const after = await readTile(x, y, signal)
      swings.push({ attempt, held: armed.held ?? null, aimed: armed.onScreen ?? null, type: after.type })
      if (isEmptyCell(after)) {
        return { ok: true, removed: true, x, y, attempts: attempt, before: before.type, swings }
      }
      if (armed.onScreen === false) {
        // Not a timing problem: the cell is off screen, so more swings cannot help.
        return {
          ok: false, removed: false, x, y, attempts: attempt, before: before.type, swings,
          reason: '目标不在屏幕内：先走近（离玩家 7 格以内且可见）再挖',
        }
      }
    }
    const final = await readTile(x, y, signal)
    return {
      ok: false, removed: false, x, y, attempts: tries, before: before.type, after: final.type, swings,
      reason: `挥了 ${tries} 次还没挖掉（现在是 ${final.type ?? -1}）：可能工具等级不够，或这一段需要更长时间`,
    }
  }

  /* ── tile / furniture placement ─────────────────────────────────────── */

  /**
   * Move one step, clearing whatever the body is caught on first.
   *
   * A 1.25-tile-wide hitbox wedged into a narrow slot makes `move` answer
   * `ok:true` while the position never changes - the cell that blocks is the one
   * at **head height**, so digging only the floor row leaves the player stuck
   * (measured: position identical for 46 steps). This clears the four rows the
   * body spans - and one more ahead - then retries, and reports the real
   * displacement instead of trusting the command.
   */
  async function walkClear({ dir = 'right', ms = 500, attempts = 4, signal } = {}) {
    const dx = dir === 'left' ? -1 : dir === 'right' ? 1 : 0
    const dy = dir === 'up' ? -1 : dir === 'down' ? 1 : 0
    const here = async () => ((await request('state', {}, { signal })).data ?? {}).player ?? {}
    const trace = []
    for (let attempt = 1; attempt <= attempts; attempt += 1) {
      const before = await here()
      const [px, py] = before.pos ?? [0, 0]
      const cleared = []
      const cells = []
      if (dx !== 0) {
        // 碰撞盒 1.25 格宽：只清正前方那一列时，隔壁那列照样把身体顶住
        // （实测站在 2 格宽的竖井里，前方 1 格是空的但第 2 格实心，位移 0）
        for (const col of [Math.floor(px) + dx, Math.floor(px) + 2 * dx]) {
          for (const row of [Math.round(py) - 2, Math.round(py) - 1, Math.round(py), Math.round(py) + 1]) cells.push([col, row])
        }
      } else if (dy !== 0) {
        const row = Math.round(py) + dy
        for (const col of [Math.floor(px) - 1, Math.floor(px), Math.floor(px) + 1]) cells.push([col, row])
      }
      for (const [x, y] of cells) {
        const cell = await readTile(x, y, signal)
        if (cell.solid !== true) continue
        const swing = await mineTile({ x, y, ms: 1400, tries: 2, signal })
        if (swing.ok) cleared.push(`${x},${y}`)
      }
      if (cleared.length) await sleep(300)
      await request('move', { dir, ms }, { signal })
      await sleep(ms + 250)
      const after = await here()
      const [x2, y2] = after.pos ?? [0, 0]
      const moved = Math.hypot(x2 - px, y2 - py)
      trace.push({ attempt, from: [px, py], to: [x2, y2], moved: +moved.toFixed(2), cleared })
      if (moved >= 0.4) return { ok: true, dir, moved: +moved.toFixed(2), attempt, cleared, trace }
    }
    const last = trace[trace.length - 1] ?? { moved: 0 }
    return {
      ok: false, dir, moved: last.moved ?? 0, trace,
      reason: `朝 ${dir} 走了 ${attempts} 次都没挪动（位移 ${last.moved ?? 0} 格）：前方夹住、或是坡/液体挡路，先看清再换方向`,
    }
  }

  /** Footprint + anchor of a furniture type, straight from the game. */
  async function objectInfo(type, { style = 0, signal } = {}) {
    const data = (await request('tiledata', { type, style }, { signal })).data ?? {}
    if (!data.hasObjectData) return { type, width: 1, height: 1, originX: 0, originY: 0 }
    return { type, width: data.width, height: data.height, originX: data.originX, originY: data.originY, styleWrapLimit: data.styleWrapLimit }
  }

  /**
   * Place one tile and wait for it to be there.
   *
   * `x,y` is the **cell the player aims at** - the same cell the cursor would be
   * on. For a multi-cell furniture use placeObject(), which works in footprint
   * coordinates instead.
   */
  async function placeTile({ x, y, type, style = 0, deadlineMs = 4000, signal } = {}) {
    const before = await readTile(x, y, signal)
    if (before.hasTile === true && before.type === type) {
      return { ok: true, placed: false, alreadyThere: true, x, y, type }
    }
    if (before.hasTile === true) {
      return { ok: false, placed: false, x, y, type, reason: `这一格已经有 ${before.type}（${before.name ?? '方块'}），先挖掉或换一格` }
    }

    const armed = (await request('place', { x, y, type, style, ms: 300 }, { signal })).data ?? {}
    const slot = armed.slot
    const stackBefore = armed.stackBefore

    const waited = await pollUntil(async () => {
      const cell = await readTile(x, y, signal)
      return cell.type === type ? cell : null
    }, { deadlineMs, signal })

    const stackAfter = await stackAt(slot, signal)
    const consumed = typeof stackBefore === 'number' && typeof stackAfter === 'number' ? stackBefore - stackAfter : null
    const evidence = { x, y, type, name: armed.name ?? null, slot, stackBefore, stackAfter, consumed, item: armed.item ?? null }

    if (!waited.hit) {
      const cell = await readTile(x, y, signal)
      return {
        ok: false, placed: false, ...evidence, after: cell.type ?? -1,
        reason: `放了 ${deadlineMs}ms 这块位置还是空的：多半是背包里没有这种方块，或者这一格缺支撑`,
      }
    }
    // A tile appeared but the stack did not move: the game placed it from
    // somewhere else, and the caller needs to know the item flow is off.
    if (consumed === 0) {
      return { ok: false, placed: true, ...evidence, reason: '方块出现了但背包没扣：物品流对不上，别把它当成自己的放置' }
    }
    return { ok: true, placed: true, ...evidence }
  }

  /**
   * Place a multi-cell furniture at its **root cell** (top-left of the footprint).
   *
   * The cursor cell is root + origin: for a 3x2 furnace with origin (1,1) the
   * player aims one cell right and one cell down from the top-left tile, which
   * is why aiming at the root itself used to fail and why aiming was described
   * as "guessing" before TileObjectData was available.
   */
  async function placeObject({ x, y, type, style = 0, deadlineMs = 4000, signal } = {}) {
    const info = await objectInfo(type, { style, signal })
    if (info.width === 1 && info.height === 1) return { ...(await placeTile({ x, y, type, style, deadlineMs, signal })), footprint: info }

    const aimX = x + info.originX
    const aimY = y + info.originY
    const attempt = await placeTile({ x: aimX, y: aimY, type, style, deadlineMs, signal })

    const footprint = []
    for (let dx = 0; dx < info.width; dx += 1) {
      for (let dy = 0; dy < info.height; dy += 1) {
        const cell = await readTile(x + dx, y + dy, signal)
        footprint.push({ x: x + dx, y: y + dy, type: cell.type ?? -1 })
      }
    }
    const filled = footprint.filter(c => c.type === type).length
    const complete = filled === footprint.length
    if (!attempt.ok) return { ...attempt, footprint: info, cells: footprint, filled, complete }
    if (!complete) {
      // Spread first, then override: `{ ok:false, ...attempt }` would let a
      // successful single-tile placement mask an incomplete footprint.
      return {
        ...attempt, ok: false, placed: true, footprint: info, cells: footprint, filled, complete,
        reason: `${info.width}x${info.height} 的家具只出现了 ${filled}/${footprint.length} 格：锚点或支撑不对`,
      }
    }
    return { ...attempt, footprint: info, cells: footprint, filled, complete }
  }

  /** Stack count of one inventory slot (null when the slot is out of range). */
  async function stackAt(slot, signal) {
    if (typeof slot !== 'number' || slot < 0) return null
    const inv = (await request('inventory', {}, { signal })).data ?? {}
    const entry = (inv.items ?? []).find(it => it.slot === slot)
    return entry?.stack ?? 0
  }

  /* ── crafting ───────────────────────────────────────────────────────── */

  /** Total number of one item type across the bag (armour and ammo slots excluded). */
  async function countItem(type, signal) {
    const inv = (await request('inventory', {}, { signal })).data ?? {}
    return (inv.items ?? []).filter(it => it.type === type).reduce((sum, it) => sum + (it.stack ?? 0), 0)
  }

  /**
   * Craft by output item id and wait for the products to arrive.
   *
   * `crafted: 0` with `dropped > 0` is a full bag, not bad materials - the two
   * used to look identical, which sent a whole session chasing the wrong cause.
   */
  async function craftItem({ item, times = 1, deadlineMs = 5000, signal } = {}) {
    const before = await countItem(item, signal)
    // 背包满时原版会先扣材料、再把产物丢掉（实测 15 铅矿 → 0 铅锭，地上也没有）。
    // 所以开工前先看空位：一个都没有就直接拒绝，别白烧材料。
    const bag = (await request('inventory', {}, { signal })).data ?? {}
    if (bag.free === 0) {
      return {
        ok: false, item, times, before, after: before, gained: 0, expected: times, crafted: 0, dropped: 0,
        reason: '背包没有空位（free:0）：先用 trash/drop 清格再合成，否则材料会被消耗而产物消失',
      }
    }
    const result = (await request('craft', { item, times }, { signal })).data ?? {}
    const perCraft = result.perCraft ?? result.crafted ?? 1
    const expected = perCraft * times

    const waited = await pollUntil(async () => {
      const count = await countItem(item, signal)
      return count >= before + expected ? count : null
    }, { deadlineMs, signal })

    const after = await countItem(item, signal)
    const evidence = { item, name: result.name ?? null, times, before, after, gained: after - before, expected, crafted: result.crafted, dropped: result.dropped }

    if (waitForDropped(result)) {
      return { ok: true, ...evidence, warning: `背包放不下：有 ${result.dropped} 个掉在地上，先清背包再捡` }
    }
    if (!waited.hit) {
      return {
        ok: false, ...evidence,
        reason: result.crafted === 0 && result.dropped === 0
          ? '一个也没做出来：材料不够，或者不在所需的工作台旁边'
          : `只拿到 ${after - before} 个（期望 ${expected}）`,
      }
    }
    return { ok: true, ...evidence }
  }

  const waitForDropped = result => typeof result.dropped === 'number' && result.dropped > 0

  /* ── equipment ──────────────────────────────────────────────────────── */

  /**
   * Armour / accessory state plus the defence the game derives from it.
   * `equipment` is an array of { slot, kind, type, name, defense } (slots 0-2 are
   * armour, 3+ are accessories); it is empty when nothing is equipped.
   */
  async function equipment(signal) {
    const inv = (await request('inventory', {}, { signal })).data ?? {}
    return { equipment: inv.equipment ?? [], defense: inv.defense ?? null }
  }

  /**
   * Equip the item in one bag slot and wait until the armour slot really changed.
   */
  async function equipFromSlot({ slot, deadlineMs = 3000, signal } = {}) {
    const before = await equipment(signal)
    const bag = (await request('inventory', {}, { signal })).data ?? {}
    const item = (bag.items ?? []).find(it => it.slot === slot)
    if (!item) return { ok: false, reason: `背包第 ${slot} 格是空的` }

    await request('equip', { slot }, { signal })
    const waited = await pollUntil(async () => {
      const nowState = await equipment(signal)
      return nowState.equipment.some(entry => entry && entry.type === item.type) ? nowState : null
    }, { deadlineMs, signal })

    if (!waited.hit) {
      return { ok: false, slot, item: item.name, reason: `${item.name} 没有出现在装备栏里` }
    }
    return {
      ok: true, slot, item: item.name, type: item.type,
      defenseBefore: before.defense, defenseAfter: waited.value.defense,
      equipment: waited.value.equipment,
    }
  }

  /* ── chests ─────────────────────────────────────────────────────────── */

  /** What is in the chest the player is standing next to. */
  async function chestList({ signal } = {}) {
    const data = (await request('chest', { action: 'list' }, { signal })).data ?? {}
    return { ok: true, chest: data.chest, items: data.items ?? [] }
  }

  /** Move one bag slot into a nearby chest and verify both sides moved. */
  async function chestPut({ invSlot, deadlineMs = 3000, signal } = {}) {
    const bagBefore = (await request('inventory', {}, { signal })).data ?? {}
    const item = (bagBefore.items ?? []).find(it => it.slot === invSlot)
    if (!item) return { ok: false, reason: `背包第 ${invSlot} 格是空的` }
    const chestBefore = await chestList({ signal })

    const result = (await request('chest', { action: 'put', invSlot }, { signal })).data ?? {}
    const waited = await pollUntil(async () => {
      const chest = await chestList({ signal })
      return chest.items.some(it => it.slot === result.chestSlot && it.type === item.type) ? chest : null
    }, { deadlineMs, signal })

    if (!waited.hit) return { ok: false, invSlot, item: item.name, reason: '箱子里没有出现这件东西' }
    return {
      ok: true, invSlot, item: item.name, type: item.type, chestSlot: result.chestSlot,
      chestItemsBefore: chestBefore.items.length, chestItemsAfter: waited.value.items.length,
    }
  }

  /** Take a chest slot into the bag and verify the bag gained it. */
  async function chestTake({ slot, count, deadlineMs = 3000, signal } = {}) {
    const chestBefore = await chestList({ signal })
    const item = chestBefore.items.find(it => it.slot === slot)
    if (!item) return { ok: false, reason: `箱子第 ${slot} 格是空的` }
    const bagBefore = await countItem(item.type, signal)

    const result = (await request('chest', { action: 'take', slot, ...(count === undefined ? {} : { count }) }, { signal })).data ?? {}
    const wanted = result.count ?? item.stack
    const waited = await pollUntil(async () => {
      const bag = await countItem(item.type, signal)
      return bag >= bagBefore + wanted ? bag : null
    }, { deadlineMs, signal })

    if (!waited.hit) return { ok: false, slot, item: item.name, reason: '背包里没有多出这件东西（可能背包满了）' }
    return { ok: true, slot, item: item.name, type: item.type, count: wanted, bagBefore, bagAfter: waited.value }
  }


  /**
   * 逐格铺一片区域（每个格子都真实使用物品并回读确认）。
   *
   * 为什么不用桥上的 `fill`：fill 只负责把 "使用物品" 排队（armed），并不等结果，
   * 一次调用把几十格全排进去时，只有最后几次真的落下，其余静默丢掉。
   * 实测：fill 60 格 → armed 13 / outOfRange 38 / 回读 0 格。
   *
   * 顺序：自下而上、每行从左到右（先有支撑再往上堆），够不着就走过去。
   */
  async function fillVerified({ x1, y1, x2, y2, type, style = 0, ms, reach = 6, signal } = {}) {
    const loX = Math.min(x1, x2), hiX = Math.max(x1, x2)
    const loY = Math.min(y1, y2), hiY = Math.max(y1, y2)
    const placed = [], failed = []
    let skipped = 0
    for (let y = hiY; y >= loY; y--) {
      for (let x = loX; x <= hiX; x++) {
        const cur = await readTile(x, y, signal)
        if (cur && cur.hasTile) { skipped++; continue }
        // 够不着就先走过去（水平方向；垂直靠挖/搭台阶，不在本函数职责内）
        for (let k = 0; k < 4; k++) {
          const st = (await request('state', {}, { signal })).data?.player
          if (!st || !st.pos) break
          const dx = x - st.pos[0]
          if (Math.abs(dx) <= reach && Math.abs(y - st.pos[1]) <= reach) break
          if (Math.abs(dx) > reach) await request('move', { dir: dx > 0 ? 'right' : 'left', ms: 200 })
          else break
        }
        const r = await placeTile({ x, y, type, style, ms, signal })
        if (r && r.ok) placed.push([x, y]); else failed.push({ x, y, reason: (r && r.reason) || (r && r.error) || 'not placed' })
      }
    }
    // skipped 必须单独报：区域里本来就是实心的格既不算成功也不算失败，
    // 否则「placed 0 / failed 0」看着像函数没干活（实测踩过一次）。
    return { ok: failed.length === 0 && placed.length > 0, placed: placed.length, failed: failed.length, skipped, cells: placed, failures: failed.slice(0, 8) }
  }

  return {
    mineTile,
    fillVerified,
    placeTile,
    placeObject,
    objectInfo,
    craftItem,
    walkClear,
    equipFromSlot,
    equipment,
    chestList,
    chestPut,
    chestTake,
    countItem,
    pollUntil,
  }
}