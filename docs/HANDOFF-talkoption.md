# 交接：NPC 对话里的「点按钮」点不到（`talkoption`）

给改 C# 的人。所有现象都是真机实测，不是推测；推测的部分会明确标注。

## 〇、这是 NPC 社交那一整块里的最后一环

`docs/capabilities.md` 里 NPC 社交共五步，**前四步都通了，卡在第五步**：

| 步 | 命令 | 状态 |
|---|---|---|
| 1. 走到 NPC 旁 | `walkto` / `move` | ✅ |
| 2. 右键开对话 | `talk`（内部走真实右键）| ✅ 通了 |
| 3. 读到台词 | 回包 `chatText` | ✅ 通了 |
| 4. 读到货架 | `shop`（直接读数据）| ✅ 通了 |
| 5. **点按钮（商店/关闭）** | `talkoption` | ❌ **就是这里** |

所以这次要修的不是"对话打不开"，而是**对话打开之后，点不到里面的按钮**。

相关命令一共四条，都在 NPC 对话这条链上，改的时候要一起看：

| 命令 | 分发入口 | 实现函数 | 作用 |
|---|---|---|---|
| `talk` | `Commands.cs:69` | `TalkTo(JsonElement p)`（`:1664`） | 走到 NPC 旁 → 真实右键 → 建立对话 |
| `talkfocus` | `Commands.cs:72` | `TalkFocus(JsonElement p)`（`:1843`） | 探针：读 `Main.npcChatFocus1..4` + 当前鼠标 |
| `talkoption` | `Commands.cs:71` | `TalkOption(JsonElement p)`（`:1791`） | **要修的那个**：扫描按钮 → 挪鼠标 → 点 |
| `shop` | `Commands.cs:73` | `ShopList(JsonElement p)`（`:2044`） | 读货架数据；不负责点击或打开对话选项 |

## 一、要做的事

在**已经打开对话**的状态下，用鼠标点中对话选项按钮。命令的 `index` 直接对应
原版的 `Main.npcChatFocus1..4`：`1=关闭`、`2=第一个业务按钮（通常是商店）`、
`3=第二个业务按钮`、`4=幸福度`。没有对应按钮的焦点位会一直是 `false`。

`talkoption {index:1..4}` 就是干这个的：扫描屏幕找按钮 → 把鼠标准星挪上去 → 点一下。
**现在扫 455 个点，一个都命中不了。**

## 二、真机现象

**复现用的就是下面这个 NPC 和这个位置，照着做能重现：**

| 项 | 值 |
|---|---|
| NPC | **爆破专家**（Demolitionist），`whoAmI = 4`，`type = 38`，站在 `4281.8, 457.8` |
| 玩家 | `4282.2, 461.4`（就在 NPC 右边 5 格内，有视线） |
| 对话文本 | `我不明白为什么我们不能干脆炸掉这些传送门。` |
| 这台机器其他友好 NPC | 26 个（向导/护士/商人/军火商…），`npc {radius:80}` 可列 |

**对话框里那四个按钮**（原版布局）：左上角竖排，「商店」通常是第 1 个，
之后是「幸福度」「关闭」等 —— 具体第几个取决于这个 NPC 有没有商店。

环境：游戏窗口在副屏（`x:66982, y:5558`），`humaninput.focus = false`（真失焦），
屏幕逻辑尺寸 `1673 x 1046`。

```
1) 右键对话 —— 成功
   use { right:true, x:4281, y:458 }
   → talkNPC 4
   → chatText '我不明白为什么我们不能干脆炸掉这些传送门。'

2) 扫描按钮 —— 全失败
   talkoption { index:1 }
   → phase 1 scanning
   → 9 秒后 phase 5 failed, scanned 455, msg:"扫完 455 个点都没有按钮 1"
   → hitX / hitY 恒 -1

3) 悬停状态 —— 恒 false
   talkfocus → { f1:false, f2:false, f3:false, f4:false, mouse:{x:-1058, y:724} }

4) 逐点写鼠标 —— 写不进去
   写 (500,160) → 连读 3 次：f1=false mouse=-1058,724 | 同 | 同
   写 (560,160) → 连读 3 次：f1=false mouse=-1058,724 | 同 | 同
   写 (620,160) → 连读 3 次：f1=false mouse=-1058,724 | 同 | 同
   写 (500,200) → 连读 3 次：f1=false mouse=-1058,724 | 同 | mouse=-1048,759（这一次是玩家真实动了鼠标）
```

第 4 条是关键：**`talkfocus {x,y}` 写进去的值，一帧后就读回系统鼠标的值。**
（`mouse` 字段读的是 `Main.mouseX/Y`，它每帧在 `Main.Update` 里被系统光标重算。）

## 三、涉及代码

