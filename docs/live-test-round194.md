# 第 194 轮真机实测记录（2026-10-29）

环境：tModLoader 1.4.4.9 + DshTerrariaBridge 0.2.0（`e8b8f5b` 构建，`Mods/DshTerrariaBridge.tmod` 198470 字节）。
世界「单人」8400×2400，专家/困难模式；角色 why（500 血）。测试在 **Host & Play 多人模式**下进行。

## 为什么要进多人模式

单人模式下窗口一失焦 `paused` 就为 true，`applyInput` 被跳过，动作命令拿到 `moved:false`。多人大厅模式下 `paused:false` 与 `hasFocus:false` 可以并存 —— 服务端在跑，客户端只负责发输入，**后台发动作就是真实动作**。这是本轮最大的工程收益：无需把游戏窗口拉到前台就能操作。

## 通过的命令（有回读证据）

| 命令 | 实测结果 |
|---|---|
| `ping` | `pong:true, version:0.2.0, inWorld:true, paused:false` |
| `players` | `why 500/500, tile (4236.5,462.7), local:true` |
| `move` | dx 30.6/1600ms；dx 1.3/300ms；失焦照跑 |
| `jump` | 462.7 → 459.1，`dY:-3.6, native:true` |
| `mine` | PalmWood(322) 一格 1 次挥动，`mode:"player"` |
| `hook` | 月钩把角色拉近 1.4 格，`equipHow:"already"` |
| `unhook` / `stop` / `idle` | 均正常 |
| `scan` / `tile` / `find` | 结构读数与 `total` 计数正确 |
| `gravity` | 脚下三格逐格 `hasTile/type/solid/solidTop` 全返回 |
| `needs` / `roomcheck` / `housediag` | 返回原版 `SceneMetrics` 判据与洪水填充明细 |
| `housing` | 28 个 NPC 的家 + `homeless` 计数 |
| `inventory` | 42 件物品 + 装备槽完整 |
| `splitstack` | 火把 10 个，`left:2071` |
| `craft` | 材料不足时明确报错，不假成功 |
| `recipes` | `火把 need:"凝胶x1 + 木材x1"` |
| `map` | 8401×2401，采样 316351 格 |
| `tiledata` | `Furnaces 3×2 origin(1,1)` |
| `heal` | 满血时 `healed:0, hasPotion:false` |
| `capture` | 非小动物时明确拒绝 |
| `select` | `{slot:0, 锯刃镐, damage:39}` |
| `minion` | 无召唤物时报 `背包里没有召唤物品` |
| `mirror` | 无魔镜时报 `背包里没有魔镜` |
| `travelstatus` | 空闲态 `active:false` |
| `humaninput` | 控制标志 + OS 鼠标（989,431 → tile 4237,455）|
| `keyevents` | tick 级按键边沿，`left/right` 成对出现 |
| `chatpost` / `chatpoll` | `hooked:true`，`pending:0` |
| `api` | `Terraria.Chest` 38 个成员带 public/static 标记 |
| `help` | 87 条命令全表 |

## 失败/缺陷（8 条）

### 1. `fly` 完全不动（真 bug，已定位到行）
连试 5 次全部 `velY:0`、`wingTime:180/180`、位置一格不动。

`Commands.cs:3638-3639` 的 `Fly` 只写：
```
BridgeSystem.JumpUntil = ...;
BridgeSystem.JumpIssued = false;
```
而 `Commands.cs:3186` 的 `Jump` 调 `BridgeSystem.BeginJump(...)`，`BridgeSystem.cs:2622-2630` 里是三行，**多了 `IntentJump = true;`**。

消费端 `BridgeSystem.cs:616`：
```
player.controlJump |= IntentJump || (triggers.Jump && JumpUntil > now);
```
没有 `IntentJump` 就只剩 `triggers.Jump`；这套环境没有真实物理按键，`controlJump` 恒 false，翅膀永远不开。
同文件 `BridgeSystem.cs:614-615` 给 up/down 时已注释"不能躲在 triggers 后面"，jump 这行没跟上。

**修法**：`Fly` 改调 `BridgeSystem.BeginJump(...)`。

