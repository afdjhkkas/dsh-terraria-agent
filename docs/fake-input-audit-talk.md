# NPC `talk` 的真实输入审计

`talk` 曾经直接写 `Player.talkNPC` 和 `Main.npcShop`，回包看起来成功，但 `Main.npcChatText` 为空，游戏没有经过原版对话路径。这属于假操作。

原版流程是：准星命中 NPC → `Main.mouseRight && Main.npcChatRelease` → `Main.HoverOverNPCs` 调用 `SetTalkNPC`、`SetNPCShopIndex(0)` 和 `NPC.GetChat()`。桥现在只设置 NPC 目标、缩放前后的鼠标坐标和右键输入时间窗，让原版完成这些结果。

验证必须在命令返回后轮询 `state`：

- `talkNPC` 等于目标 NPC 的 `whoAmI`；
- `npcChatText` 非空；
- `npcShopIndex` 由原版选择，不能把 NPC 类型当成商店数组下标。

命令回包里的 `armed`、`untilTick` 只证明输入已排队，不证明对话已经发生。若 NPC 不在准星命中范围、界面被其他 UI 截获或动作超时，必须报告未完成。
