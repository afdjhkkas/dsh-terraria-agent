# 阶段①离线推演结果（54 条用例）

> 生成方式：`npm run tc:sim`（推演包见 `docs/testcases/sim-packets.md`），
> 推演规则见 `prompts/offline-sim.md`。
> **这一阶段没有开游戏、没有发动作**，只走「路由 → 检索 → 五维自检 → 动作流推演」。
>
> 结论口径：`PASS` = 现有命令/字段能拼出完整动作流；`GAP` = 缺东西，缺口编号见第四节。

## 一、总览

| 判定 | 条数 |
|---|---|
| PASS | 30 |
| GAP | 24 |

缺口共 **29 项**，按四类归档：缺命令 9、缺读取 6、缺知识 10、缺规则与判定 4。

阶段分布：阶段 1 的 17 条里 **15 条 PASS**，阶段 2/3 的缺口几乎全部集中在
「新领域」（钓鱼/捉虫/事件/容器/液体）和「跨区导航」上。

## 二、逐条推演

| id | 判定 | 动作流要点 | 缺口 |
|---|---|---|---|
| M01 | PASS | `select(镐)`→`move` 分段→每段 `state` 判卡住→`jump` 跨坑 | — |
| M02 | PASS | `jump` 起跳/连跳→`state.grounded/velocity`；落差看 `hp` | — |
| M03 | PASS | 远离出生点→`mine` 两列逐格→每格 `tile.liquid` 判水→`climb`/绳下行 | 疑问 Q1 |
| M04 | GAP | `walkto` 分段→`state.player.zones` 认群系 | **D1** 远距群系定位 |
| M05 | GAP | 逐层下行→识别岩浆→绕开 | **R1** 液体类型 |
| M06 | GAP | 找发光蘑菇地/微光池 | **R1** 液体类型、**D1**、**K1** |
| M07 | GAP | 打开大地图读数 | **C4** `map` |
| M08 | PASS | `move {dir:right,ms:600}` → 逐帧 `state`：`flagRight` 当帧真值 + `velocity[0]` 加速曲线 + 位移 | —（红线：`ropeContact.onRopeReal` 为真时原版禁止横移，须先下绳） |
| M09 | PASS | `tile` 确认头顶 ≥3 格空 → `jump {ms:400}` → `state.flagJump` 与 `velocity[1]` 出负值、跳幅 4.5~5.5 格 | —（红线：`grounded` 必须 true，跳不动先查头顶） |
| M10 | PASS | `find type:213` 读绳上下界 → 站到脚底格 213 → `ropeContact.onRopeReal/pulley` 为真 → `move {dir:down}` → `velocity[1]` 出正值 | —（红线：脚底格不是 213 时按什么都不会动，属引擎判定） |
| M11 | PASS | 同上 → `move {dir:up}` → 绳顶以下 `velocity[1]` 出负值；已到绳顶时 `velocity[1]===0` 判 PASS | —（旧 `onRope` 看身体范围会误报，须改用 `ropeContact.onRopeReal`） |
| G01 | PASS | `select(斧)`→砍树干→`grab` | — |
| G02 | PASS | `mine`×10→`grab`→`inventory` 差额 | — |
| G03 | PASS | `find(type=铜/铁)`→`mine`→`inventory` 计数 | — |
| G04 | GAP | 挖狱石/黑曜石 | **R1**、**K1**、**K4** 镐力门槛 |
| G05 | PASS | `state.world.time` 判昼夜→`find` 花→`grab` | **K3**（时间窗细节） |
| G06 | PASS | `find(type=罐)`→`mine`→`grab` | — |
| G07 | GAP | 开箱取物 | **C7** 容器存取、疑问 Q2 |
| G08 | GAP | 空桶舀水/蜂蜜 | **C9** 液体操作、**R1**、**D4** |
| G09 | PASS | `grab` 循环→`inventory` | — |
| B01 | PASS | `place` 石平台×20→`wall` 木墙 | — |
| B02 | PASS | `place` 封闭房+门+桌椅+光源→`housediag` | **D2**（真实入住无法验证） |
| B03 | PASS | `place` 边铺边 `state` 防坠 | — |
| B04 | PASS | `place` 火把/工作台/熔炉/箱子（2×2 锚点） | — |
| B05 | GAP | 两层平台+上下连通 | **K2** 梯/绳差异 |
| B06 | PASS | `place` 门/椅/桌→`housediag` | **D2** |
| K01 | PASS | `craft` 工作台/木剑/木镐 | — |
| K02 | PASS | 工作台旁 `craft` 火把/平台/墙 | — |
| K03 | PASS | 熔炉旁 `craft` 矿→锭 | — |
| K04 | GAP | 查配方做虫网/钓竿；抓虫 | **C2** `capture` |
| K05 | GAP | 把材料存箱 | **C7** 容器存取 |
| K06 | GAP | 拆半堆叠 | **C6** `splitstack` |
| K07 | PASS | `recipes` 查铁镐需求 | — |
| N01 | GAP | 向导问答 | **D3** 对话文本读不到 |
| N02 | PASS | `talk`→`shop`→`buy` | **D2**（需 NPC 在场） |
| N03 | GAP | 护士回血 | **C8** 对话选项 |
| N04 | GAP | 渔夫任务 | **C3** `quest` |
| N05 | PASS | `shop`/`buy`/`sell` | **D2** |
| N06 | PASS | `housediag` 报缺哪条 | — |
| F01 | GAP | 抛竿→看浮标→收线 | **C1** `castline/reel`、**R5** 浮标状态、**K5** |
| F02 | GAP | 虫网抓蝴蝶/蠕虫/青蛙 | **C2**、**R4** 小动物列表、**K6** |
| F03 | PASS | `place` 笼子 | — |
| F04 | GAP | 任务鱼闭环 | **C1**、**C3**、**K5** |
| W01 | PASS | `attack` 近战 | — |
| W02 | PASS | `shoot` 弓射飞行目标 | — |
| W03 | PASS | `state.hp` 判血→`heal` 或撤退 | — |
| W04 | GAP | 去腐化清怪 | **D1**、**K1** |
| W05 | GAP | 晶状体→召唤物→祭坛→BOSS | **K8** 祭坛合成、**D1** |
| W06 | PASS | `grab` 捡掉落 | — |
| E01 | GAP | 血月守夜 | **R2** 事件状态、**C5**、**K10** |
| E02 | GAP | 手动开日食/南瓜月/霜月 | **C5** `startevent` |
| E03 | GAP | 武器丢微光池再取回 | **R1**、**K1** 微光转换 |
| E04 | GAP | 血肉墙→困难模式 | **K9** 召唤流程、**R1** |
| E05 | GAP | 入侵守基地 | **C5**、**R2**、**K10** |
| Q01 | PASS | `equip`/`accessory` 三栏 | — |
| Q02 | GAP | 喝药看 buff 计时 | **R3** buff 剩余时间 |
| Q03 | PASS | `select` 切换→`attack`/`shoot` | — |
| Q04 | PASS | `hook`/`unhook` 过沟 | — |

