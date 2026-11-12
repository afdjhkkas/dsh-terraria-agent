# 泰拉瑞亚AI智能体架构优化方案 v2.1（修正版）

## 重要修正说明

**原方案（v2.0）的问题**：
- 误把"多次验证"当成冗余，试图减少回读次数
- 低估了游戏动态性：怪物破坏、地形塌陷、水流回灌、随机刷怪
- 预验证执行器假设"规划时看到的地形 = 执行时的地形"，这在泰拉瑞亚里**不成立**

**核心认知纠正**：
> 泰拉瑞亚是**实时动态环境**，不是静态棋盘。任何"提前规划5步"的方案都会被第2步的僵尸打乱。
> **正确的优化方向**：不是减少验证，而是让验证更快、更准、更智能。

---

## 一、真正的瓶颈是什么（重新分析）

### 1.1 保留的核心机制（这些是对的）

| 机制 | 为什么必须保留 | 真实案例 |
|------|---------------|---------|
| **回读验证** | 命令成功 ≠ 效果发生 | `mine` 回 `ok:true` 但格子还在（被怪推开了） |
| **轮询确认** | 异步执行需要等待 | 挥镐动画 560ms，但 `ItemCheck` 要 2-3 帧后才结算 |
| **异常重试** | 单次失败可能是偶然 | 第1次挥镐被僵尸打断，第2次才挖掉 |
| **卡点重检索** | 地形变化需要重新判断 | 规划"走过去"，怪把桥砸了，必须重新找路 |
| **元自检5维** | 提前发现不可做的事 | 没镐子就别规划"挖50格隧道" |

### 1.2 真正的瓶颈（可以优化的）

| 问题 | 根因 | 影响 | 正确的优化方向 |
|------|------|------|----------------|
| **知识检索命中不稳定** | 关键词重叠误判 | 查到错误的知识 → 规划失败 → 重新检索 | ✅ 改进分类器（v2.0 的意图分类是对的） |
| **同一个知识反复查** | 无会话缓存 | 每次都重新加载 14 个库（70KB） | ✅ 知识缓存（v2.0 的缓存是对的） |
| **大范围 scan token 高** | radius=48 时 82KB | 一次 scan 吃掉 20% token 预算 | ✅ 渐进式扫描（新方案） |
| **批量操作串行发送** | 铺 40 格 = 40 次请求 | 网络延迟累积 6.4 秒 | ✅ 批量命令（但要保留单步验证） |
| **固定重试策略** | 所有失败都重试 3 次 | "够不着"也重试 3 次（白等） | ✅ 失败分类（立即失败 vs 可重试） |

**关键区别**：
- ❌ 错误优化：减少验证次数 → 会被怪物破坏打脸
- ✅ 正确优化：让每次验证更高效 → 不牺牲安全性

---

## 二、修正后的优化方案

### 2.1 知识检索层优化（保留 v2.0 方案）

**这部分是对的**：意图分类器 + 固定知识库绑定 + 会话缓存

**唯一修正**：增加"地形变化触发重检索"
```javascript
// src/v2/intent-classifier.js 新增
export function shouldReinspect(beforeState, afterState) {
  // 执行前后地形变化超过阈值 → 重新检索 navigation/safety_limit
  const terrainDiff = compareTerrainHash(beforeState.scan, afterState.scan)
  if (terrainDiff > 0.3) { // 30% 以上格子变化
    return {
      shouldReinspect: true,
      reason: 'terrain_changed',
      additionalKB: ['navigation', 'safety_limit'],
    }
  }
  return { shouldReinspect: false }
}
```

---

### 2.2 决策层优化（大幅修正）

#### 2.2.1 规划窗口从"5步前瞻"改为"1步+应急预案"

**错误假设**：能提前规划 5 步
**现实**：第 2 步可能出现僵尸、第 3 步桥被砸

