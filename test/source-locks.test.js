import test from 'node:test'
import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

/**
 * 源码级回归锁：锁住那些「一旦被改回去，功能就静默失效」的写法。
 *
 * 为什么需要它：`1cc9353` 一次「同步能力清单」的 chore 提交，把 GPT 修好的
 * 坐骑快捷召唤整段回退成了直写 `p.controlMount = true`。当时 331 个测试全绿，
 * 谁都没发现，直到真机实测 `mount quick` 不生效才暴露 —— 因为没有任何测试
 * 关心源码里到底是哪一行在驱动引擎。
 *
 * 这类「改坏但测试不红」的改动只能靠静态断言挡住。原则：
 * **只锁"必须存在"与"必须不存在"的精确写法**，不锁行号、不锁格式，
 * 免得正常重构就误报。
 */

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')
const BRIDGE = join(ROOT, 'DshTerrariaBridge', 'BridgeSystem.cs')
const CMDS = join(ROOT, 'DshTerrariaBridge', 'Commands.cs')

const read = async (p) => readFile(p, 'utf8')

test('坐骑边沿：必须走 Triggers.Current.QuickMount，不许退回直写 controlMount', async () => {
  const src = await read(BRIDGE)
  // 必须存在：把 QuickMount 写进输入层，让 Player.Update 里的 TriggersSet.CopyInto 带过去
  assert.match(src, /t\s*=>\s*t\.QuickMount\s*=\s*true/,
    'ApplyBridgeTrigger 里必须有 t => t.QuickMount = true；这是唯一能到引擎的边沿通路')
  // 必须不存在：直写 controlMount 的死路（失焦时 releaseMount 永假，能骑上但永远下不来）
  const directWrite = /(?:^|[^.\w])p(?:layer)?\.controlMount\s*=\s*true/m
  assert.doesNotMatch(src.replace(/\/\/.*$/gm, ''), directWrite,
    '不许再直写 p.controlMount = true：实测能骑上但下骑永远失败（Player.cs:23778 整段挂在 if (Main.hasFocus) 内）')
})

test('坐骑预开闸：闸门必须同时看 MountUntil 和 MountPreOpenUntil', async () => {
  const src = await read(BRIDGE)
  assert.match(src, /MountPreOpenUntil/,
    '必须有 MountPreOpenUntil 字段：失焦下要靠它先把 releaseMount 摆成 true')
  const gate = src.match(/OpenMountInputGate[\s\S]{0,600}?MountPreOpenUntil\s*>\s*now/)
  assert.ok(gate,
    'OpenMountInputGate 的条件里必须出现 MountPreOpenUntil > now，否则闸只开给按键那几帧，releaseMount 来不及被引擎置真')
  // 预开那几帧刻意不按键，否则自己把闸废掉
  assert.match(src, /MountUntil\s*>\s*now\s*&&\s*MountPreOpenUntil\s*<=\s*now/,
    'ApplyBridgeTrigger 的 active 条件必须是 MountUntil > now && MountPreOpenUntil <= now')
})

