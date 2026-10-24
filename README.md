# DSH Terraria Agent：给 DS 的操作指南（v0.2.0）

这份文档是给 DeepSeek 使用的 Terraria 工具说明。插件通过本机 tModLoader Mod 提供
`terraria_*` 工具，所有工具结果都是字符串化的 JSON；读取结果时先按 JSON 理解，再决定
下一步。协议的权威定义是同目录的 [`PROTOCOL.md`](PROTOCOL.md)，本文是用法与环境/安装说明。

## 工作前提

- 桥接监听 `127.0.0.1:47777`（`cordis.patch.yml` 里的 `config` 可改 host/port/timeoutMs，
  Mod 侧的端口在 `DshTerrariaBridge/BridgeSystem.cs` 里固定，两边必须一致）。
- 用户需要自己选择人物并进入世界。DS 不负责选择人物或进入世界。
- 世界坐标使用 tile 坐标，1 tile = 16 像素；x 向右增加，y 向下增加。
- `terraria_status` 检查桥是否启动、游戏是否在世界里、是否暂停；`terraria_observe`
  读取完整状态。
- **暂停要当回事**：`terraria_observe` / `terraria_status` 返回 `paused` 和 `hasFocus`。
  `paused:true` 时世界不推进，`terraria_move` / `terraria_jump` / `terraria_use_item` /
  `terraria_attack` 会被桥明确拒绝（错误信息会说明原因）。这时先请用户把游戏窗口切回前台，
  不要反复重试。
- 读世界（`state`/`scan`/`tile`/`npc`）只需要世界已加载；玩家类动作和背包/传送需要本地玩家。
- 同一个 Agent 的请求在插件里串行执行（`index.js` 的 `makeSerialRunner`），
  不要并行发送互相依赖的动作；但**一次调用不代表一个完整动作**，长动作要拆成小步并回读确认。
- 环境要求：Node ≥ 20。

## 方块编码（v0.2.0 已改变）

`type` 是 **`-1` 表示没有方块**，其余值为 Terraria 的 tile id：

| 值 | 含义 |
| --- | --- |
| `-1` | 空气（没有方块） |
| `0` | 泥土块 `TileID.Dirt`（是真实方块，不是空气） |
| `1` | 石块 |
| 其他 | 对应的 tile id |

**不要再把 `0` 当空气**：`0` 是泥土。判断空格用 `type === -1`（或 `tile.hasTile`）。
`scan`、`tile`、`mine`（`before`/`after`）、`place`（`after`）、`inventory`（物品另有规则）
都遵循这条约定。

## 推荐的操作循环

每次行动都按以下顺序执行：

1. 先调用 `terraria_observe`，确认 `paused`/`hasFocus`、位置、生命、死亡状态、手持物品和
   当前攻击状态。
2. 需要地形信息时调用 `terraria_scan`；需要精确方块信息时调用 `terraria_tile`。
3. 需要目标时调用 `terraria_npcs`，从返回的 `who` 选择 NPC。
4. 调用一个短时动作（移动 250～1000 ms、一次跳跃、一次攻击）。
5. 再调用 `terraria_observe` 或 `terraria_scan` 确认结果（移动/跳跃/攻击的返回值里已经带了
   `from`/`to`、`dY`、命中数，可以先用它判断，再用 observe 复核）。

移动建议每次 250～1000 ms。遇到坑、边缘、水或敌怪时，先停止并重新观察，不要连续发送很长的
移动命令。动作失控、切屏暂停或需要立即清除保持状态时调用 `terraria_stop`。

## 工具目录

### 状态和观察

#### `terraria_status({})`

检查桥接是否可达、Terraria 是否加载了世界。没有进入世界时也可以调用。
返回 `{pong,version,inWorld,tick,paused,hasFocus}`。

#### `terraria_observe({})`

读取当前状态。重点字段：

- `inWorld`：是否在世界中。
- `paused` / `hasFocus`：暂停与窗口焦点；`paused:true` 时不要发动作类命令。
- `world`：世界名、宽高、昼夜、时间、下雨、血月、日食、困难模式、tick。
- `player.tile`：玩家中心所在的 tile 坐标；`player.pos`、`player.velocity`：位置和速度。
- `player.life/lifeMax/mana/manaMax`、`player.dead`（死亡时不要继续发送移动、使用或攻击）。
- `player.selected`、`player.held`：当前槽位与手持物品（`type`/`name`/`stack`）。
- `player.itemAnimation`、`itemAnimationMax`、`itemTime`：使用/挥动的动画状态，
  可用来确认动作真的开始了。
