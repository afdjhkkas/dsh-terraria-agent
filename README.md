# DSH Terraria Agent：给 DS 的操作指南

这份文档是给 DeepSeek 使用的 Terraria 工具说明。插件通过本机 tModLoader Mod 提供 terraria_* 工具。所有工具结果都是字符串化的 JSON；读取结果时先按 JSON 理解，再决定下一步。

## 工作前提

- 桥接固定监听 127.0.0.1:7777。
- 用户需要自己选择人物并进入世界。DS 不负责选择人物或进入世界。
- 世界坐标使用 tile 坐标，1 tile = 16 像素；x 向右增加，y 向下增加。
- terraria_status 能检查桥是否启动；terraria_observe 能检查是否已经进世界。
- 移动、跳跃、使用物品、攻击、背包和传送需要本地玩家；世界扫描、读方块和部分世界编辑只需要世界已加载。
- 同一个 Agent 的请求会串行执行。不要并行发送互相依赖的动作。

## 推荐的操作循环

每次行动都按以下顺序执行：

1. 先调用 terraria_observe，确认位置、生命、死亡状态、手持物品和当前攻击状态。
2. 需要地形信息时调用 terraria_scan；需要精确方块信息时调用 terraria_tile。
3. 需要目标时调用 terraria_npcs，从返回的 who 选择 NPC。
4. 调用一个短时动作。
5. 再调用 terraria_observe 或 terraria_scan 确认结果。

移动建议每次 250～1000 ms。遇到坑、边缘、水或敌怪时，先停止并重新观察，不要连续发送很长的移动命令。动作失控、切屏暂停或需要立即清除保持状态时调用 terraria_stop。

## 工具目录

### 状态和观察

#### terraria_status({})

检查桥接是否可达，以及 Terraria 是否加载了世界。没有进入世界时也可以调用。

#### terraria_observe({})

读取当前状态。重点字段：

- inWorld：是否在世界中。
- world：世界名、宽高、昼夜、时间、下雨、血月、日食、困难模式。
- player.tile：玩家中心所在的 tile 坐标。
- player.pos、player.velocity：位置和速度。
- player.life、player.lifeMax、player.mana、player.manaMax。
- player.dead：死亡时不要继续发送移动、使用或攻击。
- player.selected：当前背包槽位。
- player.held：当前物品的类型、名称和数量。
- player.itemAnimation：使用物品或挥刀时可观察到动画变化。
- attack：攻击任务是否仍在运行、目标和命中次数。

### 地形读取

#### terraria_scan({x?, y?, radius?, walls?})

扫描以 x、y 为中心的方形区域。省略 x、y 时使用玩家位置；没有本地玩家时使用出生点。

- radius 默认 12，实际限制为 1～48。
- walls 默认 true。
- walls=true 时每格字段依次是 [type, wall, liquid, shape]。
- walls=false 时每格字段依次是 [type, liquid]。
- 返回值包含 x0、y0、w、h、stride、fields、tiles。
- tiles 是按行展开的数组，不能把每个数字当成一个独立方块；要按照 stride 和 fields 解码。
- type=0 表示没有方块；liquid 范围是 0～255。

示例：

~~~json
{"radius":12,"walls":true}
~~~

#### terraria_tile({x, y})

读取一个世界 tile。x、y 必填且必须是非负整数。返回方块类型和名称、墙、液体、斜坡、半砖、执行器以及帧坐标。先用 terraria_scan 找到目标，再用它确认精确状态。

### 玩家动作

#### terraria_move({direction, ms?})

短暂保持一个方向键。

- direction 必填：left、right、up、down。
- ms 默认 500，实际限制为 1～5000。
- 新方向会清除相反方向的保持状态。
- 这是短时控制，不是路径规划；每次移动后重新观察。

示例：

~~~json
{"direction":"right","ms":500}
~~~

#### terraria_jump({ms?})

短暂保持跳跃。

- ms 默认 150，实际限制为 1～2000。
- 跳跃不保证跨过任意宽度的坑；需要结合短距离移动和观察。

#### terraria_use_item({slot?, right?, ms?})

使用当前物品，也可以先选择背包槽位。

- slot 可选；有效槽位会被选中。先调用 terraria_inventory 确认槽位。
- right 默认 false；true 执行右键/物块交互。
- ms 默认 220，实际限制为 1～2000。
- 使用后应通过 terraria_observe 检查 held、itemAnimation 和位置变化。

#### terraria_stop({})

清除插件发出的移动、跳跃、使用物品和攻击保持状态。遇到异常或需要切换动作时优先调用。

### NPC 和战斗

#### terraria_npcs({radius?})

列出玩家附近的活动 NPC。

- radius 默认 80，实际限制为 1～200 tile。
- 返回每个 NPC 的 who、type、name、tile、life、lifeMax、friendly、boss、hostile。
- terraria_attack 的 who 应来自这里的返回值。

