// 示范案例库（观摩期产物）。
//
// 两层的学习目标完全不同，别混：
//   知识库  docs/kb/*.md —— 静态规则，硬约束，优先级最高，任何人（含示范）都改不了。
//   本模块  demo cases   —— 动态样本，只记「同样的局面里，人手比脚本更会拿捏的节奏」。
// 所以这里的一切输出都只是**时序建议**（按多久、等多久），不是新的规则来源：
// tuneTiming() 的结果一律被 TIMING_BOUNDS 夹住，知识库边界永远赢。
//
// 一条样本 = 场景键（局面指纹）+ 人的动作（时长/方向/瞄准）+ 脚本本来会做什么。
// 只有「人合法」且「和脚本差得明显」的样本才值得存——存一堆和脚本一样的样本只会让检索变慢。

import { appendFileSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { MIN_HP_RATIO, RULES, checkAction } from './rule-gate.js'

const HERE = dirname(fileURLToPath(import.meta.url))
export const DEFAULT_CASE_PATH = join(HERE, '..', 'data', 'demo-cases.jsonl')

// 时序硬边界。人在示范里按了 900ms 跳跃也只会被夹到 450ms——知识库/物理规律的边界不允许被示范覆盖。
export const TIMING_BOUNDS = {
  jumpHoldMs: [60, 450],
  digHoldMs: [40, 400],
  moveHoldMs: [80, 1500],
  settleMs: [120, 600],
  aimLeadMs: [0, 400],
}

const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v))

function band(v, cuts) {
  if (!Number.isFinite(v)) return 'na'
  for (let i = 0; i < cuts.length; i++) if (v <= cuts[i]) return String(cuts[i])
  return `>${cuts[cuts.length - 1]}`
}

/**
 * 局面指纹。只放「会真的改变动作节奏」的量：环境、血线、脚下有没有地、目标方位、最近威胁。
 * 坐标不进键——那会让每个格子变成一个独立场景，检索永远命中不了。
 */
export function sceneOf(snap = {}) {
  const lifeMax = Number(snap.lifeMax) || 100
  const life = Number.isFinite(snap.life) ? snap.life : lifeMax
  const ratio = lifeMax > 0 ? life / lifeMax : 1
  const vx = Number(snap.velocity?.[0] ?? snap.vx ?? 0)
  const vy = Number(snap.velocity?.[1] ?? snap.vy ?? 0)
  const threat = Number(snap.hostileDist ?? snap.threat ?? 99)
  return {
    zone: snap.zone || 'surface',
    wet: snap.wet ? 1 : 0,
    ground: snap.grounded === false ? 0 : 1,
    hp: ratio > 0.75 ? 'high' : ratio > 0.45 ? 'mid' : ratio > 0.25 ? 'low' : 'critical',
    dx: band(Number(snap.dx ?? 0), [-8, -2, 0, 2, 8]),
    dy: band(Number(snap.dy ?? 0), [-8, -2, 0, 2, 8]),
    threat: threat <= 3 ? 'near' : threat <= 8 ? 'edge' : 'clear',
    moving: Math.abs(vx) > 0.5 ? (vx > 0 ? 'right' : 'left') : Math.abs(vy) > 1 ? 'fall' : 'still',
  }
}

export function sceneLabel(key = {}) {
  const h = sceneOf(key)
  return [h.zone, h.wet ? 'wet' : 'dry', h.ground ? 'ground' : 'air', `hp-${h.hp}`,
    `dx${h.dx}`, `dy${h.dy}`, `threat-${h.threat}`, h.moving].join('|')
}

const WEIGHTS = { zone: 0.26, wet: 0.14, ground: 0.14, hp: 0.12, threat: 0.12, moving: 0.10, dx: 0.06, dy: 0.06 }

/** 0..1 相似度。同族维度全等才满分；方位（dx/dy）权重低，因为它们是连续量，分档本来就粗。 */
export function similarity(a = {}, b = {}) {
  const ka = sceneOf(a), kb = sceneOf(b)
  let score = 0
  for (const [field, w] of Object.entries(WEIGHTS)) {
    if (ka[field] === kb[field]) score += w
    else if (field === 'hp') {
      const order = ['critical', 'low', 'mid', 'high']
      const d = Math.abs(order.indexOf(ka[field]) - order.indexOf(kb[field]))
      if (d === 1) score += w * 0.45
    } else if (field === 'threat') {
      const order = ['near', 'edge', 'clear']
      const d = Math.abs(order.indexOf(ka[field]) - order.indexOf(kb[field]))
      if (d === 1) score += w * 0.4
    }
  }
  return Math.round(score * 1000) / 1000
}

