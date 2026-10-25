# 硬规则（违反任何一条，这次改动就是作废的）

## 0. 每次动手前先读这一条（用户要求：每次工作都要读一遍）

> **用指令在后台控制角色，而不是直接控制我的电脑，也不是用指令改变世界。**
> ——用户原话，2026-10-25。三件事必须同时成立：

| 要求 | 正确 | 错误（犯过） |
|---|---|---|
| 怎么下指令 | 走 mod 的桥（TCP 47777）在游戏内部执行 | 用 `tr_mouse` / `tr_key` 点菜单、切快捷栏、走路 |
| 谁在动 | 角色（键盘级别的操作被游戏自己解释成挥镐/走路/放方块） | 用户的鼠标光标被搬来搬去、游戏窗口被抢到前台 |
| 世界怎么变 | 原版 `ItemCheck` 结算（挥动动画、耗耐久、掉物品） | `WorldGen.*` / `tile.type=` / `Player.PickTile` 直接改世界 |

**禁止的操作系统级输入**（都属于"直接控制我的电脑"）：
`SetCursorPos`、`mouse_event`、`keybd_event`、`SendInput`、`SetForegroundWindow`、
`AttachThreadInput`、`ShowWindow` 抢焦点、以及任何点击用户桌面/其他窗口的自动化。
这些代码已从仓库删除（提交 `c4106eb`）；不要再加回来，也不要改用插件工具绕过去。

**唯一的输入通路**（已实现，包在 `BridgeSystem.BeginItemUse(slot, tileX, tileY, ms)` 里）：

```
PreItemCheck 钩子（游戏主线程，不需要前台、不动用户光标）
    → pl.selectedItem = 槽位
    → Main.mouseX/Y + Player.tileTargetX/Y = 目标格
    → p.controlUseItem = true / p.controlUseTile = true（放方块）
    → 原版 ItemCheck 自己挥动、判定硬度、扣耐久、掉物品
```

走路/跳跃同理：写 `controlLeft/controlRight/controlJump`，由游戏物理推动角色。
**截图只看不控**（printwindow 抓窗口像素），不属于操作系统输入。

> **总原则（用户原话，必须严格遵守）：控制角色完成任务，而不是通过指令改变世界。**
> 违反这条，整个项目就没有意义。任何"让世界变成我想要的样子"的写法都是错的；
> 唯一正确的写法是"让角色做出一个玩家也会做的动作，然后由游戏自己改变世界"。
>
> 由此推出的两条推论：
> 1. 判断标准是**屏幕上的角色像不像真人在做这件事**，不是方块最终变没变。
>    挖矿必须有完整的挥镐动作，放置必须先掏方块；动画被打断、角色不抬手，就是错的。
> 2. 出生点周围 100 格不挖（用户已两次被从出生点掉进水里/坑里）。


这些规则不是建议。用户已经因为同一条被违反而纠正过多次，每一条后面都写着它是怎么被
违反的、以及正确做法是什么。

## 1. 世界只能通过真实操作改变（最高优先级）

**禁止**任何"直接把方块写进/删出世界"的调用。具体禁用的 API：

| 禁用 | 曾经怎么犯的 |
|---|---|
| `WorldGen.PlaceTile` / `WorldGen.PlaceObject` | 放熔炉时图省事直接调 `PlaceObject`，被用户当场点名"走命令是假操作"；`fill` 和 `travel` 的铺地板也是这个（一边 `PlaceTile` 一边手动 `stack--`，等于凭空造方块） |
| `WorldGen.KillTile` / `WorldGen.KillWall` | 挖矿用 `KillTile` 绕过硬度和镐力判定；`mine` / `wall` 还留了 `admin:true` 分支直接删格子，`travel`/`walkto` 的砍树也用 `KillTile` |
| `Main.tile[x,y].type = ...` | 直接改瓦片类型 |
| `Player.PickTile(...)` 当成"挖掘" | 它是即时结算伤害，不是挥镐：画面上角色不动、没有挥动动作。用户看到的就是"你挖东西连镐子都不掏" |
| `WorldGen.RangeFrame` 单独用来"补家具 frame" | 家具本体是靠 `PlaceTile` 塞进去的 |

**唯一允许的做法**：让游戏自己的物品使用逻辑去改世界。

```
选中对应物品（pl.selectedItem = 槽位）
    → SetAim(tileX, tileY)          // 同时写 Main.mouseX/Y 和 Player.tileTargetX/Y
    → UseUntil = Now + ms           // PreItemCheck 里合成 controlUseItem
    → 等原版 ItemCheck 跑完
    → 回读 tile 确认，不确认就重试或报失败
```

这条路径在 `BridgeSystem.ApplyAim()` / `ApplySyntheticInput()` / `PreItemCheck()` 里已经
实现好了，包一层就是 `BridgeSystem.BeginItemUse(slot, tileX, tileY, ms)`（`mine` / `place` /
`fill` / `wall` / `travel` / `walkto` / `removeblock` 现在全部走它）。不要写第二条路。

这一版 tModLoader 没有可写的智能光标开关（`Main` 只有只读的 `SmartCursorWanted` /
`SmartCursorIsUsed`，写 `SmartCursorEnabled` 会编译失败 CS0117）。准星落点靠
`Main.mouseX/Y` + `Player.tileTargetX/Y` 直接压住，实测回读的格子就是传入的那一格。

物品必须真的存在、真的被消耗；没有工具就应该失败，而不是绕过工具。

## 2. 不许把"命令生效"当成"操作发生"

