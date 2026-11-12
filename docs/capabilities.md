# 能力手册（实测状态）

本文件是**能力的唯一权威清单**：每条都写清「命令 → 参数 → 回读证据 → 踩过的坑」。
没有实测证据的一律不写"可用"。

## 三层结构

| 层 | 位置 | 作用 |
|---|---|---|
| ① mod 桥命令 | `DshTerrariaBridge/Commands.cs` | 真正干活的层，60 条命令，游戏主线程内执行 |
| ② 插件工具 | `index.js` + `src/primitives.js` | 模型调用的 `terraria_*`，负责轮询验证、重试、聚合 |
| ③ 世界事实 | `docs/terraria-mechanics.md` | 坐标/方块编码/放置支撑/房屋判定等硬知识 |

**状态图例**：✅ 实测可用 ｜ ⚠️ 可用但有明确限制 ｜ ❌ 已知不可用 ｜ 🚧 未实现／未验证

---

## 0. 一页总表

| 分类 | 能力 | 命令 | 状态 |
|---|---|---|---|
| 感知 | 角色/世界状态 | `state` | ✅ |
| 感知 | 区域方块快照 | `scan {x,y,r,walls}` | ✅ |
| 感知 | 找掉进去出不来的坑 | `findpit {x,y,r,minDepth}` | 🚧 代码已编译、测试已加，**未做真机验证** |
| 感知 | 单格方块 | `tile {x,y}` | ✅ |
| 感知 | 按类型找方块 | `find {type,x1,x2,y1,y2,max}` | ✅ |
| 感知 | 附近生物 | `npc {r}` | ✅ |
| 感知 | 背包 | `inventory` | ✅ 主背包、货币栏、弹药栏、垃圾桶槽位 |
| 感知 | 当前该做什么 | `intent {path}` | 🚧 读进度表算 stage/missing/blockedBy；代码已编译、判据有测试，**未做真机验证** |
| 感知 | 地上掉落物 | `items {r}` | ✅ |
| 感知 | 玩家列表 | `players` | ✅ |
| 感知 | 游戏聊天读取 | `chatpoll {since,max}` | 🚧 代码已编译打包，待本轮游戏内验收；设计上只收原版真实发送的完整消息 |
| 感知 | Boss / 事件状态 | `boss` / `event` | ✅ |
| 感知 | 房屋判定 | `housing {x,y}` / `needs {x,y}` | ✅ |
| 移动 | 逐帧方向控制 | `move {dir,ms,terrain,force}` / `jump {ms,tiles,count}` | ✅ `move` 会先预走一遍地形，见 §1 的地形闸门 |
| 移动 | 掉坑自救 | `escape {maxMs}` / `escape {report:true}` | 🚧 桥内每 tick 自评（站稳 / 高 2 格 / 连通变宽），60 次无进展判 `stuck` 并报左右上下各几格；代码已编译、判据有测试，**未做真机验证** |
| 移动 | 爬绳 | `climb {dir,ms}` | ⚠️ 仅绳子 |
| 移动 | 走到坐标 | `walkto {x,y,tol,ms}` | ⚠️ 平坦可用 |
| 移动 | A* 寻路 | `path {x,y,ms,dig}` | ⚠️ 空腔竖井会死锁 |
| 移动 | 定向掘进 | `travel {dir,distance,dig}` | ⚠️ 仅水平方向 |
| 移动 | 回城 | `mirror` | ✅ |
| 移动 | 抓钩 | `hook {x,y,ms}` / `unhook` | ✅ |
| 移动 | 坐骑 | `mount {action}` | ✅ 骑上／下骑都成，失焦下也有效（靠 `preOpenTicks` 预开闸） |
| 移动 | 飞行 | `fly {ms}` | ✅ 自动选键：翅膀/普通飞行坐骑走跳键、悬停坐骑走上键（回包 `key`/`hovering` 可核对） |
| 移动 | 飞行·悬停坐骑 | `fly {ms}` | ✅ UFO 类悬停坐骑走**上键**：真机 `key:"up"`、`hovering:true`，地面升 37.71 格、空中再按又升 25.73 格，全程 `wingTime` 不消耗 |
| 挖掘 | 真实挥镐挖一格 | `mine {x,y,ms,guard}` | ✅ |
| 挖掘 | 挥锤拆方块 | `removeblock {x,y,ms}` | ✅ |
| 建造 | 放一格方块 | `place {x,y,type,ms,guard}` | ✅ |
| 建造 | 放墙 | `wall {x,y,type,ms,remove}` | ✅ |
| 建造 | 矩形铺地 | `fill {x1,y1,x2,y2,type}` | ⚠️ 需支撑、自下而上、≤400 格 |
| 物品 | 制作 | `craft {item,times,guard}` | ⚠️必须贴近工作站 |
| 物品 | 丢弃 / 拾取 | `drop {slot,count}` / `grab {r}` | ✅ |
| 物品 | 箱子 | `chest {action,slot,count}` | 🚧 箱子未建，未实测 |
| 物品 | 穿护甲 / 饰品 | `equip {slot}` / `accessory {slot,from,off}` | ✅ |
| 物品 | 研究 / 复制 | `research {slot}` / `duplicate {item,count}` | ✅ |
| 战斗 | 选武器 | `select {item,slot}` | ✅ |
| 战斗 | 射箭 | `shoot {x,y,ms}` | ✅ |
| 战斗 | 自动防守 | `guard {r,ms}` / `guard {off:true}` | ✅ 默认开启 |
| 战斗 | 召唤 | `minion {slot}` | ✅ |
| 战斗 | 回血 | `heal` | ✅ 需带药水 |
| 战斗 | 旧式点名攻击 | `attack {r,who,ms}` | ⚠️ 走伤害管线但无弹幕，优先用 `shoot`/`guard` |
| NPC | 对话 | `talk {npc}` / `chat {text}` | ✅ 走真实右键；≤5 格且有视线才 `armed:true`。`use {right:true}` 是它的等价底层 |
| NPC | 商店 | `shop{npc}` / `shops{want}` / `buy` / `sell` | ✅ |
| NPC | 入住房间 | `homeroom {who,x,y}` | ⚠️ 房间判定必须先通过 |
| NPC | 跟随 / 待命 | `follow {player}` / `idle` | ✅ |
| 存档 | 保存世界 | `save` | ❌ 联机开服模式下不可用（见 §7） |
| 调试 | 反射查 API | `api {type,member}` | ✅ |
| 调试 | 配方检索 | `recipes {q,limit}` | ✅ |
| 调试 | 方块元数据 | `tiledata {type}` | ✅ |
| 调试 | 命令清单 | `help` | ✅ |