| 位置 | 作用 |
|---|---|
| `BridgeSystem.cs` `InstallTalkOptionHook()` / `PrepareTalkOptionInput()` / `FinishTalkOptionInput()` | **核心，扫描态机**（NPC 对话按钮）。在 `GUIChatDrawInner` 原版调用前写鼠标；扫上半屏 `y0=80..h/2`、`step=8`、`x=100..w-100`；原版返回后读 `Main.npcChatFocus1..4`；命中后 phase 2 发 `mouseLeft=false / mouseLeftRelease=true`，phase 3 发 `mouseLeft=true / mouseLeftRelease=true` |
| `BridgeSystem.cs` `ApplyTalkInputBeforeMouseOver()` | 只保留界面层占位和 `talk` 的右键帧；`talkfocus` / `talkoption` 坐标不在这里写入，因为这里早于 `Vanilla: Player Chat` 的 `SetZoom_UI` |
| `BridgeSystem.cs:661` `ModifyInterfaceLayers()` | **插入位置**，下面第四节就是这里 |
| `Commands.cs:1791` `TalkOption(JsonElement p)` | 入口：`index` 必填（1..4）才启动扫描；`index<=0` 只读状态；`stop:true` 中止 |
| `Commands.cs` `TalkFocus(JsonElement p)` | 探针：读四个焦点位 + 当前鼠标；带 `x/y` 时写一次探针值 |

全仓 `Main.mouseX =` 共 7 处：`BridgeSystem.cs:429`、`:459`、`:471`（抓钩瞄准）、
`:592`（`TalkOptX`）、`:599`（`TalkOptHitX`）、`:623`（`ProbeMouseX`）、`:2434`（`ApplyAim`）。

## 四、代码核对结论与修复

反编译当前 tModLoader 的 `Terraria.UI.GameInterfaceLayer.Draw()` 后确认：每个界面层都会按
自身的 `InterfaceScaleType` 调 `PlayerInput.SetZoom_*()`。因此即使桥层插在
`Vanilla: Player Chat` 之前，桥层里写的 `Main.mouseX/Y` 仍会在进入原版聊天层时被再次
`SetZoom_UI()` 覆盖。原来的“前置层逐点扫描”必然扫不到焦点。

现在改为非 IL 的 MonoMod detour，挂在私有 `Main.GUIChatDrawInner()` 上：detour 前写入
当前探针点，原版方法在同一调用中执行 `DrawNPCChatButtons()` 并计算
`npcChatFocus1..4`，detour 返回后再读取焦点并推进扫描状态。这样坐标和判定处在同一
UI 缩放上下文中，点击阶段也由原版自己的按钮回调完成。

扫描步长也不能用固定 40px。按钮宽度由本地化文本测量决定，窗口缩放后短文本按钮可能比
40px 窄；当前改为 8px，只发送鼠标输入，命中后立即停止。

原版 `GUIChatDrawInner()` 的点击分支要求 `mouseLeft && mouseLeftRelease` 同时为真，成功后
会把 `mouseLeftRelease` 清为 false。桥原先在点击帧写 `mouseLeft=true`、
`mouseLeftRelease=false`，因此无论目标坐标是否正确，原版都不会执行按钮回调。修复后点击帧
同时写入两个 true，并在下一帧释放左键。

另一个已确认的语义错误是焦点编号：`npcChatFocus1` 是关闭按钮，`npcChatFocus2` 是第一个
业务按钮（通常是商店），`npcChatFocus3` 是第二个业务按钮，`npcChatFocus4` 是幸福度。
所以要点商店用 `index:2`，关闭用 `index:1`。

**尚未做真机验证**：`GUIChatDrawInner()` 会在 `PlayerInput.IgnoreMouseInterface` 为 true 时跳过
悬停与点击判定；游戏窗口失焦时扫描能否命中，仍需通过状态回读确认。代码发送左键输入不等于
按钮效果已发生，必须检查 `talkNPC`、`chatText` 或 `npcShop` 的变化。

## 六、怎么验

改完编译装包重启后，按这个顺序跑（每一步都要回读，不许只看命令回包）：

```
1) 走到 NPC 旁 → use { right:true, x:<npc格x>, y:<npc格y> }
   判据：talkNPC > 0 且 chatText 非空

2) talkfocus { x: <按钮中心x>, y: <按钮中心y> }
   判据：f1 或 f2 变成 true —— 这一步不通，后面全不用试

3) talkoption { index:2 }
   判据：phase 走到 6（inputSent）且 hitX/hitY 不再是 -1；该状态只证明左键输入已发送，仍须检查第 4 步的商店回读

4) 商店真的开了没有
   判据：Main.npcShop > 0（现在 shopIndex 恒 0 说明没开）

5) 关闭按钮
   talkoption { index:1 } 或按 Escape
   判据：talkNPC 变回 -1、chatText 变空
```

第 2 步是分水岭：**鼠标能不能"按住在目标上"**。现在它一帧就被系统光标抢回去。