每条结论都要有回读证据：`tile` / `inventory` / `state` 的真实返回值。
说"挖掉了"就要有该格 `hasTile:false`；说"放下了"就要有该格 `type==预期`；
说"捡到了"就要有背包里数量增加。没有回读就不要写"完成"。

## 3. 不许虚报

不确定就说不知道。工具返回看不懂就停下来读，不要用"大概是……"继续往下做。

## 4. 文档保持通用

`GOAL.md` / `docs/**` / `README*` 里不出现具体世界名、角色名、存档路径。

## 5. 机器相关的脚本不进仓库

`C:\Windows\Temp\trtest\` 下的东西（坐标、窗口句柄、显示器布局）不进 git。

## 6. 开发流程

- 每次提交前跑 `npm test`。
- 改了 mod 代码要重新打包才知道是否真的能用；"编译通过"不等于"能用"。
- 一条规则被违反过，就把违反方式写进本文件对应的行里，不要只写"要真实操作"这种空话。

## 7. 知识库路由（每次收到指令先走这里）

知识库在 `docs/kb/`，入口 `docs/kb/README.md`。流程固定：

**单轮自路由（默认路径）**：同一个回合里我自己判断要查哪几个库，把标记写在全回合第一行，
程序捕获这行 → 检索 → 追加进**同一个会话**续传。不额外开一次独立请求。

1. **永远先读** `docs/kb/safety-limit.md`（禁令 / 范围 / 边界 / 确认），不参与判定。
2. 收到玩家指令，**第一行先写标记**（独占一行，只写一次）：
   `NEED_KNOWLEDGE:标签1,标签2`，标签取自下文七个，可多选。
   system prompt 原文：`prompts/self-route.md`（`node scripts/route.mjs --system` 可取）。
   细则：动手改世界/动身体必带 `atomic_action` 或 `actor_behavior`；危险或大范围动土必带 `safety_limit`；
   玩家在纠正我必带 `task_correct`。
3. 把这行（连同自己写的草稿）交给工具取资料：
   `terraria_kb { need: "<本回合输出>" }` → 回 `tags` / `docs` / `continuation`。
   `continuation` 直接贴回同一回合继续，不要新开请求。
   抓不到标记时**不许静默降级**：先看 `terraria_kb` 报的 `nearMiss`
   （写了但没独占一行 → 重写一行再来）。
   **兜底顺序**：① 前置分类器 `prompts/intent-router.md` 补判一次
   （`terraria_kb { prompt: true }`）→ ② 关键词 `node scripts/route.mjs "<原话>"`
   → ③ 都没判出来就只加载 `safety_limit` 并在回复里说明"这次没判出库标签"。
3b. 标签串过 `normalizeTags()`（去重、丢非法、按加载顺序排）——写错要当场发现：
   `node scripts/route.mjs --need "<模型输出>"` 会把非法标签打出来并 exit 1。
4. **只读命中的那几个库**，不把无关资料塞进上下文；多标签按
   `safety_limit → game_rule → build_standard → actor_behavior → atomic_action → item_operation → task_correct` 叠加。
5. 玩家带纠正语气（"不是 / 别 / 又 / 错了"）时 `task_correct` 必须带，且**优先读**，
   按它 §4 的流程把纠正**落成规则或代码**，不能当场改一次就算完。

### 入库校验（往知识库写任何东西之前必答）

> **这条描述换一张世界种子还成立吗？**

**允许进库的"不是本局特有"的东西**（容易被我误删，列在这里）：

- 物品 / 物块 / 墙 / 弹幕 / buff 的**数字 ID 与枚举名**：那是游戏程序里的常量，
  换任何世界都指向同一个对象（`ItemID` / `TileID` 空间）
- 引擎常量：硬度分级、光照值、堆叠上限、作用距离与射程、燃料与弹药对应关系、
  方块可否悬空、家具占地与锚点、合成配方——凡是"代码里写死"的都属于全游戏通用规则
- 由这些常量推出的判定（"某类物品只能做某类事"、"这个形状需要几格空间"）

**不许进库的**（这才是本局数据）：
本世界坐标与地形、某处矿脉/水层/宝箱位置、"这次放下去了上次没放下"这类偶然现象、
某一次任务用的固定长宽、**我们自己工具的参数**（默认时长、重试次数、批量上限——
那是工具接口的性质，不是游戏规则）。

这些东西放：`docs/terraria-mechanics.md`（本世界地形/矿脉/坐标与时间曲线）、
`docs/debug/unknowns.md`（本世界还没搞清的事）、任务记录与提交信息。

机器校验：`npm run kb:lint`（`npm test` 里也挂了一条），
一旦在七个库里出现坐标、`x=`/`y=`、世界名、"本档/本次/实测"、毫秒或 tick 数值就报错。
这条界由用户 m05304 指定：**知识库只放所有世界都生效的通用机制**。

命令/工具参数表是**生成物**：`docs/kb/_generated-commands.md`
（由 `node scripts/kb-sync.mjs` 从 `Commands.cs` / `index.js` 生成，不要手改）。
改了命令就重新生成；`npm test` 里有一条测试会在生成物过期时直接变红。

想让 agent 自己取上下文时：

```js
import { buildContext, normalizeTags } from './src/kb-router.js'

// 1) 分类器（模型）给出 "build_standard, atomic_action, safety_limit"
const { tags } = normalizeTags('build_standard, atomic_action, safety_limit')
// 2) 只把命中的库读进来
const { docs } = await buildContext('把家完善一下', { tags })
// tags: ['safety_limit', 'build_standard', 'atomic_action']  docs: 对应文件全文

// 不传 tags 时才会用关键词兜底
const fallback = await buildContext('把家完善一下')
```