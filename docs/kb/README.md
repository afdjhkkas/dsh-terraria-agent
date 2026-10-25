# 知识库（KB）路由总表

> 目的：收到玩家指令后**先判标签，再只检索对应库**，不把无关资料塞进上下文。
> 每条结论都标来源：`已验证` = 本仓库有回读证据；`未验证` = wiki/推断，用前必须先测。

## 结构：4 大主库 + 2 个辅助库

| # | 标签 | 库文件 | 覆盖内容（对应小节） |
|---|---|---|---|
| 1 | `game_rule` | [game-rule.md](game-rule.md) | §1 世界机制 · §2 方块与物块属性 · §3 NPC 入住/生成 · §4 怪物刷新 · §5 环境生物 · §6 腐化/猩红/神圣扩散 · §7 地形生成 · §8 判定标准速查 |
| 2 | `tool_command` | [tool-command.md](tool-command.md) | §1 通路 · §2 全部命令分组功能 · §3 坐标体系 · §4 参数规范 · §5 错误说明 · §6 权限黑名单 · §7 部署链路 |
| 3 | `build_plan` | [build-plan.md](build-plan.md) | §1 基础建筑标准模板 · §2 功能性建筑规范 · §3 方块搭配 · §4 结构布局 · §5 安全建造约束 · §6 特殊环境（水下/地狱/空岛/雪原） |
| 4 | `actor_interact` | [actor-interact.md](actor-interact.md) | §1 角色移动 · §2 战斗行为 · §3 拾取/使用 · §4 NPC 交互 · §5 召唤物 · §6 药水/增益 · §7 标准动作循环 |
| 5 | `chat_correct` | [chat-correct.md](chat-correct.md) | §1 模糊请求 · §2 失败话术 · §3 歧义识别 · §4 纠错后修正 · §5 做不到怎么回 · §6 汇报格式 |
| 6 | `safety_limit` | [safety-limit.md](safety-limit.md) | §1 高危禁令 · §2 范围限制 · §3 自动操作边界 · §4 风险操作确认 · §5 证据要求 |

生成物：[_generated-commands.md](_generated-commands.md)（68 个 mod 命令 + 25 个插件工具，`node scripts/kb-sync.mjs` 生成，**不要手改**）。

另有一份 **已知的未知**：[_unknowns.md](_unknowns.md) —— 40 条"我们还没搞清楚的事"，
按 P0/P1/P2 分级，每条带测法。它不属于六个标签，用来回答"你还有什么不知道"，
以及决定下一轮进游戏先干什么。

## 路由逻辑

```
玩家消息
  ├─ 永远先加载：safety_limit（禁令/范围/边界/确认/证据要求）
  └─ 判定这条消息要"知道规则"还是"做事"
       ├─ 知道规则（为什么不行 / 能不能 / 游戏怎么算）     → game_rule
       ├─ 做事（动作本身：走/挖/放/打/捡/买卖）           → actor_interact
       ├─ 做事（要建/要挖通道/要改造地形）                → build_plan（+ game_rule §8 判定标准）
       └─ 要下具体命令（工具名/参数/坐标/报错）            → tool_command
  └─ 任何"玩家说得不清 / 我上次没做成 / 玩家纠正我"      → chat_correct
```

多标签命中时的加载顺序（后者补充前者，不重复整库加载）：

`safety_limit` → `game_rule` → `build_plan` → `actor_interact` → `tool_command` → `chat_correct`

## 判定关键词表

| 标签 | 命中词/意图 |
|---|---|
| `game_rule` | 为什么、能不能、怎么算、判定、刷怪、入住、扩散、生成、方块属性、液体的规则 |
| `tool_command` | 哪个工具、参数、坐标、报错、桥没起来、命令怎么写、部署 |
| `build_plan` | 建、造、盖、挖通道、竖井、平台、农场、刷怪场、房子尺寸 |
| `actor_interact` | 走过去、挖一格、放一块、打它、捡起来、跟他说话、买、召唤、喝药 |
| `chat_correct` | 玩家说得模糊、我失败了、玩家纠正、要求改规则、要汇报 |
| `safety_limit` | 永远加载，不看关键词 |

## 写作约定

- 一条规则一行；能测的给「测法/证据」，没测过的必须标 `未验证`。
- 不写"应该没问题""一般来说"；写不出来就留空等实测。
- 与代码相关的事实给出 `文件:行号`（例如 `DshTerrariaBridge/Commands.cs:1517` 的 `HouseDiag`）。
- 本目录**不复制** [capabilities.md](../capabilities.md)（工具权威清单）与
  [progression-plan.md](../progression-plan.md)（教训日志）；这里只写"怎么判断 / 怎么用"。

## 维护

```bash
node scripts/kb-sync.mjs   # 改过 Commands.cs / index.js 之后重新生成命令表
npm test                   # 含 kb 路由单测 + 结构完整性检查
```

改库时**必须**保留每个文件的节标题（结构测试会检查），否则 `npm test` 失败。