import test from 'node:test'
import assert from 'node:assert/strict'

import {
  classify, decide, createTaskState, enterEmergency, resolveEmergency,
  createLog, tick, observe, applyPlan, EXCEPTION_KINDS, RETRY_BUDGET, DEFAULTS,
  loadExceptionPrompt, EXCEPTION_POLICY_PATH,
} from '../src/exception-policy.js'

const snap = (over = {}) => ({
  player: { tile: [100, 50], life: 100, lifeMax: 100, ...(over.player || {}) },
  hostiles: over.hostiles || [],
  env: over.env || {},
  inventory: over.inventory || { free: 10, missing: [] },
  stuck: over.stuck || {},
})

const kinds = hits => hits.map(h => h.kind)

test('classify：血量分档（45% 提醒 / 25% 危险线）', () => {
  assert.deepEqual(kinds(classify(snap({ player: { life: 100, lifeMax: 100 } }))), [])
  assert.ok(kinds(classify(snap({ player: { life: 40, lifeMax: 100 } }))).includes('hp_low'))
  const panic = classify(snap({ player: { life: 20, lifeMax: 100 } }))
  assert.equal(panic.find(h => h.kind === 'hp_low').ratio, 0.2)
})

test('classify：敌对生物按距离入列，超出警戒半径不算', () => {
  assert.ok(kinds(classify(snap({ hostiles: [{ name: '僵尸', dist: 5 }] }))).includes('hostile_near'))
  assert.deepEqual(kinds(classify(snap({ hostiles: [{ name: '僵尸', dist: 40 }] }))), [])
})

test('classify：连续 3 次坐标不变才算卡死，1 次只是被挡', () => {
  assert.ok(kinds(classify(snap({ stuck: { blocked: true, blockedBy: '石头' } }))).includes('terrain_blocked'))
  const stuck = classify(snap({ stuck: { sameCount: 3 } }))
  assert.ok(kinds(stuck).includes('terrain_stuck'))
  assert.ok(!kinds(stuck).includes('terrain_blocked'), '卡死时不重复报阻挡')
})

test('classify：致命环境三种来源都算 critical', () => {
  for (const env of [
    { onFatal: 'lava' },
    { fatalAhead: { dist: 1, what: 'lava' } },
    { dropAhead: 12 },
    { deepWaterAhead: { dist: 2 } },
  ]) {
    const sev = classify(snap({ env })).find(h => h.severity === 'critical' || h.severity === 'high')
    assert.ok(sev, JSON.stringify(env))
  }
})

test('classify：道具缺失与背包满分开报', () => {
  assert.ok(kinds(classify(snap({ inventory: { free: 5, missing: ['钓竿'] } }))).includes('missing_item'))
  assert.ok(kinds(classify(snap({ inventory: { free: 0, missing: [] } }))).includes('inventory_full'))
})

test('decide：致命环境立刻停步 + 后退 + 探测，且要求回主线', () => {
  const st = createTaskState('挖到铁矿', '挖第 3 格')
  const hits = classify(snap({ env: { onFatal: 'lava' } }))
  const d = decide(hits, st)
  assert.deepEqual(d.plan.map(p => p.action), ['move', 'move', 'probe'])
  assert.equal(d.plan[0].dir, 'stop')
  assert.equal(d.resume, true)
  assert.equal(st.goal, '挖到铁矿')
  assert.equal(st.step, '挖第 3 格')
})

test('decide：血量低 → 后撤喝药；危险线以下额外只逃不打', () => {
  const st = createTaskState('回家')
  const d = decide(classify(snap({ player: { life: 30, lifeMax: 100 } })), st)
  assert.ok(d.plan.some(p => p.action === 'heal'))
  assert.ok(!d.plan.some(p => p.action === 'retreat'))

  const st2 = createTaskState('回家')
  const d2 = decide(classify(snap({ player: { life: 15, lifeMax: 100 } })), st2)
  assert.ok(d2.plan.some(p => p.action === 'retreat'))
})

test('decide：敌对生物先避险，预算用尽才反击，且反击后仍回主线', () => {
  const st = createTaskState('走到矿区')
  const s = snap({ hostiles: [{ name: '僵尸', dist: 5 }] })
  for (let i = 0; i < RETRY_BUDGET.evade; i++) {
    const d = decide(classify(s), st)
    assert.ok(d.plan.some(p => p.action === 'move' && p.dir === 'away_from_hostile'), `第 ${i + 1} 次应先后撤`)
  }
  const d = decide(classify(s), st)
  assert.ok(d.plan.some(p => p.action === 'attack'), '预算用尽后反击')
  assert.ok(d.plan.some(p => p.action === 'check_threat'), '打完确认威胁是否清掉')
  assert.equal(d.resume, true)
})

test('decide：地形阻挡 跳2次 → 绕行3次 → 判失败', () => {
  const st = createTaskState('向东走 60 格')
  const s = snap({ stuck: { blocked: true } })
  const seen = []
  for (let i = 0; i < 12; i++) {
    const d = decide(classify(s), st)
    seen.push(d.plan.map(p => p.action).join(','))
    if (d.fatal) { assert.ok(d.plan.some(p => p.action === 'fail')); break }
  }
  assert.equal(seen.filter(x => x.includes('jump')).length, RETRY_BUDGET.jump)
  assert.equal(seen.filter(x => x.includes('reroute')).length, RETRY_BUDGET.reroute)
  assert.ok(seen.at(-1).includes('fail'))
})

