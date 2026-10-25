# 工具与指令规范库（tool_command）

> 什么时候加载：要下具体命令（用哪个工具 / 参数怎么填 / 坐标怎么算 / 这个报错什么意思）。
> **权威清单**是 [_generated-commands.md](_generated-commands.md)（68 个 mod 命令 + 25 个插件工具，由代码生成，不要手改）。
> 本文件只写：怎么选、怎么填、会怎么错、什么不能做。

---

## 1. 通路（先搞清楚自己在用哪条）

| 通路 | 入口 | 用途 | 限制 |
|---|---|---|---|
| **mod 桥** | TCP `127.0.0.1:47777`，一行一个 JSON：`{"cmd":"mine","params":{...}}` | 所有游戏内动作的**唯一**通路 | 客户端 `netMode=1`，世界变更服务端同步，回读会滞后 1~2 帧 |
| 插件工具 | `terraria_*`（25 个） | 把常用桥命令包成工具，带**轮询验证** | 只是桥的薄封装，不会绕过桥 |
| `terraria_bridge` | `{cmd, params}` | 没有专门工具的桥命令走它 | 见 §6 黑名单 |
| 截图 | `tr_observe`（printwindow 抓像素） | **只读**看画面 | 不控制；它报的"缩放/偏移"不可信，别拿坐标去点鼠标 |

---

## 2. 命令分组（按"我要干什么"找）

### 2.1 读（无副作用，随便用）

| 命令 | 参数 | 返回要点 |
|---|---|---|
| `ping` / `state` | — | 玩家/世界/buff/手持/`zones`/攻击统计/`walk`/`follow` 状态 |
| `scan` | `x,y,r(≤48),walls,format` | `{x0,y0,w,h,stride:4,fields:["type","wall","liquid","shape"],tiles:[][]}`，`tiles` 是**行数组**（`tiles[j]` = 第 j 行），`-1`=空 |
| `tile` | `x,y` | 单格：`hasTile/type/name/wall/liquid/slope/halfBlock/actuator/solid` |
| `find` | `type,x1,x2,y1,y2,max` | 找方块；返回 `{total,found[]}` |
| `inventory` | — | 背包/装备/选中槽；`main_slots_used` |
| `items` | `r` | 地上的掉落物 |
| `recipes` | `q,limit` | 配方搜索，参数名是 **`q`** |
| `tiledata` | `type,style,alternate` | 家具尺寸与**锚点** |
| `api` | `type,member` | 反射查 API 签名/字段（`member` 是子串） |
| `npc` / `players` | `r` / — | NPC 列表 / 多人玩家列表 |
| `housing` / `needs` | `x,y` | 城镇 NPC 的家；房间缺什么（依赖原版 counter，见 game-rule §3.2，不可靠） |
| `housediag` | `x,y` | **房屋判定权威**：自算洪泛 + 逐条规则，返回 `missing[]` |
| `boss` / `event` | — | 可召唤的 Boss / 可用事件 |
| `shops` | `want` | 已解锁商店列表 |
| `chatpoll` | `since,max` | 读游戏聊天里玩家打的字（游戏内下指令的入口） |

### 2.2 动角色（控制角色，允许）

| 命令 | 参数 | 说明 |
|---|---|---|
| `move` | `dir(left/right/up/down),ms` | 写 `controlLeft/Right`，由物理推动；单步 ≤1500ms |
| `left/right/up/down` / `jump` | `ms` / `ms` | 简写版移动/跳 |
| `climb` | `dir(up/down),ms` | **绳子限定**：绳子 tile 213，x 要对齐 `col+0.5` |
| `hook` / `unhook` | `x,y,slot,ms` / — | 抓钩（包里要有抓钩类物品） |
| `fly` / `mount` | `ms` / `action,slot` | 翅膀飞行 / 坐骑 |
| `walkto` | `x,y,tol,ms` | 逐帧驱动走过去（遇墙挖、遇坑跳）；非阻塞 |
| `path` | `x,y,ms,dig` | A* 自动走；**空腔竖井会死锁**，别指望它爬坑 |
| `travel` | `dir,distance,ms,dig,stop` | 横向赶路专用；**不要用它下挖**（沿斜坡横向漂移） |
| `walkto`/`travel` 的收尾 | `stop` | 立刻停所有移动/攻击/使用 |
| `idle` | — | 站住：取消 follow/path/walk/攻击，但不清世界 |
| `follow` | `off,player,guard,range` | 跟随某个玩家（多人时用） |
| `guard` | `off,ms,r` | 自动防守（默认开，只打视线内） |

