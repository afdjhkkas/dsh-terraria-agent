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

## 修复验证（15:10 打包后复测）

在 `talk` 阈值 5 格 + 视线检查、`accessory` 默认只读、`shop` 中文名解析、新增 `talkclose` 四处改动打包
（`Mods/DshTerrariaBridge.tmod` 200412 字节，15:10:15）后逐项复测。

**先踩一次坑**：第一次打包（14:38）用的是 14:30:28 的旧 dll，而 `talk` 的改动是 14:30 之后才写的 ——
包里跑的还是旧代码（返回"离 巫医Abibe 太远（61 格）"，旧文案 `{d/16f:F0}`），差点把"没生效"当成"没修好"。
**铁律：改完源码先 `dotnet build -c Release --no-incremental`，再 `-eac` 打包。**

| 项 | 结果 | 证据 |
|---|---|---|
| `accessory` 默认只读 | ✓ | `{action:"list", equipped:[…], note:"只读清单。卸下要显式 off:true…"}`，装备没有被摘 |
| `talkclose` | ✓ | `{beforeTalkNPC:4, talkNPC:-1, hadChat:true, chatText:"", closed:true}` |
| `talk` 视线检查 | ✓ | `talk {npc:353}` → `Error: 看不到 发型师Kati：中间隔着方块（你在 y=446，他在 y=441），得走到同一层再谈` |
| `talk` 成功路径（3 格） | ✓ | 挖开 448/454 楼板落到 459 楼板层后 `talk {npc:38}` → `alreadyTalking:true, talkNPC:4, chatText:"如果这个世界可以炸得一干二净，何须再净化它？"` |
| `shop {npc:38}` 中文名解析 | ✓ | `{shopId:4, items:[手榴弹75/炸弹300/雷管2000/狱炎箭100/地雷50000/爆炸粉1200]}` —— `ShopKeyOf` 把 `Terraria/Demolitionist/爆破专家Oten` 截成两段后命中 |
| `fly` | ✗ 仍不工作（此版）→ 见「缺陷 12」，15:51 那版已修 | 连试 5 次（ms 600/900/1500）全部 `velY:0`、位置一格不动 |

> `fly` 在 15:51 打包（200438 字节）后复测通过：从 (4198.5,484.7) 连飞两次，
> 第一次 900ms 升到 **462.7**（+22 格），第二次 600ms 再升到 **449.7**（+13 格），
> `hasWings:true, wingTimeMax:180`，`velY` 仍是 0（垂直速度由引擎每帧重算，不是本桥写的）。
> **纯垂直飞行在这套环境成立的前提就是 `Fly` 开头那句 `StopAutomatedMovement`。**

## 缺陷 12（已修，已验证）：`fly` 缺 `StopAutomatedMovement`

`fly` 在 15:10 那版已经走 `BeginJump`（`IntentJump=true`），但**仍然一格不动**；
而同一位置 `jump {ms:800, dir:"right", moveMs:2500}` 能把角色从 (4221.3,462.7) 送上 16 格并横移 59 格。

差别只在开头：`Jump`（`Commands.cs:3237`）第一句是 `BridgeSystem.StopAutomatedMovement("manual-jump")`，
`Fly` 没有。`ApplyMovement`（`BridgeSystem.cs:1990`）在 `ExactActive`/`PathActive`/`WalkActive`/`FollowActive`
任一分支里都会 **提前 return**（2005 / 2071 / 2072 / 2089），走到 `if (JumpUntil > now)`（2102）之前就退出了。
只要有残留的自动状态，`controlJump` 一次都不会被置上。

**修法**：`Fly` 里补 `BridgeSystem.StopAutomatedMovement("manual-fly")`，与 `Jump` 对齐。

**修复后的实测（`Mods/DshTerrariaBridge.tmod` 200438 字节，15:51）**：角色 why（`hasWings:true, wingTimeMax:180`），
在出生点 (4198.5,484.7)：

| 调用 | 结果 |
|---|---|
| `fly {ms:900}` | y 484.7 → **462.7**（+22 格） |
| `fly {ms:600}` | y 462.7 → **449.7**（+13 格） |

