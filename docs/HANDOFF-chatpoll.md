# 游戏聊天框读取（chatpoll）——交接单

## 一句话

`chatpoll` 已改为镜像原版真实发送分支，并完成 Release 编译与带 `-eac` 的打包。
本次没有启动或重启游戏；仍需在游戏里发一条普通消息做最终验收。

## 涉及的文件（两个位置，内容必须一致）

```
仓库:   D:\deepseek harness\官方版\work\dsh-terraria-agent-repo\DshTerrariaBridge\BridgeSystem.cs
        D:\deepseek harness\官方版\work\dsh-terraria-agent-repo\DshTerrariaBridge\Commands.cs
编译目录: C:\Users\联想\Documents\My Games\Terraria\tModLoader\ModSources\DshTerrariaBridge\
         （改仓库不会自动同步，必须 cp 过去再 dotnet build）
```

## 症状时间线

| 版本 | 做法 | 结果 |
|---|---|---|
| 旧版（已证实有问题） | 在 hook 里按电平看 `Main.chatRelease && Main.chatText` 非空就入队 | 玩家发「你能说话吗」攒出 725 条：`你`×92、`你能`×258、`你能看`×70、`你能看到`×102、`你能看到我`×144、`你能说话`×40、`你能说话吗`×19 |
| 我改的第 1 版 | 改成 `chatRelease` 的上升沿（`rel && !lastRel`） | **一条都收不到**（`lines: []`）。推测 `Main.chatRelease` 常态为真，真→真没有上升沿 |
| 当前版 | 在 `DoUpdate_HandleChat` hook 前保存文本和释放状态，调用 `orig()` 后按原版发送条件确认 | 已编译、已打包；待游戏内验收 |

## 当前代码位置

### 1. 字段（`BridgeSystem.cs:913-929`）

```csharp
private static readonly object ChatLock = new object();
private static readonly List<string> ChatInbox = new List<string>();
// ChatPending 只用于诊断当前输入框内容；它不承担“变空即发送”的判定。
private static string ChatPending = "";
private static ulong ChatPendingTick;
// 上限与"丢了多少"：ChatPoll 的 since 是绝对序号，丢了旧的就要把序号补回去，
// 否则调用方手里的 since 会指向错位的内容。
private const int ChatInboxCap = 500;
private static int ChatDropped;
private static ulong ChatHookCalls;
private static ulong ChatLastSendTick;
private static ulong ChatLastCancelTick;
private static string ChatLastEvent = "";
private static string ChatLastCandidate = "";
internal static bool ChatHookInstalled;
internal static string ChatHookError = "";
private static bool ChatHookTried;
```

### 2. Hook（`BridgeSystem.cs:931-1005`）

```csharp
internal static void InstallChatHook()
{
    if (ChatHookTried) return;
    ChatHookTried = true;
    try
    {
        var mi = typeof(Main).GetMethod("DoUpdate_HandleChat",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (mi == null) { ChatHookError = "找不到 Main.DoUpdate_HandleChat"; return; }
        Terraria.ModLoader.MonoModHooks.Add(mi, (Action<Action>)(orig =>
        {
            string beforeText = Main.chatText ?? "";
            string trimmed = beforeText.Trim();
            bool wasChatOpen = Main.drawingPlayerChat;
            bool releaseBefore = Main.chatRelease;
            lock (ChatLock)
            {
                ChatHookCalls++;
                if (wasChatOpen && trimmed.Length > 0)
                {
                    ChatPending = trimmed;
                    ChatPendingTick = Main.GameUpdateCount;
                }
            }

            orig();

            try
            {
                ulong now = Main.GameUpdateCount;
                bool sent = wasChatOpen && releaseBefore && Main.inputTextEnter &&
                    trimmed.Length > 0 && (Main.chatText ?? "").Length == 0;
                bool cancelled = wasChatOpen && !sent && Main.inputTextEscape;
                if (sent)
                {
                    bool command = trimmed.StartsWith("/", StringComparison.Ordinal);
                    lock (ChatLock)
                    {
                        ChatLastCandidate = trimmed;
                        ChatLastEvent = command ? "command" : "sent";
                        if (!command)
                        {
                            ChatInbox.Add(trimmed);
                            while (ChatInbox.Count > ChatInboxCap)
                            {
                                ChatInbox.RemoveAt(0);
                                ChatDropped++;
                            }
                            ChatLastSendTick = now;
                        }
                        ChatPending = "";
                        ChatPendingTick = 0;
                    }
                }
                else if (cancelled)
                {
                    lock (ChatLock)
                    {
                        ChatLastCandidate = trimmed;
                        ChatLastEvent = "cancel";
                        ChatLastCancelTick = now;
                        ChatPending = "";
                        ChatPendingTick = 0;
                    }
                }
            }
            catch { }
        }));
        ChatHookInstalled = true;
    }
    catch (Exception ex) { ChatHookError = ex.GetType().Name + ": " + ex.Message; }
}
```

