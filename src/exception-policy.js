/**
 * 全局异常处理体系（通用，不绑定任何单个任务）
 *
 * 设计位置：和五维元自检同级，属于「常驻内核」，不是知识库条目。
 * 五维元自检解决「动手之前缺不缺知识/道具/能力」；
 * 本模块解决「动手过程中出了意外怎么办，以及处理完怎么回到主线」。
 *
 * 三条不可违反的规则：
 *   1) 顶层主目标只有一个，任何应急都是**临时子状态**，处理完必须回到原子步骤，不允许被替换；
 *   2) 每次异常触发 / 重试 / 失败都留日志（原因 + 动作 + 结果 + 第几次）；
 *   3) 判定与决策是纯函数，执行由调用方注入（便于单测，也便于以后换执行层）。
 */

import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const HERE = dirname(fileURLToPath(import.meta.url))
/** 全局异常处理规则文本（prompts/exception-policy.md），惰性读盘：缺文件时插件还能 import。 */
export const EXCEPTION_POLICY_PATH = join(HERE, '..', 'prompts', 'exception-policy.md')
let policyCache = null
export function loadExceptionPrompt() {
  if (policyCache === null) policyCache = readFileSync(EXCEPTION_POLICY_PATH, 'utf8')
  return policyCache
}

export const EXCEPTION_KINDS = {
  terrain_blocked: { severity: 'low', zh: '地形阻挡', recover: 'jump_then_reroute' },
  terrain_stuck: { severity: 'mid', zh: '卡死', recover: 'replan' },
  hostile_near: { severity: 'mid', zh: '敌对生物', recover: 'avoid_or_fight' },
  hp_low: { severity: 'high', zh: '血量偏低', recover: 'retreat_heal' },
  fatal_env: { severity: 'critical', zh: '致命环境', recover: 'abort_move' },
  liquid_deep: { severity: 'high', zh: '深水', recover: 'abort_move' },
  missing_item: { severity: 'mid', zh: '道具缺失', recover: 'pause_report' },
  inventory_full: { severity: 'low', zh: '背包满', recover: 'report_only' },
  fall_risk: { severity: 'high', zh: '落差危险', recover: 'abort_move' },
}

/** 重试预算：超过就判定任务失败并上报，而不是无限重试。 */
export const RETRY_BUDGET = {
  jump: 2,        // 同一处连续跳跃尝试
  reroute: 3,     // 重新寻路次数
  replan: 3,      // 卡死后整体重规划次数
  evade: 2,       // 同一威胁规避次数
}

/** 判定阈值（默认值；调用方可覆盖） */
export const DEFAULTS = {
  hpLowRatio: 0.45,        // 血量低于这个比例算「血量偏低」
  hpPanicRatio: 0.25,      // 低于这个比例只逃不打
  hostileAlertTiles: 8,    // 多少格内算「敌对生物接近」
  hostileFightTiles: 6,    // 这个距离内且退无可退才反击
  stuckSameTiles: 3,       // 连续几次坐标不变算卡死
  stuckEpsilon: 0.6,       // 坐标变化小于这个距离视为「没动」
  fatalLiquidTiles: 2,     // 前方几格内有岩浆/毒液算致命
  fallDangerTiles: 8,      // 前方落差超过多少格算危险
}

const num = (v, d = 0) => (typeof v === 'number' && Number.isFinite(v) ? v : d)

/**
 * 纯判定：把一帧快照翻译成「命中了哪些异常」。
 * 不做任何动作，也不改状态——只回答「现在有什么问题」。
 *
 * @param {object} snap  由 observe() 采集
 * @param {object} [opts] 覆盖 DEFAULTS
 * @returns {{kind:string,severity:string,evidence:string,ratio?:number}[]}
 */