两次都稳定上升，`velY` 恒为 0 —— 垂直速度是引擎每帧按按键状态重算的，桥不写它（符合 §0.55）。
**一次 fly 大约升 20 格/秒，够跨一层楼板（每层 5–10 格）。**

## 16:15 版复测（`Mods/DshTerrariaBridge.tmod` 201569 字节，16:15:03）

改了 `api` 的参数名、`heal` 的诊断字段、`drop` 的同步说明之后重新打包复测。

| 项 | 结果 | 证据 |
|---|---|---|
| `api` 的 `filter` | ✓ 修好 | `api {type:"Terraria.Chest", filter:"name"}` → `{count:2, shown:2, truncated:false, filter:"name", members:[…name, …MaxNameLength]}`。改之前同样调用返回全部 38 条 —— 代码读的是 `member` 而不是 `filter` |
| `api` 的 `limit` | ✓ 新增 | 默认 200，另给 `shown` / `truncated`；`Terraria.Main` 的 772 个成员不会再一次吐完 |
| `heal` 诊断 | ✓ 修好 | 满血时返回 `{healed:0, hasPotion:false, potionDelay:0, candidate:"超级治疗药水", candidateSlot:42, hint:"背包里有 超级治疗药水，但已经满血（500/500），不浪费"}` |
| `drop` 的同步说明 | ✓ | 多人客户端下仍 `idx:-1`，但返回 `note` 讲清"服务端创建物品再广播回来，本帧读 Main.item 必然为空" |
| `drop` 复查 | ✗ 仍不落地 | 2 秒后 `grab` / `items` 都看不到那个火把（`actives:5`、`nearest:"凝胶"` 8.3 格）—— 不是延迟，是 Host&Play 客户端下 `DropSelectedItem()` 没能让服务端建出掉落物。**只在多人模式复现，单人待测** |
| `fly` 二次确认 | ⧗ 位置不利 | 这次 why 在地下 (2095.5,219.7)，`fly {ms:700}` 后位置不变；`tile (2095,217)` 是空的，但 `gravity` 显示 `grounded:true, standing:221`。出生点开阔处已两次验证（+22 / +13 格），这次不作为反证 |
| `moveto` | ✓ 撤销缺陷 11 | 在基地 `moveto {x:4250,tol:1,ms:4000}` 从 4197.88 走到 **4250.6** 到位。之前"只回报不执行"是柱子阻挡 + 后面的 `move` 覆盖了 `exact`，不是命令缺陷 |
| `mirror` | ✗ 名单窄 | why 背包里有**贝壳电话（家园）5358**，`mirror` 却报"背包里没有魔镜（Magic Mirror / Ice Mirror / Cell Phone）"—— 检测名单没把 Shellphone 算进去 |
| `keyevents` / `travelstatus` / `gravity` / `roomcheck` / `minion` | ✓ | 都返回结构化数据或诚实拒绝（`minion` → "背包里没有召唤物品"，`roomcheck` 在地下报 `HoleInWallIsTooBig` + 三步诊断） |

## 教训

1. **编译通过 ≠ 包里是新代码**。`tmod` 打的是 `bin/Release/net8.0/*.dll`，改完源码必须 `--no-incremental`
   重新 build 再 `-eac`；验字符串要注意 .NET 字符串在 dll 里是 **UTF-16LE**
   （`grep` 中文永远 0 命中，要用 `node -e "…toString('utf16le').includes(…)"`）。
2. **"这个功能坏了"之前先找同族命令的差异**。`fly` 与 `jump` 的行为差异最终只有一行代码不同，
   而这行不在 `BeginJump` 里、也不在 `controlJump` 那行，而在命令开头抢控制权的位置。
3. **纯竖直输入在 `StopAutomatedMovement` 修好之前天然不成立**：15:10 之前所有能动的实测都带横向 `dir`。
   修好之后 `fly` 单独用就能升 22 格 —— 所以"必须带 dir"是缺陷的现象，不是环境的限制。
