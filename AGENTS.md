# 硬规则（违反任何一条，这次改动就是作废的）

## 0. 每次动手前先读这一条（用户要求：每次工作都要读一遍）

> **用指令在后台控制角色，而不是直接控制我的电脑，也不是用指令改变世界。**
> ——用户原话，2026-10-25。三件事必须同时成立：

| 要求 | 正确 | 错误（犯过） |
|---|---|---|
| 怎么下指令 | 走 mod 的桥（TCP 47777）在游戏内部执行 | 用 `tr_mouse` / `tr_key` 点菜单、切快捷栏、走路 |
| 谁在动 | 角色（键盘级别的操作被游戏自己解释成挥镐/走路/放方块） | 用户的鼠标光标被搬来搬去、游戏窗口被抢到前台 |
| 世界怎么变 | 原版 `ItemCheck` 结算（挥动动画、耗耐久、掉物品） | `WorldGen.*` / `tile.type=` / `Player.PickTile` 直接改世界 |

**重启游戏必须先得到用户允许，而且每一次都要单独问**（2026-10-27 违反过两次：
第一次是改了一行 `housediag` 判定就直接跑部署脚本，用户质问"我允许你重启了吗"；
第二次更严重——我问过一次、用户选了"重启"，我就把这当成长期授权，之后又接连重启两次
没再问，用户当场骂"你tm的我让你重启了吗"。）。部署脚本 `deploy2.ps1` 的默认流程是
杀进程 → 编译 → 启动 → 自动进世界，**它等于重启游戏**。规矩：

| 场景 | 正确做法 |
|---|---|
| 只改了 mod 代码，想让它生效 | 先问「要不要编译并重启」。允许了再跑部署脚本 |
| 用户正在玩 / 正在录示范 / 正在等我看画面 | **一律不许重启**，先问；只编译不启动，等他说可以再重启 |
| 判断不了会不会打断他 | 按会打断处理，先问 |
| 上一次重启是他批准的 | **不算数**。下一次要重启，还要重新问一遍 |
| 你已经问过一次、他没回答 | 等。只同步源码 + 编译，不启动；把"要重启才生效"写清楚交给他决定 |

**"问过一次"≠"以后都能重启"**：一次授权只管一次动作。抓不到确认就当没授权，
宁可停在"代码改完、测试跑完、等你说重启"，也不许自己去杀进程。

编译不等于重启：源文件同步 + 编译 + 装 dll 可以不启动游戏；只有"启动并重新进世界"才算重启。

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

## 0.5 工作循环（用户 m18179 定的执行规矩，每个任务都按这个跑）

> **用户发布任务 → 我去执行 → 遇到困难 → 我自己判断 → 用知识库的基础规则 + 已学会的基础
> 能力（放置、挖掘、行走、跳跃、开门、使用物品）解决当前阻碍 → 推进进度 → 完成目标。**
> ——用户原话，2026-10-27。他要我掌握的是这个闭环本身，不是某一关的通关记录。

| 环节 | 要求 | 犯过的错 |
|---|---|---|
| 接任务 | 直接开工，不再确认"要不要做" | 反复问"要接着做吗" |
| 执行 | 拆成可验证的小步，每步 tile/背包回读 | 一把脚本刷完不看中间结果 |
| 遇困难 | **第一反应是判断和尝试，不是汇报** | 一卡住就停下来问"要不要接着做"（m18178 已认账） |
| 判断 | 依据只能来自：知识库规则 / 真机回读 / 已掌握的能力。禁凭猜测改代码 | 猜一个原因就改一行就部署 |
| 破局 | 只用**已掌握的**基础能力组合出新解法 | 为绕障碍临时造捷径（give / teleport / 直接改世界） |
| 推进 | 解决了就继续往下做 | 做完一步就写长总结，进度没动 |
| 完成 | 对照任务验收标准逐条回读 | 说"应该可以了" |

**只有这三种情况才允许停下来问**：

1. 现有手段确实试尽（同一问题至少试过三种不同思路，并记录各自结果）；
2. 需要用户决策（目标变更 / 资源取舍 / 价值判断）；
3. 继续做就必须触碰硬规则（例如必须先改世界才能前进）。

其余一切"卡住了"，都属于**我该自己解决**的范围。判断和尝试的顺序写进 `docs/kb/actor-behavior.md`
（先看能不能绕 → 再试跳 → 再试挖天然地形 → 报具体阻碍）。

## 0.55 只能有真操作：一律只写输入，禁止替引擎算物理（用户 m22230 定的硬规矩）