- `player.grounded`：本 tick 与上一 tick 竖直速度都为 0 才为 true（比"速度为 0"更可靠）。
- `attack`：攻击任务是否仍在运行、目标和命中次数，以及**上一次攻击的结算**
  `attack.last = {target,name,hits,lifeBefore,lifeAfter,killed,endedTick}`。
  `attack.last` 在任务结束和 `stop` 之后仍然保留，可以用它事后核对伤害是否真的打出去了。

### 地形读取

#### `terraria_scan({x?, y?, radius?, walls?})`

扫描以 `x`、`y` 为中心的方形区域；省略 `x`、`y` 时用玩家位置，没有本地玩家时用出生点。

- `radius` 默认 12，限制 1～48；`walls` 默认 true。
- `walls:true` 时每格字段是 `[type, wall, liquid, shape]`（stride 4），
  `walls:false` 时是 `[type, liquid]`（stride 2）。
- 返回 `x0,y0,w,h,center,playerTile,format,legend`，`format:"rows"` 时另有 `stride`、`fields`、`tiles`。
- **`tiles` 是"行的数组"**：`tiles[j]` 是 `y = y0 + j` 这一行，`x = x0 + i` 的格子占
  `tiles[j][i*stride .. i*stride+stride-1]`。它不是"一个格子一个数字"的平铺数组。
- `shape`：`0` 实心、`1` 半砖、`2..5` 四种斜坡（`2` down-left、`3` down-right、`4` up-left、`5` up-right）。
  `liquid` 0～255；`wall` 0 表示没有墙。

解码示例（r=1，walls=true，w=h=3，stride=4）：

```json
{
  "x0": 100, "y0": 50, "w": 3, "h": 3, "stride": 4,
  "fields": ["type", "wall", "liquid", "shape"],
  "tiles": [
    [ -1, 0, 0, 0,   -1, 0, 0, 0,   -1, 0, 0, 0 ],
    [ -1, 0, 0, 0,    1, 0, 0, 0,   -1, 0, 0, 0 ],
    [  2, 0, 0, 0,    2, 0, 255, 0,  2, 0, 0, 0 ]
  ]
}
```

```js
// tiles[j].slice(i*stride, i*stride+stride) => [type, wall, liquid, shape]
// x = x0 + i, y = y0 + j
// tiles[1], i=1 → x=101, y=51, [1,0,0,0]  → 石块，无墙、无液体、实心
// tiles[2], i=1 → x=101, y=52, [2,0,255,0] → 泥块，整格水，实心
// tiles[0] 全是 -1 → 这一行三格都是空气
```

#### `terraria_tile({x, y})`

读取一个世界 tile。**`x`、`y` 必填**（缺参数会报错，不会默认成 0,0）。返回方块类型和名称、
墙、液体、坡度、半砖、执行器以及帧坐标；`hasTile:false` 时 `type` 为 `-1`。先用
`terraria_scan` 找到目标，再用它确认精确状态。

### 玩家动作与战斗

#### `terraria_move({direction, ms?})`

短暂保持一个方向键。

- `direction` 必填：`left`、`right`、`up`、`down`。新方向会清除相反方向的保持状态。
- `ms` 默认 500，限制 1～5000。
- 返回 `{dir,ms,from:[x,y],to:[x,y],dx,dy,moved,untilTick}`：`from`/`to` 是动作前后的玩家中心
  tile 坐标（保留 1 位小数），`moved` 表示是否真的位移了。**用 `moved` 判断这次移动有没有生效**，
  不要只看 `ok:true`。
- 桥侧还有 `wait` 参数（默认 `true` = 等动作结束再返回上面的结算，`false` = 只回 `untilTick`）；
  `terraria_move` 工具不传它，所以拿到的就是等完之后的结算结果。
- `up`/`down` 是动力飞行（见"已知限制"），不是爬坡/下蹲。
- 这是短时控制，不是路径规划；每次移动后重新观察。

#### `terraria_jump({ms?})`

一次跳跃冲量，只在站立时施加，不会在空中重复补冲量。