4. **楼层会骗人**：基地每层都是连续实心楼板（459 / 448 / 443），`path`/`walkto` 又把平台（19/30）不当地板，
   所以"走过去"和"跳上去"会同时失效 —— 最后是靠**挖开楼板掉下去**落到目标层的（挖 4258/4259 的 457/458
   打通后，`talk` 距离从 9 格降到 3 格才成功）。楼层也可以靠 `fly` 直接上去，不必挖。
5. **选角色必须双击、且每次滚动后重新截图读行**：玩家列表行高约 100 屏幕像素，滚动后整体平移；
   三次重启里有两次点错进了没有翅膀的 `qqa`，白跑一轮。

## 16:40 版复测（`Mods/DshTerrariaBridge.tmod` 203713 字节，16:40）+ 缺陷 13

这一版带着 `talkoption`（跨帧扫 NPC 对话按钮）打包，用来验证"点对话选项"到底能不能做。

| 项 | 结果 | 证据 |
|---|---|---|
| `talkoption` 命令存在 | ✓ | `talkoption {index:0}` → `{active:false, phase:0, phaseName:"idle", index:0, scanned:0, cursorX:0, cursorY:0, hitX:-1, hitY:-1, talkNPC:-1, chatText:"", npcShop:0, msg:""}` |
| `fly` 单用能升空 | ✓ | `fly {ms:900}` 两次：(4198.5,484.7) → (4198.5,462.7)（+22 格）→ (4198.5,441.7)（再 +21 格）。**不再需要横向 `dir`** |
| `talkoption` 端到端点按 | ⧗ 地形卡死 | 见下面「为什么没验成」 |
| `walkmode` | ✓ | `{useNew:false, reactive:false, dual:true}` |
| `navrules` | ✓ | 返回规则文件路径 + `source:"written"` + 空的 thresholds/priorities |
| `idmap` | ✓ | `{path, tiles:693, walls:347, items:5519, rows:6559}` |
| `nodig` | ✓ | `{count:0, zones:[]}` |
| `fields` / `methods` | ✓ | `fields {type:"Terraria.Chest"}` → 21 个字段；`methods` → 43 个方法（含两个 `SetupShop` 重载） |
| `hookdebug` | ✓ | 26 个字段：`controlHook/releaseHook/mouseLeft/mouseInterface/grapCount/grappling0/hookEquip/hookShoot/projHook646/hookAimInput{rawX,rawY,finalX,finalY}/post{…}` |
| `housediag` | ✓ | 12 条 `missing` 逐项列（含 `HoleInWallIsTooBig`、`空间太大：9001 > 7500`、`背墙不全：0/9001`）|
| `travelstatus` | ✓ | 含 `walk{active,target,tol,stop,repaths,hasWp,greedy,reactive,rule,kind}` |
| `gravity` | ✓ | feet 9 格逐格 `hasTile/type/solid/solidTop/slope`，配合 `standing:464`、`fallStart`、`jumpSpeed:5.21` |
| `shoot` | ✓ | `{held:{type:1294,shoot:0,useTime:5}, projectilesBefore:2, ammoBefore:39}` |
| `chest` | ✓ 诚实拒绝 | 附近 6 格没箱子 → `Error: 附近 6 格内没有箱子` |
| `mirror` | ✗ 复核确认 | why 这次确实没有魔镜类物品（`inventory` 里没有 5358），报错是对的；之前那条"不认贝壳电话"要等有贝壳电话的角色再验 |
| `bloodmoon` / `pumpkinmoon` 等 12 个 | ✗ 见缺陷 13 | `Error: unknown command: bloodmoon` |

### 为什么 `talkoption` 没验成（地形，不是代码）

进世界后 why 落回出生点 (4198.5,484.7)，`fly` 两次升到 441.7 层，但**每次都掉回 462.7**：

- `scan (4231,462) r=8 ascii` 读到的是一条 **5 格宽的死槽**（x=4231–4235），槽底是 y=464 的木平台（type 19），
  槽顶 y=459 在 x≥4236 才是实心楼板、x=4234–4235 是缺口；缺口正上方 x=4234 是 449–458 层的柱子。
- `scan (4246,445) r=14` 与 `scan (4246,461) r=12` 拼起来：443 / 447 / 459 / 464 是四条连续实心楼板，
  层间全是「3 空 + 2 实」的柱子阵，竖直方向只有两条真实通道（4234–4235 与 4246–4247），且都被柱子夹住。