---

## 1. 感知

### `intent` — 当前该做什么（读进度表，只读）

给"新开一个对话，不知道现在该干什么"用。它不推理，只做三件事：读进度表 → 数背包 → 算缺口。

```json
{"cmd":"intent","params":{}}
→ {"ok":true,"data":{
    "stage":"iron_gear","stageLabel":"铁器时代","goal":"做铁砧+铁阔剑",
    "done":false,
    "missing":[{"id":22,"label":"铁锭","have":2,"need":6}],
    "blockedBy":["缺 铁锭（22）：有 2，要 6","铁锭要熔炉，熔炉要先有 20 石头 + 3 火把"],
    "suggestedNext":"挖铁矿 → 熔炉烧成铁锭 → 做铁砧 → 做铁阔剑",
    "whyNotOther":"骷髅王/克苏鲁之眼前置是铁器，现在打不动",
    "confidence":0.95,"advanced":[],"stageOrder":["wood","iron_gear","eye_of_cthulhu","evil_boss"],
    "source":"C:\\...\\goal-progress.json"}}
```

- **数据源**：`goal-progress.json`（字段 `stage` / `stageOrder` / `stages{goal,checks,blocked,next,guard}`）。
  查找顺序：`intent {path}` 显式 → 环境变量 `DSH_GOAL_FILE` → `My Games/Terraria/tModLoader/goal-progress.json`
  → 同目录 `ModSources/DshTerrariaBridge/goal-progress.json` → 进程工作目录下同名文件。
- **判据**：`checks` 逐条比对背包数量，字段名是 `type`（不是 `id`）。`anyOf` 表示"任意一个都算"，取最大值、不求和。
- **自动推进**：当前阶段判据全满足 → 沿 `stageOrder` 往后挪一格并记进 `advanced`。**只在内存里推进，不写文件**；
  要让下次开局看到新阶段，得自己改 `goal-progress.json` 的 `stage`。
- **失败就说失败**：文件读不到 / JSON 不合法 / 顶层缺 `stages` → `ok:false` + 试过的路径清单，不猜。
- **纯只读**：不开状态机、不切槽、不写世界、不写文件，一帧算完。

### `guard` — 动手前先问一句"这属于当前阶段吗"

`mine` / `place` / `craft` 三条各有可选参数 `guard`（**默认 false**，不写就是老行为）。

```json
{"cmd":"mine","params":{"x":2100,"y":300,"guard":true}}
→ {"ok":false,"error":"当前阶段是「铁器时代」，目标是：做铁砧+铁阔剑。挖 猩红石(203) 不属于这一步。"}
```

- 只有阶段里写了 `"guard":{"strict":true}` 才拦合成品；`"guard":{"world":false}` 表示这一阶段不动世界，mine/place 直接拦。
- 是**提醒不是锁死**：`guard:false` 或干脆不传，照样执行。判据宁可放过，不误拦。

### `state` — 角色 + 世界

**蓝量：`mana` / `manaMax` 就是自己的蓝**（`Commands.cs` 里取 `pl.statMana` / `pl.statManaMax2`）。
想知道"这次施法到底耗了多少蓝"，做法是**采最低点**，不是 `use` 完立刻读：

```
use {ms:300} → 每 150ms 连续读 mana（读 8 次）→ 记录最低点 → 与施法前的值相减
```

`use` 回包时人还没开始施法，扣蓝发生在几帧之后；立刻读会得到"耗 0"的假结论。
施法后蓝会自己回，所以过了几秒再读同样看不出来。真机数据：终极棱镜单次 **12 点**
（200→188），四次连续施法都是 12，每次之间等到回满再测。
返回 `player{ tile,pos,velocity,life,lifeMax,mana,dead,direction,grounded,breath,wet,held,buffs,zones,
minions,minionSlots,grapCount,hookType,mounted,wingTime,hasWings,itemAnimation,playerIndex }`、
`world{ name,width,height,dayTime,time,raining,bloodMoon,eclipse,hardmode,tick }`、
以及 `guard / attack / path / walk / travel` 各作业状态。`ropeContact` 额外报告 `pulley`、`ropeCount`、
碰撞箱内绳格、脚底 `tileFeetY/tileFeetType` 和 `onRopeReal`；动作决策只使用 `onRopeReal`。
诊断 `inputDiag` 比对 `Main.myPlayer`、
`Main.LocalPlayer.whoAmI`、`Main.player[Main.myPlayer].whoAmI` 与响应玩家索引；并保留
`postUpdateInput` 的 Triggers 快照和 `postUpdateRunSpeeds` 的 `Player.control*` 快照（含采样 tick 与 whoAmI），
用来判断输入在哪个阶段丢失。`state.flagLeft/flagRight/flagUp/flagDown/flagJump`（`player` 内也有兼容副本）
是 `ApplyNativeControlFlags()` 返回前保存的当帧控制位，才是控制输出的可信证据；`dbgCtrlUp` 读
`PlayerInput.Triggers.Current.Up`，`dbgPlayerControlUp` 与 `humaninput.control.*` 都是在原版更新后读取的兼容观察值，
不能用来证明某一帧是否曾经置过输入。

- **`zones`** 是关键：`corrupt/crimson/jungle/snow/desert/beach/dungeon/glowshroom/meteor/hallow/underworld/rocklayer/dirtlayer/sky/surface`，找群系不用盲挖。
- `grounded` 要求「本帧与上一帧速度都为 0」，斜坡上站着不会误报。
- 证据：`state.player.grapCount` 0→1→0、`minions` 0→1、`itemAnimation` 16/8/0 全部读到过。

