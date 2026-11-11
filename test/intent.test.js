import test from 'node:test'
import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

/**
 * intent（进度检查表）的回归测试。
 *
 * 这里有两层，各有各的边界，别混：
 *  1. **语义层**：用一份与 C# `IntentCommand` / `IntentGuard` 同判据的镜像实现，
 *     对真实的 `goal-progress.json` 跑「材料够 / 不够 / 自动推进 / guard 拦截」。
 *     它锁的是**判定规则**（改 JSON 或改规则会红），但它不是 C# 本身 ——
 *     桥内的真实回包只能在游戏里回读，见文件末尾那条说明。
 *  2. **源码层**：锁住 intent 是只读的、guard 默认关闭、命令已登记。
 *     这层挡的是「改回去就静默失效」那类改动。
 */

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')
const CMDS = join(ROOT, 'DshTerrariaBridge', 'Commands.cs')
const GOAL = join(ROOT, 'goal-progress.json')

const goal = JSON.parse(await readFile(GOAL, 'utf8'))
const cmds = await readFile(CMDS, 'utf8')

/* ── 与 C# 同判据的镜像实现 ─────────────────────────────────────── */

/** 背包里这件东西有几个；anyOf = 任意一个都算，取最大值而不是求和（同名规则见 HaveGoalItem）。 */
function have(bag, check) {
  let n = bag[check.id] ?? 0
  for (const alt of check.anyOf ?? []) n = Math.max(n, bag[alt] ?? 0)
  return n
}

/** 对应 IntentCommand：判据全满足就沿 stageOrder 往后走一格，只推进连续完成的阶段。 */
function intent(bag, stageKey = goal.stage) {
  const order = goal.stageOrder
  let key = stageKey
  let st = goal.stages[key]
  const advanced = []
  for (let step = 0; step <= order.length; step++) {
    const allDone = st.checks.every((c) => have(bag, c) >= c.need)
    if (!allDone) break
    const idx = order.indexOf(key)
    if (idx < 0 || idx + 1 >= order.length) break
    key = order[idx + 1]
    advanced.push(key)
    st = goal.stages[key]
  }
  const missing = st.checks
    .filter((c) => have(bag, c) < c.need)
    .map((c) => ({ id: c.id, label: c.label, have: have(bag, c), need: c.need }))
  return { stage: key, stageLabel: st.label, done: missing.length === 0, missing, advanced }
}

/** 对应 IntentGuard：只有 guard.strict 为真的阶段才可能拦；读不到/没标 strict 一律放行。 */
function guard(stageKey, action, targetId) {
  const st = goal.stages[stageKey]
  if (!st?.guard?.strict) return null
  if (action === 'craft') {
    const wanted = st.checks.some(
      (c) => c.id === targetId || (c.anyOf ?? []).includes(targetId),
    )
    if (wanted) return null
    return `当前阶段是「${st.label}」，craft ${targetId} 不在这一阶段要凑的东西里`
  }
  if (st.guard.world === false) return `当前阶段是「${st.label}」，这一阶段不需要动土/建造`
  return null
}

/* ── 1. 进度文件本身要站得住 ───────────────────────────────────── */

test('goal-progress.json：阶段定义自洽，物品 id 都是真实数值', () => {
  assert.ok(goal.stageOrder.length >= 3, 'stageOrder 至少要有 3 个阶段')
  assert.ok(
    goal.stageOrder.includes(goal.stage),
    `当前 stage「${goal.stage}」必须出现在 stageOrder 里，否则 AI 改错一个字母就再也读不出来`,
  )
  for (const key of goal.stageOrder) {
    const st = goal.stages[key]
    assert.ok(st, `stageOrder 里的「${key}」必须在 stages 里有定义`)
    assert.ok(st.label && st.goal, `${key} 必须有 label 与 goal：AI 靠它们判断"当前在干什么"`)
    assert.ok(Array.isArray(st.checks) && st.checks.length > 0, `${key} 必须有至少一条 checks 判据`)
    assert.ok(typeof st.next === 'string' && st.next.length > 0, `${key} 必须有 next 建议`)
    for (const c of st.checks) {
      assert.ok(Number.isInteger(c.id) && c.id > 0, `${key} 的 check id 必须是正整数（真实 ItemID）：${JSON.stringify(c)}`)
      assert.ok(Number.isInteger(c.need) && c.need > 0, `${key} 的 check need 必须是正数`)
      for (const alt of c.anyOf ?? []) {
        assert.ok(Number.isInteger(alt) && alt > 0, `${key} 的 anyOf 里必须都是正整数 ItemID`)
      }
    }
  }
})

