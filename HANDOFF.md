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
| 只启用的 mod | `Mods/enabled.json` 里是 `["DshTerrariaBridge","DshAiMini","InputMethodFixer"]`（**只增不减**，别覆盖） |
| 第二个 mod | `ai-mod/`（`DshAiMini`，单机版 AI 面板），编译目录 `ModSources/DshAiMini`，**必须 `rm -f` 掉根目录的 `Knowledge.cs`/`SystemPrompt.cs`**（会和 `prompts/` 里的同名类撞 `CS0101`） |
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

## 4. 当前状态（2026-11-03 交接）

- **能玩且已真机验证**：走（连贯走 8.86 格/1000ms 无停顿）、跳（单跳 7.34 格 / 三段 20.44 格）、爬绳（`move up` 升 17.9 格、绳顶直接 `jump` 升 9.80 格）、挖（`mine` 回包带 `before.hasTile`，挖空自动收手）、放（`place` 水平 ±2~3 格 / 垂直 ±4 格，高处要跳着放）、开箱取物（两侧对账）、NPC 社交五步（右键开对话 → `talkoption` 点按钮 → `shop` 读货架 → `buy` 回读入包 → `key inventory` 一键双关）、搭柱/平台跨地形、喝药、抓钩、坐骑快捷召唤、翅膀/多段跳。
- **主线进度**：还没到打 Boss 的阶段。已在做的具体任务见 `STATUS.md`。
- **两个 mod**：`DshTerrariaBridge`（执行层，95 个命令）+ `DshAiMini`（单机版 AI 面板，`ai-mod/`，不依赖 DSH/node/桥）。

### 接手第一件事（按顺序）

1. **补完重启的最后一步**：`artifacts/` 拷包 → 清 `Mods/ai-mod.tmod` → `enabled.json` 写三件套 → 启动 → `qc.mjs` 的 `ping` 验证桥活（`Get-NetTCPConnection`/`netstat` 在这个 bash 里看不到 47777，别信它）。
2. **真机验 `mine` 的新回包**：`before:{hasTile,type,frameX}` 有没有值、内层 `ok:false` 会不会被抛回来。
3. **继续造 NPC 房子**：`roomcheck ok:true` 才算合格（`housediag` 的 solid 判定不可信）。

### 三条必须记住的接口事实

- **读箱子内容前必须 `player.openChest == 索引`**，否则 `Main.chest[].item` 是空数组（会误判"空箱"）。
- `npc` 回包里每个 NPC 的坐标字段叫 **`tile`**，不是 `x`/`y`。
- `scan` 的地图在 **`map` 换行字符串**里，不是 `tiles` 二维数组。

## 5. 硬规矩（违反过的都写在这）

1. **只能有真操作**：像"按右键开箱子"那样，只给输入标志，结果由引擎算。不许直接写 velocity / position / pulley。
2. **任何"打开了/挖掉了/放下了"都必须回读证据**，命令回 `ok` 不算。
3. **不许未经允许重启游戏**；改 C# 需要重启，先问。
4. 世界只能通过**真实物品流**改变（选槽 → 瞄准 → 使用 → 回读）。
5. 文档不写世界名/角色名/单局坐标。
6. **回包先看 `ok`，再看 `data`** —— `ok:false` 时根本没有 `data`；`armed:true` 只代表"开始挥"不代表"挖掉了"（`AGENTS.md` §0.58）。
