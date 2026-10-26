// 原子动作模型：示范只能教「单个动作怎么按」，不能教「整套任务怎么做」。
//
// 六条全局规则在这里落地，全部是纯函数、无 I/O：
//  1. 原子动作（单次短跳/长按跳/单次挥镐/单次普攻/单次转向）可入库，存 tick 时序与按键节奏；
//     执行序列（砍树、打怪、清方块、持续挖掘等整套任务）一律不入库。
//  2. 循环类任务只取原子模板；重复次数、总帧数、总时长由上层按实时状态判断何时停。
//  3. 条件分支只记「每个分支里的原子动作怎么做」，不记这次走了哪条分支、走了多久。
//  4. 空间相关任务只记执行方式，不固化坐标、距离、方块数量。
//  5. 检索只产出「单个原子动作的节奏参考」，不得覆盖上层的状态判断/终止条件/分支选择/数值计算。
//  6. 采集时自动剥离总次数、总时长、固定坐标、固定分支走向，只留原子模板。
//
// 这个模块不判断"该不该这么做"（那是 src/rule-gate.js 的活），只保证「入库的东西是原子」。

/** 原子动作白名单。key 对应 TIMING_BOUNDS 里的时序参数名。 */
export const ATOMIC_KINDS = Object.freeze({
  'jump-short': { param: 'jumpHoldMs', label: '单次轻点跳' },
  'jump-long': { param: 'jumpHoldMs', label: '单次长按跳' },
  'mine-swing': { param: 'digHoldMs', label: '单次挥镐' },
  'attack-swing': { param: 'digHoldMs', label: '单次普攻' },
  'place-swing': { param: 'digHoldMs', label: '单次放置' },
  'move': { param: 'moveHoldMs', label: '单次移动' },
  'turn': { param: 'moveHoldMs', label: '单次转向' },
})

/** 短跳与长按跳的分界（毫秒）。低于它算轻点，高于它算按住。 */
export const JUMP_TAP_MS = 200

/** 这些字段一律不许进库：它们是"这一次"的偶然量，不是动作样式。 */
export const VOLATILE_FIELDS = Object.freeze([
  'x', 'y', 'tileX', 'tileY', 'px', 'py', 'dx', 'dy',
  'count', 'repeats', 'rounds', 'loops', 'totalMs', 'durationMs', 'frames', 'frameCount',
  'path', 'route', 'waypoints', 'target', 'targetX', 'targetY',
  'branches', 'branch', 'choice', 'branchPath', 'tookBranch',
])

/** 判定一个样本是不是「执行序列/循环/分支/空间任务」（true = 不是原子，不得入库）。 */
export function sequenceRejection(sample = {}) {
  const s = sample || {}
  const h = s.human || {}
  const has = (obj, keys) => keys.some(k => obj[k] !== undefined && obj[k] !== null)
  // 整套轨迹：帧序列、总时长、帧数
  if (has(s, ['frames', 'totalMs', 'durationMs', 'frameCount'])) return 'sequence'
  // 循环任务：显式声明了重复轮数/次数大于一
  if (Number(h.count) > 1 || Number(h.repeats) > 1 || Number(s.repeats) > 1) return 'loop'
  if (has(s, ['loops', 'rounds'])) return 'loop'
  // 条件分支：带分支走向/选择记录
  if (has(s, ['branches', 'branch', 'choice', 'branchPath', 'tookBranch'])) return 'branch'
  // 空间任务：固化了坐标/路径/目标点
  if (has(s, ['path', 'route', 'waypoints', 'targetX', 'targetY'])) return 'spatial'
  return null
}

/** 把一条人的动作剥成原子模板；剥不出时序就返回 null（保持"没测到"而不是"0ms"）。 */
export function atomFromAction(action = {}) {
  const kind = String(action.kind || '')
  const spec = ATOMIC_KINDS[kind]
  if (!spec) return null
  const out = { kind }
  const hold = Number(action[spec.param])
  if (Number.isFinite(hold) && hold > 0) out.holdMs = Math.round(hold)
  // grounded 是动作发生时的身体状态，属于"动作样式"的一部分（空中跳和地面跳节奏不同），
  // 但它不是坐标，不会把场景钉死在一个格子上。
  if (typeof action.grounded === 'boolean') out.grounded = action.grounded
  return Object.keys(out).length > 1 ? out : null
}

/** 列出一条样本里被剥掉的易变量，供日志说明「为什么这条只剩这么点」。 */
export function strippedFields(sample = {}) {
  const s = sample || {}
  const out = []
  for (const f of VOLATILE_FIELDS) {
    if (s[f] !== undefined && s[f] !== null) out.push(`sample.${f}`)
    if (s.human && s.human[f] !== undefined && s.human[f] !== null) out.push(`human.${f}`)
  }
  return out
}

const JUMP_KEYS = new Set(['Space', 'space', 'jump'])

/**
 * 从一段原始帧里抽原子动作模板（规则 2/6 的采集端）。
 *
 * frames 每帧至少要有 { t, keys?, mouse?{left,right} }，t 单位毫秒。
 * 返回 { atoms, dropped }：
 *  - atoms 每个只是 { kind, holdMs }，**不含坐标、不含次数、不含总时长**。
 *  - 同类动作连续出现多次时只保留第一个（那是循环，重复次数归上层）。
 */
export function atomize(frames = [], opts = {}) {
  const jumpTapMs = Number(opts.jumpTapMs) || JUMP_TAP_MS
  const list = Array.isArray(frames) ? frames.filter(f => f && Number.isFinite(f.t)) : []
  const atoms = []
  const dropped = { loop: 0, sequence: 0, branch: 0, spatial: 0 }

  const runs = []
  let cur = null
  for (const f of list) {
    const keys = f.keys || []
    const jump = keys.some(k => JUMP_KEYS.has(k))
    const mine = !!f.mouse?.left
    const place = !!f.mouse?.right
    const kind = jump ? 'jump' : mine ? 'mine' : place ? 'place' : null
    if (!kind) { cur = null; continue }
    if (cur && cur.kind === kind) { cur.end = f.t; continue }
    cur = { kind, start: f.t, end: f.t }
    runs.push(cur)
  }

  for (const r of runs) {
    const holdMs = Math.round(r.end - r.start)
    let kind = r.kind
    if (kind === 'jump') kind = holdMs < jumpTapMs ? 'jump-short' : 'jump-long'
    else if (kind === 'mine') kind = 'mine-swing'
    else kind = 'place-swing'
    if (!ATOMIC_KINDS[kind]) { dropped.sequence += 1; continue }
    // 同类连续只留一条：剩下的都是同一个模板的重复，次数归上层。
    if (atoms.length && atoms[atoms.length - 1].kind === kind) { dropped.loop += 1; continue }
    atoms.push({ kind, holdMs })
  }
  return { atoms, dropped }
}

/** 人读的一句话摘要。 */
export function formatAtoms(atoms = []) {
  if (!atoms.length) return '（没有可入库的原子动作）'
  return atoms.map(a => `${ATOMIC_KINDS[a.kind]?.label || a.kind} ${a.holdMs}ms`).join('；')
}