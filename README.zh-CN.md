# DSH Terraria Agent

这个项目把 `dsh-mc-agent` 的"DSH 工具插件 + 游戏侧桥接"结构移植到泰拉瑞亚。
当前版本 **0.2.0**，桥接协议见 [`PROTOCOL.md`](PROTOCOL.md)（以它为准，本文只做导览）。

## 目录结构

| 路径 | 作用 |
| --- | --- |
| `index.js` | DSH 插件：注册 `terraria_*` 工具，并把每个 Agent 的调用串成一条队列（`makeSerialRunner`） |
| `src/bridge-client.js` | 本机 JSON 行协议客户端：每个请求一条 socket，带 `req` 关联、超时、取消信号和 `stopBestEffort` |
| `src/tool-def.mjs` | 工具定义与入参校验（宿主 `@deepseek-ai/dsh-tools` 优先，取不到用内置等价实现） |
| `DshTerrariaBridge/` | tModLoader Mod 源码。游戏状态只在 Terraria 主线程读写 |
| `artifacts/DshTerrariaBridge.tmod` | 用本机 tModLoader 打包好的 Mod，可直接安装 |
| `cordis.patch.yml` | DSH bundle 挂载行 + 默认 `config`（host / port / timeoutMs） |
| `install-mod.ps1` | 把 `.tmod` 装进本机 tModLoader 的 Mods 目录（带校验和备份） |
| `build-bridge-roslyn.ps1` | 用 tModLoader 自带 Roslyn 做一次快速"能否编译"检查（要求 PowerShell 7+） |
| `PROTOCOL.md` | 桥接协议 v0.2.0 的权威定义 |
| `test/` | `node --test` 测试（客户端协议、工具定义、插件装配） |

## 环境要求

- **Node.js 20 或更新**。运行时代码只用到 Node 20 就有的能力；`npm test` 用 `node --test`，
  不需要 `--test-isolation` 这类新版本才有的参数。
- **只在构建 C# Mod 时**需要 .NET 8 SDK（`%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe`）
  和目标版本的 tModLoader 安装。
- `install-mod.ps1`：Windows PowerShell 5.1 即可。
- `build-bridge-roslyn.ps1`：**PowerShell 7+ 且运行在 .NET 8 上**。Windows PowerShell 5.1 跑在
  .NET Framework 4.x，加载不了 tModLoader `Libraries` 下面向 net8.0 的 Roslyn 程序集；
  脚本会在开头就说明这一点并给出 `.NET 8 SDK` 的替代命令，而不是抛 `ReflectionTypeLoadException`。

## 安装

### 1. 安装游戏侧 Mod

先**完全关闭 tModLoader 和 Terraria**（运行中的游戏锁住 `.tmod`，复制会失败，脚本会检测并提示）：

```powershell
cd "<本仓库绝对路径>"
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-mod.ps1
```

脚本会：校验 `.tmod` 文件头（`TMOD` 魔数，防止装进一个没打完包的残件）→ 备份当前已安装版本到
`artifacts\DshTerrariaBridge.previous-installed.tmod`（已在 `.gitignore` 里）→ 复制到
`%USERPROFILE%\Documents\My Games\Terraria\tModLoader\Mods\` → 打印 SHA-256。内容完全相同则直接跳过。

也可以手工把 `artifacts/DshTerrariaBridge.tmod` 复制到同一目录。之后在 tModLoader 的模组管理器启用
**DSH Terraria Bridge**，进入单人世界后桥才开始监听 `127.0.0.1:47777`。

回滚上一版：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-mod.ps1 `
  -Artifact artifacts\DshTerrariaBridge.previous-installed.tmod -NoBackup
```

### 2. 安装 DSH 插件

用 DSH 自带的插件安装器，传入本仓库的**绝对路径**（不是随便写的路径，也不用手工编辑 profile）：

```powershell
dsh plugin --profile <profile> add "<本仓库绝对路径>"
```

`--profile` 用实际使用的 profile 名（桌面端就是 DSH Desktop 用的那个）。安装后重启 DSH。
`cordis.patch.yml` 会自动把插件挂到 profile 上。移除：

```powershell
dsh plugin --profile <profile> remove dsh-terraria-agent
```

### 3. 连接配置

默认值写在 `cordis.patch.yml` 的 `config` 里，`apply(ctx, rawConfig)` 收到的就是这份对象：

```yaml
config:
  host: 127.0.0.1     # 只允许回环地址，其它值会直接拒绝启动
  port: 47777          # 必须与 Mod 侧 BridgeSystem.Port 一致
  timeoutMs: 5000     # 单次请求超时
