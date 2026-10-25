# 知识库（KB）路由总表

> 收到玩家指令 → **先判标签 → 只读命中的库**，不把无关资料塞进上下文。

## 一条硬规定：这六个库只收"通用机制"

每个库都只写**换成任何世界种子、任何地图都成立**的东西。
写之前问一句：

> **这句话换一张地图还成立吗？**

不成立的，一律不进知识库：

| 不能进库的东西 | 例子 | 该放哪 |
|---|---|---|
| 固定坐标 | "在某个 x,y 砌墙" | `docs/terraria-mechanics.md` |
| 单局实测数值 | "某处挖到 y=NNN 见水" | `docs/terraria-mechanics.md` |
| 本次调试的偶然现象 | "这次放下去了，上次没放下" | `docs/debug/` |
| 单次任务的固定尺寸 | "按 15×5 建" | 任务记录里，不进库 |
| 特定场景的硬编码参数 | "到底就停，别超过 N 格" | 工具参数文档 |

对应的临时文档（**不是知识库，不参与路由**）：

- `docs/terraria-mechanics.md` —— 本档世界的实测地形/矿脉/坐标/时间曲线
- `docs/debug/unknowns.md` —— 本档还没搞清的 40 条未知（P0/P1/P2 + 测法）
- `docs/capabilities.md` —— 我们自己的工具能力矩阵（接口，不是游戏知识）

**自动校验**：`npm run kb:lint` 会扫这六个文件，发现坐标、`x=`/`y=`、
"本档/本次/实测/测试"、世界名、毫秒/tick 这类单局痕迹就报错；`npm test` 里也挂了这条。

## 结构：4 大主库 + 2 个辅助库

| # | 标签 | 库文件 | 覆盖内容 |
|---|---|---|---|
| 1 | `game_rule` | [game-rule.md](game-rule.md) | 房屋判定 / NPC 入住与生成 / 怪物刷新 / 物块与地形 / 环境扩散 / 世界全局机制 / 判定速查 |
| 2 | `item_operation` | [item-operation.md](item-operation.md) | 物品分类与功能边界 / 使用硬性约束 / 背包与切换 / 交互站位 |
| 3 | `build_standard` | [build-standard.md](build-standard.md) | 通用建造规范 / NPC 房屋通用规范 / 各类平台 / 方块搭配 / 地形改造 |
| 4 | `actor_behavior` | [actor-behavior.md](actor-behavior.md) | 移动 / 战斗 / 拾取 / NPC 交互 / 增益与召唤 |
| 5 | `task_correct` | [task-correct.md](task-correct.md) | 模糊指令 / 歧义 / 任务中断 / 修改迭代 / 无法完成怎么回 |
| 6 | `safety_limit` | [safety-limit.md](safety-limit.md) | 禁止行为清单 / 操作范围限制 / 自检修正规则 |

生成物：[_generated-commands.md](_generated-commands.md)（68 个 mod 命令 + 25 个插件工具，
`node scripts/kb-sync.mjs` 生成，**不要手改**）。命令/参数/报错查这里，不占六个库的位置。

## 路由逻辑

```
玩家消息
  ├─ 永远先加载：safety_limit（禁令 / 范围 / 自检）
  ├─ 问"游戏怎么算"（为什么行/不行、机制、判定）        → game_rule
  ├─ 问"东西怎么用"（镐/斧/锤/门/弓箭/药水/召唤物）      → item_operation
  ├─ 要建/要改地形（房子、平台、农场、竖井、隧道）        → build_standard（+ game_rule 判定）
  ├─ 做事（走/打/捡/找 NPC/喝药）                        → actor_behavior
  └─ 说得不清楚 / 我上次没做成 / 玩家纠正我              → task_correct
```

多标签命中时的加载顺序（后者补充前者，不重复整库加载）：

`safety_limit` → `game_rule` → `build_standard` → `actor_behavior` → `item_operation` → `task_correct`

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