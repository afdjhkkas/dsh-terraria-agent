// 观摩模式：只看、不碰。
//
// 这里最要紧的一条不是"怎么记"，而是"为什么能保证不碰"：
// 观摩期所有对游戏的写操作都要经过 guard——它在发命令之前就抛错，
// 而不是靠"我这次记得别动手"。模式切换的唯一真相是 mode store（落盘），
// 进程里没有任何代持状态，重启后仍然是同一个模式。
//
// 三流对齐：
//   A 流 = 游戏自己报的世界/玩家状态（bridge state + humaninput）
//   人 流 = 真人这一段的按键/瞄准/手持（同一批帧里的 control_* 与 mouse）
//   脚本流 = 用 planner 对同一帧状态做纯函数推演（不发送任何命令）
// 逐帧对齐后只做一件事：把「人和脚本不一样」的段落写成示范样本。

import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { actionDelta, sceneLabel, sceneOf, tuneTiming } from './demo-library.js'
import { describeViolations, gatePlan, loadRuleSet } from './rule-gate.js'

const HERE = dirname(fileURLToPath(import.meta.url))
export const DEFAULT_MODE_PATH = join(HERE, '..', 'data', 'mode.json')

export const MODES = Object.freeze({ observe: 'observe', auto: 'auto' })

/** 观摩模式下任何写操作都会撞这个错——它是护栏，不是提示。 */
export class ModeLockedError extends Error {
  constructor(action = 'dispatch') {
    super(`观摩模式禁止操作（${action}）：切回 auto 才能动手`)
    this.name = 'ModeLockedError'
    this.code = 'MODE_OBSERVE_LOCKED'
    this.action = action
  }
}

export function isWriteCommand(request = {}) {
  const readOnly = new Set(['ping', 'state', 'humaninput', 'scan', 'tile', 'items', 'inventory', 'npc', 'travelstatus', 'housediag', 'recipes', 'api', 'shops', 'shop', 'research', 'find', 'needs'])
  return !readOnly.has(String(request.cmd || ''))
}

/**
 * 写操作闸门。包一层 dispatch：观摩期直接抛错，模型没有"这次破例一下"的空间。
 */
export function createGuard(dispatch, { getMode } = {}) {
  return async function guarded(request, ...rest) {
    const mode = getMode ? getMode() : MODES.auto
    if (mode === MODES.observe && isWriteCommand(request)) throw new ModeLockedError(request?.cmd)
    return dispatch(request, ...rest)
  }
}

export function createModeStore({ file = DEFAULT_MODE_PATH, mode = MODES.auto } = {}) {
  let current = mode
  if (existsSync(file)) {
    try {
      const raw = JSON.parse(readFileSync(file, 'utf8'))
      if (raw?.mode === MODES.observe || raw?.mode === MODES.auto) current = raw.mode
    } catch { /* 坏文件按默认值走，别让模式文件把进程卡死 */ }
  }
  return {
    get: () => current,
    set(next) {
      if (next !== MODES.observe && next !== MODES.auto) throw new Error(`未知模式 ${next}`)
      current = next
      try {
        mkdirSync(dirname(file), { recursive: true })
        writeFileSync(file, JSON.stringify({ mode: next, changedAt: new Date().toISOString() }, null, 2), 'utf8')
      } catch { /* 落盘失败也要让本次切换生效 */ }
      return current
    },
    file,
  }
}

const KIND_PRIORITY = ['use', 'jump', 'move']

/** 单帧归类：这一帧的人在干什么。优先级做到"手在按攻击键时不算在走"。 */
export function classifyFrame(input = {}) {
  const c = input.control || {}
  const m = input.mouse || {}
  if (c.useItem || m.left) return m.left ? 'use' : 'use'
  if (c.jump) return 'jump'
  if (c.left || c.right) return 'move'
  return 'idle'
}

/**
 * 一段连续帧 → 一个「人力动作」。时长 = 该类帧数 × 采样间隔，
 * 这是我们从游戏自己维护的输入状态里能拿到的、最接近"人手按了多久"的量。
 */
