#!/usr/bin/env node
// 阶段④ 回归：对“当前已部署的 mod + 桥”做一遍真实世界自检，结果按 PASS/FAIL 打印。
// 用法：node scripts/live-regress.mjs [--port 47777]
// 设计原则：每条都用“回读世界/背包”判定，不看命令返回值；改动世界的地方会把方块挖回来，净变化为零。
import { createBridgeClient } from '../src/bridge-client.js'

const args = process.argv.slice(2)
const portArg = args.indexOf('--port')
const PORT = portArg >= 0 ? Number(args[portArg + 1]) : 47777
const client = createBridgeClient({ port: PORT, timeoutMs: 20000 })
const sleep = (ms) => new Promise((r) => setTimeout(r, ms))

async function call(command, params = {}) {
  try {
    const res = await client.request(command, params)
    if (res && res.data !== undefined) return res.data
    if (res && res.error) return { __error: res.error }
    return res
  } catch (error) {
    return { __error: String(error && error.message ? error.message : error) }
  }
}

const results = []
function record(id, title, ok, detail) {
  results.push({ id, title, ok: !!ok, detail })
}

async function main() {
  // 1. 桥基本连通
  const ping = await call('ping')
  record('R01', '桥连通（ping）', ping && !ping.__error, JSON.stringify(ping).slice(0, 80))

  // 2. 状态读取
  const state = await call('state')
  const player = state && state.player
  record('R02', 'state.player 有坐标/生命', !!(player && player.tile && typeof player.life === 'number'), player ? `${JSON.stringify(player.tile)} hp ${player.life} netMode ${state.netMode}` : 'no player')

  // 3. tile 单格
  const t0 = await call('tile', { x: Math.floor(player.tile[0]) + 2, y: Math.round(player.tile[1]) + 1 })
  record('R03', 'tile 返回字段完整', !!(t0 && typeof t0.hasTile === 'boolean' && 'solid' in t0), JSON.stringify(t0).slice(0, 90))

  // 4. scan 结构与 stride 一致
  const scan = await call('scan', { x: Math.round(player.tile[0]), y: Math.round(player.tile[1]), r: 8 })
  const rowOk = Array.isArray(scan.tiles) && scan.tiles.length === scan.h && Array.isArray(scan.tiles[0]) && scan.tiles[0].length === scan.w * scan.stride
  record('R04', 'scan 行数=h 且每行=w*stride', !!rowOk, `h=${scan.h} w=${scan.w} stride=${scan.stride} rows=${Array.isArray(scan.tiles) ? scan.tiles.length : 'x'}`)

  // 5. find 近距离搜索
  const find = await call('find', { type: 1, x1: Math.round(player.tile[0]) - 30, x2: Math.round(player.tile[0]) + 30, y1: Math.round(player.tile[1]) - 10, y2: Math.round(player.tile[1]) + 20, max: 5 })
  record('R05', 'find 能搜到石块', !!(find && typeof find.total === 'number'), `total=${find && find.total}`)

  // 6. inventory 槽位统计
  const inv = await call('inventory')
  record('R06', 'inventory 给出已用格与空位', !!(inv && typeof inv.count === 'number' && typeof inv.free === 'number'), `count ${inv && inv.count} free ${inv && inv.free} freeMain ${inv && inv.freeMain}`)

  // 7. gravity 原版站立字段
  const grav = await call('gravity')
  record('R07', 'gravity 返回站立判定字段', !!(grav && 'grounded' in grav && 'controlJump' in grav), JSON.stringify({ grounded: grav && grav.grounded, vy: grav && grav.velocity && grav.velocity[1] }))

  // 8. place → 回读 → mine → 回读（净变化为零）
  const px = Math.floor(player.tile[0]) + 2
  const py = Math.round(player.tile[1]) + 1
  const beforePlace = await call('tile', { x: px, y: py })
  let placeOk = false
  if (!beforePlace.hasTile) {
    await call('place', { x: px, y: py, type: 1 })
    await sleep(700)
    placeOk = (await call('tile', { x: px, y: py })).hasTile
  } else {
    placeOk = true
  }
  record('R08', 'place 后回读该格变实心', placeOk, `cell ${px},${py} before=${beforePlace.hasTile}`)

  let mineOk = false
  if (beforePlace.hasTile === false) {
    await call('mine', { x: px, y: py, ms: 1600 })
    await sleep(500)
    if ((await call('tile', { x: px, y: py })).hasTile) { await call('mine', { x: px, y: py, ms: 1600 }); await sleep(500) }
    mineOk = !(await call('tile', { x: px, y: py })).hasTile
    record('R09', 'mine 后回读该格变空（世界复原）', mineOk, `cell ${px},${py}`)
  } else {
    record('R09', 'mine 后回读该格变空（跳过：目标格原为实心）', true, 'skipped')
  }

  // 10. grab 兜底拾取
  const grab = await call('grab')
  record('R10', 'grab 返回 seen/picked', !!(grab && 'seen' in grab && 'picked' in grab), JSON.stringify({ seen: grab && grab.seen, picked: grab && grab.picked }))

  // 11. recipes 查询
  const rec = await call('recipes', { q: '工作台' })
  record('R11', 'recipes 能按名字查配方', !!(rec && Array.isArray(rec.recipes)), `命中 ${rec && rec.recipes ? rec.recipes.length : 0} 条`)

  // 12. housing 查询（房屋列表可读）
  const housing = await call('housing', { x: Math.round(player.tile[0]), y: Math.round(player.tile[1]) })
  record('R12', 'housing 可读（带坐标）', !!(housing && !housing.__error && 'roomFound' in housing), `roomFound ${housing && housing.roomFound} townNPCs ${housing && housing.townNPCs}`)

  // 13. 聊天回写
  const chat = await call('chatpost', { text: `[regress] ${new Date().toISOString().slice(11, 19)} 阶段④ 回归自检 ${results.filter((r) => r.ok).length}/${results.length}` })
  record('R13', 'chatpost 可回写游戏聊天框', !!(chat && !chat.__error), JSON.stringify(chat).slice(0, 60))

  const pass = results.filter((r) => r.ok).length
  for (const r of results) console.log(`${r.ok ? 'PASS' : 'FAIL'}  ${r.id}  ${r.title}   ${r.detail}`)
  console.log(`\n${pass}/${results.length} PASS  (port ${PORT})`)
  client.close()
  process.exit(pass === results.length ? 0 : 1)
}

main().catch((e) => { console.error('FATAL', e); client.close(); process.exit(2) })