/* ── 2. 材料够 / 不够，返回的 stage 与 missing 必须不同 ──────────── */

test('intent：材料不够时报 missing，材料够了就把阶段顶到下一段', () => {
  const empty = intent({}, 'wood')
  assert.equal(empty.stage, 'wood', '空背包时不该推进阶段')
  assert.equal(empty.done, false)
  assert.equal(empty.missing.length, goal.stages.wood.checks.length, '空背包时每条判据都该缺')

  const wood = goal.stages.wood.checks
  const bag = Object.fromEntries(wood.map((c) => [c.id, c.need]))
  const full = intent(bag, 'wood')
  // 够了的定义是"当前阶段判据全满足 → 自动推进"，所以返回的是新阶段，不是 done:true
  assert.deepEqual(full.advanced, [goal.stageOrder[1]], 'wood 齐了必须自动推进到下一段')
  assert.equal(full.stage, goal.stageOrder[1])
  assert.notEqual(full.stageLabel, empty.stageLabel, '够与不够必须落成不同阶段，不能都报同一段')
  assert.notDeepEqual(
    full.missing.map((m) => m.id),
    empty.missing.map((m) => m.id),
    '推进后缺的东西必须换成新阶段判据，不能沿用上一阶段的清单',
  )
})

test('intent：最后一段也满足时 done:true、missing 为空', () => {
  const lastKey = goal.stageOrder.at(-1)
  const st = goal.stages[lastKey]
  const bag = Object.fromEntries(st.checks.map((c) => [c.id, c.need]))
  const r = intent(bag, lastKey)
  assert.deepEqual(r.advanced, [], '最后一段之后没有下一段可推')
  assert.equal(r.stage, lastKey)
  assert.equal(r.done, true, '判据全部满足且无处可推时必须 done:true')
  assert.deepEqual(r.missing, [], 'done 时 missing 必须为空')
})

test('intent：缺一件时只报那一件，have/need 与实际数量一致', () => {
  const wood = goal.stages.wood.checks
  const bag = { [wood[0].id]: wood[0].need }
  const r = intent(bag, 'wood')
  assert.equal(r.done, false)
  assert.equal(r.missing.length, wood.length - 1)
  const first = r.missing.find((m) => m.id === wood[1].id)
  assert.ok(first, '第二件材料必须出现在 missing 里')
  assert.equal(first.have, 0)
  assert.equal(first.need, wood[1].need)
})

/* ── 3. 阶段自动推进：只推连续完成的，不跳空 ─────────────────────── */

test('intent：当前阶段满足就自动推进到 stageOrder 的下一段', () => {
  const wood = goal.stages.wood.checks
  const bag = Object.fromEntries(wood.map((c) => [c.id, c.need]))
  const r = intent(bag, 'wood')
  assert.deepEqual(r.advanced, [goal.stageOrder[1]], 'wood 完成时只能推进一格，不许一次跳两段')
  assert.equal(r.stage, goal.stageOrder[1])
})

test('intent：前一段没完成就停在原地，绝不跳空推进', () => {
  const r = intent({}, 'wood')
  assert.deepEqual(r.advanced, [], '没有完成任何判据时不许记 advanced')
  assert.equal(r.stage, 'wood', '停在当前阶段')
})

test('intent：anyOf 判据（蠕虫诱饵 70 / 血腥脊椎 1331）任一够就算够', () => {
  const st = goal.stages.evil_boss
  const check = st.checks.find((c) => c.anyOf)
  assert.ok(check, 'evil_boss 必须有一条 anyOf 判据，覆盖腐化/猩红两种世界')
  const viaFirst = Object.fromEntries(st.checks.map((c) => [c.id, c.need]))
  const viaAlt = Object.fromEntries(
    st.checks.map((c) => [c.anyOf[0], c.need]),
  )
  assert.equal(intent(viaFirst, 'evil_boss').done, true)
  assert.equal(intent(viaAlt, 'evil_boss').done, true, 'anyOf 的另一个 id 单独够也必须算够')
})

/* ── 4. guard:true 拦住不相关动作，guard:false 一律放行 ─────────── */

test('guard：严格阶段拦下不属于本阶段的动作', () => {
  const strict = goal.stageOrder.find((k) => goal.stages[k].guard?.strict)
  assert.ok(strict, '至少要有一个阶段的 guard.strict 为真，否则这道闸门等于没有')

  const st = goal.stages[strict]
  const wantedId = st.checks[0].id
  assert.equal(guard(strict, 'craft', wantedId), null, '本阶段要凑的东西不许拦')
  const blocked = guard(strict, 'craft', 4)
  assert.ok(blocked && blocked.includes(st.label), '无关 craft 必须被拦，且报出当前阶段名')
  if (st.guard.world === false) {
    assert.ok(guard(strict, 'mine', 0), 'world:false 的阶段必须拦住 mine')
  }
})