test('背包/ESC：必须发真实 Inventory 输入并让原版 ToggleInv 决定结果', async () => {
  const bridge = await read(BRIDGE)
  const cmds = await read(CMDS)
  assert.match(bridge, /ApplyBridgeTrigger\(InventoryUntil\s*>\s*now\s*,\s*ref\s+BridgeInventoryHeld[\s\S]{0,220}?t\.Inventory\s*=\s*true/,
    'PostUpdateInput 必须把 Inventory 键的 Current/边沿写进 Triggers')
  assert.match(bridge, /player\.controlInv\s*\|=\s*InventoryUntil\s*>\s*now\s*;/,
    'SetControls 路径必须把 Inventory 输入交给 Player.Update 的 ToggleInv 消费点')
  assert.match(cmds, /case\s+"key"\s*:\s*return\s+Key\(p\)/,
    '桥命令必须暴露真实 key 输入入口')
  assert.match(cmds, /requested\s*!=\s*"inventory"\s*&&\s*requested\s*!=\s*"escape"/,
    'key 命令必须明确支持 inventory/escape 两个语义别名')
  assert.doesNotMatch(cmds.replace(/\/\/.*$/gm, ''), /Main\.playerInventory\s*=\s*(?:true|false)/,
    'key 流程不许直接改 Main.playerInventory，必须让原版 ToggleInv 决定 UI 状态')
})

test('失焦前提：闸门不许无条件把 hasFocus 置真', async () => {
  const src = await read(BRIDGE)
  const fn = src.match(/internal static void OpenMountInputGate\s*\(\)\s*\{([\s\S]*?)\n\s{8}\}/)
  assert.ok(fn, '找不到 OpenMountInputGate 函数体')
  const body = fn[1]
  // 必须有窗口条件：无条件置真会把整个游戏当成一直有焦点，破坏别的输入判断
  assert.match(body, /MountUntil\s*>\s*now|MountPreOpenUntil\s*>\s*now|MountUntil\s*>/,
    'OpenMountInputGate 必须在坐骑时间窗内才置真 hasFocus，不许无条件开')
})

test('右键链路：ApplyAim 必须做缩放换算，不许直接 world - screenPosition', async () => {
  const src = await read(BRIDGE)
  const aim = src.match(/static\s+(?:bool|void)\s+ApplyAim\s*\([\s\S]*?\n\s{8}\}/)
  assert.ok(aim, '找不到 ApplyAim 函数体')
  const body = aim[0]
  // 必须做绕屏幕中心的缩放换算
  assert.match(body, /zoom|GameViewMatrix/i,
    'ApplyAim 必须处理缩放；写 Main.mouseX = worldX - screenPosition.X 会差整个屏幕（实测写 845 引擎读 125）')
})

test('世界改写：不许出现直接写 tile 的赋值', async () => {
  for (const p of [BRIDGE, CMDS]) {
    const src = (await read(p)).replace(/\/\/.*$/gm, '')
    const hits = [...src.matchAll(/Main\.tile\[[^\]]+\]\.(?:type|wall|active|hasTile)\s*=\s*(?!=)/g)]
    assert.equal(hits.length, 0,
      `${p.split(/[\\/]/).pop()} 出现 ${hits.length} 处直接写 tile；改世界只能走 选槽→瞄准→使用→等 ItemCheck→回读`)
  }
})