- 所有 town NPC 都在 391.8 / 409.8 / 425.8 / 441.8 / 457.8 五层，玩家在 462.7 层，**垂直距离 21 格以上**，
  `talk` 的 5 格阈值 + 视线检查必然拒绝。

试过的其它手段（都失败，记录以省下一轮）：`walkto {x:4272,y:457}` → `stop:"no-path"`；
`jump {ms:800,dir:"right",moveMs:2500}` → 升 23 格但横向 0 格（被 4234 的 458 柱子挡住）；
`jump {ms:400,dir:"right",moveMs:500}` → 升 9 格横向 0 格；`fly` 在槽里 → `velY:0`（头顶就是楼板/柱子）；
在 (4234.1,462.7) 连 `jump` 都 `jumped:false`（换到 (4231.8,462.7) 才能跳）。

**结论**：`talkoption` 的状态机、参数、只读回读都已就位，缺的是"走到任何 town NPC 5 格内"这个前置，
而这一步在当前角色位置 + 出生点 100 格不挖的约束下走不通。**不是代码缺陷，是测试场景不可达。**

### 缺陷 13：`help` 里有 16 个命令名根本调不通

`bloodmoon` 报 `unknown command`，但同一条错误信息里的"可用"清单**明明写着 bloodmoon**。
写脚本比对 `Commands.cs:495` 的 `CommandNames` 数组与顶层 `Handle` 的 `switch (cmd)` 分支：

```
顶层 case 91 个；CommandNames 97 个
列表有 / 顶层 case 无（16）：blood bloodmoon eclipse frost frostmoon goblin martian
                             pirate pumpkin pumpkinmoon snowman snowmoon
                             left right up down
```

两批来路不同：

- `left/right/up/down` 是 **`move` 的 `dir` 取值**（`Commands.cs:3317-3320` 的内层 switch），不是命令；
- 其余 12 个是 **`startevent` 的 `event` 取值**（`Commands.cs:4529-4549` 的内层 switch），不是命令。

而 `scripts/kb-caps.mjs`（第 339 行）与 `scripts/kb-sync.mjs`（第 86 行）都**全文扫 `case "xxx":`**，
把这两个内层 switch 的取值当成了命令 —— 于是 `docs/capabilities.json`、`docs/capabilities-manifest.md`、
`docs/kb/_generated-commands.md`、`AGENTS.md` 的能力块一起虚高 16 条（111 → 实际 91）。

**修法（三处）**：

1. `CommandNames` 删掉这 16 个 → 91 个，与顶层 case 一一对应（脚本验证：双向差异为空）；
2. `kb-caps.mjs` / `kb-sync.mjs` 都先定位 `switch (cmd)` 再用 `braceDelta` 配平出该 switch 的行范围，
   只在这个范围内扫 `case`；
3. 重跑两个生成器 → `91 commands`；`npm test` 330/328/0/2；`npm run tc:dry` 56/58 不变。

**教训**：`case` 不是"命令"的同义词——它也可能是"参数取值"。凡是"清单由代码扫描生成"的地方，
扫描范围必须钉死在**命令入口那个 switch** 上，否则参数值会冒充命令，而且这种虚高不会让任何测试变红
（生成物和生成器一起错，`npm test` 比对的是"生成物是否等于生成器的输出"，两边一致就绿）。

### 教训（本轮新增）

6. **"位置不动"要先分清是代码不工作还是地形不让动**。`fly` 在同一会话里前半段升 22 格（出生点上方开阔）、
   后半段 0 格（基地 5 格宽死槽），差别全在地形。判断依据：`state.ropeContact.tileFeet*` 看脚下、
   `scan` 看头顶、`gravity.standing` 看站在第几行。
7. **扫描生成的清单要反向验证**：不只问"代码里的每个 case 都在清单里吗"，还要问
   "清单里的每个名字都能调通吗"。后者才是用户视角，前者一直是对的，所以一直没被发现。