## 三、四类缺口汇总

### A. 缺命令（9）

| 编号 | 缺什么 | 受影响用例 | 补法 | 量级 |
|---|---|---|---|---|
| C1 | `castline` / `reel`（抛竿、收线） | F01 F04 | 新动作：选中钓竿→`BeginItemUse` 朝水面→等咬钩 | 中 |
| C2 | `capture`（虫网/笼子捕捉活物） | F02 K04 | 新动作：选中虫网→对目标生物所在格挥动 | 中 |
| C3 | `quest`（渔夫任务接取/交付） | N04 F04 | 新动作：与渔夫对话后选任务项 | 小 |
| C4 | `map`（大地图读出已探索范围与自身坐标） | M07 | 读 `Main.Map` 尺寸/探索位图 | 小 |
| C5 | `startevent`（手动触发事件） | E01 E02 E05 | 走 `Main.StartInvasion` 等原版入口，仅测试用 | 小 |
| C6 | `splitstack`（拆分堆叠） | K06 | 背包内存操作（不改世界，属角色行为） | 小 |
| C7 | `containerTake` / `containerPut`（箱子存取） | G07 K05 B04 | 现 `chest` 只读参数，补开箱后的格位读写 | 中 |
| C8 | `talkOption`（对话选项：护士治疗、向导问答） | N01 N03 | 按索引选对话项，只回"选项 id/结果"，不读文本 | 中 |
| C9 | `liquidOp`（桶舀/倒液体） | G08 | 选中空桶→`BeginItemUse` 对液面 | 中 |