export function classify(snap = {}, opts = {}) {
  const o = { ...DEFAULTS, ...opts }
  const hits = []
  const p = snap.player || {}
  const life = num(p.life, num(p.hp, 100))
  const lifeMax = num(p.lifeMax, num(p.hpMax, 100)) || 100
  const ratio = lifeMax > 0 ? life / lifeMax : 1

  // ① 致命环境优先级最高：踩进岩浆/毒液、前方深水、前方大落差
  const env = snap.env || {}
  if (env.onFatal) hits.push({ kind: 'fatal_env', severity: 'critical', evidence: `脚下/身边是 ${env.onFatal}` })
  if (env.fatalAhead) hits.push({ kind: 'fatal_env', severity: 'critical', evidence: `前方 ${env.fatalAhead.dist} 格有 ${env.fatalAhead.what}` })
  if (num(env.deepWaterAhead?.dist, 999) <= o.fatalLiquidTiles) {
    hits.push({ kind: 'liquid_deep', severity: 'high', evidence: `前方 ${env.deepWaterAhead.dist} 格是深水` })
  }
  if (num(env.dropAhead, 0) >= o.fallDangerTiles) {
    hits.push({ kind: 'fall_risk', severity: 'high', evidence: `前方落差 ${env.dropAhead} 格` })
  }

  // ② 血量
  if (ratio <= o.hpPanicRatio) hits.push({ kind: 'hp_low', severity: 'high', evidence: `血量 ${Math.round(ratio * 100)}%（危险线）`, ratio })
  else if (ratio <= o.hpLowRatio) hits.push({ kind: 'hp_low', severity: 'high', evidence: `血量 ${Math.round(ratio * 100)}%`, ratio })

  // ③ 敌对生物
  const hostiles = Array.isArray(snap.hostiles) ? snap.hostiles : []
  const nearest = hostiles
    .filter(h => num(h.dist, 999) <= o.hostileAlertTiles)
    .sort((a, b) => num(a.dist, 999) - num(b.dist, 999))[0]
  if (nearest) {
    hits.push({ kind: 'hostile_near', severity: 'mid', evidence: `${nearest.name || '敌对生物'} 距离 ${Math.round(num(nearest.dist))} 格`, dist: num(nearest.dist) })
  }

  // ④ 卡死 / 地形阻挡
  const st = snap.stuck || {}
  if (num(st.sameCount, 0) >= o.stuckSameTiles) hits.push({ kind: 'terrain_stuck', severity: 'mid', evidence: `连续 ${st.sameCount} 次坐标不变` })
  else if (st.blocked) hits.push({ kind: 'terrain_blocked', severity: 'low', evidence: st.blockedBy ? `被 ${st.blockedBy} 挡住` : '前方被挡' })

  // ⑤ 道具 / 背包
  const inv = snap.inventory || {}
  if (Array.isArray(inv.missing) && inv.missing.length) hits.push({ kind: 'missing_item', severity: 'mid', evidence: `缺：${inv.missing.join('、')}` })
  if (num(inv.free, 1) <= 0) hits.push({ kind: 'inventory_full', severity: 'low', evidence: '背包没有空格' })

  return hits
}

/** 任务状态分层：goal（主目标）+ step（当前子步骤）+ emergency（临时应急，最多一层） */
export function createTaskState(goal, step = null) {
  if (!goal || typeof goal !== 'string') throw new Error('createTaskState 需要 goal')
  const state = {
    goal,
    step,
    emergency: null,
    attempts: { jump: 0, reroute: 0, replan: 0, evade: 0 },
    failures: 0,
    history: [],
  }
  return state
}

/** 进入应急：只写 emergency，绝不动 goal/step。 */
export function enterEmergency(state, kind, evidence) {
  if (!EXCEPTION_KINDS[kind]) throw new Error(`未知异常类型 ${kind}`)
  state.emergency = { kind, evidence, since: state.history.length }
  return state.emergency
}

/** 应急处理完成：清掉应急，回到原来的 step（原样保留）。 */
export function resolveEmergency(state, outcome = 'ok') {
  const e = state.emergency
  state.emergency = null
  return { cleared: e, outcome, resumeStep: state.step }
}

/**
 * 纯决策：命中异常 → 给出「这一 Tick 该做什么」。
 * 不执行动作；执行交给 applyPlan()。返回的 plan 里一定带 `resume: true`（处理完回主线）。
 */
