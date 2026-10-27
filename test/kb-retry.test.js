import test from 'node:test'
import assert from 'node:assert/strict'
import {
  ALL_CATEGORIES,
  BLOCK_SIGNALS,
  SIGNAL_CATEGORIES,
  createRetryGate,
  formatRetryVerdict,
} from '../src/kb-retry.js'

const fakeRoute = () => (['game_rule'])
const existsAll = () => true

test('begin 只做一次前置检索，返回的就是那批 tag', () => {
  const g = createRetryGate({ route: fakeRoute, exists: existsAll })
  const r = g.begin('盖一间 npc 房子')
  assert.deepEqual(r.tags, ['game_rule'])
  assert.deepEqual(r.fresh, [])
  assert.equal(g.status().rounds, 0)
})

test('非卡点信号不触发重试（约束 4：不做无脑反复检索）', () => {
  const g = createRetryGate({ route: fakeRoute, exists: existsAll })
  g.begin('随便走走')
  for (const sig of ['bored', '', 'ok', undefined]) {
    const r = g.onBlockage({ signal: sig, detail: 'x' })
    assert.equal(r.action, 'skip', `${sig} 不该触发`)
  }
  assert.equal(g.status().rounds, 0)
})

test('no-path 会强制复核 navigation / world_biome / build_standard（约束 2）', () => {
  const g = createRetryGate({ route: () => [], exists: existsAll })
  g.begin('走过去')
  const r = g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: '2173→2215 无路' })
  assert.equal(r.action, 'retry')
  assert.deepEqual(r.forced, SIGNAL_CATEGORIES[BLOCK_SIGNALS.noPath])
  for (const t of SIGNAL_CATEGORIES[BLOCK_SIGNALS.noPath]) {
    assert.ok(r.allTags.includes(t), `强制复核后应包含 ${t}`)
  }
})

test('命中新知识 → retry；全套复核后仍无新增 → give-up（约束 3）', () => {
  // sweepAll:false 关掉"补查原始目标欠查分类"，专门测强制复核本身
  const g = createRetryGate({ route: () => ['game_rule'], exists: existsAll, sweepAll: false })
  g.begin('挖点铁')
  const r1 = g.onBlockage({ signal: BLOCK_SIGNALS.actionFailed, detail: 'place 无支撑' })
  assert.equal(r1.action, 'retry')
  assert.deepEqual(r1.fresh.sort(), ['atomic_action', 'item_operation'])

  // 同一个闸门里再卡一次、且路口只回已见过的：此时 forced 那两类也见过了 → give-up
  const r2 = g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: '另一处' })
  assert.equal(r2.action, 'retry') // navigation / world_biome / build_standard 仍是新的
  const r3 = g.onBlockage({ signal: BLOCK_SIGNALS.safetyViolation, detail: 'hp 太低' })
  assert.equal(r3.action, 'retry') // safety_limit / combat_math 仍是新的
  // targetUnreachable 的两个强制分类（navigation / item_operation）都已经见过 → 无新增才 give-up
  const r4 = g.onBlockage({ signal: BLOCK_SIGNALS.targetUnreachable, detail: '2210 站不住' })
  assert.equal(r4.action, 'give-up')
  assert.match(r4.reason, /全部遍历|仍无新增/)
  assert.equal(g.status().rounds, 4)
})

test('同一 signal + 同一 detail 不重复烧（防空转）', () => {
  const g = createRetryGate({ route: () => [], exists: existsAll })
  g.begin('走')
  const a = g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: '同一个点' })
  assert.equal(a.action, 'retry')
  const b = g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: '同一个点' })
  // 重复触发是 skip 不是 give-up：同一卡点重复只说明不必重查，不等于"知识不足"
  assert.equal(b.action, 'skip')
  assert.match(b.reason, /同一卡点/)
})

test('轮数上限到了就 give-up，不会无限重试', () => {
  const g = createRetryGate({ route: () => [], maxRounds: 2, exists: existsAll })
  g.begin('走')
  assert.equal(g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: 'a' }).action, 'retry')
  assert.equal(g.onBlockage({ signal: BLOCK_SIGNALS.actionFailed, detail: 'b' }).action, 'retry')
  const r = g.onBlockage({ signal: BLOCK_SIGNALS.safetyViolation, detail: 'c' })
  assert.equal(r.action, 'give-up')
  assert.match(r.reason, /上限/)
})

test('库文件不存在时不冒充命中', () => {
  const g = createRetryGate({ route: () => [], exists: t => t === 'navigation' })
  g.begin('走')
  const r = g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: 'x' })
  assert.deepEqual(r.fresh, ['navigation'])
  assert.deepEqual(r.forced, ['navigation', 'world_biome', 'build_standard'])
})

