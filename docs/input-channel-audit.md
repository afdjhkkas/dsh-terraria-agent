# 输入通道审计：哪些动作真正进了引擎（`da4dbc9` 实测）

本文件只记录**实测读数**，不含推测。所有数字都来自桥的 `state` / `humaninput` 回包。

## 结论速览

| 动作 | 控制位是否置上 | 引擎是否响应 | 判据 |
|---|---|---|---|
| `move left` | ✅ `flagLeft true` | ✅ **通** | 位移 −6.78 格 |
| `move right` | ✅ `flagRight true` | ✅ **通** | 位移 +7.64 格 |
| `jump` | ✅ `flagJump true` | ✅ **通** | 跳幅 6.12 格 |
| `move up` | ❌ `flagUp false` | ❌ 不动 | 8 帧 `y` 不变 |
| `move down` | ✅ `flagDown true` | ❌ 不动 | 8 帧 `y` 不变，`pulley true` |
| `hook`（抓钩） | — | ❌ **完全没发出** | `grapCount 0`、`proj -1` × 8 帧 |

**规律**：凡是被 `PostUpdateRunSpeeds` 里那次重写覆盖到的输入（left/right/jump）都通；
没被覆盖到的（up/down/hook）都不通。

---

## 1. 走路（通）

`move {dir:'right', ms:900}`，起点 `2104.88,231.38`：

```
x     2105.20  2106.14  2107.51  2109.50  2111.51
vx      0.88     1.76     2.56     2.88     2.64
flagR   true     true     true     true     false
aftR    true     true     true     true     false      ← Player.Update 跑完后仍为 true
```

**`aftR true` 是关键**：以前所有版本这里都是 `false`（写进去被引擎清掉，所以 `vx 0`）。
`da4dbc9` 在 `PostUpdateRunSpeeds` 里再写一次，写在了引擎真正读取之后。

**不同位置复现**：
- `2104.88`（脚下 `t2` 土）→ +7.64 格
- `2112.51`（脚下 `t19` 平台）→ 左 −6.78 格、右 +4.79 格
- 结论：**脚下是土还是平台不影响**，之前的差异是链路时通时断。

## 2. 跳跃（通）

`jump {ms:400}`，起点 `227.38`：

```
t0  y226.22  vy −4.61
t1  y225.07  vy −4.61
t2  y223.63  vy −4.61     ← 按住期间恒速上升
t3  y222.26  vy −3.81     ← 松开后减速
t4  y221.70  vy −2.61
t5  y221.26  vy −0.61     ← 顶点
t6  y221.44  vy +1.39
t7  y222.04  vy +2.99
t8  y223.35  vy +4.99
t9  y225.28  vy +6.99
t10 y227.28  vy +8.59
t11 y227.38  vy  0.00     ← 落回原格
```

**跳幅 6.12 格，全程约 0.66 秒。** 起跳初速 `vy = −4.61` 由引擎给出。

## 3. 上键（输入没置上）

`move {dir:'up', ms:1200}`，位置 `2113.12,231.38`（脚下 `t2` 土）：

```
y231.38  vy0.00  × 8 帧      完全不动
flagUp       **false**       ← controlUp 一次都没置上
回包 inputOnly true，note "上键已按下；当前无绳、无翅膀、未骑乘，原版不会产生垂直位移"
```

**`note` 是命令层按条件推出来的，不是引擎回执。** 命令层说“按了”，控制位是假。

已有怀疑（未验证）：`ApplyMovement()` 里有四处提前 `return`（`PathActive` / `TickWalk` /
`TickWalkReactive` / `TickRopeRelease`），任何一处命中，后面的 `RequestUp()` 都不执行。
`move` 开头虽然调了 `StopWalk("manual-move")`，但未必清掉 `WalkTarget` / `WalkX` 残留。

## 4. 下键（控制位置上了，引擎不动）

`move {dir:'down', ms:1500}`，站在绳格上（`feetType 213`、`onRopeReal true`）：

```
y224.87  vy0.00  pulley true  ropeCount 0  feetType 213  flagDown true  × 8 帧
```

