# 走路（walkto）重构方案：现状 → 三层结构对照

> 目的：把现在这 500 行、十几个 `if` 互相抢 `return` 的 `TickWalk` 拆成
> **感知 / 决策 / 执行** 三层，落实 `docs/kb/navigation.md` 的滚动局部路点规则。
> 本文只做对照，不动代码。

## 一、现在的 TickWalk 在执行什么

每 tick 顺序判断，任何一条命中就 `return`：

| # | 判据 | 命中后的动作 | 与谁冲突 |
|---|------|--------------|----------|
| 1 | `!WalkActive` | 返回 | — |
| 2 | 死了 / 超时 / 岩浆 / 泡水 | `StopWalk` 或上浮 | 泡水时直接 return，其他判断全失效 |
| 3 | 已到目标（±tol） | `StopWalk("arrived")` | — |
| 4 | 路点已到（±1） | 清 `WalkHasWp` | — |
| 5 | `py > WalkY + 2` | `StopWalk("overshoot")` | 与 6 的落差判定重叠 |
| 6 | 身体两格都实心 | 满跳 + 计数 >24 报 `embedded` | 与 9 的自救跳重叠 |
| 7 | 位置 6 帧不动 | 重算 + 跳 + 计数 >3 报 `stuck` | 与 6 重叠 |
| 8 | 每 10 tick 扫 8 列地面 | 缓存 `WalkScan` | 与 9/10 共用地形，但只在 8 里刷 |
| 9 | 前方无地面 / 落差 >14 且无门 | 重算 → 不行报 `cliff` | **今天改了 3 版**，每次影响 10 |
| 10 | 前方是门 | `doorAhead=true` 放行 | 与 11 的建材判定互斥 |
| 11 | 前方是建材 | 重算 → 不行报 `blocked-build` | 与 12 的墙高判定重叠 |
| 12 | 前方墙高 h | `h<=1` 走、`2..6` 跳、更高停 | 与 9/11 都对"前方有东西"下判断 |
| 13 | 前方是坑（≤3 宽） | 跳 | 与 9 的落差判定重叠 |

**核心毛病**：9 / 11 / 12 / 13 是四套**互相独立**的"前方有障碍"判断，各自有阈值、
各自能 `StopWalk`。改一个阈值就会和另一个打架——这就是今天"改一处坏三处"的来源。

## 二、新结构：三层，每层只干一件事

### 第 1 层：感知（`Sense()`，纯读，无副作用）

每 N tick（或位置变化 ≥1 格时）刷新一份只读快照：

```
struct Sense {
    int px, py;            // 玩家格坐标
    bool onGround;         // 脚下 1~2 格内有可站立物
    int  aheadAir;         // 前方连续可穿过格数（1..8）
    int  aheadWallH;       // 前方墙体高度（从脚下往上数，0=无墙）
    int  aheadDropDepth;   // 前方第一块可站立物的落差（-1 = 8 格内没有）
    bool aheadDoor;        // 前方 3 格内有门（开/关都算）
    bool aheadBuild;       // 前方墙体是不是自己造的建材
    bool liquidAhead;      // 前方 1~2 格有液体
}
```

规则：
- **只读**，不改世界、不按键。
- 一次读一片（8 列 × 20 行），不是逐格问。
- 平台/半砖算"可站立"（`TileStand`），开门算"可穿过"。

### 第 2 层：决策（`Decide(Sense)`，纯函数）

输入快照 + 当前目标，输出**一个**动作：

```
enum Step { GoLeft, GoRight, Jump, Stop, Replan }
```

判定顺序**唯一**，且不再有多个 `StopWalk`：

| 条件 | 动作 |
|------|------|
| 已到目标 | `Stop`（reason: arrived） |
| 有液体 | `Stop`（reason: liquid） |
| `aheadWallH == 0 && aheadDropDepth in 0..14` | `Go<dir>`（正常走，允许下坡） |
| `aheadWallH in 1..6 && 头顶空` | `Jump`（按 `MsForTiles(aheadWallH+1)` 选力度） |
| `aheadDropDepth in 7..14` | `Go<dir>`（走下去，允许自然坠落） |
| `aheadDoor` | `Go<dir>`（原版会自动开门） |
| `aheadBuild` | `Replan`（不挖，换路） |
| 其他（墙太高 / 探不到地 / 深坑） | `Replan`，重算 3 次仍不行才 `Stop` |

### 第 3 层：执行（`Act(Step)`，写 control）

只做一件事：把 `Step` 翻译成 `controlLeft/controlRight/controlJump`。

- **不再改 `velocity.X`**（会破坏台阶/斜坡/门的原版逻辑）。
- `Jump` 按 `MsForTiles` 给的力度设一次 `velocity.Y`，`controlJump = true`。
- 写完就返回，等下一 tick 回读位置，由第 1 层重新感知。

## 三、字段收敛

现在 12 个 static 字段 → 4 个：

| 保留 | 用途 |
|------|------|
| `WalkActive, WalkX, WalkY, WalkTol` | 目标 |
| `WalkWp`（单个路点） | 当前短期路点，到达即清 |
| `WalkRepaths` | 连续重算计数（≥3 判失败） |
| `WalkSense` | 上一次感知快照 |

删除：`WalkStuck / WalkStuckHard / WalkLastPx / WalkRepathSame / WalkRepathPx /
WalkHasWp / WalkScan / WalkScanAt / PathGreedy`（职责被三层吸收）。

## 四、迁移步骤（每步都可单独回滚）