test('decide：卡死重规划 3 次后失败，不回主线', () => {
  const st = createTaskState('探索洞穴')
  const s = snap({ stuck: { sameCount: 5 } })
  let last
  for (let i = 0; i < 6; i++) last = decide(classify(s), st)
  assert.ok(last.plan.some(p => p.action === 'fail'))
  assert.equal(last.resume, false)
  assert.equal(last.fatal, true)
})

test('主目标永不被应急覆盖；应急解除后 step 原样回来', () => {
  const st = createTaskState('建一间护士房', '放第 4 个火把')
  enterEmergency(st, 'hostile_near', '僵尸 5 格')
  assert.equal(st.goal, '建一间护士房')
  assert.equal(st.step, '放第 4 个火把', '进入应急不能动 step')
  assert.equal(st.emergency.kind, 'hostile_near')
  const r = resolveEmergency(st)
  assert.equal(r.resumeStep, '放第 4 个火把')
  assert.equal(st.emergency, null)
})

test('未知异常类型直接拒绝，不许静默吞掉', () => {
  const st = createTaskState('x')
  assert.throws(() => enterEmergency(st, 'aliens'), /未知异常类型/)
})

test('日志：每次触发都留证据，导出 markdown 六列齐全', () => {
  const log = createLog()
  log.add('terrain_stuck', '连续 3 次坐标不变', 'replan', 'ok', 1)
  log.add('hostile_near', '僵尸 5 格', 'move', 'ok', 2)
  const md = log.markdown()
  assert.match(md, /\| # \| 异常 \| 证据 \| 动作 \| 结果 \| 第几次 \|/)
  assert.equal(log.entries.length, 2)
  assert.match(md, /terrain_stuck/)
  log.reset()
  assert.equal(log.entries.length, 0)
})

test('tick：无异常时返回 continue；有致命异常时计入失败且不回主线', async () => {
  const st = createTaskState('钓鱼')
  const ok = await tick({ snap: snap(), state: st })
  assert.ok(ok.plan.some(p => p.action === 'continue'))
  assert.equal(ok.resumed, true)

  const st2 = createTaskState('钓鱼')
  let bad
  for (let i = 0; i < 4; i++) bad = await tick({ snap: snap({ stuck: { sameCount: 9 } }), state: st2 })
  assert.equal(bad.fatal, true, '重规划 3 次用尽后判失败')
  assert.equal(st2.failures, 1)
})

test('applyPlan：只发控制角色的命令，未知动作不报错', async () => {
  const calls = []
  const request = async (cmd, params) => { calls.push([cmd, params]); return {} }
  const log = createLog()
  await applyPlan([
    { action: 'move', dir: 'stop' },
    { action: 'jump', ms: 160 },
    { action: 'report_gap', why: '缺钓竿' },
  ], request, log, 'e')
  assert.deepEqual(calls.map(c => c[0]), ['move', 'jump'])
  assert.equal(log.entries.length, 2, 'report_gap 是判定类动作，不发命令也就不记执行日志')
  assert.ok(calls.length === 2)
})

test('observe：从桥命令拼出快照（state/npc/inventory/tile）', async () => {
  const fake = async (cmd) => {
    if (cmd === 'state') return { data: { player: { tile: [10, 20], life: 80, lifeMax: 100, grounded: true } } }
    if (cmd === 'npc') return { data: { npcs: [{ name: '僵尸', hostile: true, dist: 6 }] } }
    if (cmd === 'inventory') return { data: { items: [{ type: 1, stack: 5 }], mainFree: 3 } }
    if (cmd === 'tile') return { data: { type: -1, liquidName: '' } }
    return { data: null }
  }
  const s = await observe(fake)
  assert.equal(s.player.tile[0], 10)
  assert.equal(s.hostiles.length, 1)
  assert.equal(s.inventory.free, 3)
  assert.ok(kinds(classify(s)).includes('hostile_near'))
})

test('异常类型表与默认阈值可被覆盖，不被写死', () => {
  assert.ok(EXCEPTION_KINDS.fatal_env)
  const hits = classify(snap({ player: { life: 60, lifeMax: 100 } }), { hpLowRatio: 0.7 })
  assert.ok(kinds(hits).includes('hp_low'), '把阈值调到 70% 后 60% 也算低')
  assert.equal(DEFAULTS.hpLowRatio, 0.45, '默认值不被污染')
})
test('规则文本可加载，且写清了五类异常 / 重试预算 / 回主线', () => {
  const text = loadExceptionPrompt()
  assert.ok(text.length > 500, '规则文本太短')
  // 规则文本是给人看的，用中文名；代码里用英文 kind，两边靠 zh 对齐。
  for (const [kind, meta] of Object.entries(EXCEPTION_KINDS)) {
    assert.ok(meta.zh, `${kind} 缺中文名`)
    // 只比中文名前半段（括号里的是补充说明，规则文本里措辞可以不同）。
    const stem = meta.zh.split('（')[0]
    assert.match(text, new RegExp(stem), `规则文本缺 ${kind}（${meta.zh}）`)
  }
  assert.match(text, /重试预算|预算/)
  assert.match(text, /回主线|返回主线/)
  assert.match(EXCEPTION_POLICY_PATH, /exception-policy\.md$/)
})

test('预算数字写死在代码里，规则文本里的数字和代码一致', () => {
  const text = loadExceptionPrompt()
  assert.match(text, new RegExp(`${RETRY_BUDGET.jump} ?次`), '跳的次数对不上')
  assert.match(text, new RegExp(`${RETRY_BUDGET.reroute} ?次`), '绕行次数对不上')
})
