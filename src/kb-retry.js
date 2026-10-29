/**
 * 卡点重试检索（retrieval on blockage）。
 * ============================================================================
 * 病根一（用户 m19902）：知识库检索只在任务开始时做**一次**。第一轮没命中，
 * 后面整段执行就带着缺失的知识往下跑；失败了也不会回头想"是不是我漏查了"。
 *
 * 病根二（用户 m20002，更隐蔽）：**检索上下文割裂**。任务是 A，一开始检索漏了 A 的
 * 某个库；执行途中冒出衍生问题 B，于是拿 B 去检索、只拉 B 的东西，**原始目标 A 再也没
 * 有人回去补**。A 缺的知识从头到尾补不上，最后还把"没知识"误判成"做不到"。
 *
 * 修法（用户 m20002 的四条约束，逐条对应到代码）：
 *  1. `taskTags` 在整个任务生命周期内保留，**不被衍生问题覆盖**（见 `begin`）；
 *  2. 任意一次重检索的输入 = 【原始目标 + 当前衍生问题】，两部分都参与分类遍历
 *     （见 `onBlockage` 里的 `candidates`）；
 *  3. 知识池只累加不替换（`pool`），每次把新查到的并进去，不回退；
 *  4. 只有【原始目标相关分类全部遍历确认无匹配】且【当前衍生问题也无匹配】才判失败
 *     （见 `onBlockage` 末尾的 `uncovered` 检查）。
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
  'abilities',
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

/** 每轮最多顺手补查几个"原始目标还没遍历过"的分类（防止一轮把 14 个库全拉进上下文）。 */
export const SWEEP_PER_ROUND = 4

const norm = s => String(s ?? '').trim()
const uniq = arr => [...new Set(arr)]

/**
 * 卡点检索闸门。
 *
 * @param {object} args
 * @param {(text:string)=>string[]} args.route  把一段话路由成 tags（通常就是 kb-router 的 route）
 * @param {(tag:string)=>string|null} [args.loadDoc] 取某个库的正文，用于判断"是否命中新条目"
 * @param {number} [args.maxRounds] 同一次任务最多重试几轮（默认 6）
 * @param {(tag:string)=>boolean} [args.exists] 判断库是否真的存在（默认 loadDoc 非空即存在）
 * @param {boolean} [args.sweepAll] 是否允许补查"原始目标没命中但可能相关"的分类（默认 true）
 */
