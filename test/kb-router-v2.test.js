/**
 * kb-router × v2 意图分类器 接线测试
 *
 * 运行方式：
 *   npm test               ← 全量（推荐）
 *   node --test test/kb-router-v2.test.js
 *
 * 接线契约（src/kb-router.js 的 route()）：
 *   1. 关键词结论一个都不许丢 —— v2 只能往 tags 里"加"，不能"换"。
 *      依据：实测 59 句玩家原话，v2 会在「我能飞多高」「我这身装备能飞吗」上
 *      把 abilities 换成 atomic_action+skills，直接替换会丢掉正确库。
 *   2. safety_limit 永远第一位；玩家纠错时 task_correct 是第二位。
 *   3. v2 置信度 < 0.5 或分类器抛错 → 结果必须与 routeKeyword() 完全一致。
 *   4. classifier:'keyword' 是排障/A-B 开关，必须能回到旧行为。
 */

import test from 'node:test'
import assert from 'node:assert/strict'

import { route, routeKeyword, ALWAYS } from '../src/kb-router.js'

test('route() 默认返回 classifier 标记', () => {
  const r = route('挖这个猩红石')
  assert.ok(['keyword', 'merged'].includes(r.classifier), `classifier 异常: ${r.classifier}`)
  assert.ok(Array.isArray(r.tags) && r.tags.length > 0)
  assert.equal(r.tags[0], 'safety_limit', 'safety_limit 必须永远第一位')
})

test('classifier:keyword 与 routeKeyword() 完全一致（A/B 开关）', () => {
  for (const q of ['挖这个猩红石', '往左走10格', '海边钓鱼', '打克苏鲁之眼', '不是，我说的是造房子']) {
    const a = route(q, { classifier: 'keyword' })
    const b = routeKeyword(q)
    assert.deepEqual(a.tags, b.tags, `${q} 的 tags 不一致`)
    assert.equal(a.classifier, 'keyword')
  }
})

test('v2 只增不减：merged 的 tags 必然包含 keyword 的全部结论', () => {
  const qs = [
    '挖这个猩红石',
    '往左走10格',
    '海边钓鱼',
    '打克苏鲁之眼',
    '我这个坑跳得过去吗',
    '帮我造一间护士的房子',
    '这个方块放上去没反应，怎么回事',
    '回家',
  ]
  for (const q of qs) {
    const kw = routeKeyword(q).tags
    const merged = route(q).tags
    for (const tag of kw) {
      assert.ok(merged.includes(tag), `${q}: merged 丢了关键词的 ${tag}\n  kw=${kw}\n  merged=${merged}`)
    }
  }
})

test('「我能飞多高」「我这身装备能飞吗」必须保住 abilities（v2 会把它换掉）', () => {
  for (const q of ['我能飞多高', '我这身装备能飞吗']) {
    const r = route(q)
    assert.ok(r.tags.includes('abilities'), `${q} 丢了 abilities，实际 ${r.tags.join(',')}`)
  }
})

test('玩家纠错时 task_correct 仍是安全库之后第一位（v2 的库只能排在它后面）', () => {
  const r = route('不是，我说的是铺背景墙不是挖')
  const idx = r.tags.indexOf('task_correct')
  assert.ok(idx > 0, `task_correct 缺失: ${r.tags.join(',')}`)
  const before = r.tags.slice(0, idx)
  assert.deepEqual(before, ALWAYS, `task_correct 前面只能是 ${ALWAYS.join(',')}，实际 ${before.join(',')}`)
})

test('v2 弃权的说法，结果等于纯关键词', () => {
  for (const q of ['今天天气怎么样', '继续吧', '装备之后数值变了怎么办']) {
    const merged = route(q)
    const kw = routeKeyword(q)
    assert.deepEqual(merged.tags, kw.tags, `${q} 应当与关键词一致，实际 ${merged.tags} vs ${kw.tags}`)
  }
})

test('v2 补齐的库必须是合法库名（不出现 navigation 这类幻影库）', async () => {
  const { LIBRARIES } = await import('../src/kb-router.js')
  const names = new Set(Object.keys(LIBRARIES))
  const qs = ['打克苏鲁之眼', '这个坑跳得过去吗', '回家', '海边钓鱼', '我这身装备能飞吗']
  for (const q of qs) {
    for (const tag of route(q).tags) {
      assert.ok(names.has(tag), `${q} 出现了非法库名 ${tag}`)
    }
  }
})

test('分类器抛错不影响路由（tryClassify 兜底）', () => {
  // 传空值/怪值不能崩
  for (const v of ['', null, undefined, 123, {}]) {
    const r = route(v)
    assert.ok(Array.isArray(r.tags))
    assert.equal(r.tags[0], 'safety_limit')
  }
})