- `ms` 默认 150，限制 1～2000。
- 返回 `{ms,y0,y1,dY,grounded,untilTick}`；`dY` 为负表示上升，`grounded:false` 表示起跳前就不在地上。
- 桥侧 `wait` 默认 `true`（等跳跃窗口结束再返回），工具不传它。
- 跳跃不保证跨过任意宽度的坑；需要结合短距离移动和观察。

#### `terraria_use_item({slot?, right?, ms?})`

使用当前物品，也可以先选择背包槽位。

- `slot` 可选；有效槽位会被选中（先调用 `terraria_inventory` 确认槽位）。
- `right` 默认 false；true 是右键/物块交互。
- `ms` 默认 220，限制 1～2000。
- 返回 `{slot,right,ms,held,anim}`，`anim` 是请求之后立刻读到的 `itemAnimation`：
  `anim > 0` 才说明动画真的开始了。
- 使用后通过 `terraria_observe` 检查 `held`、`itemAnimation` 和位置变化。

#### `terraria_npcs({radius?})`

列出附近的活动 NPC。

- `radius` 工具默认 80 tile，限制 1～200（桥侧默认是 40，工具会显式传 80）。
- 每项包含 `who`、`type`、`name`、`tile`、`life`、`lifeMax`、`friendly`、`boss`、`hostile`。
- `hostile` 表示"会造成伤害且可以被攻击"——这正是 `terraria_attack` 自动选择的目标；
  `friendly:true` 的城镇 NPC 不算。
- `terraria_attack` 的 `who` 应来自这里的返回值。

#### `terraria_attack({who?, radius?, durationMs?, intervalMs?, slot?})`

启动持续攻击任务，工具立即返回，攻击在游戏更新循环中继续执行。

- `who` 可选；省略时自动选择半径内最近的可攻击敌怪（`terraria_npcs` 的 `hostile:true`）。
- `radius` 默认 40，限制 1～200 tile；`durationMs` 默认 3000（100～15000）；
  `intervalMs` 默认 180（30～2000）。
- `slot` 可选，用于先选择武器槽位；当前手持物品必须有伤害，否则报错。
- 命中需要视线（不能隔墙打），使用玩家真实伤害/暴击/攻速，不会超过武器自身的使用间隔。
- 桥侧 `wait` 默认 `false`，工具不传它，所以这个调用**立即返回**；要知道结果就用
  `terraria_observe` 轮询 `attack`（或事后看 `attack.last`）。
- 攻击过程中用 `terraria_observe` 看 `attack.active` 和 `attack.hits`；结束后看 `attack.last`
  核对 `lifeBefore`/`lifeAfter`/`killed`。需要立即停止时调用 `terraria_stop`
  （`stop` 会保留 `attack.last`）。

推荐流程：

```text
terraria_inventory({})
terraria_npcs({"radius":40})
terraria_attack({"who": NPC_WHO, "slot": WEAPON_SLOT, "durationMs":2500})
terraria_observe({})
```

#### `terraria_spawn_npc({type?, x?, y?})`

生成 NPC，仅用于测试或创造模式操作。

- `type` 默认 1（蓝史莱姆），必须是真实存在的 NPC 类型 ID（桥会校验，非法 ID 直接报错）。
- `x`、`y` 省略时生成在玩家右侧约 5 格。
- 这是管理员/创造操作，不要在普通探索中随意调用。

#### `terraria_stop({})`

清除插件发出的移动、跳跃、使用物品和攻击保持状态，返回 `{stopped:true}`。
不会清除 `attack.last`。遇到异常或需要切换动作时优先调用。

### 背包、世界编辑和传送

#### `terraria_inventory({})`

返回当前选中槽位，以及所有非空背包物品。每项包含 `slot`、`hotbar`、`type`、`name`、
`stack`、`prefix`、`rare`。

#### `terraria_mine({x, y, noItem?})`

移除指定 tile 的**方块**（v0.2.0 起默认不动背景墙）。

- `x`、`y` 必填，必须在世界范围内。
- `noItem` 默认 false；true 时抑制掉落。
- 返回 `{ok,x,y,removedTile,removedWall,before,after,wallBefore,wall}`，
  `before`/`after` 用 `-1` 表示空。默认 `walls:false`，墙只会被记录（`wallBefore`），不会被挖掉；
  需要连墙一起挖时，直接调桥传 `walls:true`（工具目前不透出这个参数）。
