# 常驻能力清单（生成物）

> 由 `node scripts/kb-caps.mjs` 从 `DshTerrariaBridge/Commands.cs` 生成，**不要手改**。
> 元自检的第 4、5 维（动作原语 / 状态读取）就是拿这张表对照的：
> 表里没有的接口和命令，一律视为不存在，不许在计划里使用。

## 一、状态读取能力（50 个命令）

| 命令 | 能读到什么（返回值顶层字段） |
|---|---|
| `api` | `type` `count` `members` |
| `blood` | — |
| `bloodmoon` | — |
| `bobber` | `count` `bobbers` |
| `boss` | `active` `bosses` |
| `chatpoll` | — |
| `eclipse` | — |
| `event` | `dayTime` `time` `day` `bloodMoon` `eclipse` `invasionType` `invasionSize` `pumpkinMoon` `snowMoon` `raining` `hardMode` `downedEye` `downedEater` `downedSkeletron` `downedWall` `downedPlantera` `downedGolem` |
| `find` | `scanned` |
| `frost` | — |
| `frostmoon` | — |
| `help` | — |
| `hook` | — |
| `hookdebug` | `controlHook` `releaseHook` `mouseLeft` `mouseLeftRelease` `mouseInterface` `grapCount` `grappling0` `pulley` `dead` `selectedSlot` `heldName` `hookEquip` `hookShoot` `isProjHook` `mouseWorldTile` |
| `housediag` | `x` `y` `tiles` `box` |
| `housing` | `townNPCs` |
| `humaninput` | `inWorld` `tick` `paused` `focus` `control` `left` `right` `up` `down` `jump` `useItem` `useTile` `grapple` `mount` |
| `idmap` | `rows` |
| `interact` | `interact` `after` `method` |
| `inventory` | `selected` `count` `free` `freeMain` `freeAll` `items` `equipment` `canFly` |
| `items` | `count` `items` |
| `keyevents` | `lastTick` `count` `time` `stride` |
| `map` | `mapWidth` `mapHeight` `sampled` `revealedPercent` `explored` |
| `needs` | `worldFields` `npc` `name` `homeless` `homeTile` `position` |
| `nodig` | `cleared` `count` `zones` |
| `npc` | `radius` `count` `npcs` |
| `path` | `from` |
| `ping` | — |
| `players` | `count` `players` |
| `pumpkin` | — |
| `pumpkinmoon` | — |
| `quest` | `anglerPresent` `note` |
| `recipes` | `count` `recipes` |
| `research` | `item` `alreadyFull` `researched` `have` `unlocked` |
| `roomcheck` | `ok` `reason` `reasonValue` |
| `scan` | `x0` `y0` `w` `h` `format` `center` `playerTile` `stride` `fields` `tiles` `legend` `map` |
| `shop` | `npc` `shop` `npcType` `distance` |
| `shopdebug` | `npc` `npcType` |
| `shops` | `scanned` |
| `snowmoon` | — |
| `state` | `inWorld` `version` `paused` `hasFocus` `screen` `mouseWorld` `mouseTile` `world` `player` `guard` `attack` `path` `walk` `input` `hint` |
| `tile` | `hasTile` `type` `name` `wall` `liquid` `liquidName` `slope` `halfBlock` `actuator` |
| `tiledata` | `name` `hasObjectData` `frameImportant` `width` `height` `originX` `originY` `styleHorizontal` `styleWrapLimit` |
| `travelstatus` | — |
| `walkdiff` | `dual` `newRule` `newPriority` `newKind` `oldRule` `oldKind` `same` |
| `walkmode` | `useNew` `reactive` `dual` |
| `chest` | `chest` `took` `count` `put` `chestSlot` |
| `down` | `untilTick` `tick` `tiles` `from` `slot` `held` `interact` `after` `method` `off` `note` `radius` `hostileInRange` `weaponSlot` `weapon` `controlHook` `releaseHook` `mouseLeft` `mouseLeftRelease` `mouseInterface` `grapCount` `grappling0` `pulley` `dead` `selectedSlot` `heldName` `hookEquip` `hookShoot` `isProjHook` `mouseWorldTile` `grapCountBefore` `proj` `equipped` `hasWings` `wingTimeMax` `wingTime` `velY` `hint` `activeAfter` `action` `active` `type` `flight` `items` `slots` `follow` `target` `targetIndex` `guard` `range` `dist` `idle` `count` `players` `attacking` `who` `selected` `free` `freeMain` `freeAll` `equipment` `canFly` `item` `name` `reason` `perCraft` `groupsUsed` `npcs` |
| `guard` | `off` `note` `untilTick` `radius` `hostileInRange` `weaponSlot` `weapon` |
| `mount` | `activeAfter` `held` `action` `active` `type` `flight` `items` |