#### terraria_attack({who?, radius?, durationMs?, intervalMs?, slot?})

启动持续攻击任务，工具会立即返回，攻击在游戏更新循环中继续执行。

- who 可选；省略时自动选择半径内最近的可攻击敌怪。
- radius 默认 40，实际限制为 1～200 tile。
- durationMs 默认 3000，实际限制为 100～15000 ms。
- intervalMs 默认 180，实际限制为 30～2000 ms。
- slot 可选，用于先选择武器槽位。
- 当前手持物品必须有伤害，否则会报错。
- 指定 who 必须仍然存在并且是可攻击敌怪。
- 攻击过程中用 terraria_observe 查看 attack.active；需要立即停止时调用 terraria_stop。

推荐流程：

~~~text
terraria_inventory({})
terraria_npcs({"radius":40})
terraria_attack({"who": NPC_WHO, "slot": WEAPON_SLOT, "durationMs":2500})
terraria_observe({})
~~~

#### terraria_spawn_npc({type?, x?, y?})

生成 NPC，仅用于测试或创造模式操作。

- type 默认 1，应使用 Terraria NPC 类型 ID。
- x、y 省略时生成在玩家右侧约 5 格。
- 这是管理员/创造操作，不要在普通探索中随意调用。

### 背包、世界编辑和传送

#### terraria_inventory({})

返回当前选中槽位，以及所有非空背包物品。每项包含 slot、hotbar、type、name、stack、prefix、rare。

#### terraria_mine({x, y, noItem?})

直接移除指定 tile 的方块和墙。

- x、y 必填，必须在世界范围内。
- noItem 默认 false；设为 true 时抑制掉落。
- 空格会返回 ok=false，不代表桥失败。
- 这是世界编辑操作，调用前先用 terraria_tile 或 terraria_scan 确认坐标。

#### terraria_place({x, y, type, style?})

在指定位置调用 Terraria 的放置规则。

- x、y、type 必填；style 默认 0。
- type 是 Terraria tile 类型 ID，不是物品名称。
- 工具直接操作世界，不模拟鼠标，也不负责选择背包物品；将其视为管理员/世界编辑操作。
- 返回 ok、requested 和 after，放置失败时检查 after。

#### terraria_teleport({x, y})

把本地玩家传送到 tile 坐标。

- x、y 必填，可以是数值坐标。
- 不会自动检查安全落点；可能传到墙、岩浆、空中或敌怪旁边。
- 传送后立即调用 terraria_observe，必要时调用 terraria_scan。

#### terraria_give_item({type, stack?})

直接把物品生成到本地玩家背包中，是创造/管理员操作。

- type 必填，是 Terraria item 类型 ID。
- stack 默认 1，实际限制为 1～9999。
- 物品名称和数量以返回值为准。

#### terraria_craft({item, times?})

按物品类型 ID 合成物品。

- item 必填，是产出物品的类型 ID。
- times 默认 1。插件参数允许到 99，但 Mod 最终最多执行 20 次；建议按 20 或更小的批次调用。
- 需要材料和配方要求的工作站，工作站搜索范围是玩家周围约 5 tile。
- 含“任意木材/任意矿石”等配方组的配方暂不支持。
- 材料不足或附近没有工作站时可能返回部分成功，例如 crafted 和 reason。背包放不下产物时，Mod 会把产物掉落到玩家附近的世界中。

### 聊天

#### terraria_chat({text})

在本地 Terraria 客户端的聊天/提示区域显示文字。

- text 必填，插件最多保留 400 个字符。
- 空白消息会报错。
- 这是本地客户端提示，不是远程服务器广播接口。

## 错误处理

- Terraria bridge is not running：确认 Mod 已启用并且监听 127.0.0.1:7777。
- not in a world：等待用户进入世界，再调用 terraria_observe。
- no local player：当前实例没有本地玩家；玩家动作只能在单人客户端世界执行。
- 玩家已死亡：先停止动作并等待用户复活，不要继续发送攻击或移动。
- out of world bounds：重新调用 terraria_observe 获取世界尺寸，修正坐标。
- 没有可攻击的敌怪：先扩大 terraria_npcs 的搜索范围，确认 hostile=true 的 NPC。
- 当前选中的物品不能造成伤害：调用 terraria_inventory 找到武器槽位后，再传 slot 给 terraria_attack。

## 安装提示

用户自己进入世界后，DS 只需使用上面的 terraria_* 工具，不需要直接连接 TCP，也不需要调用内部命令名 ping/state/move 等。

安装游戏侧 Mod：

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-mod.ps1
~~~

安装 DSH 插件：

~~~powershell
dsh plugin --profile desktop add "D:\deepseek harness\官方版\text\dsh-terraria-agent"
~~~

插件连接 127.0.0.1:7777。进入世界后，第一组调用应是：

~~~text
terraria_status({})
terraria_observe({})
~~~