### `chatpoll {since,max}` — 读取玩家真实发送的聊天

桥挂在原版 `Main.DoUpdate_HandleChat`，只读观察原版聊天流程，不注入键盘、不清空输入框，也不改变
消息发送。收件条件镜像原版发送分支：`inputTextEnter && chatRelease && chatText.Length > 0`；
因此输入框每次变长不会产生碎片，按 Esc 取消也不会被误报成已发送消息。

返回字段：

| 字段 | 含义 |
|---|---|
| `lines` | 自 `since` 起的完整聊天消息；`/` 开头且被原版命令处理的文本不会进入这里 |
| `next` / `pending` | 增量游标与尚未读取的消息数 |
| `hooked` / `hookError` | hook 是否安装成功及错误 |
| `hookCalls` | `DoUpdate_HandleChat` 被调用次数，用来确认聊天 hook 是否持续运行 |
| `pendingText` / `pendingSinceTick` | 当前仍在输入框中的文本及开始观察的 tick |
| `lastEvent` / `lastCandidate` | 最近一次 `sent`、`command` 或 `cancel` 及其原始文本 |
| `lastSendTick` / `lastCancelTick` | 最近一次真实发送/取消的 tick |
| `dropped` | 环形缓冲淘汰的旧消息数 |

验收时先记住 `next`，发送一条普通消息后轮询同一个桥，期望 `lastEvent:"sent"`、`lines` 只增加一条
完整文本、`pendingText:""`。只输入不发送时应只有 `pendingText`；按 Esc 时应为 `lastEvent:"cancel"`，
`lines` 不增加。`chatpost` 是桥向游戏显示文本的写出命令，不会伪造 `chatpoll` 的输入消息。
当前实现已完成 Release 编译与带 `-eac` 打包；本轮尚未在游戏内验证真实发送、Esc 取消和 `/` 命令三条路径，验收通过前不标为实测可用。

### `scan {x,y,r,walls}` — 网格快照
返回 `(2r+1)²` 网格，每格 `[type,wall,liquid,shape]`，`r ≤ 48`。**只覆盖已加载区段**（约玩家周围 ±100 格）。

`format:"ascii"` 的符号区分类型：`#` 实心（`tileSolid`）· `^` 半砖/斜坡 · `T` 树 · `,` 草/植物 · `-` 平台 · `+` 门 · `o` 火把 · `v` 藤蔓 · `;` 其他非实心 · `~` 水 · `L` 岩浆 · `.` 空。

**读地形必须看符号（或 `rows`/`tile` 的 `type`），不能把 `#` 一律当墙。** 以前所有非空格都画 `#`，树、平台、门和石墙长得一模一样：照图读会把整片树林当墙、把树干顶当成地表高度、把树间的空当成坑——同一条错判连续发生过三次（砍错树、误报 7~9 格落差、找不到脚边的坑）。

### `findpit {x,y,r,minDepth}` — 找"掉进去出不来的坑"
默认 `x/y` = 玩家位置，`r` = 24（≤48），`minDepth` = 6（裸跳约 5.5 格）。
判据全部用 `tileSolid`，不看形状：**连续空 ≥ minDepth + 下面有实心底 + 下半段至少一侧有 ≥ max(2, 深度/2) 格连续实心壁**；树/草/火把/平台按非实心处理，不算壁。
返回 `{count, pits:[{x0,x1,center,depth,floorY,dist}]}`。为什么要有它：ascii 丢类型，树干会盖住坑口，靠读图找坑会连错。

### `tile {x,y}` — 单格详情
`{hasTile,type,name,wall,liquid,slope,halfBlock,actuator,solid,solidTop,frameX,frameY}`。
注意 `type: -1` = 空气，**`0` 是泥土不是空气**。

### `find {type,x1,x2,y1,y2,max}` — 按 tile id 搜方块
返回 `{total,found[]}`。找祭坛（26）、水（25/203）、铁（22/204）、火把（4）都用它。
证据：基地周围 `find type:4` 数到 43 根火把。

### `npc {r}` — 附近生物
`{who,type,name,tile,life,lifeMax,friendly,boss,hostile}`；`who` 是 `Main.npc` 索引，**跟踪同一个怪要用 `who`，不要用名字+坐标**（会认错）。

### `inventory` — 背包
`{items:[{slot,id,name,stack,prefix,rare}], main_slots_used, free, ammo, coins}`。快捷栏 = slot 0-9。

### `items {r}` / `grab {r}`
`items` 读地上掉落物 `{count,items[]}`；`grab` 走原版拾取路径 `Player.GetItem`，**半径默认 8 格，物品必须靠近脚下**才能捡到。

### `roomcheck {x,y}` — **游戏自己的**房屋判定
直接调 `WorldGen.StartRoomCheck` / `CheckRoom` / `RoomNeeds`，回 `{ok, needs, reason, reasonValue, tiles, box, steps[]}`。
这是「NPC 到底会不会住进来」的唯一真值；自家 `housediag` 只用来解释差在哪。

### `housediag {x,y}` — 自己的房屋诊断
flood fill 出房间空气格，逐项给 `walled / floorRatio / ceilRatio / leftRatio / rightRatio / doors` 与 `missing[]`。
`valid` 以 `gameOk`（游戏判定）为准 —— 早期用自家 `missing.Count==0`，会把一间游戏判 ok 的房子误判成不合格。

### `screentoTile {sx,sy}` — 屏幕坐标 → 瓦片
回 `{screen, world, tile}`，用来把截图里看到的东西对准到格子。