export function decide(hits, state, snap = {}, opts = {}) {
  const o = { ...DEFAULTS, ...opts }
  const plan = []
  const attempts = state.attempts || (state.attempts = { jump: 0, reroute: 0, replan: 0, evade: 0 })
  const has = k => hits.some(h => h.kind === k)
  const hit = k => hits.find(h => h.kind === k)

  // 优先级：致命环境 > 血量 > 敌对 > 卡死 > 阻挡 > 道具 > 背包
  if (has('fatal_env') || has('liquid_deep') || has('fall_risk')) {
    const h = hit('fatal_env') || hit('liquid_deep') || hit('fall_risk')
    enterEmergency(state, h.kind, h.evidence)
    plan.push({ action: 'move', dir: 'stop', why: '立刻停止移动' })
    plan.push({ action: 'move', dir: 'back', ms: 400, why: '退回安全格' })
    plan.push({ action: 'probe', what: 'tile_ahead', why: '确认前方环境安全再继续' })
    return { plan, resume: true, emergency: state.emergency, fatal: false }
  }

  if (has('hp_low')) {
    const h = hit('hp_low')
    enterEmergency(state, 'hp_low', h.evidence)
    const panic = num(h.ratio, 1) <= o.hpPanicRatio
    plan.push({ action: 'move', dir: 'away_from_hostile', ms: 900, why: '先拉开距离' })
    plan.push({ action: 'heal', why: '喝治疗药水（有就喝）' })
    if (panic) plan.push({ action: 'retreat', why: '危险线以下只逃不打' })
    return { plan, resume: true, emergency: state.emergency }
  }

  if (has('hostile_near')) {
    const h = hit('hostile_near')
    enterEmergency(state, 'hostile_near', h.evidence)
    const canFight = num(h.dist, 99) <= o.hostileFightTiles && num(snap.hostiles?.[0]?.dist, 99) > 0
    if (attempts.evade < RETRY_BUDGET.evade) {
      attempts.evade++
      plan.push({ action: 'move', dir: 'away_from_hostile', ms: 700, why: `第 ${attempts.evade} 次避险后撤` })
      plan.push({ action: 'check_threat', why: '确认威胁是否甩开' })
    } else if (canFight) {
      plan.push({ action: 'attack', r: 12, why: '甩不开且贴脸，反击' })
      plan.push({ action: 'move', dir: 'stop', why: '站稳，别追' })
      plan.push({ action: 'check_threat', why: '威胁清掉立刻回主线' })
    } else {
      attempts.evade = 0
      plan.push({ action: 'attack', r: 14, why: '无法甩开，边打边退' })
    }
    return { plan, resume: true, emergency: state.emergency }
  }

  if (has('terrain_stuck')) {
    const h = hit('terrain_stuck')
    enterEmergency(state, 'terrain_stuck', h.evidence)
    if (attempts.replan < RETRY_BUDGET.replan) {
      attempts.replan++
      plan.push({ action: 'replan', why: `第 ${attempts.replan} 次重新规划路线` })
      plan.push({ action: 'path', why: 'A* 重算到同一目标点' })
      return { plan, resume: true, emergency: state.emergency }
    }
    plan.push({ action: 'fail', why: `重新规划 ${attempts.replan} 次仍卡死` })
    return { plan, resume: false, emergency: state.emergency, fatal: true }
  }

  if (has('terrain_blocked')) {
    const h = hit('terrain_blocked')
    enterEmergency(state, 'terrain_blocked', h.evidence)
    if (attempts.jump < RETRY_BUDGET.jump) {
      attempts.jump++
      plan.push({ action: 'jump', ms: 160, why: `第 ${attempts.jump} 次尝试跳过` })
      plan.push({ action: 'probe', what: 'position', why: '看跳完有没有过去' })
    } else if (attempts.reroute < RETRY_BUDGET.reroute) {
      // 绕行开始后不再回头重试跳跃：否则「跳 2 次→绕行→跳 2 次」会无限循环，永远到不了失败判定。
      attempts.reroute++
      plan.push({ action: 'reroute', why: `跳不过，第 ${attempts.reroute} 次绕路` })
      plan.push({ action: 'path', why: '绕行到同一目标点' })
    } else {
      plan.push({ action: 'fail', why: `绕路 ${attempts.reroute} 次仍过不去` })
      return { plan, resume: false, emergency: state.emergency, fatal: true }
    }
    return { plan, resume: true, emergency: state.emergency }
  }

  if (has('missing_item')) {
    const h = hit('missing_item')
    enterEmergency(state, 'missing_item', h.evidence)
    plan.push({ action: 'pause', why: '道具缺失：先补齐，不硬走' })
    plan.push({ action: 'report_gap', why: h.evidence })
    return { plan, resume: true, emergency: state.emergency, blocked: true }
  }

  if (has('inventory_full')) {
    const h = hit('inventory_full')
    enterEmergency(state, 'inventory_full', h.evidence)
    plan.push({ action: 'report', why: '背包满：先存箱/丢垃圾，否则掉落物会丢' })
    return { plan, resume: true, emergency: state.emergency }
  }

  return { plan: [{ action: 'continue', why: '无异常，继续原子步骤' }], resume: true, emergency: null }
}

/** 日志：每次触发/重试/失败都记一条（原因、动作、结果、第几次）。 */
export function createLog() {
  const entries = []
  return {
    entries,
    add(kind, evidence, action, outcome, attempt = 0) {
      const e = { seq: entries.length + 1, kind, evidence, action, outcome, attempt }
      entries.push(e)
      return e
    },
    markdown() {
      if (!entries.length) return '（本轮没有触发异常）'
      const head = '| # | 异常 | 证据 | 动作 | 结果 | 第几次 |\n|---|---|---|---|---|---|'
      const rows = entries.map(e => `| ${e.seq} | ${e.kind} | ${e.evidence} | ${e.action} | ${e.outcome} | ${e.attempt} |`)
      return [head, ...rows].join('\n')
    },
    reset() { entries.length = 0 },
  }
}

/**
 * 执行层：把 plan 映射成真实命令。调用方注入 request(command, params)。
 * 只做「控制角色」的动作（移动/跳/攻击/喝药/寻路），不改世界。
 */
