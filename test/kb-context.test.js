import test from 'node:test'
import assert from 'node:assert/strict'
import { buildContext, normalizeTags, loadRouterPrompt, TAGS } from '../src/kb-router.js'

test('按分类器标签只取命中的库，且 safety_limit 永远在内', async () => {
  const ctx = await buildContext('帮我造一间护士的房子', {
    tags: 'game_rule, item_operation, build_standard, atomic_action, safety_limit',
  })
  assert.deepEqual(ctx.tags, [
    'safety_limit',
    'game_rule',
    'build_standard',
    'atomic_action',
    'item_operation',
  ])
  assert.equal(ctx.docs.length, 5)
  assert.deepEqual(
    ctx.docs.map((d) => d.tag),
    ctx.tags,
  )
  // 只取这五个：没命中的两个库不该被读进来
  const got = new Set(ctx.docs.map((d) => d.tag))
  assert.ok(!got.has('actor_behavior'))
  assert.ok(!got.has('task_correct'))
  for (const d of ctx.docs) assert.ok(d.text.length > 200, `${d.tag} 正文太短`)
})

test('未命中的库不会被塞进上下文', async () => {
  const ctx = await buildContext('现在几点', { tags: 'safety_limit' })
  assert.deepEqual(ctx.docs.map((d) => d.tag), ['safety_limit'])
})

test('分类器输出里有错标签时，丢掉并报出来，不静默吞', async () => {
  const ctx = await buildContext('测试', { tags: 'game_rule, house_rule, atomic_action' })
  assert.deepEqual(ctx.tags, ['game_rule', 'atomic_action'])
  assert.deepEqual(ctx.unknown, ['house_rule'])
})

test('不传 tags 才走关键词兜底，且结果同样带 safety_limit', async () => {
  const ctx = await buildContext('往东走，路上有怪就打')
  assert.deepEqual(ctx.unknown, undefined)
  assert.ok(ctx.tags.includes('safety_limit'))
  assert.ok(ctx.tags.includes('actor_behavior'))
  assert.ok(ctx.hits.actor_behavior.length > 0, '兜底结果要能说清依据')
})

test('分类器 prompt 可取到、且标签表与 LIBRARIES 一致', () => {
  const prompt = loadRouterPrompt()
  for (const tag of TAGS) assert.ok(prompt.includes(tag), `prompt 缺 ${tag}`)
  assert.match(prompt, /只输出标签/)
})

test('标签常量不被手改坏', () => {
  assert.deepEqual(TAGS, [
    'safety_limit',
    'game_rule',
    'build_standard',
    'actor_behavior',
    'atomic_action',
    'item_operation',
    'task_correct',
  ])
  assert.deepEqual(normalizeTags(TAGS.join(',')).tags, TAGS)
})