# 项目状态（截至第 195 轮）

一个人工智能体玩泰拉瑞亚的工程记录。本文只写**有证据的结论**，
没验证过的不写、测不准的标注"未验证"。

> **能力的权威清单在 [`docs/capabilities.md`](docs/capabilities.md)**（命令 → 参数 → 回读证据 → 坑）。
> 本文是"最近一轮做了什么、还剩什么"的进度摘要；
> 还需要哪些能力见 [`docs/capability-roadmap.md`](docs/capability-roadmap.md)，
> 逐轮过程见 [`docs/playthrough.md`](docs/playthrough.md)，
> 世界硬知识见 [`docs/terraria-mechanics.md`](docs/terraria-mechanics.md)。

---

## 零-甲、同一轮的最后一个缺陷：**站平台上飞不起来**（已修、已验证）

| 项 | 内容 |
|---|---|
| 现象 | 同一角色、同一条 `fly`，站在实心方块上能升 22 格，站在 type 19 木平台上 `velY:0` 一格不动；`jump` 报 `jumped:false` |
| 根因 | `BridgeSystem.cs:691` 写的是 `controlJump \|= IntentJump \|\| (triggers.Jump && JumpUntil > now)`，而 `ApplyMovement` 开头（2067-2069）**先把所有 Intent 清成 false**，后台控制又没有真键盘 → `triggers.Jump` 恒 false → 整行恒 false |
| 为什么偶尔能成 | 只有那一次"`ApplyMovement` 还没跑过、`IntentJump` 还在"的凑巧时机才起跳（第一次 fly 升 22 格就是这么来的） |
| 修法 | 去掉 `triggers.Jump &&`：`controlJump \|= IntentJump \|\| JumpUntil > now;`（与 689-690 行 up/down 的写法一致，那两行早就附了"不能躲在 triggers 后面"的注释） |
| 验证 | 18:20 打包后真机：484.7 → 462.7（+22，实心地面）→ **441.7（+21，从平台上起跳）**，修复前这一步连试 5 次全失败 |
| 提交 | `1117dde` |

## 零、这一轮（第 195 轮）：清单里 16 个命令名调不通

真机复测 `talkoption` 时撞见的：`bloodmoon` 报 `unknown command`，
而**同一条错误信息里的可用清单就写着 bloodmoon**。写脚本比对
`DshTerrariaBridge/Commands.cs:495` 的 `CommandNames` 与顶层 `switch (cmd)`：

| | 数量 |
|---|---|
| 顶层 `case` | 91 |
| `CommandNames` | 97 |
| **列表有、调不通** | **16** |

16 个分两批，来路不同：

- `left` / `right` / `up` / `down` —— 是 `move` 的 `dir` 取值（`Commands.cs:3317-3320` 的内层 switch）；
- `blood` / `bloodmoon` / `eclipse` / `frost` / `frostmoon` / `goblin` / `martian` /
  `pirate` / `pumpkin` / `pumpkinmoon` / `snowman` / `snowmoon` —— 是 `startevent` 的 `event` 取值
  （`Commands.cs:4529-4549` 的内层 switch）。

而 `scripts/kb-caps.mjs:339` 与 `scripts/kb-sync.mjs:86` 都**全文扫 `case xxx:`**，
把两个内层 switch 的取值当成了命令，于是 capabilities.json / capabilities-manifest.md /
_generated-commands.md / AGENTS.md 的能力块一起虚高 16 条（111 → 实际 91）。

**修法**：① `CommandNames` 删掉这 16 个 → 91，与顶层 case 双向差异为空；
② 两个生成器先定位 `switch (cmd)` 再用 `braceDelta` 配平出行范围，只在这个范围内扫 case；
③ 重跑生成器 → 91 commands；`npm test` 330/328/0/2；`npm run tc:dry` 56/58 不变。

**为什么一直没被发现**：`npm test` 比对的是生成物是否等于生成器的输出，
生成器和生成物一起错，两边一致就绿。**只有反向问清单里每个名字都能调通吗才会红。**

### 同一轮的其它结果

- **`fly` 单用即可升空**（203713 字节那版）：出生点 `fly {ms:900}` 两次，
  (4198.5,484.7) → (4198.5,462.7) → (4198.5,441.7)，各 +22 / +21 格，**不再需要横向 `dir`**。
- **`talkoption` 端到端未验证**：命令存在、状态机 `phase:0/idle` 正常，但玩家落在基地
  462.7 层一条 5 格宽的死槽里（`scan` 显示 x=4231–4235、槽底 y=464 平台、槽顶 y=459 楼板，
  缺口正上方是 449–458 层的柱子），所有 town NPC 都在 391.8–457.8 层、垂直 21 格以上，
  `talk` 的 5 格阈值必然拒绝。**不是代码缺陷，是场景不可达**（换角色/换世界可复测）。
