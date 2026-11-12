# 探索能力增强 - 2026-11-11

## 背景

用户反馈：AI 探索时"遇到坑不会跳，遇到悬空阻碍不会判断要搭高过去而是直接走进洞里"，根本原因是缺少**环境感知 + 路径规划能力**。

## 本次改动

### 1. ✅ scan 命令（已存在，确认可用）

**位置**：`Commands.cs` 第 964 行

**功能**：扫描玩家周围指定范围的 tile 数据
- 参数：`r`（半径，1-48格）、`format`（rows/ascii/rle）、`walls`（是否包含墙）
- 返回：地形数组（type/wall/liquid/liqType/shape）

**用途**：AI 可以提前"看到"前方地形，判断是坑还是墙、多深多高。

---

### 2. ✅ path/walkto 平台支持（已存在，确认可用）

**位置**：`BridgeSystem.cs` 第 1751 行

**现状**：`TileStand` 函数已包含 `TileID.Sets.Platforms[ty]`，平台被识别为可站立地面。

**A* 寻路能力**：
- 支持跳跃（水平 1-4 格、上升 0-6 格）
- 支持落差（向下 1-14 格）
- 支持爬高（台阶 1-2 格自动上、3+ 格需跳）
- 支持平台穿越

---

### 3. ✅ probe 命令（新增）

**位置**：`Commands.cs` 第 3265 行

**功能**：试探性移动预判，模拟向某方向移动 N 格会不会掉坑/撞墙，**不实际移动**

**参数**：
```json
{
  "cmd": "probe",
  "dir": "left|right",  // 方向
  "steps": 5            // 试探步数（1-50）
}
```

**返回**：
```json
{
  "from": {"x": 100, "y": 50},  // 起点
  "dir": "right",
  "steps": 10,                   // 请求步数
  "reachedSteps": 7,             // 实际能走几步
  "safe": false,                 // 是否安全
  "issue": "pit_5_deep"          // 问题类型：blocked | pit_N_deep | pit_deep | none
}
```

**用途**：AI 可以先问"往右走 10 格安全吗"，再决定要不要真走或搭桥。

**issue 类型说明**：
- `none` — 安全，可以走
- `blocked` — 撞墙（身体通不过）
- `pit_3_deep` — 有坑，深度 3 格（≤3 格可跳，>3 格需搭桥）
- `pit_deep` — 极深坑（>14 格，按摔伤处理，不当通路）

---

### 4. ✅ WebSocket 实时推送（新增）

**位置**：`WebSocketServer.cs`（新文件）

**监听地址**：`ws://127.0.0.1:47778`

**推送事件**：
- `connected` — 连接成功
- `player_state` — 玩家状态变化（血量/蓝量/位置/buff）

**变化检测规则**：
- 血量/蓝量：值变化即推送
- 位置：移动超过 0.5 格即推送
- Buff：数量变化即推送（附带完整 buff 列表）

**客户端示例**：`examples/websocket-client.js`

**用途**：
- 战斗时实时监控血量（Boss 战、PvP）
- 移动反馈（检测卡死、掉落）
- Buff 监控（药水时效、Debuff 预警）

**注意**：WebSocket 推送**不能替代** scan/probe 做地形判断——它只推玩家自身状态，看不到前方地形。

---

## 探索能力对比

### 改动前：

```
AI: walkto (x+20)
→ 直接莽，撞墙/掉坑才知道
→ 卡住后只能轮询 state 看位置，判断"是不是没动"
```

### 改动后：

```
AI: probe (dir=right, steps=20)
→ 返回 {safe: false, issue: "pit_5_deep", reachedSteps: 8}
→ AI: "有 5 格深的坑，需要搭桥"
→ craft platform → place → 再 walkto
```

或者：

```
AI: scan (r=15)
→ 返回前方地形数组
→ AI 分析："前方 10 格处有 3 格高台阶"
→ probe (dir=right, steps=10)
→ 返回 {safe: false, issue: "blocked", reachedSteps: 9}
→ AI: "跳一下就能上去" → jump → walkto
```