**修正方案**：
```javascript
// src/v2/action-planner-v2.js
export class AdaptivePlanner {
  async planNextAction(goal, context) {
    // 1. 只规划当前这一步（不是5步）
    const action = await this.planSingleStep(context.player, goal, context.terrain)
    
    // 2. 但同时准备3种应急预案
    const contingencies = {
      'enemy_appears': this.prepareDefenseAction(context),
      'path_blocked': this.prepareDiveAction(context),
      'terrain_collapsed': this.prepareRollbackAction(context),
    }
    
    return {
      primary: action,              // 主动作
      contingencies,                // 应急预案（预先算好，触发时立即用）
      checkpoint: {                 // 检查点：执行后必须验证这些
        expectedPosition: action.expectedPos,
        expectedTerrain: context.terrain.hash,
        maxDeviation: 2,            // 允许偏差2格
      },
    }
  }
  
  async executeWithContingency(plan, context) {
    // 执行主动作
    const result = await this.execute(plan.primary)
    
    // 立即验证检查点
    const actualState = await this.readState()
    const deviation = this.compareState(plan.checkpoint, actualState)
    
    if (deviation.positionOff > plan.checkpoint.maxDeviation) {
      // 位置偏差过大（被怪推了？掉坑里了？）
      const reason = this.diagnoseDeviation(deviation)
      
      if (plan.contingencies[reason]) {
        // 有预案 → 立即执行，不重新规划
        return await this.execute(plan.contingencies[reason])
      } else {
        // 无预案 → 才重新规划
        return { needReplan: true, reason }
      }
    }
    
    return { ok: true, result }
  }
}
```

**关键改进**：
- 不假设"能提前规划多步"，而是"准备应急方案"
- 检查点不是"减少验证"，而是"验证得更精确"（期望位置 vs 实际位置）
- 偏差诊断 → 如果有预案就立即执行，而非重新走完整规划流程

---

#### 2.2.2 能力画像从"预验证"改为"风险评估"

**错误假设**：规划时的背包状态 = 执行时的背包状态
**现实**：怪可能打掉手上的镐、箱子可能被炸掉

**修正方案**：
```javascript
// src/v2/capability-profile-v2.js
export function assessRisk(intent, context) {
  // 不是"能不能做"，而是"做的风险有多大"
  const profile = INTENT_TO_PROFILE[intent]
  
  const risks = {
    toolLoss: 0,      // 工具丢失风险（0-1）
    interruption: 0,  // 被打断风险
    terrainChange: 0, // 地形变化风险
  }
  
  // 附近有敌人 → 被打断风险高
  if (context.nearbyNPCs.some(npc => npc.hostile)) {
    risks.interruption = 0.8
    risks.toolLoss = 0.3 // 可能被打掉物品
  }
  
  // 在液体附近 → 地形变化风险高
  if (context.terrain.nearLiquid) {
    risks.terrainChange = 0.6
  }
  
  // 计算综合风险
  const overallRisk = Math.max(...Object.values(risks))
  
  return {
    canAttempt: true, // 总是允许尝试（v2.0 的 matched:false 太绝对了）
    risk: overallRisk,
    mitigations: overallRisk > 0.5 ? generateMitigations(risks) : [],
    fallback: profile.fallbacks,
  }
}

function generateMitigations(risks) {
  const result = []
  if (risks.interruption > 0.5) {
    result.push({ action: 'clear_enemies_first', priority: 1 })
  }
  if (risks.terrainChange > 0.5) {
    result.push({ action: 'place_support_first', priority: 2 })
  }
  return result
}
```

**关键改进**：
- 从"二元判断"（能做/不能做）改为"风险评估"（风险等级 0-1）
- 高风险不是拒绝执行，而是"先缓解风险"（清怪、搭支撑）
- 保留 fallback，但只在"实际失败"时用，不在规划时预判

---

### 2.3 执行层优化（保留验证+优化性能）

#### 2.3.1 批量操作：合并请求但保留单步验证

**错误做法**（v2.0）：
```javascript
// 40格一次性发，一个失败全失败
batchPlace([...40个格子...]) → 一次fill命令
```

