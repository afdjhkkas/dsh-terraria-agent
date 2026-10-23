# DSH Terraria Agent

这个项目把 `dsh-mc-agent` 的“DSH 工具插件 + 游戏侧桥接”结构移植到泰拉瑞亚：

- `index.js`：DSH 插件，注册 Terraria 状态、观察、扫描、挖掘、放置、移动、背包、合成等工具。
- `src/bridge-client.js`：本机 JSON 行协议客户端，带超时、取消和每个 Agent 的串行队列。
- `DshTerrariaBridge/`：tModLoader Mod 源码。游戏状态只在 Terraria 主线程读取和修改。
- `artifacts/DshTerrariaBridge.tmod`：当前机器已经编译出的 Mod，可直接复制到 Mods 目录。

## 安装

### 1. 安装游戏侧 Mod

在项目目录运行下面的 PowerShell 脚本。脚本默认使用当前 Windows 用户的“文档”目录，并在覆盖旧 Mod 前留下带时间戳的备份：

```powershell
cd "D:\deepseek harness\官方版\text\dsh-terraria-agent"
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-mod.ps1
```

也可以手工把 `artifacts/DshTerrariaBridge.tmod` 复制到：

   `文档/My Games/Terraria/tModLoader/Mods/`

在 tModLoader 的模组管理器启用 **DSH Terraria Bridge**。启动或重载 Mod 后进入单人世界，桥才会监听 `127.0.0.1:7777`。

### 2. 安装 DSH 插件

把本项目目录加入 DSH 的 `desktop` profile。`cordis.patch.yml` 会自动把插件挂载到 profile，不需要手工编辑 profile 配置：

```powershell
dsh plugin --profile desktop add "D:\deepseek harness\官方版\text\dsh-terraria-agent"
```

安装后重启 DSH。若使用其他 profile，把 `desktop` 替换成实际 profile 名称。要移除插件：

```powershell
dsh plugin --profile desktop remove dsh-terraria-agent
```

固定连接 `127.0.0.1:7777`。桥接 Mod 当前固定监听这个端口：

   ```json
   { "host": "127.0.0.1", "port": 7777, "timeoutMs": 5000 }
   ```

桥只监听回环地址，不接受局域网或公网连接。DSH 启动后可先调用 `terraria_status`；返回桥可达后，再调用 `terraria_observe` 确认已经进入世界。

## 协议

每个 TCP 连接使用 UTF-8 JSON 行协议。请求形如：

```json
{"cmd":"tile","params":{"x":100,"y":80}}
```

成功响应：

```json
{"ok":true,"data":{"x":100,"y":80}}
```

失败响应：

```json
{"ok":false,"error":"..."}
```

命令包括 `ping`、`state`、`scan`、`tile`、`inventory`、`npc`、`move`、`jump`、`use`、`stop`、`mine`、`place`、`teleport`、`craft`、`give` 和 `chat`。

## 构建 Mod

把 `DshTerrariaBridge/` 放到 tModLoader 的 `ModSources` 下，用对应版本的 tModLoader 构建工具编译。项目按当前机器的 tModLoader 1.4.4 分支和 .NET 8 配置；不要把它当作原版 Terraria 1.4.5 的直接 Mod 工程。

当前 `artifacts/DshTerrariaBridge.tmod` 已用本机 tModLoader 2026.8.3.0 的构建器从仓库内最新 C# 源码重新生成。换用其他 tModLoader 版本时，应重新编译并重新测试。

`build-bridge-roslyn.ps1` 只负责用本机 tModLoader 引用编译 `DshTerrariaBridge.dll`；`.tmod` 打包仍应交给目标 tModLoader 的 ModSources 构建器完成。脚本支持 `-TModLoaderPath` 或 `TML_PATH`，并建议使用 PowerShell 7（`pwsh`），因为本机 tModLoader 的 Roslyn 引用面向 .NET 8：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\build-bridge-roslyn.ps1 `
  -TModLoaderPath "D:\STEAM\steamapps\common\tModLoader"
```

构建出新的 `.tmod` 后替换 `artifacts/DshTerrariaBridge.tmod`，再运行 `install-mod.ps1`。

## 当前边界

- 目标是本机单人客户端；玩家移动、背包、使用物品和传送依赖本地玩家。
- 世界读取和方块操作在桥的主线程执行。
- 超过 1.5 秒没有轮到主线程的请求会过期并被跳过，避免超时后重复执行挖掘、放置等有副作用命令。
- 已验证 Node 客户端的协议、错误和取消路径，并完成 Mod 的源码编译和 `.tmod` 打包；尚未在本次会话中启动 Terraria 做完整游戏内回归测试。

## Node 测试

在本目录执行：

```powershell
npm test
```

检查发布包内容（不生成实际压缩包）：

```powershell
npm run pack:check
```