### `humaninput` — 真人输入原样读回（兼容观察）
`{tick, keys, control{left,right,jump,up,down,mouseLeft,mouseRight}, player{playerIndex,tile,velocity,grounded,life}, inputDiag, mouse{left,right,x,y,tileX,tileY,os,source,inClient}}`。
`inputDiag` 的字段为 `playerIds{myPlayer,localPlayerWhoAmI,indexedPlayerWhoAmI,responsePlayerWhoAmI}`、
`postUpdateInput{tick,whoAmI,left,right,up,down,jump}`、`postUpdateRunSpeeds{tick,whoAmI,left,right,up,down,jump}`。
前者采样 ModSystem 注入后的 `PlayerInput.Triggers.Current`，后者采样原版更新后 `Player.control*`；
两阶段字段各带自己的 tick 和玩家索引。`humaninput.control.*` 是命令线程晚读值，原版可能已经在
`Player.Update` 后重置，**不能作为当帧控制证据**；要判断桥是否真的置上控制位，读 `state.flag*`（或
`state.player.flag*`）以及对应的 `flagTick`。`state.inputDiag` 只用于阶段和玩家索引诊断，
`state.player.playerIndex` 与 `humaninput.player.playerIndex` 标识各自回读的玩家。
`mouse.source` = `os`（Win32 `GetCursorPos`+`ClientToScreen` 实测，窗口失焦也准）或 `xna`（`Main.mouseX/Y`，窗口不活跃时会冻在最后一次有效值）。
录示范时必须用它，否则学到的是"操作者为了配合测试而改变的操作"。

### `keyevents {since,clear}` — tick 级按键事件
回 `{events[{tick,key,kind}], latest}`。配合 `keystream.mjs` 能把真人操作切成原子动作（`src/atom-model.js`）。
**坑**：增量拉取要用"上一批的最大 tick 不减一"，否则同一 tick 的事件会重复。

### `key {key,ms}` — 真实 Inventory / Escape 键
`key:"inventory"` 和 `key:"escape"` 都只发送 Terraria 的 `Triggers.Current.Inventory`、按下沿和松开沿，
让原版 `Player.ToggleInv()` 自己决定结果。默认键位里 Inventory 绑定的是 Escape，所以商店打开时先发
`key {key:"inventory"}`（或 `escape`）会按原版顺序关闭 NPC 对话/商店；没有对话时才打开或关闭背包。
命令返回 `untilTick` 和发送前状态，必须在该 tick 之后回读 `state.playerInventory`、`talkNPC`、`npcShopIndex`。

### 其余
`players` 多人列表、`boss {active,bosses}`、`event`（血月/日食/雨）、`housing {x,y}`（房间判定 + 无家可归人数）、`needs {x,y}`（房间尺寸/失败原因）、`help` 命令清单。

> **`needs` 的 `roomDoor/roomChair/roomTable/roomTorch` 那组布尔字段不可信**：房间完全合格、`roomcheck` 已经 `ok:true`、NPC 已入住之后，它们照样回 `false`。
> 判房间合不合只看三条：`roomcheck {x,y}` 的 `ok`、`housediag` 的 `gameOk`、`housing` 的 `homeless`。
> `needs` 只用来读 `numRoomTiles`、`roomCheckFailureReason`、`roomX1..roomY2`（2026-11-03 真机：10×7=70 格的合格房仍回 `roomTorch/roomDoor/roomChair/roomTable` 全 false）。

---

## 2. 移动

| 命令 | 说明 | 证据 / 限制 |
|---|---|---|
| `move {dir,ms}` | 按住原版方向键输入（速度、碰撞和台阶由游戏计算）；脚底真在绳上时先按下脱绳，再开始左右动作。**发出前先沿 dir 预走一遍地形**（`terrain` 默认 true）：遇到 >2 格落差直接拒绝并给出 jump / 绕 / 搭桥三个选项；**落差不大也可能拦**：落点能连通的格数 <40 且不到当前一半就判 `trap`（实测 (1796,309) 深 2 格、宽 3 格、头顶半砖，掉进去 `regions` 回 `trapped` / `cells:0`，`escape` 跳 19 次仍 `gainY:0`）。`force:true` 可硬走 | 1.4 秒顶墙位置不变，证明碰撞生效；脱绳过程看 `ropeRelease`；无绳/无翅膀时 `up` 只报告输入已发出，不承诺垂直位移 |
| `moveto {x,ms}` | 位置闭环控制：读取原版位置/速度，按实测惯性距离提前松开方向键，必要时用反向原版输入修正 | 读 `state.exact`；只有 `active:false, stopReason:"arrived"` 且位置误差 ≤ 0.08 格、速度 ≤ 0.12 才算到位；`blocked` 表示连续 45 帧几乎不动 |
| `jump {ms}` | 按住原版跳跃键输入（高度由按住时长和游戏物理决定）；脚底真在绳上时先按下脱绳，再开始跳跃 | 起跳/上台阶；控制证据看 `state.flagJump` |
| `climb {dir,ms}` | **只在脚底真实绳格上**才生效；身体中段碰到绳不算挂绳 | `ropeContact.onRopeReal` 与 `tileFeetType` 先确认，再看纵坐标回读 |
| `walkto {x,y,tol,ms}` | 走到坐标。内部是 **感知→决策→执行** 三层：每 tick 扫 8 列地面（落差/墙高/是否建材/是否门/是否坑），执行层只发方向/跳跃/右键交互输入。**出发前先验目标格**：不能站（身体被挡或脚下没地板）就直接拒绝并给出最近可站点，`force:true` 跳过校验 | 已能穿过自建房的关门（右键交互由原版处理）、跳过 1~6 格台阶、跨 ≤3 格坑；⚠️「地面→实心屋顶」这种组合仍需实机复测。目标不可站时 A* 只会回 `no-path`，看起来像"没路/被墙挡住"——实测因此把 8 格深的坑口当成走不过去 |
| `walkmode {mode}` | `legacy` / `reactive` / `new` 切换走路实现，用来同场景对比两套 | 回包带当前模式；层级为首次改动的隔离用 |
| `path {x,y,ms,dig}` | A* 寻路（离线镜像 `src/path-model.js` 的 C# 版，四类边：走/跨/跳/落） | 失败时回 `why`（含起点终点是否可站、`opened` 计数）。⚠️ 兜底包围盒 ±10 |
| `travelstatus` | 走路的实时内部状态（`active/stop/rule/kind/wp/lu/ru/greedy`） | 诊断"路点在左、控制却向右"这类问题的唯一手段 |
| `travel {dir,distance,dig}` | 定向掘进循环 | ⚠️ **只用水平方向**：`down` 会沿斜坡漂移十几格并淹死过人 |
| `mirror` | 魔镜回城 | 任意位置 → 出生点 |
| `interact {x,y}` | 压准星并发送一次原版右键输入，开门 / 开箱 / 和 NPC 对话由游戏结算 | 命令只返回输入已发送；必须在 `untilTick` 后用 `tile` / `chest` / `state` 回读 |
| `hook {x,y,ms}` | 压准星并发送原版抓钩左键输入，弹幕和拉力由游戏结算 | 必须在 `untilTick` 后回读 `state.player.grapCount` / `hookdebug`；未实测前标为 🚧 |
| `unhook` | 发送松开抓钩和短跳输入，脱钩由游戏结算 | 必须回读 `grapCount`；不再直接杀弹幕 |

