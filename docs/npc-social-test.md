# NPC 社交能力验证说明

## `talk` 参数

`talk {npc}` 的 `npc` 参数是 NPC 类型。附近同类型 NPC 中，桥选择距离最近者；距离超过命令限制时拒绝。

## 真操作判据

`talk` 不直接写 `talkNPC`、商店索引或聊天文本。它只发送一次瞄准 NPC 的真实右键输入，结果由原版界面逻辑结算。

调用后要轮询 `state`，只有同时看到目标 `talkNPC` 和非空 `npcChatText` 才算对话成功。商店是否打开用 `npcShopIndex` 和商店货架回读确认。

## 失败排查

- `talkInput.finalMouseX/finalMouseY`：按当前相机换算出的最终游戏屏幕坐标。
- `talkInput.rawMouseX/rawMouseY`：交给 `PlayerInput.SetZoom_MouseInWorld` 的原始坐标。
- `talkInput.screenX/screenY/zoom`：这次命中计算使用的相机位置和世界缩放。
- `npcChatRelease`：原版右键边沿状态。
- `talkInput.phase`：先释放一帧，再发送按下边沿，避免沿用上一轮右键状态。
- 如果台词为空，先读 `state.mouseTile` 和 `state.talkInput`，确认最终鼠标世界格落在 NPC 碰撞框内，再检查 NPC 是否仍在附近。