> **"写硬性规则只能有例如刚才开箱子那样的真操作，然后完完全全检测并剔除所有假操作。"**
> ——用户原话，2026-10-27。这句话之前他连着说了三遍"你这是假操作"，
> 一次是开箱子（界面没弹），一次是爬绳（人浮起来了），一次是 `move up`（电梯式攀升）。

### 什么叫"真操作"

**真操作 = 只把"手指动作"交给引擎，结果由原版物理/交互算出来。**
允许写的只有输入标志：`controlLeft / controlRight / controlUp / controlDown /
controlJump / controlUseItem / controlUseTile / controlHook / controlMount`
以及鼠标的 `Main.mouseLeft / mouseRight` 及其**释放边沿**（`mouseLeftRelease` / `mouseRightRelease`）。

**假操作 = 直接改引擎算出来的结果**，以下全部禁止（除读取）：

| 禁止写 | 为什么 |
|---|---|
| `player.velocity.X/Y = ...` | 速度是物理算出来的；手写就等于自己造了个比游戏快的电梯（`move up` 曾一路升 21 格） |
| `player.position / player.Center = ...` | 直接摆放角色，等于瞬移 |
| `player.pulley = false/true` | 挂绳状态由引擎判定；主动清掉会让人永远拔不住绳子 |
| `player.wet / lavaWet / immunity / statLife / statMana = ...` | 状态与数值由游戏给 |
| `npc.life = ...` / `npc.velocity = ...` | 同理，伤害要走真实命中管线 |
| `Main.tile[x,y] = ...` 直接改写 | 改世界必须走"选槽→瞄准→使用→等 ItemCheck→回读" |

**唯一例外**：物品对象自身的初始化（如新建 `Item` 时设 `position = Vector2.Zero`）、
以及"读"这些字段做判断——读不算改。

### 每次改完必跑的自检（会写进 CI 前的人工步骤）

```bash
# 1) 角色速度/位置一个都不许直接写
grep -n "velocity\.\(X\|Y\) *=[^=]\|player\.position *=\|player\.Center *=\|pulley *=" DshTerrariaBridge/*.cs
# 期望：只出现注释行（以 // 开头），出现任何赋值行就是回归

# 2) 世界改写的唯一入口
grep -n "Main\.tile\[[^]]*\]\.\(type\|wall\|active\) *=" DshTerrariaBridge/*.cs
# 期望：0 行（放方块/挖方块只能走 place/mine 的输入链路）
```

### 怎么验证"这次是真操作"

光看"人动没动"不够——上面三次假操作人都在动。要**同时**看这三样：

1. **输入标志真的置上了**（`humaninput.control.*` 为 true）；
2. **状态读数里没有我写的结果**（比如 `velocity` 的数值不是我代码里那个常数）；
3. **行为能被环境改变**：绳子不够长就该掉下来、墙挡住就该停在墙前——
   如果"任何地形都照样匀速前进"，那一定是假操作。

## 0.6 解决完必须回灌（用户 m18346 定的规矩）

> **"你要在不断的解决问题中完善自己的知识库和命中。比如我告诉你可以把部分屋顶换成平台，
> 这样你就可以不破坏房子出去——这个知识你应该写进知识库，并让自己下次遇到同样的问题时
> 能命中这条知识。"**
> ——用户原话，2026-10-27。解决问题不是终点，**把解法变成下次能自动命中的知识**才是。

每解决一个非平凡问题，收工前必须走完这三步（少一步都不算完）：

| # | 动作 | 判据 |
|---|---|---|
| 1 | **判断是不是通用知识** | 换一张世界种子还成立吗？成立才进 `docs/kb/`；是本局数据就进 `docs/terraria-mechanics.md` / `docs/debug/unknowns.md` |
| 2 | **写进对应的库** | 先看库里有没有**相反或过时的**说法，有就改掉（错规则比没规则更坏）；再补新条目，带上"为什么"和"代价" |
| 3 | **补命中，并当场验证** | 关键词表 `src/kb-router.js` 的 `KEYWORDS` 补上这次任务的原话用词 → 跑 `node scripts/route.mjs "<当时那句原话>"` 确认真的命中了那个库 → 加一条 `test/intent-router.test.js` 用例锁住 |

**第 3 步是最容易漏的**：知识写进库了，但路由命中不到，等于没写。
已经出过两次：说"盖一间 NPC 房子"漏了 `game_rule`/`npc_town`（关键词写的是带空格的 `npc 房`）；
说"被困在房子里出不去"漏了自建房出口那条（`build_standard` 里没有"出去/拆墙/出口"这些词）。