### 基础按键输入

移动、跳跃、爬绳、平台下落、使用物品、右键交互、抓钩、坐骑和飞行都只写游戏输入层：移动/跳跃最终在
`SetControls` 后的 `PostUpdateMiscEffects` 输入阶段重新落到 `Player.control*`，让原版绳索、抓钩和 `ItemCheck`
读取到同一帧的输入；`PlayerInput.Triggers.Current` 负责边沿与诊断，鼠标动作走 `Main.mouseLeft` /
`Main.mouseRight` 及释放边沿，物品结果由 `ItemCheck` 和原版物理结算。桥不写 `velocity`、`position`、
`pulley`、`grapCount` 或弹幕结果。每个改变世界或角色状态的动作都必须在命令返回后回读确认。
| `fly {ms}` | 飞行 | 按坐骑类型自动选键。回包 `key:"jump"` 表示走了跳键（翅膀/普通飞行坐骑），`key:"up"` 表示走上键（悬停坐骑）；`hovering` 直接告诉你判成了哪种 |
| `fly {ms}`（骑悬停坐骑） | 飞行 | 同一条命令自动走上键。真机：`宇宙车钥匙 mountType:7` 骑乘中 `key:"up"`，地面升 37.71 格、空中再按升 25.73 格，`wingTime` 全程 0（悬停不耗飞行时间） |
| `mount {action:"ride"}` | 坐骑（手动） | 掏出坐骑物品按左键。**只查背包**，而坐骑在 `miscEquips[3]`，所以会错过装备栏里那把钥匙 |
| `mount {action:"quick"}` | 坐骑（快捷） | 走 `PlayerInput.Triggers.Current.QuickMount` + 预开闸 `preOpenTicks`；骑上/下骑同一个命令来回切 |
| `mount {action:"info"}` | 坐骑 | 读 `miscEquips[3]` 的 `mountType`；无坐骑时 `{active:false}` |

**坐骑为什么必须"预开闸"**：原版 `Player.Update` 的 `if (controlMount) { if (releaseMount) QuickMount(); releaseMount = false; } else { releaseMount = true; }`
整段挂在 `if (Main.hasFocus)` 内，而 `releaseMount` 字段默认 `false`。**失焦时 `releaseMount` 永远是 false → `QuickMount()` 永不进入
→ 能骑上（走 `controlUseItem` 那条路）但永远下不来**。解法：命令发出时先让 `Main.hasFocus` 真 6 帧、这段时间不按键，把
`else releaseMount = true` 跑完；第 7 帧起才按 `controlMount`。回包里那个 `preOpenTicks: 6` 就是它。

`Player.miscEquips` 是五格固定布局：`[0] pet`、`[1] lightPet`、`[2] minecart`、`[3] mount`、`[4] hook`。
矿车和普通坐骑都带 `mountType`，判断是否为矿车必须再查 `MountID.Sets.Cart[mountType]`，不能只看
`mountType` 是否为正数。`mount {action:"quick"}` 只读取 `[3]`，抓钩链只读取 `[4]`。

**竖井闭环**（唯一可靠的下行方式）：逐格 `mine`，每格后等 1.5 秒让角色落到下一格；碰撞盒 20px 宽 ≈ 1.25 格，要挖的两列随中心点小数位变化：
`[floor(cx-0.55), floor(cx+0.55)]`。井底铺绳再用 `climb` 爬出。

---

## 3. 挖掘与建造

### `mine {x,y,ms}` — 真实挥镐
选镐 → `BeginItemUse` → 原版 `ItemCheck` 挥击 → 物品栏真出方块。
`ms` 是**兜底上限**（默认 8000，夹在 300..20000）：桥每 tick 回读目标格，一挖空立刻收手，
所以不必再按硬度手调时长（早期"默认 1300ms"的说法已过时）。

回包三样：`before:{hasTile,type,frameX}`（挥动前的真实状态，调用方不用猜 baseline）、
`armed:true`（**只代表「已经开始挥」**）、`verify`（说明文案）。
判真伪只认事后 `tile` 回读：`hasTile === false` 才算掉了。

**够不到 / 没工具时外层直接是 `ok:false`，没有 `data`**（内层 `ok:false` 原样抛回）——
脚本必须先判 `ok` 再判 `data`，见 `AGENTS.md` §0.58。

真机（2026-11-03，2026 年 11 月那版 tmod，224053 字节）：`mine` 命中玩家脚下 3.16 格处的土方块
→ `before:{hasTile:true,type:0,frameX:36}`，回读 `hasTile:false`；
同一命令指向 20 格外的格子 → `ok:false`「够不到：离这一格超过 7 格，先走过去再挖」且无 `data`。

### `removeblock {x,y,ms}` — 真实挥锤
同上路径，锤子拆墙/家具。

### `place {x,y,type,ms}` — 真实放置
选对应方块物品 → 原版使用路径 → 回读 `after`。多格家具用锚点：
`tiledata {type:17}` 给出 `3×2 / origin(1,1)`，瞄准 `(2081,303)` → 339 熔炉落在 `2080..2082 × 302..303`。