/**
 * 知识库闸门：不合法的样本直接丢弃，不进库。
 *
 * 判定本身不在本模块里写死——它委托给 `src/rule-gate.js`，每条规则都带着
 * `docs/kb/*.md` 的出处。这样"为什么丢弃这条示范"永远能追到一条成文规则上，
 * 而不是追到一个我随手定的阈值上。`reason` 字符串保持稳定（测试与日志都依赖），
 * 另外回传 `rule`/`kb` 便于日志直接写出来源。
 */
export function validateSample(sample = {}, opts = {}) {
  const snap = sample.snap || {}
  const human = sample.human || {}
  if (!String(human.kind || '') || String(human.kind) === 'idle') {
    return { ok: false, reason: 'no-human-action', rule: null, kb: null }
  }
  // 字段归一：本模块历史上读 `snap.velocity[1]`，闸门读 `snap.vy`。
  // 在这里一次性补齐，别让同一条规则在两条路径上得出两种结论。
  const norm = { ...snap, vy: Number(snap.velocity?.[1] ?? snap.vy) }
  const { violations } = checkAction(human, {
    snap: norm,
    opts: { minHpRatio: opts.minHpRatio ?? MIN_HP_RATIO },
    rules: opts.rules ?? RULES,
  })
  if (!violations.length) return { ok: true, reason: 'ok', rule: null, kb: null }
  const first = violations[0]
  return { ok: false, reason: first.reason, rule: first.id, kb: first.kb, violations }
}

const HOLD_KEYS = ['jumpHoldMs', 'digHoldMs', 'moveHoldMs', 'settleMs', 'aimLeadMs']

/**
 * 人和脚本的差异。只有「动作类型不同」或「同类动作时长差 30% 以上」才算值得记的修正，
 * 否则观摩会退化成把脚本自己的重复动作抄一遍。
 */
export function actionDelta(agentAction = {}, humanAction = {}) {
  const a = String(agentAction.kind || '')
  const h = String(humanAction.kind || '')
  if (a !== h) return { significant: true, reason: 'kind', agent: a, human: h }
  const diffs = []
  for (const key of HOLD_KEYS) {
    const av = Number(agentAction[key])
    const hv = Number(humanAction[key])
    // 只有两边都真的给出了正数才比。0/缺失 = "没测到这一项"，
    // 不能因为脚本填了 settleMs 而人手这段恰好没停就判成"人用了 0ms"。
    if (!Number.isFinite(av) || !Number.isFinite(hv) || av <= 0 || hv <= 0) continue
    const base = Math.max(av, 1)
    const rel = Math.abs(hv - av) / base
    if (rel >= 0.3) diffs.push({ key, agent: av, human: hv, rel: Math.round(rel * 100) / 100 })
  }
  if (diffs.length) return { significant: true, reason: 'timing', diffs }
  return { significant: false, reason: 'same' }
}

export class DemoLibrary {
  constructor({ file = DEFAULT_CASE_PATH, maxCases = 800 } = {}) {
    this.file = file
    this.maxCases = maxCases
    this.cases = []
    this.discarded = []
    this.load()
  }

  load() {
    this.cases = []
    if (!existsSync(this.file)) return this
    let text = ''
    try { text = readFileSync(this.file, 'utf8') } catch { return this }
    for (const line of text.split('\n')) {
      const s = line.trim()
      if (!s) continue
      try {
        const rec = JSON.parse(s)
        if (rec && rec.scene) this.cases.push(rec)
      } catch { /* 半行/脏行直接跳过，别让一条坏样本废掉整库 */ }
    }
    return this
  }

  /** 记一条样本。返回本次是否真的入库，以及被丢弃的原因。 */
  record(sample = {}, opts = {}) {
    const gate = validateSample(sample, opts)
    if (!gate.ok) {
      // 丢弃也要能溯源：rule/kb 来自知识库闸门，日志里直接写"违背了哪条成文规则"。
      this.discarded.push({ reason: gate.reason, rule: gate.rule ?? null, kb: gate.kb ?? null, at: sample.at || null })
      return { ok: false, reason: gate.reason, rule: gate.rule ?? null, kb: gate.kb ?? null }
    }
    const delta = sample.delta || actionDelta(sample.agent, sample.human)
    if (!delta.significant) return { ok: false, reason: 'not-significant' }
    const rec = {
      at: sample.at || new Date().toISOString(),
      scene: sceneOf(sample.snap),
      label: sceneLabel(sample.snap),
      human: sample.human,
      agent: sample.agent || null,
      delta,
      outcome: sample.outcome || null,
    }
    this.cases.push(rec)
    if (this.cases.length > this.maxCases) this.cases = this.cases.slice(-this.maxCases)
    this.flush(rec)
    return { ok: true, reason: 'recorded', case: rec }
  }