### 2. `path` / `walkto` 在平台上判无路
站木平台（type 19）或绳上时 `standS=False`，A* 直接报 `没有可行路径`，`walkto` 立刻 `stop:"no-path"`。
感知层没把平台当可站地板。基地是「每 5 格一层实心楼板 + 柱子」结构（`scan` 读到 y=443/448/459/464 四条楼板），所以整栋楼都走不了，只能一格格 `move`。

### 3. `save` 在 Host & Play 下不落盘
`save` → `saved:false, netMode:1, hint:"本进程是世界客户端，存盘要发给服务端进程（专用服务端端口 47778）"`。
且 `worldPath` 报的是 `孤立大坑.wld`，与 `state.world.name`("单人") 不一致 —— 需要确认 `Main.ActiveWorldFileData` 在 Host&Play 下取的是哪个路径。

### 4. `api` 的 `filter` 参数不生效
`api {type:"Terraria.Chest"}` 正常（38 个）；`api {type:"Terraria.Main", filter:"npcChat"}` **返回全部 772 个成员**，filter 被忽略，输出接近 2MB。
**文档里要把「不要对 Terraria.Main 用 api」写成红线。**

### 5. `shop` 用中文 NPC 名解析不出商店 id
`shop {npc:38}` → `找不到 爆破专家Oten 对应的商店 id（name=Terraria/Demolitionist/爆破专家Oten）`。
`ResolveVanillaShopIndex`（`Commands.cs:1751`）用本地化全名做前缀匹配，带中文名的 NPC 匹配不上。
绕过：`shops {want:"炸弹"}` 遍历 300 个 id 命中 `shopId:4`，再 `shop {npc:38,shopId:4}` 成功列出 6 件。
另外 `shop {shopId:38}` 直接 `Index was outside the bounds of the array` —— 商店 id ≠ NPC type，应先做范围检查再取数组。

### 6. 没有关闭对话的原语
`interact` 点空地关不掉（`talkNPC` 仍为 4）。
但 `api` 清单里有 **`True | Void CloseNPCChatOrSign()`**（public static）→ 包一条 `talkclose` 即可。

### 7. `accessory` 不带参数会卸装备（破坏性默认）
`Accessory`（`Commands.cs:2807`）第 2812 行 `bool off = Json.Bool(p, "off", true);` —— **默认值 true**，所以任何不带 `off:false` 的调用都会执行"卸下第一个饰品"。
实测中一次 `accessory {}` 卸掉了 3 号格冰冻护盾，一次 `accessory {from:26}` 又卸掉 4 号格烈火手套（`from` 被忽略，因为先走 off 分支）。两次都已装回原位。
**修法**：`off` 默认改 false；不带 `off`/`from` 时只列出饰品槽，不做任何变更。

### 8. 上楼没有可靠手段
`jump` 单次能上 5.6 格（够到 458 的楼板顶），但 `jump {dir:"right", moveMs:600}` 的横向位移在落点判断上跟不上，实际落回原层。挖开 459 楼板两格（4234/4235）后仍上不去。`fly` 又坏着，所以**上下层只能靠 `hook` 一格一格拉**。

## 顺带发现的可用原生 API（都来自第 4 条那份清单）

- `Void OpenShop(Int32)` —— 直接开商店 UI，不必依赖 `talkNPC`
- `Void CloseNPCChatOrSign()` —— 关对话/告示牌
- `Void SetNPCShopIndex(Int32)`
- `Void DrawNPCChatButtons(Int32, Color, Int32, String, String)`
- `Void DrawNPCChatBubble(Int32)`
- `False | Void HoverOverNPCs(Rectangle)`（私有，但 `talk` 已经走在它的前置层里）

## 本轮结论

- `talk` 在多人大厅模式下依然成立（此前单机实测 `talkNPC`+`npcChatText` 同时回读到）。
- 8 条缺陷里 **3 条是明确的代码 bug**（fly 缺 IntentJump、accessory 默认值、shop 中文名解析），**2 条是能力缺口**（close-chat、平台寻路），**2 条是文档红线**（api filter、save 在客户端不落盘），**1 条是设计问题**（上下层移动）。
## 追加实测（同一轮，修完代码但**尚未重启加载**时跑的第二批）