**还要检查"库里有没有错规则"**：这次就抓到一条——
`build-standard.md` 原来写"不要用平台做墙或屋顶，判定必然失败"，实测是错的
（平台能当屋顶，只是占掉几格空气）。用户一句话纠了，库里就必须改，否则下次照着错规则做。

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
   `NEED_KNOWLEDGE:标签1,标签2`，标签取自下文十三个，可多选。
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
   `safety_limit → game_rule → world_biome → progression_boss → gear_route → combat_math → loot_source → npc_town → build_standard → actor_behavior → atomic_action → item_operation → task_correct` 叠加。
5. 玩家带纠正语气（"不是 / 别 / 又 / 错了"）时 `task_correct` 必须带，且**优先读**，
   按它 §4 的流程把纠正**落成规则或代码**，不能当场改一次就算完。

### 7.4 检索不是一次性的：卡住必须重查（用户 m19902 定的规矩）

上面第 1~5 条只管"任务开始时查一次"。**这是病根**：第一轮没命中，后面整段执行就带着缺失的
知识往下跑，失败了也不会回头想"是不是我漏查了"。所以补一层**卡点重试检索**：

1. **只在这四种信号出现时才重查**（不做无脑反复检索）：
   `no-path` / `action-failed` / `safety-violation` / `target-unreachable`。
2. **每次受阻都重走一遍全套分类检查**，逐类确认是不是漏了该分类下的规则；
   对应分类由信号决定（实现见 `src/kb-retry.js` 的 `SIGNAL_CATEGORIES`）：
   - `no-path` → 必查 `navigation` / `world_biome` / `build_standard`
   - `action-failed` → 必查 `item_operation` / `atomic_action`
   - `safety-violation` → 必查 `safety_limit` / `combat_math`
   - `target-unreachable` → 必查 `navigation` / `item_operation`
3. **结果必须标记"有没有新增"**：
   有新条目 → **回决策链路重新评估**（不是继续原计划）；
   全套复核后仍无新增 → 这时才允许判定"任务按现有知识做不到"，并说明缺哪类知识。
4. 同一信号 + 同一现象**不重复烧**（返回 `skip`，不是 `give-up`——重复只说明不必重查，
   不等于知识不足）；单次任务重试最多 4 轮（`maxRounds`）。
5. 每轮结束在日志里写一行：第几轮、新增了哪些库、因此改了什么判断
   （`formatRetryVerdict()` 就是给这个用的）。

### 7.45 检索上下文不许割裂：原始目标永久保留（用户 m20002 定的规矩）

7.4 还有第二个病根，比漏查更隐蔽：**任务是 A，一开始检索漏了 A 的某个库；执行途中冒出
衍生问题 B，于是拿 B 去检索、只拉 B 的东西，原始目标 A 再也没有人回去补**。结果 A 缺的
知识从头到尾补不上，最后还把"没知识"误判成"做不到"。

四条硬约束（实现见 `src/kb-retry.js` 的 `begin` / `onBlockage`）：

1. **整个任务生命周期内永久保留原始目标标签**，衍生问题不许覆盖它。`taskTags` 在 `begin`
   时定下，之后任何 `onBlockage` 都不改它。
2. **任意一次重检索的输入 = 【原始目标 + 当前衍生问题】**，两部分都参与分类遍历。
   代码里 `candidates = taskTags ∪ route(B) ∪ SIGNAL_CATEGORIES[B] ∪ 待查分类`。
3. **知识池只累加不替换**：每轮把新查到的并进 `pool`，不回退、不重置。
4. **只有【原始目标相关分类全部遍历确认无匹配】且【当前衍生问题也无匹配】才判失败**。
   所以 `begin` 会把原始目标没命中的分类记进 `pending`，后续每轮补查一批（`SWEEP_PER_ROUND`）；
   `pending` 没清空前，答案只能是 `retry`（"还有分类没查过，不能判做不到"），不能是 `give-up`。

分类体系不变（仍是十三个库），只是**检索的输入主体不能被中途冒出的新问题顶替**。

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
一旦在库里出现坐标、`x=`/`y=`、世界名、"本档/本次/实测"、毫秒或 tick 数值就报错。
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

## 7.5 桥协议是两级结构（发错格式会静默走默认值）

```
{"cmd":"move","params":{"dir":"left","ms":600}}   → {"ok":true,"data":{...}}
```