  flush(rec) {
    try {
      mkdirSync(dirname(this.file), { recursive: true })
      appendFileSync(this.file, JSON.stringify(rec) + '\n', 'utf8')
    } catch { /* 写盘失败不应该中断观摩，内存里的样本照样能用于本次微调 */ }
  }

  /** 按局面检索最像的几条人力示范。没有够像的就返回空——宁可不学，也别学错场景。 */
  recall(snap = {}, { minSim = 0.72, limit = 3 } = {}) {
    const q = sceneOf(snap)
    return this.cases
      .map(rec => ({ rec, sim: similarity(q, rec.scene) }))
      .filter(x => x.sim >= minSim)
      .sort((a, b) => b.sim - a.sim)
      .slice(0, limit)
  }

  /**
   * 能真的拿来调时序的样本：人的那一侧至少给出过一个按住时长。
   * 只记了"动作选错"、没给时长的样本仍然有价值（纠正动作类），但进不了 tuneTiming。
   */
  static isUsable(rec) {
    const human = rec?.human || {}
    return HOLD_KEYS.some(k => Number.isFinite(Number(human[k])))
  }

  /** 最近 limit 条可用样本，按时间倒序——给人看的"这局学到了什么"。 */
  corrections(limit = 8) {
    return this.cases.filter(c => DemoLibrary.isUsable(c)).slice(-Math.max(1, limit))
  }

  stats() {
    const byKind = {}
    const byLabel = {}
    let usable = 0
    for (const c of this.cases) {
      const k = c.delta?.reason || 'unknown'
      byKind[k] = (byKind[k] || 0) + 1
      const label = c.label || 'unknown'
      byLabel[label] = (byLabel[label] || 0) + 1
      if (DemoLibrary.isUsable(c)) usable += 1
    }
    return {
      total: this.cases.length,
      usable,
      discarded: this.discarded.length,
      byKind,
      byLabel,
      file: this.file,
    }
  }

  clear() {
    this.cases = []
    try { if (existsSync(this.file)) writeFileSync(this.file, '', 'utf8') } catch { /* ignore */ }
    return this
  }
}

/**
 * 用人力示范微调时序。返回 { timing, applied, clamped }：
 *  - applied 每条都带来源（哪条示范、相似度多少），便于复述「这次被哪几条改了」。
 *  - clamped 是被人超出硬边界的部分——知识库在这里赢，超了就夹回去。
 */
export function tuneTiming(base = {}, cases = [], opts = {}) {
  const timing = {}
  for (const key of HOLD_KEYS) {
    const v = Number(base[key])
    if (Number.isFinite(v)) timing[key] = v
  }
  const applied = []
  const clamped = []
  const groups = new Map()
  for (const item of cases) {
    const rec = item.rec || item
    const sim = Number(item.sim ?? 1)
    const human = rec.human || {}
    for (const key of HOLD_KEYS) {
      const hv = Number(human[key])
      if (!Number.isFinite(hv)) continue
      if (!groups.has(key)) groups.set(key, [])
      groups.get(key).push({ value: hv, sim, from: rec.label || null })
    }
  }
  const minSim = opts.minSim ?? 0.72
  // 一两条样本就说"人的做法是这样"，那是把手抖当规律。默认要三条同时指向才改。
  const minSamples = opts.minSamples ?? 3
  for (const [key, list] of groups) {
    const usable = list.filter(x => x.sim >= minSim).sort((a, b) => a.value - b.value)
    if (usable.length < minSamples) continue
    // 相似度加权中位数：比平均值抗单条异常样本（人偶尔会手抖按久一点）。
    const total = usable.reduce((s, x) => s + x.sim, 0)
    let acc = 0, pick = usable[usable.length - 1]
    for (const x of usable) {
      acc += x.sim
      if (acc >= total / 2) { pick = x; break }
    }
    const [lo, hi] = TIMING_BOUNDS[key] || [-Infinity, Infinity]
    const target = clamp(pick.value, lo, hi)
    const before = timing[key] ?? pick.value
    if (target === pick.value && Math.abs(target - before) > 0) {
      applied.push({ param: key, from: before, to: target, sim: pick.sim, from_case: pick.from })
    } else if (target !== pick.value) {
      clamped.push({ param: key, wanted: pick.value, allowed: target, bounds: [lo, hi] })
    }
    timing[key] = target
  }
  return { timing, applied, clamped }
}