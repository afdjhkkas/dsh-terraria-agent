// 导航热更参数的离线校验。
//
// 为什么要有：mod 桥接方案每次改 C# 都要重启游戏，调试成本极高（用户 2026-10-27 指出）。
// 所以把「决策阈值 / 优先级 / 动作参数」抽到 json 里热更，只把感知层和 A* 核心留在 C#。
// 代价是配置有了两个副本（仓库 `config/nav-rules.json` 与 C# 里的 `Template()`），
// 一旦两边不一致就会出现"我改的没生效"或"游戏里写出来的默认值和我以为的不同"。
// 这个测试就是锁住这件事：**离线就能发现两副本漂移，不用进游戏**。

import { test } from 'node:test'
import assert from 'node:assert/strict'
import fs from 'node:fs'
import path from 'node:path'

const root = path.resolve(import.meta.dirname, '..')
const cfgPath = path.join(root, 'config', 'nav-rules.json')
const csPath = path.join(root, 'DshTerrariaBridge', 'NavRules.cs')

const readCfg = () => JSON.parse(fs.readFileSync(cfgPath, 'utf8'))
const readCs = () => fs.readFileSync(csPath, 'utf8')

test('config/nav-rules.json 是合法 JSON，且 thresholds/priorities 两段都在', () => {
  const cfg = readCfg()
  assert.equal(typeof cfg.thresholds, 'object')
  assert.equal(typeof cfg.priorities, 'object')
})

test('阈值全是有限正数（写成 0/负数/字符串会被 C# 当非法值忽略，等于我以为改了其实没改）', () => {
  const { thresholds } = readCfg()
  for (const [k, v] of Object.entries(thresholds)) {
    assert.equal(typeof v, 'number', `${k} 必须是数字`)
    assert.ok(Number.isFinite(v), `${k} 必须有限`)
    assert.ok(v > 0, `${k} 必须 > 0`)
  }
})

test('优先级是不重复的正整数（重复会让两条规则抢同一档，行为变成看代码顺序）', () => {
  const { priorities } = readCfg()
  const seen = new Map()
  for (const [k, v] of Object.entries(priorities)) {
    assert.equal(typeof v, 'number', `${k} 必须是数字`)
    assert.ok(Number.isInteger(v) && v > 0, `${k} 必须是正整数`)
    assert.ok(!seen.has(v), `${k} 与 ${seen.get(v)} 优先级重复：${v}`)
    seen.set(v, k)
  }
})

test('配置文件里的键，C# 的 Template() 里一个不少（少一个就等于游戏里写出来的默认值缺项）', () => {
  const cfg = readCfg()
  const cs = readCs()
  const missing = []
  for (const k of Object.keys(cfg.thresholds)) if (!cs.includes(`\\"${k}\\": `)) missing.push(`thresholds.${k}`)
  for (const k of Object.keys(cfg.priorities)) if (!cs.includes(`\\"${k}\\": `)) missing.push(`priorities.${k}`)
  assert.deepEqual(missing, [], `Template() 缺这些键：${missing.join(', ')}`)
})

test('C# 里每个键都有对应的解析分支（写了模板但不解析 = 改了不起作用）', () => {
  const cfg = readCfg()
  const cs = readCs()
  const unread = []
  for (const k of Object.keys(cfg.thresholds)) if (!cs.includes(`"${k}"`)) unread.push(`thresholds.${k}`)
  for (const k of Object.keys(cfg.priorities)) if (!cs.includes(`"${k}"`)) unread.push(`priorities.${k}`)
  assert.deepEqual(unread, [], `NavRules.cs 没读这些键：${unread.join(', ')}`)
})

test('满跳高度与 A* 的上限一致：jumpTilesMax 必须等于 path-model 的 JUMP_UP_MAX', async () => {
  const { JUMP_UP_MAX } = await import('../src/path-model.js')
  const { thresholds } = readCfg()
  assert.equal(
    thresholds.jumpTilesMax, JUMP_UP_MAX,
    `配置写 ${thresholds.jumpTilesMax}、A* 用 ${JUMP_UP_MAX}，两边不一致会让"算得出路"和"跳得上去"对不上`,
  )
})

test('Evaluate 不再有写死的优先级字面量（全部走 NavRules.P）', () => {
  const cs = readCs().replace(/NavRules\.cs$/, '')
  const bridge = fs.readFileSync(path.join(root, 'DshTerrariaBridge', 'BridgeSystem.cs'), 'utf8')
  const start = bridge.indexOf('private static ActorAct Evaluate(ActorSense s)')
  assert.ok(start > 0, '没找到 Evaluate')
  const body = bridge.slice(start, bridge.indexOf('private static int WalkDetour'))
  assert.ok(body.length > 200, 'Evaluate 片段截取失败')
  // 形如 priority = 70 / priority = 100 的写死值
  const hard = [...body.matchAll(/priority\s*=\s*(\d+)/g)].map(m => m[1])
  assert.deepEqual(hard, [], `Evaluate 里还有写死的优先级：${hard.join(', ')}；应改成 NavRules.P.xxx`)
})