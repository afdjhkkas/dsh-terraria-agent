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
  // 第一次 route 给 game_rule（begin 已见过），卡点后仍只给 game_rule → 但 forced 会补新的
  const g = createRetryGate({ route: () => ['game_rule'], exists: existsAll })
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
  assert.match(r4.reason, /仍无新增/)
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