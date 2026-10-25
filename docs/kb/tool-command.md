# 工具与指令规范库（tool_command）

> 只在"该用哪个工具 / 参数怎么填 / 坐标怎么算 / 这个报错啥意思"时加载。
> 完整清单由 `node scripts/kb-sync.mjs` 从代码生成到
> [_generated-commands.md](_generated-commands.md)（不要手改）；本文件只写**用法与坑**。

## 1. 三条通路（先搞清楚自己在用哪条）

| 通路 | 入口 | 用途 | 限制 |
|---|---|---|---|
| **mod 桥**（唯一允许操作角色的通路） | TCP `127.0.0.1:47777`，JSON 行协议 `{"cmd":..,"params":{..}}` | 所有游戏内动作 | 客户端 `netMode=1`，世界变更走服务端同步 |
| 插件工具（DSH 侧） | `terraria_*` 26 个 | 把常用桥命令包成工具、带轮询验证 | 只是桥的薄封装，不会绕过桥 |
| 截图 | `tr_observe`（printwindow 抓窗口像素） | **只读**，看画面 | 不控制；报告里的"缩放/偏移"不可信 |

**黑名单（永久禁止，见 safety-limit.md）**
`SetCursorPos`/`mouse_event`/`keybd_event`/`SendInput`/`SetForegroundWindow`/`AttachThreadInput`；
`WorldGen.PlaceTile|KillTile|PlaceObject|RangeFrame`；`Player.PickTile`；`Main.tile[x,y].type=`；
以及已删除的 `give`/`teleport`/`spawn_npc`/admin 直写分支。

## 2. 坐标体系

| 体系 | 单位 | 来源 | 备注 |
|---|---|---|---|
| 方块坐标 (tile) | 16px/格 | 桥的 `x/y`、`state.player.tile` | **绝大多数命令用这个** |
| 像素坐标（世界） | px | `state.player.position` | 只做换算用；`tile = floor(pos/16)` |
| 屏幕坐标（客户区） | px | `tr_observe` 的 `region` | 仅截图裁切用，**不要**拿去点鼠标 |
| 中心点约定 | — | `state.player.tile` 是**中心点** | 碰撞盒 1.25 格 → 下挖算两列 |

换算常用式（已验证）：
```
两列下挖:  colL = floor(px - 0.55), colR = floor(px + 0.55)
站立时身体: 站在地板 y=F → 占 F-3..F-1
玩家中心 y: 地板格 - 1.3
```

## 3. 命令分组与典型用法

### 读（无副作用，随便用）
- `state` — 玩家/世界/buff/手持/`zones`（腐化/丛林/雪原…）
- `scan {x,y,r}` — 半径 ≤48 的方块网格，返回 `{x0,y0,w,h,stride,fields,tiles[][]}`
  （`tiles[j]` 是第 j 行；`-1` 表示空，其余是方块 ID）
- `tile {x,y}` — 单格：`type/name/wall/liquid/shape/hasTile`
- `find {type,x1,x2,y1,y2,max}` — 找方块（如 `type:25` 钨、`type:203` 铁矿、`type:26` 祭坛）
- `inventory` / `items` / `recipes {q}` / `tiledata {type}` / `api {type,member}`
- `npc {r}` / `players` / `housing {x,y}` / `needs {x,y}` / `boss` / `event`
- `chatpoll` — 读游戏聊天里玩家说的话（游戏内下指令的入口）

### 动角色（控制角色，允许）
- `move {dir,ms}` — 写 `controlLeft/Right`，由物理推动；**单步 ≤1500ms**
- `jump {ms}` / `climb {up|down,ms}`（绳子限定）/ `hook` / `unhook` / `fly` / `mount`
- `walkto {x,y}` / `path {x,y}` — A*，**空腔竖井会死锁**（见 game-rule §7）
- `travel {dir,distance}` — 横向赶路专用；**不要用它下挖**
- `guard {on|off}` — 自动防守（默认开，只打视线内 8 格）

### 改世界（必须走真实物品流）
- `mine {x,y,ms}` — 挥镐；`ms` ≥1300
- `place {x,y,type,ms}` — 掏方块放下；`type` 是**方块 ID**
- `wall {x,y,slot}` — 贴墙；`craft {item}` — 用附近工作台合成；`removeblock`
- `grab` — 把脚边掉落吸进包（背包满会失败）
- `fill` — 批量铺地（内部逐格走 place，仍是真实物品流）

### NPC / 商店
- `talk {npc}` — 距离 <11；`shop {npc}` → `shops` → `buy {shopId,slot}` / `sell`
- `homeroom {who,x,y}` — 原版 `moveRoom`，房间不合法会被拒（权威判定）

### 战斗
- `attack {x,y,r,ms}` — 走真实伤害管线（视线检查）；`r` 12~22，别用 8
- `select {item|slot}` / `shoot {x,y}` / `minion`

## 4. 参数填写规范

| 参数 | 规则 | 反例 |
|---|---|---|
| `ms` | 挖掘 1300、放置 1200~1500；走路 200~1500 | 挖矿给 900 → 静默失败 |
| `slot` | 0–9 快捷栏，10+ 主背包；用 `inventory` 实读 | 猜槽位 |
| `type`（place/fill） | **方块 ID**（石=1、土=0、木=30） | 传物品 ID（石=3） |
| `type`（find） | 方块 ID | — |
| `item`（craft/equip） | **物品 ID**；`craft` 收产物物品 ID | 传配方 ID |
| `r` | `attack` 是硬半径；`scan` ≤48 | `attack r=8` 打 8.4 格外 → 拒绝 |
| `q` | `recipes` 的搜索词（不是 `filter`） | — |
| `member` | `api` 用 `{type,member}`，不是 `{q}` | — |

## 5. 常见报错 → 含义

| 报错原文 | 含义 | 处置 |
|---|---|---|
| `够不到：离这一格超过 7 格，先走过去` | 瞄准超距 | 走近或用 `walkto` |
| `背包里没有能放成方块 X 的物品` | `type` 传错（物品 ID 当方块 ID） | 用 `tiledata` 查正确方块 ID |
| `(x,y) 不是一个房间，没法让人搬进去` | 房屋不合法 | 见 build-plan §5，**别猜，先修 roomcheck** |
| `placed:false`（响应里） | 原版拒绝放置：与自身碰撞盒重叠 / 无支撑 / 距离 | 跳起放、换站位、走近 |
| `bridge not running` | mod 桥没起来 | 跑 `cycle.ps1` 重编重启（不要手动开游戏） |
| `homeroom 需要 who（NPC 索引…）` | 参数名是 `who` 不是 `name` | 先 `housing`/`npc` 取 `who` |
| `walk 超时` / 位置 10 秒不变 | A* 在空腔死锁 | 改逐格 `mine` 下挖 |

## 6. 部署链路（改代码后必须走）

```
D:\...\dsh-terraria-agent-repo   ← 唯一真相源（git）
        │  cycle.ps1 会同步到 ↓
C:\Users\<user>\Documents\My Games\Terraria\tModLoader\ModSources\DshTerrariaBridge
        │  编译 + 打包 .tmod
        └→ 启动 tModLoader（副屏 DISPLAY2，窗口 -2560,-174,1707,1067）
```

确认真的部署了：`state` 回的状态字段里有新加的键；否则就是还在跑旧包。