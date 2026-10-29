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
