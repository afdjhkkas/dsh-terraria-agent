# 离线推演包（阶段①）

> 由 `node scripts/testcases.mjs --sim` 生成。每条用例的推演结论写回 `docs/testcases/sim-gaps.md`。
> 这个模式不连游戏、不发动作，只走路由 + 检索 + 五维自检 + 动作流推演。

共 54 条。

### M01（阶段 1 · move）

**任务**：向右连续走 10 秒，路上有 1 格台阶和 2 格坑，不能卡住

**路由**：

```
NEED_KNOWLEDGE:safety_limit,atomic_action,actor_behavior
```

**要读的库**（3 个）：safety_limit(1174字) / actor_behavior(1454字) / atomic_action(4194字)

**声明需要的命令**：move, state, scan
　全部在清单里

**备注**：走完要回读坐标，位移<8 格算卡住

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### M02（阶段 1 · move）

**任务**：原地起跳、连续跳、从 3 格高处落下，确认重力与落地判定

**路由**：

```
NEED_KNOWLEDGE:safety_limit,actor_behavior
```

**要读的库**（2 个）：safety_limit(1174字) / actor_behavior(1454字)

**声明需要的命令**：jump, state
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### M03（阶段 1 · move）

**任务**：在离家 150 格的地方向下挖一条竖井进洞穴，边挖边看地形

**路由**：

```
NEED_KNOWLEDGE:safety_limit,atomic_action,game_rule
```

**要读的库**（3 个）：safety_limit(1174字) / game_rule(4006字) / atomic_action(4194字)

**声明需要的命令**：mine, scan, tile, state
　全部在清单里

**备注**：必须逐格挖，不能用 travel down

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### M04（阶段 1 · move）

**任务**：依次走到海边、沙漠、丛林、雪原，每到一个记住坐标

**路由**：

```
NEED_KNOWLEDGE:safety_limit,world_biome
```

**要读的库**（2 个）：safety_limit(1174字) / world_biome(2411字)

**声明需要的命令**：travel, state, scan
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### M05（阶段 3 · move）

**任务**：一路下到地狱，识别熔岩区块并绕开，不要被岩浆烧死

**路由**：

```
NEED_KNOWLEDGE:safety_limit,world_biome
```

**要读的库**（2 个）：safety_limit(1174字) / world_biome(2411字)

**声明需要的命令**：travel, scan, tile, state
　全部在清单里

**备注**：需要熔岩免疫来源（护身符/黑曜石皮肤药水）

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### M06（阶段 3 · move）

**任务**：找到发光蘑菇地和微光池，记录形状与位置特征

**路由**：

```
NEED_KNOWLEDGE:safety_limit,world_biome,loot_source
```

**要读的库**（3 个）：safety_limit(1174字) / world_biome(2411字) / loot_source(1807字)

**声明需要的命令**：find, travel, scan
　全部在清单里

**已知缺的知识**：world_biome 没有微光池的分布规律（在哪一层、什么地形附近会出现）

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### M07（阶段 3 · move）

**任务**：打开大地图，读出已探索区域范围并定位自己在地图上的位置

**路由**：

```
NEED_KNOWLEDGE:safety_limit
```

**要读的库**（1 个）：safety_limit(1174字)

**声明需要的命令**：—

**已知缺的命令**：map

**已知缺的知识**：没有大地图：读不到已探索区域范围，也读不到自己在地图上被标在哪

**备注**：完全没有大地图读取能力

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### G01（阶段 1 · gather）