export function framesToAction(frames = [], everyMs = 50) {
  if (!frames.length) return null
  const tally = { use: 0, jump: 0, move: 0, idle: 0, left: 0, right: 0, mouseLeft: 0 }
  let last = null
  for (const f of frames) {
    const input = f.input || {}
    const kind = classifyFrame(input)
    tally[kind] += 1
    if (input.control?.left) tally.left += 1
    if (input.control?.right) tally.right += 1
    if (input.mouse?.left) tally.mouseLeft += 1
    last = f
  }
  const input = last?.input || {}
  // 取这段里占帧数最多的那一类；帧数相同才按优先级裁决。
  // （一边走一边跳时，主导动作仍是"走"，跳只是它的附带动作。）
  let kind = 'idle'
  let best = 0
  for (const k of KIND_PRIORITY) {
    if (tally[k] > best) { best = tally[k]; kind = k }
  }
  if (best === 0) kind = 'idle'
  const action = {
    kind,
    frames: frames.length,
    jumpHoldMs: tally.jump * everyMs,
    digHoldMs: tally.use * everyMs,
    moveHoldMs: (tally.left + tally.right) * everyMs,
    settleMs: tally.idle * everyMs,
    dir: tally.right > tally.left ? 'right' : tally.left > tally.right ? 'left' : null,
    aim: input.mouse ? { tileX: input.mouse.tileX, tileY: input.mouse.tileY } : null,
    selected: input.player?.selected ?? null,
    heldType: input.player?.heldType ?? null,
  }
  if (kind === 'idle') return action
  return action
}

/**
 * 基础时序：脚本自己的"手感"。示范只许在这套数上做微调（且被 TIMING_BOUNDS 夹住），
 * 不许另起一套——否则示范就不再是"修正"，而是替换掉了知识库定下的节奏。
 */
export const BASE_TIMING = Object.freeze({
  jumpHoldMs: 150,
  digHoldMs: 90,
  moveHoldMs: 600,
  settleMs: 260,
  aimLeadMs: 120,
})

/** 脚本对这一帧的纯函数推演——不发命令，所以"预判"不会改变世界。 */
export function defaultPlanner(snap = {}) {
  const threat = Number(snap.hostileDist ?? snap.threat ?? 99)
  const dy = Number(snap.dy ?? 0)
  const base = { ...BASE_TIMING }
  if (threat <= 8) return { kind: 'attack', ...base, target: 'hostile' }
  if (dy < -1) return { kind: 'jump', ...base, jumpHoldMs: 220 }
  if (snap.stuck) return { kind: 'mine', ...base }
  return { kind: 'move', dir: 'right', ...base }
}

/**
 * 按当前局面决定"要不要参考人类时序"，以及参考到什么程度。
 *
 * 分档的理由很实际：`tuneTiming` 默认要三条同类样本才改参数（防手抖当规律）。
 * 但用户要的场景恰恰是"这个具体局面下人就该这么按"——那种局面往往只出现过一次。
 * 折中办法是按相似度分档：
 *   tier=exact  同场景标签且相似度 ≥ 0.9 → 承认这是同一局面，一条样本就生效
 *   tier=loose  只是相似（≥ minSim）           → 仍要三条，够稳了才改
 *   tier=none   没有够像的示范                 → 完全用基础时序
 * 分档只影响"要用几条样本"，不影响边界：TIMING_BOUNDS 与知识库闸门照旧管着。
 */
export function resolveTiming({ snap = {}, library = null, base = BASE_TIMING, opts = {} } = {}) {
  const minSim = Number(opts.minSim ?? 0.72)
  const exactSim = Number(opts.exactSim ?? 0.9)
  const out = {
    timing: { ...base },
    applied: [],
    clamped: [],
    matches: [],
    tier: 'none',
    label: sceneLabel(snap),
    source: 'base',
  }
  if (!library || typeof library.recall !== 'function') return out
  const found = library.recall(snap, { minSim, limit: Number(opts.limit ?? 8) }) || []
  // `recall` 回的是 `{rec, sim}`：相似度在外面，样本本体在 rec 里。
  // 这里必须先摊平，否则下面拿 `m.label`/`m.human` 全是 undefined——
  // 表现就是"明明有满相似度的示范，tier 却永远停在 loose"，一条也不生效。
  const picks = found.map((m) => (m.rec ? { ...m.rec, sim: m.sim } : m))
  out.matches = picks.map((m) => ({ sim: m.sim, label: m.label ?? sceneLabel(m.scene || {}), kind: m.human?.kind ?? null }))
  if (!picks.length) return out
  const exact = picks.filter((m) => Number(m.sim) >= exactSim && (m.label ?? null) === out.label)
  const tier = exact.length ? 'exact' : 'loose'
  const tuned = tuneTiming(base, picks, {
    minSim,
    minSamples: tier === 'exact' ? 1 : Number(opts.minSamples ?? 3),
  })
  out.tier = tier
  out.timing = tuned.timing
  out.applied = tuned.applied
  out.clamped = tuned.clamped
  out.source = tuned.applied.length ? 'demo' : 'base'
  return out
}