export function createRetryGate({ route, loadDoc, maxRounds = 6, exists, sweepAll = true } = {}) {
  if (typeof route !== 'function') throw new TypeError('createRetryGate needs route(text)')

  const has = exists || (tag => (typeof loadDoc === 'function' ? !!norm(loadDoc(tag)) : true))

  /** 约束 3：知识池。只累加，永不替换、永不清空（除了 begin 新任务）。 */
  const pool = new Set()
  /** 已经触发过的 signal+detail 组合，防同一卡点空转。 */
  const tried = new Set()

  /** 约束 1：原始目标与其分类，任务生命周期内不变。 */
  let task = ''
  let taskTags = []
  /**
   * 原始目标下"还没遍历过"的分类。`route(task)` 可能漏掉该查的库（病根二），
   * 所以任务一开始就把**所有分类**记成待确认；后续每次卡点顺手补一批。
   */
  let pending = []
  let rounds = 0
  let history = []

  function intake(tags) {
    const fresh = []
    for (const t of tags) {
      if (!ALL_CATEGORIES.includes(t)) continue
      if (pool.has(t)) continue
      if (!has(t)) continue
      pool.add(t)
      fresh.push(t)
    }
    return fresh
  }

  /** 任务开始时调一次（这就是"首次前置检索"，只做一次）。 */
  function begin(text) {
    task = norm(text)
    rounds = 0
    history = []
    pool.clear()
    tried.clear()

    const first = route(task)
    taskTags = (Array.isArray(first) ? first : first?.tags || []).filter(t => ALL_CATEGORIES.includes(t))
    // 首次命中的分类进入知识池（它们就是已知上下文，不算"新知识"）。
    for (const t of taskTags) pool.add(t)
    // 约束 1+2：原始目标没命中的分类全部记为待确认，后面卡点时按批补。
    pending = sweepAll ? ALL_CATEGORIES.filter(t => !taskTags.includes(t)) : []

    history.push({ round: 0, phase: 'begin', taskTags: [...taskTags], fresh: [], pending: pending.length })
    return { tags: [...taskTags], fresh: [], pending: [...pending] }
  }

  /**
   * 卡住了就调这个。
   * @param {object} args
   * @param {string} args.signal  BLOCK_SIGNALS 之一（别的信号直接拒绝，不重试）
   * @param {string} [args.detail] 卡点的具体现象（例如 "no-path: 2173→2215" / "place 无支撑"）
   * @returns {{action:'retry'|'give-up'|'skip', fresh:string[], allTags:string[], forced:string[], round:number, reason:string}}
   */
  function onBlockage({ signal, detail = '' } = {}) {
    const sig = norm(signal)
    if (!SIGNAL_SET.has(sig)) {
      return { action: 'skip', fresh: [], allTags: [...pool], forced: [], round: rounds, reason: `不是卡点信号（${sig || '空'}），不触发重试检索` }
    }
    const key = sig + '|' + norm(detail)
    if (tried.has(key)) {
      // 同一卡点重复 ≠ 知识不足；只是不重复烧。真正的"判定做不到"要等遍历完仍无新增。
      return { action: 'skip', fresh: [], allTags: [...pool], forced: [], round: rounds, reason: '同一卡点同一现象已经重试过，不重复烧' }
    }
    if (rounds >= maxRounds) {
      return { action: 'give-up', fresh: [], allTags: [...pool], forced: [], round: rounds, reason: `重试轮数已达上限 ${maxRounds}，判定当前知识不足` }
    }
    tried.add(key)
    rounds += 1

    // 约束 2：检索输入 = 原始目标 + 当前衍生问题，两部分都参与分类遍历。
    const routedB = route([sig, norm(detail)].filter(Boolean).join(' '))
    const bTags = (Array.isArray(routedB) ? routedB : routedB?.tags || []).filter(t => ALL_CATEGORIES.includes(t))
    const forced = SIGNAL_CATEGORIES[sig] || []

    // 约束 1：`task`（原始目标）永远在这条查询里，且 `taskTags` 永远参与遍历。
    // 额外把"原始目标还没遍历过"的分类按批补进来，这是病根二的正解。
    const sweep = pending.slice(0, SWEEP_PER_ROUND)
    for (const t of sweep) pending = pending.filter(x => x !== t)

    const candidates = uniq([...taskTags, ...bTags, ...forced, ...sweep])
    const fresh = intake(candidates)
    history.push({
      round: rounds,
      phase: 'blockage',
      signal: sig,
      detail: norm(detail),
      taskTags: [...taskTags],
      candidates,
      sweep,
      fresh,
      pending: pending.length,
    })

    if (fresh.length) {
      const fromTask = fresh.filter(t => taskTags.includes(t) || sweep.includes(t))
      const tail = fromTask.length ? `（其中 ${fromTask.join(',')} 属于原始目标欠查的部分）` : ''
      return {
        action: 'retry',
        fresh,
        allTags: [...pool],
        forced,
        round: rounds,
        reason: `命中 ${fresh.length} 条之前没有的知识：${fresh.join(',')}${tail}，回决策链路重新评估`,
      }
    }

    // 约束 4：只有【原始目标相关分类全部遍历确认无匹配】且【衍生问题也无匹配】才判失败。
    if (pending.length > 0) {
      return {
        action: 'retry',
        fresh: [],
        allTags: [...pool],
        forced,
        round: rounds,
        reason: `本轮无新增，但原始目标还有 ${pending.length} 个分类没遍历过（${pending.slice(0, 4).join(',')}），不能判做不到`,
      }
    }
    return { action: 'give-up', fresh: [], allTags: [...pool], forced, round: rounds, reason: '原始目标相关分类已全部遍历 + 衍生问题也无匹配，才允许判定任务无法完成' }
  }

  function status() {
    return {
      task,
      taskTags: [...taskTags],
      rounds,
      maxRounds,
      pool: [...pool],
      pending: [...pending],
      history: history.map(h => ({ ...h })),
    }
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