### `wall {x,y,type,ms,remove}` — 放/拆墙
- `type` 是 **wall id**（木墙 `WallID.Wood = 4`，对应 item 93 木墙），不是 tile id。
- **该格 `HasTile` 时直接拒绝**：报「这一格有方块挡着」，而原版允许墙贴在方块背后 → 家具/方块占着的格子要先 `mine` 拆掉、铺完墙再 `place` 放回。工作台拆下会回收物品，**椅子拆下有时不回收**，要重新 `craft`。
- 回 `ok:true` 只代表起手，真伪只认 1~2 秒后 `tile {x,y}` 的 `wall === type`。
- 真机证据（2026-11-03）：工作台/椅子所在 4 格铺墙上不去的现场处理，拆家具→铺墙→放回，四次全部 `wall=4`。

### `fill {x1,y1,x2,y2,type}` — 矩形铺地
- **`type` 是 tile id，不是 item id**：石块 item 3 → tile **1**；木材 item 9 → tile **30**。
- 必须**有相邻支撑**，整行悬空会在第一格失败 → 自下而上（306→305→304）铺。
- 上限 400 格。
- 铺完可能把玩家埋住（无法移动）→ 铺完要挖掉角色占的格子 `x: px..px+1, y: py-1..py+1`。
- 台阶要铺在**地板那一行**，铺在身体高度会变成墙（这是「stairsUp 从来没成功过」的真因）。

---

## 3.5 液体 / 钓鱼 / 捉虫 / 事件 / 地图（阶段②第一批）

> **状态：编译通过，一次游戏都没进。** 这里只写"代码里有、参数长这样"，
> 真正的验证在阶段③逐条实测。不要把本节的"有"当成"能用"。

| 命令 / 字段 | 参数 | 做什么 | 备注 |
|---|---|---|---|
| `tile` 新字段 | — | `liquidType` / `liquidName`（water/lava/honey/shimmer） | 只有 `liquid>0` 时才有意义 |
| `scan` 新列 | — | 行末多一列 `liqType` | stride 由 4 变 5（带墙时） |
| `state.world` 新字段 | — | `bloodMoon` `eclipse` `pumpkinMoon` `snowMoon` `invasionType` `invasionSize` `invasionProgress` `anglerQuest` `night` | 判"现在是什么局面" |
| `state.buffs[]` 新字段 | — | `timeSec`（剩余秒） | 药剂还剩多久 |
| `npc` 新字段 | — | `critter`（是不是小动物）/ `catchItem`（抓到手变成什么） | 抓虫、钓鱼认饵 |
| `castline` | `x,y` `ms` | 选中钓竿朝水面短按，由原版抛出浮标 | 抛完用 `bobber` 看 |
| `bobber` | — | 我方浮标的 `tile` / `velocityY` / `inLiquid` / `age` | 咬钩靠连续采样看下坠 |
| `reel` | `ms` | 再短按一次收线（咬钩时结算渔获） | |
| `capture` | `who` 或 `x,y` `ms` | 选虫网对准小动物挥一下 | 非小动物会明确拒绝 |
| `startevent` | `event` | bloodmoon/eclipse/pumpkin/frost/goblin/pirate/snowman/martian | **仅测试用**，正常游玩没有这条路径 |
| `map` | `step` | 已探索区域包围盒 + 占比 + 自身地图坐标 | 不返回逐格位图 |
| `splitstack` | `slot` `count` | 把一叠拆一半到空格 | 纯背包操作 |

---

## 4. 物品、合成与收纳

### `craft {item,times}` — 制作
- **`item` 是物品 id，不是配方 id**：34 木椅 / 33 熔炉 / 36 工作台 / 35 铁砧。
- **必须站在工作站 1 格内**（5 格内会报「附近没有工作站」）→ 工作站要建在矿区，不要建在基地。
- 返回 `{item,name,crafted,dropped,times,perCraft,groupsUsed}`。

### `drop {slot,count}` / `grab {r}`
背包满时挖矿会**安静失败**（土块能堆叠所以看不出来，矿石需要新格子就丢在地上）——主背包 50 格满了就先 `drop` 腾格子。

### `chest {action,slot,count}` — 箱子
`list / take / put`，需要站在箱子 6 格内。🚧 目前基地还没放箱子，未实测。

### `equip {slot}` / `accessory {slot,from,off}`
`equip` 只吃护甲槽（穿铁短剑会被明确拒绝：「铁短剑 不是护甲」）。

### `research {slot}` / `duplicate {item,count}`
旅程模式研究 → 无限复制。证据：石块 100/100 → 每次 `duplicate` +900。

---

## 5. 战斗

### `select {item,slot}`
按物品 id 或槽位选中，回读 `held{type,name,stack}`。

### `shoot {x,y,ms}` — 真实射击
选武器 → 瞄准 → 按住使用键。**默认握 600ms**：弓的 `useTime` 是 28 tick（≈467ms），握太短只放一箭、而且下一次调用还在挥动中不会重新起手（实测连射 8 次只掉 1 支箭、怪一滴血不掉）。
证据：握 1400ms 一次调用连放两箭（木箭 95→94→93），僵尸 hp 22→**2**。

### `guard {r,ms}` / `guard {off:true}` — 自动防守（默认开启）
每 tick：找 `r` 格内**视线可达**（`Collision.CanHitLine`）的敌对目标 → 换武器 → 瞄准 → 按住开火。
- 有明确挖/放动作时**让手**，不抢工具；
- 打完把原来的槽位换回来，不把你手上的方块顶掉；
- 隔墙的敌人不假装打得到。
证据：站僵尸走廊 24 秒不动 → `swings 1→12`、木箭 76→**57**、hp 全程 **100**、僵尸被清。

### `minion {slot}` — 召唤
证据：雀杖 0 → **1** 只（`minions 1/1`，槽满时如实回报「随从位已满」）。

### `heal`
用背包里的治疗药水；没药水回 `{healed:0,hasPotion:false}`。

### `attack {r,who,ms,interval}` — 旧式点名攻击
走玩家真实伤害管线（护甲/饰品/前缀/暴击），但**没有弹幕**，所以画面上看不到箭飞出去。
⚠️ 优先用 `shoot` / `guard`；`attack` 保留给「只想确认伤害数值」的场合。

---

## 6. NPC 与交易

