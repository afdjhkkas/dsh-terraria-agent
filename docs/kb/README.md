# 知识库（KB）路由总表

> 目的：收到玩家指令后，**先判定标签，再只检索对应库**，不把无关资料塞进上下文。
> 全部内容按「已验证 / 未验证」标注；`已验证` = 本仓库里有过回读证据，
> `未验证` = 来自 wiki 或推断，用之前必须先测一次。

## 六个标签

| 标签 | 库文件 | 命中条件（关键词/意图） | 大小 |
|---|---|---|---|
| `game_rule` | [game-rule.md](game-rule.md) | 世界/方块/生物/刷怪/NPC 入住/扩散/判定标准；「为什么不行」「游戏是怎么算的」 | 中 |
| `tool_command` | [tool-command.md](tool-command.md) | 用哪个工具、参数怎么填、坐标怎么算、报错含义 | 大（按需只读小节） |
| `build_plan` | [build-plan.md](build-plan.md) | 造东西：房子、农场、战斗平台、刷怪场、桥、竖井 | 中 |
| `actor_interact` | [actor-interact.md](actor-interact.md) | 怎么动/怎么打/怎么捡/怎么跟 NPC 说话/怎么喝药 | 中 |
| `chat_correct` | [chat-correct.md](chat-correct.md) | 玩家说法模糊、指令失败怎么回、被纠正后怎么改 | 小 |
| `safety_limit` | [safety-limit.md](safety-limit.md) | **每次请求都带**，不判定，永远加载 | 极小 |

## 路由规则

```
玩家消息
  ├─ 永远先加载：safety-limit.md（禁令/范围/边界/确认）
  ├─ 判定：这条消息要"知道规则"还是"做事"？
  │    ├─ 知道规则（为什么/能不能/怎么算）        → game_rule
  │    ├─ 做事（动作本身）                        → actor_interact
  │    ├─ 做事（要建/要挖一条通道/要改造地形）      → build_plan（+ game_rule 的判定标准）
  │    └─ 要下具体命令（工具名/参数/坐标）          → tool_command
  └─ 任何"玩家说得不清 / 我上次没做成 / 玩家纠正我" → chat_correct
```

多标签同时命中时的顺序（后者补充前者，不重复加载整库）：
`safety_limit` → `game_rule` → `build_plan` → `actor_interact` → `tool_command` → `chat_correct`

## 写作约定

- 一条规则一行，能测的就写「测法」；没测过的必须标 `未验证`。
- 不写"应该没问题""一般来说"这类话；写不出来就空着，等实测。
- 与代码相关的事实要给出文件:行号，避免文档和实现漂移。
- 本目录**不复制** `docs/capabilities.md`（工具清单权威版）与
  `docs/progression-plan.md`（教训日志），只写"怎么用/怎么判断"。

## 维护

改了 mod 命令或插件工具后，跑：

```bash
node scripts/kb-sync.mjs      # 从 Commands.cs / index.js 重新生成命令表
npm test                       # 含 kb 路由单测
```

生成物：`docs/kb/_generated-commands.md`（不要手改）。