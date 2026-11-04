// 第 2 项（HANDOFF §4）：真机验 mine 的新回包。
//
// 要验三件事（对应提交 1d4172a）：
//   1) 回包带 before:{hasTile,type,frameX}，且与独立 tile 回读一致；
//   2) armed:true 只代表「开始挥」——判真伪只看事后 tile 回读 hasTile===false；
//   3) 够不到时外层必须是 ok:false，而不是 ok:true + 空 data（0.58 的第四次复发点）。
//
// 用法：node scratch/verify-mine.mjs
import { call, must, close } from './qc.mjs'

const J = (o) => JSON.stringify(o)
const sleep = (ms) => new Promise((r) => setTimeout(r, ms))
let pass = 0, fail = 0
function check(name, cond, detail = '') {
  console.log(`  ${cond ? 'PASS' : 'FAIL'}  ${name}${detail ? ' :: ' + detail : ''}`)
  cond ? pass++ : fail++
}

async function waitBridge(tries = 90) {
  for (let i = 0; i < tries; i++) {
    const r = await call('ping', {}, 3000)
    if (r.ok) return r.data
    if (i % 5 === 0) console.log(`  等桥… (${i}/90) ${r.error || ''}`)
    await sleep(2000)
  }
  throw new Error('桥 3 分钟内没通')
}

function pickTarget(map, px, py, radius) {
  const lines = String(map).split('\n')
  const mid = lines.length >> 1
  const best = []
  for (let j = 0; j < lines.length; j++) {
    const row = lines[j]
    const y = py - radius + j
    for (let i = 0; i < row.length; i++) {
      if (row[i] !== '#') continue
      const x = px - radius + i
      const above = lines[j - 1] ? lines[j - 1][i] : '.'
      if (above !== '.') continue                        // 只要表面方块（上方是空气）
      const d = Math.hypot(x - px, y - py)
      if (d > 5.5) continue                              // 原版手够 ~5.2 格，7 格是硬拒绝线
      best.push({ x, y, d })
    }
  }
  best.sort((a, b) => a.d - b.d)
  return best[0] || null
}

const ping = await waitBridge()
console.log('ping:', J(ping).slice(0, 200))
if (ping.inWorld !== true) {
  console.log('  世界还没进——请进 Host & Play 世界后重跑本脚本')
  close(); process.exit(2)
}

const st = await must('state')
const p = st.player
const px = Math.floor(p.tile[0]), py = Math.floor(p.tile[1])
console.log(`玩家 why 在格 (${px},${py}) paused=${st.paused} hasFocus=${st.hasFocus}`)

// ---------- 1) 可达方块：before 字段 + 回读判真伪 ----------
const scan = await must('scan', { x: px, y: py, radius: 10, format: 'ascii' })
const target = pickTarget(scan.map, px, py, 10)
if (!target) {
  console.log('  附近 5.5 格内找不到暴露的实心方块，换个位置再测')
  close(); process.exit(3)
}
console.log(`目标格 (${target.x},${target.y}) 距玩家 ${target.d.toFixed(2)} 格`)

const tileBefore = await must('tile', { x: target.x, y: target.y })
const r1 = await call('mine', { x: target.x, y: target.y, ms: 8000 }, 20000)
check('mine 外层 ok:true', r1.ok === true, r1.ok ? '' : r1.error)

const d1 = r1.data || {}
check('before 有值', !!d1.before, d1.before ? J(d1.before) : '缺失')
check('before.hasTile === true', d1.before?.hasTile === true, String(d1.before?.hasTile))
check('before.type 与独立 tile 回读一致', d1.before?.type === tileBefore.type,
  `${d1.before?.type} vs ${tileBefore.type}`)
check('before.frameX 是数字', typeof d1.before?.frameX === 'number', String(d1.before?.frameX))
check('armed:true 且带 verify 说明', d1.armed === true && typeof d1.verify === 'string')

let after = null
for (let i = 0; i < 12; i++) {
  after = await must('tile', { x: target.x, y: target.y })
  if (after.hasTile === false) break
  await sleep(500)
}
check('回读 hasTile === false（真掉了）', after?.hasTile === false, J(after))

// ---------- 2) 够不到的方块：必须 ok:false ----------
const farX = px + 20, farY = py
const r2 = await call('mine', { x: farX, y: farY, ms: 500 }, 15000)
check('够不到时 ok === false', r2.ok === false, r2.ok ? J(r2).slice(0, 120) : r2.error)
check('够不到时没有 data', r2.data === undefined, J(r2.data))
check('错误文案说明原因', /够不到|超过|先走/.test(String(r2.error)), String(r2.error))

console.log(`\n结果：${pass} pass / ${fail} fail`)
close()
process.exit(fail ? 1 : 0)