- 新测通过：`walkmode` / `navrules` / `idmap`（693 tiles / 347 walls / 5519 items）/
  `nodig` / `fields` / `methods` / `hookdebug` / `housediag` / `travelstatus` /
  `gravity` / `shoot`；`chest` 附近无箱时诚实拒绝。

---

### 零、接着测到三个新缺陷（17:38 那版已修）

继续逐条实测剩余命令时，撞到三条，全部当场修掉并重新打包验证：

| # | 缺陷 | 现象 | 根因 | 修法 / 验证 |
|---|---|---|---|---|
| 14 | `shopdebug` 抛未捕获异常 | `Error: Index was outside the bounds of the array.` | `Main.SetNPCShopIndex(npc.type)` 传的是 NPC 类型，该 API 要的是**商店 id**（`Main.instance.shop` 只有 2 个槽） | 先 `ResolveVanillaShopIndex` 解析出 shopId、解析不到明确报错、数组加空值保护；验证 `shopdebug` → `{npc:"巫医Abibe", shopId:16, hands:2, report:[…SetupShop(16) → 18 件…]}` |
| 15 | `trash` 默认吃掉整栈 | 不带 `count` 一次销毁 2081 个火把 | `count` 默认 `it.stack` | 默认改 `1`，整栈要显式传；验证 `trash {slot:14}` → `{trashed:"南瓜", count:1, left:22}` |
| 16 | `travel` 出生点保护只挡"向下" | 不带参数跑一次就在出生点旁**挖了 10 格、挥 12 下、走 106 格** | 保护条件是 `if (dig && dir == "down")` | 改成"起点或终点任一在出生点 100 格内就拒绝 dig"；验证不带参数 → 明确拒绝，`dig:false` 放行 |

**教训**：`trash` 和 `travel` 的实现都是对的，出事的都是**没传参数**那一次 ——
会毁东西的参数默认值必须是"最小的那个"；有保护圈的逻辑要按**位置**判，不能按**方向**判。

---

## 零-2、上一轮（第 194 轮）：真机实测 87 条命令，修掉 4 个 bug

这一轮是**阶段③（游戏内逐条实测）**，完整记录见 [`docs/live-test-round194.md`](docs/live-test-round194.md)。
结论先摆：**多人模式（Host & Play）是这套桥的正确工作模式** —— 铁的事实是
`paused:false` 与 `hasFocus:false` 能同时成立，服务端在跑、客户端只发输入，
所以**不抢窗口也能让角色真的动**。单人模式一失焦就 `paused`，所有动作命令拿到 `moved:false`。

实测通过 30 条命令（`move`/`jump`/`mine`/`hook`/`scan`/`needs`/`housing`/`splitstack`/`craft`/
`map`/`heal`/`capture`/`select`/`predicted`… 全表在记录文件里），挖 PalmWood 一格 1 次挥动即掉、
`hook` 把角色实打实拉近 1.4 格、`craft` 材料不足时明确报错不假成功。

**修掉的 4 个 bug**：

| bug | 现象 | 根因 | 修法 |
|---|---|---|---|
| `fly` 完全不动 | 连试 5 次 `velY:0`、翅膀 180/180、位置不动 | `Fly` 只写 `JumpUntil`/`JumpIssued`，漏 `IntentJump = true`；`BridgeSystem.cs:616` 的 `controlJump` 没有它就只剩真实按键 triggers，而这套环境没有物理按键 | 改调 `BridgeSystem.BeginJump(...)`；`unhook` 里的"跳一下"同病同修 |
| `accessory` 不带参数就卸装备 | 一次卸掉冰冻护盾、一次卸掉烈火手套 | `Json.Bool(p,"off",true)` 默认 true | 默认改只读列清单，卸下必须显式 `off:true` |
| `shop {npc}` 中文名解析不出 | `找不到 爆破专家Oten 对应的商店 id` | `GetShopName` 第三段是中文 NPC 名，`GetShopNameFromVanillaIndex` 第三段是英文键，整串前缀匹配永远失败 | 只比前两段（`ShopKeyOf`）；`SetupShop` 包 try/catch 报友好越界提示 |
| 关不掉对话 | `interact` 点空地两次都无效 | 从没包过原生的关对话入口 | 新增只读命令 `talkclose`，内部就是 `Main.CloseNPCChatOrSign()` |

**还留着的 4 条**（都已写进记录文件，不是"忘了"）：

