/**
 * 持续性动作的统一执行框架（挖掘/砍伐、跳跃）。
 * ============================================================================
 * 规则（用户 m08124 定，全局通用，挖矿/砍树/跳跃都生效）：
 *
 *  1. 动作分两类：【瞬时点击】和【长按持续】。挖/砍 = 持续对目标格输出攻击，
 *     不是发一次指令；跳跃 = 需要高跳就长按、短距离才短按。
 *  2. 动作执行期间持续读游戏状态校验：方块每轮攻击后回读，还在就继续，
 *     没了立刻终止；长按跳一直生效，直到到达目标高度 / 落地 / 触发危险才松手。
 *  3. 完成标记只看世界状态，不看“指令发出去了”。
 *  4. 连续 20 次仍未破坏 → 判异常，停止并上报（不静默重试到死）。
 *
 * 这些函数刻意不自己发命令：`read`/`swing`/`press`/`release` 都由调用方注入，
 * 所以同样的循环既能跑真实桥，也能在测试里跑假世界。
 * ============================================================================
 */

export const ACTION_CLASS = {
  instant: '瞬时点击',
  sustained: '长按持续',
}

/** 连续多少次还挖不动就判异常（用户指定 20）。 */
export const MAX_SUSTAINED_ATTEMPTS = 20

/** 一次完整跳跃能上升多少格（原版常量：起跳约 2.3 格，跳上 3 格需缓冲） */
export const JUMP_REACH_TILES = 2.3

const defaultSleep = ms => new Promise(resolve => setTimeout(resolve, ms))

/**
 * 挖掘/砍伐循环：持续对一格输出攻击，直到世界回读显示它没了。
 * @param {object} args
 * @param {() => Promise<{type?:number, hasTile?:boolean}|null>} args.read  回读目标格
 * @param {() => Promise<void>} args.swing  输出一次攻击（等价于按住左键一段）
 * @param {number} [args.maxAttempts]  默认 20
 * @param {number} [args.settleMs]  每轮攻击后等多久再回读
 * @param {(info:object)=>void} [args.onAttempt]
 */
export async function runMineLoop({ read, swing, maxAttempts = MAX_SUSTAINED_ATTEMPTS, settleMs = 260, sleep = defaultSleep, onAttempt } = {}) {
  if (typeof read !== 'function' || typeof swing !== 'function') {
    throw new TypeError('runMineLoop needs read() and swing()')
  }
  const gone = tile => !tile || tile.hasTile === false || !(Number(tile.type) > 0)
  const startedAt = Date.now()
  let attempts = 0
  const initial = await read()
  if (gone(initial)) {
    return { ok: true, attempts: 0, already: true, elapsedMs: 0, message: '这格本来就是空的' }
  }
  while (attempts < maxAttempts) {
    attempts += 1
    await swing()
    await sleep(settleMs)
    const tile = await read()
    if (onAttempt) onAttempt({ attempt: attempts, tile })
    if (gone(tile)) {
      return { ok: true, attempts, elapsedMs: Date.now() - startedAt, message: `第 ${attempts} 次攻击后消失` }
    }
  }
  return {
    ok: false,
    attempts,
    elapsedMs: Date.now() - startedAt,
    reason: `连续 ${maxAttempts} 次攻击仍未破坏（最后回读 type=${(await read())?.type ?? '空'}）`,
    report: {
      kind: 'sustained_action_gave_up',
      action: 'mine',
      attempts,
      hint: '工具等级不够 / 目标不可挖 / 够不到（距离或视线）',
    },
  }
}

/**
 * 跳跃模式选择：由寻路给出高差，自动决定长按还是短按。
 * @param {{dy:number, gapTiles?:number}} args dy = 目标格高差（正数=要往上）
 */
export function planJump({ dy = 0, gapTiles = 0 } = {}) {
  const climb = Math.max(0, Number(dy) || 0)
  if (climb <= 0 && gapTiles <= 0) {
    return { mode: 'tap', ms: 90, hold: false, reason: '平地/下坡，短按即可' }
  }
  if (climb <= 1.5 && gapTiles <= 1) {
    return { mode: 'tap', ms: 120, hold: false, reason: `只差 ${climb} 格，小台阶用短按` }
  }
  // 高差越过一次普通跳的高度 → 必须长按，按住直到到高度/落地
  const ms = Math.min(900, Math.round(220 + climb * 130))
  return {
    mode: 'hold',
    ms,
    hold: true,
    reason: climb > JUMP_REACH_TILES
      ? `高差 ${climb} 格超过单跳 ${JUMP_REACH_TILES} 格，需要长按 + 搭脚点`
      : `高差 ${climb} 格，长按跳到顶再松手`,
  }
}

/**
 * 长按跳跃执行：按下后持续回读纵坐标，到达目标高度 / 落地 / 危险才松开。
 * @param {object} args
 * @param {() => Promise<{y:number, grounded?:boolean, hp?:number, danger?:boolean}>} args.read
 * @param {(ms:number)=>Promise<void>} args.press  开始按住（可分段续期）
 * @param {()=>Promise<void>} args.release  松手
 * @param {number} args.targetY  目标高度（世界坐标，越小越高）
 */
export async function runJumpHold({ read, press, release, targetY, maxMs = 1200, stepMs = 120, sleep = defaultSleep } = {}) {
  if (typeof read !== 'function' || typeof press !== 'function' || typeof release !== 'function') {
    throw new TypeError('runJumpHold needs read(), press() and release()')
  }
  const start = await read()
  const startY = start.y
  let elapsed = 0
  let released = false
  const finish = async (outcome) => {
    await release()
    released = true
    return { ok: outcome !== 'timeout', released, outcome, startY, endY: last?.y ?? startY, elapsedMs: elapsed, climbed: startY - (last?.y ?? startY) }
  }
  let last = start
  while (elapsed < maxMs) {
    await press(stepMs)
    elapsed += stepMs
    last = await read()
    if (last.danger || (last.hp != null && last.hp <= 0)) return finish('danger')
    if (last.grounded && elapsed > stepMs) return finish('landed')
    if (last.y <= targetY) return finish('reached')
  }
  return finish('timeout')
}

/** 某类动作是不是“持续型”（决定要不要进入循环/长按路径）。 */
export function isSustained(kind) {
  return ['mine', 'chop', 'jump_hold', 'climb', 'travel'].includes(String(kind))
}