### 2.3 改世界（必须走真实物品流）

| 命令 | 参数 | 规则 |
|---|---|---|
| `mine` | `x,y,ms` | 挥镐（自动选镐/斧）；`ms≥1300` |
| `place` | `x,y,type,ms` | 掏方块放下；`type` 是**方块 ID** |
| `place_object`（工具）/ `place` 多格 | 同上 | 家具瞄**锚点格**（`tiledata` 查），不是左上角 |
| `wall` | `x,y,type,remove,ms` | 贴/拆背墙 |
| `removeblock` | `x,y,ms` | 锤子拆（比挖更合适拆家具/墙） |
| `fill` | `x1,y1,x2,y2,type` | 批量铺地：内部逐格走真实放置，从背包扣料 |
| `craft` | `item,times` | `item` 是**产物物品 ID**；需在工位 ~1 格内 |
| `drop` / `grab` | `slot,count` / `r` | 丢垃圾 / 吸脚边掉落（背包满会失败） |
| `chest` | `action,slot,count,invSlot` | 开箱存取（站箱旁 ≤6 格） |
| `mirror` | — | 魔镜回出生点（深坑回地表最可靠的办法） |
| `save` | — | **目前坏**（NRE），别依赖 |

### 2.4 NPC / 商店 / 战斗 / 增益

| 命令 | 参数 | 说明 |
|---|---|---|
| `talk` | `npc` | <11 格，打开对话 |
| `shop` → `shops` → `buy` / `sell` | `npc` / `want` / `shopId,slot` / `slot` | <12 格；必须真的走过去 |
| `homeroom` | `x,y,who` | 原版 `moveRoom`；**因引擎判定坏，当前搬不动人**（见 game-rule §3.2） |
| `attack` | `who,r,ms,interval,slot` | 走真实伤害管线 + 视线检查；`r` 12~22 |
| `select` / `shoot` | `slot,item` / `x,y,slot,ms` | 换手持 / 射箭开火 |
| `minion` | `slot` | 召唤/收回召唤物 |
| `equip` / `accessory` | `slot` / `off,slot,from` | 穿装备 / 摘换饰品 |
| `heal` / `use` | — / `slot,right,ms,x,y` | 喝药 / 用物品 |
| `research` / `duplicate` | `slot` / `item,count` | Journey 研究 / 无限取（只在存档已研究过时可用） |
| `chatpost` | `text` | 把一句话写进游戏聊天框（我回复玩家的出口） |

---

## 3. 坐标体系

| 体系 | 单位 | 来源 | 备注 |
|---|---|---|---|
| **方块坐标（tile）** | 16px/格 | 桥的 `x/y`、`state.player.tile` | 绝大多数命令用这个 |
| 世界像素 | px | `state.player.position` | 只做换算：`tile = floor(px/16)` |
| 屏幕/客户区像素 | px | `tr_observe` 的 `region` | 仅截图裁切；**不要**拿去点鼠标 |
| 中心点约定 | — | `state.player.tile` 是**中心点** | 碰撞盒 1.25 格宽 → 下挖要挖两列 |

```
两列下挖:   colL = floor(px - 0.55), colR = floor(px + 0.55)
站立身体:   站在地板 y=F → 占 F-3 .. F-1
玩家中心 y: 地板格 - 1.3
扫描半径:   scan.r ≤ 48；超出立即报错
家具锚点:   tiledata {type} → originX/originY（熔炉 3×2 是 (1,1)）
```

---

## 4. 参数填写规范

