# 知识库（KB）路由总表

> 收到玩家指令 → **先判标签 → 只读命中的库**，不把无关资料塞进上下文。

## 一条硬规定：这些库只收"通用机制"

每个库都只写**换成任何世界种子、任何地图都成立**的东西。
写之前问一句：

> **这句话换一张地图还成立吗？**

不成立的，一律不进知识库：

| 不能进库的东西 | 例子 | 该放哪 |
|---|---|---|
| 本世界坐标 | "在某个 x,y 砌墙" | `docs/terraria-mechanics.md` |
| 本世界地形实测 | "某处挖到 y=NNN 见水" | `docs/terraria-mechanics.md` |
| 本局偶然现象 | "这次放下去了，上次没放下" | `docs/debug/` |
| 单次任务的固定尺寸 | "按 15×5 建" | 任务记录里，不进库 |
| 我们自己工具的参数 | 默认时长、重试次数、批量上限 | 工具参数文档 |

反过来，这些**属于游戏常量，必须进库**（不要当成"单局数据"删掉）：

- 物品 / 物块 / 墙 / 弹幕 / buff 的数字 ID 与枚举名（换任何世界都指同一个对象）
- 硬度分级、光照值、堆叠上限、作用距离、弹药与燃料对应、占地与锚点、合成配方
- 由这些常量推出的判定，例如"某类物品只能做某类事""这个形状需要几格空间"

对应的临时文档（**不是知识库，不参与路由**）：

- `docs/terraria-mechanics.md` —— 本档世界的实测地形/矿脉/坐标/时间曲线
- `docs/debug/unknowns.md` —— 本档还没搞清的 40 条未知（P0/P1/P2 + 测法）
- `docs/capabilities.md` —— 我们自己的工具能力矩阵（接口，不是游戏知识）

**自动校验**：`npm run kb:lint` 会扫这些库文件，发现坐标、`x=`/`y=`、
"本档/本次/实测/测试"、世界名、毫秒/tick 这类单局痕迹就报错；`npm test` 里也挂了这条。

## 结构：4 个基础主库 + 6 个内容主库 + 3 个辅助库

| # | 标签 | 库文件 | 覆盖内容 |
|---|---|---|---|
| 1 | `game_rule` | [game-rule.md](game-rule.md) | 房屋判定 / NPC 入住与生成 / 怪物刷新 / 物块与地形 / 环境扩散 / 世界全局机制 / 判定速查 |
| 2 | `item_operation` | [item-operation.md](item-operation.md) | 物品分类与功能边界 / 使用硬性约束 / 背包与切换 / 交互站位 / 常用 ID 速查 |
| 3 | `build_standard` | [build-standard.md](build-standard.md) | 通用建造规范 / NPC 房屋通用规范 / 各类平台 / 方块搭配 / 地形改造 |
| 4 | `actor_behavior` | [actor-behavior.md](actor-behavior.md) | 移动 / 战斗 / 拾取 / NPC 交互 / 增益与召唤 |
| 5 | `world_biome` | [world-biome.md](world-biome.md) | 世界分层 / 生物群系判定 / 液体窒息坠落 / 环境扩散 / 昼夜月相天气 / 地貌结构 |
| 6 | `progression_boss` | [progression-boss.md](progression-boss.md) | 进程骨牌 / 前困难模式首领 / 困难模式首领 / 祭坛 / 战前准备 |
| 7 | `gear_route` | [gear-route.md](gear-route.md) | 镐力门槛 / 矿石阶梯 / 护甲三件与职业分支 / 四条武器线 / 机动性饰品 / 词条重铸 / 增益药水 |
| 8 | `combat_math` | [combat-math.md](combat-math.md) | 伤害结算顺序 / 暴击击退无敌帧 / 伤害类型与穿透 / 生命魔力召唤位 / 减益 / 幸运 |
| 9 | `loot_source` | [loot-source.md](loot-source.md) | 自然容器 / 掉落规则 / 钓鱼板条箱 / 首领事件掉落 / NPC 商店 / 转化回收 |
| 10 | `npc_town` | [npc-town.md](npc-town.md) | 入住两条件 / 各 NPC 出现条件 / 幸福度三因素 / 传送水晶 / 商店价格 |
| 11 | `atomic_action` | [atomic-action.md](atomic-action.md) | 动作执行链条 / 判定条件 / 先后顺序 / 失败分类 / 重试策略 / 操作限制 / 批量规范 |
| 12 | `task_correct` | [task-correct.md](task-correct.md) | 模糊指令 / 歧义 / 任务中断 / 修改迭代 / 无法完成怎么回 |
| 13 | `safety_limit` | [safety-limit.md](safety-limit.md) | 禁止行为清单 / 操作范围限制 / 自检修正规则 |

生成物（由代码/游戏导出，**不要手改**）：

