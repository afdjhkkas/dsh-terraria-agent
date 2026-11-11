# 项目总目标（Project Goal）

**让接入这个项目的 AI 以「玩家」的身份把 Terraria 打到通关。**

不是"能走能挖"的演示工具，而是**会玩**的 agent：自己合装备、盖房招 NPC、挖矿升级、
按进度打 Boss，直到**击败月亮领主（Moon Lord）**。

这个世界/角色/模式由接入时的运行环境决定，本项目不假设、也不写死任何存档信息。

## 项目要向 AI 提供什么

两样东西，缺一不可：

1. **泰拉瑞亚知识** —— [`docs/terraria-mechanics.md`](docs/terraria-mechanics.md)：
   坐标/方块编码、物理与移动、放置与支撑、房屋与 NPC、战斗与装备、暂停与失焦、
   以及从木头到月亮领主的路线；每条都标注了是 wiki 还是实测。
2. **最终目标与不变量** —— 本文。AI 读完就该知道"为什么操作"和"什么算完成"。

## 当前阶段（2026-10-25 起）

用户修正了推进顺序：**先能力，后通关**。

1. 先把「和人一起玩」需要的能力做齐、做实（真实物品流，见
   [`docs/capability-roadmap.md`](docs/capability-roadmap.md)）。
2. 然后用户进入世界当队友，用指令驱动我完成任务。
3. 通关仍是长期目标，但**不再靠我单方面硬推**。

工具契约见 [`README.md`](README.md) 与 [`PROTOCOL.md`](PROTOCOL.md)。

## 通关的判定标准

- 终局事件完成：击败 `Moon Lord`，世界状态可验证（该 Boss 已死、掉落可查）。
- 全程**不用**世界编辑作弊：不得用 `admin:true` 修改世界来"假装完成"任何一步。
- 进度链条可审计：每个里程碑都能给出回读证据（`tile` / `inventory` / `state` / `npc`）。

## 不变量（任何时候都不能破坏）

1. **像玩家一样操作**：用对应工具与材料、在够得着的距离内、遵守支撑与放置规则、消耗真实物品。
   **改变世界只能走游戏自己的物品使用路径**——直接写地图（`WorldGen.PlaceTile(forced:true)`、
   直接 `PlaceWall`）产生的格子重载后会被游戏丢弃，等于白做。
   `admin:true` 只用于测试桥本身，不能用来推进进度，产出也不能当成"完成"。
2. **回读验证**：每个动作之后读回真实状态，不允许"发出去就当成功"；
   失败要区分「桥的 bug」与「游戏规则拒绝」。
3. **后台可协作**：AI 的操作不抢焦点、不动用户鼠标，用户可以在前台同时游玩。
4. **源码与产物一致**：改 `DshTerrariaBridge/` 就要重新打包并让 `artifacts/*.tmod` 与源码一致；
   `npm test` 必须通过。
5. **诚实**：做不到就说做不到，并说明卡在哪一层（协议 / 桥 / 游戏规则 / 世界条件）。

## 进度记录

AI 在运行中自行维护一份进度日志（里程碑、证据、失败与修正），
模板与格式见 [`docs/playthrough.md`](docs/playthrough.md)；
它属于运行产物，不属于项目的固定内容。

## 机器可读进度（[`goal-progress.json`](goal-progress.json)）

上面那些是给人读的。**"现在该做什么"必须落成机器读得懂的数据**，否则每新开一个对话
都要从头判断一遍——这是过去 96 条命令都没解决的那个问题。

进度表存在仓库根 `goal-progress.json`，AI 用只读命令 `intent` 读它：

```bash
npm run bridge intent            # 或 {"cmd":"intent","params":{}}
```

`intent` 回 `stage` / `goal` / `missing[{id,label,have,need}]` / `blockedBy` / `suggestedNext`，
就是"当前阶段、还差什么、下一步干什么"。`mine` / `place` / `craft` 加 `{"guard":true}` 会先问它一句。

### 文件结构

```jsonc
{
  "stage": "wood",                    // 当前阶段 key
  "stageOrder": ["wood", "iron_gear", "eye_of_cthulhu", "evil_boss"],
  "stages": {
    "wood": {
      "label": "木头阶段",
      "goal": "做工作台 + 火把，攒够 20 木材",
      "checks": [                     // 完成判据：背包数量
        {"id": 9,  "label": "木材",   "need": 20},
        {"id": 36, "label": "工作台", "need": 1},
        {"id": 8,  "label": "火把",   "need": 5}
      ],
      "blocked": ["木材靠砍树（axe 或镐都行）", "火把要 木材×1 + 凝胶×1"],
      "next": "砍树到 20 木材 → 做工作台 → 做火把",
      "guard": {"strict": false, "world": true}   // 见下
    }
  }
}
```

- 物品 id 必须是真实的 `ItemID` 数值，查 [`docs/kb/_generated-ids.md`](docs/kb/_generated-ids.md)（不要整读，用 grep）。
- `checks[].anyOf: [70, 1331]` 表示"任意一个够数就算"，例如蠕虫诱饵/血腥脊椎二选一。
- `guard.strict: true` —— 这一阶段的 `craft` 目标必须出现在 checks 里，否则 `guard:true` 会拦。
- `guard.world: false` —— 这一阶段不该动土（例如"只差一件合成品去打 Boss"），`mine`/`place` 会拦。
- `guard` 缺省或 `strict:false` + `world:true` = 什么都不拦。

### 谁维护它

**AI 自己**。`intent` 判据全满足时会沿 `stageOrder` 在内存里推进一格（回包 `advanced` 列出来），
但**不写文件**——进度落盘要 AI 自己动手：把 `stage` 改成新 key，然后把新阶段的
`checks` / `blocked` / `next` 按实际情况写准。

规矩和 §0.65 一致：**验证通过就当场落盘**。做完一件事（合出铁砧、打通 Boss）就更新这个文件并提交，
不要攒到最后一起写，否则下次开局读到的是过期进度。