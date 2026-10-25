# 任务用例库

把「这套自动化到底能不能用」从**自由通关碰运气**改成**逐条用例过一遍**。

## 为什么不靠自由跑一遍

自由通关效率低、盲区大：

- 事件和生物是随机的。猪龙鱼公爵、日食、霜月、微光池、稀有植物——碰不到，自检就永远不触发，缺口继续藏着。
- 自由跑会被生存和赶路吃掉大部分时间，边界场景它不会主动去撞。
- 能力不全的时候放任它自主跑，只会反复踩危险、卡在重试循环里，一路报缺口，处理不过来。

用例库是主动去撞，不是等它撞上。

## 三层定位（别混）

| 是什么 | 何时用 | 负责什么 |
|---|---|---|
| **五维元自检** `prompts/meta-check.md` | 每次任务都跑 | 识别缺口（规则 / 道具 / 地形路径 / 动作原语 / 状态读取） |
| **任务用例库**（本目录） | 前期主力 | 批量、主动挖出绝大多数常见缺口 |
| **自由游玩** | 用例全过之后 | 长尾补充，抓用例没覆盖的小众机制 |
| **修复** | 缺口汇总之后 | 改桥接代码、加命令、加读取接口、补知识库——**我来做**，优先级你定 |

> 关于"AI 不能自己修"这一点要说清：这个仓库里的 mod 桥接代码、命令、读取接口都是我写的
> （`DshTerrariaBridge/*.cs` 和 `index.js` 就是我自己改的），所以缺口修复**不需要你动手**。
> 你只需要决定先补哪个、以及验收。我不知道的是"这个能力该怎么做成玩家视角的操作"，
> 那种地方我会先问你，而不是猜。

## 一条用例长什么样

```json
{
  "id": "F01",
  "phase": 2,
  "category": "fish",
  "task": "装备钓竿，在水边抛竿，等浮标沉下去再收线",
  "needs": {
    "commands": ["select", "use", "state"],
    "missingCommands": ["castline", "reel"],
    "kb": ["loot_source"],
    "kbMissing": ["loot_source 只写了需要钓竿+鱼饵，缺抛竿/咬钩/收线的流程与消耗"],
    "unsupported": ["随机事件与时机窗口", "通用弹幕状态"]
  }
}
```

- `task` 是**玩家口吻的指令**，可以直接下发，不是技术描述。
- `needs.commands` 是这条要成立必须存在的命令；写进来的必须真的存在（校验会查）。
- `needs.missingCommands` 是我已知现在**没有**的命令。补上之后 `--validate` 会报「已经实现了，把声明删掉」——
  这是故意的：不回来改用例，用例库就会开始骗人。
- `needs.kb` 是必须先读的库；`kbMissing` 是我**读过之后确认**缺的条目。
- `needs.unsupported` 只能填能力清单 `notSupported` 里真有的那几条，不许自己编理由。

## 怎么跑

```bash
node scripts/testcases.mjs --list                    # 看全部用例
node scripts/testcases.mjs --list --phase 1          # 只看第一阶段
node scripts/testcases.mjs --validate                # 校验用例库自身
node scripts/testcases.mjs --dry                     # 离线预检，打印报告
node scripts/testcases.mjs --dry --phase 1           # 只预检第一阶段
node scripts/testcases.mjs --dry --md docs/testcases/gap-report.md --json docs/testcases/baseline.json
node scripts/testcases.mjs --dry --baseline docs/testcases/baseline.json   # 回归：谁修好了、谁新坏了
```

`npm test` 里挂了结构校验和预检，所以用例库写坏了会直接红。

离线预检**只判三件事**（都能落到有/没有）：命令在不在、是不是撞上"明确做不到"、库里有没有这条知识。
它不代替真机执行——真机那步还要按五维自检判规则与时序，那是我在任务里做的。

## 工作流

1. 加载五维自检规则 + 常驻能力清单（`prompts/meta-check.md` + `AGENTS.md` 能力块）。
2. 按阶段逐条下发用例：每条都走「路由 → 检索库 → 五维自检 → 缺口清单」。
3. 汇总缺口（离线预检已经把口径统一了）：按"要补什么"排，不按用例排——补一次能消掉多条。
4. 分批补：加命令 / 加读取字段 / 补知识库。
5. 全部补完重跑一轮做回归：`--baseline` 会报「修好了 / 新坏了 / 仍缺」。

## 阶段划分

| 阶段 | 内容 | 什么时候开 |
|---|---|---|
| 1 | 移动 / 采集 / 建造 / 基础合成 | 第一轮必跑，先把输入输出和物品交互跑通 |
| 2 | NPC / 钓鱼 / 装备 / 普通 BOSS / 基础事件 | 阶段 1 全过之后 |
| 3 | 困难模式 / 特殊事件 / 微光 / 稀有群系 | 日常玩法齐了之后 |
| 4 | 自由游玩 | 以上全过，只用来抓长尾 |

## 文件

| 文件 | 内容 |
|---|---|
| `testcases/cases.json` | 用例本体（唯一事实来源，手写） |
| `docs/testcases/gap-report.md` | 最近一次预检报告（生成物，`--md` 写） |
| `docs/testcases/baseline.json` | 基线（生成物，`--json` 写，回归对比用） |
| `src/testcases.js` | 校验 / 预检 / 汇总 / 渲染 / 基线对比 |
| `scripts/testcases.mjs` | 命令行入口 |