| 文件 | 内容 | 重新生成 |
|---|---|---|
| [_generated-commands.md](_generated-commands.md) | 68 个 mod 命令 + 25 个插件工具的参数与报错 | `node scripts/kb-sync.mjs` |
| [_generated-ids.md](_generated-ids.md) | 物品 / 物块 / 墙的数字 ID 与枚举名（5513 物品 · 693 物块 · 347 墙） | `npm run kb:ids -- --tsv <ids.tsv>` |

ID 表为什么算通用知识：`ItemID` / `TileID` / `WallID` 是程序常量，
同一个数字在任何世界、任何存档都指向同一个对象。

## 路由逻辑

**默认是单轮自路由**：同一个回合、同一次模型调用里，模型自己想清楚要查哪几个库，
把标记写在全回合第一行，程序捕获这行 → 检索 → 追加进**同一个会话**续传。

```
玩家指令
  → 模型第一行写：NEED_KNOWLEDGE:safety_limit,game_rule,build_standard,atomic_action
  → terraria_kb { need: "<本回合输出>" }   捕获标记，只加载命中的库
  → 回 continuation，追加到同一会话
  → 模型基于新知识输出完整动作序列
```

system prompt 原文：[`prompts/self-route.md`](../../prompts/self-route.md)
（`node scripts/route.mjs --system` 可取全文）。

**兜底（标记没写出来时，不许静默降级）**：
① 前置分类器 [`prompts/intent-router.md`](../../prompts/intent-router.md) 补判一次
（`terraria_kb { prompt: true }`，它只回一行标签不带解释）→
② 关键词 `node scripts/route.mjs "<原话>"`（粗筛，不假装等价）→
③ 都没判出来就只加载 `safety_limit` 并在回复里说明"这次没判出库标签"。

```
玩家消息
  ├─ 永远先加载：safety_limit（禁令 / 范围 / 自检）
  ├─ 问"游戏怎么算"（为什么行/不行、机制、判定）        → game_rule
  ├─ 问"这是哪/会怎样"（分层、群系、液体、天气、地形）    → world_biome
  ├─ 问"下一步打谁/怎么召唤/祭坛能干什么"                → progression_boss
  ├─ 问"装备怎么升/挖不动这矿/护甲穿不穿齐/饰品堆什么"    → gear_route
  ├─ 问"为什么打不动/多少伤害/怎么减伤"                  → combat_math
  ├─ 问"东西去哪找/怎么刷/箱子要不要开"                  → loot_source
  ├─ 问"NPC 怎么还不来/房价怎么便宜/传送水晶怎么用"      → npc_town
  ├─ 问"东西怎么用"（镐/斧/锤/门/弓箭/药水/召唤物）      → item_operation
  ├─ 要建/要改地形（房子、平台、农场、竖井、隧道）        → build_standard（+ game_rule 判定）
  ├─ 做事（走/打/捡/找 NPC/喝药）                        → actor_behavior
  ├─ 要角色动手执行单步动作（放一块/挖一格/切物品/铺墙/重试）→ atomic_action
  └─ 说得不清楚 / 我上次没做成 / 玩家纠正我              → task_correct
```

多标签命中时的加载顺序（后者补充前者，不重复整库加载）：

`safety_limit` → `game_rule` → `world_biome` → `progression_boss` → `gear_route` → `combat_math` → `loot_source` → `npc_town` → `build_standard` → `actor_behavior` → `atomic_action` → `item_operation` → `task_correct`

关键词表的**唯一事实来源**是 `src/kb-router.js` 的 `KEYWORDS`
（每条规则都可审计：命中哪些词、各打几分，`route()` 会一起返回）。

## 用法

```js
import { route, load, buildContext } from './src/kb-router.js'
route('把家完善一下')        // → { tags: ['safety_limit','build_standard'], scores, hits }
buildContext('为什么 NPC 不住进去')  // → 安全库 + 命中库的正文
```

## 维护

| 操作 | 命令 |
|---|---|
| 检查有没有混进单局数据 | `npm run kb:lint` |
| 重新生成命令附录 | `node scripts/kb-sync.mjs` |
| 校验命令表与代码一致 | `node scripts/kb-sync.mjs --check`（`npm test` 里也有） |
| 重新生成 ID 对照表 | 游戏内 `{"cmd":"idmap","params":{"path":"<临时目录>/ids.tsv"}}` → `npm run kb:ids -- --tsv <该文件>` |
| 看单轮自路由 system prompt | `node scripts/route.mjs --system` |
| 从模型输出里抓标记 | `node scripts/route.mjs --need "<模型输出>"`（抓不到 exit 1） |
| 看看兜底分类器 Prompt | `node scripts/route.mjs --prompt` |
| 校验一份分类器输出 | `node scripts/route.mjs --tags "game_rule, atomic_action"` |
| 离线兜底判一句 | `node scripts/route.mjs "帮我造一间护士的房子"` |