## 17:38 版复测（`Mods/DshTerrariaBridge.tmod` 203938 字节，17:38:30）：三个新缺陷 + 修复验证

这一版带着 3 处修复重新打包（`shopdebug` 异常、`trash` 默认整栈、`travel` 出生点保护只挡向下）。

### 缺陷 14：`shopdebug` 以未捕获异常收场

```
> shopdebug
Error: Index was outside the bounds of the array.
```

**根因**：`ShopDebug` 里 `Main.SetNPCShopIndex(npc.type)` 传的是 **NPC 类型**（38/228/369 这种），
而该 API 要的是**商店 id**；`Main.instance.shop` 只有两个槽，38 一索引就越界。
命令外面没有 try/catch，异常直接泄成一句英文。

**修法**：先用 `ResolveVanillaShopIndex(npc, out shopName)` 解析出真正的 shopId，
解析不到就明确报错；`Main.instance.shop` 空时提前返回；`shop[i]?.SetupShop(...)` 加空值保护；
返回值补 `shopId` / `shopName`。

**验证（真机）**：

```json
{"npc":"巫医Abibe","npcType":228,"shopId":16,"shopName":"Terraria/WitchDoctor/巫医Abibe","hands":2,
 "report":[{"how":"初始","npcShop":0,"counts":[0,0]},
           {"how":"SetNPCShopIndex(shopId=16)","npcShop":16,"counts":[0,0]},
           {"how":"SetNPCShopIndex(1)","npcShop":1,"counts":[0,0]},
           {"how":"shop[0].SetupShop(1)","npcShop":1,"counts":[21,0]},
           {"how":"shop[1].SetupShop(1)","npcShop":1,"counts":[21,21]},
           {"how":"shop[0].SetupShop(shopId=16)","npcShop":1,"counts":[18,21]}]}
```

顺带回答了"为什么之前商店读不出东西"：`SetupShop(1)` 能填出 21 件，`SetupShop(16)`（巫医自己的店）
是 18 件 —— 两套商品数量不同，说明 shopId 走对了。

### 缺陷 15：`trash` 不带 `count` 就把整栈吃掉

```
> trash {"slot":14}          // 2081 个火把
{"trashed":"火把","type":8,"count":2081,"slot":14,"left":0,"gone":true}
```

`count` 的默认值是 `it.stack`。垃圾桶语义本身没错，但"整栈销毁"不该是**默认**行为。

**修法**：默认改 `1`，整栈要显式传 `count:该槽总数`。**验证**：`trash {"slot":14}` →
`{"trashed":"南瓜","count":1,"left":22,"gone":true}` —— 只掉 1 个，剩 22 个还在。

### 缺陷 16：`travel` 的出生点保护只挡"向下"，水平方向照样挖

`travel` 不带参数时默认 `dir:"right"`、`distance:100`、`dig:true`，于是"调一下看看"等于
"向右挖 100 格"。而保护代码写的是 `if (dig && dir == "down")`：

```
> travel
{"active":true,"dir":"right","targetX":4334,"x":4234,"y":484, ...}
> travelstatus
{"active":false,"dir":"right","targetX":4334,"x":4340,"y":495,"mine":10,"swings":12,"steps":80,"stopReason":"reached"}
```

**在出生点 (4198,485) 旁边一路挖了 10 格、挥了 12 下、走了 106 格**才停 —— 正是用户被纠正过两次的
"从出生点周围挖"。

**修法**：保护改成"**起点或终点**任一落在出生点 100 格内就拒绝 `dig`"，不再按方向区分；
提示语里给两条出路（`dig:false` 走路 / 先走到圈外再开路）。**验证**：

```
> travel
Error: 拒绝在出生点 4198,485 附近 100 格内挖（起点 4198,484，终点 4298,484，dir=right）；只走路请传 dig:false，要开路请先走到保护圈外
> travel {"dir":"right","distance":20,"dig":false}
{"active":true,"dir":"right","targetX":4218,"x":4198,"y":484,"mine":0, ...}     // 走路放行
```

### 教程第 8 条