**任务**：徒手砍一棵树，捡起掉落的木材

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation,atomic_action
```

**要读的库**（3 个）：safety_limit(1174字) / atomic_action(4194字) / item_operation(3072字)

**声明需要的命令**：use, select, grab, inventory
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### G02（阶段 1 · gather）

**任务**：用镐挖 5 块石头和 5 块泥土，掉落物要真的进背包

**路由**：

```
NEED_KNOWLEDGE:safety_limit,atomic_action,item_operation
```

**要读的库**（3 个）：safety_limit(1174字) / atomic_action(4194字) / item_operation(3072字)

**声明需要的命令**：mine, grab, inventory, tile
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### G03（阶段 1 · gather）

**任务**：找一处铜矿或铁矿，挖下来并数清背包里多了几个

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：find, mine, grab, inventory
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### G04（阶段 3 · gather）

**任务**：挖狱石和黑曜石，需要先解决熔岩伤害

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：find, mine, grab, tile
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### G05（阶段 2 · gather）

**任务**：采几株月光草和太阳花，注意只在对应时间开花时能采

**路由**：

```
NEED_KNOWLEDGE:safety_limit,loot_source,game_rule
```

**要读的库**（3 个）：safety_limit(1174字) / game_rule(4006字) / loot_source(1807字)

**声明需要的命令**：find, mine, grab, state
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### G06（阶段 1 · gather）

**任务**：打碎一个陶罐，捡走里面的东西

**路由**：

```
NEED_KNOWLEDGE:safety_limit,loot_source
```

**要读的库**（2 个）：safety_limit(1174字) / loot_source(1807字)

**声明需要的命令**：mine, grab, inventory
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### G07（阶段 1 · gather）

**任务**：打开一个宝箱，把有用的拿走，不要全清空

**路由**：

```
NEED_KNOWLEDGE:safety_limit,loot_source
```

**要读的库**（2 个）：safety_limit(1174字) / loot_source(1807字)

**声明需要的命令**：chest, inventory, grab
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### G08（阶段 3 · gather）

**任务**：用空桶装一桶水和一桶蜂蜜

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：select, use, tile, inventory
　全部在清单里

**备注**：原版用桶装液体，理论上 use 就够，但要能在液体里瞄准

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### G09（阶段 1 · gather）

**任务**：把地上散落的钱币和材料都捡干净

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：grab, inventory, state
　全部在清单里

**备注**：拾取半径 4 格，超出要挪过去

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### B01（阶段 1 · build）

**任务**：铺一条 20 格长的石头平台，再接一段木墙

**路由**：

```
NEED_KNOWLEDGE:safety_limit,build_standard,atomic_action
```

**要读的库**（3 个）：safety_limit(1174字) / build_standard(1819字) / atomic_action(4194字)

**声明需要的命令**：place, wall, tile, inventory
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### B02（阶段 2 · build）

**任务**：照入住标准造一间 NPC 房：封闭空间、门、桌椅、光源

**路由**：

```
NEED_KNOWLEDGE:safety_limit,build_standard,npc_town
```

**要读的库**（3 个）：safety_limit(1174字) / npc_town(2175字) / build_standard(1819字)

**声明需要的命令**：place, wall, craft, homeroom, housing, housediag
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### B03（阶段 1 · build）

**任务**：铺一条 60 格长的横向通道，边铺边确认脚下不掉下去

**路由**：

```
NEED_KNOWLEDGE:safety_limit,atomic_action,build_standard
```

**要读的库**（3 个）：safety_limit(1174字) / build_standard(1819字) / atomic_action(4194字)

**声明需要的命令**：fill, place, tile, state
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### B04（阶段 1 · build）

**任务**：摆好火把、工作台、熔炉和一个箱子

**路由**：

```
NEED_KNOWLEDGE:safety_limit,build_standard,item_operation
```

**要读的库**（3 个）：safety_limit(1174字) / build_standard(1819字) / item_operation(3072字)

**声明需要的命令**：place, craft, chest, tile
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### B05（阶段 2 · build）

**任务**：搭两层平台并用梯子或绳子上下连通

**路由**：

```
NEED_KNOWLEDGE:safety_limit,build_standard
```

**要读的库**（2 个）：safety_limit(1174字) / build_standard(1819字)

**声明需要的命令**：place, climb, tile, state
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### B06（阶段 2 · build）

**任务**：把门、椅子、桌子摆到位置上，让房屋判定通过

**路由**：

```
NEED_KNOWLEDGE:safety_limit,build_standard,npc_town
```

**要读的库**（3 个）：safety_limit(1174字) / npc_town(2175字) / build_standard(1819字)

**声明需要的命令**：place, homeroom, housediag
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### K01（阶段 1 · craft）

**任务**：用木材做工作台、木剑、木镐

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：craft, recipes, inventory
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### K02（阶段 1 · craft）

**任务**：在工作台旁边做火把、木平台、木墙

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：craft, recipes, find
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### K03（阶段 1 · craft）

**任务**：在熔炉把矿石熔成金属锭

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：craft, recipes, inventory
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### K04（阶段 2 · craft）

**任务**：做一张虫网和一根木钓竿，需要哪些前置材料

**路由**：

```
NEED_KNOWLEDGE:safety_limit,loot_source,item_operation
```

**要读的库**（3 个）：safety_limit(1174字) / loot_source(1807字) / item_operation(3072字)

**声明需要的命令**：craft, recipes, inventory
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### K05（阶段 2 · craft）

**任务**：整理背包，把多余材料存进箱子

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：chest, inventory, drop
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### K06（阶段 2 · craft）

**任务**：把一组 99 个的方块拆成两半，丢掉不要的垃圾

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：drop, inventory
　全部在清单里

**已知缺的命令**：splitstack

**已知缺的知识**：item_operation 没有堆叠拆分规则

**备注**：原版是按住右键拆分，没有对应命令

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### K07（阶段 1 · craft）

**任务**：查配方判断当前材料能不能做出铁镐

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：recipes, inventory
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### N01（阶段 2 · npc）

**任务**：等向导入住，跟他对话，问某个物品怎么做

**路由**：

```
NEED_KNOWLEDGE:safety_limit,npc_town
```

**要读的库**（2 个）：safety_limit(1174字) / npc_town(2175字)

**声明需要的命令**：npc, talk, housing, homeroom
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### N02（阶段 2 · npc）

**任务**：找商人买一把铜镐和两瓶弱效治疗药水

**路由**：

```
NEED_KNOWLEDGE:safety_limit,npc_town,item_operation
```

**要读的库**（3 个）：safety_limit(1174字) / npc_town(2175字) / item_operation(3072字)

**声明需要的命令**：shops, shop, buy, npc
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### N03（阶段 2 · npc）

**任务**：受伤以后找护士回血

**路由**：

```
NEED_KNOWLEDGE:safety_limit,npc_town
```

**要读的库**（2 个）：safety_limit(1174字) / npc_town(2175字)

**声明需要的命令**：npc, heal, state
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### N04（阶段 3 · npc）

**任务**：接渔夫的钓鱼任务，钓到指定鱼后交给他

**路由**：

```
NEED_KNOWLEDGE:safety_limit
```

**要读的库**（1 个）：safety_limit(1174字)

**声明需要的命令**：npc, talk
　全部在清单里

**已知缺的命令**：quest

**已知缺的知识**：npc_town 只写了渔夫怎么出现，没有任务系统：任务进度、任务鱼种类、交付规则

**撞上明确做不到**：NPC 对话文本

**备注**：读不到任务进度与对话文本

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### N05（阶段 2 · npc）

**任务**：跟武器商、防具商买卖装备

**路由**：

```
NEED_KNOWLEDGE:safety_limit,npc_town,gear_route
```

**要读的库**（3 个）：safety_limit(1174字) / gear_route(2197字) / npc_town(2175字)

**声明需要的命令**：shops, shop, buy, sell, npc
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### N06（阶段 2 · npc）

**任务**：判断这间房为什么 NPC 不肯住，说清缺哪一条

**路由**：

```
NEED_KNOWLEDGE:safety_limit,npc_town,build_standard
```

**要读的库**（3 个）：safety_limit(1174字) / npc_town(2175字) / build_standard(1819字)

**声明需要的命令**：housing, housediag, roomcheck, tile
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### F01（阶段 2 · fish）

**任务**：装备钓竿，在水边抛竿，等浮标沉下去再收线

**路由**：

```
NEED_KNOWLEDGE:safety_limit,loot_source
```

**要读的库**（2 个）：safety_limit(1174字) / loot_source(1807字)

**声明需要的命令**：select, use, state
　全部在清单里

**已知缺的命令**：castline, reel

**已知缺的知识**：loot_source 只写了"需要钓竿+鱼饵、产物由群系深度决定"，缺动作流程与判定：怎么抛竿、咬钩的时机信号、收线、鱼饵每次消耗

**撞上明确做不到**：随机事件与时机窗口；通用弹幕状态

**备注**：抛竿/等待/收线是时序原语，use 只能点一下，也读不到浮标状态

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### F02（阶段 2 · fish）

**任务**：用虫网抓一只蝴蝶、一条蠕虫、一只青蛙

**路由**：

```
NEED_KNOWLEDGE:safety_limit,loot_source
```

**要读的库**（2 个）：safety_limit(1174字) / loot_source(1807字)

**声明需要的命令**：select, use, npc, inventory
　全部在清单里

**已知缺的知识**：loot_source 只提到松露虫，缺捕捉机制：虫网怎么用、哪些生物可捕、成功率

**撞上明确做不到**：生物 AI 内部状态

**备注**：能列出周围 NPC，但没有"等它进范围再挥网"的原语

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### F03（阶段 3 · fish）

**任务**：把抓到的小动物放进笼子里摆好

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation
```

