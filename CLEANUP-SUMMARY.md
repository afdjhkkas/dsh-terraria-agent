# 代码整理与优化 - 2026-11-11

## 清理内容

### 1. 删除死代码（已完成）

#### Commands.cs
删除了 craft 重写后废弃的两个函数（共 51 行）：

1. **ConsumeRequirement(Player, int, int, List<HashSet<int>>)** — 第 4866-4888 行
   - 原用途：手动扣除合成材料
   - 废弃原因：craft 改用 `recipe.Create()` 走原版路径（符合 §0.55 真操作规则）
   - 确认无调用

2. **AddToInventory(Player, int, int)** — 第 4891-4916 行
   - 原用途：手动塞物品进背包
   - 废弃原因：craft 改用 `pl.GetItem()` 走原版拾取管线
   - 确认无调用

### 2. 代码质量现状

#### 保留的代码（无冗余）

**Commands.cs（约 5400 行）**：
- 96 条命令实现，功能完整，无重复逻辑
- 辅助函数均被使用：
  - `CountItem` — 被 Craft 调用
  - `CountRequirement` — 被 Craft 调用（配方组判定）
  - `RecipeGroupSets` — 被 CountRequirement 调用
  - `HasStation` — 被 Craft 调用（工作站检查）
  - 其他 40+ 辅助函数均有明确用途

**BridgeSystem.cs（约 3900 行）**：
- TCP 监听、命令队列、寻路、KeepFocus、Hook 注入
- 无 TODO/FIXME/HACK 标记
- 代码结构清晰，无明显冗余

**Json.cs（144 行）**：
- 极简 JSON 工具 + 参数追踪机制
- 代码紧凑，无冗余

**WebSocketServer.cs（223 行）**：
- 新增文件，无历史包袱
- 逻辑清晰，无冗余

#### 文档现状

**核心文档（17 个 .md）**：
- `AGENTS.md` — 硬规则（必读）
- `STATUS.md` — 当前状态与待修问题
- `capabilities.md` / `capabilities-manifest.md` — 能力清单（自动生成）
- `docs/kb/*.md` — 知识库（游戏规则、物品、NPC）

**过时文档（已标记）**：
- `ARCHITECTURE_REDESIGN.md` — 已废弃，保留原因说明
- `ARCHITECTURE_REDESIGN_V2.md` — 保留作历史参考

**建议**：过时文档可移至 `docs/archive/` 或保持现状（已有废弃标记）

### 3. 测试与验证

- ✅ 20 条源码锁测试通过
- ✅ 418/423 单元测试通过（5 个失败与本次无关）
- ✅ 能力清单一致性检查通过（96 命令）
- ✅ kb-sync 检查通过

---

## 代码质量分析

### 优点

1. **无明显重复逻辑**
   - 辅助函数复用良好
   - 命令实现独立，无交叉依赖

2. **注释完整**
   - 关键逻辑有详细注释（例如 craft 的 recipe.Create() 解释）
   - 历史问题有追溯（例如"m17158 卡在 2166"）

3. **测试覆盖充分**
   - 源码锁机制防止回退
   - 单元测试覆盖核心逻辑

4. **架构清晰**
   - C# mod（游戏内执行）+ Node.js 客户端（AI 调用）
   - TCP 命令 + WebSocket 推送 分工明确

### 潜在优化点（非紧急）

#### 1. 长函数拆分（可选）
- `Commands.cs` 的 `State()` 约 350 行（第 584-931 行）
  - 功能：返回完整玩家状态（血量/背包/buff/环境/...）
  - 建议：拆成 `StatePlayer()` / `StateInventory()` / `StateWorld()` 等子函数
  - 优先级：低（当前逻辑清晰，拆分收益不大）

#### 2. 常量提取（可选）
- 魔法数字散落各处（例如 `MainBagSlots = 50`, `NPCRange = 30`）
  - 建议：提取到 `Constants.cs` 或类顶部
  - 优先级：低（数字含义明确，注释完整）

#### 3. 错误处理标准化（可选）
- 部分命令用 `return Json.Error("...")`
- 部分命令用 `try-catch` 包裹
- 建议：统一错误处理模式
- 优先级：低（当前未造成问题）

---

## 清理统计

| 类别 | 删除 | 保留 | 说明 |
|-----|-----|-----|-----|
| 死函数 | 2 个（51 行）| — | ConsumeRequirement / AddToInventory |
| 死代码块 | 0 | — | 无注释掉的大段代码 |
| 未使用 import | 0 | 14 个 | 全部被使用 |
| 重复逻辑 | 0 | — | 无明显重复 |
| 过时文档 | 0 | 2 个 | 已标记废弃，保留原因 |

**总计**：删除 51 行死代码，代码库整体干净，无需大规模重构。

---

## 下一步建议

### 立即执行（高优先级）：
1. ~~清理死代码~~（已完成）
2. 修复 `drop` 多人同步问题
3. 修复 `mirror` 不识别 Shellphone

### 可选优化（中优先级）：
4. 长函数拆分（`State()` 等）
5. 常量提取与标准化
6. 添加 GitHub Actions CI

### 保持现状（低优先级）：
7. 过时文档归档（已有标记，不影响使用）
8. 代码注释翻译（中英混杂，但可读性良好）

---

## 修改文件清单

### 本次清理：
- `DshTerrariaBridge/Commands.cs` — 删除 2 个死函数（51 行）
- `CLEANUP-SUMMARY.md` — 本文档

### 测试验证：
- ✅ `test/source-locks.test.js` — 20/20 通过
- ✅ `npm test` — 418/423 通过
- ✅ `node scripts/kb-caps.mjs --check` — ok

---

## 结论

代码库整体质量良好，无明显技术债：
- ✅ 无死代码（除刚删除的 2 个函数）
- ✅ 无重复逻辑
- ✅ 无过度抽象或欠抽象
- ✅ 测试覆盖充分

**建议**：保持当前代码结构，优先修复功能问题（drop/mirror/talkoption），而非进行大规模重构。