`Commands.Handle` 只从 `root.params` 取参数；**平铺写 `{"cmd":"move","dir":"left"}` 不会报错**，
`p` 变成 `default`，每个字段都取默认值——于是 `move left` 变成往右走、`guard {"off":true}`
变成"再开启一次"、`walkto` 的坐标全丢。

排查这类"命令返回 ok 但行为完全不对"时，**第一件事是核对报文是不是两级结构**，
再去怀疑地形、焦点、控制权。这个坑排了整整一轮，最后才发现是自己的客户端写错了。

## 8. 元自检（拿到知识之后、动手之前，固定五问）

规则正文：`prompts/meta-check.md`（`terraria_kb { meta: true }` 可取）。
这不是知识库里的一个库，是**顶层元规则**；不管什么新任务都跑同一套。

**触发点**：`NEED_KNOWLEDGE` 检索完之后、生成任何移动/交互动作之前。同一个回合，不新开请求。

**五项固定核验**（顺序不许改，每项都要能说出依据）：

| # | 维度 | 不通过记作 |
|---|---|---|
| 1 | 规则核验：库里有没有**完整**机制（触发条件 / 判定 / 后果） | 【知识缺口】 |
| 2 | 道具核验：必需物品对不对得上背包；缺了要说出取得链 | 【道具缺口】 |
| 3 | 地形/路径核验：群系、危险、能否抵达、未知障碍 | 【路径缺口】 |
| 4 | 动作原语核验：整条动作序列能不能用**清单里真实存在**的命令拼出来 | 【动作原语缺口】 |
| 5 | 状态读取核验：判定时机需要的状态有没有对应读取字段 | 【状态读取缺口】 |

第 4、5 维必须对着本节末尾的能力块（或 `docs/capabilities-manifest.md`）核，**不许凭记忆**。
清单里没有的命令名一律视为不存在。

**输出格式（机器会解析，必须照写）**：

```
SELF_CHECK:
1 规则核验: PASS
2 道具核验: GAP 缺虫网（蛛网×30 → 工作台，虫网再抓虫）
3 地形/路径核验: PASS
4 动作原语核验: GAP 没有"抓取活物"的原语
5 状态读取核验: GAP 读不到小动物弹幕
```

- `SELF_CHECK:` 独占第一行；五行齐全；`GAP` 后面必须写缺什么。
- **五项全 PASS 才动手**。任意 GAP → 不许生成动作，先给缺口清单 + `分支A（先补前置）/ 分支B（放弃）`，
  等玩家选。
- 只读小事（查坐标、看背包、走两步）可以写 `SELF_CHECK: skip（只读）`。
- 禁止编造清单里没有的命令/字段/规则；"大概能用"算 GAP。
- 机器校验：`terraria_kb { selfcheck: "<本回合输出>" }` → `canAct` / `gaps` / `unknownCommands`。

## 9. 任务用例库（"能不能用"靠它，不靠自由通关）

用例在 `testcases/cases.json`，说明在 `docs/testcases/README.md`。

自由跑一遍游戏**不算验证**：事件和生物是随机的，碰不到的场景自检永远不触发。
所以固定成逐条用例过。九个分类（移动 / 采集 / 建造 / 合成 / NPC / 钓鱼捉虫 / 战斗 / 事件 / 装备）
共 54 条，分四个阶段推进：1 基础（移动采集建造合成）→ 2 进阶（NPC/钓鱼/装备/BOSS/基础事件）
→ 3 边界（困难模式/特殊事件/微光/稀有群系）→ 4 自由游玩。

每条用例都走同一条链路，不许跳：

```
路由（NEED_KNOWLEDGE）→ 检索库 → 五维自检 → 缺口清单
```

产出不是"这条能不能过"，而是**该补什么**：缺动作原语（要加命令）/ 撞上明确做不到（要加读取或时序原语）/ 缺知识（要补库）。
补的时候按"要补什么"批量补，不按用例一条条补。

### 推进顺序（用户定的四阶段，不许跳）

1. **离线模拟推演**（不开游戏）：`prompts/offline-sim.md` 那套规则，逐条只走
   路由 → 检索 → 五维自检 → 动作流推演，输出缺口。**这一阶段一个动作都不发。**
   好处是零风险、能一口气把 54 条过完、拿到一张全局清单，而不是跑一条改一点。
2. **统一开发修复**：对着汇总清单批量补命令 / 补读取 / 补库 / 改规则，按类开发，
   不在游戏和调试之间来回切。
3. **游戏内逐条实测**：同样的用例重新下发，验证推演出来的动作流真的能跑；
   记录推演没预判到的现实偏差（碰撞、随机怪、坐标偏差、机制细节记错）。