**正确做法**：
```javascript
// src/v2/batch-executor.js
export async function batchPlaceWithVerification(blocks, context) {
  // 1. 按连续区域分组（3-10格一批）
  const batches = groupContinuousBlocks(blocks, maxBatchSize: 10)
  
  const results = []
  for (const batch of batches) {
    // 2. 每批发一次fill
    const fillResult = await call('fill', {
      x1: batch.x1, y1: batch.y1,
      x2: batch.x2, y2: batch.y2,
      type: batch.type,
    })
    
    // 3. 立即验证这一批（不是等40格全做完）
    const verification = await verifyBatch(batch)
    
    if (!verification.ok) {
      // 这批失败了 → 记录失败原因，继续下一批
      results.push({
        batch,
        ok: false,
        reason: verification.reason,
        // 不要整批回滚！已经放上的格子可能挡住了怪
      })
      
      // 诊断失败原因
      if (verification.reason === 'support_destroyed') {
        // 支撑被破坏 → 先修复支撑再继续
        await repairSupport(batch)
      }
    } else {
      results.push({ batch, ok: true })
    }
  }
  
  return {
    total: blocks.length,
    succeeded: results.filter(r => r.ok).length,
    failed: results.filter(r => !r.ok),
  }
}
```

**关键改进**：
- 批量 ≠ 一次性做完，而是"连续区域合并 + 分批验证"
- 一批失败不影响其他批（已放的格子可能挡怪，不能回滚）
- 失败诊断 → 修复问题 → 继续，而非"失败就停"

---

#### 2.3.2 渐进式扫描：替代大范围 scan

**问题**：`scan {radius:48}` 一次 82KB，吃掉 20% token
**根因**：需要大范围扫描找矿脉/找路径

**方案**：多层扫描策略
```javascript
// src/v2/progressive-scan.js
export async function progressiveScan(center, goal) {
  // 第1层：小范围高精度（radius=12，5KB）
  const inner = await scan({ x: center.x, y: center.y, radius: 12, walls: true })
  
  // 如果目标在内圈 → 直接返回
  if (isInBounds(goal, inner.bounds)) {
    return { scan: inner, layers: 1, size: '5KB' }
  }
  
  // 第2层：中范围ASCII（radius=24，10KB）
  const middle = await scan({
    x: center.x,
    y: center.y,
    radius: 24,
    format: 'ascii', // ASCII 格式，只看地形轮廓
  })
  
  if (isInBounds(goal, middle.bounds)) {
    // 用 ASCII 找到大致路径，再用 tile 精确读取关键格
    const keyPoints = extractKeyPoints(middle)
    const details = await Promise.all(
      keyPoints.map(p => tile({ x: p.x, y: p.y }))
    )
    return { scan: middle, details, layers: 2, size: '15KB' }
  }
  
  // 第3层：超大范围（radius=48，但用 find 而非 scan）
  // find 只返回匹配的坐标，不返回整片地图
  const farTargets = await find({
    type: goal.type,
    x1: center.x - 50,
    x2: center.x + 50,
    y1: center.y - 50,
    y2: center.y + 50,
    max: 10, // 最多10个结果
  })
  
  return { targets: farTargets, layers: 3, size: '2KB' }
}
```

**关键改进**：
- 内圈精确 → 中圈轮廓 → 外圈坐标，token 从 82KB 降到 15-20KB
- 不是"一次扫全部"，而是"用多少扫多少"
- 用 `find` 替代大范围 `scan`（只要坐标不要整片地图）

---

#### 2.3.3 智能重试：失败分类而非固定次数

**问题**：所有失败都重试 3 次，"够不着"也重试 3 次白等
**方案**：立即失败 vs 可重试