## 二、动作能力（59 个命令）

| 命令 | 参数 | 干什么 |
|---|---|---|
| `accessory` | `off` `slot` `from` | — |
| `attack` | `who` `r` `ms` `interval` `slot` | — |
| `buy` | `slot` `shopId` | — |
| `capture` | `x` `y` `who` `ms` | — |
| `castline` | `x` `y` `ms` | — |
| `chat` | `text` | — |
| `chatpost` | — | — |
| `climb` | `dir` `ms` `tiles` | — |
| `craft` | `item` `times` | — |
| `down` | — | — |
| `drop` | `slot` `count` | — |
| `duplicate` | `item` `count` | — |
| `equip` | `slot` | — |
| `fill` | `x1` `y1` `x2` `y2` `type` | — |
| `fly` | `ms` | — |
| `follow` | `off` `player` `guard` `range` | — |
| `goblin` | — | — |
| `grab` | `r` | — |
| `grapple` | `x` `y` `ms` | — |
| `gravity` | — | — |
| `heal` | — | — |
| `homeroom` | `x` `y` `who` | — |
| `idle` | — | — |
| `jump` | `ms` `moveMs` | — |
| `left` | — | — |
| `left` | — | — |
| `liquidop` | `x` `y` `ms` | — |
| `martian` | — | — |
| `mine` | `x` `y` `ms` | — |
| `minion` | `slot` | — |
| `mirror` | — | — |
| `move` | `dir` `ms` `tiles` | — |
| `pirate` | — | — |
| `place` | `x` `y` `type` `ms` | — |
| `reel` | `ms` | — |
| `removeblock` | `x` `y` `ms` | — |
| `right` | — | — |
| `right` | — | — |
| `save` | — | — |
| `select` | `slot` `item` | — |
| `sell` | `slot` | — |
| `shoot` | `x` `y` `slot` `ms` | — |
| `snowman` | — | — |
| `splitstack` | `slot` `count` | — |
| `startevent` | `event` | — |
| `stop` | — | — |
| `talk` | `npc` | — |
| `trash` | `slot` `count` | — |
| `travel` | `stop` `dir` `distance` `ms` `dig` | — |
| `unhook` | — | — |
| `up` | — | — |
| `up` | — | — |
| `use` | `slot` `right` `ms` `x` `y` | — |
| `walkto` | `x` `y` `tol` `ms` `dig` | — |
| `wall` | `x` `y` `type` `remove` `ms` | — |
| `chest` | `action` `slot` `count` `invSlot` | — |
| `down` | `ms` `moveMs` `slot` `right` `x` `y` `item` `off` `action` `r` `player` `guard` `range` `who` `interval` `times` `event` | 按住跳跃键时长 → 跳跃顶点高度（格） |
| `guard` | `off` `action` `ms` `r` | — |
| `mount` | `action` `slot` | — |

## 三、明确做不到的（做计划前先看这里）

> 这一节是人工维护的。它不是"暂时没写"，是"现在真的没有这个接口"。
> 任务卡在这里就是【动作原语缺口】或【状态读取缺口】，不许绕。

- **通用弹幕状态**：只能间接判断两类：自己名下的弹幕**数量**（远程攻击的凭证）、抓钩弹幕的**类型**（aiStyle 7）。拿不到任意弹幕的位置/速度/生命周期，读不到"浮标有没有落水""咬钩没咬钩"这类状态。
- **生物 AI 内部状态**：可以列半径内 NPC 的类型/血量/坐标/是否敌对，但读不到目标、路径、攻击冷却、拾取意图。
- **随机事件与时机窗口**：没有任何"等到某件事发生再动手"的原语。现有命令都是「发一个动作 → 到点返回」，不支持「等随机时长 → 在窗口内做第二动作」（钓鱼、抓虫、躲冲撞都属于这一类）。
- **NPC 对话选项与任务进度**：`talk` 能读到当前这一句正文（`chatText`）和右上角展示的物品（`cornerItem`），`quest` 能读到渔夫的每日任务（要哪条鱼、今天交没交）；但对话**选项按钮的文案读不到**（那是绘制时才算出来的），其他 NPC 也没有可读的任务字段。
- **容器以外的容器**：只能读 6 格内的箱子（list/take）。梳妆台、展示框、武器架、展示柜都读不到。
- **画面语义**：截图只能看像素（本机没前台也能抓），但不能识别"这是什么怪""这里黑不黑"。一切判定必须落到 state / tile / inventory 这类数值上。
- **音效与事件字幕**：读不到游戏内提示文本（"你感觉邪恶的东西在看着你"之类），只能靠 boss/event 状态反推。
- **合成树自动规划**：能查配方、能按 id 合成，但不会自己把"缺 3 个铁锭"翻译成"该去挖 9 个铁矿"。要我自己推。