- `path`/`walkto` 站在木平台（type 19）或绳上时判 `standS=False` → 报"没有可行路径"。
  基地是「每 5 格一层实心楼板 + 柱子」的结构，于是整栋楼走不了，只能 `move` 一格格挪。
- `save` 在 Host & Play 下不落盘（`netMode:1`，要发给专用服务端 47778）；且它报的
  `worldPath` 是 `孤立大坑.wld`，与 `state.world.name`("单人") 不一致，待查。
- `api` 的 `filter` 参数不生效：`api {type:"Terraria.Main"}` 会一次吐 772 个成员（近 2MB）。
  **文档红线：不要对 Terraria.Main 用 api**。
- 上下层没有可靠移动手段：`jump` 单次能上 5.6 格但带横向位移时落不上去，`fly` 修好后待复测。

**顺带挖到的 native 入口**（来自那次 `api` 全量清单，都是 public static）：
`Main.OpenShop(Int32)`、`Main.CloseNPCChatOrSign()`、`Main.SetNPCShopIndex(Int32)`、
`Main.DrawNPCChatButtons(Int32, Color, Int32, String, String)`、`Main.DrawNPCChatBubble(Int32)`。
前两条已经用上；`DrawNPCChatButtons` 是下一步做「点对话选项」的抓手。

### 零-0、同一轮的后续（16:15 那版，`tmod` 201569 字节）

**又修了 3 处、撤销 1 条误报**（详见记录文件的「16:15 版复测」表）：

| 项 | 结果 |
|---|---|
| `fly` | **✓ 真修好了**。补 `BridgeSystem.StopAutomatedMovement("manual-fly")` 后，在出生点 `fly {ms:900}` 升 **+22 格**、再 `{ms:600}` 升 **+13 格**。之前"必须带 `dir` 才动"是缺陷现象，不是环境限制 |
| `api` 的 `filter` | **✓** 代码读的是 `member` 而不是 `filter`；两个都认了，另加 `limit`（默认 200）+ `shown`/`truncated`，`Terraria.Main` 不再一次吐 772 条 |
| `heal` | **✓** 加了 `potionDelay` / `candidate` / `candidateSlot` / `hint`。满血时明确说"背包里有超级治疗药水，但已经满血，不浪费"，不再让 `hasPotion:false` 看着像 bug |
| `moveto`（缺陷 11） | **✓ 撤销**。基地里 `moveto {x:4250,tol:1}` 从 4197.88 走到 4250.6 到位；之前那次是柱子阻挡 + 后面的 `move` 覆盖了 `exact` |
| `drop`（缺陷 10） | **✗ 确认是真缺陷**：Host&Play 客户端下丢出去的东西，2 秒后 `grab`/`items` 都看不到 → `DropSelectedItem()` 没能让服务端建掉落物。**只在多人复现，单人待测**；返回里已加 `note` 讲清同步时序 |
| `mirror` | **新缺陷**：`why` 背包里有贝壳电话（5358），`mirror` 仍报"没有魔镜" —— 检测名单没算 Shellphone |

## 零-3、上一轮（第 193 轮）：用例库不再虚报缺口

这一轮没碰游戏代码，清的是自检工具的账：`npm run tc:dry` 报的 9 条缺口里，只有 2 条是真的。

| 用例 | 原来报的缺口 | 实际情况 |
|---|---|---|
| M07 看大地图 | 库里没有大地图读取能力 | `map` 命令早就在（`capabilities.json`），`world_biome` 也写了「地图与我到过哪」 |
| K06 拆分堆叠 | 库里没有拆分规则 | `item_operation` 的「堆叠拆分与合并」写清了拆出量夹在 1..总数-1、进第一个空格、背包满必失败 |
| F01 钓鱼 | 库里没有钓鱼流程 | `loot_source` 有「抛竿→等咬钩→收线」，`castline`/`bobber`/`reel` 三条原语都在 |
| F02 捉虫 | 库里没有捕捉机制 | `loot_source` 有虫网判定，`capture` 支持按 who 或坐标 |
| N04 / F04 渔夫任务 | 库里没有渔夫任务 | `npc-town.md:79-83` 早写了每日任务与 `quest` 回包 |
| E01 / E05 血月 / 入侵 | 没有"随机事件"能力 | `startevent` 可触发（测试入口）+ `event`/`guard`/`attack`/`heal` 可守夜 |

**真缺的只剩两条**：N04、F04 卡在同一处——渔夫任务的**交付**要点击对话选项，没有对应原语。
结果 **56/58 通过、缺口 2 条**（本轮开始时 49/58、9 条）。

真缺的知识只有 M06 微光池，已经补进库：