**要读的库**（2 个）：safety_limit(1174字) / item_operation(3072字)

**声明需要的命令**：place, inventory
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### F04（阶段 3 · fish）

**任务**：钓一条任务鱼，交给渔夫完成任务闭环

**路由**：

```
NEED_KNOWLEDGE:safety_limit
```

**要读的库**（1 个）：safety_limit(1174字)

**声明需要的命令**：select, use, npc, talk
　全部在清单里

**已知缺的命令**：castline, reel, quest

**已知缺的知识**：任务鱼与交付规则同上，未入库

**撞上明确做不到**：NPC 对话文本；通用弹幕状态

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### W01（阶段 2 · combat）

**任务**：近战砍死一只史莱姆和一只僵尸，注意别被贴身

**路由**：

```
NEED_KNOWLEDGE:safety_limit,combat_math,actor_behavior
```

**要读的库**（3 个）：safety_limit(1174字) / combat_math(1726字) / actor_behavior(1454字)

**声明需要的命令**：attack, npc, state, move
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### W02（阶段 2 · combat）

**任务**：用弓射下一只飞在天上的恶魔眼

**路由**：

```
NEED_KNOWLEDGE:safety_limit,combat_math
```

**要读的库**（2 个）：safety_limit(1174字) / combat_math(1726字)