test('guard：没有标 strict 的阶段一律放行（宁可放过，不误拦）', () => {
  const loose = goal.stageOrder.find((k) => !goal.stages[k].guard?.strict)
  assert.ok(loose, '必须存在宽松阶段')
  assert.equal(guard(loose, 'craft', 4), null)
  assert.equal(guard(loose, 'mine', 0), null)
})

/* ── 5. 源码锁：intent 只读、guard 默认关、命令已登记 ───────────── */

test('源码：intent 已登记进分发与 CommandNames', () => {
  assert.match(cmds, /case\s+"intent"\s*:\s*return\s+IntentCommand\(p\)\s*;/,
    '顶层 switch 必须有 case "intent" → IntentCommand(p)')
  const names = cmds.match(/CommandNames\s*=\s*new\[\]\s*\{([\s\S]*?)\};/)
  assert.ok(names, '找不到 CommandNames 数组')
  assert.ok(names[1].includes('"intent"'),
    'intent 必须进 CommandNames：help / unknown 提示都从这里出，漏了调用方会以为命令不存在')
})

test('源码：intent 函数体必须是纯读（不写世界、不写文件、不碰输入）', () => {
  const start = cmds.indexOf('private static string IntentCommand')
  const end = cmds.indexOf('private static string IntentGuard', start)
  assert.ok(start > 0 && end > start, '定位不到 IntentCommand 函数体')
  const body = cmds.slice(start, end)

  for (const [pattern, why] of [
    [/Main\.tile\[[^\]]*\]\s*(?:\.\w+\s*)?=/, '不许直接写 Main.tile：世界只能通过真实物品流改变'],
    [/BeginItemUse\(/, 'intent 不许开物品使用：它只回答"该做什么"，不动手'],
    [/BeginPlayerMine\(/, 'intent 不许开挖掘'],
    [/WorldGen\./, 'intent 不许调 WorldGen'],
    [/Main\.NewText\(/, 'intent 不许往聊天框写'],
    [/selectedItem\s*=(?!=)/, 'intent 不许切快捷栏'],
    [/File\.(?:Write|Append|Delete|Move|Copy)/, 'intent 是只读的：进度推进由 AI 改文件，不由命令写'],
  ]) {
    assert.doesNotMatch(body, pattern, why)
  }
  assert.match(body, /File\.ReadAllText|TryLoadGoal\(/, 'intent 必须从进度文件读，而不是把阶段写死在代码里')
})

test('源码：guard 默认 false，且 mine/place/craft 三处都上线了这道闸', () => {
  // follow 命令也有一个同名的 guard 参数（"跟着队友时顺便打怪"），所以按上下文数，不按裸名数。
  const gates = [...cmds.matchAll(/Json\.Bool\(p,\s*"guard",\s*false\)/g)]
    .filter((m) => /IntentGuard\(/.test(cmds.slice(m.index, m.index + 260)))
  assert.ok(gates.length >= 3,
    'mine / place / craft 各要一处 Json.Bool(p,"guard",false)+IntentGuard：默认 false 才不破坏现有全部调用与测试')
  for (const action of ['mine', 'place', 'craft']) {
    assert.match(cmds, new RegExp(`IntentGuard\\(p,\\s*"${action}"`),
      `${action} 必须调用 IntentGuard；否则 guard:true 对它无效`)
  }
  // 闸门读不到进度文件时必须放行：否则一次路径写错就让所有带 guard 的动作全被拒
  const gStart = cmds.indexOf('private static string IntentGuard')
  const gBody = cmds.slice(gStart, gStart + 2600)
  assert.match(gBody, /TryLoadGoal\([\s\S]{0,120}?\)\s*\)\s*return\s+null/,
    'IntentGuard 读不到进度文件必须 return null 放行（宁可放过不误拦）')
})

/**
 * 未覆盖的部分（诚实记录）：上面第 1–4 条跑的是镜像实现，不是桥内的 C#。
 * 真正「intent 回包里的 stage/missing 与游戏背包对得上」只能进游戏验证：
 *   npm run bridge intent
 * 桥没起来时这条永远不算通过（AGENTS §0.66 的三层：doctor → source-locks → 真机）。
 */
test('说明：真机验证不在本文件内', () => {
  assert.ok(true, '见 docs/capabilities.md 的 intent 行：状态需真机回读')
})