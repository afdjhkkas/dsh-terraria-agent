import test from 'node:test'
import assert from 'node:assert/strict'
import {
  runMineLoop, planJump, runJumpHold, isSustained,
  MAX_SUSTAINED_ATTEMPTS, JUMP_REACH_TILES, ACTION_CLASS,
} from '../src/action-loop.js'

const noSleep = async () => {}

test('挖到消失就停，不会把 20 次都挥完', async () => {
  let swings = 0
  const world = { type: 1, hasTile: true }
  const r = await runMineLoop({
    read: async () => ({ ...world }),
    swing: async () => { swings += 1; if (swings === 3) world.hasTile = false },
    sleep: noSleep,
  })
  assert.equal(r.ok, true)
  assert.equal(r.attempts, 3)
  assert.equal(swings, 3)
})

test('本来就是空的，一次都不挥', async () => {
  let swings = 0
  const r = await runMineLoop({ read: async () => ({ type: -1, hasTile: false }), swing: async () => { swings += 1 }, sleep: noSleep })
  assert.equal(r.ok, true)
  assert.equal(r.already, true)
  assert.equal(swings, 0)
})

test('20 次还挖不动 → 判异常并带上报信息', async () => {
  let swings = 0
  const r = await runMineLoop({ read: async () => ({ type: 1, hasTile: true }), swing: async () => { swings += 1 }, sleep: noSleep })
  assert.equal(r.ok, false)
  assert.equal(swings, MAX_SUSTAINED_ATTEMPTS)
  assert.equal(r.report.kind, 'sustained_action_gave_up')
  assert.match(r.reason, /20 次/)
})

test('每轮攻击后都回读一次，回调能看见次数', async () => {
  const seen = []
  const world = { type: 1, hasTile: true }
  await runMineLoop({
    read: async () => ({ ...world }),
    swing: async () => { if (seen.length >= 1) world.hasTile = false },
    sleep: noSleep,
    onAttempt: i => seen.push(i.attempt),
  })
  assert.deepEqual(seen, [1, 2])
})

test('跳跃模式由高差自动决定：平地短按、小台阶短按、高差大长按', () => {
  assert.equal(planJump({ dy: 0 }).mode, 'tap')
  assert.equal(planJump({ dy: 1 }).mode, 'tap')
  const high = planJump({ dy: 5 })
  assert.equal(high.mode, 'hold')
  assert.equal(high.hold, true)
  assert.ok(high.ms > 200 && high.ms <= 900)
  assert.match(high.reason, new RegExp(String(JUMP_REACH_TILES)))
})

test('长按跳到目标高度就松手', async () => {
  let releases = 0
  let y = 300
  const r = await runJumpHold({
    read: async () => ({ y, grounded: false }),
    press: async () => { y -= 1 },
    release: async () => { releases += 1 },
    targetY: 297,
    sleep: noSleep,
  })
  assert.equal(r.ok, true)
  assert.equal(r.outcome, 'reached')
  assert.equal(releases, 1)
  assert.ok(r.climbed >= 3)
})

test('血空了立刻松手（危险判定优先）', async () => {
  let releases = 0
  const r = await runJumpHold({
    read: async () => ({ y: 300, grounded: false, hp: 0 }),
    press: async () => {},
    release: async () => { releases += 1 },
    targetY: 200,
    sleep: noSleep,
  })
  assert.equal(r.outcome, 'danger')
  assert.equal(releases, 1)
})

test('落地就松手，不会一直按着', async () => {
  let releases = 0
  let n = 0
  const r = await runJumpHold({
    read: async () => { n += 1; return { y: 300, grounded: n > 2 } },
    press: async () => {},
    release: async () => { releases += 1 },
    targetY: 290,
    sleep: noSleep,
  })
  assert.equal(r.outcome, 'landed')
  assert.equal(releases, 1)
})

test('动作分类：挖/砍/长按跳是持续型，点击是瞬时型', () => {
  assert.equal(ACTION_CLASS.sustained, '长按持续')
  for (const k of ['mine', 'chop', 'jump_hold', 'climb']) assert.equal(isSustained(k), true, k)
  for (const k of ['select', 'chat', 'tile', 'instant_click']) assert.equal(isSustained(k), false, k)
})
