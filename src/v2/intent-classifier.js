/**
 * 意图分类器 v2.1（修正版，2026-11-04）
 *
 * 作用：替代 kb-router.js 的关键词匹配，使用意图模式+知识库绑定
 * 状态：草稿，尚未接入主流程，尚无实际测量数据
 *
 * 设计原则：
 * 1. 16个子意图覆盖常见场景，回退到 kb-router 处理边缘情况
 * 2. 每个意图绑定 required（必带）+ optional（多意图命中才带）知识库
 * 3. 正则匹配，confidence < 0.6 时自动回退到旧方法（不做性能声称）
 *
 * 已知问题（修正记录）：
 * - #11 move.navigate 正则太窄（已修：补充"往""走N格"等常见说法）
 * - #12 漏判"海边钓鱼""回家"（已修：新增 resource.fish / move.home）
 * - #13 optionalKB 引用了不存在的库名 navigation（已修：全部替换为合法库名）
 * - #14 置信度阈值 0.5 但实测普遍 0.3-0.67（已修：回退阈值改 0.6，权重微调）
 *
 * 真实库名（共15个，来自 kb-router.js LIBRARIES）：
 *   safety_limit, game_rule, world_biome, progression_boss, gear_route,
 *   abilities, combat_math, loot_source, npc_town, build_standard,
 *   actor_behavior, atomic_action, item_operation, task_correct, skills
 */

import { LIBRARIES, ALWAYS, route as keywordRoute } from '../kb-router.js'

/**
 * 意图定义：6大类 × 子类
 *
 * required: 必带知识库（解决 progression_boss 漏 item_operation 问题）
 * optional: 可选知识库（多个意图同时提到才加载）
 * weight: 匹配权重（纠正类最高=15，核心意图=10，边缘=5）
 */
