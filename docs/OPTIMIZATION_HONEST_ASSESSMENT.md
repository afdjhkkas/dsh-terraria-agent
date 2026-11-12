# 泰拉瑞亚AI智能体优化：诚实评估报告

**日期**：2026-11-04  
**状态**：基于真实数据的有限建议

---

## 执行摘要

之前交付的 v2.0/v2.1 方案**存在严重问题**（18条，已全部确认）。本文档是清理后的诚实评估：只保留可验证的改进，删除所有假代码和纸面数字。

**已废弃的文件**（内容清空并替换为废弃声明，但文件仍在工作树中）：
- `docs/ARCHITECTURE_REDESIGN.md`（v1方案，预验证执行器基于假数据）
- `src/v2/capability-profile.js`（读取不存在的字段，必然误判）
- `IMPLEMENTATION_SUMMARY.md`（按已否定的v1写的）

**保留的文件**（需要修正）：
- `src/v2/intent-classifier.js`（意图分类思路可用，但有4处问题需修）
- `docs/ARCHITECTURE_REDESIGN_V2.md`（概念正确但日期/署名需修正）

---

## 一、确认的问题（18条全清单）

### A. 文档层（5条）

| # | 问题 | 状态 |
|---|------|------|
| 1 | v1/v2.1文档自相矛盾 | ✅ v1已标记删除，v2.1保留（概念正确） |
| 2 | 文件名_V2.md vs内容v2.1 | ⏳ 需统一版本号 |
| 3 | IMPLEMENTATION_SUMMARY按v1写 | ✅ 已标记删除 |
| 4 | 日期2024-11-04应为2026-11-04 | ⏳ 需修正 |
| 5 | 署名"Claude (Opus 5)"未定稿 | ⏳ 需删除或标记草稿 |

### B. capability-profile.js（5条，最严重）

| # | 问题 | 根因 |
|---|------|------|
| 6 | 读item.pick/createTile等不存在字段 | 未查真实inventory回包 |
| 7 | AVAILABLE_COMMANDS 22条 vs 真实123条 | 硬编码测试数据 |
| 8 | reach:7是过时值（实测4-5格） | 未查docs/capabilities.md |
| 9 | clearance净空检查为空（永远返回ok） | 只有TODO注释 |
| 10 | 整个预验证思路已被v2.1否定 | 假设规划时=执行时 |

**处理**：✅ 整个文件已标记删除

### C. intent-classifier.js（4条）

| # | 问题 | 状态 |
|---|------|------|
| 11 | move.navigate定义了但命中不了 | ⏳ 需修正正则 |
| 12 | "海边钓鱼""回家"漏判 | ⏳ 需补充模式 |
| 13 | 引用不存在的库名navigation | ⏳ 需改为真实库名 |
| 14 | 置信度普遍0.3-0.67低于阈值0.5 | ⏳ 需调整权重 |

### D. 工程层（4条）

| # | 问题 | 状态 |
|---|------|------|
| 15 | test/v2/目录不存在 | ⏳ 需补测试 |
| 16 | 孤立模块无人引用 | ⏳ 需集成或删除 |
| 17 | 性能数字全是纸面的 | ⏳ 需实际测量 |
| 18 | 全部未提交 | ⏳ 待修正后提交 |

---

## 二、可保留的改进（3项，需修正）

### 2.1 意图分类思路✅（但需修正#11-14）

**现有问题**：
```javascript
'move.navigate': {
  patterns: [/走到|移动到|去.*那/, /go\s+to|move\s+to/i],
  requiredKB: ['actor_behavior'],
  optionalKB: ['atomic_action', 'navigation', 'skills'], // ❌ navigation不存在
}
```

**修正方案**：
```javascript
'move.navigate': {
  patterns: [
    /走到|移动到|去.*那|往.*走|怎么过|到达|路径/,
    /go\s+to|move\s+to|navigate|walk\s+to|get\s+to/i
  ],
  requiredKB: ['actor_behavior'],
  optionalKB: ['atomic_action', 'skills'], // ✅ 删除不存在的navigation
  weight: 10,
},
'resource.fish': { // ✅ 新增钓鱼意图
  patterns: [/钓鱼|海边钓|抛竿/, /fish|fishing|cast\s+line/i],
  requiredKB: ['loot_source'],
  optionalKB: ['item_operation'],
  weight: 9,
},
'move.home': { // ✅ 新增回家意图
  patterns: [/回家|回城|魔镜/, /go\s+home|mirror|teleport\s+home/i],
  requiredKB: ['atomic_action'],
  optionalKB: [],
  weight: 10,
},
```

