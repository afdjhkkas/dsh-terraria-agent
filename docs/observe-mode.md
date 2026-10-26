# 观摩模式与示范案例库

两个模式，同一条执行链，可随时切换：

- **auto**（默认）：脚本自己下指令，凭知识库 + 自检推进。
- **observe**：脚本**只读不写**。同一个回合里既采真人操作，又对同一帧做脚本预推演，把两股流对齐差分，存进示范案例库。切回 auto 后，遇到相似场景就把当初的修正拿出来用。

## 示范库只存原子动作（六条全局硬规则）

这条边界比"相似度阈值多少"重要得多，先立在这里：**示范只能教单个动作怎么按，不能教整套任务怎么做。**
落地代码在 `src/atom-model.js`，全部是纯函数，采集/入库/检索三端都过它。

| # | 规则 | 代码位置 |
|---|------|----------|
| 1 | 原子动作（单次短跳/长按跳/单次挥镐/单次普攻/单次转向）可入库；执行序列（砍树、打怪、清方块、持续挖掘）一律不入库 | `ATOMIC_KINDS` / `sequenceRejection` |
| 2 | 循环任务只取原子模板，重复次数与总时长交给上层按终止信号判断 | `atomize()` 连续同类只留一条 |
| 3 | 条件分支只记「每个分支里的原子动作怎么做」，不记这次走了哪条 | `sequenceRejection` 的 `branches`/`tookBranch` |
| 4 | 空间任务只记执行方式，不固化坐标/距离/方块数 | `atomFromAction()` 白名单字段 |
| 5 | 检索只产出节奏参考，不得覆盖上层状态判断/终止条件/分支选择/数值计算 | `recall()` 只回时序 |
| 6 | 采集时自动剥离总次数、总时长、固定坐标、固定分支走向 | `strippedFields()` + `VOLATILE_FIELDS` |

### 一条原子动作长什么样

```json
{ "kind": "mine-swing", "holdMs": 92, "grounded": true }
```

就这三个字段。没有坐标、没有"砍了几刀"、没有"这次走了哪边"。
坐标和次数不是动作样式，是**这一次**的偶然量——留住它们，检索就会退化成照抄上一次的录像。

### 终止信号归上层，不归示范

循环类任务（砍树、挖矿、打怪）到底重复几次，示范里一律不许固化。上层按实时观测决定：
方块破碎 / 实体血量归零 / 目标消失 / 抵达坐标 / 障碍清除。
原子模板只回答"这一下要按多久"，不回答"要按几下"。

### 采集端怎么筛

两条采集通道，都吐同一种原子模板：

**① 事件流（首选）** —— `keyevents` 命令按 tick 记录按下/松开边沿，`atomizeEvents(events)` 切分。
按住时长 = 松开 tick − 按下 tick，**不受任何轮询频率影响**。一开始用轮询（几十毫秒一帧），
轻点跳会算出 0ms、长按会多出半个采样间隔，全是假的；换成边沿事件后 1 tick 的差别都留得住。

**② 帧序列（旧录像重切用）** —— `atomize(frames)` 从已有的录像里切。
精度受录像采样率限制，只用来把历史素材捞回来，不作为新采集的主路径。

两条通道做同样的三件事：
1. 一次按下到松开 = 一个原子动作，按住时长即模板；
2. **紧接的同类动作**只保留第一条，其余计入 `dropped.loop`（隔了一会儿的两次独立动作都要留）；
3. 跳跃按 `JUMP_TAP_MS` 分成 `jump-short` / `jump-long` 两种节奏；超过 `MAX_ATOM_MS` 的按住算“一直按着”，丢弃。

**采集必须排除自己下发的输入。** 桥下发的 jump/use 也走同一组输入标志，不排掉的话示范库里会混进
脚本自己的动作，学出来就是自己抄自己。`PostUpdateInput` 里在 `JumpUntil` / `UseUntil` / `UseTileUntil`
期间跳过对应键，实测脚本自己跳一次 → 事件流条数为 0。


