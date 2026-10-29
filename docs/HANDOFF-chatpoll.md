# 游戏聊天框读取（chatpoll）坏了 —— 交接单

## 一句话

`chatpoll` 要么把一句话记成几百条（旧版），要么一条都读不到（我改的中间版）。
现在代码里是第三版（状态机），**还没打包验证**，游戏已关闭。

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
| 我改的第 2 版（当前代码） | 状态机：每帧 `cur = chatText.Trim()`，非空就存进 `ChatPending`，等它变空时把 `ChatPending` 入队 | **未验证**（游戏已关，没打包） |

## 当前代码位置

### 1. 字段（`BridgeSystem.cs:913-927`）

```csharp
private static readonly object ChatLock = new object();
private static readonly List<string> ChatInbox = new List<string>();
private static readonly List<string> ChatSent = new List<string>();
// 输入框每变一次就会被看到一次，所以不能"看到就记"。
// ChatPending 保存"当前这一版最长的输入"，等输入框变空时把它当成一条消息收下。
private static string ChatPending = "";
private static string ChatLastText = "";
private static ulong ChatLastTextTick;
// 上限与"丢了多少"：ChatPoll 的 since 是绝对序号，丢了旧的就要把序号补回去，
// 否则调用方手里的 since 会指向错位的内容。
private const int ChatInboxCap = 500;
private static int ChatDropped;
internal static bool ChatHookInstalled;
internal static string ChatHookError = "";
private static bool ChatHookTried;
```

### 2. Hook（`BridgeSystem.cs:929-977`）

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
            try
            {
                string cur = (Main.chatText ?? "").Trim();
                if (cur.Length > 0)
                {
                    ChatPending = cur;
                }
                else if (ChatPending.Length > 0)
                {
                    string text = ChatPending;
                    ChatPending = "";
                    ulong now = Main.GameUpdateCount;
                    bool sameAgain = text == ChatLastText && now - ChatLastTextTick < 30;
                    if (!sameAgain)
                    {
                        lock (ChatLock)
                        {
                            ChatInbox.Add(text);
                            while (ChatInbox.Count > ChatInboxCap)
                            {
                                ChatInbox.RemoveAt(0);
                                ChatDropped++;
                            }
                        }
                        ChatLastText = text;
                        ChatLastTextTick = now;
                    }
                }
            }
            catch { }
            orig();
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

## 还没查清的两件事（给改的人）

1. **`Main.chatRelease` 到底什么时候为真？** 我推断它常态为真（否则上升沿不会一次都触发不了），但没有证据。用 `ilspycmd` 反编译 `Terraria.Main` 搜 `chatRelease` 的赋值点能确认。

2. **`Main.DoUpdate_HandleChat` 是不是只在聊天框打开时每帧调用？** 如果是，那"输入框变空"那一帧之后就再也不调用这个函数了，状态机第 2 版会永远等不到"变空"。这是当前版本最大的风险点。

   验证办法：临时在 hook 里加 `ChatHookCalls++` 计数器，回包带上。聊天框关着时数字涨不涨，一试就知道。

## 正确的方向（如果状态机也不成）

不要靠 hook 猜"发送时刻"，改成**比较输入框内容的两个连续帧**：

```csharp
string now_ = Main.chatText ?? "";
if (now_.Length > 0 && now_ != ChatPrevNonEmpty) ChatPrevNonEmpty = now_;   // 记录最长版本
if (now_.Length == 0 && ChatPrevNonEmpty.Length > 0) { 入队(ChatPrevNonEmpty); ChatPrevNonEmpty = ""; }
ChatPrev = now_;
```

本质和现在的状态机一样，区别是**不依赖 hook 被调用的频率**——只要还在调就能收敛。

## 编译 / 部署

```bash
export DOTNET_ROOT="C:/Users/联想/AppData/Local/Microsoft/dotnet"
cp -f DshTerrariaBridge/BridgeSystem.cs DshTerrariaBridge/Commands.cs \
  "C:/Users/联想/Documents/My Games/Terraria/tModLoader/ModSources/DshTerrariaBridge/"
cd "C:/Users/联想/Documents/My Games/Terraria/tModLoader/ModSources/DshTerrariaBridge"
"$DOTNET_ROOT/dotnet.exe" build
```

**必须先把游戏关干净**，否则报 `TML003: Please close tModLoader`。
进程名不是 `tModLoader`（是 `dotnet`），要按命令行判：

```powershell
Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match 'tModLoader\.dll' }
```

打包成功后 `.tmod` 会自动出现在
`C:/Users/联想/Documents/My Games/Terraria/tModLoader/Mods/DshTerrariaBridge.tmod`，
看时间戳和体积确认是新包（上一版 243202 字节 / 00:14）。

## 验收

1. 进游戏，在聊天框打一句完整的话（比如「你能说话吗」）发出去
2. `node scripts/bridge.mjs chatpoll --params '{"since":0,"max":100}'`
3. **期望：`lines` 里只有一条完整句，`pending: 0`，`dropped: 0`**
4. 出错时先看回包里的 `pendingText`：非空说明它以为你还在打字、没收到"发送"信号