**声明需要的命令**：select, shoot, attack, npc
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### W03（阶段 2 · combat）

**任务**：血少了先后撤回血，不要在低血量硬打

**路由**：

```
NEED_KNOWLEDGE:safety_limit,combat_math
```

**要读的库**（2 个）：safety_limit(1174字) / combat_math(1726字)

**声明需要的命令**：state, move, guard, attack
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### W04（阶段 2 · combat）

**任务**：去腐化之地清掉噬魂怪和腐化史莱姆

**路由**：

```
NEED_KNOWLEDGE:safety_limit,world_biome,combat_math
```

**要读的库**（3 个）：safety_limit(1174字) / world_biome(2411字) / combat_math(1726字)

**声明需要的命令**：travel, find, attack, npc
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### W05（阶段 2 · combat）

**任务**：集齐晶状体做召唤物，在恶魔祭坛召唤并打死克苏鲁之眼

**路由**：

```
NEED_KNOWLEDGE:safety_limit,progression_boss
```

**要读的库**（2 个）：safety_limit(1174字) / progression_boss(2017字)

**声明需要的命令**：craft, select, use, attack, boss, npc
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### W06（阶段 2 · combat）

**任务**：BOSS 打完后把掉落物都捡回来

**路由**：

```
NEED_KNOWLEDGE:safety_limit,progression_boss
```