```javascript
// src/v2/retry-classifier.js
export function classifyFailure(error, action, context) {
  const errorType = error.reason || error.message
  
  // 立即失败类（重试也没用）
  const IMMEDIATE_FAILURES = {
    'out_of_reach': {
      shouldRetry: false,
      fix: 'move_closer',
      reason: '距离问题，重试也挖不到，必须先走近',
    },
    'no_local_player': {
      shouldRetry: false,
      fix: null,
      reason: '系统限制，无法修复',
    },
    'player_dead': {
      shouldRetry: false,
      fix: 'wait_respawn',
      reason: '死亡状态，必须等复活',
    },
    'tool_insufficient': {
      shouldRetry: false,
      fix: 'upgrade_tool',
      reason: '工具等级不够，挥100次也挖不动',
    },
  }
  
  if (IMMEDIATE_FAILURES[errorType]) {
    return IMMEDIATE_FAILURES[errorType]
  }
  
  // 可重试类（可能是偶然失败）
  const RETRYABLE_FAILURES = {
    'timing': {
      shouldRetry: true,
      maxRetries: 2,
      adjust: (action) => ({ ...action, ms: action.ms * 1.5 }),
      reason: '时间不够，加长挥动时间再试',
    },
    'interrupted': {
      shouldRetry: true,
      maxRetries: 3,
      adjust: null,
      reason: '被打断（怪物/弹幕），可以重试',
    },
    'occlusion': {
      shouldRetry: true,
      maxRetries: 1,
      adjust: async (action, context) => {
        // 被遮挡 → 先清除遮挡物
        await clearOcclusion(action.target, context)
        return action
      },
      reason: '被遮挡，清除后重试',
    },
  }
  
  return RETRYABLE_FAILURES[errorType] || {
    shouldRetry: true,
    maxRetries: 2,
    reason: '未知失败，尝试重试',
  }
}
```

**关键改进**：
- "够不着"这类立即失败，不浪费时间重试
- "被打断"这类可重试，但有上限
- "被遮挡"这类需要先修复再重试，而非盲目重试

---

### 2.4 异常处理优化（保留机制+增加学习）

**保留的机制**（这些是对的）：
- 主目标唯一性（应急是子状态，处理完回主线）
- 重试预算（跳2次→重路径3次→失败）
- 日志记录（六列：序号/异常/证据/动作/结果/第几次）

**新增：失败模式学习**
```javascript
// src/v2/failure-memory.js
export class FailureMemory {
  constructor() {
    this.patterns = new Map() // 失败模式 → 成功策略
  }
  
  record(failure) {
    const pattern = this.extractPattern(failure)
    // pattern: {location, obstacleType, attemptedAction}
    
    const key = JSON.stringify(pattern)
    if (!this.patterns.has(key)) {
      this.patterns.set(key, {
        failures: [],
        successfulFix: null,
      })
    }
    
    this.patterns.get(key).failures.push(failure)
  }
  
  recordSuccess(pattern, fix) {
    const key = JSON.stringify(pattern)
    if (this.patterns.has(key)) {
      this.patterns.get(key).successfulFix = fix
    }
  }
  
  getSuggestion(currentFailure) {
    const pattern = this.extractPattern(currentFailure)
    const key = JSON.stringify(pattern)
    
    const memory = this.patterns.get(key)
    if (memory?.successfulFix) {
      // 之前遇到过同样的坑，用成功过的方案
      return {
        suggested: true,
        fix: memory.successfulFix,
        confidence: memory.failures.length / (memory.failures.length + 1),
      }
    }
    
    return { suggested: false }
  }
  
  extractPattern(failure) {
    // 提取失败模式（位置模糊化，避免过拟合）
    return {
      biome: failure.context.zones,
      obstacleType: failure.obstacle?.type,
      attemptedAction: failure.action.type,
    }
  }
}

// 使用示例
const memory = new FailureMemory()

// 失败时记录
memory.record({
  location: {x: 2100, y: 300},
  context: {zones: ['crimson']},
  obstacle: {type: 203}, // 猩红石
  action: {type: 'mine', tool: 'iron_pickaxe'},
  reason: 'tool_insufficient',
})

// 成功修复后记录
memory.recordSuccess(
  {biome: ['crimson'], obstacleType: 203, attemptedAction: 'mine'},
  {action: 'upgrade_pickaxe', target: 'gold_pickaxe'}
)

// 下次遇到猩红石 + 铁镐 → 直接建议"升级到金镐"
```

**关键改进**：
- 不是"固定策略"，而是"从失败中学习"
- 同样的坑第二次遇到时，直接用上次成功的方案
- 模式提取时位置模糊化（避免"只在2100,300有效"的过拟合）

---

## 三、修正后的性能预期

