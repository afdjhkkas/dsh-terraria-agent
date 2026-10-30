# DSH AI Mini（测试版，未合并进主桥）

目标：不依赖外部 DSH，在游戏内配模型 + 对话，并把模型绑定到本机角色或队友 Bot。

当前已包含 HTTP 异步队列、模型配置、对话面板、知识库注入和 Bot 生命周期；Bot 是否加入世界只看桥的 `ping.inWorld` 回读。
本机角色和 Bot 共用 `bridge` 工具，它把 `DshTerrariaBridge/Commands.cs` 的完整命令集按标准两层报文转发出去，能力和 `docs/kb` 保持同源；模型仍必须遵守真操作规则并以回读确认结果。

## 当前进度

| 步 | 内容 | 状态 |
|---|---|---|
| 0 | 游戏内发 HTTP，主线程不卡 | 已完成 |
| 1 | 配置面板（官方/第三方模型、key、地址） | 已完成 |
| 1.5 | 模型 / Bot 页面（选择模型、操控我、添加队友 Bot） | 已完成；Bot 使用真实第二客户端，状态必须到“已进世界” |
| 2 | 泰拉瑞亚专项提示词 + 知识库注入 | 已完成 |
| 3 | 历史对话面板 | 已完成 |
| 4 | 日常对话走游戏聊天框 | 已完成 |

## 怎么验对话与 Bot

进游戏后：

```
1. 在“配置”页填 API 地址、密钥和模型，点“测试连接”。
2. 在“对话”页输入问题，回车或点“发送”；网络回调在主线程排空，失败会显示具体原因。
3. 在“模型 / Bot”页点“+ Bot”，填写本机已有角色名；只有状态变为“已进世界”才算加入成功。
4. 选中已进世界的 Bot 后，可在聊天中让它先调用 `players` 找队友，再调用 `follow` 跟随或护卫。
```

Bot 的“启动中”或“桥已连接，等待进世界”不能当成成功；模型工具只有在 `ping.inWorld=true` 后才会执行。
Bot 进程使用非 shell、无窗口创建方式启动，避免启动瞬间激活/抢占用户当前游戏窗口；不使用桌面抢焦点或操作系统输入注入。

### 完整能力入口

`bridge` 的参数形状固定为：

```json
{"command":"state","params":{}}
```

`command` 可以是主桥命令清单中的任意命令。参数必须嵌套在 `params`，不能平铺；动作命令返回后要继续读取 `state`、`tile`、`inventory`、`shop` 或相应诊断字段确认引擎实际结果。本机调用也经 TCP `127.0.0.1:47777` 走同一协议，因此不会出现“本机一套能力、Bot 另一套能力”的分叉。

## 为什么单独一个 mod

主桥（`DshTerrariaBridge`）已经在用，改坏了要重来。新东西先自己编译、自己验，
验通了再决定要不要并进去。目录独立性就是保险丝。

## 模型与队友绑定

打开面板后切到“模型 / Bot”：左侧选模型，点“操控我”把模型绑定到当前客户端玩家，或点“+ Bot”创建并启动一个队友条目。队友状态依次显示“启动中 → 桥已连接，等待进世界 → 已进世界”；只有最后一个状态才会把工具调用发给 Bot。添加 Bot 不会自动切换当前操控对象，点对应行的“使用”才切换。

Bot 由真实的第二个 tModLoader 客户端通过原生 `-j/-plr/-pass` 参数入服；tModLoader 会在模组加载完成后调用 `Main.AutoJoin`。不能通过本客户端直接写 `Main.player[其他索引]` 代替。只有 Bot 桥回读 `ping.inWorld=true` 才算真正加入世界。

## 文件

| 文件 | 作用 |
|---|---|
| `AiHttp.cs` | 投递 → 后台 worker → 结果队列 → 主线程 drain。**这是全部风险所在** |
| `AiTest.cs` | F8 发 / F9 读；`PostUpdateEverything` 里每帧 `TryTake` |
| `DshAiMini.cs` | Mod 入口 |

## 部署

```powershell
Copy-Item ai-mod/*.cs, ai-mod/*.csproj, ai-mod/build.txt `
  "C:/Users/联想/Documents/My Games/Terraria/tModLoader/ModSources/DshAiMini/"
Copy-Item tModLoader.targets `
  "C:/Users/联想/Documents/My Games/Terraria/tModLoader/ModSources/tModLoader.targets"
cd "C:/Users/联想/Documents/My Games/Terraria/tModLoader/ModSources/DshAiMini"
$env:TMODLOADER_DIR = "<你的 tModLoader 目录>"
dotnet build -c Release --no-incremental -p:TModLoaderPath=$env:TMODLOADER_DIR
# 编译会自动用 tMLMod.targets 打包，产物落到 Mods/DshAiMini.tmod
```

Windows PowerShell 可先执行 `$env:TMODLOADER_DIR = '你的 tModLoader 目录'`，再运行上面的
`dotnet build`；也可以直接安装仓库 `artifacts/DshAiMini.tmod`，无需在本机编译源码。

`enabled.json` 打包时会自动加进 `DshAiMini`，不用手改。