- `world_biome` 新增「微光池（以太）在哪」：世界外侧五分之一、地下层或洞穴层（地狱之上）、
  **永远和丛林同侧**、只生成一个；找它的顺序是认丛林侧 → 往那侧外围 → 下到地下/洞穴层 → 横向扫。
- `loot_source` 的「捉虫」补全机制：虫网是工具不消耗、判定距离和短兵器相当、目标会动要等进入范围
  那一下挥、只有小动物能进网、抓到是物品直接进背包。

**顺手加的防烂机制**：`validate()` 原来只检查"声明缺的命令其实已经有了"，现在也检查
"声明缺的知识其实已经写了"——用例带上 `needs.kbMissingProbes: [{text, keywords}]`，关键词
全出现在对应库里就报警告，让你回来删声明。配套 4 条测试（`test/kb-missing-probe.test.js`），
其中一条断言真实用例库这类警告必须保持 0 条。

## 零-4、上一轮（第 185~192 轮）新增

| 能力 | 命令 | 实测证据 |
|---|---|---|
| 自动防守（默认开启） | `guard` | 站僵尸走廊 24 秒不动：`swings 1→12`、木箭 76→57、hp 全程 100 |
| 真射箭 | `shoot` | 握 1400ms 连放两箭（95→94→93），僵尸 hp 22→2 |
| 抓钩 | `hook` / `unhook` | 挂住 4 格外木地板，角色被拉到 2100→2097.6；`grapCount` 1→0 |
| 召唤 | `minion` | 雀杖 0 → 1 只 |
| 选武器 | `select` | 回读 `held{type:99,name:铁弓}` |
| 万能入口 | `terraria_bridge` | 桥的 60 条命令都能按名字调用 |

修掉的真 bug：连射第二箭永不发（`useTime` 28 tick 而调用只握 400ms）、抓钩不吃 `mouseLeft`
（要 `Player.controlHook`）、`cycle.ps1` 编译失败仍拿旧 tmod 启动导致"假测"。

## 一、更早验证过的能力（全部有实测数据）


| 能力 | 命令 | 实测证据 |
|---|---|---|
| 移动/挖掘 | `travel` | **6.3 格/秒**（150 格/24 秒），含开挖与铺路 |
| 向下挖掘 | 逐格 `mine`（**不要用 `travel down`**） | 每格等 1.5 秒落到下一格；`travel down` 会沿斜坡横向漂移十几格，淹死过人 |
| 单格挖掘 | `mine` | 默认 1300ms（硬方块实测需求） |
| 建造 | `fill` / `place` | 铺路、造桥、填地板 |
| 战斗 | `attack` | 手持武器结算，弓 ≈14 伤害 vs 镐 ≈4 |
| 探测 | `tile` / `scan` / `find` / `zones` | `find` 可定位矿、箱子、腐化方块 |
| 交易 | `talk` / `shop` / `buy` / `sell` | 商人物品/价格/购买全链路 |
| 合成 | `craft` | 需在站点附近，缺材料会明确报错 |
| 研究复制 | `research` / `duplicate` | 旅行模式 100/100 研究后无限复制 |
| 回城 | `mirror` | 任意位置可用，唯一可靠的脱困手段 |

## 二、四条硬结论（都是踩坑踩出来的）

1. **`ms` 太短 = 命令成功但什么都没发生。**
   `mine` 需要 ≥1300ms，`move down` 需要 ≥800ms。命令返回成功但状态不变时，第一步是加长时间。

2. **地形问题只能"成片重建"，逐格修补无效。**
   铺 3 层地板 + 清 3 行净空 + 25 列宽度，两个方向才走得通。

3. **挖过的区域会回灌水、会塌陷。**
   干燥的竖井下次可能已被邻水灌满；出生点会从 y=320 漂到 y=363。

4. **y≈380 是水层线。**
   上方干燥但矿少，下方矿多但不可及——需要水下呼吸药水。

5. **角色不能站着挨打。**
   `guard` 默认开启：视线可达的敌人才打，正在挖/放时让手，打完把原工具换回来。

## 三、四种地形的对应挖法（实测速度）

| 地形 | 方法 | 速度 |
|---|---|---|
| 沙/土 | 盲挖（ms=150 大片挖） | 2.7 格/秒 |
| 石头 | 先填后挖 + 1300ms | 0.5 格/秒 |
| 丛林 | 先填后挖 + 1300ms | 3.2 格/秒 |
| 水网 | 铺平台 + 边走边铺 | 1.9 格/秒 |
| **任意（C# travel）** | 游戏内循环 | **6.3 格/秒** |

