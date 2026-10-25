import test from 'node:test'
import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

import {
  DIMENSIONS, parseSelfCheck, formatGapReport, checkPlanCommands, selfCheck,
  loadCapabilities, loadMetaCheckPrompt, resetCapabilitiesCache, SELF_CHECK_MARKER,
} from '../src/meta-check.js'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')

const passBlock = [
  'SELF_CHECK:',
  '1 规则核验: PASS',
  '2 道具核验: PASS',
  '3 地形/路径核验: PASS',
  '4 动作原语核验: PASS',
  '5 状态读取核验: PASS',
].join('\n')

test('五个维度固定为 1..5，名称不许改', () => {
  assert.deepEqual(DIMENSIONS.map((d) => d.no), [1, 2, 3, 4, 5])
  assert.deepEqual(
    DIMENSIONS.map((d) => d.label),
    ['规则核验', '道具核验', '地形/路径核验', '动作原语核验', '状态读取核验'],
  )
  assert.equal(SELF_CHECK_MARKER, 'SELF_CHECK')
})

test('五项全 PASS → canAct', () => {
  const check = parseSelfCheck(passBlock)
  assert.equal(check.found, true)
  assert.equal(check.markerFirst, true)
  assert.equal(check.canAct, true)
  assert.equal(check.gaps.length, 0)
  assert.equal(check.missing.length, 0)
})

test('任何一项 GAP → 不许动手，并给出 A/B 分支', () => {
  const text = [
    'SELF_CHECK:',
    '1 规则核验: PASS',
    '2 道具核验: GAP 缺钓竿（木头8+铁锭8，铁砧合成）',
    '3 地形/路径核验: PASS',
    '4 动作原语核验: PASS',
    '5 状态读取核验: PASS',
  ].join('\n')
  const check = parseSelfCheck(text)
  assert.equal(check.canAct, false)
  assert.equal(check.gaps.length, 1)
  assert.equal(check.gaps[0].gap, '道具缺口')
  assert.match(check.gaps[0].note, /钓竿/)

  const report = formatGapReport(check)
  assert.match(report, /自检未通过/)
  assert.match(report, /道具缺口/)
  assert.match(report, /分支A/)
  assert.match(report, /分支B/)
})

test('少写一维 → 未完成，canAct=false，报告里点名', () => {
  const text = ['SELF_CHECK:', '1 规则核验: PASS', '2 道具核验: PASS'].join('\n')
  const check = parseSelfCheck(text)
  assert.equal(check.found, true)
  assert.equal(check.canAct, false)
  assert.deepEqual(check.missing, [3, 4, 5])
  const report = formatGapReport(check)
  assert.match(report, /自检没做完/)
  assert.match(report, /第 3 维/)
})

test('没写 SELF_CHECK 块 → found=false，不能动手', () => {
  const check = parseSelfCheck('直接走过去挖了就行。')
  assert.equal(check.found, false)
  assert.equal(check.canAct, false)
})

test('标记没独占一行 → markerFirst=false（但仍能解析）', () => {
  const check = parseSelfCheck('我先说一句。\n' + passBlock)
  assert.equal(check.found, true)
  assert.equal(check.markerFirst, false)
  assert.equal(check.canAct, true)
})

test('容忍全角冒号 / 顿号编号 / 【维度】 / CRLF / 小写 pass', () => {
  const text = [
    'SELF_CHECK：',
    '1、【规则核验】：pass',
    '2、道具核验：GAP 缺虫网',
    '3 地形路径核验: PASS',
    '4. 动作原语: PASS',
    '5 状态: GAP 读不到弹幕',
  ].join('\r\n')
  const check = parseSelfCheck(text)
  assert.equal(check.found, true, '应认出一行 SELF_CHECK：')
  assert.equal(check.canAct, false)
  assert.equal(check.items.length, 5)
  assert.deepEqual(check.gaps.map((g) => g.gap), ['道具缺口', '状态读取缺口'])
})

