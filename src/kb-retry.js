/**
 * 卡点重试检索（retrieval on blockage）。
 * ============================================================================
 * 病根（用户 m19902 指出）：知识库检索只在任务开始时做**一次**。第一轮没命中，
 * 后面整段执行就带着缺失的知识往下跑；失败了也不会回头想"是不是我漏查了"。
 *
 * 这里把它改成**由卡点触发**的重复检索，且只在真卡住时才触发：
 *
 *  1. 检索不是一次性前置动作。以下信号出现时触发二次/多次检索：
 *     no-path / action-failed / safety-violation / target-unreachable。
 *  2. 每次受阻都**重走全套分类检查**，逐类确认是不是漏了该分类下的规则。
 *  3. 结果必须标记"本次有没有命中**新**条目"：
 *     有新 → 回决策链路重新评估；仍无新增 → 才允许判定任务无法完成。
 *  4. 不是无脑反复检索：只认上面那四个信号，且同一信号同一上下文不重复烧。
 *
 * 模块刻意不碰 I/O：`route` 与 `loadDoc` 由调用方注入，所以真库和测试假库都能跑。
 * ============================================================================
 */

/** 允许触发重试的卡点信号。别的信号一律不触发（约束 4）。 */
export const BLOCK_SIGNALS = Object.freeze({
  noPath: 'no-path',
  actionFailed: 'action-failed',
  safetyViolation: 'safety-violation',
  targetUnreachable: 'target-unreachable',
})

const SIGNAL_SET = new Set(Object.values(BLOCK_SIGNALS))

/**
 * 每个卡点信号在"全套分类检查"时必须**强制复核**的分类。
 * 理由写在这里，因为下次改它的人需要知道为什么是这几个：
 *  - no-path：走不过去要么是地形判定错（navigation），要么是被水体/危险区挡住（world_biome），
 *    要么是自己的房子/建筑挡路且不许拆（build_standard）。
 *  - action-failed：动作发出去没效果，最常见是拿错工具（item_operation）、
 *    够不到或没支撑（atomic_action），或报文/默认值问题（atomic_action）。
 *  - safety-violation：被安全线拦下，规则本身在 safety_limit，具体数值常在 combat_math。
 *  - target-unreachable：目标格站不住/被占格物挡住（navigation、item_operation）。
 */
export const SIGNAL_CATEGORIES = Object.freeze({
  [BLOCK_SIGNALS.noPath]: ['navigation', 'world_biome', 'build_standard'],
  [BLOCK_SIGNALS.actionFailed]: ['item_operation', 'atomic_action'],
  [BLOCK_SIGNALS.safetyViolation]: ['safety_limit', 'combat_math'],
  [BLOCK_SIGNALS.targetUnreachable]: ['navigation', 'item_operation'],
})

/** 全套分类（与 docs/kb/ 的库一一对应；这里只列名字，文件映射由 kb-router 负责）。 */
export const ALL_CATEGORIES = Object.freeze([
  'safety_limit',
  'game_rule',
  'world_biome',
  'progression_boss',
  'gear_route',
  'combat_math',
  'loot_source',
  'npc_town',
  'build_standard',
  'actor_behavior',
  'atomic_action',
  'item_operation',
  'task_correct',
  'navigation',
])

const norm = s => String(s ?? '').trim()

/**
 * 卡点检索闸门。
 *
 * @param {object} args
 * @param {(text:string)=>string[]} args.route  把一段话路由成 tags（通常就是 kb-router 的 route）
 * @param {(tag:string)=>string|null} [args.loadDoc] 取某个库的正文，用于判断"是否命中新条目"
 * @param {number} [args.maxRounds] 同一次任务最多重试几轮（默认 4，够覆盖连续多个卡点）
 * @param {(tag:string)=>boolean} [args.exists] 判断库是否真的存在（默认 loadDoc 非空即存在）
 */