## 四、当前进度

> **⚠️ §四～§六 是早期「从零开荒」阶段的记录（当时用的是另一个角色，无护甲、只到过 x=3385）。
> 保留作历史对照。当前状态见 §零～§零-0 与文末「§八」** —— 现在的实测角色 `why` 是 500 血、
> 耀斑套装、天顶剑、有翅膀的成型号，基地在 (4260,462) 附近，30 个 NPC 常驻。

- **最远到达**：东 x=3385（丛林深处）／西 x=1557（雪原边界）
- **装备**：铁弓、铁镐×4、铁锭 3、**无护甲**
- **资源**：晶状体 19（够做可疑眼球）、蛛网 99、木材 357
- **目标**：找腐化群系的恶魔祭坛 → 做可疑眼球 → 打克苏鲁之眼

## 五、卡住的地方（诚实记录）

| 问题 | 原因 |
|---|---|
| 过不了雪原 | 无护甲，5 次尝试全部阵亡 |
| 找不到腐化 | 东侧 2000+ 格已排除；西侧在雪原之外 |
| 矿挖不够 | y>380 是水层，深层矿不可及 |
| 没有护甲 | 需要 45~75 铁矿，只挖到 7 |

## 六、下一步（按可行性排序）

1. **采水叶（沙漠）+ 珊瑚（海洋）→ 水下呼吸药水** → 打开 380 以下全部矿藏
2. 有矿 → 铁甲 → 过雪原 → 找祭坛 → 打眼球
3. 或者：先做床（织布机→丝绸→床）稳定重生点，降低每次失败的成本

## 七、代码结构

```
DshTerrariaBridge/
  Commands.cs      40+ 条命令（移动/挖掘/战斗/交易/合成/探测）
  BridgeSystem.cs  游戏内 tick 任务：挖掘、A* 寻路、直线旅行
docs/
  progression-plan.md  60+ 条编号教训（本项目的核心资产）
  playthrough.md       逐轮记录
  capabilities.md      能力矩阵
  testcases/           用例库 + 离线预检报告（56/58 通过，缺口 2 条）
test/                  330 项测试（328 通过，2 项主机差异跳过）
```

---

## 八、当前状态与下一步

**环境**：tModLoader 1.4.4.9（`D:/STEAM/steamapps/common/tModLoader`），桥 `127.0.0.1:47777`，
**必须用多人模式（开服并开始游戏）** —— 单人模式一失焦就 `paused`，所有动作命令拿到 `moved:false`；
Host & Play 下 `paused:false` 与 `hasFocus:false` 能同时成立，后台发输入就是真实输入。

**当前进度的准确说法**：

- 桥命令 **110 条**（只读 50 / 动作 56 / 看参数 4），本轮真机实测覆盖 **90+ 条**，
  通过/诚实拒绝的占绝大多数，失败项全部带行号根因记录在 [`docs/live-test-round194.md`](docs/live-test-round194.md)。
- 已修 **7 处**：`fly`（两处：`BeginJump` 的 `IntentJump` + `StopAutomatedMovement`）、
  `accessory` 破坏性默认、`shop` 中文名解析、新增 `talkclose`、`talk` 阈值 12→5 格 + 视线检查、
  `api` 的 `filter`/`limit`、`heal` 诊断字段。
- 已知未修 **4 条**：`drop` 在 Host & Play 客户端不落地、`mirror` 不认贝壳电话、
  `path`/`walkto` 不把平台当地板、`save` 在客户端不落盘。
- **N04/F04 两条用例**（渔夫任务交付、护士治疗）仍缺「点对话选项」原语。
  设计已明确：`Main.DrawNPCChatButtons` + `npcChatFocus1..4` 跨帧扫描反推按钮矩形，
  再发真实左键 —— 需要**跨帧状态机**（每帧只能测一个点，扫完约 15 秒），
  且必须打包重启才能验证，故留作独立一轮。

**下一步（按价值排序）**：

1. 做 `talkoption {index}`，把 N04/F04 补上，用例库 58/58。
2. 修 `drop`：多人客户端下改用 `Item.NewItem(...)` + 显式网络同步，而不是 `DropSelectedItem()`。
3. 修 `mirror`：把 Shellphone(5358) / Cell Phone(5358 变体) / Ice Mirror 一起认。
4. `path`/`walkto` 把 `solidTop` 平台（19/30）算进可站地板。
5. 全部改动打包重启后一次性复测，再把 6+ 笔提交 push 到 `origin/main`（**等用户确认**）。

---

**本项目的价值不在于通关，而在于把"一个 AI 能对游戏做什么"逐条测出来并留下证据。**