| 参数 | 规则 | 反例（会安静地失败） |
|---|---|---|
| `ms` | 挖 1300、放 1200~1500、走 200~1500 | 挖矿给 900 → 返回成功但方块不动 |
| `slot` | 0–9 快捷栏、10+ 主背包；先 `inventory` 实读 | 猜槽位 |
| `type`（place/fill/find/wall） | **方块/墙 ID** | 传物品 ID（石：物品 3 ≠ 方块 1） |
| `item`（craft/equip/duplicate） | **物品 ID** | 传配方 ID |
| `q`（recipes） | 搜索词 | 写 `filter` |
| `member`（api） | 类型里的成员子串 | 写 `q` |
| `r`（attack） | 12~22（硬半径） | `r=8` 打 8.4 格外的怪会被拒 |
| `who`（homeroom/attack/equip） | NPC 索引，从 `npc`/`housing` 取 | 传名字 |
| `dir`（move/climb/travel） | `left/right/up/down` | 传中文/缩写 |

---

## 5. 错误说明（原文 → 含义 → 处置）

| 报错原文 | 含义 | 处置 |
|---|---|---|
| `bridge not running` | mod 桥没起来（游戏没开/没进世界/mod 没部署） | 用 `cycle.ps1` 重编重启，别手动开 |
| `够不到：离这一格超过 7 格，先走过去` | 瞄准超距 | 走进 ≤7 格，或 `walkto` |
| `背包里没有能放成方块 X 的物品` | `type` 传错（拿了物品 ID）或真没这个材料 | `tiledata` 查方块 ID；`inventory` 查材料 |
| `背包里没有能放成墙 X 的物品` | 墙同理 | 先 `craft` 木墙/土墙 |
| 响应里 `placed:false` | 原版拒绝：与自身碰撞盒重叠 / 无支撑 / 距离 | 跳起放、换站位、先补支撑 |
| 响应里 `armed:true` 但 `placed:false` | **只是"挥了一下"**，不代表放上了 | 必须回读 `tile` |
| `(x,y) 不是一个房间，没法让人搬进去` | 引擎判定拒绝（本环境这个判定本身坏，见 game-rule §3.2） | 用 `housediag` 自己判 |
| `moved:false, reason:"None"` | `moveRoom` 静默 no-op | 同上，别重试，先看 `housediag` |
| `walk 超时` / 位置 10 秒不变 | A* 在空腔/竖井死锁 | 改逐格 `mine` + `climb` |
| `steps-exhausted` | 下挖/搭台在一格上反复失败 | 换起点、先补脚下、检查是否被埋 |
| `craft` 返回 `crafted:0` | 离工位太远或材料不足 | 走近工位（~1 格）再看 `need` |
| `idle`/`stop` 之后角色还在动 | 还有别的 hold 在跑（travel/guard） | 再发一次 `stop`，并 `state` 看 `walk/follow` |

---

## 6. 权限黑名单（永久禁止，见 safety-limit.md）

**禁止直接改世界的 API**
`WorldGen.PlaceTile` / `PlaceObject` / `KillTile` / `KillWall` / `RangeFrame`；
`Player.PickTile`（已被删除，不再是可选项）；
`Main.tile[x,y].type = ...`；
任何 `admin` 直写分支（已删）。

**禁止直接控制用户的电脑**
`SetCursorPos` / `mouse_event` / `keybd_event` / `SendInput` / `SetForegroundWindow` /
`AttachThreadInput`；插件侧 `tr_key` / `tr_walk` / `tr_mouse` 已全部删除（它们要么只到游戏的菜单层，要么抢焦点）。

**禁止的作弊命令（已从代码删除，不要再加回来）**
`give`（凭空给物品）、`spawn_npc`（凭空生成 NPC）、`teleport`（瞬移）。

**允许的"后台"含义**
用 mod 桥**在游戏内部**驱动角色（写 control 标志 / 走原版 ItemCheck / 走原版 moveRoom），
不是点用户桌面，也不是直接改世界数组。三者区别见 `AGENTS.md` §0。

---

## 7. 部署链路（改代码后必须走）

```
D:\...\dsh-terraria-agent-repo          ← 唯一真相源（git）
        │  C:\Windows\Temp\trtest\cycle.ps1 会：同步 → 编译 → 打包 .tmod
        ↓
…\tModLoader\ModSources\DshTerrariaBridge
        │  编译失败会 exit 1 并**不启动游戏**
        └→ 启动 tModLoader（副屏 DISPLAY2，窗口 -2560,-174,1707,1067）→ 自动进多人存档
```

验证真的部署了：新命令能调用（例如 `housediag`），或 `state` 出现新字段。
**改仓库 ≠ 已生效**：不同步 + 不重编，游戏仍在跑旧包。