/** 只记计划、不发命令的动作（判定/上报类）。 */
const NON_EXEC = new Set(['probe', 'check_threat', 'replan', 'reroute', 'retreat', 'report', 'report_gap', 'pause', 'continue', 'fail'])

export async function applyPlan(plan, request, log, evidence = '') {
  const done = []
  for (const step of plan) {
    if (NON_EXEC.has(step.action)) { done.push({ action: step.action, ok: true, executed: false }); continue }
    try {
      switch (step.action) {
        case 'move':
          if (step.dir === 'stop') await request('move', { dir: 'up', ms: 1 })
          else if (step.dir === 'away_from_hostile') await request('move', { dir: 'left', ms: step.ms || 700 })
          else if (step.dir === 'back') await request('move', { dir: 'left', ms: step.ms || 400 })
          else await request('move', { dir: step.dir, ms: step.ms || 400 })
          break
        case 'jump': await request('jump', { ms: step.ms || 160 }); break
        case 'attack': await request('attack', { r: step.r || 12, ms: 400 }); break
        case 'heal': await request('heal', {}); break
        case 'path': await request('path', {}); break
        default: break
      }
      done.push({ action: step.action, ok: true })
      if (log && step.action !== 'continue') log.add(step.action, evidence, step.why || '', 'ok')
    } catch (err) {
      done.push({ action: step.action, ok: false, error: String(err && err.message || err) })
      if (log) log.add(step.action, evidence, step.why || '', 'error')
    }
  }
  return done
}

/**
 * 一帧完整处理：采集 → 判定 → 决策 →（可选）执行 → 记日志。
 * 返回 { hits, plan, applied, resumed, state }；调用方据此决定是否继续原子步骤。
 */
export async function tick({ snap, state, request, log, apply = false, opts = {} }) {
  const hits = classify(snap, opts)
  const decision = decide(hits, state, snap, opts)
  let applied = []
  if (apply && request) applied = await applyPlan(decision.plan, request, log, hits.map(h => h.evidence).join('；'))
  const resumed = decision.resume && !decision.fatal
  if (!decision.fatal && decision.emergency) resolveEmergency(state, 'handled')
  if (decision.fatal) state.failures++
  return { hits, plan: decision.plan, applied, resumed, fatal: !!decision.fatal, state }
}
/**
 * 采集一帧快照（用桥的既有命令，不新增 mod 命令）。
 * 只读：state / npc / inventory / tile，不改世界。
 *
 * @param {(cmd:string,params?:object)=>Promise<any>} request
 */
export async function observe(request, opts = {}) {
  const o = { ...DEFAULTS, ...opts }
  const get = async (cmd, params = {}) => {
    try { const r = await request(cmd, params); return r && r.data !== undefined ? r.data : r } catch { return null }
  }
  const st = await get('state')
  if (!st || !st.player) return null
  const p = st.player
  const tile = p.tile || [0, 0]
  const hostiles = []
  const npc = await get('npc', { r: o.hostileAlertTiles + 4 })
  for (const n of (npc && (npc.npcs || npc.found || [])) || []) {
    if (n.hostile || n.friendly === false) hostiles.push({ name: n.name, dist: n.dist, hp: n.life })
  }
  const inv = await get('inventory')
  const slots = (inv && (inv.items || inv.slots)) || []
  const free = (inv && typeof inv.mainFree === 'number') ? inv.mainFree : slots.filter(s => !s || !s.type || s.type <= 0).length
  // 前方 2 格的环境探测（只读 3 个格子，避免把帧率拖垮）
  const ahead = []
  for (const dx of [1, 2]) {
    const t = await get('tile', { x: Math.round(tile[0]) + dx, y: Math.round(tile[1]) })
    if (t) ahead.push({ dx, type: t.type, liquidName: t.liquidName || '', solid: !!t.solid })
  }
  const fatalAhead = ahead.find(a => a.liquidName === 'lava' || a.liquidName === 'shimmer' || a.liquidName === 'honey')
  const deepWater = ahead.find(a => a.liquidName === 'water')
  return {
    player: { tile, life: p.life ?? p.hp, lifeMax: p.lifeMax ?? p.hpMax, grounded: p.grounded, wet: !!p.wet },
    hostiles,
    env: {
      onFatal: p.wet && (p.liquidName === 'lava' || p.liquidName === 'shimmer') ? (p.liquidName) : undefined,
      fatalAhead: fatalAhead ? { dist: fatalAhead.dx, what: fatalAhead.liquidName } : undefined,
      deepWaterAhead: deepWater ? { dist: deepWater.dx } : undefined,
      dropAhead: undefined,
    },
    inventory: { free, missing: opts.missing || [] },
    stuck: opts.stuck || {},
  }
}