| 指标 | 旧架构 | v2.0方案（错误） | v2.1方案（修正） | 说明 |
|------|--------|-----------------|-----------------|------|
| **验证次数** | 每步验证 | 规划窗口，减少70% | 每步验证 | ❌ 不减少（动态环境必须验证） |
| **验证精度** | 通用检查 | - | 期望 vs 实际对比 | ✅ 提高精度（偏差2格触发应急） |
| **知识命中率** | ~75% | ~95% | ~95% | ✅ 保留（意图分类是对的） |
| **知识I/O** | 70KB | 10-20KB | 10-20KB | ✅ 保留（缓存是对的） |
| **扫描开销** | 82KB | - | 15-20KB | ✅ 渐进式扫描 |
| **批量操作** | 40次请求 | 4-5次 | 4-10批（分批验证） | ✅ 合并请求但保留验证 |
| **无效重试** | 固定3次 | - | 立即失败识别 | ✅ "够不着"不重试 |
| **应急响应** | 重新规划 | - | 预案立即执行 | ✅ 怪物出现时不重走全流程 |

**关键差异**：
- v2.0 试图减少验证 → **错误**（会被怪物打脸）
- v2.1 保留验证但提高效率 → **正确**（验证更快、更准、更智能）

---

## 四、实施路线（修正）

### 阶段1：知识检索优化（2周）✅ 保留v2.0方案

**产出**：
- `src/v2/intent-classifier.js`（已完成）
- `src/v2/knowledge-cache.js`
- 测试：命中率对比

### 阶段2：渐进式扫描（1周）🆕 新增

**产出**：
- `src/v2/progressive-scan.js`
- 测试：token消耗对比（82KB → 15-20KB）

### 阶段3：批量操作优化（1周）

**产出**：
- `src/v2/batch-executor.js`（分批验证，不是一次性）
- 测试：铺40格时间（6.4秒 → 2-3秒）

### 阶段4：智能重试（1周）

**产出**：
- `src/v2/retry-classifier.js`（立即失败 vs 可重试）
- `src/v2/failure-memory.js`（失败模式学习）

### 阶段5：应急预案系统（2周）

**产出**：
- `src/v2/action-planner-v2.js`（1步+应急预案）
- `src/v2/contingency-executor.js`
- 测试：怪物破坏场景下的响应速度

---

## 五、关键设计原则（修正）

### ✅ 保留的原则

1. **回读验证不能减少**  
   理由：命令成功 ≠ 效果发生（怪物、地形、随机性）

2. **轮询确认不能省略**  
   理由：异步执行（`ItemCheck` 延迟 2-3 帧）

3. **异常处理必须保留**  
   理由：单次失败可能是偶然，重试可能成功

4. **元自检仍需要**  
   理由：避免"没镐子去挖50格"这类明显不可做的事

### ✅ 优化的方向

1. **让验证更快**：渐进式扫描（82KB → 15KB）

2. **让验证更准**：期望 vs 实际对比（偏差触发应急）

3. **让重试更智能**：立即失败识别 + 失败模式学习

4. **让响应更快**：应急预案（不重走全流程）

### ❌ 错误的方向

1. ~~减少验证次数~~（会被怪物打脸）

2. ~~提前规划5步~~（第2步就会被打乱）

3. ~~预验证替代回读~~（规划时的状态 ≠ 执行时的状态）

4. ~~批量一次性做完~~（一个失败可能影响后续）

---

## 六、总结

**核心认知转变**：
- v2.0：把决策链路当作"冗余" → 试图简化
- v2.1：认识到泰拉瑞亚是**动态环境** → 保留安全机制，优化效率

**正确的优化**：
- ✅ 知识检索更准（意图分类）
- ✅ 扫描更高效（渐进式）
- ✅ 批量合并请求（但分批验证）
- ✅ 重试更智能（立即失败识别）
- ✅ 应急更快（预案系统）

**错误的优化**：
- ❌ 减少验证次数
- ❌ 多步前瞻规划
- ❌ 预验证替代回读

**预期提升**：
- 知识命中率：75% → 95%（+20%）
- 扫描开销：82KB → 15KB（-80%）
- 批量操作：6.4秒 → 2-3秒（-50%）
- 无效重试：3次盲试 → 立即识别（-100%）

感谢你的纠正！这让整个方案更符合实际游戏环境。
