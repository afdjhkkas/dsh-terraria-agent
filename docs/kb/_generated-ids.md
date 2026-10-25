# 物品 / 物块 ID 对照（引擎常量 · 生成物）

> **不要手改这个文件。** 它由 `node scripts/kb-ids.mjs --tsv <tsv>` 从游戏导出的常量表生成。
>
> 这些数字是程序常量：同一个 id 在任何世界、任何存档都指向同一个对象，
> 所以属于知识库，不属于"本局数据"。相反，本世界的坐标、矿脉位置、
> 某次动作的耗时参数都不在这里。

重新生成（需要游戏在运行、已进世界）：

```
# 1) 导出常量表（mod 命令，只写文件，不碰世界）
{"cmd":"idmap","params":{"path":"<临时目录>/ids.tsv"}}
# 2) 生成这份附录
node scripts/kb-ids.mjs --tsv "<临时目录>/ids.tsv"
```

覆盖：物块 693 条 · 背景墙 347 条 · 物品 5513 条。

---

## 1. 挖掘与建造工具

> 镐挖硬质物块、斧砍植被与木制物、锤改外形与拆墙；数字是工具力（越大能挖的越硬）。

| 名称 | 物品 ID | 放置后物块 ID | 工具力 / 伤害 |
|---|---|---|---|
| `CopperPickaxe` 铜镐 | 3509 | — | 镐 35 |
| `TinPickaxe` 锡镐 | 3503 | — | 镐 35 |
| `IronPickaxe` 铁镐 | 1 | — | 镐 40 |
| `LeadPickaxe` 铅镐 | 3497 | — | 镐 43 |
| `SilverPickaxe` 银镐 | 3515 | — | 镐 45 |
| `TungstenPickaxe` 钨镐 | 3491 | — | 镐 50 |
| `GoldPickaxe` 金镐 | 3521 | — | 镐 55 |
| `PlatinumPickaxe` 铂金镐 | 3485 | — | 镐 59 |
| `MoltenPickaxe` 熔岩镐 | 122 | — | 镐 100 |
| `CopperAxe` 铜斧 | 3506 | — | 斧 7 |
| `IronAxe` 铁斧 | 10 | — | 斧 9 |
| `SilverAxe` 银斧 | 3512 | — | 斧 10 |
| `GoldAxe` 金斧 | 3518 | — | 斧 11 |
| `WoodenHammer` 木锤 | 196 | — | 锤 25 |
| `CopperHammer` 铜锤 | 3505 | — | 锤 35 |
| `IronHammer` 铁锤 | 7 | — | 锤 40 |
| `SilverHammer` 银锤 | 3511 | — | 锤 45 |
| `GoldHammer` 金锤 | 3517 | — | 锤 55 |
| `MoltenHamaxe` 熔岩锤斧 | 217 | — | 斧 30 · 锤 70 |

## 2. 可放置物块

> 「放置后物块」就是放下之后该格变成的物块 id —— 判断"放成了没有"要读这个 id，不是读物品 id。

| 名称 | 物品 ID | 放置后物块 ID | 工具力 / 伤害 |
|---|---|---|---|
| `DirtBlock` 土块 | 2 | 0 (Dirt) | — |
| `StoneBlock` 石块 | 3 | 1 (Stone) | — |
| `ClayBlock` 粘土块 | 133 | 40 (ClayBlock) | — |
| `SandBlock` 沙块 | 169 | 53 (Sand) | — |
| `SnowBlock` 雪块 | 593 | 147 (SnowBlock) | — |
| `IceBlock` 冰雪块 | 664 | 161 (IceBlock) | — |
| `MudBlock` 泥块 | 176 | 59 (Mud) | — |
| `AshBlock` 灰烬块 | 172 | 57 (Ash) | — |
| `SiltBlock` 泥沙块 | 424 | 123 (Silt) | — |
| `SlushBlock` 雪泥块 | 1103 | 224 (Slush) | — |
| `Obsidian` 黑曜石 | 173 | 56 (Obsidian) | — |
| `Glass` 玻璃 | 170 | 54 (Glass) | — |
| `Wood` 木材 | 9 | 30 (WoodBlock) | — |

## 3. 家具与制作站

> 多格家具有固定占地与锚点，放下后占地每一格都应是同一个物块 id。

| 名称 | 物品 ID | 放置后物块 ID | 工具力 / 伤害 |
|---|---|---|---|
| `WorkBench` 工作台 | 36 | 18 (WorkBenches) | — |
| `Furnace` 熔炉 | 33 | 17 (Furnaces) | — |
| `IronAnvil` 铁砧 | 35 | 16 (Anvils) | — |
| `LeadAnvil` 铅砧 | 716 | 16 (Anvils) | — |
| `MythrilAnvil` 秘银砧 | 525 | 134 (MythrilAnvil) | — |
| `Chest` 宝箱 | 48 | 21 (Containers) | — |
| `GoldChest` 金箱 | 306 | 21 (Containers) | — |
| `TrashCan` 垃圾桶 | 348 | 21 (Containers) | — |
| `WoodenDoor` 木门 | 25 | 10 (ClosedDoor) | — |
| `WoodenChair` 木椅 | 34 | 15 (Chairs) | — |
| `WoodenTable` 木桌 | 32 | 14 (Tables) | — |
| `Bed` 床 | 224 | 79 (Beds) | — |

## 4. 光源

> 房屋判定要光源；照亮地下避免视野死角。

| 名称 | 物品 ID | 放置后物块 ID | 工具力 / 伤害 |
|---|---|---|---|
| `Torch` 火把 | 8 | 4 (Torches) | — |
| `Campfire` 篝火 | 966 | 215 (Campfire) | — |
| `Candle` 蜡烛 | 105 | 33 (Candles) | — |
| `ChainLantern` 挂链灯笼 | 136 | 42 (HangingLanterns) | — |

## 5. 弹药

> 远程武器必须配对应弹药；弹药耗尽即无法攻击。

| 名称 | 物品 ID | 放置后物块 ID | 工具力 / 伤害 |
|---|---|---|---|
| `WoodenArrow` 木箭 | 40 | — | 伤害 5 |
| `FlamingArrow` 烈焰箭 | 41 | — | 伤害 7 |
| `UnholyArrow` 邪箭 | 47 | — | 伤害 12 |
| `Shuriken` 手里剑 | 42 | — | 伤害 10 |

## 6. 矿石、锭与基础材料

> 锭是多数装备的合成材料；矿石要先熔炼成锭。

| 名称 | 物品 ID | 放置后物块 ID | 工具力 / 伤害 |
|---|---|---|---|
| `CopperOre` 铜矿 | 12 | 7 (Copper) | — |
| `TinOre` 锡矿 | 699 | 166 (Tin) | — |
| `IronOre` 铁矿 | 11 | 6 (Iron) | — |
| `SilverOre` 银矿 | 14 | 9 (Silver) | — |
| `GoldOre` 金矿 | 13 | 8 (Gold) | — |
| `CopperBar` 铜锭 | 20 | 239 (MetalBars) | — |
| `TinBar` 锡锭 | 703 | 239 (MetalBars) | — |
| `IronBar` 铁锭 | 22 | 239 (MetalBars) | — |
| `SilverBar` 银锭 | 21 | 239 (MetalBars) | — |
| `GoldBar` 金锭 | 19 | 239 (MetalBars) | — |
| `Gel` 凝胶 | 23 | — | — |
| `Silk` 丝绸 | 225 | — | — |
| `Lens` 晶状体 | 38 | — | — |
| `LifeCrystal` 生命水晶 | 29 | — | — |
| `Bottle` 玻璃瓶 | 31 | 13 (Bottles) | — |

## 7. 背景墙

> 背景墙物品放置后形成背景墙；房屋判定要求墙铺满。

| 名称 | 物品 ID | 放置后物块 ID | 工具力 / 伤害 |
|---|---|---|---|
| `StoneWall` 石墙 | 26 | — | — |
| `WoodWall` 木墙 | 93 | — | — |

## 8. 挖掘 / 砍伐 / 锤击类物品全表

> 共 104 件。工具力越大，能处理的物块越硬；类别决定能做什么（镐/斧/锤不能互相替代）。

| 名称 | 物品 ID | 镐 | 斧 | 锤 |
|---|---|---|---|---|
| `IronPickaxe` 铁镐 | 1 | 40 | 0 | 0 |
| `IronHammer` 铁锤 | 7 | 0 | 0 | 40 |
| `IronAxe` 铁斧 | 10 | 0 | 9 | 0 |
| `WarAxeoftheNight` 暗夜战斧 | 45 | 0 | 15 | 0 |
| `NightmarePickaxe` 梦魇镐 | 103 | 65 | 0 | 0 |
| `TheBreaker` 魔锤 | 104 | 0 | 0 | 55 |
| `MoltenPickaxe` 熔岩镐 | 122 | 100 | 0 | 0 |
| `WoodenHammer` 木锤 | 196 | 0 | 0 | 25 |
| `MeteorHamaxe` 流星锤斧 | 204 | 0 | 20 | 60 |
| `MoltenHamaxe` 熔岩锤斧 | 217 | 0 | 30 | 70 |
| `Pwnhammer` 神锤 | 367 | 0 | 0 | 80 |
| `CobaltChainsaw` 钴链锯 | 383 | 0 | 14 | 0 |
| `MythrilChainsaw` 秘银链锯 | 384 | 0 | 17 | 0 |
| `CobaltDrill` 钴钻头 | 385 | 110 | 0 | 0 |
| `MythrilDrill` 秘银钻头 | 386 | 150 | 0 | 0 |
| `AdamantiteChainsaw` 精金链锯 | 387 | 0 | 20 | 0 |
| `AdamantiteDrill` 精金钻头 | 388 | 180 | 0 | 0 |
| `Drax` 斧钻 | 579 | 200 | 22 | 0 |
| `EbonwoodHammer` 乌木锤 | 654 | 0 | 0 | 40 |
| `RichMahoganyHammer` 红木锤 | 657 | 0 | 0 | 35 |
| `PearlwoodHammer` 珍珠木锤 | 660 | 0 | 0 | 55 |
| `CobaltPickaxe` 钴镐 | 776 | 110 | 0 | 0 |
| `MythrilPickaxe` 秘银镐 | 777 | 150 | 0 | 0 |
| `AdamantitePickaxe` 精金镐 | 778 | 180 | 0 | 0 |
| `Hammush` 蘑菇锤 | 787 | 0 | 0 | 85 |
| `FleshGrinder` 血肉锤 | 797 | 0 | 0 | 55 |
| `DeathbringerPickaxe` 死亡使者镐 | 798 | 70 | 0 | 0 |
| `BloodLustCluster` 嗜血狂斧 | 799 | 0 | 15 | 0 |
| `CactusPickaxe` 仙人掌镐 | 882 | 35 | 0 | 0 |
| `ShadewoodHammer` 暗影木锤 | 922 | 0 | 0 | 40 |
| `PickaxeAxe` 镐斧 | 990 | 200 | 22 | 0 |
| `CobaltWaraxe` 钴战斧 | 991 | 0 | 14 | 0 |
| `MythrilWaraxe` 秘银战斧 | 992 | 0 | 17 | 0 |
| `AdamantiteWaraxe` 精金战斧 | 993 | 0 | 20 | 0 |
| `PalladiumPickaxe` 钯金镐 | 1188 | 130 | 0 | 0 |
| `PalladiumDrill` 钯金钻头 | 1189 | 130 | 0 | 0 |
| `PalladiumChainsaw` 钯金链锯 | 1190 | 0 | 15 | 0 |
| `OrichalcumPickaxe` 山铜镐 | 1195 | 165 | 0 | 0 |
| `OrichalcumDrill` 山铜钻头 | 1196 | 165 | 0 | 0 |
| `OrichalcumChainsaw` 山铜链锯 | 1197 | 0 | 18 | 0 |
| `TitaniumPickaxe` 钛金镐 | 1202 | 190 | 0 | 0 |
| `TitaniumDrill` 钛金钻头 | 1203 | 190 | 0 | 0 |
| `TitaniumChainsaw` 钛金链锯 | 1204 | 0 | 21 | 0 |
| `PalladiumWaraxe` 钯金战斧 | 1222 | 0 | 15 | 0 |
| `OrichalcumWaraxe` 山铜战斧 | 1223 | 0 | 18 | 0 |
| `TitaniumWaraxe` 钛金战斧 | 1224 | 0 | 21 | 0 |
| `ChlorophytePickaxe` 叶绿镐 | 1230 | 200 | 0 | 0 |
| `ChlorophyteDrill` 叶绿钻头 | 1231 | 200 | 0 | 0 |
| `ChlorophyteChainsaw` 叶绿链锯 | 1232 | 0 | 23 | 0 |
| `ChlorophyteGreataxe` 叶绿巨斧 | 1233 | 0 | 23 | 0 |
| `ChlorophyteWarhammer` 叶绿战锤 | 1234 | 0 | 0 | 90 |
| `ChlorophyteJackhammer` 叶绿手提钻 | 1262 | 0 | 0 | 90 |
| `Picksaw` 锯刃镐 | 1294 | 210 | 25 | 0 |
| `TheAxe` 斧 | 1305 | 0 | 35 | 100 |
| `BonePickaxe` 骨镐 | 1320 | 55 | 0 | 0 |
| `SpectrePickaxe` 幽灵镐 | 1506 | 200 | 0 | 0 |
| `SpectreHamaxe` 幽灵锤斧 | 1507 | 0 | 30 | 90 |
| `CnadyCanePickaxe` 糖棒镐 | 1917 | 55 | 0 | 0 |
| `ShroomiteDiggingClaw` 蘑菇矿挖爪 | 2176 | 200 | 25 | 0 |
| `Rockfish` 岩鱼锤 | 2320 | 0 | 0 | 70 |
| `ReaverShark` 掠夺鲨 | 2341 | 59 | 0 | 0 |
| `SawtoothShark` 锯齿鲨 | 2342 | 0 | 14 | 0 |
| `PalmWoodHammer` 棕榈木锤 | 2516 | 0 | 0 | 35 |
| `BorealWoodHammer` 针叶木锤 | 2746 | 0 | 0 | 35 |
| `VortexDrill` 星旋钻头 | 2774 | 225 | 0 | 0 |
| `VortexPickaxe` 星旋镐 | 2776 | 225 | 0 | 0 |
| `NebulaDrill` 星云钻头 | 2779 | 225 | 0 | 0 |
| `NebulaPickaxe` 星云镐 | 2781 | 225 | 0 | 0 |
| `SolarFlareDrill` 耀斑钻头 | 2784 | 225 | 0 | 0 |
| `SolarFlarePickaxe` 耀斑镐斧 | 2786 | 225 | 0 | 0 |
| `LaserDrill` 激光钻头 | 2798 | 230 | 0 | 0 |
| `ButchersChainsaw` 屠夫链锯 | 3098 | 0 | 30 | 0 |
| `StardustDrill` 星尘钻头 | 3464 | 225 | 0 | 0 |
| `StardustPickaxe` 星尘镐 | 3466 | 225 | 0 | 0 |
| `PlatinumHammer` 铂金锤 | 3481 | 0 | 0 | 59 |
| `PlatinumAxe` 铂金斧 | 3482 | 0 | 12 | 0 |
| `PlatinumPickaxe` 铂金镐 | 3485 | 59 | 0 | 0 |
| `TungstenHammer` 钨锤 | 3487 | 0 | 0 | 50 |
| `TungstenAxe` 钨斧 | 3488 | 0 | 11 | 0 |
| `TungstenPickaxe` 钨镐 | 3491 | 50 | 0 | 0 |
| `LeadHammer` 铅锤 | 3493 | 0 | 0 | 43 |
| `LeadAxe` 铅斧 | 3494 | 0 | 10 | 0 |
| `LeadPickaxe` 铅镐 | 3497 | 43 | 0 | 0 |
| `TinHammer` 锡锤 | 3499 | 0 | 0 | 38 |
| `TinAxe` 锡斧 | 3500 | 0 | 8 | 0 |
| `TinPickaxe` 锡镐 | 3503 | 35 | 0 | 0 |
| `CopperHammer` 铜锤 | 3505 | 0 | 0 | 35 |
| `CopperAxe` 铜斧 | 3506 | 0 | 7 | 0 |
| `CopperPickaxe` 铜镐 | 3509 | 35 | 0 | 0 |
| `SilverHammer` 银锤 | 3511 | 0 | 0 | 45 |
| `SilverAxe` 银斧 | 3512 | 0 | 10 | 0 |
| `SilverPickaxe` 银镐 | 3515 | 45 | 0 | 0 |
| `GoldHammer` 金锤 | 3517 | 0 | 0 | 55 |
| `GoldAxe` 金斧 | 3518 | 0 | 11 | 0 |
| `GoldPickaxe` 金镐 | 3521 | 55 | 0 | 0 |
| `LunarHamaxeSolar` 耀斑锤斧 | 3522 | 0 | 30 | 100 |
| `LunarHamaxeVortex` 星旋锤斧 | 3523 | 0 | 30 | 100 |
| `LunarHamaxeNebula` 星云锤斧 | 3524 | 0 | 30 | 100 |
| `LunarHamaxeStardust` 星尘锤斧 | 3525 | 0 | 30 | 100 |
| `FossilPickaxe` 化石镐 | 4059 | 55 | 0 | 0 |
| `BloodHamaxe` 锤斧 | 4317 | 0 | 30 | 80 |
| `LucyTheAxe` 露西斧 | 5095 | 0 | 30 | 0 |
| `AshWoodHammer` 白蜡木锤 | 5283 | 0 | 0 | 45 |
| `AcornAxe` 再生之斧 | 5295 | 0 | 30 | 0 |

## 9. 完整物品 → 物块 / 墙 对照（可放置物品）

> 共 2842 件。这是"放下之后世界会变成什么"的权威映射，判断放置是否成功时按这里的物块 id 回读。

| 名称 | 物品 ID | 物块 ID | 物块名 | 墙 ID |
|---|---|---|---|---|
| `StoneBlock` 石块 | 3 | 1 | Stone | — |
| `Torch` 火把 | 8 | 4 | Torches | — |
| `Wood` 木材 | 9 | 30 | WoodBlock | — |
| `IronOre` 铁矿 | 11 | 6 | Iron | — |
| `CopperOre` 铜矿 | 12 | 7 | Copper | — |
| `GoldOre` 金矿 | 13 | 8 | Gold | — |
| `SilverOre` 银矿 | 14 | 9 | Silver | — |
| `GoldBar` 金锭 | 19 | 239 | MetalBars | — |
| `CopperBar` 铜锭 | 20 | 239 | MetalBars | — |
| `SilverBar` 银锭 | 21 | 239 | MetalBars | — |
| `IronBar` 铁锭 | 22 | 239 | MetalBars | — |
| `WoodenDoor` 木门 | 25 | 10 | ClosedDoor | — |
| `StoneWall` 石墙 | 26 | — | — | 1 |
| `Acorn` 橡实 | 27 | 20 | Saplings | — |
| `DirtWall` 土墙 | 30 | — | — | 16 |
| `Bottle` 玻璃瓶 | 31 | 13 | Bottles | — |
| `WoodenTable` 木桌 | 32 | 14 | Tables | — |
| `Furnace` 熔炉 | 33 | 17 | Furnaces | — |
| `WoodenChair` 木椅 | 34 | 15 | Chairs | — |
| `IronAnvil` 铁砧 | 35 | 16 | Anvils | — |
| `WorkBench` 工作台 | 36 | 18 | WorkBenches | — |
| `Chest` 宝箱 | 48 | 21 | Containers | — |
| `AngelStatue` 天使雕像 | 52 | 105 | Statues | — |
| `DemoniteOre` 魔矿 | 56 | 22 | Demonite | — |
| `DemoniteBar` 魔矿锭 | 57 | 239 | MetalBars | — |
| `CorruptSeeds` 腐化种子 | 59 | 23 | CorruptGrass | — |
| `EbonstoneBlock` 黑檀石块 | 61 | 25 | Ebonstone | — |
| `GrassSeeds` 草种子 | 62 | 2 | Grass | — |
| `Sunflower` 向日葵 | 63 | 27 | Sunflower | — |
| `CopperCoin` 铜币 | 71 | 330 | CopperCoinPile | — |
| `SilverCoin` 银币 | 72 | 331 | SilverCoinPile | — |
| `GoldCoin` 金币 | 73 | 332 | GoldCoinPile | — |
| `PlatinumCoin` 铂金币 | 74 | 333 | PlatinumCoinPile | — |
| `Chain` 链条 | 85 | 214 | Chain | — |
| `PiggyBank` 猪猪存钱罐 | 87 | 29 | PiggyBank | — |
| `WoodWall` 木墙 | 93 | — | — | 4 |
| `WoodPlatform` 木平台 | 94 | 19 | Platforms | — |
| `Candle` 蜡烛 | 105 | 33 | Candles | — |
| `CopperChandelier` 铜吊灯 | 106 | 34 | Chandeliers | — |
| `SilverChandelier` 银吊灯 | 107 | 34 | Chandeliers | — |
| `GoldChandelier` 金吊灯 | 108 | 34 | Chandeliers | — |
| `Meteorite` 陨石 | 116 | 37 | Meteorite | — |
| `MeteoriteBar` 陨石锭 | 117 | 239 | MetalBars | — |
| `GrayBrick` 灰砖 | 129 | 38 | GrayBrick | — |
| `GrayBrickWall` 灰砖墙 | 130 | — | — | 5 |
| `RedBrick` 红砖 | 131 | 39 | RedBrick | — |
| `RedBrickWall` 红砖墙 | 132 | — | — | 6 |
| `ClayBlock` 粘土块 | 133 | 40 | ClayBlock | — |
| `BlueBrick` 蓝砖 | 134 | 41 | BlueDungeonBrick | — |
| `BlueBrickWall` 蓝砖墙 | 135 | — | — | 17 |
| `ChainLantern` 挂链灯笼 | 136 | 42 | HangingLanterns | — |
| `GreenBrick` 绿砖 | 137 | 43 | GreenDungeonBrick | — |
| `GreenBrickWall` 绿砖墙 | 138 | — | — | 18 |
| `PinkBrick` 粉砖 | 139 | 44 | PinkDungeonBrick | — |
| `PinkBrickWall` 粉砖墙 | 140 | — | — | 19 |
| `GoldBrick` 金砖 | 141 | 45 | GoldBrick | — |
| `GoldBrickWall` 金砖墙 | 142 | — | — | 10 |
| `SilverBrick` 银砖 | 143 | 46 | SilverBrick | — |
| `SilverBrickWall` 银砖墙 | 144 | — | — | 11 |
| `CopperBrick` 铜砖 | 145 | 47 | CopperBrick | — |
| `CopperBrickWall` 铜砖墙 | 146 | — | — | 12 |
| `Spike` 斯派克 | 147 | 48 | Spikes | — |
| `WaterCandle` 水蜡烛 | 148 | 49 | WaterCandle | — |
| `Book` 书 | 149 | 50 | Books | — |
| `Cobweb` 蛛网 | 150 | 51 | Cobweb | — |
| `SandBlock` 沙块 | 169 | 53 | Sand | — |
| `Glass` 玻璃 | 170 | 54 | Glass | — |
| `Sign` 标牌 | 171 | 55 | Signs | — |
| `AshBlock` 灰烬块 | 172 | 57 | Ash | — |
| `Obsidian` 黑曜石 | 173 | 56 | Obsidian | — |
| `Hellstone` 狱石 | 174 | 58 | Hellstone | — |
| `HellstoneBar` 狱石锭 | 175 | 239 | MetalBars | — |
| `MudBlock` 泥块 | 176 | 59 | Mud | — |
| `Sapphire` 萨菲 | 177 | 178 | ExposedGems | — |
| `Ruby` 鲁比 | 178 | 178 | ExposedGems | — |
| `Emerald` 翡翠 | 179 | 178 | ExposedGems | — |
| `Topaz` 黄玉 | 180 | 178 | ExposedGems | — |
| `Amethyst` 紫晶 | 181 | 178 | ExposedGems | — |
| `Diamond` 钻石 | 182 | 178 | ExposedGems | — |
| `GlowingMushroom` 发光蘑菇 | 183 | 190 | MushroomBlock | — |
| `ObsidianBrick` 黑曜石砖 | 192 | 75 | ObsidianBrick | — |
| `MushroomGrassSeeds` 蘑菇草种子 | 194 | 70 | MushroomGrass | — |
| `JungleGrassSeeds` 丛林草种子 | 195 | 60 | JungleGrass | — |
| `StaffofRegrowth` 再生法杖 | 213 | 2 | Grass | — |
| `HellstoneBrick` 狱石砖 | 214 | 76 | HellstoneBrick | — |
| `Hellforge` 地狱熔炉 | 221 | 77 | Hellforge | — |
| `ClayPot` 粘土盆 | 222 | 78 | ClayPot | — |
| `Bed` 床 | 224 | 79 | Beds | — |
| `FishBowl` 鱼缸 | 250 | 282 | FishBowl | — |
| `Coral` 珊瑚 | 275 | 81 | Coral | — |
| `Cactus` 仙人掌 | 276 | 188 | CactusBlock | — |
| `GoldChest` 金箱 | 306 | 21 | Containers | — |
| `DaybloomSeeds` 太阳花种子 | 307 | 82 | ImmatureHerbs | — |
| `MoonglowSeeds` 月光草种子 | 308 | 82 | ImmatureHerbs | — |
| `BlinkrootSeeds` 闪耀根种子 | 309 | 82 | ImmatureHerbs | — |
| `DeathweedSeeds` 死亡草种子 | 310 | 82 | ImmatureHerbs | — |
| `WaterleafSeeds` 幌菊种子 | 311 | 82 | ImmatureHerbs | — |
| `FireblossomSeeds` 火焰花种子 | 312 | 82 | ImmatureHerbs | — |
| `Tombstone` 墓石 | 321 | 85 | Tombstones | — |
| `ShadowChest` 暗影箱 | 328 | 21 | Containers | — |
| `ObsidianBrickWall` 黑曜石砖墙 | 330 | — | — | 20 |
| `Loom` 织布机 | 332 | 86 | Loom | — |
| `Piano` 钢琴 | 333 | 87 | Pianos | — |
| `Dresser` 梳妆台 | 334 | 88 | Dressers | — |
| `Bench` 长椅 | 335 | 89 | Benches | — |
| `Bathtub` 浴缸 | 336 | 90 | Bathtubs | — |
| `RedBanner` 红旗 | 337 | 91 | Banners | — |
| `GreenBanner` 绿旗 | 338 | 91 | Banners | — |
| `BlueBanner` 蓝旗 | 339 | 91 | Banners | — |
| `YellowBanner` 黄旗 | 340 | 91 | Banners | — |
| `LampPost` 灯柱 | 341 | 92 | Lampposts | — |
| `TikiTorch` 提基火把 | 342 | 93 | Lamps | — |
| `Barrel` 桶 | 343 | 21 | Containers | — |
| `ChineseLantern` 中式灯笼 | 344 | 95 | ChineseLanterns | — |
| `CookingPot` 烹饪锅 | 345 | 96 | CookingPots | — |
| `Safe` 保险箱 | 346 | 97 | Safes | — |
| `SkullLantern` 骷髅头灯笼 | 347 | 98 | SkullLanterns | — |
| `TrashCan` 垃圾桶 | 348 | 21 | Containers | — |
| `Candelabra` 烛台 | 349 | 100 | Candelabras | — |
| `PinkVase` 粉花瓶 | 350 | 13 | Bottles | — |
| `Mug` 玻璃杯 | 351 | 13 | Bottles | — |
| `Keg` 酒桶 | 352 | 94 | Kegs | — |
| `Bookcase` 书架 | 354 | 101 | Bookcases | — |
| `Throne` 王座 | 355 | 102 | Thrones | — |
| `Bowl` 碗 | 356 | 103 | Bowls | — |
| `Toilet` 马桶 | 358 | 15 | Chairs | — |
| `GrandfatherClock` 落地大摆钟 | 359 | 104 | GrandfatherClocks | — |
| `ArmorStatue` 盔甲雕像 | 360 | 105 | Statues | — |
| `Sawmill` 锯木机 | 363 | 106 | Sawmill | — |
| `CobaltOre` 钴矿 | 364 | 107 | Cobalt | — |
| `MythrilOre` 秘银矿 | 365 | 108 | Mythril | — |
| `AdamantiteOre` 精金矿 | 366 | 111 | Adamantite | — |
| `HallowedSeeds` 圣种 | 369 | 109 | HallowedGrass | — |
| `EbonsandBlock` 黑檀沙块 | 370 | 112 | Ebonsand | — |
| `CobaltBar` 钴锭 | 381 | 239 | MetalBars | — |
| `MythrilBar` 秘银锭 | 382 | 239 | MetalBars | — |
| `AdamantiteBar` 精金锭 | 391 | 239 | MetalBars | — |
| `GlassWall` 玻璃墙 | 392 | — | — | 21 |
| `TinkerersWorkshop` 工匠作坊 | 398 | 114 | TinkerersWorkbench | — |
| `PearlsandBlock` 珍珠沙块 | 408 | 116 | Pearlsand | — |
| `PearlstoneBlock` 珍珠石块 | 409 | 117 | Pearlstone | — |
| `PearlstoneBrick` 珍珠石砖 | 412 | 118 | PearlstoneBrick | — |
| `IridescentBrick` 荧光砖 | 413 | 119 | IridescentBrick | — |
| `MudstoneBlock` 泥石砖 | 414 | 120 | Mudstone | — |
| `CobaltBrick` 钴砖 | 415 | 121 | CobaltBrick | — |
| `MythrilBrick` 秘银砖 | 416 | 122 | MythrilBrick | — |
| `PearlstoneBrickWall` 珍珠石砖墙 | 417 | — | — | 22 |
| `IridescentBrickWall` 荧光砖墙 | 418 | — | — | 23 |
| `MudstoneBrickWall` 泥石砖墙 | 419 | — | — | 24 |
| `CobaltBrickWall` 钴砖墙 | 420 | — | — | 25 |
| `MythrilBrickWall` 秘银砖墙 | 421 | — | — | 26 |
| `SiltBlock` 泥沙块 | 424 | 123 | Silt | — |
| `BlueTorch` 蓝火把 | 427 | 4 | Torches | — |
| `RedTorch` 红火把 | 428 | 4 | Torches | — |
| `GreenTorch` 绿火把 | 429 | 4 | Torches | — |
| `PurpleTorch` 紫火把 | 430 | 4 | Torches | — |
| `WhiteTorch` 白火把 | 431 | 4 | Torches | — |
| `YellowTorch` 黄火把 | 432 | 4 | Torches | — |
| `DemonTorch` 恶魔火把 | 433 | 4 | Torches | — |
| `StarStatue` 星星雕像 | 438 | 105 | Statues | — |
| `SwordStatue` 宝剑雕像 | 439 | 105 | Statues | — |
| `SlimeStatue` 史莱姆雕像 | 440 | 105 | Statues | — |
| `GoblinStatue` 哥布林雕像 | 441 | 105 | Statues | — |
| `ShieldStatue` 护盾雕像 | 442 | 105 | Statues | — |
| `BatStatue` 蝙蝠雕像 | 443 | 105 | Statues | — |
| `FishStatue` 金鱼雕像 | 444 | 105 | Statues | — |
| `BunnyStatue` 兔兔雕像 | 445 | 105 | Statues | — |
| `SkeletonStatue` 骷髅雕像 | 446 | 105 | Statues | — |
| `ReaperStatue` 死神雕像 | 447 | 105 | Statues | — |
| `WomanStatue` 女人雕像 | 448 | 105 | Statues | — |
| `ImpStatue` 小鬼雕像 | 449 | 105 | Statues | — |
| `GargoyleStatue` 石像鬼雕像 | 450 | 105 | Statues | — |
| `GloomStatue` 幽冥雕像 | 451 | 105 | Statues | — |
| `HornetStatue` 黄蜂雕像 | 452 | 105 | Statues | — |
| `BombStatue` 炸弹雕像 | 453 | 105 | Statues | — |
| `CrabStatue` 螃蟹雕像 | 454 | 105 | Statues | — |
| `HammerStatue` 战锤雕像 | 455 | 105 | Statues | — |
| `PotionStatue` 药水雕像 | 456 | 105 | Statues | — |
| `SpearStatue` 长矛雕像 | 457 | 105 | Statues | — |
| `CrossStatue` 十字架雕像 | 458 | 105 | Statues | — |
| `JellyfishStatue` 水母雕像 | 459 | 105 | Statues | — |
| `BowStatue` 弓雕像 | 460 | 105 | Statues | — |
| `BoomerangStatue` 回旋镖雕像 | 461 | 105 | Statues | — |
| `BootStatue` 靴子雕像 | 462 | 105 | Statues | — |
| `ChestStatue` 宝箱雕像 | 463 | 105 | Statues | — |
| `BirdStatue` 小鸟雕像 | 464 | 105 | Statues | — |
| `AxeStatue` 战斧雕像 | 465 | 105 | Statues | — |
| `CorruptStatue` 腐化雕像 | 466 | 105 | Statues | — |
| `TreeStatue` 树木雕像 | 467 | 105 | Statues | — |
| `AnvilStatue` 砧雕像 | 468 | 105 | Statues | — |
| `PickaxeStatue` 镐雕像 | 469 | 105 | Statues | — |
| `MushroomStatue` 蘑菇雕像 | 470 | 349 | MushroomStatue | — |
| `EyeballStatue` 魔眼雕像 | 471 | 105 | Statues | — |
| `PillarStatue` 石柱雕像 | 472 | 105 | Statues | — |
| `HeartStatue` 心形雕像 | 473 | 105 | Statues | — |
| `PotStatue` 陶罐雕像 | 474 | 105 | Statues | — |
| `SunflowerStatue` 向日葵雕像 | 475 | 105 | Statues | — |
| `KingStatue` 国王雕像 | 476 | 105 | Statues | — |
| `QueenStatue` 女王雕像 | 477 | 105 | Statues | — |
| `PiranhaStatue` 食人鱼雕像 | 478 | 105 | Statues | — |
| `PlankedWall` 板条墙 | 479 | — | — | 27 |
| `WoodenBeam` 木梁 | 480 | 124 | WoodenBeam | — |
| `CrystalBall` 水晶球 | 487 | 125 | CrystalBall | — |
| `DiscoBall` 迪斯科球 | 488 | 126 | DiscoBall | — |
| `Mannequin` 人体模型 | 498 | 470 | DisplayDoll | — |
| `CrystalShard` 水晶碎块 | 502 | 129 | Crystals | — |
| `ActiveStoneBlock` 通电石块 | 511 | 130 | ActiveStoneBlock | — |
| `InactiveStoneBlock` 未通电石块 | 512 | 131 | InactiveStoneBlock | — |
| `Lever` 控制杆 | 513 | 132 | Lever | — |
| `CursedTorch` 诅咒火把 | 523 | 4 | Torches | — |
| `AdamantiteForge` 精金熔炉 | 524 | 133 | AdamantiteForge | — |
| `MythrilAnvil` 秘银砧 | 525 | 134 | MythrilAnvil | — |
| `RedPressurePlate` 红压力板 | 529 | 135 | PressurePlates | — |
| `Switch` 开关 | 538 | 136 | Switches | — |
| `DartTrap` 飞镖机关 | 539 | 137 | Traps | — |
| `Boulder` 巨石 | 540 | 138 | Boulder | — |
| `GreenPressurePlate` 绿压力板 | 541 | 135 | PressurePlates | — |
| `GrayPressurePlate` 灰压力板 | 542 | 135 | PressurePlates | — |
| `BrownPressurePlate` 棕压力板 | 543 | 135 | PressurePlates | — |
| `MusicBoxOverworldDay` 八音盒（人间日） | 562 | 139 | MusicBoxes | — |
| `MusicBoxEerie` 八音盒（恐惧） | 563 | 139 | MusicBoxes | — |
| `MusicBoxNight` 八音盒（暗夜） | 564 | 139 | MusicBoxes | — |
| `MusicBoxTitle` 八音盒（标题） | 565 | 139 | MusicBoxes | — |
| `MusicBoxUnderground` 八音盒（地下） | 566 | 139 | MusicBoxes | — |
| `MusicBoxBoss1` 八音盒(Boss 1) | 567 | 139 | MusicBoxes | — |
| `MusicBoxJungle` 八音盒（丛林） | 568 | 139 | MusicBoxes | — |
| `MusicBoxCorruption` 八音盒（腐化之地） | 569 | 139 | MusicBoxes | — |
| `MusicBoxUndergroundCorruption` 八音盒（地下腐化之地） | 570 | 139 | MusicBoxes | — |
| `MusicBoxTheHallow` 八音盒（神圣之地） | 571 | 139 | MusicBoxes | — |
| `MusicBoxBoss2` 八音盒(Boss 2( | 572 | 139 | MusicBoxes | — |
| `MusicBoxUndergroundHallow` 八音盒（地下神圣之地） | 573 | 139 | MusicBoxes | — |
| `MusicBoxBoss3` 八音盒(Boss 3) | 574 | 139 | MusicBoxes | — |
| `DemoniteBrick` 魔矿砖 | 577 | 140 | DemoniteBrick | — |
| `Explosives` 炸药 | 580 | 141 | Explosives | — |
| `InletPump` 入水泵 | 581 | 142 | InletPump | — |
| `OutletPump` 出水泵 | 582 | 143 | OutletPump | — |
| `Timer1Second` 1秒计时器 | 583 | 144 | Timers | — |
| `Timer3Second` 3秒计时器 | 584 | 144 | Timers | — |
| `Timer5Second` 5秒计时器 | 585 | 144 | Timers | — |
| `CandyCaneBlock` 糖棒块 | 586 | 145 | CandyCaneBlock | — |
| `CandyCaneWall` 糖棒墙 | 587 | — | — | 29 |
| `GreenCandyCaneBlock` 绿糖棒块 | 591 | 146 | GreenCandyCaneBlock | — |
| `GreenCandyCaneWall` 绿糖棒墙 | 592 | — | — | 30 |
| `SnowBlock` 雪块 | 593 | 147 | SnowBlock | — |
| `SnowBrick` 雪砖 | 594 | 148 | SnowBrick | — |
| `SnowBrickWall` 雪砖墙 | 595 | — | — | 31 |
| `BlueLight` 蓝灯 | 596 | 149 | HolidayLights | — |
| `RedLight` 红灯 | 597 | 149 | HolidayLights | — |
| `GreenLight` 绿灯 | 598 | 149 | HolidayLights | — |
| `AdamantiteBeam` 精金梁 | 604 | 150 | AdamantiteBeam | — |
| `AdamantiteBeamWall` 精金梁墙 | 605 | — | — | 32 |
| `DemoniteBrickWall` 魔矿砖墙 | 606 | — | — | 33 |
| `SandstoneBrick` 沙岩砖 | 607 | 151 | SandstoneBrick | — |
| `SandstoneBrickWall` 沙岩砖墙 | 608 | — | — | 34 |
| `EbonstoneBrick` 黑檀石砖 | 609 | 152 | EbonstoneBrick | — |
| `EbonstoneBrickWall` 黑檀石砖墙 | 610 | — | — | 35 |
| `RedStucco` 红泥灰 | 611 | 153 | RedStucco | — |
| `YellowStucco` 黄泥灰 | 612 | 154 | YellowStucco | — |
| `GreenStucco` 绿泥灰 | 613 | 155 | GreenStucco | — |
| `GrayStucco` 灰泥灰 | 614 | 156 | GrayStucco | — |
| `RedStuccoWall` 红泥灰墙 | 615 | — | — | 36 |
| `YellowStuccoWall` 黄泥灰墙 | 616 | — | — | 37 |
| `GreenStuccoWall` 绿泥灰墙 | 617 | — | — | 38 |
| `GrayStuccoWall` 灰泥灰墙 | 618 | — | — | 39 |
| `Ebonwood` 乌木 | 619 | 157 | Ebonwood | — |
| `RichMahogany` 红木 | 620 | 158 | RichMahogany | — |
| `Pearlwood` 珍珠木 | 621 | 159 | Pearlwood | — |
| `EbonwoodWall` 乌木墙 | 622 | — | — | 41 |
| `RichMahoganyWall` 红木墙 | 623 | — | — | 42 |
| `PearlwoodWall` 珍珠木墙 | 624 | — | — | 43 |
| `EbonwoodChest` 乌木箱 | 625 | 21 | Containers | — |
| `RichMahoganyChest` 红木箱 | 626 | 21 | Containers | — |
| `PearlwoodChest` 珍珠木箱 | 627 | 21 | Containers | — |
| `EbonwoodChair` 乌木椅 | 628 | 15 | Chairs | — |
| `RichMahoganyChair` 红木椅 | 629 | 15 | Chairs | — |
| `PearlwoodChair` 珍珠木椅 | 630 | 15 | Chairs | — |
| `EbonwoodPlatform` 乌木平台 | 631 | 19 | Platforms | — |
| `RichMahoganyPlatform` 红木平台 | 632 | 19 | Platforms | — |
| `PearlwoodPlatform` 珍珠木平台 | 633 | 19 | Platforms | — |
| `BonePlatform` 骨头平台 | 634 | 19 | Platforms | — |
| `EbonwoodWorkBench` 乌木工作台 | 635 | 18 | WorkBenches | — |
| `RichMahoganyWorkBench` 红木工作台 | 636 | 18 | WorkBenches | — |
| `PearlwoodWorkBench` 珍珠木工作台 | 637 | 18 | WorkBenches | — |
| `EbonwoodTable` 乌木桌 | 638 | 14 | Tables | — |
| `RichMahoganyTable` 红木桌 | 639 | 14 | Tables | — |
| `PearlwoodTable` 珍珠木桌 | 640 | 14 | Tables | — |
| `EbonwoodPiano` 乌木钢琴 | 641 | 87 | Pianos | — |
| `RichMahoganyPiano` 红木钢琴 | 642 | 87 | Pianos | — |
| `PearlwoodPiano` 珍珠木钢琴 | 643 | 87 | Pianos | — |
| `EbonwoodBed` 乌木床 | 644 | 79 | Beds | — |
| `RichMahoganyBed` 红木床 | 645 | 79 | Beds | — |
| `PearlwoodBed` 珍珠木床 | 646 | 79 | Beds | — |
| `EbonwoodDresser` 乌木梳妆台 | 647 | 88 | Dressers | — |
| `RichMahoganyDresser` 红木梳妆台 | 648 | 88 | Dressers | — |
| `PearlwoodDresser` 珍珠木梳妆台 | 649 | 88 | Dressers | — |
| `EbonwoodDoor` 乌木门 | 650 | 10 | ClosedDoor | — |
| `RichMahoganyDoor` 红木门 | 651 | 10 | ClosedDoor | — |
| `PearlwoodDoor` 珍珠木门 | 652 | 10 | ClosedDoor | — |
| `RainbowBrick` 彩虹砖 | 662 | 160 | RainbowBrick | — |
| `RainbowBrickWall` 彩虹砖墙 | 663 | — | — | 44 |
| `IceBlock` 冰雪块 | 664 | 161 | IceBlock | — |
| `BorealWoodWorkBench` 针叶木工作台 | 673 | 18 | WorkBenches | — |
| `BorealWoodTable` 针叶木桌 | 677 | 14 | Tables | — |
| `IvyChest` 常春藤箱 | 680 | 21 | Containers | — |
| `IceChest` 冰冻箱 | 681 | 21 | Containers | — |
| `TinOre` 锡矿 | 699 | 166 | Tin | — |
| `LeadOre` 铅矿 | 700 | 167 | Lead | — |
| `TungstenOre` 钨矿 | 701 | 168 | Tungsten | — |
| `PlatinumOre` 铂金矿 | 702 | 169 | Platinum | — |
| `TinBar` 锡锭 | 703 | 239 | MetalBars | — |
| `LeadBar` 铅锭 | 704 | 239 | MetalBars | — |
| `TungstenBar` 钨锭 | 705 | 239 | MetalBars | — |
| `PlatinumBar` 铂金锭 | 706 | 239 | MetalBars | — |
| `TinChandelier` 锡吊灯 | 710 | 34 | Chandeliers | — |
| `TungstenChandelier` 钨吊灯 | 711 | 34 | Chandeliers | — |
| `PlatinumChandelier` 铂金吊灯 | 712 | 34 | Chandeliers | — |
| `PlatinumCandle` 铂金蜡烛 | 713 | 174 | PlatinumCandle | — |
| `PlatinumCandelabra` 铂金烛台 | 714 | 173 | PlatinumCandelabra | — |
| `LeadAnvil` 铅砧 | 716 | 16 | Anvils | — |
| `TinBrick` 锡砖 | 717 | 175 | TinBrick | — |
| `TungstenBrick` 钨砖 | 718 | 176 | TungstenBrick | — |
| `PlatinumBrick` 铂金砖 | 719 | 177 | PlatinumBrick | — |
| `TinBrickWall` 锡砖墙 | 720 | — | — | 45 |
| `TungstenBrickWall` 钨砖墙 | 721 | — | — | 46 |
| `PlatinumBrickWall` 铂金砖墙 | 722 | — | — | 47 |
| `GrassWall` 草墙 | 745 | — | — | 66 |
| `JungleWall` 丛林墙 | 746 | — | — | 67 |
| `FlowerWall` 花墙 | 747 | — | — | 68 |
| `CactusWall` 仙人掌墙 | 750 | — | — | 72 |
| `Cloud` 克劳德 | 751 | 189 | Cloud | — |
| `CloudWall` 云墙 | 752 | — | — | 73 |
| `SlimeBlock` 史莱姆块 | 762 | 193 | SlimeBlock | — |
| `FleshBlock` 血肉块 | 763 | 195 | FleshBlock | — |
| `MushroomWall` 蘑菇墙 | 764 | — | — | 74 |
| `RainCloud` 雨云 | 765 | 196 | RainCloud | — |
| `BoneBlock` 骨块 | 766 | 194 | BoneBlock | — |
| `FrozenSlimeBlock` 冰冻史莱姆块 | 767 | 197 | FrozenSlimeBlock | — |
| `BoneBlockWall` 骨块墙 | 768 | — | — | 75 |
| `SlimeBlockWall` 史莱姆块墙 | 769 | — | — | 76 |
| `FleshBlockWall` 血肉块墙 | 770 | — | — | 77 |
| `AsphaltBlock` 沥青块 | 775 | 198 | Asphalt | — |
| `AnkhBanner` 十字章旗 | 789 | 91 | Banners | — |
| `SnakeBanner` 蛇旗 | 790 | 91 | Banners | — |
| `OmegaBanner` 欧米茄旗 | 791 | 91 | Banners | — |
| `LivingWoodChair` 生命木椅 | 806 | 15 | Chairs | — |
| `CactusChair` 仙人掌椅 | 807 | 15 | Chairs | — |
| `BoneChair` 骨头椅 | 808 | 15 | Chairs | — |
| `FleshChair` 血肉椅 | 809 | 15 | Chairs | — |
| `MushroomChair` 蘑菇椅 | 810 | 15 | Chairs | — |
| `BoneWorkBench` 骨头工作台 | 811 | 18 | WorkBenches | — |
| `CactusWorkBench` 仙人掌工作台 | 812 | 18 | WorkBenches | — |
| `FleshWorkBench` 血肉工作台 | 813 | 18 | WorkBenches | — |
| `MushroomWorkBench` 蘑菇工作台 | 814 | 18 | WorkBenches | — |
| `SlimeWorkBench` 史莱姆工作台 | 815 | 18 | WorkBenches | — |
| `CactusDoor` 仙人掌门 | 816 | 10 | ClosedDoor | — |
| `FleshDoor` 血肉门 | 817 | 10 | ClosedDoor | — |
| `MushroomDoor` 蘑菇门 | 818 | 10 | ClosedDoor | — |
| `LivingWoodDoor` 生命木门 | 819 | 10 | ClosedDoor | — |
| `BoneDoor` 骨头门 | 820 | 10 | ClosedDoor | — |
| `SunplateBlock` 日盘块 | 824 | 202 | Sunplate | — |
| `DiscWall` 飞盘墙 | 825 | — | — | 82 |
| `SkywareChair` 天域椅 | 826 | 15 | Chairs | — |
| `BoneTable` 骨头桌 | 827 | 14 | Tables | — |
| `FleshTable` 血肉桌 | 828 | 14 | Tables | — |
| `LivingWoodTable` 生命木桌 | 829 | 14 | Tables | — |
| `SkywareTable` 天域桌 | 830 | 14 | Tables | — |
| `LivingWoodChest` 生命木箱 | 831 | 21 | Containers | — |
| `LivingWoodWand` 生命木魔棒 | 832 | 191 | LivingWood | — |
| `PurpleIceBlock` 紫冰雪块 | 833 | 163 | CorruptIce | — |
| `PinkIceBlock` 粉冰雪块 | 834 | 164 | HallowedIce | — |
| `RedIceBlock` 红冰雪块 | 835 | 200 | FleshIce | — |
| `CrimstoneBlock` 猩红石块 | 836 | 203 | Crimstone | — |
| `SkywareDoor` 天域门 | 837 | 10 | ClosedDoor | — |
| `SkywareChest` 天域箱 | 838 | 21 | Containers | — |
| `WorldBanner` 世界旗 | 845 | 91 | Banners | — |
| `SunBanner` 太阳旗 | 846 | 91 | Banners | — |
| `GravityBanner` 重力旗 | 847 | 91 | Banners | — |
| `BluePressurePlate` 蓝压力板 | 852 | 135 | PressurePlates | — |
| `YellowPressurePlate` 黄压力板 | 853 | 135 | PressurePlates | — |
| `BorealWoodSofa` 针叶木沙发 | 858 | 89 | Benches | — |
| `CrimtaneOre` 猩红矿 | 880 | 204 | Crimtane | — |
| `IceBrick` 冰雪砖 | 883 | 206 | IceBrick | — |
| `IceBrickWall` 冰雪砖墙 | 884 | — | — | 84 |
| `PureWaterFountain` 纯净喷泉 | 909 | 207 | WaterFountain | — |
| `DesertWaterFountain` 沙漠喷泉 | 910 | 207 | WaterFountain | — |
| `Shadewood` 暗影木 | 911 | 208 | Shadewood | — |
| `ShadewoodDoor` 暗影木门 | 912 | 10 | ClosedDoor | — |
| `ShadewoodPlatform` 暗影木平台 | 913 | 19 | Platforms | — |
| `ShadewoodChest` 暗影木箱 | 914 | 21 | Containers | — |
| `ShadewoodChair` 暗影木椅 | 915 | 15 | Chairs | — |
| `ShadewoodWorkBench` 暗影木工作台 | 916 | 18 | WorkBenches | — |
| `ShadewoodTable` 暗影木桌 | 917 | 14 | Tables | — |
| `ShadewoodDresser` 暗影木梳妆台 | 918 | 88 | Dressers | — |
| `ShadewoodPiano` 暗影木钢琴 | 919 | 87 | Pianos | — |
| `ShadewoodBed` 暗影木床 | 920 | 79 | Beds | — |
| `ShadewoodWall` 暗影木墙 | 927 | — | — | 85 |
| `Cannon` 大炮 | 928 | 209 | Cannon | — |
| `BoneWand` 白骨魔棒 | 932 | 194 | BoneBlock | — |
| `LeafWand` 树叶魔棒 | 933 | 192 | LeafBlock | — |
| `LandMine` 地雷 | 937 | 210 | LandMine | — |
| `JungleWaterFountain` 丛林喷泉 | 940 | 207 | WaterFountain | — |
| `IcyWaterFountain` 冰雪喷泉 | 941 | 207 | WaterFountain | — |
| `CorruptWaterFountain` 腐化喷泉 | 942 | 207 | WaterFountain | — |
| `CrimsonWaterFountain` 猩红喷泉 | 943 | 207 | WaterFountain | — |
| `HallowedWaterFountain` 神圣喷泉 | 944 | 207 | WaterFountain | — |
| `BloodWaterFountain` 血水喷泉 | 945 | 207 | WaterFountain | — |
| `ChlorophyteOre` 叶绿矿 | 947 | 211 | Chlorophyte | — |
| `SnowballLauncher` 雪球发射器 | 951 | 212 | SnowballLauncher | — |
| `WebCoveredChest` 蛛丝箱 | 952 | 21 | Containers | — |
| `Rope` 绳 | 965 | 213 | Rope | — |
| `Campfire` 篝火 | 966 | 215 | Campfire | — |
| `RedRocket` 红火箭 | 970 | 216 | Firework | — |
| `GreenRocket` 绿火箭 | 971 | 216 | Firework | — |
| `BlueRocket` 蓝火箭 | 972 | 216 | Firework | — |
| `YellowRocket` 黄火箭 | 973 | 216 | Firework | — |
| `IceTorch` 冰雪火把 | 974 | 4 | Torches | — |
| `BlendOMatic` 搅拌机 | 995 | 217 | Blendomatic | — |
| `MeatGrinder` 绞肉机 | 996 | 218 | MeatGrinder | — |
| `Extractinator` 提炼机 | 997 | 219 | Extractinator | — |
| `Solidifier` 固化机 | 998 | 220 | Solidifier | — |
| `Amber` 琥珀 | 999 | 178 | ExposedGems | — |
| `ChlorophyteBar` 叶绿锭 | 1006 | 239 | MetalBars | — |
| `LihzahrdBrick` 丛林蜥蜴砖 | 1101 | 226 | LihzahrdBrick | — |
| `LihzahrdBrickWall` 丛林蜥蜴砖墙 | 1102 | — | — | 112 |
| `SlushBlock` 雪泥块 | 1103 | 224 | Slush | — |
| `PalladiumOre` 钯金矿 | 1104 | 221 | Palladium | — |
| `OrichalcumOre` 山铜矿 | 1105 | 222 | Orichalcum | — |
| `TitaniumOre` 钛金矿 | 1106 | 223 | Titanium | — |
| `TealMushroom` 青绿蘑菇 | 1107 | 227 | DyePlants | — |
| `GreenMushroom` 绿蘑菇 | 1108 | 227 | DyePlants | — |
| `SkyBlueFlower` 天蓝花朵 | 1109 | 227 | DyePlants | — |
| `YellowMarigold` 黄万寿菊 | 1110 | 227 | DyePlants | — |
| `BlueBerries` 蓝浆果 | 1111 | 227 | DyePlants | — |
| `LimeKelp` 橙绿海藻 | 1112 | 227 | DyePlants | — |
| `OrangeBloodroot` 橙血根草 | 1114 | 227 | DyePlants | — |
| `DyeVat` 染缸 | 1120 | 228 | DyeVat | — |
| `HoneyBlock` 蜂蜜块 | 1125 | 229 | HoneyBlock | — |
| `HiveWall` 蜂巢墙 | 1126 | — | — | 108 |
| `CrispyHoneyBlock` 松脆蜂蜜块 | 1127 | 230 | CrispyHoneyBlock | — |
| `HiveWand` 蜂巢魔棒 | 1129 | 225 | Hive | — |
| `LihzahrdDoor` 丛林蜥蜴门 | 1137 | 10 | ClosedDoor | — |
| `DungeonDoor` 地牢门 | 1138 | 10 | ClosedDoor | — |
| `LeadDoor` 铅门 | 1139 | 10 | ClosedDoor | — |
| `IronDoor` 铁门 | 1140 | 10 | ClosedDoor | — |
| `LihzahrdChest` 丛林蜥蜴箱 | 1142 | 21 | Containers | — |
| `LihzahrdChair` 丛林蜥蜴椅 | 1143 | 15 | Chairs | — |
| `LihzahrdTable` 丛林蜥蜴桌 | 1144 | 14 | Tables | — |
| `LihzahrdWorkBench` 丛林蜥蜴工作台 | 1145 | 18 | WorkBenches | — |
| `SuperDartTrap` 超级飞镖机关 | 1146 | 137 | Traps | — |
| `FlameTrap` 烈焰机关 | 1147 | 137 | Traps | — |
| `SpikyBallTrap` 尖球机关 | 1148 | 137 | Traps | — |
| `SpearTrap` 长矛机关 | 1149 | 137 | Traps | — |
| `WoodenSpike` 木尖刺 | 1150 | 232 | WoodenSpikes | — |
| `LihzahrdPressurePlate` 丛林蜥蜴压力板 | 1151 | 135 | PressurePlates | — |
| `LihzahrdStatue` 丛林蜥蜴雕像 | 1152 | 105 | Statues | — |
| `LihzahrdWatcherStatue` 丛林蜥蜴看守人雕像 | 1153 | 105 | Statues | — |
| `LihzahrdGuardianStatue` 丛林蜥蜴守卫雕像 | 1154 | 105 | Statues | — |
| `GraveMarker` 墓石碑 | 1173 | 85 | Tombstones | — |
| `CrossGraveMarker` 十字墓石碑 | 1174 | 85 | Tombstones | — |
| `Headstone` 碑石 | 1175 | 85 | Tombstones | — |
| `Gravestone` 墓碑 | 1176 | 85 | Tombstones | — |
| `Obelisk` 方尖碑 | 1177 | 85 | Tombstones | — |
| `PalladiumBar` 钯金锭 | 1184 | 239 | MetalBars | — |
| `OrichalcumBar` 山铜锭 | 1191 | 239 | MetalBars | — |
| `TitaniumBar` 钛金锭 | 1198 | 239 | MetalBars | — |
| `OrichalcumAnvil` 山铜砧 | 1220 | 134 | MythrilAnvil | — |
| `TitaniumForge` 钛金熔炉 | 1221 | 133 | AdamantiteForge | — |
| `HallowedBar` 神圣锭 | 1225 | 239 | MetalBars | — |
| `OrangeTorch` 橙火把 | 1245 | 4 | Torches | — |
| `CrimsandBlock` 猩红沙块 | 1246 | 234 | Crimsand | — |
| `CrimtaneBar` 猩红矿锭 | 1257 | 239 | MetalBars | — |
| `Teleporter` 传送机 | 1263 | 235 | Teleporter | — |
| `PurpleStainedGlass` 紫花窗玻璃 | 1267 | — | — | 88 |
| `YellowStainedGlass` 黄花窗玻璃 | 1268 | — | — | 89 |
| `BlueStainedGlass` 蓝花窗玻璃 | 1269 | — | — | 90 |
| `GreenStainedGlass` 绿花窗玻璃 | 1270 | — | — | 91 |
| `RedStainedGlass` 红花窗玻璃 | 1271 | — | — | 92 |
| `MulticoloredStainedGlass` 五彩花窗玻璃 | 1272 | — | — | 93 |
| `LihzahrdAltar` 丛林蜥蜴祭坛 | 1292 | 237 | LihzahrdAltar | — |
| `WaterChest` 水中箱 | 1298 | 21 | Containers | — |
| `IchorTorch` 灵液火把 | 1333 | 4 | Torches | — |
| `BunnyCannon` 兔兔炮 | 1337 | 209 | Cannon | — |
| `Cog` 齿轮 | 1344 | 272 | Cog | — |
| `EyeofCthulhuTrophy` 克苏鲁之眼纪念章 | 1360 | 240 | Painting3X3 | — |
| `EaterofWorldsTrophy` 世界吞噬怪纪念章 | 1361 | 240 | Painting3X3 | — |
| `BrainofCthulhuTrophy` 克苏鲁之脑纪念章 | 1362 | 240 | Painting3X3 | — |
| `SkeletronTrophy` 骷髅王纪念章 | 1363 | 240 | Painting3X3 | — |
| `QueenBeeTrophy` 蜂王纪念章 | 1364 | 240 | Painting3X3 | — |
| `WallofFleshTrophy` 血肉墙纪念章 | 1365 | 240 | Painting3X3 | — |
| `DestroyerTrophy` 毁灭者纪念章 | 1366 | 240 | Painting3X3 | — |
| `SkeletronPrimeTrophy` 机械骷髅王纪念章 | 1367 | 240 | Painting3X3 | — |
| `RetinazerTrophy` 激光眼纪念章 | 1368 | 240 | Painting3X3 | — |
| `SpazmatismTrophy` 魔焰眼纪念章 | 1369 | 240 | Painting3X3 | — |
| `PlanteraTrophy` 世纪之花纪念章 | 1370 | 240 | Painting3X3 | — |
| `GolemTrophy` 石巨人纪念章 | 1371 | 240 | Painting3X3 | — |
| `BloodMoonRising` 血月升空 | 1372 | 240 | Painting3X3 | — |
| `TheHangedMan` 倒吊人 | 1373 | 240 | Painting3X3 | — |
| `GloryoftheFire` 烈火荣耀 | 1374 | 240 | Painting3X3 | — |
| `BoneWarp` 扭曲的骨头 | 1375 | 240 | Painting3X3 | — |
| `WallSkeleton` 壁挂骷髅 | 1376 | 240 | Painting3X3 | — |
| `HangingSkeleton` 吊挂骷髅 | 1377 | 240 | Painting3X3 | — |
| `BlueSlabWall` 蓝板墙 | 1378 | — | — | 100 |
| `BlueTiledWall` 蓝瓷砖墙 | 1379 | — | — | 101 |
| `PinkSlabWall` 粉板墙 | 1380 | — | — | 102 |
| `PinkTiledWall` 粉瓷砖墙 | 1381 | — | — | 103 |
| `GreenSlabWall` 绿板墙 | 1382 | — | — | 104 |
| `GreenTiledWall` 绿瓷砖墙 | 1383 | — | — | 105 |
| `BlueBrickPlatform` 蓝砖平台 | 1384 | 19 | Platforms | — |
| `PinkBrickPlatform` 粉砖平台 | 1385 | 19 | Platforms | — |
| `GreenBrickPlatform` 绿砖平台 | 1386 | 19 | Platforms | — |
| `MetalShelf` 金属架 | 1387 | 19 | Platforms | — |
| `BrassShelf` 黄铜架 | 1388 | 19 | Platforms | — |
| `WoodShelf` 木架 | 1389 | 19 | Platforms | — |
| `BrassLantern` 黄铜灯笼 | 1390 | 42 | HangingLanterns | — |
| `CagedLantern` 笼式灯笼 | 1391 | 42 | HangingLanterns | — |
| `CarriageLantern` 马车灯笼 | 1392 | 42 | HangingLanterns | — |
| `AlchemyLantern` 炼金灯笼 | 1393 | 42 | HangingLanterns | — |
| `DiablostLamp` 魔教徒灯 | 1394 | 42 | HangingLanterns | — |
| `OilRagSconse` 油布烛台 | 1395 | 42 | HangingLanterns | — |
| `BlueDungeonChair` 蓝地牢椅 | 1396 | 15 | Chairs | — |
| `BlueDungeonTable` 蓝地牢桌 | 1397 | 14 | Tables | — |
| `BlueDungeonWorkBench` 蓝地牢工作台 | 1398 | 18 | WorkBenches | — |
| `GreenDungeonChair` 绿地牢椅 | 1399 | 15 | Chairs | — |
| `GreenDungeonTable` 绿地牢桌 | 1400 | 14 | Tables | — |
| `GreenDungeonWorkBench` 绿地牢工作台 | 1401 | 18 | WorkBenches | — |
| `PinkDungeonChair` 粉地牢椅 | 1402 | 15 | Chairs | — |
| `PinkDungeonTable` 粉地牢桌 | 1403 | 14 | Tables | — |
| `PinkDungeonWorkBench` 粉地牢工作台 | 1404 | 18 | WorkBenches | — |
| `BlueDungeonCandle` 蓝地牢蜡烛 | 1405 | 33 | Candles | — |
| `GreenDungeonCandle` 绿地牢蜡烛 | 1406 | 33 | Candles | — |
| `PinkDungeonCandle` 粉地牢蜡烛 | 1407 | 33 | Candles | — |
| `BlueDungeonVase` 蓝地牢花瓶 | 1408 | 105 | Statues | — |
| `GreenDungeonVase` 绿地牢花瓶 | 1409 | 105 | Statues | — |
| `PinkDungeonVase` 粉地牢花瓶 | 1410 | 105 | Statues | — |
| `BlueDungeonDoor` 蓝地牢门 | 1411 | 10 | ClosedDoor | — |
| `GreenDungeonDoor` 绿地牢门 | 1412 | 10 | ClosedDoor | — |
| `PinkDungeonDoor` 粉地牢门 | 1413 | 10 | ClosedDoor | — |
| `BlueDungeonBookcase` 蓝地牢书架 | 1414 | 101 | Bookcases | — |
| `GreenDungeonBookcase` 绿地牢书架 | 1415 | 101 | Bookcases | — |
| `PinkDungeonBookcase` 粉地牢书架 | 1416 | 101 | Bookcases | — |
| `Catacomb` 地下墓穴 | 1417 | 241 | Painting4X3 | — |
| `DungeonShelf` 地牢架 | 1418 | 19 | Platforms | — |
| `SkellingtonJSkellingsworth` 骷髅杰克 | 1419 | 240 | Painting3X3 | — |
| `TheCursedMan` 被诅咒的人 | 1420 | 240 | Painting3X3 | — |
| `TheEyeSeestheEnd` 最后一眼 | 1421 | 242 | Painting6X4 | — |
| `SomethingEvilisWatchingYou` 恶魔注视 | 1422 | 242 | Painting6X4 | — |
| `TheTwinsHaveAwoken` 苏醒的双子魔眼 | 1423 | 242 | Painting6X4 | — |
| `TheScreamer` 呐喊者 | 1424 | 242 | Painting6X4 | — |
| `GoblinsPlayingPoker` 玩扑克牌的哥布林 | 1425 | 242 | Painting6X4 | — |
| `Dryadisque` 树妖女 | 1426 | 242 | Painting6X4 | — |
| `Sunflowers` 向日葵 | 1427 | 240 | Painting3X3 | — |
| `TerrarianGothic` 泰拉式哥特 | 1428 | 240 | Painting3X3 | — |
| `ImbuingStation` 灌注站 | 1430 | 243 | ImbuingStation | — |
| `StarinaBottle` 星星瓶 | 1431 | 42 | HangingLanterns | — |
| `Impact` 碰撞 | 1433 | 242 | Painting6X4 | — |
| `PoweredbyBirds` 鸟力驱动 | 1434 | 242 | Painting6X4 | — |
| `TheDestroyer` 毁灭者 | 1435 | 242 | Painting6X4 | — |
| `ThePersistencyofEyes` 永恒之眼 | 1436 | 242 | Painting6X4 | — |
| `UnicornCrossingtheHallows` 越过神圣之地的独角兽 | 1437 | 242 | Painting6X4 | — |
| `GreatWave` 巨浪 | 1438 | 242 | Painting6X4 | — |
| `StarryNight` 星空 | 1439 | 242 | Painting6X4 | — |
| `GuidePicasso` 向导毕加索 | 1440 | 240 | Painting3X3 | — |
| `TheGuardiansGaze` 守卫凝视 | 1441 | 240 | Painting3X3 | — |
| `FatherofSomeone` 某人之父 | 1442 | 240 | Painting3X3 | — |
| `NurseLisa` 丽莎护士 | 1443 | 240 | Painting3X3 | — |
| `WoodenFence` 木栅栏 | 1447 | — | — | 106 |
| `LeadFence` 铅栅栏 | 1448 | — | — | 107 |
| `BubbleMachine` 泡泡机 | 1449 | 244 | BubbleMachine | — |
| `MarchingBonesBanner` 骷髅行军旗 | 1451 | 91 | Banners | — |
| `NecromanticSign` 死灵标旗 | 1452 | 91 | Banners | — |
| `RustedCompanyStandard` 秀逗连队旗 | 1453 | 91 | Banners | — |
| `RaggedBrotherhoodSigil` 丐帮帮旗 | 1454 | 91 | Banners | — |
| `MoltenLegionFlag` 熔火军团旗 | 1455 | 91 | Banners | — |
| `DiabolicSigil` 魔教旗 | 1456 | 91 | Banners | — |
| `ObsidianPlatform` 黑曜石平台 | 1457 | 19 | Platforms | — |
| `ObsidianDoor` 黑曜石门 | 1458 | 10 | ClosedDoor | — |
| `ObsidianChair` 黑曜石椅 | 1459 | 15 | Chairs | — |
| `ObsidianTable` 黑曜石桌 | 1460 | 14 | Tables | — |
| `ObsidianWorkBench` 黑曜石工作台 | 1461 | 18 | WorkBenches | — |
| `ObsidianVase` 黑曜石花瓶 | 1462 | 105 | Statues | — |
| `ObsidianBookcase` 黑曜石书架 | 1463 | 101 | Bookcases | — |
| `HellboundBanner` 地狱之界旗 | 1464 | 91 | Banners | — |
| `HellHammerBanner` 地狱之锤旗 | 1465 | 91 | Banners | — |
| `HelltowerBanner` 地狱之塔旗 | 1466 | 91 | Banners | — |
| `LostHopesofManBanner` 绝望人旗 | 1467 | 91 | Banners | — |
| `ObsidianWatcherBanner` 黑曜石看守人旗 | 1468 | 91 | Banners | — |
| `LavaEruptsBanner` 熔岩喷液旗 | 1469 | 91 | Banners | — |
| `BlueDungeonBed` 蓝地牢床 | 1470 | 79 | Beds | — |
| `GreenDungeonBed` 绿地牢床 | 1471 | 79 | Beds | — |
| `PinkDungeonBed` 粉地牢床 | 1472 | 79 | Beds | — |
| `ObsidianBed` 黑曜石床 | 1473 | 79 | Beds | — |
| `Waldo` 沃尔多 | 1474 | 245 | Painting2X3 | — |
| `Darkness` 黑暗 | 1475 | 245 | Painting2X3 | — |
| `DarkSoulReaper` 暗魂死神 | 1476 | 245 | Painting2X3 | — |
| `Land` 大地 | 1477 | 245 | Painting2X3 | — |
| `TrappedGhost` 受困鬼魂 | 1478 | 245 | Painting2X3 | — |
| `DemonsEye` 恶魔眼 | 1479 | 246 | Painting3X2 | — |
| `FindingGold` 淘金 | 1480 | 246 | Painting3X2 | — |
| `FirstEncounter` 初次邂逅 | 1481 | 246 | Painting3X2 | — |
| `GoodMorning` 早安 | 1482 | 246 | Painting3X2 | — |
| `UndergroundReward` 地下馈赠 | 1483 | 246 | Painting3X2 | — |
| `ThroughtheWindow` 窗外 | 1484 | 246 | Painting3X2 | — |
| `PlaceAbovetheClouds` 云之上 | 1485 | 246 | Painting3X2 | — |
| `DoNotStepontheGrass` 请勿践踏草坪 | 1486 | 246 | Painting3X2 | — |
| `ColdWatersintheWhiteLand` 冰雪寒溪 | 1487 | 246 | Painting3X2 | — |
| `LightlessChasms` 阴暗幽谷 | 1488 | 246 | Painting3X2 | — |
| `TheLandofDeceivingLooks` 浮华假象的大地 | 1489 | 246 | Painting3X2 | — |
| `Daylight` 黎明 | 1490 | 246 | Painting3X2 | — |
| `SecretoftheSands` 沙漠的秘密 | 1491 | 246 | Painting3X2 | — |
| `DeadlandComesAlive` 死地复生 | 1492 | 246 | Painting3X2 | — |
| `EvilPresence` 恶灵现世 | 1493 | 246 | Painting3X2 | — |
| `SkyGuardian` 天空守卫 | 1494 | 246 | Painting3X2 | — |
| `AmericanExplosive` 美洲爆炸狂 | 1495 | 245 | Painting2X3 | — |
| `Discover` 探索 | 1496 | 240 | Painting3X3 | — |
| `HandEarth` 大地之手 | 1497 | 240 | Painting3X3 | — |
| `OldMiner` 老矿工 | 1498 | 240 | Painting3X3 | — |
| `Skelehead` 骨蛇之头 | 1499 | 240 | Painting3X3 | — |
| `FacingtheCerebralMastermind` 直面摄魂师 | 1500 | 242 | Painting6X4 | — |
| `LakeofFire` 火湖 | 1501 | 242 | Painting6X4 | — |
| `TrioSuperHeroes` 超级英雄三剑客 | 1502 | 242 | Painting6X4 | — |
| `GothicChair` 哥特椅 | 1509 | 15 | Chairs | — |
| `GothicTable` 哥特桌 | 1510 | 14 | Tables | — |
| `GothicWorkBench` 哥特工作台 | 1511 | 18 | WorkBenches | — |
| `GothicBookcase` 哥特书架 | 1512 | 101 | Bookcases | — |
| `JungleChest` 丛林箱 | 1528 | 21 | Containers | — |
| `CorruptionChest` 腐化箱 | 1529 | 21 | Containers | — |
| `CrimsonChest` 猩红箱 | 1530 | 21 | Containers | — |
| `HallowedChest` 神圣箱 | 1531 | 21 | Containers | — |
| `FrozenChest` 冰雪箱 | 1532 | 21 | Containers | — |
| `ImpFace` 小鬼脸 | 1538 | 240 | Painting3X3 | — |
| `OminousPresence` 凶兆 | 1539 | 240 | Painting3X3 | — |
| `ShiningMoon` 闪亮月亮 | 1540 | 240 | Painting3X3 | — |
| `LivingGore` 血肉之躯 | 1541 | 246 | Painting3X2 | — |
| `FlowingMagma` 岩浆漫流 | 1542 | 246 | Painting3X2 | — |
| `Autohammer` 自动锤炼机 | 1551 | 247 | Autohammer | — |
| `ShroomiteBar` 蘑菇矿锭 | 1552 | 239 | MetalBars | — |
| `TheCreationoftheGuide` 创造向导 | 1573 | 242 | Painting6X4 | — |
| `TheMerchant` 商人 | 1574 | 240 | Painting3X3 | — |
| `CrownoDevoursHisLunch` Crowno吞噬其餐 | 1575 | 240 | Painting3X3 | — |
| `RareEnchantment` 稀有魔力 | 1576 | 240 | Painting3X3 | — |
| `GloriousNight` 荣耀之夜 | 1577 | 245 | Painting2X3 | — |
| `PalladiumColumn` 钯金柱 | 1589 | 248 | PalladiumColumn | — |
| `PalladiumColumnWall` 钯金柱墙 | 1590 | — | — | 109 |
| `BubblegumBlock` 泡泡糖块 | 1591 | 249 | BubblegumBlock | — |
| `BubblegumBlockWall` 泡泡糖块墙 | 1592 | — | — | 110 |
| `TitanstoneBlock` 钛石块 | 1593 | 250 | Titanstone | — |
| `TitanstoneBlockWall` 钛石块墙 | 1594 | — | — | 111 |
| `MusicBoxSnow` 八音盒（雪原） | 1596 | 139 | MusicBoxes | — |
| `MusicBoxSpace` 八音盒（夜间太空） | 1597 | 139 | MusicBoxes | — |
| `MusicBoxCrimson` 八音盒（猩红之地） | 1598 | 139 | MusicBoxes | — |
| `MusicBoxBoss4` 八音盒(Boss 4) | 1599 | 139 | MusicBoxes | — |
| `MusicBoxAltOverworldDay` 八音盒（人间日备选曲） | 1600 | 139 | MusicBoxes | — |
| `MusicBoxRain` 八音盒（雨） | 1601 | 139 | MusicBoxes | — |
| `MusicBoxIce` 八音盒（冰雪） | 1602 | 139 | MusicBoxes | — |
| `MusicBoxDesert` 八音盒（沙漠） | 1603 | 139 | MusicBoxes | — |
| `MusicBoxOcean` 八音盒（日间海洋） | 1604 | 139 | MusicBoxes | — |
| `MusicBoxDungeon` 八音盒（地牢） | 1605 | 139 | MusicBoxes | — |
| `MusicBoxPlantera` 八音盒（世纪之花） | 1606 | 139 | MusicBoxes | — |
| `MusicBoxBoss5` 八音盒(Boss 5) | 1607 | 139 | MusicBoxes | — |
| `MusicBoxTemple` 八音盒（神庙） | 1608 | 139 | MusicBoxes | — |
| `MusicBoxEclipse` 八音盒（日食） | 1609 | 139 | MusicBoxes | — |
| `MusicBoxMushrooms` 八音盒（蘑菇） | 1610 | 139 | MusicBoxes | — |
| `AnglerFishBanner` 琵琶鱼旗 | 1615 | 91 | Banners | — |
| `AngryNimbusBanner` 愤怒雨云怪旗 | 1616 | 91 | Banners | — |
| `AnomuraFungusBanner` 歪尾真菌旗 | 1617 | 91 | Banners | — |
| `AntlionBanner` 蚁狮旗 | 1618 | 91 | Banners | — |
| `ArapaimaBanner` 巨骨舌鱼旗 | 1619 | 91 | Banners | — |
| `ArmoredSkeletonBanner` 装甲骷髅旗 | 1620 | 91 | Banners | — |
| `BatBanner` 洞穴蝙蝠旗 | 1621 | 91 | Banners | — |
| `BirdBanner` 鸟旗 | 1622 | 91 | Banners | — |
| `BlackRecluseBanner` 黑隐士旗 | 1623 | 91 | Banners | — |
| `BloodFeederBanner` 嗜血怪旗 | 1624 | 91 | Banners | — |
| `BloodJellyBanner` 血水母旗 | 1625 | 91 | Banners | — |
| `BloodCrawlerBanner` 血爬虫旗 | 1626 | 91 | Banners | — |
| `BoneSerpentBanner` 骨蛇旗 | 1627 | 91 | Banners | — |
| `BunnyBanner` 兔兔旗 | 1628 | 91 | Banners | — |
| `ChaosElementalBanner` 混沌精旗 | 1629 | 91 | Banners | — |
| `MimicBanner` 宝箱怪旗 | 1630 | 91 | Banners | — |
| `ClownBanner` 小丑旗 | 1631 | 91 | Banners | — |
| `CorruptBunnyBanner` 腐化兔兔旗 | 1632 | 91 | Banners | — |
| `CorruptGoldfishBanner` 腐化金鱼旗 | 1633 | 91 | Banners | — |
| `CrabBanner` 螃蟹旗 | 1634 | 91 | Banners | — |
| `CrimeraBanner` 猩红喀迈拉旗 | 1635 | 91 | Banners | — |
| `CrimsonAxeBanner` 猩红斧旗 | 1636 | 91 | Banners | — |
| `CursedHammerBanner` 诅咒锤旗 | 1637 | 91 | Banners | — |
| `DemonBanner` 恶魔旗 | 1638 | 91 | Banners | — |
| `DemonEyeBanner` 恶魔眼旗 | 1639 | 91 | Banners | — |
| `DerplingBanner` 跳跳兽旗 | 1640 | 91 | Banners | — |
| `EaterofSoulsBanner` 噬魂怪旗 | 1641 | 91 | Banners | — |
| `EnchantedSwordBanner` 附魔剑旗 | 1642 | 91 | Banners | — |
| `ZombieEskimoBanner` 冰冻僵尸旗 | 1643 | 91 | Banners | — |
| `FaceMonsterBanner` 脸怪旗 | 1644 | 91 | Banners | — |
| `FloatyGrossBanner` 恶心浮游怪旗 | 1645 | 91 | Banners | — |
| `FlyingFishBanner` 飞鱼旗 | 1646 | 91 | Banners | — |
| `FlyingSnakeBanner` 飞蛇旗 | 1647 | 91 | Banners | — |
| `FrankensteinBanner` 科学怪人旗 | 1648 | 91 | Banners | — |
| `FungiBulbBanner` 真菌球怪旗 | 1649 | 91 | Banners | — |
| `FungoFishBanner` 蘑菇鱼旗 | 1650 | 91 | Banners | — |
| `GastropodBanner` 腹足怪旗 | 1651 | 91 | Banners | — |
| `GoblinThiefBanner` 哥布林盗贼旗 | 1652 | 91 | Banners | — |
| `GoblinSorcererBanner` 哥布林巫士旗 | 1653 | 91 | Banners | — |
| `GoblinPeonBanner` 哥布林苦力旗 | 1654 | 91 | Banners | — |
| `GoblinScoutBanner` 哥布林侦察兵旗 | 1655 | 91 | Banners | — |
| `GoblinWarriorBanner` 哥布林战士旗 | 1656 | 91 | Banners | — |
| `GoldfishBanner` 金鱼旗 | 1657 | 91 | Banners | — |
| `HarpyBanner` 鸟妖旗 | 1658 | 91 | Banners | — |
| `HellbatBanner` 地狱蝙蝠旗 | 1659 | 91 | Banners | — |
| `HerplingBanner` 蹦蹦兽旗 | 1660 | 91 | Banners | — |
| `HornetBanner` 黄蜂旗 | 1661 | 91 | Banners | — |
| `IceElementalBanner` 冰雪精旗 | 1662 | 91 | Banners | — |
| `IcyMermanBanner` 冰雪人鱼旗 | 1663 | 91 | Banners | — |
| `FireImpBanner` 火焰小鬼旗 | 1664 | 91 | Banners | — |
| `JellyfishBanner` 蓝水母旗 | 1665 | 91 | Banners | — |
| `JungleCreeperBanner` 丛林蜘蛛旗 | 1666 | 91 | Banners | — |
| `LihzahrdBanner` 丛林蜥蜴旗 | 1667 | 91 | Banners | — |
| `ManEaterBanner` 食人怪旗 | 1668 | 91 | Banners | — |
| `MeteorHeadBanner` 流星头旗 | 1669 | 91 | Banners | — |
| `MothBanner` 飞蛾旗 | 1670 | 91 | Banners | — |
| `MummyBanner` 木乃伊旗 | 1671 | 91 | Banners | — |
| `MushiLadybugBanner` 瓢虫旗 | 1672 | 91 | Banners | — |
| `ParrotBanner` 鹦鹉旗 | 1673 | 91 | Banners | — |
| `PigronBanner` 猪龙旗 | 1674 | 91 | Banners | — |
| `PiranhaBanner` 食人鱼旗 | 1675 | 91 | Banners | — |
| `PirateBanner` 海盗水手旗 | 1676 | 91 | Banners | — |
| `PixieBanner` 妖精旗 | 1677 | 91 | Banners | — |
| `RaincoatZombieBanner` 雨衣僵尸旗 | 1678 | 91 | Banners | — |
| `ReaperBanner` 死神旗 | 1679 | 91 | Banners | — |
| `SharkBanner` 鲨鱼旗 | 1680 | 91 | Banners | — |
| `SkeletonBanner` 骷髅旗 | 1681 | 91 | Banners | — |
| `SkeletonMageBanner` 暗黑法师旗 | 1682 | 91 | Banners | — |
| `SlimeBanner` 蓝史莱姆旗 | 1683 | 91 | Banners | — |
| `SnowFlinxBanner` 小雪怪旗 | 1684 | 91 | Banners | — |
| `SpiderBanner` 爬墙蜘蛛旗 | 1685 | 91 | Banners | — |
| `SporeZombieBanner` 孢子僵尸旗 | 1686 | 91 | Banners | — |
| `SwampThingBanner` 沼泽怪旗 | 1687 | 91 | Banners | — |
| `TortoiseBanner` 巨型陆龟旗 | 1688 | 91 | Banners | — |
| `ToxicSludgeBanner` 毒泥旗 | 1689 | 91 | Banners | — |
| `UmbrellaSlimeBanner` 雨伞史莱姆旗 | 1690 | 91 | Banners | — |
| `UnicornBanner` 独角兽旗 | 1691 | 91 | Banners | — |
| `VampireBanner` 吸血鬼旗 | 1692 | 91 | Banners | — |
| `VultureBanner` 秃鹰旗 | 1693 | 91 | Banners | — |
| `NypmhBanner` 宁芙旗 | 1694 | 91 | Banners | — |
| `WerewolfBanner` 狼人旗 | 1695 | 91 | Banners | — |
| `WolfBanner` 狼旗 | 1696 | 91 | Banners | — |
| `WorldFeederBanner` 吞世怪旗 | 1697 | 91 | Banners | — |
| `WormBanner` 蠕虫旗 | 1698 | 91 | Banners | — |
| `WraithBanner` 幻灵旗 | 1699 | 91 | Banners | — |
| `WyvernBanner` 飞龙旗 | 1700 | 91 | Banners | — |
| `ZombieBanner` 僵尸旗 | 1701 | 91 | Banners | — |
| `GlassPlatform` 玻璃平台 | 1702 | 19 | Platforms | — |
| `GlassChair` 玻璃椅 | 1703 | 15 | Chairs | — |
| `GoldenChair` 金椅 | 1704 | 15 | Chairs | — |
| `GoldenToilet` 金马桶 | 1705 | 15 | Chairs | — |
| `BarStool` 高脚凳 | 1706 | 15 | Chairs | — |
| `HoneyChair` 蜂蜜椅 | 1707 | 15 | Chairs | — |
| `SteampunkChair` 蒸汽朋克椅 | 1708 | 15 | Chairs | — |
| `GlassDoor` 玻璃门 | 1709 | 10 | ClosedDoor | — |
| `GoldenDoor` 金门 | 1710 | 10 | ClosedDoor | — |
| `HoneyDoor` 蜂蜜门 | 1711 | 10 | ClosedDoor | — |
| `SteampunkDoor` 蒸汽朋克门 | 1712 | 10 | ClosedDoor | — |
| `GlassTable` 玻璃桌 | 1713 | 14 | Tables | — |
| `BanquetTable` 宴会桌 | 1714 | 14 | Tables | — |
| `Bar` 锭 | 1715 | 14 | Tables | — |
| `GoldenTable` 金桌 | 1716 | 14 | Tables | — |
| `HoneyTable` 蜂蜜桌 | 1717 | 14 | Tables | — |
| `SteampunkTable` 蒸汽朋克桌 | 1718 | 14 | Tables | — |
| `GlassBed` 玻璃床 | 1719 | 79 | Beds | — |
| `GoldenBed` 金床 | 1720 | 79 | Beds | — |
| `HoneyBed` 蜂蜜床 | 1721 | 79 | Beds | — |
| `SteampunkBed` 蒸汽朋克床 | 1722 | 79 | Beds | — |
| `LivingWoodWall` 生命木墙 | 1723 | — | — | 78 |
| `Pumpkin` 南瓜 | 1725 | 251 | PumpkinBlock | — |
| `PumpkinWall` 南瓜墙 | 1726 | — | — | 113 |
| `Hay` 干草 | 1727 | 252 | HayBlock | — |
| `HayWall` 干草墙 | 1728 | — | — | 114 |
| `SpookyWood` 阴森木 | 1729 | 253 | SpookyWood | — |
| `SpookyWoodWall` 阴森木墙 | 1730 | — | — | 115 |
| `Cauldron` 大锅 | 1791 | 96 | CookingPots | — |
| `PumpkinChair` 南瓜椅 | 1792 | 15 | Chairs | — |
| `PumpkinDoor` 南瓜门 | 1793 | 10 | ClosedDoor | — |
| `PumpkinTable` 南瓜桌 | 1794 | 14 | Tables | — |
| `PumpkinWorkBench` 南瓜工作台 | 1795 | 18 | WorkBenches | — |
| `PumpkinPlatform` 南瓜平台 | 1796 | 19 | Platforms | — |
| `HangingJackOLantern` 杰克南瓜挂灯 | 1808 | 42 | HangingLanterns | — |
| `Jackelier` 南瓜头吊灯 | 1812 | 34 | Chandeliers | — |
| `JackOLantern` 杰克南瓜灯 | 1813 | 35 | Jackolanterns | — |
| `SpookyChair` 阴森椅 | 1814 | 15 | Chairs | — |
| `SpookyDoor` 阴森门 | 1815 | 10 | ClosedDoor | — |
| `SpookyTable` 阴森桌 | 1816 | 14 | Tables | — |
| `SpookyWorkBench` 阴森工作台 | 1817 | 18 | WorkBenches | — |
| `SpookyPlatform` 阴森木平台 | 1818 | 19 | Platforms | — |
| `PumpkinSeed` 南瓜子 | 1828 | 254 | Pumpkins | — |
| `JackingSkeletron` 骷髅夜惊魂 | 1846 | 242 | Painting6X4 | — |
| `BitterHarvest` 苦涩收获 | 1847 | 242 | Painting6X4 | — |
| `BloodMoonCountess` 血月伯爵夫人 | 1848 | 242 | Painting6X4 | — |
| `HallowsEve` 万圣节前夕 | 1849 | 242 | Painting6X4 | — |
| `MorbidCuriosity` 病态好奇心 | 1850 | 242 | Painting6X4 | — |
| `MourningWoodTrophy` 哀木纪念章 | 1855 | 240 | Painting3X3 | — |
| `PumpkingTrophy` 南瓜王纪念章 | 1856 | 240 | Painting3X3 | — |
| `HeartLantern` 红心灯笼 | 1859 | 42 | HangingLanterns | — |
| `Present` 礼物 | 1869 | 36 | Presents | — |
| `PineTreeBlock` 松树块 | 1872 | 170 | PineTree | — |
| `ChristmasTree` 圣诞树 | 1873 | 171 | ChristmasTree | — |
| `Holly` 霍利 | 1908 | 246 | Painting3X2 | — |
| `PineDoor` 松树门 | 1924 | 10 | ClosedDoor | — |
| `PineChair` 松树椅 | 1925 | 15 | Chairs | — |
| `PineTable` 松树桌 | 1926 | 14 | Tables | — |
| `ChristmasTreeWallpaper` 圣诞树壁纸 | 1948 | — | — | 116 |
| `OrnamentWallpaper` 装饰壁纸 | 1949 | — | — | 117 |
| `CandyCaneWallpaper` 糖棒壁纸 | 1950 | — | — | 118 |
| `FestiveWallpaper` 节日壁纸 | 1951 | — | — | 119 |
| `StarsWallpaper` 星星壁纸 | 1952 | — | — | 120 |
| `SquigglesWallpaper` 波浪条纹壁纸 | 1953 | — | — | 121 |
| `SnowflakeWallpaper` 雪花壁纸 | 1954 | — | — | 122 |
| `KrampusHornWallpaper` 坎卜斯犄角壁纸 | 1955 | — | — | 123 |
| `BluegreenWallpaper` 蓝绿壁纸 | 1956 | — | — | 124 |
| `GrinchFingerWallpaper` 格林奇手指壁纸 | 1957 | — | — | 125 |
| `IceQueenTrophy` 冰雪女王纪念章 | 1960 | 240 | Painting3X3 | — |
| `SantaNK1Trophy` 圣诞坦克纪念章 | 1961 | 240 | Painting3X3 | — |
| `EverscreamTrophy` 常绿尖叫怪纪念章 | 1962 | 240 | Painting3X3 | — |
| `MusicBoxPumpkinMoon` 八音盒（南瓜月） | 1963 | 139 | MusicBoxes | — |
| `MusicBoxAltUnderground` 八音盒（地下备选曲） | 1964 | 139 | MusicBoxes | — |
| `MusicBoxFrostMoon` 八音盒（霜月） | 1965 | 139 | MusicBoxes | — |
| `AmethystGemsparkBlock` 紫晶晶莹宝石块 | 1970 | 262 | AmethystGemspark | — |
| `TopazGemsparkBlock` 黄玉晶莹宝石块 | 1971 | 263 | TopazGemspark | — |
| `SapphireGemsparkBlock` 蓝玉晶莹宝石块 | 1972 | 264 | SapphireGemspark | — |
| `EmeraldGemsparkBlock` 翡翠晶莹宝石块 | 1973 | 265 | EmeraldGemspark | — |
| `RubyGemsparkBlock` 红玉晶莹宝石块 | 1974 | 266 | RubyGemspark | — |
| `DiamondGemsparkBlock` 钻石晶莹宝石块 | 1975 | 267 | DiamondGemspark | — |
| `AmberGemsparkBlock` 琥珀晶莹宝石块 | 1976 | 268 | AmberGemspark | — |
| `Womannquin` 女模特 | 1989 | 470 | DisplayDoll | — |
| `FireflyinaBottle` 萤火虫瓶 | 1993 | 270 | FireflyinaBottle | — |
| `LightningBuginaBottle` 荧光虫瓶 | 2005 | 271 | LightningBuginaBottle | — |
| `FancyGreyWallpaper` 别致灰壁纸 | 2008 | — | — | 126 |
| `IceFloeWallpaper` 浮冰壁纸 | 2009 | — | — | 127 |
| `MusicWallpaper` 音符壁纸 | 2010 | — | — | 128 |
| `PurpleRainWallpaper` 紫雨壁纸 | 2011 | — | — | 129 |
| `RainbowWallpaper` 彩虹壁纸 | 2012 | — | — | 130 |
| `SparkleStoneWallpaper` 烁石壁纸 | 2013 | — | — | 131 |
| `StarlitHeavenWallpaper` 星光天堂壁纸 | 2014 | — | — | 132 |
| `CactusBookcase` 仙人掌书架 | 2020 | 101 | Bookcases | — |
| `EbonwoodBookcase` 乌木书架 | 2021 | 101 | Bookcases | — |
| `FleshBookcase` 血肉书架 | 2022 | 101 | Bookcases | — |
| `HoneyBookcase` 蜂蜜书架 | 2023 | 101 | Bookcases | — |
| `SteampunkBookcase` 蒸汽朋克书架 | 2024 | 101 | Bookcases | — |
| `GlassBookcase` 玻璃书架 | 2025 | 101 | Bookcases | — |
| `RichMahoganyBookcase` 红木书架 | 2026 | 101 | Bookcases | — |
| `PearlwoodBookcase` 珍珠木书架 | 2027 | 101 | Bookcases | — |
| `SpookyBookcase` 阴森书架 | 2028 | 101 | Bookcases | — |
| `SkywareBookcase` 天域书架 | 2029 | 101 | Bookcases | — |
| `LihzahrdBookcase` 丛林蜥蜴书架 | 2030 | 101 | Bookcases | — |
| `FrozenBookcase` 冰冻书架 | 2031 | 101 | Bookcases | — |
| `CactusLantern` 仙人掌灯笼 | 2032 | 42 | HangingLanterns | — |
| `EbonwoodLantern` 乌木灯笼 | 2033 | 42 | HangingLanterns | — |
| `FleshLantern` 血肉灯笼 | 2034 | 42 | HangingLanterns | — |
| `HoneyLantern` 蜂蜜灯笼 | 2035 | 42 | HangingLanterns | — |
| `SteampunkLantern` 蒸汽朋克灯笼 | 2036 | 42 | HangingLanterns | — |
| `GlassLantern` 玻璃灯笼 | 2037 | 42 | HangingLanterns | — |
| `RichMahoganyLantern` 红木灯笼 | 2038 | 42 | HangingLanterns | — |
| `PearlwoodLantern` 珍珠木灯笼 | 2039 | 42 | HangingLanterns | — |
| `FrozenLantern` 冰冻灯笼 | 2040 | 42 | HangingLanterns | — |
| `LihzahrdLantern` 丛林蜥蜴灯笼 | 2041 | 42 | HangingLanterns | — |
| `SkywareLantern` 天域灯笼 | 2042 | 42 | HangingLanterns | — |
| `SpookyLantern` 阴森灯笼 | 2043 | 42 | HangingLanterns | — |
| `FrozenDoor` 冰冻门 | 2044 | 10 | ClosedDoor | — |
| `CactusCandle` 仙人掌蜡烛 | 2045 | 33 | Candles | — |
| `EbonwoodCandle` 乌木蜡烛 | 2046 | 33 | Candles | — |
| `FleshCandle` 血肉蜡烛 | 2047 | 33 | Candles | — |
| `GlassCandle` 玻璃蜡烛 | 2048 | 33 | Candles | — |
| `FrozenCandle` 冰冻蜡烛 | 2049 | 33 | Candles | — |
| `RichMahoganyCandle` 红木蜡烛 | 2050 | 33 | Candles | — |
| `PearlwoodCandle` 珍珠木蜡烛 | 2051 | 33 | Candles | — |
| `LihzahrdCandle` 丛林蜥蜴蜡烛 | 2052 | 33 | Candles | — |
| `SkywareCandle` 天域蜡烛 | 2053 | 33 | Candles | — |
| `PumpkinCandle` 南瓜蜡烛 | 2054 | 33 | Candles | — |
| `CactusChandelier` 仙人掌吊灯 | 2055 | 34 | Chandeliers | — |
| `EbonwoodChandelier` 乌木吊灯 | 2056 | 34 | Chandeliers | — |
| `FleshChandelier` 血肉吊灯 | 2057 | 34 | Chandeliers | — |
| `HoneyChandelier` 蜂蜜吊灯 | 2058 | 34 | Chandeliers | — |
| `FrozenChandelier` 冰冻吊灯 | 2059 | 34 | Chandeliers | — |
| `RichMahoganyChandelier` 红木吊灯 | 2060 | 34 | Chandeliers | — |
| `PearlwoodChandelier` 珍珠木吊灯 | 2061 | 34 | Chandeliers | — |
| `LihzahrdChandelier` 丛林蜥蜴吊灯 | 2062 | 34 | Chandeliers | — |
| `SkywareChandelier` 天域吊灯 | 2063 | 34 | Chandeliers | — |
| `SpookyChandelier` 阴森吊灯 | 2064 | 34 | Chandeliers | — |
| `GlassChandelier` 玻璃吊灯 | 2065 | 34 | Chandeliers | — |
| `CactusBed` 仙人掌床 | 2066 | 79 | Beds | — |
| `FleshBed` 血肉床 | 2067 | 79 | Beds | — |
| `FrozenBed` 冰冻床 | 2068 | 79 | Beds | — |
| `LihzahrdBed` 丛林蜥蜴床 | 2069 | 79 | Beds | — |
| `SkywareBed` 天域床 | 2070 | 79 | Beds | — |
| `SpookyBed` 阴森床 | 2071 | 79 | Beds | — |
| `CactusBathtub` 仙人掌浴缸 | 2072 | 90 | Bathtubs | — |
| `EbonwoodBathtub` 乌木浴缸 | 2073 | 90 | Bathtubs | — |
| `FleshBathtub` 血肉浴缸 | 2074 | 90 | Bathtubs | — |
| `GlassBathtub` 玻璃浴缸 | 2075 | 90 | Bathtubs | — |
| `FrozenBathtub` 冰冻浴缸 | 2076 | 90 | Bathtubs | — |
| `RichMahoganyBathtub` 红木浴缸 | 2077 | 90 | Bathtubs | — |
| `PearlwoodBathtub` 珍珠木浴缸 | 2078 | 90 | Bathtubs | — |
| `LihzahrdBathtub` 丛林蜥蜴浴缸 | 2079 | 90 | Bathtubs | — |
| `SkywareBathtub` 天域浴缸 | 2080 | 90 | Bathtubs | — |
| `SpookyBathtub` 阴森浴缸 | 2081 | 90 | Bathtubs | — |
| `CactusLamp` 仙人掌灯 | 2082 | 93 | Lamps | — |
| `EbonwoodLamp` 乌木灯 | 2083 | 93 | Lamps | — |
| `FleshLamp` 血肉灯 | 2084 | 93 | Lamps | — |
| `GlassLamp` 玻璃灯 | 2085 | 93 | Lamps | — |
| `FrozenLamp` 冰冻灯 | 2086 | 93 | Lamps | — |
| `RichMahoganyLamp` 红木灯 | 2087 | 93 | Lamps | — |
| `PearlwoodLamp` 珍珠木灯 | 2088 | 93 | Lamps | — |
| `LihzahrdLamp` 丛林蜥蜴灯 | 2089 | 93 | Lamps | — |
| `SkywareLamp` 天域灯 | 2090 | 93 | Lamps | — |
| `SpookyLamp` 阴森灯 | 2091 | 93 | Lamps | — |
| `CactusCandelabra` 仙人掌烛台 | 2092 | 100 | Candelabras | — |
| `EbonwoodCandelabra` 乌木烛台 | 2093 | 100 | Candelabras | — |
| `FleshCandelabra` 血肉烛台 | 2094 | 100 | Candelabras | — |
| `HoneyCandelabra` 蜂蜜烛台 | 2095 | 100 | Candelabras | — |
| `SteampunkCandelabra` 蒸汽朋克烛台 | 2096 | 100 | Candelabras | — |
| `GlassCandelabra` 玻璃烛台 | 2097 | 100 | Candelabras | — |
| `RichMahoganyCandelabra` 红木大烛台 | 2098 | 100 | Candelabras | — |
| `PearlwoodCandelabra` 珍珠木烛台 | 2099 | 100 | Candelabras | — |
| `FrozenCandelabra` 冰冻烛台 | 2100 | 100 | Candelabras | — |
| `LihzahrdCandelabra` 丛林蜥蜴烛台 | 2101 | 100 | Candelabras | — |
| `SkywareCandelabra` 天域烛台 | 2102 | 100 | Candelabras | — |
| `SpookyCandelabra` 阴森大烛台 | 2103 | 100 | Candelabras | — |
| `BlacksmithRack` 铁匠架 | 2114 | 240 | Painting3X3 | — |
| `CarpentryRack` 木工架 | 2115 | 240 | Painting3X3 | — |
| `HelmetRack` 头盔架 | 2116 | 240 | Painting3X3 | — |
| `SpearRack` 长矛架 | 2117 | 240 | Painting3X3 | — |
| `SwordRack` 剑架 | 2118 | 240 | Painting3X3 | — |
| `StoneSlab` 石板 | 2119 | 273 | StoneSlab | — |
| `SandstoneSlab` 沙岩板 | 2120 | 274 | SandStoneSlab | — |
| `HoneyBathtub` 蜂蜜浴缸 | 2124 | 90 | Bathtubs | — |
| `SteampunkBathtub` 蒸汽朋克浴缸 | 2125 | 90 | Bathtubs | — |
| `LivingWoodBathtub` 生命木浴缸 | 2126 | 90 | Bathtubs | — |
| `ShadewoodBathtub` 暗影木浴缸 | 2127 | 90 | Bathtubs | — |
| `BoneBathtub` 骨头浴缸 | 2128 | 90 | Bathtubs | — |
| `HoneyLamp` 蜂蜜灯 | 2129 | 93 | Lamps | — |
| `SteampunkLamp` 蒸汽朋克灯 | 2130 | 93 | Lamps | — |
| `LivingWoodLamp` 生命木灯 | 2131 | 93 | Lamps | — |
| `ShadewoodLamp` 暗影木灯 | 2132 | 93 | Lamps | — |
| `GoldenLamp` 金灯 | 2133 | 93 | Lamps | — |
| `BoneLamp` 骨头灯 | 2134 | 93 | Lamps | — |
| `LivingWoodBookcase` 生命木书架 | 2135 | 101 | Bookcases | — |
| `ShadewoodBookcase` 暗影木书架 | 2136 | 101 | Bookcases | — |
| `GoldenBookcase` 金书架 | 2137 | 101 | Bookcases | — |
| `BoneBookcase` 骨头书架 | 2138 | 101 | Bookcases | — |
| `LivingWoodBed` 生命木床 | 2139 | 79 | Beds | — |
| `BoneBed` 骨头床 | 2140 | 79 | Beds | — |
| `LivingWoodChandelier` 生命木吊灯 | 2141 | 34 | Chandeliers | — |
| `ShadewoodChandelier` 暗影木吊灯 | 2142 | 34 | Chandeliers | — |
| `GoldenChandelier` 金制吊灯 | 2143 | 34 | Chandeliers | — |
| `BoneChandelier` 骨头吊灯 | 2144 | 34 | Chandeliers | — |
| `LivingWoodLantern` 生命木灯笼 | 2145 | 42 | HangingLanterns | — |
| `ShadewoodLantern` 暗影木灯笼 | 2146 | 42 | HangingLanterns | — |
| `GoldenLantern` 金灯笼 | 2147 | 42 | HangingLanterns | — |
| `BoneLantern` 骨头灯笼 | 2148 | 42 | HangingLanterns | — |
| `LivingWoodCandelabra` 生命木烛台 | 2149 | 100 | Candelabras | — |
| `ShadewoodCandelabra` 暗影木烛台 | 2150 | 100 | Candelabras | — |
| `GoldenCandelabra` 金烛台 | 2151 | 100 | Candelabras | — |
| `BoneCandelabra` 骨头烛台 | 2152 | 100 | Candelabras | — |
| `LivingWoodCandle` 生命木蜡烛 | 2153 | 33 | Candles | — |
| `ShadewoodCandle` 暗影木蜡烛 | 2154 | 33 | Candles | — |
| `GoldenCandle` 金蜡烛 | 2155 | 33 | Candles | — |
| `BubbleWallpaper` 泡泡壁纸 | 2158 | — | — | 133 |
| `CopperPipeWallpaper` 铜管壁纸 | 2159 | — | — | 134 |
| `DuckyWallpaper` 黄鸭壁纸 | 2160 | — | — | 135 |
| `BunnyCage` 兔兔笼 | 2162 | 275 | BunnyCage | — |
| `SquirrelCage` 松鼠笼 | 2163 | 276 | SquirrelCage | — |
| `MallardDuckCage` 野鸭笼 | 2164 | 277 | MallardDuckCage | — |
| `DuckCage` 鸭笼 | 2165 | 278 | DuckCage | — |
| `BirdCage` 鸟笼 | 2166 | 279 | BirdCage | — |
| `BlueJayCage` 冠蓝鸦笼 | 2167 | 280 | BlueJay | — |
| `CardinalCage` 红雀笼 | 2168 | 281 | CardinalCage | — |
| `WaterfallWall` 瀑布墙 | 2169 | — | — | 136 |
| `LavafallWall` 熔岩瀑布墙 | 2170 | — | — | 137 |
| `CrimsonSeeds` 猩红种子 | 2171 | 199 | CrimsonGrass | — |
| `HeavyWorkBench` 重型工作台 | 2172 | 283 | HeavyWorkBench | — |
| `CopperPlating` 铜镀层 | 2173 | 284 | CopperPlating | — |
| `SnailCage` 蜗牛笼 | 2174 | 285 | SnailCage | — |
| `GlowingSnailCage` 发光蜗牛笼 | 2175 | 286 | GlowingSnailCage | — |
| `AmmoBox` 弹药箱 | 2177 | 287 | AmmoBox | — |
| `MonarchButterflyJar` 帝王蝶罐 | 2178 | 288 | MonarchButterflyJar | — |
| `PurpleEmperorButterflyJar` 紫君王蝶罐 | 2179 | 289 | PurpleEmperorButterflyJar | — |
| `RedAdmiralButterflyJar` 红纹蝶罐 | 2180 | 290 | RedAdmiralButterflyJar | — |
| `UlyssesButterflyJar` 尤利西斯蝶罐 | 2181 | 291 | UlyssesButterflyJar | — |
| `SulphurButterflyJar` 硫磺蝶罐 | 2182 | 292 | SulphurButterflyJar | — |
| `TreeNymphButterflyJar` 树若虫蝴蝶罐 | 2183 | 293 | TreeNymphButterflyJar | — |
| `ZebraSwallowtailButterflyJar` 斑纹燕尾蝶罐 | 2184 | 294 | ZebraSwallowtailButterflyJar | — |
| `JuliaButterflyJar` 茱莉娅蝶罐 | 2185 | 295 | JuliaButterflyJar | — |
| `ScorpionCage` 蝎子笼 | 2186 | 296 | ScorpionCage | — |
| `BlackScorpionCage` 黑蝎子笼 | 2187 | 297 | BlackScorpionCage | — |
| `FrogCage` 蛙笼 | 2190 | 298 | FrogCage | — |
| `MouseCage` 老鼠笼 | 2191 | 299 | MouseCage | — |
| `BoneWelder` 骨头焊机 | 2192 | 300 | BoneWelder | — |
| `FleshCloningVaat` 血肉克隆台 | 2193 | 301 | FleshCloningVat | — |
| `GlassKiln` 玻璃窑 | 2194 | 302 | GlassKiln | — |
| `LihzahrdFurnace` 丛林蜥蜴熔炉 | 2195 | 303 | LihzahrdFurnace | — |
| `LivingLoom` 生命木织机 | 2196 | 304 | LivingLoom | — |
| `SkyMill` 天磨 | 2197 | 305 | SkyMill | — |
| `IceMachine` 冰雪机 | 2198 | 306 | IceMachine | — |
| `SteampunkBoiler` 蒸汽朋克锅炉 | 2203 | 307 | SteampunkBoiler | — |
| `HoneyDispenser` 蜂蜜分配机 | 2204 | 308 | HoneyDispenser | — |
| `PenguinCage` 企鹅笼 | 2206 | 309 | PenguinCage | — |
| `WormCage` 蠕虫笼 | 2207 | 310 | WormCage | — |
| `EbonwoodFence` 乌木栅栏 | 2210 | — | — | 138 |
| `RichMahoganyFence` 红木栅栏 | 2211 | — | — | 139 |
| `PearlwoodFence` 珍珠木栅栏 | 2212 | — | — | 140 |
| `ShadewoodFence` 暗影木栅栏 | 2213 | — | — | 141 |
| `DynastyChandelier` 大王朝灯笼 | 2224 | 34 | Chandeliers | — |
| `DynastyLamp` 王朝灯 | 2225 | 93 | Lamps | — |
| `DynastyLantern` 王朝灯笼 | 2226 | 42 | HangingLanterns | — |
| `DynastyCandelabra` 大王朝蜡烛 | 2227 | 100 | Candelabras | — |
| `DynastyChair` 王朝椅 | 2228 | 15 | Chairs | — |
| `DynastyWorkBench` 王朝工作台 | 2229 | 18 | WorkBenches | — |
| `DynastyChest` 王朝箱 | 2230 | 21 | Containers | — |
| `DynastyBed` 王朝床 | 2231 | 79 | Beds | — |
| `DynastyBathtub` 王朝浴缸 | 2232 | 90 | Bathtubs | — |
| `DynastyBookcase` 王朝书架 | 2233 | 101 | Bookcases | — |
| `DynastyCup` 王朝杯 | 2234 | 13 | Bottles | — |
| `DynastyBowl` 王朝碗 | 2235 | 103 | Bowls | — |
| `DynastyCandle` 王朝蜡烛 | 2236 | 33 | Candles | — |
| `DynastyClock` 王朝时钟 | 2237 | 104 | GrandfatherClocks | — |
| `GoldenClock` 金时钟 | 2238 | 104 | GrandfatherClocks | — |
| `GlassClock` 玻璃时钟 | 2239 | 104 | GrandfatherClocks | — |
| `HoneyClock` 蜂蜜时钟 | 2240 | 104 | GrandfatherClocks | — |
| `SteampunkClock` 蒸汽朋克时钟 | 2241 | 104 | GrandfatherClocks | — |
| `FancyDishes` 精致餐具 | 2242 | 103 | Bowls | — |
| `GlassBowl` 玻璃碗 | 2243 | 103 | Bowls | — |
| `WineGlass` 葡萄酒杯 | 2244 | 13 | Bottles | — |
| `LivingWoodPiano` 生命木钢琴 | 2245 | 87 | Pianos | — |
| `FleshPiano` 血肉钢琴 | 2246 | 87 | Pianos | — |
| `FrozenPiano` 冰冻钢琴 | 2247 | 87 | Pianos | — |
| `FrozenTable` 冰冻桌 | 2248 | 14 | Tables | — |
| `HoneyChest` 蜂蜜箱 | 2249 | 21 | Containers | — |
| `SteampunkChest` 蒸汽朋克箱 | 2250 | 21 | Containers | — |
| `HoneyWorkBench` 蜂蜜工作台 | 2251 | 18 | WorkBenches | — |
| `FrozenWorkBench` 冰冻工作台 | 2252 | 18 | WorkBenches | — |
| `SteampunkWorkBench` 蒸汽朋克工作台 | 2253 | 18 | WorkBenches | — |
| `GlassPiano` 玻璃钢琴 | 2254 | 87 | Pianos | — |
| `HoneyPiano` 蜂蜜钢琴 | 2255 | 87 | Pianos | — |
| `SteampunkPiano` 蒸汽朋克钢琴 | 2256 | 87 | Pianos | — |
| `HoneyCup` 蜂蜜杯 | 2257 | 13 | Bottles | — |
| `SteampunkCup` 圣餐杯 | 2258 | 13 | Bottles | — |
| `DynastyTable` 王朝桌 | 2259 | 14 | Tables | — |
| `DynastyWood` 王朝木 | 2260 | 311 | DynastyWood | — |
| `RedDynastyShingles` 红王朝瓦 | 2261 | 312 | RedDynastyShingles | — |
| `BlueDynastyShingles` 蓝王朝瓦 | 2262 | 313 | BlueDynastyShingles | — |
| `WhiteDynastyWall` 白王朝墙 | 2263 | — | — | 142 |
| `BlueDynastyWall` 蓝王朝墙 | 2264 | — | — | 143 |
| `DynastyDoor` 王朝门 | 2265 | 10 | ClosedDoor | — |
| `ArcaneRuneWall` 奥术神符墙 | 2271 | — | — | 144 |
| `UltrabrightTorch` 超亮火把 | 2274 | 4 | Torches | — |
| `TigerSkin` 虎皮 | 2281 | 242 | Painting6X4 | — |
| `LeopardSkin` 豹皮 | 2282 | 242 | Painting6X4 | — |
| `ZebraSkin` 斑马皮 | 2283 | 242 | Painting6X4 | — |
| `FrozenChair` 冰冻椅 | 2288 | 15 | Chairs | — |
| `IronFence` 铁栅栏 | 2333 | — | — | 145 |
| `WoodenCrate` 木匣 | 2334 | 376 | FishingCrate | — |
| `IronCrate` 铁匣 | 2335 | 376 | FishingCrate | — |
| `GoldenCrate` 金匣 | 2336 | 376 | FishingCrate | — |
| `MinecartTrack` 矿车轨道 | 2340 | 314 | MinecartTrack | — |
| `ShiverthornSeeds` 寒颤棘种子 | 2357 | 82 | ImmatureHerbs | — |
| `BlueDungeonPiano` 蓝地牢钢琴 | 2376 | 87 | Pianos | — |
| `GreenDungeonPiano` 绿地牢钢琴 | 2377 | 87 | Pianos | — |
| `PinkDungeonPiano` 粉地牢钢琴 | 2378 | 87 | Pianos | — |
| `GoldenPiano` 金钢琴 | 2379 | 87 | Pianos | — |
| `ObsidianPiano` 黑曜石钢琴 | 2380 | 87 | Pianos | — |
| `BonePiano` 骨头钢琴 | 2381 | 87 | Pianos | — |
| `CactusPiano` 仙人掌钢琴 | 2382 | 87 | Pianos | — |
| `SpookyPiano` 阴森钢琴 | 2383 | 87 | Pianos | — |
| `SkywarePiano` 天域钢琴 | 2384 | 87 | Pianos | — |
| `LihzahrdPiano` 丛林蜥蜴钢琴 | 2385 | 87 | Pianos | — |
| `BlueDungeonDresser` 蓝地牢梳妆台 | 2386 | 88 | Dressers | — |
| `GreenDungeonDresser` 绿地牢梳妆台 | 2387 | 88 | Dressers | — |
| `PinkDungeonDresser` 粉地牢梳妆台 | 2388 | 88 | Dressers | — |
| `GoldenDresser` 金梳妆台 | 2389 | 88 | Dressers | — |
| `ObsidianDresser` 黑曜石梳妆台 | 2390 | 88 | Dressers | — |
| `BoneDresser` 骨头梳妆台 | 2391 | 88 | Dressers | — |
| `CactusDresser` 仙人掌梳妆台 | 2392 | 88 | Dressers | — |
| `SpookyDresser` 阴森梳妆台 | 2393 | 88 | Dressers | — |
| `SkywareDresser` 天域梳妆台 | 2394 | 88 | Dressers | — |
| `HoneyDresser` 蜂蜜梳妆台 | 2395 | 88 | Dressers | — |
| `LihzahrdDresser` 丛林蜥蜴梳妆台 | 2396 | 88 | Dressers | — |
| `Sofa` 沙发 | 2397 | 89 | Benches | — |
| `EbonwoodSofa` 乌木沙发 | 2398 | 89 | Benches | — |
| `RichMahoganySofa` 红木沙发 | 2399 | 89 | Benches | — |
| `PearlwoodSofa` 珍珠木沙发 | 2400 | 89 | Benches | — |
| `ShadewoodSofa` 暗影木沙发 | 2401 | 89 | Benches | — |
| `BlueDungeonSofa` 蓝地牢沙发 | 2402 | 89 | Benches | — |
| `GreenDungeonSofa` 绿地牢沙发 | 2403 | 89 | Benches | — |
| `PinkDungeonSofa` 粉地牢沙发 | 2404 | 89 | Benches | — |
| `GoldenSofa` 金沙发 | 2405 | 89 | Benches | — |
| `ObsidianSofa` 黑曜石沙发 | 2406 | 89 | Benches | — |
| `BoneSofa` 骨头沙发 | 2407 | 89 | Benches | — |
| `CactusSofa` 仙人掌沙发 | 2408 | 89 | Benches | — |
| `SpookySofa` 阴森沙发 | 2409 | 89 | Benches | — |
| `SkywareSofa` 天域沙发 | 2410 | 89 | Benches | — |
| `HoneySofa` 蜂蜜沙发 | 2411 | 89 | Benches | — |
| `SteampunkSofa` 蒸汽朋克沙发 | 2412 | 89 | Benches | — |
| `MushroomSofa` 蘑菇沙发 | 2413 | 89 | Benches | — |
| `GlassSofa` 玻璃沙发 | 2414 | 89 | Benches | — |
| `PumpkinSofa` 南瓜沙发 | 2415 | 89 | Benches | — |
| `LihzahrdSofa` 丛林蜥蜴沙发 | 2416 | 89 | Benches | — |
| `CopperPlatingWall` 镀铜墙 | 2432 | — | — | 146 |
| `StoneSlabWall` 石板墙 | 2433 | — | — | 147 |
| `Sail` 船帆 | 2434 | — | — | 148 |
| `CoralstoneBlock` 珊瑚石块 | 2435 | 315 | Coralstone | — |
| `BlueJellyfishJar` 蓝水母罐 | 2439 | 316 | BlueJellyfishBowl | — |
| `GreenJellyfishJar` 绿水母罐 | 2440 | 317 | GreenJellyfishBowl | — |
| `PinkJellyfishJar` 粉水母罐 | 2441 | 318 | PinkJellyfishBowl | — |
| `LifePreserver` 救生圈 | 2442 | 240 | Painting3X3 | — |
| `ShipsWheel` 舵轮 | 2443 | 240 | Painting3X3 | — |
| `CompassRose` 罗盘针 | 2444 | 240 | Painting3X3 | — |
| `WallAnchor` 墙锚 | 2445 | 240 | Painting3X3 | — |
| `GoldfishTrophy` 金鱼纪念章 | 2446 | 240 | Painting3X3 | — |
| `BunnyfishTrophy` 兔兔鱼纪念章 | 2447 | 240 | Painting3X3 | — |
| `SwordfishTrophy` 剑鱼纪念章 | 2448 | 240 | Painting3X3 | — |
| `SharkteethTrophy` 鲨牙纪念章 | 2449 | 240 | Painting3X3 | — |
| `KingSlimeTrophy` 史莱姆王纪念章 | 2489 | 240 | Painting3X3 | — |
| `ShipInABottle` 船舶瓶 | 2490 | 319 | ShipInABottle | — |
| `PressureTrack` 压力板轨道 | 2492 | 314 | MinecartTrack | — |
| `TreasureMap` 宝藏地图 | 2495 | 242 | Painting6X4 | — |
| `SeaweedPlanter` 海草花盆 | 2496 | 320 | SeaweedPlanter | — |
| `PillaginMePixels` 海贼像素画 | 2497 | 242 | Painting6X4 | — |
| `BorealWood` 针叶木 | 2503 | 321 | BorealWood | — |
| `PalmWood` 棕榈木 | 2504 | 322 | PalmWood | — |
| `BorealWoodWall` 针叶木墙 | 2505 | — | — | 149 |
| `PalmWoodWall` 棕榈木墙 | 2506 | — | — | 151 |
| `BorealWoodFence` 针叶木栅栏 | 2507 | — | — | 150 |
| `PalmWoodFence` 棕榈木栅栏 | 2508 | — | — | 152 |
| `PalmWoodPlatform` 棕榈木平台 | 2518 | 19 | Platforms | — |
| `PalmWoodBathtub` 棕榈木浴缸 | 2519 | 90 | Bathtubs | — |
| `PalmWoodBed` 棕榈木床 | 2520 | 79 | Beds | — |
| `PalmWoodBench` 棕榈木长椅 | 2521 | 89 | Benches | — |
| `PalmWoodCandelabra` 棕榈木烛台 | 2522 | 100 | Candelabras | — |
| `PalmWoodCandle` 棕榈木蜡烛 | 2523 | 33 | Candles | — |
| `PalmWoodChair` 棕榈木椅 | 2524 | 15 | Chairs | — |
| `PalmWoodChandelier` 棕榈木吊灯 | 2525 | 34 | Chandeliers | — |
| `PalmWoodChest` 棕榈木箱 | 2526 | 21 | Containers | — |
| `PalmWoodSofa` 棕榈木沙发 | 2527 | 89 | Benches | — |
| `PalmWoodDoor` 棕榈木门 | 2528 | 10 | ClosedDoor | — |
| `PalmWoodDresser` 棕榈木梳妆台 | 2529 | 88 | Dressers | — |
| `PalmWoodLantern` 棕榈木灯笼 | 2530 | 42 | HangingLanterns | — |
| `PalmWoodPiano` 棕榈木钢琴 | 2531 | 87 | Pianos | — |
| `PalmWoodTable` 棕榈木桌 | 2532 | 14 | Tables | — |
| `PalmWoodLamp` 棕榈木灯 | 2533 | 93 | Lamps | — |
| `PalmWoodWorkBench` 棕榈木工作台 | 2534 | 18 | WorkBenches | — |
| `PalmWoodBookcase` 棕榈木书架 | 2536 | 101 | Bookcases | — |
| `MushroomBathtub` 蘑菇浴缸 | 2537 | 90 | Bathtubs | — |
| `MushroomBed` 蘑菇床 | 2538 | 79 | Beds | — |
| `MushroomBench` 蘑菇长椅 | 2539 | 89 | Benches | — |
| `MushroomBookcase` 蘑菇书架 | 2540 | 101 | Bookcases | — |
| `MushroomCandelabra` 蘑菇烛台 | 2541 | 100 | Candelabras | — |
| `MushroomCandle` 蘑菇蜡烛 | 2542 | 33 | Candles | — |
| `MushroomChandelier` 蘑菇吊灯 | 2543 | 34 | Chandeliers | — |
| `MushroomChest` 蘑菇箱 | 2544 | 21 | Containers | — |
| `MushroomDresser` 蘑菇梳妆台 | 2545 | 88 | Dressers | — |
| `MushroomLantern` 蘑菇灯笼 | 2546 | 42 | HangingLanterns | — |
| `MushroomLamp` 蘑菇灯 | 2547 | 93 | Lamps | — |
| `MushroomPiano` 蘑菇钢琴 | 2548 | 87 | Pianos | — |
| `MushroomPlatform` 蘑菇平台 | 2549 | 19 | Platforms | — |
| `MushroomTable` 蘑菇桌 | 2550 | 14 | Tables | — |
| `BorealWoodBathtub` 针叶木浴缸 | 2552 | 90 | Bathtubs | — |
| `BorealWoodBed` 针叶木床 | 2553 | 79 | Beds | — |
| `BorealWoodBookcase` 针叶木书架 | 2554 | 101 | Bookcases | — |
| `BorealWoodCandelabra` 针叶木烛台 | 2555 | 100 | Candelabras | — |
| `BorealWoodCandle` 针叶木蜡烛 | 2556 | 33 | Candles | — |
| `BorealWoodChair` 针叶木椅 | 2557 | 15 | Chairs | — |
| `BorealWoodChandelier` 针叶木吊灯 | 2558 | 34 | Chandeliers | — |
| `BorealWoodChest` 针叶木箱 | 2559 | 21 | Containers | — |
| `BorealWoodClock` 针叶木时钟 | 2560 | 104 | GrandfatherClocks | — |
| `BorealWoodDoor` 针叶木门 | 2561 | 10 | ClosedDoor | — |
| `BorealWoodDresser` 针叶木梳妆台 | 2562 | 88 | Dressers | — |
| `BorealWoodLamp` 针叶木灯 | 2563 | 93 | Lamps | — |
| `BorealWoodLantern` 针叶木灯笼 | 2564 | 42 | HangingLanterns | — |
| `BorealWoodPiano` 针叶木钢琴 | 2565 | 87 | Pianos | — |
| `BorealWoodPlatform` 针叶木平台 | 2566 | 19 | Platforms | — |
| `SlimeBathtub` 史莱姆浴缸 | 2567 | 90 | Bathtubs | — |
| `SlimeBed` 史莱姆床 | 2568 | 79 | Beds | — |
| `SlimeBookcase` 史莱姆书架 | 2569 | 101 | Bookcases | — |
| `SlimeCandelabra` 史莱姆烛台 | 2570 | 100 | Candelabras | — |
| `SlimeCandle` 史莱姆蜡烛 | 2571 | 33 | Candles | — |
| `SlimeChair` 史莱姆椅 | 2572 | 15 | Chairs | — |
| `SlimeChandelier` 史莱姆吊灯 | 2573 | 34 | Chandeliers | — |
| `SlimeChest` 史莱姆箱 | 2574 | 21 | Containers | — |
| `SlimeClock` 史莱姆时钟 | 2575 | 104 | GrandfatherClocks | — |
| `SlimeDoor` 史莱姆门 | 2576 | 10 | ClosedDoor | — |
| `SlimeDresser` 史莱姆梳妆台 | 2577 | 88 | Dressers | — |
| `SlimeLamp` 史莱姆灯 | 2578 | 93 | Lamps | — |
| `SlimeLantern` 史莱姆灯笼 | 2579 | 42 | HangingLanterns | — |
| `SlimePiano` 史莱姆钢琴 | 2580 | 87 | Pianos | — |
| `SlimePlatform` 史莱姆平台 | 2581 | 19 | Platforms | — |
| `SlimeSofa` 史莱姆沙发 | 2582 | 89 | Benches | — |
| `SlimeTable` 史莱姆桌 | 2583 | 14 | Tables | — |
| `DukeFishronTrophy` 猪龙鱼公爵纪念章 | 2589 | 240 | Painting3X3 | — |
| `BoneClock` 骨头时钟 | 2591 | 104 | GrandfatherClocks | — |
| `CactusClock` 仙人掌时钟 | 2592 | 104 | GrandfatherClocks | — |
| `EbonwoodClock` 乌木时钟 | 2593 | 104 | GrandfatherClocks | — |
| `FrozenClock` 冰冻时钟 | 2594 | 104 | GrandfatherClocks | — |
| `LihzahrdClock` 丛林蜥蜴时钟 | 2595 | 104 | GrandfatherClocks | — |
| `LivingWoodClock` 生命木时钟 | 2596 | 104 | GrandfatherClocks | — |
| `RichMahoganyClock` 红木时钟 | 2597 | 104 | GrandfatherClocks | — |
| `FleshClock` 血肉时钟 | 2598 | 104 | GrandfatherClocks | — |
| `MushroomClock` 蘑菇时钟 | 2599 | 104 | GrandfatherClocks | — |
| `ObsidianClock` 黑曜石时钟 | 2600 | 104 | GrandfatherClocks | — |
| `PalmWoodClock` 棕榈木时钟 | 2601 | 104 | GrandfatherClocks | — |
| `PearlwoodClock` 珍珠木时钟 | 2602 | 104 | GrandfatherClocks | — |
| `PumpkinClock` 南瓜时钟 | 2603 | 104 | GrandfatherClocks | — |
| `ShadewoodClock` 暗影木时钟 | 2604 | 104 | GrandfatherClocks | — |
| `SpookyClock` 阴森时钟 | 2605 | 104 | GrandfatherClocks | — |
| `SkywareClock` 天域时钟 | 2606 | 104 | GrandfatherClocks | — |
| `GreenDungeonChest` 绿地牢箱 | 2612 | 21 | Containers | — |
| `PinkDungeonChest` 粉地牢箱 | 2613 | 21 | Containers | — |
| `BlueDungeonChest` 蓝地牢箱 | 2614 | 21 | Containers | — |
| `BoneChest` 骨箱 | 2615 | 21 | Containers | — |
| `CactusChest` 仙人掌箱 | 2616 | 21 | Containers | — |
| `FleshChest` 血肉箱 | 2617 | 21 | Containers | — |
| `ObsidianChest` 黑曜石箱 | 2618 | 21 | Containers | — |
| `PumpkinChest` 南瓜箱 | 2619 | 21 | Containers | — |
| `SpookyChest` 阴森箱 | 2620 | 21 | Containers | — |
| `Seashell` 贝壳 | 2625 | 324 | BeachPiles | — |
| `Starfish` 海星 | 2626 | 324 | BeachPiles | — |
| `SteampunkPlatform` 蒸汽朋克平台 | 2627 | 19 | Platforms | — |
| `SkywarePlatform` 天域平台 | 2628 | 19 | Platforms | — |
| `LivingWoodPlatform` 生命木平台 | 2629 | 19 | Platforms | — |
| `HoneyPlatform` 蜂蜜平台 | 2630 | 19 | Platforms | — |
| `SkywareWorkbench` 天域工作台 | 2631 | 18 | WorkBenches | — |
| `GlassWorkBench` 玻璃工作台 | 2632 | 18 | WorkBenches | — |
| `LivingWoodWorkBench` 生命木工作台 | 2633 | 18 | WorkBenches | — |
| `FleshSofa` 血肉沙发 | 2634 | 89 | Benches | — |
| `FrozenSofa` 冰冻沙发 | 2635 | 89 | Benches | — |
| `LivingWoodSofa` 生命木沙发 | 2636 | 89 | Benches | — |
| `PumpkinDresser` 南瓜梳妆台 | 2637 | 88 | Dressers | — |
| `SteampunkDresser` 蒸汽朋克梳妆台 | 2638 | 88 | Dressers | — |
| `GlassDresser` 玻璃梳妆台 | 2639 | 88 | Dressers | — |
| `FleshDresser` 血肉梳妆台 | 2640 | 88 | Dressers | — |
| `PumpkinLantern` 南瓜灯笼 | 2641 | 42 | HangingLanterns | — |
| `ObsidianLantern` 黑曜石灯笼 | 2642 | 42 | HangingLanterns | — |
| `PumpkinLamp` 南瓜灯 | 2643 | 93 | Lamps | — |
| `ObsidianLamp` 黑曜石灯 | 2644 | 93 | Lamps | — |
| `BlueDungeonLamp` 蓝地牢灯 | 2645 | 93 | Lamps | — |
| `GreenDungeonLamp` 绿地牢灯 | 2646 | 93 | Lamps | — |
| `PinkDungeonLamp` 粉地牢灯 | 2647 | 93 | Lamps | — |
| `HoneyCandle` 蜂蜜蜡烛 | 2648 | 33 | Candles | — |
| `SteampunkCandle` 蒸汽朋克蜡烛 | 2649 | 33 | Candles | — |
| `SpookyCandle` 阴森蜡烛 | 2650 | 33 | Candles | — |
| `ObsidianCandle` 黑曜石蜡烛 | 2651 | 33 | Candles | — |
| `BlueDungeonChandelier` 蓝地牢吊灯 | 2652 | 34 | Chandeliers | — |
| `GreenDungeonChandelier` 绿地牢吊灯 | 2653 | 34 | Chandeliers | — |
| `PinkDungeonChandelier` 粉地牢吊灯 | 2654 | 34 | Chandeliers | — |
| `SteampunkChandelier` 蒸汽朋克吊灯 | 2655 | 34 | Chandeliers | — |
| `PumpkinChandelier` 南瓜吊灯 | 2656 | 34 | Chandeliers | — |
| `ObsidianChandelier` 黑曜石吊灯 | 2657 | 34 | Chandeliers | — |
| `BlueDungeonBathtub` 蓝地牢浴缸 | 2658 | 90 | Bathtubs | — |
| `GreenDungeonBathtub` 绿地牢浴缸 | 2659 | 90 | Bathtubs | — |
| `PinkDungeonBathtub` 粉地牢浴缸 | 2660 | 90 | Bathtubs | — |
| `PumpkinBathtub` 南瓜浴缸 | 2661 | 90 | Bathtubs | — |
| `ObsidianBathtub` 黑曜石浴缸 | 2662 | 90 | Bathtubs | — |
| `GoldenBathtub` 金浴缸 | 2663 | 90 | Bathtubs | — |
| `BlueDungeonCandelabra` 蓝地牢烛台 | 2664 | 100 | Candelabras | — |
| `GreenDungeonCandelabra` 绿地牢烛台 | 2665 | 100 | Candelabras | — |
| `PinkDungeonCandelabra` 粉地牢烛台 | 2666 | 100 | Candelabras | — |
| `ObsidianCandelabra` 黑曜石烛台 | 2667 | 100 | Candelabras | — |
| `PumpkinCandelabra` 南瓜烛台 | 2668 | 100 | Candelabras | — |
| `PumpkinBed` 南瓜床 | 2669 | 79 | Beds | — |
| `PumpkinBookcase` 南瓜书架 | 2670 | 101 | Bookcases | — |
| `PumpkinPiano` 南瓜钢琴 | 2671 | 87 | Pianos | — |
| `SharkStatue` 鲨鱼雕像 | 2672 | 105 | Statues | — |
| `AmberGemsparkWall` 琥珀晶莹宝石墙 | 2677 | — | — | 153 |
| `AmberGemsparkWallOff` 黯淡琥珀晶莹宝石墙 | 2678 | — | — | 157 |
| `AmethystGemsparkWall` 紫晶晶莹宝石墙 | 2679 | — | — | 154 |
| `AmethystGemsparkWallOff` 黯淡紫晶晶莹宝石墙 | 2680 | — | — | 158 |
| `DiamondGemsparkWall` 钻石晶莹宝石墙 | 2681 | — | — | 155 |
| `DiamondGemsparkWallOff` 黯淡钻石晶莹宝石墙 | 2682 | — | — | 159 |
| `EmeraldGemsparkWall` 翡翠晶莹宝石墙 | 2683 | — | — | 156 |
| `EmeraldGemsparkWallOff` 黯淡翡翠晶莹宝石墙 | 2684 | — | — | 160 |
| `RubyGemsparkWall` 红玉晶莹宝石墙 | 2685 | — | — | 164 |
| `RubyGemsparkWallOff` 黯淡红玉晶莹宝石墙 | 2686 | — | — | 161 |
| `SapphireGemsparkWall` 蓝玉晶莹宝石墙 | 2687 | — | — | 165 |
| `SapphireGemsparkWallOff` 黯淡蓝玉晶莹宝石墙 | 2688 | — | — | 162 |
| `TopazGemsparkWall` 黄玉晶莹宝石墙 | 2689 | — | — | 166 |
| `TopazGemsparkWallOff` 黯淡黄玉晶莹宝石墙 | 2690 | — | — | 163 |
| `TinPlatingWall` 镀锡墙 | 2691 | — | — | 167 |
| `TinPlating` 锡镀层 | 2692 | 325 | TinPlating | — |
| `WaterfallBlock` 瀑布块 | 2693 | 326 | Waterfall | — |
| `LavafallBlock` 熔岩瀑布块 | 2694 | 327 | Lavafall | — |
| `ConfettiBlock` 彩纸块 | 2695 | 328 | Confetti | — |
| `ConfettiWall` 彩纸墙 | 2696 | — | — | 168 |
| `ConfettiBlockBlack` 午夜彩纸块 | 2697 | 329 | ConfettiBlack | — |
| `ConfettiWallBlack` 午夜彩纸墙 | 2698 | — | — | 169 |
| `WeaponRack` 武器架 | 2699 | 471 | WeaponsRack2 | — |
| `FireworksBox` 烟花盒 | 2700 | 335 | FireworksBox | — |
| `LivingFireBlock` 活火块 | 2701 | 336 | LivingFire | — |
| `AlphabetStatue0` 0字雕像 | 2702 | 337 | AlphabetStatues | — |
| `AlphabetStatue1` 1字雕像 | 2703 | 337 | AlphabetStatues | — |
| `AlphabetStatue2` 2字雕像 | 2704 | 337 | AlphabetStatues | — |
| `AlphabetStatue3` 3字雕像 | 2705 | 337 | AlphabetStatues | — |
| `AlphabetStatue4` 4字雕像 | 2706 | 337 | AlphabetStatues | — |
| `AlphabetStatue5` 5字雕像 | 2707 | 337 | AlphabetStatues | — |
| `AlphabetStatue6` 6字雕像 | 2708 | 337 | AlphabetStatues | — |
| `AlphabetStatue7` 7字雕像 | 2709 | 337 | AlphabetStatues | — |
| `AlphabetStatue8` 8字雕像 | 2710 | 337 | AlphabetStatues | — |
| `AlphabetStatue9` 9字雕像 | 2711 | 337 | AlphabetStatues | — |
| `AlphabetStatueA` A字雕像 | 2712 | 337 | AlphabetStatues | — |
| `AlphabetStatueB` B字雕像 | 2713 | 337 | AlphabetStatues | — |
| `AlphabetStatueC` C字雕像 | 2714 | 337 | AlphabetStatues | — |
| `AlphabetStatueD` D字雕像 | 2715 | 337 | AlphabetStatues | — |
| `AlphabetStatueE` E字雕像 | 2716 | 337 | AlphabetStatues | — |
| `AlphabetStatueF` F字雕像 | 2717 | 337 | AlphabetStatues | — |
| `AlphabetStatueG` G字雕像 | 2718 | 337 | AlphabetStatues | — |
| `AlphabetStatueH` H字雕像 | 2719 | 337 | AlphabetStatues | — |
| `AlphabetStatueI` I字雕像 | 2720 | 337 | AlphabetStatues | — |
| `AlphabetStatueJ` J字雕像 | 2721 | 337 | AlphabetStatues | — |
| `AlphabetStatueK` K字雕像 | 2722 | 337 | AlphabetStatues | — |
| `AlphabetStatueL` L字雕像 | 2723 | 337 | AlphabetStatues | — |
| `AlphabetStatueM` M字雕像 | 2724 | 337 | AlphabetStatues | — |
| `AlphabetStatueN` N字雕像 | 2725 | 337 | AlphabetStatues | — |
| `AlphabetStatueO` O字雕像 | 2726 | 337 | AlphabetStatues | — |
| `AlphabetStatueP` P字雕像 | 2727 | 337 | AlphabetStatues | — |
| `AlphabetStatueQ` Q字雕像 | 2728 | 337 | AlphabetStatues | — |
| `AlphabetStatueR` R字雕像 | 2729 | 337 | AlphabetStatues | — |
| `AlphabetStatueS` S字雕像 | 2730 | 337 | AlphabetStatues | — |
| `AlphabetStatueT` T字雕像 | 2731 | 337 | AlphabetStatues | — |
| `AlphabetStatueU` U字雕像 | 2732 | 337 | AlphabetStatues | — |
| `AlphabetStatueV` V字雕像 | 2733 | 337 | AlphabetStatues | — |
| `AlphabetStatueW` W字雕像 | 2734 | 337 | AlphabetStatues | — |
| `AlphabetStatueX` X字雕像 | 2735 | 337 | AlphabetStatues | — |
| `AlphabetStatueY` Y字雕像 | 2736 | 337 | AlphabetStatues | — |
| `AlphabetStatueZ` Z字雕像 | 2737 | 337 | AlphabetStatues | — |
| `FireworkFountain` 烟花喷泉 | 2738 | 338 | FireworkFountain | — |
| `BoosterTrack` 增速轨道 | 2739 | 314 | MinecartTrack | — |
| `GrasshopperCage` 蚱蜢笼 | 2741 | 339 | GrasshopperCage | — |
| `MusicBoxUndergroundCrimson` 八音盒（地下猩红之地） | 2742 | 139 | MusicBoxes | — |
| `CactusTable` 仙人掌桌 | 2743 | 14 | Tables | — |
| `CactusPlatform` 仙人掌平台 | 2744 | 19 | Platforms | — |
| `GlassChest` 玻璃箱 | 2748 | 21 | Containers | — |
| `LivingCursedFireBlock` 诅咒活火块 | 2751 | 340 | LivingCursedFire | — |
| `LivingDemonFireBlock` 恶魔活火块 | 2752 | 341 | LivingDemonFire | — |
| `LivingFrostFireBlock` 寒霜活火块 | 2753 | 342 | LivingFrostFire | — |
| `LivingIchorBlock` 灵液活火块 | 2754 | 343 | LivingIchor | — |
| `LivingUltrabrightFireBlock` 超亮活火块 | 2755 | 344 | LivingUltrabrightFire | — |
| `HoneyfallBlock` 蜂蜜瀑布块 | 2787 | 345 | Honeyfall | — |
| `HoneyfallWall` 蜂蜜瀑布墙 | 2788 | — | — | 172 |
| `ChlorophyteBrickWall` 叶绿砖墙 | 2789 | — | — | 173 |
| `CrimtaneBrickWall` 猩红矿砖墙 | 2790 | — | — | 174 |
| `ShroomitePlatingWall` 蘑菇矿镀层墙 | 2791 | — | — | 175 |
| `ChlorophyteBrick` 叶绿砖 | 2792 | 346 | ChlorophyteBrick | — |
| `CrimtaneBrick` 猩红矿砖 | 2793 | 347 | CrimtaneBrick | — |
| `ShroomitePlating` 蘑菇矿镀层 | 2794 | 348 | ShroomitePlating | — |
| `MartianAstroClock` 火星占星钟 | 2809 | 104 | GrandfatherClocks | — |
| `MartianBathtub` 火星浴缸 | 2810 | 90 | Bathtubs | — |
| `MartianBed` 火星床 | 2811 | 79 | Beds | — |
| `MartianHoverChair` 火星摇摆椅 | 2812 | 15 | Chairs | — |
| `MartianChandelier` 火星吊灯 | 2813 | 34 | Chandeliers | — |
| `MartianChest` 火星箱 | 2814 | 21 | Containers | — |
| `MartianDoor` 火星门 | 2815 | 10 | ClosedDoor | — |
| `MartianDresser` 火星梳妆台 | 2816 | 88 | Dressers | — |
| `MartianHolobookcase` 火星整体书架 | 2817 | 101 | Bookcases | — |
| `MartianHoverCandle` 火星摇摆蜡烛 | 2818 | 33 | Candles | — |
| `MartianLamppost` 火星灯柱 | 2819 | 93 | Lamps | — |
| `MartianLantern` 火星灯笼 | 2820 | 42 | HangingLanterns | — |
| `MartianPiano` 火星钢琴 | 2821 | 87 | Pianos | — |
| `MartianPlatform` 火星平台 | 2822 | 19 | Platforms | — |
| `MartianSofa` 火星沙发 | 2823 | 89 | Benches | — |
| `MartianTable` 火星桌 | 2824 | 14 | Tables | — |
| `MartianTableLamp` 火星桌灯 | 2825 | 100 | Candelabras | — |
| `MartianWorkBench` 火星工作台 | 2826 | 18 | WorkBenches | — |
| `WoodenSink` 木水槽 | 2827 | 172 | Sinks | — |
| `EbonwoodSink` 乌木水槽 | 2828 | 172 | Sinks | — |
| `RichMahoganySink` 红木水槽 | 2829 | 172 | Sinks | — |
| `PearlwoodSink` 珍珠木水槽 | 2830 | 172 | Sinks | — |
| `BoneSink` 骨头水槽 | 2831 | 172 | Sinks | — |
| `FleshSink` 血肉水槽 | 2832 | 172 | Sinks | — |
| `LivingWoodSink` 生命木水槽 | 2833 | 172 | Sinks | — |
| `SkywareSink` 天域水槽 | 2834 | 172 | Sinks | — |
| `ShadewoodSink` 暗影木水槽 | 2835 | 172 | Sinks | — |
| `LihzahrdSink` 丛林蜥蜴水槽 | 2836 | 172 | Sinks | — |
| `BlueDungeonSink` 蓝地牢水槽 | 2837 | 172 | Sinks | — |
| `GreenDungeonSink` 绿地牢水槽 | 2838 | 172 | Sinks | — |
| `PinkDungeonSink` 粉地牢水槽 | 2839 | 172 | Sinks | — |
| `ObsidianSink` 黑曜石水槽 | 2840 | 172 | Sinks | — |
| `MetalSink` 金属水槽 | 2841 | 172 | Sinks | — |
| `GlassSink` 玻璃水槽 | 2842 | 172 | Sinks | — |
| `GoldenSink` 金水槽 | 2843 | 172 | Sinks | — |
| `HoneySink` 蜂蜜水槽 | 2844 | 172 | Sinks | — |
| `SteampunkSink` 蒸汽朋克水槽 | 2845 | 172 | Sinks | — |
| `PumpkinSink` 南瓜水槽 | 2846 | 172 | Sinks | — |
| `SpookySink` 阴森水槽 | 2847 | 172 | Sinks | — |
| `FrozenSink` 冰冻水槽 | 2848 | 172 | Sinks | — |
| `DynastySink` 王朝水槽 | 2849 | 172 | Sinks | — |
| `PalmWoodSink` 棕榈木水槽 | 2850 | 172 | Sinks | — |
| `MushroomSink` 蘑菇水槽 | 2851 | 172 | Sinks | — |
| `BorealWoodSink` 针叶木水槽 | 2852 | 172 | Sinks | — |
| `SlimeSink` 史莱姆水槽 | 2853 | 172 | Sinks | — |
| `CactusSink` 仙人掌水槽 | 2854 | 172 | Sinks | — |
| `MartianSink` 火星水槽 | 2855 | 172 | Sinks | — |
| `MartianConduitPlating` 火星管道镀层 | 2860 | 350 | MartianConduitPlating | — |
| `MartianConduitWall` 火星管道墙 | 2861 | — | — | 176 |
| `PaintingCastleMarsberg` 火星贝格城堡 | 2865 | 242 | Painting6X4 | — |
| `PaintingMartiaLisa` 火娜丽莎 | 2866 | 242 | Painting6X4 | — |
| `PaintingTheTruthIsUpThere` 真理就在火星 | 2867 | 242 | Painting6X4 | — |
| `SmokeBlock` 烟雾块 | 2868 | 351 | ChimneySmoke | — |
| `AngryTrapperBanner` 愤怒捕手旗 | 2897 | 91 | Banners | — |
| `ArmoredVikingBanner` 装甲维京海盗旗 | 2898 | 91 | Banners | — |
| `BlackSlimeBanner` 黑史莱姆旗 | 2899 | 91 | Banners | — |
| `BlueArmoredBonesBanner` 蓝装甲骷髅旗 | 2900 | 91 | Banners | — |
| `BlueCultistArcherBanner` 蓝邪教徒弓箭手旗 | 2901 | 91 | Banners | — |
| `BlueCultistCasterBanner` 拜月教忠教徒旗 | 2902 | 91 | Banners | — |
| `BlueCultistFighterBanner` 蓝邪教徒战士旗 | 2903 | 91 | Banners | — |
| `BoneLeeBanner` 骷髅李小龙旗 | 2904 | 91 | Banners | — |
| `ClingerBanner` 爬藤怪旗 | 2905 | 91 | Banners | — |
| `CochinealBeetleBanner` 胭脂虫旗 | 2906 | 91 | Banners | — |
| `CorruptPenguinBanner` 腐化企鹅旗 | 2907 | 91 | Banners | — |
| `CorruptSlimeBanner` 腐化史莱姆旗 | 2908 | 91 | Banners | — |
| `CorruptorBanner` 腐化者旗 | 2909 | 91 | Banners | — |
| `CrimslimeBanner` 猩红史莱姆旗 | 2910 | 91 | Banners | — |
| `CursedSkullBanner` 诅咒骷髅头旗 | 2911 | 91 | Banners | — |
| `CyanBeetleBanner` 青壳虫旗 | 2912 | 91 | Banners | — |
| `DevourerBanner` 吞噬怪旗 | 2913 | 91 | Banners | — |
| `DiablolistBanner` 魔教徒旗 | 2914 | 91 | Banners | — |
| `DoctorBonesBanner` 骷髅博士旗 | 2915 | 91 | Banners | — |
| `DungeonSlimeBanner` 地牢史莱姆旗 | 2916 | 91 | Banners | — |
| `DungeonSpiritBanner` 地牢幽魂旗 | 2917 | 91 | Banners | — |
| `ElfArcherBanner` 精灵弓箭手旗 | 2918 | 91 | Banners | — |
| `ElfCopterBanner` 精灵直升机旗 | 2919 | 91 | Banners | — |
| `EyezorBanner` 眼怪旗 | 2920 | 91 | Banners | — |
| `FlockoBanner` 雪花怪旗 | 2921 | 91 | Banners | — |
| `GhostBanner` 鬼魂旗 | 2922 | 91 | Banners | — |
| `GiantBatBanner` 巨型蝙蝠旗 | 2923 | 91 | Banners | — |
| `GiantCursedSkullBanner` 巨型诅咒骷髅头旗 | 2924 | 91 | Banners | — |
| `GiantFlyingFoxBanner` 巨型飞狐旗 | 2925 | 91 | Banners | — |
| `GingerbreadManBanner` 姜饼人旗 | 2926 | 91 | Banners | — |
| `GoblinArcherBanner` 哥布林弓箭手旗 | 2927 | 91 | Banners | — |
| `GreenSlimeBanner` 绿史莱姆旗 | 2928 | 91 | Banners | — |
| `HeadlessHorsemanBanner` 无头骑士旗 | 2929 | 91 | Banners | — |
| `HellArmoredBonesBanner` 地狱装甲骷髅旗 | 2930 | 91 | Banners | — |
| `HellhoundBanner` 地狱犬旗 | 2931 | 91 | Banners | — |
| `HoppinJackBanner` 弹跳杰克南瓜灯旗 | 2932 | 91 | Banners | — |
| `IceBatBanner` 冰雪蝙蝠旗 | 2933 | 91 | Banners | — |
| `IceGolemBanner` 冰雪巨人旗 | 2934 | 91 | Banners | — |
| `IceSlimeBanner` 冰雪史莱姆旗 | 2935 | 91 | Banners | — |
| `IchorStickerBanner` 灵液黏黏怪旗 | 2936 | 91 | Banners | — |
| `IlluminantBatBanner` 夜明蝙蝠旗 | 2937 | 91 | Banners | — |
| `IlluminantSlimeBanner` 夜明史莱姆旗 | 2938 | 91 | Banners | — |
| `JungleBatBanner` 丛林蝙蝠旗 | 2939 | 91 | Banners | — |
| `JungleSlimeBanner` 丛林史莱姆旗 | 2940 | 91 | Banners | — |
| `KrampusBanner` 坎卜斯旗 | 2941 | 91 | Banners | — |
| `LacBeetleBanner` 紫胶虫旗 | 2942 | 91 | Banners | — |
| `LavaBatBanner` 熔岩蝙蝠旗 | 2943 | 91 | Banners | — |
| `LavaSlimeBanner` 熔岩史莱姆旗 | 2944 | 91 | Banners | — |
| `MartianBrainscramblerBanner` 火星扰脑怪旗 | 2945 | 91 | Banners | — |
| `MartianDroneBanner` 火星飞船旗 | 2946 | 91 | Banners | — |
| `MartianEngineerBanner` 火星工程师旗 | 2947 | 91 | Banners | — |
| `MartianGigazapperBanner` 火星电击怪旗 | 2948 | 91 | Banners | — |
| `MartianGreyGruntBanner` 火星灰咕噜兽旗 | 2949 | 91 | Banners | — |
| `MartianOfficerBanner` 火星军官旗 | 2950 | 91 | Banners | — |
| `MartianRaygunnerBanner` 火星激光枪手旗 | 2951 | 91 | Banners | — |
| `MartianScutlixGunnerBanner` 火星鳞甲怪枪手旗 | 2952 | 91 | Banners | — |
| `MartianTeslaTurretBanner` 火星特斯拉炮塔旗 | 2953 | 91 | Banners | — |
| `MisterStabbyBanner` 戳刺先生旗 | 2954 | 91 | Banners | — |
| `MotherSlimeBanner` 史莱姆之母旗 | 2955 | 91 | Banners | — |
| `NecromancerBanner` 死灵法师旗 | 2956 | 91 | Banners | — |
| `NutcrackerBanner` 胡桃夹士旗 | 2957 | 91 | Banners | — |
| `PaladinBanner` 圣骑士旗 | 2958 | 91 | Banners | — |
| `PenguinBanner` 企鹅旗 | 2959 | 91 | Banners | — |
| `PinkyBanner` 粉史莱姆旗 | 2960 | 91 | Banners | — |
| `PoltergeistBanner` 胡闹鬼旗 | 2961 | 91 | Banners | — |
| `PossessedArmorBanner` 装甲幻影魔旗 | 2962 | 91 | Banners | — |
| `PresentMimicBanner` 礼物宝箱怪旗 | 2963 | 91 | Banners | — |
| `PurpleSlimeBanner` 紫史莱姆旗 | 2964 | 91 | Banners | — |
| `RaggedCasterBanner` 褴褛邪教徒法师旗 | 2965 | 91 | Banners | — |
| `RainbowSlimeBanner` 彩虹史莱姆旗 | 2966 | 91 | Banners | — |
| `RavenBanner` 乌鸦旗 | 2967 | 91 | Banners | — |
| `RedSlimeBanner` 红史莱姆旗 | 2968 | 91 | Banners | — |
| `RuneWizardBanner` 符文巫师旗 | 2969 | 91 | Banners | — |
| `RustyArmoredBonesBanner` 生锈装甲骷髅旗 | 2970 | 91 | Banners | — |
| `ScarecrowBanner` 稻草人旗 | 2971 | 91 | Banners | — |
| `ScutlixBanner` 鳞甲怪旗 | 2972 | 91 | Banners | — |
| `SkeletonArcherBanner` 骷髅弓箭手旗 | 2973 | 91 | Banners | — |
| `SkeletonCommandoBanner` 骷髅突击手旗 | 2974 | 91 | Banners | — |
| `SkeletonSniperBanner` 骷髅狙击手旗 | 2975 | 91 | Banners | — |
| `SlimerBanner` 恶翅史莱姆旗 | 2976 | 91 | Banners | — |
| `SnatcherBanner` 抓人草旗 | 2977 | 91 | Banners | — |
| `SnowBallaBanner` 巴拉雪人旗 | 2978 | 91 | Banners | — |
| `SnowmanGangstaBanner` 雪人暴徒旗 | 2979 | 91 | Banners | — |
| `SpikedIceSlimeBanner` 尖刺冰雪史莱姆旗 | 2980 | 91 | Banners | — |
| `SpikedJungleSlimeBanner` 尖刺丛林史莱姆旗 | 2981 | 91 | Banners | — |
| `SplinterlingBanner` 树精旗 | 2982 | 91 | Banners | — |
| `SquidBanner` 乌贼旗 | 2983 | 91 | Banners | — |
| `TacticalSkeletonBanner` 骷髅特警旗 | 2984 | 91 | Banners | — |
| `TheGroomBanner` 僵尸新郎旗 | 2985 | 91 | Banners | — |
| `TimBanner` 蒂姆旗 | 2986 | 91 | Banners | — |
| `UndeadMinerBanner` 不死矿工旗 | 2987 | 91 | Banners | — |
| `UndeadVikingBanner` 亡灵维京海盗旗 | 2988 | 91 | Banners | — |
| `WhiteCultistArcherBanner` 白邪教徒弓箭手旗 | 2989 | 91 | Banners | — |
| `WhiteCultistCasterBanner` 白邪教徒法师旗 | 2990 | 91 | Banners | — |
| `WhiteCultistFighterBanner` 白邪教徒战士旗 | 2991 | 91 | Banners | — |
| `YellowSlimeBanner` 黄史莱姆旗 | 2992 | 91 | Banners | — |
| `YetiBanner` 雪兽旗 | 2993 | 91 | Banners | — |
| `ZombieElfBanner` 僵尸精灵旗 | 2994 | 91 | Banners | — |
| `SparkyPainting` 斯派基 | 2995 | 242 | Painting6X4 | — |
| `VineRope` 藤蔓绳 | 2996 | 353 | VineRope | — |
| `BewitchingTable` 施法桌 | 2999 | 354 | BewitchingTable | — |
| `AlchemyTable` 炼药桌 | 3000 | 355 | AlchemyTable | — |
| `BoneTorch` 骨头火把 | 3004 | 4 | Torches | — |
| `MusicBoxLunarBoss` 八音盒（月亮Boss） | 3044 | 139 | MusicBoxes | — |
| `RainbowTorch` 彩虹火把 | 3045 | 4 | Torches | — |
| `CursedCampfire` 诅咒篝火 | 3046 | 215 | Campfire | — |
| `DemonCampfire` 恶魔篝火 | 3047 | 215 | Campfire | — |
| `FrozenCampfire` 冰冻篝火 | 3048 | 215 | Campfire | — |
| `IchorCampfire` 灵液篝火 | 3049 | 215 | Campfire | — |
| `RainbowCampfire` 彩虹篝火 | 3050 | 215 | Campfire | — |
| `PaintingAcorns` 橡果 | 3055 | 242 | Painting6X4 | — |
| `PaintingColdSnap` 寒冰英姿 | 3056 | 242 | Painting6X4 | — |
| `PaintingCursedSaint` 诅咒的圣诞骷髅王 | 3057 | 242 | Painting6X4 | — |
| `PaintingSnowfellas` 吉祥三雪宝 | 3058 | 242 | Painting6X4 | — |
| `PaintingTheSeason` 圣诞雪季 | 3059 | 242 | Painting6X4 | — |
| `Sundial` 附魔日晷 | 3064 | 356 | Sundial | — |
| `MarbleBlock` 光面大理石块 | 3066 | 357 | MarbleBlock | — |
| `HellstoneBrickWall` 狱石砖墙 | 3067 | — | — | 177 |
| `GoldBirdCage` 金鸟笼 | 3070 | 358 | GoldBirdCage | — |
| `GoldBunnyCage` 金兔笼 | 3071 | 359 | GoldBunnyCage | — |
| `GoldButterflyCage` 金蝴蝶罐 | 3072 | 360 | GoldButterflyCage | — |
| `GoldFrogCage` 金蛙笼 | 3073 | 361 | GoldFrogCage | — |
| `GoldGrasshopperCage` 金蚱蜢笼 | 3074 | 362 | GoldGrasshopperCage | — |
| `GoldMouseCage` 金老鼠笼 | 3075 | 363 | GoldMouseCage | — |
| `GoldWormCage` 金蠕虫笼 | 3076 | 364 | GoldWormCage | — |
| `SilkRope` 丝绸绳 | 3077 | 365 | SilkRope | — |
| `WebRope` 蛛丝绳 | 3078 | 366 | WebRope | — |
| `Marble` 大理石块 | 3081 | 367 | Marble | — |
| `MarbleWall` 大理石墙 | 3082 | — | — | 183 |
| `MarbleBlockWall` 光面大理石墙 | 3083 | — | — | 179 |
| `Granite` 花岗岩块 | 3086 | 368 | Granite | — |
| `GraniteBlock` 光面花岗岩块 | 3087 | 369 | GraniteBlock | — |
| `GraniteWall` 花岗岩墙 | 3088 | — | — | 184 |
| `GraniteBlockWall` 光面花岗岩墙 | 3089 | — | — | 181 |
| `MeteoriteBrick` 陨石砖 | 3100 | 370 | MeteoriteBrick | — |
| `MeteoriteBrickWall` 陨石砖墙 | 3101 | — | — | 182 |
| `PinkSlimeBlock` 粉史莱姆块 | 3113 | 371 | PinkSlimeBlock | — |
| `PinkTorch` 粉火把 | 3114 | 4 | Torches | — |
| `PeaceCandle` 和平蜡烛 | 3117 | 372 | PeaceCandle | — |
| `GraniteChest` 花岗岩箱 | 3125 | 21 | Containers | — |
| `MeteoriteClock` 陨石钟 | 3126 | 104 | GrandfatherClocks | — |
| `MarbleClock` 大理石时钟 | 3127 | 104 | GrandfatherClocks | — |
| `GraniteClock` 花岗岩时钟 | 3128 | 104 | GrandfatherClocks | — |
| `MeteoriteDoor` 陨石门 | 3129 | 10 | ClosedDoor | — |
| `MarbleDoor` 大理石门 | 3130 | 10 | ClosedDoor | — |
| `GraniteDoor` 花岗岩门 | 3131 | 10 | ClosedDoor | — |
| `MeteoriteDresser` 陨石梳妆台 | 3132 | 88 | Dressers | — |
| `MarbleDresser` 大理石梳妆台 | 3133 | 88 | Dressers | — |
| `GraniteDresser` 花岗岩梳妆台 | 3134 | 88 | Dressers | — |
| `MeteoriteLamp` 陨石灯 | 3135 | 93 | Lamps | — |
| `MarbleLamp` 大理石灯 | 3136 | 93 | Lamps | — |
| `GraniteLamp` 花岗岩灯 | 3137 | 93 | Lamps | — |
| `MeteoriteLantern` 陨石灯笼 | 3138 | 42 | HangingLanterns | — |
| `MarbleLantern` 大理石灯笼 | 3139 | 42 | HangingLanterns | — |
| `GraniteLantern` 花岗岩灯笼 | 3140 | 42 | HangingLanterns | — |
| `MeteoritePiano` 陨石钢琴 | 3141 | 87 | Pianos | — |
| `MarblePiano` 大理石钢琴 | 3142 | 87 | Pianos | — |
| `GranitePiano` 花岗岩钢琴 | 3143 | 87 | Pianos | — |
| `MeteoritePlatform` 陨石平台 | 3144 | 19 | Platforms | — |
| `MarblePlatform` 大理石平台 | 3145 | 19 | Platforms | — |
| `GranitePlatform` 花岗岩平台 | 3146 | 19 | Platforms | — |
| `MeteoriteSink` 陨石水槽 | 3147 | 172 | Sinks | — |
| `MarbleSink` 大理石水槽 | 3148 | 172 | Sinks | — |
| `GraniteSink` 花岗岩水槽 | 3149 | 172 | Sinks | — |
| `MeteoriteSofa` 陨石沙发 | 3150 | 89 | Benches | — |
| `MarbleSofa` 大理石沙发 | 3151 | 89 | Benches | — |
| `GraniteSofa` 花岗岩沙发 | 3152 | 89 | Benches | — |
| `MeteoriteTable` 陨石桌 | 3153 | 14 | Tables | — |
| `MarbleTable` 大理石桌 | 3154 | 14 | Tables | — |
| `GraniteTable` 花岗岩桌 | 3155 | 14 | Tables | — |
| `MeteoriteWorkBench` 陨石工作台 | 3156 | 18 | WorkBenches | — |
| `MarbleWorkBench` 大理石工作台 | 3157 | 18 | WorkBenches | — |
| `GraniteWorkBench` 花岗岩工作台 | 3158 | 18 | WorkBenches | — |
| `MeteoriteBathtub` 陨石浴缸 | 3159 | 90 | Bathtubs | — |
| `MarbleBathtub` 大理石浴缸 | 3160 | 90 | Bathtubs | — |
| `GraniteBathtub` 花岗岩浴缸 | 3161 | 90 | Bathtubs | — |
| `MeteoriteBed` 陨石床 | 3162 | 79 | Beds | — |
| `MarbleBed` 大理石床 | 3163 | 79 | Beds | — |
| `GraniteBed` 花岗岩床 | 3164 | 79 | Beds | — |
| `MeteoriteBookcase` 陨石书架 | 3165 | 101 | Bookcases | — |
| `MarbleBookcase` 大理石书架 | 3166 | 101 | Bookcases | — |
| `GraniteBookcase` 花岗岩书架 | 3167 | 101 | Bookcases | — |
| `MeteoriteCandelabra` 陨石烛台 | 3168 | 100 | Candelabras | — |
| `MarbleCandelabra` 大理石烛台 | 3169 | 100 | Candelabras | — |
| `GraniteCandelabra` 花岗岩烛台 | 3170 | 100 | Candelabras | — |
| `MeteoriteCandle` 陨石蜡烛 | 3171 | 33 | Candles | — |
| `MarbleCandle` 大理石蜡烛 | 3172 | 33 | Candles | — |
| `GraniteCandle` 花岗岩蜡烛 | 3173 | 33 | Candles | — |
| `MeteoriteChair` 陨石椅 | 3174 | 15 | Chairs | — |
| `MarbleChair` 大理石椅 | 3175 | 15 | Chairs | — |
| `GraniteChair` 花岗岩椅 | 3176 | 15 | Chairs | — |
| `MeteoriteChandelier` 陨石吊灯 | 3177 | 34 | Chandeliers | — |
| `MarbleChandelier` 大理石吊灯 | 3178 | 34 | Chandeliers | — |
| `GraniteChandelier` 花岗岩吊灯 | 3179 | 34 | Chandeliers | — |
| `MeteoriteChest` 陨石箱 | 3180 | 21 | Containers | — |
| `MarbleChest` 大理石箱 | 3181 | 21 | Containers | — |
| `MagicWaterDropper` 魔法滴水管 | 3182 | 373 | WaterDrip | — |
| `MagicLavaDropper` 魔法熔岩滴管 | 3184 | 374 | LavaDrip | — |
| `MagicHoneyDropper` 魔法蜂蜜滴管 | 3185 | 375 | HoneyDrip | — |
| `SharpeningStation` 利器站 | 3198 | 377 | SharpeningStation | — |
| `TargetDummy` 傀儡 | 3202 | 378 | TargetDummy | — |
| `CorruptFishingCrate` 腐化匣 | 3203 | 376 | FishingCrate | — |
| `CrimsonFishingCrate` 猩红匣 | 3204 | 376 | FishingCrate | — |
| `DungeonFishingCrate` 地牢匣 | 3205 | 376 | FishingCrate | — |
| `FloatingIslandFishingCrate` 天空匣 | 3206 | 376 | FishingCrate | — |
| `HallowedFishingCrate` 神圣匣 | 3207 | 376 | FishingCrate | — |
| `JungleFishingCrate` 丛林匣 | 3208 | 376 | FishingCrate | — |
| `Bubble` 泡泡 | 3214 | 379 | Bubble | — |
| `DayBloomPlanterBox` 太阳花种植盆 | 3215 | 380 | PlanterBox | — |
| `MoonglowPlanterBox` 月光草花盆 | 3216 | 380 | PlanterBox | — |
| `CorruptPlanterBox` 死亡草种植盆 | 3217 | 380 | PlanterBox | — |
| `CrimsonPlanterBox` 死亡草种植盆 | 3218 | 380 | PlanterBox | — |
| `BlinkrootPlanterBox` 闪耀根种植盆 | 3219 | 380 | PlanterBox | — |
| `WaterleafPlanterBox` 幌菊花盆 | 3220 | 380 | PlanterBox | — |
| `ShiverthornPlanterBox` 寒颤棘种植盆 | 3221 | 380 | PlanterBox | — |
| `FireBlossomPlanterBox` 火焰花花盆 | 3222 | 380 | PlanterBox | — |
| `RichGravestone1` 金十字墓石碑 | 3229 | 85 | Tombstones | — |
| `RichGravestone2` 金墓石 | 3230 | 85 | Tombstones | — |
| `RichGravestone3` 金墓石碑 | 3231 | 85 | Tombstones | — |
| `RichGravestone4` 金墓碑 | 3232 | 85 | Tombstones | — |
| `RichGravestone5` 金碑石 | 3233 | 85 | Tombstones | — |
| `CrystalBlock` 水晶块 | 3234 | 385 | CrystalBlock | — |
| `MusicBoxMartians` 八音盒（火星暴乱） | 3235 | 139 | MusicBoxes | — |
| `MusicBoxPirates` 八音盒（海盗入侵） | 3236 | 139 | MusicBoxes | — |
| `MusicBoxHell` 八音盒（地狱） | 3237 | 139 | MusicBoxes | — |
| `CrystalBlockWall` 水晶块墙 | 3238 | — | — | 186 |
| `Trapdoor` 机关门 | 3239 | 387 | TrapdoorClosed | — |
| `TallGate` 高门 | 3240 | 388 | TallGateClosed | — |
| `LavaLamp` 熔岩灯 | 3253 | 390 | LavaLamp | — |
| `CageEnchantedNightcrawler` 附魔夜行者笼 | 3254 | 391 | CageEnchantedNightcrawler | — |
| `CageBuggy` 蚜虫笼 | 3255 | 392 | CageBuggy | — |
| `CageGrubby` 蛆虫笼 | 3256 | 393 | CageGrubby | — |
| `CageSluggy` 鼻涕虫笼 | 3257 | 394 | CageSluggy | — |
| `SpectreBar` 幽灵锭 | 3261 | 239 | MetalBars | — |
| `ItemFrame` 物品框 | 3270 | 395 | ItemFrame | — |
| `Sandstone` 沙岩块 | 3271 | 396 | Sandstone | — |
| `HardenedSand` 硬化沙块 | 3272 | 397 | HardenedSand | — |
| `SandstoneWall` 沙岩墙 | 3273 | — | — | 275 |
| `CorruptHardenedSand` 硬化黑檀沙块 | 3274 | 398 | CorruptHardenedSand | — |
| `CrimsonHardenedSand` 硬化猩红沙块 | 3275 | 399 | CrimsonHardenedSand | — |
| `CorruptSandstone` 黑檀沙岩块 | 3276 | 400 | CorruptSandstone | — |
| `CrimsonSandstone` 猩红沙岩块 | 3277 | 401 | CrimsonSandstone | — |
| `HallowHardenedSand` 硬化珍珠沙块 | 3338 | 402 | HallowHardenedSand | — |
| `HallowSandstone` 珍珠沙岩块 | 3339 | 403 | HallowSandstone | — |
| `HardenedSandWall` 硬化沙墙 | 3340 | — | — | 304 |
| `CorruptHardenedSandWall` 硬化黑檀沙墙 | 3341 | — | — | 305 |
| `CrimsonHardenedSandWall` 硬化猩红沙墙 | 3342 | — | — | 306 |
| `HallowHardenedSandWall` 硬化珍珠沙墙 | 3343 | — | — | 307 |
| `CorruptSandstoneWall` 黑檀沙岩墙 | 3344 | — | — | 308 |
| `CrimsonSandstoneWall` 猩红沙岩墙 | 3345 | — | — | 309 |
| `HallowSandstoneWall` 珍珠沙岩墙 | 3346 | — | — | 310 |
| `DesertFossil` 沙漠化石 | 3347 | 404 | DesertFossil | — |
| `DesertFossilWall` 沙漠化石墙 | 3348 | — | — | 311 |
| `AncientCultistTrophy` 拜月教邪教徒纪念章 | 3357 | 240 | Painting3X3 | — |
| `MartianSaucerTrophy` 火星飞碟纪念章 | 3358 | 240 | Painting3X3 | — |
| `FlyingDutchmanTrophy` 荷兰飞盗船纪念章 | 3359 | 240 | Painting3X3 | — |
| `LivingMahoganyWand` 生命红木魔棒 | 3360 | 383 | LivingMahogany | — |
| `LivingMahoganyLeafWand` 红木树叶魔棒 | 3361 | 384 | LivingMahoganyLeaves | — |
| `Fireplace` 壁炉 | 3364 | 405 | Fireplace | — |
| `Chimney` 烟囱 | 3365 | 406 | Chimney | — |
| `ConfettiCannon` 彩纸炮 | 3369 | 209 | Cannon | — |
| `MusicBoxTowers` 八音盒（神塔） | 3370 | 139 | MusicBoxes | — |
| `MusicBoxGoblins` 八音盒（哥布林入侵） | 3371 | 139 | MusicBoxes | — |
| `FossilOre` 坚固头盔 | 3380 | 407 | FossilOre | — |
| `StrangePlant1` 奇异植物 | 3385 | 227 | DyePlants | — |
| `StrangePlant2` 奇异植物 | 3386 | 227 | DyePlants | — |
| `StrangePlant3` 奇异植物 | 3387 | 227 | DyePlants | — |
| `StrangePlant4` 奇异植物 | 3388 | 227 | DyePlants | — |
| `GoblinSummonerBanner` 哥布林术士旗帜 | 3390 | 91 | Banners | — |
| `SalamanderBanner` 蝾螈旗 | 3391 | 91 | Banners | — |
| `GiantShellyBanner` 巨型卷壳怪旗 | 3392 | 91 | Banners | — |
| `CrawdadBanner` 龙虾旗 | 3393 | 91 | Banners | — |
| `FritzBanner` 弗里茨旗 | 3394 | 91 | Banners | — |
| `CreatureFromTheDeepBanner` 水月怪旗 | 3395 | 91 | Banners | — |
| `DrManFlyBanner` 飞人博士旗 | 3396 | 91 | Banners | — |
| `MothronBanner` 蛾怪旗 | 3397 | 91 | Banners | — |
| `SeveredHandBanner` 残手旗 | 3398 | 91 | Banners | — |
| `ThePossessedBanner` 攀爬魔旗 | 3399 | 91 | Banners | — |
| `ButcherBanner` 屠夫旗 | 3400 | 91 | Banners | — |
| `PsychoBanner` 变态人旗 | 3401 | 91 | Banners | — |
| `DeadlySphereBanner` 致命球旗 | 3402 | 91 | Banners | — |
| `NailheadBanner` 钉头旗 | 3403 | 91 | Banners | — |
| `PoisonousSporeBanner` 毒孢旗 | 3404 | 91 | Banners | — |
| `MedusaBanner` 蛇发女妖旗 | 3405 | 91 | Banners | — |
| `GreekSkeletonBanner` 装甲步兵旗 | 3406 | 91 | Banners | — |
| `GraniteFlyerBanner` 花岗精旗 | 3407 | 91 | Banners | — |
| `GraniteGolemBanner` 花岗岩巨人横幅 | 3408 | 91 | Banners | — |
| `BloodZombieBanner` 血腥僵尸旗 | 3409 | 91 | Banners | — |
| `DripplerBanner` 滴滴怪旗 | 3410 | 91 | Banners | — |
| `TombCrawlerBanner` 墓穴爬虫旗 | 3411 | 91 | Banners | — |
| `DuneSplicerBanner` 沙虫旗 | 3412 | 91 | Banners | — |
| `FlyingAntlionBanner` 蚁狮蜂旗 | 3413 | 91 | Banners | — |
| `WalkingAntlionBanner` 蚁狮马旗 | 3414 | 91 | Banners | — |
| `DesertGhoulBanner` 食尸鬼旗 | 3415 | 91 | Banners | — |
| `DesertLamiaBanner` 拉弥亚旗 | 3416 | 91 | Banners | — |
| `DesertDjinnBanner` 沙漠幽魂旗 | 3417 | 91 | Banners | — |
| `DesertBasiliskBanner` 蛇蜥怪旗 | 3418 | 91 | Banners | — |
| `RavagerScorpionBanner` 沙贼横幅 | 3419 | 91 | Banners | — |
| `StardustSoldierBanner` 观星怪旗 | 3420 | 91 | Banners | — |
| `StardustWormBanner` 银河织妖旗 | 3421 | 91 | Banners | — |
| `StardustJellyfishBanner` 液体入侵怪旗 | 3422 | 91 | Banners | — |
| `StardustSpiderBanner` 闪耀炮手旗 | 3423 | 91 | Banners | — |
| `StardustSmallCellBanner` 迷你星细胞法杖旗 | 3424 | 91 | Banners | — |
| `StardustLargeCellBanner` 星细胞法杖旗 | 3425 | 91 | Banners | — |
| `SolarCoriteBanner` 流星火怪旗 | 3426 | 91 | Banners | — |
| `SolarSrollerBanner` 火滚怪旗 | 3427 | 91 | Banners | — |
| `SolarCrawltipedeBanner` 千足蜈蚣旗 | 3428 | 91 | Banners | — |
| `SolarDrakomireRiderBanner` 火龙怪骑士旗 | 3429 | 91 | Banners | — |
| `SolarDrakomireBanner` 火龙怪旗 | 3430 | 91 | Banners | — |
| `SolarSolenianBanner` 火月怪旗 | 3431 | 91 | Banners | — |
| `NebulaSoldierBanner` 预言帝旗 | 3432 | 91 | Banners | — |
| `NebulaHeadcrabBanner` 吮脑怪旗 | 3433 | 91 | Banners | — |
| `NebulaBrainBanner` 星云浮怪旗 | 3434 | 91 | Banners | — |
| `NebulaBeastBanner` 进化兽旗 | 3435 | 91 | Banners | — |
| `VortexLarvaBanner` 异星幼虫旗 | 3436 | 91 | Banners | — |
| `VortexHornetQueenBanner` 异星蜂王旗 | 3437 | 91 | Banners | — |
| `VortexHornetBanner` 异星黄蜂旗 | 3438 | 91 | Banners | — |
| `VortexSoldierBanner` 星旋怪旗 | 3439 | 91 | Banners | — |
| `VortexRiflemanBanner` 漩泥怪旗 | 3440 | 91 | Banners | — |
| `PirateCaptainBanner` 海盗船长旗 | 3441 | 91 | Banners | — |
| `PirateDeadeyeBanner` 海盗神射手旗 | 3442 | 91 | Banners | — |
| `PirateCorsairBanner` 私船海盗旗 | 3443 | 91 | Banners | — |
| `PirateCrossbowerBanner` 海盗弩手旗 | 3444 | 91 | Banners | — |
| `MartianWalkerBanner` 火星走妖旗 | 3445 | 91 | Banners | — |
| `RedDevilBanner` 红魔鬼旗 | 3446 | 91 | Banners | — |
| `PinkJellyfishBanner` 粉水母旗 | 3447 | 91 | Banners | — |
| `GreenJellyfishBanner` 绿水母旗 | 3448 | 91 | Banners | — |
| `DarkMummyBanner` 暗黑木乃伊旗 | 3449 | 91 | Banners | — |
| `LightMummyBanner` 光明木乃伊旗 | 3450 | 91 | Banners | — |
| `AngryBonesBanner` 愤怒骷髅怪旗 | 3451 | 91 | Banners | — |
| `IceTortoiseBanner` 冰雪陆龟旗 | 3452 | 91 | Banners | — |
| `LunarOre` 夜明矿 | 3460 | 408 | LunarOre | — |
| `LunarBrick` 夜明砖 | 3461 | 409 | LunarBrick | — |
| `LunarBar` 夜明锭 | 3467 | 239 | MetalBars | — |
| `LunarBrickWall` 夜明砖墙 | 3472 | — | — | 224 |
| `VortexMonolith` 星旋天塔柱 | 3536 | 410 | LunarMonolith | — |
| `NebulaMonolith` 星云天塔柱 | 3537 | 410 | LunarMonolith | — |
| `StardustMonolith` 星尘天塔柱 | 3538 | 410 | LunarMonolith | — |
| `SolarMonolith` 日耀天塔柱 | 3539 | 410 | LunarMonolith | — |
| `Detonator` 引爆器 | 3545 | 411 | Detonator | — |
| `LunarCraftingStation` 远古操纵机 | 3549 | 412 | LunarCraftingStation | — |
| `SquirrelOrangeCage` 红松鼠笼 | 3565 | 413 | SquirrelOrangeCage | — |
| `SquirrelGoldCage` 金松鼠笼 | 3566 | 414 | SquirrelGoldCage | — |
| `LunarBlockSolar` 日耀碎片块 | 3573 | 415 | LunarBlockSolar | — |
| `LunarBlockVortex` 星旋碎片块 | 3574 | 416 | LunarBlockVortex | — |
| `LunarBlockNebula` 星云碎片块 | 3575 | 417 | LunarBlockNebula | — |
| `LunarBlockStardust` 星尘碎片块 | 3576 | 418 | LunarBlockStardust | — |
| `LivingLeafWall` 生命树叶墙 | 3584 | — | — | 60 |
| `SandSlimeBanner` 沙史莱姆旗 | 3593 | 91 | Banners | — |
| `SeaSnailBanner` 海蜗牛旗 | 3594 | 91 | Banners | — |
| `MoonLordTrophy` 月亮领主纪念章 | 3595 | 240 | Painting3X3 | — |
| `MoonLordPainting` 不是小孩，也不是乌贼 | 3596 | 242 | Painting6X4 | — |
| `LogicGateLamp_Off` 逻辑门灯（关） | 3602 | 419 | LogicGateLamp | — |
| `LogicGate_AND` 逻辑门（与） | 3603 | 420 | LogicGate | — |
| `LogicGate_OR` 逻辑门（或） | 3604 | 420 | LogicGate | — |
| `LogicGate_NAND` 逻辑门（与非） | 3605 | 420 | LogicGate | — |
| `LogicGate_NOR` 逻辑门（或非） | 3606 | 420 | LogicGate | — |
| `LogicGate_XOR` 逻辑门（异或） | 3607 | 420 | LogicGate | — |
| `LogicGate_NXOR` 逻辑门（同或） | 3608 | 420 | LogicGate | — |
| `ConveyorBeltLeft` 传送带（顺时针） | 3609 | 421 | ConveyorBeltLeft | — |
| `ConveyorBeltRight` 传送带（逆时针） | 3610 | 422 | ConveyorBeltRight | — |
| `LogicSensor_Sun` 逻辑感应器（昼） | 3613 | 423 | LogicSensor | — |
| `LogicSensor_Moon` 逻辑感应器（夜） | 3614 | 423 | LogicSensor | — |
| `LogicSensor_Above` 逻辑感应器（玩家出入上方） | 3615 | 423 | LogicSensor | — |
| `WirePipe` 分线盒 | 3616 | 424 | WirePipe | — |
| `AnnouncementBox` 广播盒 | 3617 | 425 | AnnouncementBox | — |
| `LogicGateLamp_On` 逻辑门灯（开） | 3618 | 419 | LogicGateLamp | — |
| `TeamBlockRed` 红团队块 | 3621 | 426 | TeamBlockRed | — |
| `TeamBlockRedPlatform` 红团队平台 | 3622 | 427 | TeamBlockRedPlatform | — |
| `WeightedPressurePlatePink` 粉色加重压力板 | 3626 | 428 | WeightedPressurePlate | — |
| `WireBulb` 彩线灯泡 | 3629 | 429 | WireBulb | — |
| `WeightedPressurePlateOrange` 橙色加重压力板 | 3630 | 428 | WeightedPressurePlate | — |
| `WeightedPressurePlatePurple` 紫色加重压力板 | 3631 | 428 | WeightedPressurePlate | — |
| `WeightedPressurePlateCyan` 青色加重压力板 | 3632 | 428 | WeightedPressurePlate | — |
| `TeamBlockGreen` 绿团队块 | 3633 | 430 | TeamBlockGreen | — |
| `TeamBlockBlue` 蓝团队块 | 3634 | 431 | TeamBlockBlue | — |
| `TeamBlockYellow` 黄团队块 | 3635 | 432 | TeamBlockYellow | — |
| `TeamBlockPink` 粉团队块 | 3636 | 433 | TeamBlockPink | — |
| `TeamBlockWhite` 白团队块 | 3637 | 434 | TeamBlockWhite | — |
| `TeamBlockGreenPlatform` 绿团队平台 | 3638 | 435 | TeamBlockGreenPlatform | — |
| `TeamBlockBluePlatform` 蓝团队平台 | 3639 | 436 | TeamBlockBluePlatform | — |
| `TeamBlockYellowPlatform` 黄团队平台 | 3640 | 437 | TeamBlockYellowPlatform | — |
| `TeamBlockPinkPlatform` 粉团队平台 | 3641 | 438 | TeamBlockPinkPlatform | — |
| `TeamBlockWhitePlatform` 白团队平台 | 3642 | 439 | TeamBlockWhitePlatform | — |
| `GemLockRuby` 红玉宝石锁 | 3644 | 440 | GemLocks | — |
| `GemLockSapphire` 蓝玉宝石锁 | 3645 | 440 | GemLocks | — |
| `GemLockEmerald` 翡翠宝石锁 | 3646 | 440 | GemLocks | — |
| `GemLockTopaz` 黄玉宝石锁 | 3647 | 440 | GemLocks | — |
| `GemLockAmethyst` 紫晶宝石锁 | 3648 | 440 | GemLocks | — |
| `GemLockDiamond` 钻石宝石锁 | 3649 | 440 | GemLocks | — |
| `GemLockAmber` 琥珀宝石锁 | 3650 | 440 | GemLocks | — |
| `SquirrelStatue` 松鼠雕像 | 3651 | 105 | Statues | — |
| `ButterflyStatue` 蝴蝶雕像 | 3652 | 105 | Statues | — |
| `WormStatue` 蠕虫雕像 | 3653 | 105 | Statues | — |
| `FireflyStatue` 萤火虫雕像 | 3654 | 105 | Statues | — |
| `ScorpionStatue` 蝎子雕像 | 3655 | 105 | Statues | — |
| `SnailStatue` 蜗牛雕像 | 3656 | 105 | Statues | — |
| `GrasshopperStatue` 蚱蜢雕像 | 3657 | 105 | Statues | — |
| `MouseStatue` 老鼠雕像 | 3658 | 105 | Statues | — |
| `DuckStatue` 小鸭雕像 | 3659 | 105 | Statues | — |
| `PenguinStatue` 企鹅雕像 | 3660 | 105 | Statues | — |
| `FrogStatue` 青蛙雕像 | 3661 | 105 | Statues | — |
| `BuggyStatue` 蚜虫雕像 | 3662 | 105 | Statues | — |
| `LogicGateLamp_Faulty` 逻辑门灯（故障） | 3663 | 419 | LogicGateLamp | — |
| `PortalGunStation` 传送枪站 | 3664 | 209 | Cannon | — |
| `Fake_Chest` 受困宝箱 | 3665 | 441 | FakeContainers | — |
| `Fake_GoldChest` 受困金箱 | 3666 | 441 | FakeContainers | — |
| `Fake_ShadowChest` 受困暗影箱 | 3667 | 441 | FakeContainers | — |
| `Fake_EbonwoodChest` 受困乌木箱 | 3668 | 441 | FakeContainers | — |
| `Fake_RichMahoganyChest` 受困红木箱 | 3669 | 441 | FakeContainers | — |
| `Fake_PearlwoodChest` 受困珍珠木箱 | 3670 | 441 | FakeContainers | — |
| `Fake_IvyChest` 受困常春藤箱 | 3671 | 441 | FakeContainers | — |
| `Fake_IceChest` 受困冰冻箱 | 3672 | 441 | FakeContainers | — |
| `Fake_LivingWoodChest` 受困生命木箱 | 3673 | 441 | FakeContainers | — |
| `Fake_SkywareChest` 受困天域箱 | 3674 | 441 | FakeContainers | — |
| `Fake_ShadewoodChest` 受困暗影木箱 | 3675 | 441 | FakeContainers | — |
| `Fake_WebCoveredChest` 受困蛛丝箱 | 3676 | 441 | FakeContainers | — |
| `Fake_LihzahrdChest` 受困丛林蜥蜴箱 | 3677 | 441 | FakeContainers | — |
| `Fake_WaterChest` 受困水中箱 | 3678 | 441 | FakeContainers | — |
| `Fake_JungleChest` 受困丛林箱 | 3679 | 441 | FakeContainers | — |
| `Fake_CorruptionChest` 受困腐化箱 | 3680 | 441 | FakeContainers | — |
| `Fake_CrimsonChest` 受困猩红箱 | 3681 | 441 | FakeContainers | — |
| `Fake_HallowedChest` 受困神圣箱 | 3682 | 441 | FakeContainers | — |
| `Fake_FrozenChest` 受困冰雪箱 | 3683 | 441 | FakeContainers | — |
| `Fake_DynastyChest` 受困王朝箱 | 3684 | 441 | FakeContainers | — |
| `Fake_HoneyChest` 受困蜂蜜箱 | 3685 | 441 | FakeContainers | — |
| `Fake_SteampunkChest` 受困蒸汽朋克箱 | 3686 | 441 | FakeContainers | — |
| `Fake_PalmWoodChest` 受困棕榈木箱 | 3687 | 441 | FakeContainers | — |
| `Fake_MushroomChest` 受困蘑菇箱 | 3688 | 441 | FakeContainers | — |
| `Fake_BorealWoodChest` 受困针叶木箱 | 3689 | 441 | FakeContainers | — |
| `Fake_SlimeChest` 受困史莱姆箱 | 3690 | 441 | FakeContainers | — |
| `Fake_GreenDungeonChest` 受困绿地牢箱 | 3691 | 441 | FakeContainers | — |
| `Fake_PinkDungeonChest` 受困粉地牢箱 | 3692 | 441 | FakeContainers | — |
| `Fake_BlueDungeonChest` 受困蓝地牢箱 | 3693 | 441 | FakeContainers | — |
| `Fake_BoneChest` 受困骨箱 | 3694 | 441 | FakeContainers | — |
| `Fake_CactusChest` 受困仙人掌箱 | 3695 | 441 | FakeContainers | — |
| `Fake_FleshChest` 受困血肉箱 | 3696 | 441 | FakeContainers | — |
| `Fake_ObsidianChest` 受困黑曜石箱 | 3697 | 441 | FakeContainers | — |
| `Fake_PumpkinChest` 受困南瓜箱 | 3698 | 441 | FakeContainers | — |
| `Fake_SpookyChest` 受困阴森箱 | 3699 | 441 | FakeContainers | — |
| `Fake_GlassChest` 受困玻璃箱 | 3700 | 441 | FakeContainers | — |
| `Fake_MartianChest` 受困火星箱 | 3701 | 441 | FakeContainers | — |
| `Fake_MeteoriteChest` 受困陨石箱 | 3702 | 441 | FakeContainers | — |
| `Fake_GraniteChest` 受困花岗岩箱 | 3703 | 441 | FakeContainers | — |
| `Fake_MarbleChest` 受困大理石箱 | 3704 | 441 | FakeContainers | — |
| `Fake_newchest1` ItemName.Fake_newchest1 | 3705 | 441 | FakeContainers | — |
| `Fake_newchest2` ItemName.Fake_newchest2 | 3706 | 441 | FakeContainers | — |
| `ProjectilePressurePad` 青绿压力垫板 | 3707 | 442 | ProjectilePressurePad | — |
| `WallCreeperStatue` 爬墙蜘蛛雕像 | 3708 | 105 | Statues | — |
| `UnicornStatue` 独角兽雕像 | 3709 | 105 | Statues | — |
| `DripplerStatue` 滴滴怪雕像 | 3710 | 105 | Statues | — |
| `WraithStatue` 幻灵雕像 | 3711 | 105 | Statues | — |
| `BoneSkeletonStatue` 骨头骷髅雕像 | 3712 | 105 | Statues | — |
| `UndeadVikingStatue` 亡灵维京海盗雕像 | 3713 | 105 | Statues | — |
| `MedusaStatue` 蛇发女妖雕像 | 3714 | 105 | Statues | — |
| `HarpyStatue` 鸟妖雕像 | 3715 | 105 | Statues | — |
| `PigronStatue` 猪龙雕像 | 3716 | 105 | Statues | — |
| `HopliteStatue` 装甲步兵雕像 | 3717 | 105 | Statues | — |
| `GraniteGolemStatue` 花岗岩巨人雕像 | 3718 | 105 | Statues | — |
| `ZombieArmStatue` 武装僵尸雕像 | 3719 | 105 | Statues | — |
| `BloodZombieStatue` 血腥僵尸雕像 | 3720 | 105 | Statues | — |
| `GeyserTrap` 喷泉 | 3722 | 443 | GeyserTrap | — |
| `UltraBrightCampfire` 超亮篝火 | 3723 | 215 | Campfire | — |
| `BoneCampfire` 骨头篝火 | 3724 | 215 | Campfire | — |
| `PixelBox` 像素盒 | 3725 | 445 | PixelBox | — |
| `LogicSensor_Water` 液体感应器（水） | 3726 | 423 | LogicSensor | — |
| `LogicSensor_Lava` 液体感应器（熔岩） | 3727 | 423 | LogicSensor | — |
| `LogicSensor_Honey` 液体感应器（蜂蜜） | 3728 | 423 | LogicSensor | — |
| `LogicSensor_Liquid` 液体感应器（任何） | 3729 | 423 | LogicSensor | — |
| `SillyBalloonPink` 呆萌粉气球 | 3736 | 446 | SillyBalloonPink | — |
| `SillyBalloonPurple` 呆萌紫气球 | 3737 | 447 | SillyBalloonPurple | — |
| `SillyBalloonGreen` 呆萌绿气球 | 3738 | 448 | SillyBalloonGreen | — |
| `SillyStreamerBlue` 蓝饰带 | 3739 | 449 | SillyStreamerBlue | — |
| `SillyStreamerGreen` 绿饰带 | 3740 | 450 | SillyStreamerGreen | — |
| `SillyStreamerPink` 粉饰带 | 3741 | 451 | SillyStreamerPink | — |
| `SillyBalloonMachine` 呆萌气球机 | 3742 | 452 | SillyBalloonMachine | — |
| `SillyBalloonTiedPink` 呆萌丝带气球（粉） | 3743 | 453 | SillyBalloonTile | — |
| `SillyBalloonTiedPurple` 呆萌丝带气球（紫） | 3744 | 453 | SillyBalloonTile | — |
| `SillyBalloonTiedGreen` 呆萌丝带气球（绿） | 3745 | 453 | SillyBalloonTile | — |
| `Pigronata` 猪龙彩罐 | 3746 | 454 | Pigronata | — |
| `PartyMonolith` 派对中心 | 3747 | 455 | PartyMonolith | — |
| `PartyBundleOfBalloonTile` 呆萌丝带派对气球束 | 3748 | 456 | PartyBundleOfBalloonTile | — |
| `PartyPresent` 派对礼物 | 3749 | 457 | PartyPresent | — |
| `SliceOfCake` 蛋糕块 | 3750 | 621 | SliceOfCake | — |
| `CogWall` 齿轮墙 | 3751 | — | — | 225 |
| `SandFallWall` 沙暴墙 | 3752 | — | — | 226 |
| `SnowFallWall` 降雪墙 | 3753 | — | — | 227 |
| `SandFallBlock` 沙暴块 | 3754 | 458 | SandFallBlock | — |
| `SnowFallBlock` 降雪块 | 3755 | 459 | SnowFallBlock | — |
| `SnowCloudBlock` 雪云 | 3756 | 460 | SnowCloud | — |
| `SillyBalloonPinkWall` 呆萌粉气球墙 | 3760 | — | — | 228 |
| `SillyBalloonPurpleWall` 呆萌紫气球墙 | 3761 | — | — | 229 |
| `SillyBalloonGreenWall` 呆萌绿气球墙 | 3762 | — | — | 230 |
| `SandElementalBanner` 沙尘精旗 | 3780 | 91 | Banners | — |
| `MagicSandDropper` 魔法沙粒滴管 | 3782 | 461 | SandDrip | — |
| `SandsharkBanner` 沙鲨旗 | 3789 | 91 | Banners | — |
| `SandsharkCorruptBanner` 噬骨沙鲨旗 | 3790 | 91 | Banners | — |
| `SandsharkCrimsonBanner` 戮血沙鲨旗 | 3791 | 91 | Banners | — |
| `SandsharkHallowedBanner` 晶狐沙鲨旗 | 3792 | 91 | Banners | — |
| `TumbleweedBanner` 愤怒翻滚怪旗 | 3793 | 91 | Banners | — |
| `DjinnLamp` 沙漠幽魂灯 | 3795 | 462 | DjinnLamp | — |
| `MusicBoxSandstorm` 八音盒（沙尘暴） | 3796 | 139 | MusicBoxes | — |
| `DefendersForge` 护卫熔炉 | 3813 | 463 | DefendersForge | — |
| `WarTable` 战争桌 | 3814 | 464 | WarTable | — |
| `WarTableBanner` 战争桌旗 | 3815 | 465 | WarTableBanner | — |
| `DD2ElderCrystalStand` 埃特尼亚水晶座 | 3816 | 466 | ElderCrystalStand | — |
| `DD2GoblinBomberBanner` 天国哥布林投弹手旗 | 3837 | 91 | Banners | — |
| `DD2GoblinBanner` 天国哥布林旗 | 3838 | 91 | Banners | — |
| `DD2SkeletonBanner` 撒旦骷髅旗 | 3839 | 91 | Banners | — |
| `DD2DrakinBanner` 德拉克龙旗 | 3840 | 91 | Banners | — |
| `DD2KoboldFlyerBanner` 小妖魔滑翔怪旗 | 3841 | 91 | Banners | — |
| `DD2KoboldBanner` 小妖魔旗 | 3842 | 91 | Banners | — |
| `DD2WitherBeastBanner` 枯萎兽旗 | 3843 | 91 | Banners | — |
| `DD2WyvernBanner` 天国飞龙旗 | 3844 | 91 | Banners | — |
| `DD2JavelinThrowerBanner` 天国标枪投掷怪旗 | 3845 | 91 | Banners | — |
| `DD2LightningBugBanner` 天国荧光虫旗 | 3846 | 91 | Banners | — |
| `BossTrophyBetsy` 双足翼龙纪念章 | 3866 | 240 | Painting3X3 | — |
| `BossTrophyDarkmage` 暗黑魔法师纪念章 | 3867 | 240 | Painting3X3 | — |
| `BossTrophyOgre` 食人魔纪念章 | 3868 | 240 | Painting3X3 | — |
| `MusicBoxDD2` 音乐盒（撒旦军队） | 3869 | 139 | MusicBoxes | — |
| `CrystalChest` 水晶宝箱 | 3884 | 467 | Containers2 | — |
| `GoldenChest` 金色宝箱 | 3885 | 467 | Containers2 | — |
| `Fake_CrystalChest` 受困水晶宝箱 | 3886 | 468 | FakeContainers2 | — |
| `Fake_GoldenChest` 受困金色宝箱 | 3887 | 468 | FakeContainers2 | — |
| `CrystalDoor` 水晶门 | 3888 | 10 | ClosedDoor | — |
| `CrystalChair` 水晶椅子 | 3889 | 15 | Chairs | — |
| `CrystalCandle` 水晶蜡烛 | 3890 | 33 | Candles | — |
| `CrystalLantern` 水晶灯笼 | 3891 | 42 | HangingLanterns | — |
| `CrystalLamp` 水晶灯 | 3892 | 93 | Lamps | — |
| `CrystalCandelabra` 水晶烛台 | 3893 | 100 | Candelabras | — |
| `CrystalChandelier` 水晶吊灯 | 3894 | 34 | Chandeliers | — |
| `CrystalBathtub` 水晶浴缸 | 3895 | 90 | Bathtubs | — |
| `CrystalSink` 水晶水槽 | 3896 | 172 | Sinks | — |
| `CrystalBed` 水晶床 | 3897 | 79 | Beds | — |
| `CrystalClock` 水晶时钟 | 3898 | 104 | GrandfatherClocks | — |
| `SkywareClock2` 日盘时钟 | 3899 | 104 | GrandfatherClocks | — |
| `DungeonClockBlue` 蓝地牢时钟 | 3900 | 104 | GrandfatherClocks | — |
| `DungeonClockGreen` 绿地牢时钟 | 3901 | 104 | GrandfatherClocks | — |
| `DungeonClockPink` 粉地牢时钟 | 3902 | 104 | GrandfatherClocks | — |
| `CrystalPlatform` 水晶平台 | 3903 | 19 | Platforms | — |
| `GoldenPlatform` 金色平台 | 3904 | 19 | Platforms | — |
| `DynastyPlatform` 王朝木平台 | 3905 | 19 | Platforms | — |
| `LihzahrdPlatform` 丛林蜥蜴平台 | 3906 | 19 | Platforms | — |
| `FleshPlatform` 血肉平台 | 3907 | 19 | Platforms | — |
| `FrozenPlatform` 冰冻平台 | 3908 | 19 | Platforms | — |
| `CrystalWorkbench` 水晶工作台 | 3909 | 18 | WorkBenches | — |
| `GoldenWorkbench` 金色工作台 | 3910 | 18 | WorkBenches | — |
| `CrystalDresser` 水晶梳妆台 | 3911 | 88 | Dressers | — |
| `DynastyDresser` 王朝梳妆台 | 3912 | 88 | Dressers | — |
| `FrozenDresser` 冰冻梳妆台 | 3913 | 88 | Dressers | — |
| `LivingWoodDresser` 生命木梳妆台 | 3914 | 88 | Dressers | — |
| `CrystalPiano` 水晶钢琴 | 3915 | 87 | Pianos | — |
| `DynastyPiano` 王朝钢琴 | 3916 | 87 | Pianos | — |
| `CrystalBookCase` 水晶书架 | 3917 | 101 | Bookcases | — |
| `CrystalSofaHowDoesThatEvenWork` 水晶沙发 | 3918 | 89 | Benches | — |
| `DynastySofa` 王朝沙发 | 3919 | 89 | Benches | — |
| `CrystalTable` 水晶桌 | 3920 | 469 | Tables2 | — |
| `SpiderBathtub` 蜘蛛浴缸 | 3931 | 90 | Bathtubs | — |
| `SpiderBed` 蜘蛛床 | 3932 | 79 | Beds | — |
| `SpiderBookcase` 蜘蛛书架 | 3933 | 101 | Bookcases | — |
| `SpiderDresser` 蜘蛛梳妆台 | 3934 | 88 | Dressers | — |
| `SpiderCandelabra` 蜘蛛烛台 | 3935 | 100 | Candelabras | — |
| `SpiderCandle` 蜘蛛蜡烛 | 3936 | 33 | Candles | — |
| `SpiderChair` 蜘蛛椅 | 3937 | 15 | Chairs | — |
| `SpiderChandelier` 蜘蛛吊灯 | 3938 | 34 | Chandeliers | — |
| `SpiderChest` 蜘蛛箱 | 3939 | 467 | Containers2 | — |
| `SpiderClock` 蜘蛛时钟 | 3940 | 104 | GrandfatherClocks | — |
| `SpiderDoor` 蜘蛛门 | 3941 | 10 | ClosedDoor | — |
| `SpiderLamp` 蜘蛛灯 | 3942 | 93 | Lamps | — |
| `SpiderLantern` 蜘蛛灯笼 | 3943 | 42 | HangingLanterns | — |
| `SpiderPiano` 蜘蛛钢琴 | 3944 | 87 | Pianos | — |
| `SpiderPlatform` 蜘蛛平台 | 3945 | 19 | Platforms | — |
| `SpiderSinkSpiderSinkDoesWhateverASpiderSinkDoes` 蜘蛛水槽 | 3946 | 172 | Sinks | — |
| `SpiderSofa` 蜘蛛沙发 | 3947 | 89 | Benches | — |
| `SpiderTable` 蜘蛛桌 | 3948 | 469 | Tables2 | — |
| `SpiderWorkbench` 蜘蛛工作台 | 3949 | 18 | WorkBenches | — |
| `Fake_SpiderChest` 受困蜘蛛箱 | 3950 | 468 | FakeContainers2 | — |
| `IronBrick` 铁砖 | 3951 | 472 | IronBrick | — |
| `IronBrickWall` 铁砖墙 | 3952 | — | — | 231 |
| `LeadBrick` 铅砖 | 3953 | 473 | LeadBrick | — |
| `LeadBrickWall` 铅砖墙 | 3954 | — | — | 232 |
| `LesionBlock` 损伤块 | 3955 | 474 | LesionBlock | — |
| `LesionBlockWall` 损伤块墙 | 3956 | — | — | 233 |
| `LesionPlatform` 损伤平台 | 3957 | 19 | Platforms | — |
| `LesionBathtub` 损伤浴缸 | 3958 | 90 | Bathtubs | — |
| `LesionBed` 损伤床 | 3959 | 79 | Beds | — |
| `LesionBookcase` 损伤书架 | 3960 | 101 | Bookcases | — |
| `LesionCandelabra` 损伤烛台 | 3961 | 100 | Candelabras | — |
| `LesionCandle` 损伤蜡烛 | 3962 | 33 | Candles | — |
| `LesionChair` 损伤椅 | 3963 | 15 | Chairs | — |
| `LesionChandelier` 损伤吊灯 | 3964 | 34 | Chandeliers | — |
| `LesionChest` 损伤宝箱 | 3965 | 467 | Containers2 | — |
| `LesionClock` 损伤时钟 | 3966 | 104 | GrandfatherClocks | — |
| `LesionDoor` 损伤门 | 3967 | 10 | ClosedDoor | — |
| `LesionDresser` 损伤梳妆台 | 3968 | 88 | Dressers | — |
| `LesionLamp` 损伤灯 | 3969 | 93 | Lamps | — |
| `LesionLantern` 损伤灯笼 | 3970 | 42 | HangingLanterns | — |
| `LesionPiano` 损伤钢琴 | 3971 | 87 | Pianos | — |
| `LesionSink` 损伤水槽 | 3972 | 172 | Sinks | — |
| `LesionSofa` 损伤沙发 | 3973 | 89 | Benches | — |
| `LesionTable` 损伤桌 | 3974 | 469 | Tables2 | — |
| `LesionWorkbench` 损伤工作台 | 3975 | 18 | WorkBenches | — |
| `Fake_LesionChest` 受困损伤箱 | 3976 | 468 | FakeContainers2 | — |
| `HatRack` 帽架 | 3977 | 475 | HatRack | — |
| `WoodenCrateHard` 珍珠木匣 | 3979 | 376 | FishingCrate | — |
| `IronCrateHard` 秘银匣 | 3980 | 376 | FishingCrate | — |
| `GoldenCrateHard` 钛金匣 | 3981 | 376 | FishingCrate | — |
| `CorruptFishingCrateHard` 污损匣 | 3982 | 376 | FishingCrate | — |
| `CrimsonFishingCrateHard` 血匣 | 3983 | 376 | FishingCrate | — |
| `DungeonFishingCrateHard` 围栏匣 | 3984 | 376 | FishingCrate | — |
| `FloatingIslandFishingCrateHard` 天蓝匣 | 3985 | 376 | FishingCrate | — |
| `HallowedFishingCrateHard` 天赐匣 | 3986 | 376 | FishingCrate | — |
| `JungleFishingCrateHard` 荆棘匣 | 3987 | 376 | FishingCrate | — |
| `DeadMansChest` 死人宝箱 | 3988 | 467 | Containers2 | — |
| `GolfCup` 高尔夫球洞 | 4040 | 476 | GolfHole | — |
| `FlowerPacketBlue` 蓝花种子 | 4041 | 3 | Plants | — |
| `FlowerPacketMagenta` 洋红花种子 | 4042 | 3 | Plants | — |
| `FlowerPacketPink` 粉花种子 | 4043 | 3 | Plants | — |
| `FlowerPacketRed` 红花种子 | 4044 | 3 | Plants | — |
| `FlowerPacketYellow` 黄花种子 | 4045 | 3 | Plants | — |
| `FlowerPacketViolet` 蓝紫花种子 | 4046 | 3 | Plants | — |
| `FlowerPacketWhite` 白花种子 | 4047 | 3 | Plants | — |
| `FlowerPacketTallGrass` 高茎草种子 | 4048 | 3 | Plants | — |
| `CrimstoneBrick` 猩红石砖 | 4050 | 478 | CrimstoneBrick | — |
| `SmoothSandstone` 光面沙岩 | 4051 | 479 | SmoothSandstone | — |
| `CrimstoneBrickWall` 猩红石砖墙 | 4052 | — | — | 234 |
| `SmoothSandstoneWall` 光面沙岩墙 | 4053 | — | — | 235 |
| `BloodMoonMonolith` 血月天塔柱 | 4054 | 480 | BloodMoonMonolith | — |
| `DrumSet` 鼓组 | 4063 | 486 | DrumSet | — |
| `PicnicTable` 野餐桌 | 4064 | 487 | PicnicTable | — |
| `PicnicTableWithCloth` 精致野餐桌 | 4065 | 487 | PicnicTable | — |
| `JunoniaShell` 眼蛱蝶壳 | 4071 | 324 | BeachPiles | — |
| `LightningWhelkShell` 闪电海螺壳 | 4072 | 324 | BeachPiles | — |
| `TulipShell` 郁金香壳 | 4073 | 324 | BeachPiles | — |
| `PinWheel` 风车 | 4074 | 489 | PinWheel | — |
| `WeatherVane` 风向标 | 4075 | 490 | WeatherVane | — |
| `VoidVault` 虚空保险库 | 4076 | 491 | VoidVault | — |
| `MusicBoxOceanAlt` 八音盒（夜间海洋） | 4077 | 139 | MusicBoxes | — |
| `MusicBoxSlimeRain` 八音盒（史莱姆雨） | 4078 | 139 | MusicBoxes | — |
| `MusicBoxSpaceAlt` 八音盒（日间太空） | 4079 | 139 | MusicBoxes | — |
| `MusicBoxTownDay` 八音盒（日间小镇） | 4080 | 139 | MusicBoxes | — |
| `MusicBoxTownNight` 八音盒（夜间小镇） | 4081 | 139 | MusicBoxes | — |
| `MusicBoxWindyDay` 八音盒（有风的日子） | 4082 | 139 | MusicBoxes | — |
| `GolfCupFlagWhite` 白三角旗 | 4083 | 493 | GolfCupFlag | — |
| `GolfCupFlagRed` 红三角旗 | 4084 | 493 | GolfCupFlag | — |
| `GolfCupFlagGreen` 绿三角旗 | 4085 | 493 | GolfCupFlag | — |
| `GolfCupFlagBlue` 蓝三角旗 | 4086 | 493 | GolfCupFlag | — |
| `GolfCupFlagYellow` 黄三角旗 | 4087 | 493 | GolfCupFlag | — |
| `GolfCupFlagPurple` 紫三角旗 | 4088 | 493 | GolfCupFlag | — |
| `GolfTee` 高尔夫球座 | 4089 | 494 | GolfTee | — |
| `ShellPileBlock` 贝壳堆 | 4090 | 495 | ShellPile | — |
| `AntiPortalBlock` 反入口块 | 4091 | 496 | AntiPortalBlock | — |
| `ToiletEbonyWood` 乌木马桶 | 4096 | 497 | Toilets | — |
| `ToiletRichMahogany` 红木马桶 | 4097 | 497 | Toilets | — |
| `ToiletPearlwood` 珍珠木马桶 | 4098 | 497 | Toilets | — |
| `ToiletLivingWood` 生命木马桶 | 4099 | 497 | Toilets | — |
| `ToiletCactus` 仙人掌马桶 | 4100 | 497 | Toilets | — |
| `ToiletBone` 骨头马桶 | 4101 | 497 | Toilets | — |
| `ToiletFlesh` 血肉马桶 | 4102 | 497 | Toilets | — |
| `ToiletMushroom` 蘑菇马桶 | 4103 | 497 | Toilets | — |
| `ToiletSunplate` 天域马桶 | 4104 | 497 | Toilets | — |
| `ToiletShadewood` 暗影木马桶 | 4105 | 497 | Toilets | — |
| `ToiletLihzhard` 丛林蜥蜴马桶 | 4106 | 497 | Toilets | — |
| `ToiletDungeonBlue` 蓝地牢马桶 | 4107 | 497 | Toilets | — |
| `ToiletDungeonGreen` 绿地牢马桶 | 4108 | 497 | Toilets | — |
| `ToiletDungeonPink` 粉地牢马桶 | 4109 | 497 | Toilets | — |
| `ToiletObsidian` 黑曜石马桶 | 4110 | 497 | Toilets | — |
| `ToiletFrozen` 冰冻马桶 | 4111 | 497 | Toilets | — |
| `ToiletGlass` 玻璃马桶 | 4112 | 497 | Toilets | — |
| `ToiletHoney` 蜂蜜马桶 | 4113 | 497 | Toilets | — |
| `ToiletSteampunk` 蒸汽朋克马桶 | 4114 | 497 | Toilets | — |
| `ToiletPumpkin` 南瓜马桶 | 4115 | 497 | Toilets | — |
| `ToiletSpooky` 阴森马桶 | 4116 | 497 | Toilets | — |
| `ToiletDynasty` 王朝马桶 | 4117 | 497 | Toilets | — |
| `ToiletPalm` 棕榈木马桶 | 4118 | 497 | Toilets | — |
| `ToiletBoreal` 针叶木马桶 | 4119 | 497 | Toilets | — |
| `ToiletSlime` 史莱姆马桶 | 4120 | 497 | Toilets | — |
| `ToiletMartian` 火星马桶 | 4121 | 497 | Toilets | — |
| `ToiletGranite` 花岗岩马桶 | 4122 | 497 | Toilets | — |
| `ToiletMarble` 大理石马桶 | 4123 | 497 | Toilets | — |
| `ToiletCrystal` 水晶马桶 | 4124 | 497 | Toilets | — |
| `ToiletSpider` 蜘蛛马桶 | 4125 | 497 | Toilets | — |
| `ToiletLesion` 损伤马桶 | 4126 | 497 | Toilets | — |
| `ToiletDiamond` 钻石马桶 | 4127 | 497 | Toilets | — |
| `SpiderBlock` 蜘蛛窝块 | 4139 | 498 | Spider | — |
| `SpiderWall` 蜘蛛窝墙 | 4140 | — | — | 236 |
| `ToiletMeteor` 流星马桶 | 4141 | 497 | Toilets | — |
| `LesionStation` 衰变室 | 4142 | 499 | LesionStation | — |
| `SolarBathtub` 日耀浴缸 | 4145 | 90 | Bathtubs | — |
| `SolarBed` 日耀床 | 4146 | 79 | Beds | — |
| `SolarBookcase` 日耀书架 | 4147 | 101 | Bookcases | — |
| `SolarDresser` 日耀梳妆台 | 4148 | 88 | Dressers | — |
| `SolarCandelabra` 日耀烛台 | 4149 | 100 | Candelabras | — |
| `SolarCandle` 日耀蜡烛 | 4150 | 33 | Candles | — |
| `SolarChair` 日耀椅 | 4151 | 15 | Chairs | — |
| `SolarChandelier` 日耀吊灯 | 4152 | 34 | Chandeliers | — |
| `SolarChest` 日耀箱 | 4153 | 467 | Containers2 | — |
| `SolarClock` 日耀时钟 | 4154 | 104 | GrandfatherClocks | — |
| `SolarDoor` 日耀门 | 4155 | 10 | ClosedDoor | — |
| `SolarLamp` 日耀灯 | 4156 | 93 | Lamps | — |
| `SolarLantern` 日耀灯笼 | 4157 | 42 | HangingLanterns | — |
| `SolarPiano` 日耀钢琴 | 4158 | 87 | Pianos | — |
| `SolarPlatform` 日耀平台 | 4159 | 19 | Platforms | — |
| `SolarSink` 日耀水槽 | 4160 | 172 | Sinks | — |
| `SolarSofa` 日耀沙发 | 4161 | 89 | Benches | — |
| `SolarTable` 日耀桌 | 4162 | 469 | Tables2 | — |
| `SolarWorkbench` 日耀工作台 | 4163 | 18 | WorkBenches | — |
| `Fake_SolarChest` 受困日耀箱 | 4164 | 468 | FakeContainers2 | — |
| `SolarToilet` 日耀马桶 | 4165 | 497 | Toilets | — |
| `VortexBathtub` 星旋浴缸 | 4166 | 90 | Bathtubs | — |
| `VortexBed` 星旋床 | 4167 | 79 | Beds | — |
| `VortexBookcase` 星旋书架 | 4168 | 101 | Bookcases | — |
| `VortexDresser` 星旋梳妆台 | 4169 | 88 | Dressers | — |
| `VortexCandelabra` 星旋烛台 | 4170 | 100 | Candelabras | — |
| `VortexCandle` 星旋蜡烛 | 4171 | 33 | Candles | — |
| `VortexChair` 星旋椅 | 4172 | 15 | Chairs | — |
| `VortexChandelier` 星旋吊灯 | 4173 | 34 | Chandeliers | — |
| `VortexChest` 星旋箱 | 4174 | 467 | Containers2 | — |
| `VortexClock` 星旋时钟 | 4175 | 104 | GrandfatherClocks | — |
| `VortexDoor` 星旋门 | 4176 | 10 | ClosedDoor | — |
| `VortexLamp` 星旋灯 | 4177 | 93 | Lamps | — |
| `VortexLantern` 星旋灯笼 | 4178 | 42 | HangingLanterns | — |
| `VortexPiano` 星旋钢琴 | 4179 | 87 | Pianos | — |
| `VortexPlatform` 星旋平台 | 4180 | 19 | Platforms | — |
| `VortexSink` 星旋水槽 | 4181 | 172 | Sinks | — |
| `VortexSofa` 星旋沙发 | 4182 | 89 | Benches | — |
| `VortexTable` 星旋桌 | 4183 | 469 | Tables2 | — |
| `VortexWorkbench` 星旋工作台 | 4184 | 18 | WorkBenches | — |
| `Fake_VortexChest` 受困星旋箱 | 4185 | 468 | FakeContainers2 | — |
| `VortexToilet` 星旋马桶 | 4186 | 497 | Toilets | — |
| `NebulaBathtub` 星云浴缸 | 4187 | 90 | Bathtubs | — |
| `NebulaBed` 星云床 | 4188 | 79 | Beds | — |
| `NebulaBookcase` 星云书架 | 4189 | 101 | Bookcases | — |
| `NebulaDresser` 星云梳妆台 | 4190 | 88 | Dressers | — |
| `NebulaCandelabra` 星云烛台 | 4191 | 100 | Candelabras | — |
| `NebulaCandle` 星云蜡烛 | 4192 | 33 | Candles | — |
| `NebulaChair` 星云椅 | 4193 | 15 | Chairs | — |
| `NebulaChandelier` 星云吊灯 | 4194 | 34 | Chandeliers | — |
| `NebulaChest` 星云箱 | 4195 | 467 | Containers2 | — |
| `NebulaClock` 星云时钟 | 4196 | 104 | GrandfatherClocks | — |
| `NebulaDoor` 星云门 | 4197 | 10 | ClosedDoor | — |
| `NebulaLamp` 星云灯 | 4198 | 93 | Lamps | — |
| `NebulaLantern` 星云灯笼 | 4199 | 42 | HangingLanterns | — |
| `NebulaPiano` 星云钢琴 | 4200 | 87 | Pianos | — |
| `NebulaPlatform` 星云平台 | 4201 | 19 | Platforms | — |
| `NebulaSink` 星云水槽 | 4202 | 172 | Sinks | — |
| `NebulaSofa` 星云沙发 | 4203 | 89 | Benches | — |
| `NebulaTable` 星云桌 | 4204 | 469 | Tables2 | — |
| `NebulaWorkbench` 星云工作台 | 4205 | 18 | WorkBenches | — |
| `Fake_NebulaChest` 受困星云箱 | 4206 | 468 | FakeContainers2 | — |
| `NebulaToilet` 星云马桶 | 4207 | 497 | Toilets | — |
| `StardustBathtub` 星尘浴缸 | 4208 | 90 | Bathtubs | — |
| `StardustBed` 星尘床 | 4209 | 79 | Beds | — |
| `StardustBookcase` 星尘书架 | 4210 | 101 | Bookcases | — |
| `StardustDresser` 星尘梳妆台 | 4211 | 88 | Dressers | — |
| `StardustCandelabra` 星尘烛台 | 4212 | 100 | Candelabras | — |
| `StardustCandle` 星尘蜡烛 | 4213 | 33 | Candles | — |
| `StardustChair` 星尘椅 | 4214 | 15 | Chairs | — |
| `StardustChandelier` 星尘吊灯 | 4215 | 34 | Chandeliers | — |
| `StardustChest` 星尘箱 | 4216 | 467 | Containers2 | — |
| `StardustClock` 星尘时钟 | 4217 | 104 | GrandfatherClocks | — |
| `StardustDoor` 星尘门 | 4218 | 10 | ClosedDoor | — |
| `StardustLamp` 星尘灯 | 4219 | 93 | Lamps | — |
| `StardustLantern` 星尘灯笼 | 4220 | 42 | HangingLanterns | — |
| `StardustPiano` 星尘钢琴 | 4221 | 87 | Pianos | — |
| `StardustPlatform` 星尘平台 | 4222 | 19 | Platforms | — |
| `StardustSink` 星尘水槽 | 4223 | 172 | Sinks | — |
| `StardustSofa` 星尘沙发 | 4224 | 89 | Benches | — |
| `StardustTable` 星尘桌 | 4225 | 469 | Tables2 | — |
| `StardustWorkbench` 星尘工作台 | 4226 | 18 | WorkBenches | — |
| `Fake_StardustChest` 受困星尘箱 | 4227 | 468 | FakeContainers2 | — |
| `StardustToilet` 星尘马桶 | 4228 | 497 | Toilets | — |
| `SolarBrick` 日耀砖 | 4229 | 500 | SolarBrick | — |
| `VortexBrick` 星旋砖 | 4230 | 501 | VortexBrick | — |
| `NebulaBrick` 星云砖 | 4231 | 502 | NebulaBrick | — |
| `StardustBrick` 星尘砖 | 4232 | 503 | StardustBrick | — |
| `SolarBrickWall` 日耀砖墙 | 4233 | — | — | 237 |
| `VortexBrickWall` 星旋砖墙 | 4234 | — | — | 238 |
| `NebulaBrickWall` 星云砖墙 | 4235 | — | — | 239 |
| `StardustBrickWall` 星尘砖墙 | 4236 | — | — | 240 |
| `MusicBoxDayRemix` 八音盒（日间混合曲） | 4237 | 139 | MusicBoxes | — |
| `CrackedBlueBrick` 破裂蓝砖 | 4238 | 481 | CrackedBlueDungeonBrick | — |
| `CrackedGreenBrick` 破裂绿砖 | 4239 | 482 | CrackedGreenDungeonBrick | — |
| `CrackedPinkBrick` 破裂粉砖 | 4240 | 483 | CrackedPinkDungeonBrick | — |
| `FlowerPacketWild` 野花种子 | 4241 | 3 | Plants | — |
| `OrangeStainedGlass` 橙花窗玻璃 | 4260 | — | — | 241 |
| `OrangePressurePlate` 橙压力板 | 4261 | 135 | PressurePlates | — |
| `GolfChest` 高尔夫宝箱 | 4265 | 467 | Containers2 | — |
| `Fake_GolfChest` 受困高尔夫宝箱 | 4266 | 468 | FakeContainers2 | — |
| `DesertChest` 沙岩宝箱 | 4267 | 467 | Containers2 | — |
| `Fake_DesertChest` 受困沙岩箱 | 4268 | 468 | FakeContainers2 | — |
| `GoldGoldfishBowl` 金鱼缸 | 4275 | 505 | GoldGoldfishBowl | — |
| `CatBast` 韧皮雕像 | 4276 | 506 | CatBast | — |
| `GoldStarryGlassBlock` 金星块 | 4277 | 507 | GoldStarryGlassBlock | — |
| `BlueStarryGlassBlock` 蓝星块 | 4278 | 508 | BlueStarryGlassBlock | — |
| `GoldStarryGlassWall` 金星墙 | 4279 | — | — | 242 |
| `BlueStarryGlassWall` 蓝星墙 | 4280 | — | — | 243 |
| `SandstoneBathtub` 沙岩浴缸 | 4298 | 90 | Bathtubs | — |
| `SandstoneBed` 沙岩床 | 4299 | 79 | Beds | — |
| `SandstoneBookcase` 沙岩书架 | 4300 | 101 | Bookcases | — |
| `SandstoneDresser` 沙岩梳妆台 | 4301 | 88 | Dressers | — |
| `SandstoneCandelabra` 沙岩烛台 | 4302 | 100 | Candelabras | — |
| `SandstoneCandle` 沙岩蜡烛 | 4303 | 33 | Candles | — |
| `SandstoneChair` 沙岩椅 | 4304 | 15 | Chairs | — |
| `SandstoneChandelier` 沙岩吊灯 | 4305 | 34 | Chandeliers | — |
| `SandstoneClock` 沙岩时钟 | 4306 | 104 | GrandfatherClocks | — |
| `SandstoneDoor` 沙岩门 | 4307 | 10 | ClosedDoor | — |
| `SandstoneLamp` 沙岩灯 | 4308 | 93 | Lamps | — |
| `SandstoneLantern` 沙岩灯笼 | 4309 | 42 | HangingLanterns | — |
| `SandstonePiano` 沙岩钢琴 | 4310 | 87 | Pianos | — |
| `SandstonePlatform` 沙岩平台 | 4311 | 19 | Platforms | — |
| `SandstoneSink` 沙岩水槽 | 4312 | 172 | Sinks | — |
| `SandstoneSofa` 沙岩沙发 | 4313 | 89 | Benches | — |
| `SandstoneTable` 沙岩桌 | 4314 | 469 | Tables2 | — |
| `SandstoneWorkbench` 沙岩工作台 | 4315 | 18 | WorkBenches | — |
| `SandstoneToilet` 沙岩马桶 | 4316 | 497 | Toilets | — |
| `VoidMonolith` 虚空巨石 | 4318 | 509 | VoidMonolith | — |
| `ArrowSign` 箭头符号 | 4319 | 510 | ArrowSign | — |
| `PaintedArrowSign` 彩色箭头符号 | 4320 | 511 | PaintedArrowSign | — |
| `FoodPlatter` 盘子 | 4326 | 520 | FoodPlatter | — |
| `BlackDragonflyJar` 黑蜻蜓罐 | 4327 | 521 | BlackDragonflyJar | — |
| `BlueDragonflyJar` 蓝蜻蜓罐 | 4328 | 522 | BlueDragonflyJar | — |
| `GreenDragonflyJar` 绿蜻蜓罐 | 4329 | 523 | GreenDragonflyJar | — |
| `OrangeDragonflyJar` 橙蜻蜓罐 | 4330 | 524 | OrangeDragonflyJar | — |
| `RedDragonflyJar` 红蜻蜓罐 | 4331 | 525 | RedDragonflyJar | — |
| `YellowDragonflyJar` 黄蜻蜓罐 | 4332 | 526 | YellowDragonflyJar | — |
| `GoldDragonflyJar` 金蜻蜓罐 | 4333 | 527 | GoldDragonflyJar | — |
| `DragonflyStatue` 蜻蜓雕像 | 4342 | 105 | Statues | — |
| `GreenMoss` 绿苔藓 | 4349 | 179 | GreenMoss | — |
| `BrownMoss` 棕苔藓 | 4350 | 180 | BrownMoss | — |
| `RedMoss` 红苔藓 | 4351 | 181 | RedMoss | — |
| `BlueMoss` 蓝苔藓 | 4352 | 182 | BlueMoss | — |
| `PurpleMoss` 紫苔藓 | 4353 | 183 | PurpleMoss | — |
| `LavaMoss` 熔岩苔藓 | 4354 | 381 | LavaMoss | — |
| `BoulderStatue` 巨石雕像 | 4355 | 531 | BoulderStatue | — |
| `MusicBoxTitleAlt` 八音盒（旅程开始） | 4356 | 139 | MusicBoxes | — |
| `MusicBoxStorm` 八音盒（暴风雨） | 4357 | 139 | MusicBoxes | — |
| `MusicBoxGraveyard` 八音盒（墓地） | 4358 | 139 | MusicBoxes | — |
| `SeagullStatue` 海鸥雕像 | 4360 | 105 | Statues | — |
| `MaggotCage` 蝇蛆笼 | 4364 | 532 | MaggotCage | — |
| `RatCage` 大鼠笼 | 4376 | 533 | RatCage | — |
| `KryptonMoss` 氪苔藓 | 4377 | 534 | KryptonMoss | — |
| `XenonMoss` 氙苔藓 | 4378 | 536 | XenonMoss | — |
| `LadybugCage` 瓢虫笼 | 4380 | 538 | LadybugCage | — |
| `DesertTorch` 沙漠火把 | 4383 | 4 | Torches | — |
| `CoralTorch` 珊瑚火把 | 4384 | 4 | Torches | — |
| `CorruptTorch` 腐化火把 | 4385 | 4 | Torches | — |
| `CrimsonTorch` 猩红火把 | 4386 | 4 | Torches | — |
| `HallowedTorch` 神圣火把 | 4387 | 4 | Torches | — |
| `JungleTorch` 丛林火把 | 4388 | 4 | Torches | — |
| `ArgonMoss` 氩苔藓 | 4389 | 539 | ArgonMoss | — |
| `RollingCactus` 仙人球 | 4390 | 484 | RollingCactus | — |
| `ThinIce` 薄冰 | 4391 | 162 | BreakableIce | — |
| `EchoBlock` 回声块 | 4392 | 541 | EchoBlock | — |
| `OwlCage` 猫头鹰笼 | 4396 | 542 | OwlCage | — |
| `OwlStatue` 猫头鹰雕像 | 4397 | 105 | Statues | — |
| `PupfishBowl` 鳉鱼缸 | 4398 | 543 | PupfishBowl | — |
| `GoldLadybugCage` 金瓢虫笼 | 4399 | 544 | GoldLadybugCage | — |
| `FrozenCrate` 冰冻匣 | 4405 | 376 | FishingCrate | — |
| `FrozenCrateHard` 针叶木匣 | 4406 | 376 | FishingCrate | — |
| `OasisCrate` 绿洲匣 | 4407 | 376 | FishingCrate | — |
| `OasisCrateHard` 幻象匣 | 4408 | 376 | FishingCrate | — |
| `StoneDoor` 石门 | 4415 | 10 | ClosedDoor | — |
| `StonePlatform` 石平台 | 4416 | 19 | Platforms | — |
| `OasisFountain` 绿洲喷泉 | 4417 | 207 | WaterFountain | — |
| `LawnFlamingo` 草坪火烈鸟 | 4420 | 545 | LawnFlamingo | — |
| `MusicBoxUndergroundJungle` 八音盒（地下丛林） | 4421 | 139 | MusicBoxes | — |
| `Grate` 格栅 | 4422 | 546 | Grate | — |
| `WroughtIronFence` 锻铁栅栏 | 4424 | — | — | 245 |
| `PottedForestCedar` 盆栽森林雪松 | 4430 | 547 | PottedPlants1 | — |
| `PottedJungleCedar` 盆栽丛林雪松 | 4431 | 547 | PottedPlants1 | — |
| `PottedHallowCedar` 盆栽神圣雪松 | 4432 | 547 | PottedPlants1 | — |
| `PottedForestTree` 盆栽林木 | 4433 | 547 | PottedPlants1 | — |
| `PottedJungleTree` 盆栽丛林树 | 4434 | 547 | PottedPlants1 | — |
| `PottedHallowTree` 盆栽神圣树 | 4435 | 548 | PottedPlants2 | — |
| `PottedForestPalm` 盆栽森林棕榈 | 4436 | 548 | PottedPlants2 | — |
| `PottedJunglePalm` 盆栽丛林棕榈 | 4437 | 548 | PottedPlants2 | — |
| `PottedHallowPalm` 盆栽神圣棕榈 | 4438 | 548 | PottedPlants2 | — |
| `PottedForestBamboo` 盆栽森林竹 | 4439 | 548 | PottedPlants2 | — |
| `PottedJungleBamboo` 盆栽丛林竹 | 4440 | 548 | PottedPlants2 | — |
| `PottedHallowBamboo` 盆栽神圣竹 | 4441 | 548 | PottedPlants2 | — |
| `SandcastleBucket` 沙堡桶 | 4460 | 552 | Sandcastles | — |
| `TurtleCage` 龟笼 | 4461 | 550 | TurtleCage | — |
| `TurtleJungleCage` 丛林龟笼 | 4462 | 551 | TurtleJungleCage | — |
| `TurtleStatue` 龟雕像 | 4466 | 105 | Statues | — |
| `GrebeCage` 䴙䴘笼 | 4473 | 553 | GrebeCage | — |
| `SeagullCage` 海鸥笼 | 4474 | 554 | SeagullCage | — |
| `WaterStriderCage` 水黾笼 | 4475 | 555 | WaterStriderCage | — |
| `GoldWaterStriderCage` 金水黾笼 | 4476 | 556 | GoldWaterStriderCage | — |
| `SeahorseCage` 海马笼 | 4481 | 558 | SeahorseCage | — |
| `GoldSeahorseCage` 金海马笼 | 4483 | 559 | GoldSeahorseCage | — |
| `TimerOneHalfSecond` 1/2秒计时器 | 4484 | 144 | Timers | — |
| `TimerOneFourthSecond` 1/4秒计时器 | 4485 | 144 | Timers | — |
| `EbonstoneEcho` 黑檀石墙 | 4486 | — | — | 246 |
| `MudWallEcho` 泥墙 | 4487 | — | — | 247 |
| `PearlstoneEcho` 珍珠石墙 | 4488 | — | — | 248 |
| `SnowWallEcho` 雪墙 | 4489 | — | — | 249 |
| `AmethystEcho` 紫晶石墙 | 4490 | — | — | 250 |
| `TopazEcho` 黄玉石墙 | 4491 | — | — | 251 |
| `SapphireEcho` 蓝玉石墙 | 4492 | — | — | 252 |
| `EmeraldEcho` 翡翠石墙 | 4493 | — | — | 253 |
| `RubyEcho` 红玉石墙 | 4494 | — | — | 254 |
| `DiamondEcho` 钻石石墙 | 4495 | — | — | 255 |
| `Cave1Echo` 绿苔藓墙 | 4496 | — | — | 256 |
| `Cave2Echo` 棕苔藓墙 | 4497 | — | — | 257 |
| `Cave3Echo` 红苔藓墙 | 4498 | — | — | 258 |
| `Cave4Echo` 蓝苔藓墙 | 4499 | — | — | 259 |
| `Cave5Echo` 紫苔藓墙 | 4500 | — | — | 260 |
| `Cave6Echo` 岩石土墙 | 4501 | — | — | 261 |
| `Cave7Echo` 古老石墙 | 4502 | — | — | 262 |
| `SpiderEcho` 蜘蛛墙 | 4503 | — | — | 263 |
| `CorruptGrassEcho` 腐化草墙 | 4504 | — | — | 264 |
| `HallowedGrassEcho` 神圣草墙 | 4505 | — | — | 265 |
| `IceEcho` 冰雪墙 | 4506 | — | — | 266 |
| `ObsidianBackEcho` 黑曜石墙 | 4507 | — | — | 267 |
| `CrimsonGrassEcho` 猩红草墙 | 4508 | — | — | 268 |
| `CrimstoneEcho` 猩红石墙 | 4509 | — | — | 269 |
| `CaveWall1Echo` 洞穴土墙 | 4510 | — | — | 270 |
| `CaveWall2Echo` 粗糙土墙 | 4511 | — | — | 271 |
| `Cave8Echo` 陡峭石墙 | 4512 | — | — | 274 |
| `Corruption1Echo` 腐化生长墙 | 4513 | — | — | 276 |
| `Corruption2Echo` 腐化弥撒墙 | 4514 | — | — | 277 |
| `Corruption3Echo` 腐化脓疱墙 | 4515 | — | — | 278 |
| `Corruption4Echo` 腐化卷须墙 | 4516 | — | — | 279 |
| `Crimson1Echo` 猩红硬皮墙 | 4517 | — | — | 280 |
| `Crimson2Echo` 猩红痂墙 | 4518 | — | — | 281 |
| `Crimson3Echo` 猩红牙墙 | 4519 | — | — | 282 |
| `Crimson4Echo` 猩红水疱墙 | 4520 | — | — | 283 |
| `Dirt1Echo` 分层土墙 | 4521 | — | — | 284 |
| `Dirt2Echo` 崩塌土墙 | 4522 | — | — | 285 |
| `Dirt3Echo` 破裂土墙 | 4523 | — | — | 286 |
| `Dirt4Echo` 波动土墙 | 4524 | — | — | 287 |
| `Hallow1Echo` 神圣棱镜墙 | 4525 | — | — | 288 |
| `Hallow2Echo` 神圣洞穴墙 | 4526 | — | — | 289 |
| `Hallow3Echo` 神圣碎片墙 | 4527 | — | — | 290 |
| `Hallow4Echo` 神圣水晶墙 | 4528 | — | — | 291 |
| `Jungle1Echo` 地衣石墙 | 4529 | — | — | 292 |
| `Jungle2Echo` 多叶丛林墙 | 4530 | — | — | 293 |
| `Jungle3Echo` 常春藤石墙 | 4531 | — | — | 294 |
| `Jungle4Echo` 丛林藤蔓墙 | 4532 | — | — | 295 |
| `Lava1Echo` 余烬墙 | 4533 | — | — | 296 |
| `Lava2Echo` 煤渣墙 | 4534 | — | — | 297 |
| `Lava3Echo` 岩浆墙 | 4535 | — | — | 298 |
| `Lava4Echo` 阴燃石墙 | 4536 | — | — | 299 |
| `Rocks1Echo` 磨损石墙 | 4537 | — | — | 300 |
| `Rocks2Echo` 钟乳石墙 | 4538 | — | — | 301 |
| `Rocks3Echo` 斑驳石墙 | 4539 | — | — | 302 |
| `Rocks4Echo` 断裂石墙 | 4540 | — | — | 303 |
| `TheBrideBanner` 新娘旗 | 4541 | 91 | Banners | — |
| `ZombieMermanBanner` 僵尸人鱼旗 | 4542 | 91 | Banners | — |
| `EyeballFlyingFishBanner` 游荡眼球怪鱼旗 | 4543 | 91 | Banners | — |
| `BloodSquidBanner` 血乌贼旗 | 4544 | 91 | Banners | — |
| `BloodEelBanner` 血鳗鱼旗 | 4545 | 91 | Banners | — |
| `GoblinSharkBanner` 血浆哥布林鲨鱼旗 | 4546 | 91 | Banners | — |
| `LargeBambooBlock` 大竹子 | 4547 | 563 | LargeBambooBlock | — |
| `LargeBambooBlockWall` 大竹墙 | 4548 | — | — | 313 |
| `FogMachine` 烟雾机 | 4552 | 565 | FogMachine | — |
| `PlasmaLamp` 等离子灯 | 4553 | 564 | PlasmaLamp | — |
| `MarbleColumn` 大理石柱 | 4554 | 561 | MarbleColumn | — |
| `BambooBlock` 竹子 | 4564 | 562 | BambooBlock | — |
| `BambooBlockWall` 竹墙 | 4565 | — | — | 312 |
| `BambooBathtub` 竹浴缸 | 4566 | 90 | Bathtubs | — |
| `BambooBed` 竹床 | 4567 | 79 | Beds | — |
| `BambooBookcase` 竹书架 | 4568 | 101 | Bookcases | — |
| `BambooDresser` 竹梳妆台 | 4569 | 88 | Dressers | — |
| `BambooCandelabra` 竹烛台 | 4570 | 100 | Candelabras | — |
| `BambooCandle` 竹蜡烛 | 4571 | 33 | Candles | — |
| `BambooChair` 竹椅 | 4572 | 15 | Chairs | — |
| `BambooChandelier` 竹吊灯 | 4573 | 34 | Chandeliers | — |
| `BambooChest` 竹宝箱 | 4574 | 467 | Containers2 | — |
| `BambooClock` 竹时钟 | 4575 | 104 | GrandfatherClocks | — |
| `BambooDoor` 竹门 | 4576 | 10 | ClosedDoor | — |
| `BambooLamp` 竹灯 | 4577 | 93 | Lamps | — |
| `BambooLantern` 竹灯笼 | 4578 | 42 | HangingLanterns | — |
| `BambooPiano` 竹钢琴 | 4579 | 87 | Pianos | — |
| `BambooPlatform` 竹平台 | 4580 | 19 | Platforms | — |
| `BambooSink` 竹水槽 | 4581 | 172 | Sinks | — |
| `BambooSofa` 竹沙发 | 4582 | 89 | Benches | — |
| `BambooTable` 竹桌 | 4583 | 469 | Tables2 | — |
| `BambooWorkbench` 竹工作台 | 4584 | 18 | WorkBenches | — |
| `Fake_BambooChest` 受困竹宝箱 | 4585 | 468 | FakeContainers2 | — |
| `BambooToilet` 竹马桶 | 4586 | 497 | Toilets | — |
| `GolfTrophyBronze` 高尔夫青铜纪念章 | 4599 | 560 | GolfTrophies | — |
| `GolfTrophySilver` 高尔夫银纪念章 | 4600 | 560 | GolfTrophies | — |
| `GolfTrophyGold` 高尔夫金纪念章 | 4601 | 560 | GolfTrophies | — |
| `BloodNautilusBanner` 恐惧鹦鹉螺旗 | 4602 | 91 | Banners | — |
| `MusicBoxJungleNight` 八音盒（夜间丛林） | 4606 | 139 | MusicBoxes | — |
| `GardenGnome` 花园侏儒 | 4609 | 567 | GardenGnome | — |
| `AndrewSphinx` 安德鲁狮身人面像 | 4626 | 240 | Painting3X3 | — |
| `WatchfulAntlion` 警惕的蚁狮 | 4627 | 240 | Painting3X3 | — |
| `BurningSpirit` 烈焰之魂 | 4628 | 240 | Painting3X3 | — |
| `JawsOfDeath` 鬼门关 | 4629 | 240 | Painting3X3 | — |
| `TheSandsOfSlime` 史莱姆沙漠 | 4630 | 240 | Painting3X3 | — |
| `SnakesIHateSnakes` 蛇，我讨厌蛇 | 4631 | 240 | Painting3X3 | — |
| `LifeAboveTheSand` 沙漠上的生活 | 4632 | 242 | Painting6X4 | — |
| `Oasis` 绿洲 | 4633 | 242 | Painting6X4 | — |
| `PrehistoryPreserved` 史前果酱 | 4634 | 242 | Painting6X4 | — |
| `AncientTablet` 远古碑牌 | 4635 | 242 | Painting6X4 | — |
| `Uluru` 乌卢鲁 | 4636 | 242 | Painting6X4 | — |
| `VisitingThePyramids` 参观金字塔 | 4637 | 242 | Painting6X4 | — |
| `BandageBoy` 绷带男孩 | 4638 | 245 | Painting2X3 | — |
| `DivineEye` 天赐之眼 | 4639 | 245 | Painting2X3 | — |
| `AmethystStoneBlock` 紫晶石块 | 4640 | 67 | Amethyst | — |
| `TopazStoneBlock` 黄玉石块 | 4641 | 66 | Topaz | — |
| `SapphireStoneBlock` 蓝玉石块 | 4642 | 63 | Sapphire | — |
| `EmeraldStoneBlock` 翡翠石块 | 4643 | 65 | Emerald | — |
| `RubyStoneBlock` 红玉石块 | 4644 | 64 | Ruby | — |
| `DiamondStoneBlock` 钻石石块 | 4645 | 68 | Diamond | — |
| `AmberStoneBlock` 琥珀石块 | 4646 | 566 | AmberStoneBlock | — |
| `AmberStoneWallEcho` 琥珀石墙 | 4647 | — | — | 314 |
| `PinkFairyJar` 粉仙灵罐 | 4655 | 568 | PinkFairyJar | — |
| `GreenFairyJar` 绿仙灵罐 | 4656 | 569 | GreenFairyJar | — |
| `BlueFairyJar` 蓝仙灵罐 | 4657 | 570 | BlueFairyJar | — |
| `GolfPainting1` 绵延起伏的绿地 | 4658 | 242 | Painting6X4 | — |
| `GolfPainting2` 静止球研究 | 4659 | 245 | Painting2X3 | — |
| `GolfPainting3` 躲开！ | 4660 | 240 | Painting3X3 | — |
| `GolfPainting4` 反射的假象 | 4661 | 246 | Painting3X2 | — |
| `BambooFence` 竹栅栏 | 4667 | — | — | 315 |
| `DandelionBanner` 愤怒蒲公英旗 | 4687 | 91 | Banners | — |
| `GnomeBanner` 侏儒旗 | 4688 | 91 | Banners | — |
| `DesertCampfire` 沙漠篝火 | 4689 | 215 | Campfire | — |
| `CoralCampfire` 珊瑚篝火 | 4690 | 215 | Campfire | — |
| `CorruptCampfire` 腐化篝火 | 4691 | 215 | Campfire | — |
| `CrimsonCampfire` 猩红篝火 | 4692 | 215 | Campfire | — |
| `HallowedCampfire` 神圣篝火 | 4693 | 215 | Campfire | — |
| `JungleCampfire` 丛林篝火 | 4694 | 215 | Campfire | — |
| `SoulBottleLight` 瓶中光明之魂 | 4695 | 572 | SoulBottles | — |
| `SoulBottleNight` 瓶中暗影之魂 | 4696 | 572 | SoulBottles | — |
| `SoulBottleFlight` 瓶中飞翔之魂 | 4697 | 572 | SoulBottles | — |
| `SoulBottleSight` 瓶中视域之魂 | 4698 | 572 | SoulBottles | — |
| `SoulBottleMight` 瓶中力量之魂 | 4699 | 572 | SoulBottles | — |
| `SoulBottleFright` 瓶中恐惧之魂 | 4700 | 572 | SoulBottles | — |
| `TatteredWoodSign` 破旧木制标牌 | 4710 | 573 | TatteredWoodSign | — |
| `DungeonDesertChest` 沙漠箱 | 4712 | 467 | Containers2 | — |
| `Fake_DungeonDesertChest` 受困沙漠箱 | 4713 | 468 | FakeContainers2 | — |
| `BorealBeam` 针叶木梁 | 4717 | 574 | BorealBeam | — |
| `RichMahoganyBeam` 红木梁 | 4718 | 575 | RichMahoganyBeam | — |
| `GraniteColumn` 花岗岩柱 | 4719 | 576 | GraniteColumn | — |
| `SandstoneColumn` 沙岩柱 | 4720 | 577 | SandstoneColumn | — |
| `MushroomBeam` 蘑菇梁 | 4721 | 578 | MushroomBeam | — |
| `Nevermore` 影魔 | 4723 | 240 | Painting3X3 | — |
| `Reborn` 重生 | 4724 | 240 | Painting3X3 | — |
| `Graveyard` 墓地 | 4725 | 242 | Painting6X4 | — |
| `GhostManifestation` 鬼魂显现 | 4726 | 245 | Painting2X3 | — |
| `WickedUndead` 邪恶亡灵 | 4727 | 245 | Painting2X3 | — |
| `BloodyGoblet` 血腥酒杯 | 4728 | 245 | Painting2X3 | — |
| `StillLife` 静物画 | 4729 | 246 | Painting3X2 | — |
| `TerraToilet` 泰拉马桶 | 4731 | 497 | Toilets | — |
| `RockGolemHead` 岩石巨人头 | 4761 | 579 | RockGolemHead | — |
| `FairyQueenTrophy` 光之女皇纪念章 | 4783 | 240 | Painting3X3 | — |
| `HellButterflyJar` 地狱蝴蝶罐 | 4846 | 580 | HellButterflyJar | — |
| `LavaflyinaBottle` 瓶中熔岩蝇 | 4848 | 581 | LavaflyinaBottle | — |
| `MagmaSnailCage` 岩浆蜗牛笼 | 4850 | 582 | MagmaSnailCage | — |
| `GemTreeTopazSeed` 黄玉宝石果 | 4851 | 590 | GemSaplings | — |
| `GemTreeAmethystSeed` 紫晶宝石果 | 4852 | 590 | GemSaplings | — |
| `GemTreeSapphireSeed` 蓝玉宝石果 | 4853 | 590 | GemSaplings | — |
| `GemTreeEmeraldSeed` 翡翠宝石果 | 4854 | 590 | GemSaplings | — |
| `GemTreeRubySeed` 红玉宝石果 | 4855 | 590 | GemSaplings | — |
| `GemTreeDiamondSeed` 钻石宝石果 | 4856 | 590 | GemSaplings | — |
| `GemTreeAmberSeed` 琥珀宝石果 | 4857 | 590 | GemSaplings | — |
| `PotSuspended` 吊挂盆 | 4858 | 591 | PotsSuspended | — |
| `PotSuspendedDaybloom` 吊挂太阳花 | 4859 | 591 | PotsSuspended | — |
| `PotSuspendedMoonglow` 吊挂月光草 | 4860 | 591 | PotsSuspended | — |
| `PotSuspendedWaterleaf` 吊挂幌菊 | 4861 | 591 | PotsSuspended | — |
| `PotSuspendedShiverthorn` 吊挂寒颤棘 | 4862 | 591 | PotsSuspended | — |
| `PotSuspendedBlinkroot` 吊挂闪耀根 | 4863 | 591 | PotsSuspended | — |
| `PotSuspendedDeathweedCorrupt` 吊挂腐化死亡草 | 4864 | 591 | PotsSuspended | — |
| `PotSuspendedDeathweedCrimson` 吊挂猩红死亡草 | 4865 | 591 | PotsSuspended | — |
| `PotSuspendedFireblossom` 吊挂火舌花 | 4866 | 591 | PotsSuspended | — |
| `BrazierSuspended` 吊挂火盆 | 4867 | 592 | BrazierSuspended | — |
| `VolcanoSmall` 小火山 | 4868 | 593 | VolcanoSmall | — |
| `VolcanoLarge` 大火山 | 4869 | 594 | VolcanoLarge | — |
| `VanityTreeSakuraSeed` 樱花树苗 | 4871 | 595 | VanityTreeSakuraSaplings | — |
| `TeleportationPylonJungle` 丛林晶塔 | 4875 | 597 | TeleportationPylon | — |
| `TeleportationPylonPurity` 森林晶塔 | 4876 | 597 | TeleportationPylon | — |
| `LavaCrate` 黑曜石匣 | 4877 | 376 | FishingCrate | — |
| `LavaCrateHard` 狱石匣 | 4878 | 376 | FishingCrate | — |
| `LavaFishbowl` 熔岩蛇缸 | 4880 | 598 | LavafishBowl | — |
| `AmethystBunnyCage` 紫晶兔笼 | 4882 | 599 | AmethystBunnyCage | — |
| `TopazBunnyCage` 黄玉兔笼 | 4883 | 600 | TopazBunnyCage | — |
| `SapphireBunnyCage` 蓝玉兔笼 | 4884 | 601 | SapphireBunnyCage | — |
| `EmeraldBunnyCage` 翡翠兔笼 | 4885 | 602 | EmeraldBunnyCage | — |
| `RubyBunnyCage` 红玉兔笼 | 4886 | 603 | RubyBunnyCage | — |
| `DiamondBunnyCage` 钻石兔笼 | 4887 | 604 | DiamondBunnyCage | — |
| `AmberBunnyCage` 琥珀兔笼 | 4888 | 605 | AmberBunnyCage | — |
| `AmethystSquirrelCage` 紫晶松鼠笼 | 4889 | 606 | AmethystSquirrelCage | — |
| `TopazSquirrelCage` 黄玉松鼠笼 | 4890 | 607 | TopazSquirrelCage | — |
| `SapphireSquirrelCage` 蓝玉松鼠笼 | 4891 | 608 | SapphireSquirrelCage | — |
| `EmeraldSquirrelCage` 翡翠松鼠笼 | 4892 | 609 | EmeraldSquirrelCage | — |
| `RubySquirrelCage` 红玉松鼠笼 | 4893 | 610 | RubySquirrelCage | — |
| `DiamondSquirrelCage` 钻石松鼠笼 | 4894 | 611 | DiamondSquirrelCage | — |
| `AmberSquirrelCage` 琥珀松鼠笼 | 4895 | 612 | AmberSquirrelCage | — |
| `PottedLavaPlantPalm` 盆栽岩浆棕榈 | 4902 | 548 | PottedPlants2 | — |
| `PottedLavaPlantBush` 盆栽硫磺灌木 | 4903 | 548 | PottedPlants2 | — |
| `PottedLavaPlantBramble` 盆栽火荆棘 | 4904 | 613 | PottedLavaPlants | — |
| `PottedLavaPlantBulb` 盆栽熔岩球茎 | 4905 | 613 | PottedLavaPlants | — |
| `PottedLavaPlantTendrils` 盆栽余烬卷须 | 4906 | 614 | PottedLavaPlantTendrils | — |
| `VanityTreeYellowWillowSeed` 黄柳树苗 | 4907 | 615 | VanityTreeWillowSaplings | — |
| `TeleportationPylonHallow` 神圣晶塔 | 4916 | 597 | TeleportationPylon | — |
| `TeleportationPylonUnderground` 洞穴晶塔 | 4917 | 597 | TeleportationPylon | — |
| `TeleportationPylonOcean` 海洋晶塔 | 4918 | 597 | TeleportationPylon | — |
| `TeleportationPylonDesert` 沙漠晶塔 | 4919 | 597 | TeleportationPylon | — |
| `TeleportationPylonSnow` 雪原晶塔 | 4920 | 597 | TeleportationPylon | — |
| `TeleportationPylonMushroom` 蘑菇晶塔 | 4921 | 597 | TeleportationPylon | — |
| `CavernFountain` 洞穴喷泉 | 4922 | 207 | WaterFountain | — |
| `EyeofCthulhuMasterTrophy` 克苏鲁之眼圣物 | 4924 | 617 | MasterTrophyBase | — |
| `EaterofWorldsMasterTrophy` 世界吞噬怪圣物 | 4925 | 617 | MasterTrophyBase | — |
| `BrainofCthulhuMasterTrophy` 克苏鲁之脑圣物 | 4926 | 617 | MasterTrophyBase | — |
| `SkeletronMasterTrophy` 骷髅王圣物 | 4927 | 617 | MasterTrophyBase | — |
| `QueenBeeMasterTrophy` 蜂王圣物 | 4928 | 617 | MasterTrophyBase | — |
| `KingSlimeMasterTrophy` 史莱姆王圣物 | 4929 | 617 | MasterTrophyBase | — |
| `WallofFleshMasterTrophy` 血肉墙圣物 | 4930 | 617 | MasterTrophyBase | — |
| `TwinsMasterTrophy` 双子魔眼圣物 | 4931 | 617 | MasterTrophyBase | — |
| `DestroyerMasterTrophy` 毁灭者圣物 | 4932 | 617 | MasterTrophyBase | — |
| `SkeletronPrimeMasterTrophy` 机械骷髅王圣物 | 4933 | 617 | MasterTrophyBase | — |
| `PlanteraMasterTrophy` 世纪之花圣物 | 4934 | 617 | MasterTrophyBase | — |
| `GolemMasterTrophy` 石巨人圣物 | 4935 | 617 | MasterTrophyBase | — |
| `DukeFishronMasterTrophy` 猪龙鱼公爵圣物 | 4936 | 617 | MasterTrophyBase | — |
| `LunaticCultistMasterTrophy` 拜月教邪教徒圣物 | 4937 | 617 | MasterTrophyBase | — |
| `MoonLordMasterTrophy` 月亮领主圣物 | 4938 | 617 | MasterTrophyBase | — |
| `UFOMasterTrophy` 火星飞碟圣物 | 4939 | 617 | MasterTrophyBase | — |
| `FlyingDutchmanMasterTrophy` 荷兰飞盗船圣物 | 4940 | 617 | MasterTrophyBase | — |
| `MourningWoodMasterTrophy` 哀木圣物 | 4941 | 617 | MasterTrophyBase | — |
| `PumpkingMasterTrophy` 南瓜王圣物 | 4942 | 617 | MasterTrophyBase | — |
| `IceQueenMasterTrophy` 冰雪女王圣物 | 4943 | 617 | MasterTrophyBase | — |
| `EverscreamMasterTrophy` 常绿尖叫怪圣物 | 4944 | 617 | MasterTrophyBase | — |
| `SantankMasterTrophy` 圣诞坦克圣物 | 4945 | 617 | MasterTrophyBase | — |
| `DarkMageMasterTrophy` 暗黑魔法师圣物 | 4946 | 617 | MasterTrophyBase | — |
| `OgreMasterTrophy` 食人魔圣物 | 4947 | 617 | MasterTrophyBase | — |
| `BetsyMasterTrophy` 双足翼龙圣物 | 4948 | 617 | MasterTrophyBase | — |
| `FairyQueenMasterTrophy` 光之女皇圣物 | 4949 | 617 | MasterTrophyBase | — |
| `QueenSlimeMasterTrophy` 史莱姆皇后圣物 | 4950 | 617 | MasterTrophyBase | — |
| `TeleportationPylonVictory` 万能晶塔 | 4951 | 597 | TeleportationPylon | — |
| `QueenSlimeTrophy` 史莱姆皇后纪念章 | 4958 | 240 | Painting3X3 | — |
| `AccentSlab` 石材板 | 4962 | 618 | AccentSlab | — |
| `TruffleWormCage` 松露蠕虫笼 | 4963 | 619 | TruffleWormCage | — |
| `EmpressButterflyJar` 七彩草蛉罐 | 4964 | 620 | EmpressButterflyJar | — |
| `RockGolemBanner` 岩石巨人旗 | 4965 | 91 | Banners | — |
| `BloodMummyBanner` 血木乃伊旗 | 4966 | 91 | Banners | — |
| `SporeSkeletonBanner` 孢子骷髅旗 | 4967 | 91 | Banners | — |
| `SporeBatBanner` 孢子蝙蝠旗 | 4968 | 91 | Banners | — |
| `LarvaeAntlionBanner` 蚁狮幼虫旗 | 4969 | 91 | Banners | — |
| `CrimsonBunnyBanner` 毒兔兔旗 | 4970 | 91 | Banners | — |
| `CrimsonGoldfishBanner` 毒金鱼旗 | 4971 | 91 | Banners | — |
| `CrimsonPenguinBanner` 毒企鹅旗 | 4972 | 91 | Banners | — |
| `BigMimicCorruptionBanner` 腐化宝箱怪旗 | 4973 | 91 | Banners | — |
| `BigMimicCrimsonBanner` 猩红宝箱怪旗 | 4974 | 91 | Banners | — |
| `BigMimicHallowBanner` 神圣宝箱怪旗 | 4975 | 91 | Banners | — |
| `MossHornetBanner` 苔藓黄蜂旗 | 4976 | 91 | Banners | — |
| `WanderingEyeBanner` 游荡眼球怪旗 | 4977 | 91 | Banners | — |
| `MusicBoxQueenSlime` 八音盒（史莱姆皇后） | 4979 | 139 | MusicBoxes | — |
| `MusicBoxEmpressOfLight` 八音盒（光之女皇） | 4985 | 139 | MusicBoxes | — |
| `MusicBoxDukeFishron` 八音盒（猪龙鱼公爵） | 4990 | 139 | MusicBoxes | — |
| `MusicBoxMorningRain` 八音盒（晨雨） | 4991 | 139 | MusicBoxes | — |
| `MusicBoxConsoleTitle` 八音盒（标题备选曲） | 4992 | 139 | MusicBoxes | — |
| `ChippysCouch` 花栗鼠沙发 | 4993 | 89 | Benches | — |
| `OceanCrate` 海洋匣 | 5002 | 376 | FishingCrate | — |
| `OceanCrateHard` 海边匣 | 5003 | 376 | FishingCrate | — |
| `MusicBoxUndergroundDesert` 八音盒（地下沙漠） | 5006 | 139 | MusicBoxes | — |
| `TeaKettle` 茶壶 | 5008 | 622 | TeaKettle | — |
| `MusicBoxOWRain` 异界八音盒（雨） | 5014 | 139 | MusicBoxes | — |
| `MusicBoxOWDay` 异界八音盒（人间日） | 5015 | 139 | MusicBoxes | — |
| `MusicBoxOWNight` 异界八音盒（暗夜） | 5016 | 139 | MusicBoxes | — |
| `MusicBoxOWUnderground` 异界八音盒（地下） | 5017 | 139 | MusicBoxes | — |
| `MusicBoxOWDesert` 异界八音盒（沙漠） | 5018 | 139 | MusicBoxes | — |
| `MusicBoxOWOcean` 异界八音盒（海洋） | 5019 | 139 | MusicBoxes | — |
| `MusicBoxOWMushroom` 异界八音盒（蘑菇） | 5020 | 139 | MusicBoxes | — |
| `MusicBoxOWDungeon` 异界八音盒（地牢） | 5021 | 139 | MusicBoxes | — |
| `MusicBoxOWSpace` 异界八音盒（太空） | 5022 | 139 | MusicBoxes | — |
| `MusicBoxOWUnderworld` 异界八音盒（地狱） | 5023 | 139 | MusicBoxes | — |
| `MusicBoxOWSnow` 异界八音盒（雪） | 5024 | 139 | MusicBoxes | — |
| `MusicBoxOWCorruption` 异界八音盒（腐化之地） | 5025 | 139 | MusicBoxes | — |
| `MusicBoxOWUndergroundCorruption` 异界八音盒（地下腐化之地） | 5026 | 139 | MusicBoxes | — |
| `MusicBoxOWCrimson` 异界八音盒（猩红之地） | 5027 | 139 | MusicBoxes | — |
| `MusicBoxOWUndergroundCrimson` 异界八音盒（地下猩红之地） | 5028 | 139 | MusicBoxes | — |
| `MusicBoxOWUndergroundSnow` 异界八音盒（冰雪） | 5029 | 139 | MusicBoxes | — |
| `MusicBoxOWUndergroundHallow` 异界八音盒（地下神圣之地） | 5030 | 139 | MusicBoxes | — |
| `MusicBoxOWBloodMoon` 异界八音盒（恐惧） | 5031 | 139 | MusicBoxes | — |
| `MusicBoxOWBoss2` 异界八音盒(Boss 2) | 5032 | 139 | MusicBoxes | — |
| `MusicBoxOWBoss1` 异界八音盒(Boss 1) | 5033 | 139 | MusicBoxes | — |
| `MusicBoxOWInvasion` 异界八音盒（入侵） | 5034 | 139 | MusicBoxes | — |
| `MusicBoxOWTowers` 异界八音盒（神塔） | 5035 | 139 | MusicBoxes | — |
| `MusicBoxOWMoonLord` 异界八音盒（月亮Boss） | 5036 | 139 | MusicBoxes | — |
| `MusicBoxOWPlantera` 异界八音盒（世纪之花） | 5037 | 139 | MusicBoxes | — |
| `MusicBoxOWJungle` 异界八音盒（丛林） | 5038 | 139 | MusicBoxes | — |
| `MusicBoxOWWallOfFlesh` 异界八音盒（血肉墙） | 5039 | 139 | MusicBoxes | — |
| `MusicBoxOWHallow` 异界八音盒（神圣之地） | 5040 | 139 | MusicBoxes | — |
| `MusicBoxCredits` 八音盒（旅程结束） | 5044 | 139 | MusicBoxes | — |
| `BeeHive` 蜂巢 | 5066 | 444 | BeeHive | — |
| `AntlionEggs` 蚁狮卵 | 5067 | 485 | AntlionLarva | — |
| `PottedCrystalPlantFern` 水晶蕨盆栽 | 5081 | 623 | PottedCrystalPlants | — |
| `PottedCrystalPlantSpiral` 水晶螺旋盆栽 | 5082 | 623 | PottedCrystalPlants | — |
| `PottedCrystalPlantTeardrop` 水晶泪滴盆栽 | 5083 | 623 | PottedCrystalPlants | — |
| `PottedCrystalPlantTree` 水晶树盆栽 | 5084 | 623 | PottedCrystalPlants | — |
| `Princess64` 公主64号 | 5085 | 242 | Painting6X4 | — |
| `PaintingOfALass` 小姑娘画像 | 5086 | 245 | Painting2X3 | — |
| `DarkSideHallow` 圣徒的黑暗面 | 5087 | 240 | Painting3X3 | — |
| `DeerclopsTrophy` 鹿角怪纪念章 | 5108 | 240 | Painting3X3 | — |
| `DeerclopsMasterTrophy` 鹿角怪圣物 | 5110 | 617 | MasterTrophyBase | — |
| `MusicBoxDeerclops` 八音盒（鹿角怪） | 5112 | 139 | MusicBoxes | — |
| `PaintingWilson` 绅士科学家 | 5121 | 242 | Painting6X4 | — |
| `PaintingWillow` 纵火者 | 5122 | 242 | Painting6X4 | — |
| `PaintingWendy` 遗族 | 5123 | 242 | Painting6X4 | — |
| `PaintingWolfgang` 大力士 | 5124 | 242 | Painting6X4 | — |
| `VioletMoss` 氖苔藓 | 5127 | 625 | VioletMoss | — |
| `RainbowMoss` 氦苔藓 | 5128 | 627 | RainbowMoss | — |
| `StinkbugCage` 臭虫笼 | 5133 | 629 | StinkbugCage | — |
| `VenomDartTrap` 毒液飞镖机关 | 5135 | 137 | Traps | — |
| `StinkbugHousingBlocker` 臭虫拦截器 | 5137 | 630 | StinkbugHousingBlocker | — |
| `StinkbugHousingBlockerEcho` 幽灵臭虫拦截器 | 5138 | 631 | StinkbugHousingBlockerEcho | — |
| `CoralBathtub` 礁浴缸 | 5148 | 90 | Bathtubs | — |
| `CoralBed` 礁床 | 5149 | 79 | Beds | — |
| `CoralBookcase` 礁书架 | 5150 | 101 | Bookcases | — |
| `CoralDresser` 礁梳妆台 | 5151 | 88 | Dressers | — |
| `CoralCandelabra` 礁烛台 | 5152 | 100 | Candelabras | — |
| `CoralCandle` 礁蜡烛 | 5153 | 33 | Candles | — |
| `CoralChair` 礁椅 | 5154 | 15 | Chairs | — |
| `CoralChandelier` 礁吊灯 | 5155 | 34 | Chandeliers | — |
| `CoralChest` 礁宝箱 | 5156 | 467 | Containers2 | — |
| `CoralClock` 礁钟 | 5157 | 104 | GrandfatherClocks | — |
| `CoralDoor` 礁门 | 5158 | 10 | ClosedDoor | — |
| `CoralLamp` 礁灯 | 5159 | 93 | Lamps | — |
| `CoralLantern` 礁灯笼 | 5160 | 42 | HangingLanterns | — |
| `CoralPiano` 礁钢琴 | 5161 | 87 | Pianos | — |
| `CoralPlatform` 礁平台 | 5162 | 19 | Platforms | — |
| `CoralSink` 礁水槽 | 5163 | 172 | Sinks | — |
| `CoralSofa` 礁沙发 | 5164 | 89 | Benches | — |
| `CoralTable` 礁桌子 | 5165 | 469 | Tables2 | — |
| `CoralWorkbench` 礁工作台 | 5166 | 18 | WorkBenches | — |
| `Fake_CoralChest` 受困礁宝箱 | 5167 | 468 | FakeContainers2 | — |
| `CoralToilet` 礁马桶 | 5168 | 497 | Toilets | — |
| `BalloonBathtub` 气球浴缸 | 5169 | 90 | Bathtubs | — |
| `BalloonBed` 气球床 | 5170 | 79 | Beds | — |
| `BalloonBookcase` 气球书架 | 5171 | 101 | Bookcases | — |
| `BalloonDresser` 气球梳妆台 | 5172 | 88 | Dressers | — |
| `BalloonCandelabra` 气球烛台 | 5173 | 100 | Candelabras | — |
| `BalloonCandle` 气球蜡烛 | 5174 | 33 | Candles | — |
| `BalloonChair` 气球椅 | 5175 | 15 | Chairs | — |
| `BalloonChandelier` 气球吊灯 | 5176 | 34 | Chandeliers | — |
| `BalloonChest` 气球宝箱 | 5177 | 467 | Containers2 | — |
| `BalloonClock` 气球钟 | 5178 | 104 | GrandfatherClocks | — |
| `BalloonDoor` 气球门 | 5179 | 10 | ClosedDoor | — |
| `BalloonLamp` 气球灯 | 5180 | 93 | Lamps | — |
| `BalloonLantern` 气球灯笼 | 5181 | 42 | HangingLanterns | — |
| `BalloonPiano` 气球钢琴 | 5182 | 87 | Pianos | — |
| `BalloonPlatform` 气球平台 | 5183 | 19 | Platforms | — |
| `BalloonSink` 气球水槽 | 5184 | 172 | Sinks | — |
| `BalloonSofa` 气球沙发 | 5185 | 89 | Benches | — |
| `BalloonTable` 气球桌 | 5186 | 469 | Tables2 | — |
| `BalloonWorkbench` 气球工作台 | 5187 | 18 | WorkBenches | — |
| `Fake_BalloonChest` 受困气球宝箱 | 5188 | 468 | FakeContainers2 | — |
| `BalloonToilet` 气球马桶 | 5189 | 497 | Toilets | — |
| `AshWoodBathtub` 白蜡木浴缸 | 5190 | 90 | Bathtubs | — |
| `AshWoodBed` 白蜡木床 | 5191 | 79 | Beds | — |
| `AshWoodBookcase` 白蜡木书架 | 5192 | 101 | Bookcases | — |
| `AshWoodDresser` 针叶木梳妆台 | 5193 | 88 | Dressers | — |
| `AshWoodCandelabra` 白蜡木烛台 | 5194 | 100 | Candelabras | — |
| `AshWoodCandle` 白蜡木蜡烛 | 5195 | 33 | Candles | — |
| `AshWoodChair` 白蜡木椅子 | 5196 | 15 | Chairs | — |
| `AshWoodChandelier` 白蜡木吊灯 | 5197 | 34 | Chandeliers | — |
| `AshWoodChest` 白蜡木宝箱 | 5198 | 467 | Containers2 | — |
| `AshWoodClock` 白蜡木钟 | 5199 | 104 | GrandfatherClocks | — |
| `AshWoodDoor` 白蜡木门 | 5200 | 10 | ClosedDoor | — |
| `AshWoodLamp` 白蜡木灯 | 5201 | 93 | Lamps | — |
| `AshWoodLantern` 白蜡木灯笼 | 5202 | 42 | HangingLanterns | — |
| `AshWoodPiano` 白蜡木钢琴 | 5203 | 87 | Pianos | — |
| `AshWoodPlatform` 白蜡木平台 | 5204 | 19 | Platforms | — |
| `AshWoodSink` 白蜡木水槽 | 5205 | 172 | Sinks | — |
| `AshWoodSofa` 白蜡木沙发 | 5206 | 89 | Benches | — |
| `AshWoodTable` 白蜡木桌子 | 5207 | 469 | Tables2 | — |
| `AshWoodWorkbench` 白蜡木工作台 | 5208 | 18 | WorkBenches | — |
| `Fake_AshWoodChest` 受困白蜡木宝箱 | 5209 | 468 | FakeContainers2 | — |
| `AshWoodToilet` 白蜡木马桶 | 5210 | 497 | Toilets | — |
| `ScarletMacawCage` 猩红金刚鹦鹉笼 | 5213 | 632 | ScarletMacawCage | — |
| `AshGrassSeeds` 白蜡草种子 | 5214 | 633 | AshGrass | — |
| `AshWood` 白蜡木 | 5215 | 635 | AshWood | — |
| `AshWoodWall` 白蜡木墙 | 5216 | — | — | 316 |
| `AshWoodFence` 白蜡木栅栏 | 5217 | — | — | 317 |
| `Outcast` 流浪者 | 5218 | 240 | Painting3X3 | — |
| `FairyGuides` 仙灵指引 | 5219 | 240 | Painting3X3 | — |
| `AHorribleNightforAlchemy` 恐怖炼药之夜 | 5220 | 240 | Painting3X3 | — |
| `MorningHunt` 晨猎 | 5221 | 240 | Painting3X3 | — |
| `SuspiciouslySparkly` 可疑闪光 | 5222 | 240 | Painting3X3 | — |
| `Requiem` 安魂 | 5223 | 240 | Painting3X3 | — |
| `CatSword` 猫剑 | 5224 | 240 | Painting3X3 | — |
| `KargohsSummon` Kargoh's Summon | 5225 | 240 | Painting3X3 | — |
| `HighPitch` 高音 | 5226 | 240 | Painting3X3 | — |
| `AMachineforTerrarians` 泰拉瑞亚人的机器 | 5227 | 240 | Painting3X3 | — |
| `TerraBladeChronicles` 泰拉刃史志 | 5228 | 240 | Painting3X3 | — |
| `BennyWarhol` Benny Warhol | 5229 | 240 | Painting3X3 | — |
| `LizardKing` 蜥蜴王 | 5230 | 240 | Painting3X3 | — |
| `MySon` 我的儿子 | 5231 | 240 | Painting3X3 | — |
| `Duality` 对偶 | 5232 | 240 | Painting3X3 | — |
| `ParsecPals` Parsec Pals | 5233 | 240 | Painting3X3 | — |
| `RemnantsofDevotion` 虔诚的遗骸 | 5234 | 242 | Painting6X4 | — |
| `NotSoLostInParadise` 并未如此迷失在乐园 | 5235 | 242 | Painting6X4 | — |
| `OcularResonance` 视觉共振 | 5236 | 242 | Painting6X4 | — |
| `WingsofEvil` 邪恶之翼 | 5237 | 242 | Painting6X4 | — |
| `Constellation` 星座 | 5238 | 242 | Painting6X4 | — |
| `Eyezorhead` 眼怪头 | 5239 | 242 | Painting6X4 | — |
| `DreadoftheRedSea` 红海恐惧 | 5240 | 242 | Painting6X4 | — |
| `DoNotEattheVileMushroom` 不要吃卑鄙的蘑菇！ | 5241 | 242 | Painting6X4 | — |
| `YuumaTheBlueTiger` Yuuma, The Blue Tiger | 5242 | 242 | Painting6X4 | — |
| `MoonmanandCompany` 月球人公司 | 5243 | 242 | Painting6X4 | — |
| `SunshineofIsrapony` Sunshine of Israpony | 5244 | 242 | Painting6X4 | — |
| `Purity` 纯净 | 5245 | 242 | Painting6X4 | — |
| `SufficientlyAdvanced` 极其先进 | 5246 | 245 | Painting2X3 | — |
| `StrangeGrowth` 奇特成长 | 5247 | 245 | Painting2X3 | — |
| `HappyLittleTree` 快乐小树 | 5248 | 245 | Painting2X3 | — |
| `StrangeDeadFellows` 奇怪死者 | 5249 | 245 | Painting2X3 | — |
| `Secrets` 秘密 | 5250 | 245 | Painting2X3 | — |
| `Thunderbolt` 雷震 | 5251 | 245 | Painting2X3 | — |
| `Crustography` 甲壳纲地理学 | 5252 | 245 | Painting2X3 | — |
| `TheWerewolf` 狼人 | 5253 | 245 | Painting2X3 | — |
| `BlessingfromTheHeavens` 天堂的祝福 | 5254 | 245 | Painting2X3 | — |
| `LoveisintheTrashSlot` 爱在垃圾槽 | 5255 | 245 | Painting2X3 | — |
| `Fangs` 獠牙 | 5256 | 245 | Painting2X3 | — |
| `HailtotheKing` 国王万岁 | 5257 | 245 | Painting2X3 | — |
| `SeeTheWorldForWhatItIs` 看清世界的真谛 | 5258 | 246 | Painting3X2 | — |
| `WhatLurksBelow` 下面潜伏着什么 | 5259 | 246 | Painting3X2 | — |
| `ThisIsGettingOutOfHand` 局面失控 | 5260 | 246 | Painting3X2 | — |
| `Buddies` 铁哥们 | 5261 | 246 | Painting3X2 | — |
| `MidnightSun` 午夜太阳 | 5262 | 246 | Painting3X2 | — |
| `CouchGag` 沙发塞物 | 5263 | 246 | Painting3X2 | — |
| `SilentFish` 沉默的鱼 | 5264 | 246 | Painting3X2 | — |
| `TheDuke` 公爵 | 5265 | 246 | Painting3X2 | — |
| `RoyalRomance` 皇家浪漫 | 5266 | 246 | Painting3X2 | — |
| `Bioluminescence` 生物发光 | 5267 | 246 | Painting3X2 | — |
| `Wildflowers` 野花 | 5268 | 246 | Painting3X2 | — |
| `VikingVoyage` 维京之旅 | 5269 | 246 | Painting3X2 | — |
| `Bifrost` 彩虹桥 | 5270 | 246 | Painting3X2 | — |
| `Heartlands` 心脏地带 | 5271 | 246 | Painting3X2 | — |
| `ForestTroll` 森林巨魔 | 5272 | 246 | Painting3X2 | — |
| `AuroraBorealis` 北极光 | 5273 | 246 | Painting3X2 | — |
| `LadyOfTheLake` 湖泊夫人 | 5274 | 242 | Painting6X4 | — |
| `RepairedLifeCrystal` 修复的生命水晶 | 5286 | 12 | Heart | — |
| `RepairedManaCrystal` 修复的魔力水晶 | 5287 | 639 | ManaCrystal | — |
| `EchoWall` 回音墙 | 5291 | — | — | 318 |
| `EchoPlatform` 回声平台 | 5292 | 19 | Platforms | — |
| `MushroomTorch` 蘑菇火把 | 5293 | 4 | Torches | — |
| `AcornAxe` 再生之斧 | 5295 | 2 | Grass | — |
| `ChlorophyteExtractinator` 叶绿素提炼机 | 5296 | 642 | ChlorophyteExtractinator | — |
| `MushroomCampfire` 蘑菇篝火 | 5299 | 215 | Campfire | — |
| `BlueMacawCage` 蓝色金刚鹦鹉笼 | 5301 | 640 | BlueMacawCage | — |
| `ReefBlock` 礁块 | 5306 | 641 | ReefBlock | — |
| `ReefWall` 礁墙 | 5307 | — | — | 319 |
| `PlacePainting` r/泰拉瑞亚 | 5308 | 242 | Painting6X4 | — |
| `PrincessStyle` 公主风 | 5310 | 242 | Painting6X4 | — |
| `ToucanCage` 犀鸟笼 | 5314 | 643 | ToucanCage | — |
| `YellowCockatielCage` 黄色玄凤鹦鹉笼 | 5315 | 644 | YellowCockatielCage | — |
| `GrayCockatielCage` 灰色玄凤鹦鹉笼 | 5316 | 645 | GrayCockatielCage | — |
| `MacawStatue` 金刚鹦鹉雕像 | 5317 | 105 | Statues | — |
| `ToucanStatue` 犀鸟雕像 | 5318 | 105 | Statues | — |
| `CockatielStatue` 玄凤鹦鹉雕像 | 5319 | 105 | Statues | — |
| `PlaceableHealingPotion` 装饰治疗药水 | 5320 | 13 | Bottles | — |
| `PlaceableManaPotion` 装饰魔力药水 | 5321 | 13 | Bottles | — |
| `ShadowCandle` 影烛 | 5322 | 646 | ShadowCandle | — |
| `RubblemakerSmall` 碎石机（小） | 5324 | 647 | LargePilesEcho | — |
| `TNTBarrel` TNT枪管 | 5327 | 654 | TNTBarrel | — |
| `RubblemakerMedium` 碎石机（中） | 5329 | 647 | LargePilesEcho | — |
| `RubblemakerLarge` 碎石机（大） | 5330 | 647 | LargePilesEcho | — |
| `EchoMonolith` 回声室 | 5345 | 657 | EchoMonolith | — |
| `ShimmerMonolith` 伊瑟尔天塔柱 | 5347 | 658 | ShimmerMonolith | — |
| `ShimmerBlock` 镒块 | 5349 | 659 | ShimmerBlock | — |
| `ShimmerflyinaBottle` 仙灵瓶 | 5351 | 660 | ShimmerflyinaBottle | — |
| `ShimmerSlimeBanner` 微光史莱姆旗 | 5352 | 91 | Banners | — |
| `ShimmerTorch` 伊瑟尔火把 | 5353 | 4 | Torches | — |
| `ShimmerCampfire` 伊瑟尔篝火 | 5357 | 215 | Campfire | — |
| `MusicBoxShimmer` 八音盒（伊瑟尔） | 5362 | 139 | MusicBoxes | — |
| `SpiderWallUnsafe` 蜘蛛墙 | 5363 | — | — | 62 |
| `BlueBrickWallUnsafe` 被诅咒的蓝砖墙 | 5365 | — | — | 7 |
| `BlueSlabWallUnsafe` 被诅咒的蓝板墙 | 5366 | — | — | 94 |
| `BlueTiledWallUnsafe` 被诅咒的蓝瓷砖墙 | 5367 | — | — | 95 |
| `PinkBrickWallUnsafe` 被诅咒的粉砖墙 | 5368 | — | — | 9 |
| `PinkSlabWallUnsafe` 被诅咒的粉板墙 | 5369 | — | — | 96 |
| `PinkTiledWallUnsafe` 被诅咒的粉瓷砖墙 | 5370 | — | — | 97 |
| `GreenBrickWallUnsafe` 被诅咒的绿砖墙 | 5371 | — | — | 8 |
| `GreenSlabWallUnsafe` 被诅咒的绿板墙 | 5372 | — | — | 98 |
| `GreenTiledWallUnsafe` 被诅咒的绿瓷砖墙 | 5373 | — | — | 99 |
| `SandstoneWallUnsafe` 危险的沙岩墙 | 5374 | — | — | 187 |
| `HardenedSandWallUnsafe` 危险的硬化沙墙 | 5375 | — | — | 216 |
| `LihzahrdWallUnsafe` 被禁的蜥蜴砖墙 | 5376 | — | — | 87 |
| `Moondial` 附魔月晷 | 5381 | 663 | Moondial | — |
| `BouncyBoulder` 弹力巨石 | 5383 | 664 | BouncyBoulder | — |
| `LifeCrystalBoulder` 巨型生命水晶 | 5384 | 665 | LifeCrystalBoulder | — |
| `SunOrnament` 太阳之眼 | 5388 | 240 | Painting3X3 | — |
| `HoplitePizza` 奶酪比萨海报 | 5389 | 242 | Painting6X4 | — |
| `PoopBlock` 臭臭 | 5395 | 666 | PoopBlock | — |
| `PoopWall` 臭臭墙 | 5396 | — | — | 320 |
| `ShimmerWall` 镒墙 | 5397 | — | — | 321 |
| `ShimmerBrick` 镒砖 | 5398 | 667 | ShimmerBrick | — |
| `ShimmerBrickWall` 镒砖墙 | 5399 | — | — | 322 |
| `LunarRustBrick` 月亮锈砖 | 5401 | 669 | LunarRustBrick | — |
| `DarkCelestialBrick` 暗黑天界砖 | 5402 | 670 | DarkCelestialBrick | — |
| `AstraBrick` 阿斯特拉砖 | 5403 | 671 | AstraBrick | — |
| `CosmicEmberBrick` 宇宙余烬砖 | 5404 | 672 | CosmicEmberBrick | — |
| `CryocoreBrick` 冷核砖 | 5405 | 673 | CryocoreBrick | — |
| `MercuryBrick` 水银砖 | 5406 | 674 | MercuryBrick | — |
| `StarRoyaleBrick` 星星皇家砖 | 5407 | 675 | StarRoyaleBrick | — |
| `HeavenforgeBrick` 天熔砖 | 5408 | 676 | HeavenforgeBrick | — |
| `LunarRustBrickWall` 月亮锈砖墙 | 5409 | — | — | 323 |
| `DarkCelestialBrickWall` 暗黑天界砖墙 | 5410 | — | — | 324 |
| `AstraBrickWall` 阿斯特拉砖墙 | 5411 | — | — | 325 |
| `CosmicEmberBrickWall` 宇宙余烬砖墙 | 5412 | — | — | 326 |
| `CryocoreBrickWall` 冷核砖墙 | 5413 | — | — | 327 |
| `MercuryBrickWall` 水银砖墙 | 5414 | — | — | 328 |
| `StarRoyaleBrickWall` 星星皇家砖墙 | 5415 | — | — | 329 |
| `HeavenforgeBrickWall` 天熔砖墙 | 5416 | — | — | 330 |
| `AncientBlueDungeonBrick` 远古蓝砖 | 5417 | 677 | AncientBlueBrick | — |
| `AncientBlueDungeonBrickWall` 远古蓝砖墙 | 5418 | — | — | 331 |
| `AncientGreenDungeonBrick` 远古绿砖 | 5419 | 678 | AncientGreenBrick | — |
| `AncientGreenDungeonBrickWall` 远古绿砖墙 | 5420 | — | — | 332 |
| `AncientPinkDungeonBrick` 远古粉砖 | 5421 | 679 | AncientPinkBrick | — |
| `AncientPinkDungeonBrickWall` 远古粉砖墙 | 5422 | — | — | 333 |
| `AncientGoldBrick` 远古金砖 | 5423 | 680 | AncientGoldBrick | — |
| `AncientGoldBrickWall` 远古金砖墙 | 5424 | — | — | 334 |
| `AncientSilverBrick` 远古银砖 | 5425 | 681 | AncientSilverBrick | — |
| `AncientSilverBrickWall` 远古银砖墙 | 5426 | — | — | 335 |
| `AncientCopperBrick` 远古铜砖 | 5427 | 682 | AncientCopperBrick | — |
| `AncientCopperBrickWall` 远古铜砖墙 | 5428 | — | — | 336 |
| `AncientCobaltBrick` 远古钴砖 | 5429 | 685 | AncientCobaltBrick | — |
| `AncientCobaltBrickWall` 远古钴砖墙 | 5430 | — | — | 339 |
| `AncientMythrilBrick` 远古秘银砖 | 5431 | 686 | AncientMythrilBrick | — |
| `AncientMythrilBrickWall` 远古秘银砖墙 | 5432 | — | — | 340 |
| `AncientObsidianBrick` 远古黑曜石砖 | 5433 | 683 | AncientObsidianBrick | — |
| `AncientObsidianBrickWall` 远古黑曜石砖墙 | 5434 | — | — | 337 |
| `AncientHellstoneBrick` 远古狱石砖 | 5435 | 684 | AncientHellstoneBrick | — |
| `AncientHellstoneBrickWall` 远古狱石砖墙 | 5436 | — | — | 338 |
| `LavaMossBlock` 熔岩苔藓砖 | 5439 | 687 | LavaMossBlock | — |
| `ArgonMossBlock` 氩苔藓砖 | 5440 | 688 | ArgonMossBlock | — |
| `KryptonMossBlock` 氪苔藓砖 | 5441 | 689 | KryptonMossBlock | — |
| `XenonMossBlock` 氙苔藓砖 | 5442 | 690 | XenonMossBlock | — |
| `VioletMossBlock` 氖苔藓砖 | 5443 | 691 | VioletMossBlock | — |
| `RainbowMossBlock` 氦苔藓砖 | 5444 | 692 | RainbowMossBlock | — |
| `LavaMossBlockWall` 熔岩苔藓砖墙 | 5445 | — | — | 341 |
| `ArgonMossBlockWall` 氩苔藓砖墙 | 5446 | — | — | 342 |
| `KryptonMossBlockWall` 氪苔藓砖墙 | 5447 | — | — | 343 |
| `XenonMossBlockWall` 氙苔藓砖墙 | 5448 | — | — | 344 |
| `VioletMossBlockWall` 氖苔藓砖墙 | 5449 | — | — | 345 |
| `RainbowMossBlockWall` 氦苔藓砖墙 | 5450 | — | — | 346 |

## 10. 物块枚举全表

> 共 693 条，按 id 升序。左是 id，右是程序里的枚举名。

- 0 `Dirt`
- 1 `Stone`
- 2 `Grass`
- 3 `Plants`
- 4 `Torches`
- 5 `Trees`
- 6 `Iron`
- 7 `Copper`
- 8 `Gold`
- 9 `Silver`
- 10 `ClosedDoor`
- 11 `OpenDoor`
- 12 `Heart`
- 13 `Bottles`
- 14 `Tables`
- 15 `Chairs`
- 16 `Anvils`
- 17 `Furnaces`
- 18 `WorkBenches`
- 19 `Platforms`
- 20 `Saplings`
- 21 `Containers`
- 22 `Demonite`
- 23 `CorruptGrass`
- 24 `CorruptPlants`
- 25 `Ebonstone`
- 26 `DemonAltar`
- 27 `Sunflower`
- 28 `Pots`
- 29 `PiggyBank`
- 30 `WoodBlock`
- 31 `ShadowOrbs`
- 32 `CorruptThorns`
- 33 `Candles`
- 34 `Chandeliers`
- 35 `Jackolanterns`
- 36 `Presents`
- 37 `Meteorite`
- 38 `GrayBrick`
- 39 `RedBrick`
- 40 `ClayBlock`
- 41 `BlueDungeonBrick`
- 42 `HangingLanterns`
- 43 `GreenDungeonBrick`
- 44 `PinkDungeonBrick`
- 45 `GoldBrick`
- 46 `SilverBrick`
- 47 `CopperBrick`
- 48 `Spikes`
- 49 `WaterCandle`
- 50 `Books`
- 51 `Cobweb`
- 52 `Vines`
- 53 `Sand`
- 54 `Glass`
- 55 `Signs`
- 56 `Obsidian`
- 57 `Ash`
- 58 `Hellstone`
- 59 `Mud`
- 60 `JungleGrass`
- 61 `JunglePlants`
- 62 `JungleVines`
- 63 `Sapphire`
- 64 `Ruby`
- 65 `Emerald`
- 66 `Topaz`
- 67 `Amethyst`
- 68 `Diamond`
- 69 `JungleThorns`
- 70 `MushroomGrass`
- 71 `MushroomPlants`
- 72 `MushroomTrees`
- 73 `Plants2`
- 74 `JunglePlants2`
- 75 `ObsidianBrick`
- 76 `HellstoneBrick`
- 77 `Hellforge`
- 78 `ClayPot`
- 79 `Beds`
- 80 `Cactus`
- 81 `Coral`
- 82 `ImmatureHerbs`
- 83 `MatureHerbs`
- 84 `BloomingHerbs`
- 85 `Tombstones`
- 86 `Loom`
- 87 `Pianos`
- 88 `Dressers`
- 89 `Benches`
- 90 `Bathtubs`
- 91 `Banners`
- 92 `Lampposts`
- 93 `Lamps`
- 94 `Kegs`
- 95 `ChineseLanterns`
- 96 `CookingPots`
- 97 `Safes`
- 98 `SkullLanterns`
- 99 `TrashCan`
- 100 `Candelabras`
- 101 `Bookcases`
- 102 `Thrones`
- 103 `Bowls`
- 104 `GrandfatherClocks`
- 105 `Statues`
- 106 `Sawmill`
- 107 `Cobalt`
- 108 `Mythril`
- 109 `HallowedGrass`
- 110 `HallowedPlants`
- 111 `Adamantite`
- 112 `Ebonsand`
- 113 `HallowedPlants2`
- 114 `TinkerersWorkbench`
- 115 `HallowedVines`
- 116 `Pearlsand`
- 117 `Pearlstone`
- 118 `PearlstoneBrick`
- 119 `IridescentBrick`
- 120 `Mudstone`
- 121 `CobaltBrick`
- 122 `MythrilBrick`
- 123 `Silt`
- 124 `WoodenBeam`
- 125 `CrystalBall`
- 126 `DiscoBall`
- 127 `MagicalIceBlock`
- 128 `Mannequin`
- 129 `Crystals`
- 130 `ActiveStoneBlock`
- 131 `InactiveStoneBlock`
- 132 `Lever`
- 133 `AdamantiteForge`
- 134 `MythrilAnvil`
- 135 `PressurePlates`
- 136 `Switches`
- 137 `Traps`
- 138 `Boulder`
- 139 `MusicBoxes`
- 140 `DemoniteBrick`
- 141 `Explosives`
- 142 `InletPump`
- 143 `OutletPump`
- 144 `Timers`
- 145 `CandyCaneBlock`
- 146 `GreenCandyCaneBlock`
- 147 `SnowBlock`
- 148 `SnowBrick`
- 149 `HolidayLights`
- 150 `AdamantiteBeam`
- 151 `SandstoneBrick`
- 152 `EbonstoneBrick`
- 153 `RedStucco`
- 154 `YellowStucco`
- 155 `GreenStucco`
- 156 `GrayStucco`
- 157 `Ebonwood`
- 158 `RichMahogany`
- 159 `Pearlwood`
- 160 `RainbowBrick`
- 161 `IceBlock`
- 162 `BreakableIce`
- 163 `CorruptIce`
- 164 `HallowedIce`
- 165 `Stalactite`
- 166 `Tin`
- 167 `Lead`
- 168 `Tungsten`
- 169 `Platinum`
- 170 `PineTree`
- 171 `ChristmasTree`
- 172 `Sinks`
- 173 `PlatinumCandelabra`
- 174 `PlatinumCandle`
- 175 `TinBrick`
- 176 `TungstenBrick`
- 177 `PlatinumBrick`
- 178 `ExposedGems`
- 179 `GreenMoss`
- 180 `BrownMoss`
- 181 `RedMoss`
- 182 `BlueMoss`
- 183 `PurpleMoss`
- 184 `LongMoss`
- 185 `SmallPiles`
- 186 `LargePiles`
- 187 `LargePiles2`
- 188 `CactusBlock`
- 189 `Cloud`
- 190 `MushroomBlock`
- 191 `LivingWood`
- 192 `LeafBlock`
- 193 `SlimeBlock`
- 194 `BoneBlock`
- 195 `FleshBlock`
- 196 `RainCloud`
- 197 `FrozenSlimeBlock`
- 198 `Asphalt`
- 199 `CrimsonGrass`
- 200 `FleshIce`
- 201 `CrimsonPlants`
- 202 `Sunplate`
- 203 `Crimstone`
- 204 `Crimtane`
- 205 `CrimsonVines`
- 206 `IceBrick`
- 207 `WaterFountain`
- 208 `Shadewood`
- 209 `Cannon`
- 210 `LandMine`
- 211 `Chlorophyte`
- 212 `SnowballLauncher`
- 213 `Rope`
- 214 `Chain`
- 215 `Campfire`
- 216 `Firework`
- 217 `Blendomatic`
- 218 `MeatGrinder`
- 219 `Extractinator`
- 220 `Solidifier`
- 221 `Palladium`
- 222 `Orichalcum`
- 223 `Titanium`
- 224 `Slush`
- 225 `Hive`
- 226 `LihzahrdBrick`
- 227 `DyePlants`
- 228 `DyeVat`
- 229 `HoneyBlock`
- 230 `CrispyHoneyBlock`
- 231 `Larva`
- 232 `WoodenSpikes`
- 233 `PlantDetritus`
- 234 `Crimsand`
- 235 `Teleporter`
- 236 `LifeFruit`
- 237 `LihzahrdAltar`
- 238 `PlanteraBulb`
- 239 `MetalBars`
- 240 `Painting3X3`
- 241 `Painting4X3`
- 242 `Painting6X4`
- 243 `ImbuingStation`
- 244 `BubbleMachine`
- 245 `Painting2X3`
- 246 `Painting3X2`
- 247 `Autohammer`
- 248 `PalladiumColumn`
- 249 `BubblegumBlock`
- 250 `Titanstone`
- 251 `PumpkinBlock`
- 252 `HayBlock`
- 253 `SpookyWood`
- 254 `Pumpkins`
- 255 `AmethystGemsparkOff`
- 256 `TopazGemsparkOff`
- 257 `SapphireGemsparkOff`
- 258 `EmeraldGemsparkOff`
- 259 `RubyGemsparkOff`
- 260 `DiamondGemsparkOff`
- 261 `AmberGemsparkOff`
- 262 `AmethystGemspark`
- 263 `TopazGemspark`
- 264 `SapphireGemspark`
- 265 `EmeraldGemspark`
- 266 `RubyGemspark`
- 267 `DiamondGemspark`
- 268 `AmberGemspark`
- 269 `Womannequin`
- 270 `FireflyinaBottle`
- 271 `LightningBuginaBottle`
- 272 `Cog`
- 273 `StoneSlab`
- 274 `SandStoneSlab`
- 275 `BunnyCage`
- 276 `SquirrelCage`
- 277 `MallardDuckCage`
- 278 `DuckCage`
- 279 `BirdCage`
- 280 `BlueJay`
- 281 `CardinalCage`
- 282 `FishBowl`
- 283 `HeavyWorkBench`
- 284 `CopperPlating`
- 285 `SnailCage`
- 286 `GlowingSnailCage`
- 287 `AmmoBox`
- 288 `MonarchButterflyJar`
- 289 `PurpleEmperorButterflyJar`
- 290 `RedAdmiralButterflyJar`
- 291 `UlyssesButterflyJar`
- 292 `SulphurButterflyJar`
- 293 `TreeNymphButterflyJar`
- 294 `ZebraSwallowtailButterflyJar`
- 295 `JuliaButterflyJar`
- 296 `ScorpionCage`
- 297 `BlackScorpionCage`
- 298 `FrogCage`
- 299 `MouseCage`
- 300 `BoneWelder`
- 301 `FleshCloningVat`
- 302 `GlassKiln`
- 303 `LihzahrdFurnace`
- 304 `LivingLoom`
- 305 `SkyMill`
- 306 `IceMachine`
- 307 `SteampunkBoiler`
- 308 `HoneyDispenser`
- 309 `PenguinCage`
- 310 `WormCage`
- 311 `DynastyWood`
- 312 `RedDynastyShingles`
- 313 `BlueDynastyShingles`
- 314 `MinecartTrack`
- 315 `Coralstone`
- 316 `BlueJellyfishBowl`
- 317 `GreenJellyfishBowl`
- 318 `PinkJellyfishBowl`
- 319 `ShipInABottle`
- 320 `SeaweedPlanter`
- 321 `BorealWood`
- 322 `PalmWood`
- 323 `PalmTree`
- 324 `BeachPiles`
- 325 `TinPlating`
- 326 `Waterfall`
- 327 `Lavafall`
- 328 `Confetti`
- 329 `ConfettiBlack`
- 330 `CopperCoinPile`
- 331 `SilverCoinPile`
- 332 `GoldCoinPile`
- 333 `PlatinumCoinPile`
- 334 `WeaponsRack`
- 335 `FireworksBox`
- 336 `LivingFire`
- 337 `AlphabetStatues`
- 338 `FireworkFountain`
- 339 `GrasshopperCage`
- 340 `LivingCursedFire`
- 341 `LivingDemonFire`
- 342 `LivingFrostFire`
- 343 `LivingIchor`
- 344 `LivingUltrabrightFire`
- 345 `Honeyfall`
- 346 `ChlorophyteBrick`
- 347 `CrimtaneBrick`
- 348 `ShroomitePlating`
- 349 `MushroomStatue`
- 350 `MartianConduitPlating`
- 351 `ChimneySmoke`
- 352 `CrimsonThorns`
- 353 `VineRope`
- 354 `BewitchingTable`
- 355 `AlchemyTable`
- 356 `Sundial`
- 357 `MarbleBlock`
- 358 `GoldBirdCage`
- 359 `GoldBunnyCage`
- 360 `GoldButterflyCage`
- 361 `GoldFrogCage`
- 362 `GoldGrasshopperCage`
- 363 `GoldMouseCage`
- 364 `GoldWormCage`
- 365 `SilkRope`
- 366 `WebRope`
- 367 `Marble`
- 368 `Granite`
- 369 `GraniteBlock`
- 370 `MeteoriteBrick`
- 371 `PinkSlimeBlock`
- 372 `PeaceCandle`
- 373 `WaterDrip`
- 374 `LavaDrip`
- 375 `HoneyDrip`
- 376 `FishingCrate`
- 377 `SharpeningStation`
- 378 `TargetDummy`
- 379 `Bubble`
- 380 `PlanterBox`
- 381 `LavaMoss`
- 382 `VineFlowers`
- 383 `LivingMahogany`
- 384 `LivingMahoganyLeaves`
- 385 `CrystalBlock`
- 386 `TrapdoorOpen`
- 387 `TrapdoorClosed`
- 388 `TallGateClosed`
- 389 `TallGateOpen`
- 390 `LavaLamp`
- 391 `CageEnchantedNightcrawler`
- 392 `CageBuggy`
- 393 `CageGrubby`
- 394 `CageSluggy`
- 395 `ItemFrame`
- 396 `Sandstone`
- 397 `HardenedSand`
- 398 `CorruptHardenedSand`
- 399 `CrimsonHardenedSand`
- 400 `CorruptSandstone`
- 401 `CrimsonSandstone`
- 402 `HallowHardenedSand`
- 403 `HallowSandstone`
- 404 `DesertFossil`
- 405 `Fireplace`
- 406 `Chimney`
- 407 `FossilOre`
- 408 `LunarOre`
- 409 `LunarBrick`
- 410 `LunarMonolith`
- 411 `Detonator`
- 412 `LunarCraftingStation`
- 413 `SquirrelOrangeCage`
- 414 `SquirrelGoldCage`
- 415 `LunarBlockSolar`
- 416 `LunarBlockVortex`
- 417 `LunarBlockNebula`
- 418 `LunarBlockStardust`
- 419 `LogicGateLamp`
- 420 `LogicGate`
- 421 `ConveyorBeltLeft`
- 422 `ConveyorBeltRight`
- 423 `LogicSensor`
- 424 `WirePipe`
- 425 `AnnouncementBox`
- 426 `TeamBlockRed`
- 427 `TeamBlockRedPlatform`
- 428 `WeightedPressurePlate`
- 429 `WireBulb`
- 430 `TeamBlockGreen`
- 431 `TeamBlockBlue`
- 432 `TeamBlockYellow`
- 433 `TeamBlockPink`
- 434 `TeamBlockWhite`
- 435 `TeamBlockGreenPlatform`
- 436 `TeamBlockBluePlatform`
- 437 `TeamBlockYellowPlatform`
- 438 `TeamBlockPinkPlatform`
- 439 `TeamBlockWhitePlatform`
- 440 `GemLocks`
- 441 `FakeContainers`
- 442 `ProjectilePressurePad`
- 443 `GeyserTrap`
- 444 `BeeHive`
- 445 `PixelBox`
- 446 `SillyBalloonPink`
- 447 `SillyBalloonPurple`
- 448 `SillyBalloonGreen`
- 449 `SillyStreamerBlue`
- 450 `SillyStreamerGreen`
- 451 `SillyStreamerPink`
- 452 `SillyBalloonMachine`
- 453 `SillyBalloonTile`
- 454 `Pigronata`
- 455 `PartyMonolith`
- 456 `PartyBundleOfBalloonTile`
- 457 `PartyPresent`
- 458 `SandFallBlock`
- 459 `SnowFallBlock`
- 460 `SnowCloud`
- 461 `SandDrip`
- 462 `DjinnLamp`
- 463 `DefendersForge`
- 464 `WarTable`
- 465 `WarTableBanner`
- 466 `ElderCrystalStand`
- 467 `Containers2`
- 468 `FakeContainers2`
- 469 `Tables2`
- 470 `DisplayDoll`
- 471 `WeaponsRack2`
- 472 `IronBrick`
- 473 `LeadBrick`
- 474 `LesionBlock`
- 475 `HatRack`
- 476 `GolfHole`
- 477 `GolfGrass`
- 478 `CrimstoneBrick`
- 479 `SmoothSandstone`
- 480 `BloodMoonMonolith`
- 481 `CrackedBlueDungeonBrick`
- 482 `CrackedGreenDungeonBrick`
- 483 `CrackedPinkDungeonBrick`
- 484 `RollingCactus`
- 485 `AntlionLarva`
- 486 `DrumSet`
- 487 `PicnicTable`
- 488 `FallenLog`
- 489 `PinWheel`
- 490 `WeatherVane`
- 491 `VoidVault`
- 492 `GolfGrassHallowed`
- 493 `GolfCupFlag`
- 494 `GolfTee`
- 495 `ShellPile`
- 496 `AntiPortalBlock`
- 497 `Toilets`
- 498 `Spider`
- 499 `LesionStation`
- 500 `SolarBrick`
- 501 `VortexBrick`
- 502 `NebulaBrick`
- 503 `StardustBrick`
- 504 `MysticSnakeRope`
- 505 `GoldGoldfishBowl`
- 506 `CatBast`
- 507 `GoldStarryGlassBlock`
- 508 `BlueStarryGlassBlock`
- 509 `VoidMonolith`
- 510 `ArrowSign`
- 511 `PaintedArrowSign`
- 512 `GreenMossBrick`
- 513 `BrownMossBrick`
- 514 `RedMossBrick`
- 515 `BlueMossBrick`
- 516 `PurpleMossBrick`
- 517 `LavaMossBrick`
- 518 `LilyPad`
- 519 `Cattail`
- 520 `FoodPlatter`
- 521 `BlackDragonflyJar`
- 522 `BlueDragonflyJar`
- 523 `GreenDragonflyJar`
- 524 `OrangeDragonflyJar`
- 525 `RedDragonflyJar`
- 526 `YellowDragonflyJar`
- 527 `GoldDragonflyJar`
- 528 `MushroomVines`
- 529 `SeaOats`
- 530 `OasisPlants`
- 531 `BoulderStatue`
- 532 `MaggotCage`
- 533 `RatCage`
- 534 `KryptonMoss`
- 535 `KryptonMossBrick`
- 536 `XenonMoss`
- 537 `XenonMossBrick`
- 538 `LadybugCage`
- 539 `ArgonMoss`
- 540 `ArgonMossBrick`
- 541 `EchoBlock`
- 542 `OwlCage`
- 543 `PupfishBowl`
- 544 `GoldLadybugCage`
- 545 `LawnFlamingo`
- 546 `Grate`
- 547 `PottedPlants1`
- 548 `PottedPlants2`
- 549 `Seaweed`
- 550 `TurtleCage`
- 551 `TurtleJungleCage`
- 552 `Sandcastles`
- 553 `GrebeCage`
- 554 `SeagullCage`
- 555 `WaterStriderCage`
- 556 `GoldWaterStriderCage`
- 557 `GrateClosed`
- 558 `SeahorseCage`
- 559 `GoldSeahorseCage`
- 560 `GolfTrophies`
- 561 `MarbleColumn`
- 562 `BambooBlock`
- 563 `LargeBambooBlock`
- 564 `PlasmaLamp`
- 565 `FogMachine`
- 566 `AmberStoneBlock`
- 567 `GardenGnome`
- 568 `PinkFairyJar`
- 569 `GreenFairyJar`
- 570 `BlueFairyJar`
- 571 `Bamboo`
- 572 `SoulBottles`
- 573 `TatteredWoodSign`
- 574 `BorealBeam`
- 575 `RichMahoganyBeam`
- 576 `GraniteColumn`
- 577 `SandstoneColumn`
- 578 `MushroomBeam`
- 579 `RockGolemHead`
- 580 `HellButterflyJar`
- 581 `LavaflyinaBottle`
- 582 `MagmaSnailCage`
- 583 `TreeTopaz`
- 584 `TreeAmethyst`
- 585 `TreeSapphire`
- 586 `TreeEmerald`
- 587 `TreeRuby`
- 588 `TreeDiamond`
- 589 `TreeAmber`
- 590 `GemSaplings`
- 591 `PotsSuspended`
- 592 `BrazierSuspended`
- 593 `VolcanoSmall`
- 594 `VolcanoLarge`
- 595 `VanityTreeSakuraSaplings`
- 596 `VanityTreeSakura`
- 597 `TeleportationPylon`
- 598 `LavafishBowl`
- 599 `AmethystBunnyCage`
- 600 `TopazBunnyCage`
- 601 `SapphireBunnyCage`
- 602 `EmeraldBunnyCage`
- 603 `RubyBunnyCage`
- 604 `DiamondBunnyCage`
- 605 `AmberBunnyCage`
- 606 `AmethystSquirrelCage`
- 607 `TopazSquirrelCage`
- 608 `SapphireSquirrelCage`
- 609 `EmeraldSquirrelCage`
- 610 `RubySquirrelCage`
- 611 `DiamondSquirrelCage`
- 612 `AmberSquirrelCage`
- 613 `PottedLavaPlants`
- 614 `PottedLavaPlantTendrils`
- 615 `VanityTreeWillowSaplings`
- 616 `VanityTreeYellowWillow`
- 617 `MasterTrophyBase`
- 618 `AccentSlab`
- 619 `TruffleWormCage`
- 620 `EmpressButterflyJar`
- 621 `SliceOfCake`
- 622 `TeaKettle`
- 623 `PottedCrystalPlants`
- 624 `AbigailsFlower`
- 625 `VioletMoss`
- 626 `VioletMossBrick`
- 627 `RainbowMoss`
- 628 `RainbowMossBrick`
- 629 `StinkbugCage`
- 630 `StinkbugHousingBlocker`
- 631 `StinkbugHousingBlockerEcho`
- 632 `ScarletMacawCage`
- 633 `AshGrass`
- 634 `TreeAsh`
- 635 `AshWood`
- 636 `CorruptVines`
- 637 `AshPlants`
- 638 `AshVines`
- 639 `ManaCrystal`
- 640 `BlueMacawCage`
- 641 `ReefBlock`
- 642 `ChlorophyteExtractinator`
- 643 `ToucanCage`
- 644 `YellowCockatielCage`
- 645 `GrayCockatielCage`
- 646 `ShadowCandle`
- 647 `LargePilesEcho`
- 648 `LargePiles2Echo`
- 649 `SmallPiles2x1Echo`
- 650 `SmallPiles1x1Echo`
- 651 `PlantDetritus3x2Echo`
- 652 `PlantDetritus2x2Echo`
- 653 `PotsEcho`
- 654 `TNTBarrel`
- 655 `PlanteraThorns`
- 656 `GlowTulip`
- 657 `EchoMonolith`
- 658 `ShimmerMonolith`
- 659 `ShimmerBlock`
- 660 `ShimmerflyinaBottle`
- 661 `CorruptJungleGrass`
- 662 `CrimsonJungleGrass`
- 663 `Moondial`
- 664 `BouncyBoulder`
- 665 `LifeCrystalBoulder`
- 666 `PoopBlock`
- 667 `ShimmerBrick`
- 668 `DirtiestBlock`
- 669 `LunarRustBrick`
- 670 `DarkCelestialBrick`
- 671 `AstraBrick`
- 672 `CosmicEmberBrick`
- 673 `CryocoreBrick`
- 674 `MercuryBrick`
- 675 `StarRoyaleBrick`
- 676 `HeavenforgeBrick`
- 677 `AncientBlueBrick`
- 678 `AncientGreenBrick`
- 679 `AncientPinkBrick`
- 680 `AncientGoldBrick`
- 681 `AncientSilverBrick`
- 682 `AncientCopperBrick`
- 683 `AncientObsidianBrick`
- 684 `AncientHellstoneBrick`
- 685 `AncientCobaltBrick`
- 686 `AncientMythrilBrick`
- 687 `LavaMossBlock`
- 688 `ArgonMossBlock`
- 689 `KryptonMossBlock`
- 690 `XenonMossBlock`
- 691 `VioletMossBlock`
- 692 `RainbowMossBlock`

## 11. 背景墙枚举全表

> 共 347 条。

- 0 `None`
- 1 `Stone`
- 2 `DirtUnsafe`
- 3 `EbonstoneUnsafe`
- 4 `Wood`
- 5 `GrayBrick`
- 6 `RedBrick`
- 7 `BlueDungeonUnsafe`
- 8 `GreenDungeonUnsafe`
- 9 `PinkDungeonUnsafe`
- 10 `GoldBrick`
- 11 `SilverBrick`
- 12 `CopperBrick`
- 13 `HellstoneBrickUnsafe`
- 14 `ObsidianBrickUnsafe`
- 15 `MudUnsafe`
- 16 `Dirt`
- 17 `BlueDungeon`
- 18 `GreenDungeon`
- 19 `PinkDungeon`
- 20 `ObsidianBrick`
- 21 `Glass`
- 22 `PearlstoneBrick`
- 23 `IridescentBrick`
- 24 `MudstoneBrick`
- 25 `CobaltBrick`
- 26 `MythrilBrick`
- 27 `Planked`
- 28 `PearlstoneBrickUnsafe`
- 29 `CandyCane`
- 30 `GreenCandyCane`
- 31 `SnowBrick`
- 32 `AdamantiteBeam`
- 33 `DemoniteBrick`
- 34 `SandstoneBrick`
- 35 `EbonstoneBrick`
- 36 `RedStucco`
- 37 `YellowStucco`
- 38 `GreenStucco`
- 39 `Gray`
- 40 `SnowWallUnsafe`
- 41 `Ebonwood`
- 42 `RichMaogany`
- 43 `Pearlwood`
- 44 `RainbowBrick`
- 45 `TinBrick`
- 46 `TungstenBrick`
- 47 `PlatinumBrick`
- 48 `AmethystUnsafe`
- 49 `TopazUnsafe`
- 50 `SapphireUnsafe`
- 51 `EmeraldUnsafe`
- 52 `RubyUnsafe`
- 53 `DiamondUnsafe`
- 54 `CaveUnsafe`
- 55 `Cave2Unsafe`
- 56 `Cave3Unsafe`
- 57 `Cave4Unsafe`
- 58 `Cave5Unsafe`
- 59 `Cave6Unsafe`
- 60 `LivingLeaf`
- 61 `Cave7Unsafe`
- 62 `SpiderUnsafe`
- 63 `GrassUnsafe`
- 64 `JungleUnsafe`
- 65 `FlowerUnsafe`
- 66 `Grass`
- 67 `Jungle`
- 68 `Flower`
- 69 `CorruptGrassUnsafe`
- 70 `HallowedGrassUnsafe`
- 71 `IceUnsafe`
- 72 `Cactus`
- 73 `Cloud`
- 74 `Mushroom`
- 75 `Bone`
- 76 `Slime`
- 77 `Flesh`
- 78 `LivingWood`
- 79 `ObsidianBackUnsafe`
- 80 `MushroomUnsafe`
- 81 `CrimsonGrassUnsafe`
- 82 `DiscWall`
- 83 `CrimstoneUnsafe`
- 84 `IceBrick`
- 85 `Shadewood`
- 86 `HiveUnsafe`
- 87 `LihzahrdBrickUnsafe`
- 88 `PurpleStainedGlass`
- 89 `YellowStainedGlass`
- 90 `BlueStainedGlass`
- 91 `GreenStainedGlass`
- 92 `RedStainedGlass`
- 93 `RainbowStainedGlass`
- 94 `BlueDungeonSlabUnsafe`
- 95 `BlueDungeonTileUnsafe`
- 96 `PinkDungeonSlabUnsafe`
- 97 `PinkDungeonTileUnsafe`
- 98 `GreenDungeonSlabUnsafe`
- 99 `GreenDungeonTileUnsafe`
- 100 `BlueDungeonSlab`
- 101 `BlueDungeonTile`
- 102 `PinkDungeonSlab`
- 103 `PinkDungeonTile`
- 104 `GreenDungeonSlab`
- 105 `GreenDungeonTile`
- 106 `WoodenFence`
- 107 `MetalFence`
- 108 `Hive`
- 109 `PalladiumColumn`
- 110 `BubblegumBlock`
- 111 `TitanstoneBlock`
- 112 `LihzahrdBrick`
- 113 `Pumpkin`
- 114 `Hay`
- 115 `SpookyWood`
- 116 `ChristmasTreeWallpaper`
- 117 `OrnamentWallpaper`
- 118 `CandyCaneWallpaper`
- 119 `FestiveWallpaper`
- 120 `StarsWallpaper`
- 121 `SquigglesWallpaper`
- 122 `SnowflakeWallpaper`
- 123 `KrampusHornWallpaper`
- 124 `BluegreenWallpaper`
- 125 `GrinchFingerWallpaper`
- 126 `FancyGrayWallpaper`
- 127 `IceFloeWallpaper`
- 128 `MusicWallpaper`
- 129 `PurpleRainWallpaper`
- 130 `RainbowWallpaper`
- 131 `SparkleStoneWallpaper`
- 132 `StarlitHeavenWallpaper`
- 133 `BubbleWallpaper`
- 134 `CopperPipeWallpaper`
- 135 `DuckyWallpaper`
- 136 `Waterfall`
- 137 `Lavafall`
- 138 `EbonwoodFence`
- 139 `RichMahoganyFence`
- 140 `PearlwoodFence`
- 141 `ShadewoodFence`
- 142 `WhiteDynasty`
- 143 `BlueDynasty`
- 144 `ArcaneRunes`
- 145 `IronFence`
- 146 `CopperPlating`
- 147 `StoneSlab`
- 148 `Sail`
- 149 `BorealWood`
- 150 `BorealWoodFence`
- 151 `PalmWood`
- 152 `PalmWoodFence`
- 153 `AmberGemspark`
- 154 `AmethystGemspark`
- 155 `DiamondGemspark`
- 156 `EmeraldGemspark`
- 157 `AmberGemsparkOff`
- 158 `AmethystGemsparkOff`
- 159 `DiamondGemsparkOff`
- 160 `EmeraldGemsparkOff`
- 161 `RubyGemsparkOff`
- 162 `SapphireGemsparkOff`
- 163 `TopazGemsparkOff`
- 164 `RubyGemspark`
- 165 `SapphireGemspark`
- 166 `TopazGemspark`
- 167 `TinPlating`
- 168 `Confetti`
- 169 `ConfettiBlack`
- 170 `CaveWall`
- 171 `CaveWall2`
- 172 `Honeyfall`
- 173 `ChlorophyteBrick`
- 174 `CrimtaneBrick`
- 175 `ShroomitePlating`
- 176 `MartianConduit`
- 177 `HellstoneBrick`
- 178 `MarbleUnsafe`
- 179 `MarbleBlock`
- 180 `GraniteUnsafe`
- 181 `GraniteBlock`
- 182 `MeteoriteBrick`
- 183 `Marble`
- 184 `Granite`
- 185 `Cave8Unsafe`
- 186 `Crystal`
- 187 `Sandstone`
- 188 `CorruptionUnsafe1`
- 189 `CorruptionUnsafe2`
- 190 `CorruptionUnsafe3`
- 191 `CorruptionUnsafe4`
- 192 `CrimsonUnsafe1`
- 193 `CrimsonUnsafe2`
- 194 `CrimsonUnsafe3`
- 195 `CrimsonUnsafe4`
- 196 `DirtUnsafe1`
- 197 `DirtUnsafe2`
- 198 `DirtUnsafe3`
- 199 `DirtUnsafe4`
- 200 `HallowUnsafe1`
- 201 `HallowUnsafe2`
- 202 `HallowUnsafe3`
- 203 `HallowUnsafe4`
- 204 `JungleUnsafe1`
- 205 `JungleUnsafe2`
- 206 `JungleUnsafe3`
- 207 `JungleUnsafe4`
- 208 `LavaUnsafe1`
- 209 `LavaUnsafe2`
- 210 `LavaUnsafe3`
- 211 `LavaUnsafe4`
- 212 `RocksUnsafe1`
- 213 `RocksUnsafe2`
- 214 `RocksUnsafe3`
- 215 `RocksUnsafe4`
- 216 `HardenedSand`
- 217 `CorruptHardenedSand`
- 218 `CrimsonHardenedSand`
- 219 `HallowHardenedSand`
- 220 `CorruptSandstone`
- 221 `CrimsonSandstone`
- 222 `HallowSandstone`
- 223 `DesertFossil`
- 224 `LunarBrickWall`
- 225 `CogWall`
- 226 `SandFall`
- 227 `SnowFall`
- 228 `SillyBalloonPinkWall`
- 229 `SillyBalloonPurpleWall`
- 230 `SillyBalloonGreenWall`
- 231 `IronBrick`
- 232 `LeadBrick`
- 233 `LesionBlock`
- 234 `CrimstoneBrick`
- 235 `SmoothSandstone`
- 236 `Spider`
- 237 `SolarBrick`
- 238 `VortexBrick`
- 239 `NebulaBrick`
- 240 `StardustBrick`
- 241 `OrangeStainedGlass`
- 242 `GoldStarryGlassWall`
- 243 `BlueStarryGlassWall`
- 244 `LivingWoodUnsafe`
- 245 `WroughtIronFence`
- 246 `EbonstoneEcho`
- 247 `MudWallEcho`
- 248 `PearlstoneEcho`
- 249 `SnowWallEcho`
- 250 `AmethystEcho`
- 251 `TopazEcho`
- 252 `SapphireEcho`
- 253 `EmeraldEcho`
- 254 `RubyEcho`
- 255 `DiamondEcho`
- 256 `Cave1Echo`
- 257 `Cave2Echo`
- 258 `Cave3Echo`
- 259 `Cave4Echo`
- 260 `Cave5Echo`
- 261 `Cave6Echo`
- 262 `Cave7Echo`
- 263 `SpiderEcho`
- 264 `CorruptGrassEcho`
- 265 `HallowedGrassEcho`
- 266 `IceEcho`
- 267 `ObsidianBackEcho`
- 268 `CrimsonGrassEcho`
- 269 `CrimstoneEcho`
- 270 `CaveWall1Echo`
- 271 `CaveWall2Echo`
- 272 `MarbleEchoUnused`
- 273 `GraniteEchoUnused`
- 274 `Cave8Echo`
- 275 `SandstoneEcho`
- 276 `Corruption1Echo`
- 277 `Corruption2Echo`
- 278 `Corruption3Echo`
- 279 `Corruption4Echo`
- 280 `Crimson1Echo`
- 281 `Crimson2Echo`
- 282 `Crimson3Echo`
- 283 `Crimson4Echo`
- 284 `Dirt1Echo`
- 285 `Dirt2Echo`
- 286 `Dirt3Echo`
- 287 `Dirt4Echo`
- 288 `Hallow1Echo`
- 289 `Hallow2Echo`
- 290 `Hallow3Echo`
- 291 `Hallow4Echo`
- 292 `Jungle1Echo`
- 293 `Jungle2Echo`
- 294 `Jungle3Echo`
- 295 `Jungle4Echo`
- 296 `Lava1Echo`
- 297 `Lava2Echo`
- 298 `Lava3Echo`
- 299 `Lava4Echo`
- 300 `Rocks1Echo`
- 301 `Rocks2Echo`
- 302 `Rocks3Echo`
- 303 `Rocks4Echo`
- 304 `HardenedSandEcho`
- 305 `CorruptHardenedSandEcho`
- 306 `CrimsonHardenedSandEcho`
- 307 `HallowHardenedSandEcho`
- 308 `CorruptSandstoneEcho`
- 309 `CrimsonSandstoneEcho`
- 310 `HallowSandstoneEcho`
- 311 `DesertFossilEcho`
- 312 `BambooBlockWall`
- 313 `LargeBambooBlockWall`
- 314 `AmberStoneWallEcho`
- 315 `BambooFence`
- 316 `AshWood`
- 317 `AshWoodFence`
- 318 `EchoWall`
- 319 `ReefWall`
- 320 `PoopWall`
- 321 `ShimmerBlockWall`
- 322 `ShimmerBrickWall`
- 323 `LunarRustBrickWall`
- 324 `DarkCelestialBrickWall`
- 325 `AstraBrickWall`
- 326 `CosmicEmberBrickWall`
- 327 `CryocoreBrickWall`
- 328 `MercuryBrickWall`
- 329 `StarRoyaleBrickWall`
- 330 `HeavenforgeBrickWall`
- 331 `AncientBlueBrickWall`
- 332 `AncientGreenBrickWall`
- 333 `AncientPinkBrickWall`
- 334 `AncientGoldBrickWall`
- 335 `AncientSilverBrickWall`
- 336 `AncientCopperBrickWall`
- 337 `AncientObsidianBrickWall`
- 338 `AncientHellstoneBrickWall`
- 339 `AncientCobaltBrickWall`
- 340 `AncientMythrilBrickWall`
- 341 `LavaMossBlockWall`
- 342 `ArgonMossBlockWall`
- 343 `KryptonMossBlockWall`
- 344 `XenonMossBlockWall`
- 345 `VioletMossBlockWall`
- 346 `RainbowMossBlockWall`

## 12. 完整物品表（全量）

> 共 5513 条，按物品 id 升序。`放置物块` 非空表示放下后该格会变成对应物块 id。这张表就是"游戏里一共有哪些 item id"的权威答案，离线可查，不必进游戏试。

| 物品 ID | 枚举名 | 中文名 | 放置物块 | 墙 | 镐/斧/锤 | 伤害 |
|---|---|---|---|---|---|---|
| 1 | `IronPickaxe` | 铁镐 | — | — | 40/·/· | 5 |
| 2 | `DirtBlock` | 土块 | — | — | ·/·/· | — |
| 3 | `StoneBlock` | 石块 | 1 | — | ·/·/· | — |
| 4 | `IronBroadsword` | 铁阔剑 | — | — | ·/·/· | 12 |
| 5 | `Mushroom` | 蘑菇 | — | — | ·/·/· | — |
| 6 | `IronShortsword` | 铁短剑 | — | — | ·/·/· | 8 |
| 7 | `IronHammer` | 铁锤 | — | — | ·/·/40 | 7 |
| 8 | `Torch` | 火把 | 4 | — | ·/·/· | — |
| 9 | `Wood` | 木材 | 30 | — | ·/·/· | — |
| 10 | `IronAxe` | 铁斧 | — | — | ·/9/· | 5 |
| 11 | `IronOre` | 铁矿 | 6 | — | ·/·/· | — |
| 12 | `CopperOre` | 铜矿 | 7 | — | ·/·/· | — |
| 13 | `GoldOre` | 金矿 | 8 | — | ·/·/· | — |
| 14 | `SilverOre` | 银矿 | 9 | — | ·/·/· | — |
| 15 | `CopperWatch` | 铜表 | — | — | ·/·/· | — |
| 16 | `SilverWatch` | 银表 | — | — | ·/·/· | — |
| 17 | `GoldWatch` | 金表 | — | — | ·/·/· | — |
| 18 | `DepthMeter` | 深度计 | — | — | ·/·/· | — |
| 19 | `GoldBar` | 金锭 | 239 | — | ·/·/· | — |
| 20 | `CopperBar` | 铜锭 | 239 | — | ·/·/· | — |
| 21 | `SilverBar` | 银锭 | 239 | — | ·/·/· | — |
| 22 | `IronBar` | 铁锭 | 239 | — | ·/·/· | — |
| 23 | `Gel` | 凝胶 | — | — | ·/·/· | — |
| 24 | `WoodenSword` | 木剑 | — | — | ·/·/· | 7 |
| 25 | `WoodenDoor` | 木门 | 10 | — | ·/·/· | — |
| 26 | `StoneWall` | 石墙 | — | 1 | ·/·/· | — |
| 27 | `Acorn` | 橡实 | 20 | — | ·/·/· | — |
| 28 | `LesserHealingPotion` | 弱效治疗药水 | — | — | ·/·/· | — |
| 29 | `LifeCrystal` | 生命水晶 | — | — | ·/·/· | — |
| 30 | `DirtWall` | 土墙 | — | 16 | ·/·/· | — |
| 31 | `Bottle` | 玻璃瓶 | 13 | — | ·/·/· | — |
| 32 | `WoodenTable` | 木桌 | 14 | — | ·/·/· | — |
| 33 | `Furnace` | 熔炉 | 17 | — | ·/·/· | — |
| 34 | `WoodenChair` | 木椅 | 15 | — | ·/·/· | — |
| 35 | `IronAnvil` | 铁砧 | 16 | — | ·/·/· | — |
| 36 | `WorkBench` | 工作台 | 18 | — | ·/·/· | — |
| 37 | `Goggles` | 护目镜 | — | — | ·/·/· | — |
| 38 | `Lens` | 晶状体 | — | — | ·/·/· | — |
| 39 | `WoodenBow` | 木弓 | — | — | ·/·/· | 4 |
| 40 | `WoodenArrow` | 木箭 | — | — | ·/·/· | 5 |
| 41 | `FlamingArrow` | 烈焰箭 | — | — | ·/·/· | 7 |
| 42 | `Shuriken` | 手里剑 | — | — | ·/·/· | 10 |
| 43 | `SuspiciousLookingEye` | 可疑眼球 | — | — | ·/·/· | — |
| 44 | `DemonBow` | 恶魔弓 | — | — | ·/·/· | 14 |
| 45 | `WarAxeoftheNight` | 暗夜战斧 | — | — | ·/15/· | 20 |
| 46 | `LightsBane` | 魔光剑 | — | — | ·/·/· | 16 |
| 47 | `UnholyArrow` | 邪箭 | — | — | ·/·/· | 12 |
| 48 | `Chest` | 宝箱 | 21 | — | ·/·/· | — |
| 49 | `BandofRegeneration` | 再生手环 | — | — | ·/·/· | — |
| 50 | `MagicMirror` | 魔镜 | — | — | ·/·/· | — |
| 51 | `JestersArrow` | 小丑之箭 | — | — | ·/·/· | 10 |
| 52 | `AngelStatue` | 天使雕像 | 105 | — | ·/·/· | — |
| 53 | `CloudinaBottle` | 云朵瓶 | — | — | ·/·/· | — |
| 54 | `HermesBoots` | 赫尔墨斯靴 | — | — | ·/·/· | — |
| 55 | `EnchantedBoomerang` | 附魔回旋镖 | — | — | ·/·/· | 17 |
| 56 | `DemoniteOre` | 魔矿 | 22 | — | ·/·/· | — |
| 57 | `DemoniteBar` | 魔矿锭 | 239 | — | ·/·/· | — |
| 58 | `Heart` | 心 | — | — | ·/·/· | — |
| 59 | `CorruptSeeds` | 腐化种子 | 23 | — | ·/·/· | — |
| 60 | `VileMushroom` | 魔菇 | — | — | ·/·/· | — |
| 61 | `EbonstoneBlock` | 黑檀石块 | 25 | — | ·/·/· | — |
| 62 | `GrassSeeds` | 草种子 | 2 | — | ·/·/· | — |
| 63 | `Sunflower` | 向日葵 | 27 | — | ·/·/· | — |
| 64 | `Vilethorn` | 魔刺 | — | — | ·/·/· | 10 |
| 65 | `Starfury` | 星怒 | — | — | ·/·/· | 25 |
| 66 | `PurificationPowder` | 净化粉 | — | — | ·/·/· | — |
| 67 | `VilePowder` | 魔粉 | — | — | ·/·/· | — |
| 68 | `RottenChunk` | 腐肉 | — | — | ·/·/· | — |
| 69 | `WormTooth` | 蠕虫毒牙 | — | — | ·/·/· | — |
| 70 | `WormFood` | 蠕虫诱饵 | — | — | ·/·/· | — |
| 71 | `CopperCoin` | 铜币 | 330 | — | ·/·/· | 25 |
| 72 | `SilverCoin` | 银币 | 331 | — | ·/·/· | 50 |
| 73 | `GoldCoin` | 金币 | 332 | — | ·/·/· | 100 |
| 74 | `PlatinumCoin` | 铂金币 | 333 | — | ·/·/· | 200 |
| 75 | `FallenStar` | 坠落之星 | — | — | ·/·/· | — |
| 76 | `CopperGreaves` | 铜护胫 | — | — | ·/·/· | — |
| 77 | `IronGreaves` | 铁护胫 | — | — | ·/·/· | — |
| 78 | `SilverGreaves` | 银护胫 | — | — | ·/·/· | — |
| 79 | `GoldGreaves` | 金护胫 | — | — | ·/·/· | — |
| 80 | `CopperChainmail` | 铜链甲 | — | — | ·/·/· | — |
| 81 | `IronChainmail` | 铁链甲 | — | — | ·/·/· | — |
| 82 | `SilverChainmail` | 银链甲 | — | — | ·/·/· | — |
| 83 | `GoldChainmail` | 金链甲 | — | — | ·/·/· | — |
| 84 | `GrapplingHook` | 抓钩 | — | — | ·/·/· | — |
| 85 | `Chain` | 链条 | 214 | — | ·/·/· | — |
| 86 | `ShadowScale` | 暗影鳞片 | — | — | ·/·/· | — |
| 87 | `PiggyBank` | 猪猪存钱罐 | 29 | — | ·/·/· | — |
| 88 | `MiningHelmet` | 挖矿头盔 | — | — | ·/·/· | — |
| 89 | `CopperHelmet` | 铜头盔 | — | — | ·/·/· | — |
| 90 | `IronHelmet` | 铁头盔 | — | — | ·/·/· | — |
| 91 | `SilverHelmet` | 银头盔 | — | — | ·/·/· | — |
| 92 | `GoldHelmet` | 金头盔 | — | — | ·/·/· | — |
| 93 | `WoodWall` | 木墙 | — | 4 | ·/·/· | — |
| 94 | `WoodPlatform` | 木平台 | 19 | — | ·/·/· | — |
| 95 | `FlintlockPistol` | 燧发枪 | — | — | ·/·/· | 13 |
| 96 | `Musket` | 火枪 | — | — | ·/·/· | 31 |
| 97 | `MusketBall` | 火枪子弹 | — | — | ·/·/· | 7 |
| 98 | `Minishark` | 迷你鲨 | — | — | ·/·/· | 6 |
| 99 | `IronBow` | 铁弓 | — | — | ·/·/· | 8 |
| 100 | `ShadowGreaves` | 暗影护胫 | — | — | ·/·/· | — |
| 101 | `ShadowScalemail` | 暗影鳞甲 | — | — | ·/·/· | — |
| 102 | `ShadowHelmet` | 暗影头盔 | — | — | ·/·/· | — |
| 103 | `NightmarePickaxe` | 梦魇镐 | — | — | 65/·/· | 9 |
| 104 | `TheBreaker` | 魔锤 | — | — | ·/·/55 | 24 |
| 105 | `Candle` | 蜡烛 | 33 | — | ·/·/· | — |
| 106 | `CopperChandelier` | 铜吊灯 | 34 | — | ·/·/· | — |
| 107 | `SilverChandelier` | 银吊灯 | 34 | — | ·/·/· | — |
| 108 | `GoldChandelier` | 金吊灯 | 34 | — | ·/·/· | — |
| 109 | `ManaCrystal` | 魔力水晶 | — | — | ·/·/· | — |
| 110 | `LesserManaPotion` | 弱效魔力药水 | — | — | ·/·/· | — |
| 111 | `BandofStarpower` | 星力手环 | — | — | ·/·/· | — |
| 112 | `FlowerofFire` | 火之花 | — | — | ·/·/· | 48 |
| 113 | `MagicMissile` | 魔法导弹 | — | — | ·/·/· | 35 |
| 114 | `DirtRod` | 土魔杖 | — | — | ·/·/· | — |
| 115 | `ShadowOrb` | 暗影珠 | — | — | ·/·/· | — |
| 116 | `Meteorite` | 陨石 | 37 | — | ·/·/· | — |
| 117 | `MeteoriteBar` | 陨石锭 | 239 | — | ·/·/· | — |
| 118 | `Hook` | 爪钩 | — | — | ·/·/· | — |
| 119 | `Flamarang` | 烈焰回旋镖 | — | — | ·/·/· | 49 |
| 120 | `MoltenFury` | 熔火之怒 | — | — | ·/·/· | 31 |
| 121 | `FieryGreatsword` | 火山 | — | — | ·/·/· | 40 |
| 122 | `MoltenPickaxe` | 熔岩镐 | — | — | 100/·/· | 12 |
| 123 | `MeteorHelmet` | 流星头盔 | — | — | ·/·/· | — |
| 124 | `MeteorSuit` | 流星护甲 | — | — | ·/·/· | — |
| 125 | `MeteorLeggings` | 流星护腿 | — | — | ·/·/· | — |
| 126 | `BottledWater` | 水瓶 | — | — | ·/·/· | — |
| 127 | `SpaceGun` | 太空枪 | — | — | ·/·/· | 17 |
| 128 | `RocketBoots` | 火箭靴 | — | — | ·/·/· | — |
| 129 | `GrayBrick` | 灰砖 | 38 | — | ·/·/· | — |
| 130 | `GrayBrickWall` | 灰砖墙 | — | 5 | ·/·/· | — |
| 131 | `RedBrick` | 红砖 | 39 | — | ·/·/· | — |
| 132 | `RedBrickWall` | 红砖墙 | — | 6 | ·/·/· | — |
| 133 | `ClayBlock` | 粘土块 | 40 | — | ·/·/· | — |
| 134 | `BlueBrick` | 蓝砖 | 41 | — | ·/·/· | — |
| 135 | `BlueBrickWall` | 蓝砖墙 | — | 17 | ·/·/· | — |
| 136 | `ChainLantern` | 挂链灯笼 | 42 | — | ·/·/· | — |
| 137 | `GreenBrick` | 绿砖 | 43 | — | ·/·/· | — |
| 138 | `GreenBrickWall` | 绿砖墙 | — | 18 | ·/·/· | — |
| 139 | `PinkBrick` | 粉砖 | 44 | — | ·/·/· | — |
| 140 | `PinkBrickWall` | 粉砖墙 | — | 19 | ·/·/· | — |
| 141 | `GoldBrick` | 金砖 | 45 | — | ·/·/· | — |
| 142 | `GoldBrickWall` | 金砖墙 | — | 10 | ·/·/· | — |
| 143 | `SilverBrick` | 银砖 | 46 | — | ·/·/· | — |
| 144 | `SilverBrickWall` | 银砖墙 | — | 11 | ·/·/· | — |
| 145 | `CopperBrick` | 铜砖 | 47 | — | ·/·/· | — |
| 146 | `CopperBrickWall` | 铜砖墙 | — | 12 | ·/·/· | — |
| 147 | `Spike` | 斯派克 | 48 | — | ·/·/· | — |
| 148 | `WaterCandle` | 水蜡烛 | 49 | — | ·/·/· | — |
| 149 | `Book` | 书 | 50 | — | ·/·/· | — |
| 150 | `Cobweb` | 蛛网 | 51 | — | ·/·/· | — |
| 151 | `NecroHelmet` | 死灵头盔 | — | — | ·/·/· | — |
| 152 | `NecroBreastplate` | 死灵胸甲 | — | — | ·/·/· | — |
| 153 | `NecroGreaves` | 死灵护胫 | — | — | ·/·/· | — |
| 154 | `Bone` | 骨头 | — | — | ·/·/· | 20 |
| 155 | `Muramasa` | 村正大刀 | — | — | ·/·/· | 24 |
| 156 | `CobaltShield` | 钴护盾 | — | — | ·/·/· | — |
| 157 | `AquaScepter` | 海蓝权杖 | — | — | ·/·/· | 27 |
| 158 | `LuckyHorseshoe` | 幸运马掌 | — | — | ·/·/· | — |
| 159 | `ShinyRedBalloon` | 闪亮红气球 | — | — | ·/·/· | — |
| 160 | `Harpoon` | 鱼叉枪 | — | — | ·/·/· | 25 |
| 161 | `SpikyBall` | 尖球 | — | — | ·/·/· | 16 |
| 162 | `BallOHurt` | 链球 | — | — | ·/·/· | 15 |
| 163 | `BlueMoon` | 蓝月 | — | — | ·/·/· | 27 |
| 164 | `Handgun` | 手枪 | — | — | ·/·/· | 26 |
| 165 | `WaterBolt` | 水矢 | — | — | ·/·/· | 19 |
| 166 | `Bomb` | 炸弹 | — | — | ·/·/· | — |
| 167 | `Dynamite` | 雷管 | — | — | ·/·/· | — |
| 168 | `Grenade` | 手榴弹 | — | — | ·/·/· | 60 |
| 169 | `SandBlock` | 沙块 | 53 | — | ·/·/· | — |
| 170 | `Glass` | 玻璃 | 54 | — | ·/·/· | — |
| 171 | `Sign` | 标牌 | 55 | — | ·/·/· | — |
| 172 | `AshBlock` | 灰烬块 | 57 | — | ·/·/· | — |
| 173 | `Obsidian` | 黑曜石 | 56 | — | ·/·/· | — |
| 174 | `Hellstone` | 狱石 | 58 | — | ·/·/· | — |
| 175 | `HellstoneBar` | 狱石锭 | 239 | — | ·/·/· | — |
| 176 | `MudBlock` | 泥块 | 59 | — | ·/·/· | — |
| 177 | `Sapphire` | 萨菲 | 178 | — | ·/·/· | — |
| 178 | `Ruby` | 鲁比 | 178 | — | ·/·/· | — |
| 179 | `Emerald` | 翡翠 | 178 | — | ·/·/· | — |
| 180 | `Topaz` | 黄玉 | 178 | — | ·/·/· | — |
| 181 | `Amethyst` | 紫晶 | 178 | — | ·/·/· | — |
| 182 | `Diamond` | 钻石 | 178 | — | ·/·/· | — |
| 183 | `GlowingMushroom` | 发光蘑菇 | 190 | — | ·/·/· | — |
| 184 | `Star` | 星星 | — | — | ·/·/· | — |
| 185 | `IvyWhip` | 常春藤鞭 | — | — | ·/·/· | — |
| 186 | `BreathingReed` | 芦苇呼吸管 | — | — | ·/·/· | 10 |
| 187 | `Flipper` | 脚蹼 | — | — | ·/·/· | — |
| 188 | `HealingPotion` | 治疗药水 | — | — | ·/·/· | — |
| 189 | `ManaPotion` | 魔力药水 | — | — | ·/·/· | — |
| 190 | `BladeofGrass` | 草剑 | — | — | ·/·/· | 18 |
| 191 | `ThornChakram` | 荆棘旋刃 | — | — | ·/·/· | 25 |
| 192 | `ObsidianBrick` | 黑曜石砖 | 75 | — | ·/·/· | — |
| 193 | `ObsidianSkull` | 黑曜石骷髅头 | — | — | ·/·/· | — |
| 194 | `MushroomGrassSeeds` | 蘑菇草种子 | 70 | — | ·/·/· | — |
| 195 | `JungleGrassSeeds` | 丛林草种子 | 60 | — | ·/·/· | — |
| 196 | `WoodenHammer` | 木锤 | — | — | ·/·/25 | 2 |
| 197 | `StarCannon` | 星星炮 | — | — | ·/·/· | 55 |
| 198 | `BluePhaseblade` | 蓝陨石光剑 | — | — | ·/·/· | 26 |
| 199 | `RedPhaseblade` | 红陨石光剑 | — | — | ·/·/· | 26 |
| 200 | `GreenPhaseblade` | 绿陨石光剑 | — | — | ·/·/· | 26 |
| 201 | `PurplePhaseblade` | 紫陨石光剑 | — | — | ·/·/· | 26 |
| 202 | `WhitePhaseblade` | 白陨石光剑 | — | — | ·/·/· | 26 |
| 203 | `YellowPhaseblade` | 黄陨石光剑 | — | — | ·/·/· | 26 |
| 204 | `MeteorHamaxe` | 流星锤斧 | — | — | ·/20/60 | 20 |
| 205 | `EmptyBucket` | 空桶 | — | — | ·/·/· | — |
| 206 | `WaterBucket` | 水桶 | — | — | ·/·/· | — |
| 207 | `LavaBucket` | 熔岩桶 | — | — | ·/·/· | — |
| 208 | `JungleRose` | 丛林玫瑰 | — | — | ·/·/· | — |
| 209 | `Stinger` | 毒刺 | — | — | ·/·/· | — |
| 210 | `Vine` | 藤蔓 | — | — | ·/·/· | — |
| 211 | `FeralClaws` | 猛爪手套 | — | — | ·/·/· | — |
| 212 | `AnkletoftheWind` | 疾风脚镯 | — | — | ·/·/· | — |
| 213 | `StaffofRegrowth` | 再生法杖 | 2 | — | ·/·/· | 7 |
| 214 | `HellstoneBrick` | 狱石砖 | 76 | — | ·/·/· | — |
| 215 | `WhoopieCushion` | 整蛊坐垫 | — | — | ·/·/· | — |
| 216 | `Shackle` | 脚镣 | — | — | ·/·/· | — |
| 217 | `MoltenHamaxe` | 熔岩锤斧 | — | — | ·/30/70 | 20 |
| 218 | `Flamelash` | 烈焰火鞭 | — | — | ·/·/· | 32 |
| 219 | `PhoenixBlaster` | 凤凰爆破枪 | — | — | ·/·/· | 30 |
| 220 | `Sunfury` | 阳炎之怒 | — | — | ·/·/· | 32 |
| 221 | `Hellforge` | 地狱熔炉 | 77 | — | ·/·/· | — |
| 222 | `ClayPot` | 粘土盆 | 78 | — | ·/·/· | — |
| 223 | `NaturesGift` | 大自然的恩赐 | — | — | ·/·/· | — |
| 224 | `Bed` | 床 | 79 | — | ·/·/· | — |
| 225 | `Silk` | 丝绸 | — | — | ·/·/· | — |
| 227 | `RestorationPotion` | 恢复药水 | — | — | ·/·/· | — |
| 228 | `JungleHat` | 丛林帽 | — | — | ·/·/· | — |
| 229 | `JungleShirt` | 丛林衣 | — | — | ·/·/· | — |
| 230 | `JunglePants` | 丛林裤 | — | — | ·/·/· | — |
| 231 | `MoltenHelmet` | 熔岩头盔 | — | — | ·/·/· | — |
| 232 | `MoltenBreastplate` | 熔岩胸甲 | — | — | ·/·/· | — |
| 233 | `MoltenGreaves` | 熔岩护胫 | — | — | ·/·/· | — |
| 234 | `MeteorShot` | 流星弹 | — | — | ·/·/· | 8 |
| 235 | `StickyBomb` | 粘性炸弹 | — | — | ·/·/· | — |
| 236 | `BlackLens` | 黑晶状体 | — | — | ·/·/· | — |
| 237 | `Sunglasses` | 墨镜 | — | — | ·/·/· | — |
| 238 | `WizardHat` | 巫师帽 | — | — | ·/·/· | — |
| 239 | `TopHat` | 高顶礼帽 | — | — | ·/·/· | — |
| 240 | `TuxedoShirt` | 西装衣 | — | — | ·/·/· | — |
| 241 | `TuxedoPants` | 西装裤 | — | — | ·/·/· | — |
| 242 | `SummerHat` | 夏日草帽 | — | — | ·/·/· | — |
| 243 | `BunnyHood` | 兔兔兜帽 | — | — | ·/·/· | — |
| 244 | `PlumbersHat` | 管道工帽 | — | — | ·/·/· | — |
| 245 | `PlumbersShirt` | 管道工衣 | — | — | ·/·/· | — |
| 246 | `PlumbersPants` | 管道工背带裤 | — | — | ·/·/· | — |
| 247 | `HerosHat` | 英雄帽 | — | — | ·/·/· | — |
| 248 | `HerosShirt` | 英雄衣 | — | — | ·/·/· | — |
| 249 | `HerosPants` | 英雄裤 | — | — | ·/·/· | — |
| 250 | `FishBowl` | 鱼缸 | 282 | — | ·/·/· | — |
| 251 | `ArchaeologistsHat` | 考古帽 | — | — | ·/·/· | — |
| 252 | `ArchaeologistsJacket` | 考古夹克 | — | — | ·/·/· | — |
| 253 | `ArchaeologistsPants` | 考古裤 | — | — | ·/·/· | — |
| 254 | `BlackThread` | 黑线 | — | — | ·/·/· | — |
| 255 | `GreenThread` | 绿线 | — | — | ·/·/· | — |
| 256 | `NinjaHood` | 忍者兜帽 | — | — | ·/·/· | — |
| 257 | `NinjaShirt` | 忍者衣 | — | — | ·/·/· | — |
| 258 | `NinjaPants` | 忍者裤 | — | — | ·/·/· | — |
| 259 | `Leather` | 皮革 | — | — | ·/·/· | — |
| 260 | `RedHat` | 红帽 | — | — | ·/·/· | — |
| 261 | `Goldfish` | 金鱼 | — | — | ·/·/· | — |
| 262 | `Robe` | 长袍 | — | — | ·/·/· | — |
| 263 | `RobotHat` | 机器人帽 | — | — | ·/·/· | — |
| 264 | `GoldCrown` | 金冠 | — | — | ·/·/· | — |
| 265 | `HellfireArrow` | 狱炎箭 | — | — | ·/·/· | 13 |
| 266 | `Sandgun` | 沙枪 | — | — | ·/·/· | 30 |
| 267 | `GuideVoodooDoll` | 向导巫毒娃娃 | — | — | ·/·/· | — |
| 268 | `DivingHelmet` | 潜水头盔 | — | — | ·/·/· | — |
| 269 | `FamiliarShirt` | 便装衣 | — | — | ·/·/· | — |
| 270 | `FamiliarPants` | 便装裤 | — | — | ·/·/· | — |
| 271 | `FamiliarWig` | 便装假发 | — | — | ·/·/· | — |
| 272 | `DemonScythe` | 恶魔锄刀 | — | — | ·/·/· | 35 |
| 273 | `NightsEdge` | 永夜刃 | — | — | ·/·/· | 40 |
| 274 | `DarkLance` | 暗黑长戟 | — | — | ·/·/· | 34 |
| 275 | `Coral` | 珊瑚 | 81 | — | ·/·/· | — |
| 276 | `Cactus` | 仙人掌 | 188 | — | ·/·/· | — |
| 277 | `Trident` | 三叉戟 | — | — | ·/·/· | 14 |
| 278 | `SilverBullet` | 银子弹 | — | — | ·/·/· | 9 |
| 279 | `ThrowingKnife` | 投刀 | — | — | ·/·/· | 12 |
| 280 | `Spear` | 长矛 | — | — | ·/·/· | 8 |
| 281 | `Blowpipe` | 吹管 | — | — | ·/·/· | 9 |
| 282 | `Glowstick` | 荧光棒 | — | — | ·/·/· | — |
| 283 | `Seed` | 种子 | — | — | ·/·/· | 4 |
| 284 | `WoodenBoomerang` | 木制回旋镖 | — | — | ·/·/· | 10 |
| 285 | `Aglet` | 金属带扣 | — | — | ·/·/· | — |
| 286 | `StickyGlowstick` | 粘性荧光棒 | — | — | ·/·/· | — |
| 287 | `PoisonedKnife` | 毒刀 | — | — | ·/·/· | 14 |
| 288 | `ObsidianSkinPotion` | 黑曜石皮药水 | — | — | ·/·/· | — |
| 289 | `RegenerationPotion` | 再生药水 | — | — | ·/·/· | — |
| 290 | `SwiftnessPotion` | 敏捷药水 | — | — | ·/·/· | — |
| 291 | `GillsPotion` | 鱼鳃药水 | — | — | ·/·/· | — |
| 292 | `IronskinPotion` | 铁皮药水 | — | — | ·/·/· | — |
| 293 | `ManaRegenerationPotion` | 魔力再生药水 | — | — | ·/·/· | — |
| 294 | `MagicPowerPotion` | 魔能药水 | — | — | ·/·/· | — |
| 295 | `FeatherfallPotion` | 羽落药水 | — | — | ·/·/· | — |
| 296 | `SpelunkerPotion` | 洞穴探险药水 | — | — | ·/·/· | — |
| 297 | `InvisibilityPotion` | 隐身药水 | — | — | ·/·/· | — |
| 298 | `ShinePotion` | 光芒药水 | — | — | ·/·/· | — |
| 299 | `NightOwlPotion` | 夜猫子药水 | — | — | ·/·/· | — |
| 300 | `BattlePotion` | 战斗药水 | — | — | ·/·/· | — |
| 301 | `ThornsPotion` | 荆棘药水 | — | — | ·/·/· | — |
| 302 | `WaterWalkingPotion` | 水上漂药水 | — | — | ·/·/· | — |
| 303 | `ArcheryPotion` | 箭术药水 | — | — | ·/·/· | — |
| 304 | `HunterPotion` | 狩猎药水 | — | — | ·/·/· | — |
| 305 | `GravitationPotion` | 重力药水 | — | — | ·/·/· | — |
| 306 | `GoldChest` | 金箱 | 21 | — | ·/·/· | — |
| 307 | `DaybloomSeeds` | 太阳花种子 | 82 | — | ·/·/· | — |
| 308 | `MoonglowSeeds` | 月光草种子 | 82 | — | ·/·/· | — |
| 309 | `BlinkrootSeeds` | 闪耀根种子 | 82 | — | ·/·/· | — |
| 310 | `DeathweedSeeds` | 死亡草种子 | 82 | — | ·/·/· | — |
| 311 | `WaterleafSeeds` | 幌菊种子 | 82 | — | ·/·/· | — |
| 312 | `FireblossomSeeds` | 火焰花种子 | 82 | — | ·/·/· | — |
| 313 | `Daybloom` | 太阳花 | — | — | ·/·/· | — |
| 314 | `Moonglow` | 月光草 | — | — | ·/·/· | — |
| 315 | `Blinkroot` | 闪耀根 | — | — | ·/·/· | — |
| 316 | `Deathweed` | 死亡草 | — | — | ·/·/· | — |
| 317 | `Waterleaf` | 幌菊 | — | — | ·/·/· | — |
| 318 | `Fireblossom` | 火焰花 | — | — | ·/·/· | — |
| 319 | `SharkFin` | 鲨鱼鳍 | — | — | ·/·/· | — |
| 320 | `Feather` | 羽毛 | — | — | ·/·/· | — |
| 321 | `Tombstone` | 墓石 | 85 | — | ·/·/· | — |
| 322 | `MimeMask` | 丑角面具 | — | — | ·/·/· | — |
| 323 | `AntlionMandible` | 蚁狮上颚 | — | — | ·/·/· | — |
| 324 | `IllegalGunParts` | 非法枪械部件 | — | — | ·/·/· | — |
| 325 | `TheDoctorsShirt` | 博士衣 | — | — | ·/·/· | — |
| 326 | `TheDoctorsPants` | 博士裤 | — | — | ·/·/· | — |
| 327 | `GoldenKey` | 金钥匙 | — | — | ·/·/· | — |
| 328 | `ShadowChest` | 暗影箱 | 21 | — | ·/·/· | — |
| 329 | `ShadowKey` | 暗影钥匙 | — | — | ·/·/· | — |
| 330 | `ObsidianBrickWall` | 黑曜石砖墙 | — | 20 | ·/·/· | — |
| 331 | `JungleSpores` | 丛林孢子 | — | — | ·/·/· | — |
| 332 | `Loom` | 织布机 | 86 | — | ·/·/· | — |
| 333 | `Piano` | 钢琴 | 87 | — | ·/·/· | — |
| 334 | `Dresser` | 梳妆台 | 88 | — | ·/·/· | — |
| 335 | `Bench` | 长椅 | 89 | — | ·/·/· | — |
| 336 | `Bathtub` | 浴缸 | 90 | — | ·/·/· | — |
| 337 | `RedBanner` | 红旗 | 91 | — | ·/·/· | — |
| 338 | `GreenBanner` | 绿旗 | 91 | — | ·/·/· | — |
| 339 | `BlueBanner` | 蓝旗 | 91 | — | ·/·/· | — |
| 340 | `YellowBanner` | 黄旗 | 91 | — | ·/·/· | — |
| 341 | `LampPost` | 灯柱 | 92 | — | ·/·/· | — |
| 342 | `TikiTorch` | 提基火把 | 93 | — | ·/·/· | — |
| 343 | `Barrel` | 桶 | 21 | — | ·/·/· | — |
| 344 | `ChineseLantern` | 中式灯笼 | 95 | — | ·/·/· | — |
| 345 | `CookingPot` | 烹饪锅 | 96 | — | ·/·/· | — |
| 346 | `Safe` | 保险箱 | 97 | — | ·/·/· | — |
| 347 | `SkullLantern` | 骷髅头灯笼 | 98 | — | ·/·/· | — |
| 348 | `TrashCan` | 垃圾桶 | 21 | — | ·/·/· | — |
| 349 | `Candelabra` | 烛台 | 100 | — | ·/·/· | — |
| 350 | `PinkVase` | 粉花瓶 | 13 | — | ·/·/· | — |
| 351 | `Mug` | 玻璃杯 | 13 | — | ·/·/· | — |
| 352 | `Keg` | 酒桶 | 94 | — | ·/·/· | — |
| 353 | `Ale` | 麦芽酒 | — | — | ·/·/· | — |
| 354 | `Bookcase` | 书架 | 101 | — | ·/·/· | — |
| 355 | `Throne` | 王座 | 102 | — | ·/·/· | — |
| 356 | `Bowl` | 碗 | 103 | — | ·/·/· | — |
| 357 | `BowlofSoup` | 鱼菇汤 | — | — | ·/·/· | — |
| 358 | `Toilet` | 马桶 | 15 | — | ·/·/· | — |
| 359 | `GrandfatherClock` | 落地大摆钟 | 104 | — | ·/·/· | — |
| 360 | `ArmorStatue` | 盔甲雕像 | 105 | — | ·/·/· | — |
| 361 | `GoblinBattleStandard` | 哥布林战旗 | — | — | ·/·/· | — |
| 362 | `TatteredCloth` | 破布 | — | — | ·/·/· | — |
| 363 | `Sawmill` | 锯木机 | 106 | — | ·/·/· | — |
| 364 | `CobaltOre` | 钴矿 | 107 | — | ·/·/· | — |
| 365 | `MythrilOre` | 秘银矿 | 108 | — | ·/·/· | — |
| 366 | `AdamantiteOre` | 精金矿 | 111 | — | ·/·/· | — |
| 367 | `Pwnhammer` | 神锤 | — | — | ·/·/80 | 26 |
| 368 | `Excalibur` | 断钢剑 | — | — | ·/·/· | 72 |
| 369 | `HallowedSeeds` | 圣种 | 109 | — | ·/·/· | — |
| 370 | `EbonsandBlock` | 黑檀沙块 | 112 | — | ·/·/· | — |
| 371 | `CobaltHat` | 钴帽 | — | — | ·/·/· | — |
| 372 | `CobaltHelmet` | 钴头盔 | — | — | ·/·/· | — |
| 373 | `CobaltMask` | 钴面具 | — | — | ·/·/· | — |
| 374 | `CobaltBreastplate` | 钴胸甲 | — | — | ·/·/· | — |
| 375 | `CobaltLeggings` | 钴护腿 | — | — | ·/·/· | — |
| 376 | `MythrilHood` | 秘银兜帽 | — | — | ·/·/· | — |
| 377 | `MythrilHelmet` | 秘银头盔 | — | — | ·/·/· | — |
| 378 | `MythrilHat` | 秘银帽 | — | — | ·/·/· | — |
| 379 | `MythrilChainmail` | 秘银链甲 | — | — | ·/·/· | — |
| 380 | `MythrilGreaves` | 秘银护胫 | — | — | ·/·/· | — |
| 381 | `CobaltBar` | 钴锭 | 239 | — | ·/·/· | — |
| 382 | `MythrilBar` | 秘银锭 | 239 | — | ·/·/· | — |
| 383 | `CobaltChainsaw` | 钴链锯 | — | — | ·/14/· | 23 |
| 384 | `MythrilChainsaw` | 秘银链锯 | — | — | ·/17/· | 29 |
| 385 | `CobaltDrill` | 钴钻头 | — | — | 110/·/· | 10 |
| 386 | `MythrilDrill` | 秘银钻头 | — | — | 150/·/· | 15 |
| 387 | `AdamantiteChainsaw` | 精金链锯 | — | — | ·/20/· | 33 |
| 388 | `AdamantiteDrill` | 精金钻头 | — | — | 180/·/· | 20 |
| 389 | `DaoofPow` | 太极连枷 | — | — | ·/·/· | 50 |
| 390 | `MythrilHalberd` | 秘银长戟 | — | — | ·/·/· | 45 |
| 391 | `AdamantiteBar` | 精金锭 | 239 | — | ·/·/· | — |
| 392 | `GlassWall` | 玻璃墙 | — | 21 | ·/·/· | — |
| 393 | `Compass` | 罗盘 | — | — | ·/·/· | — |
| 394 | `DivingGear` | 潜水装备 | — | — | ·/·/· | — |
| 395 | `GPS` | 全球定位系统 | — | — | ·/·/· | — |
| 396 | `ObsidianHorseshoe` | 黑曜石马掌 | — | — | ·/·/· | — |
| 397 | `ObsidianShield` | 黑曜石护盾 | — | — | ·/·/· | — |
| 398 | `TinkerersWorkshop` | 工匠作坊 | 114 | — | ·/·/· | — |
| 399 | `CloudinaBalloon` | 云朵气球 | — | — | ·/·/· | — |
| 400 | `AdamantiteHeadgear` | 精金头饰 | — | — | ·/·/· | — |
| 401 | `AdamantiteHelmet` | 精金头盔 | — | — | ·/·/· | — |
| 402 | `AdamantiteMask` | 精金面具 | — | — | ·/·/· | — |
| 403 | `AdamantiteBreastplate` | 精金胸甲 | — | — | ·/·/· | — |
| 404 | `AdamantiteLeggings` | 精金护腿 | — | — | ·/·/· | — |
| 405 | `SpectreBoots` | 幽灵靴 | — | — | ·/·/· | — |
| 406 | `AdamantiteGlaive` | 精金关刀 | — | — | ·/·/· | 49 |
| 407 | `Toolbelt` | 工具腰带 | — | — | ·/·/· | — |
| 408 | `PearlsandBlock` | 珍珠沙块 | 116 | — | ·/·/· | — |
| 409 | `PearlstoneBlock` | 珍珠石块 | 117 | — | ·/·/· | — |
| 410 | `MiningShirt` | 挖矿衣 | — | — | ·/·/· | — |
| 411 | `MiningPants` | 挖矿裤 | — | — | ·/·/· | — |
| 412 | `PearlstoneBrick` | 珍珠石砖 | 118 | — | ·/·/· | — |
| 413 | `IridescentBrick` | 荧光砖 | 119 | — | ·/·/· | — |
| 414 | `MudstoneBlock` | 泥石砖 | 120 | — | ·/·/· | — |
| 415 | `CobaltBrick` | 钴砖 | 121 | — | ·/·/· | — |
| 416 | `MythrilBrick` | 秘银砖 | 122 | — | ·/·/· | — |
| 417 | `PearlstoneBrickWall` | 珍珠石砖墙 | — | 22 | ·/·/· | — |
| 418 | `IridescentBrickWall` | 荧光砖墙 | — | 23 | ·/·/· | — |
| 419 | `MudstoneBrickWall` | 泥石砖墙 | — | 24 | ·/·/· | — |
| 420 | `CobaltBrickWall` | 钴砖墙 | — | 25 | ·/·/· | — |
| 421 | `MythrilBrickWall` | 秘银砖墙 | — | 26 | ·/·/· | — |
| 422 | `HolyWater` | 圣水 | — | — | ·/·/· | 20 |
| 423 | `UnholyWater` | 邪水 | — | — | ·/·/· | 20 |
| 424 | `SiltBlock` | 泥沙块 | 123 | — | ·/·/· | — |
| 425 | `FairyBell` | 仙灵铃铛 | — | — | ·/·/· | — |
| 426 | `BreakerBlade` | 毁灭刃 | — | — | ·/·/· | 70 |
| 427 | `BlueTorch` | 蓝火把 | 4 | — | ·/·/· | — |
| 428 | `RedTorch` | 红火把 | 4 | — | ·/·/· | — |
| 429 | `GreenTorch` | 绿火把 | 4 | — | ·/·/· | — |
| 430 | `PurpleTorch` | 紫火把 | 4 | — | ·/·/· | — |
| 431 | `WhiteTorch` | 白火把 | 4 | — | ·/·/· | — |
| 432 | `YellowTorch` | 黄火把 | 4 | — | ·/·/· | — |
| 433 | `DemonTorch` | 恶魔火把 | 4 | — | ·/·/· | — |
| 434 | `ClockworkAssaultRifle` | 发条式突击步枪 | — | — | ·/·/· | 17 |
| 435 | `CobaltRepeater` | 钴连弩 | — | — | ·/·/· | 35 |
| 436 | `MythrilRepeater` | 秘银连弩 | — | — | ·/·/· | 39 |
| 437 | `DualHook` | 双钩 | — | — | ·/·/· | — |
| 438 | `StarStatue` | 星星雕像 | 105 | — | ·/·/· | — |
| 439 | `SwordStatue` | 宝剑雕像 | 105 | — | ·/·/· | — |
| 440 | `SlimeStatue` | 史莱姆雕像 | 105 | — | ·/·/· | — |
| 441 | `GoblinStatue` | 哥布林雕像 | 105 | — | ·/·/· | — |
| 442 | `ShieldStatue` | 护盾雕像 | 105 | — | ·/·/· | — |
| 443 | `BatStatue` | 蝙蝠雕像 | 105 | — | ·/·/· | — |
| 444 | `FishStatue` | 金鱼雕像 | 105 | — | ·/·/· | — |
| 445 | `BunnyStatue` | 兔兔雕像 | 105 | — | ·/·/· | — |
| 446 | `SkeletonStatue` | 骷髅雕像 | 105 | — | ·/·/· | — |
| 447 | `ReaperStatue` | 死神雕像 | 105 | — | ·/·/· | — |
| 448 | `WomanStatue` | 女人雕像 | 105 | — | ·/·/· | — |
| 449 | `ImpStatue` | 小鬼雕像 | 105 | — | ·/·/· | — |
| 450 | `GargoyleStatue` | 石像鬼雕像 | 105 | — | ·/·/· | — |
| 451 | `GloomStatue` | 幽冥雕像 | 105 | — | ·/·/· | — |
| 452 | `HornetStatue` | 黄蜂雕像 | 105 | — | ·/·/· | — |
| 453 | `BombStatue` | 炸弹雕像 | 105 | — | ·/·/· | — |
| 454 | `CrabStatue` | 螃蟹雕像 | 105 | — | ·/·/· | — |
| 455 | `HammerStatue` | 战锤雕像 | 105 | — | ·/·/· | — |
| 456 | `PotionStatue` | 药水雕像 | 105 | — | ·/·/· | — |
| 457 | `SpearStatue` | 长矛雕像 | 105 | — | ·/·/· | — |
| 458 | `CrossStatue` | 十字架雕像 | 105 | — | ·/·/· | — |
| 459 | `JellyfishStatue` | 水母雕像 | 105 | — | ·/·/· | — |
| 460 | `BowStatue` | 弓雕像 | 105 | — | ·/·/· | — |
| 461 | `BoomerangStatue` | 回旋镖雕像 | 105 | — | ·/·/· | — |
| 462 | `BootStatue` | 靴子雕像 | 105 | — | ·/·/· | — |
| 463 | `ChestStatue` | 宝箱雕像 | 105 | — | ·/·/· | — |
| 464 | `BirdStatue` | 小鸟雕像 | 105 | — | ·/·/· | — |
| 465 | `AxeStatue` | 战斧雕像 | 105 | — | ·/·/· | — |
| 466 | `CorruptStatue` | 腐化雕像 | 105 | — | ·/·/· | — |
| 467 | `TreeStatue` | 树木雕像 | 105 | — | ·/·/· | — |
| 468 | `AnvilStatue` | 砧雕像 | 105 | — | ·/·/· | — |
| 469 | `PickaxeStatue` | 镐雕像 | 105 | — | ·/·/· | — |
| 470 | `MushroomStatue` | 蘑菇雕像 | 349 | — | ·/·/· | — |
| 471 | `EyeballStatue` | 魔眼雕像 | 105 | — | ·/·/· | — |
| 472 | `PillarStatue` | 石柱雕像 | 105 | — | ·/·/· | — |
| 473 | `HeartStatue` | 心形雕像 | 105 | — | ·/·/· | — |
| 474 | `PotStatue` | 陶罐雕像 | 105 | — | ·/·/· | — |
| 475 | `SunflowerStatue` | 向日葵雕像 | 105 | — | ·/·/· | — |
| 476 | `KingStatue` | 国王雕像 | 105 | — | ·/·/· | — |
| 477 | `QueenStatue` | 女王雕像 | 105 | — | ·/·/· | — |
| 478 | `PiranhaStatue` | 食人鱼雕像 | 105 | — | ·/·/· | — |
| 479 | `PlankedWall` | 板条墙 | — | 27 | ·/·/· | — |
| 480 | `WoodenBeam` | 木梁 | 124 | — | ·/·/· | — |
| 481 | `AdamantiteRepeater` | 精金连弩 | — | — | ·/·/· | 42 |
| 482 | `AdamantiteSword` | 精金剑 | — | — | ·/·/· | 61 |
| 483 | `CobaltSword` | 钴剑 | — | — | ·/·/· | 40 |
| 484 | `MythrilSword` | 秘银剑 | — | — | ·/·/· | 50 |
| 485 | `MoonCharm` | 月光护身符 | — | — | ·/·/· | — |
| 486 | `Ruler` | 标尺 | — | — | ·/·/· | 12 |
| 487 | `CrystalBall` | 水晶球 | 125 | — | ·/·/· | — |
| 488 | `DiscoBall` | 迪斯科球 | 126 | — | ·/·/· | — |
| 489 | `SorcererEmblem` | 巫士徽章 | — | — | ·/·/· | — |
| 490 | `WarriorEmblem` | 战士徽章 | — | — | ·/·/· | — |
| 491 | `RangerEmblem` | 游侠徽章 | — | — | ·/·/· | — |
| 492 | `DemonWings` | 恶魔之翼 | — | — | ·/·/· | — |
| 493 | `AngelWings` | 天使之翼 | — | — | ·/·/· | — |
| 494 | `MagicalHarp` | 魔法竖琴 | — | — | ·/·/· | 42 |
| 495 | `RainbowRod` | 彩虹魔杖 | — | — | ·/·/· | 50 |
| 496 | `IceRod` | 冰雪魔杖 | — | — | ·/·/· | 28 |
| 497 | `NeptunesShell` | 海神贝壳 | — | — | ·/·/· | — |
| 498 | `Mannequin` | 人体模型 | 470 | — | ·/·/· | — |
| 499 | `GreaterHealingPotion` | 强效治疗药水 | — | — | ·/·/· | — |
| 500 | `GreaterManaPotion` | 强效魔力药水 | — | — | ·/·/· | — |
| 501 | `PixieDust` | 妖精尘 | — | — | ·/·/· | — |
| 502 | `CrystalShard` | 水晶碎块 | 129 | — | ·/·/· | — |
| 503 | `ClownHat` | 小丑帽 | — | — | ·/·/· | — |
| 504 | `ClownShirt` | 小丑衣 | — | — | ·/·/· | — |
| 505 | `ClownPants` | 小丑裤 | — | — | ·/·/· | — |
| 506 | `Flamethrower` | 火焰喷射器 | — | — | ·/·/· | 35 |
| 507 | `Bell` | 铃铛 | — | — | ·/·/· | — |
| 508 | `Harp` | 竖琴 | — | — | ·/·/· | — |
| 509 | `Wrench` | 红扳手 | — | — | ·/·/· | — |
| 510 | `WireCutter` | 钢丝钳 | — | — | ·/·/· | — |
| 511 | `ActiveStoneBlock` | 通电石块 | 130 | — | ·/·/· | — |
| 512 | `InactiveStoneBlock` | 未通电石块 | 131 | — | ·/·/· | — |
| 513 | `Lever` | 控制杆 | 132 | — | ·/·/· | — |
| 514 | `LaserRifle` | 激光步枪 | — | — | ·/·/· | 29 |
| 515 | `CrystalBullet` | 水晶子弹 | — | — | ·/·/· | 9 |
| 516 | `HolyArrow` | 圣箭 | — | — | ·/·/· | 13 |
| 517 | `MagicDagger` | 魔法飞刀 | — | — | ·/·/· | 35 |
| 518 | `CrystalStorm` | 水晶风暴 | — | — | ·/·/· | 32 |
| 519 | `CursedFlames` | 诅咒焰 | — | — | ·/·/· | 55 |
| 520 | `SoulofLight` | 光明之魂 | — | — | ·/·/· | — |
| 521 | `SoulofNight` | 暗影之魂 | — | — | ·/·/· | — |
| 522 | `CursedFlame` | 诅咒焰 | — | — | ·/·/· | — |
| 523 | `CursedTorch` | 诅咒火把 | 4 | — | ·/·/· | — |
| 524 | `AdamantiteForge` | 精金熔炉 | 133 | — | ·/·/· | — |
| 525 | `MythrilAnvil` | 秘银砧 | 134 | — | ·/·/· | — |
| 526 | `UnicornHorn` | 独角兽角 | — | — | ·/·/· | — |
| 527 | `DarkShard` | 暗黑碎块 | — | — | ·/·/· | — |
| 528 | `LightShard` | 光明碎块 | — | — | ·/·/· | — |
| 529 | `RedPressurePlate` | 红压力板 | 135 | — | ·/·/· | — |
| 530 | `Wire` | 电线 | — | — | ·/·/· | — |
| 531 | `SpellTome` | 魔法书 | — | — | ·/·/· | — |
| 532 | `StarCloak` | 星星斗篷 | — | — | ·/·/· | — |
| 533 | `Megashark` | 巨兽鲨 | — | — | ·/·/· | 25 |
| 534 | `Shotgun` | 霰弹枪 | — | — | ·/·/· | 24 |
| 535 | `PhilosophersStone` | 点金石 | — | — | ·/·/· | — |
| 536 | `TitanGlove` | 泰坦手套 | — | — | ·/·/· | — |
| 537 | `CobaltNaginata` | 钴剃刀 | — | — | ·/·/· | 44 |
| 538 | `Switch` | 开关 | 136 | — | ·/·/· | — |
| 539 | `DartTrap` | 飞镖机关 | 137 | — | ·/·/· | — |
| 540 | `Boulder` | 巨石 | 138 | — | ·/·/· | — |
| 541 | `GreenPressurePlate` | 绿压力板 | 135 | — | ·/·/· | — |
| 542 | `GrayPressurePlate` | 灰压力板 | 135 | — | ·/·/· | — |
| 543 | `BrownPressurePlate` | 棕压力板 | 135 | — | ·/·/· | — |
| 544 | `MechanicalEye` | 机械魔眼 | — | — | ·/·/· | — |
| 545 | `CursedArrow` | 诅咒箭 | — | — | ·/·/· | 17 |
| 546 | `CursedBullet` | 诅咒弹 | — | — | ·/·/· | 12 |
| 547 | `SoulofFright` | 恐惧之魂 | — | — | ·/·/· | — |
| 548 | `SoulofMight` | 力量之魂 | — | — | ·/·/· | — |
| 549 | `SoulofSight` | 视域之魂 | — | — | ·/·/· | — |
| 550 | `Gungnir` | 永恒之枪 | — | — | ·/·/· | 61 |
| 551 | `HallowedPlateMail` | 神圣板甲 | — | — | ·/·/· | — |
| 552 | `HallowedGreaves` | 神圣护胫 | — | — | ·/·/· | — |
| 553 | `HallowedHelmet` | 神圣头盔 | — | — | ·/·/· | — |
| 554 | `CrossNecklace` | 十字项链 | — | — | ·/·/· | — |
| 555 | `ManaFlower` | 魔力花 | — | — | ·/·/· | — |
| 556 | `MechanicalWorm` | 机械蠕虫 | — | — | ·/·/· | — |
| 557 | `MechanicalSkull` | 机械骷髅头 | — | — | ·/·/· | — |
| 558 | `HallowedHeadgear` | 神圣头饰 | — | — | ·/·/· | — |
| 559 | `HallowedMask` | 神圣面具 | — | — | ·/·/· | — |
| 560 | `SlimeCrown` | 史莱姆王冠 | — | — | ·/·/· | — |
| 561 | `LightDisc` | 光辉飞盘 | — | — | ·/·/· | 60 |
| 562 | `MusicBoxOverworldDay` | 八音盒（人间日） | 139 | — | ·/·/· | — |
| 563 | `MusicBoxEerie` | 八音盒（恐惧） | 139 | — | ·/·/· | — |
| 564 | `MusicBoxNight` | 八音盒（暗夜） | 139 | — | ·/·/· | — |
| 565 | `MusicBoxTitle` | 八音盒（标题） | 139 | — | ·/·/· | — |
| 566 | `MusicBoxUnderground` | 八音盒（地下） | 139 | — | ·/·/· | — |
| 567 | `MusicBoxBoss1` | 八音盒(Boss 1) | 139 | — | ·/·/· | — |
| 568 | `MusicBoxJungle` | 八音盒（丛林） | 139 | — | ·/·/· | — |
| 569 | `MusicBoxCorruption` | 八音盒（腐化之地） | 139 | — | ·/·/· | — |
| 570 | `MusicBoxUndergroundCorruption` | 八音盒（地下腐化之地） | 139 | — | ·/·/· | — |
| 571 | `MusicBoxTheHallow` | 八音盒（神圣之地） | 139 | — | ·/·/· | — |
| 572 | `MusicBoxBoss2` | 八音盒(Boss 2( | 139 | — | ·/·/· | — |
| 573 | `MusicBoxUndergroundHallow` | 八音盒（地下神圣之地） | 139 | — | ·/·/· | — |
| 574 | `MusicBoxBoss3` | 八音盒(Boss 3) | 139 | — | ·/·/· | — |
| 575 | `SoulofFlight` | 飞翔之魂 | — | — | ·/·/· | — |
| 576 | `MusicBox` | 八音盒 | — | — | ·/·/· | — |
| 577 | `DemoniteBrick` | 魔矿砖 | 140 | — | ·/·/· | — |
| 578 | `HallowedRepeater` | 神圣连弩 | — | — | ·/·/· | 50 |
| 579 | `Drax` | 斧钻 | — | — | 200/22/· | 35 |
| 580 | `Explosives` | 炸药 | 141 | — | ·/·/· | — |
| 581 | `InletPump` | 入水泵 | 142 | — | ·/·/· | — |
| 582 | `OutletPump` | 出水泵 | 143 | — | ·/·/· | — |
| 583 | `Timer1Second` | 1秒计时器 | 144 | — | ·/·/· | — |
| 584 | `Timer3Second` | 3秒计时器 | 144 | — | ·/·/· | — |
| 585 | `Timer5Second` | 5秒计时器 | 144 | — | ·/·/· | — |
| 586 | `CandyCaneBlock` | 糖棒块 | 145 | — | ·/·/· | — |
| 587 | `CandyCaneWall` | 糖棒墙 | — | 29 | ·/·/· | — |
| 588 | `SantaHat` | 圣诞帽 | — | — | ·/·/· | — |
| 589 | `SantaShirt` | 圣诞衣 | — | — | ·/·/· | — |
| 590 | `SantaPants` | 圣诞裤 | — | — | ·/·/· | — |
| 591 | `GreenCandyCaneBlock` | 绿糖棒块 | 146 | — | ·/·/· | — |
| 592 | `GreenCandyCaneWall` | 绿糖棒墙 | — | 30 | ·/·/· | — |
| 593 | `SnowBlock` | 雪块 | 147 | — | ·/·/· | — |
| 594 | `SnowBrick` | 雪砖 | 148 | — | ·/·/· | — |
| 595 | `SnowBrickWall` | 雪砖墙 | — | 31 | ·/·/· | — |
| 596 | `BlueLight` | 蓝灯 | 149 | — | ·/·/· | — |
| 597 | `RedLight` | 红灯 | 149 | — | ·/·/· | — |
| 598 | `GreenLight` | 绿灯 | 149 | — | ·/·/· | — |
| 599 | `BluePresent` | 蓝礼物 | — | — | ·/·/· | — |
| 600 | `GreenPresent` | 绿礼物 | — | — | ·/·/· | — |
| 601 | `YellowPresent` | 黄礼物 | — | — | ·/·/· | — |
| 602 | `SnowGlobe` | 水晶雪球 | — | — | ·/·/· | — |
| 603 | `Carrot` | 胡萝卜 | — | — | ·/·/· | — |
| 604 | `AdamantiteBeam` | 精金梁 | 150 | — | ·/·/· | — |
| 605 | `AdamantiteBeamWall` | 精金梁墙 | — | 32 | ·/·/· | — |
| 606 | `DemoniteBrickWall` | 魔矿砖墙 | — | 33 | ·/·/· | — |
| 607 | `SandstoneBrick` | 沙岩砖 | 151 | — | ·/·/· | — |
| 608 | `SandstoneBrickWall` | 沙岩砖墙 | — | 34 | ·/·/· | — |
| 609 | `EbonstoneBrick` | 黑檀石砖 | 152 | — | ·/·/· | — |
| 610 | `EbonstoneBrickWall` | 黑檀石砖墙 | — | 35 | ·/·/· | — |
| 611 | `RedStucco` | 红泥灰 | 153 | — | ·/·/· | — |
| 612 | `YellowStucco` | 黄泥灰 | 154 | — | ·/·/· | — |
| 613 | `GreenStucco` | 绿泥灰 | 155 | — | ·/·/· | — |
| 614 | `GrayStucco` | 灰泥灰 | 156 | — | ·/·/· | — |
| 615 | `RedStuccoWall` | 红泥灰墙 | — | 36 | ·/·/· | — |
| 616 | `YellowStuccoWall` | 黄泥灰墙 | — | 37 | ·/·/· | — |
| 617 | `GreenStuccoWall` | 绿泥灰墙 | — | 38 | ·/·/· | — |
| 618 | `GrayStuccoWall` | 灰泥灰墙 | — | 39 | ·/·/· | — |
| 619 | `Ebonwood` | 乌木 | 157 | — | ·/·/· | — |
| 620 | `RichMahogany` | 红木 | 158 | — | ·/·/· | — |
| 621 | `Pearlwood` | 珍珠木 | 159 | — | ·/·/· | — |
| 622 | `EbonwoodWall` | 乌木墙 | — | 41 | ·/·/· | — |
| 623 | `RichMahoganyWall` | 红木墙 | — | 42 | ·/·/· | — |
| 624 | `PearlwoodWall` | 珍珠木墙 | — | 43 | ·/·/· | — |
| 625 | `EbonwoodChest` | 乌木箱 | 21 | — | ·/·/· | — |
| 626 | `RichMahoganyChest` | 红木箱 | 21 | — | ·/·/· | — |
| 627 | `PearlwoodChest` | 珍珠木箱 | 21 | — | ·/·/· | — |
| 628 | `EbonwoodChair` | 乌木椅 | 15 | — | ·/·/· | — |
| 629 | `RichMahoganyChair` | 红木椅 | 15 | — | ·/·/· | — |
| 630 | `PearlwoodChair` | 珍珠木椅 | 15 | — | ·/·/· | — |
| 631 | `EbonwoodPlatform` | 乌木平台 | 19 | — | ·/·/· | — |
| 632 | `RichMahoganyPlatform` | 红木平台 | 19 | — | ·/·/· | — |
| 633 | `PearlwoodPlatform` | 珍珠木平台 | 19 | — | ·/·/· | — |
| 634 | `BonePlatform` | 骨头平台 | 19 | — | ·/·/· | — |
| 635 | `EbonwoodWorkBench` | 乌木工作台 | 18 | — | ·/·/· | — |
| 636 | `RichMahoganyWorkBench` | 红木工作台 | 18 | — | ·/·/· | — |
| 637 | `PearlwoodWorkBench` | 珍珠木工作台 | 18 | — | ·/·/· | — |
| 638 | `EbonwoodTable` | 乌木桌 | 14 | — | ·/·/· | — |
| 639 | `RichMahoganyTable` | 红木桌 | 14 | — | ·/·/· | — |
| 640 | `PearlwoodTable` | 珍珠木桌 | 14 | — | ·/·/· | — |
| 641 | `EbonwoodPiano` | 乌木钢琴 | 87 | — | ·/·/· | — |
| 642 | `RichMahoganyPiano` | 红木钢琴 | 87 | — | ·/·/· | — |
| 643 | `PearlwoodPiano` | 珍珠木钢琴 | 87 | — | ·/·/· | — |
| 644 | `EbonwoodBed` | 乌木床 | 79 | — | ·/·/· | — |
| 645 | `RichMahoganyBed` | 红木床 | 79 | — | ·/·/· | — |
| 646 | `PearlwoodBed` | 珍珠木床 | 79 | — | ·/·/· | — |
| 647 | `EbonwoodDresser` | 乌木梳妆台 | 88 | — | ·/·/· | — |
| 648 | `RichMahoganyDresser` | 红木梳妆台 | 88 | — | ·/·/· | — |
| 649 | `PearlwoodDresser` | 珍珠木梳妆台 | 88 | — | ·/·/· | — |
| 650 | `EbonwoodDoor` | 乌木门 | 10 | — | ·/·/· | — |
| 651 | `RichMahoganyDoor` | 红木门 | 10 | — | ·/·/· | — |
| 652 | `PearlwoodDoor` | 珍珠木门 | 10 | — | ·/·/· | — |
| 653 | `EbonwoodSword` | 乌木剑 | — | — | ·/·/· | 11 |
| 654 | `EbonwoodHammer` | 乌木锤 | — | — | ·/·/40 | 7 |
| 655 | `EbonwoodBow` | 乌木弓 | — | — | ·/·/· | 8 |
| 656 | `RichMahoganySword` | 红木剑 | — | — | ·/·/· | 8 |
| 657 | `RichMahoganyHammer` | 红木锤 | — | — | ·/·/35 | 4 |
| 658 | `RichMahoganyBow` | 红木弓 | — | — | ·/·/· | 6 |
| 659 | `PearlwoodSword` | 珍珠木剑 | — | — | ·/·/· | 30 |
| 660 | `PearlwoodHammer` | 珍珠木锤 | — | — | ·/·/55 | 10 |
| 661 | `PearlwoodBow` | 珍珠木弓 | — | — | ·/·/· | 12 |
| 662 | `RainbowBrick` | 彩虹砖 | 160 | — | ·/·/· | — |
| 663 | `RainbowBrickWall` | 彩虹砖墙 | — | 44 | ·/·/· | — |
| 664 | `IceBlock` | 冰雪块 | 161 | — | ·/·/· | — |
| 665 | `RedsWings` | Red的翅膀 | — | — | ·/·/· | — |
| 666 | `RedsHelmet` | Red的头盔 | — | — | ·/·/· | — |
| 667 | `RedsBreastplate` | Red的胸甲 | — | — | ·/·/· | — |
| 668 | `RedsLeggings` | Red的护腿 | — | — | ·/·/· | — |
| 669 | `Fish` | 鱼 | — | — | ·/·/· | — |
| 670 | `IceBoomerang` | 冰雪回旋镖 | — | — | ·/·/· | 21 |
| 671 | `Keybrand` | 钥匙剑 | — | — | ·/·/· | 105 |
| 672 | `Cutlass` | 短弯刀 | — | — | ·/·/· | 53 |
| 673 | `BorealWoodWorkBench` | 针叶木工作台 | 18 | — | ·/·/· | — |
| 674 | `TrueExcalibur` | 原版断钢剑 | — | — | ·/·/· | 72 |
| 675 | `TrueNightsEdge` | 原版永夜刃 | — | — | ·/·/· | 70 |
| 676 | `Frostbrand` | 霜印剑 | — | — | ·/·/· | 49 |
| 677 | `BorealWoodTable` | 针叶木桌 | 14 | — | ·/·/· | — |
| 678 | `RedPotion` | 红药水 | — | — | ·/·/· | — |
| 679 | `TacticalShotgun` | 战术霰弹枪 | — | — | ·/·/· | 29 |
| 680 | `IvyChest` | 常春藤箱 | 21 | — | ·/·/· | — |
| 681 | `IceChest` | 冰冻箱 | 21 | — | ·/·/· | — |
| 682 | `Marrow` | 骸骨弓 | — | — | ·/·/· | 53 |
| 683 | `UnholyTrident` | 邪恶三叉戟 | — | — | ·/·/· | 88 |
| 684 | `FrostHelmet` | 寒霜头盔 | — | — | ·/·/· | — |
| 685 | `FrostBreastplate` | 寒霜胸甲 | — | — | ·/·/· | — |
| 686 | `FrostLeggings` | 寒霜护腿 | — | — | ·/·/· | — |
| 687 | `TinHelmet` | 锡头盔 | — | — | ·/·/· | — |
| 688 | `TinChainmail` | 锡链甲 | — | — | ·/·/· | — |
| 689 | `TinGreaves` | 锡护胫 | — | — | ·/·/· | — |
| 690 | `LeadHelmet` | 铅头盔 | — | — | ·/·/· | — |
| 691 | `LeadChainmail` | 铅链甲 | — | — | ·/·/· | — |
| 692 | `LeadGreaves` | 铅护胫 | — | — | ·/·/· | — |
| 693 | `TungstenHelmet` | 钨头盔 | — | — | ·/·/· | — |
| 694 | `TungstenChainmail` | 钨链甲 | — | — | ·/·/· | — |
| 695 | `TungstenGreaves` | 钨护胫 | — | — | ·/·/· | — |
| 696 | `PlatinumHelmet` | 铂金头盔 | — | — | ·/·/· | — |
| 697 | `PlatinumChainmail` | 铂金链甲 | — | — | ·/·/· | — |
| 698 | `PlatinumGreaves` | 铂金护胫 | — | — | ·/·/· | — |
| 699 | `TinOre` | 锡矿 | 166 | — | ·/·/· | — |
| 700 | `LeadOre` | 铅矿 | 167 | — | ·/·/· | — |
| 701 | `TungstenOre` | 钨矿 | 168 | — | ·/·/· | — |
| 702 | `PlatinumOre` | 铂金矿 | 169 | — | ·/·/· | — |
| 703 | `TinBar` | 锡锭 | 239 | — | ·/·/· | — |
| 704 | `LeadBar` | 铅锭 | 239 | — | ·/·/· | — |
| 705 | `TungstenBar` | 钨锭 | 239 | — | ·/·/· | — |
| 706 | `PlatinumBar` | 铂金锭 | 239 | — | ·/·/· | — |
| 707 | `TinWatch` | 锡表 | — | — | ·/·/· | — |
| 708 | `TungstenWatch` | 钨表 | — | — | ·/·/· | — |
| 709 | `PlatinumWatch` | 铂金表 | — | — | ·/·/· | — |
| 710 | `TinChandelier` | 锡吊灯 | 34 | — | ·/·/· | — |
| 711 | `TungstenChandelier` | 钨吊灯 | 34 | — | ·/·/· | — |
| 712 | `PlatinumChandelier` | 铂金吊灯 | 34 | — | ·/·/· | — |
| 713 | `PlatinumCandle` | 铂金蜡烛 | 174 | — | ·/·/· | — |
| 714 | `PlatinumCandelabra` | 铂金烛台 | 173 | — | ·/·/· | — |
| 715 | `PlatinumCrown` | 铂金冠 | — | — | ·/·/· | — |
| 716 | `LeadAnvil` | 铅砧 | 16 | — | ·/·/· | — |
| 717 | `TinBrick` | 锡砖 | 175 | — | ·/·/· | — |
| 718 | `TungstenBrick` | 钨砖 | 176 | — | ·/·/· | — |
| 719 | `PlatinumBrick` | 铂金砖 | 177 | — | ·/·/· | — |
| 720 | `TinBrickWall` | 锡砖墙 | — | 45 | ·/·/· | — |
| 721 | `TungstenBrickWall` | 钨砖墙 | — | 46 | ·/·/· | — |
| 722 | `PlatinumBrickWall` | 铂金砖墙 | — | 47 | ·/·/· | — |
| 723 | `BeamSword` | 光束剑 | — | — | ·/·/· | 52 |
| 724 | `IceBlade` | 冰雪刃 | — | — | ·/·/· | 17 |
| 725 | `IceBow` | 冰雪弓 | — | — | ·/·/· | 39 |
| 726 | `FrostStaff` | 寒霜法杖 | — | — | ·/·/· | 46 |
| 727 | `WoodHelmet` | 木头盔 | — | — | ·/·/· | — |
| 728 | `WoodBreastplate` | 木胸甲 | — | — | ·/·/· | — |
| 729 | `WoodGreaves` | 木护胫 | — | — | ·/·/· | — |
| 730 | `EbonwoodHelmet` | 乌木头盔 | — | — | ·/·/· | — |
| 731 | `EbonwoodBreastplate` | 乌木胸甲 | — | — | ·/·/· | — |
| 732 | `EbonwoodGreaves` | 乌木护胫 | — | — | ·/·/· | — |
| 733 | `RichMahoganyHelmet` | 红木头盔 | — | — | ·/·/· | — |
| 734 | `RichMahoganyBreastplate` | 红木胸甲 | — | — | ·/·/· | — |
| 735 | `RichMahoganyGreaves` | 红木护胫 | — | — | ·/·/· | — |
| 736 | `PearlwoodHelmet` | 珍珠木头盔 | — | — | ·/·/· | — |
| 737 | `PearlwoodBreastplate` | 珍珠木胸甲 | — | — | ·/·/· | — |
| 738 | `PearlwoodGreaves` | 珍珠木护胫 | — | — | ·/·/· | — |
| 739 | `AmethystStaff` | 紫晶法杖 | — | — | ·/·/· | 15 |
| 740 | `TopazStaff` | 黄玉法杖 | — | — | ·/·/· | 16 |
| 741 | `SapphireStaff` | 蓝玉法杖 | — | — | ·/·/· | 18 |
| 742 | `EmeraldStaff` | 翡翠法杖 | — | — | ·/·/· | 19 |
| 743 | `RubyStaff` | 红玉法杖 | — | — | ·/·/· | 21 |
| 744 | `DiamondStaff` | 钻石法杖 | — | — | ·/·/· | 23 |
| 745 | `GrassWall` | 草墙 | — | 66 | ·/·/· | — |
| 746 | `JungleWall` | 丛林墙 | — | 67 | ·/·/· | — |
| 747 | `FlowerWall` | 花墙 | — | 68 | ·/·/· | — |
| 748 | `Jetpack` | 喷气背包 | — | — | ·/·/· | — |
| 749 | `ButterflyWings` | 蝴蝶之翼 | — | — | ·/·/· | — |
| 750 | `CactusWall` | 仙人掌墙 | — | 72 | ·/·/· | — |
| 751 | `Cloud` | 克劳德 | 189 | — | ·/·/· | — |
| 752 | `CloudWall` | 云墙 | — | 73 | ·/·/· | — |
| 753 | `Seaweed` | 海草 | — | — | ·/·/· | — |
| 754 | `RuneHat` | 符文帽 | — | — | ·/·/· | — |
| 755 | `RuneRobe` | 符文长袍 | — | — | ·/·/· | — |
| 756 | `MushroomSpear` | 蘑菇长矛 | — | — | ·/·/· | 60 |
| 757 | `TerraBlade` | 泰拉刃 | — | — | ·/·/· | 85 |
| 758 | `GrenadeLauncher` | 手榴弹发射器 | — | — | ·/·/· | 60 |
| 759 | `RocketLauncher` | 火箭发射器 | — | — | ·/·/· | 55 |
| 760 | `ProximityMineLauncher` | 感应雷发射器 | — | — | ·/·/· | 80 |
| 761 | `FairyWings` | 仙灵之翼 | — | — | ·/·/· | — |
| 762 | `SlimeBlock` | 史莱姆块 | 193 | — | ·/·/· | — |
| 763 | `FleshBlock` | 血肉块 | 195 | — | ·/·/· | — |
| 764 | `MushroomWall` | 蘑菇墙 | — | 74 | ·/·/· | — |
| 765 | `RainCloud` | 雨云 | 196 | — | ·/·/· | — |
| 766 | `BoneBlock` | 骨块 | 194 | — | ·/·/· | — |
| 767 | `FrozenSlimeBlock` | 冰冻史莱姆块 | 197 | — | ·/·/· | — |
| 768 | `BoneBlockWall` | 骨块墙 | — | 75 | ·/·/· | — |
| 769 | `SlimeBlockWall` | 史莱姆块墙 | — | 76 | ·/·/· | — |
| 770 | `FleshBlockWall` | 血肉块墙 | — | 77 | ·/·/· | — |
| 771 | `RocketI` | 初级火箭 | — | — | ·/·/· | 40 |
| 772 | `RocketII` | 二级火箭 | — | — | ·/·/· | 40 |
| 773 | `RocketIII` | 三级火箭 | — | — | ·/·/· | 65 |
| 774 | `RocketIV` | 四级火箭 | — | — | ·/·/· | 65 |
| 775 | `AsphaltBlock` | 沥青块 | 198 | — | ·/·/· | — |
| 776 | `CobaltPickaxe` | 钴镐 | — | — | 110/·/· | 10 |
| 777 | `MythrilPickaxe` | 秘银镐 | — | — | 150/·/· | 15 |
| 778 | `AdamantitePickaxe` | 精金镐 | — | — | 180/·/· | 20 |
| 779 | `Clentaminator` | 环境改造枪 | — | — | ·/·/· | — |
| 780 | `GreenSolution` | 绿溶液 | — | — | ·/·/· | — |
| 781 | `BlueSolution` | 蓝溶液 | — | — | ·/·/· | — |
| 782 | `PurpleSolution` | 紫溶液 | — | — | ·/·/· | — |
| 783 | `DarkBlueSolution` | 深蓝溶液 | — | — | ·/·/· | — |
| 784 | `RedSolution` | 红溶液 | — | — | ·/·/· | — |
| 785 | `HarpyWings` | 鸟妖之翼 | — | — | ·/·/· | — |
| 786 | `BoneWings` | 骨之翼 | — | — | ·/·/· | — |
| 787 | `Hammush` | 蘑菇锤 | — | — | ·/·/85 | 26 |
| 788 | `NettleBurst` | 爆裂藤蔓 | — | — | ·/·/· | 35 |
| 789 | `AnkhBanner` | 十字章旗 | 91 | — | ·/·/· | — |
| 790 | `SnakeBanner` | 蛇旗 | 91 | — | ·/·/· | — |
| 791 | `OmegaBanner` | 欧米茄旗 | 91 | — | ·/·/· | — |
| 792 | `CrimsonHelmet` | 猩红头盔 | — | — | ·/·/· | — |
| 793 | `CrimsonScalemail` | 猩红鳞甲 | — | — | ·/·/· | — |
| 794 | `CrimsonGreaves` | 猩红护胫 | — | — | ·/·/· | — |
| 795 | `BloodButcherer` | 血腥屠刀 | — | — | ·/·/· | 22 |
| 796 | `TendonBow` | 肌腱弓 | — | — | ·/·/· | 19 |
| 797 | `FleshGrinder` | 血肉锤 | — | — | ·/·/55 | 23 |
| 798 | `DeathbringerPickaxe` | 死亡使者镐 | — | — | 70/·/· | 12 |
| 799 | `BloodLustCluster` | 嗜血狂斧 | — | — | ·/15/· | 22 |
| 800 | `TheUndertaker` | 夺命枪 | — | — | ·/·/· | 19 |
| 801 | `TheMeatball` | 血肉之球 | — | — | ·/·/· | 17 |
| 802 | `TheRottedFork` | 腐叉 | — | — | ·/·/· | 17 |
| 803 | `EskimoHood` | 防雪兜帽 | — | — | ·/·/· | — |
| 804 | `EskimoCoat` | 防雪大衣 | — | — | ·/·/· | — |
| 805 | `EskimoPants` | 防雪裤 | — | — | ·/·/· | — |
| 806 | `LivingWoodChair` | 生命木椅 | 15 | — | ·/·/· | — |
| 807 | `CactusChair` | 仙人掌椅 | 15 | — | ·/·/· | — |
| 808 | `BoneChair` | 骨头椅 | 15 | — | ·/·/· | — |
| 809 | `FleshChair` | 血肉椅 | 15 | — | ·/·/· | — |
| 810 | `MushroomChair` | 蘑菇椅 | 15 | — | ·/·/· | — |
| 811 | `BoneWorkBench` | 骨头工作台 | 18 | — | ·/·/· | — |
| 812 | `CactusWorkBench` | 仙人掌工作台 | 18 | — | ·/·/· | — |
| 813 | `FleshWorkBench` | 血肉工作台 | 18 | — | ·/·/· | — |
| 814 | `MushroomWorkBench` | 蘑菇工作台 | 18 | — | ·/·/· | — |
| 815 | `SlimeWorkBench` | 史莱姆工作台 | 18 | — | ·/·/· | — |
| 816 | `CactusDoor` | 仙人掌门 | 10 | — | ·/·/· | — |
| 817 | `FleshDoor` | 血肉门 | 10 | — | ·/·/· | — |
| 818 | `MushroomDoor` | 蘑菇门 | 10 | — | ·/·/· | — |
| 819 | `LivingWoodDoor` | 生命木门 | 10 | — | ·/·/· | — |
| 820 | `BoneDoor` | 骨头门 | 10 | — | ·/·/· | — |
| 821 | `FlameWings` | 烈焰之翼 | — | — | ·/·/· | — |
| 822 | `FrozenWings` | 冰冻之翼 | — | — | ·/·/· | — |
| 823 | `GhostWings` | 幽灵之翼 | — | — | ·/·/· | — |
| 824 | `SunplateBlock` | 日盘块 | 202 | — | ·/·/· | — |
| 825 | `DiscWall` | 飞盘墙 | — | 82 | ·/·/· | — |
| 826 | `SkywareChair` | 天域椅 | 15 | — | ·/·/· | — |
| 827 | `BoneTable` | 骨头桌 | 14 | — | ·/·/· | — |
| 828 | `FleshTable` | 血肉桌 | 14 | — | ·/·/· | — |
| 829 | `LivingWoodTable` | 生命木桌 | 14 | — | ·/·/· | — |
| 830 | `SkywareTable` | 天域桌 | 14 | — | ·/·/· | — |
| 831 | `LivingWoodChest` | 生命木箱 | 21 | — | ·/·/· | — |
| 832 | `LivingWoodWand` | 生命木魔棒 | 191 | — | ·/·/· | — |
| 833 | `PurpleIceBlock` | 紫冰雪块 | 163 | — | ·/·/· | — |
| 834 | `PinkIceBlock` | 粉冰雪块 | 164 | — | ·/·/· | — |
| 835 | `RedIceBlock` | 红冰雪块 | 200 | — | ·/·/· | — |
| 836 | `CrimstoneBlock` | 猩红石块 | 203 | — | ·/·/· | — |
| 837 | `SkywareDoor` | 天域门 | 10 | — | ·/·/· | — |
| 838 | `SkywareChest` | 天域箱 | 21 | — | ·/·/· | — |
| 839 | `SteampunkHat` | 蒸汽朋克帽 | — | — | ·/·/· | — |
| 840 | `SteampunkShirt` | 蒸汽朋克衣 | — | — | ·/·/· | — |
| 841 | `SteampunkPants` | 蒸汽朋克裤 | — | — | ·/·/· | — |
| 842 | `BeeHat` | 蜜蜂帽 | — | — | ·/·/· | — |
| 843 | `BeeShirt` | 蜜蜂衣 | — | — | ·/·/· | — |
| 844 | `BeePants` | 蜜蜂裤 | — | — | ·/·/· | — |
| 845 | `WorldBanner` | 世界旗 | 91 | — | ·/·/· | — |
| 846 | `SunBanner` | 太阳旗 | 91 | — | ·/·/· | — |
| 847 | `GravityBanner` | 重力旗 | 91 | — | ·/·/· | — |
| 848 | `PharaohsMask` | 法老面具 | — | — | ·/·/· | — |
| 849 | `Actuator` | 制动器 | — | — | ·/·/· | — |
| 850 | `BlueWrench` | 蓝扳手 | — | — | ·/·/· | — |
| 851 | `GreenWrench` | 绿扳手 | — | — | ·/·/· | — |
| 852 | `BluePressurePlate` | 蓝压力板 | 135 | — | ·/·/· | — |
| 853 | `YellowPressurePlate` | 黄压力板 | 135 | — | ·/·/· | — |
| 854 | `DiscountCard` | 优惠卡 | — | — | ·/·/· | — |
| 855 | `LuckyCoin` | 幸运币 | — | — | ·/·/· | — |
| 856 | `UnicornonaStick` | 棒棒独角兽 | — | — | ·/·/· | — |
| 857 | `SandstorminaBottle` | 沙暴瓶 | — | — | ·/·/· | — |
| 858 | `BorealWoodSofa` | 针叶木沙发 | 89 | — | ·/·/· | — |
| 859 | `BeachBall` | 沙滩球 | — | — | ·/·/· | — |
| 860 | `CharmofMyths` | 神话护身符 | — | — | ·/·/· | — |
| 861 | `MoonShell` | 月亮贝壳 | — | — | ·/·/· | — |
| 862 | `StarVeil` | 星星面纱 | — | — | ·/·/· | — |
| 863 | `WaterWalkingBoots` | 水上漂靴 | — | — | ·/·/· | — |
| 864 | `Tiara` | 头冠 | — | — | ·/·/· | — |
| 865 | `PrincessDress` | 公主裙 | — | — | ·/·/· | — |
| 866 | `PharaohsRobe` | 法老长袍 | — | — | ·/·/· | — |
| 867 | `GreenCap` | 绿帽 | — | — | ·/·/· | — |
| 868 | `MushroomCap` | 蘑菇帽 | — | — | ·/·/· | — |
| 869 | `TamOShanter` | 苏格兰便帽 | — | — | ·/·/· | — |
| 870 | `MummyMask` | 木乃伊面具 | — | — | ·/·/· | — |
| 871 | `MummyShirt` | 木乃伊衣 | — | — | ·/·/· | — |
| 872 | `MummyPants` | 木乃伊裤 | — | — | ·/·/· | — |
| 873 | `CowboyHat` | 牛仔帽 | — | — | ·/·/· | — |
| 874 | `CowboyJacket` | 牛仔夹克 | — | — | ·/·/· | — |
| 875 | `CowboyPants` | 牛仔裤 | — | — | ·/·/· | — |
| 876 | `PirateHat` | 海盗帽 | — | — | ·/·/· | — |
| 877 | `PirateShirt` | 海盗衣 | — | — | ·/·/· | — |
| 878 | `PiratePants` | 海盗裤 | — | — | ·/·/· | — |
| 879 | `VikingHelmet` | 维京海盗头盔 | — | — | ·/·/· | — |
| 880 | `CrimtaneOre` | 猩红矿 | 204 | — | ·/·/· | — |
| 881 | `CactusSword` | 仙人掌剑 | — | — | ·/·/· | 10 |
| 882 | `CactusPickaxe` | 仙人掌镐 | — | — | 35/·/· | 4 |
| 883 | `IceBrick` | 冰雪砖 | 206 | — | ·/·/· | — |
| 884 | `IceBrickWall` | 冰雪砖墙 | — | 84 | ·/·/· | — |
| 885 | `AdhesiveBandage` | 粘性绷带 | — | — | ·/·/· | — |
| 886 | `ArmorPolish` | 盔甲抛光剂 | — | — | ·/·/· | — |
| 887 | `Bezoar` | 牛黄 | — | — | ·/·/· | — |
| 888 | `Blindfold` | 蒙眼布 | — | — | ·/·/· | — |
| 889 | `FastClock` | 快走时钟 | — | — | ·/·/· | — |
| 890 | `Megaphone` | 扩音器 | — | — | ·/·/· | — |
| 891 | `Nazar` | 邪眼 | — | — | ·/·/· | — |
| 892 | `Vitamins` | 维生素 | — | — | ·/·/· | — |
| 893 | `TrifoldMap` | 三折地图 | — | — | ·/·/· | — |
| 894 | `CactusHelmet` | 仙人掌头盔 | — | — | ·/·/· | — |
| 895 | `CactusBreastplate` | 仙人掌胸甲 | — | — | ·/·/· | — |
| 896 | `CactusLeggings` | 仙人掌护腿 | — | — | ·/·/· | — |
| 897 | `PowerGlove` | 强力手套 | — | — | ·/·/· | — |
| 898 | `LightningBoots` | 闪电靴 | — | — | ·/·/· | — |
| 899 | `SunStone` | 太阳石 | — | — | ·/·/· | — |
| 900 | `MoonStone` | 月亮石 | — | — | ·/·/· | — |
| 901 | `ArmorBracing` | 盔甲背带 | — | — | ·/·/· | — |
| 902 | `MedicatedBandage` | 药用绷带 | — | — | ·/·/· | — |
| 903 | `ThePlan` | 计划书 | — | — | ·/·/· | — |
| 904 | `CountercurseMantra` | 反诅咒咒语 | — | — | ·/·/· | — |
| 905 | `CoinGun` | 钱币枪 | — | — | ·/·/· | — |
| 906 | `LavaCharm` | 熔岩护身符 | — | — | ·/·/· | — |
| 907 | `ObsidianWaterWalkingBoots` | 黑曜石水上漂靴 | — | — | ·/·/· | — |
| 908 | `LavaWaders` | 熔岩靴 | — | — | ·/·/· | — |
| 909 | `PureWaterFountain` | 纯净喷泉 | 207 | — | ·/·/· | — |
| 910 | `DesertWaterFountain` | 沙漠喷泉 | 207 | — | ·/·/· | — |
| 911 | `Shadewood` | 暗影木 | 208 | — | ·/·/· | — |
| 912 | `ShadewoodDoor` | 暗影木门 | 10 | — | ·/·/· | — |
| 913 | `ShadewoodPlatform` | 暗影木平台 | 19 | — | ·/·/· | — |
| 914 | `ShadewoodChest` | 暗影木箱 | 21 | — | ·/·/· | — |
| 915 | `ShadewoodChair` | 暗影木椅 | 15 | — | ·/·/· | — |
| 916 | `ShadewoodWorkBench` | 暗影木工作台 | 18 | — | ·/·/· | — |
| 917 | `ShadewoodTable` | 暗影木桌 | 14 | — | ·/·/· | — |
| 918 | `ShadewoodDresser` | 暗影木梳妆台 | 88 | — | ·/·/· | — |
| 919 | `ShadewoodPiano` | 暗影木钢琴 | 87 | — | ·/·/· | — |
| 920 | `ShadewoodBed` | 暗影木床 | 79 | — | ·/·/· | — |
| 921 | `ShadewoodSword` | 暗影木剑 | — | — | ·/·/· | 11 |
| 922 | `ShadewoodHammer` | 暗影木锤 | — | — | ·/·/40 | 7 |
| 923 | `ShadewoodBow` | 暗影木弓 | — | — | ·/·/· | 8 |
| 924 | `ShadewoodHelmet` | 暗影木头盔 | — | — | ·/·/· | — |
| 925 | `ShadewoodBreastplate` | 暗影木胸甲 | — | — | ·/·/· | — |
| 926 | `ShadewoodGreaves` | 暗影木护胫 | — | — | ·/·/· | — |
| 927 | `ShadewoodWall` | 暗影木墙 | — | 85 | ·/·/· | — |
| 928 | `Cannon` | 大炮 | 209 | — | ·/·/· | — |
| 929 | `Cannonball` | 炮弹 | — | — | ·/·/· | 300 |
| 930 | `FlareGun` | 信号枪 | — | — | ·/·/· | 2 |
| 931 | `Flare` | 照明弹 | — | — | ·/·/· | 1 |
| 932 | `BoneWand` | 白骨魔棒 | 194 | — | ·/·/· | — |
| 933 | `LeafWand` | 树叶魔棒 | 192 | — | ·/·/· | — |
| 934 | `FlyingCarpet` | 飞毯 | — | — | ·/·/· | — |
| 935 | `AvengerEmblem` | 复仇者徽章 | — | — | ·/·/· | — |
| 936 | `MechanicalGlove` | 机械手套 | — | — | ·/·/· | — |
| 937 | `LandMine` | 地雷 | 210 | — | ·/·/· | — |
| 938 | `PaladinsShield` | 圣骑士护盾 | — | — | ·/·/· | — |
| 939 | `WebSlinger` | 蛛丝吊索 | — | — | ·/·/· | — |
| 940 | `JungleWaterFountain` | 丛林喷泉 | 207 | — | ·/·/· | — |
| 941 | `IcyWaterFountain` | 冰雪喷泉 | 207 | — | ·/·/· | — |
| 942 | `CorruptWaterFountain` | 腐化喷泉 | 207 | — | ·/·/· | — |
| 943 | `CrimsonWaterFountain` | 猩红喷泉 | 207 | — | ·/·/· | — |
| 944 | `HallowedWaterFountain` | 神圣喷泉 | 207 | — | ·/·/· | — |
| 945 | `BloodWaterFountain` | 血水喷泉 | 207 | — | ·/·/· | — |
| 946 | `Umbrella` | 伞 | — | — | ·/·/· | 10 |
| 947 | `ChlorophyteOre` | 叶绿矿 | 211 | — | ·/·/· | — |
| 948 | `SteampunkWings` | 蒸汽朋克之翼 | — | — | ·/·/· | — |
| 949 | `Snowball` | 雪球 | — | — | ·/·/· | 8 |
| 950 | `IceSkates` | 溜冰鞋 | — | — | ·/·/· | — |
| 951 | `SnowballLauncher` | 雪球发射器 | 212 | — | ·/·/· | — |
| 952 | `WebCoveredChest` | 蛛丝箱 | 21 | — | ·/·/· | — |
| 953 | `ClimbingClaws` | 攀爬爪 | — | — | ·/·/· | — |
| 954 | `AncientIronHelmet` | 远古铁头盔 | — | — | ·/·/· | — |
| 955 | `AncientGoldHelmet` | 远古金头盔 | — | — | ·/·/· | — |
| 956 | `AncientShadowHelmet` | 远古暗影头盔 | — | — | ·/·/· | — |
| 957 | `AncientShadowScalemail` | 远古暗影鳞甲 | — | — | ·/·/· | — |
| 958 | `AncientShadowGreaves` | 远古暗影护胫 | — | — | ·/·/· | — |
| 959 | `AncientNecroHelmet` | 远古死灵头盔 | — | — | ·/·/· | — |
| 960 | `AncientCobaltHelmet` | 远古钴头盔 | — | — | ·/·/· | — |
| 961 | `AncientCobaltBreastplate` | 远古钴胸甲 | — | — | ·/·/· | — |
| 962 | `AncientCobaltLeggings` | 远古钴护腿 | — | — | ·/·/· | — |
| 963 | `BlackBelt` | 黑腰带 | — | — | ·/·/· | — |
| 964 | `Boomstick` | 三发猎枪 | — | — | ·/·/· | 14 |
| 965 | `Rope` | 绳 | 213 | — | ·/·/· | — |
| 966 | `Campfire` | 篝火 | 215 | — | ·/·/· | — |
| 967 | `Marshmallow` | 棉花糖 | — | — | ·/·/· | — |
| 968 | `MarshmallowonaStick` | 棒棒棉花糖 | — | — | ·/·/· | — |
| 969 | `CookedMarshmallow` | 熟棉花糖 | — | — | ·/·/· | — |
| 970 | `RedRocket` | 红火箭 | 216 | — | ·/·/· | — |
| 971 | `GreenRocket` | 绿火箭 | 216 | — | ·/·/· | — |
| 972 | `BlueRocket` | 蓝火箭 | 216 | — | ·/·/· | — |
| 973 | `YellowRocket` | 黄火箭 | 216 | — | ·/·/· | — |
| 974 | `IceTorch` | 冰雪火把 | 4 | — | ·/·/· | — |
| 975 | `ShoeSpikes` | 鞋钉 | — | — | ·/·/· | — |
| 976 | `TigerClimbingGear` | 猛虎攀爬装备 | — | — | ·/·/· | — |
| 977 | `Tabi` | 分趾厚底袜 | — | — | ·/·/· | — |
| 978 | `PinkEskimoHood` | 粉色防雪兜帽 | — | — | ·/·/· | — |
| 979 | `PinkEskimoCoat` | 粉色防雪大衣 | — | — | ·/·/· | — |
| 980 | `PinkEskimoPants` | 粉色防雪裤 | — | — | ·/·/· | — |
| 981 | `PinkThread` | 粉线 | — | — | ·/·/· | — |
| 982 | `ManaRegenerationBand` | 魔力再生手环 | — | — | ·/·/· | — |
| 983 | `SandstorminaBalloon` | 沙暴气球 | — | — | ·/·/· | — |
| 984 | `MasterNinjaGear` | 忍者大师装备 | — | — | ·/·/· | — |
| 985 | `RopeCoil` | 绳圈 | — | — | ·/·/· | — |
| 986 | `Blowgun` | 吹箭筒 | — | — | ·/·/· | 27 |
| 987 | `BlizzardinaBottle` | 暴雪瓶 | — | — | ·/·/· | — |
| 988 | `FrostburnArrow` | 霜冻箭 | — | — | ·/·/· | 7 |
| 989 | `EnchantedSword` | 附魔剑 | — | — | ·/·/· | 23 |
| 990 | `PickaxeAxe` | 镐斧 | — | — | 200/22/· | 35 |
| 991 | `CobaltWaraxe` | 钴战斧 | — | — | ·/14/· | 33 |
| 992 | `MythrilWaraxe` | 秘银战斧 | — | — | ·/17/· | 39 |
| 993 | `AdamantiteWaraxe` | 精金战斧 | — | — | ·/20/· | 43 |
| 994 | `EatersBone` | 吞噬怪骨头 | — | — | ·/·/· | — |
| 995 | `BlendOMatic` | 搅拌机 | 217 | — | ·/·/· | — |
| 996 | `MeatGrinder` | 绞肉机 | 218 | — | ·/·/· | — |
| 997 | `Extractinator` | 提炼机 | 219 | — | ·/·/· | — |
| 998 | `Solidifier` | 固化机 | 220 | — | ·/·/· | — |
| 999 | `Amber` | 琥珀 | 178 | — | ·/·/· | — |
| 1000 | `ConfettiGun` | 彩纸枪 | — | — | ·/·/· | — |
| 1001 | `ChlorophyteMask` | 叶绿面具 | — | — | ·/·/· | — |
| 1002 | `ChlorophyteHelmet` | 叶绿头盔 | — | — | ·/·/· | — |
| 1003 | `ChlorophyteHeadgear` | 叶绿头饰 | — | — | ·/·/· | — |
| 1004 | `ChlorophytePlateMail` | 叶绿板甲 | — | — | ·/·/· | — |
| 1005 | `ChlorophyteGreaves` | 叶绿护胫 | — | — | ·/·/· | — |
| 1006 | `ChlorophyteBar` | 叶绿锭 | 239 | — | ·/·/· | — |
| 1007 | `RedDye` | 红染料 | — | — | ·/·/· | — |
| 1008 | `OrangeDye` | 橙染料 | — | — | ·/·/· | — |
| 1009 | `YellowDye` | 黄染料 | — | — | ·/·/· | — |
| 1010 | `LimeDye` | 橙绿染料 | — | — | ·/·/· | — |
| 1011 | `GreenDye` | 绿染料 | — | — | ·/·/· | — |
| 1012 | `TealDye` | 青绿染料 | — | — | ·/·/· | — |
| 1013 | `CyanDye` | 青染料 | — | — | ·/·/· | — |
| 1014 | `SkyBlueDye` | 天蓝染料 | — | — | ·/·/· | — |
| 1015 | `BlueDye` | 蓝染料 | — | — | ·/·/· | — |
| 1016 | `PurpleDye` | 紫染料 | — | — | ·/·/· | — |
| 1017 | `VioletDye` | 蓝紫染料 | — | — | ·/·/· | — |
| 1018 | `PinkDye` | 粉染料 | — | — | ·/·/· | — |
| 1019 | `RedandBlackDye` | 红黑染料 | — | — | ·/·/· | — |
| 1020 | `OrangeandBlackDye` | 橙黑染料 | — | — | ·/·/· | — |
| 1021 | `YellowandBlackDye` | 黄黑染料 | — | — | ·/·/· | — |
| 1022 | `LimeandBlackDye` | 橙绿黑染料 | — | — | ·/·/· | — |
| 1023 | `GreenandBlackDye` | 绿黑染料 | — | — | ·/·/· | — |
| 1024 | `TealandBlackDye` | 青绿黑染料 | — | — | ·/·/· | — |
| 1025 | `CyanandBlackDye` | 青黑染料 | — | — | ·/·/· | — |
| 1026 | `SkyBlueandBlackDye` | 天蓝黑染料 | — | — | ·/·/· | — |
| 1027 | `BlueandBlackDye` | 蓝黑染料 | — | — | ·/·/· | — |
| 1028 | `PurpleandBlackDye` | 紫黑染料 | — | — | ·/·/· | — |
| 1029 | `VioletandBlackDye` | 蓝紫黑染料 | — | — | ·/·/· | — |
| 1030 | `PinkandBlackDye` | 粉黑染料 | — | — | ·/·/· | — |
| 1031 | `FlameDye` | 红焰染料 | — | — | ·/·/· | — |
| 1032 | `FlameAndBlackDye` | 红焰黑染料 | — | — | ·/·/· | — |
| 1033 | `GreenFlameDye` | 绿焰染料 | — | — | ·/·/· | — |
| 1034 | `GreenFlameAndBlackDye` | 绿焰黑染料 | — | — | ·/·/· | — |
| 1035 | `BlueFlameDye` | 蓝焰染料 | — | — | ·/·/· | — |
| 1036 | `BlueFlameAndBlackDye` | 蓝焰黑染料 | — | — | ·/·/· | — |
| 1037 | `SilverDye` | 银染料 | — | — | ·/·/· | — |
| 1038 | `BrightRedDye` | 淡红染料 | — | — | ·/·/· | — |
| 1039 | `BrightOrangeDye` | 淡橙染料 | — | — | ·/·/· | — |
| 1040 | `BrightYellowDye` | 淡黄染料 | — | — | ·/·/· | — |
| 1041 | `BrightLimeDye` | 淡橙绿染料 | — | — | ·/·/· | — |
| 1042 | `BrightGreenDye` | 淡绿染料 | — | — | ·/·/· | — |
| 1043 | `BrightTealDye` | 淡青绿染料 | — | — | ·/·/· | — |
| 1044 | `BrightCyanDye` | 淡青染料 | — | — | ·/·/· | — |
| 1045 | `BrightSkyBlueDye` | 淡天蓝染料 | — | — | ·/·/· | — |
| 1046 | `BrightBlueDye` | 淡蓝染料 | — | — | ·/·/· | — |
| 1047 | `BrightPurpleDye` | 淡紫染料 | — | — | ·/·/· | — |
| 1048 | `BrightVioletDye` | 淡蓝紫染料 | — | — | ·/·/· | — |
| 1049 | `BrightPinkDye` | 淡粉染料 | — | — | ·/·/· | — |
| 1050 | `BlackDye` | 黑染料 | — | — | ·/·/· | — |
| 1051 | `RedandSilverDye` | 红银染料 | — | — | ·/·/· | — |
| 1052 | `OrangeandSilverDye` | 橙银染料 | — | — | ·/·/· | — |
| 1053 | `YellowandSilverDye` | 黄银染料 | — | — | ·/·/· | — |
| 1054 | `LimeandSilverDye` | 橙绿银染料 | — | — | ·/·/· | — |
| 1055 | `GreenandSilverDye` | 绿银染料 | — | — | ·/·/· | — |
| 1056 | `TealandSilverDye` | 青绿银染料 | — | — | ·/·/· | — |
| 1057 | `CyanandSilverDye` | 青银染料 | — | — | ·/·/· | — |
| 1058 | `SkyBlueandSilverDye` | 天蓝银染料 | — | — | ·/·/· | — |
| 1059 | `BlueandSilverDye` | 蓝银染料 | — | — | ·/·/· | — |
| 1060 | `PurpleandSilverDye` | 紫银染料 | — | — | ·/·/· | — |
| 1061 | `VioletandSilverDye` | 蓝紫银染料 | — | — | ·/·/· | — |
| 1062 | `PinkandSilverDye` | 粉银染料 | — | — | ·/·/· | — |
| 1063 | `IntenseFlameDye` | 亮红焰染料 | — | — | ·/·/· | — |
| 1064 | `IntenseGreenFlameDye` | 亮绿焰染料 | — | — | ·/·/· | — |
| 1065 | `IntenseBlueFlameDye` | 亮蓝焰染料 | — | — | ·/·/· | — |
| 1066 | `RainbowDye` | 彩虹染料 | — | — | ·/·/· | — |
| 1067 | `IntenseRainbowDye` | 亮彩虹染料 | — | — | ·/·/· | — |
| 1068 | `YellowGradientDye` | 渐变黄染料 | — | — | ·/·/· | — |
| 1069 | `CyanGradientDye` | 渐变青染料 | — | — | ·/·/· | — |
| 1070 | `VioletGradientDye` | 渐变蓝紫染料 | — | — | ·/·/· | — |
| 1071 | `Paintbrush` | 漆刷 | — | — | ·/·/· | — |
| 1072 | `PaintRoller` | 涂漆滚刷 | — | — | ·/·/· | — |
| 1073 | `RedPaint` | 红漆 | — | — | ·/·/· | — |
| 1074 | `OrangePaint` | 橙漆 | — | — | ·/·/· | — |
| 1075 | `YellowPaint` | 黄漆 | — | — | ·/·/· | — |
| 1076 | `LimePaint` | 橙绿漆 | — | — | ·/·/· | — |
| 1077 | `GreenPaint` | 绿漆 | — | — | ·/·/· | — |
| 1078 | `TealPaint` | 青绿漆 | — | — | ·/·/· | — |
| 1079 | `CyanPaint` | 青漆 | — | — | ·/·/· | — |
| 1080 | `SkyBluePaint` | 天蓝漆 | — | — | ·/·/· | — |
| 1081 | `BluePaint` | 蓝漆 | — | — | ·/·/· | — |
| 1082 | `PurplePaint` | 紫漆 | — | — | ·/·/· | — |
| 1083 | `VioletPaint` | 蓝紫漆 | — | — | ·/·/· | — |
| 1084 | `PinkPaint` | 粉漆 | — | — | ·/·/· | — |
| 1085 | `DeepRedPaint` | 深红漆 | — | — | ·/·/· | — |
| 1086 | `DeepOrangePaint` | 深橙漆 | — | — | ·/·/· | — |
| 1087 | `DeepYellowPaint` | 深黄漆 | — | — | ·/·/· | — |
| 1088 | `DeepLimePaint` | 深橙绿漆 | — | — | ·/·/· | — |
| 1089 | `DeepGreenPaint` | 深绿漆 | — | — | ·/·/· | — |
| 1090 | `DeepTealPaint` | 深青绿漆 | — | — | ·/·/· | — |
| 1091 | `DeepCyanPaint` | 深青漆 | — | — | ·/·/· | — |
| 1092 | `DeepSkyBluePaint` | 深天蓝漆 | — | — | ·/·/· | — |
| 1093 | `DeepBluePaint` | 深蓝漆 | — | — | ·/·/· | — |
| 1094 | `DeepPurplePaint` | 深紫漆 | — | — | ·/·/· | — |
| 1095 | `DeepVioletPaint` | 深蓝紫漆 | — | — | ·/·/· | — |
| 1096 | `DeepPinkPaint` | 深粉漆 | — | — | ·/·/· | — |
| 1097 | `BlackPaint` | 黑漆 | — | — | ·/·/· | — |
| 1098 | `WhitePaint` | 白漆 | — | — | ·/·/· | — |
| 1099 | `GrayPaint` | 灰漆 | — | — | ·/·/· | — |
| 1100 | `PaintScraper` | 漆铲 | — | — | ·/·/· | — |
| 1101 | `LihzahrdBrick` | 丛林蜥蜴砖 | 226 | — | ·/·/· | — |
| 1102 | `LihzahrdBrickWall` | 丛林蜥蜴砖墙 | — | 112 | ·/·/· | — |
| 1103 | `SlushBlock` | 雪泥块 | 224 | — | ·/·/· | — |
| 1104 | `PalladiumOre` | 钯金矿 | 221 | — | ·/·/· | — |
| 1105 | `OrichalcumOre` | 山铜矿 | 222 | — | ·/·/· | — |
| 1106 | `TitaniumOre` | 钛金矿 | 223 | — | ·/·/· | — |
| 1107 | `TealMushroom` | 青绿蘑菇 | 227 | — | ·/·/· | — |
| 1108 | `GreenMushroom` | 绿蘑菇 | 227 | — | ·/·/· | — |
| 1109 | `SkyBlueFlower` | 天蓝花朵 | 227 | — | ·/·/· | — |
| 1110 | `YellowMarigold` | 黄万寿菊 | 227 | — | ·/·/· | — |
| 1111 | `BlueBerries` | 蓝浆果 | 227 | — | ·/·/· | — |
| 1112 | `LimeKelp` | 橙绿海藻 | 227 | — | ·/·/· | — |
| 1113 | `PinkPricklyPear` | 粉仙人球 | — | — | ·/·/· | — |
| 1114 | `OrangeBloodroot` | 橙血根草 | 227 | — | ·/·/· | — |
| 1115 | `RedHusk` | 红外壳 | — | — | ·/·/· | — |
| 1116 | `CyanHusk` | 青外壳 | — | — | ·/·/· | — |
| 1117 | `VioletHusk` | 蓝紫外壳 | — | — | ·/·/· | — |
| 1118 | `PurpleMucos` | 紫黏液 | — | — | ·/·/· | — |
| 1119 | `BlackInk` | 黑墨水 | — | — | ·/·/· | — |
| 1120 | `DyeVat` | 染缸 | 228 | — | ·/·/· | — |
| 1121 | `BeeGun` | 蜜蜂枪 | — | — | ·/·/· | 9 |
| 1122 | `PossessedHatchet` | 疯狂飞斧 | — | — | ·/·/· | 80 |
| 1123 | `BeeKeeper` | 养蜂人 | — | — | ·/·/· | 30 |
| 1124 | `Hive` | 蜂巢 | — | — | ·/·/· | — |
| 1125 | `HoneyBlock` | 蜂蜜块 | 229 | — | ·/·/· | — |
| 1126 | `HiveWall` | 蜂巢墙 | — | 108 | ·/·/· | — |
| 1127 | `CrispyHoneyBlock` | 松脆蜂蜜块 | 230 | — | ·/·/· | — |
| 1128 | `HoneyBucket` | 蜂蜜桶 | — | — | ·/·/· | — |
| 1129 | `HiveWand` | 蜂巢魔棒 | 225 | — | ·/·/· | — |
| 1130 | `Beenade` | 蜜蜂手榴弹 | — | — | ·/·/· | 12 |
| 1131 | `GravityGlobe` | 重力球 | — | — | ·/·/· | — |
| 1132 | `HoneyComb` | 蜂窝 | — | — | ·/·/· | — |
| 1133 | `Abeemination` | 憎恶之蜂 | — | — | ·/·/· | — |
| 1134 | `BottledHoney` | 蜂蜜瓶 | — | — | ·/·/· | — |
| 1135 | `RainHat` | 雨帽 | — | — | ·/·/· | — |
| 1136 | `RainCoat` | 雨衣 | — | — | ·/·/· | — |
| 1137 | `LihzahrdDoor` | 丛林蜥蜴门 | 10 | — | ·/·/· | — |
| 1138 | `DungeonDoor` | 地牢门 | 10 | — | ·/·/· | — |
| 1139 | `LeadDoor` | 铅门 | 10 | — | ·/·/· | — |
| 1140 | `IronDoor` | 铁门 | 10 | — | ·/·/· | — |
| 1141 | `TempleKey` | 神庙钥匙 | — | — | ·/·/· | — |
| 1142 | `LihzahrdChest` | 丛林蜥蜴箱 | 21 | — | ·/·/· | — |
| 1143 | `LihzahrdChair` | 丛林蜥蜴椅 | 15 | — | ·/·/· | — |
| 1144 | `LihzahrdTable` | 丛林蜥蜴桌 | 14 | — | ·/·/· | — |
| 1145 | `LihzahrdWorkBench` | 丛林蜥蜴工作台 | 18 | — | ·/·/· | — |
| 1146 | `SuperDartTrap` | 超级飞镖机关 | 137 | — | ·/·/· | — |
| 1147 | `FlameTrap` | 烈焰机关 | 137 | — | ·/·/· | — |
| 1148 | `SpikyBallTrap` | 尖球机关 | 137 | — | ·/·/· | — |
| 1149 | `SpearTrap` | 长矛机关 | 137 | — | ·/·/· | — |
| 1150 | `WoodenSpike` | 木尖刺 | 232 | — | ·/·/· | — |
| 1151 | `LihzahrdPressurePlate` | 丛林蜥蜴压力板 | 135 | — | ·/·/· | — |
| 1152 | `LihzahrdStatue` | 丛林蜥蜴雕像 | 105 | — | ·/·/· | — |
| 1153 | `LihzahrdWatcherStatue` | 丛林蜥蜴看守人雕像 | 105 | — | ·/·/· | — |
| 1154 | `LihzahrdGuardianStatue` | 丛林蜥蜴守卫雕像 | 105 | — | ·/·/· | — |
| 1155 | `WaspGun` | 胡蜂枪 | — | — | ·/·/· | 31 |
| 1156 | `PiranhaGun` | 食人鱼枪 | — | — | ·/·/· | 38 |
| 1157 | `PygmyStaff` | 矮人法杖 | — | — | ·/·/· | 40 |
| 1158 | `PygmyNecklace` | 矮人项链 | — | — | ·/·/· | — |
| 1159 | `TikiMask` | 提基面具 | — | — | ·/·/· | — |
| 1160 | `TikiShirt` | 提基衣 | — | — | ·/·/· | — |
| 1161 | `TikiPants` | 提基裤 | — | — | ·/·/· | — |
| 1162 | `LeafWings` | 叶之翼 | — | — | ·/·/· | — |
| 1163 | `BlizzardinaBalloon` | 暴雪气球 | — | — | ·/·/· | — |
| 1164 | `BundleofBalloons` | 气球束 | — | — | ·/·/· | — |
| 1165 | `BatWings` | 蝙蝠之翼 | — | — | ·/·/· | — |
| 1166 | `BoneSword` | 骨剑 | — | — | ·/·/· | 19 |
| 1167 | `HerculesBeetle` | 大力士甲虫 | — | — | ·/·/· | — |
| 1168 | `SmokeBomb` | 烟雾弹 | — | — | ·/·/· | — |
| 1169 | `BoneKey` | 骨头钥匙 | — | — | ·/·/· | — |
| 1170 | `Nectar` | 花蜜 | — | — | ·/·/· | — |
| 1171 | `TikiTotem` | 提基图腾 | — | — | ·/·/· | — |
| 1172 | `LizardEgg` | 蜥蜴蛋 | — | — | ·/·/· | — |
| 1173 | `GraveMarker` | 墓石碑 | 85 | — | ·/·/· | — |
| 1174 | `CrossGraveMarker` | 十字墓石碑 | 85 | — | ·/·/· | — |
| 1175 | `Headstone` | 碑石 | 85 | — | ·/·/· | — |
| 1176 | `Gravestone` | 墓碑 | 85 | — | ·/·/· | — |
| 1177 | `Obelisk` | 方尖碑 | 85 | — | ·/·/· | — |
| 1178 | `LeafBlower` | 吹叶机 | — | — | ·/·/· | 48 |
| 1179 | `ChlorophyteBullet` | 叶绿弹 | — | — | ·/·/· | 9 |
| 1180 | `ParrotCracker` | 鹦鹉饼干 | — | — | ·/·/· | — |
| 1181 | `StrangeGlowingMushroom` | 奇异发光蘑菇 | — | — | ·/·/· | — |
| 1182 | `Seedling` | 幼苗 | — | — | ·/·/· | — |
| 1183 | `WispinaBottle` | 妖灵瓶 | — | — | ·/·/· | — |
| 1184 | `PalladiumBar` | 钯金锭 | 239 | — | ·/·/· | — |
| 1185 | `PalladiumSword` | 钯金剑 | — | — | ·/·/· | 49 |
| 1186 | `PalladiumPike` | 钯金刺矛 | — | — | ·/·/· | 44 |
| 1187 | `PalladiumRepeater` | 钯金连弩 | — | — | ·/·/· | 37 |
| 1188 | `PalladiumPickaxe` | 钯金镐 | — | — | 130/·/· | 12 |
| 1189 | `PalladiumDrill` | 钯金钻头 | — | — | 130/·/· | 12 |
| 1190 | `PalladiumChainsaw` | 钯金链锯 | — | — | ·/15/· | 26 |
| 1191 | `OrichalcumBar` | 山铜锭 | 239 | — | ·/·/· | — |
| 1192 | `OrichalcumSword` | 山铜剑 | — | — | ·/·/· | 59 |
| 1193 | `OrichalcumHalberd` | 山铜长戟 | — | — | ·/·/· | 46 |
| 1194 | `OrichalcumRepeater` | 山铜连弩 | — | — | ·/·/· | 40 |
| 1195 | `OrichalcumPickaxe` | 山铜镐 | — | — | 165/·/· | 17 |
| 1196 | `OrichalcumDrill` | 山铜钻头 | — | — | 165/·/· | 17 |
| 1197 | `OrichalcumChainsaw` | 山铜链锯 | — | — | ·/18/· | 31 |
| 1198 | `TitaniumBar` | 钛金锭 | 239 | — | ·/·/· | — |
| 1199 | `TitaniumSword` | 钛金剑 | — | — | ·/·/· | 61 |
| 1200 | `TitaniumTrident` | 钛金三叉戟 | — | — | ·/·/· | 48 |
| 1201 | `TitaniumRepeater` | 钛金连弩 | — | — | ·/·/· | 43 |
| 1202 | `TitaniumPickaxe` | 钛金镐 | — | — | 190/·/· | 27 |
| 1203 | `TitaniumDrill` | 钛金钻头 | — | — | 190/·/· | 27 |
| 1204 | `TitaniumChainsaw` | 钛金链锯 | — | — | ·/21/· | 34 |
| 1205 | `PalladiumMask` | 钯金面具 | — | — | ·/·/· | — |
| 1206 | `PalladiumHelmet` | 钯金头盔 | — | — | ·/·/· | — |
| 1207 | `PalladiumHeadgear` | 钯金头饰 | — | — | ·/·/· | — |
| 1208 | `PalladiumBreastplate` | 钯金胸甲 | — | — | ·/·/· | — |
| 1209 | `PalladiumLeggings` | 钯金护腿 | — | — | ·/·/· | — |
| 1210 | `OrichalcumMask` | 山铜面具 | — | — | ·/·/· | — |
| 1211 | `OrichalcumHelmet` | 山铜头盔 | — | — | ·/·/· | — |
| 1212 | `OrichalcumHeadgear` | 山铜头饰 | — | — | ·/·/· | — |
| 1213 | `OrichalcumBreastplate` | 山铜胸甲 | — | — | ·/·/· | — |
| 1214 | `OrichalcumLeggings` | 山铜护腿 | — | — | ·/·/· | — |
| 1215 | `TitaniumMask` | 钛金面具 | — | — | ·/·/· | — |
| 1216 | `TitaniumHelmet` | 钛金头盔 | — | — | ·/·/· | — |
| 1217 | `TitaniumHeadgear` | 钛金头饰 | — | — | ·/·/· | — |
| 1218 | `TitaniumBreastplate` | 钛金胸甲 | — | — | ·/·/· | — |
| 1219 | `TitaniumLeggings` | 钛金护腿 | — | — | ·/·/· | — |
| 1220 | `OrichalcumAnvil` | 山铜砧 | 134 | — | ·/·/· | — |
| 1221 | `TitaniumForge` | 钛金熔炉 | 133 | — | ·/·/· | — |
| 1222 | `PalladiumWaraxe` | 钯金战斧 | — | — | ·/15/· | 36 |
| 1223 | `OrichalcumWaraxe` | 山铜战斧 | — | — | ·/18/· | 41 |
| 1224 | `TitaniumWaraxe` | 钛金战斧 | — | — | ·/21/· | 44 |
| 1225 | `HallowedBar` | 神圣锭 | 239 | — | ·/·/· | — |
| 1226 | `ChlorophyteClaymore` | 叶绿双刃刀 | — | — | ·/·/· | 95 |
| 1227 | `ChlorophyteSaber` | 叶绿军刀 | — | — | ·/·/· | 57 |
| 1228 | `ChlorophytePartisan` | 叶绿镋 | — | — | ·/·/· | 49 |
| 1229 | `ChlorophyteShotbow` | 叶绿连弩 | — | — | ·/·/· | 34 |
| 1230 | `ChlorophytePickaxe` | 叶绿镐 | — | — | 200/·/· | 40 |
| 1231 | `ChlorophyteDrill` | 叶绿钻头 | — | — | 200/·/· | 35 |
| 1232 | `ChlorophyteChainsaw` | 叶绿链锯 | — | — | ·/23/· | 50 |
| 1233 | `ChlorophyteGreataxe` | 叶绿巨斧 | — | — | ·/23/· | 70 |
| 1234 | `ChlorophyteWarhammer` | 叶绿战锤 | — | — | ·/·/90 | 80 |
| 1235 | `ChlorophyteArrow` | 叶绿箭 | — | — | ·/·/· | 16 |
| 1236 | `AmethystHook` | 紫晶钩 | — | — | ·/·/· | — |
| 1237 | `TopazHook` | 黄玉钩 | — | — | ·/·/· | — |
| 1238 | `SapphireHook` | 蓝玉钩 | — | — | ·/·/· | — |
| 1239 | `EmeraldHook` | 翡翠钩 | — | — | ·/·/· | — |
| 1240 | `RubyHook` | 红玉钩 | — | — | ·/·/· | — |
| 1241 | `DiamondHook` | 钻石钩 | — | — | ·/·/· | — |
| 1242 | `AmberMosquito` | 蚊子琥珀 | — | — | ·/·/· | — |
| 1243 | `UmbrellaHat` | 伞帽 | — | — | ·/·/· | — |
| 1244 | `NimbusRod` | 雨云魔杖 | — | — | ·/·/· | 30 |
| 1245 | `OrangeTorch` | 橙火把 | 4 | — | ·/·/· | — |
| 1246 | `CrimsandBlock` | 猩红沙块 | 234 | — | ·/·/· | — |
| 1247 | `BeeCloak` | 蜜蜂斗篷 | — | — | ·/·/· | — |
| 1248 | `EyeoftheGolem` | 石巨人之眼 | — | — | ·/·/· | — |
| 1249 | `HoneyBalloon` | 蜂蜜气球 | — | — | ·/·/· | — |
| 1250 | `BlueHorseshoeBalloon` | 蓝马掌气球 | — | — | ·/·/· | — |
| 1251 | `WhiteHorseshoeBalloon` | 白马掌气球 | — | — | ·/·/· | — |
| 1252 | `YellowHorseshoeBalloon` | 黄马掌气球 | — | — | ·/·/· | — |
| 1253 | `FrozenTurtleShell` | 冰冻海龟壳 | — | — | ·/·/· | — |
| 1254 | `SniperRifle` | 狙击步枪 | — | — | ·/·/· | 185 |
| 1255 | `VenusMagnum` | 维纳斯万能枪 | — | — | ·/·/· | 50 |
| 1256 | `CrimsonRod` | 猩红魔杖 | — | — | ·/·/· | 12 |
| 1257 | `CrimtaneBar` | 猩红矿锭 | 239 | — | ·/·/· | — |
| 1258 | `Stynger` | 毒刺发射器 | — | — | ·/·/· | 45 |
| 1259 | `FlowerPow` | 花冠 | — | — | ·/·/· | 65 |
| 1260 | `RainbowGun` | 彩虹枪 | — | — | ·/·/· | 45 |
| 1261 | `StyngerBolt` | 毒刺矢 | — | — | ·/·/· | 17 |
| 1262 | `ChlorophyteJackhammer` | 叶绿手提钻 | — | — | ·/·/90 | 45 |
| 1263 | `Teleporter` | 传送机 | 235 | — | ·/·/· | — |
| 1264 | `FlowerofFrost` | 寒霜之花 | — | — | ·/·/· | 60 |
| 1265 | `Uzi` | 乌兹冲锋枪 | — | — | ·/·/· | 30 |
| 1266 | `MagnetSphere` | 磁球 | — | — | ·/·/· | 48 |
| 1267 | `PurpleStainedGlass` | 紫花窗玻璃 | — | 88 | ·/·/· | — |
| 1268 | `YellowStainedGlass` | 黄花窗玻璃 | — | 89 | ·/·/· | — |
| 1269 | `BlueStainedGlass` | 蓝花窗玻璃 | — | 90 | ·/·/· | — |
| 1270 | `GreenStainedGlass` | 绿花窗玻璃 | — | 91 | ·/·/· | — |
| 1271 | `RedStainedGlass` | 红花窗玻璃 | — | 92 | ·/·/· | — |
| 1272 | `MulticoloredStainedGlass` | 五彩花窗玻璃 | — | 93 | ·/·/· | — |
| 1273 | `SkeletronHand` | 骷髅王之手 | — | — | ·/·/· | — |
| 1274 | `Skull` | 骷髅头 | — | — | ·/·/· | — |
| 1275 | `BallaHat` | 巴拉帽 | — | — | ·/·/· | — |
| 1276 | `GangstaHat` | 黑帮帽 | — | — | ·/·/· | — |
| 1277 | `SailorHat` | 水手帽 | — | — | ·/·/· | — |
| 1278 | `EyePatch` | 眼罩 | — | — | ·/·/· | — |
| 1279 | `SailorShirt` | 水手衣 | — | — | ·/·/· | — |
| 1280 | `SailorPants` | 水手裤 | — | — | ·/·/· | — |
| 1281 | `SkeletronMask` | 骷髅王面具 | — | — | ·/·/· | — |
| 1282 | `AmethystRobe` | 紫晶长袍 | — | — | ·/·/· | — |
| 1283 | `TopazRobe` | 黄玉长袍 | — | — | ·/·/· | — |
| 1284 | `SapphireRobe` | 蓝玉长袍 | — | — | ·/·/· | — |
| 1285 | `EmeraldRobe` | 翡翠长袍 | — | — | ·/·/· | — |
| 1286 | `RubyRobe` | 红玉长袍 | — | — | ·/·/· | — |
| 1287 | `DiamondRobe` | 钻石长袍 | — | — | ·/·/· | — |
| 1288 | `WhiteTuxedoShirt` | 白西装衣 | — | — | ·/·/· | — |
| 1289 | `WhiteTuxedoPants` | 白西装裤 | — | — | ·/·/· | — |
| 1290 | `PanicNecklace` | 恐慌项链 | — | — | ·/·/· | — |
| 1291 | `LifeFruit` | 生命果 | — | — | ·/·/· | — |
| 1292 | `LihzahrdAltar` | 丛林蜥蜴祭坛 | 237 | — | ·/·/· | — |
| 1293 | `LihzahrdPowerCell` | 丛林蜥蜴电池 | — | — | ·/·/· | — |
| 1294 | `Picksaw` | 锯刃镐 | — | — | 210/25/· | 34 |
| 1295 | `HeatRay` | 高温射线枪 | — | — | ·/·/· | 90 |
| 1296 | `StaffofEarth` | 大地法杖 | — | — | ·/·/· | 125 |
| 1297 | `GolemFist` | 石巨人之拳 | — | — | ·/·/· | 90 |
| 1298 | `WaterChest` | 水中箱 | 21 | — | ·/·/· | — |
| 1299 | `Binoculars` | 双筒望远镜 | — | — | ·/·/· | — |
| 1300 | `RifleScope` | 步枪瞄准镜 | — | — | ·/·/· | — |
| 1301 | `DestroyerEmblem` | 毁灭者徽章 | — | — | ·/·/· | — |
| 1302 | `HighVelocityBullet` | 高射速子弹 | — | — | ·/·/· | 11 |
| 1303 | `JellyfishNecklace` | 水母项链 | — | — | ·/·/· | — |
| 1304 | `ZombieArm` | 僵尸臂 | — | — | ·/·/· | 15 |
| 1305 | `TheAxe` | 斧 | — | — | ·/35/100 | 72 |
| 1306 | `IceSickle` | 冰雪镰刀 | — | — | ·/·/· | 50 |
| 1307 | `ClothierVoodooDoll` | 服装商巫毒娃娃 | — | — | ·/·/· | — |
| 1308 | `PoisonStaff` | 剧毒法杖 | — | — | ·/·/· | 43 |
| 1309 | `SlimeStaff` | 史莱姆法杖 | — | — | ·/·/· | 8 |
| 1310 | `PoisonDart` | 毒镖 | — | — | ·/·/· | 10 |
| 1311 | `EyeSpring` | 弹簧眼 | — | — | ·/·/· | — |
| 1312 | `ToySled` | 玩具雪橇 | — | — | ·/·/· | — |
| 1313 | `BookofSkulls` | 骷髅头法书 | — | — | ·/·/· | 29 |
| 1314 | `KOCannon` | 致胜炮 | — | — | ·/·/· | 40 |
| 1315 | `PirateMap` | 海盗地图 | — | — | ·/·/· | — |
| 1316 | `TurtleHelmet` | 海龟头盔 | — | — | ·/·/· | — |
| 1317 | `TurtleScaleMail` | 海龟铠甲 | — | — | ·/·/· | — |
| 1318 | `TurtleLeggings` | 海龟护腿 | — | — | ·/·/· | — |
| 1319 | `SnowballCannon` | 雪球炮 | — | — | ·/·/· | 10 |
| 1320 | `BonePickaxe` | 骨镐 | — | — | 55/·/· | 8 |
| 1321 | `MagicQuiver` | 魔法箭袋 | — | — | ·/·/· | — |
| 1322 | `MagmaStone` | 岩浆石 | — | — | ·/·/· | — |
| 1323 | `ObsidianRose` | 黑曜石玫瑰 | — | — | ·/·/· | — |
| 1324 | `Bananarang` | 香蕉回旋镖 | — | — | ·/·/· | 45 |
| 1325 | `ChainKnife` | 链刀 | — | — | ·/·/· | 12 |
| 1326 | `RodofDiscord` | 混沌传送杖 | — | — | ·/·/· | — |
| 1327 | `DeathSickle` | 死神镰刀 | — | — | ·/·/· | 57 |
| 1328 | `TurtleShell` | 海龟壳 | — | — | ·/·/· | — |
| 1329 | `TissueSample` | 组织样本 | — | — | ·/·/· | — |
| 1330 | `Vertebrae` | 椎骨 | — | — | ·/·/· | — |
| 1331 | `BloodySpine` | 血腥脊椎 | — | — | ·/·/· | — |
| 1332 | `Ichor` | 灵液 | — | — | ·/·/· | — |
| 1333 | `IchorTorch` | 灵液火把 | 4 | — | ·/·/· | — |
| 1334 | `IchorArrow` | 灵液箭 | — | — | ·/·/· | 16 |
| 1335 | `IchorBullet` | 灵液弹 | — | — | ·/·/· | 13 |
| 1336 | `GoldenShower` | 黄金雨 | — | — | ·/·/· | 30 |
| 1337 | `BunnyCannon` | 兔兔炮 | 209 | — | ·/·/· | — |
| 1338 | `ExplosiveBunny` | 爆炸兔 | — | — | ·/·/· | 350 |
| 1339 | `VialofVenom` | 小瓶毒液 | — | — | ·/·/· | — |
| 1340 | `FlaskofVenom` | 大瓶毒液 | — | — | ·/·/· | — |
| 1341 | `VenomArrow` | 毒液箭 | — | — | ·/·/· | 19 |
| 1342 | `VenomBullet` | 毒液弹 | — | — | ·/·/· | 15 |
| 1343 | `FireGauntlet` | 烈火手套 | — | — | ·/·/· | — |
| 1344 | `Cog` | 齿轮 | 272 | — | ·/·/· | — |
| 1345 | `Confetti` | 彩纸 | — | — | ·/·/· | — |
| 1346 | `Nanites` | 纳米机器人 | — | — | ·/·/· | — |
| 1347 | `ExplosivePowder` | 爆炸粉 | — | — | ·/·/· | — |
| 1348 | `GoldDust` | 金尘 | — | — | ·/·/· | — |
| 1349 | `PartyBullet` | 派对弹 | — | — | ·/·/· | 10 |
| 1350 | `NanoBullet` | 纳米弹 | — | — | ·/·/· | 15 |
| 1351 | `ExplodingBullet` | 爆破弹 | — | — | ·/·/· | 10 |
| 1352 | `GoldenBullet` | 金子弹 | — | — | ·/·/· | 10 |
| 1353 | `FlaskofCursedFlames` | 诅咒焰瓶 | — | — | ·/·/· | — |
| 1354 | `FlaskofFire` | 烈火瓶 | — | — | ·/·/· | — |
| 1355 | `FlaskofGold` | 金瓶 | — | — | ·/·/· | — |
| 1356 | `FlaskofIchor` | 灵液瓶 | — | — | ·/·/· | — |
| 1357 | `FlaskofNanites` | 纳米机器人之瓶 | — | — | ·/·/· | — |
| 1358 | `FlaskofParty` | 派对瓶 | — | — | ·/·/· | — |
| 1359 | `FlaskofPoison` | 毒药瓶 | — | — | ·/·/· | — |
| 1360 | `EyeofCthulhuTrophy` | 克苏鲁之眼纪念章 | 240 | — | ·/·/· | — |
| 1361 | `EaterofWorldsTrophy` | 世界吞噬怪纪念章 | 240 | — | ·/·/· | — |
| 1362 | `BrainofCthulhuTrophy` | 克苏鲁之脑纪念章 | 240 | — | ·/·/· | — |
| 1363 | `SkeletronTrophy` | 骷髅王纪念章 | 240 | — | ·/·/· | — |
| 1364 | `QueenBeeTrophy` | 蜂王纪念章 | 240 | — | ·/·/· | — |
| 1365 | `WallofFleshTrophy` | 血肉墙纪念章 | 240 | — | ·/·/· | — |
| 1366 | `DestroyerTrophy` | 毁灭者纪念章 | 240 | — | ·/·/· | — |
| 1367 | `SkeletronPrimeTrophy` | 机械骷髅王纪念章 | 240 | — | ·/·/· | — |
| 1368 | `RetinazerTrophy` | 激光眼纪念章 | 240 | — | ·/·/· | — |
| 1369 | `SpazmatismTrophy` | 魔焰眼纪念章 | 240 | — | ·/·/· | — |
| 1370 | `PlanteraTrophy` | 世纪之花纪念章 | 240 | — | ·/·/· | — |
| 1371 | `GolemTrophy` | 石巨人纪念章 | 240 | — | ·/·/· | — |
| 1372 | `BloodMoonRising` | 血月升空 | 240 | — | ·/·/· | — |
| 1373 | `TheHangedMan` | 倒吊人 | 240 | — | ·/·/· | — |
| 1374 | `GloryoftheFire` | 烈火荣耀 | 240 | — | ·/·/· | — |
| 1375 | `BoneWarp` | 扭曲的骨头 | 240 | — | ·/·/· | — |
| 1376 | `WallSkeleton` | 壁挂骷髅 | 240 | — | ·/·/· | — |
| 1377 | `HangingSkeleton` | 吊挂骷髅 | 240 | — | ·/·/· | — |
| 1378 | `BlueSlabWall` | 蓝板墙 | — | 100 | ·/·/· | — |
| 1379 | `BlueTiledWall` | 蓝瓷砖墙 | — | 101 | ·/·/· | — |
| 1380 | `PinkSlabWall` | 粉板墙 | — | 102 | ·/·/· | — |
| 1381 | `PinkTiledWall` | 粉瓷砖墙 | — | 103 | ·/·/· | — |
| 1382 | `GreenSlabWall` | 绿板墙 | — | 104 | ·/·/· | — |
| 1383 | `GreenTiledWall` | 绿瓷砖墙 | — | 105 | ·/·/· | — |
| 1384 | `BlueBrickPlatform` | 蓝砖平台 | 19 | — | ·/·/· | — |
| 1385 | `PinkBrickPlatform` | 粉砖平台 | 19 | — | ·/·/· | — |
| 1386 | `GreenBrickPlatform` | 绿砖平台 | 19 | — | ·/·/· | — |
| 1387 | `MetalShelf` | 金属架 | 19 | — | ·/·/· | — |
| 1388 | `BrassShelf` | 黄铜架 | 19 | — | ·/·/· | — |
| 1389 | `WoodShelf` | 木架 | 19 | — | ·/·/· | — |
| 1390 | `BrassLantern` | 黄铜灯笼 | 42 | — | ·/·/· | — |
| 1391 | `CagedLantern` | 笼式灯笼 | 42 | — | ·/·/· | — |
| 1392 | `CarriageLantern` | 马车灯笼 | 42 | — | ·/·/· | — |
| 1393 | `AlchemyLantern` | 炼金灯笼 | 42 | — | ·/·/· | — |
| 1394 | `DiablostLamp` | 魔教徒灯 | 42 | — | ·/·/· | — |
| 1395 | `OilRagSconse` | 油布烛台 | 42 | — | ·/·/· | — |
| 1396 | `BlueDungeonChair` | 蓝地牢椅 | 15 | — | ·/·/· | — |
| 1397 | `BlueDungeonTable` | 蓝地牢桌 | 14 | — | ·/·/· | — |
| 1398 | `BlueDungeonWorkBench` | 蓝地牢工作台 | 18 | — | ·/·/· | — |
| 1399 | `GreenDungeonChair` | 绿地牢椅 | 15 | — | ·/·/· | — |
| 1400 | `GreenDungeonTable` | 绿地牢桌 | 14 | — | ·/·/· | — |
| 1401 | `GreenDungeonWorkBench` | 绿地牢工作台 | 18 | — | ·/·/· | — |
| 1402 | `PinkDungeonChair` | 粉地牢椅 | 15 | — | ·/·/· | — |
| 1403 | `PinkDungeonTable` | 粉地牢桌 | 14 | — | ·/·/· | — |
| 1404 | `PinkDungeonWorkBench` | 粉地牢工作台 | 18 | — | ·/·/· | — |
| 1405 | `BlueDungeonCandle` | 蓝地牢蜡烛 | 33 | — | ·/·/· | — |
| 1406 | `GreenDungeonCandle` | 绿地牢蜡烛 | 33 | — | ·/·/· | — |
| 1407 | `PinkDungeonCandle` | 粉地牢蜡烛 | 33 | — | ·/·/· | — |
| 1408 | `BlueDungeonVase` | 蓝地牢花瓶 | 105 | — | ·/·/· | — |
| 1409 | `GreenDungeonVase` | 绿地牢花瓶 | 105 | — | ·/·/· | — |
| 1410 | `PinkDungeonVase` | 粉地牢花瓶 | 105 | — | ·/·/· | — |
| 1411 | `BlueDungeonDoor` | 蓝地牢门 | 10 | — | ·/·/· | — |
| 1412 | `GreenDungeonDoor` | 绿地牢门 | 10 | — | ·/·/· | — |
| 1413 | `PinkDungeonDoor` | 粉地牢门 | 10 | — | ·/·/· | — |
| 1414 | `BlueDungeonBookcase` | 蓝地牢书架 | 101 | — | ·/·/· | — |
| 1415 | `GreenDungeonBookcase` | 绿地牢书架 | 101 | — | ·/·/· | — |
| 1416 | `PinkDungeonBookcase` | 粉地牢书架 | 101 | — | ·/·/· | — |
| 1417 | `Catacomb` | 地下墓穴 | 241 | — | ·/·/· | — |
| 1418 | `DungeonShelf` | 地牢架 | 19 | — | ·/·/· | — |
| 1419 | `SkellingtonJSkellingsworth` | 骷髅杰克 | 240 | — | ·/·/· | — |
| 1420 | `TheCursedMan` | 被诅咒的人 | 240 | — | ·/·/· | — |
| 1421 | `TheEyeSeestheEnd` | 最后一眼 | 242 | — | ·/·/· | — |
| 1422 | `SomethingEvilisWatchingYou` | 恶魔注视 | 242 | — | ·/·/· | — |
| 1423 | `TheTwinsHaveAwoken` | 苏醒的双子魔眼 | 242 | — | ·/·/· | — |
| 1424 | `TheScreamer` | 呐喊者 | 242 | — | ·/·/· | — |
| 1425 | `GoblinsPlayingPoker` | 玩扑克牌的哥布林 | 242 | — | ·/·/· | — |
| 1426 | `Dryadisque` | 树妖女 | 242 | — | ·/·/· | — |
| 1427 | `Sunflowers` | 向日葵 | 240 | — | ·/·/· | — |
| 1428 | `TerrarianGothic` | 泰拉式哥特 | 240 | — | ·/·/· | — |
| 1429 | `Beanie` | 小便帽 | — | — | ·/·/· | — |
| 1430 | `ImbuingStation` | 灌注站 | 243 | — | ·/·/· | — |
| 1431 | `StarinaBottle` | 星星瓶 | 42 | — | ·/·/· | — |
| 1432 | `EmptyBullet` | 空心子弹 | — | — | ·/·/· | — |
| 1433 | `Impact` | 碰撞 | 242 | — | ·/·/· | — |
| 1434 | `PoweredbyBirds` | 鸟力驱动 | 242 | — | ·/·/· | — |
| 1435 | `TheDestroyer` | 毁灭者 | 242 | — | ·/·/· | — |
| 1436 | `ThePersistencyofEyes` | 永恒之眼 | 242 | — | ·/·/· | — |
| 1437 | `UnicornCrossingtheHallows` | 越过神圣之地的独角兽 | 242 | — | ·/·/· | — |
| 1438 | `GreatWave` | 巨浪 | 242 | — | ·/·/· | — |
| 1439 | `StarryNight` | 星空 | 242 | — | ·/·/· | — |
| 1440 | `GuidePicasso` | 向导毕加索 | 240 | — | ·/·/· | — |
| 1441 | `TheGuardiansGaze` | 守卫凝视 | 240 | — | ·/·/· | — |
| 1442 | `FatherofSomeone` | 某人之父 | 240 | — | ·/·/· | — |
| 1443 | `NurseLisa` | 丽莎护士 | 240 | — | ·/·/· | — |
| 1444 | `ShadowbeamStaff` | 暗影束法杖 | — | — | ·/·/· | 80 |
| 1445 | `InfernoFork` | 狱火叉 | — | — | ·/·/· | 70 |
| 1446 | `SpectreStaff` | 幽灵法杖 | — | — | ·/·/· | 65 |
| 1447 | `WoodenFence` | 木栅栏 | — | 106 | ·/·/· | — |
| 1448 | `LeadFence` | 铅栅栏 | — | 107 | ·/·/· | — |
| 1449 | `BubbleMachine` | 泡泡机 | 244 | — | ·/·/· | — |
| 1450 | `BubbleWand` | 泡泡魔棒 | — | — | ·/·/· | — |
| 1451 | `MarchingBonesBanner` | 骷髅行军旗 | 91 | — | ·/·/· | — |
| 1452 | `NecromanticSign` | 死灵标旗 | 91 | — | ·/·/· | — |
| 1453 | `RustedCompanyStandard` | 秀逗连队旗 | 91 | — | ·/·/· | — |
| 1454 | `RaggedBrotherhoodSigil` | 丐帮帮旗 | 91 | — | ·/·/· | — |
| 1455 | `MoltenLegionFlag` | 熔火军团旗 | 91 | — | ·/·/· | — |
| 1456 | `DiabolicSigil` | 魔教旗 | 91 | — | ·/·/· | — |
| 1457 | `ObsidianPlatform` | 黑曜石平台 | 19 | — | ·/·/· | — |
| 1458 | `ObsidianDoor` | 黑曜石门 | 10 | — | ·/·/· | — |
| 1459 | `ObsidianChair` | 黑曜石椅 | 15 | — | ·/·/· | — |
| 1460 | `ObsidianTable` | 黑曜石桌 | 14 | — | ·/·/· | — |
| 1461 | `ObsidianWorkBench` | 黑曜石工作台 | 18 | — | ·/·/· | — |
| 1462 | `ObsidianVase` | 黑曜石花瓶 | 105 | — | ·/·/· | — |
| 1463 | `ObsidianBookcase` | 黑曜石书架 | 101 | — | ·/·/· | — |
| 1464 | `HellboundBanner` | 地狱之界旗 | 91 | — | ·/·/· | — |
| 1465 | `HellHammerBanner` | 地狱之锤旗 | 91 | — | ·/·/· | — |
| 1466 | `HelltowerBanner` | 地狱之塔旗 | 91 | — | ·/·/· | — |
| 1467 | `LostHopesofManBanner` | 绝望人旗 | 91 | — | ·/·/· | — |
| 1468 | `ObsidianWatcherBanner` | 黑曜石看守人旗 | 91 | — | ·/·/· | — |
| 1469 | `LavaEruptsBanner` | 熔岩喷液旗 | 91 | — | ·/·/· | — |
| 1470 | `BlueDungeonBed` | 蓝地牢床 | 79 | — | ·/·/· | — |
| 1471 | `GreenDungeonBed` | 绿地牢床 | 79 | — | ·/·/· | — |
| 1472 | `PinkDungeonBed` | 粉地牢床 | 79 | — | ·/·/· | — |
| 1473 | `ObsidianBed` | 黑曜石床 | 79 | — | ·/·/· | — |
| 1474 | `Waldo` | 沃尔多 | 245 | — | ·/·/· | — |
| 1475 | `Darkness` | 黑暗 | 245 | — | ·/·/· | — |
| 1476 | `DarkSoulReaper` | 暗魂死神 | 245 | — | ·/·/· | — |
| 1477 | `Land` | 大地 | 245 | — | ·/·/· | — |
| 1478 | `TrappedGhost` | 受困鬼魂 | 245 | — | ·/·/· | — |
| 1479 | `DemonsEye` | 恶魔眼 | 246 | — | ·/·/· | — |
| 1480 | `FindingGold` | 淘金 | 246 | — | ·/·/· | — |
| 1481 | `FirstEncounter` | 初次邂逅 | 246 | — | ·/·/· | — |
| 1482 | `GoodMorning` | 早安 | 246 | — | ·/·/· | — |
| 1483 | `UndergroundReward` | 地下馈赠 | 246 | — | ·/·/· | — |
| 1484 | `ThroughtheWindow` | 窗外 | 246 | — | ·/·/· | — |
| 1485 | `PlaceAbovetheClouds` | 云之上 | 246 | — | ·/·/· | — |
| 1486 | `DoNotStepontheGrass` | 请勿践踏草坪 | 246 | — | ·/·/· | — |
| 1487 | `ColdWatersintheWhiteLand` | 冰雪寒溪 | 246 | — | ·/·/· | — |
| 1488 | `LightlessChasms` | 阴暗幽谷 | 246 | — | ·/·/· | — |
| 1489 | `TheLandofDeceivingLooks` | 浮华假象的大地 | 246 | — | ·/·/· | — |
| 1490 | `Daylight` | 黎明 | 246 | — | ·/·/· | — |
| 1491 | `SecretoftheSands` | 沙漠的秘密 | 246 | — | ·/·/· | — |
| 1492 | `DeadlandComesAlive` | 死地复生 | 246 | — | ·/·/· | — |
| 1493 | `EvilPresence` | 恶灵现世 | 246 | — | ·/·/· | — |
| 1494 | `SkyGuardian` | 天空守卫 | 246 | — | ·/·/· | — |
| 1495 | `AmericanExplosive` | 美洲爆炸狂 | 245 | — | ·/·/· | — |
| 1496 | `Discover` | 探索 | 240 | — | ·/·/· | — |
| 1497 | `HandEarth` | 大地之手 | 240 | — | ·/·/· | — |
| 1498 | `OldMiner` | 老矿工 | 240 | — | ·/·/· | — |
| 1499 | `Skelehead` | 骨蛇之头 | 240 | — | ·/·/· | — |
| 1500 | `FacingtheCerebralMastermind` | 直面摄魂师 | 242 | — | ·/·/· | — |
| 1501 | `LakeofFire` | 火湖 | 242 | — | ·/·/· | — |
| 1502 | `TrioSuperHeroes` | 超级英雄三剑客 | 242 | — | ·/·/· | — |
| 1503 | `SpectreHood` | 幽灵兜帽 | — | — | ·/·/· | — |
| 1504 | `SpectreRobe` | 幽灵长袍 | — | — | ·/·/· | — |
| 1505 | `SpectrePants` | 幽灵裤 | — | — | ·/·/· | — |
| 1506 | `SpectrePickaxe` | 幽灵镐 | — | — | 200/·/· | 32 |
| 1507 | `SpectreHamaxe` | 幽灵锤斧 | — | — | ·/30/90 | 60 |
| 1508 | `Ectoplasm` | 灵气 | — | — | ·/·/· | — |
| 1509 | `GothicChair` | 哥特椅 | 15 | — | ·/·/· | — |
| 1510 | `GothicTable` | 哥特桌 | 14 | — | ·/·/· | — |
| 1511 | `GothicWorkBench` | 哥特工作台 | 18 | — | ·/·/· | — |
| 1512 | `GothicBookcase` | 哥特书架 | 101 | — | ·/·/· | — |
| 1513 | `PaladinsHammer` | 圣骑士锤 | — | — | ·/·/· | 90 |
| 1514 | `SWATHelmet` | 特战头盔 | — | — | ·/·/· | — |
| 1515 | `BeeWings` | 蜜蜂之翼 | — | — | ·/·/· | — |
| 1516 | `GiantHarpyFeather` | 巨型鸟妖之羽 | — | — | ·/·/· | — |
| 1517 | `BoneFeather` | 骨之羽 | — | — | ·/·/· | — |
| 1518 | `FireFeather` | 火羽 | — | — | ·/·/· | — |
| 1519 | `IceFeather` | 冰雪羽 | — | — | ·/·/· | — |
| 1520 | `BrokenBatWing` | 破蝙蝠之翼 | — | — | ·/·/· | — |
| 1521 | `TatteredBeeWing` | 褴褛蜜蜂之翼 | — | — | ·/·/· | — |
| 1522 | `LargeAmethyst` | 大紫晶 | — | — | ·/·/· | — |
| 1523 | `LargeTopaz` | 大黄玉 | — | — | ·/·/· | — |
| 1524 | `LargeSapphire` | 大蓝玉 | — | — | ·/·/· | — |
| 1525 | `LargeEmerald` | 大翡翠 | — | — | ·/·/· | — |
| 1526 | `LargeRuby` | 大红玉 | — | — | ·/·/· | — |
| 1527 | `LargeDiamond` | 大钻石 | — | — | ·/·/· | — |
| 1528 | `JungleChest` | 丛林箱 | 21 | — | ·/·/· | — |
| 1529 | `CorruptionChest` | 腐化箱 | 21 | — | ·/·/· | — |
| 1530 | `CrimsonChest` | 猩红箱 | 21 | — | ·/·/· | — |
| 1531 | `HallowedChest` | 神圣箱 | 21 | — | ·/·/· | — |
| 1532 | `FrozenChest` | 冰雪箱 | 21 | — | ·/·/· | — |
| 1533 | `JungleKey` | 丛林钥匙 | — | — | ·/·/· | — |
| 1534 | `CorruptionKey` | 腐化钥匙 | — | — | ·/·/· | — |
| 1535 | `CrimsonKey` | 猩红钥匙 | — | — | ·/·/· | — |
| 1536 | `HallowedKey` | 神圣钥匙 | — | — | ·/·/· | — |
| 1537 | `FrozenKey` | 冰冻钥匙 | — | — | ·/·/· | — |
| 1538 | `ImpFace` | 小鬼脸 | 240 | — | ·/·/· | — |
| 1539 | `OminousPresence` | 凶兆 | 240 | — | ·/·/· | — |
| 1540 | `ShiningMoon` | 闪亮月亮 | 240 | — | ·/·/· | — |
| 1541 | `LivingGore` | 血肉之躯 | 246 | — | ·/·/· | — |
| 1542 | `FlowingMagma` | 岩浆漫流 | 246 | — | ·/·/· | — |
| 1543 | `SpectrePaintbrush` | 幽灵漆刷 | — | — | ·/·/· | — |
| 1544 | `SpectrePaintRoller` | 幽灵涂漆滚刷 | — | — | ·/·/· | — |
| 1545 | `SpectrePaintScraper` | 幽灵漆铲 | — | — | ·/·/· | — |
| 1546 | `ShroomiteHeadgear` | 蘑菇矿头饰 | — | — | ·/·/· | — |
| 1547 | `ShroomiteMask` | 蘑菇矿面具 | — | — | ·/·/· | — |
| 1548 | `ShroomiteHelmet` | 蘑菇矿头盔 | — | — | ·/·/· | — |
| 1549 | `ShroomiteBreastplate` | 蘑菇矿胸甲 | — | — | ·/·/· | — |
| 1550 | `ShroomiteLeggings` | 蘑菇矿护腿 | — | — | ·/·/· | — |
| 1551 | `Autohammer` | 自动锤炼机 | 247 | — | ·/·/· | — |
| 1552 | `ShroomiteBar` | 蘑菇矿锭 | 239 | — | ·/·/· | — |
| 1553 | `SDMG` | 太空海豚机枪 | — | — | ·/·/· | 85 |
| 1554 | `CenxsTiara` | Cenx的头冠 | — | — | ·/·/· | — |
| 1555 | `CenxsBreastplate` | Cenx的胸甲 | — | — | ·/·/· | — |
| 1556 | `CenxsLeggings` | Cenx的护腿 | — | — | ·/·/· | — |
| 1557 | `CrownosMask` | Crowno的面具 | — | — | ·/·/· | — |
| 1558 | `CrownosBreastplate` | Crowno的胸甲 | — | — | ·/·/· | — |
| 1559 | `CrownosLeggings` | Crowno的护腿 | — | — | ·/·/· | — |
| 1560 | `WillsHelmet` | Will的头盔 | — | — | ·/·/· | — |
| 1561 | `WillsBreastplate` | Will的胸甲 | — | — | ·/·/· | — |
| 1562 | `WillsLeggings` | Will的护腿 | — | — | ·/·/· | — |
| 1563 | `JimsHelmet` | Jim的头盔 | — | — | ·/·/· | — |
| 1564 | `JimsBreastplate` | Jim的胸甲 | — | — | ·/·/· | — |
| 1565 | `JimsLeggings` | Jim的护腿 | — | — | ·/·/· | — |
| 1566 | `AaronsHelmet` | Aaron的头盔 | — | — | ·/·/· | — |
| 1567 | `AaronsBreastplate` | Aaron的胸甲 | — | — | ·/·/· | — |
| 1568 | `AaronsLeggings` | Aaron的护腿 | — | — | ·/·/· | — |
| 1569 | `VampireKnives` | 吸血鬼刀 | — | — | ·/·/· | 29 |
| 1570 | `BrokenHeroSword` | 断裂英雄剑 | — | — | ·/·/· | — |
| 1571 | `ScourgeoftheCorruptor` | 腐化者之戟 | — | — | ·/·/· | 70 |
| 1572 | `StaffoftheFrostHydra` | 寒霜九头蛇法杖 | — | — | ·/·/· | 100 |
| 1573 | `TheCreationoftheGuide` | 创造向导 | 242 | — | ·/·/· | — |
| 1574 | `TheMerchant` | 商人 | 240 | — | ·/·/· | — |
| 1575 | `CrownoDevoursHisLunch` | Crowno吞噬其餐 | 240 | — | ·/·/· | — |
| 1576 | `RareEnchantment` | 稀有魔力 | 240 | — | ·/·/· | — |
| 1577 | `GloriousNight` | 荣耀之夜 | 245 | — | ·/·/· | — |
| 1578 | `SweetheartNecklace` | 甜心项链 | — | — | ·/·/· | — |
| 1579 | `FlurryBoots` | 疾风雪靴 | — | — | ·/·/· | — |
| 1580 | `DTownsHelmet` | D-Town的头盔 | — | — | ·/·/· | — |
| 1581 | `DTownsBreastplate` | D-Town的胸甲 | — | — | ·/·/· | — |
| 1582 | `DTownsLeggings` | D-Town的护腿 | — | — | ·/·/· | — |
| 1583 | `DTownsWings` | D-Town的翅膀 | — | — | ·/·/· | — |
| 1584 | `WillsWings` | Will的翅膀 | — | — | ·/·/· | — |
| 1585 | `CrownosWings` | Crowno的翅膀 | — | — | ·/·/· | — |
| 1586 | `CenxsWings` | Cenx的翅膀 | — | — | ·/·/· | — |
| 1587 | `CenxsDress` | Cenx的上衣 | — | — | ·/·/· | — |
| 1588 | `CenxsDressPants` | Cenx的裤装 | — | — | ·/·/· | — |
| 1589 | `PalladiumColumn` | 钯金柱 | 248 | — | ·/·/· | — |
| 1590 | `PalladiumColumnWall` | 钯金柱墙 | — | 109 | ·/·/· | — |
| 1591 | `BubblegumBlock` | 泡泡糖块 | 249 | — | ·/·/· | — |
| 1592 | `BubblegumBlockWall` | 泡泡糖块墙 | — | 110 | ·/·/· | — |
| 1593 | `TitanstoneBlock` | 钛石块 | 250 | — | ·/·/· | — |
| 1594 | `TitanstoneBlockWall` | 钛石块墙 | — | 111 | ·/·/· | — |
| 1595 | `MagicCuffs` | 魔法手铐 | — | — | ·/·/· | — |
| 1596 | `MusicBoxSnow` | 八音盒（雪原） | 139 | — | ·/·/· | — |
| 1597 | `MusicBoxSpace` | 八音盒（夜间太空） | 139 | — | ·/·/· | — |
| 1598 | `MusicBoxCrimson` | 八音盒（猩红之地） | 139 | — | ·/·/· | — |
| 1599 | `MusicBoxBoss4` | 八音盒(Boss 4) | 139 | — | ·/·/· | — |
| 1600 | `MusicBoxAltOverworldDay` | 八音盒（人间日备选曲） | 139 | — | ·/·/· | — |
| 1601 | `MusicBoxRain` | 八音盒（雨） | 139 | — | ·/·/· | — |
| 1602 | `MusicBoxIce` | 八音盒（冰雪） | 139 | — | ·/·/· | — |
| 1603 | `MusicBoxDesert` | 八音盒（沙漠） | 139 | — | ·/·/· | — |
| 1604 | `MusicBoxOcean` | 八音盒（日间海洋） | 139 | — | ·/·/· | — |
| 1605 | `MusicBoxDungeon` | 八音盒（地牢） | 139 | — | ·/·/· | — |
| 1606 | `MusicBoxPlantera` | 八音盒（世纪之花） | 139 | — | ·/·/· | — |
| 1607 | `MusicBoxBoss5` | 八音盒(Boss 5) | 139 | — | ·/·/· | — |
| 1608 | `MusicBoxTemple` | 八音盒（神庙） | 139 | — | ·/·/· | — |
| 1609 | `MusicBoxEclipse` | 八音盒（日食） | 139 | — | ·/·/· | — |
| 1610 | `MusicBoxMushrooms` | 八音盒（蘑菇） | 139 | — | ·/·/· | — |
| 1611 | `ButterflyDust` | 蝴蝶尘 | — | — | ·/·/· | — |
| 1612 | `AnkhCharm` | 十字章护身符 | — | — | ·/·/· | — |
| 1613 | `AnkhShield` | 十字章护盾 | — | — | ·/·/· | — |
| 1614 | `BlueFlare` | 蓝照明弹 | — | — | ·/·/· | 1 |
| 1615 | `AnglerFishBanner` | 琵琶鱼旗 | 91 | — | ·/·/· | — |
| 1616 | `AngryNimbusBanner` | 愤怒雨云怪旗 | 91 | — | ·/·/· | — |
| 1617 | `AnomuraFungusBanner` | 歪尾真菌旗 | 91 | — | ·/·/· | — |
| 1618 | `AntlionBanner` | 蚁狮旗 | 91 | — | ·/·/· | — |
| 1619 | `ArapaimaBanner` | 巨骨舌鱼旗 | 91 | — | ·/·/· | — |
| 1620 | `ArmoredSkeletonBanner` | 装甲骷髅旗 | 91 | — | ·/·/· | — |
| 1621 | `BatBanner` | 洞穴蝙蝠旗 | 91 | — | ·/·/· | — |
| 1622 | `BirdBanner` | 鸟旗 | 91 | — | ·/·/· | — |
| 1623 | `BlackRecluseBanner` | 黑隐士旗 | 91 | — | ·/·/· | — |
| 1624 | `BloodFeederBanner` | 嗜血怪旗 | 91 | — | ·/·/· | — |
| 1625 | `BloodJellyBanner` | 血水母旗 | 91 | — | ·/·/· | — |
| 1626 | `BloodCrawlerBanner` | 血爬虫旗 | 91 | — | ·/·/· | — |
| 1627 | `BoneSerpentBanner` | 骨蛇旗 | 91 | — | ·/·/· | — |
| 1628 | `BunnyBanner` | 兔兔旗 | 91 | — | ·/·/· | — |
| 1629 | `ChaosElementalBanner` | 混沌精旗 | 91 | — | ·/·/· | — |
| 1630 | `MimicBanner` | 宝箱怪旗 | 91 | — | ·/·/· | — |
| 1631 | `ClownBanner` | 小丑旗 | 91 | — | ·/·/· | — |
| 1632 | `CorruptBunnyBanner` | 腐化兔兔旗 | 91 | — | ·/·/· | — |
| 1633 | `CorruptGoldfishBanner` | 腐化金鱼旗 | 91 | — | ·/·/· | — |
| 1634 | `CrabBanner` | 螃蟹旗 | 91 | — | ·/·/· | — |
| 1635 | `CrimeraBanner` | 猩红喀迈拉旗 | 91 | — | ·/·/· | — |
| 1636 | `CrimsonAxeBanner` | 猩红斧旗 | 91 | — | ·/·/· | — |
| 1637 | `CursedHammerBanner` | 诅咒锤旗 | 91 | — | ·/·/· | — |
| 1638 | `DemonBanner` | 恶魔旗 | 91 | — | ·/·/· | — |
| 1639 | `DemonEyeBanner` | 恶魔眼旗 | 91 | — | ·/·/· | — |
| 1640 | `DerplingBanner` | 跳跳兽旗 | 91 | — | ·/·/· | — |
| 1641 | `EaterofSoulsBanner` | 噬魂怪旗 | 91 | — | ·/·/· | — |
| 1642 | `EnchantedSwordBanner` | 附魔剑旗 | 91 | — | ·/·/· | — |
| 1643 | `ZombieEskimoBanner` | 冰冻僵尸旗 | 91 | — | ·/·/· | — |
| 1644 | `FaceMonsterBanner` | 脸怪旗 | 91 | — | ·/·/· | — |
| 1645 | `FloatyGrossBanner` | 恶心浮游怪旗 | 91 | — | ·/·/· | — |
| 1646 | `FlyingFishBanner` | 飞鱼旗 | 91 | — | ·/·/· | — |
| 1647 | `FlyingSnakeBanner` | 飞蛇旗 | 91 | — | ·/·/· | — |
| 1648 | `FrankensteinBanner` | 科学怪人旗 | 91 | — | ·/·/· | — |
| 1649 | `FungiBulbBanner` | 真菌球怪旗 | 91 | — | ·/·/· | — |
| 1650 | `FungoFishBanner` | 蘑菇鱼旗 | 91 | — | ·/·/· | — |
| 1651 | `GastropodBanner` | 腹足怪旗 | 91 | — | ·/·/· | — |
| 1652 | `GoblinThiefBanner` | 哥布林盗贼旗 | 91 | — | ·/·/· | — |
| 1653 | `GoblinSorcererBanner` | 哥布林巫士旗 | 91 | — | ·/·/· | — |
| 1654 | `GoblinPeonBanner` | 哥布林苦力旗 | 91 | — | ·/·/· | — |
| 1655 | `GoblinScoutBanner` | 哥布林侦察兵旗 | 91 | — | ·/·/· | — |
| 1656 | `GoblinWarriorBanner` | 哥布林战士旗 | 91 | — | ·/·/· | — |
| 1657 | `GoldfishBanner` | 金鱼旗 | 91 | — | ·/·/· | — |
| 1658 | `HarpyBanner` | 鸟妖旗 | 91 | — | ·/·/· | — |
| 1659 | `HellbatBanner` | 地狱蝙蝠旗 | 91 | — | ·/·/· | — |
| 1660 | `HerplingBanner` | 蹦蹦兽旗 | 91 | — | ·/·/· | — |
| 1661 | `HornetBanner` | 黄蜂旗 | 91 | — | ·/·/· | — |
| 1662 | `IceElementalBanner` | 冰雪精旗 | 91 | — | ·/·/· | — |
| 1663 | `IcyMermanBanner` | 冰雪人鱼旗 | 91 | — | ·/·/· | — |
| 1664 | `FireImpBanner` | 火焰小鬼旗 | 91 | — | ·/·/· | — |
| 1665 | `JellyfishBanner` | 蓝水母旗 | 91 | — | ·/·/· | — |
| 1666 | `JungleCreeperBanner` | 丛林蜘蛛旗 | 91 | — | ·/·/· | — |
| 1667 | `LihzahrdBanner` | 丛林蜥蜴旗 | 91 | — | ·/·/· | — |
| 1668 | `ManEaterBanner` | 食人怪旗 | 91 | — | ·/·/· | — |
| 1669 | `MeteorHeadBanner` | 流星头旗 | 91 | — | ·/·/· | — |
| 1670 | `MothBanner` | 飞蛾旗 | 91 | — | ·/·/· | — |
| 1671 | `MummyBanner` | 木乃伊旗 | 91 | — | ·/·/· | — |
| 1672 | `MushiLadybugBanner` | 瓢虫旗 | 91 | — | ·/·/· | — |
| 1673 | `ParrotBanner` | 鹦鹉旗 | 91 | — | ·/·/· | — |
| 1674 | `PigronBanner` | 猪龙旗 | 91 | — | ·/·/· | — |
| 1675 | `PiranhaBanner` | 食人鱼旗 | 91 | — | ·/·/· | — |
| 1676 | `PirateBanner` | 海盗水手旗 | 91 | — | ·/·/· | — |
| 1677 | `PixieBanner` | 妖精旗 | 91 | — | ·/·/· | — |
| 1678 | `RaincoatZombieBanner` | 雨衣僵尸旗 | 91 | — | ·/·/· | — |
| 1679 | `ReaperBanner` | 死神旗 | 91 | — | ·/·/· | — |
| 1680 | `SharkBanner` | 鲨鱼旗 | 91 | — | ·/·/· | — |
| 1681 | `SkeletonBanner` | 骷髅旗 | 91 | — | ·/·/· | — |
| 1682 | `SkeletonMageBanner` | 暗黑法师旗 | 91 | — | ·/·/· | — |
| 1683 | `SlimeBanner` | 蓝史莱姆旗 | 91 | — | ·/·/· | — |
| 1684 | `SnowFlinxBanner` | 小雪怪旗 | 91 | — | ·/·/· | — |
| 1685 | `SpiderBanner` | 爬墙蜘蛛旗 | 91 | — | ·/·/· | — |
| 1686 | `SporeZombieBanner` | 孢子僵尸旗 | 91 | — | ·/·/· | — |
| 1687 | `SwampThingBanner` | 沼泽怪旗 | 91 | — | ·/·/· | — |
| 1688 | `TortoiseBanner` | 巨型陆龟旗 | 91 | — | ·/·/· | — |
| 1689 | `ToxicSludgeBanner` | 毒泥旗 | 91 | — | ·/·/· | — |
| 1690 | `UmbrellaSlimeBanner` | 雨伞史莱姆旗 | 91 | — | ·/·/· | — |
| 1691 | `UnicornBanner` | 独角兽旗 | 91 | — | ·/·/· | — |
| 1692 | `VampireBanner` | 吸血鬼旗 | 91 | — | ·/·/· | — |
| 1693 | `VultureBanner` | 秃鹰旗 | 91 | — | ·/·/· | — |
| 1694 | `NypmhBanner` | 宁芙旗 | 91 | — | ·/·/· | — |
| 1695 | `WerewolfBanner` | 狼人旗 | 91 | — | ·/·/· | — |
| 1696 | `WolfBanner` | 狼旗 | 91 | — | ·/·/· | — |
| 1697 | `WorldFeederBanner` | 吞世怪旗 | 91 | — | ·/·/· | — |
| 1698 | `WormBanner` | 蠕虫旗 | 91 | — | ·/·/· | — |
| 1699 | `WraithBanner` | 幻灵旗 | 91 | — | ·/·/· | — |
| 1700 | `WyvernBanner` | 飞龙旗 | 91 | — | ·/·/· | — |
| 1701 | `ZombieBanner` | 僵尸旗 | 91 | — | ·/·/· | — |
| 1702 | `GlassPlatform` | 玻璃平台 | 19 | — | ·/·/· | — |
| 1703 | `GlassChair` | 玻璃椅 | 15 | — | ·/·/· | — |
| 1704 | `GoldenChair` | 金椅 | 15 | — | ·/·/· | — |
| 1705 | `GoldenToilet` | 金马桶 | 15 | — | ·/·/· | — |
| 1706 | `BarStool` | 高脚凳 | 15 | — | ·/·/· | — |
| 1707 | `HoneyChair` | 蜂蜜椅 | 15 | — | ·/·/· | — |
| 1708 | `SteampunkChair` | 蒸汽朋克椅 | 15 | — | ·/·/· | — |
| 1709 | `GlassDoor` | 玻璃门 | 10 | — | ·/·/· | — |
| 1710 | `GoldenDoor` | 金门 | 10 | — | ·/·/· | — |
| 1711 | `HoneyDoor` | 蜂蜜门 | 10 | — | ·/·/· | — |
| 1712 | `SteampunkDoor` | 蒸汽朋克门 | 10 | — | ·/·/· | — |
| 1713 | `GlassTable` | 玻璃桌 | 14 | — | ·/·/· | — |
| 1714 | `BanquetTable` | 宴会桌 | 14 | — | ·/·/· | — |
| 1715 | `Bar` | 锭 | 14 | — | ·/·/· | — |
| 1716 | `GoldenTable` | 金桌 | 14 | — | ·/·/· | — |
| 1717 | `HoneyTable` | 蜂蜜桌 | 14 | — | ·/·/· | — |
| 1718 | `SteampunkTable` | 蒸汽朋克桌 | 14 | — | ·/·/· | — |
| 1719 | `GlassBed` | 玻璃床 | 79 | — | ·/·/· | — |
| 1720 | `GoldenBed` | 金床 | 79 | — | ·/·/· | — |
| 1721 | `HoneyBed` | 蜂蜜床 | 79 | — | ·/·/· | — |
| 1722 | `SteampunkBed` | 蒸汽朋克床 | 79 | — | ·/·/· | — |
| 1723 | `LivingWoodWall` | 生命木墙 | — | 78 | ·/·/· | — |
| 1724 | `FartinaJar` | 罐中臭屁 | — | — | ·/·/· | — |
| 1725 | `Pumpkin` | 南瓜 | 251 | — | ·/·/· | — |
| 1726 | `PumpkinWall` | 南瓜墙 | — | 113 | ·/·/· | — |
| 1727 | `Hay` | 干草 | 252 | — | ·/·/· | — |
| 1728 | `HayWall` | 干草墙 | — | 114 | ·/·/· | — |
| 1729 | `SpookyWood` | 阴森木 | 253 | — | ·/·/· | — |
| 1730 | `SpookyWoodWall` | 阴森木墙 | — | 115 | ·/·/· | — |
| 1731 | `PumpkinHelmet` | 南瓜头盔 | — | — | ·/·/· | — |
| 1732 | `PumpkinBreastplate` | 南瓜胸甲 | — | — | ·/·/· | — |
| 1733 | `PumpkinLeggings` | 南瓜护腿 | — | — | ·/·/· | — |
| 1734 | `CandyApple` | 焦糖苹果 | — | — | ·/·/· | — |
| 1735 | `SoulCake` | 灵魂蛋糕 | — | — | ·/·/· | — |
| 1736 | `NurseHat` | 护士帽 | — | — | ·/·/· | — |
| 1737 | `NurseShirt` | 护士衣 | — | — | ·/·/· | — |
| 1738 | `NursePants` | 护士裤 | — | — | ·/·/· | — |
| 1739 | `WizardsHat` | 巫师帽 | — | — | ·/·/· | — |
| 1740 | `GuyFawkesMask` | 盖伊·福克斯面具 | — | — | ·/·/· | — |
| 1741 | `DyeTraderRobe` | 染料商长袍 | — | — | ·/·/· | — |
| 1742 | `SteampunkGoggles` | 蒸汽朋克护目镜 | — | — | ·/·/· | — |
| 1743 | `CyborgHelmet` | 机器侠头盔 | — | — | ·/·/· | — |
| 1744 | `CyborgShirt` | 机器侠衣 | — | — | ·/·/· | — |
| 1745 | `CyborgPants` | 机器侠裤 | — | — | ·/·/· | — |
| 1746 | `CreeperMask` | 苦力怕面具 | — | — | ·/·/· | — |
| 1747 | `CreeperShirt` | 苦力怕衣 | — | — | ·/·/· | — |
| 1748 | `CreeperPants` | 苦力怕裤 | — | — | ·/·/· | — |
| 1749 | `CatMask` | 猫咪面具 | — | — | ·/·/· | — |
| 1750 | `CatShirt` | 猫咪衣 | — | — | ·/·/· | — |
| 1751 | `CatPants` | 猫咪裤 | — | — | ·/·/· | — |
| 1752 | `GhostMask` | 鬼魂面具 | — | — | ·/·/· | — |
| 1753 | `GhostShirt` | 鬼魂衣 | — | — | ·/·/· | — |
| 1754 | `PumpkinMask` | 南瓜面具 | — | — | ·/·/· | — |
| 1755 | `PumpkinShirt` | 南瓜衣 | — | — | ·/·/· | — |
| 1756 | `PumpkinPants` | 南瓜裤 | — | — | ·/·/· | — |
| 1757 | `RobotMask` | 机器人面具 | — | — | ·/·/· | — |
| 1758 | `RobotShirt` | 机器人衣 | — | — | ·/·/· | — |
| 1759 | `RobotPants` | 机器人裤 | — | — | ·/·/· | — |
| 1760 | `UnicornMask` | 独角兽面具 | — | — | ·/·/· | — |
| 1761 | `UnicornShirt` | 独角兽衣 | — | — | ·/·/· | — |
| 1762 | `UnicornPants` | 独角兽裤 | — | — | ·/·/· | — |
| 1763 | `VampireMask` | 吸血鬼面具 | — | — | ·/·/· | — |
| 1764 | `VampireShirt` | 吸血鬼衣 | — | — | ·/·/· | — |
| 1765 | `VampirePants` | 吸血鬼裤 | — | — | ·/·/· | — |
| 1766 | `WitchHat` | 女巫帽 | — | — | ·/·/· | — |
| 1767 | `LeprechaunHat` | 矮妖帽 | — | — | ·/·/· | — |
| 1768 | `LeprechaunShirt` | 矮妖衣 | — | — | ·/·/· | — |
| 1769 | `LeprechaunPants` | 矮妖裤 | — | — | ·/·/· | — |
| 1770 | `PixieShirt` | 妖精衣 | — | — | ·/·/· | — |
| 1771 | `PixiePants` | 妖精裤 | — | — | ·/·/· | — |
| 1772 | `PrincessHat` | 公主帽 | — | — | ·/·/· | — |
| 1773 | `PrincessDressNew` | 公主裙 | — | — | ·/·/· | — |
| 1774 | `GoodieBag` | 礼袋 | — | — | ·/·/· | — |
| 1775 | `WitchDress` | 女巫服 | — | — | ·/·/· | — |
| 1776 | `WitchBoots` | 女巫靴 | — | — | ·/·/· | — |
| 1777 | `BrideofFrankensteinMask` | 科学怪人新娘面具 | — | — | ·/·/· | — |
| 1778 | `BrideofFrankensteinDress` | 科学怪人新娘服 | — | — | ·/·/· | — |
| 1779 | `KarateTortoiseMask` | 空手道陆龟面具 | — | — | ·/·/· | — |
| 1780 | `KarateTortoiseShirt` | 空手道陆龟衣 | — | — | ·/·/· | — |
| 1781 | `KarateTortoisePants` | 空手道陆龟裤 | — | — | ·/·/· | — |
| 1782 | `CandyCornRifle` | 玉米糖步枪 | — | — | ·/·/· | 44 |
| 1783 | `CandyCorn` | 玉米糖 | — | — | ·/·/· | 9 |
| 1784 | `JackOLanternLauncher` | 杰克南瓜灯发射器 | — | — | ·/·/· | 65 |
| 1785 | `ExplosiveJackOLantern` | 爆炸杰克南瓜灯 | — | — | ·/·/· | 60 |
| 1786 | `Sickle` | 镰刀 | — | — | ·/·/· | 9 |
| 1787 | `PumpkinPie` | 南瓜饼 | — | — | ·/·/· | — |
| 1788 | `ScarecrowHat` | 稻草人帽 | — | — | ·/·/· | — |
| 1789 | `ScarecrowShirt` | 稻草人衣 | — | — | ·/·/· | — |
| 1790 | `ScarecrowPants` | 稻草人裤 | — | — | ·/·/· | — |
| 1791 | `Cauldron` | 大锅 | 96 | — | ·/·/· | — |
| 1792 | `PumpkinChair` | 南瓜椅 | 15 | — | ·/·/· | — |
| 1793 | `PumpkinDoor` | 南瓜门 | 10 | — | ·/·/· | — |
| 1794 | `PumpkinTable` | 南瓜桌 | 14 | — | ·/·/· | — |
| 1795 | `PumpkinWorkBench` | 南瓜工作台 | 18 | — | ·/·/· | — |
| 1796 | `PumpkinPlatform` | 南瓜平台 | 19 | — | ·/·/· | — |
| 1797 | `TatteredFairyWings` | 褴褛仙灵之翼 | — | — | ·/·/· | — |
| 1798 | `SpiderEgg` | 蜘蛛卵 | — | — | ·/·/· | — |
| 1799 | `MagicalPumpkinSeed` | 魔法南瓜子 | — | — | ·/·/· | — |
| 1800 | `BatHook` | 蝙蝠钩 | — | — | ·/·/· | — |
| 1801 | `BatScepter` | 蝙蝠权杖 | — | — | ·/·/· | 45 |
| 1802 | `RavenStaff` | 乌鸦法杖 | — | — | ·/·/· | 55 |
| 1808 | `HangingJackOLantern` | 杰克南瓜挂灯 | 42 | — | ·/·/· | — |
| 1809 | `RottenEgg` | 臭蛋 | — | — | ·/·/· | 13 |
| 1810 | `UnluckyYarn` | 霉运纱线 | — | — | ·/·/· | — |
| 1811 | `BlackFairyDust` | 黑色仙尘 | — | — | ·/·/· | — |
| 1812 | `Jackelier` | 南瓜头吊灯 | 34 | — | ·/·/· | — |
| 1813 | `JackOLantern` | 杰克南瓜灯 | 35 | — | ·/·/· | — |
| 1814 | `SpookyChair` | 阴森椅 | 15 | — | ·/·/· | — |
| 1815 | `SpookyDoor` | 阴森门 | 10 | — | ·/·/· | — |
| 1816 | `SpookyTable` | 阴森桌 | 14 | — | ·/·/· | — |
| 1817 | `SpookyWorkBench` | 阴森工作台 | 18 | — | ·/·/· | — |
| 1818 | `SpookyPlatform` | 阴森木平台 | 19 | — | ·/·/· | — |
| 1819 | `ReaperHood` | 死神兜帽 | — | — | ·/·/· | — |
| 1820 | `ReaperRobe` | 死神长袍 | — | — | ·/·/· | — |
| 1821 | `FoxMask` | 狐狸面具 | — | — | ·/·/· | — |
| 1822 | `FoxShirt` | 狐狸衣 | — | — | ·/·/· | — |
| 1823 | `FoxPants` | 狐狸裤 | — | — | ·/·/· | — |
| 1824 | `CatEars` | 猫耳 | — | — | ·/·/· | — |
| 1825 | `BloodyMachete` | 血腥砍刀 | — | — | ·/·/· | 20 |
| 1826 | `TheHorsemansBlade` | 无头骑士剑 | — | — | ·/·/· | 150 |
| 1827 | `BladedGlove` | 利刃手套 | — | — | ·/·/· | 14 |
| 1828 | `PumpkinSeed` | 南瓜子 | 254 | — | ·/·/· | — |
| 1829 | `SpookyHook` | 阴森钩 | — | — | ·/·/· | — |
| 1830 | `SpookyWings` | 阴森之翼 | — | — | ·/·/· | — |
| 1831 | `SpookyTwig` | 阴森嫩枝 | — | — | ·/·/· | — |
| 1832 | `SpookyHelmet` | 阴森头盔 | — | — | ·/·/· | — |
| 1833 | `SpookyBreastplate` | 阴森胸甲 | — | — | ·/·/· | — |
| 1834 | `SpookyLeggings` | 阴森护腿 | — | — | ·/·/· | — |
| 1835 | `StakeLauncher` | 尖桩发射器 | — | — | ·/·/· | 75 |
| 1836 | `Stake` | 尖桩 | — | — | ·/·/· | 25 |
| 1837 | `CursedSapling` | 诅咒树苗 | — | — | ·/·/· | — |
| 1838 | `SpaceCreatureMask` | 太空生物面具 | — | — | ·/·/· | — |
| 1839 | `SpaceCreatureShirt` | 太空生物衣 | — | — | ·/·/· | — |
| 1840 | `SpaceCreaturePants` | 太空生物裤 | — | — | ·/·/· | — |
| 1841 | `WolfMask` | 狼面具 | — | — | ·/·/· | — |
| 1842 | `WolfShirt` | 狼衣 | — | — | ·/·/· | — |
| 1843 | `WolfPants` | 狼裤 | — | — | ·/·/· | — |
| 1844 | `PumpkinMoonMedallion` | 南瓜月勋章 | — | — | ·/·/· | — |
| 1845 | `NecromanticScroll` | 死灵卷轴 | — | — | ·/·/· | — |
| 1846 | `JackingSkeletron` | 骷髅夜惊魂 | 242 | — | ·/·/· | — |
| 1847 | `BitterHarvest` | 苦涩收获 | 242 | — | ·/·/· | — |
| 1848 | `BloodMoonCountess` | 血月伯爵夫人 | 242 | — | ·/·/· | — |
| 1849 | `HallowsEve` | 万圣节前夕 | 242 | — | ·/·/· | — |
| 1850 | `MorbidCuriosity` | 病态好奇心 | 242 | — | ·/·/· | — |
| 1851 | `TreasureHunterShirt` | 寻宝人衣 | — | — | ·/·/· | — |
| 1852 | `TreasureHunterPants` | 寻宝人裤 | — | — | ·/·/· | — |
| 1853 | `DryadCoverings` | 树妖遮体服 | — | — | ·/·/· | — |
| 1854 | `DryadLoincloth` | 树妖腰布 | — | — | ·/·/· | — |
| 1855 | `MourningWoodTrophy` | 哀木纪念章 | 240 | — | ·/·/· | — |
| 1856 | `PumpkingTrophy` | 南瓜王纪念章 | 240 | — | ·/·/· | — |
| 1857 | `JackOLanternMask` | 杰克南瓜灯面具 | — | — | ·/·/· | — |
| 1858 | `SniperScope` | 狙击镜 | — | — | ·/·/· | — |
| 1859 | `HeartLantern` | 红心灯笼 | 42 | — | ·/·/· | — |
| 1860 | `JellyfishDivingGear` | 水母潜水装备 | — | — | ·/·/· | — |
| 1861 | `ArcticDivingGear` | 北极潜水装备 | — | — | ·/·/· | — |
| 1862 | `FrostsparkBoots` | 霜花靴 | — | — | ·/·/· | — |
| 1863 | `FartInABalloon` | 臭屁气球 | — | — | ·/·/· | — |
| 1864 | `PapyrusScarab` | 甲虫莎草纸 | — | — | ·/·/· | — |
| 1865 | `CelestialStone` | 天界石 | — | — | ·/·/· | — |
| 1866 | `Hoverboard` | 悬浮板 | — | — | ·/·/· | — |
| 1867 | `CandyCane` | 糖棒 | — | — | ·/·/· | — |
| 1868 | `SugarPlum` | 蜜糖李 | — | — | ·/·/· | — |
| 1869 | `Present` | 礼物 | 36 | — | ·/·/· | — |
| 1870 | `RedRyder` | 红莱德枪 | — | — | ·/·/· | 20 |
| 1871 | `FestiveWings` | 喜庆之翼 | — | — | ·/·/· | — |
| 1872 | `PineTreeBlock` | 松树块 | 170 | — | ·/·/· | — |
| 1873 | `ChristmasTree` | 圣诞树 | 171 | — | ·/·/· | — |
| 1874 | `StarTopper1` | 星星礼帽1 | — | — | ·/·/· | — |
| 1875 | `StarTopper2` | 星星礼帽2 | — | — | ·/·/· | — |
| 1876 | `StarTopper3` | 星星礼帽3 | — | — | ·/·/· | — |
| 1877 | `BowTopper` | 蝴蝶结礼帽 | — | — | ·/·/· | — |
| 1878 | `WhiteGarland` | 白花环 | — | — | ·/·/· | — |
| 1879 | `WhiteAndRedGarland` | 白红花环 | — | — | ·/·/· | — |
| 1880 | `RedGardland` | 红花环 | — | — | ·/·/· | — |
| 1881 | `RedAndGreenGardland` | 红绿花环 | — | — | ·/·/· | — |
| 1882 | `GreenGardland` | 绿花环 | — | — | ·/·/· | — |
| 1883 | `GreenAndWhiteGarland` | 绿白花环 | — | — | ·/·/· | — |
| 1884 | `MulticoloredBulb` | 五彩灯泡 | — | — | ·/·/· | — |
| 1885 | `RedBulb` | 红灯泡 | — | — | ·/·/· | — |
| 1886 | `YellowBulb` | 黄灯泡 | — | — | ·/·/· | — |
| 1887 | `GreenBulb` | 绿灯泡 | — | — | ·/·/· | — |
| 1888 | `RedAndGreenBulb` | 红绿灯泡 | — | — | ·/·/· | — |
| 1889 | `YellowAndGreenBulb` | 黄绿灯泡 | — | — | ·/·/· | — |
| 1890 | `RedAndYellowBulb` | 红黄灯泡 | — | — | ·/·/· | — |
| 1891 | `WhiteBulb` | 白灯泡 | — | — | ·/·/· | — |
| 1892 | `WhiteAndRedBulb` | 白红灯泡 | — | — | ·/·/· | — |
| 1893 | `WhiteAndYellowBulb` | 白黄灯泡 | — | — | ·/·/· | — |
| 1894 | `WhiteAndGreenBulb` | 白绿灯泡 | — | — | ·/·/· | — |
| 1895 | `MulticoloredLights` | 五彩光 | — | — | ·/·/· | — |
| 1896 | `RedLights` | 红光 | — | — | ·/·/· | — |
| 1897 | `GreenLights` | 绿光 | — | — | ·/·/· | — |
| 1898 | `BlueLights` | 蓝光 | — | — | ·/·/· | — |
| 1899 | `YellowLights` | 黄光 | — | — | ·/·/· | — |
| 1900 | `RedAndYellowLights` | 红黄光 | — | — | ·/·/· | — |
| 1901 | `RedAndGreenLights` | 红绿光 | — | — | ·/·/· | — |
| 1902 | `YellowAndGreenLights` | 黄绿光 | — | — | ·/·/· | — |
| 1903 | `BlueAndGreenLights` | 蓝绿光 | — | — | ·/·/· | — |
| 1904 | `RedAndBlueLights` | 红蓝光 | — | — | ·/·/· | — |
| 1905 | `BlueAndYellowLights` | 蓝黄光 | — | — | ·/·/· | — |
| 1906 | `GiantBow` | 巨型蝴蝶结 | — | — | ·/·/· | — |
| 1907 | `ReindeerAntlers` | 驯鹿角 | — | — | ·/·/· | — |
| 1908 | `Holly` | 霍利 | 246 | — | ·/·/· | — |
| 1909 | `CandyCaneSword` | 糖棒剑 | — | — | ·/·/· | 19 |
| 1910 | `ElfMelter` | 精灵熔炉 | — | — | ·/·/· | 53 |
| 1911 | `ChristmasPudding` | 圣诞布丁 | — | — | ·/·/· | — |
| 1912 | `Eggnog` | 蛋酒 | — | — | ·/·/· | — |
| 1913 | `StarAnise` | 星形茴香 | — | — | ·/·/· | 14 |
| 1914 | `ReindeerBells` | 驯鹿铃铛 | — | — | ·/·/· | — |
| 1915 | `CandyCaneHook` | 糖棒钩 | — | — | ·/·/· | — |
| 1916 | `ChristmasHook` | 圣诞钩 | — | — | ·/·/· | — |
| 1917 | `CnadyCanePickaxe` | 糖棒镐 | — | — | 55/·/· | 7 |
| 1918 | `FruitcakeChakram` | 水果蛋糕旋刃 | — | — | ·/·/· | 19 |
| 1919 | `SugarCookie` | 蜜糖饼干 | — | — | ·/·/· | — |
| 1920 | `GingerbreadCookie` | 姜饼 | — | — | ·/·/· | — |
| 1921 | `HandWarmer` | 暖手宝 | — | — | ·/·/· | — |
| 1922 | `Coal` | 煤 | — | — | ·/·/· | — |
| 1923 | `Toolbox` | 工具箱 | — | — | ·/·/· | — |
| 1924 | `PineDoor` | 松树门 | 10 | — | ·/·/· | — |
| 1925 | `PineChair` | 松树椅 | 15 | — | ·/·/· | — |
| 1926 | `PineTable` | 松树桌 | 14 | — | ·/·/· | — |
| 1927 | `DogWhistle` | 狗哨 | — | — | ·/·/· | — |
| 1928 | `ChristmasTreeSword` | 圣诞树剑 | — | — | ·/·/· | 86 |
| 1929 | `ChainGun` | 链式机枪 | — | — | ·/·/· | 31 |
| 1930 | `Razorpine` | 剃刀松 | — | — | ·/·/· | 48 |
| 1931 | `BlizzardStaff` | 暴雪法杖 | — | — | ·/·/· | 58 |
| 1932 | `MrsClauseHat` | 圣诞夫人帽 | — | — | ·/·/· | — |
| 1933 | `MrsClauseShirt` | 圣诞夫人衣 | — | — | ·/·/· | — |
| 1934 | `MrsClauseHeels` | 圣诞夫人高跟鞋 | — | — | ·/·/· | — |
| 1935 | `ParkaHood` | 派克兜帽 | — | — | ·/·/· | — |
| 1936 | `ParkaCoat` | 派克大衣 | — | — | ·/·/· | — |
| 1937 | `ParkaPants` | 派克裤 | — | — | ·/·/· | — |
| 1938 | `SnowHat` | 雪帽 | — | — | ·/·/· | — |
| 1939 | `UglySweater` | 丑毛衣 | — | — | ·/·/· | — |
| 1940 | `TreeMask` | 树面具 | — | — | ·/·/· | — |
| 1941 | `TreeShirt` | 树衣 | — | — | ·/·/· | — |
| 1942 | `TreeTrunks` | 树干 | — | — | ·/·/· | — |
| 1943 | `ElfHat` | 精灵帽 | — | — | ·/·/· | — |
| 1944 | `ElfShirt` | 精灵衣 | — | — | ·/·/· | — |
| 1945 | `ElfPants` | 精灵裤 | — | — | ·/·/· | — |
| 1946 | `SnowmanCannon` | 雪人炮 | — | — | ·/·/· | 67 |
| 1947 | `NorthPole` | 北极 | — | — | ·/·/· | 80 |
| 1948 | `ChristmasTreeWallpaper` | 圣诞树壁纸 | — | 116 | ·/·/· | — |
| 1949 | `OrnamentWallpaper` | 装饰壁纸 | — | 117 | ·/·/· | — |
| 1950 | `CandyCaneWallpaper` | 糖棒壁纸 | — | 118 | ·/·/· | — |
| 1951 | `FestiveWallpaper` | 节日壁纸 | — | 119 | ·/·/· | — |
| 1952 | `StarsWallpaper` | 星星壁纸 | — | 120 | ·/·/· | — |
| 1953 | `SquigglesWallpaper` | 波浪条纹壁纸 | — | 121 | ·/·/· | — |
| 1954 | `SnowflakeWallpaper` | 雪花壁纸 | — | 122 | ·/·/· | — |
| 1955 | `KrampusHornWallpaper` | 坎卜斯犄角壁纸 | — | 123 | ·/·/· | — |
| 1956 | `BluegreenWallpaper` | 蓝绿壁纸 | — | 124 | ·/·/· | — |
| 1957 | `GrinchFingerWallpaper` | 格林奇手指壁纸 | — | 125 | ·/·/· | — |
| 1958 | `NaughtyPresent` | 调皮礼物 | — | — | ·/·/· | — |
| 1959 | `BabyGrinchMischiefWhistle` | 格林奇宝宝的恶作剧口哨 | — | — | ·/·/· | — |
| 1960 | `IceQueenTrophy` | 冰雪女王纪念章 | 240 | — | ·/·/· | — |
| 1961 | `SantaNK1Trophy` | 圣诞坦克纪念章 | 240 | — | ·/·/· | — |
| 1962 | `EverscreamTrophy` | 常绿尖叫怪纪念章 | 240 | — | ·/·/· | — |
| 1963 | `MusicBoxPumpkinMoon` | 八音盒（南瓜月） | 139 | — | ·/·/· | — |
| 1964 | `MusicBoxAltUnderground` | 八音盒（地下备选曲） | 139 | — | ·/·/· | — |
| 1965 | `MusicBoxFrostMoon` | 八音盒（霜月） | 139 | — | ·/·/· | — |
| 1966 | `BrownPaint` | 棕漆 | — | — | ·/·/· | — |
| 1967 | `ShadowPaint` | 暗影漆 | — | — | ·/·/· | — |
| 1968 | `NegativePaint` | 反色漆 | — | — | ·/·/· | — |
| 1969 | `TeamDye` | 团队染料 | — | — | ·/·/· | — |
| 1970 | `AmethystGemsparkBlock` | 紫晶晶莹宝石块 | 262 | — | ·/·/· | — |
| 1971 | `TopazGemsparkBlock` | 黄玉晶莹宝石块 | 263 | — | ·/·/· | — |
| 1972 | `SapphireGemsparkBlock` | 蓝玉晶莹宝石块 | 264 | — | ·/·/· | — |
| 1973 | `EmeraldGemsparkBlock` | 翡翠晶莹宝石块 | 265 | — | ·/·/· | — |
| 1974 | `RubyGemsparkBlock` | 红玉晶莹宝石块 | 266 | — | ·/·/· | — |
| 1975 | `DiamondGemsparkBlock` | 钻石晶莹宝石块 | 267 | — | ·/·/· | — |
| 1976 | `AmberGemsparkBlock` | 琥珀晶莹宝石块 | 268 | — | ·/·/· | — |
| 1977 | `LifeHairDye` | 生命染发剂 | — | — | ·/·/· | — |
| 1978 | `ManaHairDye` | 魔力染发剂 | — | — | ·/·/· | — |
| 1979 | `DepthHairDye` | 深度染发剂 | — | — | ·/·/· | — |
| 1980 | `MoneyHairDye` | 钱币染发剂 | — | — | ·/·/· | — |
| 1981 | `TimeHairDye` | 时间染发剂 | — | — | ·/·/· | — |
| 1982 | `TeamHairDye` | 团队染发剂 | — | — | ·/·/· | — |
| 1983 | `BiomeHairDye` | 生物群落染发剂 | — | — | ·/·/· | — |
| 1984 | `PartyHairDye` | 派对染发剂 | — | — | ·/·/· | — |
| 1985 | `RainbowHairDye` | 彩虹染发剂 | — | — | ·/·/· | — |
| 1986 | `SpeedHairDye` | 速度染发剂 | — | — | ·/·/· | — |
| 1987 | `AngelHalo` | 天使光环 | — | — | ·/·/· | — |
| 1988 | `Fez` | 菲斯帽 | — | — | ·/·/· | — |
| 1989 | `Womannquin` | 女模特 | 470 | — | ·/·/· | — |
| 1990 | `HairDyeRemover` | 染发剂清除剂 | — | — | ·/·/· | — |
| 1991 | `BugNet` | 虫网 | — | — | ·/·/· | — |
| 1992 | `Firefly` | 萤火虫 | — | — | ·/·/· | — |
| 1993 | `FireflyinaBottle` | 萤火虫瓶 | 270 | — | ·/·/· | — |
| 1994 | `MonarchButterfly` | 帝王蝶 | — | — | ·/·/· | — |
| 1995 | `PurpleEmperorButterfly` | 紫君王蝶 | — | — | ·/·/· | — |
| 1996 | `RedAdmiralButterfly` | 红纹蝶 | — | — | ·/·/· | — |
| 1997 | `UlyssesButterfly` | 尤利西斯蝶 | — | — | ·/·/· | — |
| 1998 | `SulphurButterfly` | 硫磺蝶 | — | — | ·/·/· | — |
| 1999 | `TreeNymphButterfly` | 树若虫蝴蝶 | — | — | ·/·/· | — |
| 2000 | `ZebraSwallowtailButterfly` | 斑纹燕尾蝶 | — | — | ·/·/· | — |
| 2001 | `JuliaButterfly` | 茱莉娅蝶 | — | — | ·/·/· | — |
| 2002 | `Worm` | 蠕虫 | — | — | ·/·/· | — |
| 2003 | `Mouse` | 老鼠 | — | — | ·/·/· | — |
| 2004 | `LightningBug` | 荧光虫 | — | — | ·/·/· | — |
| 2005 | `LightningBuginaBottle` | 荧光虫瓶 | 271 | — | ·/·/· | — |
| 2006 | `Snail` | 蜗牛 | — | — | ·/·/· | — |
| 2007 | `GlowingSnail` | 发光蜗牛 | — | — | ·/·/· | — |
| 2008 | `FancyGreyWallpaper` | 别致灰壁纸 | — | 126 | ·/·/· | — |
| 2009 | `IceFloeWallpaper` | 浮冰壁纸 | — | 127 | ·/·/· | — |
| 2010 | `MusicWallpaper` | 音符壁纸 | — | 128 | ·/·/· | — |
| 2011 | `PurpleRainWallpaper` | 紫雨壁纸 | — | 129 | ·/·/· | — |
| 2012 | `RainbowWallpaper` | 彩虹壁纸 | — | 130 | ·/·/· | — |
| 2013 | `SparkleStoneWallpaper` | 烁石壁纸 | — | 131 | ·/·/· | — |
| 2014 | `StarlitHeavenWallpaper` | 星光天堂壁纸 | — | 132 | ·/·/· | — |
| 2015 | `Bird` | 鸟 | — | — | ·/·/· | — |
| 2016 | `BlueJay` | 冠蓝鸦 | — | — | ·/·/· | — |
| 2017 | `Cardinal` | 红雀 | — | — | ·/·/· | — |
| 2018 | `Squirrel` | 松鼠 | — | — | ·/·/· | — |
| 2019 | `Bunny` | 兔兔 | — | — | ·/·/· | — |
| 2020 | `CactusBookcase` | 仙人掌书架 | 101 | — | ·/·/· | — |
| 2021 | `EbonwoodBookcase` | 乌木书架 | 101 | — | ·/·/· | — |
| 2022 | `FleshBookcase` | 血肉书架 | 101 | — | ·/·/· | — |
| 2023 | `HoneyBookcase` | 蜂蜜书架 | 101 | — | ·/·/· | — |
| 2024 | `SteampunkBookcase` | 蒸汽朋克书架 | 101 | — | ·/·/· | — |
| 2025 | `GlassBookcase` | 玻璃书架 | 101 | — | ·/·/· | — |
| 2026 | `RichMahoganyBookcase` | 红木书架 | 101 | — | ·/·/· | — |
| 2027 | `PearlwoodBookcase` | 珍珠木书架 | 101 | — | ·/·/· | — |
| 2028 | `SpookyBookcase` | 阴森书架 | 101 | — | ·/·/· | — |
| 2029 | `SkywareBookcase` | 天域书架 | 101 | — | ·/·/· | — |
| 2030 | `LihzahrdBookcase` | 丛林蜥蜴书架 | 101 | — | ·/·/· | — |
| 2031 | `FrozenBookcase` | 冰冻书架 | 101 | — | ·/·/· | — |
| 2032 | `CactusLantern` | 仙人掌灯笼 | 42 | — | ·/·/· | — |
| 2033 | `EbonwoodLantern` | 乌木灯笼 | 42 | — | ·/·/· | — |
| 2034 | `FleshLantern` | 血肉灯笼 | 42 | — | ·/·/· | — |
| 2035 | `HoneyLantern` | 蜂蜜灯笼 | 42 | — | ·/·/· | — |
| 2036 | `SteampunkLantern` | 蒸汽朋克灯笼 | 42 | — | ·/·/· | — |
| 2037 | `GlassLantern` | 玻璃灯笼 | 42 | — | ·/·/· | — |
| 2038 | `RichMahoganyLantern` | 红木灯笼 | 42 | — | ·/·/· | — |
| 2039 | `PearlwoodLantern` | 珍珠木灯笼 | 42 | — | ·/·/· | — |
| 2040 | `FrozenLantern` | 冰冻灯笼 | 42 | — | ·/·/· | — |
| 2041 | `LihzahrdLantern` | 丛林蜥蜴灯笼 | 42 | — | ·/·/· | — |
| 2042 | `SkywareLantern` | 天域灯笼 | 42 | — | ·/·/· | — |
| 2043 | `SpookyLantern` | 阴森灯笼 | 42 | — | ·/·/· | — |
| 2044 | `FrozenDoor` | 冰冻门 | 10 | — | ·/·/· | — |
| 2045 | `CactusCandle` | 仙人掌蜡烛 | 33 | — | ·/·/· | — |
| 2046 | `EbonwoodCandle` | 乌木蜡烛 | 33 | — | ·/·/· | — |
| 2047 | `FleshCandle` | 血肉蜡烛 | 33 | — | ·/·/· | — |
| 2048 | `GlassCandle` | 玻璃蜡烛 | 33 | — | ·/·/· | — |
| 2049 | `FrozenCandle` | 冰冻蜡烛 | 33 | — | ·/·/· | — |
| 2050 | `RichMahoganyCandle` | 红木蜡烛 | 33 | — | ·/·/· | — |
| 2051 | `PearlwoodCandle` | 珍珠木蜡烛 | 33 | — | ·/·/· | — |
| 2052 | `LihzahrdCandle` | 丛林蜥蜴蜡烛 | 33 | — | ·/·/· | — |
| 2053 | `SkywareCandle` | 天域蜡烛 | 33 | — | ·/·/· | — |
| 2054 | `PumpkinCandle` | 南瓜蜡烛 | 33 | — | ·/·/· | — |
| 2055 | `CactusChandelier` | 仙人掌吊灯 | 34 | — | ·/·/· | — |
| 2056 | `EbonwoodChandelier` | 乌木吊灯 | 34 | — | ·/·/· | — |
| 2057 | `FleshChandelier` | 血肉吊灯 | 34 | — | ·/·/· | — |
| 2058 | `HoneyChandelier` | 蜂蜜吊灯 | 34 | — | ·/·/· | — |
| 2059 | `FrozenChandelier` | 冰冻吊灯 | 34 | — | ·/·/· | — |
| 2060 | `RichMahoganyChandelier` | 红木吊灯 | 34 | — | ·/·/· | — |
| 2061 | `PearlwoodChandelier` | 珍珠木吊灯 | 34 | — | ·/·/· | — |
| 2062 | `LihzahrdChandelier` | 丛林蜥蜴吊灯 | 34 | — | ·/·/· | — |
| 2063 | `SkywareChandelier` | 天域吊灯 | 34 | — | ·/·/· | — |
| 2064 | `SpookyChandelier` | 阴森吊灯 | 34 | — | ·/·/· | — |
| 2065 | `GlassChandelier` | 玻璃吊灯 | 34 | — | ·/·/· | — |
| 2066 | `CactusBed` | 仙人掌床 | 79 | — | ·/·/· | — |
| 2067 | `FleshBed` | 血肉床 | 79 | — | ·/·/· | — |
| 2068 | `FrozenBed` | 冰冻床 | 79 | — | ·/·/· | — |
| 2069 | `LihzahrdBed` | 丛林蜥蜴床 | 79 | — | ·/·/· | — |
| 2070 | `SkywareBed` | 天域床 | 79 | — | ·/·/· | — |
| 2071 | `SpookyBed` | 阴森床 | 79 | — | ·/·/· | — |
| 2072 | `CactusBathtub` | 仙人掌浴缸 | 90 | — | ·/·/· | — |
| 2073 | `EbonwoodBathtub` | 乌木浴缸 | 90 | — | ·/·/· | — |
| 2074 | `FleshBathtub` | 血肉浴缸 | 90 | — | ·/·/· | — |
| 2075 | `GlassBathtub` | 玻璃浴缸 | 90 | — | ·/·/· | — |
| 2076 | `FrozenBathtub` | 冰冻浴缸 | 90 | — | ·/·/· | — |
| 2077 | `RichMahoganyBathtub` | 红木浴缸 | 90 | — | ·/·/· | — |
| 2078 | `PearlwoodBathtub` | 珍珠木浴缸 | 90 | — | ·/·/· | — |
| 2079 | `LihzahrdBathtub` | 丛林蜥蜴浴缸 | 90 | — | ·/·/· | — |
| 2080 | `SkywareBathtub` | 天域浴缸 | 90 | — | ·/·/· | — |
| 2081 | `SpookyBathtub` | 阴森浴缸 | 90 | — | ·/·/· | — |
| 2082 | `CactusLamp` | 仙人掌灯 | 93 | — | ·/·/· | — |
| 2083 | `EbonwoodLamp` | 乌木灯 | 93 | — | ·/·/· | — |
| 2084 | `FleshLamp` | 血肉灯 | 93 | — | ·/·/· | — |
| 2085 | `GlassLamp` | 玻璃灯 | 93 | — | ·/·/· | — |
| 2086 | `FrozenLamp` | 冰冻灯 | 93 | — | ·/·/· | — |
| 2087 | `RichMahoganyLamp` | 红木灯 | 93 | — | ·/·/· | — |
| 2088 | `PearlwoodLamp` | 珍珠木灯 | 93 | — | ·/·/· | — |
| 2089 | `LihzahrdLamp` | 丛林蜥蜴灯 | 93 | — | ·/·/· | — |
| 2090 | `SkywareLamp` | 天域灯 | 93 | — | ·/·/· | — |
| 2091 | `SpookyLamp` | 阴森灯 | 93 | — | ·/·/· | — |
| 2092 | `CactusCandelabra` | 仙人掌烛台 | 100 | — | ·/·/· | — |
| 2093 | `EbonwoodCandelabra` | 乌木烛台 | 100 | — | ·/·/· | — |
| 2094 | `FleshCandelabra` | 血肉烛台 | 100 | — | ·/·/· | — |
| 2095 | `HoneyCandelabra` | 蜂蜜烛台 | 100 | — | ·/·/· | — |
| 2096 | `SteampunkCandelabra` | 蒸汽朋克烛台 | 100 | — | ·/·/· | — |
| 2097 | `GlassCandelabra` | 玻璃烛台 | 100 | — | ·/·/· | — |
| 2098 | `RichMahoganyCandelabra` | 红木大烛台 | 100 | — | ·/·/· | — |
| 2099 | `PearlwoodCandelabra` | 珍珠木烛台 | 100 | — | ·/·/· | — |
| 2100 | `FrozenCandelabra` | 冰冻烛台 | 100 | — | ·/·/· | — |
| 2101 | `LihzahrdCandelabra` | 丛林蜥蜴烛台 | 100 | — | ·/·/· | — |
| 2102 | `SkywareCandelabra` | 天域烛台 | 100 | — | ·/·/· | — |
| 2103 | `SpookyCandelabra` | 阴森大烛台 | 100 | — | ·/·/· | — |
| 2104 | `BrainMask` | 克苏鲁之脑面具 | — | — | ·/·/· | — |
| 2105 | `FleshMask` | 血肉墙面具 | — | — | ·/·/· | — |
| 2106 | `TwinMask` | 双子魔眼面具 | — | — | ·/·/· | — |
| 2107 | `SkeletronPrimeMask` | 机械骷髅王面具 | — | — | ·/·/· | — |
| 2108 | `BeeMask` | 蜂王面具 | — | — | ·/·/· | — |
| 2109 | `PlanteraMask` | 世纪之花面具 | — | — | ·/·/· | — |
| 2110 | `GolemMask` | 石巨人面具 | — | — | ·/·/· | — |
| 2111 | `EaterMask` | 世界吞噬怪面具 | — | — | ·/·/· | — |
| 2112 | `EyeMask` | 克苏鲁之眼面具 | — | — | ·/·/· | — |
| 2113 | `DestroyerMask` | 毁灭者面具 | — | — | ·/·/· | — |
| 2114 | `BlacksmithRack` | 铁匠架 | 240 | — | ·/·/· | — |
| 2115 | `CarpentryRack` | 木工架 | 240 | — | ·/·/· | — |
| 2116 | `HelmetRack` | 头盔架 | 240 | — | ·/·/· | — |
| 2117 | `SpearRack` | 长矛架 | 240 | — | ·/·/· | — |
| 2118 | `SwordRack` | 剑架 | 240 | — | ·/·/· | — |
| 2119 | `StoneSlab` | 石板 | 273 | — | ·/·/· | — |
| 2120 | `SandstoneSlab` | 沙岩板 | 274 | — | ·/·/· | — |
| 2121 | `Frog` | 青蛙 | — | — | ·/·/· | — |
| 2122 | `MallardDuck` | 野鸭 | — | — | ·/·/· | — |
| 2123 | `Duck` | 鸭 | — | — | ·/·/· | — |
| 2124 | `HoneyBathtub` | 蜂蜜浴缸 | 90 | — | ·/·/· | — |
| 2125 | `SteampunkBathtub` | 蒸汽朋克浴缸 | 90 | — | ·/·/· | — |
| 2126 | `LivingWoodBathtub` | 生命木浴缸 | 90 | — | ·/·/· | — |
| 2127 | `ShadewoodBathtub` | 暗影木浴缸 | 90 | — | ·/·/· | — |
| 2128 | `BoneBathtub` | 骨头浴缸 | 90 | — | ·/·/· | — |
| 2129 | `HoneyLamp` | 蜂蜜灯 | 93 | — | ·/·/· | — |
| 2130 | `SteampunkLamp` | 蒸汽朋克灯 | 93 | — | ·/·/· | — |
| 2131 | `LivingWoodLamp` | 生命木灯 | 93 | — | ·/·/· | — |
| 2132 | `ShadewoodLamp` | 暗影木灯 | 93 | — | ·/·/· | — |
| 2133 | `GoldenLamp` | 金灯 | 93 | — | ·/·/· | — |
| 2134 | `BoneLamp` | 骨头灯 | 93 | — | ·/·/· | — |
| 2135 | `LivingWoodBookcase` | 生命木书架 | 101 | — | ·/·/· | — |
| 2136 | `ShadewoodBookcase` | 暗影木书架 | 101 | — | ·/·/· | — |
| 2137 | `GoldenBookcase` | 金书架 | 101 | — | ·/·/· | — |
| 2138 | `BoneBookcase` | 骨头书架 | 101 | — | ·/·/· | — |
| 2139 | `LivingWoodBed` | 生命木床 | 79 | — | ·/·/· | — |
| 2140 | `BoneBed` | 骨头床 | 79 | — | ·/·/· | — |
| 2141 | `LivingWoodChandelier` | 生命木吊灯 | 34 | — | ·/·/· | — |
| 2142 | `ShadewoodChandelier` | 暗影木吊灯 | 34 | — | ·/·/· | — |
| 2143 | `GoldenChandelier` | 金制吊灯 | 34 | — | ·/·/· | — |
| 2144 | `BoneChandelier` | 骨头吊灯 | 34 | — | ·/·/· | — |
| 2145 | `LivingWoodLantern` | 生命木灯笼 | 42 | — | ·/·/· | — |
| 2146 | `ShadewoodLantern` | 暗影木灯笼 | 42 | — | ·/·/· | — |
| 2147 | `GoldenLantern` | 金灯笼 | 42 | — | ·/·/· | — |
| 2148 | `BoneLantern` | 骨头灯笼 | 42 | — | ·/·/· | — |
| 2149 | `LivingWoodCandelabra` | 生命木烛台 | 100 | — | ·/·/· | — |
| 2150 | `ShadewoodCandelabra` | 暗影木烛台 | 100 | — | ·/·/· | — |
| 2151 | `GoldenCandelabra` | 金烛台 | 100 | — | ·/·/· | — |
| 2152 | `BoneCandelabra` | 骨头烛台 | 100 | — | ·/·/· | — |
| 2153 | `LivingWoodCandle` | 生命木蜡烛 | 33 | — | ·/·/· | — |
| 2154 | `ShadewoodCandle` | 暗影木蜡烛 | 33 | — | ·/·/· | — |
| 2155 | `GoldenCandle` | 金蜡烛 | 33 | — | ·/·/· | — |
| 2156 | `BlackScorpion` | 黑蝎子 | — | — | ·/·/· | — |
| 2157 | `Scorpion` | 蝎子 | — | — | ·/·/· | — |
| 2158 | `BubbleWallpaper` | 泡泡壁纸 | — | 133 | ·/·/· | — |
| 2159 | `CopperPipeWallpaper` | 铜管壁纸 | — | 134 | ·/·/· | — |
| 2160 | `DuckyWallpaper` | 黄鸭壁纸 | — | 135 | ·/·/· | — |
| 2161 | `FrostCore` | 寒霜核 | — | — | ·/·/· | — |
| 2162 | `BunnyCage` | 兔兔笼 | 275 | — | ·/·/· | — |
| 2163 | `SquirrelCage` | 松鼠笼 | 276 | — | ·/·/· | — |
| 2164 | `MallardDuckCage` | 野鸭笼 | 277 | — | ·/·/· | — |
| 2165 | `DuckCage` | 鸭笼 | 278 | — | ·/·/· | — |
| 2166 | `BirdCage` | 鸟笼 | 279 | — | ·/·/· | — |
| 2167 | `BlueJayCage` | 冠蓝鸦笼 | 280 | — | ·/·/· | — |
| 2168 | `CardinalCage` | 红雀笼 | 281 | — | ·/·/· | — |
| 2169 | `WaterfallWall` | 瀑布墙 | — | 136 | ·/·/· | — |
| 2170 | `LavafallWall` | 熔岩瀑布墙 | — | 137 | ·/·/· | — |
| 2171 | `CrimsonSeeds` | 猩红种子 | 199 | — | ·/·/· | — |
| 2172 | `HeavyWorkBench` | 重型工作台 | 283 | — | ·/·/· | — |
| 2173 | `CopperPlating` | 铜镀层 | 284 | — | ·/·/· | — |
| 2174 | `SnailCage` | 蜗牛笼 | 285 | — | ·/·/· | — |
| 2175 | `GlowingSnailCage` | 发光蜗牛笼 | 286 | — | ·/·/· | — |
| 2176 | `ShroomiteDiggingClaw` | 蘑菇矿挖爪 | — | — | 200/25/· | 45 |
| 2177 | `AmmoBox` | 弹药箱 | 287 | — | ·/·/· | — |
| 2178 | `MonarchButterflyJar` | 帝王蝶罐 | 288 | — | ·/·/· | — |
| 2179 | `PurpleEmperorButterflyJar` | 紫君王蝶罐 | 289 | — | ·/·/· | — |
| 2180 | `RedAdmiralButterflyJar` | 红纹蝶罐 | 290 | — | ·/·/· | — |
| 2181 | `UlyssesButterflyJar` | 尤利西斯蝶罐 | 291 | — | ·/·/· | — |
| 2182 | `SulphurButterflyJar` | 硫磺蝶罐 | 292 | — | ·/·/· | — |
| 2183 | `TreeNymphButterflyJar` | 树若虫蝴蝶罐 | 293 | — | ·/·/· | — |
| 2184 | `ZebraSwallowtailButterflyJar` | 斑纹燕尾蝶罐 | 294 | — | ·/·/· | — |
| 2185 | `JuliaButterflyJar` | 茱莉娅蝶罐 | 295 | — | ·/·/· | — |
| 2186 | `ScorpionCage` | 蝎子笼 | 296 | — | ·/·/· | — |
| 2187 | `BlackScorpionCage` | 黑蝎子笼 | 297 | — | ·/·/· | — |
| 2188 | `VenomStaff` | 毒液法杖 | — | — | ·/·/· | 44 |
| 2189 | `SpectreMask` | 幽灵面具 | — | — | ·/·/· | — |
| 2190 | `FrogCage` | 蛙笼 | 298 | — | ·/·/· | — |
| 2191 | `MouseCage` | 老鼠笼 | 299 | — | ·/·/· | — |
| 2192 | `BoneWelder` | 骨头焊机 | 300 | — | ·/·/· | — |
| 2193 | `FleshCloningVaat` | 血肉克隆台 | 301 | — | ·/·/· | — |
| 2194 | `GlassKiln` | 玻璃窑 | 302 | — | ·/·/· | — |
| 2195 | `LihzahrdFurnace` | 丛林蜥蜴熔炉 | 303 | — | ·/·/· | — |
| 2196 | `LivingLoom` | 生命木织机 | 304 | — | ·/·/· | — |
| 2197 | `SkyMill` | 天磨 | 305 | — | ·/·/· | — |
| 2198 | `IceMachine` | 冰雪机 | 306 | — | ·/·/· | — |
| 2199 | `BeetleHelmet` | 甲虫头盔 | — | — | ·/·/· | — |
| 2200 | `BeetleScaleMail` | 甲虫铠甲 | — | — | ·/·/· | — |
| 2201 | `BeetleShell` | 甲虫壳 | — | — | ·/·/· | — |
| 2202 | `BeetleLeggings` | 甲虫护腿 | — | — | ·/·/· | — |
| 2203 | `SteampunkBoiler` | 蒸汽朋克锅炉 | 307 | — | ·/·/· | — |
| 2204 | `HoneyDispenser` | 蜂蜜分配机 | 308 | — | ·/·/· | — |
| 2205 | `Penguin` | 企鹅 | — | — | ·/·/· | — |
| 2206 | `PenguinCage` | 企鹅笼 | 309 | — | ·/·/· | — |
| 2207 | `WormCage` | 蠕虫笼 | 310 | — | ·/·/· | — |
| 2208 | `Terrarium` | 泰拉制笼机 | — | — | ·/·/· | — |
| 2209 | `SuperManaPotion` | 超级魔力药水 | — | — | ·/·/· | — |
| 2210 | `EbonwoodFence` | 乌木栅栏 | — | 138 | ·/·/· | — |
| 2211 | `RichMahoganyFence` | 红木栅栏 | — | 139 | ·/·/· | — |
| 2212 | `PearlwoodFence` | 珍珠木栅栏 | — | 140 | ·/·/· | — |
| 2213 | `ShadewoodFence` | 暗影木栅栏 | — | 141 | ·/·/· | — |
| 2214 | `BrickLayer` | 砖层 | — | — | ·/·/· | — |
| 2215 | `ExtendoGrip` | 加长握爪 | — | — | ·/·/· | — |
| 2216 | `PaintSprayer` | 喷漆器 | — | — | ·/·/· | — |
| 2217 | `PortableCementMixer` | 便携式水泥搅拌机 | — | — | ·/·/· | — |
| 2218 | `BeetleHusk` | 甲虫外壳 | — | — | ·/·/· | — |
| 2219 | `CelestialMagnet` | 天界磁石 | — | — | ·/·/· | — |
| 2220 | `CelestialEmblem` | 天界徽章 | — | — | ·/·/· | — |
| 2221 | `CelestialCuffs` | 天界手铐 | — | — | ·/·/· | — |
| 2222 | `PeddlersHat` | 商贩帽 | — | — | ·/·/· | — |
| 2223 | `PulseBow` | 脉冲弓 | — | — | ·/·/· | 80 |
| 2224 | `DynastyChandelier` | 大王朝灯笼 | 34 | — | ·/·/· | — |
| 2225 | `DynastyLamp` | 王朝灯 | 93 | — | ·/·/· | — |
| 2226 | `DynastyLantern` | 王朝灯笼 | 42 | — | ·/·/· | — |
| 2227 | `DynastyCandelabra` | 大王朝蜡烛 | 100 | — | ·/·/· | — |
| 2228 | `DynastyChair` | 王朝椅 | 15 | — | ·/·/· | — |
| 2229 | `DynastyWorkBench` | 王朝工作台 | 18 | — | ·/·/· | — |
| 2230 | `DynastyChest` | 王朝箱 | 21 | — | ·/·/· | — |
| 2231 | `DynastyBed` | 王朝床 | 79 | — | ·/·/· | — |
| 2232 | `DynastyBathtub` | 王朝浴缸 | 90 | — | ·/·/· | — |
| 2233 | `DynastyBookcase` | 王朝书架 | 101 | — | ·/·/· | — |
| 2234 | `DynastyCup` | 王朝杯 | 13 | — | ·/·/· | — |
| 2235 | `DynastyBowl` | 王朝碗 | 103 | — | ·/·/· | — |
| 2236 | `DynastyCandle` | 王朝蜡烛 | 33 | — | ·/·/· | — |
| 2237 | `DynastyClock` | 王朝时钟 | 104 | — | ·/·/· | — |
| 2238 | `GoldenClock` | 金时钟 | 104 | — | ·/·/· | — |
| 2239 | `GlassClock` | 玻璃时钟 | 104 | — | ·/·/· | — |
| 2240 | `HoneyClock` | 蜂蜜时钟 | 104 | — | ·/·/· | — |
| 2241 | `SteampunkClock` | 蒸汽朋克时钟 | 104 | — | ·/·/· | — |
| 2242 | `FancyDishes` | 精致餐具 | 103 | — | ·/·/· | — |
| 2243 | `GlassBowl` | 玻璃碗 | 103 | — | ·/·/· | — |
| 2244 | `WineGlass` | 葡萄酒杯 | 13 | — | ·/·/· | — |
| 2245 | `LivingWoodPiano` | 生命木钢琴 | 87 | — | ·/·/· | — |
| 2246 | `FleshPiano` | 血肉钢琴 | 87 | — | ·/·/· | — |
| 2247 | `FrozenPiano` | 冰冻钢琴 | 87 | — | ·/·/· | — |
| 2248 | `FrozenTable` | 冰冻桌 | 14 | — | ·/·/· | — |
| 2249 | `HoneyChest` | 蜂蜜箱 | 21 | — | ·/·/· | — |
| 2250 | `SteampunkChest` | 蒸汽朋克箱 | 21 | — | ·/·/· | — |
| 2251 | `HoneyWorkBench` | 蜂蜜工作台 | 18 | — | ·/·/· | — |
| 2252 | `FrozenWorkBench` | 冰冻工作台 | 18 | — | ·/·/· | — |
| 2253 | `SteampunkWorkBench` | 蒸汽朋克工作台 | 18 | — | ·/·/· | — |
| 2254 | `GlassPiano` | 玻璃钢琴 | 87 | — | ·/·/· | — |
| 2255 | `HoneyPiano` | 蜂蜜钢琴 | 87 | — | ·/·/· | — |
| 2256 | `SteampunkPiano` | 蒸汽朋克钢琴 | 87 | — | ·/·/· | — |
| 2257 | `HoneyCup` | 蜂蜜杯 | 13 | — | ·/·/· | — |
| 2258 | `SteampunkCup` | 圣餐杯 | 13 | — | ·/·/· | — |
| 2259 | `DynastyTable` | 王朝桌 | 14 | — | ·/·/· | — |
| 2260 | `DynastyWood` | 王朝木 | 311 | — | ·/·/· | — |
| 2261 | `RedDynastyShingles` | 红王朝瓦 | 312 | — | ·/·/· | — |
| 2262 | `BlueDynastyShingles` | 蓝王朝瓦 | 313 | — | ·/·/· | — |
| 2263 | `WhiteDynastyWall` | 白王朝墙 | — | 142 | ·/·/· | — |
| 2264 | `BlueDynastyWall` | 蓝王朝墙 | — | 143 | ·/·/· | — |
| 2265 | `DynastyDoor` | 王朝门 | 10 | — | ·/·/· | — |
| 2266 | `Sake` | 清酒 | — | — | ·/·/· | — |
| 2267 | `PadThai` | 泰式炒面 | — | — | ·/·/· | — |
| 2268 | `Pho` | 越南河粉 | — | — | ·/·/· | — |
| 2269 | `Revolver` | 左轮手枪 | — | — | ·/·/· | 20 |
| 2270 | `Gatligator` | 鳄鱼机关枪 | — | — | ·/·/· | 21 |
| 2271 | `ArcaneRuneWall` | 奥术神符墙 | — | 144 | ·/·/· | — |
| 2272 | `WaterGun` | 水枪 | — | — | ·/·/· | — |
| 2273 | `Katana` | 武士刀 | — | — | ·/·/· | 18 |
| 2274 | `UltrabrightTorch` | 超亮火把 | 4 | — | ·/·/· | — |
| 2275 | `MagicHat` | 魔法帽 | — | — | ·/·/· | — |
| 2276 | `DiamondRing` | 钻石戒指 | — | — | ·/·/· | — |
| 2277 | `Gi` | 稽古衣 | — | — | ·/·/· | — |
| 2278 | `Kimono` | 和服 | — | — | ·/·/· | — |
| 2279 | `GypsyRobe` | 神秘长袍 | — | — | ·/·/· | — |
| 2280 | `BeetleWings` | 甲虫之翼 | — | — | ·/·/· | — |
| 2281 | `TigerSkin` | 虎皮 | 242 | — | ·/·/· | — |
| 2282 | `LeopardSkin` | 豹皮 | 242 | — | ·/·/· | — |
| 2283 | `ZebraSkin` | 斑马皮 | 242 | — | ·/·/· | — |
| 2284 | `CrimsonCloak` | 猩红斗篷 | — | — | ·/·/· | — |
| 2285 | `MysteriousCape` | 神秘披风 | — | — | ·/·/· | — |
| 2286 | `RedCape` | 红披风 | — | — | ·/·/· | — |
| 2287 | `WinterCape` | 冬季披风 | — | — | ·/·/· | — |
| 2288 | `FrozenChair` | 冰冻椅 | 15 | — | ·/·/· | — |
| 2289 | `WoodFishingPole` | 木钓竿 | — | — | ·/·/· | — |
| 2290 | `Bass` | 鲈鱼 | — | — | ·/·/· | — |
| 2291 | `ReinforcedFishingPole` | 强化钓竿 | — | — | ·/·/· | — |
| 2292 | `FiberglassFishingPole` | 玻璃钢钓竿 | — | — | ·/·/· | — |
| 2293 | `FisherofSouls` | 灵魂钓手 | — | — | ·/·/· | — |
| 2294 | `GoldenFishingRod` | 金钓竿 | — | — | ·/·/· | — |
| 2295 | `MechanicsRod` | 机械师钓竿 | — | — | ·/·/· | — |
| 2296 | `SittingDucksFishingRod` | 冤大头钓竿 | — | — | ·/·/· | — |
| 2297 | `Trout` | 鳟鱼 | — | — | ·/·/· | — |
| 2298 | `Salmon` | 三文鱼 | — | — | ·/·/· | — |
| 2299 | `AtlanticCod` | 大西洋鳕鱼 | — | — | ·/·/· | — |
| 2300 | `Tuna` | 金枪鱼 | — | — | ·/·/· | — |
| 2301 | `RedSnapper` | 红鲷鱼 | — | — | ·/·/· | — |
| 2302 | `NeonTetra` | 霓虹脂鲤 | — | — | ·/·/· | — |
| 2303 | `ArmoredCavefish` | 装甲洞穴鱼 | — | — | ·/·/· | — |
| 2304 | `Damselfish` | 雀鲷 | — | — | ·/·/· | — |
| 2305 | `CrimsonTigerfish` | 猩红虎鱼 | — | — | ·/·/· | — |
| 2306 | `FrostMinnow` | 寒霜鲦鱼 | — | — | ·/·/· | — |
| 2307 | `PrincessFish` | 公主鱼 | — | — | ·/·/· | — |
| 2308 | `GoldenCarp` | 金鲤鱼 | — | — | ·/·/· | — |
| 2309 | `SpecularFish` | 镜面鱼 | — | — | ·/·/· | — |
| 2310 | `Prismite` | 七彩矿鱼 | — | — | ·/·/· | — |
| 2311 | `VariegatedLardfish` | 斑驳油鱼 | — | — | ·/·/· | — |
| 2312 | `FlarefinKoi` | 闪鳍锦鲤 | — | — | ·/·/· | — |
| 2313 | `DoubleCod` | 双鳍鳕鱼 | — | — | ·/·/· | — |
| 2314 | `Honeyfin` | 蜂蜜鱼 | — | — | ·/·/· | — |
| 2315 | `Obsidifish` | 黑曜石鱼 | — | — | ·/·/· | — |
| 2316 | `Shrimp` | 虾 | — | — | ·/·/· | — |
| 2317 | `ChaosFish` | 混沌鱼 | — | — | ·/·/· | — |
| 2318 | `Ebonkoi` | 黑檀锦鲤 | — | — | ·/·/· | — |
| 2319 | `Hemopiranha` | 血腥食人鱼 | — | — | ·/·/· | — |
| 2320 | `Rockfish` | 岩鱼锤 | — | — | ·/·/70 | 24 |
| 2321 | `Stinkfish` | 臭味鱼 | — | — | ·/·/· | — |
| 2322 | `MiningPotion` | 挖矿药水 | — | — | ·/·/· | — |
| 2323 | `HeartreachPotion` | 拾心药水 | — | — | ·/·/· | — |
| 2324 | `CalmingPotion` | 镇静药水 | — | — | ·/·/· | — |
| 2325 | `BuilderPotion` | 建筑工药水 | — | — | ·/·/· | — |
| 2326 | `TitanPotion` | 泰坦药水 | — | — | ·/·/· | — |
| 2327 | `FlipperPotion` | 脚蹼药水 | — | — | ·/·/· | — |
| 2328 | `SummoningPotion` | 召唤药水 | — | — | ·/·/· | — |
| 2329 | `TrapsightPotion` | 危险感药水 | — | — | ·/·/· | — |
| 2330 | `PurpleClubberfish` | 紫挥棒鱼 | — | — | ·/·/· | 35 |
| 2331 | `ObsidianSwordfish` | 黑曜石剑鱼 | — | — | ·/·/· | 70 |
| 2332 | `Swordfish` | 剑鱼 | — | — | ·/·/· | 19 |
| 2333 | `IronFence` | 铁栅栏 | — | 145 | ·/·/· | — |
| 2334 | `WoodenCrate` | 木匣 | 376 | — | ·/·/· | — |
| 2335 | `IronCrate` | 铁匣 | 376 | — | ·/·/· | — |
| 2336 | `GoldenCrate` | 金匣 | 376 | — | ·/·/· | — |
| 2337 | `OldShoe` | 旧鞋 | — | — | ·/·/· | — |
| 2338 | `FishingSeaweed` | 海草 | — | — | ·/·/· | — |
| 2339 | `TinCan` | 锡罐 | — | — | ·/·/· | — |
| 2340 | `MinecartTrack` | 矿车轨道 | 314 | — | ·/·/· | — |
| 2341 | `ReaverShark` | 掠夺鲨 | — | — | 59/·/· | 16 |
| 2342 | `SawtoothShark` | 锯齿鲨 | — | — | ·/14/· | 13 |
| 2343 | `Minecart` | 矿车 | — | — | ·/·/· | — |
| 2344 | `AmmoReservationPotion` | 弹药储备药水 | — | — | ·/·/· | — |
| 2345 | `LifeforcePotion` | 生命力药水 | — | — | ·/·/· | — |
| 2346 | `EndurancePotion` | 耐力药水 | — | — | ·/·/· | — |
| 2347 | `RagePotion` | 暴怒药水 | — | — | ·/·/· | — |
| 2348 | `InfernoPotion` | 狱火药水 | — | — | ·/·/· | — |
| 2349 | `WrathPotion` | 怒气药水 | — | — | ·/·/· | — |
| 2350 | `RecallPotion` | 回忆药水 | — | — | ·/·/· | — |
| 2351 | `TeleportationPotion` | 传送药水 | — | — | ·/·/· | — |
| 2352 | `LovePotion` | 爱情药水 | — | — | ·/·/· | — |
| 2353 | `StinkPotion` | 臭味药水 | — | — | ·/·/· | — |
| 2354 | `FishingPotion` | 钓鱼药水 | — | — | ·/·/· | — |
| 2355 | `SonarPotion` | 声纳药水 | — | — | ·/·/· | — |
| 2356 | `CratePotion` | 宝匣药水 | — | — | ·/·/· | — |
| 2357 | `ShiverthornSeeds` | 寒颤棘种子 | 82 | — | ·/·/· | — |
| 2358 | `Shiverthorn` | 寒颤棘 | — | — | ·/·/· | — |
| 2359 | `WarmthPotion` | 保暖药水 | — | — | ·/·/· | — |
| 2360 | `FishHook` | 鱼钩 | — | — | ·/·/· | — |
| 2361 | `BeeHeadgear` | 蜜蜂头饰 | — | — | ·/·/· | — |
| 2362 | `BeeBreastplate` | 蜜蜂胸甲 | — | — | ·/·/· | — |
| 2363 | `BeeGreaves` | 蜜蜂护胫 | — | — | ·/·/· | — |
| 2364 | `HornetStaff` | 黄蜂法杖 | — | — | ·/·/· | 12 |
| 2365 | `ImpStaff` | 小鬼法杖 | — | — | ·/·/· | 17 |
| 2366 | `QueenSpiderStaff` | 蜘蛛女王法杖 | — | — | ·/·/· | 26 |
| 2367 | `AnglerHat` | 渔夫帽 | — | — | ·/·/· | — |
| 2368 | `AnglerVest` | 渔夫背心 | — | — | ·/·/· | — |
| 2369 | `AnglerPants` | 渔夫裤 | — | — | ·/·/· | — |
| 2370 | `SpiderMask` | 蜘蛛面具 | — | — | ·/·/· | — |
| 2371 | `SpiderBreastplate` | 蜘蛛胸甲 | — | — | ·/·/· | — |
| 2372 | `SpiderGreaves` | 蜘蛛护胫 | — | — | ·/·/· | — |
| 2373 | `HighTestFishingLine` | 优质钓鱼线 | — | — | ·/·/· | — |
| 2374 | `AnglerEarring` | 渔夫耳环 | — | — | ·/·/· | — |
| 2375 | `TackleBox` | 钓具箱 | — | — | ·/·/· | — |
| 2376 | `BlueDungeonPiano` | 蓝地牢钢琴 | 87 | — | ·/·/· | — |
| 2377 | `GreenDungeonPiano` | 绿地牢钢琴 | 87 | — | ·/·/· | — |
| 2378 | `PinkDungeonPiano` | 粉地牢钢琴 | 87 | — | ·/·/· | — |
| 2379 | `GoldenPiano` | 金钢琴 | 87 | — | ·/·/· | — |
| 2380 | `ObsidianPiano` | 黑曜石钢琴 | 87 | — | ·/·/· | — |
| 2381 | `BonePiano` | 骨头钢琴 | 87 | — | ·/·/· | — |
| 2382 | `CactusPiano` | 仙人掌钢琴 | 87 | — | ·/·/· | — |
| 2383 | `SpookyPiano` | 阴森钢琴 | 87 | — | ·/·/· | — |
| 2384 | `SkywarePiano` | 天域钢琴 | 87 | — | ·/·/· | — |
| 2385 | `LihzahrdPiano` | 丛林蜥蜴钢琴 | 87 | — | ·/·/· | — |
| 2386 | `BlueDungeonDresser` | 蓝地牢梳妆台 | 88 | — | ·/·/· | — |
| 2387 | `GreenDungeonDresser` | 绿地牢梳妆台 | 88 | — | ·/·/· | — |
| 2388 | `PinkDungeonDresser` | 粉地牢梳妆台 | 88 | — | ·/·/· | — |
| 2389 | `GoldenDresser` | 金梳妆台 | 88 | — | ·/·/· | — |
| 2390 | `ObsidianDresser` | 黑曜石梳妆台 | 88 | — | ·/·/· | — |
| 2391 | `BoneDresser` | 骨头梳妆台 | 88 | — | ·/·/· | — |
| 2392 | `CactusDresser` | 仙人掌梳妆台 | 88 | — | ·/·/· | — |
| 2393 | `SpookyDresser` | 阴森梳妆台 | 88 | — | ·/·/· | — |
| 2394 | `SkywareDresser` | 天域梳妆台 | 88 | — | ·/·/· | — |
| 2395 | `HoneyDresser` | 蜂蜜梳妆台 | 88 | — | ·/·/· | — |
| 2396 | `LihzahrdDresser` | 丛林蜥蜴梳妆台 | 88 | — | ·/·/· | — |
| 2397 | `Sofa` | 沙发 | 89 | — | ·/·/· | — |
| 2398 | `EbonwoodSofa` | 乌木沙发 | 89 | — | ·/·/· | — |
| 2399 | `RichMahoganySofa` | 红木沙发 | 89 | — | ·/·/· | — |
| 2400 | `PearlwoodSofa` | 珍珠木沙发 | 89 | — | ·/·/· | — |
| 2401 | `ShadewoodSofa` | 暗影木沙发 | 89 | — | ·/·/· | — |
| 2402 | `BlueDungeonSofa` | 蓝地牢沙发 | 89 | — | ·/·/· | — |
| 2403 | `GreenDungeonSofa` | 绿地牢沙发 | 89 | — | ·/·/· | — |
| 2404 | `PinkDungeonSofa` | 粉地牢沙发 | 89 | — | ·/·/· | — |
| 2405 | `GoldenSofa` | 金沙发 | 89 | — | ·/·/· | — |
| 2406 | `ObsidianSofa` | 黑曜石沙发 | 89 | — | ·/·/· | — |
| 2407 | `BoneSofa` | 骨头沙发 | 89 | — | ·/·/· | — |
| 2408 | `CactusSofa` | 仙人掌沙发 | 89 | — | ·/·/· | — |
| 2409 | `SpookySofa` | 阴森沙发 | 89 | — | ·/·/· | — |
| 2410 | `SkywareSofa` | 天域沙发 | 89 | — | ·/·/· | — |
| 2411 | `HoneySofa` | 蜂蜜沙发 | 89 | — | ·/·/· | — |
| 2412 | `SteampunkSofa` | 蒸汽朋克沙发 | 89 | — | ·/·/· | — |
| 2413 | `MushroomSofa` | 蘑菇沙发 | 89 | — | ·/·/· | — |
| 2414 | `GlassSofa` | 玻璃沙发 | 89 | — | ·/·/· | — |
| 2415 | `PumpkinSofa` | 南瓜沙发 | 89 | — | ·/·/· | — |
| 2416 | `LihzahrdSofa` | 丛林蜥蜴沙发 | 89 | — | ·/·/· | — |
| 2417 | `SeashellHairpin` | 贝壳发夹 | — | — | ·/·/· | — |
| 2418 | `MermaidAdornment` | 美人鱼饰品 | — | — | ·/·/· | — |
| 2419 | `MermaidTail` | 美人鱼鱼尾裤 | — | — | ·/·/· | — |
| 2420 | `ZephyrFish` | 和风鱼 | — | — | ·/·/· | — |
| 2421 | `Fleshcatcher` | 捕肉手 | — | — | ·/·/· | — |
| 2422 | `HotlineFishingHook` | 熔线钓钩 | — | — | ·/·/· | — |
| 2423 | `FrogLeg` | 蛙腿 | — | — | ·/·/· | — |
| 2424 | `Anchor` | 锚 | — | — | ·/·/· | 70 |
| 2425 | `CookedFish` | 熟鱼 | — | — | ·/·/· | — |
| 2426 | `CookedShrimp` | 熟虾 | — | — | ·/·/· | — |
| 2427 | `Sashimi` | 生鱼片 | — | — | ·/·/· | — |
| 2428 | `FuzzyCarrot` | 绒毛胡萝卜 | — | — | ·/·/· | — |
| 2429 | `ScalyTruffle` | 带鳞松露 | — | — | ·/·/· | — |
| 2430 | `SlimySaddle` | 粘鞍 | — | — | ·/·/· | — |
| 2431 | `BeeWax` | 蜂蜡 | — | — | ·/·/· | — |
| 2432 | `CopperPlatingWall` | 镀铜墙 | — | 146 | ·/·/· | — |
| 2433 | `StoneSlabWall` | 石板墙 | — | 147 | ·/·/· | — |
| 2434 | `Sail` | 船帆 | — | 148 | ·/·/· | — |
| 2435 | `CoralstoneBlock` | 珊瑚石块 | 315 | — | ·/·/· | — |
| 2436 | `BlueJellyfish` | 蓝水母 | — | — | ·/·/· | — |
| 2437 | `GreenJellyfish` | 绿水母 | — | — | ·/·/· | — |
| 2438 | `PinkJellyfish` | 粉水母 | — | — | ·/·/· | — |
| 2439 | `BlueJellyfishJar` | 蓝水母罐 | 316 | — | ·/·/· | — |
| 2440 | `GreenJellyfishJar` | 绿水母罐 | 317 | — | ·/·/· | — |
| 2441 | `PinkJellyfishJar` | 粉水母罐 | 318 | — | ·/·/· | — |
| 2442 | `LifePreserver` | 救生圈 | 240 | — | ·/·/· | — |
| 2443 | `ShipsWheel` | 舵轮 | 240 | — | ·/·/· | — |
| 2444 | `CompassRose` | 罗盘针 | 240 | — | ·/·/· | — |
| 2445 | `WallAnchor` | 墙锚 | 240 | — | ·/·/· | — |
| 2446 | `GoldfishTrophy` | 金鱼纪念章 | 240 | — | ·/·/· | — |
| 2447 | `BunnyfishTrophy` | 兔兔鱼纪念章 | 240 | — | ·/·/· | — |
| 2448 | `SwordfishTrophy` | 剑鱼纪念章 | 240 | — | ·/·/· | — |
| 2449 | `SharkteethTrophy` | 鲨牙纪念章 | 240 | — | ·/·/· | — |
| 2450 | `Batfish` | 蝙蝠鱼 | — | — | ·/·/· | — |
| 2451 | `BumblebeeTuna` | 大黄蜂金枪鱼 | — | — | ·/·/· | — |
| 2452 | `Catfish` | 鲶鱼 | — | — | ·/·/· | — |
| 2453 | `Cloudfish` | 云鱼 | — | — | ·/·/· | — |
| 2454 | `Cursedfish` | 诅咒鱼 | — | — | ·/·/· | — |
| 2455 | `Dirtfish` | 土鱼 | — | — | ·/·/· | — |
| 2456 | `DynamiteFish` | 雷管鱼 | — | — | ·/·/· | — |
| 2457 | `EaterofPlankton` | 浮游噬鱼 | — | — | ·/·/· | — |
| 2458 | `FallenStarfish` | 坠落海星 | — | — | ·/·/· | — |
| 2459 | `TheFishofCthulu` | 克苏鲁鱼 | — | — | ·/·/· | — |
| 2460 | `Fishotron` | 骷髅鱼 | — | — | ·/·/· | — |
| 2461 | `Harpyfish` | 鸟妖鱼 | — | — | ·/·/· | — |
| 2462 | `Hungerfish` | 饥饿鱼 | — | — | ·/·/· | — |
| 2463 | `Ichorfish` | 灵液鱼 | — | — | ·/·/· | — |
| 2464 | `Jewelfish` | 珠宝鱼 | — | — | ·/·/· | — |
| 2465 | `MirageFish` | 幻影鱼 | — | — | ·/·/· | — |
| 2466 | `MutantFlinxfin` | 突变弗林鱼 | — | — | ·/·/· | — |
| 2467 | `Pengfish` | 企鹅鱼 | — | — | ·/·/· | — |
| 2468 | `Pixiefish` | 妖精鱼 | — | — | ·/·/· | — |
| 2469 | `Spiderfish` | 蜘蛛鱼 | — | — | ·/·/· | — |
| 2470 | `TundraTrout` | 苔原鳟鱼 | — | — | ·/·/· | — |
| 2471 | `UnicornFish` | 独角兽鱼 | — | — | ·/·/· | — |
| 2472 | `GuideVoodooFish` | 向导巫毒鱼 | — | — | ·/·/· | — |
| 2473 | `Wyverntail` | 飞龙尾 | — | — | ·/·/· | — |
| 2474 | `ZombieFish` | 僵尸鱼 | — | — | ·/·/· | — |
| 2475 | `AmanitaFungifin` | 毒菌鱼 | — | — | ·/·/· | — |
| 2476 | `Angelfish` | 天使鱼 | — | — | ·/·/· | — |
| 2477 | `BloodyManowar` | 血腥战神 | — | — | ·/·/· | — |
| 2478 | `Bonefish` | 骷髅鱼 | — | — | ·/·/· | — |
| 2479 | `Bunnyfish` | 兔兔鱼 | — | — | ·/·/· | — |
| 2480 | `CapnTunabeard` | 金枪鱼须船长 | — | — | ·/·/· | — |
| 2481 | `Clownfish` | 小丑鱼 | — | — | ·/·/· | — |
| 2482 | `DemonicHellfish` | 恶魔地狱鱼 | — | — | ·/·/· | — |
| 2483 | `Derpfish` | 跳跳鱼 | — | — | ·/·/· | — |
| 2484 | `Fishron` | 猪龙鱼 | — | — | ·/·/· | — |
| 2485 | `InfectedScabbardfish` | 染病鞘鱼 | — | — | ·/·/· | — |
| 2486 | `Mudfish` | 泥鱼 | — | — | ·/·/· | — |
| 2487 | `Slimefish` | 史莱姆鱼 | — | — | ·/·/· | — |
| 2488 | `TropicalBarracuda` | 热带梭鱼 | — | — | ·/·/· | — |
| 2489 | `KingSlimeTrophy` | 史莱姆王纪念章 | 240 | — | ·/·/· | — |
| 2490 | `ShipInABottle` | 船舶瓶 | 319 | — | ·/·/· | — |
| 2491 | `HardySaddle` | 海龟鞍 | — | — | ·/·/· | — |
| 2492 | `PressureTrack` | 压力板轨道 | 314 | — | ·/·/· | — |
| 2493 | `KingSlimeMask` | 史莱姆王面具 | — | — | ·/·/· | — |
| 2494 | `FinWings` | 鳍翼 | — | — | ·/·/· | — |
| 2495 | `TreasureMap` | 宝藏地图 | 242 | — | ·/·/· | — |
| 2496 | `SeaweedPlanter` | 海草花盆 | 320 | — | ·/·/· | — |
| 2497 | `PillaginMePixels` | 海贼像素画 | 242 | — | ·/·/· | — |
| 2498 | `FishCostumeMask` | 鱼装面具 | — | — | ·/·/· | — |
| 2499 | `FishCostumeShirt` | 鱼装衣 | — | — | ·/·/· | — |
| 2500 | `FishCostumeFinskirt` | 鱼装鳍裙 | — | — | ·/·/· | — |
| 2501 | `GingerBeard` | 姜须 | — | — | ·/·/· | — |
| 2502 | `HoneyedGoggles` | 涂蜜护目镜 | — | — | ·/·/· | — |
| 2503 | `BorealWood` | 针叶木 | 321 | — | ·/·/· | — |
| 2504 | `PalmWood` | 棕榈木 | 322 | — | ·/·/· | — |
| 2505 | `BorealWoodWall` | 针叶木墙 | — | 149 | ·/·/· | — |
| 2506 | `PalmWoodWall` | 棕榈木墙 | — | 151 | ·/·/· | — |
| 2507 | `BorealWoodFence` | 针叶木栅栏 | — | 150 | ·/·/· | — |
| 2508 | `PalmWoodFence` | 棕榈木栅栏 | — | 152 | ·/·/· | — |
| 2509 | `BorealWoodHelmet` | 针叶木头盔 | — | — | ·/·/· | — |
| 2510 | `BorealWoodBreastplate` | 针叶木胸甲 | — | — | ·/·/· | — |
| 2511 | `BorealWoodGreaves` | 针叶木护胫 | — | — | ·/·/· | — |
| 2512 | `PalmWoodHelmet` | 棕榈木头盔 | — | — | ·/·/· | — |
| 2513 | `PalmWoodBreastplate` | 棕榈木胸甲 | — | — | ·/·/· | — |
| 2514 | `PalmWoodGreaves` | 棕榈木护胫 | — | — | ·/·/· | — |
| 2515 | `PalmWoodBow` | 棕榈木弓 | — | — | ·/·/· | 6 |
| 2516 | `PalmWoodHammer` | 棕榈木锤 | — | — | ·/·/35 | 4 |
| 2517 | `PalmWoodSword` | 棕榈木剑 | — | — | ·/·/· | 8 |
| 2518 | `PalmWoodPlatform` | 棕榈木平台 | 19 | — | ·/·/· | — |
| 2519 | `PalmWoodBathtub` | 棕榈木浴缸 | 90 | — | ·/·/· | — |
| 2520 | `PalmWoodBed` | 棕榈木床 | 79 | — | ·/·/· | — |
| 2521 | `PalmWoodBench` | 棕榈木长椅 | 89 | — | ·/·/· | — |
| 2522 | `PalmWoodCandelabra` | 棕榈木烛台 | 100 | — | ·/·/· | — |
| 2523 | `PalmWoodCandle` | 棕榈木蜡烛 | 33 | — | ·/·/· | — |
| 2524 | `PalmWoodChair` | 棕榈木椅 | 15 | — | ·/·/· | — |
| 2525 | `PalmWoodChandelier` | 棕榈木吊灯 | 34 | — | ·/·/· | — |
| 2526 | `PalmWoodChest` | 棕榈木箱 | 21 | — | ·/·/· | — |
| 2527 | `PalmWoodSofa` | 棕榈木沙发 | 89 | — | ·/·/· | — |
| 2528 | `PalmWoodDoor` | 棕榈木门 | 10 | — | ·/·/· | — |
| 2529 | `PalmWoodDresser` | 棕榈木梳妆台 | 88 | — | ·/·/· | — |
| 2530 | `PalmWoodLantern` | 棕榈木灯笼 | 42 | — | ·/·/· | — |
| 2531 | `PalmWoodPiano` | 棕榈木钢琴 | 87 | — | ·/·/· | — |
| 2532 | `PalmWoodTable` | 棕榈木桌 | 14 | — | ·/·/· | — |
| 2533 | `PalmWoodLamp` | 棕榈木灯 | 93 | — | ·/·/· | — |
| 2534 | `PalmWoodWorkBench` | 棕榈木工作台 | 18 | — | ·/·/· | — |
| 2535 | `OpticStaff` | 魔眼法杖 | — | — | ·/·/· | 24 |
| 2536 | `PalmWoodBookcase` | 棕榈木书架 | 101 | — | ·/·/· | — |
| 2537 | `MushroomBathtub` | 蘑菇浴缸 | 90 | — | ·/·/· | — |
| 2538 | `MushroomBed` | 蘑菇床 | 79 | — | ·/·/· | — |
| 2539 | `MushroomBench` | 蘑菇长椅 | 89 | — | ·/·/· | — |
| 2540 | `MushroomBookcase` | 蘑菇书架 | 101 | — | ·/·/· | — |
| 2541 | `MushroomCandelabra` | 蘑菇烛台 | 100 | — | ·/·/· | — |
| 2542 | `MushroomCandle` | 蘑菇蜡烛 | 33 | — | ·/·/· | — |
| 2543 | `MushroomChandelier` | 蘑菇吊灯 | 34 | — | ·/·/· | — |
| 2544 | `MushroomChest` | 蘑菇箱 | 21 | — | ·/·/· | — |
| 2545 | `MushroomDresser` | 蘑菇梳妆台 | 88 | — | ·/·/· | — |
| 2546 | `MushroomLantern` | 蘑菇灯笼 | 42 | — | ·/·/· | — |
| 2547 | `MushroomLamp` | 蘑菇灯 | 93 | — | ·/·/· | — |
| 2548 | `MushroomPiano` | 蘑菇钢琴 | 87 | — | ·/·/· | — |
| 2549 | `MushroomPlatform` | 蘑菇平台 | 19 | — | ·/·/· | — |
| 2550 | `MushroomTable` | 蘑菇桌 | 14 | — | ·/·/· | — |
| 2551 | `SpiderStaff` | 蜘蛛法杖 | — | — | ·/·/· | 26 |
| 2552 | `BorealWoodBathtub` | 针叶木浴缸 | 90 | — | ·/·/· | — |
| 2553 | `BorealWoodBed` | 针叶木床 | 79 | — | ·/·/· | — |
| 2554 | `BorealWoodBookcase` | 针叶木书架 | 101 | — | ·/·/· | — |
| 2555 | `BorealWoodCandelabra` | 针叶木烛台 | 100 | — | ·/·/· | — |
| 2556 | `BorealWoodCandle` | 针叶木蜡烛 | 33 | — | ·/·/· | — |
| 2557 | `BorealWoodChair` | 针叶木椅 | 15 | — | ·/·/· | — |
| 2558 | `BorealWoodChandelier` | 针叶木吊灯 | 34 | — | ·/·/· | — |
| 2559 | `BorealWoodChest` | 针叶木箱 | 21 | — | ·/·/· | — |
| 2560 | `BorealWoodClock` | 针叶木时钟 | 104 | — | ·/·/· | — |
| 2561 | `BorealWoodDoor` | 针叶木门 | 10 | — | ·/·/· | — |
| 2562 | `BorealWoodDresser` | 针叶木梳妆台 | 88 | — | ·/·/· | — |
| 2563 | `BorealWoodLamp` | 针叶木灯 | 93 | — | ·/·/· | — |
| 2564 | `BorealWoodLantern` | 针叶木灯笼 | 42 | — | ·/·/· | — |
| 2565 | `BorealWoodPiano` | 针叶木钢琴 | 87 | — | ·/·/· | — |
| 2566 | `BorealWoodPlatform` | 针叶木平台 | 19 | — | ·/·/· | — |
| 2567 | `SlimeBathtub` | 史莱姆浴缸 | 90 | — | ·/·/· | — |
| 2568 | `SlimeBed` | 史莱姆床 | 79 | — | ·/·/· | — |
| 2569 | `SlimeBookcase` | 史莱姆书架 | 101 | — | ·/·/· | — |
| 2570 | `SlimeCandelabra` | 史莱姆烛台 | 100 | — | ·/·/· | — |
| 2571 | `SlimeCandle` | 史莱姆蜡烛 | 33 | — | ·/·/· | — |
| 2572 | `SlimeChair` | 史莱姆椅 | 15 | — | ·/·/· | — |
| 2573 | `SlimeChandelier` | 史莱姆吊灯 | 34 | — | ·/·/· | — |
| 2574 | `SlimeChest` | 史莱姆箱 | 21 | — | ·/·/· | — |
| 2575 | `SlimeClock` | 史莱姆时钟 | 104 | — | ·/·/· | — |
| 2576 | `SlimeDoor` | 史莱姆门 | 10 | — | ·/·/· | — |
| 2577 | `SlimeDresser` | 史莱姆梳妆台 | 88 | — | ·/·/· | — |
| 2578 | `SlimeLamp` | 史莱姆灯 | 93 | — | ·/·/· | — |
| 2579 | `SlimeLantern` | 史莱姆灯笼 | 42 | — | ·/·/· | — |
| 2580 | `SlimePiano` | 史莱姆钢琴 | 87 | — | ·/·/· | — |
| 2581 | `SlimePlatform` | 史莱姆平台 | 19 | — | ·/·/· | — |
| 2582 | `SlimeSofa` | 史莱姆沙发 | 89 | — | ·/·/· | — |
| 2583 | `SlimeTable` | 史莱姆桌 | 14 | — | ·/·/· | — |
| 2584 | `PirateStaff` | 海盗法杖 | — | — | ·/·/· | 40 |
| 2585 | `SlimeHook` | 史莱姆钩 | — | — | ·/·/· | — |
| 2586 | `StickyGrenade` | 粘性手榴弹 | — | — | ·/·/· | 60 |
| 2587 | `TartarSauce` | 塔塔酱 | — | — | ·/·/· | — |
| 2588 | `DukeFishronMask` | 猪龙鱼公爵面具 | — | — | ·/·/· | — |
| 2589 | `DukeFishronTrophy` | 猪龙鱼公爵纪念章 | 240 | — | ·/·/· | — |
| 2590 | `MolotovCocktail` | 莫洛托夫鸡尾酒 | — | — | ·/·/· | 23 |
| 2591 | `BoneClock` | 骨头时钟 | 104 | — | ·/·/· | — |
| 2592 | `CactusClock` | 仙人掌时钟 | 104 | — | ·/·/· | — |
| 2593 | `EbonwoodClock` | 乌木时钟 | 104 | — | ·/·/· | — |
| 2594 | `FrozenClock` | 冰冻时钟 | 104 | — | ·/·/· | — |
| 2595 | `LihzahrdClock` | 丛林蜥蜴时钟 | 104 | — | ·/·/· | — |
| 2596 | `LivingWoodClock` | 生命木时钟 | 104 | — | ·/·/· | — |
| 2597 | `RichMahoganyClock` | 红木时钟 | 104 | — | ·/·/· | — |
| 2598 | `FleshClock` | 血肉时钟 | 104 | — | ·/·/· | — |
| 2599 | `MushroomClock` | 蘑菇时钟 | 104 | — | ·/·/· | — |
| 2600 | `ObsidianClock` | 黑曜石时钟 | 104 | — | ·/·/· | — |
| 2601 | `PalmWoodClock` | 棕榈木时钟 | 104 | — | ·/·/· | — |
| 2602 | `PearlwoodClock` | 珍珠木时钟 | 104 | — | ·/·/· | — |
| 2603 | `PumpkinClock` | 南瓜时钟 | 104 | — | ·/·/· | — |
| 2604 | `ShadewoodClock` | 暗影木时钟 | 104 | — | ·/·/· | — |
| 2605 | `SpookyClock` | 阴森时钟 | 104 | — | ·/·/· | — |
| 2606 | `SkywareClock` | 天域时钟 | 104 | — | ·/·/· | — |
| 2607 | `SpiderFang` | 蜘蛛牙 | — | — | ·/·/· | — |
| 2608 | `FalconBlade` | 猎鹰刃 | — | — | ·/·/· | 25 |
| 2609 | `FishronWings` | 猪龙鱼之翼 | — | — | ·/·/· | — |
| 2610 | `SlimeGun` | 史莱姆枪 | — | — | ·/·/· | — |
| 2611 | `Flairon` | 猪鲨链球 | — | — | ·/·/· | 66 |
| 2612 | `GreenDungeonChest` | 绿地牢箱 | 21 | — | ·/·/· | — |
| 2613 | `PinkDungeonChest` | 粉地牢箱 | 21 | — | ·/·/· | — |
| 2614 | `BlueDungeonChest` | 蓝地牢箱 | 21 | — | ·/·/· | — |
| 2615 | `BoneChest` | 骨箱 | 21 | — | ·/·/· | — |
| 2616 | `CactusChest` | 仙人掌箱 | 21 | — | ·/·/· | — |
| 2617 | `FleshChest` | 血肉箱 | 21 | — | ·/·/· | — |
| 2618 | `ObsidianChest` | 黑曜石箱 | 21 | — | ·/·/· | — |
| 2619 | `PumpkinChest` | 南瓜箱 | 21 | — | ·/·/· | — |
| 2620 | `SpookyChest` | 阴森箱 | 21 | — | ·/·/· | — |
| 2621 | `TempestStaff` | 暴风雨法杖 | — | — | ·/·/· | 50 |
| 2622 | `RazorbladeTyphoon` | 利刃台风 | — | — | ·/·/· | 85 |
| 2623 | `BubbleGun` | 泡泡枪 | — | — | ·/·/· | 70 |
| 2624 | `Tsunami` | 海啸 | — | — | ·/·/· | 53 |
| 2625 | `Seashell` | 贝壳 | 324 | — | ·/·/· | — |
| 2626 | `Starfish` | 海星 | 324 | — | ·/·/· | — |
| 2627 | `SteampunkPlatform` | 蒸汽朋克平台 | 19 | — | ·/·/· | — |
| 2628 | `SkywarePlatform` | 天域平台 | 19 | — | ·/·/· | — |
| 2629 | `LivingWoodPlatform` | 生命木平台 | 19 | — | ·/·/· | — |
| 2630 | `HoneyPlatform` | 蜂蜜平台 | 19 | — | ·/·/· | — |
| 2631 | `SkywareWorkbench` | 天域工作台 | 18 | — | ·/·/· | — |
| 2632 | `GlassWorkBench` | 玻璃工作台 | 18 | — | ·/·/· | — |
| 2633 | `LivingWoodWorkBench` | 生命木工作台 | 18 | — | ·/·/· | — |
| 2634 | `FleshSofa` | 血肉沙发 | 89 | — | ·/·/· | — |
| 2635 | `FrozenSofa` | 冰冻沙发 | 89 | — | ·/·/· | — |
| 2636 | `LivingWoodSofa` | 生命木沙发 | 89 | — | ·/·/· | — |
| 2637 | `PumpkinDresser` | 南瓜梳妆台 | 88 | — | ·/·/· | — |
| 2638 | `SteampunkDresser` | 蒸汽朋克梳妆台 | 88 | — | ·/·/· | — |
| 2639 | `GlassDresser` | 玻璃梳妆台 | 88 | — | ·/·/· | — |
| 2640 | `FleshDresser` | 血肉梳妆台 | 88 | — | ·/·/· | — |
| 2641 | `PumpkinLantern` | 南瓜灯笼 | 42 | — | ·/·/· | — |
| 2642 | `ObsidianLantern` | 黑曜石灯笼 | 42 | — | ·/·/· | — |
| 2643 | `PumpkinLamp` | 南瓜灯 | 93 | — | ·/·/· | — |
| 2644 | `ObsidianLamp` | 黑曜石灯 | 93 | — | ·/·/· | — |
| 2645 | `BlueDungeonLamp` | 蓝地牢灯 | 93 | — | ·/·/· | — |
| 2646 | `GreenDungeonLamp` | 绿地牢灯 | 93 | — | ·/·/· | — |
| 2647 | `PinkDungeonLamp` | 粉地牢灯 | 93 | — | ·/·/· | — |
| 2648 | `HoneyCandle` | 蜂蜜蜡烛 | 33 | — | ·/·/· | — |
| 2649 | `SteampunkCandle` | 蒸汽朋克蜡烛 | 33 | — | ·/·/· | — |
| 2650 | `SpookyCandle` | 阴森蜡烛 | 33 | — | ·/·/· | — |
| 2651 | `ObsidianCandle` | 黑曜石蜡烛 | 33 | — | ·/·/· | — |
| 2652 | `BlueDungeonChandelier` | 蓝地牢吊灯 | 34 | — | ·/·/· | — |
| 2653 | `GreenDungeonChandelier` | 绿地牢吊灯 | 34 | — | ·/·/· | — |
| 2654 | `PinkDungeonChandelier` | 粉地牢吊灯 | 34 | — | ·/·/· | — |
| 2655 | `SteampunkChandelier` | 蒸汽朋克吊灯 | 34 | — | ·/·/· | — |
| 2656 | `PumpkinChandelier` | 南瓜吊灯 | 34 | — | ·/·/· | — |
| 2657 | `ObsidianChandelier` | 黑曜石吊灯 | 34 | — | ·/·/· | — |
| 2658 | `BlueDungeonBathtub` | 蓝地牢浴缸 | 90 | — | ·/·/· | — |
| 2659 | `GreenDungeonBathtub` | 绿地牢浴缸 | 90 | — | ·/·/· | — |
| 2660 | `PinkDungeonBathtub` | 粉地牢浴缸 | 90 | — | ·/·/· | — |
| 2661 | `PumpkinBathtub` | 南瓜浴缸 | 90 | — | ·/·/· | — |
| 2662 | `ObsidianBathtub` | 黑曜石浴缸 | 90 | — | ·/·/· | — |
| 2663 | `GoldenBathtub` | 金浴缸 | 90 | — | ·/·/· | — |
| 2664 | `BlueDungeonCandelabra` | 蓝地牢烛台 | 100 | — | ·/·/· | — |
| 2665 | `GreenDungeonCandelabra` | 绿地牢烛台 | 100 | — | ·/·/· | — |
| 2666 | `PinkDungeonCandelabra` | 粉地牢烛台 | 100 | — | ·/·/· | — |
| 2667 | `ObsidianCandelabra` | 黑曜石烛台 | 100 | — | ·/·/· | — |
| 2668 | `PumpkinCandelabra` | 南瓜烛台 | 100 | — | ·/·/· | — |
| 2669 | `PumpkinBed` | 南瓜床 | 79 | — | ·/·/· | — |
| 2670 | `PumpkinBookcase` | 南瓜书架 | 101 | — | ·/·/· | — |
| 2671 | `PumpkinPiano` | 南瓜钢琴 | 87 | — | ·/·/· | — |
| 2672 | `SharkStatue` | 鲨鱼雕像 | 105 | — | ·/·/· | — |
| 2673 | `TruffleWorm` | 松露虫 | — | — | ·/·/· | — |
| 2674 | `ApprenticeBait` | 学徒诱饵 | — | — | ·/·/· | — |
| 2675 | `JourneymanBait` | 熟手诱饵 | — | — | ·/·/· | — |
| 2676 | `MasterBait` | 大师诱饵 | — | — | ·/·/· | — |
| 2677 | `AmberGemsparkWall` | 琥珀晶莹宝石墙 | — | 153 | ·/·/· | — |
| 2678 | `AmberGemsparkWallOff` | 黯淡琥珀晶莹宝石墙 | — | 157 | ·/·/· | — |
| 2679 | `AmethystGemsparkWall` | 紫晶晶莹宝石墙 | — | 154 | ·/·/· | — |
| 2680 | `AmethystGemsparkWallOff` | 黯淡紫晶晶莹宝石墙 | — | 158 | ·/·/· | — |
| 2681 | `DiamondGemsparkWall` | 钻石晶莹宝石墙 | — | 155 | ·/·/· | — |
| 2682 | `DiamondGemsparkWallOff` | 黯淡钻石晶莹宝石墙 | — | 159 | ·/·/· | — |
| 2683 | `EmeraldGemsparkWall` | 翡翠晶莹宝石墙 | — | 156 | ·/·/· | — |
| 2684 | `EmeraldGemsparkWallOff` | 黯淡翡翠晶莹宝石墙 | — | 160 | ·/·/· | — |
| 2685 | `RubyGemsparkWall` | 红玉晶莹宝石墙 | — | 164 | ·/·/· | — |
| 2686 | `RubyGemsparkWallOff` | 黯淡红玉晶莹宝石墙 | — | 161 | ·/·/· | — |
| 2687 | `SapphireGemsparkWall` | 蓝玉晶莹宝石墙 | — | 165 | ·/·/· | — |
| 2688 | `SapphireGemsparkWallOff` | 黯淡蓝玉晶莹宝石墙 | — | 162 | ·/·/· | — |
| 2689 | `TopazGemsparkWall` | 黄玉晶莹宝石墙 | — | 166 | ·/·/· | — |
| 2690 | `TopazGemsparkWallOff` | 黯淡黄玉晶莹宝石墙 | — | 163 | ·/·/· | — |
| 2691 | `TinPlatingWall` | 镀锡墙 | — | 167 | ·/·/· | — |
| 2692 | `TinPlating` | 锡镀层 | 325 | — | ·/·/· | — |
| 2693 | `WaterfallBlock` | 瀑布块 | 326 | — | ·/·/· | — |
| 2694 | `LavafallBlock` | 熔岩瀑布块 | 327 | — | ·/·/· | — |
| 2695 | `ConfettiBlock` | 彩纸块 | 328 | — | ·/·/· | — |
| 2696 | `ConfettiWall` | 彩纸墙 | — | 168 | ·/·/· | — |
| 2697 | `ConfettiBlockBlack` | 午夜彩纸块 | 329 | — | ·/·/· | — |
| 2698 | `ConfettiWallBlack` | 午夜彩纸墙 | — | 169 | ·/·/· | — |
| 2699 | `WeaponRack` | 武器架 | 471 | — | ·/·/· | — |
| 2700 | `FireworksBox` | 烟花盒 | 335 | — | ·/·/· | — |
| 2701 | `LivingFireBlock` | 活火块 | 336 | — | ·/·/· | — |
| 2702 | `AlphabetStatue0` | 0字雕像 | 337 | — | ·/·/· | — |
| 2703 | `AlphabetStatue1` | 1字雕像 | 337 | — | ·/·/· | — |
| 2704 | `AlphabetStatue2` | 2字雕像 | 337 | — | ·/·/· | — |
| 2705 | `AlphabetStatue3` | 3字雕像 | 337 | — | ·/·/· | — |
| 2706 | `AlphabetStatue4` | 4字雕像 | 337 | — | ·/·/· | — |
| 2707 | `AlphabetStatue5` | 5字雕像 | 337 | — | ·/·/· | — |
| 2708 | `AlphabetStatue6` | 6字雕像 | 337 | — | ·/·/· | — |
| 2709 | `AlphabetStatue7` | 7字雕像 | 337 | — | ·/·/· | — |
| 2710 | `AlphabetStatue8` | 8字雕像 | 337 | — | ·/·/· | — |
| 2711 | `AlphabetStatue9` | 9字雕像 | 337 | — | ·/·/· | — |
| 2712 | `AlphabetStatueA` | A字雕像 | 337 | — | ·/·/· | — |
| 2713 | `AlphabetStatueB` | B字雕像 | 337 | — | ·/·/· | — |
| 2714 | `AlphabetStatueC` | C字雕像 | 337 | — | ·/·/· | — |
| 2715 | `AlphabetStatueD` | D字雕像 | 337 | — | ·/·/· | — |
| 2716 | `AlphabetStatueE` | E字雕像 | 337 | — | ·/·/· | — |
| 2717 | `AlphabetStatueF` | F字雕像 | 337 | — | ·/·/· | — |
| 2718 | `AlphabetStatueG` | G字雕像 | 337 | — | ·/·/· | — |
| 2719 | `AlphabetStatueH` | H字雕像 | 337 | — | ·/·/· | — |
| 2720 | `AlphabetStatueI` | I字雕像 | 337 | — | ·/·/· | — |
| 2721 | `AlphabetStatueJ` | J字雕像 | 337 | — | ·/·/· | — |
| 2722 | `AlphabetStatueK` | K字雕像 | 337 | — | ·/·/· | — |
| 2723 | `AlphabetStatueL` | L字雕像 | 337 | — | ·/·/· | — |
| 2724 | `AlphabetStatueM` | M字雕像 | 337 | — | ·/·/· | — |
| 2725 | `AlphabetStatueN` | N字雕像 | 337 | — | ·/·/· | — |
| 2726 | `AlphabetStatueO` | O字雕像 | 337 | — | ·/·/· | — |
| 2727 | `AlphabetStatueP` | P字雕像 | 337 | — | ·/·/· | — |
| 2728 | `AlphabetStatueQ` | Q字雕像 | 337 | — | ·/·/· | — |
| 2729 | `AlphabetStatueR` | R字雕像 | 337 | — | ·/·/· | — |
| 2730 | `AlphabetStatueS` | S字雕像 | 337 | — | ·/·/· | — |
| 2731 | `AlphabetStatueT` | T字雕像 | 337 | — | ·/·/· | — |
| 2732 | `AlphabetStatueU` | U字雕像 | 337 | — | ·/·/· | — |
| 2733 | `AlphabetStatueV` | V字雕像 | 337 | — | ·/·/· | — |
| 2734 | `AlphabetStatueW` | W字雕像 | 337 | — | ·/·/· | — |
| 2735 | `AlphabetStatueX` | X字雕像 | 337 | — | ·/·/· | — |
| 2736 | `AlphabetStatueY` | Y字雕像 | 337 | — | ·/·/· | — |
| 2737 | `AlphabetStatueZ` | Z字雕像 | 337 | — | ·/·/· | — |
| 2738 | `FireworkFountain` | 烟花喷泉 | 338 | — | ·/·/· | — |
| 2739 | `BoosterTrack` | 增速轨道 | 314 | — | ·/·/· | — |
| 2740 | `Grasshopper` | 蚱蜢 | — | — | ·/·/· | — |
| 2741 | `GrasshopperCage` | 蚱蜢笼 | 339 | — | ·/·/· | — |
| 2742 | `MusicBoxUndergroundCrimson` | 八音盒（地下猩红之地） | 139 | — | ·/·/· | — |
| 2743 | `CactusTable` | 仙人掌桌 | 14 | — | ·/·/· | — |
| 2744 | `CactusPlatform` | 仙人掌平台 | 19 | — | ·/·/· | — |
| 2745 | `BorealWoodSword` | 针叶木剑 | — | — | ·/·/· | 8 |
| 2746 | `BorealWoodHammer` | 针叶木锤 | — | — | ·/·/35 | 4 |
| 2747 | `BorealWoodBow` | 针叶木弓 | — | — | ·/·/· | 6 |
| 2748 | `GlassChest` | 玻璃箱 | 21 | — | ·/·/· | — |
| 2749 | `XenoStaff` | 外星法杖 | — | — | ·/·/· | 36 |
| 2750 | `MeteorStaff` | 流星法杖 | — | — | ·/·/· | 50 |
| 2751 | `LivingCursedFireBlock` | 诅咒活火块 | 340 | — | ·/·/· | — |
| 2752 | `LivingDemonFireBlock` | 恶魔活火块 | 341 | — | ·/·/· | — |
| 2753 | `LivingFrostFireBlock` | 寒霜活火块 | 342 | — | ·/·/· | — |
| 2754 | `LivingIchorBlock` | 灵液活火块 | 343 | — | ·/·/· | — |
| 2755 | `LivingUltrabrightFireBlock` | 超亮活火块 | 344 | — | ·/·/· | — |
| 2756 | `GenderChangePotion` | 变性药水 | — | — | ·/·/· | — |
| 2757 | `VortexHelmet` | 星旋头盔 | — | — | ·/·/· | — |
| 2758 | `VortexBreastplate` | 星旋胸甲 | — | — | ·/·/· | — |
| 2759 | `VortexLeggings` | 星旋护腿 | — | — | ·/·/· | — |
| 2760 | `NebulaHelmet` | 星云头盔 | — | — | ·/·/· | — |
| 2761 | `NebulaBreastplate` | 星云胸甲 | — | — | ·/·/· | — |
| 2762 | `NebulaLeggings` | 星云护腿 | — | — | ·/·/· | — |
| 2763 | `SolarFlareHelmet` | 耀斑头盔 | — | — | ·/·/· | — |
| 2764 | `SolarFlareBreastplate` | 耀斑胸甲 | — | — | ·/·/· | — |
| 2765 | `SolarFlareLeggings` | 耀斑护腿 | — | — | ·/·/· | — |
| 2766 | `LunarTabletFragment` | 日耀碑牌碎片 | — | — | ·/·/· | — |
| 2767 | `SolarTablet` | 日耀碑牌 | — | — | ·/·/· | — |
| 2768 | `DrillContainmentUnit` | 钻头控制装置 | — | — | ·/·/· | — |
| 2769 | `CosmicCarKey` | 宇宙车钥匙 | — | — | ·/·/· | — |
| 2770 | `MothronWings` | 蛾怪之翼 | — | — | ·/·/· | — |
| 2771 | `BrainScrambler` | 扰脑怪 | — | — | ·/·/· | — |
| 2774 | `VortexDrill` | 星旋钻头 | — | — | 225/·/· | 50 |
| 2776 | `VortexPickaxe` | 星旋镐 | — | — | 225/·/· | 80 |
| 2779 | `NebulaDrill` | 星云钻头 | — | — | 225/·/· | 50 |
| 2781 | `NebulaPickaxe` | 星云镐 | — | — | 225/·/· | 80 |
| 2784 | `SolarFlareDrill` | 耀斑钻头 | — | — | 225/·/· | 50 |
| 2786 | `SolarFlarePickaxe` | 耀斑镐斧 | — | — | 225/·/· | 80 |
| 2787 | `HoneyfallBlock` | 蜂蜜瀑布块 | 345 | — | ·/·/· | — |
| 2788 | `HoneyfallWall` | 蜂蜜瀑布墙 | — | 172 | ·/·/· | — |
| 2789 | `ChlorophyteBrickWall` | 叶绿砖墙 | — | 173 | ·/·/· | — |
| 2790 | `CrimtaneBrickWall` | 猩红矿砖墙 | — | 174 | ·/·/· | — |
| 2791 | `ShroomitePlatingWall` | 蘑菇矿镀层墙 | — | 175 | ·/·/· | — |
| 2792 | `ChlorophyteBrick` | 叶绿砖 | 346 | — | ·/·/· | — |
| 2793 | `CrimtaneBrick` | 猩红矿砖 | 347 | — | ·/·/· | — |
| 2794 | `ShroomitePlating` | 蘑菇矿镀层 | 348 | — | ·/·/· | — |
| 2795 | `LaserMachinegun` | 激光机枪 | — | — | ·/·/· | 60 |
| 2796 | `ElectrosphereLauncher` | 电圈发射器 | — | — | ·/·/· | 40 |
| 2797 | `Xenopopper` | 外星霰弹枪 | — | — | ·/·/· | 45 |
| 2798 | `LaserDrill` | 激光钻头 | — | — | 230/·/· | 35 |
| 2799 | `LaserRuler` | 机械标尺 | — | — | ·/·/· | — |
| 2800 | `AntiGravityHook` | 反重力钩 | — | — | ·/·/· | — |
| 2801 | `MoonMask` | 月亮面具 | — | — | ·/·/· | — |
| 2802 | `SunMask` | 太阳面具 | — | — | ·/·/· | — |
| 2803 | `MartianCostumeMask` | 火星制服面具 | — | — | ·/·/· | — |
| 2804 | `MartianCostumeShirt` | 火星制服衣 | — | — | ·/·/· | — |
| 2805 | `MartianCostumePants` | 火星时装裤 | — | — | ·/·/· | — |
| 2806 | `MartianUniformHelmet` | 火星制服头盔 | — | — | ·/·/· | — |
| 2807 | `MartianUniformTorso` | 火星制服上衣 | — | — | ·/·/· | — |
| 2808 | `MartianUniformPants` | 火星制服裤 | — | — | ·/·/· | — |
| 2809 | `MartianAstroClock` | 火星占星钟 | 104 | — | ·/·/· | — |
| 2810 | `MartianBathtub` | 火星浴缸 | 90 | — | ·/·/· | — |
| 2811 | `MartianBed` | 火星床 | 79 | — | ·/·/· | — |
| 2812 | `MartianHoverChair` | 火星摇摆椅 | 15 | — | ·/·/· | — |
| 2813 | `MartianChandelier` | 火星吊灯 | 34 | — | ·/·/· | — |
| 2814 | `MartianChest` | 火星箱 | 21 | — | ·/·/· | — |
| 2815 | `MartianDoor` | 火星门 | 10 | — | ·/·/· | — |
| 2816 | `MartianDresser` | 火星梳妆台 | 88 | — | ·/·/· | — |
| 2817 | `MartianHolobookcase` | 火星整体书架 | 101 | — | ·/·/· | — |
| 2818 | `MartianHoverCandle` | 火星摇摆蜡烛 | 33 | — | ·/·/· | — |
| 2819 | `MartianLamppost` | 火星灯柱 | 93 | — | ·/·/· | — |
| 2820 | `MartianLantern` | 火星灯笼 | 42 | — | ·/·/· | — |
| 2821 | `MartianPiano` | 火星钢琴 | 87 | — | ·/·/· | — |
| 2822 | `MartianPlatform` | 火星平台 | 19 | — | ·/·/· | — |
| 2823 | `MartianSofa` | 火星沙发 | 89 | — | ·/·/· | — |
| 2824 | `MartianTable` | 火星桌 | 14 | — | ·/·/· | — |
| 2825 | `MartianTableLamp` | 火星桌灯 | 100 | — | ·/·/· | — |
| 2826 | `MartianWorkBench` | 火星工作台 | 18 | — | ·/·/· | — |
| 2827 | `WoodenSink` | 木水槽 | 172 | — | ·/·/· | — |
| 2828 | `EbonwoodSink` | 乌木水槽 | 172 | — | ·/·/· | — |
| 2829 | `RichMahoganySink` | 红木水槽 | 172 | — | ·/·/· | — |
| 2830 | `PearlwoodSink` | 珍珠木水槽 | 172 | — | ·/·/· | — |
| 2831 | `BoneSink` | 骨头水槽 | 172 | — | ·/·/· | — |
| 2832 | `FleshSink` | 血肉水槽 | 172 | — | ·/·/· | — |
| 2833 | `LivingWoodSink` | 生命木水槽 | 172 | — | ·/·/· | — |
| 2834 | `SkywareSink` | 天域水槽 | 172 | — | ·/·/· | — |
| 2835 | `ShadewoodSink` | 暗影木水槽 | 172 | — | ·/·/· | — |
| 2836 | `LihzahrdSink` | 丛林蜥蜴水槽 | 172 | — | ·/·/· | — |
| 2837 | `BlueDungeonSink` | 蓝地牢水槽 | 172 | — | ·/·/· | — |
| 2838 | `GreenDungeonSink` | 绿地牢水槽 | 172 | — | ·/·/· | — |
| 2839 | `PinkDungeonSink` | 粉地牢水槽 | 172 | — | ·/·/· | — |
| 2840 | `ObsidianSink` | 黑曜石水槽 | 172 | — | ·/·/· | — |
| 2841 | `MetalSink` | 金属水槽 | 172 | — | ·/·/· | — |
| 2842 | `GlassSink` | 玻璃水槽 | 172 | — | ·/·/· | — |
| 2843 | `GoldenSink` | 金水槽 | 172 | — | ·/·/· | — |
| 2844 | `HoneySink` | 蜂蜜水槽 | 172 | — | ·/·/· | — |
| 2845 | `SteampunkSink` | 蒸汽朋克水槽 | 172 | — | ·/·/· | — |
| 2846 | `PumpkinSink` | 南瓜水槽 | 172 | — | ·/·/· | — |
| 2847 | `SpookySink` | 阴森水槽 | 172 | — | ·/·/· | — |
| 2848 | `FrozenSink` | 冰冻水槽 | 172 | — | ·/·/· | — |
| 2849 | `DynastySink` | 王朝水槽 | 172 | — | ·/·/· | — |
| 2850 | `PalmWoodSink` | 棕榈木水槽 | 172 | — | ·/·/· | — |
| 2851 | `MushroomSink` | 蘑菇水槽 | 172 | — | ·/·/· | — |
| 2852 | `BorealWoodSink` | 针叶木水槽 | 172 | — | ·/·/· | — |
| 2853 | `SlimeSink` | 史莱姆水槽 | 172 | — | ·/·/· | — |
| 2854 | `CactusSink` | 仙人掌水槽 | 172 | — | ·/·/· | — |
| 2855 | `MartianSink` | 火星水槽 | 172 | — | ·/·/· | — |
| 2856 | `WhiteLunaticHood` | 日耀邪教徒兜帽 | — | — | ·/·/· | — |
| 2857 | `BlueLunaticHood` | 月亮邪教徒兜帽 | — | — | ·/·/· | — |
| 2858 | `WhiteLunaticRobe` | 日耀邪教徒长袍 | — | — | ·/·/· | — |
| 2859 | `BlueLunaticRobe` | 月亮邪教徒长袍 | — | — | ·/·/· | — |
| 2860 | `MartianConduitPlating` | 火星管道镀层 | 350 | — | ·/·/· | — |
| 2861 | `MartianConduitWall` | 火星管道墙 | — | 176 | ·/·/· | — |
| 2862 | `HiTekSunglasses` | 高科技墨镜 | — | — | ·/·/· | — |
| 2863 | `MartianHairDye` | 火星染发剂 | — | — | ·/·/· | — |
| 2864 | `MartianArmorDye` | 火星染料 | — | — | ·/·/· | — |
| 2865 | `PaintingCastleMarsberg` | 火星贝格城堡 | 242 | — | ·/·/· | — |
| 2866 | `PaintingMartiaLisa` | 火娜丽莎 | 242 | — | ·/·/· | — |
| 2867 | `PaintingTheTruthIsUpThere` | 真理就在火星 | 242 | — | ·/·/· | — |
| 2868 | `SmokeBlock` | 烟雾块 | 351 | — | ·/·/· | — |
| 2869 | `LivingFlameDye` | 鲜艳红焰染料 | — | — | ·/·/· | — |
| 2870 | `LivingRainbowDye` | 鲜艳彩虹染料 | — | — | ·/·/· | — |
| 2871 | `ShadowDye` | 暗影染料 | — | — | ·/·/· | — |
| 2872 | `NegativeDye` | 阴暗染料 | — | — | ·/·/· | — |
| 2873 | `LivingOceanDye` | 鲜艳海蓝染料 | — | — | ·/·/· | — |
| 2874 | `BrownDye` | 棕染料 | — | — | ·/·/· | — |
| 2875 | `BrownAndBlackDye` | 棕黑染料 | — | — | ·/·/· | — |
| 2876 | `BrightBrownDye` | 蓝棕染料 | — | — | ·/·/· | — |
| 2877 | `BrownAndSilverDye` | 棕银染料 | — | — | ·/·/· | — |
| 2878 | `WispDye` | 妖灵染料 | — | — | ·/·/· | — |
| 2879 | `PixieDye` | 妖精染料 | — | — | ·/·/· | — |
| 2880 | `InfluxWaver` | 波涌之刃 | — | — | ·/·/· | 100 |
| 2882 | `ChargedBlasterCannon` | 带电爆破炮 | — | — | ·/·/· | 100 |
| 2883 | `ChlorophyteDye` | 叶绿染料 | — | — | ·/·/· | — |
| 2884 | `UnicornWispDye` | 独角妖灵染料 | — | — | ·/·/· | — |
| 2885 | `InfernalWispDye` | 地狱妖灵染料 | — | — | ·/·/· | — |
| 2886 | `ViciousPowder` | 毒粉 | — | — | ·/·/· | — |
| 2887 | `ViciousMushroom` | 毒蘑菇 | — | — | ·/·/· | — |
| 2888 | `BeesKnees` | 蜂膝弓 | — | — | ·/·/· | 23 |
| 2889 | `GoldBird` | 金鸟 | — | — | ·/·/· | — |
| 2890 | `GoldBunny` | 金兔 | — | — | ·/·/· | — |
| 2891 | `GoldButterfly` | 金蝴蝶 | — | — | ·/·/· | — |
| 2892 | `GoldFrog` | 金蛙 | — | — | ·/·/· | — |
| 2893 | `GoldGrasshopper` | 金蚱蜢 | — | — | ·/·/· | — |
| 2894 | `GoldMouse` | 金老鼠 | — | — | ·/·/· | — |
| 2895 | `GoldWorm` | 金蠕虫 | — | — | ·/·/· | — |
| 2896 | `StickyDynamite` | 粘性雷管 | — | — | ·/·/· | — |
| 2897 | `AngryTrapperBanner` | 愤怒捕手旗 | 91 | — | ·/·/· | — |
| 2898 | `ArmoredVikingBanner` | 装甲维京海盗旗 | 91 | — | ·/·/· | — |
| 2899 | `BlackSlimeBanner` | 黑史莱姆旗 | 91 | — | ·/·/· | — |
| 2900 | `BlueArmoredBonesBanner` | 蓝装甲骷髅旗 | 91 | — | ·/·/· | — |
| 2901 | `BlueCultistArcherBanner` | 蓝邪教徒弓箭手旗 | 91 | — | ·/·/· | — |
| 2902 | `BlueCultistCasterBanner` | 拜月教忠教徒旗 | 91 | — | ·/·/· | — |
| 2903 | `BlueCultistFighterBanner` | 蓝邪教徒战士旗 | 91 | — | ·/·/· | — |
| 2904 | `BoneLeeBanner` | 骷髅李小龙旗 | 91 | — | ·/·/· | — |
| 2905 | `ClingerBanner` | 爬藤怪旗 | 91 | — | ·/·/· | — |
| 2906 | `CochinealBeetleBanner` | 胭脂虫旗 | 91 | — | ·/·/· | — |
| 2907 | `CorruptPenguinBanner` | 腐化企鹅旗 | 91 | — | ·/·/· | — |
| 2908 | `CorruptSlimeBanner` | 腐化史莱姆旗 | 91 | — | ·/·/· | — |
| 2909 | `CorruptorBanner` | 腐化者旗 | 91 | — | ·/·/· | — |
| 2910 | `CrimslimeBanner` | 猩红史莱姆旗 | 91 | — | ·/·/· | — |
| 2911 | `CursedSkullBanner` | 诅咒骷髅头旗 | 91 | — | ·/·/· | — |
| 2912 | `CyanBeetleBanner` | 青壳虫旗 | 91 | — | ·/·/· | — |
| 2913 | `DevourerBanner` | 吞噬怪旗 | 91 | — | ·/·/· | — |
| 2914 | `DiablolistBanner` | 魔教徒旗 | 91 | — | ·/·/· | — |
| 2915 | `DoctorBonesBanner` | 骷髅博士旗 | 91 | — | ·/·/· | — |
| 2916 | `DungeonSlimeBanner` | 地牢史莱姆旗 | 91 | — | ·/·/· | — |
| 2917 | `DungeonSpiritBanner` | 地牢幽魂旗 | 91 | — | ·/·/· | — |
| 2918 | `ElfArcherBanner` | 精灵弓箭手旗 | 91 | — | ·/·/· | — |
| 2919 | `ElfCopterBanner` | 精灵直升机旗 | 91 | — | ·/·/· | — |
| 2920 | `EyezorBanner` | 眼怪旗 | 91 | — | ·/·/· | — |
| 2921 | `FlockoBanner` | 雪花怪旗 | 91 | — | ·/·/· | — |
| 2922 | `GhostBanner` | 鬼魂旗 | 91 | — | ·/·/· | — |
| 2923 | `GiantBatBanner` | 巨型蝙蝠旗 | 91 | — | ·/·/· | — |
| 2924 | `GiantCursedSkullBanner` | 巨型诅咒骷髅头旗 | 91 | — | ·/·/· | — |
| 2925 | `GiantFlyingFoxBanner` | 巨型飞狐旗 | 91 | — | ·/·/· | — |
| 2926 | `GingerbreadManBanner` | 姜饼人旗 | 91 | — | ·/·/· | — |
| 2927 | `GoblinArcherBanner` | 哥布林弓箭手旗 | 91 | — | ·/·/· | — |
| 2928 | `GreenSlimeBanner` | 绿史莱姆旗 | 91 | — | ·/·/· | — |
| 2929 | `HeadlessHorsemanBanner` | 无头骑士旗 | 91 | — | ·/·/· | — |
| 2930 | `HellArmoredBonesBanner` | 地狱装甲骷髅旗 | 91 | — | ·/·/· | — |
| 2931 | `HellhoundBanner` | 地狱犬旗 | 91 | — | ·/·/· | — |
| 2932 | `HoppinJackBanner` | 弹跳杰克南瓜灯旗 | 91 | — | ·/·/· | — |
| 2933 | `IceBatBanner` | 冰雪蝙蝠旗 | 91 | — | ·/·/· | — |
| 2934 | `IceGolemBanner` | 冰雪巨人旗 | 91 | — | ·/·/· | — |
| 2935 | `IceSlimeBanner` | 冰雪史莱姆旗 | 91 | — | ·/·/· | — |
| 2936 | `IchorStickerBanner` | 灵液黏黏怪旗 | 91 | — | ·/·/· | — |
| 2937 | `IlluminantBatBanner` | 夜明蝙蝠旗 | 91 | — | ·/·/· | — |
| 2938 | `IlluminantSlimeBanner` | 夜明史莱姆旗 | 91 | — | ·/·/· | — |
| 2939 | `JungleBatBanner` | 丛林蝙蝠旗 | 91 | — | ·/·/· | — |
| 2940 | `JungleSlimeBanner` | 丛林史莱姆旗 | 91 | — | ·/·/· | — |
| 2941 | `KrampusBanner` | 坎卜斯旗 | 91 | — | ·/·/· | — |
| 2942 | `LacBeetleBanner` | 紫胶虫旗 | 91 | — | ·/·/· | — |
| 2943 | `LavaBatBanner` | 熔岩蝙蝠旗 | 91 | — | ·/·/· | — |
| 2944 | `LavaSlimeBanner` | 熔岩史莱姆旗 | 91 | — | ·/·/· | — |
| 2945 | `MartianBrainscramblerBanner` | 火星扰脑怪旗 | 91 | — | ·/·/· | — |
| 2946 | `MartianDroneBanner` | 火星飞船旗 | 91 | — | ·/·/· | — |
| 2947 | `MartianEngineerBanner` | 火星工程师旗 | 91 | — | ·/·/· | — |
| 2948 | `MartianGigazapperBanner` | 火星电击怪旗 | 91 | — | ·/·/· | — |
| 2949 | `MartianGreyGruntBanner` | 火星灰咕噜兽旗 | 91 | — | ·/·/· | — |
| 2950 | `MartianOfficerBanner` | 火星军官旗 | 91 | — | ·/·/· | — |
| 2951 | `MartianRaygunnerBanner` | 火星激光枪手旗 | 91 | — | ·/·/· | — |
| 2952 | `MartianScutlixGunnerBanner` | 火星鳞甲怪枪手旗 | 91 | — | ·/·/· | — |
| 2953 | `MartianTeslaTurretBanner` | 火星特斯拉炮塔旗 | 91 | — | ·/·/· | — |
| 2954 | `MisterStabbyBanner` | 戳刺先生旗 | 91 | — | ·/·/· | — |
| 2955 | `MotherSlimeBanner` | 史莱姆之母旗 | 91 | — | ·/·/· | — |
| 2956 | `NecromancerBanner` | 死灵法师旗 | 91 | — | ·/·/· | — |
| 2957 | `NutcrackerBanner` | 胡桃夹士旗 | 91 | — | ·/·/· | — |
| 2958 | `PaladinBanner` | 圣骑士旗 | 91 | — | ·/·/· | — |
| 2959 | `PenguinBanner` | 企鹅旗 | 91 | — | ·/·/· | — |
| 2960 | `PinkyBanner` | 粉史莱姆旗 | 91 | — | ·/·/· | — |
| 2961 | `PoltergeistBanner` | 胡闹鬼旗 | 91 | — | ·/·/· | — |
| 2962 | `PossessedArmorBanner` | 装甲幻影魔旗 | 91 | — | ·/·/· | — |
| 2963 | `PresentMimicBanner` | 礼物宝箱怪旗 | 91 | — | ·/·/· | — |
| 2964 | `PurpleSlimeBanner` | 紫史莱姆旗 | 91 | — | ·/·/· | — |
| 2965 | `RaggedCasterBanner` | 褴褛邪教徒法师旗 | 91 | — | ·/·/· | — |
| 2966 | `RainbowSlimeBanner` | 彩虹史莱姆旗 | 91 | — | ·/·/· | — |
| 2967 | `RavenBanner` | 乌鸦旗 | 91 | — | ·/·/· | — |
| 2968 | `RedSlimeBanner` | 红史莱姆旗 | 91 | — | ·/·/· | — |
| 2969 | `RuneWizardBanner` | 符文巫师旗 | 91 | — | ·/·/· | — |
| 2970 | `RustyArmoredBonesBanner` | 生锈装甲骷髅旗 | 91 | — | ·/·/· | — |
| 2971 | `ScarecrowBanner` | 稻草人旗 | 91 | — | ·/·/· | — |
| 2972 | `ScutlixBanner` | 鳞甲怪旗 | 91 | — | ·/·/· | — |
| 2973 | `SkeletonArcherBanner` | 骷髅弓箭手旗 | 91 | — | ·/·/· | — |
| 2974 | `SkeletonCommandoBanner` | 骷髅突击手旗 | 91 | — | ·/·/· | — |
| 2975 | `SkeletonSniperBanner` | 骷髅狙击手旗 | 91 | — | ·/·/· | — |
| 2976 | `SlimerBanner` | 恶翅史莱姆旗 | 91 | — | ·/·/· | — |
| 2977 | `SnatcherBanner` | 抓人草旗 | 91 | — | ·/·/· | — |
| 2978 | `SnowBallaBanner` | 巴拉雪人旗 | 91 | — | ·/·/· | — |
| 2979 | `SnowmanGangstaBanner` | 雪人暴徒旗 | 91 | — | ·/·/· | — |
| 2980 | `SpikedIceSlimeBanner` | 尖刺冰雪史莱姆旗 | 91 | — | ·/·/· | — |
| 2981 | `SpikedJungleSlimeBanner` | 尖刺丛林史莱姆旗 | 91 | — | ·/·/· | — |
| 2982 | `SplinterlingBanner` | 树精旗 | 91 | — | ·/·/· | — |
| 2983 | `SquidBanner` | 乌贼旗 | 91 | — | ·/·/· | — |
| 2984 | `TacticalSkeletonBanner` | 骷髅特警旗 | 91 | — | ·/·/· | — |
| 2985 | `TheGroomBanner` | 僵尸新郎旗 | 91 | — | ·/·/· | — |
| 2986 | `TimBanner` | 蒂姆旗 | 91 | — | ·/·/· | — |
| 2987 | `UndeadMinerBanner` | 不死矿工旗 | 91 | — | ·/·/· | — |
| 2988 | `UndeadVikingBanner` | 亡灵维京海盗旗 | 91 | — | ·/·/· | — |
| 2989 | `WhiteCultistArcherBanner` | 白邪教徒弓箭手旗 | 91 | — | ·/·/· | — |
| 2990 | `WhiteCultistCasterBanner` | 白邪教徒法师旗 | 91 | — | ·/·/· | — |
| 2991 | `WhiteCultistFighterBanner` | 白邪教徒战士旗 | 91 | — | ·/·/· | — |
| 2992 | `YellowSlimeBanner` | 黄史莱姆旗 | 91 | — | ·/·/· | — |
| 2993 | `YetiBanner` | 雪兽旗 | 91 | — | ·/·/· | — |
| 2994 | `ZombieElfBanner` | 僵尸精灵旗 | 91 | — | ·/·/· | — |
| 2995 | `SparkyPainting` | 斯派基 | 242 | — | ·/·/· | — |
| 2996 | `VineRope` | 藤蔓绳 | 353 | — | ·/·/· | — |
| 2997 | `WormholePotion` | 虫洞药水 | — | — | ·/·/· | — |
| 2998 | `SummonerEmblem` | 召唤师徽章 | — | — | ·/·/· | — |
| 2999 | `BewitchingTable` | 施法桌 | 354 | — | ·/·/· | — |
| 3000 | `AlchemyTable` | 炼药桌 | 355 | — | ·/·/· | — |
| 3001 | `StrangeBrew` | 诡药 | — | — | ·/·/· | — |
| 3002 | `SpelunkerGlowstick` | 洞穴探险荧光棒 | — | — | ·/·/· | — |
| 3003 | `BoneArrow` | 骨箭 | — | — | ·/·/· | 8 |
| 3004 | `BoneTorch` | 骨头火把 | 4 | — | ·/·/· | — |
| 3005 | `VineRopeCoil` | 藤蔓绳圈 | — | — | ·/·/· | — |
| 3006 | `SoulDrain` | 夺命杖 | — | — | ·/·/· | 35 |
| 3007 | `DartPistol` | 飞镖手枪 | — | — | ·/·/· | 28 |
| 3008 | `DartRifle` | 飞镖步枪 | — | — | ·/·/· | 52 |
| 3009 | `CrystalDart` | 水晶镖 | — | — | ·/·/· | 14 |
| 3010 | `CursedDart` | 诅咒镖 | — | — | ·/·/· | 9 |
| 3011 | `IchorDart` | 灵液镖 | — | — | ·/·/· | 10 |
| 3012 | `ChainGuillotines` | 铁链血滴子 | — | — | ·/·/· | 59 |
| 3013 | `FetidBaghnakhs` | 臭虎爪 | — | — | ·/·/· | 60 |
| 3014 | `ClingerStaff` | 爬藤怪法杖 | — | — | ·/·/· | 43 |
| 3015 | `PutridScent` | 腐香囊 | — | — | ·/·/· | — |
| 3016 | `FleshKnuckles` | 血肉指关节 | — | — | ·/·/· | — |
| 3017 | `FlowerBoots` | 花靴 | — | — | ·/·/· | — |
| 3018 | `Seedler` | 种子弯刀 | — | — | ·/·/· | 50 |
| 3019 | `HellwingBow` | 地狱之翼弓 | — | — | ·/·/· | 22 |
| 3020 | `TendonHook` | 肌腱钩 | — | — | ·/·/· | — |
| 3021 | `ThornHook` | 刺钩 | — | — | ·/·/· | — |
| 3022 | `IlluminantHook` | 荧光钩 | — | — | ·/·/· | — |
| 3023 | `WormHook` | 蠕虫钩 | — | — | ·/·/· | — |
| 3024 | `DevDye` | Skiphs的血 | — | — | ·/·/· | — |
| 3025 | `PurpleOozeDye` | 紫泥染料 | — | — | ·/·/· | — |
| 3026 | `ReflectiveSilverDye` | 反光银染料 | — | — | ·/·/· | — |
| 3027 | `ReflectiveGoldDye` | 反光金染料 | — | — | ·/·/· | — |
| 3028 | `BlueAcidDye` | 蓝酸性染料 | — | — | ·/·/· | — |
| 3029 | `DaedalusStormbow` | 代达罗斯风暴弓 | — | — | ·/·/· | 38 |
| 3030 | `FlyingKnife` | 飞刀 | — | — | ·/·/· | 40 |
| 3031 | `BottomlessBucket` | 无底水桶 | — | — | ·/·/· | — |
| 3032 | `SuperAbsorbantSponge` | 超级吸水棉 | — | — | ·/·/· | — |
| 3033 | `GoldRing` | 金戒指 | — | — | ·/·/· | — |
| 3034 | `CoinRing` | 钱币戒指 | — | — | ·/·/· | — |
| 3035 | `GreedyRing` | 贪婪戒指 | — | — | ·/·/· | — |
| 3036 | `FishFinder` | 探鱼器 | — | — | ·/·/· | — |
| 3037 | `WeatherRadio` | 天气收音机 | — | — | ·/·/· | — |
| 3038 | `HadesDye` | 冥王染料 | — | — | ·/·/· | — |
| 3039 | `TwilightDye` | 暮光染料 | — | — | ·/·/· | — |
| 3040 | `AcidDye` | 酸性染料 | — | — | ·/·/· | — |
| 3041 | `MushroomDye` | 发光蘑菇染料 | — | — | ·/·/· | — |
| 3042 | `PhaseDye` | 闪光染料 | — | — | ·/·/· | — |
| 3043 | `MagicLantern` | 魔法灯笼 | — | — | ·/·/· | — |
| 3044 | `MusicBoxLunarBoss` | 八音盒（月亮Boss） | 139 | — | ·/·/· | — |
| 3045 | `RainbowTorch` | 彩虹火把 | 4 | — | ·/·/· | — |
| 3046 | `CursedCampfire` | 诅咒篝火 | 215 | — | ·/·/· | — |
| 3047 | `DemonCampfire` | 恶魔篝火 | 215 | — | ·/·/· | — |
| 3048 | `FrozenCampfire` | 冰冻篝火 | 215 | — | ·/·/· | — |
| 3049 | `IchorCampfire` | 灵液篝火 | 215 | — | ·/·/· | — |
| 3050 | `RainbowCampfire` | 彩虹篝火 | 215 | — | ·/·/· | — |
| 3051 | `CrystalVileShard` | 魔晶碎块 | — | — | ·/·/· | 25 |
| 3052 | `ShadowFlameBow` | 暗影焰弓 | — | — | ·/·/· | 47 |
| 3053 | `ShadowFlameHexDoll` | 暗影焰妖娃 | — | — | ·/·/· | 32 |
| 3054 | `ShadowFlameKnife` | 暗影焰刀 | — | — | ·/·/· | 38 |
| 3055 | `PaintingAcorns` | 橡果 | 242 | — | ·/·/· | — |
| 3056 | `PaintingColdSnap` | 寒冰英姿 | 242 | — | ·/·/· | — |
| 3057 | `PaintingCursedSaint` | 诅咒的圣诞骷髅王 | 242 | — | ·/·/· | — |
| 3058 | `PaintingSnowfellas` | 吉祥三雪宝 | 242 | — | ·/·/· | — |
| 3059 | `PaintingTheSeason` | 圣诞雪季 | 242 | — | ·/·/· | — |
| 3060 | `BoneRattle` | 骨头宝 | — | — | ·/·/· | — |
| 3061 | `ArchitectGizmoPack` | 建筑师发明背包 | — | — | ·/·/· | — |
| 3062 | `CrimsonHeart` | 猩红之心 | — | — | ·/·/· | — |
| 3063 | `Meowmere` | 彩虹猫之刃 | — | — | ·/·/· | 200 |
| 3064 | `Sundial` | 附魔日晷 | 356 | — | ·/·/· | — |
| 3065 | `StarWrath` | 狂星之怒 | — | — | ·/·/· | 170 |
| 3066 | `MarbleBlock` | 光面大理石块 | 357 | — | ·/·/· | — |
| 3067 | `HellstoneBrickWall` | 狱石砖墙 | — | 177 | ·/·/· | — |
| 3068 | `CordageGuide` | 植物纤维绳索宝典 | — | — | ·/·/· | — |
| 3069 | `WandofSparking` | 火花魔棒 | — | — | ·/·/· | 14 |
| 3070 | `GoldBirdCage` | 金鸟笼 | 358 | — | ·/·/· | — |
| 3071 | `GoldBunnyCage` | 金兔笼 | 359 | — | ·/·/· | — |
| 3072 | `GoldButterflyCage` | 金蝴蝶罐 | 360 | — | ·/·/· | — |
| 3073 | `GoldFrogCage` | 金蛙笼 | 361 | — | ·/·/· | — |
| 3074 | `GoldGrasshopperCage` | 金蚱蜢笼 | 362 | — | ·/·/· | — |
| 3075 | `GoldMouseCage` | 金老鼠笼 | 363 | — | ·/·/· | — |
| 3076 | `GoldWormCage` | 金蠕虫笼 | 364 | — | ·/·/· | — |
| 3077 | `SilkRope` | 丝绸绳 | 365 | — | ·/·/· | — |
| 3078 | `WebRope` | 蛛丝绳 | 366 | — | ·/·/· | — |
| 3079 | `SilkRopeCoil` | 丝绸绳圈 | — | — | ·/·/· | — |
| 3080 | `WebRopeCoil` | 蛛丝绳圈 | — | — | ·/·/· | — |
| 3081 | `Marble` | 大理石块 | 367 | — | ·/·/· | — |
| 3082 | `MarbleWall` | 大理石墙 | — | 183 | ·/·/· | — |
| 3083 | `MarbleBlockWall` | 光面大理石墙 | — | 179 | ·/·/· | — |
| 3084 | `Radar` | 雷达 | — | — | ·/·/· | — |
| 3085 | `LockBox` | 金锁盒 | — | — | ·/·/· | — |
| 3086 | `Granite` | 花岗岩块 | 368 | — | ·/·/· | — |
| 3087 | `GraniteBlock` | 光面花岗岩块 | 369 | — | ·/·/· | — |
| 3088 | `GraniteWall` | 花岗岩墙 | — | 184 | ·/·/· | — |
| 3089 | `GraniteBlockWall` | 光面花岗岩墙 | — | 181 | ·/·/· | — |
| 3090 | `RoyalGel` | 皇家凝胶 | — | — | ·/·/· | — |
| 3091 | `NightKey` | 夜光钥匙 | — | — | ·/·/· | — |
| 3092 | `LightKey` | 光明钥匙 | — | — | ·/·/· | — |
| 3093 | `HerbBag` | 草药袋 | — | — | ·/·/· | — |
| 3094 | `Javelin` | 标枪 | — | — | ·/·/· | 17 |
| 3095 | `TallyCounter` | 杀怪计数器 | — | — | ·/·/· | — |
| 3096 | `Sextant` | 六分仪 | — | — | ·/·/· | — |
| 3097 | `EoCShield` | 克苏鲁护盾 | — | — | ·/·/· | 30 |
| 3098 | `ButchersChainsaw` | 屠夫链锯 | — | — | ·/30/· | 120 |
| 3099 | `Stopwatch` | 秒表 | — | — | ·/·/· | — |
| 3100 | `MeteoriteBrick` | 陨石砖 | 370 | — | ·/·/· | — |
| 3101 | `MeteoriteBrickWall` | 陨石砖墙 | — | 182 | ·/·/· | — |
| 3102 | `MetalDetector` | 金属探测器 | — | — | ·/·/· | — |
| 3103 | `EndlessQuiver` | 无尽箭袋 | — | — | ·/·/· | 5 |
| 3104 | `EndlessMusketPouch` | 无尽火枪袋 | — | — | ·/·/· | 7 |
| 3105 | `ToxicFlask` | 毒气瓶 | — | — | ·/·/· | 52 |
| 3106 | `PsychoKnife` | 变态刀 | — | — | ·/·/· | 85 |
| 3107 | `NailGun` | 钉枪 | — | — | ·/·/· | 85 |
| 3108 | `Nail` | 钉子 | — | — | ·/·/· | 30 |
| 3109 | `NightVisionHelmet` | 夜视头盔 | — | — | ·/·/· | — |
| 3110 | `CelestialShell` | 天界壳 | — | — | ·/·/· | — |
| 3111 | `PinkGel` | 粉凝胶 | — | — | ·/·/· | — |
| 3112 | `BouncyGlowstick` | 弹力荧光棒 | — | — | ·/·/· | — |
| 3113 | `PinkSlimeBlock` | 粉史莱姆块 | 371 | — | ·/·/· | — |
| 3114 | `PinkTorch` | 粉火把 | 4 | — | ·/·/· | — |
| 3115 | `BouncyBomb` | 弹力炸弹 | — | — | ·/·/· | — |
| 3116 | `BouncyGrenade` | 弹力手榴弹 | — | — | ·/·/· | 65 |
| 3117 | `PeaceCandle` | 和平蜡烛 | 372 | — | ·/·/· | — |
| 3118 | `LifeformAnalyzer` | 生命体分析机 | — | — | ·/·/· | — |
| 3119 | `DPSMeter` | 每秒伤害计数器 | — | — | ·/·/· | — |
| 3120 | `FishermansGuide` | 渔民袖珍宝典 | — | — | ·/·/· | — |
| 3121 | `GoblinTech` | 哥布林数据仪 | — | — | ·/·/· | — |
| 3122 | `REK` | R.E.K.3000 | — | — | ·/·/· | — |
| 3123 | `PDA` | 个人数字助手 | — | — | ·/·/· | — |
| 3124 | `CellPhone` | 手机 | — | — | ·/·/· | — |
| 3125 | `GraniteChest` | 花岗岩箱 | 21 | — | ·/·/· | — |
| 3126 | `MeteoriteClock` | 陨石钟 | 104 | — | ·/·/· | — |
| 3127 | `MarbleClock` | 大理石时钟 | 104 | — | ·/·/· | — |
| 3128 | `GraniteClock` | 花岗岩时钟 | 104 | — | ·/·/· | — |
| 3129 | `MeteoriteDoor` | 陨石门 | 10 | — | ·/·/· | — |
| 3130 | `MarbleDoor` | 大理石门 | 10 | — | ·/·/· | — |
| 3131 | `GraniteDoor` | 花岗岩门 | 10 | — | ·/·/· | — |
| 3132 | `MeteoriteDresser` | 陨石梳妆台 | 88 | — | ·/·/· | — |
| 3133 | `MarbleDresser` | 大理石梳妆台 | 88 | — | ·/·/· | — |
| 3134 | `GraniteDresser` | 花岗岩梳妆台 | 88 | — | ·/·/· | — |
| 3135 | `MeteoriteLamp` | 陨石灯 | 93 | — | ·/·/· | — |
| 3136 | `MarbleLamp` | 大理石灯 | 93 | — | ·/·/· | — |
| 3137 | `GraniteLamp` | 花岗岩灯 | 93 | — | ·/·/· | — |
| 3138 | `MeteoriteLantern` | 陨石灯笼 | 42 | — | ·/·/· | — |
| 3139 | `MarbleLantern` | 大理石灯笼 | 42 | — | ·/·/· | — |
| 3140 | `GraniteLantern` | 花岗岩灯笼 | 42 | — | ·/·/· | — |
| 3141 | `MeteoritePiano` | 陨石钢琴 | 87 | — | ·/·/· | — |
| 3142 | `MarblePiano` | 大理石钢琴 | 87 | — | ·/·/· | — |
| 3143 | `GranitePiano` | 花岗岩钢琴 | 87 | — | ·/·/· | — |
| 3144 | `MeteoritePlatform` | 陨石平台 | 19 | — | ·/·/· | — |
| 3145 | `MarblePlatform` | 大理石平台 | 19 | — | ·/·/· | — |
| 3146 | `GranitePlatform` | 花岗岩平台 | 19 | — | ·/·/· | — |
| 3147 | `MeteoriteSink` | 陨石水槽 | 172 | — | ·/·/· | — |
| 3148 | `MarbleSink` | 大理石水槽 | 172 | — | ·/·/· | — |
| 3149 | `GraniteSink` | 花岗岩水槽 | 172 | — | ·/·/· | — |
| 3150 | `MeteoriteSofa` | 陨石沙发 | 89 | — | ·/·/· | — |
| 3151 | `MarbleSofa` | 大理石沙发 | 89 | — | ·/·/· | — |
| 3152 | `GraniteSofa` | 花岗岩沙发 | 89 | — | ·/·/· | — |
| 3153 | `MeteoriteTable` | 陨石桌 | 14 | — | ·/·/· | — |
| 3154 | `MarbleTable` | 大理石桌 | 14 | — | ·/·/· | — |
| 3155 | `GraniteTable` | 花岗岩桌 | 14 | — | ·/·/· | — |
| 3156 | `MeteoriteWorkBench` | 陨石工作台 | 18 | — | ·/·/· | — |
| 3157 | `MarbleWorkBench` | 大理石工作台 | 18 | — | ·/·/· | — |
| 3158 | `GraniteWorkBench` | 花岗岩工作台 | 18 | — | ·/·/· | — |
| 3159 | `MeteoriteBathtub` | 陨石浴缸 | 90 | — | ·/·/· | — |
| 3160 | `MarbleBathtub` | 大理石浴缸 | 90 | — | ·/·/· | — |
| 3161 | `GraniteBathtub` | 花岗岩浴缸 | 90 | — | ·/·/· | — |
| 3162 | `MeteoriteBed` | 陨石床 | 79 | — | ·/·/· | — |
| 3163 | `MarbleBed` | 大理石床 | 79 | — | ·/·/· | — |
| 3164 | `GraniteBed` | 花岗岩床 | 79 | — | ·/·/· | — |
| 3165 | `MeteoriteBookcase` | 陨石书架 | 101 | — | ·/·/· | — |
| 3166 | `MarbleBookcase` | 大理石书架 | 101 | — | ·/·/· | — |
| 3167 | `GraniteBookcase` | 花岗岩书架 | 101 | — | ·/·/· | — |
| 3168 | `MeteoriteCandelabra` | 陨石烛台 | 100 | — | ·/·/· | — |
| 3169 | `MarbleCandelabra` | 大理石烛台 | 100 | — | ·/·/· | — |
| 3170 | `GraniteCandelabra` | 花岗岩烛台 | 100 | — | ·/·/· | — |
| 3171 | `MeteoriteCandle` | 陨石蜡烛 | 33 | — | ·/·/· | — |
| 3172 | `MarbleCandle` | 大理石蜡烛 | 33 | — | ·/·/· | — |
| 3173 | `GraniteCandle` | 花岗岩蜡烛 | 33 | — | ·/·/· | — |
| 3174 | `MeteoriteChair` | 陨石椅 | 15 | — | ·/·/· | — |
| 3175 | `MarbleChair` | 大理石椅 | 15 | — | ·/·/· | — |
| 3176 | `GraniteChair` | 花岗岩椅 | 15 | — | ·/·/· | — |
| 3177 | `MeteoriteChandelier` | 陨石吊灯 | 34 | — | ·/·/· | — |
| 3178 | `MarbleChandelier` | 大理石吊灯 | 34 | — | ·/·/· | — |
| 3179 | `GraniteChandelier` | 花岗岩吊灯 | 34 | — | ·/·/· | — |
| 3180 | `MeteoriteChest` | 陨石箱 | 21 | — | ·/·/· | — |
| 3181 | `MarbleChest` | 大理石箱 | 21 | — | ·/·/· | — |
| 3182 | `MagicWaterDropper` | 魔法滴水管 | 373 | — | ·/·/· | — |
| 3183 | `GoldenBugNet` | 金虫网 | — | — | ·/·/· | — |
| 3184 | `MagicLavaDropper` | 魔法熔岩滴管 | 374 | — | ·/·/· | — |
| 3185 | `MagicHoneyDropper` | 魔法蜂蜜滴管 | 375 | — | ·/·/· | — |
| 3186 | `EmptyDropper` | 空滴管 | — | — | ·/·/· | — |
| 3187 | `GladiatorHelmet` | 勇士头盔 | — | — | ·/·/· | — |
| 3188 | `GladiatorBreastplate` | 勇士胸甲 | — | — | ·/·/· | — |
| 3189 | `GladiatorLeggings` | 勇士护腿 | — | — | ·/·/· | — |
| 3190 | `ReflectiveDye` | 反光染料 | — | — | ·/·/· | — |
| 3191 | `EnchantedNightcrawler` | 附魔夜行者 | — | — | ·/·/· | — |
| 3192 | `Grubby` | 蛆虫 | — | — | ·/·/· | — |
| 3193 | `Sluggy` | 鼻涕虫 | — | — | ·/·/· | — |
| 3194 | `Buggy` | 蚜虫 | — | — | ·/·/· | — |
| 3195 | `GrubSoup` | 蛆虫汤 | — | — | ·/·/· | — |
| 3196 | `BombFish` | 炸弹鱼 | — | — | ·/·/· | — |
| 3197 | `FrostDaggerfish` | 寒霜飞鱼 | — | — | ·/·/· | 17 |
| 3198 | `SharpeningStation` | 利器站 | 377 | — | ·/·/· | — |
| 3199 | `IceMirror` | 冰雪镜 | — | — | ·/·/· | — |
| 3200 | `SailfishBoots` | 航鱼靴 | — | — | ·/·/· | — |
| 3201 | `TsunamiInABottle` | 海啸瓶 | — | — | ·/·/· | — |
| 3202 | `TargetDummy` | 傀儡 | 378 | — | ·/·/· | — |
| 3203 | `CorruptFishingCrate` | 腐化匣 | 376 | — | ·/·/· | — |
| 3204 | `CrimsonFishingCrate` | 猩红匣 | 376 | — | ·/·/· | — |
| 3205 | `DungeonFishingCrate` | 地牢匣 | 376 | — | ·/·/· | — |
| 3206 | `FloatingIslandFishingCrate` | 天空匣 | 376 | — | ·/·/· | — |
| 3207 | `HallowedFishingCrate` | 神圣匣 | 376 | — | ·/·/· | — |
| 3208 | `JungleFishingCrate` | 丛林匣 | 376 | — | ·/·/· | — |
| 3209 | `CrystalSerpent` | 水晶蛇 | — | — | ·/·/· | 40 |
| 3210 | `Toxikarp` | 毒弹枪 | — | — | ·/·/· | 43 |
| 3211 | `Bladetongue` | 舌锋剑 | — | — | ·/·/· | 55 |
| 3212 | `SharkToothNecklace` | 鲨牙项链 | — | — | ·/·/· | — |
| 3213 | `MoneyTrough` | 钱币槽 | — | — | ·/·/· | — |
| 3214 | `Bubble` | 泡泡 | 379 | — | ·/·/· | — |
| 3215 | `DayBloomPlanterBox` | 太阳花种植盆 | 380 | — | ·/·/· | — |
| 3216 | `MoonglowPlanterBox` | 月光草花盆 | 380 | — | ·/·/· | — |
| 3217 | `CorruptPlanterBox` | 死亡草种植盆 | 380 | — | ·/·/· | — |
| 3218 | `CrimsonPlanterBox` | 死亡草种植盆 | 380 | — | ·/·/· | — |
| 3219 | `BlinkrootPlanterBox` | 闪耀根种植盆 | 380 | — | ·/·/· | — |
| 3220 | `WaterleafPlanterBox` | 幌菊花盆 | 380 | — | ·/·/· | — |
| 3221 | `ShiverthornPlanterBox` | 寒颤棘种植盆 | 380 | — | ·/·/· | — |
| 3222 | `FireBlossomPlanterBox` | 火焰花花盆 | 380 | — | ·/·/· | — |
| 3223 | `BrainOfConfusion` | 混乱之脑 | — | — | ·/·/· | — |
| 3224 | `WormScarf` | 蠕虫围巾 | — | — | ·/·/· | — |
| 3225 | `BalloonPufferfish` | 气球河豚鱼 | — | — | ·/·/· | — |
| 3226 | `BejeweledValkyrieHead` | Lazure的女武神头环 | — | — | ·/·/· | — |
| 3227 | `BejeweledValkyrieBody` | Lazure的女武神斗蓬 | — | — | ·/·/· | — |
| 3228 | `BejeweledValkyrieWing` | Lazure的屏障台 | — | — | ·/·/· | — |
| 3229 | `RichGravestone1` | 金十字墓石碑 | 85 | — | ·/·/· | — |
| 3230 | `RichGravestone2` | 金墓石 | 85 | — | ·/·/· | — |
| 3231 | `RichGravestone3` | 金墓石碑 | 85 | — | ·/·/· | — |
| 3232 | `RichGravestone4` | 金墓碑 | 85 | — | ·/·/· | — |
| 3233 | `RichGravestone5` | 金碑石 | 85 | — | ·/·/· | — |
| 3234 | `CrystalBlock` | 水晶块 | 385 | — | ·/·/· | — |
| 3235 | `MusicBoxMartians` | 八音盒（火星暴乱） | 139 | — | ·/·/· | — |
| 3236 | `MusicBoxPirates` | 八音盒（海盗入侵） | 139 | — | ·/·/· | — |
| 3237 | `MusicBoxHell` | 八音盒（地狱） | 139 | — | ·/·/· | — |
| 3238 | `CrystalBlockWall` | 水晶块墙 | — | 186 | ·/·/· | — |
| 3239 | `Trapdoor` | 机关门 | 387 | — | ·/·/· | — |
| 3240 | `TallGate` | 高门 | 388 | — | ·/·/· | — |
| 3241 | `SharkronBalloon` | 鲨鱼龙气球 | — | — | ·/·/· | — |
| 3242 | `TaxCollectorHat` | 税收官帽 | — | — | ·/·/· | — |
| 3243 | `TaxCollectorSuit` | 税收官套装 | — | — | ·/·/· | — |
| 3244 | `TaxCollectorPants` | 税收官裤 | — | — | ·/·/· | — |
| 3245 | `BoneGlove` | 骨头手套 | — | — | ·/·/· | — |
| 3246 | `ClothierJacket` | 服装商夹克 | — | — | ·/·/· | — |
| 3247 | `ClothierPants` | 服装商裤 | — | — | ·/·/· | — |
| 3248 | `DyeTraderTurban` | 染料商头巾 | — | — | ·/·/· | — |
| 3249 | `DeadlySphereStaff` | 致命球法杖 | — | — | ·/·/· | 40 |
| 3250 | `BalloonHorseshoeFart` | 绿马蹄气球 | — | — | ·/·/· | — |
| 3251 | `BalloonHorseshoeHoney` | 琥珀马掌气球 | — | — | ·/·/· | — |
| 3252 | `BalloonHorseshoeSharkron` | 粉马蹄气球 | — | — | ·/·/· | — |
| 3253 | `LavaLamp` | 熔岩灯 | 390 | — | ·/·/· | — |
| 3254 | `CageEnchantedNightcrawler` | 附魔夜行者笼 | 391 | — | ·/·/· | — |
| 3255 | `CageBuggy` | 蚜虫笼 | 392 | — | ·/·/· | — |
| 3256 | `CageGrubby` | 蛆虫笼 | 393 | — | ·/·/· | — |
| 3257 | `CageSluggy` | 鼻涕虫笼 | 394 | — | ·/·/· | — |
| 3258 | `SlapHand` | 拍拍手 | — | — | ·/·/· | 55 |
| 3259 | `TwilightHairDye` | 暮光染发剂 | — | — | ·/·/· | — |
| 3260 | `BlessedApple` | 恩赐苹果 | — | — | ·/·/· | — |
| 3261 | `SpectreBar` | 幽灵锭 | 239 | — | ·/·/· | — |
| 3262 | `Code1` | 代码1球 | — | — | ·/·/· | 21 |
| 3263 | `BuccaneerBandana` | 西域海盗头巾 | — | — | ·/·/· | — |
| 3264 | `BuccaneerShirt` | 西域海盗上装 | — | — | ·/·/· | — |
| 3265 | `BuccaneerPants` | 西域海盗马裤 | — | — | ·/·/· | — |
| 3266 | `ObsidianHelm` | 黑曜石逃犯帽 | — | — | ·/·/· | — |
| 3267 | `ObsidianShirt` | 黑曜石风衣 | — | — | ·/·/· | — |
| 3268 | `ObsidianPants` | 黑曜石裤 | — | — | ·/·/· | — |
| 3269 | `MedusaHead` | 蛇发女妖头 | — | — | ·/·/· | 40 |
| 3270 | `ItemFrame` | 物品框 | 395 | — | ·/·/· | — |
| 3271 | `Sandstone` | 沙岩块 | 396 | — | ·/·/· | — |
| 3272 | `HardenedSand` | 硬化沙块 | 397 | — | ·/·/· | — |
| 3273 | `SandstoneWall` | 沙岩墙 | — | 275 | ·/·/· | — |
| 3274 | `CorruptHardenedSand` | 硬化黑檀沙块 | 398 | — | ·/·/· | — |
| 3275 | `CrimsonHardenedSand` | 硬化猩红沙块 | 399 | — | ·/·/· | — |
| 3276 | `CorruptSandstone` | 黑檀沙岩块 | 400 | — | ·/·/· | — |
| 3277 | `CrimsonSandstone` | 猩红沙岩块 | 401 | — | ·/·/· | — |
| 3278 | `WoodYoyo` | 木悠悠球 | — | — | ·/·/· | 9 |
| 3279 | `CorruptYoyo` | 抑郁球 | — | — | ·/·/· | 16 |
| 3280 | `CrimsonYoyo` | 血脉球 | — | — | ·/·/· | 17 |
| 3281 | `JungleYoyo` | 亚马逊球 | — | — | ·/·/· | 18 |
| 3282 | `Cascade` | 喷流球 | — | — | ·/·/· | 27 |
| 3283 | `Chik` | 吉克球 | — | — | ·/·/· | 39 |
| 3284 | `Code2` | 代码2球 | — | — | ·/·/· | 54 |
| 3285 | `Rally` | 对打球 | — | — | ·/·/· | 14 |
| 3286 | `Yelets` | 叶列茨球 | — | — | ·/·/· | 60 |
| 3287 | `RedsYoyo` | Red的抛球 | — | — | ·/·/· | 70 |
| 3288 | `ValkyrieYoyo` | 女武神悠悠球 | — | — | ·/·/· | 70 |
| 3289 | `Amarok` | 冰雪悠悠球 | — | — | ·/·/· | 43 |
| 3290 | `HelFire` | 狱火球 | — | — | ·/·/· | 41 |
| 3291 | `Kraken` | 克拉肯球 | — | — | ·/·/· | 95 |
| 3292 | `TheEyeOfCthulhu` | 克苏鲁之眼 | — | — | ·/·/· | 115 |
| 3293 | `RedString` | 红绳 | — | — | ·/·/· | — |
| 3294 | `OrangeString` | 橙绳 | — | — | ·/·/· | — |
| 3295 | `YellowString` | 黄绳 | — | — | ·/·/· | — |
| 3296 | `LimeString` | 绿黄绳 | — | — | ·/·/· | — |
| 3297 | `GreenString` | 绿绳 | — | — | ·/·/· | — |
| 3298 | `TealString` | 青绳 | — | — | ·/·/· | — |
| 3299 | `CyanString` | 蓝绿绳 | — | — | ·/·/· | — |
| 3300 | `SkyBlueString` | 天蓝绳 | — | — | ·/·/· | — |
| 3301 | `BlueString` | 蓝绳 | — | — | ·/·/· | — |
| 3302 | `PurpleString` | 紫绳 | — | — | ·/·/· | — |
| 3303 | `VioletString` | 紫罗兰绳 | — | — | ·/·/· | — |
| 3304 | `PinkString` | 粉绳 | — | — | ·/·/· | — |
| 3305 | `BrownString` | 棕绳 | — | — | ·/·/· | — |
| 3306 | `WhiteString` | 白绳 | — | — | ·/·/· | — |
| 3307 | `RainbowString` | 彩虹绳 | — | — | ·/·/· | — |
| 3308 | `BlackString` | 黑绳 | — | — | ·/·/· | — |
| 3309 | `BlackCounterweight` | 黑平衡锤 | — | — | ·/·/· | — |
| 3310 | `BlueCounterweight` | 蓝平衡锤 | — | — | ·/·/· | — |
| 3311 | `GreenCounterweight` | 绿平衡锤 | — | — | ·/·/· | — |
| 3312 | `PurpleCounterweight` | 紫平衡锤 | — | — | ·/·/· | — |
| 3313 | `RedCounterweight` | 红平衡锤 | — | — | ·/·/· | — |
| 3314 | `YellowCounterweight` | 黄平衡锤 | — | — | ·/·/· | — |
| 3315 | `FormatC` | 好胜球 | — | — | ·/·/· | 39 |
| 3316 | `Gradient` | 渐变球 | — | — | ·/·/· | 49 |
| 3317 | `Valor` | 英勇球 | — | — | ·/·/· | 28 |
| 3318 | `KingSlimeBossBag` | 宝藏袋（史莱姆王） | — | — | ·/·/· | — |
| 3319 | `EyeOfCthulhuBossBag` | 宝藏袋（克苏鲁之眼） | — | — | ·/·/· | — |
| 3320 | `EaterOfWorldsBossBag` | 宝藏袋（世界吞噬怪） | — | — | ·/·/· | — |
| 3321 | `BrainOfCthulhuBossBag` | 宝藏袋（克苏鲁之脑） | — | — | ·/·/· | — |
| 3322 | `QueenBeeBossBag` | 宝藏袋（蜂王） | — | — | ·/·/· | — |
| 3323 | `SkeletronBossBag` | 宝藏袋（骷髅王） | — | — | ·/·/· | — |
| 3324 | `WallOfFleshBossBag` | 宝藏袋（血肉墙） | — | — | ·/·/· | — |
| 3325 | `DestroyerBossBag` | 宝藏袋（毁灭者） | — | — | ·/·/· | — |
| 3326 | `TwinsBossBag` | 宝藏袋（双子魔眼） | — | — | ·/·/· | — |
| 3327 | `SkeletronPrimeBossBag` | 宝藏袋（机械骷髅王） | — | — | ·/·/· | — |
| 3328 | `PlanteraBossBag` | 宝藏袋（世纪之花） | — | — | ·/·/· | — |
| 3329 | `GolemBossBag` | 宝藏袋（石巨人） | — | — | ·/·/· | — |
| 3330 | `FishronBossBag` | 宝藏袋（猪龙鱼公爵） | — | — | ·/·/· | — |
| 3331 | `CultistBossBag` | 宝藏袋（拜月教邪教徒） | — | — | ·/·/· | — |
| 3332 | `MoonLordBossBag` | 宝藏袋（月亮领主） | — | — | ·/·/· | — |
| 3333 | `HiveBackpack` | 蜂巢背包 | — | — | ·/·/· | — |
| 3334 | `YoYoGlove` | 悠悠球手套 | — | — | ·/·/· | — |
| 3335 | `DemonHeart` | 恶魔之心 | — | — | ·/·/· | — |
| 3336 | `SporeSac` | 孢子囊 | — | — | ·/·/· | — |
| 3337 | `ShinyStone` | 闪亮石 | — | — | ·/·/· | — |
| 3338 | `HallowHardenedSand` | 硬化珍珠沙块 | 402 | — | ·/·/· | — |
| 3339 | `HallowSandstone` | 珍珠沙岩块 | 403 | — | ·/·/· | — |
| 3340 | `HardenedSandWall` | 硬化沙墙 | — | 304 | ·/·/· | — |
| 3341 | `CorruptHardenedSandWall` | 硬化黑檀沙墙 | — | 305 | ·/·/· | — |
| 3342 | `CrimsonHardenedSandWall` | 硬化猩红沙墙 | — | 306 | ·/·/· | — |
| 3343 | `HallowHardenedSandWall` | 硬化珍珠沙墙 | — | 307 | ·/·/· | — |
| 3344 | `CorruptSandstoneWall` | 黑檀沙岩墙 | — | 308 | ·/·/· | — |
| 3345 | `CrimsonSandstoneWall` | 猩红沙岩墙 | — | 309 | ·/·/· | — |
| 3346 | `HallowSandstoneWall` | 珍珠沙岩墙 | — | 310 | ·/·/· | — |
| 3347 | `DesertFossil` | 沙漠化石 | 404 | — | ·/·/· | — |
| 3348 | `DesertFossilWall` | 沙漠化石墙 | — | 311 | ·/·/· | — |
| 3349 | `DyeTradersScimitar` | 外星弯刀 | — | — | ·/·/· | 20 |
| 3350 | `PainterPaintballGun` | 彩弹枪 | — | — | ·/·/· | 12 |
| 3351 | `TaxCollectorsStickOfDoom` | 精致手杖 | — | — | ·/·/· | 16 |
| 3352 | `StylistKilLaKillScissorsIWish` | 时尚剪刀 | — | — | ·/·/· | 14 |
| 3353 | `MinecartMech` | 机械货车 | — | — | ·/·/· | — |
| 3354 | `MechanicalWheelPiece` | 机械车轮片 | — | — | ·/·/· | — |
| 3355 | `MechanicalWagonPiece` | 机械车体片 | — | — | ·/·/· | — |
| 3356 | `MechanicalBatteryPiece` | 机械电池片 | — | — | ·/·/· | — |
| 3357 | `AncientCultistTrophy` | 拜月教邪教徒纪念章 | 240 | — | ·/·/· | — |
| 3358 | `MartianSaucerTrophy` | 火星飞碟纪念章 | 240 | — | ·/·/· | — |
| 3359 | `FlyingDutchmanTrophy` | 荷兰飞盗船纪念章 | 240 | — | ·/·/· | — |
| 3360 | `LivingMahoganyWand` | 生命红木魔棒 | 383 | — | ·/·/· | — |
| 3361 | `LivingMahoganyLeafWand` | 红木树叶魔棒 | 384 | — | ·/·/· | — |
| 3362 | `FallenTuxedoShirt` | 堕落西装衣 | — | — | ·/·/· | — |
| 3363 | `FallenTuxedoPants` | 堕落西装裤 | — | — | ·/·/· | — |
| 3364 | `Fireplace` | 壁炉 | 405 | — | ·/·/· | — |
| 3365 | `Chimney` | 烟囱 | 406 | — | ·/·/· | — |
| 3366 | `YoyoBag` | 悠悠球袋 | — | — | ·/·/· | — |
| 3367 | `ShrimpyTruffle` | 虾松露 | — | — | ·/·/· | — |
| 3368 | `Arkhalis` | Arkhalis剑 | — | — | ·/·/· | 25 |
| 3369 | `ConfettiCannon` | 彩纸炮 | 209 | — | ·/·/· | — |
| 3370 | `MusicBoxTowers` | 八音盒（神塔） | 139 | — | ·/·/· | — |
| 3371 | `MusicBoxGoblins` | 八音盒（哥布林入侵） | 139 | — | ·/·/· | — |
| 3372 | `BossMaskCultist` | 拜月教邪教徒面具 | — | — | ·/·/· | — |
| 3373 | `BossMaskMoonlord` | 月亮领主面具 | — | — | ·/·/· | — |
| 3374 | `FossilHelm` | 化石头盔 | — | — | ·/·/· | — |
| 3375 | `FossilShirt` | 化石板甲 | — | — | ·/·/· | — |
| 3376 | `FossilPants` | 化石护胫 | — | — | ·/·/· | — |
| 3377 | `AmberStaff` | 琥珀法杖 | — | — | ·/·/· | 21 |
| 3378 | `BoneJavelin` | 骨头标枪 | — | — | ·/·/· | 20 |
| 3379 | `BoneDagger` | 骨投刀 | — | — | ·/·/· | 14 |
| 3380 | `FossilOre` | 坚固头盔 | 407 | — | ·/·/· | — |
| 3381 | `StardustHelmet` | 星尘头盔 | — | — | ·/·/· | — |
| 3382 | `StardustBreastplate` | 星尘板甲 | — | — | ·/·/· | — |
| 3383 | `StardustLeggings` | 星尘护腿 | — | — | ·/·/· | — |
| 3384 | `PortalGun` | 传送枪 | — | — | ·/·/· | — |
| 3385 | `StrangePlant1` | 奇异植物 | 227 | — | ·/·/· | — |
| 3386 | `StrangePlant2` | 奇异植物 | 227 | — | ·/·/· | — |
| 3387 | `StrangePlant3` | 奇异植物 | 227 | — | ·/·/· | — |
| 3388 | `StrangePlant4` | 奇异植物 | 227 | — | ·/·/· | — |
| 3389 | `Terrarian` | 泰拉悠悠球 | — | — | ·/·/· | 190 |
| 3390 | `GoblinSummonerBanner` | 哥布林术士旗帜 | 91 | — | ·/·/· | — |
| 3391 | `SalamanderBanner` | 蝾螈旗 | 91 | — | ·/·/· | — |
| 3392 | `GiantShellyBanner` | 巨型卷壳怪旗 | 91 | — | ·/·/· | — |
| 3393 | `CrawdadBanner` | 龙虾旗 | 91 | — | ·/·/· | — |
| 3394 | `FritzBanner` | 弗里茨旗 | 91 | — | ·/·/· | — |
| 3395 | `CreatureFromTheDeepBanner` | 水月怪旗 | 91 | — | ·/·/· | — |
| 3396 | `DrManFlyBanner` | 飞人博士旗 | 91 | — | ·/·/· | — |
| 3397 | `MothronBanner` | 蛾怪旗 | 91 | — | ·/·/· | — |
| 3398 | `SeveredHandBanner` | 残手旗 | 91 | — | ·/·/· | — |
| 3399 | `ThePossessedBanner` | 攀爬魔旗 | 91 | — | ·/·/· | — |
| 3400 | `ButcherBanner` | 屠夫旗 | 91 | — | ·/·/· | — |
| 3401 | `PsychoBanner` | 变态人旗 | 91 | — | ·/·/· | — |
| 3402 | `DeadlySphereBanner` | 致命球旗 | 91 | — | ·/·/· | — |
| 3403 | `NailheadBanner` | 钉头旗 | 91 | — | ·/·/· | — |
| 3404 | `PoisonousSporeBanner` | 毒孢旗 | 91 | — | ·/·/· | — |
| 3405 | `MedusaBanner` | 蛇发女妖旗 | 91 | — | ·/·/· | — |
| 3406 | `GreekSkeletonBanner` | 装甲步兵旗 | 91 | — | ·/·/· | — |
| 3407 | `GraniteFlyerBanner` | 花岗精旗 | 91 | — | ·/·/· | — |
| 3408 | `GraniteGolemBanner` | 花岗岩巨人横幅 | 91 | — | ·/·/· | — |
| 3409 | `BloodZombieBanner` | 血腥僵尸旗 | 91 | — | ·/·/· | — |
| 3410 | `DripplerBanner` | 滴滴怪旗 | 91 | — | ·/·/· | — |
| 3411 | `TombCrawlerBanner` | 墓穴爬虫旗 | 91 | — | ·/·/· | — |
| 3412 | `DuneSplicerBanner` | 沙虫旗 | 91 | — | ·/·/· | — |
| 3413 | `FlyingAntlionBanner` | 蚁狮蜂旗 | 91 | — | ·/·/· | — |
| 3414 | `WalkingAntlionBanner` | 蚁狮马旗 | 91 | — | ·/·/· | — |
| 3415 | `DesertGhoulBanner` | 食尸鬼旗 | 91 | — | ·/·/· | — |
| 3416 | `DesertLamiaBanner` | 拉弥亚旗 | 91 | — | ·/·/· | — |
| 3417 | `DesertDjinnBanner` | 沙漠幽魂旗 | 91 | — | ·/·/· | — |
| 3418 | `DesertBasiliskBanner` | 蛇蜥怪旗 | 91 | — | ·/·/· | — |
| 3419 | `RavagerScorpionBanner` | 沙贼横幅 | 91 | — | ·/·/· | — |
| 3420 | `StardustSoldierBanner` | 观星怪旗 | 91 | — | ·/·/· | — |
| 3421 | `StardustWormBanner` | 银河织妖旗 | 91 | — | ·/·/· | — |
| 3422 | `StardustJellyfishBanner` | 液体入侵怪旗 | 91 | — | ·/·/· | — |
| 3423 | `StardustSpiderBanner` | 闪耀炮手旗 | 91 | — | ·/·/· | — |
| 3424 | `StardustSmallCellBanner` | 迷你星细胞法杖旗 | 91 | — | ·/·/· | — |
| 3425 | `StardustLargeCellBanner` | 星细胞法杖旗 | 91 | — | ·/·/· | — |
| 3426 | `SolarCoriteBanner` | 流星火怪旗 | 91 | — | ·/·/· | — |
| 3427 | `SolarSrollerBanner` | 火滚怪旗 | 91 | — | ·/·/· | — |
| 3428 | `SolarCrawltipedeBanner` | 千足蜈蚣旗 | 91 | — | ·/·/· | — |
| 3429 | `SolarDrakomireRiderBanner` | 火龙怪骑士旗 | 91 | — | ·/·/· | — |
| 3430 | `SolarDrakomireBanner` | 火龙怪旗 | 91 | — | ·/·/· | — |
| 3431 | `SolarSolenianBanner` | 火月怪旗 | 91 | — | ·/·/· | — |
| 3432 | `NebulaSoldierBanner` | 预言帝旗 | 91 | — | ·/·/· | — |
| 3433 | `NebulaHeadcrabBanner` | 吮脑怪旗 | 91 | — | ·/·/· | — |
| 3434 | `NebulaBrainBanner` | 星云浮怪旗 | 91 | — | ·/·/· | — |
| 3435 | `NebulaBeastBanner` | 进化兽旗 | 91 | — | ·/·/· | — |
| 3436 | `VortexLarvaBanner` | 异星幼虫旗 | 91 | — | ·/·/· | — |
| 3437 | `VortexHornetQueenBanner` | 异星蜂王旗 | 91 | — | ·/·/· | — |
| 3438 | `VortexHornetBanner` | 异星黄蜂旗 | 91 | — | ·/·/· | — |
| 3439 | `VortexSoldierBanner` | 星旋怪旗 | 91 | — | ·/·/· | — |
| 3440 | `VortexRiflemanBanner` | 漩泥怪旗 | 91 | — | ·/·/· | — |
| 3441 | `PirateCaptainBanner` | 海盗船长旗 | 91 | — | ·/·/· | — |
| 3442 | `PirateDeadeyeBanner` | 海盗神射手旗 | 91 | — | ·/·/· | — |
| 3443 | `PirateCorsairBanner` | 私船海盗旗 | 91 | — | ·/·/· | — |
| 3444 | `PirateCrossbowerBanner` | 海盗弩手旗 | 91 | — | ·/·/· | — |
| 3445 | `MartianWalkerBanner` | 火星走妖旗 | 91 | — | ·/·/· | — |
| 3446 | `RedDevilBanner` | 红魔鬼旗 | 91 | — | ·/·/· | — |
| 3447 | `PinkJellyfishBanner` | 粉水母旗 | 91 | — | ·/·/· | — |
| 3448 | `GreenJellyfishBanner` | 绿水母旗 | 91 | — | ·/·/· | — |
| 3449 | `DarkMummyBanner` | 暗黑木乃伊旗 | 91 | — | ·/·/· | — |
| 3450 | `LightMummyBanner` | 光明木乃伊旗 | 91 | — | ·/·/· | — |
| 3451 | `AngryBonesBanner` | 愤怒骷髅怪旗 | 91 | — | ·/·/· | — |
| 3452 | `IceTortoiseBanner` | 冰雪陆龟旗 | 91 | — | ·/·/· | — |
| 3453 | `NebulaPickup1` | 伤害强化焰 | — | — | ·/·/· | — |
| 3454 | `NebulaPickup2` | 生命强化焰 | — | — | ·/·/· | — |
| 3455 | `NebulaPickup3` | 魔力强化焰 | — | — | ·/·/· | — |
| 3456 | `FragmentVortex` | 星旋碎片 | — | — | ·/·/· | — |
| 3457 | `FragmentNebula` | 星云碎片 | — | — | ·/·/· | — |
| 3458 | `FragmentSolar` | 日耀碎片 | — | — | ·/·/· | — |
| 3459 | `FragmentStardust` | 星尘碎片 | — | — | ·/·/· | — |
| 3460 | `LunarOre` | 夜明矿 | 408 | — | ·/·/· | — |
| 3461 | `LunarBrick` | 夜明砖 | 409 | — | ·/·/· | — |
| 3464 | `StardustDrill` | 星尘钻头 | — | — | 225/·/· | 50 |
| 3466 | `StardustPickaxe` | 星尘镐 | — | — | 225/·/· | 80 |
| 3467 | `LunarBar` | 夜明锭 | 239 | — | ·/·/· | — |
| 3468 | `WingsSolar` | 日耀之翼 | — | — | ·/·/· | — |
| 3469 | `WingsVortex` | 星旋强化翼 | — | — | ·/·/· | — |
| 3470 | `WingsNebula` | 星云斗篷 | — | — | ·/·/· | — |
| 3471 | `WingsStardust` | 星尘之翼 | — | — | ·/·/· | — |
| 3472 | `LunarBrickWall` | 夜明砖墙 | — | 224 | ·/·/· | — |
| 3473 | `SolarEruption` | 日耀喷发剑 | — | — | ·/·/· | 105 |
| 3474 | `StardustCellStaff` | 星尘细胞法杖 | — | — | ·/·/· | 60 |
| 3475 | `VortexBeater` | 星旋机枪 | — | — | ·/·/· | 50 |
| 3476 | `NebulaArcanum` | 星云奥秘 | — | — | ·/·/· | 70 |
| 3477 | `BloodWater` | 血水 | — | — | ·/·/· | 20 |
| 3478 | `TheBrideHat` | 面纱 | — | — | ·/·/· | — |
| 3479 | `TheBrideDress` | 婚裙 | — | — | ·/·/· | — |
| 3480 | `PlatinumBow` | 铂金弓 | — | — | ·/·/· | 13 |
| 3481 | `PlatinumHammer` | 铂金锤 | — | — | ·/·/59 | 10 |
| 3482 | `PlatinumAxe` | 铂金斧 | — | — | ·/12/· | 8 |
| 3483 | `PlatinumShortsword` | 铂金短剑 | — | — | ·/·/· | 13 |
| 3484 | `PlatinumBroadsword` | 铂金宽剑 | — | — | ·/·/· | 16 |
| 3485 | `PlatinumPickaxe` | 铂金镐 | — | — | 59/·/· | 7 |
| 3486 | `TungstenBow` | 钨弓 | — | — | ·/·/· | 10 |
| 3487 | `TungstenHammer` | 钨锤 | — | — | ·/·/50 | 9 |
| 3488 | `TungstenAxe` | 钨斧 | — | — | ·/11/· | 7 |
| 3489 | `TungstenShortsword` | 钨短剑 | — | — | ·/·/· | 10 |
| 3490 | `TungstenBroadsword` | 钨宽剑 | — | — | ·/·/· | 14 |
| 3491 | `TungstenPickaxe` | 钨镐 | — | — | 50/·/· | 6 |
| 3492 | `LeadBow` | 铅弓 | — | — | ·/·/· | 9 |
| 3493 | `LeadHammer` | 铅锤 | — | — | ·/·/43 | 8 |
| 3494 | `LeadAxe` | 铅斧 | — | — | ·/10/· | 6 |
| 3495 | `LeadShortsword` | 铅短剑 | — | — | ·/·/· | 9 |
| 3496 | `LeadBroadsword` | 铅宽剑 | — | — | ·/·/· | 13 |
| 3497 | `LeadPickaxe` | 铅镐 | — | — | 43/·/· | 6 |
| 3498 | `TinBow` | 锡弓 | — | — | ·/·/· | 7 |
| 3499 | `TinHammer` | 锡锤 | — | — | ·/·/38 | 6 |
| 3500 | `TinAxe` | 锡斧 | — | — | ·/8/· | 4 |
| 3501 | `TinShortsword` | 锡短剑 | — | — | ·/·/· | 7 |
| 3502 | `TinBroadsword` | 锡宽剑 | — | — | ·/·/· | 10 |
| 3503 | `TinPickaxe` | 锡镐 | — | — | 35/·/· | 5 |
| 3504 | `CopperBow` | 铜弓 | — | — | ·/·/· | 6 |
| 3505 | `CopperHammer` | 铜锤 | — | — | ·/·/35 | 4 |
| 3506 | `CopperAxe` | 铜斧 | — | — | ·/7/· | 3 |
| 3507 | `CopperShortsword` | 铜短剑 | — | — | ·/·/· | 5 |
| 3508 | `CopperBroadsword` | 铜阔剑 | — | — | ·/·/· | 9 |
| 3509 | `CopperPickaxe` | 铜镐 | — | — | 35/·/· | 4 |
| 3510 | `SilverBow` | 银弓 | — | — | ·/·/· | 9 |
| 3511 | `SilverHammer` | 银锤 | — | — | ·/·/45 | 9 |
| 3512 | `SilverAxe` | 银斧 | — | — | ·/10/· | 6 |
| 3513 | `SilverShortsword` | 银短剑 | — | — | ·/·/· | 9 |
| 3514 | `SilverBroadsword` | 银阔剑 | — | — | ·/·/· | 14 |
| 3515 | `SilverPickaxe` | 银镐 | — | — | 45/·/· | 6 |
| 3516 | `GoldBow` | 金弓 | — | — | ·/·/· | 11 |
| 3517 | `GoldHammer` | 金锤 | — | — | ·/·/55 | 9 |
| 3518 | `GoldAxe` | 金斧 | — | — | ·/11/· | 7 |
| 3519 | `GoldShortsword` | 金短剑 | — | — | ·/·/· | 12 |
| 3520 | `GoldBroadsword` | 金阔剑 | — | — | ·/·/· | 15 |
| 3521 | `GoldPickaxe` | 金镐 | — | — | 55/·/· | 6 |
| 3522 | `LunarHamaxeSolar` | 耀斑锤斧 | — | — | ·/30/100 | 60 |
| 3523 | `LunarHamaxeVortex` | 星旋锤斧 | — | — | ·/30/100 | 60 |
| 3524 | `LunarHamaxeNebula` | 星云锤斧 | — | — | ·/30/100 | 60 |
| 3525 | `LunarHamaxeStardust` | 星尘锤斧 | — | — | ·/30/100 | 60 |
| 3526 | `SolarDye` | 日耀染料 | — | — | ·/·/· | — |
| 3527 | `NebulaDye` | 星云燃料 | — | — | ·/·/· | — |
| 3528 | `VortexDye` | 星旋染料 | — | — | ·/·/· | — |
| 3529 | `StardustDye` | 星尘染料 | — | — | ·/·/· | — |
| 3530 | `VoidDye` | 缥缈染料 | — | — | ·/·/· | — |
| 3531 | `StardustDragonStaff` | 星尘之龙法杖 | — | — | ·/·/· | 40 |
| 3532 | `Bacon` | 培根 | — | — | ·/·/· | — |
| 3533 | `ShiftingSandsDye` | 流沙染料 | — | — | ·/·/· | — |
| 3534 | `MirageDye` | 幻象染料 | — | — | ·/·/· | — |
| 3535 | `ShiftingPearlSandsDye` | 珍珠流沙染料 | — | — | ·/·/· | — |
| 3536 | `VortexMonolith` | 星旋天塔柱 | 410 | — | ·/·/· | — |
| 3537 | `NebulaMonolith` | 星云天塔柱 | 410 | — | ·/·/· | — |
| 3538 | `StardustMonolith` | 星尘天塔柱 | 410 | — | ·/·/· | — |
| 3539 | `SolarMonolith` | 日耀天塔柱 | 410 | — | ·/·/· | — |
| 3540 | `Phantasm` | 幻影弓 | — | — | ·/·/· | 50 |
| 3541 | `LastPrism` | 终极棱镜 | — | — | ·/·/· | 100 |
| 3542 | `NebulaBlaze` | 星云烈焰 | — | — | ·/·/· | 130 |
| 3543 | `DayBreak` | 破晓之光 | — | — | ·/·/· | 150 |
| 3544 | `SuperHealingPotion` | 超级治疗药水 | — | — | ·/·/· | — |
| 3545 | `Detonator` | 引爆器 | 411 | — | ·/·/· | — |
| 3546 | `FireworksLauncher` | 喜庆弹射器 | — | — | ·/·/· | 25 |
| 3547 | `BouncyDynamite` | 弹力雷管 | — | — | ·/·/· | — |
| 3548 | `PartyGirlGrenade` | 快乐手榴弹 | — | — | ·/·/· | 30 |
| 3549 | `LunarCraftingStation` | 远古操纵机 | 412 | — | ·/·/· | — |
| 3550 | `FlameAndSilverDye` | 红焰银染料 | — | — | ·/·/· | — |
| 3551 | `GreenFlameAndSilverDye` | 绿焰银染料 | — | — | ·/·/· | — |
| 3552 | `BlueFlameAndSilverDye` | 蓝焰银染料 | — | — | ·/·/· | — |
| 3553 | `ReflectiveCopperDye` | 反光铜染料 | — | — | ·/·/· | — |
| 3554 | `ReflectiveObsidianDye` | 反光黑曜石染料 | — | — | ·/·/· | — |
| 3555 | `ReflectiveMetalDye` | 反光金属染料 | — | — | ·/·/· | — |
| 3556 | `MidnightRainbowDye` | 午夜彩虹染料 | — | — | ·/·/· | — |
| 3557 | `BlackAndWhiteDye` | 黑白染料 | — | — | ·/·/· | — |
| 3558 | `BrightSilverDye` | 淡银染料 | — | — | ·/·/· | — |
| 3559 | `SilverAndBlackDye` | 银黑染料 | — | — | ·/·/· | — |
| 3560 | `RedAcidDye` | 红酸性染料 | — | — | ·/·/· | — |
| 3561 | `GelDye` | 凝胶染料 | — | — | ·/·/· | — |
| 3562 | `PinkGelDye` | 粉凝胶染料 | — | — | ·/·/· | — |
| 3563 | `SquirrelRed` | 红松鼠 | — | — | ·/·/· | — |
| 3564 | `SquirrelGold` | 金松鼠 | — | — | ·/·/· | — |
| 3565 | `SquirrelOrangeCage` | 红松鼠笼 | 413 | — | ·/·/· | — |
| 3566 | `SquirrelGoldCage` | 金松鼠笼 | 414 | — | ·/·/· | — |
| 3567 | `MoonlordBullet` | 夜明弹 | — | — | ·/·/· | 20 |
| 3568 | `MoonlordArrow` | 夜明箭 | — | — | ·/·/· | 15 |
| 3569 | `MoonlordTurretStaff` | 月亮传送门法杖 | — | — | ·/·/· | 100 |
| 3570 | `LunarFlareBook` | 月耀 | — | — | ·/·/· | 100 |
| 3571 | `RainbowCrystalStaff` | 七彩水晶法杖 | — | — | ·/·/· | 130 |
| 3572 | `LunarHook` | 月钩 | — | — | ·/·/· | — |
| 3573 | `LunarBlockSolar` | 日耀碎片块 | 415 | — | ·/·/· | — |
| 3574 | `LunarBlockVortex` | 星旋碎片块 | 416 | — | ·/·/· | — |
| 3575 | `LunarBlockNebula` | 星云碎片块 | 417 | — | ·/·/· | — |
| 3576 | `LunarBlockStardust` | 星尘碎片块 | 418 | — | ·/·/· | — |
| 3577 | `SuspiciousLookingTentacle` | 可疑触手 | — | — | ·/·/· | — |
| 3578 | `Yoraiz0rShirt` | Yoraiz0r的制服 | — | — | ·/·/· | — |
| 3579 | `Yoraiz0rPants` | Yoraiz0r的裙 | — | — | ·/·/· | — |
| 3580 | `Yoraiz0rWings` | Yoraiz0r的魔法 | — | — | ·/·/· | — |
| 3581 | `Yoraiz0rDarkness` | Yoraiz0r的怒容 | — | — | ·/·/· | — |
| 3582 | `JimsWings` | Jim的翅膀 | — | — | ·/·/· | — |
| 3583 | `Yoraiz0rHead` | Yoraiz0r的染色护目镜 | — | — | ·/·/· | — |
| 3584 | `LivingLeafWall` | 生命树叶墙 | — | 60 | ·/·/· | — |
| 3585 | `SkiphsHelm` | Skiphs的面具 | — | — | ·/·/· | — |
| 3586 | `SkiphsShirt` | Skiphs的皮肤 | — | — | ·/·/· | — |
| 3587 | `SkiphsPants` | Skiphs的熊裤 | — | — | ·/·/· | — |
| 3588 | `SkiphsWings` | Skiphs的爪子 | — | — | ·/·/· | — |
| 3589 | `LokisHelm` | Loki的头盔 | — | — | ·/·/· | — |
| 3590 | `LokisShirt` | Loki的胸甲 | — | — | ·/·/· | — |
| 3591 | `LokisPants` | Loki的护胫 | — | — | ·/·/· | — |
| 3592 | `LokisWings` | Loki的翅膀 | — | — | ·/·/· | — |
| 3593 | `SandSlimeBanner` | 沙史莱姆旗 | 91 | — | ·/·/· | — |
| 3594 | `SeaSnailBanner` | 海蜗牛旗 | 91 | — | ·/·/· | — |
| 3595 | `MoonLordTrophy` | 月亮领主纪念章 | 240 | — | ·/·/· | — |
| 3596 | `MoonLordPainting` | 不是小孩，也不是乌贼 | 242 | — | ·/·/· | — |
| 3597 | `BurningHadesDye` | 烈焰冥王染料 | — | — | ·/·/· | — |
| 3598 | `GrimDye` | 恐怖染料 | — | — | ·/·/· | — |
| 3599 | `LokisDye` | Loki的染料 | — | — | ·/·/· | — |
| 3600 | `ShadowflameHadesDye` | 暗影焰冥王染料 | — | — | ·/·/· | — |
| 3601 | `CelestialSigil` | 天界符 | — | — | ·/·/· | — |
| 3602 | `LogicGateLamp_Off` | 逻辑门灯（关） | 419 | — | ·/·/· | — |
| 3603 | `LogicGate_AND` | 逻辑门（与） | 420 | — | ·/·/· | — |
| 3604 | `LogicGate_OR` | 逻辑门（或） | 420 | — | ·/·/· | — |
| 3605 | `LogicGate_NAND` | 逻辑门（与非） | 420 | — | ·/·/· | — |
| 3606 | `LogicGate_NOR` | 逻辑门（或非） | 420 | — | ·/·/· | — |
| 3607 | `LogicGate_XOR` | 逻辑门（异或） | 420 | — | ·/·/· | — |
| 3608 | `LogicGate_NXOR` | 逻辑门（同或） | 420 | — | ·/·/· | — |
| 3609 | `ConveyorBeltLeft` | 传送带（顺时针） | 421 | — | ·/·/· | — |
| 3610 | `ConveyorBeltRight` | 传送带（逆时针） | 422 | — | ·/·/· | — |
| 3611 | `WireKite` | 精密线控仪 | — | — | ·/·/· | — |
| 3612 | `YellowWrench` | 黄扳手 | — | — | ·/·/· | — |
| 3613 | `LogicSensor_Sun` | 逻辑感应器（昼） | 423 | — | ·/·/· | — |
| 3614 | `LogicSensor_Moon` | 逻辑感应器（夜） | 423 | — | ·/·/· | — |
| 3615 | `LogicSensor_Above` | 逻辑感应器（玩家出入上方） | 423 | — | ·/·/· | — |
| 3616 | `WirePipe` | 分线盒 | 424 | — | ·/·/· | — |
| 3617 | `AnnouncementBox` | 广播盒 | 425 | — | ·/·/· | — |
| 3618 | `LogicGateLamp_On` | 逻辑门灯（开） | 419 | — | ·/·/· | — |
| 3619 | `MechanicalLens` | 机械晶状体 | — | — | ·/·/· | — |
| 3620 | `ActuationRod` | 制动魔杖 | — | — | ·/·/· | — |
| 3621 | `TeamBlockRed` | 红团队块 | 426 | — | ·/·/· | — |
| 3622 | `TeamBlockRedPlatform` | 红团队平台 | 427 | — | ·/·/· | — |
| 3623 | `StaticHook` | 静止钩 | — | — | ·/·/· | — |
| 3624 | `ActuationAccessory` | 自动安放器 | — | — | ·/·/· | — |
| 3625 | `MulticolorWrench` | 五彩扳手 | — | — | ·/·/· | — |
| 3626 | `WeightedPressurePlatePink` | 粉色加重压力板 | 428 | — | ·/·/· | — |
| 3627 | `EngineeringHelmet` | 工程头盔 | — | — | ·/·/· | — |
| 3628 | `CompanionCube` | 同伴方块 | — | — | ·/·/· | — |
| 3629 | `WireBulb` | 彩线灯泡 | 429 | — | ·/·/· | — |
| 3630 | `WeightedPressurePlateOrange` | 橙色加重压力板 | 428 | — | ·/·/· | — |
| 3631 | `WeightedPressurePlatePurple` | 紫色加重压力板 | 428 | — | ·/·/· | — |
| 3632 | `WeightedPressurePlateCyan` | 青色加重压力板 | 428 | — | ·/·/· | — |
| 3633 | `TeamBlockGreen` | 绿团队块 | 430 | — | ·/·/· | — |
| 3634 | `TeamBlockBlue` | 蓝团队块 | 431 | — | ·/·/· | — |
| 3635 | `TeamBlockYellow` | 黄团队块 | 432 | — | ·/·/· | — |
| 3636 | `TeamBlockPink` | 粉团队块 | 433 | — | ·/·/· | — |
| 3637 | `TeamBlockWhite` | 白团队块 | 434 | — | ·/·/· | — |
| 3638 | `TeamBlockGreenPlatform` | 绿团队平台 | 435 | — | ·/·/· | — |
| 3639 | `TeamBlockBluePlatform` | 蓝团队平台 | 436 | — | ·/·/· | — |
| 3640 | `TeamBlockYellowPlatform` | 黄团队平台 | 437 | — | ·/·/· | — |
| 3641 | `TeamBlockPinkPlatform` | 粉团队平台 | 438 | — | ·/·/· | — |
| 3642 | `TeamBlockWhitePlatform` | 白团队平台 | 439 | — | ·/·/· | — |
| 3643 | `LargeAmber` | 大琥珀 | — | — | ·/·/· | — |
| 3644 | `GemLockRuby` | 红玉宝石锁 | 440 | — | ·/·/· | — |
| 3645 | `GemLockSapphire` | 蓝玉宝石锁 | 440 | — | ·/·/· | — |
| 3646 | `GemLockEmerald` | 翡翠宝石锁 | 440 | — | ·/·/· | — |
| 3647 | `GemLockTopaz` | 黄玉宝石锁 | 440 | — | ·/·/· | — |
| 3648 | `GemLockAmethyst` | 紫晶宝石锁 | 440 | — | ·/·/· | — |
| 3649 | `GemLockDiamond` | 钻石宝石锁 | 440 | — | ·/·/· | — |
| 3650 | `GemLockAmber` | 琥珀宝石锁 | 440 | — | ·/·/· | — |
| 3651 | `SquirrelStatue` | 松鼠雕像 | 105 | — | ·/·/· | — |
| 3652 | `ButterflyStatue` | 蝴蝶雕像 | 105 | — | ·/·/· | — |
| 3653 | `WormStatue` | 蠕虫雕像 | 105 | — | ·/·/· | — |
| 3654 | `FireflyStatue` | 萤火虫雕像 | 105 | — | ·/·/· | — |
| 3655 | `ScorpionStatue` | 蝎子雕像 | 105 | — | ·/·/· | — |
| 3656 | `SnailStatue` | 蜗牛雕像 | 105 | — | ·/·/· | — |
| 3657 | `GrasshopperStatue` | 蚱蜢雕像 | 105 | — | ·/·/· | — |
| 3658 | `MouseStatue` | 老鼠雕像 | 105 | — | ·/·/· | — |
| 3659 | `DuckStatue` | 小鸭雕像 | 105 | — | ·/·/· | — |
| 3660 | `PenguinStatue` | 企鹅雕像 | 105 | — | ·/·/· | — |
| 3661 | `FrogStatue` | 青蛙雕像 | 105 | — | ·/·/· | — |
| 3662 | `BuggyStatue` | 蚜虫雕像 | 105 | — | ·/·/· | — |
| 3663 | `LogicGateLamp_Faulty` | 逻辑门灯（故障） | 419 | — | ·/·/· | — |
| 3664 | `PortalGunStation` | 传送枪站 | 209 | — | ·/·/· | — |
| 3665 | `Fake_Chest` | 受困宝箱 | 441 | — | ·/·/· | — |
| 3666 | `Fake_GoldChest` | 受困金箱 | 441 | — | ·/·/· | — |
| 3667 | `Fake_ShadowChest` | 受困暗影箱 | 441 | — | ·/·/· | — |
| 3668 | `Fake_EbonwoodChest` | 受困乌木箱 | 441 | — | ·/·/· | — |
| 3669 | `Fake_RichMahoganyChest` | 受困红木箱 | 441 | — | ·/·/· | — |
| 3670 | `Fake_PearlwoodChest` | 受困珍珠木箱 | 441 | — | ·/·/· | — |
| 3671 | `Fake_IvyChest` | 受困常春藤箱 | 441 | — | ·/·/· | — |
| 3672 | `Fake_IceChest` | 受困冰冻箱 | 441 | — | ·/·/· | — |
| 3673 | `Fake_LivingWoodChest` | 受困生命木箱 | 441 | — | ·/·/· | — |
| 3674 | `Fake_SkywareChest` | 受困天域箱 | 441 | — | ·/·/· | — |
| 3675 | `Fake_ShadewoodChest` | 受困暗影木箱 | 441 | — | ·/·/· | — |
| 3676 | `Fake_WebCoveredChest` | 受困蛛丝箱 | 441 | — | ·/·/· | — |
| 3677 | `Fake_LihzahrdChest` | 受困丛林蜥蜴箱 | 441 | — | ·/·/· | — |
| 3678 | `Fake_WaterChest` | 受困水中箱 | 441 | — | ·/·/· | — |
| 3679 | `Fake_JungleChest` | 受困丛林箱 | 441 | — | ·/·/· | — |
| 3680 | `Fake_CorruptionChest` | 受困腐化箱 | 441 | — | ·/·/· | — |
| 3681 | `Fake_CrimsonChest` | 受困猩红箱 | 441 | — | ·/·/· | — |
| 3682 | `Fake_HallowedChest` | 受困神圣箱 | 441 | — | ·/·/· | — |
| 3683 | `Fake_FrozenChest` | 受困冰雪箱 | 441 | — | ·/·/· | — |
| 3684 | `Fake_DynastyChest` | 受困王朝箱 | 441 | — | ·/·/· | — |
| 3685 | `Fake_HoneyChest` | 受困蜂蜜箱 | 441 | — | ·/·/· | — |
| 3686 | `Fake_SteampunkChest` | 受困蒸汽朋克箱 | 441 | — | ·/·/· | — |
| 3687 | `Fake_PalmWoodChest` | 受困棕榈木箱 | 441 | — | ·/·/· | — |
| 3688 | `Fake_MushroomChest` | 受困蘑菇箱 | 441 | — | ·/·/· | — |
| 3689 | `Fake_BorealWoodChest` | 受困针叶木箱 | 441 | — | ·/·/· | — |
| 3690 | `Fake_SlimeChest` | 受困史莱姆箱 | 441 | — | ·/·/· | — |
| 3691 | `Fake_GreenDungeonChest` | 受困绿地牢箱 | 441 | — | ·/·/· | — |
| 3692 | `Fake_PinkDungeonChest` | 受困粉地牢箱 | 441 | — | ·/·/· | — |
| 3693 | `Fake_BlueDungeonChest` | 受困蓝地牢箱 | 441 | — | ·/·/· | — |
| 3694 | `Fake_BoneChest` | 受困骨箱 | 441 | — | ·/·/· | — |
| 3695 | `Fake_CactusChest` | 受困仙人掌箱 | 441 | — | ·/·/· | — |
| 3696 | `Fake_FleshChest` | 受困血肉箱 | 441 | — | ·/·/· | — |
| 3697 | `Fake_ObsidianChest` | 受困黑曜石箱 | 441 | — | ·/·/· | — |
| 3698 | `Fake_PumpkinChest` | 受困南瓜箱 | 441 | — | ·/·/· | — |
| 3699 | `Fake_SpookyChest` | 受困阴森箱 | 441 | — | ·/·/· | — |
| 3700 | `Fake_GlassChest` | 受困玻璃箱 | 441 | — | ·/·/· | — |
| 3701 | `Fake_MartianChest` | 受困火星箱 | 441 | — | ·/·/· | — |
| 3702 | `Fake_MeteoriteChest` | 受困陨石箱 | 441 | — | ·/·/· | — |
| 3703 | `Fake_GraniteChest` | 受困花岗岩箱 | 441 | — | ·/·/· | — |
| 3704 | `Fake_MarbleChest` | 受困大理石箱 | 441 | — | ·/·/· | — |
| 3705 | `Fake_newchest1` | ItemName.Fake_newchest1 | 441 | — | ·/·/· | — |
| 3706 | `Fake_newchest2` | ItemName.Fake_newchest2 | 441 | — | ·/·/· | — |
| 3707 | `ProjectilePressurePad` | 青绿压力垫板 | 442 | — | ·/·/· | — |
| 3708 | `WallCreeperStatue` | 爬墙蜘蛛雕像 | 105 | — | ·/·/· | — |
| 3709 | `UnicornStatue` | 独角兽雕像 | 105 | — | ·/·/· | — |
| 3710 | `DripplerStatue` | 滴滴怪雕像 | 105 | — | ·/·/· | — |
| 3711 | `WraithStatue` | 幻灵雕像 | 105 | — | ·/·/· | — |
| 3712 | `BoneSkeletonStatue` | 骨头骷髅雕像 | 105 | — | ·/·/· | — |
| 3713 | `UndeadVikingStatue` | 亡灵维京海盗雕像 | 105 | — | ·/·/· | — |
| 3714 | `MedusaStatue` | 蛇发女妖雕像 | 105 | — | ·/·/· | — |
| 3715 | `HarpyStatue` | 鸟妖雕像 | 105 | — | ·/·/· | — |
| 3716 | `PigronStatue` | 猪龙雕像 | 105 | — | ·/·/· | — |
| 3717 | `HopliteStatue` | 装甲步兵雕像 | 105 | — | ·/·/· | — |
| 3718 | `GraniteGolemStatue` | 花岗岩巨人雕像 | 105 | — | ·/·/· | — |
| 3719 | `ZombieArmStatue` | 武装僵尸雕像 | 105 | — | ·/·/· | — |
| 3720 | `BloodZombieStatue` | 血腥僵尸雕像 | 105 | — | ·/·/· | — |
| 3721 | `AnglerTackleBag` | 渔夫渔具袋 | — | — | ·/·/· | — |
| 3722 | `GeyserTrap` | 喷泉 | 443 | — | ·/·/· | — |
| 3723 | `UltraBrightCampfire` | 超亮篝火 | 215 | — | ·/·/· | — |
| 3724 | `BoneCampfire` | 骨头篝火 | 215 | — | ·/·/· | — |
| 3725 | `PixelBox` | 像素盒 | 445 | — | ·/·/· | — |
| 3726 | `LogicSensor_Water` | 液体感应器（水） | 423 | — | ·/·/· | — |
| 3727 | `LogicSensor_Lava` | 液体感应器（熔岩） | 423 | — | ·/·/· | — |
| 3728 | `LogicSensor_Honey` | 液体感应器（蜂蜜） | 423 | — | ·/·/· | — |
| 3729 | `LogicSensor_Liquid` | 液体感应器（任何） | 423 | — | ·/·/· | — |
| 3730 | `PartyBundleOfBalloonsAccessory` | 派对气球束 | — | — | ·/·/· | — |
| 3731 | `PartyBalloonAnimal` | 气球兔兔 | — | — | ·/·/· | — |
| 3732 | `PartyHat` | 派对帽 | — | — | ·/·/· | — |
| 3733 | `FlowerBoyHat` | 呆萌向日葵花瓣头盔 | — | — | ·/·/· | — |
| 3734 | `FlowerBoyShirt` | 呆萌向日葵上衣 | — | — | ·/·/· | — |
| 3735 | `FlowerBoyPants` | 呆萌向日葵裤装 | — | — | ·/·/· | — |
| 3736 | `SillyBalloonPink` | 呆萌粉气球 | 446 | — | ·/·/· | — |
| 3737 | `SillyBalloonPurple` | 呆萌紫气球 | 447 | — | ·/·/· | — |
| 3738 | `SillyBalloonGreen` | 呆萌绿气球 | 448 | — | ·/·/· | — |
| 3739 | `SillyStreamerBlue` | 蓝饰带 | 449 | — | ·/·/· | — |
| 3740 | `SillyStreamerGreen` | 绿饰带 | 450 | — | ·/·/· | — |
| 3741 | `SillyStreamerPink` | 粉饰带 | 451 | — | ·/·/· | — |
| 3742 | `SillyBalloonMachine` | 呆萌气球机 | 452 | — | ·/·/· | — |
| 3743 | `SillyBalloonTiedPink` | 呆萌丝带气球（粉） | 453 | — | ·/·/· | — |
| 3744 | `SillyBalloonTiedPurple` | 呆萌丝带气球（紫） | 453 | — | ·/·/· | — |
| 3745 | `SillyBalloonTiedGreen` | 呆萌丝带气球（绿） | 453 | — | ·/·/· | — |
| 3746 | `Pigronata` | 猪龙彩罐 | 454 | — | ·/·/· | — |
| 3747 | `PartyMonolith` | 派对中心 | 455 | — | ·/·/· | — |
| 3748 | `PartyBundleOfBalloonTile` | 呆萌丝带派对气球束 | 456 | — | ·/·/· | — |
| 3749 | `PartyPresent` | 派对礼物 | 457 | — | ·/·/· | — |
| 3750 | `SliceOfCake` | 蛋糕块 | 621 | — | ·/·/· | — |
| 3751 | `CogWall` | 齿轮墙 | — | 225 | ·/·/· | — |
| 3752 | `SandFallWall` | 沙暴墙 | — | 226 | ·/·/· | — |
| 3753 | `SnowFallWall` | 降雪墙 | — | 227 | ·/·/· | — |
| 3754 | `SandFallBlock` | 沙暴块 | 458 | — | ·/·/· | — |
| 3755 | `SnowFallBlock` | 降雪块 | 459 | — | ·/·/· | — |
| 3756 | `SnowCloudBlock` | 雪云 | 460 | — | ·/·/· | — |
| 3757 | `PedguinHat` | 企鹅兜帽 | — | — | ·/·/· | — |
| 3758 | `PedguinShirt` | 企鹅夹克 | — | — | ·/·/· | — |
| 3759 | `PedguinPants` | 企鹅裤 | — | — | ·/·/· | — |
| 3760 | `SillyBalloonPinkWall` | 呆萌粉气球墙 | — | 228 | ·/·/· | — |
| 3761 | `SillyBalloonPurpleWall` | 呆萌紫气球墙 | — | 229 | ·/·/· | — |
| 3762 | `SillyBalloonGreenWall` | 呆萌绿气球墙 | — | 230 | ·/·/· | — |
| 3763 | `AviatorSunglasses` | 0x33飞行员 | — | — | ·/·/· | — |
| 3764 | `BluePhasesaber` | 蓝晶光刃 | — | — | ·/·/· | 48 |
| 3765 | `RedPhasesaber` | 红晶光刃 | — | — | ·/·/· | 48 |
| 3766 | `GreenPhasesaber` | 绿晶光刃 | — | — | ·/·/· | 48 |
| 3767 | `PurplePhasesaber` | 紫晶光刃 | — | — | ·/·/· | 48 |
| 3768 | `WhitePhasesaber` | 白晶光刃 | — | — | ·/·/· | 48 |
| 3769 | `YellowPhasesaber` | 黄晶光刃 | — | — | ·/·/· | 48 |
| 3770 | `DjinnsCurse` | 神灵诅咒 | — | — | ·/·/· | — |
| 3771 | `AncientHorn` | 远古号角 | — | — | ·/·/· | — |
| 3772 | `AntlionClaw` | 颌骨剑 | — | — | ·/·/· | 16 |
| 3773 | `AncientArmorHat` | 远古头饰 | — | — | ·/·/· | — |
| 3774 | `AncientArmorShirt` | 远古上衣 | — | — | ·/·/· | — |
| 3775 | `AncientArmorPants` | 远古裤 | — | — | ·/·/· | — |
| 3776 | `AncientBattleArmorHat` | 禁戒面具 | — | — | ·/·/· | — |
| 3777 | `AncientBattleArmorShirt` | 禁戒长袍 | — | — | ·/·/· | — |
| 3778 | `AncientBattleArmorPants` | 禁戒裤 | — | — | ·/·/· | — |
| 3779 | `SpiritFlame` | 神灯烈焰 | — | — | ·/·/· | 85 |
| 3780 | `SandElementalBanner` | 沙尘精旗 | 91 | — | ·/·/· | — |
| 3781 | `PocketMirror` | 袖珍镜 | — | — | ·/·/· | — |
| 3782 | `MagicSandDropper` | 魔法沙粒滴管 | 461 | — | ·/·/· | — |
| 3783 | `AncientBattleArmorMaterial` | 禁戒碎片 | — | — | ·/·/· | — |
| 3784 | `LamiaPants` | 拉弥亚蛇尾裤 | — | — | ·/·/· | — |
| 3785 | `LamiaShirt` | 拉弥亚披肩 | — | — | ·/·/· | — |
| 3786 | `LamiaHat` | 拉弥亚面具 | — | — | ·/·/· | — |
| 3787 | `SkyFracture` | 裂天剑 | — | — | ·/·/· | 38 |
| 3788 | `OnyxBlaster` | 玛瑙爆破枪 | — | — | ·/·/· | 24 |
| 3789 | `SandsharkBanner` | 沙鲨旗 | 91 | — | ·/·/· | — |
| 3790 | `SandsharkCorruptBanner` | 噬骨沙鲨旗 | 91 | — | ·/·/· | — |
| 3791 | `SandsharkCrimsonBanner` | 戮血沙鲨旗 | 91 | — | ·/·/· | — |
| 3792 | `SandsharkHallowedBanner` | 晶狐沙鲨旗 | 91 | — | ·/·/· | — |
| 3793 | `TumbleweedBanner` | 愤怒翻滚怪旗 | 91 | — | ·/·/· | — |
| 3794 | `AncientCloth` | 远古布匹 | — | — | ·/·/· | — |
| 3795 | `DjinnLamp` | 沙漠幽魂灯 | 462 | — | ·/·/· | — |
| 3796 | `MusicBoxSandstorm` | 八音盒（沙尘暴） | 139 | — | ·/·/· | — |
| 3797 | `ApprenticeHat` | 学徒帽 | — | — | ·/·/· | — |
| 3798 | `ApprenticeRobe` | 学徒长袍 | — | — | ·/·/· | — |
| 3799 | `ApprenticeTrousers` | 学徒裤 | — | — | ·/·/· | — |
| 3800 | `SquireGreatHelm` | 侍卫大头盔 | — | — | ·/·/· | — |
| 3801 | `SquirePlating` | 侍卫板甲 | — | — | ·/·/· | — |
| 3802 | `SquireGreaves` | 侍卫护胫 | — | — | ·/·/· | — |
| 3803 | `HuntressWig` | 女猎人假发 | — | — | ·/·/· | — |
| 3804 | `HuntressJerkin` | 女猎人上衣 | — | — | ·/·/· | — |
| 3805 | `HuntressPants` | 女猎人裤 | — | — | ·/·/· | — |
| 3806 | `MonkBrows` | 和尚浓眉秃头帽 | — | — | ·/·/· | — |
| 3807 | `MonkShirt` | 和尚衣 | — | — | ·/·/· | — |
| 3808 | `MonkPants` | 和尚裤 | — | — | ·/·/· | — |
| 3809 | `ApprenticeScarf` | 学徒围巾 | — | — | ·/·/· | — |
| 3810 | `SquireShield` | 侍卫护盾 | — | — | ·/·/· | — |
| 3811 | `HuntressBuckler` | 女猎人圆盾 | — | — | ·/·/· | — |
| 3812 | `MonkBelt` | 和尚腰带 | — | — | ·/·/· | — |
| 3813 | `DefendersForge` | 护卫熔炉 | 463 | — | ·/·/· | — |
| 3814 | `WarTable` | 战争桌 | 464 | — | ·/·/· | — |
| 3815 | `WarTableBanner` | 战争桌旗 | 465 | — | ·/·/· | — |
| 3816 | `DD2ElderCrystalStand` | 埃特尼亚水晶座 | 466 | — | ·/·/· | — |
| 3817 | `DefenderMedal` | 护卫奖章 | — | — | ·/·/· | — |
| 3818 | `DD2FlameburstTowerT1Popper` | 爆炸烈焰魔杖 | — | — | ·/·/· | 17 |
| 3819 | `DD2FlameburstTowerT2Popper` | 爆炸烈焰手杖 | — | — | ·/·/· | 42 |
| 3820 | `DD2FlameburstTowerT3Popper` | 爆炸烈焰法杖 | — | — | ·/·/· | 88 |
| 3821 | `AleThrowingGlove` | 麦芽酒投掷器 | — | — | ·/·/· | 20 |
| 3822 | `DD2EnergyCrystal` | 天国魔力 | — | — | ·/·/· | — |
| 3823 | `DD2SquireDemonSword` | 地狱烙印 | — | — | ·/·/· | 95 |
| 3824 | `DD2BallistraTowerT1Popper` | 弩车魔杖 | — | — | ·/·/· | 30 |
| 3825 | `DD2BallistraTowerT2Popper` | 弩车手杖 | — | — | ·/·/· | 74 |
| 3826 | `DD2BallistraTowerT3Popper` | 弩车法杖 | — | — | ·/·/· | 156 |
| 3827 | `DD2SquireBetsySword` | 飞龙 | — | — | ·/·/· | 180 |
| 3828 | `DD2ElderCrystal` | 埃特尼亚水晶 | — | — | ·/·/· | — |
| 3829 | `DD2LightningAuraT1Popper` | 闪电光环魔杖 | — | — | ·/·/· | 4 |
| 3830 | `DD2LightningAuraT2Popper` | 闪电光环手杖 | — | — | ·/·/· | 11 |
| 3831 | `DD2LightningAuraT3Popper` | 闪电光环法杖 | — | — | ·/·/· | 34 |
| 3832 | `DD2ExplosiveTrapT1Popper` | 爆炸机关魔杖 | — | — | ·/·/· | 24 |
| 3833 | `DD2ExplosiveTrapT2Popper` | 爆炸机关手杖 | — | — | ·/·/· | 59 |
| 3834 | `DD2ExplosiveTrapT3Popper` | 爆炸机关法杖 | — | — | ·/·/· | 126 |
| 3835 | `MonkStaffT1` | 瞌睡章鱼 | — | — | ·/·/· | 50 |
| 3836 | `MonkStaffT2` | 恐怖关刀 | — | — | ·/·/· | 45 |
| 3837 | `DD2GoblinBomberBanner` | 天国哥布林投弹手旗 | 91 | — | ·/·/· | — |
| 3838 | `DD2GoblinBanner` | 天国哥布林旗 | 91 | — | ·/·/· | — |
| 3839 | `DD2SkeletonBanner` | 撒旦骷髅旗 | 91 | — | ·/·/· | — |
| 3840 | `DD2DrakinBanner` | 德拉克龙旗 | 91 | — | ·/·/· | — |
| 3841 | `DD2KoboldFlyerBanner` | 小妖魔滑翔怪旗 | 91 | — | ·/·/· | — |
| 3842 | `DD2KoboldBanner` | 小妖魔旗 | 91 | — | ·/·/· | — |
| 3843 | `DD2WitherBeastBanner` | 枯萎兽旗 | 91 | — | ·/·/· | — |
| 3844 | `DD2WyvernBanner` | 天国飞龙旗 | 91 | — | ·/·/· | — |
| 3845 | `DD2JavelinThrowerBanner` | 天国标枪投掷怪旗 | 91 | — | ·/·/· | — |
| 3846 | `DD2LightningBugBanner` | 天国荧光虫旗 | 91 | — | ·/·/· | — |
| 3852 | `BookStaff` | 无限智慧巨著 | — | — | ·/·/· | 36 |
| 3853 | `BoringBow` | ItemName.BoringBow | — | — | ·/·/· | — |
| 3854 | `DD2PhoenixBow` | 幽灵凤凰 | — | — | ·/·/· | 32 |
| 3855 | `DD2PetGato` | Gato蛋 | — | — | ·/·/· | — |
| 3856 | `DD2PetGhost` | 飞眼怪蛋 | — | — | ·/·/· | — |
| 3857 | `DD2PetDragon` | 龙蛋 | — | — | ·/·/· | — |
| 3858 | `MonkStaffT3` | 天龙之怒 | — | — | ·/·/· | 140 |
| 3859 | `DD2BetsyBow` | 空中祸害 | — | — | ·/·/· | 39 |
| 3860 | `BossBagBetsy` | 宝藏袋（双足翼龙） | — | — | ·/·/· | — |
| 3863 | `BossMaskBetsy` | 双足翼龙面具 | — | — | ·/·/· | — |
| 3864 | `BossMaskDarkMage` | 暗黑魔法师面具 | — | — | ·/·/· | — |
| 3865 | `BossMaskOgre` | 食人魔面具 | — | — | ·/·/· | — |
| 3866 | `BossTrophyBetsy` | 双足翼龙纪念章 | 240 | — | ·/·/· | — |
| 3867 | `BossTrophyDarkmage` | 暗黑魔法师纪念章 | 240 | — | ·/·/· | — |
| 3868 | `BossTrophyOgre` | 食人魔纪念章 | 240 | — | ·/·/· | — |
| 3869 | `MusicBoxDD2` | 音乐盒（撒旦军队） | 139 | — | ·/·/· | — |
| 3870 | `ApprenticeStaffT3` | 双足翼龙怒气 | — | — | ·/·/· | 100 |
| 3871 | `SquireAltHead` | 英灵殿骑士头盔 | — | — | ·/·/· | — |
| 3872 | `SquireAltShirt` | 英灵殿骑士胸甲 | — | — | ·/·/· | — |
| 3873 | `SquireAltPants` | 英灵殿骑士护胫 | — | — | ·/·/· | — |
| 3874 | `ApprenticeAltHead` | 暗黑艺术家帽子 | — | — | ·/·/· | — |
| 3875 | `ApprenticeAltShirt` | 暗黑艺术家长袍 | — | — | ·/·/· | — |
| 3876 | `ApprenticeAltPants` | 暗黑艺术家护腿 | — | — | ·/·/· | — |
| 3877 | `HuntressAltHead` | 红色骑术兜帽 | — | — | ·/·/· | — |
| 3878 | `HuntressAltShirt` | 红色骑术服 | — | — | ·/·/· | — |
| 3879 | `HuntressAltPants` | 红色骑术护腿 | — | — | ·/·/· | — |
| 3880 | `MonkAltHead` | 渗透忍者头盔 | — | — | ·/·/· | — |
| 3881 | `MonkAltShirt` | 渗透忍者上衣 | — | — | ·/·/· | — |
| 3882 | `MonkAltPants` | 渗透忍者裤装 | — | — | ·/·/· | — |
| 3883 | `BetsyWings` | 双足翼龙之翼 | — | — | ·/·/· | — |
| 3884 | `CrystalChest` | 水晶宝箱 | 467 | — | ·/·/· | — |
| 3885 | `GoldenChest` | 金色宝箱 | 467 | — | ·/·/· | — |
| 3886 | `Fake_CrystalChest` | 受困水晶宝箱 | 468 | — | ·/·/· | — |
| 3887 | `Fake_GoldenChest` | 受困金色宝箱 | 468 | — | ·/·/· | — |
| 3888 | `CrystalDoor` | 水晶门 | 10 | — | ·/·/· | — |
| 3889 | `CrystalChair` | 水晶椅子 | 15 | — | ·/·/· | — |
| 3890 | `CrystalCandle` | 水晶蜡烛 | 33 | — | ·/·/· | — |
| 3891 | `CrystalLantern` | 水晶灯笼 | 42 | — | ·/·/· | — |
| 3892 | `CrystalLamp` | 水晶灯 | 93 | — | ·/·/· | — |
| 3893 | `CrystalCandelabra` | 水晶烛台 | 100 | — | ·/·/· | — |
| 3894 | `CrystalChandelier` | 水晶吊灯 | 34 | — | ·/·/· | — |
| 3895 | `CrystalBathtub` | 水晶浴缸 | 90 | — | ·/·/· | — |
| 3896 | `CrystalSink` | 水晶水槽 | 172 | — | ·/·/· | — |
| 3897 | `CrystalBed` | 水晶床 | 79 | — | ·/·/· | — |
| 3898 | `CrystalClock` | 水晶时钟 | 104 | — | ·/·/· | — |
| 3899 | `SkywareClock2` | 日盘时钟 | 104 | — | ·/·/· | — |
| 3900 | `DungeonClockBlue` | 蓝地牢时钟 | 104 | — | ·/·/· | — |
| 3901 | `DungeonClockGreen` | 绿地牢时钟 | 104 | — | ·/·/· | — |
| 3902 | `DungeonClockPink` | 粉地牢时钟 | 104 | — | ·/·/· | — |
| 3903 | `CrystalPlatform` | 水晶平台 | 19 | — | ·/·/· | — |
| 3904 | `GoldenPlatform` | 金色平台 | 19 | — | ·/·/· | — |
| 3905 | `DynastyPlatform` | 王朝木平台 | 19 | — | ·/·/· | — |
| 3906 | `LihzahrdPlatform` | 丛林蜥蜴平台 | 19 | — | ·/·/· | — |
| 3907 | `FleshPlatform` | 血肉平台 | 19 | — | ·/·/· | — |
| 3908 | `FrozenPlatform` | 冰冻平台 | 19 | — | ·/·/· | — |
| 3909 | `CrystalWorkbench` | 水晶工作台 | 18 | — | ·/·/· | — |
| 3910 | `GoldenWorkbench` | 金色工作台 | 18 | — | ·/·/· | — |
| 3911 | `CrystalDresser` | 水晶梳妆台 | 88 | — | ·/·/· | — |
| 3912 | `DynastyDresser` | 王朝梳妆台 | 88 | — | ·/·/· | — |
| 3913 | `FrozenDresser` | 冰冻梳妆台 | 88 | — | ·/·/· | — |
| 3914 | `LivingWoodDresser` | 生命木梳妆台 | 88 | — | ·/·/· | — |
| 3915 | `CrystalPiano` | 水晶钢琴 | 87 | — | ·/·/· | — |
| 3916 | `DynastyPiano` | 王朝钢琴 | 87 | — | ·/·/· | — |
| 3917 | `CrystalBookCase` | 水晶书架 | 101 | — | ·/·/· | — |
| 3918 | `CrystalSofaHowDoesThatEvenWork` | 水晶沙发 | 89 | — | ·/·/· | — |
| 3919 | `DynastySofa` | 王朝沙发 | 89 | — | ·/·/· | — |
| 3920 | `CrystalTable` | 水晶桌 | 469 | — | ·/·/· | — |
| 3921 | `ArkhalisHat` | Arkhalis的兜帽 | — | — | ·/·/· | — |
| 3922 | `ArkhalisShirt` | Arkhalis的紧身衣 | — | — | ·/·/· | — |
| 3923 | `ArkhalisPants` | Arkhalis的紧身服 | — | — | ·/·/· | — |
| 3924 | `ArkhalisWings` | Arkhalis的飞翼 | — | — | ·/·/· | — |
| 3925 | `LeinforsHat` | Leinfors的护发器 | — | — | ·/·/· | — |
| 3926 | `LeinforsShirt` | Leinfors的奇异风 | — | — | ·/·/· | — |
| 3927 | `LeinforsPants` | Leinfors的潮裤 | — | — | ·/·/· | — |
| 3928 | `LeinforsWings` | Leinfors的卷缠斗篷 | — | — | ·/·/· | — |
| 3929 | `LeinforsAccessory` | Leinfors的奢华洗发液 | — | — | ·/·/· | — |
| 3930 | `Celeb2` | 喜庆弹射器Mk2 | — | — | ·/·/· | 50 |
| 3931 | `SpiderBathtub` | 蜘蛛浴缸 | 90 | — | ·/·/· | — |
| 3932 | `SpiderBed` | 蜘蛛床 | 79 | — | ·/·/· | — |
| 3933 | `SpiderBookcase` | 蜘蛛书架 | 101 | — | ·/·/· | — |
| 3934 | `SpiderDresser` | 蜘蛛梳妆台 | 88 | — | ·/·/· | — |
| 3935 | `SpiderCandelabra` | 蜘蛛烛台 | 100 | — | ·/·/· | — |
| 3936 | `SpiderCandle` | 蜘蛛蜡烛 | 33 | — | ·/·/· | — |
| 3937 | `SpiderChair` | 蜘蛛椅 | 15 | — | ·/·/· | — |
| 3938 | `SpiderChandelier` | 蜘蛛吊灯 | 34 | — | ·/·/· | — |
| 3939 | `SpiderChest` | 蜘蛛箱 | 467 | — | ·/·/· | — |
| 3940 | `SpiderClock` | 蜘蛛时钟 | 104 | — | ·/·/· | — |
| 3941 | `SpiderDoor` | 蜘蛛门 | 10 | — | ·/·/· | — |
| 3942 | `SpiderLamp` | 蜘蛛灯 | 93 | — | ·/·/· | — |
| 3943 | `SpiderLantern` | 蜘蛛灯笼 | 42 | — | ·/·/· | — |
| 3944 | `SpiderPiano` | 蜘蛛钢琴 | 87 | — | ·/·/· | — |
| 3945 | `SpiderPlatform` | 蜘蛛平台 | 19 | — | ·/·/· | — |
| 3946 | `SpiderSinkSpiderSinkDoesWhateverASpiderSinkDoes` | 蜘蛛水槽 | 172 | — | ·/·/· | — |
| 3947 | `SpiderSofa` | 蜘蛛沙发 | 89 | — | ·/·/· | — |
| 3948 | `SpiderTable` | 蜘蛛桌 | 469 | — | ·/·/· | — |
| 3949 | `SpiderWorkbench` | 蜘蛛工作台 | 18 | — | ·/·/· | — |
| 3950 | `Fake_SpiderChest` | 受困蜘蛛箱 | 468 | — | ·/·/· | — |
| 3951 | `IronBrick` | 铁砖 | 472 | — | ·/·/· | — |
| 3952 | `IronBrickWall` | 铁砖墙 | — | 231 | ·/·/· | — |
| 3953 | `LeadBrick` | 铅砖 | 473 | — | ·/·/· | — |
| 3954 | `LeadBrickWall` | 铅砖墙 | — | 232 | ·/·/· | — |
| 3955 | `LesionBlock` | 损伤块 | 474 | — | ·/·/· | — |
| 3956 | `LesionBlockWall` | 损伤块墙 | — | 233 | ·/·/· | — |
| 3957 | `LesionPlatform` | 损伤平台 | 19 | — | ·/·/· | — |
| 3958 | `LesionBathtub` | 损伤浴缸 | 90 | — | ·/·/· | — |
| 3959 | `LesionBed` | 损伤床 | 79 | — | ·/·/· | — |
| 3960 | `LesionBookcase` | 损伤书架 | 101 | — | ·/·/· | — |
| 3961 | `LesionCandelabra` | 损伤烛台 | 100 | — | ·/·/· | — |
| 3962 | `LesionCandle` | 损伤蜡烛 | 33 | — | ·/·/· | — |
| 3963 | `LesionChair` | 损伤椅 | 15 | — | ·/·/· | — |
| 3964 | `LesionChandelier` | 损伤吊灯 | 34 | — | ·/·/· | — |
| 3965 | `LesionChest` | 损伤宝箱 | 467 | — | ·/·/· | — |
| 3966 | `LesionClock` | 损伤时钟 | 104 | — | ·/·/· | — |
| 3967 | `LesionDoor` | 损伤门 | 10 | — | ·/·/· | — |
| 3968 | `LesionDresser` | 损伤梳妆台 | 88 | — | ·/·/· | — |
| 3969 | `LesionLamp` | 损伤灯 | 93 | — | ·/·/· | — |
| 3970 | `LesionLantern` | 损伤灯笼 | 42 | — | ·/·/· | — |
| 3971 | `LesionPiano` | 损伤钢琴 | 87 | — | ·/·/· | — |
| 3972 | `LesionSink` | 损伤水槽 | 172 | — | ·/·/· | — |
| 3973 | `LesionSofa` | 损伤沙发 | 89 | — | ·/·/· | — |
| 3974 | `LesionTable` | 损伤桌 | 469 | — | ·/·/· | — |
| 3975 | `LesionWorkbench` | 损伤工作台 | 18 | — | ·/·/· | — |
| 3976 | `Fake_LesionChest` | 受困损伤箱 | 468 | — | ·/·/· | — |
| 3977 | `HatRack` | 帽架 | 475 | — | ·/·/· | — |
| 3979 | `WoodenCrateHard` | 珍珠木匣 | 376 | — | ·/·/· | — |
| 3980 | `IronCrateHard` | 秘银匣 | 376 | — | ·/·/· | — |
| 3981 | `GoldenCrateHard` | 钛金匣 | 376 | — | ·/·/· | — |
| 3982 | `CorruptFishingCrateHard` | 污损匣 | 376 | — | ·/·/· | — |
| 3983 | `CrimsonFishingCrateHard` | 血匣 | 376 | — | ·/·/· | — |
| 3984 | `DungeonFishingCrateHard` | 围栏匣 | 376 | — | ·/·/· | — |
| 3985 | `FloatingIslandFishingCrateHard` | 天蓝匣 | 376 | — | ·/·/· | — |
| 3986 | `HallowedFishingCrateHard` | 天赐匣 | 376 | — | ·/·/· | — |
| 3987 | `JungleFishingCrateHard` | 荆棘匣 | 376 | — | ·/·/· | — |
| 3988 | `DeadMansChest` | 死人宝箱 | 467 | — | ·/·/· | — |
| 3989 | `GolfBall` | 高尔夫球 | — | — | ·/·/· | — |
| 3990 | `AmphibianBoots` | 水陆两用靴 | — | — | ·/·/· | — |
| 3991 | `ArcaneFlower` | 奥秘花 | — | — | ·/·/· | — |
| 3992 | `BerserkerGlove` | 狂战士手套 | — | — | ·/·/· | — |
| 3993 | `FairyBoots` | 仙灵靴 | — | — | ·/·/· | — |
| 3994 | `FrogFlipper` | 青蛙脚蹼 | — | — | ·/·/· | — |
| 3995 | `FrogGear` | 青蛙装备 | — | — | ·/·/· | — |
| 3996 | `FrogWebbing` | 青蛙带 | — | — | ·/·/· | — |
| 3997 | `FrozenShield` | 冰冻护盾 | — | — | ·/·/· | — |
| 3998 | `HeroShield` | 英雄护盾 | — | — | ·/·/· | — |
| 3999 | `LavaSkull` | 岩浆骷髅头 | — | — | ·/·/· | — |
| 4000 | `MagnetFlower` | 磁花 | — | — | ·/·/· | — |
| 4001 | `ManaCloak` | 魔力斗篷 | — | — | ·/·/· | — |
| 4002 | `MoltenQuiver` | 熔火箭袋 | — | — | ·/·/· | — |
| 4003 | `MoltenSkullRose` | 熔火骷髅头玫瑰 | — | — | ·/·/· | — |
| 4004 | `ObsidianSkullRose` | 黑曜石骷髅头玫瑰 | — | — | ·/·/· | — |
| 4005 | `ReconScope` | 侦察镜 | — | — | ·/·/· | — |
| 4006 | `StalkersQuiver` | 潜行者箭袋 | — | — | ·/·/· | — |
| 4007 | `StingerNecklace` | 毒刺项链 | — | — | ·/·/· | — |
| 4008 | `UltrabrightHelmet` | 超亮头盔 | — | — | ·/·/· | — |
| 4009 | `Apple` | 苹果 | — | — | ·/·/· | — |
| 4011 | `ApplePie` | 苹果派 | — | — | ·/·/· | — |
| 4012 | `BananaSplit` | 香蕉船 | — | — | ·/·/· | — |
| 4013 | `BBQRibs` | 烧烤排骨 | — | — | ·/·/· | — |
| 4014 | `BunnyStew` | 炖兔兔 | — | — | ·/·/· | — |
| 4015 | `Burger` | 汉堡 | — | — | ·/·/· | — |
| 4016 | `ChickenNugget` | 鸡块 | — | — | ·/·/· | — |
| 4017 | `ChocolateChipCookie` | 巧克力曲奇饼干 | — | — | ·/·/· | — |
| 4018 | `CreamSoda` | 奶油苏打 | — | — | ·/·/· | — |
| 4019 | `Escargot` | 食用蜗牛 | — | — | ·/·/· | — |
| 4020 | `FriedEgg` | 煎蛋 | — | — | ·/·/· | — |
| 4021 | `Fries` | 炸薯条 | — | — | ·/·/· | — |
| 4022 | `GoldenDelight` | 金怡口 | — | — | ·/·/· | — |
| 4023 | `Grapes` | 葡萄 | — | — | ·/·/· | — |
| 4024 | `GrilledSquirrel` | 烤松鼠 | — | — | ·/·/· | — |
| 4025 | `Hotdog` | 热狗 | — | — | ·/·/· | — |
| 4026 | `IceCream` | 冰淇淋 | — | — | ·/·/· | — |
| 4027 | `Milkshake` | 奶昔 | — | — | ·/·/· | — |
| 4028 | `Nachos` | 玉米片 | — | — | ·/·/· | — |
| 4029 | `Pizza` | 披萨 | — | — | ·/·/· | — |
| 4030 | `PotatoChips` | 薯片 | — | — | ·/·/· | — |
| 4031 | `RoastedBird` | 烤鸟 | — | — | ·/·/· | — |
| 4032 | `RoastedDuck` | 烤鸭 | — | — | ·/·/· | — |
| 4033 | `SauteedFrogLegs` | 炒蛙腿 | — | — | ·/·/· | — |
| 4034 | `SeafoodDinner` | 海鲜大餐 | — | — | ·/·/· | — |
| 4035 | `ShrimpPoBoy` | 鲜虾三明治 | — | — | ·/·/· | — |
| 4036 | `Spaghetti` | 意大利面 | — | — | ·/·/· | — |
| 4037 | `Steak` | 牛排 | — | — | ·/·/· | — |
| 4038 | `MoltenCharm` | 熔火护身符 | — | — | ·/·/· | — |
| 4039 | `GolfClubIron` | 高尔夫球杆（铁杆） | — | — | ·/·/· | — |
| 4040 | `GolfCup` | 高尔夫球洞 | 476 | — | ·/·/· | — |
| 4041 | `FlowerPacketBlue` | 蓝花种子 | 3 | — | ·/·/· | — |
| 4042 | `FlowerPacketMagenta` | 洋红花种子 | 3 | — | ·/·/· | — |
| 4043 | `FlowerPacketPink` | 粉花种子 | 3 | — | ·/·/· | — |
| 4044 | `FlowerPacketRed` | 红花种子 | 3 | — | ·/·/· | — |
| 4045 | `FlowerPacketYellow` | 黄花种子 | 3 | — | ·/·/· | — |
| 4046 | `FlowerPacketViolet` | 蓝紫花种子 | 3 | — | ·/·/· | — |
| 4047 | `FlowerPacketWhite` | 白花种子 | 3 | — | ·/·/· | — |
| 4048 | `FlowerPacketTallGrass` | 高茎草种子 | 3 | — | ·/·/· | — |
| 4049 | `LawnMower` | 割草机 | — | — | ·/·/· | — |
| 4050 | `CrimstoneBrick` | 猩红石砖 | 478 | — | ·/·/· | — |
| 4051 | `SmoothSandstone` | 光面沙岩 | 479 | — | ·/·/· | — |
| 4052 | `CrimstoneBrickWall` | 猩红石砖墙 | — | 234 | ·/·/· | — |
| 4053 | `SmoothSandstoneWall` | 光面沙岩墙 | — | 235 | ·/·/· | — |
| 4054 | `BloodMoonMonolith` | 血月天塔柱 | 480 | — | ·/·/· | — |
| 4055 | `SandBoots` | 沙丘行者靴 | — | — | ·/·/· | — |
| 4056 | `AncientChisel` | 远古凿子 | — | — | ·/·/· | — |
| 4057 | `CarbonGuitar` | 雨歌 | — | — | ·/·/· | — |
| 4059 | `FossilPickaxe` | 化石镐 | — | — | 55/·/· | 8 |
| 4060 | `SuperStarCannon` | 超级星星枪炮 | — | — | ·/·/· | 60 |
| 4061 | `ThunderSpear` | 风暴长矛 | — | — | ·/·/· | 14 |
| 4062 | `ThunderStaff` | 霹雳法杖 | — | — | ·/·/· | 20 |
| 4063 | `DrumSet` | 鼓组 | 486 | — | ·/·/· | — |
| 4064 | `PicnicTable` | 野餐桌 | 487 | — | ·/·/· | — |
| 4065 | `PicnicTableWithCloth` | 精致野餐桌 | 487 | — | ·/·/· | — |
| 4066 | `DesertMinecart` | 沙漠矿车 | — | — | ·/·/· | — |
| 4067 | `FishMinecart` | 鲤鱼矿车 | — | — | ·/·/· | — |
| 4068 | `FairyCritterPink` | 粉仙灵 | — | — | ·/·/· | — |
| 4069 | `FairyCritterGreen` | 绿仙灵 | — | — | ·/·/· | — |
| 4070 | `FairyCritterBlue` | 蓝仙灵 | — | — | ·/·/· | — |
| 4071 | `JunoniaShell` | 眼蛱蝶壳 | 324 | — | ·/·/· | — |
| 4072 | `LightningWhelkShell` | 闪电海螺壳 | 324 | — | ·/·/· | — |
| 4073 | `TulipShell` | 郁金香壳 | 324 | — | ·/·/· | — |
| 4074 | `PinWheel` | 风车 | 489 | — | ·/·/· | — |
| 4075 | `WeatherVane` | 风向标 | 490 | — | ·/·/· | — |
| 4076 | `VoidVault` | 虚空保险库 | 491 | — | ·/·/· | — |
| 4077 | `MusicBoxOceanAlt` | 八音盒（夜间海洋） | 139 | — | ·/·/· | — |
| 4078 | `MusicBoxSlimeRain` | 八音盒（史莱姆雨） | 139 | — | ·/·/· | — |
| 4079 | `MusicBoxSpaceAlt` | 八音盒（日间太空） | 139 | — | ·/·/· | — |
| 4080 | `MusicBoxTownDay` | 八音盒（日间小镇） | 139 | — | ·/·/· | — |
| 4081 | `MusicBoxTownNight` | 八音盒（夜间小镇） | 139 | — | ·/·/· | — |
| 4082 | `MusicBoxWindyDay` | 八音盒（有风的日子） | 139 | — | ·/·/· | — |
| 4083 | `GolfCupFlagWhite` | 白三角旗 | 493 | — | ·/·/· | — |
| 4084 | `GolfCupFlagRed` | 红三角旗 | 493 | — | ·/·/· | — |
| 4085 | `GolfCupFlagGreen` | 绿三角旗 | 493 | — | ·/·/· | — |
| 4086 | `GolfCupFlagBlue` | 蓝三角旗 | 493 | — | ·/·/· | — |
| 4087 | `GolfCupFlagYellow` | 黄三角旗 | 493 | — | ·/·/· | — |
| 4088 | `GolfCupFlagPurple` | 紫三角旗 | 493 | — | ·/·/· | — |
| 4089 | `GolfTee` | 高尔夫球座 | 494 | — | ·/·/· | — |
| 4090 | `ShellPileBlock` | 贝壳堆 | 495 | — | ·/·/· | — |
| 4091 | `AntiPortalBlock` | 反入口块 | 496 | — | ·/·/· | — |
| 4092 | `GolfClubPutter` | 高尔夫球杆（推杆） | — | — | ·/·/· | — |
| 4093 | `GolfClubWedge` | 高尔夫球杆（挖起杆） | — | — | ·/·/· | — |
| 4094 | `GolfClubDriver` | 高尔夫球杆（木杆） | — | — | ·/·/· | — |
| 4095 | `GolfWhistle` | 高尔夫口哨 | — | — | ·/·/· | — |
| 4096 | `ToiletEbonyWood` | 乌木马桶 | 497 | — | ·/·/· | — |
| 4097 | `ToiletRichMahogany` | 红木马桶 | 497 | — | ·/·/· | — |
| 4098 | `ToiletPearlwood` | 珍珠木马桶 | 497 | — | ·/·/· | — |
| 4099 | `ToiletLivingWood` | 生命木马桶 | 497 | — | ·/·/· | — |
| 4100 | `ToiletCactus` | 仙人掌马桶 | 497 | — | ·/·/· | — |
| 4101 | `ToiletBone` | 骨头马桶 | 497 | — | ·/·/· | — |
| 4102 | `ToiletFlesh` | 血肉马桶 | 497 | — | ·/·/· | — |
| 4103 | `ToiletMushroom` | 蘑菇马桶 | 497 | — | ·/·/· | — |
| 4104 | `ToiletSunplate` | 天域马桶 | 497 | — | ·/·/· | — |
| 4105 | `ToiletShadewood` | 暗影木马桶 | 497 | — | ·/·/· | — |
| 4106 | `ToiletLihzhard` | 丛林蜥蜴马桶 | 497 | — | ·/·/· | — |
| 4107 | `ToiletDungeonBlue` | 蓝地牢马桶 | 497 | — | ·/·/· | — |
| 4108 | `ToiletDungeonGreen` | 绿地牢马桶 | 497 | — | ·/·/· | — |
| 4109 | `ToiletDungeonPink` | 粉地牢马桶 | 497 | — | ·/·/· | — |
| 4110 | `ToiletObsidian` | 黑曜石马桶 | 497 | — | ·/·/· | — |
| 4111 | `ToiletFrozen` | 冰冻马桶 | 497 | — | ·/·/· | — |
| 4112 | `ToiletGlass` | 玻璃马桶 | 497 | — | ·/·/· | — |
| 4113 | `ToiletHoney` | 蜂蜜马桶 | 497 | — | ·/·/· | — |
| 4114 | `ToiletSteampunk` | 蒸汽朋克马桶 | 497 | — | ·/·/· | — |
| 4115 | `ToiletPumpkin` | 南瓜马桶 | 497 | — | ·/·/· | — |
| 4116 | `ToiletSpooky` | 阴森马桶 | 497 | — | ·/·/· | — |
| 4117 | `ToiletDynasty` | 王朝马桶 | 497 | — | ·/·/· | — |
| 4118 | `ToiletPalm` | 棕榈木马桶 | 497 | — | ·/·/· | — |
| 4119 | `ToiletBoreal` | 针叶木马桶 | 497 | — | ·/·/· | — |
| 4120 | `ToiletSlime` | 史莱姆马桶 | 497 | — | ·/·/· | — |
| 4121 | `ToiletMartian` | 火星马桶 | 497 | — | ·/·/· | — |
| 4122 | `ToiletGranite` | 花岗岩马桶 | 497 | — | ·/·/· | — |
| 4123 | `ToiletMarble` | 大理石马桶 | 497 | — | ·/·/· | — |
| 4124 | `ToiletCrystal` | 水晶马桶 | 497 | — | ·/·/· | — |
| 4125 | `ToiletSpider` | 蜘蛛马桶 | 497 | — | ·/·/· | — |
| 4126 | `ToiletLesion` | 损伤马桶 | 497 | — | ·/·/· | — |
| 4127 | `ToiletDiamond` | 钻石马桶 | 497 | — | ·/·/· | — |
| 4128 | `MaidHead` | 女仆帽 | — | — | ·/·/· | — |
| 4129 | `MaidShirt` | 女仆裙 | — | — | ·/·/· | — |
| 4130 | `MaidPants` | 女仆鞋 | — | — | ·/·/· | — |
| 4131 | `VoidLens` | 虚空袋 | — | — | ·/·/· | — |
| 4132 | `MaidHead2` | 粉女仆帽 | — | — | ·/·/· | — |
| 4133 | `MaidShirt2` | 粉女仆裙 | — | — | ·/·/· | — |
| 4134 | `MaidPants2` | 粉女仆鞋 | — | — | ·/·/· | — |
| 4135 | `GolfHat` | 乡村俱乐部帽 | — | — | ·/·/· | — |
| 4136 | `GolfShirt` | 乡村俱乐部背心 | — | — | ·/·/· | — |
| 4137 | `GolfPants` | 乡村俱乐部裤 | — | — | ·/·/· | — |
| 4138 | `GolfVisor` | 乡村俱乐部面罩 | — | — | ·/·/· | — |
| 4139 | `SpiderBlock` | 蜘蛛窝块 | 498 | — | ·/·/· | — |
| 4140 | `SpiderWall` | 蜘蛛窝墙 | — | 236 | ·/·/· | — |
| 4141 | `ToiletMeteor` | 流星马桶 | 497 | — | ·/·/· | — |
| 4142 | `LesionStation` | 衰变室 | 499 | — | ·/·/· | — |
| 4144 | `Terragrim` | 泰拉魔刃 | — | — | ·/·/· | 17 |
| 4145 | `SolarBathtub` | 日耀浴缸 | 90 | — | ·/·/· | — |
| 4146 | `SolarBed` | 日耀床 | 79 | — | ·/·/· | — |
| 4147 | `SolarBookcase` | 日耀书架 | 101 | — | ·/·/· | — |
| 4148 | `SolarDresser` | 日耀梳妆台 | 88 | — | ·/·/· | — |
| 4149 | `SolarCandelabra` | 日耀烛台 | 100 | — | ·/·/· | — |
| 4150 | `SolarCandle` | 日耀蜡烛 | 33 | — | ·/·/· | — |
| 4151 | `SolarChair` | 日耀椅 | 15 | — | ·/·/· | — |
| 4152 | `SolarChandelier` | 日耀吊灯 | 34 | — | ·/·/· | — |
| 4153 | `SolarChest` | 日耀箱 | 467 | — | ·/·/· | — |
| 4154 | `SolarClock` | 日耀时钟 | 104 | — | ·/·/· | — |
| 4155 | `SolarDoor` | 日耀门 | 10 | — | ·/·/· | — |
| 4156 | `SolarLamp` | 日耀灯 | 93 | — | ·/·/· | — |
| 4157 | `SolarLantern` | 日耀灯笼 | 42 | — | ·/·/· | — |
| 4158 | `SolarPiano` | 日耀钢琴 | 87 | — | ·/·/· | — |
| 4159 | `SolarPlatform` | 日耀平台 | 19 | — | ·/·/· | — |
| 4160 | `SolarSink` | 日耀水槽 | 172 | — | ·/·/· | — |
| 4161 | `SolarSofa` | 日耀沙发 | 89 | — | ·/·/· | — |
| 4162 | `SolarTable` | 日耀桌 | 469 | — | ·/·/· | — |
| 4163 | `SolarWorkbench` | 日耀工作台 | 18 | — | ·/·/· | — |
| 4164 | `Fake_SolarChest` | 受困日耀箱 | 468 | — | ·/·/· | — |
| 4165 | `SolarToilet` | 日耀马桶 | 497 | — | ·/·/· | — |
| 4166 | `VortexBathtub` | 星旋浴缸 | 90 | — | ·/·/· | — |
| 4167 | `VortexBed` | 星旋床 | 79 | — | ·/·/· | — |
| 4168 | `VortexBookcase` | 星旋书架 | 101 | — | ·/·/· | — |
| 4169 | `VortexDresser` | 星旋梳妆台 | 88 | — | ·/·/· | — |
| 4170 | `VortexCandelabra` | 星旋烛台 | 100 | — | ·/·/· | — |
| 4171 | `VortexCandle` | 星旋蜡烛 | 33 | — | ·/·/· | — |
| 4172 | `VortexChair` | 星旋椅 | 15 | — | ·/·/· | — |
| 4173 | `VortexChandelier` | 星旋吊灯 | 34 | — | ·/·/· | — |
| 4174 | `VortexChest` | 星旋箱 | 467 | — | ·/·/· | — |
| 4175 | `VortexClock` | 星旋时钟 | 104 | — | ·/·/· | — |
| 4176 | `VortexDoor` | 星旋门 | 10 | — | ·/·/· | — |
| 4177 | `VortexLamp` | 星旋灯 | 93 | — | ·/·/· | — |
| 4178 | `VortexLantern` | 星旋灯笼 | 42 | — | ·/·/· | — |
| 4179 | `VortexPiano` | 星旋钢琴 | 87 | — | ·/·/· | — |
| 4180 | `VortexPlatform` | 星旋平台 | 19 | — | ·/·/· | — |
| 4181 | `VortexSink` | 星旋水槽 | 172 | — | ·/·/· | — |
| 4182 | `VortexSofa` | 星旋沙发 | 89 | — | ·/·/· | — |
| 4183 | `VortexTable` | 星旋桌 | 469 | — | ·/·/· | — |
| 4184 | `VortexWorkbench` | 星旋工作台 | 18 | — | ·/·/· | — |
| 4185 | `Fake_VortexChest` | 受困星旋箱 | 468 | — | ·/·/· | — |
| 4186 | `VortexToilet` | 星旋马桶 | 497 | — | ·/·/· | — |
| 4187 | `NebulaBathtub` | 星云浴缸 | 90 | — | ·/·/· | — |
| 4188 | `NebulaBed` | 星云床 | 79 | — | ·/·/· | — |
| 4189 | `NebulaBookcase` | 星云书架 | 101 | — | ·/·/· | — |
| 4190 | `NebulaDresser` | 星云梳妆台 | 88 | — | ·/·/· | — |
| 4191 | `NebulaCandelabra` | 星云烛台 | 100 | — | ·/·/· | — |
| 4192 | `NebulaCandle` | 星云蜡烛 | 33 | — | ·/·/· | — |
| 4193 | `NebulaChair` | 星云椅 | 15 | — | ·/·/· | — |
| 4194 | `NebulaChandelier` | 星云吊灯 | 34 | — | ·/·/· | — |
| 4195 | `NebulaChest` | 星云箱 | 467 | — | ·/·/· | — |
| 4196 | `NebulaClock` | 星云时钟 | 104 | — | ·/·/· | — |
| 4197 | `NebulaDoor` | 星云门 | 10 | — | ·/·/· | — |
| 4198 | `NebulaLamp` | 星云灯 | 93 | — | ·/·/· | — |
| 4199 | `NebulaLantern` | 星云灯笼 | 42 | — | ·/·/· | — |
| 4200 | `NebulaPiano` | 星云钢琴 | 87 | — | ·/·/· | — |
| 4201 | `NebulaPlatform` | 星云平台 | 19 | — | ·/·/· | — |
| 4202 | `NebulaSink` | 星云水槽 | 172 | — | ·/·/· | — |
| 4203 | `NebulaSofa` | 星云沙发 | 89 | — | ·/·/· | — |
| 4204 | `NebulaTable` | 星云桌 | 469 | — | ·/·/· | — |
| 4205 | `NebulaWorkbench` | 星云工作台 | 18 | — | ·/·/· | — |
| 4206 | `Fake_NebulaChest` | 受困星云箱 | 468 | — | ·/·/· | — |
| 4207 | `NebulaToilet` | 星云马桶 | 497 | — | ·/·/· | — |
| 4208 | `StardustBathtub` | 星尘浴缸 | 90 | — | ·/·/· | — |
| 4209 | `StardustBed` | 星尘床 | 79 | — | ·/·/· | — |
| 4210 | `StardustBookcase` | 星尘书架 | 101 | — | ·/·/· | — |
| 4211 | `StardustDresser` | 星尘梳妆台 | 88 | — | ·/·/· | — |
| 4212 | `StardustCandelabra` | 星尘烛台 | 100 | — | ·/·/· | — |
| 4213 | `StardustCandle` | 星尘蜡烛 | 33 | — | ·/·/· | — |
| 4214 | `StardustChair` | 星尘椅 | 15 | — | ·/·/· | — |
| 4215 | `StardustChandelier` | 星尘吊灯 | 34 | — | ·/·/· | — |
| 4216 | `StardustChest` | 星尘箱 | 467 | — | ·/·/· | — |
| 4217 | `StardustClock` | 星尘时钟 | 104 | — | ·/·/· | — |
| 4218 | `StardustDoor` | 星尘门 | 10 | — | ·/·/· | — |
| 4219 | `StardustLamp` | 星尘灯 | 93 | — | ·/·/· | — |
| 4220 | `StardustLantern` | 星尘灯笼 | 42 | — | ·/·/· | — |
| 4221 | `StardustPiano` | 星尘钢琴 | 87 | — | ·/·/· | — |
| 4222 | `StardustPlatform` | 星尘平台 | 19 | — | ·/·/· | — |
| 4223 | `StardustSink` | 星尘水槽 | 172 | — | ·/·/· | — |
| 4224 | `StardustSofa` | 星尘沙发 | 89 | — | ·/·/· | — |
| 4225 | `StardustTable` | 星尘桌 | 469 | — | ·/·/· | — |
| 4226 | `StardustWorkbench` | 星尘工作台 | 18 | — | ·/·/· | — |
| 4227 | `Fake_StardustChest` | 受困星尘箱 | 468 | — | ·/·/· | — |
| 4228 | `StardustToilet` | 星尘马桶 | 497 | — | ·/·/· | — |
| 4229 | `SolarBrick` | 日耀砖 | 500 | — | ·/·/· | — |
| 4230 | `VortexBrick` | 星旋砖 | 501 | — | ·/·/· | — |
| 4231 | `NebulaBrick` | 星云砖 | 502 | — | ·/·/· | — |
| 4232 | `StardustBrick` | 星尘砖 | 503 | — | ·/·/· | — |
| 4233 | `SolarBrickWall` | 日耀砖墙 | — | 237 | ·/·/· | — |
| 4234 | `VortexBrickWall` | 星旋砖墙 | — | 238 | ·/·/· | — |
| 4235 | `NebulaBrickWall` | 星云砖墙 | — | 239 | ·/·/· | — |
| 4236 | `StardustBrickWall` | 星尘砖墙 | — | 240 | ·/·/· | — |
| 4237 | `MusicBoxDayRemix` | 八音盒（日间混合曲） | 139 | — | ·/·/· | — |
| 4238 | `CrackedBlueBrick` | 破裂蓝砖 | 481 | — | ·/·/· | — |
| 4239 | `CrackedGreenBrick` | 破裂绿砖 | 482 | — | ·/·/· | — |
| 4240 | `CrackedPinkBrick` | 破裂粉砖 | 483 | — | ·/·/· | — |
| 4241 | `FlowerPacketWild` | 野花种子 | 3 | — | ·/·/· | — |
| 4242 | `GolfBallDyedBlack` | 黑高尔夫球 | — | — | ·/·/· | — |
| 4243 | `GolfBallDyedBlue` | 蓝高尔夫球 | — | — | ·/·/· | — |
| 4244 | `GolfBallDyedBrown` | 棕高尔夫球 | — | — | ·/·/· | — |
| 4245 | `GolfBallDyedCyan` | 青高尔夫球 | — | — | ·/·/· | — |
| 4246 | `GolfBallDyedGreen` | 绿高尔夫球 | — | — | ·/·/· | — |
| 4247 | `GolfBallDyedLimeGreen` | 橙绿高尔夫球 | — | — | ·/·/· | — |
| 4248 | `GolfBallDyedOrange` | 橙高尔夫球 | — | — | ·/·/· | — |
| 4249 | `GolfBallDyedPink` | 粉高尔夫球 | — | — | ·/·/· | — |
| 4250 | `GolfBallDyedPurple` | 紫高尔夫球 | — | — | ·/·/· | — |
| 4251 | `GolfBallDyedRed` | 红高尔夫球 | — | — | ·/·/· | — |
| 4252 | `GolfBallDyedSkyBlue` | 天蓝高尔夫球 | — | — | ·/·/· | — |
| 4253 | `GolfBallDyedTeal` | 青绿高尔夫球 | — | — | ·/·/· | — |
| 4254 | `GolfBallDyedViolet` | 蓝紫高尔夫球 | — | — | ·/·/· | — |
| 4255 | `GolfBallDyedYellow` | 黄高尔夫球 | — | — | ·/·/· | — |
| 4256 | `AmberRobe` | 琥珀长袍 | — | — | ·/·/· | — |
| 4257 | `AmberHook` | 琥珀钩 | — | — | ·/·/· | — |
| 4258 | `OrangePhaseblade` | 橙陨石光剑 | — | — | ·/·/· | 26 |
| 4259 | `OrangePhasesaber` | 橙晶光刃 | — | — | ·/·/· | 48 |
| 4260 | `OrangeStainedGlass` | 橙花窗玻璃 | — | 241 | ·/·/· | — |
| 4261 | `OrangePressurePlate` | 橙压力板 | 135 | — | ·/·/· | — |
| 4262 | `MysticCoilSnake` | 耍蛇者长笛 | — | — | ·/·/· | — |
| 4263 | `MagicConch` | 魔法海螺 | — | — | ·/·/· | — |
| 4264 | `GolfCart` | 高尔夫球车钥匙 | — | — | ·/·/· | — |
| 4265 | `GolfChest` | 高尔夫宝箱 | 467 | — | ·/·/· | — |
| 4266 | `Fake_GolfChest` | 受困高尔夫宝箱 | 468 | — | ·/·/· | — |
| 4267 | `DesertChest` | 沙岩宝箱 | 467 | — | ·/·/· | — |
| 4268 | `Fake_DesertChest` | 受困沙岩箱 | 468 | — | ·/·/· | — |
| 4269 | `SanguineStaff` | 血红法杖 | — | — | ·/·/· | 35 |
| 4270 | `SharpTears` | 血荆棘 | — | — | ·/·/· | 34 |
| 4271 | `BloodMoonStarter` | 血泪 | — | — | ·/·/· | — |
| 4272 | `DripplerFlail` | 滴滴怪跛子 | — | — | ·/·/· | 55 |
| 4273 | `VampireFrogStaff` | 吸血鬼青蛙法杖 | — | — | ·/·/· | 11 |
| 4274 | `GoldGoldfish` | 金金鱼 | — | — | ·/·/· | — |
| 4275 | `GoldGoldfishBowl` | 金鱼缸 | 505 | — | ·/·/· | — |
| 4276 | `CatBast` | 韧皮雕像 | 506 | — | ·/·/· | — |
| 4277 | `GoldStarryGlassBlock` | 金星块 | 507 | — | ·/·/· | — |
| 4278 | `BlueStarryGlassBlock` | 蓝星块 | 508 | — | ·/·/· | — |
| 4279 | `GoldStarryGlassWall` | 金星墙 | — | 242 | ·/·/· | — |
| 4280 | `BlueStarryGlassWall` | 蓝星墙 | — | 243 | ·/·/· | — |
| 4281 | `BabyBirdStaff` | 雀杖 | — | — | ·/·/· | 7 |
| 4282 | `Apricot` | 杏 | — | — | ·/·/· | — |
| 4283 | `Banana` | 香蕉 | — | — | ·/·/· | — |
| 4284 | `BlackCurrant` | 黑醋栗 | — | — | ·/·/· | — |
| 4285 | `BloodOrange` | 血橙 | — | — | ·/·/· | — |
| 4286 | `Cherry` | 樱桃 | — | — | ·/·/· | — |
| 4287 | `Coconut` | 椰子 | — | — | ·/·/· | — |
| 4288 | `Dragonfruit` | 火龙果 | — | — | ·/·/· | — |
| 4289 | `Elderberry` | 接骨木果 | — | — | ·/·/· | — |
| 4290 | `Grapefruit` | 葡萄柚 | — | — | ·/·/· | — |
| 4291 | `Lemon` | 柠檬 | — | — | ·/·/· | — |
| 4292 | `Mango` | 芒果 | — | — | ·/·/· | — |
| 4293 | `Peach` | 桃子 | — | — | ·/·/· | — |
| 4294 | `Pineapple` | 菠萝 | — | — | ·/·/· | — |
| 4295 | `Plum` | 李子 | — | — | ·/·/· | — |
| 4296 | `Rambutan` | 红毛丹 | — | — | ·/·/· | — |
| 4297 | `Starfruit` | 杨桃 | — | — | ·/·/· | — |
| 4298 | `SandstoneBathtub` | 沙岩浴缸 | 90 | — | ·/·/· | — |
| 4299 | `SandstoneBed` | 沙岩床 | 79 | — | ·/·/· | — |
| 4300 | `SandstoneBookcase` | 沙岩书架 | 101 | — | ·/·/· | — |
| 4301 | `SandstoneDresser` | 沙岩梳妆台 | 88 | — | ·/·/· | — |
| 4302 | `SandstoneCandelabra` | 沙岩烛台 | 100 | — | ·/·/· | — |
| 4303 | `SandstoneCandle` | 沙岩蜡烛 | 33 | — | ·/·/· | — |
| 4304 | `SandstoneChair` | 沙岩椅 | 15 | — | ·/·/· | — |
| 4305 | `SandstoneChandelier` | 沙岩吊灯 | 34 | — | ·/·/· | — |
| 4306 | `SandstoneClock` | 沙岩时钟 | 104 | — | ·/·/· | — |
| 4307 | `SandstoneDoor` | 沙岩门 | 10 | — | ·/·/· | — |
| 4308 | `SandstoneLamp` | 沙岩灯 | 93 | — | ·/·/· | — |
| 4309 | `SandstoneLantern` | 沙岩灯笼 | 42 | — | ·/·/· | — |
| 4310 | `SandstonePiano` | 沙岩钢琴 | 87 | — | ·/·/· | — |
| 4311 | `SandstonePlatform` | 沙岩平台 | 19 | — | ·/·/· | — |
| 4312 | `SandstoneSink` | 沙岩水槽 | 172 | — | ·/·/· | — |
| 4313 | `SandstoneSofa` | 沙岩沙发 | 89 | — | ·/·/· | — |
| 4314 | `SandstoneTable` | 沙岩桌 | 469 | — | ·/·/· | — |
| 4315 | `SandstoneWorkbench` | 沙岩工作台 | 18 | — | ·/·/· | — |
| 4316 | `SandstoneToilet` | 沙岩马桶 | 497 | — | ·/·/· | — |
| 4317 | `BloodHamaxe` | 锤斧 | — | — | ·/30/80 | 30 |
| 4318 | `VoidMonolith` | 虚空巨石 | 509 | — | ·/·/· | — |
| 4319 | `ArrowSign` | 箭头符号 | 510 | — | ·/·/· | — |
| 4320 | `PaintedArrowSign` | 彩色箭头符号 | 511 | — | ·/·/· | — |
| 4321 | `GameMasterShirt` | 大师级玩家夹克 | — | — | ·/·/· | — |
| 4322 | `GameMasterPants` | 大师级玩家裤 | — | — | ·/·/· | — |
| 4323 | `StarPrincessCrown` | 星星公主王冠 | — | — | ·/·/· | — |
| 4324 | `StarPrincessDress` | 星星公主裙 | — | — | ·/·/· | — |
| 4325 | `BloodFishingRod` | 鱼饵投掷者 | — | — | ·/·/· | — |
| 4326 | `FoodPlatter` | 盘子 | 520 | — | ·/·/· | — |
| 4327 | `BlackDragonflyJar` | 黑蜻蜓罐 | 521 | — | ·/·/· | — |
| 4328 | `BlueDragonflyJar` | 蓝蜻蜓罐 | 522 | — | ·/·/· | — |
| 4329 | `GreenDragonflyJar` | 绿蜻蜓罐 | 523 | — | ·/·/· | — |
| 4330 | `OrangeDragonflyJar` | 橙蜻蜓罐 | 524 | — | ·/·/· | — |
| 4331 | `RedDragonflyJar` | 红蜻蜓罐 | 525 | — | ·/·/· | — |
| 4332 | `YellowDragonflyJar` | 黄蜻蜓罐 | 526 | — | ·/·/· | — |
| 4333 | `GoldDragonflyJar` | 金蜻蜓罐 | 527 | — | ·/·/· | — |
| 4334 | `BlackDragonfly` | 黑蜻蜓 | — | — | ·/·/· | — |
| 4335 | `BlueDragonfly` | 蓝蜻蜓 | — | — | ·/·/· | — |
| 4336 | `GreenDragonfly` | 绿蜻蜓 | — | — | ·/·/· | — |
| 4337 | `OrangeDragonfly` | 橙蜻蜓 | — | — | ·/·/· | — |
| 4338 | `RedDragonfly` | 红蜻蜓 | — | — | ·/·/· | — |
| 4339 | `YellowDragonfly` | 黄蜻蜓 | — | — | ·/·/· | — |
| 4340 | `GoldDragonfly` | 金蜻蜓 | — | — | ·/·/· | — |
| 4341 | `PortableStool` | 梯凳 | — | — | ·/·/· | — |
| 4342 | `DragonflyStatue` | 蜻蜓雕像 | 105 | — | ·/·/· | — |
| 4343 | `PaperAirplaneA` | 纸飞机 | — | — | ·/·/· | 4 |
| 4344 | `PaperAirplaneB` | 白纸飞机 | — | — | ·/·/· | 4 |
| 4345 | `CanOfWorms` | 蠕虫罐头 | — | — | ·/·/· | — |
| 4346 | `EncumberingStone` | 障碍石 | — | — | ·/·/· | — |
| 4347 | `ZapinatorGray` | 灰冲击枪 | — | — | ·/·/· | 42 |
| 4348 | `ZapinatorOrange` | 橙冲击枪 | — | — | ·/·/· | 100 |
| 4349 | `GreenMoss` | 绿苔藓 | 179 | — | ·/·/· | — |
| 4350 | `BrownMoss` | 棕苔藓 | 180 | — | ·/·/· | — |
| 4351 | `RedMoss` | 红苔藓 | 181 | — | ·/·/· | — |
| 4352 | `BlueMoss` | 蓝苔藓 | 182 | — | ·/·/· | — |
| 4353 | `PurpleMoss` | 紫苔藓 | 183 | — | ·/·/· | — |
| 4354 | `LavaMoss` | 熔岩苔藓 | 381 | — | ·/·/· | — |
| 4355 | `BoulderStatue` | 巨石雕像 | 531 | — | ·/·/· | — |
| 4356 | `MusicBoxTitleAlt` | 八音盒（旅程开始） | 139 | — | ·/·/· | — |
| 4357 | `MusicBoxStorm` | 八音盒（暴风雨） | 139 | — | ·/·/· | — |
| 4358 | `MusicBoxGraveyard` | 八音盒（墓地） | 139 | — | ·/·/· | — |
| 4359 | `Seagull` | 海鸥 | — | — | ·/·/· | — |
| 4360 | `SeagullStatue` | 海鸥雕像 | 105 | — | ·/·/· | — |
| 4361 | `LadyBug` | 瓢虫 | — | — | ·/·/· | — |
| 4362 | `GoldLadyBug` | 金瓢虫 | — | — | ·/·/· | — |
| 4363 | `Maggot` | 蝇蛆 | — | — | ·/·/· | — |
| 4364 | `MaggotCage` | 蝇蛆笼 | 532 | — | ·/·/· | — |
| 4365 | `CelestialWand` | 天界魔棒 | — | — | ·/·/· | — |
| 4366 | `EucaluptusSap` | 桉树汁 | — | — | ·/·/· | — |
| 4367 | `KiteBlue` | 蓝风筝 | — | — | ·/·/· | — |
| 4368 | `KiteBlueAndYellow` | 蓝黄风筝 | — | — | ·/·/· | — |
| 4369 | `KiteRed` | 红风筝 | — | — | ·/·/· | — |
| 4370 | `KiteRedAndYellow` | 红黄风筝 | — | — | ·/·/· | — |
| 4371 | `KiteYellow` | 黄风筝 | — | — | ·/·/· | — |
| 4372 | `IvyGuitar` | 常春藤 | — | — | ·/·/· | — |
| 4373 | `Pupfish` | 鳉鱼 | — | — | ·/·/· | — |
| 4374 | `Grebe` | 䴙䴘 | — | — | ·/·/· | — |
| 4375 | `Rat` | 大鼠 | — | — | ·/·/· | — |
| 4376 | `RatCage` | 大鼠笼 | 533 | — | ·/·/· | — |
| 4377 | `KryptonMoss` | 氪苔藓 | 534 | — | ·/·/· | — |
| 4378 | `XenonMoss` | 氙苔藓 | 536 | — | ·/·/· | — |
| 4379 | `KiteWyvern` | 飞龙风筝 | — | — | ·/·/· | — |
| 4380 | `LadybugCage` | 瓢虫笼 | 538 | — | ·/·/· | — |
| 4381 | `BloodRainBow` | 血雨弓 | — | — | ·/·/· | 14 |
| 4382 | `CombatBook` | 先进战斗技术 | — | — | ·/·/· | — |
| 4383 | `DesertTorch` | 沙漠火把 | 4 | — | ·/·/· | — |
| 4384 | `CoralTorch` | 珊瑚火把 | 4 | — | ·/·/· | — |
| 4385 | `CorruptTorch` | 腐化火把 | 4 | — | ·/·/· | — |
| 4386 | `CrimsonTorch` | 猩红火把 | 4 | — | ·/·/· | — |
| 4387 | `HallowedTorch` | 神圣火把 | 4 | — | ·/·/· | — |
| 4388 | `JungleTorch` | 丛林火把 | 4 | — | ·/·/· | — |
| 4389 | `ArgonMoss` | 氩苔藓 | 539 | — | ·/·/· | — |
| 4390 | `RollingCactus` | 仙人球 | 484 | — | ·/·/· | — |
| 4391 | `ThinIce` | 薄冰 | 162 | — | ·/·/· | — |
| 4392 | `EchoBlock` | 回声块 | 541 | — | ·/·/· | — |
| 4393 | `ScarabFish` | 甲虫鱼 | — | — | ·/·/· | — |
| 4394 | `ScorpioFish` | 蝎子鱼 | — | — | ·/·/· | — |
| 4395 | `Owl` | 猫头鹰 | — | — | ·/·/· | — |
| 4396 | `OwlCage` | 猫头鹰笼 | 542 | — | ·/·/· | — |
| 4397 | `OwlStatue` | 猫头鹰雕像 | 105 | — | ·/·/· | — |
| 4398 | `PupfishBowl` | 鳉鱼缸 | 543 | — | ·/·/· | — |
| 4399 | `GoldLadybugCage` | 金瓢虫笼 | 544 | — | ·/·/· | — |
| 4400 | `Geode` | 晶洞 | — | — | ·/·/· | — |
| 4401 | `Flounder` | 偏口鱼 | — | — | ·/·/· | — |
| 4402 | `RockLobster` | 岩石龙虾 | — | — | ·/·/· | — |
| 4403 | `LobsterTail` | 龙虾尾 | — | — | ·/·/· | — |
| 4404 | `FloatingTube` | 浮游圈 | — | — | ·/·/· | — |
| 4405 | `FrozenCrate` | 冰冻匣 | 376 | — | ·/·/· | — |
| 4406 | `FrozenCrateHard` | 针叶木匣 | 376 | — | ·/·/· | — |
| 4407 | `OasisCrate` | 绿洲匣 | 376 | — | ·/·/· | — |
| 4408 | `OasisCrateHard` | 幻象匣 | 376 | — | ·/·/· | — |
| 4409 | `SpectreGoggles` | 幽灵护目镜 | — | — | ·/·/· | — |
| 4410 | `Oyster` | 牡蛎 | — | — | ·/·/· | — |
| 4411 | `ShuckedOyster` | 去壳牡蛎 | — | — | ·/·/· | — |
| 4412 | `WhitePearl` | 白珍珠 | — | — | ·/·/· | — |
| 4413 | `BlackPearl` | 黑珍珠 | — | — | ·/·/· | — |
| 4414 | `PinkPearl` | 粉珍珠 | — | — | ·/·/· | — |
| 4415 | `StoneDoor` | 石门 | 10 | — | ·/·/· | — |
| 4416 | `StonePlatform` | 石平台 | 19 | — | ·/·/· | — |
| 4417 | `OasisFountain` | 绿洲喷泉 | 207 | — | ·/·/· | — |
| 4418 | `WaterStrider` | 水黾 | — | — | ·/·/· | — |
| 4419 | `GoldWaterStrider` | 金水黾 | — | — | ·/·/· | — |
| 4420 | `LawnFlamingo` | 草坪火烈鸟 | 545 | — | ·/·/· | — |
| 4421 | `MusicBoxUndergroundJungle` | 八音盒（地下丛林） | 139 | — | ·/·/· | — |
| 4422 | `Grate` | 格栅 | 546 | — | ·/·/· | — |
| 4423 | `ScarabBomb` | 甲虫炸弹 | — | — | ·/·/· | — |
| 4424 | `WroughtIronFence` | 锻铁栅栏 | — | 245 | ·/·/· | — |
| 4425 | `SharkBait` | 鲨鱼鱼饵 | — | — | ·/·/· | — |
| 4426 | `BeeMinecart` | 蜜蜂矿车 | — | — | ·/·/· | — |
| 4427 | `LadybugMinecart` | 瓢虫矿车 | — | — | ·/·/· | — |
| 4428 | `PigronMinecart` | 猪龙矿车 | — | — | ·/·/· | — |
| 4429 | `SunflowerMinecart` | 向日葵矿车 | — | — | ·/·/· | — |
| 4430 | `PottedForestCedar` | 盆栽森林雪松 | 547 | — | ·/·/· | — |
| 4431 | `PottedJungleCedar` | 盆栽丛林雪松 | 547 | — | ·/·/· | — |
| 4432 | `PottedHallowCedar` | 盆栽神圣雪松 | 547 | — | ·/·/· | — |
| 4433 | `PottedForestTree` | 盆栽林木 | 547 | — | ·/·/· | — |
| 4434 | `PottedJungleTree` | 盆栽丛林树 | 547 | — | ·/·/· | — |
| 4435 | `PottedHallowTree` | 盆栽神圣树 | 548 | — | ·/·/· | — |
| 4436 | `PottedForestPalm` | 盆栽森林棕榈 | 548 | — | ·/·/· | — |
| 4437 | `PottedJunglePalm` | 盆栽丛林棕榈 | 548 | — | ·/·/· | — |
| 4438 | `PottedHallowPalm` | 盆栽神圣棕榈 | 548 | — | ·/·/· | — |
| 4439 | `PottedForestBamboo` | 盆栽森林竹 | 548 | — | ·/·/· | — |
| 4440 | `PottedJungleBamboo` | 盆栽丛林竹 | 548 | — | ·/·/· | — |
| 4441 | `PottedHallowBamboo` | 盆栽神圣竹 | 548 | — | ·/·/· | — |
| 4442 | `ScarabFishingRod` | 甲虫钓竿 | — | — | ·/·/· | — |
| 4443 | `HellMinecart` | 恶魔地狱车 | — | — | ·/·/· | — |
| 4444 | `WitchBroom` | 女巫扫帚 | — | — | ·/·/· | — |
| 4445 | `ClusterRocketI` | 一级集束火箭 | — | — | ·/·/· | 50 |
| 4446 | `ClusterRocketII` | 二级集束火箭 | — | — | ·/·/· | 50 |
| 4447 | `WetRocket` | 潮湿火箭 | — | — | ·/·/· | 40 |
| 4448 | `LavaRocket` | 熔岩火箭 | — | — | ·/·/· | 40 |
| 4449 | `HoneyRocket` | 蜂蜜火箭 | — | — | ·/·/· | 40 |
| 4450 | `ShroomMinecart` | 蘑菇矿车 | — | — | ·/·/· | — |
| 4451 | `AmethystMinecart` | 紫晶矿车 | — | — | ·/·/· | — |
| 4452 | `TopazMinecart` | 黄玉矿车 | — | — | ·/·/· | — |
| 4453 | `SapphireMinecart` | 蓝玉矿车 | — | — | ·/·/· | — |
| 4454 | `EmeraldMinecart` | 翡翠矿车 | — | — | ·/·/· | — |
| 4455 | `RubyMinecart` | 红玉矿车 | — | — | ·/·/· | — |
| 4456 | `DiamondMinecart` | 钻石矿车 | — | — | ·/·/· | — |
| 4457 | `MiniNukeI` | 一级迷你核弹 | — | — | ·/·/· | 75 |
| 4458 | `MiniNukeII` | 二级迷你核弹 | — | — | ·/·/· | 75 |
| 4459 | `DryRocket` | 干火箭 | — | — | ·/·/· | 40 |
| 4460 | `SandcastleBucket` | 沙堡桶 | 552 | — | ·/·/· | — |
| 4461 | `TurtleCage` | 龟笼 | 550 | — | ·/·/· | — |
| 4462 | `TurtleJungleCage` | 丛林龟笼 | 551 | — | ·/·/· | — |
| 4463 | `Gladius` | 罗马短剑 | — | — | ·/·/· | 15 |
| 4464 | `Turtle` | 龟 | — | — | ·/·/· | — |
| 4465 | `TurtleJungle` | 丛林龟 | — | — | ·/·/· | — |
| 4466 | `TurtleStatue` | 龟雕像 | 105 | — | ·/·/· | — |
| 4467 | `AmberMinecart` | 琥珀矿车 | — | — | ·/·/· | — |
| 4468 | `BeetleMinecart` | 甲虫矿车 | — | — | ·/·/· | — |
| 4469 | `MeowmereMinecart` | 彩虹猫之刃矿车 | — | — | ·/·/· | — |
| 4470 | `PartyMinecart` | 派对手推车 | — | — | ·/·/· | — |
| 4471 | `PirateMinecart` | 荷兰人 | — | — | ·/·/· | — |
| 4472 | `SteampunkMinecart` | 蒸汽朋克矿车 | — | — | ·/·/· | — |
| 4473 | `GrebeCage` | 䴙䴘笼 | 553 | — | ·/·/· | — |
| 4474 | `SeagullCage` | 海鸥笼 | 554 | — | ·/·/· | — |
| 4475 | `WaterStriderCage` | 水黾笼 | 555 | — | ·/·/· | — |
| 4476 | `GoldWaterStriderCage` | 金水黾笼 | 556 | — | ·/·/· | — |
| 4477 | `LuckPotionLesser` | 弱效幸运药水 | — | — | ·/·/· | — |
| 4478 | `LuckPotion` | 幸运药水 | — | — | ·/·/· | — |
| 4479 | `LuckPotionGreater` | 强效幸运药水 | — | — | ·/·/· | — |
| 4480 | `Seahorse` | 海马 | — | — | ·/·/· | — |
| 4481 | `SeahorseCage` | 海马笼 | 558 | — | ·/·/· | — |
| 4482 | `GoldSeahorse` | 金海马 | — | — | ·/·/· | — |
| 4483 | `GoldSeahorseCage` | 金海马笼 | 559 | — | ·/·/· | — |
| 4484 | `TimerOneHalfSecond` | 1/2秒计时器 | 144 | — | ·/·/· | — |
| 4485 | `TimerOneFourthSecond` | 1/4秒计时器 | 144 | — | ·/·/· | — |
| 4486 | `EbonstoneEcho` | 黑檀石墙 | — | 246 | ·/·/· | — |
| 4487 | `MudWallEcho` | 泥墙 | — | 247 | ·/·/· | — |
| 4488 | `PearlstoneEcho` | 珍珠石墙 | — | 248 | ·/·/· | — |
| 4489 | `SnowWallEcho` | 雪墙 | — | 249 | ·/·/· | — |
| 4490 | `AmethystEcho` | 紫晶石墙 | — | 250 | ·/·/· | — |
| 4491 | `TopazEcho` | 黄玉石墙 | — | 251 | ·/·/· | — |
| 4492 | `SapphireEcho` | 蓝玉石墙 | — | 252 | ·/·/· | — |
| 4493 | `EmeraldEcho` | 翡翠石墙 | — | 253 | ·/·/· | — |
| 4494 | `RubyEcho` | 红玉石墙 | — | 254 | ·/·/· | — |
| 4495 | `DiamondEcho` | 钻石石墙 | — | 255 | ·/·/· | — |
| 4496 | `Cave1Echo` | 绿苔藓墙 | — | 256 | ·/·/· | — |
| 4497 | `Cave2Echo` | 棕苔藓墙 | — | 257 | ·/·/· | — |
| 4498 | `Cave3Echo` | 红苔藓墙 | — | 258 | ·/·/· | — |
| 4499 | `Cave4Echo` | 蓝苔藓墙 | — | 259 | ·/·/· | — |
| 4500 | `Cave5Echo` | 紫苔藓墙 | — | 260 | ·/·/· | — |
| 4501 | `Cave6Echo` | 岩石土墙 | — | 261 | ·/·/· | — |
| 4502 | `Cave7Echo` | 古老石墙 | — | 262 | ·/·/· | — |
| 4503 | `SpiderEcho` | 蜘蛛墙 | — | 263 | ·/·/· | — |
| 4504 | `CorruptGrassEcho` | 腐化草墙 | — | 264 | ·/·/· | — |
| 4505 | `HallowedGrassEcho` | 神圣草墙 | — | 265 | ·/·/· | — |
| 4506 | `IceEcho` | 冰雪墙 | — | 266 | ·/·/· | — |
| 4507 | `ObsidianBackEcho` | 黑曜石墙 | — | 267 | ·/·/· | — |
| 4508 | `CrimsonGrassEcho` | 猩红草墙 | — | 268 | ·/·/· | — |
| 4509 | `CrimstoneEcho` | 猩红石墙 | — | 269 | ·/·/· | — |
| 4510 | `CaveWall1Echo` | 洞穴土墙 | — | 270 | ·/·/· | — |
| 4511 | `CaveWall2Echo` | 粗糙土墙 | — | 271 | ·/·/· | — |
| 4512 | `Cave8Echo` | 陡峭石墙 | — | 274 | ·/·/· | — |
| 4513 | `Corruption1Echo` | 腐化生长墙 | — | 276 | ·/·/· | — |
| 4514 | `Corruption2Echo` | 腐化弥撒墙 | — | 277 | ·/·/· | — |
| 4515 | `Corruption3Echo` | 腐化脓疱墙 | — | 278 | ·/·/· | — |
| 4516 | `Corruption4Echo` | 腐化卷须墙 | — | 279 | ·/·/· | — |
| 4517 | `Crimson1Echo` | 猩红硬皮墙 | — | 280 | ·/·/· | — |
| 4518 | `Crimson2Echo` | 猩红痂墙 | — | 281 | ·/·/· | — |
| 4519 | `Crimson3Echo` | 猩红牙墙 | — | 282 | ·/·/· | — |
| 4520 | `Crimson4Echo` | 猩红水疱墙 | — | 283 | ·/·/· | — |
| 4521 | `Dirt1Echo` | 分层土墙 | — | 284 | ·/·/· | — |
| 4522 | `Dirt2Echo` | 崩塌土墙 | — | 285 | ·/·/· | — |
| 4523 | `Dirt3Echo` | 破裂土墙 | — | 286 | ·/·/· | — |
| 4524 | `Dirt4Echo` | 波动土墙 | — | 287 | ·/·/· | — |
| 4525 | `Hallow1Echo` | 神圣棱镜墙 | — | 288 | ·/·/· | — |
| 4526 | `Hallow2Echo` | 神圣洞穴墙 | — | 289 | ·/·/· | — |
| 4527 | `Hallow3Echo` | 神圣碎片墙 | — | 290 | ·/·/· | — |
| 4528 | `Hallow4Echo` | 神圣水晶墙 | — | 291 | ·/·/· | — |
| 4529 | `Jungle1Echo` | 地衣石墙 | — | 292 | ·/·/· | — |
| 4530 | `Jungle2Echo` | 多叶丛林墙 | — | 293 | ·/·/· | — |
| 4531 | `Jungle3Echo` | 常春藤石墙 | — | 294 | ·/·/· | — |
| 4532 | `Jungle4Echo` | 丛林藤蔓墙 | — | 295 | ·/·/· | — |
| 4533 | `Lava1Echo` | 余烬墙 | — | 296 | ·/·/· | — |
| 4534 | `Lava2Echo` | 煤渣墙 | — | 297 | ·/·/· | — |
| 4535 | `Lava3Echo` | 岩浆墙 | — | 298 | ·/·/· | — |
| 4536 | `Lava4Echo` | 阴燃石墙 | — | 299 | ·/·/· | — |
| 4537 | `Rocks1Echo` | 磨损石墙 | — | 300 | ·/·/· | — |
| 4538 | `Rocks2Echo` | 钟乳石墙 | — | 301 | ·/·/· | — |
| 4539 | `Rocks3Echo` | 斑驳石墙 | — | 302 | ·/·/· | — |
| 4540 | `Rocks4Echo` | 断裂石墙 | — | 303 | ·/·/· | — |
| 4541 | `TheBrideBanner` | 新娘旗 | 91 | — | ·/·/· | — |
| 4542 | `ZombieMermanBanner` | 僵尸人鱼旗 | 91 | — | ·/·/· | — |
| 4543 | `EyeballFlyingFishBanner` | 游荡眼球怪鱼旗 | 91 | — | ·/·/· | — |
| 4544 | `BloodSquidBanner` | 血乌贼旗 | 91 | — | ·/·/· | — |
| 4545 | `BloodEelBanner` | 血鳗鱼旗 | 91 | — | ·/·/· | — |
| 4546 | `GoblinSharkBanner` | 血浆哥布林鲨鱼旗 | 91 | — | ·/·/· | — |
| 4547 | `LargeBambooBlock` | 大竹子 | 563 | — | ·/·/· | — |
| 4548 | `LargeBambooBlockWall` | 大竹墙 | — | 313 | ·/·/· | — |
| 4549 | `DemonHorns` | 恶魔角 | — | — | ·/·/· | — |
| 4550 | `BambooLeaf` | 竹叶 | — | — | ·/·/· | — |
| 4551 | `HellCake` | 地狱蛋糕块 | — | — | ·/·/· | — |
| 4552 | `FogMachine` | 烟雾机 | 565 | — | ·/·/· | — |
| 4553 | `PlasmaLamp` | 等离子灯 | 564 | — | ·/·/· | — |
| 4554 | `MarbleColumn` | 大理石柱 | 561 | — | ·/·/· | — |
| 4555 | `ChefHat` | 厨师帽 | — | — | ·/·/· | — |
| 4556 | `ChefShirt` | 厨师服 | — | — | ·/·/· | — |
| 4557 | `ChefPants` | 厨师裤 | — | — | ·/·/· | — |
| 4558 | `StarHairpin` | 星星发夹 | — | — | ·/·/· | — |
| 4559 | `HeartHairpin` | 心形发夹 | — | — | ·/·/· | — |
| 4560 | `BunnyEars` | 兔耳朵 | — | — | ·/·/· | — |
| 4561 | `DevilHorns` | 魔鬼角 | — | — | ·/·/· | — |
| 4562 | `Fedora` | 软呢帽 | — | — | ·/·/· | — |
| 4563 | `UnicornHornHat` | 假独角兽角 | — | — | ·/·/· | — |
| 4564 | `BambooBlock` | 竹子 | 562 | — | ·/·/· | — |
| 4565 | `BambooBlockWall` | 竹墙 | — | 312 | ·/·/· | — |
| 4566 | `BambooBathtub` | 竹浴缸 | 90 | — | ·/·/· | — |
| 4567 | `BambooBed` | 竹床 | 79 | — | ·/·/· | — |
| 4568 | `BambooBookcase` | 竹书架 | 101 | — | ·/·/· | — |
| 4569 | `BambooDresser` | 竹梳妆台 | 88 | — | ·/·/· | — |
| 4570 | `BambooCandelabra` | 竹烛台 | 100 | — | ·/·/· | — |
| 4571 | `BambooCandle` | 竹蜡烛 | 33 | — | ·/·/· | — |
| 4572 | `BambooChair` | 竹椅 | 15 | — | ·/·/· | — |
| 4573 | `BambooChandelier` | 竹吊灯 | 34 | — | ·/·/· | — |
| 4574 | `BambooChest` | 竹宝箱 | 467 | — | ·/·/· | — |
| 4575 | `BambooClock` | 竹时钟 | 104 | — | ·/·/· | — |
| 4576 | `BambooDoor` | 竹门 | 10 | — | ·/·/· | — |
| 4577 | `BambooLamp` | 竹灯 | 93 | — | ·/·/· | — |
| 4578 | `BambooLantern` | 竹灯笼 | 42 | — | ·/·/· | — |
| 4579 | `BambooPiano` | 竹钢琴 | 87 | — | ·/·/· | — |
| 4580 | `BambooPlatform` | 竹平台 | 19 | — | ·/·/· | — |
| 4581 | `BambooSink` | 竹水槽 | 172 | — | ·/·/· | — |
| 4582 | `BambooSofa` | 竹沙发 | 89 | — | ·/·/· | — |
| 4583 | `BambooTable` | 竹桌 | 469 | — | ·/·/· | — |
| 4584 | `BambooWorkbench` | 竹工作台 | 18 | — | ·/·/· | — |
| 4585 | `Fake_BambooChest` | 受困竹宝箱 | 468 | — | ·/·/· | — |
| 4586 | `BambooToilet` | 竹马桶 | 497 | — | ·/·/· | — |
| 4587 | `GolfClubStoneIron` | 磨损高尔夫球杆（铁杆） | — | — | ·/·/· | — |
| 4588 | `GolfClubRustyPutter` | 磨损高尔夫球杆（推杆） | — | — | ·/·/· | — |
| 4589 | `GolfClubBronzeWedge` | 磨损高尔夫球杆（挖起杆） | — | — | ·/·/· | — |
| 4590 | `GolfClubWoodDriver` | 磨损高尔夫球杆（木杆） | — | — | ·/·/· | — |
| 4591 | `GolfClubMythrilIron` | 精致高尔夫球杆（铁杆） | — | — | ·/·/· | — |
| 4592 | `GolfClubLeadPutter` | 精致高尔夫球杆（推杆） | — | — | ·/·/· | — |
| 4593 | `GolfClubGoldWedge` | 精致高尔夫球杆（挖起杆） | — | — | ·/·/· | — |
| 4594 | `GolfClubPearlwoodDriver` | 精致高尔夫球杆（木杆） | — | — | ·/·/· | — |
| 4595 | `GolfClubTitaniumIron` | 高级高尔夫球杆（铁杆） | — | — | ·/·/· | — |
| 4596 | `GolfClubShroomitePutter` | 高级高尔夫球杆（推杆） | — | — | ·/·/· | — |
| 4597 | `GolfClubDiamondWedge` | 高级高尔夫球杆（挖起杆） | — | — | ·/·/· | — |
| 4598 | `GolfClubChlorophyteDriver` | 高级高尔夫球杆（木杆） | — | — | ·/·/· | — |
| 4599 | `GolfTrophyBronze` | 高尔夫青铜纪念章 | 560 | — | ·/·/· | — |
| 4600 | `GolfTrophySilver` | 高尔夫银纪念章 | 560 | — | ·/·/· | — |
| 4601 | `GolfTrophyGold` | 高尔夫金纪念章 | 560 | — | ·/·/· | — |
| 4602 | `BloodNautilusBanner` | 恐惧鹦鹉螺旗 | 91 | — | ·/·/· | — |
| 4603 | `BirdieRattle` | 小鸟嘎嘎 | — | — | ·/·/· | — |
| 4604 | `ExoticEasternChewToy` | 奇异咀嚼玩具 | — | — | ·/·/· | — |
| 4605 | `BedazzledNectar` | 眩晕花蜜 | — | — | ·/·/· | — |
| 4606 | `MusicBoxJungleNight` | 八音盒（夜间丛林） | 139 | — | ·/·/· | — |
| 4607 | `StormTigerStaff` | 沙漠虎杖 | — | — | ·/·/· | 41 |
| 4608 | `ChumBucket` | 鱼饵桶 | — | — | ·/·/· | — |
| 4609 | `GardenGnome` | 花园侏儒 | 567 | — | ·/·/· | — |
| 4610 | `KiteBoneSerpent` | 骨蛇风筝 | — | — | ·/·/· | — |
| 4611 | `KiteWorldFeeder` | 吞世怪风筝 | — | — | ·/·/· | — |
| 4612 | `KiteBunny` | 兔兔风筝 | — | — | ·/·/· | — |
| 4613 | `KitePigron` | 猪龙风筝 | — | — | ·/·/· | — |
| 4614 | `AppleJuice` | 苹果汁 | — | — | ·/·/· | — |
| 4615 | `GrapeJuice` | 葡萄汁 | — | — | ·/·/· | — |
| 4616 | `Lemonade` | 柠檬水 | — | — | ·/·/· | — |
| 4617 | `BananaDaiquiri` | 冰冻香蕉代基里 | — | — | ·/·/· | — |
| 4618 | `PeachSangria` | 桃子果酒 | — | — | ·/·/· | — |
| 4619 | `PinaColada` | 椰林飘香 | — | — | ·/·/· | — |
| 4620 | `TropicalSmoothie` | 热带奶昔 | — | — | ·/·/· | — |
| 4621 | `BloodyMoscato` | 血腥麝香葡萄 | — | — | ·/·/· | — |
| 4622 | `SmoothieofDarkness` | 暗黑奶昔 | — | — | ·/·/· | — |
| 4623 | `PrismaticPunch` | 七彩潘趣酒 | — | — | ·/·/· | — |
| 4624 | `FruitJuice` | 果汁 | — | — | ·/·/· | — |
| 4625 | `FruitSalad` | 水果沙拉 | — | — | ·/·/· | — |
| 4626 | `AndrewSphinx` | 安德鲁狮身人面像 | 240 | — | ·/·/· | — |
| 4627 | `WatchfulAntlion` | 警惕的蚁狮 | 240 | — | ·/·/· | — |
| 4628 | `BurningSpirit` | 烈焰之魂 | 240 | — | ·/·/· | — |
| 4629 | `JawsOfDeath` | 鬼门关 | 240 | — | ·/·/· | — |
| 4630 | `TheSandsOfSlime` | 史莱姆沙漠 | 240 | — | ·/·/· | — |
| 4631 | `SnakesIHateSnakes` | 蛇，我讨厌蛇 | 240 | — | ·/·/· | — |
| 4632 | `LifeAboveTheSand` | 沙漠上的生活 | 242 | — | ·/·/· | — |
| 4633 | `Oasis` | 绿洲 | 242 | — | ·/·/· | — |
| 4634 | `PrehistoryPreserved` | 史前果酱 | 242 | — | ·/·/· | — |
| 4635 | `AncientTablet` | 远古碑牌 | 242 | — | ·/·/· | — |
| 4636 | `Uluru` | 乌卢鲁 | 242 | — | ·/·/· | — |
| 4637 | `VisitingThePyramids` | 参观金字塔 | 242 | — | ·/·/· | — |
| 4638 | `BandageBoy` | 绷带男孩 | 245 | — | ·/·/· | — |
| 4639 | `DivineEye` | 天赐之眼 | 245 | — | ·/·/· | — |
| 4640 | `AmethystStoneBlock` | 紫晶石块 | 67 | — | ·/·/· | — |
| 4641 | `TopazStoneBlock` | 黄玉石块 | 66 | — | ·/·/· | — |
| 4642 | `SapphireStoneBlock` | 蓝玉石块 | 63 | — | ·/·/· | — |
| 4643 | `EmeraldStoneBlock` | 翡翠石块 | 65 | — | ·/·/· | — |
| 4644 | `RubyStoneBlock` | 红玉石块 | 64 | — | ·/·/· | — |
| 4645 | `DiamondStoneBlock` | 钻石石块 | 68 | — | ·/·/· | — |
| 4646 | `AmberStoneBlock` | 琥珀石块 | 566 | — | ·/·/· | — |
| 4647 | `AmberStoneWallEcho` | 琥珀石墙 | — | 314 | ·/·/· | — |
| 4648 | `KiteManEater` | 食人怪风筝 | — | — | ·/·/· | — |
| 4649 | `KiteJellyfishBlue` | 蓝水母风筝 | — | — | ·/·/· | — |
| 4650 | `KiteJellyfishPink` | 粉水母风筝 | — | — | ·/·/· | — |
| 4651 | `KiteShark` | 鲨鱼风筝 | — | — | ·/·/· | — |
| 4652 | `SuperHeroMask` | 超级英雄面具 | — | — | ·/·/· | — |
| 4653 | `SuperHeroCostume` | 超级英雄服装 | — | — | ·/·/· | — |
| 4654 | `SuperHeroTights` | 超级英雄紧身裤 | — | — | ·/·/· | — |
| 4655 | `PinkFairyJar` | 粉仙灵罐 | 568 | — | ·/·/· | — |
| 4656 | `GreenFairyJar` | 绿仙灵罐 | 569 | — | ·/·/· | — |
| 4657 | `BlueFairyJar` | 蓝仙灵罐 | 570 | — | ·/·/· | — |
| 4658 | `GolfPainting1` | 绵延起伏的绿地 | 242 | — | ·/·/· | — |
| 4659 | `GolfPainting2` | 静止球研究 | 245 | — | ·/·/· | — |
| 4660 | `GolfPainting3` | 躲开！ | 240 | — | ·/·/· | — |
| 4661 | `GolfPainting4` | 反射的假象 | 246 | — | ·/·/· | — |
| 4662 | `FogboundDye` | 灰雾染料 | — | — | ·/·/· | — |
| 4663 | `BloodbathDye` | 大屠杀染料 | — | — | ·/·/· | — |
| 4664 | `PrettyPinkDressSkirt` | 漂亮粉连衣裙 | — | — | ·/·/· | — |
| 4665 | `PrettyPinkDressPants` | 漂亮粉长袜 | — | — | ·/·/· | — |
| 4666 | `PrettyPinkRibbon` | 漂亮粉丝带 | — | — | ·/·/· | — |
| 4667 | `BambooFence` | 竹栅栏 | — | 315 | ·/·/· | — |
| 4668 | `GlowPaint` | 发光涂料 | — | — | ·/·/· | — |
| 4669 | `KiteSandShark` | 沙鲨风筝 | — | — | ·/·/· | — |
| 4670 | `KiteBunnyCorrupt` | 腐化兔兔风筝 | — | — | ·/·/· | — |
| 4671 | `KiteBunnyCrimson` | 猩红兔兔风筝 | — | — | ·/·/· | — |
| 4672 | `BlandWhip` | 皮鞭 | — | — | ·/·/· | 14 |
| 4673 | `DrumStick` | 鼓槌 | — | — | ·/·/· | — |
| 4674 | `KiteGoldfish` | 金鱼风筝 | — | — | ·/·/· | — |
| 4675 | `KiteAngryTrapper` | 愤怒捕手风筝 | — | — | ·/·/· | — |
| 4676 | `KiteKoi` | 锦鲤风筝 | — | — | ·/·/· | — |
| 4677 | `KiteCrawltipede` | 千足蜈蚣风筝 | — | — | ·/·/· | — |
| 4678 | `SwordWhip` | 迪朗达尔 | — | — | ·/·/· | 55 |
| 4679 | `MaceWhip` | 晨星 | — | — | ·/·/· | 165 |
| 4680 | `ScytheWhip` | 暗黑收割 | — | — | ·/·/· | 100 |
| 4681 | `KiteSpectrum` | 光谱风筝 | — | — | ·/·/· | — |
| 4682 | `ReleaseDoves` | 释放鸽子 | — | — | ·/·/· | — |
| 4683 | `KiteWanderingEye` | 游荡眼球怪风筝 | — | — | ·/·/· | — |
| 4684 | `KiteUnicorn` | 独角兽风筝 | — | — | ·/·/· | — |
| 4685 | `UndertakerHat` | 掘墓者帽 | — | — | ·/·/· | — |
| 4686 | `UndertakerCoat` | 掘墓者外套 | — | — | ·/·/· | — |
| 4687 | `DandelionBanner` | 愤怒蒲公英旗 | 91 | — | ·/·/· | — |
| 4688 | `GnomeBanner` | 侏儒旗 | 91 | — | ·/·/· | — |
| 4689 | `DesertCampfire` | 沙漠篝火 | 215 | — | ·/·/· | — |
| 4690 | `CoralCampfire` | 珊瑚篝火 | 215 | — | ·/·/· | — |
| 4691 | `CorruptCampfire` | 腐化篝火 | 215 | — | ·/·/· | — |
| 4692 | `CrimsonCampfire` | 猩红篝火 | 215 | — | ·/·/· | — |
| 4693 | `HallowedCampfire` | 神圣篝火 | 215 | — | ·/·/· | — |
| 4694 | `JungleCampfire` | 丛林篝火 | 215 | — | ·/·/· | — |
| 4695 | `SoulBottleLight` | 瓶中光明之魂 | 572 | — | ·/·/· | — |
| 4696 | `SoulBottleNight` | 瓶中暗影之魂 | 572 | — | ·/·/· | — |
| 4697 | `SoulBottleFlight` | 瓶中飞翔之魂 | 572 | — | ·/·/· | — |
| 4698 | `SoulBottleSight` | 瓶中视域之魂 | 572 | — | ·/·/· | — |
| 4699 | `SoulBottleMight` | 瓶中力量之魂 | 572 | — | ·/·/· | — |
| 4700 | `SoulBottleFright` | 瓶中恐惧之魂 | 572 | — | ·/·/· | — |
| 4701 | `MudBud` | 泥芽 | — | — | ·/·/· | — |
| 4702 | `ReleaseLantern` | 释放灯笼 | — | — | ·/·/· | — |
| 4703 | `QuadBarrelShotgun` | 四管霰弹枪 | — | — | ·/·/· | 14 |
| 4704 | `FuneralHat` | 葬礼帽 | — | — | ·/·/· | — |
| 4705 | `FuneralCoat` | 葬礼外套 | — | — | ·/·/· | — |
| 4706 | `FuneralPants` | 葬礼裤 | — | — | ·/·/· | — |
| 4707 | `TragicUmbrella` | 悲剧雨伞 | — | — | ·/·/· | 15 |
| 4708 | `VictorianGothHat` | 维多利亚哥特帽 | — | — | ·/·/· | — |
| 4709 | `VictorianGothDress` | 维多利亚哥特裙 | — | — | ·/·/· | — |
| 4710 | `TatteredWoodSign` | 破旧木制标牌 | 573 | — | ·/·/· | — |
| 4711 | `GravediggerShovel` | 掘墓者铲子 | — | — | ·/·/· | 12 |
| 4712 | `DungeonDesertChest` | 沙漠箱 | 467 | — | ·/·/· | — |
| 4713 | `Fake_DungeonDesertChest` | 受困沙漠箱 | 468 | — | ·/·/· | — |
| 4714 | `DungeonDesertKey` | 沙漠钥匙 | — | — | ·/·/· | — |
| 4715 | `SparkleGuitar` | 星星吉他 | — | — | ·/·/· | 85 |
| 4716 | `MolluskWhistle` | 软体动物口哨 | — | — | ·/·/· | — |
| 4717 | `BorealBeam` | 针叶木梁 | 574 | — | ·/·/· | — |
| 4718 | `RichMahoganyBeam` | 红木梁 | 575 | — | ·/·/· | — |
| 4719 | `GraniteColumn` | 花岗岩柱 | 576 | — | ·/·/· | — |
| 4720 | `SandstoneColumn` | 沙岩柱 | 577 | — | ·/·/· | — |
| 4721 | `MushroomBeam` | 蘑菇梁 | 578 | — | ·/·/· | — |
| 4723 | `Nevermore` | 影魔 | 240 | — | ·/·/· | — |
| 4724 | `Reborn` | 重生 | 240 | — | ·/·/· | — |
| 4725 | `Graveyard` | 墓地 | 242 | — | ·/·/· | — |
| 4726 | `GhostManifestation` | 鬼魂显现 | 245 | — | ·/·/· | — |
| 4727 | `WickedUndead` | 邪恶亡灵 | 245 | — | ·/·/· | — |
| 4728 | `BloodyGoblet` | 血腥酒杯 | 245 | — | ·/·/· | — |
| 4729 | `StillLife` | 静物画 | 246 | — | ·/·/· | — |
| 4730 | `GhostarsWings` | 幽灵之星无极翼 | — | — | ·/·/· | — |
| 4731 | `TerraToilet` | 泰拉马桶 | 497 | — | ·/·/· | — |
| 4732 | `GhostarSkullPin` | 幽灵之星灵魂罐 | — | — | ·/·/· | — |
| 4733 | `GhostarShirt` | 幽灵之星上衣 | — | — | ·/·/· | — |
| 4734 | `GhostarPants` | 幽灵之星紧身裤 | — | — | ·/·/· | — |
| 4735 | `BallOfFuseWire` | 保险丝球 | — | — | ·/·/· | — |
| 4736 | `FullMoonSqueakyToy` | 满月尖叫玩具 | — | — | ·/·/· | — |
| 4737 | `OrnateShadowKey` | 装饰暗影钥匙 | — | — | ·/·/· | — |
| 4738 | `DrManFlyMask` | 飞人博士面具 | — | — | ·/·/· | — |
| 4739 | `DrManFlyLabCoat` | 飞人博士实验服 | — | — | ·/·/· | — |
| 4740 | `ButcherMask` | 屠夫面具 | — | — | ·/·/· | — |
| 4741 | `ButcherApron` | 屠夫的染血围裙 | — | — | ·/·/· | — |
| 4742 | `ButcherPants` | 屠夫的染血裤 | — | — | ·/·/· | — |
| 4743 | `Football` | 足球 | — | — | ·/·/· | — |
| 4744 | `HunterCloak` | 猎人斗篷 | — | — | ·/·/· | — |
| 4745 | `CoffinMinecart` | 棺材矿车 | — | — | ·/·/· | — |
| 4746 | `SafemanWings` | 萨菲曼的毛毯披肩 | — | — | ·/·/· | — |
| 4747 | `SafemanSunHair` | 萨菲曼的晴天 | — | — | ·/·/· | — |
| 4748 | `SafemanSunDress` | 萨菲曼的太阳裙 | — | — | ·/·/· | — |
| 4749 | `SafemanDressLeggings` | 萨菲曼的粉色护腿 | — | — | ·/·/· | — |
| 4750 | `FoodBarbarianWings` | FoodBarbarian的褴褛龙之翼 | — | — | ·/·/· | — |
| 4751 | `FoodBarbarianHelm` | FoodBarbarian的角盔 | — | — | ·/·/· | — |
| 4752 | `FoodBarbarianArmor` | FoodBarbarian的野狼护肩 | — | — | ·/·/· | — |
| 4753 | `FoodBarbarianGreaves` | FoodBarbarian的野猪护胫 | — | — | ·/·/· | — |
| 4754 | `GroxTheGreatWings` | 了不起的格洛西翅翼 | — | — | ·/·/· | — |
| 4755 | `GroxTheGreatHelm` | 了不起的格洛西角形风帽 | — | — | ·/·/· | — |
| 4756 | `GroxTheGreatArmor` | 了不起的格洛西护胸 | — | — | ·/·/· | — |
| 4757 | `GroxTheGreatGreaves` | 了不起的格洛西护胫 | — | — | ·/·/· | — |
| 4758 | `Smolstar` | 刃杖 | — | — | ·/·/· | 6 |
| 4759 | `SquirrelHook` | 松鼠钩 | — | — | ·/·/· | — |
| 4760 | `BouncingShield` | 中士联盾 | — | — | ·/·/· | 80 |
| 4761 | `RockGolemHead` | 岩石巨人头 | 579 | — | ·/·/· | — |
| 4762 | `CritterShampoo` | 小动物香波 | — | — | ·/·/· | — |
| 4763 | `DiggingMoleMinecart` | 挖掘鼹鼠矿车 | — | — | ·/·/· | — |
| 4764 | `Shroomerang` | 蘑菇回旋镖 | — | — | ·/·/· | 23 |
| 4765 | `TreeGlobe` | 树球 | — | — | ·/·/· | — |
| 4766 | `WorldGlobe` | 世界球 | — | — | ·/·/· | — |
| 4767 | `DontHurtCrittersBook` | 小动物友谊指南 | — | — | ·/·/· | — |
| 4768 | `DogEars` | 狗耳朵 | — | — | ·/·/· | — |
| 4769 | `DogTail` | 狗尾巴 | — | — | ·/·/· | — |
| 4770 | `FoxEars` | 狐狸耳朵 | — | — | ·/·/· | — |
| 4771 | `FoxTail` | 狐狸尾巴 | — | — | ·/·/· | — |
| 4772 | `LizardEars` | 蜥蜴耳朵 | — | — | ·/·/· | — |
| 4773 | `LizardTail` | 蜥蜴尾巴 | — | — | ·/·/· | — |
| 4774 | `PandaEars` | 熊猫耳朵 | — | — | ·/·/· | — |
| 4775 | `BunnyTail` | 兔尾巴 | — | — | ·/·/· | — |
| 4776 | `FairyGlowstick` | 仙灵荧光棒 | — | — | ·/·/· | — |
| 4777 | `LightningCarrot` | 闪电胡萝卜 | — | — | ·/·/· | — |
| 4778 | `HallowBossDye` | 七彩染料 | — | — | ·/·/· | — |
| 4779 | `MushroomHat` | 蘑菇礼帽 | — | — | ·/·/· | — |
| 4780 | `MushroomVest` | 蘑菇背心 | — | — | ·/·/· | — |
| 4781 | `MushroomPants` | 蘑菇裤 | — | — | ·/·/· | — |
| 4782 | `FairyQueenBossBag` | 宝藏袋（光之女皇） | — | — | ·/·/· | — |
| 4783 | `FairyQueenTrophy` | 光之女皇纪念章 | 240 | — | ·/·/· | — |
| 4784 | `FairyQueenMask` | 光之女皇面具 | — | — | ·/·/· | — |
| 4785 | `PaintedHorseSaddle` | 布满灰尘的牛皮鞍 | — | — | ·/·/· | — |
| 4786 | `MajesticHorseSaddle` | 皇家镀金鞍 | — | — | ·/·/· | — |
| 4787 | `DarkHorseSaddle` | 黑镶嵌鞍 | — | — | ·/·/· | — |
| 4788 | `JoustingLance` | 比武长戟 | — | — | ·/·/· | 60 |
| 4789 | `ShadowJoustingLance` | 暗影比武长戟 | — | — | ·/·/· | 130 |
| 4790 | `HallowJoustingLance` | 神圣比武长戟 | — | — | ·/·/· | 90 |
| 4791 | `PogoStick` | 蹦蹦跷 | — | — | ·/·/· | — |
| 4792 | `PirateShipMountItem` | 黑斑 | — | — | ·/·/· | — |
| 4793 | `SpookyWoodMountItem` | 魔法树枝 | — | — | ·/·/· | — |
| 4794 | `SantankMountItem` | 玩具坦克 | — | — | ·/·/· | — |
| 4795 | `WallOfFleshGoatMountItem` | 山羊骷髅头 | — | — | ·/·/· | — |
| 4796 | `DarkMageBookMountItem` | 暗黑魔法师巨著 | — | — | ·/·/· | — |
| 4797 | `KingSlimePetItem` | 皇家怡口 | — | — | ·/·/· | — |
| 4798 | `EyeOfCthulhuPetItem` | 咧嘴笑的可疑眼球 | — | — | ·/·/· | — |
| 4799 | `EaterOfWorldsPetItem` | 蠕动残骸 | — | — | ·/·/· | — |
| 4800 | `BrainOfCthulhuPetItem` | 罐中脑 | — | — | ·/·/· | — |
| 4801 | `SkeletronPetItem` | 疯狂骷髅头 | — | — | ·/·/· | — |
| 4802 | `QueenBeePetItem` | 闪耀蜂蜜 | — | — | ·/·/· | — |
| 4803 | `DestroyerPetItem` | 失效探测器 | — | — | ·/·/· | — |
| 4804 | `TwinsPetItem` | 微型双子魔眼 | — | — | ·/·/· | — |
| 4805 | `SkeletronPrimePetItem` | 机器人骷髅头 | — | — | ·/·/· | — |
| 4806 | `PlanteraPetItem` | 世纪之花幼苗 | — | — | ·/·/· | — |
| 4807 | `GolemPetItem` | 石巨人守卫 | — | — | ·/·/· | — |
| 4808 | `DukeFishronPetItem` | 海底猪 | — | — | ·/·/· | — |
| 4809 | `LunaticCultistPetItem` | 碑牌碎片 | — | — | ·/·/· | — |
| 4810 | `MoonLordPetItem` | 月亮乌贼 | — | — | ·/·/· | — |
| 4811 | `FairyQueenPetItem` | 光之珠宝 | — | — | ·/·/· | — |
| 4812 | `PumpkingPetItem` | 南瓜香味蜡烛 | — | — | ·/·/· | — |
| 4813 | `EverscreamPetItem` | 灌木星星 | — | — | ·/·/· | — |
| 4814 | `IceQueenPetItem` | 冰冻王冠 | — | — | ·/·/· | — |
| 4815 | `MartianPetItem` | 宇宙滑板 | — | — | ·/·/· | — |
| 4816 | `DD2OgrePetItem` | 食人魔棍棒 | — | — | ·/·/· | — |
| 4817 | `DD2BetsyPetItem` | 双足翼龙蛋 | — | — | ·/·/· | — |
| 4818 | `CombatWrench` | 战斗扳手 | — | — | ·/·/· | 25 |
| 4819 | `DemonConch` | 恶魔海螺 | — | — | ·/·/· | — |
| 4820 | `BottomlessLavaBucket` | 无底熔岩桶 | — | — | ·/·/· | — |
| 4821 | `FireproofBugNet` | 防熔岩虫网 | — | — | ·/·/· | — |
| 4822 | `FlameWakerBoots` | 烈焰靴 | — | — | ·/·/· | — |
| 4823 | `RainbowWings` | 女皇翅翼 | — | — | ·/·/· | — |
| 4824 | `WetBomb` | 潮湿炸弹 | — | — | ·/·/· | — |
| 4825 | `LavaBomb` | 熔岩炸弹 | — | — | ·/·/· | — |
| 4826 | `HoneyBomb` | 蜂蜜炸弹 | — | — | ·/·/· | — |
| 4827 | `DryBomb` | 干炸弹 | — | — | ·/·/· | — |
| 4828 | `SuperheatedBlood` | 过热的血 | — | — | ·/·/· | — |
| 4829 | `LicenseCat` | 猫咪许可证 | — | — | ·/·/· | — |
| 4830 | `LicenseDog` | 狗狗许可证 | — | — | ·/·/· | — |
| 4831 | `GemSquirrelAmethyst` | 紫晶松鼠 | — | — | ·/·/· | — |
| 4832 | `GemSquirrelTopaz` | 黄玉松鼠 | — | — | ·/·/· | — |
| 4833 | `GemSquirrelSapphire` | 蓝玉松鼠 | — | — | ·/·/· | — |
| 4834 | `GemSquirrelEmerald` | 翡翠松鼠 | — | — | ·/·/· | — |
| 4835 | `GemSquirrelRuby` | 红玉松鼠 | — | — | ·/·/· | — |
| 4836 | `GemSquirrelDiamond` | 钻石松鼠 | — | — | ·/·/· | — |
| 4837 | `GemSquirrelAmber` | 琥珀松鼠 | — | — | ·/·/· | — |
| 4838 | `GemBunnyAmethyst` | 紫晶兔兔 | — | — | ·/·/· | — |
| 4839 | `GemBunnyTopaz` | 黄玉兔兔 | — | — | ·/·/· | — |
| 4840 | `GemBunnySapphire` | 蓝玉兔兔 | — | — | ·/·/· | — |
| 4841 | `GemBunnyEmerald` | 翡翠兔兔 | — | — | ·/·/· | — |
| 4842 | `GemBunnyRuby` | 红玉兔兔 | — | — | ·/·/· | — |
| 4843 | `GemBunnyDiamond` | 钻石兔兔 | — | — | ·/·/· | — |
| 4844 | `GemBunnyAmber` | 琥珀兔兔 | — | — | ·/·/· | — |
| 4845 | `HellButterfly` | 地狱蝴蝶 | — | — | ·/·/· | — |
| 4846 | `HellButterflyJar` | 地狱蝴蝶罐 | 580 | — | ·/·/· | — |
| 4847 | `Lavafly` | 熔岩蝇 | — | — | ·/·/· | — |
| 4848 | `LavaflyinaBottle` | 瓶中熔岩蝇 | 581 | — | ·/·/· | — |
| 4849 | `MagmaSnail` | 岩浆蜗牛 | — | — | ·/·/· | — |
| 4850 | `MagmaSnailCage` | 岩浆蜗牛笼 | 582 | — | ·/·/· | — |
| 4851 | `GemTreeTopazSeed` | 黄玉宝石果 | 590 | — | ·/·/· | — |
| 4852 | `GemTreeAmethystSeed` | 紫晶宝石果 | 590 | — | ·/·/· | — |
| 4853 | `GemTreeSapphireSeed` | 蓝玉宝石果 | 590 | — | ·/·/· | — |
| 4854 | `GemTreeEmeraldSeed` | 翡翠宝石果 | 590 | — | ·/·/· | — |
| 4855 | `GemTreeRubySeed` | 红玉宝石果 | 590 | — | ·/·/· | — |
| 4856 | `GemTreeDiamondSeed` | 钻石宝石果 | 590 | — | ·/·/· | — |
| 4857 | `GemTreeAmberSeed` | 琥珀宝石果 | 590 | — | ·/·/· | — |
| 4858 | `PotSuspended` | 吊挂盆 | 591 | — | ·/·/· | — |
| 4859 | `PotSuspendedDaybloom` | 吊挂太阳花 | 591 | — | ·/·/· | — |
| 4860 | `PotSuspendedMoonglow` | 吊挂月光草 | 591 | — | ·/·/· | — |
| 4861 | `PotSuspendedWaterleaf` | 吊挂幌菊 | 591 | — | ·/·/· | — |
| 4862 | `PotSuspendedShiverthorn` | 吊挂寒颤棘 | 591 | — | ·/·/· | — |
| 4863 | `PotSuspendedBlinkroot` | 吊挂闪耀根 | 591 | — | ·/·/· | — |
| 4864 | `PotSuspendedDeathweedCorrupt` | 吊挂腐化死亡草 | 591 | — | ·/·/· | — |
| 4865 | `PotSuspendedDeathweedCrimson` | 吊挂猩红死亡草 | 591 | — | ·/·/· | — |
| 4866 | `PotSuspendedFireblossom` | 吊挂火舌花 | 591 | — | ·/·/· | — |
| 4867 | `BrazierSuspended` | 吊挂火盆 | 592 | — | ·/·/· | — |
| 4868 | `VolcanoSmall` | 小火山 | 593 | — | ·/·/· | — |
| 4869 | `VolcanoLarge` | 大火山 | 594 | — | ·/·/· | — |
| 4870 | `PotionOfReturn` | 返回药水 | — | — | ·/·/· | — |
| 4871 | `VanityTreeSakuraSeed` | 樱花树苗 | 595 | — | ·/·/· | — |
| 4872 | `LavaAbsorbantSponge` | 熔岩吸水棉 | — | — | ·/·/· | — |
| 4873 | `HallowedHood` | 神圣兜帽 | — | — | ·/·/· | — |
| 4874 | `HellfireTreads` | 狱炎花纹 | — | — | ·/·/· | — |
| 4875 | `TeleportationPylonJungle` | 丛林晶塔 | 597 | — | ·/·/· | — |
| 4876 | `TeleportationPylonPurity` | 森林晶塔 | 597 | — | ·/·/· | — |
| 4877 | `LavaCrate` | 黑曜石匣 | 376 | — | ·/·/· | — |
| 4878 | `LavaCrateHard` | 狱石匣 | 376 | — | ·/·/· | — |
| 4879 | `ObsidianLockbox` | 黑曜石锁盒 | — | — | ·/·/· | — |
| 4880 | `LavaFishbowl` | 熔岩蛇缸 | 598 | — | ·/·/· | — |
| 4881 | `LavaFishingHook` | 防熔岩钓钩 | — | — | ·/·/· | — |
| 4882 | `AmethystBunnyCage` | 紫晶兔笼 | 599 | — | ·/·/· | — |
| 4883 | `TopazBunnyCage` | 黄玉兔笼 | 600 | — | ·/·/· | — |
| 4884 | `SapphireBunnyCage` | 蓝玉兔笼 | 601 | — | ·/·/· | — |
| 4885 | `EmeraldBunnyCage` | 翡翠兔笼 | 602 | — | ·/·/· | — |
| 4886 | `RubyBunnyCage` | 红玉兔笼 | 603 | — | ·/·/· | — |
| 4887 | `DiamondBunnyCage` | 钻石兔笼 | 604 | — | ·/·/· | — |
| 4888 | `AmberBunnyCage` | 琥珀兔笼 | 605 | — | ·/·/· | — |
| 4889 | `AmethystSquirrelCage` | 紫晶松鼠笼 | 606 | — | ·/·/· | — |
| 4890 | `TopazSquirrelCage` | 黄玉松鼠笼 | 607 | — | ·/·/· | — |
| 4891 | `SapphireSquirrelCage` | 蓝玉松鼠笼 | 608 | — | ·/·/· | — |
| 4892 | `EmeraldSquirrelCage` | 翡翠松鼠笼 | 609 | — | ·/·/· | — |
| 4893 | `RubySquirrelCage` | 红玉松鼠笼 | 610 | — | ·/·/· | — |
| 4894 | `DiamondSquirrelCage` | 钻石松鼠笼 | 611 | — | ·/·/· | — |
| 4895 | `AmberSquirrelCage` | 琥珀松鼠笼 | 612 | — | ·/·/· | — |
| 4896 | `AncientHallowedMask` | 远古神圣面具 | — | — | ·/·/· | — |
| 4897 | `AncientHallowedHelmet` | 远古神圣头盔 | — | — | ·/·/· | — |
| 4898 | `AncientHallowedHeadgear` | 远古神圣头饰 | — | — | ·/·/· | — |
| 4899 | `AncientHallowedHood` | 远古神圣兜帽 | — | — | ·/·/· | — |
| 4900 | `AncientHallowedPlateMail` | 远古神圣板甲 | — | — | ·/·/· | — |
| 4901 | `AncientHallowedGreaves` | 远古神圣护胫 | — | — | ·/·/· | — |
| 4902 | `PottedLavaPlantPalm` | 盆栽岩浆棕榈 | 548 | — | ·/·/· | — |
| 4903 | `PottedLavaPlantBush` | 盆栽硫磺灌木 | 548 | — | ·/·/· | — |
| 4904 | `PottedLavaPlantBramble` | 盆栽火荆棘 | 613 | — | ·/·/· | — |
| 4905 | `PottedLavaPlantBulb` | 盆栽熔岩球茎 | 613 | — | ·/·/· | — |
| 4906 | `PottedLavaPlantTendrils` | 盆栽余烬卷须 | 614 | — | ·/·/· | — |
| 4907 | `VanityTreeYellowWillowSeed` | 黄柳树苗 | 615 | — | ·/·/· | — |
| 4908 | `DirtBomb` | 土炸弹 | — | — | ·/·/· | — |
| 4909 | `DirtStickyBomb` | 粘性土炸弹 | — | — | ·/·/· | — |
| 4910 | `LicenseBunny` | 兔兔许可证 | — | — | ·/·/· | — |
| 4911 | `CoolWhip` | 冷鞭 | — | — | ·/·/· | 45 |
| 4912 | `FireWhip` | 鞭炮 | — | — | ·/·/· | 37 |
| 4913 | `ThornWhip` | 荆鞭 | — | — | ·/·/· | 18 |
| 4914 | `RainbowWhip` | 万花筒 | — | — | ·/·/· | 180 |
| 4915 | `TungstenBullet` | 钨子弹 | — | — | ·/·/· | 9 |
| 4916 | `TeleportationPylonHallow` | 神圣晶塔 | 597 | — | ·/·/· | — |
| 4917 | `TeleportationPylonUnderground` | 洞穴晶塔 | 597 | — | ·/·/· | — |
| 4918 | `TeleportationPylonOcean` | 海洋晶塔 | 597 | — | ·/·/· | — |
| 4919 | `TeleportationPylonDesert` | 沙漠晶塔 | 597 | — | ·/·/· | — |
| 4920 | `TeleportationPylonSnow` | 雪原晶塔 | 597 | — | ·/·/· | — |
| 4921 | `TeleportationPylonMushroom` | 蘑菇晶塔 | 597 | — | ·/·/· | — |
| 4922 | `CavernFountain` | 洞穴喷泉 | 207 | — | ·/·/· | — |
| 4923 | `PiercingStarlight` | 星光 | — | — | ·/·/· | 80 |
| 4924 | `EyeofCthulhuMasterTrophy` | 克苏鲁之眼圣物 | 617 | — | ·/·/· | — |
| 4925 | `EaterofWorldsMasterTrophy` | 世界吞噬怪圣物 | 617 | — | ·/·/· | — |
| 4926 | `BrainofCthulhuMasterTrophy` | 克苏鲁之脑圣物 | 617 | — | ·/·/· | — |
| 4927 | `SkeletronMasterTrophy` | 骷髅王圣物 | 617 | — | ·/·/· | — |
| 4928 | `QueenBeeMasterTrophy` | 蜂王圣物 | 617 | — | ·/·/· | — |
| 4929 | `KingSlimeMasterTrophy` | 史莱姆王圣物 | 617 | — | ·/·/· | — |
| 4930 | `WallofFleshMasterTrophy` | 血肉墙圣物 | 617 | — | ·/·/· | — |
| 4931 | `TwinsMasterTrophy` | 双子魔眼圣物 | 617 | — | ·/·/· | — |
| 4932 | `DestroyerMasterTrophy` | 毁灭者圣物 | 617 | — | ·/·/· | — |
| 4933 | `SkeletronPrimeMasterTrophy` | 机械骷髅王圣物 | 617 | — | ·/·/· | — |
| 4934 | `PlanteraMasterTrophy` | 世纪之花圣物 | 617 | — | ·/·/· | — |
| 4935 | `GolemMasterTrophy` | 石巨人圣物 | 617 | — | ·/·/· | — |
| 4936 | `DukeFishronMasterTrophy` | 猪龙鱼公爵圣物 | 617 | — | ·/·/· | — |
| 4937 | `LunaticCultistMasterTrophy` | 拜月教邪教徒圣物 | 617 | — | ·/·/· | — |
| 4938 | `MoonLordMasterTrophy` | 月亮领主圣物 | 617 | — | ·/·/· | — |
| 4939 | `UFOMasterTrophy` | 火星飞碟圣物 | 617 | — | ·/·/· | — |
| 4940 | `FlyingDutchmanMasterTrophy` | 荷兰飞盗船圣物 | 617 | — | ·/·/· | — |
| 4941 | `MourningWoodMasterTrophy` | 哀木圣物 | 617 | — | ·/·/· | — |
| 4942 | `PumpkingMasterTrophy` | 南瓜王圣物 | 617 | — | ·/·/· | — |
| 4943 | `IceQueenMasterTrophy` | 冰雪女王圣物 | 617 | — | ·/·/· | — |
| 4944 | `EverscreamMasterTrophy` | 常绿尖叫怪圣物 | 617 | — | ·/·/· | — |
| 4945 | `SantankMasterTrophy` | 圣诞坦克圣物 | 617 | — | ·/·/· | — |
| 4946 | `DarkMageMasterTrophy` | 暗黑魔法师圣物 | 617 | — | ·/·/· | — |
| 4947 | `OgreMasterTrophy` | 食人魔圣物 | 617 | — | ·/·/· | — |
| 4948 | `BetsyMasterTrophy` | 双足翼龙圣物 | 617 | — | ·/·/· | — |
| 4949 | `FairyQueenMasterTrophy` | 光之女皇圣物 | 617 | — | ·/·/· | — |
| 4950 | `QueenSlimeMasterTrophy` | 史莱姆皇后圣物 | 617 | — | ·/·/· | — |
| 4951 | `TeleportationPylonVictory` | 万能晶塔 | 597 | — | ·/·/· | — |
| 4952 | `FairyQueenMagicItem` | 夜光 | — | — | ·/·/· | 50 |
| 4953 | `FairyQueenRangedItem` | 日暮 | — | — | ·/·/· | 50 |
| 4954 | `LongRainbowTrailWings` | 天界星盘 | — | — | ·/·/· | — |
| 4955 | `RabbitOrder` | 兔子头盔 | — | — | ·/·/· | — |
| 4956 | `Zenith` | 天顶剑 | — | — | ·/·/· | 190 |
| 4957 | `QueenSlimeBossBag` | 宝藏袋（史莱姆皇后） | — | — | ·/·/· | — |
| 4958 | `QueenSlimeTrophy` | 史莱姆皇后纪念章 | 240 | — | ·/·/· | — |
| 4959 | `QueenSlimeMask` | 史莱姆皇后面具 | — | — | ·/·/· | — |
| 4960 | `QueenSlimePetItem` | 皇家美食 | — | — | ·/·/· | — |
| 4961 | `EmpressButterfly` | 七彩草蛉 | — | — | ·/·/· | — |
| 4962 | `AccentSlab` | 石材板 | 618 | — | ·/·/· | — |
| 4963 | `TruffleWormCage` | 松露蠕虫笼 | 619 | — | ·/·/· | — |
| 4964 | `EmpressButterflyJar` | 七彩草蛉罐 | 620 | — | ·/·/· | — |
| 4965 | `RockGolemBanner` | 岩石巨人旗 | 91 | — | ·/·/· | — |
| 4966 | `BloodMummyBanner` | 血木乃伊旗 | 91 | — | ·/·/· | — |
| 4967 | `SporeSkeletonBanner` | 孢子骷髅旗 | 91 | — | ·/·/· | — |
| 4968 | `SporeBatBanner` | 孢子蝙蝠旗 | 91 | — | ·/·/· | — |
| 4969 | `LarvaeAntlionBanner` | 蚁狮幼虫旗 | 91 | — | ·/·/· | — |
| 4970 | `CrimsonBunnyBanner` | 毒兔兔旗 | 91 | — | ·/·/· | — |
| 4971 | `CrimsonGoldfishBanner` | 毒金鱼旗 | 91 | — | ·/·/· | — |
| 4972 | `CrimsonPenguinBanner` | 毒企鹅旗 | 91 | — | ·/·/· | — |
| 4973 | `BigMimicCorruptionBanner` | 腐化宝箱怪旗 | 91 | — | ·/·/· | — |
| 4974 | `BigMimicCrimsonBanner` | 猩红宝箱怪旗 | 91 | — | ·/·/· | — |
| 4975 | `BigMimicHallowBanner` | 神圣宝箱怪旗 | 91 | — | ·/·/· | — |
| 4976 | `MossHornetBanner` | 苔藓黄蜂旗 | 91 | — | ·/·/· | — |
| 4977 | `WanderingEyeBanner` | 游荡眼球怪旗 | 91 | — | ·/·/· | — |
| 4978 | `CreativeWings` | 雏翼 | — | — | ·/·/· | — |
| 4979 | `MusicBoxQueenSlime` | 八音盒（史莱姆皇后） | 139 | — | ·/·/· | — |
| 4980 | `QueenSlimeHook` | 失谐钩爪 | — | — | ·/·/· | — |
| 4981 | `QueenSlimeMountSaddle` | 明胶后座 | — | — | ·/·/· | — |
| 4982 | `CrystalNinjaHelmet` | 水晶刺客兜帽 | — | — | ·/·/· | — |
| 4983 | `CrystalNinjaChestplate` | 水晶刺客上衣 | — | — | ·/·/· | — |
| 4984 | `CrystalNinjaLeggings` | 水晶刺客裤 | — | — | ·/·/· | — |
| 4985 | `MusicBoxEmpressOfLight` | 八音盒（光之女皇） | 139 | — | ·/·/· | — |
| 4986 | `GelBalloon` | 闪耀史莱姆气球 | — | — | ·/·/· | — |
| 4987 | `VolatileGelatin` | 挥发明胶 | — | — | ·/·/· | — |
| 4988 | `QueenSlimeCrystal` | 明胶水晶 | — | — | ·/·/· | — |
| 4989 | `EmpressFlightBooster` | 飞升之证 | — | — | ·/·/· | — |
| 4990 | `MusicBoxDukeFishron` | 八音盒（猪龙鱼公爵） | 139 | — | ·/·/· | — |
| 4991 | `MusicBoxMorningRain` | 八音盒（晨雨） | 139 | — | ·/·/· | — |
| 4992 | `MusicBoxConsoleTitle` | 八音盒（标题备选曲） | 139 | — | ·/·/· | — |
| 4993 | `ChippysCouch` | 花栗鼠沙发 | 89 | — | ·/·/· | — |
| 4994 | `GraduationCapBlue` | 蓝毕业帽 | — | — | ·/·/· | — |
| 4995 | `GraduationCapMaroon` | 褐红毕业帽 | — | — | ·/·/· | — |
| 4996 | `GraduationCapBlack` | 黑毕业帽 | — | — | ·/·/· | — |
| 4997 | `GraduationGownBlue` | 蓝毕业礼服 | — | — | ·/·/· | — |
| 4998 | `GraduationGownMaroon` | 褐红毕业礼服 | — | — | ·/·/· | — |
| 4999 | `GraduationGownBlack` | 黑毕业礼服 | — | — | ·/·/· | — |
| 5000 | `TerrasparkBoots` | 泰拉闪耀靴 | — | — | ·/·/· | — |
| 5001 | `MoonLordLegs` | 月亮领主腿 | — | — | ·/·/· | — |
| 5002 | `OceanCrate` | 海洋匣 | 376 | — | ·/·/· | — |
| 5003 | `OceanCrateHard` | 海边匣 | 376 | — | ·/·/· | — |
| 5004 | `BadgersHat` | 獾皮帽 | — | — | ·/·/· | — |
| 5005 | `EmpressBlade` | 泰拉棱镜 | — | — | ·/·/· | 90 |
| 5006 | `MusicBoxUndergroundDesert` | 八音盒（地下沙漠） | 139 | — | ·/·/· | — |
| 5007 | `DeadMansSweater` | 死人毛衣 | — | — | ·/·/· | — |
| 5008 | `TeaKettle` | 茶壶 | 622 | — | ·/·/· | — |
| 5009 | `Teacup` | 茶杯 | — | — | ·/·/· | — |
| 5010 | `TreasureMagnet` | 宝藏磁石 | — | — | ·/·/· | — |
| 5011 | `Mace` | 链锤 | — | — | ·/·/· | 9 |
| 5012 | `FlamingMace` | 烈焰链锤 | — | — | ·/·/· | 9 |
| 5014 | `MusicBoxOWRain` | 异界八音盒（雨） | 139 | — | ·/·/· | — |
| 5015 | `MusicBoxOWDay` | 异界八音盒（人间日） | 139 | — | ·/·/· | — |
| 5016 | `MusicBoxOWNight` | 异界八音盒（暗夜） | 139 | — | ·/·/· | — |
| 5017 | `MusicBoxOWUnderground` | 异界八音盒（地下） | 139 | — | ·/·/· | — |
| 5018 | `MusicBoxOWDesert` | 异界八音盒（沙漠） | 139 | — | ·/·/· | — |
| 5019 | `MusicBoxOWOcean` | 异界八音盒（海洋） | 139 | — | ·/·/· | — |
| 5020 | `MusicBoxOWMushroom` | 异界八音盒（蘑菇） | 139 | — | ·/·/· | — |
| 5021 | `MusicBoxOWDungeon` | 异界八音盒（地牢） | 139 | — | ·/·/· | — |
| 5022 | `MusicBoxOWSpace` | 异界八音盒（太空） | 139 | — | ·/·/· | — |
| 5023 | `MusicBoxOWUnderworld` | 异界八音盒（地狱） | 139 | — | ·/·/· | — |
| 5024 | `MusicBoxOWSnow` | 异界八音盒（雪） | 139 | — | ·/·/· | — |
| 5025 | `MusicBoxOWCorruption` | 异界八音盒（腐化之地） | 139 | — | ·/·/· | — |
| 5026 | `MusicBoxOWUndergroundCorruption` | 异界八音盒（地下腐化之地） | 139 | — | ·/·/· | — |
| 5027 | `MusicBoxOWCrimson` | 异界八音盒（猩红之地） | 139 | — | ·/·/· | — |
| 5028 | `MusicBoxOWUndergroundCrimson` | 异界八音盒（地下猩红之地） | 139 | — | ·/·/· | — |
| 5029 | `MusicBoxOWUndergroundSnow` | 异界八音盒（冰雪） | 139 | — | ·/·/· | — |
| 5030 | `MusicBoxOWUndergroundHallow` | 异界八音盒（地下神圣之地） | 139 | — | ·/·/· | — |
| 5031 | `MusicBoxOWBloodMoon` | 异界八音盒（恐惧） | 139 | — | ·/·/· | — |
| 5032 | `MusicBoxOWBoss2` | 异界八音盒(Boss 2) | 139 | — | ·/·/· | — |
| 5033 | `MusicBoxOWBoss1` | 异界八音盒(Boss 1) | 139 | — | ·/·/· | — |
| 5034 | `MusicBoxOWInvasion` | 异界八音盒（入侵） | 139 | — | ·/·/· | — |
| 5035 | `MusicBoxOWTowers` | 异界八音盒（神塔） | 139 | — | ·/·/· | — |
| 5036 | `MusicBoxOWMoonLord` | 异界八音盒（月亮Boss） | 139 | — | ·/·/· | — |
| 5037 | `MusicBoxOWPlantera` | 异界八音盒（世纪之花） | 139 | — | ·/·/· | — |
| 5038 | `MusicBoxOWJungle` | 异界八音盒（丛林） | 139 | — | ·/·/· | — |
| 5039 | `MusicBoxOWWallOfFlesh` | 异界八音盒（血肉墙） | 139 | — | ·/·/· | — |
| 5040 | `MusicBoxOWHallow` | 异界八音盒（神圣之地） | 139 | — | ·/·/· | — |
| 5041 | `MilkCarton` | 盒装牛奶 | — | — | ·/·/· | — |
| 5042 | `CoffeeCup` | 咖啡 | — | — | ·/·/· | — |
| 5043 | `TorchGodsFavor` | 火把神徽章 | — | — | ·/·/· | — |
| 5044 | `MusicBoxCredits` | 八音盒（旅程结束） | 139 | — | ·/·/· | — |
| 5045 | `PlaguebringerHelmet` | 瘟疫使者骷髅头 | — | — | ·/·/· | — |
| 5046 | `PlaguebringerChestplate` | 瘟疫使者斗篷 | — | — | ·/·/· | — |
| 5047 | `PlaguebringerGreaves` | 瘟疫使者便鞋 | — | — | ·/·/· | — |
| 5048 | `RoninHat` | 浪人阵伞 | — | — | ·/·/· | — |
| 5049 | `RoninShirt` | 浪人浴衣 | — | — | ·/·/· | — |
| 5050 | `RoninPants` | 浪人木屐 | — | — | ·/·/· | — |
| 5051 | `TimelessTravelerHood` | 永恒旅人兜帽 | — | — | ·/·/· | — |
| 5052 | `TimelessTravelerRobe` | 永恒旅人斗篷 | — | — | ·/·/· | — |
| 5053 | `TimelessTravelerBottom` | 永恒旅人鞋履 | — | — | ·/·/· | — |
| 5054 | `FloretProtectorHelmet` | 护花者头盔 | — | — | ·/·/· | — |
| 5055 | `FloretProtectorChestplate` | 护花者衬衫 | — | — | ·/·/· | — |
| 5056 | `FloretProtectorLegs` | 护花者裤子 | — | — | ·/·/· | — |
| 5057 | `CapricornMask` | 摩羯座头盔 | — | — | ·/·/· | — |
| 5058 | `CapricornChestplate` | 摩羯座护胸 | — | — | ·/·/· | — |
| 5059 | `CapricornLegs` | 摩羯座兽蹄 | — | — | ·/·/· | — |
| 5060 | `CapricornTail` | 摩羯座尾巴 | — | — | ·/·/· | — |
| 5061 | `TVHeadMask` | 视频脸 | — | — | ·/·/· | — |
| 5062 | `TVHeadSuit` | 激光外套 | — | — | ·/·/· | — |
| 5063 | `TVHeadPants` | 细条纹裤 | — | — | ·/·/· | — |
| 5064 | `LavaproofTackleBag` | 防熔岩渔具袋 | — | — | ·/·/· | — |
| 5065 | `PrincessWeapon` | 共鸣权杖 | — | — | ·/·/· | 70 |
| 5066 | `BeeHive` | 蜂巢 | 444 | — | ·/·/· | — |
| 5067 | `AntlionEggs` | 蚁狮卵 | 485 | — | ·/·/· | — |
| 5068 | `FlinxFurCoat` | 小雪怪皮毛外套 | — | — | ·/·/· | — |
| 5069 | `FlinxStaff` | 小雪怪法杖 | — | — | ·/·/· | 8 |
| 5070 | `FlinxFur` | 小雪怪皮毛 | — | — | ·/·/· | — |
| 5071 | `RoyalTiara` | 皇家头冠 | — | — | ·/·/· | — |
| 5072 | `RoyalDressTop` | 皇家女衫 | — | — | ·/·/· | — |
| 5073 | `RoyalDressBottom` | 皇家礼服裙 | — | — | ·/·/· | — |
| 5074 | `BoneWhip` | 脊柱骨鞭 | — | — | ·/·/· | 27 |
| 5075 | `RainbowCursor` | 彩虹色光标 | — | — | ·/·/· | — |
| 5076 | `RoyalScepter` | 皇家权杖 | — | — | ·/·/· | — |
| 5077 | `GlassSlipper` | 玻璃拖鞋 | — | — | ·/·/· | — |
| 5078 | `PrinceUniform` | 王子制服 | — | — | ·/·/· | — |
| 5079 | `PrincePants` | 王子裤子 | — | — | ·/·/· | — |
| 5080 | `PrinceCape` | 王子披风 | — | — | ·/·/· | — |
| 5081 | `PottedCrystalPlantFern` | 水晶蕨盆栽 | 623 | — | ·/·/· | — |
| 5082 | `PottedCrystalPlantSpiral` | 水晶螺旋盆栽 | 623 | — | ·/·/· | — |
| 5083 | `PottedCrystalPlantTeardrop` | 水晶泪滴盆栽 | 623 | — | ·/·/· | — |
| 5084 | `PottedCrystalPlantTree` | 水晶树盆栽 | 623 | — | ·/·/· | — |
| 5085 | `Princess64` | 公主64号 | 242 | — | ·/·/· | — |
| 5086 | `PaintingOfALass` | 小姑娘画像 | 245 | — | ·/·/· | — |
| 5087 | `DarkSideHallow` | 圣徒的黑暗面 | 240 | — | ·/·/· | — |
| 5088 | `BerniePetItem` | 伯尼的钮扣 | — | — | ·/·/· | — |
| 5089 | `GlommerPetItem` | 咕噜咪的花 | — | — | ·/·/· | — |
| 5090 | `DeerclopsPetItem` | 鹿角怪眼球 | — | — | ·/·/· | — |
| 5091 | `PigPetItem` | 怪物肉 | — | — | ·/·/· | — |
| 5092 | `MonsterLasagna` | 怪物千层面 | — | — | ·/·/· | — |
| 5093 | `FroggleBunwich` | 蛙腿三明治 | — | — | ·/·/· | — |
| 5094 | `TentacleSpike` | 触手钉锤 | — | — | ·/·/· | 20 |
| 5095 | `LucyTheAxe` | 露西斧 | — | — | ·/30/· | 27 |
| 5096 | `HamBat` | 火腿棍 | — | — | ·/·/· | 57 |
| 5097 | `BatBat` | 蝙蝠棍 | — | — | ·/·/· | 36 |
| 5098 | `ChesterPetItem` | 眼骨 | — | — | ·/·/· | — |
| 5099 | `GarlandHat` | 花冠 | — | — | ·/·/· | — |
| 5100 | `BoneHelm` | 骸骨头盔 | — | — | ·/·/· | — |
| 5101 | `Eyebrella` | 眼球伞 | — | — | ·/·/· | — |
| 5102 | `WilsonShirt` | 绅士马甲 | — | — | ·/·/· | — |
| 5103 | `WilsonPants` | 绅士裤子 | — | — | ·/·/· | — |
| 5104 | `WilsonBeardShort` | 绅士胡子 | — | — | ·/·/· | — |
| 5105 | `WilsonBeardLong` | 绅士长胡子 | — | — | ·/·/· | — |
| 5106 | `WilsonBeardMagnificent` | 绅士大胡子 | — | — | ·/·/· | — |
| 5107 | `Magiluminescence` | 魔光护符 | — | — | ·/·/· | — |
| 5108 | `DeerclopsTrophy` | 鹿角怪纪念章 | 240 | — | ·/·/· | — |
| 5109 | `DeerclopsMask` | 鹿角怪面具 | — | — | ·/·/· | — |
| 5110 | `DeerclopsMasterTrophy` | 鹿角怪圣物 | 617 | — | ·/·/· | — |
| 5111 | `DeerclopsBossBag` | 宝藏袋（鹿角怪） | — | — | ·/·/· | — |
| 5112 | `MusicBoxDeerclops` | 八音盒（鹿角怪） | 139 | — | ·/·/· | — |
| 5113 | `DontStarveShaderItem` | 收音机零件 | — | — | ·/·/· | — |
| 5114 | `AbigailsFlower` | 阿比盖尔的花 | — | — | ·/·/· | 6 |
| 5115 | `WillowShirt` | 纵火者毛衣 | — | — | ·/·/· | — |
| 5116 | `WillowSkirt` | 纵火者短裙 | — | — | ·/·/· | — |
| 5117 | `PewMaticHorn` | 气喇叭 | — | — | ·/·/· | 20 |
| 5118 | `WeatherPain` | 天候棒 | — | — | ·/·/· | 13 |
| 5119 | `HoundiusShootius` | 眼球激光塔 | — | — | ·/·/· | 24 |
| 5120 | `DeerThing` | 鹿零件 | — | — | ·/·/· | — |
| 5121 | `PaintingWilson` | 绅士科学家 | 242 | — | ·/·/· | — |
| 5122 | `PaintingWillow` | 纵火者 | 242 | — | ·/·/· | — |
| 5123 | `PaintingWendy` | 遗族 | 242 | — | ·/·/· | — |
| 5124 | `PaintingWolfgang` | 大力士 | 242 | — | ·/·/· | — |
| 5125 | `FartMinecart` | 放屁车 | — | — | ·/·/· | — |
| 5126 | `HandOfCreation` | 创造之手 | — | — | ·/·/· | — |
| 5127 | `VioletMoss` | 氖苔藓 | 625 | — | ·/·/· | — |
| 5128 | `RainbowMoss` | 氦苔藓 | 627 | — | ·/·/· | — |
| 5129 | `Flymeal` | 飞虫剑 | — | — | ·/·/· | 15 |
| 5130 | `WolfMountItem` | 莉莉丝的项链 | — | — | ·/·/· | — |
| 5131 | `ResplendentDessert` | 华美甜点 | — | — | ·/·/· | — |
| 5132 | `Stinkbug` | 臭虫 | — | — | ·/·/· | — |
| 5133 | `StinkbugCage` | 臭虫笼 | 629 | — | ·/·/· | — |
| 5134 | `Clentaminator2` | 泰拉改造器 | — | — | ·/·/· | — |
| 5135 | `VenomDartTrap` | 毒液飞镖机关 | 137 | — | ·/·/· | — |
| 5136 | `VulkelfEar` | 瓦尔精灵耳朵 | — | — | ·/·/· | — |
| 5137 | `StinkbugHousingBlocker` | 臭虫拦截器 | 630 | — | ·/·/· | — |
| 5138 | `StinkbugHousingBlockerEcho` | 幽灵臭虫拦截器 | 631 | — | ·/·/· | — |
| 5139 | `FishingBobber` | 钓鱼浮标 | — | — | ·/·/· | — |
| 5140 | `FishingBobberGlowingStar` | 发光钓鱼浮标 | — | — | ·/·/· | — |
| 5141 | `FishingBobberGlowingLava` | 熔岩苔藓钓鱼浮标 | — | — | ·/·/· | — |
| 5142 | `FishingBobberGlowingKrypton` | 氪苔藓钓鱼浮标 | — | — | ·/·/· | — |
| 5143 | `FishingBobberGlowingXenon` | 氙苔藓钓鱼浮标 | — | — | ·/·/· | — |
| 5144 | `FishingBobberGlowingArgon` | 氩苔藓钓鱼浮标 | — | — | ·/·/· | — |
| 5145 | `FishingBobberGlowingViolet` | 氖苔藓钓鱼浮标 | — | — | ·/·/· | — |
| 5146 | `FishingBobberGlowingRainbow` | 氦苔藓钓鱼浮标 | — | — | ·/·/· | — |
| 5147 | `WandofFrosting` | 结霜魔杖 | — | — | ·/·/· | 15 |
| 5148 | `CoralBathtub` | 礁浴缸 | 90 | — | ·/·/· | — |
| 5149 | `CoralBed` | 礁床 | 79 | — | ·/·/· | — |
| 5150 | `CoralBookcase` | 礁书架 | 101 | — | ·/·/· | — |
| 5151 | `CoralDresser` | 礁梳妆台 | 88 | — | ·/·/· | — |
| 5152 | `CoralCandelabra` | 礁烛台 | 100 | — | ·/·/· | — |
| 5153 | `CoralCandle` | 礁蜡烛 | 33 | — | ·/·/· | — |
| 5154 | `CoralChair` | 礁椅 | 15 | — | ·/·/· | — |
| 5155 | `CoralChandelier` | 礁吊灯 | 34 | — | ·/·/· | — |
| 5156 | `CoralChest` | 礁宝箱 | 467 | — | ·/·/· | — |
| 5157 | `CoralClock` | 礁钟 | 104 | — | ·/·/· | — |
| 5158 | `CoralDoor` | 礁门 | 10 | — | ·/·/· | — |
| 5159 | `CoralLamp` | 礁灯 | 93 | — | ·/·/· | — |
| 5160 | `CoralLantern` | 礁灯笼 | 42 | — | ·/·/· | — |
| 5161 | `CoralPiano` | 礁钢琴 | 87 | — | ·/·/· | — |
| 5162 | `CoralPlatform` | 礁平台 | 19 | — | ·/·/· | — |
| 5163 | `CoralSink` | 礁水槽 | 172 | — | ·/·/· | — |
| 5164 | `CoralSofa` | 礁沙发 | 89 | — | ·/·/· | — |
| 5165 | `CoralTable` | 礁桌子 | 469 | — | ·/·/· | — |
| 5166 | `CoralWorkbench` | 礁工作台 | 18 | — | ·/·/· | — |
| 5167 | `Fake_CoralChest` | 受困礁宝箱 | 468 | — | ·/·/· | — |
| 5168 | `CoralToilet` | 礁马桶 | 497 | — | ·/·/· | — |
| 5169 | `BalloonBathtub` | 气球浴缸 | 90 | — | ·/·/· | — |
| 5170 | `BalloonBed` | 气球床 | 79 | — | ·/·/· | — |
| 5171 | `BalloonBookcase` | 气球书架 | 101 | — | ·/·/· | — |
| 5172 | `BalloonDresser` | 气球梳妆台 | 88 | — | ·/·/· | — |
| 5173 | `BalloonCandelabra` | 气球烛台 | 100 | — | ·/·/· | — |
| 5174 | `BalloonCandle` | 气球蜡烛 | 33 | — | ·/·/· | — |
| 5175 | `BalloonChair` | 气球椅 | 15 | — | ·/·/· | — |
| 5176 | `BalloonChandelier` | 气球吊灯 | 34 | — | ·/·/· | — |
| 5177 | `BalloonChest` | 气球宝箱 | 467 | — | ·/·/· | — |
| 5178 | `BalloonClock` | 气球钟 | 104 | — | ·/·/· | — |
| 5179 | `BalloonDoor` | 气球门 | 10 | — | ·/·/· | — |
| 5180 | `BalloonLamp` | 气球灯 | 93 | — | ·/·/· | — |
| 5181 | `BalloonLantern` | 气球灯笼 | 42 | — | ·/·/· | — |
| 5182 | `BalloonPiano` | 气球钢琴 | 87 | — | ·/·/· | — |
| 5183 | `BalloonPlatform` | 气球平台 | 19 | — | ·/·/· | — |
| 5184 | `BalloonSink` | 气球水槽 | 172 | — | ·/·/· | — |
| 5185 | `BalloonSofa` | 气球沙发 | 89 | — | ·/·/· | — |
| 5186 | `BalloonTable` | 气球桌 | 469 | — | ·/·/· | — |
| 5187 | `BalloonWorkbench` | 气球工作台 | 18 | — | ·/·/· | — |
| 5188 | `Fake_BalloonChest` | 受困气球宝箱 | 468 | — | ·/·/· | — |
| 5189 | `BalloonToilet` | 气球马桶 | 497 | — | ·/·/· | — |
| 5190 | `AshWoodBathtub` | 白蜡木浴缸 | 90 | — | ·/·/· | — |
| 5191 | `AshWoodBed` | 白蜡木床 | 79 | — | ·/·/· | — |
| 5192 | `AshWoodBookcase` | 白蜡木书架 | 101 | — | ·/·/· | — |
| 5193 | `AshWoodDresser` | 针叶木梳妆台 | 88 | — | ·/·/· | — |
| 5194 | `AshWoodCandelabra` | 白蜡木烛台 | 100 | — | ·/·/· | — |
| 5195 | `AshWoodCandle` | 白蜡木蜡烛 | 33 | — | ·/·/· | — |
| 5196 | `AshWoodChair` | 白蜡木椅子 | 15 | — | ·/·/· | — |
| 5197 | `AshWoodChandelier` | 白蜡木吊灯 | 34 | — | ·/·/· | — |
| 5198 | `AshWoodChest` | 白蜡木宝箱 | 467 | — | ·/·/· | — |
| 5199 | `AshWoodClock` | 白蜡木钟 | 104 | — | ·/·/· | — |
| 5200 | `AshWoodDoor` | 白蜡木门 | 10 | — | ·/·/· | — |
| 5201 | `AshWoodLamp` | 白蜡木灯 | 93 | — | ·/·/· | — |
| 5202 | `AshWoodLantern` | 白蜡木灯笼 | 42 | — | ·/·/· | — |
| 5203 | `AshWoodPiano` | 白蜡木钢琴 | 87 | — | ·/·/· | — |
| 5204 | `AshWoodPlatform` | 白蜡木平台 | 19 | — | ·/·/· | — |
| 5205 | `AshWoodSink` | 白蜡木水槽 | 172 | — | ·/·/· | — |
| 5206 | `AshWoodSofa` | 白蜡木沙发 | 89 | — | ·/·/· | — |
| 5207 | `AshWoodTable` | 白蜡木桌子 | 469 | — | ·/·/· | — |
| 5208 | `AshWoodWorkbench` | 白蜡木工作台 | 18 | — | ·/·/· | — |
| 5209 | `Fake_AshWoodChest` | 受困白蜡木宝箱 | 468 | — | ·/·/· | — |
| 5210 | `AshWoodToilet` | 白蜡木马桶 | 497 | — | ·/·/· | — |
| 5211 | `BiomeSightPotion` | 生物群落观测药水 | — | — | ·/·/· | — |
| 5212 | `ScarletMacaw` | 猩红金刚鹦鹉 | — | — | ·/·/· | — |
| 5213 | `ScarletMacawCage` | 猩红金刚鹦鹉笼 | 632 | — | ·/·/· | — |
| 5214 | `AshGrassSeeds` | 白蜡草种子 | 633 | — | ·/·/· | — |
| 5215 | `AshWood` | 白蜡木 | 635 | — | ·/·/· | — |
| 5216 | `AshWoodWall` | 白蜡木墙 | — | 316 | ·/·/· | — |
| 5217 | `AshWoodFence` | 白蜡木栅栏 | — | 317 | ·/·/· | — |
| 5218 | `Outcast` | 流浪者 | 240 | — | ·/·/· | — |
| 5219 | `FairyGuides` | 仙灵指引 | 240 | — | ·/·/· | — |
| 5220 | `AHorribleNightforAlchemy` | 恐怖炼药之夜 | 240 | — | ·/·/· | — |
| 5221 | `MorningHunt` | 晨猎 | 240 | — | ·/·/· | — |
| 5222 | `SuspiciouslySparkly` | 可疑闪光 | 240 | — | ·/·/· | — |
| 5223 | `Requiem` | 安魂 | 240 | — | ·/·/· | — |
| 5224 | `CatSword` | 猫剑 | 240 | — | ·/·/· | — |
| 5225 | `KargohsSummon` | Kargoh's Summon | 240 | — | ·/·/· | — |
| 5226 | `HighPitch` | 高音 | 240 | — | ·/·/· | — |
| 5227 | `AMachineforTerrarians` | 泰拉瑞亚人的机器 | 240 | — | ·/·/· | — |
| 5228 | `TerraBladeChronicles` | 泰拉刃史志 | 240 | — | ·/·/· | — |
| 5229 | `BennyWarhol` | Benny Warhol | 240 | — | ·/·/· | — |
| 5230 | `LizardKing` | 蜥蜴王 | 240 | — | ·/·/· | — |
| 5231 | `MySon` | 我的儿子 | 240 | — | ·/·/· | — |
| 5232 | `Duality` | 对偶 | 240 | — | ·/·/· | — |
| 5233 | `ParsecPals` | Parsec Pals | 240 | — | ·/·/· | — |
| 5234 | `RemnantsofDevotion` | 虔诚的遗骸 | 242 | — | ·/·/· | — |
| 5235 | `NotSoLostInParadise` | 并未如此迷失在乐园 | 242 | — | ·/·/· | — |
| 5236 | `OcularResonance` | 视觉共振 | 242 | — | ·/·/· | — |
| 5237 | `WingsofEvil` | 邪恶之翼 | 242 | — | ·/·/· | — |
| 5238 | `Constellation` | 星座 | 242 | — | ·/·/· | — |
| 5239 | `Eyezorhead` | 眼怪头 | 242 | — | ·/·/· | — |
| 5240 | `DreadoftheRedSea` | 红海恐惧 | 242 | — | ·/·/· | — |
| 5241 | `DoNotEattheVileMushroom` | 不要吃卑鄙的蘑菇！ | 242 | — | ·/·/· | — |
| 5242 | `YuumaTheBlueTiger` | Yuuma, The Blue Tiger | 242 | — | ·/·/· | — |
| 5243 | `MoonmanandCompany` | 月球人公司 | 242 | — | ·/·/· | — |
| 5244 | `SunshineofIsrapony` | Sunshine of Israpony | 242 | — | ·/·/· | — |
| 5245 | `Purity` | 纯净 | 242 | — | ·/·/· | — |
| 5246 | `SufficientlyAdvanced` | 极其先进 | 245 | — | ·/·/· | — |
| 5247 | `StrangeGrowth` | 奇特成长 | 245 | — | ·/·/· | — |
| 5248 | `HappyLittleTree` | 快乐小树 | 245 | — | ·/·/· | — |
| 5249 | `StrangeDeadFellows` | 奇怪死者 | 245 | — | ·/·/· | — |
| 5250 | `Secrets` | 秘密 | 245 | — | ·/·/· | — |
| 5251 | `Thunderbolt` | 雷震 | 245 | — | ·/·/· | — |
| 5252 | `Crustography` | 甲壳纲地理学 | 245 | — | ·/·/· | — |
| 5253 | `TheWerewolf` | 狼人 | 245 | — | ·/·/· | — |
| 5254 | `BlessingfromTheHeavens` | 天堂的祝福 | 245 | — | ·/·/· | — |
| 5255 | `LoveisintheTrashSlot` | 爱在垃圾槽 | 245 | — | ·/·/· | — |
| 5256 | `Fangs` | 獠牙 | 245 | — | ·/·/· | — |
| 5257 | `HailtotheKing` | 国王万岁 | 245 | — | ·/·/· | — |
| 5258 | `SeeTheWorldForWhatItIs` | 看清世界的真谛 | 246 | — | ·/·/· | — |
| 5259 | `WhatLurksBelow` | 下面潜伏着什么 | 246 | — | ·/·/· | — |
| 5260 | `ThisIsGettingOutOfHand` | 局面失控 | 246 | — | ·/·/· | — |
| 5261 | `Buddies` | 铁哥们 | 246 | — | ·/·/· | — |
| 5262 | `MidnightSun` | 午夜太阳 | 246 | — | ·/·/· | — |
| 5263 | `CouchGag` | 沙发塞物 | 246 | — | ·/·/· | — |
| 5264 | `SilentFish` | 沉默的鱼 | 246 | — | ·/·/· | — |
| 5265 | `TheDuke` | 公爵 | 246 | — | ·/·/· | — |
| 5266 | `RoyalRomance` | 皇家浪漫 | 246 | — | ·/·/· | — |
| 5267 | `Bioluminescence` | 生物发光 | 246 | — | ·/·/· | — |
| 5268 | `Wildflowers` | 野花 | 246 | — | ·/·/· | — |
| 5269 | `VikingVoyage` | 维京之旅 | 246 | — | ·/·/· | — |
| 5270 | `Bifrost` | 彩虹桥 | 246 | — | ·/·/· | — |
| 5271 | `Heartlands` | 心脏地带 | 246 | — | ·/·/· | — |
| 5272 | `ForestTroll` | 森林巨魔 | 246 | — | ·/·/· | — |
| 5273 | `AuroraBorealis` | 北极光 | 246 | — | ·/·/· | — |
| 5274 | `LadyOfTheLake` | 湖泊夫人 | 242 | — | ·/·/· | — |
| 5275 | `JojaCola` | 可咂可乐 | — | — | ·/·/· | — |
| 5276 | `JunimoPetItem` | 星珠 | — | — | ·/·/· | — |
| 5277 | `SpicyPepper` | 辣椒 | — | — | ·/·/· | — |
| 5278 | `Pomegranate` | 石榴 | — | — | ·/·/· | — |
| 5279 | `AshWoodHelmet` | 白蜡木头盔 | — | — | ·/·/· | — |
| 5280 | `AshWoodBreastplate` | 白蜡木胸甲 | — | — | ·/·/· | — |
| 5281 | `AshWoodGreaves` | 白蜡木护胫 | — | — | ·/·/· | — |
| 5282 | `AshWoodBow` | 白蜡木弓 | — | — | ·/·/· | 10 |
| 5283 | `AshWoodHammer` | 白蜡木锤 | — | — | ·/·/45 | 9 |
| 5284 | `AshWoodSword` | 白蜡木剑 | — | — | ·/·/· | 13 |
| 5285 | `MoonGlobe` | 月球仪 | — | — | ·/·/· | — |
| 5286 | `RepairedLifeCrystal` | 修复的生命水晶 | 12 | — | ·/·/· | — |
| 5287 | `RepairedManaCrystal` | 修复的魔力水晶 | 639 | — | ·/·/· | — |
| 5288 | `TerraFartMinecart` | 泰拉放屁车 | — | — | ·/·/· | — |
| 5289 | `MinecartPowerup` | 矿车升级包 | — | — | ·/·/· | — |
| 5290 | `JimsCap` | 吉姆的帽子 | — | — | ·/·/· | — |
| 5291 | `EchoWall` | 回音墙 | — | 318 | ·/·/· | — |
| 5292 | `EchoPlatform` | 回声平台 | 19 | — | ·/·/· | — |
| 5293 | `MushroomTorch` | 蘑菇火把 | 4 | — | ·/·/· | — |
| 5294 | `HiveFive` | 蜂巢球 | — | — | ·/·/· | 24 |
| 5295 | `AcornAxe` | 再生之斧 | 2 | — | ·/30/· | 20 |
| 5296 | `ChlorophyteExtractinator` | 叶绿素提炼机 | 642 | — | ·/·/· | — |
| 5297 | `BlueEgg` | 蓝色鸡蛋 | — | — | ·/·/· | — |
| 5298 | `Trimarang` | 三尖回旋镖 | — | — | ·/·/· | 21 |
| 5299 | `MushroomCampfire` | 蘑菇篝火 | 215 | — | ·/·/· | — |
| 5300 | `BlueMacaw` | 蓝色金刚鹦鹉 | — | — | ·/·/· | — |
| 5301 | `BlueMacawCage` | 蓝色金刚鹦鹉笼 | 640 | — | ·/·/· | — |
| 5302 | `BottomlessHoneyBucket` | 无底蜂蜜桶 | — | — | ·/·/· | — |
| 5303 | `HoneyAbsorbantSponge` | 蜂蜜吸水棉 | — | — | ·/·/· | — |
| 5304 | `UltraAbsorbantSponge` | 超级吸水棉 | — | — | ·/·/· | — |
| 5305 | `GoblorcEar` | 哥布魔耳朵 | — | — | ·/·/· | — |
| 5306 | `ReefBlock` | 礁块 | 641 | — | ·/·/· | — |
| 5307 | `ReefWall` | 礁墙 | — | 319 | ·/·/· | — |
| 5308 | `PlacePainting` | r/泰拉瑞亚 | 242 | — | ·/·/· | — |
| 5309 | `DontHurtNatureBook` | 环境保护指南 | — | — | ·/·/· | — |
| 5310 | `PrincessStyle` | 公主风 | 242 | — | ·/·/· | — |
| 5311 | `Toucan` | 犀鸟 | — | — | ·/·/· | — |
| 5312 | `YellowCockatiel` | 黄色玄凤鹦鹉 | — | — | ·/·/· | — |
| 5313 | `GrayCockatiel` | 灰色玄凤鹦鹉 | — | — | ·/·/· | — |
| 5314 | `ToucanCage` | 犀鸟笼 | 643 | — | ·/·/· | — |
| 5315 | `YellowCockatielCage` | 黄色玄凤鹦鹉笼 | 644 | — | ·/·/· | — |
| 5316 | `GrayCockatielCage` | 灰色玄凤鹦鹉笼 | 645 | — | ·/·/· | — |
| 5317 | `MacawStatue` | 金刚鹦鹉雕像 | 105 | — | ·/·/· | — |
| 5318 | `ToucanStatue` | 犀鸟雕像 | 105 | — | ·/·/· | — |
| 5319 | `CockatielStatue` | 玄凤鹦鹉雕像 | 105 | — | ·/·/· | — |
| 5320 | `PlaceableHealingPotion` | 装饰治疗药水 | 13 | — | ·/·/· | — |
| 5321 | `PlaceableManaPotion` | 装饰魔力药水 | 13 | — | ·/·/· | — |
| 5322 | `ShadowCandle` | 影烛 | 646 | — | ·/·/· | — |
| 5323 | `DontHurtComboBook` | 和平共处指南 | — | — | ·/·/· | — |
| 5324 | `RubblemakerSmall` | 碎石机（小） | 647 | — | ·/·/· | — |
| 5325 | `ClosedVoidBag` | 闭合的虚空袋 | — | — | ·/·/· | — |
| 5326 | `ArtisanLoaf` | 工匠面包 | — | — | ·/·/· | — |
| 5327 | `TNTBarrel` | TNT枪管 | 654 | — | ·/·/· | — |
| 5328 | `ChestLock` | 宝箱锁 | — | — | ·/·/· | — |
| 5329 | `RubblemakerMedium` | 碎石机（中） | 647 | — | ·/·/· | — |
| 5330 | `RubblemakerLarge` | 碎石机（大） | 647 | — | ·/·/· | — |
| 5331 | `HorseshoeBundle` | 马掌气球束 | — | — | ·/·/· | — |
| 5332 | `SpiffoPlush` | 棕绒浣熊 | — | — | ·/·/· | — |
| 5333 | `GlowTulip` | 发光郁金香 | — | — | ·/·/· | — |
| 5334 | `MechdusaSummon` | 奥库瑞姆的剃刀 | — | — | ·/·/· | — |
| 5335 | `RodOfHarmony` | 和谐杖 | — | — | ·/·/· | — |
| 5336 | `CombatBookVolumeTwo` | 先进战斗技术：卷二 | — | — | ·/·/· | — |
| 5337 | `AegisCrystal` | 生命水晶 | — | — | ·/·/· | — |
| 5338 | `AegisFruit` | 埃癸斯果 | — | — | ·/·/· | — |
| 5339 | `ArcaneCrystal` | 奥术水晶 | — | — | ·/·/· | — |
| 5340 | `GalaxyPearl` | 银河珍珠 | — | — | ·/·/· | — |
| 5341 | `GummyWorm` | 黏性蠕虫 | — | — | ·/·/· | — |
| 5342 | `Ambrosia` | 珍馐 | — | — | ·/·/· | — |
| 5343 | `PeddlersSatchel` | 商贩背包 | — | — | ·/·/· | — |
| 5344 | `EchoCoating` | 回声涂料 | — | — | ·/·/· | — |
| 5345 | `EchoMonolith` | 回声室 | 657 | — | ·/·/· | — |
| 5346 | `GasTrap` | 气阱 | — | — | ·/·/· | — |
| 5347 | `ShimmerMonolith` | 伊瑟尔天塔柱 | 658 | — | ·/·/· | — |
| 5348 | `ShimmerArrow` | 微光箭 | — | — | ·/·/· | 12 |
| 5349 | `ShimmerBlock` | 镒块 | 659 | — | ·/·/· | — |
| 5350 | `Shimmerfly` | 仙灵 | — | — | ·/·/· | — |
| 5351 | `ShimmerflyinaBottle` | 仙灵瓶 | 660 | — | ·/·/· | — |
| 5352 | `ShimmerSlimeBanner` | 微光史莱姆旗 | 91 | — | ·/·/· | — |
| 5353 | `ShimmerTorch` | 伊瑟尔火把 | 4 | — | ·/·/· | — |
| 5354 | `ReflectiveShades` | 反光墨镜 | — | — | ·/·/· | — |
| 5355 | `ShimmerCloak` | 多彩斗篷 | — | — | ·/·/· | — |
| 5356 | `UsedGasTrap` | 用过的气阱 | — | — | ·/·/· | — |
| 5357 | `ShimmerCampfire` | 伊瑟尔篝火 | 215 | — | ·/·/· | — |
| 5358 | `Shellphone` | 贝壳电话（家园） | — | — | ·/·/· | — |
| 5359 | `ShellphoneSpawn` | 贝壳电话（生成位置） | — | — | ·/·/· | — |
| 5360 | `ShellphoneOcean` | 贝壳电话（海洋） | — | — | ·/·/· | — |
| 5361 | `ShellphoneHell` | 贝壳电话（地狱） | — | — | ·/·/· | — |
| 5362 | `MusicBoxShimmer` | 八音盒（伊瑟尔） | 139 | — | ·/·/· | — |
| 5363 | `SpiderWallUnsafe` | 蜘蛛墙 | — | 62 | ·/·/· | — |
| 5364 | `BottomlessShimmerBucket` | 无底微光桶 | — | — | ·/·/· | — |
| 5365 | `BlueBrickWallUnsafe` | 被诅咒的蓝砖墙 | — | 7 | ·/·/· | — |
| 5366 | `BlueSlabWallUnsafe` | 被诅咒的蓝板墙 | — | 94 | ·/·/· | — |
| 5367 | `BlueTiledWallUnsafe` | 被诅咒的蓝瓷砖墙 | — | 95 | ·/·/· | — |
| 5368 | `PinkBrickWallUnsafe` | 被诅咒的粉砖墙 | — | 9 | ·/·/· | — |
| 5369 | `PinkSlabWallUnsafe` | 被诅咒的粉板墙 | — | 96 | ·/·/· | — |
| 5370 | `PinkTiledWallUnsafe` | 被诅咒的粉瓷砖墙 | — | 97 | ·/·/· | — |
| 5371 | `GreenBrickWallUnsafe` | 被诅咒的绿砖墙 | — | 8 | ·/·/· | — |
| 5372 | `GreenSlabWallUnsafe` | 被诅咒的绿板墙 | — | 98 | ·/·/· | — |
| 5373 | `GreenTiledWallUnsafe` | 被诅咒的绿瓷砖墙 | — | 99 | ·/·/· | — |
| 5374 | `SandstoneWallUnsafe` | 危险的沙岩墙 | — | 187 | ·/·/· | — |
| 5375 | `HardenedSandWallUnsafe` | 危险的硬化沙墙 | — | 216 | ·/·/· | — |
| 5376 | `LihzahrdWallUnsafe` | 被禁的蜥蜴砖墙 | — | 87 | ·/·/· | — |
| 5377 | `SpelunkerFlare` | 洞穴探险照明弹 | — | — | ·/·/· | 1 |
| 5378 | `CursedFlare` | 被诅咒的照明弹 | — | — | ·/·/· | 1 |
| 5379 | `RainbowFlare` | 彩虹照明弹 | — | — | ·/·/· | 1 |
| 5380 | `ShimmerFlare` | 微光照明弹 | — | — | ·/·/· | 1 |
| 5381 | `Moondial` | 附魔月晷 | 663 | — | ·/·/· | — |
| 5382 | `WaffleIron` | 华夫铁 | — | — | ·/·/· | 50 |
| 5383 | `BouncyBoulder` | 弹力巨石 | 664 | — | ·/·/· | — |
| 5384 | `LifeCrystalBoulder` | 巨型生命水晶 | 665 | — | ·/·/· | — |
| 5385 | `DizzyHat` | Dizzy的稀有壁虎帽 | — | — | ·/·/· | — |
| 5386 | `LincolnsHoodie` | 雷尼布罗连帽衫 | — | — | ·/·/· | — |
| 5387 | `LincolnsPants` | 雷尼布罗裤 | — | — | ·/·/· | — |
| 5388 | `SunOrnament` | 太阳之眼 | 240 | — | ·/·/· | — |
| 5389 | `HoplitePizza` | 奶酪比萨海报 | 242 | — | ·/·/· | — |
| 5390 | `LincolnsHood` | 雷尼布罗兜帽 | — | — | ·/·/· | — |
| 5391 | `UncumberingStone` | 清障石 | — | — | ·/·/· | — |
| 5392 | `SandSolution` | 黄色溶液 | — | — | ·/·/· | — |
| 5393 | `SnowSolution` | 白色溶液 | — | — | ·/·/· | — |
| 5394 | `DirtSolution` | 棕色溶液 | — | — | ·/·/· | — |
| 5395 | `PoopBlock` | 臭臭 | 666 | — | ·/·/· | — |
| 5396 | `PoopWall` | 臭臭墙 | — | 320 | ·/·/· | — |
| 5397 | `ShimmerWall` | 镒墙 | — | 321 | ·/·/· | — |
| 5398 | `ShimmerBrick` | 镒砖 | 667 | — | ·/·/· | — |
| 5399 | `ShimmerBrickWall` | 镒砖墙 | — | 322 | ·/·/· | — |
| 5400 | `DirtiestBlock` | 最脏的块 | — | — | ·/·/· | — |
| 5401 | `LunarRustBrick` | 月亮锈砖 | 669 | — | ·/·/· | — |
| 5402 | `DarkCelestialBrick` | 暗黑天界砖 | 670 | — | ·/·/· | — |
| 5403 | `AstraBrick` | 阿斯特拉砖 | 671 | — | ·/·/· | — |
| 5404 | `CosmicEmberBrick` | 宇宙余烬砖 | 672 | — | ·/·/· | — |
| 5405 | `CryocoreBrick` | 冷核砖 | 673 | — | ·/·/· | — |
| 5406 | `MercuryBrick` | 水银砖 | 674 | — | ·/·/· | — |
| 5407 | `StarRoyaleBrick` | 星星皇家砖 | 675 | — | ·/·/· | — |
| 5408 | `HeavenforgeBrick` | 天熔砖 | 676 | — | ·/·/· | — |
| 5409 | `LunarRustBrickWall` | 月亮锈砖墙 | — | 323 | ·/·/· | — |
| 5410 | `DarkCelestialBrickWall` | 暗黑天界砖墙 | — | 324 | ·/·/· | — |
| 5411 | `AstraBrickWall` | 阿斯特拉砖墙 | — | 325 | ·/·/· | — |
| 5412 | `CosmicEmberBrickWall` | 宇宙余烬砖墙 | — | 326 | ·/·/· | — |
| 5413 | `CryocoreBrickWall` | 冷核砖墙 | — | 327 | ·/·/· | — |
| 5414 | `MercuryBrickWall` | 水银砖墙 | — | 328 | ·/·/· | — |
| 5415 | `StarRoyaleBrickWall` | 星星皇家砖墙 | — | 329 | ·/·/· | — |
| 5416 | `HeavenforgeBrickWall` | 天熔砖墙 | — | 330 | ·/·/· | — |
| 5417 | `AncientBlueDungeonBrick` | 远古蓝砖 | 677 | — | ·/·/· | — |
| 5418 | `AncientBlueDungeonBrickWall` | 远古蓝砖墙 | — | 331 | ·/·/· | — |
| 5419 | `AncientGreenDungeonBrick` | 远古绿砖 | 678 | — | ·/·/· | — |
| 5420 | `AncientGreenDungeonBrickWall` | 远古绿砖墙 | — | 332 | ·/·/· | — |
| 5421 | `AncientPinkDungeonBrick` | 远古粉砖 | 679 | — | ·/·/· | — |
| 5422 | `AncientPinkDungeonBrickWall` | 远古粉砖墙 | — | 333 | ·/·/· | — |
| 5423 | `AncientGoldBrick` | 远古金砖 | 680 | — | ·/·/· | — |
| 5424 | `AncientGoldBrickWall` | 远古金砖墙 | — | 334 | ·/·/· | — |
| 5425 | `AncientSilverBrick` | 远古银砖 | 681 | — | ·/·/· | — |
| 5426 | `AncientSilverBrickWall` | 远古银砖墙 | — | 335 | ·/·/· | — |
| 5427 | `AncientCopperBrick` | 远古铜砖 | 682 | — | ·/·/· | — |
| 5428 | `AncientCopperBrickWall` | 远古铜砖墙 | — | 336 | ·/·/· | — |
| 5429 | `AncientCobaltBrick` | 远古钴砖 | 685 | — | ·/·/· | — |
| 5430 | `AncientCobaltBrickWall` | 远古钴砖墙 | — | 339 | ·/·/· | — |
| 5431 | `AncientMythrilBrick` | 远古秘银砖 | 686 | — | ·/·/· | — |
| 5432 | `AncientMythrilBrickWall` | 远古秘银砖墙 | — | 340 | ·/·/· | — |
| 5433 | `AncientObsidianBrick` | 远古黑曜石砖 | 683 | — | ·/·/· | — |
| 5434 | `AncientObsidianBrickWall` | 远古黑曜石砖墙 | — | 337 | ·/·/· | — |
| 5435 | `AncientHellstoneBrick` | 远古狱石砖 | 684 | — | ·/·/· | — |
| 5436 | `AncientHellstoneBrickWall` | 远古狱石砖墙 | — | 338 | ·/·/· | — |
| 5437 | `ShellphoneDummy` | 外壳电话 | — | — | ·/·/· | — |
| 5438 | `Fertilizer` | 肥料 | — | — | ·/·/· | — |
| 5439 | `LavaMossBlock` | 熔岩苔藓砖 | 687 | — | ·/·/· | — |
| 5440 | `ArgonMossBlock` | 氩苔藓砖 | 688 | — | ·/·/· | — |
| 5441 | `KryptonMossBlock` | 氪苔藓砖 | 689 | — | ·/·/· | — |
| 5442 | `XenonMossBlock` | 氙苔藓砖 | 690 | — | ·/·/· | — |
| 5443 | `VioletMossBlock` | 氖苔藓砖 | 691 | — | ·/·/· | — |
| 5444 | `RainbowMossBlock` | 氦苔藓砖 | 692 | — | ·/·/· | — |
| 5445 | `LavaMossBlockWall` | 熔岩苔藓砖墙 | — | 341 | ·/·/· | — |
| 5446 | `ArgonMossBlockWall` | 氩苔藓砖墙 | — | 342 | ·/·/· | — |
| 5447 | `KryptonMossBlockWall` | 氪苔藓砖墙 | — | 343 | ·/·/· | — |
| 5448 | `XenonMossBlockWall` | 氙苔藓砖墙 | — | 344 | ·/·/· | — |
| 5449 | `VioletMossBlockWall` | 氖苔藓砖墙 | — | 345 | ·/·/· | — |
| 5450 | `RainbowMossBlockWall` | 氦苔藓砖墙 | — | 346 | ·/·/· | — |
| 5451 | `JimsDrone` | 四人竞速无人机 | — | — | ·/·/· | — |
| 5452 | `JimsDroneVisor` | 第一人称视角护目镜 | — | — | ·/·/· | — |
| 5453 | `DontHurtCrittersBookInactive` | 小动物友谊指南（非活动） | — | — | ·/·/· | — |
| 5454 | `DontHurtNatureBookInactive` | 环境保护指南（非活动） | — | — | ·/·/· | — |
| 5455 | `DontHurtComboBookInactive` | 和平共处指南（非活动） | — | — | ·/·/· | — |
| 5456 | `ModLoader/Jofairden_Body` | 仙女座铠甲 | — | — | ·/·/· | — |
| 5457 | `ModLoader/Jofairden_Head` | 仙女座头盔 | — | — | ·/·/· | — |
| 5458 | `ModLoader/Jofairden_Legs` | 仙女座护腿 | — | — | ·/·/· | — |
| 5459 | `ModLoader/AetherBreaker_Body` | AetherBreaker之铠 | — | — | ·/·/· | — |
| 5460 | `ModLoader/AetherBreaker_Head` | AetherBreaker之盔 | — | — | ·/·/· | — |
| 5461 | `ModLoader/AetherBreaker_Legs` | AetherBreaker护腿 | — | — | ·/·/· | — |
| 5462 | `ModLoader/AetherBreaker_Wings` | AetherBreaker之翼 | — | — | ·/·/· | — |
| 5463 | `ModLoader/AlejandroAkbal_Back` | AlejandroAkbal披风 | — | — | ·/·/· | — |
| 5464 | `ModLoader/AlejandroAkbal_Body` | AlejandroAkbal之铠 | — | — | ·/·/· | — |
| 5465 | `ModLoader/AlejandroAkbal_Head` | AlejandroAkbal之盔 | — | — | ·/·/· | — |
| 5466 | `ModLoader/AlejandroAkbal_Legs` | AlejandroAkbal护腿 | — | — | ·/·/· | — |
| 5467 | `ModLoader/Coolmike5000_Body` | Coolmike5000之铠 | — | — | ·/·/· | — |
| 5468 | `ModLoader/Coolmike5000_Head` | Coolmike5000之盔 | — | — | ·/·/· | — |
| 5469 | `ModLoader/Coolmike5000_Legs` | Coolmike5000护腿 | — | — | ·/·/· | — |
| 5470 | `ModLoader/Coolmike5000_Wings` | Coolmike5000之翼 | — | — | ·/·/· | — |
| 5471 | `ModLoader/dinidini_Body` | dinidini之铠 | — | — | ·/·/· | — |
| 5472 | `ModLoader/dinidini_Head` | dinidini之盔 | — | — | ·/·/· | — |
| 5473 | `ModLoader/dinidini_Legs` | dinidini护腿 | — | — | ·/·/· | — |
| 5474 | `ModLoader/dinidini_Wings` | dinidini之翼 | — | — | ·/·/· | — |
| 5475 | `ModLoader/dschosen_Body` | dschosen之铠 | — | — | ·/·/· | — |
| 5476 | `ModLoader/dschosen_Head` | dschosen之盔 | — | — | ·/·/· | — |
| 5477 | `ModLoader/dschosen_Legs` | dschosen护腿 | — | — | ·/·/· | — |
| 5478 | `ModLoader/dschosen_Wings` | dschosen之翼 | — | — | ·/·/· | — |
| 5479 | `ModLoader/Elfinlocks_Body` | Elfinlocks之铠 | — | — | ·/·/· | — |
| 5480 | `ModLoader/Elfinlocks_Head` | Elfinlocks之盔 | — | — | ·/·/· | — |
| 5481 | `ModLoader/Elfinlocks_Legs` | Elfinlocks护腿 | — | — | ·/·/· | — |
| 5482 | `ModLoader/Frosty_Hat` | Frosty的帽子 | — | — | ·/·/· | — |
| 5483 | `ModLoader/Frosty_Hoodie` | Frosty的外衫 | — | — | ·/·/· | — |
| 5484 | `ModLoader/Frosty_Pants` | Frosty的裤子 | — | — | ·/·/· | — |
| 5485 | `ModLoader/Glory_Body` | Glory之铠 | — | — | ·/·/· | — |
| 5486 | `ModLoader/Glory_Head` | Glory之盔 | — | — | ·/·/· | — |
| 5487 | `ModLoader/Glory_Legs` | Glory护腿 | — | — | ·/·/· | — |
| 5488 | `ModLoader/Guildpack_Body` | Guildpack之铠 | — | — | ·/·/· | — |
| 5489 | `ModLoader/Guildpack_Head` | Guildpack之盔 | — | — | ·/·/· | — |
| 5490 | `ModLoader/Guildpack_Legs` | Guildpack护腿 | — | — | ·/·/· | — |
| 5491 | `ModLoader/HER0zero_Body` | HER0zero之铠 | — | — | ·/·/· | — |
| 5492 | `ModLoader/HER0zero_Head` | HER0zero之盔 | — | — | ·/·/· | — |
| 5493 | `ModLoader/HER0zero_Legs` | HER0zero护腿 | — | — | ·/·/· | — |
| 5494 | `ModLoader/Linus_Body` | Hunter的外衣 | — | — | ·/·/· | — |
| 5495 | `ModLoader/Linus_Head` | Hunter的帽子 | — | — | ·/·/· | — |
| 5496 | `ModLoader/Linus_Legs` | Hunter的长裤 | — | — | ·/·/· | — |
| 5497 | `ModLoader/Linus_Wings` | 遥远的呼唤 | — | — | ·/·/· | — |
| 5498 | `ModLoader/Mayne_Body` | Mayne之铠 | — | — | ·/·/· | — |
| 5499 | `ModLoader/Mayne_Head` | Mayne之盔 | — | — | ·/·/· | — |
| 5500 | `ModLoader/Mayne_Legs` | Mayne护腿 | — | — | ·/·/· | — |
| 5501 | `ModLoader/Mayne_Wings` | Mayne之翼 | — | — | ·/·/· | — |
| 5502 | `ModLoader/Orian_Body` | Orian之铠 | — | — | ·/·/· | — |
| 5503 | `ModLoader/Orian_Head` | Orian之盔 | — | — | ·/·/· | — |
| 5504 | `ModLoader/Orian_Legs` | Orian护腿 | — | — | ·/·/· | — |
| 5505 | `ModLoader/POCKETS_Body` | POCKETS之铠 | — | — | ·/·/· | — |
| 5506 | `ModLoader/POCKETS_Head` | POCKETS之盔 | — | — | ·/·/· | — |
| 5507 | `ModLoader/POCKETS_Legs` | POCKETS护腿 | — | — | ·/·/· | — |
| 5508 | `ModLoader/POCKETS_Wings` | POCKETS之翼 | — | — | ·/·/· | — |
| 5509 | `ModLoader/Polyblank_Body` | Polyblank之铠 | — | — | ·/·/· | — |
| 5510 | `ModLoader/Polyblank_Head` | Polyblank之盔 | — | — | ·/·/· | — |
| 5511 | `ModLoader/Polyblank_Legs` | Polyblank护腿 | — | — | ·/·/· | — |
| 5512 | `ModLoader/Remeus_Body` | Remeus之铠 | — | — | ·/·/· | — |
| 5513 | `ModLoader/Remeus_Head` | Remeus之盔 | — | — | ·/·/· | — |
| 5514 | `ModLoader/Remeus_Legs` | Remeus护腿 | — | — | ·/·/· | — |
| 5515 | `ModLoader/Saethar_Body` | Saethar之铠 | — | — | ·/·/· | — |
| 5516 | `ModLoader/Saethar_Head` | Saethar之盔 | — | — | ·/·/· | — |
| 5517 | `ModLoader/Saethar_Legs` | Saethar护腿 | — | — | ·/·/· | — |
| 5518 | `ModLoader/Saethar_Wings` | Saethar之翼 | — | — | ·/·/· | — |
| 5519 | `ModLoader/Sailing_Squid_Body` | Sailing_Squid之铠 | — | — | ·/·/· | — |
| 5520 | `ModLoader/Sailing_Squid_Head` | Sailing_Squid之盔 | — | — | ·/·/· | — |
| 5521 | `ModLoader/Sailing_Squid_Legs` | Sailing_Squid护腿 | — | — | ·/·/· | — |
| 5522 | `ModLoader/Sailing_Squid_Wings` | Sailing_Squid之翼 | — | — | ·/·/· | — |
| 5523 | `ModLoader/Squid_Body` | Squid之铠 | — | — | ·/·/· | — |
| 5524 | `ModLoader/Squid_Head` | Squid之盔 | — | — | ·/·/· | — |
| 5525 | `ModLoader/Squid_Legs` | Squid护腿 | — | — | ·/·/· | — |
| 5526 | `ModLoader/Tantamount_Body` | Tantamount之铠 | — | — | ·/·/· | — |
| 5527 | `ModLoader/Tantamount_Head` | Tantamount之盔 | — | — | ·/·/· | — |
| 5528 | `ModLoader/Tantamount_Legs` | Tantamount护腿 | — | — | ·/·/· | — |
| 5529 | `ModLoader/Tantamount_Wings` | Tantamount之翼 | — | — | ·/·/· | — |
| 5530 | `ModLoader/toplayz_Body` | toplayz之铠 | — | — | ·/·/· | — |
| 5531 | `ModLoader/toplayz_Head` | toplayz之盔 | — | — | ·/·/· | — |
| 5532 | `ModLoader/toplayz_Legs` | toplayz护腿 | — | — | ·/·/· | — |
| 5533 | `ModLoader/WitchDaggah_Head` | WitchDaggah之盔 | — | — | ·/·/· | — |
| 5534 | `ModLoader/xAqult_Body` | Catalyst的铠甲 | — | — | ·/·/· | — |
| 5535 | `ModLoader/xAqult_Head` | Catalyst的荣耀之冕 | — | — | ·/·/· | — |
| 5536 | `ModLoader/xAqult_Legs` | Catalyst护膝 | — | — | ·/·/· | — |
| 5537 | `ModLoader/xAqult_Lens` | Catalyst之瞳 | — | — | ·/·/· | — |
| 5538 | `ModLoader/xAqult_Mask` | Catalyst的面具 | — | — | ·/·/· | — |
| 5539 | `ModLoader/xAqult_Wings` | Catalyst的棱晶锐翼 | — | — | ·/·/· | — |
| 5540 | `ModLoader/Zeph_Body` | Zeph之铠 | — | — | ·/·/· | — |
| 5541 | `ModLoader/Zeph_Head` | Zeph之盔 | — | — | ·/·/· | — |
| 5542 | `ModLoader/Zeph_Legs` | Zeph护腿 | — | — | ·/·/· | — |
| 5543 | `ModLoader/Zeph_Wings` | Zeph之翼 | — | — | ·/·/· | — |
| 5544 | `ModLoader/StartBag` | 起始包 | — | — | ·/·/· | — |
| 5545 | `ModLoader/UnloadedItem` | 卸载物品 | — | — | ·/·/· | — |