test('SIGNAL_CATEGORIES 覆盖全部信号且只引用真实分类', () => {
  for (const sig of Object.values(BLOCK_SIGNALS)) {
    assert.ok(SIGNAL_CATEGORIES[sig]?.length, `${sig} 没有强制分类`)
    for (const t of SIGNAL_CATEGORIES[sig]) {
      assert.ok(ALL_CATEGORIES.includes(t), `${sig} 引用了不存在的分类 ${t}`)
    }
  }
})

test('formatRetryVerdict 说清轮次与新增', () => {
  const g = createRetryGate({ route: () => [], exists: existsAll })
  g.begin('走')
  const r = g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: 'x' })
  const s = formatRetryVerdict(r)
  assert.match(s, /第1轮/)
  assert.match(s, /新增：navigation/)
  assert.equal(formatRetryVerdict(null), '（无检索结果）')
})
// ============================================================================
// 用户 m20002 的四条约束：检索上下文不能割裂
// ============================================================================

test('约束1：原始目标分类在整个任务里保留，不被衍生问题覆盖', () => {
  const g = createRetryGate({ route: () => ['game_rule', 'npc_town'], exists: existsAll })
  g.begin('盖一间 NPC 房子')
  assert.deepEqual(g.status().taskTags, ['game_rule', 'npc_town'])
  // 连卡三次，衍生问题全是"走路走不过去"，原始目标标签不能被顶掉
  g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: '门口过不去' })
  g.onBlockage({ signal: BLOCK_SIGNALS.actionFailed, detail: '工作台放不下' })
  g.onBlockage({ signal: BLOCK_SIGNALS.targetUnreachable, detail: '目标格站不住' })
  assert.deepEqual(g.status().taskTags, ['game_rule', 'npc_town'])
  assert.equal(g.status().task, '盖一间 NPC 房子')
})

test('约束2：重检索输入 = 原始目标 + 衍生问题，两部分都参与遍历', () => {
  const g = createRetryGate({ route: () => ['npc_town'], exists: existsAll })
  g.begin('盖一间 NPC 房子')
  const r = g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: '门口过不去' })
  // npc_town 来自原始目标；navigation/world_biome/build_standard 来自衍生问题
  for (const t of ['npc_town', ...SIGNAL_CATEGORIES[BLOCK_SIGNALS.noPath]]) {
    assert.ok(r.allTags.includes(t), `合并知识池里应有 ${t}`)
  }
  const last = g.status().history.at(-1)
  assert.ok(last.candidates.includes('npc_town'), '候选分类必须显式带上原始目标标签')
})

test('约束3：知识池只累加不替换', () => {
  const g = createRetryGate({ route: () => ['npc_town'], exists: existsAll })
  g.begin('盖一间 NPC 房子')
  const r1 = g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: 'a' })
  const after1 = [...r1.allTags]
  const r2 = g.onBlockage({ signal: BLOCK_SIGNALS.actionFailed, detail: 'b' })
  for (const t of after1) assert.ok(r2.allTags.includes(t), `第一轮的 ${t} 不该丢`)
  assert.ok(r2.allTags.length > after1.length, '第二轮应把新知识并进来')
})

test('约束4：原始目标分类没遍历完之前不许判做不到', () => {
  // exists 全 false：保证本轮一个都不“命中”，专门测 pending 未清空时的判定
  const g = createRetryGate({ route: () => ([]), exists: () => false })
  g.begin('盖一间 NPC 房子')
  const r = g.onBlockage({ signal: BLOCK_SIGNALS.noPath, detail: 'x' })
  assert.equal(r.action, 'retry')
  assert.equal(r.fresh.length, 0, '本轮确实没有新命中')
  assert.match(r.reason, /没遍历过/)
  assert.ok(g.status().pending.length > 0)
})

test('约束4：原始目标分类全部遍历 + 衍生问题无匹配 → 才 give-up', () => {
  const g = createRetryGate({ route: () => ([]), exists: existsAll, maxRounds: 20 })
  g.begin('盖一间 NPC 房子')
  let last = null
  const signals = ['no-path', 'action-failed', 'safety-violation', 'target-unreachable']
  for (let i = 0; i < 20; i++) {
    last = g.onBlockage({ signal: signals[i % 4], detail: 's' + i })
    if (last.action === 'give-up') break
  }
  assert.equal(last.action, 'give-up')
  assert.equal(g.status().pending.length, 0, '判做不到时 pending 必须已经清空')
  assert.match(last.reason, /全部遍历/)
})

test('sweepAll:false 时不补查原始目标欠查分类（旧行为可回退）', () => {
  const g = createRetryGate({ route: () => ([]), exists: existsAll, sweepAll: false })
  const r0 = g.begin('随便')
  assert.deepEqual(r0.pending, [])
  const r = g.onBlockage({ signal: BLOCK_SIGNALS.actionFailed, detail: 'x' })
  assert.deepEqual(r.fresh.sort(), ['atomic_action', 'item_operation'])
  const r2 = g.onBlockage({ signal: BLOCK_SIGNALS.actionFailed, detail: 'y' })
  assert.equal(r2.action, 'give-up')
})
