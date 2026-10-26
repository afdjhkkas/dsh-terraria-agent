import test from 'node:test'
import assert from 'node:assert/strict'
import { mkdtempSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

import { DemoLibrary } from '../src/demo-library.js'
import { BASE_TIMING, createAutoPlanner, defaultPlanner, formatTimingReport, resolveTiming } from '../src/observe-mode.js'
import {
  KB_SAFETY,
  MIN_HP_RATIO,
  RULES,
  checkAction,
  describeViolations,
  formatRuleSet,
  gatePlan,
  kbFor,
  loadRuleSet,
  ruleIds,
} from '../src/rule-gate.js'

const tmp = () => join(mkdtempSync(join(tmpdir(), 'dsh-gate-')), 'cases.jsonl')
const snap = () => ({ life: 100, lifeMax: 100, grounded: true, wet: false, zone: 'surface', dx: 3, dy: 0, hostileDist: 20, inWorld: true })
const lib = () => new DemoLibrary({ file: tmp() })

test('闸门每条规则都带知识库出处，没有出处的不许进表', () => {
  assert.ok(RULES.length >= 8)
  for (const rule of RULES) {
    assert.ok(rule.id && rule.reason && rule.kb, `规则 ${rule.id} 缺 id/reason/kb`)
    assert.equal(typeof rule.check, 'function')
    assert.match(rule.kb, /^[a-z_]+$/, 'kb 字段必须是知识库标签')
    assert.ok(rule.why && rule.why.length > 6, `规则 ${rule.id} 缺 why`)
  }
  assert.ok(ruleIds().includes('hp-floor'))
  assert.equal(kbFor('hp-floor'), KB_SAFETY)
})

test('按标签加载规则集，安全库永远在', () => {
  const set = loadRuleSet({ tags: ['combat_math'] })
  assert.ok(set.rules.some((r) => r.id === 'hp-floor'), '安全线属于 always 规则，任何任务都带上')
  assert.ok(set.sources.includes(KB_SAFETY))
  assert.match(formatRuleSet(set), /rules=\d+/)
  assert.match(formatRuleSet(set), /kb=safety_limit/)
  const empty = loadRuleSet()
  assert.ok(empty.rules.length >= 4, '至少要带上全部 always 规则')
})

test('checkAction：违规返回条目，不违规返回空', () => {
  const human = { kind: 'mine', digHoldMs: 90 }
  assert.equal(checkAction(human, { snap: snap() }).ok, true)
  const low = checkAction(human, { snap: { ...snap(), life: 20 } })
  assert.equal(low.ok, false)
  assert.equal(low.violations[0].reason, 'below-safety-line')
  assert.equal(low.violations[0].kb, KB_SAFETY)

  // 只有岩浆/微光/蜂蜜拦；普通水允许（真人教学：水里出路就是挖通道）。
  assert.equal(checkAction(human, { snap: { ...snap(), wet: true } }).ok, true, '水里挖不拦')
  const lava = checkAction(human, { snap: { ...snap(), liquidName: 'lava' } })
  assert.equal(lava.violations[0].reason, 'liquid-mining')

  const dead = checkAction({ kind: 'move' }, { snap: { ...snap(), dead: true } })
  assert.equal(dead.violations[0].reason, 'player-dead')

  const out = checkAction({ kind: 'move' }, { snap: { ...snap(), inWorld: false } })
  assert.equal(out.violations[0].reason, 'not-in-world')
})

test('竖直速度两种形状都认（velocity 数组与摊平的 vy）', () => {
  const move = { kind: 'move' }
  assert.equal(checkAction(move, { snap: { ...snap(), velocity: [0, 14], grounded: false } }).violations[0].reason, 'uncontrolled-fall')
  assert.equal(checkAction(move, { snap: { ...snap(), vy: 14, grounded: false } }).violations[0].reason, 'uncontrolled-fall')
  assert.equal(checkAction(move, { snap: { ...snap(), velocity: [0, 2], grounded: false } }).ok, true, '正常下落不算失控')
})

test('低血拦的是作业，不是逃生', () => {
  const low = { ...snap(), life: 10 }
  assert.equal(checkAction({ kind: 'mine' }, { snap: low }).ok, false)
  assert.equal(checkAction({ kind: 'attack' }, { snap: low }).ok, false)
  assert.equal(checkAction({ kind: 'move' }, { snap: low }).ok, true, '低血往回跑是正道')
  assert.equal(checkAction({ kind: 'jump' }, { snap: low }).ok, true)
})

test('安全比例可调，默认取知识库的 0.35', () => {
  assert.equal(MIN_HP_RATIO, 0.35)
  const s = { ...snap(), life: 34 }
  assert.equal(checkAction({ kind: 'mine' }, { snap: s }).ok, false, '34% 已跌破安全线')
  assert.equal(checkAction({ kind: 'mine' }, { snap: s, opts: { minHpRatio: 0.3 } }).ok, true, '显式放宽时按传入值判')
  assert.equal(checkAction({ kind: 'mine' }, { snap: { ...snap(), life: 40 } }).ok, true, '40% 还在线上，不该拦')
})

test('取巧命令一律拒绝', () => {
  const out = checkAction({ kind: 'command', cmd: 'teleport' }, { snap: snap() })
  assert.equal(out.ok, false)
  assert.equal(out.violations[0].reason, 'command-shortcut')
})

test('gatePlan 失败时 plan 为 null，且没有旁路参数', () => {
  const bad = gatePlan({ kind: 'mine', digHoldMs: 90 }, { snap: { ...snap(), life: 5 } })
  assert.equal(bad.ok, false)
  assert.equal(bad.plan, null)
  assert.match(bad.why, /below-safety-line|安全/)
  assert.match(describeViolations(bad.violations), /血量|below-safety-line/)

  const good = gatePlan({ kind: 'move', moveHoldMs: 600 }, { snap: snap() })
  assert.equal(good.ok, true)
  assert.deepEqual(good.plan, { kind: 'move', moveHoldMs: 600 })

  // 想绕过只能改规则本身：带上 force/override/bypass 也一样被拦。
  for (const bypass of [{ force: true }, { override: true }, { bypass: true, force: true }]) {
    const r = gatePlan({ kind: 'mine', digHoldMs: 90 }, { snap: { ...snap(), life: 5 }, ...bypass })
    assert.equal(r.ok, false, `不该有旁路参数 ${JSON.stringify(bypass)}`)
    assert.equal(r.plan, null)
  }
})

test('resolveTiming：同场景且几乎一样时，一条示范就生效', () => {
  const l = lib()
  l.record({ snap: snap(), human: { kind: 'move', moveHoldMs: 900 }, agent: { kind: 'move', moveHoldMs: 600 } })
  const r = resolveTiming({ snap: snap(), library: l })
  assert.equal(r.tier, 'exact')
  assert.equal(r.source, 'demo')
  assert.equal(r.timing.moveHoldMs, 900)
})

test('resolveTiming：只是相似时要三条样本才改，且夹在边界内', () => {
  const l = lib()
  const one = resolveTiming({ snap: snap(), library: l })
  assert.equal(one.tier, 'none')
  assert.equal(one.timing.moveHoldMs, BASE_TIMING.moveHoldMs)

  for (let i = 0; i < 3; i++) {
    l.record({ snap: snap(), human: { kind: 'move', moveHoldMs: 2000 }, agent: { kind: 'move', moveHoldMs: 600 } })
  }
  const loose = resolveTiming({ snap: snap(), library: l, opts: { exactSim: 1.01 } })
  assert.equal(loose.tier, 'loose')
  assert.equal(loose.timing.moveHoldMs, 1500, '示范按 2000ms 也只许到硬边界 1500')
  assert.equal(loose.clamped[0].param, 'moveHoldMs')
})

test('createAutoPlanner：推演出来后过闸门，违规就不出动作', () => {
  const plan = createAutoPlanner({ planner: () => ({ kind: 'mine', digHoldMs: 90 }) })
  const blocked = plan({ ...snap(), life: 8 })
  assert.equal(blocked.kind, 'hold')
  assert.equal(blocked.blocked, true)
  assert.ok(blocked.violations.length > 0)
  assert.equal(blocked.draft.kind, 'mine', '被拦下的草案要留证，方便事后看是规则还是示范的问题')

  const ok = plan(snap())
  assert.equal(ok.blocked, false)
  assert.equal(ok.kind, 'mine')
})

test('createAutoPlanner：示范只改时长，不改动作类型', () => {
  const l = lib()
  l.record({ snap: snap(), human: { kind: 'mine', digHoldMs: 260 }, agent: { kind: 'move', moveHoldMs: 600 } })
  const plan = createAutoPlanner({ planner: () => ({ kind: 'move', dir: 'right', ...BASE_TIMING }), library: l })
  const out = plan(snap())
  assert.equal(out.kind, 'move', '示范想换成挖也不行——动作语义以推演为准')
  assert.equal(out.dir, 'right')
  assert.equal(out.digHoldMs, 260, '示范学到的时长叠上来')
})

test('formatTimingReport 把依据条数、改了哪些参数、拦在哪条规则都写出来', () => {
  const l = lib()
  l.record({ snap: snap(), human: { kind: 'move', moveHoldMs: 900 }, agent: { kind: 'move', moveHoldMs: 600 } })
  const plan = createAutoPlanner({ planner: () => ({ kind: 'move', ...BASE_TIMING }), library: l })(snap())
  const text = formatTimingReport(plan)
  assert.match(text, /tier=exact/)
  assert.match(text, /source=demo/)
  assert.match(text, /moveHoldMs 600→900ms/)

  const blocked = createAutoPlanner({ planner: () => ({ kind: 'mine', digHoldMs: 90 }) })({ ...snap(), life: 8 })
  assert.match(formatTimingReport(blocked), /blocked:/)
})

test('defaultPlanner 仍然只看局面，不碰世界', () => {
  assert.equal(defaultPlanner({ ...snap(), hostileDist: 3 }).kind, 'attack')
  assert.equal(defaultPlanner({ ...snap(), dy: -3 }).kind, 'jump')
  assert.equal(defaultPlanner({ ...snap(), stuck: true }).kind, 'mine')
  assert.equal(defaultPlanner(snap()).kind, 'move')
})