1. 加 `Sense` 结构 + `SenseWorld()`，**只读**，先用日志输出，不改行为。
2. 加 `Decide(Sense)` 纯函数 + 单元测试（给定快照 → 期望动作），覆盖 13 行表格。
3. `TickWalk` 改成：`Sense()` → `Decide()` → `Act()`，旧分支全部删掉。
4. 真机跑三条基准路线：出生点→房 A、房 A→房 B、房 B→东侧空地，记录到达/卡住位置。
5. 对比新旧：只有"同等或更远"才算通过。

## 五、验收基准（真机）

| 路线 | 现在 | 目标 |
|------|------|------|
| 出生点 → 房 A 门（2161） | 卡 2173 | 到达 |
| 房 A → 房 B 门（2177） | 未测 | 到达 |
| 房 B → 东侧 x=2210 | 未测 | 到达 |
| 全程 | — | 不挖任何建材、不无故跳 |
## 六、检测器变更记录（规范：一次只改一类，改前后跑同一套用例）

用例集：`test/sense.test.js`（13 例，覆盖平台／平台尽头／门／天然墙／自建墙／上坡／坑／悬崖／卡方块／门格／到达／危险态／方向）。
离线镜像：`src/sense-model.js`，字段名与 C# 的 `ActorSense` 一一对应。**改 C# 必须同步改它并重跑。**

### 变更 1（悬崖 vs 坑）— 唯一一次改动

- **要解决的单一问题**：一格宽的坑被误判成悬崖，导致 `avoid-cliff` 抢先于 `jump-gap`，人停在坑边不动。
- **改动点**：
  - `cliffAhead`：由「第 1 列够不着地」→「前 3 列全都够不着地」。
  - `gapAhead`：对岸深度由「第 1 列的 depth」→「第 2~4 列第一个落得下脚的地面与脚下的高差」。
  - 文件：`DshTerrariaBridge/BridgeSystem.cs`（`SenseWorld`）、`src/sense-model.js`。
- **信号定义未变**：`blockingSelf / onGround / stalled / cliffAhead / doorAhead / buildAhead / gapAhead / wallH` 字段名与含义保持不变，`Evaluate`、知识库、行为规则一行未动。
- **前后对比**：改前 13 例中 3 例红（坑、上坡、平台尽头；后两例是用例自身写错，已改正并注明）；改后 **13/13 绿**。

---

## 四、实施结果（2026-10-26/27 这一轮做完的）

三层已落地，`TickWalk` 不再是一条"谁先命中谁 return"的链，而是：

```
SenseWorld()       感知：算 WalkScan（8 列地面 + 落差 + 墙高 + 是否建材 + 是否门 + 是否坑）
ActorDecide()      决策：按优先级返回一条 ActorAct{rule, priority, kind}
ActActor()         执行：只写 controlLeft/Right/Jump，必要时 WalkJump(高度)
```

优先级从高到低（`DshTerrariaBridge/BridgeSystem.cs` 约 1069 行起）：

| 规则 | 何时命中 | 动作 |
|---|---|---|
| `unstick-blocked` | 身体两格都实心 | 满跳把它挤出来 |
| `stall-jump` | 位置 6 帧不动 | 满跳 |
| `climb-slope` | 前方墙高 1~6 | 跳 `min(6, 墙高+1.2)` 格 |
| `jump-gap` | 前方是坑 | 跳 4 格 |
| `open-door` | 前方是门 | 放开门的控制，等它自己开 |
| `move-waypoint` | 其余 | 朝路点走 |

**关键修正（都是真机上量出来的）**

1. **平台不是空气也不是实心**：`TileSolid` 只查 `Main.tileSolid`，平台（19）在里面是 `false`，
   于是被当成"能穿过去"，人一走到平台上就掉下去。新增 `TileStand()`（实心 **或** 平台），
   地面判定全部改用它 —— 这一条把 `walkto` 从"走 11 格"提到"走 68 格"。
2. **门格可以站**：门的 `tileSolid` 是 true，但人站在门口时会踩空判成悬崖。A* 的
   `PathFloorAt` 对门格返回"可站"。
3. **横移只查落点那一层**：从平台边跳到屋顶时，头顶和脚下都在平台行上，横向判定若把
   整个身体高度都算进去就会被自己的平台挡住。
4. **跳跃按高度差取时长**：`JumpCurve`/`MsForTiles` 已有，默认改满跳 400ms（6.1 格），
   只有"只差一格"才故意短跳。
5. **走的时候不跳、不挖**：用户连着纠正三次（"明明走就可以为什么要边走边跳"、
   "万一遇到更大的坑你不是需要大跳吗怎么会写死"、"为什么会莫名其妙挥镐子"）。
   现在只有 `wallH>=1` 或 `gapAhead` 才跳，`PathAllowDig=false` 时一律不挖。

**A* 与滚动局部路点**

- `ComputePath` 是离线镜像 `src/path-model.js` 的 C# 版：四类边（走 / 跨 / 跳 / 落）。
- 跳边受 `JUMP_VERT_MAX`（6.1 格）、`JUMP_HORIZ_MAX`（4 格）、`JUMP_VERT_DOWN_MAX` 约束。
- 固定场景集 `test/path.test.js` 15 例（平地/台阶/高台/坑/实心墙/门/平台/建材/水…），
  改任何常量必须先跑这套再提交；期间靠它抓出一个真缺陷：`JUMP_HORIZ_MAX` 3 是错的，
  满跳滞空时间足够横移 4 格。

**还没解决的**

- 从地面跳上"实心屋顶 + 平台边缘"这种组合，需要先退回平台下方再起跳；
  现在会规划出这条路径但执行时容易错过起跳点（冲过头），属于**执行层时序问题**。
- `fill`/`place` 在"完全悬空的一整排"上仍然放不住：需要逐格从有支撑的一侧连续铺，
  且看不到支撑时会静默失败（回包 `armed:true` 但回读是空的）。
