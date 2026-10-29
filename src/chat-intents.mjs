// chat-intents —— 常驻层能自己办的那几条固定指令。
//
// 为什么要有这个文件：chatd 只搬消息时，玩家在游戏里说「飞一下」，只有等某个
// 对话里的 agent 醒着才会被执行；一旦没人看着，指令就只是躺在 inbox 里。
// 这里把「不需要判断、照着做就行」的那几条固定动作放在常驻进程里，
// 让它们在没有 agent 的时候也能立刻完成并在游戏框回报。
//
// 边界（重要）：
//   - 只放**确定性**的指令。需要看情况判断的（往哪走能出去、这东西能不能合成）
//     一律不在这里，留给上层 agent。
//   - 每条执行完必须回读证据再回报（AGENTS.md §2：不许把"命令生效"当成"操作发生"）。
//   - 这里不碰世界：移动、飞行、看状态都是角色自己的动作。

const NUM = '[0-9]+(?:\\.[0-9]+)?'

/** 走格用的刹车距离：松手后还会滑一段，越快要越早松。 */
export function brakeDistance(speed) {
  return Math.abs(speed) * 0.62 + 0.1
}

/**
 * 把玩家说的一句话解析成结构化意图；认不出就返回 null（上层 agent 再处理）。
 * 纯函数，不做任何 I/O。
 */
export function parseIntent(raw) {
  const text = String(raw ?? '').trim()
  if (!text) return null
  // 「再」「再走」只是语气，去掉不影响判定；但「再」+没方向时方向沿用上次，见 walk 分支
  const raw0 = String(raw ?? '').trim()
  const s = raw0.replace(/[，。！？、；：\s]/g, '').replace(/^再/, '')

  // 飞行 / 坐骑
  if (/^(飞一下|起飞|飞|飞起来|飞个|往上飞)$/.test(s)) return { kind: 'fly' }
  if (/^(下骑|下来|落地|下落|下来吧)$/.test(s)) return { kind: 'dismount' }
  if (/^(骑上|骑|上坐骑|召唤坐骑|叫坐骑)$/.test(s)) return { kind: 'mount' }

  // 停
  if (/^(停|停手|停下|别动|站住|停一下)$/.test(s)) return { kind: 'stop' }

  // 走格：右走四格 / 往右 4 格 / 向左三格
  const walk = s.match(new RegExp(`^(?:往|向|朝)?(右|左)(?:边|手)?(?:走|走个|移动|挪)?([零一二三四五六七八九十百两\\d.]+)?格?$`))
  if (walk) {
    const dir = walk[1] === '右' ? 'right' : 'left'
    const n = cnToNumber(walk[2])
    if (n === null || n === undefined) return { kind: 'walk', dir, tiles: 1 }
    return { kind: 'walk', dir, tiles: n }
  }

  // 只说「走五格」不给方向：dir 用 null，由执行层沿用上一次的方向。
  const bare = s.match(new RegExp(`^(?:走|走个|移动|挪|前进)([零一二三四五六七八九十百两\\d.]+)格?$`))
  if (bare) {
    const n = cnToNumber(bare[1])
    if (n !== null && n > 0) return { kind: 'walk', dir: null, tiles: n }
  }

  // 读状态
  if (/^(坐标|在哪|位置|报坐标|报位置|你在哪)$/.test(s)) return { kind: 'where' }
  if (/^(血量|血|蓝量|蓝|状态|报状态|看看状态|身体状态)$/.test(s)) return { kind: 'status' }
  if (/^(背包|物品|有什么东西)$/.test(s)) return { kind: 'inventory' }

  return null
}

/** 中文数字 → 阿拉伯数字；认不出返回 null。 */
export function cnToNumber(token) {
  if (token === undefined || token === null || token === '') return null
  if (/^\d+(\.\d+)?$/.test(token)) return Number(token)
  const digits = { 零: 0, 一: 1, 二: 2, 两: 2, 三: 3, 四: 4, 五: 5, 六: 6, 七: 7, 八: 8, 九: 9 }
  if (token === '十') return 10
  if (token.length === 1 && token in digits) return digits[token]
  const m = token.match(/^([一二两三四五六七八九])?十([一二三四五六七八九])?$/)
  if (m) {
    const tens = m[1] ? digits[m[1]] : 1
    const ones = m[2] ? digits[m[2]] : 0
    return tens * 10 + ones
  }
  return null
}

/**
 * 走格的动作序列：先按住方向，轮询位置，剩余距离进刹车距离就松手。
 * bridge 由调用方注入（(cmd, params) => data），这样这个函数可以在测试里跑假桥。
 */
export async function walkTiles(bridge, sleep, dir, tiles) {
  // bridge 必须是 async 的（进程内直连）。起子进程那一版单次调用上百毫秒，
  // 轮询周期被拉长，角色在两次读数之间多跑好几格，刹车距离白算。
  const before = await bridge('state', {})
  const x0 = before.player.pos[0]
  const target = x0 + (dir === 'right' ? tiles : -tiles)
  const deadline = Date.now() + 15000
  let released = null
  let releaseSpeed = null
  let stillCount = 0

  await bridge('move', { dir, ms: Math.max(400, Math.abs(tiles) * 400) })
  while (Date.now() < deadline) {
    await sleep(50)
    const cur = await bridge('state', {})
    const v = Math.abs(cur.player.velocity[0])
    const left = Math.abs(target - cur.player.pos[0])
    if (released === null && left <= brakeDistance(v)) {
      await bridge('stop', {})
      released = cur.player.pos[0]
      releaseSpeed = v
    }
    if (released !== null && v === 0) break
    // 被挡住：连续若干次位置与速度都不动就承认走不动，不要假装走完
    if (released === null && v < 0.05) { stillCount++ } else { stillCount = 0 }
    if (stillCount >= 6) {
      await bridge('stop', {})
      return { ok: false, reason: 'blocked', from: x0, to: cur.player.pos[0] }
    }
  }
  await sleep(250)
  const after = await bridge('state', {})
  const x1 = after.player.pos[0]
  return {
    ok: true,
    from: x0,
    to: x1,
    moved: x1 - x0,
    target,
    error: x1 - target,
    releaseSpeed,
  }
}

/** 读一处状态，拼成能给玩家看的一行。 */
export function describeStatus(data) {
  const p = data.player || {}
  const pos = p.pos || []
  const life = `${p.life ?? '?'}/${p.lifeMax ?? '?'}`
  const mana = `${p.mana ?? '?'}/${p.manaMax ?? '?'}`
  const where = `${Number(pos[0] ?? 0).toFixed(1)}, ${Number(pos[1] ?? 0).toFixed(1)}`
  return `位置 ${where}｜血 ${life}｜蓝 ${mana}｜${p.grounded ? '站地上' : '空中'}`
}

/** 从 inventory 回包里挑出「手上有货」的那几格，避免把 50 格空槽也念一遍。 */
export function describeInventory(data, limit = 6) {
  const items = (data.items || []).filter(it => it && it.name && (it.stack ?? 0) > 0 && !it.empty)
  const shown = items.slice(0, limit).map(it => `${it.name}×${it.stack}`)
  const more = items.length > limit ? ` …共 ${items.length} 种` : ''
  return shown.length ? shown.join('、') + more : '背包是空的'
}