### B. 缺读取（6）

| 编号 | 缺什么 | 受影响用例 | 补法 |
|---|---|---|---|
| R1 | **液体类型**（水/岩浆/蜂蜜/微光，不只是数量） | M05 M06 G04 G08 E03 E04 | `tile`/`scan` 增加 `liquidType` 字段 |
| R2 | **事件状态**（血月/日食/入侵是否进行中、剩余时间） | E01 E05 | `state.world` 增加 `event` 块 |
| R3 | **buff 剩余时间** | Q02 | `state.player.buffs[]` 增 `time` |
| R4 | **小动物/生物列表**（critter 分类 + 位置） | F02 | `npc` 结果加 `kind: critter` 分类 |
| R5 | **浮标/咬钩状态**（钓鱼专用，不是通用弹幕） | F01 F04 | 只读"浮标存在/下沉"布尔，避免通用弹幕读取 |
| R6 | **地图已探索范围** | M07 | 与 C4 同一份数据 |

### C. 缺知识（10）

| 编号 | 缺什么 | 受影响用例 | 落到哪个库 |
|---|---|---|---|
| K1 | 液体与危险：岩浆伤害、黑曜石生成条件、微光转换 | G04 M05 E03 E04 W04 | `world_biome` + `loot_source` |
| K2 | 附着物差异：梯子 / 绳 / 平台 的上下行规则 | B05 | `atomic_action` |
| K3 | 采集时间窗：花开放、生物出没时段 | G05 F02 | `loot_source` |
| K4 | 镐力门槛表（哪级镐能挖哪种矿） | G04 | `item_operation` |
| K5 | 钓鱼：抛竿/咬钩/渔力/鱼种 | F01 F04 | `loot_source`（新小节） |
| K6 | 虫网与笼子：能抓什么、笼子怎么摆 | F02 K04 | `loot_source` + `build_standard` |
| K7 | 容器容量与存取规则 | G07 K05 | `item_operation` |
| K8 | 祭坛合成与召唤物（克苏鲁之眼等） | W05 | `progression_boss` |
| K9 | 血肉墙召唤（巫毒娃娃→岩浆） | E04 | `progression_boss` |
| K10 | 事件触发条件与被入侵时的处置 | E01 E02 E05 | `progression_boss` / 新库 |

### D. 缺规则与判定（4）

| 编号 | 缺什么 | 受影响用例 | 说明 |
|---|---|---|---|
| D1 | **远距群系定位** | M04 M06 W04 W05 | `path` 只能在载入区（约 ±100 格）内算路，跨区怎么走没有规则 |
| D2 | **引擎房屋判定坏死** | B02 B06 N02 N05 | 引擎 `CheckRoom` 在本档恒报错，NPC 真实入住无法验证，只能 `housediag` 自评 |
| D3 | **NPC 对话文本读不到** | N01 N03 | `notSupported` 明列；只能按索引点选项，不能读回答 |
| D4 | **液体交互边界** | G08 | 水不能挤开、桶的装取边界未验证 |

## 四、留到阶段③实测验证的疑问

推演依赖知识库，**知识记错了推演发现不了**。以下 5 条标为疑问，进游戏时逐条打勾：

| 编号 | 疑问 | 怎么验 |
|---|---|---|
| Q1 | `tile`/`scan` 到底返不返液体类型 | 对水/岩浆各读一格，看字段 |
| Q2 | `chest` 是只读还是能开关容器 | 对已知箱子调用，看返回与 `Main.chest` |
| Q3 | 空桶 `use` 能否真实舀水 | 水边选中桶挥动，读背包变化 |
| Q4 | 小动物在不在 `npc` 列表里 | 找蝴蝶调用 `npc`，看有没有 |
| Q5 | 绳与梯子上行是否同一套逻辑 | 各铺一段，`climb` 对比 |