- 本来就是空的会返回 `ok:false`，这是正常拒绝，不代表桥失败。
- 调用前先用 `terraria_tile` 或 `terraria_scan` 确认坐标。

#### `terraria_place({x, y, type, style?})`

按 Terraria 的放置规则在指定位置放一个方块。

- `x`、`y`、`type` 必填；`type` 必须是合法 tile id（`1..TileID.Count-1`，桥会校验，越界直接报错）。
- `type` 是 Terraria tile 类型 ID，不是物品名称；`style` 默认 0。
- 工具固定使用 `forced:false`（走正常放置规则：需要支撑、不能和已有方块重叠）。
- 返回 `{ok,x,y,requested,after}`，`after` 是放置后该格实际类型（`-1` 为空）；`after` 与
  `requested` 不同就说明没放成功。
- 直接操作世界，不模拟鼠标，也不消耗背包物品；视为管理员/世界编辑操作。

#### `terraria_teleport({x, y})`

把本地玩家传送到 tile 坐标。

- `x`、`y` 必填，必须是**有限数值且在世界范围内**；桥会拒绝 `NaN`/无穷/越界坐标。
- 桥会把玩家从实心方块里向上推出（返回 `adjusted` 表示是否调整过），并清零速度；
  但仍可能落在岩浆、水、空中或敌怪旁边，落点不做安全判断。
- 返回 `{x,y,from,tile,adjusted}`。
- 传送后立即调用 `terraria_observe`，必要时调用 `terraria_scan`。

#### `terraria_give_item({type, stack?})`

直接把物品生成到本地玩家背包中，是创造/管理员操作。

- `type` 必填，必须是真实存在的 item 类型 ID（桥会校验）。
- `stack` 默认 1，限制 1～9999。
- 返回 `{type,stack,name,added,dropped}`：`added` 是真正进了背包的数量，`dropped` 是背包放不下
  而掉在地上的数量；两者都可能为 0，物品名以返回值为准。

#### `terraria_craft({item, times?})`

按物品类型 ID 合成物品。

- `item` 必填，是产出物品的类型 ID；`times` 默认 1，桥侧限制 1～20（工具参数允许更大，但会被
  桥截断到 20，返回值里的 `times` 是桥实际执行的次数）。建议按 20 或更小的批次调用。
- 支持配方组（"任意木材/任意矿石"）和配方条件（附近有水/墓地/夜晚等），也检查工作站；
  条件不满足会明确报错，不会凭空合成。
- 返回 `{item,name,crafted,times,perCraft,reason?,dropped?}`：`crafted` 只统计真正进了背包的数量，
  掉在地上的数量在 `dropped` 里。材料不足或缺少工作站时会返回部分成功（`crafted` + `reason`）。

### 聊天

#### `terraria_chat({text})`

在本地 Terraria 客户端的聊天/提示区域显示文字。

- `text` 必填；插件会先截断到 400 字符，桥再回报 `{sent,length,truncated}`。
- 空白消息会报错。
- 这是本地客户端提示（`Main.NewText`），不是服务器广播接口。

## 错误处理

`{"ok":false,"error":"..."}` 是**正常的游戏侧拒绝**：命令没有生效，错误信息说明原因
（越界、不在世界、材料不足、没视线、暂停中……）。**它不代表桥坏了，也不要立刻重连或重试**；
先读错误信息，再按下面处理：

| 错误信息（片段） | 处理 |
| --- | --- |
| `not in a world` | 等用户进入世界，再 `terraria_observe` |
| `no local player` | 当前实例没有本地玩家，玩家动作只能在单人客户端世界执行 |
| `the game is paused` | 世界没有推进：请用户切回游戏窗口，再发动作 |
| `out of world bounds` | 重新 `terraria_observe` 取世界尺寸，修正坐标 |
| `x,y must be finite` | 坐标是 NaN/无穷；改传真实数字 |
| `invalid tile type` / item / NPC id | 类型 ID 不存在；先用 `terraria_inventory` / 文档确认合法 ID |
| 玩家已死亡 | 先停止动作并等用户复活 |
| 没有可攻击的敌怪 | 扩大 `terraria_npcs` 范围，确认 `hostile:true` |
| 当前选中的物品不能造成伤害 | `terraria_inventory` 找到武器槽位后传 `slot` |