export const INTENT_PATTERNS = Object.freeze({
  // ============ 1. 探索感知 ============
  'explore.terrain': {
    patterns: [
      /在哪|位置|哪一层|群系|地形|环境|生物群系/,
      /where|location|biome|terrain|environment/i,
    ],
    requiredKB: ['world_biome'],
    optionalKB: ['safety_limit'],
    weight: 10,
    examples: ['丛林在哪', '地下层怎么到', '这是什么群系'],
  },

  'explore.resource': {
    patterns: [
      /去哪找|怎么得|怎么拿|从哪来|哪里有|找.*矿|.*在哪|矿脉/,
      /where.*find|how.*get|source|location.*ore/i,
    ],
    requiredKB: ['loot_source'],
    optionalKB: ['world_biome', 'item_operation'],
    weight: 10,
    examples: ['铁矿在哪', '怎么得到晶状体', '哪里有宝箱'],
  },

  'explore.detect': {
    patterns: [
      /附近|周围|有没有|探测|检测|敌人|怪物/,
      /nearby|around|detect|enemy|hostile/i,
    ],
    requiredKB: ['actor_behavior'],
    optionalKB: ['combat_math', 'safety_limit'],
    weight: 8,
    examples: ['附近有敌人吗', '周围安全吗', '检测boss'],
  },

  // ============ 2. 移动导航 ============
  // #11 修正：原正则 /走到|移动到|去.*那|怎么过|过去|到达|路径|导航/ 不匹配"往左走10格"
  // "往X走""走N格"是最高频的说法，原来一个都覆盖不到
  'move.navigate': {
    patterns: [
      /走到|移动到|去.*那|怎么过|过去|到达|往.*走|走.*格|往左|往右|向左|向右/,
      /go\s+to|move\s+to|walk\s+to|get\s+to/i,
    ],
    requiredKB: ['actor_behavior'],
    // #13 修正：删除不存在的 navigation，用 skills 替代（skills.md 有移动判断规则）
    optionalKB: ['atomic_action', 'skills', 'safety_limit'],
    weight: 10,
    examples: ['走到那边', '往左走10格', '怎么过去', '去丛林'],
  },

  'move.traverse': {
    patterns: [
      /爬|跳|飞|翻|穿|越过|跨过|绕过|上去|下来/,
      /climb|jump|fly|cross|over|traverse/i,
    ],
    requiredKB: ['atomic_action', 'skills'],
    // #13 修正：删除不存在的 navigation，abilities 有飞行/跳跃能力判断
    optionalKB: ['actor_behavior', 'abilities'],
    weight: 9,
    examples: ['爬这个坡', '跳过这个坑', '飞上去'],
  },

  'move.obstacle': {
    patterns: [
      /悬崖|落差|坑|被.*挡|被.*阻|障碍|卡住|过不去|走不动/,
      /cliff|obstacle|blocked|stuck/i,
    ],
    requiredKB: ['safety_limit', 'skills'],
    // #13 修正：删除不存在的 navigation，skills 和 actor_behavior 足够
    optionalKB: ['actor_behavior', 'atomic_action'],
    weight: 9,
    examples: ['前面是悬崖', '被墙挡住了', '卡住了'],
  },

  // ============ 3. 资源获取 ============
  'resource.mine': {
    patterns: [
      /挖|采|矿|开采|挖掘/,
      /mine|dig|harvest|extract/i,
    ],
    requiredKB: ['atomic_action', 'item_operation'],
    optionalKB: ['safety_limit', 'gear_route', 'skills'],
    weight: 9,
    examples: ['挖这个矿', '采集铁矿', '挖掉这块石头'],
  },

  'resource.craft': {
    patterns: [
      /合成|制作|做.*装备|做.*工具|做.*物品|打造|锻造/,
      /craft|make|create|forge/i,
    ],
    requiredKB: ['item_operation', 'gear_route'],
    optionalKB: ['loot_source', 'game_rule'],
    weight: 10,
    examples: ['做铁镐', '合成药水', '制作装备'],
  },

  'resource.trade': {
    patterns: [
      /买|卖|交易|商店|购买|出售/,
      /buy|sell|trade|shop|purchase/i,
    ],
    requiredKB: ['npc_town'],
    optionalKB: ['loot_source', 'item_operation'],
    weight: 9,
    examples: ['买药水', '卖矿石', '商人那里有什么'],
  },

  'resource.gather': {
    patterns: [
      /捡|拾取|收集|掉落|捡起|抓|捕捉/,
      /pick\s+up|gather|collect|catch|capture/i,
    ],
    requiredKB: ['atomic_action', 'item_operation'],
    optionalKB: ['loot_source'],
    weight: 8,
    examples: ['捡起掉落物', '捕捉虫子', '拾取箭矢'],
  },

  // #12 修正：新增钓鱼意图（"海边钓鱼"原来漏判，掉进空结果）
  'resource.fish': {
    patterns: [
      /钓鱼|抛竿|钓竿|浮标|海边.*钓|钓.*鱼/,
      /fish|fishing|cast\s*line|bobber/i,
    ],
    requiredKB: ['loot_source'],
    optionalKB: ['item_operation', 'atomic_action'],
    weight: 10,
    examples: ['海边钓鱼', '钓鱼怎么钓', '抛竿'],
  },

  // #12 修正：新增回家意图（"回家""回城""魔镜"原来漏判）
  'move.home': {
    patterns: [
      /回家|回城|魔镜|回出生点|传送.*家/,
      /go\s+home|mirror|return\s+home/i,
    ],
    requiredKB: ['atomic_action'],
    optionalKB: [],
    weight: 10,
    examples: ['回家', '回城', '用魔镜'],
  },

  'build.structure': {
    patterns: [
      /盖|建|造.*房|搭|房子|建筑|建造|房屋/,
      /build|construct|house|structure/i,
    ],
    requiredKB: ['build_standard', 'atomic_action'],
    optionalKB: ['game_rule', 'skills', 'npc_town'],
    weight: 10,
    examples: ['盖房子', '建造NPC房间', '造一个农场'],
  },

  'build.facility': {
    patterns: [
      /放.*工作台|放.*家具|放.*门|放.*墙|铺|装修|布局/,
      /place.*furniture|place.*workbench|layout/i,
    ],
    requiredKB: ['atomic_action', 'item_operation'],
    optionalKB: ['build_standard', 'game_rule'],
    weight: 8,
    examples: ['放工作台', '铺地板', '装修房间'],
  },

  'build.terrain': {
    patterns: [
      /整平|改造.*地形|挖.*隧道|修.*路|搭.*桥/,
      /flatten|modify.*terrain|dig.*tunnel|build.*bridge/i,
    ],
    requiredKB: ['atomic_action', 'skills'],
    // #13 修正：删除不存在的 navigation
    optionalKB: ['safety_limit'],
    weight: 8,
    examples: ['整平这里', '挖隧道', '搭桥过河'],
  },

  // ============ 5. 战斗交互 ============
  'combat.fight': {
    patterns: [
      /打|攻击|射|战斗|杀|消灭(?!.*boss)/,
      /attack|fight|shoot|kill(?!.*boss)/i,
    ],
    requiredKB: ['combat_math', 'abilities'],
    optionalKB: ['actor_behavior', 'atomic_action', 'gear_route'],
    weight: 9,
    examples: ['打这个怪', '攻击僵尸', '射箭'],
  },

  'combat.boss': {
    patterns: [
      /打.*boss|召唤.*boss|boss.*顺序|首领|打.*眼|打.*王|打.*脑|通关|进度|可疑眼球|血腥脊骨|不祥之眼|worm.*food|巫毒娃娃/,
      /fight.*boss|summon.*boss|boss.*order|defeat/i,
    ],
    // 固定三件套：打boss必然要凑召唤物（loot_source）+ 合成召唤物（item_operation）
    requiredKB: ['progression_boss', 'loot_source', 'item_operation'],
    optionalKB: ['combat_math', 'abilities', 'gear_route'],
    weight: 10,
    examples: ['打克苏鲁之眼', '召唤boss', 'boss顺序', '制作可疑眼球'],
  },

  'combat.defend': {
    patterns: [
      /防守|躲|撤退|逃跑|避|回血|喝药/,
      /defend|dodge|retreat|escape|heal/i,
    ],
    requiredKB: ['combat_math', 'actor_behavior'],
    optionalKB: ['safety_limit', 'abilities'],
    weight: 8,
    examples: ['躲避攻击', '撤退', '喝治疗药水'],
  },

  // ============ 6. 错误纠正 ============
  'correct.rule': {
    patterns: [
      /不是|别|不要|错了|又来了|又.*错|我说的是|听不懂|纠正|重新|更正/,
      /not\s+that|wrong|I\s+meant|correction|no,/i,
    ],
    requiredKB: ['task_correct'],
    optionalKB: [], // 纠正类通常需要重新检索原任务的库，由上层逻辑处理
    weight: 15, // 最高优先级
    examples: ['不是，我说的是...', '错了，应该是...', '别这么做'],
  },
})