## 五、阶段②修补顺序（按"解锁用例数 / 工作量"排）

1. **R1 液体类型** —— 一条读取解锁 6 条用例（M05 M06 G04 G08 E03 E04）
2. **C5 startevent + R2 事件状态** —— 解锁 3 条事件用例
3. **C1 castline/reel + R5 浮标 + K5 钓鱼知识** —— 解锁 2 条
4. **C2 capture + R4 小动物 + K6** —— 解锁 2 条
5. **C7 容器存取 + K7** —— 解锁 3 条（G07 K05 B04 验证）
6. **C3 quest + K8 + K9** —— BOSS/任务链闭环
7. **D1 远距导航规则** —— 解锁 4 条，但要先解决"载入区外读不到地图"这个根本限制
8. 其余（C4 map、C6 splitstack、C8 talkOption、C9 liquidOp、R3 buff、K2 K3 K4）——
   单条小补，可并行
## 四、阶段②进度：第一批已补（编译通过，**实机未验**）

上一节的逐条判定是阶段①（离线推演）当时的快照，留作对照；下表是补完之后的状态。
权威的当前状态看 `docs/testcases/gap-report.md`（`npm run tc:dry` 生成）。

| 缺口 | 补成了什么 | 影响的用例 | 验证程度 |
|---|---|---|---|
| **R1** | `tile` / `scan` 增加 `liquidType` / `liquidName`（水/岩浆/蜂蜜/微光） | M05 M06 G04 G08 E03 E04 | 编译通过，实机未验 |
| **R2** | `state.world` 增加 `bloodMoon` / `eclipse` / `pumpkinMoon` / `snowMoon` / `invasionType` / `invasionSize` / `invasionProgress` / `anglerQuest` / `night` | E01 E05 | 编译通过，实机未验 |
| **R3** | `state.buffs[]` 增加 `timeSec`（剩余秒数） | Q02 | 编译通过，实机未验 |
| **R4** | `npc` 列表增加 `critter`（是不是小动物）/ `catchItem`（抓到手会变成什么） | F02 | 编译通过，实机未验 |
| **R5** | 新命令 `bobber`：浮标位置/速度/是否浸液 | F01 F04 | 编译通过，实机未验 |
| **C1** | 新命令 `castline` / `reel` | F01 F04 | 编译通过，实机未验 |
| **C2** | 新命令 `capture`（虫网，可给 `who` 或 `x,y`） | F02 K04 | 编译通过，实机未验 |
| **C4** | 新命令 `map`：已探索区域包围盒 + 占比 + 自身地图坐标 | M07 | 编译通过，实机未验 |
| **C5** | 新命令 `startevent`：血月/日食/南瓜月/霜月/四种入侵（**仅测试用**） | E01 E02 E05 | 编译通过，实机未验 |
| **C6** | 新命令 `splitstack`：把一叠拆一半到空格 | K06 | 编译通过，实机未验 |

第二批（C3 `quest`）已补并在真机验过（见下）；`liquidop` 已补但被道具卡住；
`containerTake/Put` 其实第一批就有（`chest {action:"take"|"put"}`），当时是我没查清单就当成缺口；
仍缺：`talkOption`（对话选项文案读不到、也点不了）、三块知识（钓竿与鱼饵取得链、容器存取规则、祭坛与血肉墙召唤链）。

**这批的诚实边界**：全部只做到"编译通过 + 用例声明已更新"，一次游戏都没进。
按用户定的四阶段，真正的验证在阶段③逐条实测；届时重点验三件事：
`tile` 的液体类型与游戏画面一致、`bobber` 的咬钩信号可判、`capture` 真能抓到东西。

### 阶段②第一批的实机验证结果（真机跑过，逐条留证）