这一批跑的是**旧版 mod**（`Mods/DshTerrariaBridge.tmod` 还是 13:24 那份），
所以下面每条都是旧行为；修好的代码已提交 `6b06f2a`，等重启加载后再复测。

| 命令 | 结果 |
|---|---|
| `items` | ✓ `{count:0}` |
| `equip {slot:1}` | ✓ 诚实拒绝：`天顶剑 不是护甲` |
| `use {ms:200}` | ✓ `anim:13`，手持锯刃镐真的挥了 |
| `guard` / `guard {off:true}` | ✓ 开启返回 `untilTick`+武器，关闭返回 `off:true` |
| `attack` | ✓ 诚实拒绝：`半径 40 格内没有可攻击的敌怪` |
| `mount {action:"info"}` | ✓ `active:false, items:[]` |
| `research` | ✓ `slot 越界`（探索模式无此功能） |
| `duplicate {slot:0}` | ✓ `duplicate 需要 item`（应给 item id） |
| `place {x,y}` | ✓ 先报 `需要 type`，给了 type 后报 `背包里没有能放置 Dirt 的物品` |
| `bobber` | ✓ `{count:0}` |
| `castline` | ✓ 先报 `需要 x,y（水面那一格）` |
| `reel` | ✓ `{used:true, poleSlot:8}` |
| `grab` | ✓ `seen:0, actives:7, nearest:"发光蘑菇" 371` |
| `moveto {x:4240}` | ⚠ 返回 `{from:4235.88,target:4240,delta:4.12}`，但角色只挪了 0.1 格 — **未真正走到** |
| `drop {slot:14,count:1}` | ⚠ `left:2070`（背包少了 1 个），但随后 `items`/`grab` 都 `seen:0` — **物品消失，未落地** |
| `talk {npc:38}`（距离 10 格） | ⚠ `armed:true`、鼠标被放到 `finalMouseX:1001/finalMouseY:454`（正好是 NPC 的屏幕坐标），但回读 `talkNPC:-1`、`npcChatText:""` |

## 缺陷 9：`talk` 的距离阈值大于引擎自己的对话距离

`Commands.cs` 里 `talk` 的门槛是 **12 格**（`if (d > 12 * 16f)`），实测在 **10 格**处
`armed:true`、鼠标定位完全正确（`finalMouseX/Y` 与世界坐标差值和 NPC 位置对得上），
但引擎没开对话（`talkNPC:-1`）。

Terraria 自己的 NPC 交互判定比 12 格严得多（约 5 格），而且要求**视线不被方块挡住**。
本轮那次尝试里玩家在 y=462.7（脚踩 y=464 平台层），爆破商在 y=457.8（脚踩 y=459 楼板层），
中间隔着一整层实心楼板 —— 就算距离改成 5 格也谈不起来。

**修法**（未做）：把阈值降到 5 格左右，并在返回里明确报"隔着方块看不到"，
不要用 `armed:true` 让人误以为马上会开对话。

## 缺陷 10：`drop` 之后物品既不在地上也不在包里

`drop {slot:14,count:1}` 返回 `{dropped:"火把",count:1,onGround:false,left:2070}`，
背包确实从 2071 → 2070，但紧接着 `items` 与 `grab` 都是 `seen:0`。
Host & Play 下掉落物由服务端生成再同步，可能只是延迟；但也可能客户端吞了物品。
**待复现**：换单人模式重复一次，确认是同步问题还是真丢。

## 缺陷 11：`moveto` 只回报目标不执行

`moveto {x:4240,tol:1,ms:3000}` 返回 `{from:4235.88,target:4240,delta:4.12}`，
看起来像"已经算好要走 4.12 格"，但角色实际只从 4235.88 挪到 4236。
`state.exact.stopReason` 报 `"manual-move"` —— 说明它被后面的 `move` 覆盖掉了。
需要确认 `moveto` 是同步执行还是异步下达；如果是异步，就不该在返回值里写 `from/target` 让人以为已到位。