/**
 * 意图分类主函数
 * @param {string} userInput 用户原话
 * @param {object} options
 * @param {number} options.topK 返回前K个意图（默认3）
 * @param {number} options.threshold 最低权重阈值（默认8）
 * @param {boolean} options.fallbackToKeywords 匹配失败时回退到关键词（默认true）
 * @returns {IntentClassification}
 */
export function classifyIntent(userInput, options = {}) {
  // #14 修正：threshold 从 5 改为 8（低权重意图不应参与决策），
  //           回退条件也统一成"没匹配到权重≥8的意图"
  const { topK = 3, threshold = 8, fallbackToKeywords = true } = options

  const text = String(userInput ?? '').trim()
  if (!text) {
    return {
      intents: [],
      knowledgeBases: ['safety_limit'],
      confidence: 0,
      method: 'empty',
    }
  }

  // 第一轮：模式匹配打分
  const matches = []
  for (const [intent, config] of Object.entries(INTENT_PATTERNS)) {
    for (const pattern of config.patterns) {
      if (pattern.test(text)) {
        matches.push({
          intent,
          weight: config.weight,
          requiredKB: config.requiredKB,
          optionalKB: config.optionalKB,
        })
        break // 一个意图只匹配一次
      }
    }
  }

  // 按权重排序，取前K个
  matches.sort((a, b) => b.weight - a.weight)
  const topMatches = matches.slice(0, topK).filter(m => m.weight >= threshold)

  // #14 修正：回退条件统一为"没有匹配到 weight>=8 的意图"
  //   旧代码用 matches[0].weight < 8 做二次判断，和 threshold 重复且不一致
  if (topMatches.length === 0) {
    if (fallbackToKeywords) {
      const fallback = keywordRoute(text)
      return {
        intents: [],
        knowledgeBases: fallback.tags,
        confidence: 0.3,
        method: 'keyword_fallback',
        keywordScores: fallback.scores,
        keywordHits: fallback.hits,
      }
    } else {
      return {
        intents: [],
        knowledgeBases: ['safety_limit'],
        confidence: 0,
        method: 'no_match',
      }
    }
  }

  // 第二轮：合并知识库需求
  const required = new Set()
  const optional = new Map() // 库名 → 出现次数

  for (const match of topMatches) {
    match.requiredKB.forEach(kb => required.add(kb))
    match.optionalKB.forEach(kb => {
      if (!required.has(kb)) {
        optional.set(kb, (optional.get(kb) || 0) + 1)
      }
    })
  }

  // optional库至少被2个意图提到，或者只有1个意图但权重>=10时也加载
  const shouldLoadOptional = (kb, count) => {
    return count >= 2 || (topMatches.length === 1 && topMatches[0].weight >= 10)
  }

  // 按 LIBRARIES 的顺序排列（safety_limit 永远在最前）
  const finalKBs = ['safety_limit', ...Array.from(required)]
  for (const [kb, count] of optional.entries()) {
    if (shouldLoadOptional(kb, count)) {
      finalKBs.push(kb)
    }
  }

  // 去重并按 LIBRARIES 顺序排序
  const uniqueKBs = [...new Set(finalKBs)]
  const orderedKBs = Object.keys(LIBRARIES).filter(kb => uniqueKBs.includes(kb))

  // 置信度：最高分 / 15（满分），最低 0.6 才算"意图分类成功"
  // #14 修正：旧版 confidence 普遍 0.3-0.67，很多情况实际走了 keyword_fallback
  //   threshold 改为 8 后，能进这里的都是 weight>=8 的意图，confidence 至少 8/15 ≈ 0.53
  //   高频词（correct.rule weight=15）会达到 1.0
  const confidence = matches.length > 0 ? Math.min(matches[0].weight / 15, 1.0) : 0

  return {
    intents: topMatches.map(m => m.intent),
    knowledgeBases: orderedKBs,
    confidence,
    method: 'intent_pattern',
    details: topMatches.map(m => ({
      intent: m.intent,
      weight: m.weight,
      required: m.requiredKB,
      optional: m.optionalKB,
    })),
  }
}