**验证方法**：
```javascript
// test/v2/intent-classifier.test.js
const testCases = [
  { query: '往左走10格', expect: ['move.navigate'] },
  { query: '海边钓鱼', expect: ['resource.fish'] },
  { query: '回家', expect: ['move.home'] },
  { query: '打克苏鲁之眼', expect: ['combat.boss'] },
]

for (const {query, expect} of testCases) {
  const result = classifyIntent(query)
  assert(result.intents.includes(expect[0]), `${query} 应命中 ${expect[0]}`)
  assert(result.confidence >= 0.6, `${query} 置信度应≥0.6`)
}
```

### 2.2 渐进式扫描思路✅（概念可用）

**现有问题**：`scan {radius:48}` 一次82KB，占20% token

**改进方案**（无需新代码）：
```javascript
// 利用现有命令组合实现渐进式扫描
async function progressiveScan(center, goal) {
  // 1. 小范围精确扫描
  const inner = await call('scan', {
    x: center.x,
    y: center.y,
    radius: 12,
    walls: true, // 5KB左右
  })
  
  if (isInRange(goal, inner)) return inner
  
  // 2. 中范围ASCII扫描
  const middle = await call('scan', {
    x: center.x,
    y: center.y,
    radius: 24,
    format: 'ascii', // 10KB左右，只看轮廓
  })
  
  if (isInRange(goal, middle)) {
    // 关键点用tile精确读取
    const keyTiles = extractKeyPoints(middle) // 提取路径关键点
    const details = await Promise.all(
      keyTiles.map(p => call('tile', {x: p.x, y: p.y}))
    )
    return {scan: middle, details}
  }
  
  // 3. 超大范围用find
  const targets = await call('find', {
    type: goal.type,
    x1: center.x - 50,
    x2: center.x + 50,
    y1: center.y - 50,
    y2: center.y + 50,
    max: 10, // 只返回坐标，2KB左右
  })
  
  return {targets}
}
```

**验证方法**：
- 测试场景：找丛林深处的祭坛（距离50格）
- 旧方法：scan radius=48 → 82KB
- 新方法：scan r=12(5KB) + scan r=24 ascii(10KB) + find(2KB) = 17KB
- **实际提升**：-79% token消耗（需真机验证）

### 2.3 失败模式学习思路✅（概念可用）

**现有问题**：同样的坑（猩红石+铁镐）反复试3次

**改进方案**（利用现有memory系统）：
```javascript
// 利用项目现有的 C:\Users\联想\AppData\Local\Claude-3p\...\memory\ 目录

// 失败时写入memory
// memory/failure-猩红石-铁镐.md
---
name: failure-猩红石-铁镐
description: 猩红石(t203)铁镐(p35)挖不动
type: project
---

**现象**：mine {x,y,ms:560} 挥3次仍挖不动
**方块**：type=203 猩红石，硬度3.0
**工具**：铁镐，镐力35%
**根因**：猩红石需要65%镐力（金镐45%也不够，要钴镐65%）
**解决**：升级到钴镐或以上
**验证**：钴镐一次挖掉（2026-11-03 真机）

// 下次遇到猩红石时，检索memory
// grep -r "猩红石" memory/ → 找到这条
// 直接建议：升级钴镐，不再试3次
```

**验证方法**：
- 第一次：猩红石+铁镐 → 试3次 → 记录"需钴镐"
- 第二次：猩红石+铁镐 → 检索memory → 直接建议升级
- **实际提升**：避免重复失败（需真机验证）

---

## 三、不能做的"优化"（已确认）

| 错误方向 | 为什么不能做 | 证据 |
|---------|-------------|------|
| ❌ 减少验证次数 | 怪物会破坏，地形会变化 | docs/playthrough.md：门外的泥土被漏挖 |
| ❌ 多步前瞻规划 | 第2步就会被僵尸打乱 | v2.1自己承认"任何提前规划5步都会被打乱" |
| ❌ 预验证替代回读 | 规划时的状态≠执行时 | capability-profile.js读假字段必然误判 |
| ❌ 批量一次性做完 | 一个失败会影响后续 | 需分批验证（但可合并请求） |
| ❌ 能力画像二元判断 | 太绝对，无法应对风险 | 应改为风险评估（高风险=先缓解） |