---

## 示例代码

### probe 命令用法
见 `examples/probe-usage.js`

### WebSocket 客户端
见 `examples/websocket-client.js`

### 完整探索策略
见 `examples/exploration-strategy.js` — 演示如何组合 scan + probe + path

---

## 测试验证

### 源码锁
新增 2 条源码锁（`test/source-locks.test.js`）：
1. `probe 命令必须存在并只读预判，不实际移动`
2. `WebSocket 推送必须在 PostUpdateEverything 里调用`

全部 20 条源码锁通过 ✓

### 单元测试
- 总计 423 项测试
- 通过 418 项 ✓
- 失败 2 项（环境权限问题，与本次改动无关）
- 跳过 3 项

### 能力清单
- `docs/capabilities.json` 已重新生成（96 命令）
- `docs/kb/_generated-commands.md` 已同步

---

## 架构说明

### 为什么不是"AI 一键探索"？

当前设计是**低层提供原语，高层组合策略**：
- `scan` — 感知地形
- `probe` — 预判风险
- `path` — 执行移动
- **组合逻辑** — 由 AI 层（外部调用方）实现

**优点**：
- 灵活：不同场景（探索/战斗/建造）可以有不同策略
- 可调试：每一步都是显式命令，能看到 AI 的决策过程
- 可扩展：加新策略不用改 mod 代码

**缺点**：
- 需要 AI 自己写探索逻辑（scan → 判断 → craft/jump/walkto）
- 命令往返有延迟（虽然 probe 比真走快）

### 实时推送的定位

WebSocket 推送**不是**地形感知的替代品，它只推**玩家自身状态**：
- ✓ 血量掉了 → 推送（用于战斗决策）
- ✓ 位置变了 → 推送（用于移动反馈）
- ✗ 前方有坑 → **不推送**（必须主动 scan/probe）

---

## 后续优化方向

1. **NPC/怪物事件推送** — Boss 出现、稀有怪刷新、NPC 对话触发
2. **物品掉落推送** — 地上有稀有物品时通知
3. **危险预警** — 即将溺水、即将摔死、岩浆接近
4. **probe 支持向上/向下** — 当前只支持 left/right，可以加 up/down
5. **高层探索策略库** — 在 npm 包里提供常用探索模式（直线探索、螺旋探索、地图填充）

---

## 修改文件清单

### 新增文件：
- `DshTerrariaBridge/WebSocketServer.cs` — WebSocket 服务端
- `examples/websocket-client.js` — WebSocket 客户端示例
- `examples/probe-usage.js` — probe 命令示例
- `examples/exploration-strategy.js` — 探索策略示例
- `CHANGELOG-exploration.md` — 本文档

### 修改文件：
- `DshTerrariaBridge/Commands.cs` — 添加 probe 命令（第 62 行 + 第 3265 行）
- `DshTerrariaBridge/BridgeSystem.cs` — 启动 WebSocket 服务 + 每帧推送（第 165 行 + 第 1000 行）
- `DshTerrariaBridge/BridgeSystem.cs` — PathBodyFree/PathFloorAt 改为 internal（第 1756/1771 行）
- `test/source-locks.test.js` — 新增 2 条源码锁（第 264 行起）
- `docs/capabilities.json` — 重新生成（96 命令）
- `docs/kb/_generated-commands.md` — 重新生成（96 命令）

---

## 结论

**问题**："遇到坑不会跳，遇到悬空阻碍直接走进洞里"

**根因**：缺少环境感知 + 预判能力

**解决方案**：
1. `scan` — 已存在，可扫描地形
2. `probe` — 新增，可预判移动风险
3. `path` — 已完善，支持平台 + 跳跃 + 爬高
4. WebSocket — 新增，实时推送状态变化

**下一步**：AI 层（外部调用方）需要实现探索策略，组合这些原语。示例见 `examples/exploration-strategy.js`。
