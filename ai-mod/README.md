# DSH AI Mini（测试版，未合并进主桥）

目标：不依赖外部 DSH，在游戏内配模型 + 对话。

**现在只做到第 0 步：证明「mod 里能发 HTTP 且不卡游戏」。**
这一步不成立，后面的面板、提示词、历史对话都没有意义。

## 当前进度

| 步 | 内容 | 状态 |
|---|---|---|
| 0 | 游戏内发 HTTP，主线程不卡 | 代码已编译（`DshAiMini.tmod` 15,704 字节），**待真机验证** |
| 1 | 配置面板（官方/第三方模型、key、地址） | 未开始 |
| 2 | 泰拉瑞亚专项提示词 + 知识库注入 | 未开始 |
| 3 | 历史对话面板 | 未开始 |
| 4 | 日常对话走游戏聊天框 | 未开始 |

## 怎么验第 0 步

进游戏后：

```
F8  → 发一个 POST 到 https://httpbin.org/post
F9  → 把上一次的结果打到聊天框
```

判据（三条全过才算这一步成立）：

1. **F8 之后游戏不卡**：能正常走路、动作不断帧。
2. **F9 能读到结果**：`[AI] HTTP 200 / xxxms（主线程上过了 N 帧）: {...}`
3. **`N` 不能是 0**：说明主线程确实在等网络的那段时间里继续跑了帧。

如果第 1 条不过（明显卡顿），说明 `AiHttp` 的线程边界有问题，要改成本地进程而不是直接 `HttpClient`。

## 为什么单独一个 mod

主桥（`DshTerrariaBridge`）已经在用，改坏了要重来。新东西先自己编译、自己验，
验通了再决定要不要并进去。目录独立性就是保险丝。

## 文件

| 文件 | 作用 |
|---|---|
| `AiHttp.cs` | 投递 → 后台 worker → 结果队列 → 主线程 drain。**这是全部风险所在** |
| `AiTest.cs` | F8 发 / F9 读；`PostUpdateEverything` 里每帧 `TryTake` |
| `DshAiMini.cs` | Mod 入口 |

## 部署

```bash
cp -f ai-mod/*.cs ai-mod/*.csproj ai-mod/build.txt \
  "C:/Users/联想/Documents/My Games/Terraria/tModLoader/ModSources/DshAiMini/"
cd "C:/Users/联想/Documents/My Games/Terraria/tModLoader/ModSources/DshAiMini"
dotnet build -c Release --no-incremental
# 编译会自动用 tMLMod.targets 打包，产物落到 Mods/DshAiMini.tmod
```

`enabled.json` 打包时会自动加进 `DshAiMini`，不用手改。