**控制位、`pulley`、脚下绳格三者全部成立，`velocity.Y` 一步不动。**

值得注意的矛盾：`pulley true` 而 `ropeCount 0` —— 这两个在原版里由同一段逻辑算出，
身体覆盖 `224.87~227.5`、绳在 `222~227`，`ropeCount` 不该是 0。

## 5. 抓钩（连发射都没有）

`hook {x:2094, y:221}`（对空气按，只测按键是否出去）：

```
grapCountBefore 0
回包 {"distance":7,"equipHow":"already","grapCountBefore":0,"proj":-1,
      "note":"已发送真实抓钩输入；请在 untilTick 后回读 state.grapCount / hookdebug"}

y227.38  vy0.00  grap 0  × 8 帧
```

`grapCount` 恒 0、`proj` 恒 −1 → **钩头没有发射**。

---

## 修复建议（给改代码的人）

`PostUpdateRunSpeeds()` 里目前只有这一句：

```csharp
BridgeSystem.ApplyNativeControlFlags(Player);
```

而 `ApplyNativeControlFlags` **只处理 left / right / up / down / jump**，
**没有 `controlHook` / `controlUseItem` / `controlUseTile` / `controlMount`**。

建议补齐（同样只写输入标志、不碰 velocity / position / pulley）：

```csharp
player.controlHook     |= IntentHook;
player.controlUseItem  |= IntentUseItem;
player.controlUseTile  |= IntentUseTile;
player.controlMount    |= IntentMount;
```

并让 `hook` / `use` / `place` 命令设置对应的 `Intent*`。

另建议：
1. `ApplyMovement()` 的四个提前 `return` 在手动命令（`move`/`jump`/`climb`/`hook`）触发时先全清；
2. `state` 补 `afterCtrlUp` / `afterCtrlDown`（现在只有 left/right/jump）；
3. `mine` / `place` 改成**结果驱动**：按住直到回读 `hasTile` 变化，不要按 `ms` 时长判断。

---

## 精确移动（`moveto`）的实测参数

`moveto` 现在用原版左右键做闭环控制。桥只读 `position.X` 和 `velocity.X`，按实测的松手滑行距离提前释放方向键；引擎自己负责加速、摩擦、碰撞和最终停下。桥不写 `velocity`。

控制规则：

- 目标距离大于当前速度对应的刹车距离时，持续按目标方向；
- 剩余距离进入刹车距离后，松开左右键，让原版摩擦滑行；
- 如果越过目标或惯性方向背离目标，再按目标方向让原版减速和反向；
- 只有位置误差 ≤ 0.08 格且速度 ≤ 0.12 时才报告 `exact.stopReason=arrived`；连续 45 帧几乎不动则报告 `blocked`。

刹车距离来自真机查表，不能用一个固定的线性秒数：

```
轮询间隔     50 ms
满速         |vx| ≈ 3.5 格/秒
刹车距离     ≈ |vx| × 0.62 + 0.10
松手条件     剩余距离 <= 刹车距离
```

九组实测（`delta` = 目标格数，`relV` = 松手瞬间速度，`coast` = 惯性滑行格数）：

| delta | real | err | relV | coast |
|---|---|---|---|---|
| +2 | +1.98 | −0.02 | 1.94 | 0.71 |
| +4 | +3.77 | −0.23 | 2.71 | 1.32 |
| +6 | +6.15 | +0.15 | 3.48 | 2.12 |
| +8 | +8.13 | +0.13 | 3.47 | 2.09 |
| −2 | −1.98 | +0.02 | 1.94 | 0.71 |
| −4 | −3.78 | +0.22 | 2.71 | 1.32 |
| −6 | −6.14 | −0.14 | 3.48 | 2.11 |
| −8 | −8.05 | −0.05 | 3.57 | 2.22 |

`coast / relV` 比值随速度递增（0.366 → 0.487 → 0.609 → 0.622），
说明减速不是匀减速（每帧乘系数、低于阈值归零），**建议按 `relV` 分档查表，不要硬拟合线性公式**。
