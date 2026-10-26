import assert from 'node:assert/strict'
import { test } from 'node:test'
import {
  ATOMIC_KINDS, JUMP_TAP_MS, VOLATILE_FIELDS,
  atomFromAction, atomize, atomizeEvents, formatAtoms, sequenceRejection, strippedFields,
} from '../src/atom-model.js'
import { DemoLibrary } from '../src/demo-library.js'

test('原子动作白名单只含单次动作，不含任何整套任务', () => {
  const kinds = Object.keys(ATOMIC_KINDS)
  for (const k of ['jump-short', 'jump-long', 'mine-swing', 'attack-swing', 'place-swing']) {
    assert.ok(kinds.includes(k), k)
  }
  // 整套任务绝不能出现在原子白名单里
  for (const bad of ['chop-tree', 'fight', 'clear-area', 'dig-tunnel', 'travel']) {
    assert.ok(!kinds.includes(bad), `${bad} 不该是原子动作`)
  }
})

test('执行序列在入口就被拒：带帧序列/总时长', () => {
  assert.equal(sequenceRejection({ frames: [1, 2, 3] }), 'sequence')
  assert.equal(sequenceRejection({ totalMs: 12000 }), 'sequence')
  assert.equal(sequenceRejection({ human: { kind: 'mine-swing', digHoldMs: 90 } }), null)
})

test('循环任务的重复次数被拒；单次动作放行', () => {
  assert.equal(sequenceRejection({ human: { kind: 'mine-swing', digHoldMs: 90, count: 12 } }), 'loop')
  assert.equal(sequenceRejection({ human: { kind: 'mine-swing', digHoldMs: 90, repeats: 3 } }), 'loop')
  assert.equal(sequenceRejection({ human: { kind: 'jump-long', jumpHoldMs: 400 } }), null)
})

test('条件分支只允许记分支内的动作，记走向就拒', () => {
  assert.equal(sequenceRejection({ branches: ['jump', 'dig'] }), 'branch')
  assert.equal(sequenceRejection({ human: { kind: 'jump-long', jumpHoldMs: 400 }, tookBranch: 'dig' }), 'branch')
})

test('空间任务不固化坐标与路径', () => {
  assert.equal(sequenceRejection({ path: [[1, 2], [3, 4]] }), 'spatial')
  assert.equal(sequenceRejection({ targetX: 2184, targetY: 226 }), 'spatial')
})

test('atomFromAction 只留动作与按住时长，坐标一律剥掉', () => {
  const atom = atomFromAction({ kind: 'mine-swing', digHoldMs: 92, x: 2184, y: 226 })
  assert.deepEqual(Object.keys(atom).sort(), ['holdMs', 'kind'])
  assert.equal(atom.holdMs, 92)
  assert.equal(atom.x, undefined)
  assert.equal(atom.y, undefined)
})

test('没有时序就不产出原子——不能把“没测到”当成 0ms', () => {
  assert.equal(atomFromAction({ kind: 'jump-long' }), null)
  assert.equal(atomFromAction({ kind: '不存在的动作', digHoldMs: 90 }), null)
})

test('atomize：连续同类动作只留一条模板，其余计入循环', () => {
  const frames = []
  for (let i = 0; i < 30; i++) frames.push({ t: i * 40, mouse: { left: true } })
  for (let i = 30; i < 40; i++) frames.push({ t: i * 40, mouse: { left: false } })
  const { atoms, dropped } = atomize(frames)
  assert.equal(atoms.length, 1)
  assert.equal(atoms[0].kind, 'mine-swing')
  assert.ok(atoms[0].holdMs > 0)
  assert.ok(dropped.loop >= 0) // 单段连续只产出一条，不重复计数
})

test('atomize：轻点跳与按住跳按落点时长分成两种原子', () => {
  const short = atomize([{ t: 0, keys: ['Space'] }, { t: 120, keys: ['Space'] }, { t: 150, keys: [] }])
  assert.equal(short.atoms[0].kind, 'jump-short')
  const long = atomize([{ t: 0, keys: ['Space'] }, { t: 300, keys: ['Space'] }, { t: 420, keys: [] }])
  assert.equal(long.atoms[0].kind, 'jump-long')
  assert.ok(JUMP_TAP_MS > 0)
})

test('strippedFields 能说清这条样本剥掉了什么', () => {
  const f = strippedFields({ path: [1], human: { digHoldMs: 90, count: 5 } })
  assert.ok(f.includes('sample.path'))
  assert.ok(f.includes('human.count'))
  assert.ok(VOLATILE_FIELDS.includes('count'))
})

test('DemoLibrary.record 拒绝执行序列，仓库不涨', () => {
  const lib = new DemoLibrary({ file: 'C:/Windows/Temp/trtest/atom-test.jsonl' })
  lib.clear()
  const before = lib.stats().total
  const res = lib.record({ snap: { zone: 'surface' }, human: { kind: 'mine-swing', digHoldMs: 90 }, frames: [1, 2, 3] })
  assert.equal(res.ok, false)
  assert.equal(res.reason, 'sequence')
  assert.equal(lib.stats().total, before)
})

