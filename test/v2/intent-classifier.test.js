/**
 * intent-classifier.js 测试
 *
 * 运行方式：
 *   npm test              ← 全量，包含这里（推荐）
 *   npm run test:v2       ← 只跑 test/v2/ 下所有测试
 *   node --test test/v2/intent-classifier.test.js  ← 单文件
 *
 * ⚠️  node --test test/v2/（传目录）会报 fail，Node 会尝试把目录名当文件读。
 *    用上面三种方式之一。
 *
 * 覆盖：
 * - #11 move.navigate 现在能命中"往左走10格"
 * - #12 resource.fish / move.home 新增漏判意图
 * - #13 不再引用不存在的库名 navigation
 * - #14 threshold=8，所有 case 的 confidence 应 >= 0.53
 */

import { describe, it } from 'node:test'
import assert from 'node:assert/strict'
import { classifyIntent, INTENT_PATTERNS } from '../../src/v2/intent-classifier.js'
import { LIBRARIES } from '../../src/kb-router.js'

// 合法库名集合（用来检验 #13：不得引用不存在的库）
const VALID_KB_NAMES = new Set(Object.keys(LIBRARIES))

// ============ 静态检查 ============

describe('INTENT_PATTERNS 静态合法性', () => {
  it('所有 requiredKB 和 optionalKB 只含合法库名', () => {
    for (const [intent, config] of Object.entries(INTENT_PATTERNS)) {
      for (const kb of [...config.requiredKB, ...config.optionalKB]) {
        assert(
          VALID_KB_NAMES.has(kb),
          `意图 ${intent} 引用了不存在的库名: "${kb}"（合法库名见 kb-router.js LIBRARIES）`
        )
      }
    }
  })

  it('所有 weight 在 [8, 15] 范围内（低于阈值的意图没有意义）', () => {
    for (const [intent, config] of Object.entries(INTENT_PATTERNS)) {
      assert(
        config.weight >= 8 && config.weight <= 15,
        `意图 ${intent} 的 weight=${config.weight} 超出范围 [8,15]`
      )
    }
  })

  it('所有 requiredKB 非空（空的 required 意图没有意义）', () => {
    for (const [intent, config] of Object.entries(INTENT_PATTERNS)) {
      assert(
        config.requiredKB.length > 0,
        `意图 ${intent} 的 requiredKB 为空`
      )
    }
  })
})

// ============ #11 修正：move.navigate ============

describe('#11 修正：move.navigate 正则覆盖', () => {
  const cases = [
    '往左走10格',
    '往右走5格',
    '向左走',
    '向右走',
    '走到那边',
    '移动到x=2100',
    '过去',
  ]

  for (const query of cases) {
    it(`"${query}" 应命中 move.navigate`, () => {
      const result = classifyIntent(query)
      assert(
        result.intents.includes('move.navigate'),
        `"${query}" → intents=${JSON.stringify(result.intents)}，method=${result.method}`
      )
    })
  }
})

// ============ #12 修正：resource.fish / move.home ============

describe('#12 修正：新增意图覆盖', () => {
  it('"海边钓鱼" 应命中 resource.fish', () => {
    const result = classifyIntent('海边钓鱼')
    assert(
      result.intents.includes('resource.fish'),
      `intents=${JSON.stringify(result.intents)}，method=${result.method}`
    )
  })

  it('"抛竿" 应命中 resource.fish', () => {
    const result = classifyIntent('抛竿')
    assert(
      result.intents.includes('resource.fish'),
      `intents=${JSON.stringify(result.intents)}，method=${result.method}`
    )
  })

  it('"回家" 应命中 move.home', () => {
    const result = classifyIntent('回家')
    assert(
      result.intents.includes('move.home'),
      `intents=${JSON.stringify(result.intents)}，method=${result.method}`
    )
  })

  it('"回城" 应命中 move.home', () => {
    const result = classifyIntent('回城')
    assert(
      result.intents.includes('move.home'),
      `intents=${JSON.stringify(result.intents)}，method=${result.method}`
    )
  })
})

// ============ #13 修正：requiredKB 库名合法性（已由静态检查覆盖，这里加运行时检查）============

describe('#13 修正：classifyIntent 返回的 knowledgeBases 全是合法名', () => {
  const queries = [
    '卡住了',
    '前面是悬崖',
    '爬这个坡',
    '被墙挡住',
  ]

  for (const query of queries) {
    it(`"${query}" 返回的 knowledgeBases 全合法`, () => {
      const result = classifyIntent(query)
      for (const kb of result.knowledgeBases) {
        assert(
          VALID_KB_NAMES.has(kb),
          `"${query}" 返回了不存在的库名: "${kb}"`
        )
      }
    })
  }
})

// ============ #14 修正：置信度和回退行为 ============

describe('#14 修正：threshold=8 后的置信度', () => {
  it('常见查询走意图分类而非 keyword_fallback', () => {
    const shouldUseIntentClassifier = [
      '打克苏鲁之眼',
      '挖铁矿',
      '盖房子',
      '往左走10格',
      '海边钓鱼',
      '回家',
      '不是，我说的是别的',
    ]

    for (const query of shouldUseIntentClassifier) {
      const result = classifyIntent(query)
      assert.notEqual(
        result.method,
        'keyword_fallback',
        `"${query}" 应走意图分类，但走了 keyword_fallback（confidence=${result.confidence.toFixed(2)}，intents=${JSON.stringify(result.intents)}）`
      )
    }
  })

  it('纯英文垃圾输入走 keyword_fallback', () => {
    const result = classifyIntent('zzzzz gibberish xyz')
    // 无法命中任何意图，应回退
    assert.equal(
      result.method,
      'keyword_fallback',
      `预期 keyword_fallback，实际 method=${result.method}`
    )
  })

  it('空输入返回 method:empty', () => {
    const result = classifyIntent('')
    assert.equal(result.method, 'empty')
    assert.deepEqual(result.intents, [])
  })
})

// ============ boss 三件套（关键行为验证）============

describe('boss 三件套：打 boss 必带 progression_boss+loot_source+item_operation', () => {
  const bossCases = [
    '打克苏鲁之眼',
    '制作可疑眼球',
    'boss顺序是什么',
    '召唤boss',
  ]

  for (const query of bossCases) {
    it(`"${query}" 必带三件套`, () => {
      const result = classifyIntent(query)
      for (const kb of ['progression_boss', 'loot_source', 'item_operation']) {
        assert(
          result.knowledgeBases.includes(kb),
          `"${query}" 缺少 ${kb}（knowledgeBases=${JSON.stringify(result.knowledgeBases)}）`
        )
      }
    })
  }
})

// ============ 纠正类最高优先级 ============

describe('纠正类应排在第一位', () => {
  it('"不是，我说的是挖矿" 纠正类应排第一', () => {
    const result = classifyIntent('不是，我说的是挖矿')
    assert(
      result.intents[0] === 'correct.rule',
      `intents[0]=${result.intents[0]}，预期 correct.rule`
    )
    assert(
      result.knowledgeBases.includes('task_correct'),
      `缺少 task_correct，knowledgeBases=${JSON.stringify(result.knowledgeBases)}`
    )
  })
})
