import test from 'node:test'
import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import {
  CASES_PATH, SCHEMA, loadCases, loadCaps, validate, precheck, runSuite, aggregate, formatReport, diffReports,
} from '../src/testcases.js'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')

test('用例库结构合法：id 唯一、阶段 1~4、分类在册、字段齐全', async () => {
  const v = await validate()
  assert.deepEqual(v.errors, [], v.errors.join('\n'))
  assert.ok(v.total >= 50, `用例太少（${v.total}），覆盖不到九大类`)
})

test('用例库声明为“缺”的命令，必须真的还缺（补上了就要回来改用例）', async () => {
  const doc = await loadCases()
  const caps = await loadCaps()
  const names = new Set(caps.commands.map((c) => c.name))
  for (const c of doc.cases) {
    for (const cmd of c.needs.missingCommands ?? []) {
      assert.ok(!names.has(cmd), `${c.id}: ${cmd} 已经实现了，把 missingCommands 里的声明删掉`)
    }
    for (const cmd of c.needs.commands ?? []) {
      assert.ok(names.has(cmd), `${c.id}: 需要用 ${cmd}，但能力清单里没有（应该写进 missingCommands）`)
    }
  }
})

test('用例引用的“明确做不到”必须是清单里真有的那几条，不许自己编', async () => {
  const doc = await loadCases()
  const caps = await loadCaps()
  const known = new Set(caps.notSupported.map((x) => x.what))
  for (const c of doc.cases) {
    for (const what of c.needs.unsupported ?? []) {
      assert.ok(known.has(what), `${c.id}: "${what}" 不在 notSupported 里`)
    }
  }
})

test('阶段 1 只允许出现移动/采集/建造/合成四类（这是用户定的推进顺序）', async () => {
  const doc = await loadCases()
  const allowed = new Set(['move', 'gather', 'build', 'craft'])
  for (const c of doc.cases.filter((x) => x.phase === 1)) {
    assert.ok(allowed.has(c.category), `${c.id} 是阶段 1，但分类是 ${c.category}`)
  }
})

test('九大类每个都有用例，别偏科', async () => {
  const doc = await loadCases()
  const seen = new Set(doc.cases.map((c) => c.category))
  for (const cat of Object.keys(doc.categories)) {
    assert.ok(seen.has(cat), `分类 ${cat}（${doc.categories[cat]}）一条用例都没有`)
  }
  assert.equal(Object.keys(doc.categories).length, 9)
})

test('离线预检能跑出报告，形状对得上', async () => {
  const report = await runSuite()
  assert.equal(report.schema, SCHEMA)
  assert.equal(report.total, (await loadCases()).cases.length)
  assert.equal(report.pass + report.gap, report.total)
  assert.ok(Array.isArray(report.results))
  for (const r of report.results) {
    assert.ok(['PASS', 'GAP'].includes(r.status))
    if (r.status === 'GAP') assert.ok(r.gaps.length > 0, `${r.id} 标了 GAP 却没有缺口说明`)
    for (const g of r.gaps) assert.ok(['动作原语缺口', '能力缺口', '知识缺口'].includes(g.dimension))
  }
  assert.ok(report.aggregate.commands.length + report.aggregate.knowledge.length + report.aggregate.unsupported.length > 0)
})

test('--phase 过滤只返回该阶段', async () => {
  const r1 = await runSuite({ phase: 1 })
  assert.ok(r1.total > 0)
  assert.ok(r1.results.every((r) => r.phase === 1))
  const r2 = await runSuite({ phase: 2 })
  assert.equal(r1.total + r2.total <= (await loadCases()).cases.length, true)
})

test('按分类过滤只返回该分类', async () => {
  const r = await runSuite({ category: 'fish' })
  assert.ok(r.total > 0)
  assert.ok(r.results.every((x) => x.category === 'fish'))
})

test('汇总把缺口归到“要补什么”，而不是按用例列', async () => {
  const report = await runSuite()
  const flat = [
    ...report.aggregate.commands,
    ...report.aggregate.knowledge,
    ...report.aggregate.unsupported,
  ]
  for (const row of flat) {
    assert.ok(typeof row.what === 'string' && row.what.length > 0)
    assert.ok(row.count === row.cases.length)
    assert.ok(row.cases.length > 0)
  }
  // 按影响面排序
  const counts = report.aggregate.commands.map((x) => x.count)
  assert.deepEqual(counts, [...counts].sort((a, b) => b - a))
})

