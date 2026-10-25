# 硬规则（违反任何一条，这次改动就是作废的）

这些规则不是建议。用户已经因为同一条被违反而纠正过多次，每一条后面都写着它是怎么被
违反的、以及正确做法是什么。

## 1. 世界只能通过真实操作改变（最高优先级）

**禁止**任何"直接把方块写进/删出世界"的调用。具体禁用的 API：

| 禁用 | 曾经怎么犯的 |
|---|---|
| `WorldGen.PlaceTile` / `WorldGen.PlaceObject` | 放熔炉时图省事直接调 `PlaceObject`，被用户当场点名"走命令是假操作"；`fill` 和 `travel` 的铺地板也是这个（一边 `PlaceTile` 一边手动 `stack--`，等于凭空造方块） |
| `WorldGen.KillTile` / `WorldGen.KillWall` | 挖矿用 `KillTile` 绕过硬度和镐力判定；`mine` / `wall` 还留了 `admin:true` 分支直接删格子，`travel`/`walkto` 的砍树也用 `KillTile` |
| `Main.tile[x,y].type = ...` | 直接改瓦片类型 |
| `Player.PickTile(...)` 当成"挖掘" | 它是即时结算伤害，不是挥镐：画面上角色不动、没有挥动动作。用户看到的就是"你挖东西连镐子都不掏" |
| `WorldGen.RangeFrame` 单独用来"补家具 frame" | 家具本体是靠 `PlaceTile` 塞进去的 |

**唯一允许的做法**：让游戏自己的物品使用逻辑去改世界。

```
选中对应物品（pl.selectedItem = 槽位）
    → SetAim(tileX, tileY)          // 同时写 Main.mouseX/Y 和 Player.tileTargetX/Y
    → UseUntil = Now + ms           // PreItemCheck 里合成 controlUseItem
    → 等原版 ItemCheck 跑完
    → 回读 tile 确认，不确认就重试或报失败
```

这条路径在 `BridgeSystem.ApplyAim()` / `ApplySyntheticInput()` / `PreItemCheck()` 里已经
实现好了，包一层就是 `BridgeSystem.BeginItemUse(slot, tileX, tileY, ms)`（`mine` / `place` /
`fill` / `wall` / `travel` / `walkto` / `removeblock` 现在全部走它）。不要写第二条路。

注意 `Main.SmartCursorEnabled` 会被 `BeginItemUse` 强制关掉：智能光标会在原版 `ItemCheck`
里重算 `tileTargetX/Y`，把目标格改到旁边一格（实测请求 (x,y) 挖到了邻居）。

物品必须真的存在、真的被消耗；没有工具就应该失败，而不是绕过工具。

## 2. 不许把"命令生效"当成"操作发生"

每条结论都要有回读证据：`tile` / `inventory` / `state` 的真实返回值。
说"挖掉了"就要有该格 `hasTile:false`；说"放下了"就要有该格 `type==预期`；
说"捡到了"就要有背包里数量增加。没有回读就不要写"完成"。

## 3. 不许虚报

不确定就说不知道。工具返回看不懂就停下来读，不要用"大概是……"继续往下做。

## 4. 文档保持通用

`GOAL.md` / `docs/**` / `README*` 里不出现具体世界名、角色名、存档路径。

## 5. 机器相关的脚本不进仓库

`C:\Windows\Temp\trtest\` 下的东西（坐标、窗口句柄、显示器布局）不进 git。

## 6. 开发流程

- 每次提交前跑 `npm test`。
- 改了 mod 代码要重新打包才知道是否真的能用；"编译通过"不等于"能用"。
- 一条规则被违反过，就把违反方式写进本文件对应的行里，不要只写"要真实操作"这种空话。