| 补的东西 | 实测 | 证据 |
|---|---|---|
| `state.world` 事件字段 | ✅ | `bloodMoon=false eclipse=false pumpkinMoon=false snowMoon=false invasionType=0 invasionSize=0 anglerQuest=22 night=true`（当时确实天黑了） |
| `map` | ✅ | `mapWidth=8401 mapHeight=2401 revealedPercent=1 explored={1552,80,4144,1152}`，与已探索区域相符 |
| `tile` 液体字段 | ✅ 字段在（干格） | `liquid=0 liquidType=0 liquidName=""`；**有液体的格子还没取样到** |
| `scan` 加列 | ⚠️ 结构对、无样本 | `stride=5`、`fields=[type,wall,liquid,liqType,shape]`，但基地 ±40 格内 `liquid>0` 的格子为 0 |
| `npc` 小动物字段 | ✅ | 恶魔眼 `critter=false catchItem=0`（夜里 40 格内没有小动物） |
| `splitstack` | ✅ | 蛛网 48 号格 94 → 拆 40 到 58 号格，回来读是 `x54` + `x40` |
| `castline` + `bobber` | ✅ | 对着地面抛竿后 `bobber` 读到 `count=1, tile=[2116.2,301.83], inLiquid=false` |
| `reel` | ✅ | 收线后 `bobber` 从 `count=1` 变 `count=0` |
| `startevent` | ⚠️ 只验了错路 | 传 `nonsense` 得到"只支持 xxx"的明确拒绝；**真开事件没做**（会改变局面，等阶段③按用例来） |
| `capture` | ❌ 无法验 | 虫网（物品 1991）**没有配方**，背包里也没有；夜里基地 40 格内没有小动物 |
| `liquidop` | ❌ 无法验 | 空桶要 2 个铁锭，背包只有 1 个；没有水桶也没有水 |
| `state.buffs[].timeSec` | ❌ 无法验 | 唯一药水是虫洞药水，没有队友时用了不留 buff |

**这一轮的教训（已写进第一一七节）**：
1. 我先前用 `craft {item:199}` 想造虫网，199 不是虫网（真正的虫网是 1991），而且 1991 压根没有配方。
   **找 ID 必须查 `docs/kb/_generated-ids.md`，不能拿数字猜。**
2. `castline` 第一版返回 `cast:true`——但"用了钓竿"和"抛出浮标"是两件事。已把返回改成
   `used:true` 并写明"要用 `bobber` 读才算抛出"。这就是规则 2（不许把命令生效当成操作发生）。
3. 基地太挤：`travel right` 走了 40 秒只前进 5 格，`move` 也几乎不动（被自己砌的房子挡着）。
   想验证液体得先离开基地，这本身就说明"基地被自己堵死了"是个待收拾的问题。

### 阶段②第二批的实机验证结果

| 补的东西 | 实测 | 证据 |
|---|---|---|
| `quest` | ✅ | `{"quests":[{"npc":"Angler","kind":"daily_fish","index":22,"itemId":2472,"itemName":"向导巫毒鱼","finishedToday":false,"questsFinished":0}],"anglerPresent":false}` |
| `talk` 的 `chatText` / `cornerItem` / `isAngler` | ⚠️ 已装上，**没验到** | 编译通过、`cycle.ps1` 打包部署成功；但基地 80 格内没有任何城镇 NPC（`npc` 只回一只僵尸），渔夫也不在（`anglerPresent:false`），所以这几行还没被读到过 |
| `chest` 的 `take` / `put` | ✅（本来就有） | 第一批就实现了，这轮复查发现"缺 C7"是我漏查清单 |

**这一轮学到的**：
1. `quest` 的口径是"引擎里真实存在的任务字段"，不是"我猜某个 NPC 有任务"。
   `api` 查了两遍才确认：`Player` 上只有 `anglerQuestsFinished`，当前任务在 `Main.anglerQuest`，
   要的鱼在 `Main.anglerQuestItemNetIDs[Main.anglerQuest]`。先查再写，比先写再猜省一轮部署。
2. 我把不到场的 NPC 当成"命令坏了"过一次；这次先读 `anglerPresent` 再判，
   分清"没有对象"和"命令失效"是两种失败。