| 命令 | 说明 |
|---|---|
| `talk {npc}` | 瞄准附近 NPC 发送一次真实右键；原版 `HoverOverNPCs` 负责选中、台词和商店。**先查距离：≤5 格且有视线**才 `armed:true`，否则直接报错并说明差多少 |
| `chat {text}` | 在世界聊天框说话 |
| `shop {npc}` / `shops {want}` / `shopdebug` | 读商店货架、按名字找商品 |
| `buy {slot}` / `sell {slot}` | 买卖（商人卖弱效治疗药水 300 铜、绳 10 铜） |
| `homeroom {who,x,y}` | 让 NPC 搬进指定房间（走原版 `WorldGen.moveRoom`） |
| `follow {player,range,guard}` / `idle` | 跟随玩家 / 原地待命 |

`homeroom` 前置条件：`housing {x,y}` 要 `roomFound:true`，且 **`roomcheck {x,y}` 的 `ok:true`**（`housediag.gameOk` 也应为 true）。
**不要用 `needs` 里那组家具布尔当门槛**——它们恒 false，见上文 `needs` 的说明。

`talk` 只返回输入已排队，不把 `talkNPC`、商店索引或台词当作成功证据。命令完成后必须轮询
`state.talkNPC`、`state.npcChatText`、`state.npcShopIndex`；其中 `npcChatText` 非空才算原版对话路径真正执行。
实测一通到底的判据（旅商为例）：`use {right:true,x,y}` → `talkfocus.talk={npc:35,chatLen:19}` +
`chatText` 变成真实台词 → 再 `shop {}` 就能读出 `shopId`/`npcType`/货架 → `buy`/`sell` 结算。
**`talk` 与 `use {right:true}` 走同一条链路**，前者多了距离/视线预检，后者要自己给坐标。

---

## 7. 存档与调试

### `save` ❌
联机开服（`netMode=1`）下 `WorldFile.SaveWorld()` 抛 `NullReferenceException @ SaveFileFormatHeader`——客户端没有本地世界元数据，这是 tModLoader 的既有限制，不是我们的 bug。世界由服务端自动保存（退出/自动存档）。

### `api {type,member}` — 反射查 API
查签名别猜：
```
api {type:"Terraria.Player", member:"controlHook"} → "field | Boolean controlHook"
```
这一轮靠它确认了 `releaseHook`、`controlHook`、`pulley` 的存在，少走很多弯路。

### `recipes {q,limit}` / `tiledata {type}` / `help` / `ping`

---

## 8. 已知不可用 / 未验证（诚实清单）

| 项目 | 状态 | 说明 |
|---|---|---|
| 排水 | ❌ | 水里悬空放不上方块（9 格全失败） |
| ~~飞行 `fly`~~ | ✅ | **已修好并真机复验**。根因：原版两种飞行不是同一个键——`Player.cs:25783` 的 `if (mount.CanHover()) {...} else if (mount.CanFly() && controlJump ...)`，悬停坐骑读 `controlUp`、翅膀与普通飞行坐骑读 `controlJump`；旧实现只写跳键，骑悬停坐骑时 `velY` 恒 0。现在按 `CanHover()` 分流。真机：`hasWings:true` 时 `fly {ms:1500}` 从地面升 56 格（`vy` −4.61→−11.91、`wingTime` 180→148） |
| 坐骑 `ride` 子命令 | ⚠️ | `ride` 只查背包槽，读不到 `miscEquips[3]`，所以在坐骑只在装备槽时误报背包里没有坐骑召唤物品。用 `quick` 或先读 `miscEquips[3]` 确认 |
| 箱子收纳 `chest` | 🚧 | 代码有，世界还没箱子，未实测 |
| 液体类型 `liquidop` | 🚧 | §3.5 新加，只编译过 |
| 钓鱼链 `castline` / `bobber` / `reel` | 🚧 | 同上，未实测 |
| 捉虫 `capture` | 🚧 | 同上，未实测 |
| 事件 `startevent` | 🚧 | 同上，未实测（且是测**试专用**路径） |
| 地图 / 诊断 `map` / `walkdiff` / `navrules` / `nodig` / `props` | 🚧 | 只读诊断，未系统性验证字段 |
| 拆分 `splitstack` | 🚧 | 纯背包操作，需空格才能测 |
| `save` | ❌ | 见 §7 |
| `path` 竖井上行 | ❌ | 空腔里跳 60 秒原地不动 |
| `travel down` | ❌ | 沿斜坡漂移，淹死过 |
| 隔墙攻击 | ❌ | 视线不通时 `guard`/`shoot` 都不会命中，属正确行为 |

---

## 9. 知识库：单轮自路由（`terraria_kb`）

同一个回合、同一次模型调用里，模型自己判断要查哪几个库，先吐一行标记，程序按标记取资料后
把正文追加进**同一个会话**续传——不开第二次独立请求。

```
系统提示：terraria_kb { system: true }        → prompts/self-route.md
模型第一行：NEED_KNOWLEDGE:safety_limit,game_rule,build_standard,atomic_action
取资料：  terraria_kb { need: "<本回合输出>" } → { tags, docs, continuation, markerFirst, unknown }
续传：    把 continuation 贴回同一回合继续
```

| 调用 | 作用 |
|---|---|
| `terraria_kb { system: true }` | 返回单轮自路由 system prompt（`prompts/self-route.md`） |
| `terraria_kb { need: "<模型输出>" }` | 抓 `NEED_KNOWLEDGE:` 标记，只加载命中库，并给 `continuation` |
| `terraria_kb { tags: "a, b, c" }` | 已有标签时直接取库；非法标签丢掉并在 `unknown` 里报出来 |
| `terraria_kb { prompt: true }` | 兜底：返回一次性分类器 Prompt（`prompts/intent-router.md`） |
| `terraria_kb { task: "..." }` | 兜底：关键词粗筛（不如分类器准） |

标记必须**独占一行**；写在句子中间不算数，工具会报 `nearMiss` 让上层重写一行再来。

