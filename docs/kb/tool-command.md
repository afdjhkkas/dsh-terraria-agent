# 工具与指令规范库（tool_command）

> 什么时候加载：要下具体命令（用哪个工具 / 参数怎么填 / 坐标怎么算 / 这个报错什么意思）。
> **权威清单**是 [_generated-commands.md](_generated-commands.md)（由 `Commands.cs` 与 `index.js` 生成，不要手改）；
> 本文件只写：怎么选、怎么填、会怎么错、什么不能做。

---

## 1. 通路（先搞清楚自己在用哪条）

| 通路 | 入口 | 用途 | 限制 |
|---|---|---|---|
| **mod 桥** | TCP `127.0.0.1:47777`，一行一个 JSON `{"cmd":"mine","params":{...}}` | 所有游戏内动作的**唯一**通路 | 客户端 `netMode=1`；世界变更靠服务端回广播，回读滞后 1~2 帧 |
| 插件工具 | `terraria_*`（25 个） | 常用桥命令的封装，带**轮询验证** | 只是薄封装，不绕过桥 |
| `terraria_bridge` | `{cmd, params}` | 没有专门工具的桥命令都走它 | 见 §6 黑名单 |
| **游戏聊天通道** | `chatpoll {since,max}` 读玩家在游戏里打的字；`chatpost {text}` 把我的话写回游戏聊天 | 玩家在游戏里下指令 / 我回话 | 单向轮询；玩家逐字发会收到半句话，要合并 |
| 截图 | `tr_observe`（printwindow 抓窗口像素） | **只读**看画面 | 不控制；它报的"缩放/偏移"不可信，别拿坐标点鼠标 |

`ping` 只要 mod 加载完就可用；`state`/`scan`/`tile` 必须进了世界。

---

## 2. 命令分组（按"我要干什么"找）

### 2.1 读（无副作用）

| 命令 | 参数 | 返回要点 |
|---|---|---|
| `ping` / `state` | — | `{inWorld,tick,paused,hasFocus,world,player}`；`player` 含 `tile/pos/velocity/life/mana/wet/grounded/buffs/selected/held/zones`、`attack` 统计、`walk`/`follow` 状态 |
| `scan` | `x,y,r(≤48),walls,format` | `{x0,y0,w,h,stride:4,fields:["type","wall","liquid","shape"],tiles:[][]}`；`tiles[j]` 是**第 j 行**，`-1`=空 |
| `tile` | `x,y` | 单格：`hasTile/type/name/wall/liquid/slope/halfBlock/actuator/solid/solidTop/frameX/frameY` |
| `find` | `type,x1,x2,y1,y2,max` | 找方块 → `{total,found:[{x,y}]}`；**只在已加载区有效** |
| `tiledata` | `type,style,alternate` | 家具尺寸与**锚点** `{width,height,originX,originY}` |
| `inventory` | — | 背包/装备/选中槽、`main_slots_used`（上限 50） |
| `items` | `r` | 地上掉落物（`Main.item[]`） |
| `recipes` | `q,limit` | 配方搜索，参数名是 **`q`** |
| `api` | `type,member` | 反射查类型成员签名（`member` 是子串，例如 `api{type:"TileID",member:"Iron"}`） |
| `npc` / `players` | `r` / — | NPC 列表（含 `life/hostile`）/ 多人玩家 |
| `housing` / `needs` | `x,y` | 城镇 NPC 的家 / 房间缺什么（**依赖引擎计数器，本档不可信**） |
| `housediag` | `x,y` | **房屋判定权威**：自算洪泛 + 逐条规则 → `{tiles,enclosed,walled,floor,ceil,left,right,light,comfort,flat,missing,valid}` |
| `boss` / `event` | — | 可召唤 Boss / 可用事件 |
| `shops` / `shop` | `want` / `npc` | 商店列表 / 打开某个 NPC 的商店（`shopId`） |
| `chatpoll` | `since,max` | 读游戏聊天（玩家指令入口） |

### 2.2 动角色（控制角色，允许）

| 命令 | 参数 | 说明 |
|---|---|---|
| `move` | `dir(left/right/up/down),ms` | 写 `controlLeft/Right`，由物理推动；单步 ≤1500ms |
| `left/right/up/down`、`jump` | `ms` | 简写 |
| `walkto` | `x,y,tol,ms` | 逐帧驱动走过去（遇墙挖、遇坑跳）；非阻塞 |
| `path` | `x,y,ms,dig` | A* 自动走；**空腔竖井会死锁**（原地弹 60 秒） |
| `travel` | `dir,distance,ms,dig,stop` | 横向赶路专用；**不许用它下挖**（沿斜坡横漂，实测漂 300 格） |
| `climb` | `dir(up/down),ms` | **绳子限定**：tile 213，x 必须对齐 `col+0.5` |
| `hook` / `unhook` | `x,y,slot,ms` / — | 抓钩（包里有钩类物品） |
| `fly` / `mount` | `ms` / `action,slot` | 翅膀飞行 / 坐骑（都要先有物品） |
| `stop` / `idle` | — / — | 立刻停所有移动/攻击/使用；`idle` 会取消 follow/path，不清世界 |
| `follow` | `off,player,guard,range` | 跟随某个玩家（多人同玩） |
| `guard` | `off,ms,r` | 自动防守（**默认开**，只打视线内） |