另外两类情况要区分：

- **连接层失败**（`Terraria bridge is not running`、超时等）：桥或游戏没起来，
  按提示请用户启动/确认 Mod 已启用。
- **`main thread did not process the request within 1500 ms`**：主线程超过 1.5 s 没有执行这条请求。
  消息会说明请求是否已经开始执行。有副作用的命令（`mine`/`place`/`give`/`craft`/`spawn_npc`/`teleport`）
  **不是幂等的**，看到这条错误先调用 `state`/`inventory`/`tile` 核对实际后果，再决定要不要重试。

## 已知限制

- **移动是桥直接改速度**：`terraria_move` 每帧把水平速度推向目标方向，所以没有击退、冲刺
  （克苏鲁之盾等）、坐骑速度、传送带、冰面惯性这些原版手感；碰撞和重力仍由游戏解算，
  但"被击退"这类效果会被覆盖。
- **`up`/`down` 是动力飞行**：按住 `up` 会持续上升（不受翅膀/火箭消耗限制），`down` 会强制下沉。
  它们不是爬坡或下蹲，别用它们模拟原版跳跃路线。
- **`attack` 是桥驱动的命中**：命中判定由桥计算（视线、距离、玩家伤害/暴击/攻速），
  不会播放真实的武器挥击；近战/远程/魔法的弹药与魔力消耗不会发生。用它验证战斗流程没问题，
  不要把它当作"真实操作"的证据。
- **只覆盖单人客户端**：世界修改、背包和传送都在本地执行；多人客户端会与服务器不同步，
  专用服务器实例没有本地玩家。

### 扫描的 token 成本

`terraria_scan` 的输出是**紧凑 JSON**（0.2.0 起不再两空格缩进，体积约为旧版的 1/4）。
`format` 由工具自己决定：`radius ≤ 24` 用 `rows`（精确），`radius > 24` 自动改用 `ascii`；
也可以自己传 `format:"ascii"|"rle"|"rows"` 覆盖。

| 请求 | 大致体积 |
| --- | --- |
| `radius:12`（默认，rows） | ≈ 5 KB |
| `radius:48`（rows，精确到方块 ID） | ≈ 82 KB |
| `radius:48`（ascii，自动） | ≈ 10 KB |

所以：**局部观察用 `radius` 8～12**；要看大范围地形就用 `ascii`（自动），需要精确 ID 再回头
用 `terraria_tile` 抽查那一格。

## 安装与构建

### 安装游戏侧 Mod

先**完全关闭 tModLoader 和 Terraria**（游戏运行时锁住 `.tmod`，复制会失败）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-mod.ps1
```

脚本会校验 `.tmod` 魔数、备份当前已安装版本到
`artifacts\DshTerrariaBridge.previous-installed.tmod`，然后复制到
`文档/My Games/Terraria/tModLoader/Mods/` 并打印 SHA-256。
之后在 tModLoader 的模组管理器启用 **DSH Terraria Bridge**，进入单人世界后桥才开始监听。

### 安装 DSH 插件

用 DSH 自己的插件安装器，传入本仓库目录的**绝对路径**：

```powershell
dsh plugin --profile <profile> add "<本仓库的绝对路径>"
```

`--profile` 换成实际使用的 profile（桌面端默认那个）。安装后重启 DSH。
`cordis.patch.yml` 会把插件挂载到 profile，并带上默认配置
（`host`/`port`/`timeoutMs`，见该文件）；要改端口就改那里的 `config` 再重启。

要移除插件：

```powershell
dsh plugin --profile <profile> remove dsh-terraria-agent
```

### 环境要求

- Node.js **20 或更新**（`npm test` 用 `node --test`，不需要新版参数）。
- 只有构建 C# Mod 才需要 .NET 8 SDK 和 tModLoader 安装（见 `README.zh-CN.md`）。
- `install-mod.ps1` / `build-bridge-roslyn.ps1` 用 Windows PowerShell 5.1 即可运行；
  `build-bridge-roslyn.ps1` 例外，它要求 PowerShell 7+（.NET 8），否则会直接给出可用替代方案。

### 进世界后的第一组调用

```text
terraria_status({})
terraria_observe({})
```