test('抓钩：releaseHook 必须置 true，否则 QuickGrapple 一次都不会被调用', async () => {
  const src = await read(BRIDGE)
  // 原版 Player.cs:26293：if (controlHook) { if (releaseHook) QuickGrapple(); releaseHook = false; }
  //                      else releaseHook = true;
  // 曾经写成 releaseHook = false（误以为 QuickGrapple 开头有 if (releaseHook) return），
  // 结果发射路径整条死掉：controlHook true、鼠标 true、grapCount 恒 0。
  assert.match(src, /p\.releaseHook\s*=\s*true\s*;/,
    '必须出现 p.releaseHook = true；它和 controlHook 同时为真才发射')
  assert.doesNotMatch(src.replace(/\/\/.*$/gm, ''), /p\.releaseHook\s*=\s*false\s*;/,
    '不许写 p.releaseHook = false：那会让 QuickGrapple 永远进不去（grapCount 恒 0）')
  // 瞄准必须走 zoom 前后两段，缺一段就会偏移整个屏幕
  assert.match(src, /ApplyHookAimBeforeZoom\s*\(/,
    '必须有 ApplyHookAimBeforeZoom：zoom 变换前写目标')
  assert.match(src, /ApplyHookAimAfterZoom\s*\(/,
    '必须有 ApplyHookAimAfterZoom：zoom 变换后写最终屏幕坐标')
})

test('talkoption：点击帧必须同时给 mouseLeft 与 mouseLeftRelease', async () => {
  const src = await read(BRIDGE)
  const phase3 = src.match(/if\s*\(TalkOptPhase\s*==\s*3\)[\s\S]{0,500}/)
  assert.ok(phase3, '找不到 talkoption 的按下阶段')
  assert.match(phase3[0], /Main\.mouseLeft\s*=\s*true\s*;/,
    '按下阶段必须给 Main.mouseLeft=true')
  assert.match(phase3[0], /Main\.mouseLeftRelease\s*=\s*true\s*;/,
    '按下阶段必须同时给 Main.mouseLeftRelease=true；原版按钮点击条件要求两个都为真')
  assert.doesNotMatch(phase3[0], /Main\.mouseLeftRelease\s*=\s*false\s*;/,
    '按下阶段不能把 mouseLeftRelease 清掉，否则 GUIChatDrawInner 不会触发按钮')
})

test('talkoption：索引语义必须明确为原版 focus1=关闭、focus2=第一个业务按钮', async () => {
  const src = await read(CMDS)
  assert.match(src, /index 只能是 1\.\.4：1=关闭，2=第一个业务按钮/,
    'talkoption 的错误提示必须明确 focus1/focus2 的真实语义，避免把商店误叫成 index 1')
})

test('talkoption：输入层必须和 Vanilla Player Chat 使用同一套 UI 坐标', async () => {
  const src = await read(BRIDGE)
  const layer = src.match(/layers\.Insert\(idx,[\s\S]{0,500}?InterfaceScaleType\.(UI|Game)\)\);/)
  assert.ok(layer, '找不到 talk 输入层的缩放声明')
  assert.equal(layer[1], 'UI',
    'talkoption 输入层必须与 Vanilla: Player Chat 一样声明为 UI 缩放层')
  assert.match(src, /const int step = 8;/,
    '按钮宽度由本地化文本决定，扫描步长不能再用 40px 跳过短按钮')
})

test('talkoption：坐标必须在 GUIChatDrawInner 原版读取前注入', async () => {
  const src = await read(BRIDGE)
  assert.match(src, /InstallTalkOptionHook\s*\(/,
    '必须安装 talkoption 的 GUIChatDrawInner 钩子')
  assert.match(src, /GetMethod\("GUIChatDrawInner"/,
    '必须挂到原版聊天绘制入口，而不是只依赖独立界面层')
  assert.match(src, /Action<Action<Main>, Main>/,
    '实例方法 detour 的 orig 必须接收 Main 实例，否则 hook 会进入 phase 1 但不执行原版绘制')
  assert.match(src, /PrepareTalkOptionInput\s*\(\);[\s\S]{0,160}orig\(self\);[\s\S]{0,160}FinishTalkOptionInput\s*\(\);/,
    '必须按“写输入 → 原版处理 → 读焦点/推进状态”顺序执行，并把 self 传给 orig')
  assert.match(src, /if\s*\(TalkOptPhase\s*>\s*0\s*&&\s*TalkOptPhase\s*<\s*5\)\s*return true;/,
    '独立界面层不得再提前执行旧扫描逻辑')
})

test('爬绳：climb 必须先判定脚底是不是绳，没绳就直接拒', async () => {
  const src = await read(CMDS)
  const fn = src.match(/private static string Climb\s*\(JsonElement p\)\s*\{([\s\S]*?)\n\s{8}\}/)
  assert.ok(fn, '找不到 Climb 函数体')
  const body = fn[1]
  assert.match(body, /OnRopeNow\s*\(/,
    'Climb 必须调 OnRopeNow 判前置；站平台上按上下不爬是引擎不认（脚底那格不是绳）')
  // 没绳时必须把爬行窗口清零，否则命令返回 ok 但人一帧都不动
  const guard = body.match(/if\s*\(\s*!\s*onRope\s*\)\s*\{([\s\S]{0,300}?)\}/)
  assert.ok(guard, 'Climb 必须有 if (!onRope) 分支')
  assert.match(guard[1], /ClimbUpUntil\s*=\s*0/,
    '没绳时必须把 ClimbUpUntil 清零，不能留着窗口让上层以为正在爬')
})

test('预开闸回包：mount 的 quick 与 dismount 都必须带 preOpenTicks', async () => {
  const src = await read(CMDS)
  const hits = [...src.matchAll(/preOpenTicks\s*=\s*preOpen/g)]
  assert.ok(hits.length >= 2,
    `preOpenTicks 至少要在 quick 与 dismount 两处回包里出现（实际 ${hits.length} 处）；` +
    '少了它就没法从回包判断预开闸真的生效了')
})

test('聊天读取：只收原版真实发送，不用输入框变空猜测', async () => {
  const src = await read(BRIDGE)
  // 发送判定必须镜像原版：聊天框打开、回车释放、原版确认 Enter，且原文非空并被原版清空。
  assert.match(src,
    /bool\s+sent\s*=\s*wasChatOpen\s*&&\s*releaseBefore\s*&&\s*Main\.inputTextEnter\s*&&/,
    'chatpoll 必须以原版 inputTextEnter + chatRelease 发送分支为准')
  assert.match(src,
    /trimmed\.Length\s*>\s*0\s*&&\s*\(Main\.chatText\s*\?\?\s*""\)\.Length\s*==\s*0/,
    '必须确认原版发送后输入框被清空，不能按中间态文本入队')
  assert.match(src, /bool\s+cancelled\s*=\s*wasChatOpen\s*&&\s*!sent\s*&&\s*Main\.inputTextEscape/,
    'Esc 取消必须走独立分支，不能进入聊天 lines')
  assert.match(src, /ChatLastEvent\s*=\s*command\s*\?\s*"command"\s*:\s*"sent"/,
    '必须区分普通消息与原版命令')
  assert.doesNotMatch(src, /else\s+if\s*\(ChatPending\.Length\s*>\s*0\)/,
    '不许回退到“输入框变空就算发送”的旧状态机')
})

test('假操作：不许出现直接写 velocity / position / pulley 的赋值', async () => {
  for (const p of [BRIDGE, CMDS]) {
    const name = p.split(/[\\/]/).pop()
    const src = (await read(p)).replace(/\/\/.*$/gm, '')
    for (const [label, re] of [
      // (?!=) 是关键：p.velocity.Y == 0f 是读比较，不是赋值，别误报
      ['velocity.X/Y', /(?:^|[^.\w])p(?:layer)?\.velocity\.[XY]\s*=(?!=)/m],
      ['position', /(?:^|[^.\w])p(?:layer)?\.position\s*=(?!=)/m],
      ['Center', /(?:^|[^.\w])p(?:layer)?\.Center\s*=(?!=)/m],
      ['pulley', /(?:^|[^.\w])p(?:layer)?\.pulley\s*=(?!=)/m],
      ['grapCount', /(?:^|[^.\w])p(?:layer)?\.grapCount\s*=(?!=)/m],
    ]) {
      assert.doesNotMatch(src, re,
        `${name} 出现直接写 ${label}；这属于替引擎算物理（AGENTS.md §0.55）`)
    }
  }
})

// craft 必须走原版合成路径，不许手动扣材料/手动加产物。
// 曾经的写法：ConsumeRequirement 手动 it.stack -= take、AddToInventory 手动 it.SetDefaults + it.stack =。
// 那是替引擎算结果（§0.55），且 QuickSpawnItem 凭空造物。改为 recipe.Create() + pl.GetItem()。
test('craft 走原版合成路径：recipe.Create 消耗材料、pl.GetItem 给产物', async () => {
  const src = await read(CMDS)
  const craft = src.slice(src.indexOf('private static string Craft('))
  const body = craft.slice(0, craft.indexOf('\n        }'))
  // 必须存在：原版消耗
  assert.match(body, /\.Create\(\)/,
    'Craft 必须调 recipe.Create() 让原版消耗材料（处理配方组 + mod 钩子）')
  assert.match(body, /pl\.GetItem\(/,
    'Craft 必须用 pl.GetItem() 走原版拾取管线给产物，不再手动写 inventory')
  // 必须不存在：手动扣材料/手动加产物
  assert.doesNotMatch(body, /ConsumeRequirement\(/,
    'Craft 不许再手动 ConsumeRequirement（it.stack -= take 是替引擎算结果）')
  assert.doesNotMatch(body, /AddToInventory\(/,
    'Craft 不许再手动 AddToInventory（it.SetDefaults + it.stack = 是直接写背包）')
})

// 参数校验：传了但没人读的参数名必须出现在回包里，不能静默吞掉。
// 曾经的后果：move 传 direction（应为 dir）静默走默认 right、trash 不带 count 吃掉整栈。
test('参数校验：Handle 必须追踪已读参数并回传 ignoredParams', async () => {
  const json = await read(join(ROOT, 'DshTerrariaBridge', 'Json.cs'))
  const cmds = await read(CMDS)
  // Json.cs 必须有参数追踪机制
  assert.match(json, /BeginParamTracking\(\)/,
    'Json.cs 必须有 BeginParamTracking：每条命令开始前重置已读参数集')
  assert.match(json, /EndParamTracking\(\)/,
    'Json.cs 必须有 EndParamTracking：命令结束后取出已读参数集')
  assert.match(json, /WithIgnoredParams\(/,
    'Json.cs 必须有 WithIgnoredParams：把"传了但没人读"的参数名附进回包')
  // Commands.cs 的 Handle 必须调用追踪
  assert.match(cmds, /Json\.BeginParamTracking\(\)/,
    'Handle 必须调 Json.BeginParamTracking()')
  assert.match(cmds, /Json\.EndParamTracking\(\)/,
    'Handle 必须调 Json.EndParamTracking()')
  assert.match(cmds, /Json\.WithIgnoredParams\(/,
    'Handle 必须调 Json.WithIgnoredParams() 把未读参数名附进成功回包')
})

// fly 必须按坐骑类型分流用键。
// 原版 Player.cs:25783 是 if (mount.CanHover()) { mount.Hover(this); }
//                      else if (mount.CanFly() && controlJump && ...)
// —— 悬停坐骑读的是 controlUp，只写 controlJump 一个字都不会被读到。
// 曾经的后果：fly 在骑着坐骑时 velY 恒 0，被误判成"没翅膀所以诚实拒绝"。
test('fly 必须区分悬停坐骑与翅膀，不能用同一个键', async () => {
  const src = await read(CMDS)
  const fly = src.slice(src.indexOf('private static string Fly('))
  const body = fly.slice(0, fly.indexOf('\n        }'))
  assert.match(body, /CanHover\(\)/, '必须判断 CanHover()')
  assert.match(body, /UpUntil\s*=/, '悬停坐骑要走上键时间窗')
  assert.match(body, /BeginJump\(/, '翅膀/普通飞行坐骑要走上跳键')
  assert.match(body, /key\s*=\s*hovering\s*\?\s*"up"\s*:\s*"jump"/, '回包要说清这次用的是哪个键')
})

test('probe 命令必须存在并只读预判，不实际移动', async () => {
  const src = await read(CMDS)
  assert.match(src, /case\s+"probe"\s*:\s*return\s+Probe\(p\)/,
    'Dispatch 必须有 probe 命令分支')
  assert.match(src, /private static string Probe\s*\(JsonElement p\)/,
    '必须有 Probe 函数实现')
  const probe = src.match(/private static string Probe\s*\(JsonElement p\)\s*\{([\s\S]*?)\n\s{8}\}/)
  assert.ok(probe, '找不到 Probe 函数体')
  const body = probe[1]
  assert.match(body, /PathBodyFree/,
    'probe 必须调 PathBodyFree 判断通行')
  assert.match(body, /PathFloorAt/,
    'probe 必须调 PathFloorAt 判断地板')
  assert.doesNotMatch(body, /BeginWalk|PathActive|controlLeft|controlRight/,
    'probe 是只读预判，不许实际移动或设置控制标志')
})

test('WebSocket 推送必须在 PostUpdateEverything 里调用', async () => {
  const src = await read(BRIDGE)
  const post = src.match(/public override void PostUpdateEverything\s*\(\)\s*\{([\s\S]*?)\n\s{8}\}/)
  assert.ok(post, '找不到 PostUpdateEverything 函数体')
  assert.match(post[1], /WebSocketServer\.Update\s*\(\)/,
    'PostUpdateEverything 必须调 WebSocketServer.Update() 推送状态变化')
})