export function createRetryGate({ route, loadDoc, maxRounds = 4, exists } = {}) {
  if (typeof route !== 'function') throw new TypeError('createRetryGate needs route(text)')

  const has = exists || (tag => (typeof loadDoc === 'function' ? !!norm(loadDoc(tag)) : true))
  const seen = new Set() // 已经查过的 tag（跨轮累积）
  const tried = new Set() // 已经触发过的 signal+detail 组合，防同一卡点空转
  let task = ''
  let rounds = 0
  let history = []

  function intake(tags) {
    const fresh = []
    for (const t of tags) {
      if (!ALL_CATEGORIES.includes(t)) continue
      if (seen.has(t)) continue
      if (!has(t)) continue
      seen.add(t)
      fresh.push(t)
    }
    return fresh
  }

  /** 任务开始时调一次（这就是"首次前置检索"，只做一次）。 */
  function begin(text) {
    task = norm(text)
    rounds = 0
    history = []
    seen.clear()
    tried.clear()
    const first = typeof route === 'function' ? route(task) : []
    const tags = (Array.isArray(first) ? first : first?.tags || []).filter(t => ALL_CATEGORIES.includes(t))
    // begin 不把首次命中的当成"新知识"，它们本来就是已知上下文
    for (const t of tags) seen.add(t)
    history.push({ round: 0, phase: 'begin', tags: [...tags], fresh: [] })
    return { tags: [...tags], fresh: [] }
  }

  /**
   * 卡住了就调这个。
   * @param {object} args
   * @param {string} args.signal  BLOCK_SIGNALS 之一（别的信号直接拒绝，不重试）
   * @param {string} [args.detail] 卡点的具体现象（例如 "no-path: 2173→2215" / "place 无支撑"）
   * @returns {{action:'retry'|'give-up', fresh:string[], allTags:string[], forced:string[], round:number, reason:string}}
   */
  function onBlockage({ signal, detail = '' } = {}) {
    const sig = norm(signal)
    if (!SIGNAL_SET.has(sig)) {
      return { action: 'skip', fresh: [], allTags: [...seen], forced: [], round: rounds, reason: `不是卡点信号（${sig || '空'}），不触发重试检索` }
    }
    const key = sig + '|' + norm(detail)
    if (tried.has(key)) {
      // 同一卡点重复 ≠ 知识不足；只是不重复烧。真正的"判定做不到"要等全套复核后仍无新增。
      return { action: 'skip', fresh: [], allTags: [...seen], forced: [], round: rounds, reason: '同一卡点同一现象已经重试过，不重复烧' }
    }
    if (rounds >= maxRounds) {
      return { action: 'give-up', fresh: [], allTags: [...seen], forced: [], round: rounds, reason: `重试轮数已达上限 ${maxRounds}，判定当前知识不足` }
    }
    tried.add(key)
    rounds += 1

    // 约束 2：每次受阻都**重走全套分类检查**。
    // 做法 = ①把卡点现象拼进查询重跑关键词路由；②再按 SIGNAL_CATEGORIES 强制复核相关分类。
    const queryParts = [task, sig, norm(detail)].filter(Boolean)
    const routed = route(queryParts.join(' '))
    const routedTags = (Array.isArray(routed) ? routed : routed?.tags || []).filter(t => ALL_CATEGORIES.includes(t))
    const forced = SIGNAL_CATEGORIES[sig] || []

    const fresh = intake([...routedTags, ...forced])
    history.push({ round: rounds, phase: 'blockage', signal: sig, detail: norm(detail), tags: [...new Set([...routedTags, ...forced])], fresh })

    if (fresh.length) {
      return { action: 'retry', fresh, allTags: [...seen], forced, round: rounds, reason: `命中 ${fresh.length} 条之前没有的知识：${fresh.join(',')}，回决策链路重新评估` }
    }
    return { action: 'give-up', fresh: [], allTags: [...seen], forced, round: rounds, reason: '全套分类复核后仍无新增知识，才允许判定任务无法完成' }
  }

  function status() {
    return { task, rounds, maxRounds, seen: [...seen], history: history.map(h => ({ ...h })) }
  }

  return { begin, onBlockage, status, BLOCK_SIGNALS }
}

/** 把闸门的判定结果写成一句能直接贴进日志的话（第 5 条要求：报告本次修正了什么）。 */
export function formatRetryVerdict(r) {
  if (!r) return '（无检索结果）'
  const head = { retry: '回决策链路重评', 'give-up': '判定知识不足', skip: '不触发' }[r.action] || r.action
  const fresh = r.fresh?.length ? ` 新增：${r.fresh.join(',')}` : ' 新增：无'
  return `[第${r.round}轮 ${head}]${fresh} —— ${r.reason}`
}