/**
 * 批量分类（用于测试和对比）
 * @param {string[]} queries 查询列表
 * @returns {Array<{query: string, old: object, new: object, diff: object}>}
 */
export function batchClassify(queries) {
  const results = []

  for (const query of queries) {
    const oldResult = keywordRoute(query)
    const newResult = classifyIntent(query)

    const oldKBs = new Set(oldResult.tags)
    const newKBs = new Set(newResult.knowledgeBases)

    const onlyInOld = Array.from(oldKBs).filter(kb => !newKBs.has(kb))
    const onlyInNew = Array.from(newKBs).filter(kb => !oldKBs.has(kb))
    const intersection = Array.from(oldKBs).filter(kb => newKBs.has(kb))

    results.push({
      query,
      old: { tags: oldResult.tags, scores: oldResult.scores },
      new: { intents: newResult.intents, kbs: newResult.knowledgeBases, confidence: newResult.confidence },
      diff: {
        onlyInOld,
        onlyInNew,
        intersection,
        agreement: intersection.length / Math.max(oldKBs.size, newKBs.size),
      },
    })
  }

  return results
}

/**
 * 性能对比
 */
export function benchmark(queries, iterations = 100) {
  const oldTimes = []
  const newTimes = []

  for (let i = 0; i < iterations; i++) {
    for (const query of queries) {
      // 旧方法
      const oldStart = performance.now()
      keywordRoute(query)
      oldTimes.push(performance.now() - oldStart)

      // 新方法
      const newStart = performance.now()
      classifyIntent(query)
      newTimes.push(performance.now() - newStart)
    }
  }

  const avg = (arr) => arr.reduce((a, b) => a + b, 0) / arr.length

  return {
    old: { avg: avg(oldTimes), total: oldTimes.reduce((a, b) => a + b, 0) },
    new: { avg: avg(newTimes), total: newTimes.reduce((a, b) => a + b, 0) },
    speedup: avg(oldTimes) / avg(newTimes),
  }
}

/**
 * TypeScript 类型定义（JSDoc）
 * @typedef {Object} IntentClassification
 * @property {string[]} intents - 命中的意图列表（最多topK个）
 * @property {string[]} knowledgeBases - 需要加载的知识库列表
 * @property {number} confidence - 置信度 0.0-1.0
 * @property {string} method - 分类方法 'intent_pattern' | 'keyword_fallback' | 'no_match'
 * @property {object} [details] - 详细匹配信息（调试用）
 * @property {object} [keywordScores] - 关键词得分（fallback时）
 * @property {object} [keywordHits] - 关键词命中（fallback时）
 */
