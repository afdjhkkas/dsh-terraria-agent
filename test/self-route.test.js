import test from 'node:test'
import assert from 'node:assert/strict'

import {
  NEED_MARKER, TAGS, parseNeedKnowledge, buildContinuation,
  loadSelfRoutePrompt, buildContext, LIBRARIES,
} from '../src/kb-router.js'

test('单轮自路由：system prompt 能取到，且全部已注册标签都在里面', () => {
  const p = loadSelfRoutePrompt()
  assert.ok(p.includes(NEED_MARKER), 'system prompt 必须写明标记格式')
  for (const t of TAGS) assert.ok(p.includes(t), `system prompt 缺标签 ${t}`)
  assert.ok(p.includes('独占一行'), '必须要求标记独占一行')
})

test('单轮自路由：第一行的标记能被抓到，草稿留在后面', () => {
  const r = parseNeedKnowledge('NEED_KNOWLEDGE:safety_limit,game_rule\n草稿：先查房屋判定')
  assert.equal(r.found, true)
  assert.equal(r.markerFirst, true)
  assert.deepEqual(r.tags, ['safety_limit', 'game_rule'])
  assert.equal(r.unknown.length, 0)
  assert.equal(r.draft, '草稿：先查房屋判定')
})

test('单轮自路由：容忍全角冒号/中文逗号/多余空格/CRLF', () => {
  const r = parseNeedKnowledge('NEED_KNOWLEDGE： game_rule， build_standard 、atomic_action\r\n想法')
  assert.equal(r.found, true)
  assert.deepEqual(r.tags, ['game_rule', 'build_standard', 'atomic_action'])
  assert.equal(r.draft, '想法')
})

test('单轮自路由：标签行尾跟中文说明要被截断，不能被当成标签吞掉', () => {
  const r = parseNeedKnowledge('NEED_KNOWLEDGE:safety_limit,game_rule（这两个必须先读）\n继续')
  assert.deepEqual(r.tags, ['safety_limit', 'game_rule'])
  assert.equal(r.unknown.length, 0)
})

test('单轮自路由：瞎编的英文标签要报 unknown，不能静默丢', () => {
  const r = parseNeedKnowledge('NEED_KNOWLEDGE:game_rule, house_rule')
  assert.deepEqual(r.tags, ['game_rule'])
  assert.deepEqual(r.unknown, ['house_rule'])
})

test('单轮自路由：输出顺序被规范成加载顺序，模型排错也不算错', () => {
  const r = parseNeedKnowledge('NEED_KNOWLEDGE:task_correct,safety_limit,item_operation')
  assert.deepEqual(r.tags, ['safety_limit', 'item_operation', 'task_correct'])
})

test('单轮自路由：完全没写标记 → found:false 且不 nearMiss', () => {
  const r = parseNeedKnowledge('我先看看背包有没有木头。')
  assert.equal(r.found, false)
  assert.equal(r.nearMiss, false)
  assert.deepEqual(r.tags, [])
})

test('单轮自路由：标记写在句中间 → 不算数，但要 nearMiss（好让上层重问）', () => {
  const r = parseNeedKnowledge('我先想想再写标记 NEED_KNOWLEDGE:actor_behavior 然后继续')
  assert.equal(r.found, false)
  assert.equal(r.nearMiss, true)
})

test('单轮自路由：续传提示包含每个命中库的正文一次、并带上原任务', () => {
  const docs = [
    { tag: 'safety_limit', file: 'safety-limit.md', text: '禁令A' },
    { tag: 'game_rule', file: 'game-rule.md', text: '规则B' },
  ]
  const c = buildContinuation(docs, { task: '帮我造护士房间' })
  assert.ok(c.includes('禁令A') && c.includes('规则B'))
  assert.equal(c.split('规则B').length - 1, 1, '同一段知识不能重复贴两遍')
  assert.ok(c.includes('帮我造护士房间'), '续传必须带原任务，避免模型跑偏')
  assert.ok(c.includes('回读'), '续传要要求验证，不能只说"输出动作"')
})

test('单轮自路由：只加载命中的库，没点名的库一个字都不许进上下文', async () => {
  const r = parseNeedKnowledge('NEED_KNOWLEDGE:safety_limit,build_standard\n造房子')
  const ctx = await buildContext(r.draft, { tags: r.tags })
  assert.deepEqual(ctx.tags, ['safety_limit', 'build_standard'])
  const files = ctx.docs.map((d) => d.file)
  assert.equal(files.length, 2)
  for (const tag of TAGS) {
    if (r.tags.includes(tag)) continue
    assert.ok(!files.includes(LIBRARIES[tag].file), `${tag} 没被点名却被加载了`)
  }
})

test('单轮自路由：加载顺序始终是 safety_limit 在最前（覆盖优先级）', async () => {
  const r = parseNeedKnowledge('NEED_KNOWLEDGE:item_operation,safety_limit,task_correct')
  const ctx = await buildContext('x', { tags: r.tags })
  assert.equal(ctx.docs[0].tag, 'safety_limit')
})