/**
 * 自主模式的规划器：推演 → 按场景调时序 → **过知识库闸门**。
 *
 * 顺序不能反。先调时序再过闸门，意味着"示范改出来的方案"同样要受规则检验；
 * 反过来（先过闸门再调时序）就会漏掉时序调完之后才越界的那些情况。
 *
 * 闸门不过时**不出动作**：返回 `{kind:'hold', blocked:true}`，把违规条目原样带出去。
 * 这里没有 force/override 参数，也没有"示范要求例外"的分支——写出来就等于承认规则可以被绕。
 */
export function createAutoPlanner({
  planner = defaultPlanner,
  library = null,
  rules = null,
  tags = [],
  base = BASE_TIMING,
  opts = {},
} = {}) {
  const ruleSet = rules ? (Array.isArray(rules) ? { rules, sources: [] } : rules) : loadRuleSet({ tags })
  return function plan(snap = {}, ctx = {}) {
    const draft = planner(snap) || {}
    const tuned = resolveTiming({ snap, library, base, opts })
    const candidate = { ...draft }
    // 只把示范学到的"时长"叠上去，动作类型/方向这些语义字段一律以推演为准。
    for (const key of Object.keys(base)) {
      if (Number.isFinite(Number(tuned.timing[key]))) candidate[key] = Number(tuned.timing[key])
    }
    const verdict = gatePlan(candidate, { snap, opts: ctx.gateOpts ?? {}, rules: ruleSet.rules })
    const meta = {
      tier: tuned.tier,
      source: tuned.source,
      applied: tuned.applied,
      clamped: tuned.clamped,
      matches: tuned.matches,
      label: tuned.label,
      rules: ruleSet.rules.length,
    }
    if (!verdict.ok) {
      return {
        kind: 'hold',
        blocked: true,
        violations: verdict.violations,
        why: verdict.why,
        draft: candidate,
        timing: meta,
      }
    }
    return { ...candidate, blocked: false, timing: meta }
  }
}

/** 一次规划用了什么时序、依据几条示范、有没有被规则拦下——运行时日志就靠它。 */
export function formatTimingReport(plan = {}) {
  const t = plan.timing || {}
  const bit = [
    `[timing] tier=${t.tier ?? 'none'} source=${t.source ?? 'base'} sim=${(t.matches?.[0]?.sim ?? 0).toFixed?.(2) ?? '0.00'} rules=${t.rules ?? 0}`,
  ]
  if (t.applied?.length) {
    bit.push(t.applied.map((a) => `${a.param} ${a.from}→${a.to}ms`).join(', '))
  }
  if (t.clamped?.length) {
    bit.push(`clamped: ${t.clamped.map((c) => `${c.param} 要求${c.wanted} 停在${c.allowed}`).join(', ')}`)
  }
  if (plan.blocked) bit.push(`blocked: ${plan.why || describeViolations(plan.violations || [])}`)
  return bit.join(' | ')
}

function pushLimited(list, item, limit) {
  list.push(item)
  if (list.length > limit) list.splice(0, list.length - limit)
  return list
}

/**
 * 观测器：喂帧、切段、比对、入库。全部 I/O 通过 read/now 注入，测试里不需要真游戏。
 */
