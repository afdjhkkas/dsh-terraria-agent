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
| 感知 | 单格方块 | `tile {x,y}` | ✅ |
| 感知 | 按类型找方块 | `find {type,x1,x2,y1,y2,max}` | ✅ |
| 感知 | 附近生物 | `npc {r}` | ✅ |
| 感知 | 背包 | `inventory` | ✅ |
| 感知 | 地上掉落物 | `items {r}` | ✅ |
| 感知 | 玩家列表 | `players` | ✅ |
| 感知 | Boss / 事件状态 | `boss` / `event` | ✅ |
| 感知 | 房屋判定 | `housing {x,y}` / `needs {x,y}` | ✅ |
| 移动 | 逐帧方向控制 | `move {dir,ms}` / `jump {ms}` | ✅ |
| 移动 | 爬绳 | `climb {dir,ms}` | ⚠️ 仅绳子 |
| 移动 | 走到坐标 | `walkto {x,y,tol,ms}` | ⚠️ 平坦可用 |
| 移动 | A* 寻路 | `path {x,y,ms,dig}` | ⚠️ 空腔竖井会死锁 |
| 移动 | 定向掘进 | `travel {dir,distance,dig}` | ⚠️ 仅水平方向 |
| 移动 | 回城 | `mirror` | ✅ |
| 移动 | 抓钩 | `hook {x,y,ms}` / `unhook` | ✅ |
| 移动 | 飞行 / 坐骑 | `fly {ms}` / `mount {action}` | 🚧 无翅膀/坐骑时诚实拒绝 |
| 挖掘 | 真实挥镐挖一格 | `mine {x,y,ms}` | ✅ |
| 挖掘 | 挥锤拆方块 | `removeblock {x,y,ms}` | ✅ |
| 建造 | 放一格方块 | `place {x,y,type,ms}` | ✅ |
| 建造 | 放墙 | `wall {x,y,type,ms,remove}` | ✅ |
| 建造 | 矩形铺地 | `fill {x1,y1,x2,y2,type}` | ⚠️ 需支撑、自下而上、≤400 格 |
| 物品 | 制作 | `craft {item,times}` | ⚠️必须贴近工作站 |
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
| NPC | 对话 | `talk {npc}` / `chat {text}` | ✅ |
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

### `state` — 角色 + 世界
返回 `player{ tile,pos,velocity,life,lifeMax,mana,dead,direction,grounded,breath,wet,held,buffs,zones,
minions,minionSlots,grapCount,hookType,mounted,wingTime,hasWings,itemAnimation }`、
`world{ name,width,height,dayTime,time,raining,bloodMoon,eclipse,hardmode,tick }`、
以及 `guard / attack / path / walk / travel` 各作业状态。

- **`zones`** 是关键：`corrupt/crimson/jungle/snow/desert/beach/dungeon/glowshroom/meteor/hallow/underworld/rocklayer/dirtlayer/sky/surface`，找群系不用盲挖。
- `grounded` 要求「本帧与上一帧速度都为 0」，斜坡上站着不会误报。
- 证据：`state.player.grapCount` 0→1→0、`minions` 0→1、`itemAnimation` 16/8/0 全部读到过。

### `scan {x,y,r,walls}` — 网格快照
返回 `(2r+1)²` 网格，每格 `[type,wall,liquid,shape]`，`r ≤ 48`。**只覆盖已加载区段**（约玩家周围 ±100 格）。

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

### 其余
`players` 多人列表、`boss {active,bosses}`、`event`（血月/日食/雨）、`housing {x,y}`（房间判定 + 无家可归人数）、`needs {x,y}`（房间差什么：门/椅/桌/火把/墙）、`help` 命令清单。

---

## 2. 移动

