import test from 'node:test'
import assert from 'node:assert/strict'
import { mkdtempSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

import { DemoLibrary } from '../src/demo-library.js'
import {
  MODES,
  ModeLockedError,
  classifyFrame,
  createGuard,
  createModeStore,
  createObserver,
  defaultPlanner,
  formatObserveReport,
  framesToAction,
} from '../src/observe-mode.js'

const tmpFile = () => join(mkdtempSync(join(tmpdir(), 'dsh-obs-')), 'mode.json')
const snap = () => ({ life: 100, lifeMax: 100, grounded: true, wet: false, zone: 'surface', dx: 3, dy: 0, hostileDist: 20, inWorld: true })

const walkInput = () => ({ control: { right: true }, mouse: {}, player: { selected: 0, heldType: 1 } })
const jumpInput = () => ({ control: { jump: true }, mouse: {}, player: { selected: 0, heldType: 1 } })
const mineInput = () => ({ control: { useItem: true }, mouse: { left: true, tileX: 2140, tileY: 300 }, player: { selected: 2, heldType: 4 } })

test('classifyFrame：攻击键优先于跳跃/移动，且左右互斥取占优', () => {
  assert.equal(classifyFrame(mineInput()), 'use')
  assert.equal(classifyFrame(jumpInput()), 'jump')
  assert.equal(classifyFrame(walkInput()), 'move')
  assert.equal(classifyFrame({ control: {} }), 'idle')
  assert.equal(classifyFrame({ mouse: { left: true } }), 'use', '只按鼠标左键也算使用')
})

test('framesToAction：时长按帧数×间隔累加，方向取占优一侧', () => {
  const frames = [
    { input: walkInput() },
    { input: walkInput() },
    { input: jumpInput() },
  ]
  const a = framesToAction(frames, 50)
  assert.equal(a.frames, 3)
  assert.equal(a.kind, 'move')
  assert.equal(a.jumpHoldMs, 50)
  assert.equal(a.moveHoldMs, 100)
  assert.equal(a.dir, 'right')
  assert.equal(framesToAction([], 50), null)
})

test('framesToAction：挖矿段带出瞄准格与手持槽', () => {
  const a = framesToAction([{ input: mineInput() }, { input: mineInput() }], 50)
  assert.equal(a.kind, 'use')
  assert.equal(a.digHoldMs, 100)
  assert.deepEqual(a.aim, { tileX: 2140, tileY: 300 })
  assert.equal(a.selected, 2)
})

test('mode store 落盘并跨实例生效', () => {
  const file = tmpFile()
  const s1 = createModeStore({ file })
  assert.equal(s1.get(), MODES.auto, '默认 auto，不改变既有行为')
  s1.set(MODES.observe)
  assert.equal(createModeStore({ file }).get(), MODES.observe, '重启后仍是观摩模式')
  assert.throws(() => s1.set('whatever'), /未知模式/)
})

test('guard：观摩期写命令直接抛错，读命令放行；auto 期全部放行', async () => {
  const sent = []
  const dispatch = async (req) => { sent.push(req.cmd); return { ok: true } }
  let mode = MODES.observe
  const guarded = createGuard(dispatch, { getMode: () => mode })

  for (const cmd of ['state', 'humaninput', 'tile', 'inventory', 'travelstatus']) {
    await guarded({ cmd })
  }
  await assert.rejects(() => guarded({ cmd: 'use' }), (e) => e instanceof ModeLockedError && e.code === 'MODE_OBSERVE_LOCKED')
  await assert.rejects(() => guarded({ cmd: 'craft' }), ModeLockedError)
  await assert.rejects(() => guarded({ cmd: 'save' }), ModeLockedError)
  assert.deepEqual(sent, ['state', 'humaninput', 'tile', 'inventory', 'travelstatus'], '观摩期一个写命令都不许出去')

  mode = MODES.auto
  await guarded({ cmd: 'use' })
  assert.equal(sent.at(-1), 'use')
})

test('observer：人在走而脚本推演也在走 → 不算修正，不入库', () => {
  const lib = new DemoLibrary({ file: join(mkdtempSync(join(tmpdir(), 'dsh-obs-')), 'c.jsonl') })
  const obs = createObserver({ read: async () => null, library: lib, everyMs: 50 })
  // 12 帧 × 50ms = 600ms，与默认推演的 moveHoldMs 一致，属于"人和脚本判断相同"
  for (let i = 0; i < 12; i++) obs.tick({ snap: snap(), input: walkInput() })
  const out = obs.flush('test')
  assert.equal(lib.stats().total, 0, '一致的走法不该进库')
  assert.equal(out.recorded, false)
})

test('observer：人跳得比脚本久 → 记录时序修正', () => {
  const lib = new DemoLibrary({ file: join(mkdtempSync(join(tmpdir(), 'dsh-obs-')), 'c.jsonl') })
  const obs = createObserver({
    read: async () => null,
    library: lib,
    planner: () => ({ kind: 'jump', jumpHoldMs: 120 }),
    everyMs: 50,
  })
  for (let i = 0; i < 6; i++) obs.tick({ snap: snap(), input: jumpInput() })
  const out = obs.flush('test')
  assert.equal(out.recorded, true)
  assert.equal(lib.stats().total, 1)
  const report = formatObserveReport({ stats: obs.stats(), corrections: obs.corrections() })
  assert.match(report, /时序修正/)
  assert.match(report, /jumpHoldMs 120ms→300ms/)
})

test('observer：动作类型不同时报"动作不同"，日志可读', () => {
  const lib = new DemoLibrary({ file: join(mkdtempSync(join(tmpdir(), 'dsh-obs-')), 'c.jsonl') })
  const obs = createObserver({ read: async () => null, library: lib, planner: () => ({ kind: 'move', dir: 'right' }), everyMs: 50 })
  for (let i = 0; i < 3; i++) obs.tick({ snap: snap(), input: mineInput() })
  obs.flush('test')
  const report = formatObserveReport({ stats: obs.stats(), corrections: obs.corrections() })
  assert.match(report, /动作不同：脚本 move → 人手 use/)
  assert.match(report, /frames=3/)
})

test('observer：掉血到安全线以下 / 在液体里的样本被丢弃', () => {
  const lib = new DemoLibrary({ file: join(mkdtempSync(join(tmpdir(), 'dsh-obs-')), 'c.jsonl') })
  const obs = createObserver({ read: async () => null, library: lib, planner: () => ({ kind: 'move', moveHoldMs: 100 }), everyMs: 50 })
  for (let i = 0; i < 8; i++) obs.tick({ snap: { ...snap(), life: 15 }, input: walkInput() })
  const out = obs.flush('test')
  assert.equal(out.recorded, false)
  assert.equal(out.reason, 'below-safety-line')
  assert.equal(lib.stats().total, 0)
  assert.match(formatObserveReport({ stats: obs.stats(), corrections: [] }), /discarded=1/)
})

test('observer.run 用注入的 read/sleep 跑完并报告样本数', async () => {
  const lib = new DemoLibrary({ file: join(mkdtempSync(join(tmpdir(), 'dsh-obs-')), 'c.jsonl') })
  let n = 0
  const inputs = [jumpInput(), jumpInput(), jumpInput(), walkInput(), walkInput()]
  const obs = createObserver({
    read: async () => ({ snap: snap(), input: inputs[Math.min(n++, inputs.length - 1)] }),
    library: lib,
    planner: () => ({ kind: 'jump', jumpHoldMs: 100 }),
    everyMs: 20,
  })
  const res = await obs.run({ ms: 1000, sleep: async () => {} })
  assert.ok(res.frames >= 5, `应采到至少 5 帧，实际 ${res.frames}`)
  assert.ok(res.samples >= 1, '至少切出 1 段')
  assert.equal(typeof res.recorded, 'number')
})

test('observer：未进世界不采样', () => {
  const lib = new DemoLibrary({ file: join(mkdtempSync(join(tmpdir(), 'dsh-obs-')), 'c.jsonl') })
  const obs = createObserver({ read: async () => null, library: lib })
  const out = obs.tick({ snap: { ...snap(), inWorld: false }, input: walkInput() })
  assert.equal(out.flushed, null)
  assert.equal(obs.stats().frames, 0)
})

test('defaultPlanner 是纯函数：威胁近→攻击，上坡→跳，卡住→挖', () => {
  assert.equal(defaultPlanner({ hostileDist: 3 }).kind, 'attack')
  assert.equal(defaultPlanner({ hostileDist: 30, dy: -3 }).kind, 'jump')
  assert.equal(defaultPlanner({ hostileDist: 30, stuck: true }).kind, 'mine')
  assert.equal(defaultPlanner({ hostileDist: 30 }).kind, 'move')
  const frozen = Object.freeze({ hostileDist: 30 })
  assert.doesNotThrow(() => defaultPlanner(frozen), '推演不能改输入对象')
})