十四个标签与加载顺序：
`safety_limit` → `game_rule` → `world_biome` → `progression_boss` → `gear_route` → `abilities` → `combat_math` → `loot_source` → `npc_town` → `build_standard` → `actor_behavior` → `atomic_action` → `item_operation` → `task_correct`。

| 标签 | 库 | 管什么 |
|---|---|---|
| `safety_limit` | `safety-limit.md` | 禁令、范围、自检（永远加载） |
| `game_rule` | `game-rule.md` | 房屋判定、NPC 入住、刷怪、物块地形、扩散、世界机制 |
| `world_biome` | `world-biome.md` | 世界分层、生物群系判定、液体窒息坠落、天气月相、地貌结构 |
| `progression_boss` | `progression-boss.md` | 进程骨牌、首领召唤与前置、祭坛、困难模式、战前准备 |
| `combat_math` | `combat-math.md` | 伤害结算、防御减法、暴击/无敌帧、穿透、减益、幸运 |
| `gear_route` | `gear-route.md` | 镐力门槛、矿石阶梯、护甲三件与职业分支、四条武器线、机动性、词条、药水 |
| `abilities` | `abilities.md` | 当前能力门槛、连续移动 profile、闭环动作、可达性标定、失效与重标 |
| `loot_source` | `loot-source.md` | 容器与掉落、钓鱼板条箱、首领掉落、商店幸福度、转化回收 |
| `npc_town` | `npc-town.md` | 入住两条件、各 NPC 出现条件、幸福度三因素、传送水晶、商店价格 |
| `build_standard` | `build-standard.md` | 房屋/平台/农场/竖井/隧道的通用建造规范 |
| `actor_behavior` | `actor-behavior.md` | 移动、战斗、拾取、NPC 交互、增益召唤 |
| `atomic_action` | `atomic-action.md` | 单步动作链条、判定、先后顺序、失败分类、重试 |
| `item_operation` | `item-operation.md` | 物品分类与用途、装备弹药药水、背包切换 |
| `task_correct` | `task-correct.md` | 模糊指令、歧义、中断、迭代、做不成怎么回 |

命令/参数/报错去 `docs/kb/_generated-commands.md`（生成物，69 命令 / 26 工具），
不要往这些理论库里塞接口文档。

---

## 10. 硬规则（违反会立刻出问题）

1. **世界只能通过真实操作改变**：`WorldGen.PlaceTile/KillTile`、`Main.tile[x,y].type = ...` 全部禁止；只能走「选物品 → 原版使用/挥击」路径。
2. **只用桥在游戏内部驱动角色**，不用 OS 键鼠去点用户的桌面（`tr_mouse/tr_key` 那条路已废弃）。
3. **出生点周围 100 格内不下挖**（曾把出生点下方挖穿水层，每次回城都掉进水里淹死）。
4. **竖井逐格挖**，不要用 `travel down`；下行后必须留绳子或楼梯。
5. **挖之前先探水**：脚下 3 行里 `liquid >= 240`（满格）或连续 ≥2 格就是水腔，停手。
6. **开打前读 hp**：低于 50~55% 先回血，不要边打边掉。
7. **每一步都要回读**：命令回 `ok:true` 不等于事情发生了（背包满、方块无支撑、动画被打断都会「成功但没做」）。
8. **改仓库代码后必须同步进 ModSources 再打包**，否则装进游戏的是旧代码（`cycle.ps1` 已做同步，并在编译失败时直接退出）。

## 10. 全局异常处理（`terraria_exception`）

五维元自检管「动手之前缺不缺东西」，这一层管「动手过程中出了意外怎么办、处理完怎么回到主线」。
规则正文 `prompts/exception-policy.md`，实现 `src/exception-policy.js`，规则与代码由测试互相校验。

```
读状态判异常：terraria_exception { observe: true }
出应急计划：  terraria_exception { goal: "…", step: "…", snap: "<JSON>" }
真的执行：    terraria_exception { goal: "…", step: "…", apply: true }
取规则全文：  terraria_exception { prompt: true }
看证据表：    terraria_exception { log: true }
换任务清预算：terraria_exception { goal: "…", reset: true }
```

| 异常 | 判定 | 处理顺序 |
|---|---|---|
| 地形阻挡 | 前方格子实心 / 半砖 / 斜坡 | 先跳 → A\* 绕行 → 判卡死 |
| 卡死 | 连续 3 次坐标位移 < 0.6 格 | 重规划，3 次用尽判失败 |
| 敌对生物 | 警戒半径内的敌对目标 | 后撤；<45% 边逃边喝药；<25% 只逃；甩不开且 ≤6 格才反击 |
| 血量偏低 | <45% 提醒 / <25% 危险线 | 后撤 + 喝药，危险线以下不打 |
| 致命环境 | 岩浆 / 微光 / 蜂蜜 / 深水 / 大落差 | 松键停步 → 退回安全位 → 探明再走 |
| 道具缺失 | 必需物品不在背包 | 暂停执行，报缺什么、缺几个 |
| 背包满 | 无空格 | 不中断主线，但捡东西前先腾格子 |

重试预算（用尽即判任务失败并上报，不许静默重试）：同处跳跃 2 次 / 重新寻路 3 次 /
卡死重规划 3 次 / 同一威胁规避 2 次。

三条铁律：主目标唯一且不被应急替换；应急是临时子状态，同一时刻最多一个，处理完回原步骤；
每次触发、重试、失败都写六列日志（序号 / 异常 / 证据 / 动作 / 结果 / 第几次）。

`apply:true` 时只下发 `move` / `jump` / `heal` / `attack` 这类**控制角色**的命令，不碰世界。

### 行进状态可见性

`state` 的 `guard` 块里带 `latched`：

```json
{"left":false,"right":false,"up":false,"down":false,"jump":false,"use":false,
 "climbUp":false,"climbDown":false,"travel":false}
```

还有 `guardDefault`、`resetReason`、`resetCount`。换世界 / 回菜单 / 死亡时 `ResetMotion()`
会把全部 latch、path、walk、follow、travel、guard、hook 一次清空，`resetReason` 会记下原因
（例如 `world#1224859936`）。这是排查「角色自己一直往右走 / 一直自动打怪」的第一现场。