`atomize(frames)` 从原始帧里切出按键周期，做三件事：
1. 一次按下到松开 = 一个原子动作，按住时长即模板；
2. 同类连续出现只保留第一条，其余计入 `dropped.loop`；
3. 跳跃按 `JUMP_TAP_MS` 分成 `jump-short` / `jump-long` 两种节奏。

`record()` 在**入口**就用 `sequenceRejection` 拒掉带 `frames`/`totalMs`/`repeats>1`/`branches`/`path` 的样本，
不是"先存下来再过滤"——不让它进来，才不会被误用。
## 优先级（硬规矩）

```
知识库（docs/kb/**，静态规则）→ 编译成 src/rule-gate.js 的 RULES
  > 五维自检（prompts/meta-check.md）
    > 示范案例（data/demo-cases.jsonl，只影响时序/瞄准/等待）
```

三个模块各管一段，互不越界：

| 文件 | 角色 | 能不能改世界 |
|---|---|---|
| `src/rule-gate.js` | 静态规则（每条带 `docs/kb` 出处） | 不碰，纯判定 |
| `src/demo-library.js` | 动态样本（人怎么做） | 不碰，只存/查时序 |
| `src/observe-mode.js` | 模式开关 + 规划器（把两者接起来） | 只在 auto 且过闸后才出动作 |

### 闸门：所有动作的必经之路（`src/rule-gate.js`）

九条规则，`check()` 返回 true = 违反；每条都有 `id` / `reason`（稳定字符串）/ `kb`（出处）/ `why`（人话）：

| id | reason | 出处 | 拦什么 |
|---|---|---|---|
| `no-out-of-world` | `not-in-world` | safety_limit | 没进世界/已退出还下动作 |
| `no-dead-action` | `player-dead` | safety_limit | 角色已死 |
| `no-action-while-paused` | `paused` | safety_limit | 游戏暂停 |
| `hp-floor` | `below-safety-line` | safety_limit | 血量 <35% 还打/挖/放/用（**移动跳跃不拦**——低血往回跑是该做的） |
| `no-liquid-mining` | `liquid-mining` | safety_limit | 泡在水里开挖 |
| `no-uncontrolled-fall` | `uncontrolled-fall` | safety_limit | 竖直速度 >8 还在 move/jump |
| `no-multi-task` | `multi-task` | safety_limit | 边走边挖、边挖边放 |
| `no-command-shortcut` | `command-shortcut` | atomic_action | 用 `give`/`teleport`/`removeblock` 等直写世界 |
| `reach-check` | `out-of-reach` | safety_limit | 隔空操作（镐 7 格、剑 4 格、放/用 4 格） |

`always: true` 表示"任何任务都带"：`loadRuleSet({tags})` 按标签取子集时也一定会补上它们。

两个入口，缺一不可：

- `validateSample(sample)` —— **入库前**。不合法的示范直接丢弃，`record()` 回 `{ok:false,reason,rule,kb}`，日志能追到是哪条规则拦的。
- `gatePlan(plan, {snap, opts})` —— **执行前**。不过就回 `{ok:false, plan:null, violations:[…]}`；函数签名里**没有** force/override/bypass，传了也没用（`test/rule-gate.test.js` 有专门用例钉着这一点）。示范不能把禁止动作变成允许——这是"知识库永远优先"在代码里的落点。

### 示范只能动这五个数

| 参数 | 含义 | 允许范围 |
|---|---|---|
| `jumpHoldMs` | 起跳按键按多久 | 60–450 |
| `digHoldMs` | 挥镐/挖单格按多久 | 40–400 |
| `moveHoldMs` | 走一步按多久 | 80–1500 |
| `settleMs` | 动作后等落定的时间 | 120–600 |
| `aimLeadMs` | 瞄准提前量 | 0–400 |

越界一律夹回（`TIMING_BOUNDS`），知识库里的距离/伤害/配方判定一个字都不动。动作类型、方向、目标格这些语义字段永远以脚本推演为准。

## observe 模式干什么