export function createObserver({
  read,
  library,
  planner = defaultPlanner,
  everyMs = 50,
  windowMs = 1000,
  now = () => Date.now(),
  onSample = null,
} = {}) {
  if (typeof read !== 'function') throw new Error('createObserver 需要 read()')
  const buffer = []
  const corrections = []
  const stats = { frames: 0, samples: 0, recorded: 0, discarded: 0, discardReasons: {}, windows: 0 }
  let segStart = null
  let lastKind = null

  function flushSegment(reason = 'switch') {
    if (!buffer.length) return null
    const frames = buffer.slice()
    buffer.length = 0
    const last = frames[frames.length - 1]
    const snap = last.snap || {}
    const human = framesToAction(frames, everyMs)
    // 人整段没动手：这是"没示范"，不是"人做错了"。不进库，也不许编造修正。
    if (human.kind === 'idle') {
      stats.discarded += 1
      stats.discardReasons['human-idle'] = (stats.discardReasons['human-idle'] || 0) + 1
      return null
    }
    const agent = planner(snap)
    const delta = actionDelta(agent, human)
    const sample = { at: new Date(now()).toISOString(), snap, human, agent, delta, reason }
    stats.samples += 1
    const res = library.record(sample)
    if (res.ok) {
      stats.recorded += 1
      const entry = {
        scene: sceneLabel(snap),
        human,
        agent,
        delta,
        sim: res.case ? 1 : null,
        at: sample.at,
      }
      pushLimited(corrections, entry, 50)
      if (onSample) { try { onSample(entry) } catch { /* 回调炸了不影响观摩 */ } }
    } else {
      stats.discarded += 1
      stats.discardReasons[res.reason] = (stats.discardReasons[res.reason] || 0) + 1
    }
    return { sample, recorded: res.ok, reason: res.reason, entry: corrections[corrections.length - 1] }
  }

  /** 采一帧。输入应是 {snap, input}，snap 是 state 的玩家/世界快照，input 是 humaninput 的读数。 */
  function tick(frame = {}) {
    const snap = frame.snap || frame
    const input = frame.input || frame.snap?.input || null
    if (!input) return { flushed: null }
    if (snap?.inWorld === false) return { flushed: null }
    const kind = classifyFrame(input)
    stats.frames += 1
    if (segStart === null) segStart = now()
    buffer.push({ snap, input, t: now() })
    let flushed = null
    if (lastKind !== null && kind !== lastKind && lastKind !== 'idle') {
      flushed = flushSegment('switch')
      segStart = now()
    }
    lastKind = kind
    const held = now() - (segStart ?? now())
    if (held >= windowMs) {
      const res = flushSegment('window')
      stats.windows += 1
      segStart = now()
      lastKind = null
      flushed = res || flushed
    }
    return { flushed }
  }

  /** 跑一段时间的观摩。ms<=0 时只跑 read 一次（测试用）。 */
  async function run({ ms = 0, sleep = (d) => new Promise(r => setTimeout(r, d)), maxFrames = Infinity } = {}) {
    const startedAt = now()
    let frames = 0
    do {
      const frame = await read()
      tick(frame)
      frames += 1
      if (frames >= maxFrames) break
      if (ms > 0) {
        await sleep(everyMs)
        if (now() - startedAt >= ms) break
      } else break
    } while (true)
    const tail = flushSegment('end')
    return { ...stats, corrections, tail }
  }

  return {
    tick,
    run,
    flush: flushSegment,
    stats: () => ({ ...stats }),
    corrections: () => corrections.slice(),
  }
}

/** 需求里那条"日志报本局观摩样本数、修正了哪些动作策略"。人读的一行行，别塞 JSON。 */
export function formatObserveReport(result = {}, { limit = 8 } = {}) {
  const lines = []
  const s = result.stats || result
  const reasons = Object.entries(s.discardReasons || {}).map(([k, v]) => `${k}=${v}`).join(',')
  lines.push(`[obs] frames=${s.frames} samples=${s.samples} recorded=${s.recorded} discarded=${s.discarded}${reasons ? ` (${reasons})` : ''}`)
  const fixes = (result.corrections || []).slice(-limit)
  if (!fixes.length) {
    lines.push('[obs] 本局没有可用的时序修正（人做的和脚本推演一致，或样本被丢弃）')
    return lines.join('\n')
  }
  for (const f of fixes) {
    const d = f.delta || {}
    if (d.reason === 'kind') lines.push(`[obs] ${f.scene} 动作不同：脚本 ${d.agent} → 人手 ${d.human}`)
    else if (d.reason === 'timing') {
      const parts = (d.diffs || []).map(x => `${x.key} ${x.agent}ms→${x.human}ms`).join('，')
      lines.push(`[obs] ${f.scene} 时序修正：${parts}`)
    }
  }
  return lines.join('\n')
}

/**
 * 案例库的静态报告（不采样，只读盘上的样本）。
 * 和 formatObserveReport 分开：那个报"这一局采到了什么"，这个报"库里现在有什么"。
 */
export function formatLibraryReport({ stats = {}, corrections = [] } = {}, { limit = 8 } = {}) {
  const lines = []
  lines.push(`[lib] 样本 ${stats.total ?? 0} 条（可用于调时序 ${stats.usable ?? 0} 条，观测期丢弃 ${stats.discarded ?? 0} 条）`)
  const byLabel = Object.entries(stats.byLabel || {}).sort((a, b) => b[1] - a[1])
  if (byLabel.length) {
    lines.push('[lib] 场景分布：')
    for (const [label, n] of byLabel.slice(0, limit)) lines.push(`  ${label} :: ${n}`)
  } else {
    lines.push('[lib] 库里还没有样本——用 observe 子命令采一局再回来看')
  }
  const list = corrections.slice(-limit)
  if (list.length) {
    lines.push('[lib] 最近学到的：')
    for (const f of list) {
      const d = f.delta || {}
      const scene = f.label || f.scene || 'unknown'
      if (d.reason === 'kind') lines.push(`  ${scene} :: 动作 ${d.agent} → ${d.human}`)
      else if (d.reason === 'timing') {
        const parts = (d.diffs || []).map(x => `${x.key} ${x.agent}→${x.human}`).join('，')
        lines.push(`  ${scene} :: 时序 ${parts}`)
      } else lines.push(`  ${scene} :: ${d.reason || 'unknown'}`)
    }
  }
  return lines.join('\n')
}