### 2.3 改世界（必须走真实物品流）

| 命令 | 参数 | 规则 |
|---|---|---|
| `mine` | `x,y,ms` | 挥镐/斧（自动选工具）；`ms≥1300`；挖完手持会留在镐上 |
| `place` | `x,y,type,ms` | 掏方块放；`type` 是**方块 ID**；多格家具瞄锚点 |
| `wall` | `x,y,type,remove,ms` | 贴/拆背墙（`type` 是墙 ID） |
| `removeblock` | `x,y,ms` | 锤子拆（家具/墙比镐合适） |
| `fill` | `x1,y1,x2,y2,type` | 批量铺地：内部逐格真实放置、真扣料；**单次 ≤30 格**，且要自下而上铺（先铺有支撑的一行） |
| `craft` | `item,times` | `item` 是**产物物品 ID**；需在工位 ~1 格内；`times` 上限 20 |
| `drop` / `grab` | `slot,count` / `r` | 丢垃圾 / 吸脚边（≤4 格；背包满必失败） |
| `chest` | `action,slot,count,invSlot` | 开箱存取（站箱 ≤6 格） |
| `mirror` | — | 魔镜回出生点（从深坑回地表最可靠） |
| `save` | — | **坏**（NRE），别依赖 |

### 2.4 NPC / 商店 / 战斗 / 增益

| 命令 | 参数 | 说明 |
|---|---|---|
| `talk` | `npc` | <11 格，打开对话（真实走近，不许凭空开 UI） |
| `buy` / `sell` | `shopId,slot` / `slot` | <12 格；`buy` 走 `Player.BuyItem` 真扣钱（实测买火把 1138→1088） |
| `homeroom` | `x,y,who` | 原版 `moveRoom`；**本档判定坏，搬不动人** |
| `attack` | `who,r,ms,interval,slot` | 真实伤害管线 + 视线检查；`r` 用 12~22 |
| `select` / `shoot` | `slot,item` / `x,y,slot,ms` | 换手持 / 射箭 |
| `minion` | `slot` | 召唤/收回召唤物 |
| `equip` / `accessory` | `slot` / `off,slot,from` | 穿装备 / 换饰品 |
| `heal` / `use` | — / `slot,right,ms,x,y` | 喝药 / 用物品 |
| `research` / `duplicate` | `slot` / `item,count` | Journey 研究 / 无限取 |
| `chatpost` | `text` | 把一句话写进游戏聊天框 |

---

## 3. 坐标体系

| 体系 | 单位 | 来源 | 备注 |
|---|---|---|---|
| **方块坐标（tile）** | 16px/格 | 桥的 `x/y`、`state.player.tile` | 绝大多数命令用这个 |
| 世界像素 | px | `state.player.pos/position` | `tile = floor(px/16)` |
| 屏幕/客户区像素 | px | `tr_observe` 的 `region` | 仅截图裁切，**不要**拿去点鼠标 |
| 中心点约定 | — | `state.player.tile` 是**中心点** | 碰撞盒 20×42px（≈1.25×2.6 格）→ 下挖要两列 |

```
两列下挖:   colL = floor(centreX - 0.55), colR = floor(centreX + 0.55)
站立身体:   脚在地板 y=F → 占 F-3 .. F-1（头顶那格最容易漏）
玩家中心 y: 地板格 - 1.3
隧道宽度:   3 格（身体 3 格高）；只挖 dy=-2..0，禁止挖 dy=+1
家具锚点:   tiledata{type} → originX/originY（熔炉 3×2 = (1,1)，铁砧 2×1 = (0,0)）
```

---

## 4. 参数填写规范

| 参数 | 规则 | 反例（会安静地失败） |
|---|---|---|
| `ms` | 挖 1300、放 1200~1500、走 200~1500 | 挖矿给 900 → 返回成功、方块不动 |
| `slot` | 0–9 快捷栏、10+ 主背包；先 `inventory` 实读 | 猜槽位 |
| `type`（place/fill/find/wall） | **方块/墙 ID** | 传物品 ID（石：物品 3 ≠ 方块 1） |
| `item`（craft/equip/duplicate） | **物品 ID** | 传配方 ID |
| `q`（recipes） | 搜索词 | 写 `filter` |
| `member`（api） | 类型成员子串 | 写 `q` |
| `r`（attack） | 12~22 | `r=8` 打 8.4 格外的怪会被拒 |
| `r`（scan/grab/items） | `scan` ≤48；`grab` ≤4 有效 | 拿 `grab r=12` 当"远处拾取" |
| `who`（homeroom/attack/equip） | NPC 索引（`npc`/`housing` 里取） | 传名字 |
| `dir` | `left/right/up/down` | 传中文/缩写 |
| `max`（find） | 上限条数，别省 | 省了只拿到一条，误判"只有一块" |