1. **写命令全部拦下**：`move`/`mine`/`place`/`use`/`attack`/`craft`/… 一律不发给桥，但照样分配序号、照样进逐帧表（状态记 `blocked`）。这样"脚本本来想做什么"也被记录下来，成为对齐用的一股流。
2. **采真人输入**：轮询桥的 `humaninput`，拿到 `control{left,right,jump,useItem,useTile}`、鼠标世界坐标与目标格、按下的键、玩家位置/速度/手持物。
3. **三流对齐**：以"玩家格 + 手持物 + 移动键"为键把连续帧聚成一段（dwell），脚本侧同键的预推演动作落进同一段。一段 = 一个样本。
4. **差分**：每段算出真人 vs 脚本在 `dx / dy / hold / aimX / aimY / holdMs / 目标格` 上的差；同时命中同一类时，真人的按住时长被记成"修正值"。

对齐的容差（`ALIGN`）：位置 ≤1 格、目标格 ≤1 格、时长 ≤1.5 倍或 ±120ms 内视为同一动作，超了记成"多出来的一步 / 漏掉的一步"。

## 案例库长什么样

`data/demo-cases.jsonl`，一行一个样本（`version:1`）：

```json
{"at":"2026-01-01T00:00:00.000Z",
 "scene":{"zone":"surface","wet":0,"ground":1,"hp":"high",
          "dx":"3","dy":"2","threat":"clear","moving":"still"},
 "label":"surface|dry|ground|hp-high|dx3|dy2|threat-clear|still",
 "human":{"kind":"mine","dx":3,"dy":1,"digHoldMs":140,"settleMs":260,"aimLeadMs":120},
 "agent":{"kind":"mine","dx":3,"dy":2,"digHoldMs":90,"settleMs":260,"aimLeadMs":120},
 "delta":{"significant":true,"reason":"timing","diffs":{"digHoldMs":50}},
 "outcome":{"ok":true,"blocked":false}}
```

场景指纹用**分档**而不是原始坐标，换一张地图也成立（`sceneOf`，坐标一律不进键——进了就会"每格一个场景"，永远命中不了）：

| 维度 | 取值 |
|---|---|
| `zone` | 桥 `state.player.zones` 给的群系（surface/dirtlayer/underground/cavern/underworld/…） |
| `wet` | 0 / 1（泡在液体里） |
| `ground` | 0 / 1（脚下有没有地） |
| `hp` | `high` >75% / `mid` >45% / `low` >25% / `critical` ≤25% |
| `dx`、`dy` | 按 `[-8,-2,0,2,8]` 分档（目标相对方位） |
| `threat` | `near` ≤3 格 / `edge` ≤8 格 / `clear` |
| `moving` | `still` / `left` / `right` / `fall`（按速度判） |

相似度 = 加权匹配：`zone 0.26、wet 0.14、ground 0.14、hp 0.12、threat 0.12、moving 0.10、dx 0.06、dy 0.06`（`WEIGHTS`，`src/demo-library.js`）。`hp` 与 `threat` 差一档给部分分（0.45w / 0.4w），其余维度必须全等。低于 **0.72** 视为不像，宁可不给建议。

`actionDelta(agent, human)` 只在两种情况下算"值得记"：动作类型不同，或同类动作的按住时长差 ≥30%。否则观摩会退化成把脚本自己的重复动作抄一遍。

`tuneTiming(base, cases, {minSim=0.72, minSamples=3})`：按参数分组取**相似度加权中位数**（比平均值抗手抖），再夹进 `TIMING_BOUNDS`。返回 `{timing, applied, clamped}`——`applied` 每项带 `{param,from,to,sim,from_case}`，`clamped` 是被边界挡回去的部分。

## 切回 auto 时

规划器 `createAutoPlanner({library, rules})` 的顺序是**先推演 → 再调时序 → 最后过闸门**，顺序不能反：