| 命令 | 说明 | 证据 / 限制 |
|---|---|---|
| `move {dir,ms}` | 逐帧写速度（真人按住方向键的效果） | 1.4 秒顶墙位置不变，证明碰撞生效 |
| `jump {ms}` | 置跳跃标志 | 起跳/上台阶 |
| `climb {dir,ms}` | **只在绳子上**才生效 | 301→291、455→307；绳道分段 28 格 |
| `walkto {x,y,tol,ms}` | 直线走到坐标 | 平地可用；有坑会失败 |
| `path {x,y,ms,dig}` | A* 寻路 + 可选挖掘 | ⚠️ 空腔竖井会「跳一分钟原地不动」，判据需再修 |
| `travel {dir,distance,dig}` | 定向掘进循环 | ⚠️ **只用水平方向**：`down` 会沿斜坡漂移十几格并淹死过人 |
| `mirror` | 魔镜回城 | 任意位置 → 出生点 |
| `hook {x,y,ms}` | 抓钩（`controlHook` 驱动） | 挂住 4 格外木地板：`grapCount 0→1`、角色被拉到 2100→2097.6 |
| `unhook` | 松钩（`releaseHook` + 收弹幕） | `grapCount 1→0`，角色留在原地 |
| `fly {ms}` | 飞行/跳跃 | 无翅膀时回 `hasWings:false` + 明确提示，不假装飞起来 |
| `mount {action}` | 坐骑 | `info` 返回可用坐骑物品；无坐骑时 `{active:false,items:[]}` |

**竖井闭环**（唯一可靠的下行方式）：逐格 `mine`，每格后等 1.5 秒让角色落到下一格；碰撞盒 20px 宽 ≈ 1.25 格，要挖的两列随中心点小数位变化：
`[floor(cx-0.55), floor(cx+0.55)]`。井底铺绳再用 `climb` 爬出。

---

## 3. 挖掘与建造

### `mine {x,y,ms}` — 真实挥镐
选镐 → `BeginItemUse` → 原版 `ItemCheck` 挥击 → 物品栏真出方块。**默认 1300ms**（铁镐挖石头/丛林草要 1.2~1.3 秒；900ms 会「返回成功但方块不动」）。
回读：`before/after` 方块类型 + 背包数量变化。

### `removeblock {x,y,ms}` — 真实挥锤
同上路径，锤子拆墙/家具。

### `place {x,y,type,ms}` — 真实放置
选对应方块物品 → 原版使用路径 → 回读 `after`。多格家具用锚点：
`tiledata {type:17}` 给出 `3×2 / origin(1,1)`，瞄准 `(2081,303)` → 339 熔炉落在 `2080..2082 × 302..303`。

### `wall {x,y,type,ms,remove}` — 放/拆墙

### `fill {x1,y1,x2,y2,type}` — 矩形铺地
- **`type` 是 tile id，不是 item id**：石块 item 3 → tile **1**；木材 item 9 → tile **30**。
- 必须**有相邻支撑**，整行悬空会在第一格失败 → 自下而上（306→305→304）铺。
- 上限 400 格。
- 铺完可能把玩家埋住（无法移动）→ 铺完要挖掉角色占的格子 `x: px..px+1, y: py-1..py+1`。
- 台阶要铺在**地板那一行**，铺在身体高度会变成墙（这是「stairsUp 从来没成功过」的真因）。

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
| `talk {npc}` | 与 NPC 对话（**≤12 格**） |
| `chat {text}` | 在世界聊天框说话 |
| `shop {npc}` / `shops {want}` / `shopdebug` | 读商店货架、按名字找商品 |
| `buy {slot}` / `sell {slot}` | 买卖（商人卖弱效治疗药水 300 铜、绳 10 铜） |
| `homeroom {who,x,y}` | 让 NPC 搬进指定房间（走原版 `WorldGen.moveRoom`） |
| `follow {player,range,guard}` / `idle` | 跟随玩家 / 原地待命 |

`homeroom` 前置条件：`housing {x,y}` 要 `roomFound:true`，且 `needs` 里门/椅/桌/火把/墙都齐。

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
| 排水 | ❌ | 水里悬空放不上方块（实测 9 格全失败） |
| 飞行 | 🚧 | 无翅膀/坐骑，`fly` 只做诚实拒绝 |
| 坐骑 | 🚧 | 背包无坐骑物品 |
| 箱子收纳 | 🚧 | 代码有，世界还没箱子，未实测 |
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

七个标签与加载顺序：
`safety_limit` → `game_rule` → `build_standard` → `actor_behavior` → `atomic_action` → `item_operation` → `task_correct`。

命令/参数/报错去 `docs/kb/_generated-commands.md`（生成物，69 命令 / 26 工具），
不要往七个理论库里塞接口文档。

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