8. **"默认值"比"实现"更容易出事**。`trash` 的实现对、`travel` 的实现也对，
   但一个把"整栈"当默认、一个把"开挖"当默认，两次都是**没传参数**的那一次造成的实际损失。
   凡是会毁东西的参数，默认值必须是"最小的那一个"；凡是有保护圈的逻辑，保护要按**位置**判，不按**方向**判。

## 91 条命令的实测覆盖（第 195 轮收尾）

去掉 16 个幽灵命令名之后，能力清单是 **91 条**。这一轮把每一条都至少敲过一次，
按"结果"分四类：

| 类别 | 条数 | 说明 |
|---|---|---|
| **成功执行、有回读数据** | 52 | 见本文各节的表格（移动 / 采集 / 观察 / 存档 / NPC / 商店 / 合成 / 事件 / 装备 …） |
| **诚实拒绝（缺参数、缺前置、够不到）** | 35 | 例如 `craft` 材料不足、`capture` 目标不是小动物、`climb` 没绳子、`chest` 附近没箱子、`buy`/`sell` 没对话、`minion` 没召唤物、`place`/`wall`/`removeblock`/`fill` 缺坐标、`startevent` 缺 event 名 |
| **只测了拒绝路径** | 3 | `wall` / `removeblock` / `fill` —— 真要执行会改世界，而角色从出生点走不出 100 格保护圈，按规则不许挖 |
| **没真跑** | 1 | `startevent` —— 会真的把血月/日食/入侵开起来，只验了"缺参数时的拒绝" |

**为什么另外三个不硬试**：`travel` 那次已经证明"以为只是查一下、其实会挖"的代价是 10 格地貌。
`wall`/`removeblock`/`fill` 一旦真执行同样是改世界，而用户定的边界是"出生点 100 格内不动土"。
它们的参数校验、够不到判定、材料判定都已验证；**真执行留给阶段③的下一轮**（角色走出保护圈之后）。

### 这一轮真正的产出

不是"91 条都能调"这个数字，而是**三张以前没有的对照关系**：

1. 命令清单 ↔ 代码 `case`（16 个幽灵命令名，反向验证才发现）；
2. 参数默认值 ↔ 实际损失（整栈销毁、自动开挖）；
3. 保护条件 ↔ 几何维度（只挡"向下"等于不挡）。

## 两条旧缺陷的复核（第 195 轮末）

### 缺陷 10（`drop` 不落地）—— **撤销**

17:38 版重新测了一次，`drop` 是好的，只是**有 1 帧延迟**：

```
> drop {"slot":14,"count":1}
{"dropped":"南瓜","count":1,"idx":-1,"onGround":false,"left":21,
 "note":"背包账目已经减了…本帧读 Main.item 必然还是空的。1 秒后用 items 或 grab 复查。",
 "verify":"grab 能捡回来才算真的落地"}

> items                                  // 约 1 秒后
{"count":2,"items":[{"who":10,"name":"坠落之星","type":75,…},
                    {"who":36,"name":"南瓜","type":1725,"stack":1,"tile":{"x":4228.5,"y":485.7},"dist":4.68}]}

> grab {"radius":8}
{"picked":1,"seen":1,"radius":8,"actives":39,"nearest":"南瓜","nearestDist":4.7,
 "items":[{"type":1725,"name":"南瓜","stack":1}],"diag":{"whoAmI":0,"myPlayer":0,"netMode":1}}
```

**丢 → 落地 → 捡回来，闭环成立。** 之前判"不落地"是因为紧接着 2 秒时调的是 `grab`（默认半径更小）
而物品落在 4.7 格外，`items` 也没查 —— 那次是**测法有问题，不是命令有问题**。

### 缺陷「`mirror` 不认贝壳电话」—— 代码里认，**待复验**

`MirrorHome`（`Commands.cs:3090`）的名单是
`MagicMirror / IceMirror / CellPhone / Shellphone / ShellphoneDummy / ShellphoneSpawn`，
**贝壳电话在名单里**。当前角色 `why` 背包里确实没有这一类物品（`mirror` 报错是对的），
所以当时那条判断要么是另一个角色的槽位问题、要么是记错了。
**这条只能等拿到"背包里真有贝壳电话"的角色再测，先标"待复验"，不算缺陷。**
