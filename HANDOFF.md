# 交接说明（给接手的 AI / 人）

> 这份文档只写**仓库之外**、接手动不动就卡住的东西。代码能力看 `docs/capabilities.md`，
> 协议看 `PROTOCOL.md`，规则看 `AGENTS.md`，当前进度看 `STATUS.md` + `docs/playthrough.md`。

## 1. 环境（这台机器特有，仓库里没有）

| 项 | 值 |
|---|---|
| 游戏 | tModLoader 1.4.4.9，路径 `D:\STEAM\steamapps\common\tModLoader` |
| 启动 | `dotnet\dotnet.exe tModLoader.dll`（工作目录就是上面那个） |
| 桥端口 | `127.0.0.1:47777`（客户端） / `47778`（专用服务端，**只有 `save` 走这个**） |
| 编译源 | `%USERPROFILE%\Documents\My Games\Terraria\tModLoader\ModSources\DshTerrariaBridge` |
| 成品 | `%USERPROFILE%\Documents\My Games\Terraria\tModLoader\Mods\DshTerrariaBridge.tmod` |
| 只启用的 mod | `Mods/enabled.json` 里必须**只有** `DshTerrariaBridge`（别动它） |
| 游戏窗口 | 副屏 `-2560,-174 1707x1067`（截屏/录屏用这个区域） |

## 2. 改动 → 生效的完整流程（顺序不能错）

```bash
REPO="<本仓库>/DshTerrariaBridge"
SYNC="C:/Users/<你>/Documents/My Games/Terraria/tModLoader/ModSources/DshTerrariaBridge"

# 0) 先关游戏（tmod 被占用会打包成空壳）
# 1) 同步源码（别用 robocopy /MIR，返回码 1 时它其实没同步）
cp -f "$REPO"/*.cs "$SYNC"/
# 2) 编译
"C:/Users/<你>/AppData/Local/Microsoft/dotnet/dotnet.exe" build "$SYNC/DshTerrariaBridge.csproj" -c Release
# 3) 打包成 tmod（-eac 必须给，否则打出来是空壳）
cd "D:/STEAM/steamapps/common/tModLoader"
./dotnet/dotnet.exe tModLoader.dll -server -build "$SYNC" -eac "$SYNC/bin/Release/net8.0/DshTerrariaBridge.dll"
# 4) 确认 tmod 大小合理（正常 ~170KB；只有几百字节就是空壳）
# 5) 启动游戏，等 3~4 分钟加载，再用客户端连 47777
```

**踩过的坑**：`-eac` 漏了会打空壳；`ModSources` 没同步会装旧 dll；`enabled.json` 被改会启用一堆别的 mod。

## 3. 连游戏的最小客户端

仓库里没有（属于本机脚本）。一个能用的实现就 20 行：

```js
// 协议是「一行一个 JSON」，请求必须两层：{cmd, params}
const sock = net.connect(47777, '127.0.0.1');
sock.write(JSON.stringify({ cmd: 'state', params: {} }) + '\n');
// 回包也是信封：{ok, ms, data} 或 {ok:false, error} —— 取 .data 才是命令结果
```

**这个坑害过很久**：把参数平铺在顶层（`{cmd:'move', dir:'right'}`）时，
服务端读到 `params` 为空 → 所有字段走默认值 → 表现为“命令回 ok 但完全没效果”。
`AGENTS.md` §7.5 有记录。

## 4. 当前状态（交接时的真实情况）

- **能玩**：走、跳、挖、放、探图、开箱取物、造 NPC 房、搭柱/平台跨地形、喝药。
- **已知缺陷**：**爬绳（`climb`）还没做成纯输入**。见下。
- 主线进度：东边走到海滩尽头算探索完毕；西边卡在 1840 一带；装备还没升级到能打 Boss。

### 爬绳这个未解决项（接手第一件事）

- 现象：`climb`/`move up` 在绳子上**不动**，`state.dbgCtrlUp` 恒 `false`。
- 结论：`Player.controlUp` 在 `Player.Update` 内部被重置回 `false`，我在 `PostUpdateInput`/`ModPlayer.PreUpdate` 设都没用。
- 已试过：`MonoModHooks.Modify(typeof(Player).GetMethod("Update", ...))` 前置注入 `controlUp = true` —— **注入没生效**（`dbgCtrlUp` 仍 false）。`PulleyMovement` 这个方法在 tModLoader.dll 里**不存在**，别去钩它。
- 参考：`BridgeSystem.cs` 的 `ForceClimbInput` / `InstallClimbHook` / `ClimbHookLog`，以及 `state` 的 `dbgCtrlUp`/`dbgPulley`/`dbgRopeTile` 三个诊断字段。
- **注意**：`Player.velocity` / `Player.position` / `Player.pulley` / `Main.chest` 这类“直接写状态”的写法**全部已被删除**（`AGENTS.md` §0.55）。修爬绳必须走输入层，不能再用直接赋速度的老办法。

## 5. 硬规矩（违反过的都写在这）

1. **只能有真操作**：像“按右键开箱子”那样，只给输入标志，结果由引擎算。不许直接写 velocity / position / pulley。
2. **任何“打开了/挖掉了/放下了”都必须回读证据**，命令回 `ok` 不算。
3. **不许未经允许重启游戏**；改 C# 需要重启，先问。
4. 世界只能通过**真实物品流**改变（选槽 → 瞄准 → 使用 → 回读）。
5. 文档不写世界名/角色名/单局坐标。
