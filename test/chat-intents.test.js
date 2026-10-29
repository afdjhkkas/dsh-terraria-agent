import { test } from 'node:test'
import assert from 'node:assert/strict'
import {
  parseIntent,
  cnToNumber,
  brakeDistance,
  walkTiles,
  describeStatus,
  describeInventory,
} from '../src/chat-intents.mjs'

// ---- 认得出的固定指令 ----
test('飞行/坐骑/停 这几条必须认得出来', () => {
  assert.deepEqual(parseIntent('飞一下'), { kind: 'fly' })
  assert.deepEqual(parseIntent('起飞'), { kind: 'fly' })
  assert.deepEqual(parseIntent('下骑'), { kind: 'dismount' })
  assert.deepEqual(parseIntent('骑上'), { kind: 'mount' })
  assert.deepEqual(parseIntent('召唤坐骑'), { kind: 'mount' })
  assert.deepEqual(parseIntent('停'), { kind: 'stop' })
})

test('走格：中文数字、阿拉伯数字、带不带"格"都要认', () => {
  assert.deepEqual(parseIntent('右走四格'), { kind: 'walk', dir: 'right', tiles: 4 })
  assert.deepEqual(parseIntent('往左 3 格'), { kind: 'walk', dir: 'left', tiles: 3 })
  assert.deepEqual(parseIntent('向左走两格'), { kind: 'walk', dir: 'left', tiles: 2 })
  assert.deepEqual(parseIntent('向右移动十二格'), { kind: 'walk', dir: 'right', tiles: 12 })
  // 只说方向没给数字，按一格处理，不要当成认不出
  assert.deepEqual(parseIntent('往右'), { kind: 'walk', dir: 'right', tiles: 1 })
  // 「再」是语气词，不影响方向；只给格数不给方向时沿用上一次的方向（无上下文时默认右）
  assert.deepEqual(parseIntent('再走五格'), { kind: 'walk', dir: null, tiles: 5 })
  assert.deepEqual(parseIntent('再飞一下'), { kind: 'fly' })
})

test('读状态的三种说法', () => {
  assert.deepEqual(parseIntent('坐标'), { kind: 'where' })
  assert.deepEqual(parseIntent('报位置'), { kind: 'where' })
  assert.deepEqual(parseIntent('血量'), { kind: 'status' })
  assert.deepEqual(parseIntent('蓝量'), { kind: 'status' })
  assert.deepEqual(parseIntent('背包'), { kind: 'inventory' })
})

test('认不出的句子必须返回 null，交给上层而不是硬猜', () => {
  for (const t of ['今天天气怎么样', '去挖矿吧', '这个能合成吗', '帮我打boss', '', '   ']) {
    assert.equal(parseIntent(t), null, `不该把「${t}」当成固定指令`)
  }
})

test('标点不影响识别', () => {
  assert.deepEqual(parseIntent('飞一下！'), { kind: 'fly' })
  assert.deepEqual(parseIntent('右走四格。'), { kind: 'walk', dir: 'right', tiles: 4 })
})

// ---- 中文数字 ----
test('中文数字换算', () => {
  assert.equal(cnToNumber('四'), 4)
  assert.equal(cnToNumber('两'), 2)
  assert.equal(cnToNumber('十'), 10)
  assert.equal(cnToNumber('十二'), 12)
  assert.equal(cnToNumber('二十'), 20)
  assert.equal(cnToNumber('35'), 35)
  assert.equal(cnToNumber(''), null)
  assert.equal(cnToNumber('很多'), null)
})

// ---- 刹车距离：速度越快越早松手 ----
test('刹车距离随速度单调递增', () => {
  let prev = -1
  for (const v of [0, 1, 2, 3, 4]) {
    const d = brakeDistance(v)
    assert.ok(d > prev, `vx=${v} 的刹车距离应该比更慢时大`)
    prev = d
  }
})

