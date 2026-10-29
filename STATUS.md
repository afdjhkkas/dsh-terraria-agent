# 项目状态（截至第 194 轮）

一个人工智能体玩泰拉瑞亚的工程记录。本文只写**有证据的结论**，
没验证过的不写、测不准的标注"未验证"。

> **能力的权威清单在 [`docs/capabilities.md`](docs/capabilities.md)**（命令 → 参数 → 回读证据 → 坑）。
> 本文是"最近一轮做了什么、还剩什么"的进度摘要；
> 还需要哪些能力见 [`docs/capability-roadmap.md`](docs/capability-roadmap.md)，
> 逐轮过程见 [`docs/playthrough.md`](docs/playthrough.md)，
> 世界硬知识见 [`docs/terraria-mechanics.md`](docs/terraria-mechanics.md)。

---

## 零、这一轮（第 194 轮）：真机实测 87 条命令，修掉 4 个 bug

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

## 零-1、上一轮（第 193 轮）：用例库不再虚报缺口

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

## 零-1、上一轮（第 185~192 轮）新增

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

**本项目的价值不在于通关，而在于把"一个 AI 能对游戏做什么"逐条测出来并留下证据。**