```

改完重启 DSH。桥只监听回环地址，不接受局域网或公网连接。

## 工具与协议

18 个工具：`terraria_status`、`terraria_observe`、`terraria_scan`、`terraria_tile`、
`terraria_inventory`、`terraria_npcs`、`terraria_move`、`terraria_jump`、`terraria_use_item`、
`terraria_attack`、`terraria_stop`、`terraria_spawn_npc`、`terraria_mine`、`terraria_place`、
`terraria_teleport`、`terraria_craft`、`terraria_give_item`、`terraria_chat`。

对应的桥命令：`ping`、`state`、`scan`、`tile`、`inventory`、`npc`、`move`、`jump`、`use`、
`attack`、`stop`、`spawn_npc`、`mine`、`place`、`teleport`、`craft`、`give`、`chat`、`help`。

协议要点（细节全在 `PROTOCOL.md`）：

- 传输：TCP `127.0.0.1:47777`，一行一个 UTF-8 JSON；请求 `{"cmd","params"}`（客户端另带一个
  `req` 关联号），成功 `{"ok":true,"data"}`，失败 `{"ok":false,"error"}`。
- **`{"ok":false}` 是正常的游戏侧拒绝**（越界、暂停中、材料不足……），说明命令没有生效，
  不代表桥坏了，也不该立刻重连重试。
- **方块 `type` 用 `-1` 表示空**；`0` 是泥土块 `TileID.Dirt`（v0.2.0 修正了"0 当空气"的旧约定）。
- `scan` 有 `format: rows|ascii|rle`；`rows` 的 `tiles` 是"行的数组"，
  第 `j` 行第 `i` 格在 `tiles[j][i*stride .. i*stride+stride-1]`。
- 暂停（`Main.gamePaused`）时 `move`/`jump`/`use`/`attack` 会被拒绝；`state`/`ping` 会给出
  `paused`、`hasFocus`。

给模型看的完整用法（工具参数、解码示例、token 成本、错误表）在 [`README.md`](README.md)。

## 构建 Mod

把 `DshTerrariaBridge/` 放到 tModLoader 的 `ModSources` 下（本机是
`%USERPROFILE%\Documents\My Games\Terraria\tModLoader\ModSources\DshTerrariaBridge`），
然后用 .NET 8 SDK 构建——**这是产出 `artifacts/DshTerrariaBridge.tmod` 的实际方式**：

```powershell
cd "$env:USERPROFILE\Documents\My Games\Terraria\tModLoader\ModSources\DshTerrariaBridge"
& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" build -c Release
```

`dotnet build` 除了编译，还会跑 tModLoader 自己的 `BuildMod` target
（`tMLMod.targets` 里 `dotnet tModLoader.dll -build <projectDir> -eac <TargetPath>`），
把 `DshTerrariaBridge.tmod` 打包进
`%USERPROFILE%\Documents\My Games\Terraria\tModLoader\Mods\`。把这个文件复制成
`artifacts/DshTerrariaBridge.tmod`，再运行 `install-mod.ps1`。

`build-bridge-roslyn.ps1` 只是快速编译检查（只产出 DLL，不打包 `.tmod`），且要求
PowerShell 7+ / .NET 8：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\build-bridge-roslyn.ps1 `
  -TModLoaderPath "D:\STEAM\steamapps\common\tModLoader"
```

工程按本机 tModLoader 1.4.4 分支和 .NET 8 配置；不要把它当作原版 Terraria 1.4.5 的直接 Mod 工程。
换用其他 tModLoader 版本时需要重新编译并重新测试。

## 测试

```powershell
npm test          # node --test，跑 test/ 下全部用例
npm run pack:check  # npm pack --dry-run，检查发布包内容
```

`npm pack` 出来的包里包含 `index.js`、`src/`、`test/`、`PROTOCOL.md`、C# 源码、
`artifacts/DshTerrariaBridge.tmod` 和两个 PowerShell 脚本，所以用户拿到包后可以直接
`npm test`、`npm run install:mod`。

## 当前边界

- 目标是**本机单人客户端**；世界修改、背包、使用物品和传送都在本地执行，多人客户端会与服务器不同步，
  专用服务器实例没有本地玩家。
- 世界读取和方块操作在桥的主线程执行；主线程 1.5 s 内没能执行的请求会返回明确的错误，
  有副作用的命令看到这个错误时应先核对实际后果再决定是否重试。
- 移动是桥直接改速度：没有击退/冲刺/坐骑/冰面惯性保真；`up`/`down` 是动力飞行。
- `attack` 是桥驱动的命中（视线、真实伤害/暴击/攻速），不是真实的武器挥击，也不耗弹药/魔力。
- `artifacts/` 里的 `.tmod` 必须与源码同为 0.2.0 协议；只换一边会出现"旧 Mod + 新插件"的错配
  （`type` 编码、`scan` 格式、`attack` 结算都会对不上）。

## 版本记录

- **0.2.0**：方块编码 `-1` = 空（`0` = 泥土）；`scan` 增加 `format`；`move`/`jump`/`attack`
  增加 `wait` 与结算返回；`attack` 改为视线 + 真实伤害并保留 `attack.last`；`craft` 支持配方组与
  配方条件；`mine` 默认不再挖墙（`walls` 参数）；`give`/`place`/`spawn_npc` 校验类型 ID；
  `teleport` 拒绝越界并推出实心方块；`chat` 截断到 400 字符并回报 `truncated`；
  `ping`/`state` 增加 `paused`/`hasFocus`；`cordis.patch.yml` 真正下发配置；引擎要求改为 Node ≥ 20。