test('SKIP 只用于只读小事；不算 canAct', () => {
  const check = parseSelfCheck('SELF_CHECK: skip（只读）')
  assert.equal(check.skip, true)
  assert.equal(check.canAct, false)
  assert.match(formatGapReport(check), /自检没做完|缺口/)
})

test('checkPlanCommands：编造的命令必须报出来，真实命令识别为 used', () => {
  const names = ['mine', 'place', 'move', 'state']
  const plan = '先 `mine{x,y}`，再 `place{x,y,type}`，然后用 `fish{rod}` 抛竿，`state` 看血量。'
  const r = checkPlanCommands(plan, names)
  assert.deepEqual(r.unknown, ['fish'])
  assert.deepEqual(r.used, ['mine', 'place', 'state'])
})

test('能力清单从 docs/capabilities.json 读，且每条都有 kind', async () => {
  resetCapabilitiesCache()
  const caps = await loadCapabilities()
  assert.ok(caps.commands.length > 50, '清单应该覆盖全部命令')
  for (const c of caps.commands) {
    assert.ok(['read', 'act', 'both'].includes(c.kind), `${c.name} 的 kind 不合法：${c.kind}`)
  }
  // 抽查几条必须分类正确的
  const kind = (n) => caps.byName.get(n)?.kind
  assert.equal(kind('mine'), 'act')
  assert.equal(kind('place'), 'act')
  assert.equal(kind('move'), 'act')
  assert.equal(kind('state'), 'read')
  assert.equal(kind('tile'), 'read')
  assert.equal(kind('inventory'), 'read')
  assert.ok(caps.notSupported.length >= 5, '必须写明明确做不到的事')
})

test('selfCheck 把编造命令也算作不许动手', async () => {
  resetCapabilitiesCache()
  const r = await selfCheck(passBlock + '\n计划：先 `fish{rod}`。')
  assert.equal(r.found, true)
  assert.equal(r.canAct, false, '五个 PASS 但用了不存在的命令，也不许动手')
  assert.deepEqual(r.unknownCommands, ['fish'])
})

test('能力清单与代码一致（生成物过期就红）', async () => {
  const { execFileSync } = await import('node:child_process')
  const out = execFileSync(process.execPath, [join(ROOT, 'scripts', 'kb-caps.mjs'), '--check'], {
    cwd: ROOT,
    encoding: 'utf8',
  })
  assert.match(out, /^ok: \d+ commands/)
})

test('AGENTS.md 里有能力块，且块里的命令数与清单一致', async () => {
  resetCapabilitiesCache()
  const caps = await loadCapabilities()
  const agents = await readFile(join(ROOT, 'AGENTS.md'), 'utf8')
  assert.match(agents, /<!-- caps:begin/)
  assert.match(agents, /<!-- caps:end -->/)
  for (const name of caps.names) {
    if (name === 'stop') continue // stop 这种单词太常见，只校验块内列表
    assert.ok(agents.includes(name), `AGENTS.md 能力块缺少命令 ${name}`)
  }
})

test('元自检规则正文可加载，且包含五维与 A/B 分支', async () => {
  const prompt = await loadMetaCheckPrompt()
  for (const d of DIMENSIONS) {
    assert.ok(prompt.includes(d.label) || prompt.includes(d.label.replace('/', '')), `规则正文缺少 ${d.label}`)
  }
  assert.match(prompt, /分支A/)
  assert.match(prompt, /分支B/)
  assert.match(prompt, /SELF_CHECK/)
})
test('同一回合第一行是 NEED_KNOWLEDGE 时，SELF_CHECK 仍算独占第一行；有别的废话就不算', () => {
  const ok = 'NEED_KNOWLEDGE: safety_limit, world_biome\nSELF_CHECK:\n1 规则核验: PASS\n2 道具核验: PASS\n3 地形/路径核验: PASS\n4 动作原语核验: PASS\n5 状态读取核验: PASS'
  const good = parseSelfCheck(ok)
  assert.equal(good.markerFirst, true)
  assert.equal(good.canAct, true)

  const noisy = '先说一句没用的\nSELF_CHECK:\n1 规则核验: PASS'
  assert.equal(parseSelfCheck(noisy).markerFirst, false)
})