**要读的库**（2 个）：safety_limit(1174字) / progression_boss(2017字)

**声明需要的命令**：grab, inventory, boss
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### E01（阶段 3 · event）

**任务**：血月来了，在家门口顶住整夜刷怪

**路由**：

```
NEED_KNOWLEDGE:safety_limit
```

**要读的库**（1 个）：safety_limit(1174字)

**声明需要的命令**：event, guard, attack, state
　全部在清单里

**已知缺的命令**：startevent

**撞上明确做不到**：随机事件与时机窗口

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### E02（阶段 3 · event）

**任务**：手动开一次日食、南瓜月、霜月，各打一轮

**路由**：

```
NEED_KNOWLEDGE:safety_limit
```

**要读的库**（1 个）：safety_limit(1174字)

**声明需要的命令**：event, attack, guard
　全部在清单里

**已知缺的命令**：startevent

**备注**：event 只能读状态，没有触发的口子

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### E03（阶段 3 · event）

**任务**：把一件武器丢进微光池，看它变成什么，再验证能不能换回来

**路由**：

```
NEED_KNOWLEDGE:safety_limit,item_operation,loot_source
```

**要读的库**（3 个）：safety_limit(1174字) / loot_source(1807字) / item_operation(3072字)

**声明需要的命令**：drop, find, inventory, tile
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### E04（阶段 3 · event）

**任务**：击败血肉墙，进困难模式，确认新矿石和新怪出现

**路由**：

```
NEED_KNOWLEDGE:safety_limit,progression_boss,world_biome
```

**要读的库**（3 个）：safety_limit(1174字) / world_biome(2411字) / progression_boss(2017字)

**声明需要的命令**：boss, attack, select, use, find
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### E05（阶段 3 · event）

**任务**：哥布林入侵或海盗入侵时守住基地

**路由**：

```
NEED_KNOWLEDGE:safety_limit
```

**要读的库**（1 个）：safety_limit(1174字)

**声明需要的命令**：event, guard, attack, npc
　全部在清单里

**已知缺的命令**：startevent

**撞上明确做不到**：随机事件与时机窗口

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### Q01（阶段 2 · gear）

**任务**：把武器、护甲、配饰分别装备到对应栏位

**路由**：

```
NEED_KNOWLEDGE:safety_limit,gear_route
```

**要读的库**（2 个）：safety_limit(1174字) / gear_route(2197字)

**声明需要的命令**：equip, accessory, inventory
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### Q02（阶段 2 · gear）

**任务**：喝一瓶敏捷药水和一瓶治疗药水，确认 buff 出现并计时

**路由**：

```
NEED_KNOWLEDGE:safety_limit,gear_route
```

**要读的库**（2 个）：safety_limit(1174字) / gear_route(2197字)

**声明需要的命令**：select, use, state
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### Q03（阶段 2 · gear）

**任务**：近战和远程来回切换打同一只怪

**路由**：

```
NEED_KNOWLEDGE:safety_limit,combat_math
```

**要读的库**（2 个）：safety_limit(1174字) / combat_math(1726字)

**声明需要的命令**：select, use, shoot, attack
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。

### Q04（阶段 2 · gear）

**任务**：用钩爪把自己拉过一道 8 格宽的沟

**路由**：

```
NEED_KNOWLEDGE:safety_limit,actor_behavior
```

**要读的库**（2 个）：safety_limit(1174字) / actor_behavior(1454字)

**声明需要的命令**：hook, unhook, state, move
　全部在清单里

**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。