test('报告渲染出关键信息：通过数、缺口表、逐条结果', async () => {
  const report = await runSuite()
  const md = formatReport(report)
  assert.match(md, /# 用例预检报告/)
  assert.ok(md.includes(`${report.pass}/${report.total} 通过`))
  assert.match(md, /## 一、该补的东西/)
  assert.match(md, /## 二、逐条结果/)
  for (const r of report.results.slice(0, 3)) assert.ok(md.includes(r.id))
})

test('基线对比能分出修好了 / 新坏了 / 仍缺', () => {
  const before = {
    results: [
      { id: 'A', status: 'GAP', gaps: [{ dimension: '动作原语缺口', detail: 'x' }] },
      { id: 'B', status: 'PASS', gaps: [] },
      { id: 'C', status: 'GAP', gaps: [{ dimension: '知识缺口', detail: 'y' }] },
    ],
  }
  const after = {
    results: [
      { id: 'A', status: 'PASS', gaps: [] },
      { id: 'B', status: 'GAP', gaps: [{ dimension: '知识缺口', detail: 'z' }] },
      { id: 'C', status: 'GAP', gaps: [{ dimension: '知识缺口', detail: 'y' }] },
    ],
  }
  const d = diffReports(before, after)
  assert.deepEqual(d.fixed, ['A'])
  assert.deepEqual(d.broken, ['B'])
  assert.deepEqual(d.still, ['C'])
})

test('知识缺口必须是“读过之后确认缺”的具体描述，不是空话', async () => {
  const doc = await loadCases()
  for (const c of doc.cases) {
    for (const what of c.needs.kbMissing ?? []) {
      assert.ok(what.length >= 8, `${c.id}: 知识缺口写得太短，没法照着补：${what}`)
      assert.ok(!/^(没有|缺)$/.test(what), `${c.id}: 知识缺口只写了“没有”，要写缺什么`)
    }
  }
})

test('用例库落盘在仓库里，能被独立读出来（不依赖内存缓存）', async () => {
  const raw = JSON.parse(await readFile(CASES_PATH, 'utf8'))
  assert.equal(raw.schema, SCHEMA)
  assert.ok(Array.isArray(raw.cases) && raw.cases.length > 0)
  assert.ok(raw.categories && raw.phases)
})

test('仓库里留了报告和基线，README 说明怎么跑', async () => {
  const readme = await readFile(join(ROOT, 'docs', 'testcases', 'README.md'), 'utf8')
  assert.match(readme, /node scripts\/testcases\.mjs --dry/)
  assert.match(readme, /--baseline/)
  const report = await readFile(join(ROOT, 'docs', 'testcases', 'gap-report.md'), 'utf8')
  assert.match(report, /用例预检报告/)
})
test('阶段①离线推演：推演包覆盖全部用例，缺口表四类齐全', async () => {
  const doc = await loadCases()
  const packets = await readFile(join(ROOT, 'docs', 'testcases', 'sim-packets.md'), 'utf8')
  const gaps = await readFile(join(ROOT, 'docs', 'testcases', 'sim-gaps.md'), 'utf8')
  const prompt = await readFile(join(ROOT, 'prompts', 'offline-sim.md'), 'utf8')

  // 每条用例都要有推演包和逐条结论
  for (const c of doc.cases) {
    assert.match(packets, new RegExp(`### ${c.id}（`), `推演包缺 ${c.id}`)
    assert.match(gaps, new RegExp(`\\| ${c.id} \\|`), `缺口表缺 ${c.id}`)
  }
  // 推演包必须带路由标记，且编号合法（不许出现未注册标签）
  const markers = packets.match(/NEED_KNOWLEDGE:([^\n]*)/g) ?? []
  assert.ok(markers.length >= doc.cases.length, `路由标记少于用例数（${markers.length}）`)
  const { TAGS } = await import('../src/kb-router.js')
  for (const m of markers) {
    for (const tag of m.slice('NEED_KNOWLEDGE:'.length).split(',').map((x) => x.trim()).filter(Boolean)) {
      assert.ok(TAGS.includes(tag), `推演包里出现未注册标签 ${tag}`)
    }
  }
  // 四类汇总齐全、编号唯一
  for (const head of ['缺命令', '缺读取', '缺知识', '缺规则与判定']) {
    assert.ok(gaps.includes(head), `缺口表缺分类：${head}`)
  }
  // 缺口编号是 C1/R1/K1/D1 这种不带前导零的；带前导零的是用例 id（K01…），别混
  const ids = [...gaps.matchAll(/^\| ([CRKD][1-9]\d*) \|/gm)].map((m) => m[1])
  assert.deepEqual(ids, [...new Set(ids)], '缺口编号重复')
  assert.ok(ids.length >= 20, `缺口项太少（${ids.length}）`)
  // 疑问清单要留给阶段③
  assert.match(gaps, /留到阶段③实测验证的疑问/)
  assert.match(prompt, /不生成任何发给游戏的动作指令/)
})