### 3. 读取（`BridgeSystem.cs:984-1010`）

```csharp
internal static object ChatPoll(int since, int max)
{
    lock (ChatLock)
    {
        if (since < 0) since = 0;
        int rel = since - ChatDropped;   // 绝对序号 → 缓冲内下标
        if (rel < 0) rel = 0;
        if (rel > ChatInbox.Count) rel = ChatInbox.Count;
        int take = Math.Min(max, ChatInbox.Count - rel);
        var lines = ChatInbox.GetRange(rel, take);
        int next = ChatDropped + rel + take;
        return new
        {
            hooked = ChatHookInstalled,
            hookError = ChatHookError.Length > 0 ? ChatHookError : null,
            next = next,
            pending = ChatDropped + ChatInbox.Count - next,
            dropped = ChatDropped,
            pendingText = ChatPending,   // 诊断：非空 = 玩家正在打字还没发
            lines = lines.ToArray(),
        };
    }
}
```

### 4. 命令入口（`Commands.cs:117`）

```csharp
case "chatpoll":
    return Json.Ok(BridgeSystem.ChatPoll(Json.Int(p, "since", 0),
        Math.Clamp(Json.Int(p, "max", 20), 1, 500)));
```

## 已排除的旧方案

- 逐帧看到非空文本就入队：中文输入法会把一句话拆成大量中间态。
- `chatRelease` 上升沿：已实测收不到消息，不能作为独立发送信号。
- “输入框变空就算发送”：Esc 取消和原版处理时序会造成误报。

当前实现只在 `orig()` 返回后同时满足 `wasChatOpen && releaseBefore &&
Main.inputTextEnter && trimmed.Length > 0 && (Main.chatText ?? "").Length == 0` 时入队；
Esc 取消走 `lastEvent: "cancel"`，以 `/` 开头且由原版处理的命令走 `lastEvent: "command"`，都不会进入 `lines`。

## 编译 / 部署

```powershell
$repo = "D:\deepseek harness\官方版\work\dsh-terraria-agent-repo"
$sync = "C:\Users\联想\Documents\My Games\Terraria\tModLoader\ModSources\DshTerrariaBridge"
$dotnet = "C:\Users\联想\AppData\Local\Microsoft\dotnet\dotnet.exe"
$game = "D:\STEAM\steamapps\common\tModLoader"
Copy-Item "$repo\DshTerrariaBridge\*.cs" $sync -Force
& $dotnet build "$sync\DshTerrariaBridge.csproj" -c Release --no-incremental
Push-Location $game
& $dotnet tModLoader.dll -server -build $sync -eac "$sync\bin\Release\net8.0\DshTerrariaBridge.dll"
Pop-Location
```

**必须先把游戏关干净**，否则报 `TML003: Please close tModLoader`。
进程名不是 `tModLoader`（是 `dotnet`），要按命令行判：

```powershell
Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match 'tModLoader\.dll' }
```

打包成功后 `.tmod` 会自动出现在
`C:/Users/联想/Documents/My Games/Terraria/tModLoader/Mods/DshTerrariaBridge.tmod`，
必须检查体积大于 50 KB、前四字节为 `TMOD`，并把同一个包同步到仓库 `artifacts/`。
本次产物为 215792 字节，魔数 `TMOD`；游戏未启动或重启。

## 验收

1. 进游戏，在聊天框打一句完整的话（比如「你能说话吗」）发出去
2. `node scripts/bridge.mjs chatpoll --params '{"since":0,"max":100}'`
3. **期望：`lines` 里只有一条完整句，`pending: 0`，`dropped: 0`**
4. 出错时先看 `hooked/hookError/hookCalls`；输入中看 `pendingText`，取消或命令看 `lastEvent`。