---

## 5. 错误说明（原文 → 含义 → 处置）

| 报错原文 | 含义 | 处置 |
|---|---|---|
| `bridge not running` | mod 桥没起来（游戏没开 / 没进世界 / mod 没部署） | 用 `cycle.ps1` 重编重启，别手动开 |
| `够不到：离这一格超过 7 格，先走过去` | 瞄准超距 | 走进 ≤7 格（实用 ≤4） |
| `背包里没有能放成方块 X 的物品` | `type` 传错（拿了物品 ID）或真没材料 | `tiledata` 查方块 ID；`inventory` 查材料 |
| `背包里没有能放成墙 X 的物品` | 同上 | 先 `craft` 墙 |
| `placed:false` | 原版拒绝：与自身碰撞盒重叠 / 无支撑 / 超距 | 跳起放、换站位、先补支撑 |
| `armed:true` 但 `placed:false` | **只是挥了一下**，没放上 | 必须回读 `tile` |
| `(x,y) 不是一个房间，没法让人搬进去` | 引擎判定拒绝（本档该判定坏，见 game-rule §3.2） | 用 `housediag` 自己判 |
| `moved:false, reason:"None"` | `moveRoom` 静默 no-op | 别重试；先 `housediag` |
| `walk 超时` / 位置 10 秒不变 | A* 在空腔/竖井死锁 | 改逐格 `mine` + `climb`，或 `mirror` |
| `steps-exhausted` | 下挖/搭台在同一格反复失败 | 换起点、先补脚下、确认没被自己埋住 |
| `craft` 返回 `crafted:0` | 离工位太远或材料不足（看 `need`） | 走近工位（~1 格） |
| `grab` 返回 `picked:0` | 脚边没东西 / 背包满 / 距离 >4 | 先 `items` 看位置；`inventory` 看空格 |
| `save` 抛 NRE | `Main.SaveWorld` 在本档坏 | 别调；靠正常关窗口存档 |
| `idle`/`stop` 之后角色还在动 | 还有别的 hold 在跑（travel/guard/follow） | 再发一次 `stop`，`state` 看 `walk/follow` |
| `mine` 后手持变成镐 | 设计如此（自动选工具） | 打完记得 `select` 回武器 |

---

## 6. 权限黑名单（永久禁止，详见 safety-limit.md）

**禁止直接改世界**
`WorldGen.PlaceTile` / `PlaceObject` / `KillTile` / `KillWall` / `RangeFrame`、`Player.PickTile`、
`Main.tile[x,y].type = ...`、任何 `admin` 直写分支（都已删除，不要加回）。

**禁止直接控制用户的电脑**
`SetCursorPos` / `mouse_event` / `keybd_event` / `SendInput` / `SetForegroundWindow` / `AttachThreadInput`；
插件侧 `tr_key` / `tr_walk` / `tr_mouse` 已删除。

**禁止的作弊命令（已删）**
`give`（凭空给物品）、`spawn_npc`（凭空生成 NPC）、`teleport`（瞬移）。

**"后台"的合法含义**
用 mod 桥**在游戏内部**驱动角色（写 control 标志 / 走原版 `ItemCheck` / 走原版 `moveRoom`），
不是点用户桌面，也不是直接改世界数组。三者区别见 `AGENTS.md` §0。

---

## 7. 部署链路（改代码后必须走）

```
D:\...\dsh-terraria-agent-repo            ← 唯一真相源（git）
        │  C:\Windows\Temp\trtest\cycle.ps1：
        │    1) 同步仓库 → ModSources
        │    2) 编译（.NET 8 SDK）；失败则 exit 1 且**不启动游戏**
        │    3) 打包 .tmod 部署到 tModLoader
        │    4) 启动游戏到副屏 DISPLAY2（窗口 -2560,-174,1707,1067）
        │    5) 自动进多人存档
        ↓ 约 3 分钟
     可以用新命令了（例如 `housediag`），或 `state` 出现新字段
```

- **改仓库 ≠ 已生效**：不同步 + 不重编，游戏仍在跑旧包（踩过：改了半天的 `travel` 其实没部署）。
- 游戏在运行时 `.tmod` 被锁，必须先关窗口再编译；`cycle.ps1` 已处理。
- 验证部署：跑一个**只有新版本才有**的命令，别用行为推断。