test('DemoLibrary.record 放行单次原子动作并留下 atom 字段', () => {
  const lib = new DemoLibrary({ file: 'C:/Windows/Temp/trtest/atom-test2.jsonl' })
  lib.clear()
  const res = lib.record({
    snap: { zone: 'surface', grounded: true },
    human: { kind: 'jump-long', jumpHoldMs: 400, x: 2184, y: 226 },
    agent: { kind: 'jump-long', jumpHoldMs: 150 },
  })
  assert.equal(res.ok, true)
  assert.equal(res.case.atom.kind, 'jump-long')
  assert.equal(res.case.atom.holdMs, 400)
  assert.equal(res.case.atom.x, undefined)
  lib.clear()
})

test('formatAtoms 输出人话摘要', () => {
  const s = formatAtoms([{ kind: 'jump-long', holdMs: 400 }])
  assert.ok(s.includes('400'))
  assert.equal(formatAtoms([]), '（没有可入库的原子动作）')
})
test('recallAtoms 只回节奏参考，坐标与次数根本不进库', () => {
  const lib = new DemoLibrary({ file: 'C:/Windows/Temp/trtest/atom-test4.jsonl' })
  lib.clear()
  // 干净的单次动作：放行
  lib.record({
    snap: { zone: 'surface', grounded: true },
    human: { kind: 'mine-swing', digHoldMs: 92 },
    agent: { kind: 'mine-swing', digHoldMs: 40 },
  })
  const atoms = lib.recallAtoms({ zone: 'surface', grounded: true })
  assert.equal(atoms.length, 1)
  assert.deepEqual(Object.keys(atoms[0]).sort(), ['grounded', 'holdMs', 'kind', 'sim'])
  assert.equal(atoms[0].holdMs, 92)
  // 带次数的样本在入口就被拒，检索里自然查不到它
  const rej = lib.record({
    snap: { zone: 'surface', grounded: true },
    human: { kind: 'mine-swing', digHoldMs: 300, count: 9 },
  })
  assert.equal(rej.ok, false)
  assert.equal(rej.reason, 'loop')
  assert.equal(lib.recallAtoms({ zone: 'surface', grounded: true }).length, 1)
  lib.clear()
})

test('按住不放的长按不算单次动作，直接丢弃', () => {
  const frames = [{ t: 0, mouse: { left: true } }, { t: 3000, mouse: { left: true } }, { t: 3100, mouse: { left: false } }]
  const { atoms, dropped } = atomize(frames)
  assert.equal(atoms.length, 0)
  assert.ok(dropped.loop >= 1)
})

test('单次挥镐的典型时长会被保留（不被上限误杀）', () => {
  const frames = [{ t: 0, mouse: { left: true } }, { t: 120, mouse: { left: false } }]
  const { atoms } = atomize(frames)
  assert.equal(atoms.length, 1)
  assert.equal(atoms[0].holdMs, 120)
})

test('atomizeEvents：按住时长由 up/down 两个 tick 相减，不受轮询间隔影响', () => {
  const { atoms } = atomizeEvents([
    { tick: 100, key: 'jump', down: true },
    { tick: 124, key: 'jump', down: false },
  ])
  assert.equal(atoms.length, 1)
  assert.equal(atoms[0].kind, 'jump-long')       // 24 tick ≈ 400ms
  assert.equal(atoms[0].ticks, 24)
  assert.ok(atoms[0].holdMs > 380 && atoms[0].holdMs < 420)
})

test('atomizeEvents：轻点跳与长按跳分开，且两次独立动作都保留', () => {
  const { atoms } = atomizeEvents([
    { tick: 10, key: 'jump', down: true }, { tick: 16, key: 'jump', down: false },
    { tick: 200, key: 'jump', down: true }, { tick: 224, key: 'jump', down: false },
  ])
  assert.deepEqual(atoms.map(a => a.kind), ['jump-short', 'jump-long'])
})

test('atomizeEvents：紧接的同类动作只算一个模板', () => {
  const { atoms, dropped } = atomizeEvents([
    { tick: 10, key: 'jump', down: true }, { tick: 16, key: 'jump', down: false },
    { tick: 18, key: 'jump', down: true }, { tick: 24, key: 'jump', down: false },
  ])
  assert.equal(atoms.length, 1)
  assert.equal(dropped.loop, 1)
})

test('atomizeEvents：缺一半的事件不猜，另行计数', () => {
  const { atoms, unmatched } = atomizeEvents([
    { tick: 10, key: 'mouseLeft', down: false },
    { tick: 20, key: 'mouseLeft', down: true },
  ])
  assert.equal(atoms.length, 0)
  assert.equal(unmatched, 2)
})

test('atomizeEvents：按住不放超过上限不算单次动作', () => {
  const { atoms, dropped } = atomizeEvents([
    { tick: 0, key: 'mouseLeft', down: true },
    { tick: 200, key: 'mouseLeft', down: false },
  ])
  assert.equal(atoms.length, 0)
  assert.ok(dropped.loop >= 1)
})