1. `defaultPlanner(snap)` 看局面出草稿（只看局面，不碰世界）；
2. `resolveTiming({snap, library})` 召回示范、算分档（见下），把学到的**时长**叠到草稿上（动作类型/方向仍以推演为准）；
3. `gatePlan(candidate, {snap, rules})` 过闸门；不过就返回 `{kind:'hold', blocked:true, violations}`——**不出动作**。

`resolveTiming` 的分档，是为了解决"要三条样本才改"和"这个具体局面人就该这么按，而它只出现过一次"之间的矛盾：

| 档 | 条件 | 生效门槛 |
|---|---|---|
| `exact` | 同 `label` 且相似度 ≥0.9 | 1 条就生效 |
| `loose` | 只是相似（≥0.72） | 仍要 3 条 |
| `none` | 没有够像的示范 | 用基础时序 |

分档只影响"要用几条样本"，不影响边界：`TIMING_BOUNDS` 与知识库闸门照旧管着。
日志用 `formatTimingReport(plan)` 出一行：`[timing] tier=exact source=demo sim=1.00 rules=9`，
后面跟「`digHoldMs 90→140ms`」这类实际改动；被拦时再跟「拦在 `hp-floor`（safety_limit）」。

## CLI

```bash
npm run observe -- status                  # 当前模式 + 库里有多少样本
npm run observe -- observe --ms 60000      # 观摩 60 秒（只读），结束自动切回 auto 并出报告
npm run observe -- observe --ms 0          # 只采一帧、对齐入库，打完报告立刻退出
npm run observe -- auto                    # 手动切回自动（观摩中途被打断时用）
npm run observe:report                     # 只读报告：样本数/丢弃原因 + 被示范改过的时序参数
```

`--every N` 改采样间隔（毫秒，默认 200，下限 50）。观摩期间脚本只读：`createGuard` 会把任何非只读命令挡成
`OBSERVE_MODE_BLOCKED`，所以"观摩模式误操作"这条路是封死的，不是靠自觉。

样本入库要过四道闸，前两道在采集侧，后两道是知识库闸门：

| 丢弃原因 | 谁挡的 | 含义 |
|---|---|---|
| `human-idle` | `flushSegment` | 人整段没动手——这是没示范，不是人做错了 |
| `not-significant` | `actionDelta` | 和脚本推演差得不明显，抄一遍没意义 |
| `below-safety-line` / `liquid-mining` / `uncontrolled-fall` / `player-dead` / `paused` / `not-in-world` | `rule-gate.js` | **人做的这件事本身违背成文规则**，不学 |
| `out-of-reach` / `multi-task` / `command-shortcut` | `rule-gate.js` | 隔空操作、边做两件事、走捷径改世界，同样不学 |

丢弃时 `record()` 回 `{ok:false, reason, rule, kb}`，日志按 `reason` 计数；样本少时先看这一行——若某一类占多数，问题在采集或玩法，不在检索。

## 桥里的新命令

`humaninput`（只读）：返回真人在这一帧对控制器的输入与鼠标落点。

```json
{"tick":123,"paused":false,"focus":true,
 "control":{"left":false,"right":true,"up":false,"down":false,
            "jump":false,"useItem":false,"useTile":false},
 "mouse":{"left":false,"right":false,"x":640,"y":360,"tileX":2120,"tileY":300},
 "keys":[], "player":{"tile":{"x":2118,"y":299},"velocity":{"x":3.1,"y":0},"…"}}
```

它是观测，不是注射：只读 `Main.mouseState` / `Player.control*` / `Main.keyState`，不写任何字段。
`mouse` 的前两个字段是**真人**按键状态，脚本自己合成的 `controlUseItem` 不算在内（`PreItemCheck` 期间写进去的值不会被误认成真人操作）。

## 边界（明确不做）

- 不开第二个游戏进程、不读内存、不装抓包钩子——样本只来自桥上能看见的东西。
- 不做"用示范替换知识库"：库里没有的机制，样本再多也不会被采纳。
- 不采集画面语义（截图只用于人看的证据，不进样本）。
- 不跨存档共享样本：`data/` 是本机运行期数据，已进 `.gitignore`。