// ---- walkTiles：用假桥跑，不需要游戏 ----
function makeFakeBridge(startX, movePerTick, { blocked = false } = {}) {
  let x = startX
  let v = 0
  const calls = []
  const bridge = async (cmd, params) => {
    calls.push([cmd, params])
    if (cmd === 'move') { v = blocked ? 0 : (movePerTick > 0 ? 3 : -3) }
    if (cmd === 'stop') v = 0
    return {
      player: {
        pos: [x, 100],
        velocity: [v, 0],
        grounded: true,
        life: 500, lifeMax: 500, mana: 200, manaMax: 200,
      },
      items: [],
    }
  }
  // 每读一次 state 就往前走一点，模拟游戏自己推动角色
  const tick = () => { if (v !== 0) x += movePerTick }
  return { bridge, tick, calls, getX: () => x }
}

test('walkTiles 走够格数并在进刹车距离时松手', async () => {
  // 3 格/次 × 每 50ms 一次 ≈ 满速，4 格需要十几次
  const fake = makeFakeBridge(0, 0.15)
  const sleep = async () => { fake.tick() }
  const r = await walkTiles(fake.bridge, sleep, 'right', 4)
  assert.equal(r.ok, true)
  assert.ok(fake.calls.some(([c]) => c === 'move'), '必须发过 move')
  assert.ok(fake.calls.some(([c]) => c === 'stop'), '必须发过 stop 松手')
  const moveIdx = fake.calls.findIndex(([c]) => c === 'move')
  const stopIdx = fake.calls.findIndex(([c]) => c === 'stop')
  assert.ok(stopIdx > moveIdx, 'stop 必须在 move 之后')
})

test('walkTiles 走不动时要承认 blocked，不许假装走完', async () => {
  const fake = makeFakeBridge(0, 0, { blocked: true })
  const sleep = async () => {}
  const r = await walkTiles(fake.bridge, sleep, 'right', 5)
  assert.equal(r.ok, false)
  assert.equal(r.reason, 'blocked')
  assert.equal(r.to, 0)
})

// ---- 回报文案 ----
test('describeStatus 把位置/血/蓝拼成一行', () => {
  const s = describeStatus({ player: { pos: [4235.21, 379.38], life: 500, lifeMax: 500, mana: 200, manaMax: 200, grounded: true } })
  assert.match(s, /4235\.2/)
  assert.match(s, /500\/500/)
  assert.match(s, /200\/200/)
  assert.match(s, /站地上/)
})

test('describeInventory 只念有货的格子', () => {
  const s = describeInventory({
    items: [
      { name: '天顶剑', stack: 1 },
      { name: '', stack: 0 },
      { name: '石块', stack: 2746 },
    ],
  })
  assert.match(s, /天顶剑×1/)
  assert.match(s, /石块×2746/)
  assert.equal(describeInventory({ items: [] }), '背包是空的')
})

// 取消与继续：必须和「停」区分开。
// 「停」只是让角色别动；「取消」还要把正在跑的任务作废，并拦住后续指令。
// 这两条是玩家在游戏里唯一能反悔的手柄，认错了就等于没有。
test('取消/继续 与 停 分得清', () => {
  for (const t of ['取消', '取消掉', '撤销', '算了', '不要了', '别干了', '别做了', '停任务', '不干了', '中止', '中断']) {
    assert.equal(parseIntent(t)?.kind, 'cancel', t)
  }
  for (const t of ['继续', '接着', '恢复', '接着干', '继续吧']) {
    assert.equal(parseIntent(t)?.kind, 'resume', t)
  }
  for (const t of ['停', '停手', '停下', '别动', '站住', '停一下']) {
    assert.equal(parseIntent(t)?.kind, 'stop', t)
  }
})

test('walkTiles 中途被叫停会松手并报 cancelled', async () => {
  let n = 0
  const fake = async (cmd) => {
    if (cmd === 'state') { n++; return { player: { pos: [100 + n * 0.5, 0], velocity: [2, 0] } } }
    return {}
  }
  const r = await walkTiles(fake, async () => {}, 'right', 20, () => n >= 4)
  assert.equal(r.ok, false)
  assert.equal(r.reason, 'cancelled')
})