4. **回归 + 开放自由探索**：全量用例实测通过后再放开自由游玩，只用来抓长尾。

铁律：**离线推演发现不了"知识本身记错"**——那只有阶段③进游戏才会暴露。
所以推演里对某条 KB 结论不完全确定的，标 `疑问:<条目>`，一律留到实测阶段打勾。

```bash
npm run tc:list                 # 54 条都是什么
npm run tc:check                # 用例库自身有没有写错
npm run tc:sim                  # 阶段①：生成推演包（不连游戏）
npm run tc:dry                  # 离线预检，出缺口清单
node scripts/testcases.mjs --dry --md docs/testcases/gap-report.md --json docs/testcases/baseline.json
node scripts/testcases.mjs --dry --baseline docs/testcases/baseline.json   # 回归：修好了/新坏了/仍缺
```

推演结论写 `docs/testcases/sim-gaps.md`（逐条判定 + 四类缺口 + 疑问清单，
测试会校验 54 条都在、编号不重复、四类齐全）。

**硬规矩**：
- 用例里写 `missingCommands` 的东西，补上之后必须回来删声明（`--validate` 会报，测试会红）。
- 用例里写 `unsupported` 的，只能是能力清单 `notSupported` 里真有的条目，不许自己编理由。
- 新加命令或读取字段，先跑 `npm run tc:dry` 看它让哪几条用例从 GAP 变 PASS，再提交。


## 11. 全局异常处理（执行过程中每步都要过一遍）

元自检管「动手之前缺不缺东西」，本节管「动手过程中出了意外怎么办、处理完怎么回主线」。
规则全文：`prompts/exception-policy.md`（`terraria_exception {prompt:true}` 取）。实现：`src/exception-policy.js`。

三条硬约束：

1. **主目标只有一个**，应急是临时子状态，同一时刻最多一个；处理完必须回到原子步骤，不许借机改做别的、也不许被应急替换。
2. **重试必须有预算**：同处跳跃 2 次 → 重新寻路 3 次 → 卡死重规划 3 次，用完判任务失败并上报，绝不静默重试、绝不假装继续。
3. **每次触发/重试/失败都留日志**（六列：序号 / 异常 / 证据 / 动作 / 结果 / 第几次）。

固定分支（不要临场发挥）：

| 异常 | 处理顺序 |
|---|---|
| 地形阻挡 | 先跳 → 跳不过就 A\* 绕行 → 连续 3 次坐标不变判卡死 → 重规划 3 次仍失败则判任务失败 |
| 敌对生物 | 先避险后撤；血量 <45% 边逃边喝药；<25% 只逃不打；只有甩不开且 ≤6 格才反击，打完回原步骤 |
| 致命环境（岩浆/微光/蜂蜜/深水/大落差）| 立刻松键停步 → 退回上一格安全位 → 探明再走；绕不过就报「此路不通」改道 |
| 道具缺失 / 背包满 | 缺道具：暂停并报缺什么、缺几个；背包满：不中断主线，但捡东西前先腾格子 |

调用方式：`terraria_exception {observe:true}` 读实时状态判异常；给 `goal`/`step` 出应急计划；
`apply:true` 才真的下发移动/跳跃/喝药/攻击（都走 `move`/`jump`/`heal`/`attack`，不改世界）。


<!-- caps:begin 本节由 scripts/kb-caps.mjs 生成，不要手改 -->

**只读（49）**：api blood bloodmoon bobber boss chatpoll eclipse event fields find frost frostmoon help hook hookdebug housediag housing humaninput idmap interact inventory items keyevents map methods navrules needs nodig npc path ping players pumpkin pumpkinmoon quest recipes research roomcheck scan shop shopdebug shops snowmoon state tile tiledata travelstatus walkdiff walkmode

**动作（56）**：accessory attack buy capture castline chat chatpost climb craft down drop duplicate equip fill fly follow goblin grab grapple gravity heal homeroom idle jump left left liquidop martian mine minion mirror move moveto pirate place reel removeblock right right save select sell shoot snowman splitstack startevent stop talk trash travel unhook up up use walkto wall

**看参数（4）**：chest down guard mount

**明确做不到**：
- 通用弹幕状态
- 生物 AI 内部状态
- 随机事件与时机窗口
- NPC 对话选项与任务进度
- 容器以外的容器
- 画面语义
- 音效与事件字幕
- 合成树自动规划

完整表（含每个只读命令返回哪些字段）见 `docs/capabilities-manifest.md`。

<!-- caps:end -->
