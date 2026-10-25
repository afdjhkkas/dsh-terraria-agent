# 硬规则（违反任何一条，这次改动就是作废的）

> **总原则（用户原话，必须严格遵守）：控制角色完成任务，而不是通过指令改变世界。**
> 违反这条，整个项目就没有意义。任何"让世界变成我想要的样子"的写法都是错的；
> 唯一正确的写法是"让角色做出一个玩家也会做的动作，然后由游戏自己改变世界"。
>
> 由此推出的两条推论：
> 1. 判断标准是**屏幕上的角色像不像真人在做这件事**，不是方块最终变没变。
>    挖矿必须有完整的挥镐动作，放置必须先掏方块；动画被打断、角色不抬手，就是错的。
> 2. 出生点周围 100 格不挖（用户已两次被从出生点掉进水里/坑里）。


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

这一版 tModLoader 没有可写的智能光标开关（`Main` 只有只读的 `SmartCursorWanted` /
`SmartCursorIsUsed`，写 `SmartCursorEnabled` 会编译失败 CS0117）。准星落点靠
`Main.mouseX/Y` + `Player.tileTargetX/Y` 直接压住，实测回读的格子就是传入的那一格。

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