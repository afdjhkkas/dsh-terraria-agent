# 观摩模式与示范案例库

两个模式，同一条执行链，可随时切换：

- **auto**（默认）：脚本自己下指令，凭知识库 + 自检推进。
- **observe**：脚本**只读不写**。同一个回合里既采真人操作，又对同一帧做脚本预推演，把两股流对齐差分，存进示范案例库。切回 auto 后，遇到相似场景就把当初的修正拿出来用。

## 优先级（硬规矩）

```
知识库（docs/kb/**，静态规则）
  > 五维自检（prompts/meta-check.md）
    > 示范案例（data/demo-cases.jsonl，只影响时序/瞄准/等待）
```

示范案例**只能**微调下面四个数值，永远不能改规则、加命令、放宽安全限制：

| 参数 | 含义 | 允许范围 |
|---|---|---|
| `jumpHoldMs` | 起跳按键按多久 | 60–450 |
| `digHoldMs` | 挥镐/挖单格按多久 | 40–400 |
| `moveHoldMs` | 走一步按多久 | 80–1500 |
| `settleMs` | 动作后等落定的时间 | 120–600 |
| `aimLeadMs` | 瞄准提前量 | 0–400 |

超出范围就被夹回边界（`clampTiming`），知识库里的距离/伤害/配方判定一个字都不动。

## observe 模式干什么

1. **写命令全部拦下**：`move`/`mine`/`place`/`use`/`attack`/`craft`/… 一律不发给桥，但照样分配序号、照样进逐帧表（状态记 `blocked`）。这样"脚本本来想做什么"也被记录下来，成为对齐用的一股流。
2. **采真人输入**：轮询桥的 `humaninput`，拿到 `control{left,right,jump,useItem,useTile}`、鼠标世界坐标与目标格、按下的键、玩家位置/速度/手持物。
3. **三流对齐**：以"玩家格 + 手持物 + 移动键"为键把连续帧聚成一段（dwell），脚本侧同键的预推演动作落进同一段。一段 = 一个样本。
4. **差分**：每段算出真人 vs 脚本在 `dx / dy / hold / aimX / aimY / holdMs / 目标格` 上的差；同时命中同一类时，真人的按住时长被记成"修正值"。

对齐的容差（`ALIGN`）：位置 ≤1 格、目标格 ≤1 格、时长 ≤1.5 倍或 ±120ms 内视为同一动作，超了记成"多出来的一步 / 漏掉的一步"。

## 案例库长什么样

`data/demo-cases.jsonl`，一行一个样本（`version:1`）：

```json
{"version":1,"id":"…","at":"2026-01-01T00:00:00.000Z","mode":"observe",
 "scene":{"zone":"surface","depth":"surface","liquid":"dry","light":"day",
          "threat":"none","terrain":"flat","moving":false,"tool":"pickaxe",
          "target":"solid"},
 "signature":"surface|surface|dry|day|none|flat|not-moving|pickaxe|dx3|dy2",
 "script":{"kind":"mine","dx":3,"dy":2,"holdMs":90,"aim":{"x":0,"y":0},"tileTarget":{"x":0,"y":0}},
 "human":{"kind":"mine","dx":3,"dy":1,"holdMs":140,"aim":{…},"tileTarget":{…}},
 "delta":{"dx":0,"dy":-1,"holdMs":50,"aimX":0,"aimY":0,"tileDx":0,"tileDy":0},
 "outcome":{"ok":true,"blocked":false,"hpDrop":0}}
```

场景指纹用**分档**而不是原始坐标，换一张地图也成立：深度（surface/underground/cavern/underworld）、液体（dry/wet/lava/honey/shimmer）、光照（day/night/dark）、威胁（none/near/far）、地形（flat/step/wall/pit）、手持物类别（pickaxe/axe/sword/block/torch/other）、目标格相对方位（水平 `dx` 分 2/3/4/5/6/8 档，垂直 `dy` 分 0/1/2/3 档）。

相似度 = 加权匹配（zone 0.30、depth 0.14、terrain 0.12、liquid 0.10、threat 0.10、tool 0.08、target 0.06、light 0.04、moving 0.04、dx 0.06、dy 0.04、threat 0.02），同分再按时间新近度破平。低于 **0.72** 视为不像，宁可不给建议（早期草稿写过 0.55，实际取值以代码为准）。

`tuneTiming(scene, cases)`：把同场景、同一动作类、且**真人确实成功**（`outcome.ok`）的样本按 `moveHoldMs + delta.holdMs` 这样的"实际按住时长"取加权中位数，再夹进上表边界。样本不足 3 条不改。

## 切回 auto 时

`applyRecall(recall)` 把 `tuneTiming` 的结果并进当前执行参数；`explainRecall(recall)` 给出人话：
「本场景有 7 条示范；挖矿按住时长 90ms → 137ms（7 条样本）」。
日志固定报三行：本局采集样本数、命中场景数、实际改了哪几个参数。

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

样本入库有三道闸：**人整段没动手**（`human-idle`）、**和脚本推演差得不明显**（`not-significant`）、
**人在死亡/不在世界里**（`player-dead` 等）。丢弃原因会按类计数回显，样本少时先看这行再怀疑采集链路。

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