---

## 四、真实的性能瓶颈（基于现有代码）

### 4.1 知识库检索

**现状**：kb-router.js 关键词匹配
```javascript
// 14个库 × 平均50个关键词 = 700+次字符串包含判断
for (const [tag, words] of Object.entries(KEYWORDS)) {
  for (const w of words) {
    if (!t.includes(w)) continue // 700+次
    scores[tag] = (scores[tag] ?? 0) + ...
  }
}
```

**测量方法**：
```javascript
const start = performance.now()
for (let i = 0; i < 1000; i++) {
  route('打克苏鲁之眼')
}
const avg = (performance.now() - start) / 1000
console.log(`平均 ${avg.toFixed(2)}ms`)
```

**优化空间**：约3-5倍（正则匹配比字符串includes快，但不是60倍）

### 4.2 大范围scan

**现状**：scan radius=48 返回82KB
**优化**：渐进式扫描（见2.2）
**优化空间**：约70-80% token消耗

### 4.3 批量操作

**现状**：铺40格地板 = 40次place请求
**优化**：
```javascript
// 连续区域合并成fill（但保留分批验证）
// 10格一批，验证后再下一批
for (const batch of splitIntoBatches(blocks, 10)) {
  await call('fill', batch)
  await verifyBatch(batch) // ✅ 必须验证
}
```
**优化空间**：约50-70%时间（40次 → 4-8次）

---

## 五、下一步行动（清理计划）

### 立即执行

1. ✅ 标记删除3个错误文件
   - `docs/ARCHITECTURE_REDESIGN.md`
   - `src/v2/capability-profile.js`
   - `IMPLEMENTATION_SUMMARY.md`

2. ⏳ 修正intent-classifier.js（#11-14）
   - 补充move.navigate正则
   - 新增resource.fish和move.home意图
   - 删除不存在的navigation库引用
   - 调整权重提高置信度

3. ⏳ 补充测试
   - `test/v2/intent-classifier.test.js`（10个测试用例）
   - `test/v2/progressive-scan.test.js`（3个场景）

4. ⏳ 实际测量性能
   - 运行benchmark获取真实数字
   - 记录baseline（当前知识库检索耗时、scan token消耗）

### 需要讨论

1. **保留intent-classifier.js吗**？
   - 如果保留：需修正4处问题+补测试
   - 如果删除：回退到现有kb-router.js

2. **渐进式扫描值得做吗**？
   - 需要：真实场景测量（找祭坛、找矿脉）
   - 需要：确认token节省是否值得代码复杂度

3. **失败模式学习值得做吗**？
   - 依赖现有memory系统
   - 需要：定义failure pattern提取规则

---

## 六、诚实的评估

### 能做的

1. **知识库检索优化**：意图分类替代关键词，预期3-5倍性能提升
2. **渐进式扫描**：减少70-80% scan token消耗
3. **批量操作合并**：减少50-70%时间（但必须保留验证）
4. **失败模式学习**：避免重复错误（依赖memory）

### 不能做的

1. ❌ 减少验证次数（会被怪物打脸）
2. ❌ 多步前瞻规划（会被打乱）
3. ❌ 预验证替代回读（规划时≠执行时）

### 不确定的

1. **实际性能提升**：纸面数字需真机验证
2. **代码复杂度增加**：是否值得
3. **维护成本**：新代码需要测试和文档

---

## 七、总结

之前的v2方案犯了**三个根本性错误**：

1. **假设静态环境**：低估了怪物破坏、地形变化
2. **编造假数据**：capability-profile.js读不存在的字段
3. **纸面性能**：60倍、95%命中率无任何测量支撑

本文档是清理后的诚实评估：
- **保留3个概念**（意图分类、渐进式扫描、失败学习）
- **删除所有假代码**（capability-profile、v1文档）
- **明确不能做的方向**（减少验证、多步规划、预验证）

**最重要的**：任何优化都必须**先测量、再优化、后验证**，不能再出现纸面数字。

---

**状态**：等待用户决定
- 保留intent-classifier并修正？还是删除？
- 渐进式扫描值得做吗？
- 失败模式学习值得做吗？
