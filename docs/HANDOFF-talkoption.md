# 交接：`talkoption` 点不到对话按钮

给改 C# 的人。所有现象都是真机实测，不是推测；推测的部分会明确标注。

## 一、要做的事

在**已经打开对话**的状态下，用鼠标点中对话选项按钮。四个按钮分别是
「商店 / 幸福度 / 关闭 / 其他」，原版用 `Main.npcChatFocus1..4` 标记哪个按钮被悬停。

`talkoption {index:1..4}` 就是干这个的：扫描屏幕找按钮 → 把鼠标准星挪上去 → 点一下。
**现在扫 455 个点，一个都命中不了。**

## 二、真机现象

环境：游戏窗口在副屏（`x:66982, y:5558`），`humaninput.focus = false`（真失焦），
屏幕逻辑尺寸 `1673 x 1046`。玩家站在爆破专家 who 4 旁边。

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
| `BridgeSystem.cs:540` `StepTalkOption()` | 扫描态机。扫上半屏 `y0=80..h/2`、`step=40`、`x=100..w-100`；命中判据 `Main.npcChatFocus1..4`；命中后 phase 2 发 `mouseLeft=false / mouseLeftRelease=true`，phase 3 发 `mouseLeft=true / mouseLeftRelease=false` |
| `BridgeSystem.cs:618` `ApplyTalkInputBeforeMouseOver()` | 每帧（绘制阶段）写 `ProbeMouseX/Y` → `Main.mouseX/Y`，然后驱动 `StepTalkOption()` 与 `talk` 的右键帧 |
| `BridgeSystem.cs:661` `ModifyInterfaceLayers()` | **插入位置**，下面第四节就是这里 |
| `Commands.cs:1791` `TalkOption(JsonElement p)` | 入口：`index` 必填（1..4）才启动扫描；`index<=0` 只读状态；`stop:true` 中止 |
| `Commands.cs` `TalkFocus(JsonElement p)` | 探针：读四个焦点位 + 当前鼠标；带 `x/y` 时写一次探针值 |

全仓 `Main.mouseX =` 共 7 处：`BridgeSystem.cs:429`、`:459`、`:471`（抓钩瞄准）、
`:592`（`TalkOptX`）、`:599`（`TalkOptHitX`）、`:623`（`ProbeMouseX`）、`:2434`（`ApplyAim`）。

## 四、我改了一处（推测，**未验证**）

`ModifyInterfaceLayers()` 原来插在 `"Vanilla: Mouse Over"` 之前：

```csharp
int mouseOver = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Over");
layers.Insert(mouseOver, new LegacyGameInterfaceLayer("DshTerrariaBridge: Talk Input", ...));
```

**推测的层序**（原版 `Main.DrawInterface`）：

```
Vanilla: Chat
Vanilla: Player Chat     ← 对话按钮在这里画，读 Main.mouseX 判悬停
Vanilla: Mouse Text
Vanilla: Mouse Over
...
```

如果这个顺序成立，插在 `Mouse Over` 之前就等于排在 `Player Chat` **之后** ——
这一帧按钮已经用**系统鼠标**画完了，我们再挪 `Main.mouseX`，要等到下一帧才被读到，
而下一帧 `Main.Update` 又把它还原成系统鼠标。于是 `npcChatFocus1..4` 永远是 false。

**已改成**插在 `"Vanilla: Player Chat"` 之前，找不到就退到 `"Vanilla: Chat"` 之前，
再找不到就 `idx = 0`（`BridgeSystem.cs:661-680`）。

**这一处只是推断，没有铁证**：
- `ilspycmd` 反编译 `Terraria.Main` 输出 0 行（失败），层名是照 `ModifyInterfaceLayers`
  里能查到的字符串推的。
- 改完还没编译、没打包、没重启，所以**不知道有没有用**。

**请先确认层序**（反编译 `Main.DrawInterface` 找 `AddLayer` / `layers.Add` 的调用顺序，
或直接在 `ModifyInterfaceLayers` 里把 `layers` 的 `Name` 全打印出来看一眼）。
如果 `Player Chat` 确实在 `Mouse Over` 之前，我这个改法方向是对的。

## 五、如果层序不是问题，还有三个候选方向

1. **`npcChatFocus1..4` 的重算时机**：也许它不是绘制时算的，而是在
   `Main.Update` 里用系统鼠标算的。如果是这样，绘制阶段写 `Main.mouseX` 完全无效，
   必须改成**在 `Main.Update` 之前**写（`PostUpdateInput` 或 `PreUpdate`），
   并让它撑过整个 `Main.Update` 不被覆盖。这是三种可能里最难但最根本的一种。

2. **失焦时原版是否根本不重算**：`humaninput.focus = false`。有些 UI 悬停逻辑
   写在 `if (Main.hasFocus)` 里（`releaseMount` 那个坑就是这么来的）。
   可以照 `OpenMountInputGate()` 的做法，在扫描窗口内把 `Main.hasFocus` 临时置真
   （`Main.Update` 每帧用 `IsActive` 重算，不会残留）。

3. **坐标基准**：`InterfaceScaleType.Game` 与对话按钮的 `InterfaceScaleType.UI`
   缩放不同。`Main.mouseX` 是**未缩放**的屏幕坐标，但如果扫描用的是世界坐标
   或缩放后的坐标，455 个点会整体偏掉。可以用 `talkfocus` 手动把鼠标移到
   **玩家亲眼看到的按钮位置**（比如截图量出按钮中心），看 `f1` 会不会亮 ——
   亮了就说明是坐标基准错，不是时序错。

## 六、怎么验

改完编译装包重启后，按这个顺序跑（每一步都要回读，不许只看命令回包）：

```
1) 走到 NPC 旁 → use { right:true, x:<npc格x>, y:<npc格y> }
   判据：talkNPC > 0 且 chatText 非空

2) talkfocus { x: <按钮中心x>, y: <按钮中心y> }
   判据：f1 或 f2 变成 true —— 这一步不通，后面全不用试

3) talkoption { index:1 }
   判据：phase 走到 4 且 hitX/hitY 不再是 -1

4) 商店真的开了没有
   判据：Main.npcShop > 0（现在 shopIndex 恒 0 说明没开）

5) 关闭按钮
   talkoption { index:3 } 或按 Escape
   判据：talkNPC 变回 -1、chatText 变空
```

第 2 步是分水岭：**鼠标能不能"按住在目标上"**。现在它一帧就被系统光标抢回去。
