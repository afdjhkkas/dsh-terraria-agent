#!/usr/bin/env node
// chatd —— 常驻聊天网关。
//
// 解决的问题：之前靠对话里的临时轮询盯聊天框，对话一结束或游戏退出再进就断了。
// 这个进程独立于任何对话，只做三件事：
//   1) 盯游戏聊天框（chatpoll），收到新行就追加进 data/chat-inbox.jsonl
//   2) 读 data/chat-outbox.jsonl 里的待发消息，用 dialog 发进游戏框
//   3) 断线自动重连（游戏没开就等，不退出）
//
// 它不思考、不做决策，只搬消息。真正的回复由上层（人或 agent）写进 outbox。
//
//   npm run chatd            正常跑
//   npm run chatd -- --once  只跑一轮就退出（自检用）
//
// 单实例：启动时写 data/chatd.pid，发现已有存活实例就退出，避免两个进程抢聊天框。

import { execFileSync } from 'node:child_process'
import { appendFileSync, existsSync, mkdirSync, readFileSync, renameSync, writeFileSync, unlinkSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'
import { parseIntent, walkTiles, describeStatus, describeInventory } from '../src/chat-intents.mjs'
import { createBridgeClient } from '../src/bridge-client.js'
import { say as wakeSession } from './wake.mjs'

const ROOT = dirname(dirname(fileURLToPath(import.meta.url)))
const DATA = join(ROOT, 'data')
const INBOX = join(DATA, 'chat-inbox.jsonl')
const PENDING = join(DATA, 'chat-pending.jsonl')
// 急停闸：一个文件。存在 = 所有任务作废，直到有人删掉它。
// 用文件而不是内存变量，是因为要跨进程：上层（对话里的 agent）也能看到并确认。
const STOP = join(DATA, 'STOP')

function isStopped() {
  return existsSync(STOP)
}

function raiseStop(reason) {
  mkdirSync(DATA, { recursive: true })
  writeFileSync(STOP, JSON.stringify({ at: new Date().toISOString(), reason }), 'utf8')
}

function clearStop() {
  try { if (existsSync(STOP)) unlinkSync(STOP) } catch {}
}
const OUTBOX = join(DATA, 'chat-outbox.jsonl')
const CURSOR = join(DATA, 'chatd-cursor.json')
const PIDFILE = join(DATA, 'chatd.pid')
const LOG = join(DATA, 'chatd.log')

const ONCE = process.argv.includes('--once')
const POLL_MS = 1200      // chatpoll 节奏：游戏里打字不需要更快
const OUTBOX_TICK = 150   // outbox 节奏：上层写一行，最多这么久就进游戏框
const RECONNECT_MS = 5000
const HOST = process.env.TERRARIA_BRIDGE_HOST || '127.0.0.1'
const PORT = Number(process.env.TERRARIA_BRIDGE_PORT || 47777)
const TIMEOUT_MS = Number(process.env.TERRARIA_BRIDGE_TIMEOUT_MS || 15000)

mkdirSync(DATA, { recursive: true })

function log(msg) {
  const line = `[${new Date().toISOString()}] ${msg}`
  console.log(line)
  try { appendFileSync(LOG, line + '\n') } catch {}
}

// 认不出的指令进这张表，等对话里的「上层」来读。处理完由上层写
// data/chat-pending.done 标记，或用 `npm run chatd -- --drain-pending` 清空。
function appendPending(rec) {
  try { appendFileSync(PENDING, JSON.stringify({ ...rec, done: false }) + '\n') }
  catch (e) { log('pending 写入失败: ' + e.message) }
}

const client = createBridgeClient({ host: HOST, port: PORT, timeoutMs: TIMEOUT_MS })

// 注意：这里必须在**进程内**直连，不能起 scripts/bridge.mjs 子进程。
// 走过一次弯路：每读一次状态都 spawn 一个 node 进程，单次 150ms 以上，
// 于是「按住走 N 格」的轮询周期被拉到 200ms 量级，角色在两次读取之间跑出去好几格，
// 刹车距离算得再准也没用——表现为「说走 5 格实际走了 7.6 格」。
// 改成进程内 request 之后单次只要几毫秒，50ms 轮询才真的成立。
async function bridge(cmd, params) {
  const res = await client.request(cmd, params || {})
  const body = res && typeof res === 'object' && 'data' in res ? res.data : res
  if (res && res.ok === false) throw new Error(res.error || `${cmd} 失败`)
  return body
}

function loadCursor() {
  try { return JSON.parse(readFileSync(CURSOR, 'utf8')).since ?? 0 } catch { return 0 }
}
function saveCursor(since) {
  try { writeFileSync(CURSOR, JSON.stringify({ since, at: new Date().toISOString() })) } catch {}
}

// ---- 单实例锁 ----
function alivePid(pid) {
  try { process.kill(pid, 0); return true } catch { return false }
}
if (existsSync(PIDFILE)) {
  const old = Number(readFileSync(PIDFILE, 'utf8').trim())
  if (old && old !== process.pid && alivePid(old)) {
    console.error(`chatd 已经在跑（pid ${old}），本次不启动`)
    process.exit(0)
  }
}
writeFileSync(PIDFILE, String(process.pid))
function cleanup() { try { unlinkSync(PIDFILE) } catch {} }
process.on('exit', cleanup)
process.on('SIGINT', () => { cleanup(); process.exit(0) })
process.on('SIGTERM', () => { cleanup(); process.exit(0) })

// ---- outbox：原子读走，避免重复发 ----
function drainOutbox() {
  if (!existsSync(OUTBOX)) return []
  const tmp = OUTBOX + '.taking'
  try { renameSync(OUTBOX, tmp) } catch { return [] }
  let lines = []
  try {
    lines = readFileSync(tmp, 'utf8').split('\n').map(s => s.trim()).filter(Boolean)
  } catch {}
  try { unlinkSync(tmp) } catch {}
  const out = []
  for (const l of lines) {
    try { out.push(JSON.parse(l)) } catch {}
  }
  return out
}

function intentKind(line) {
  try { const it = parseIntent(line); return it ? it.kind : null } catch { return null }
}

function sendToGame(kind, text) {
  try {
    execFileSync(process.execPath, [join(ROOT, 'scripts/say.mjs'), kind, text],
      { encoding: 'utf8', cwd: ROOT, timeout: 20000, stdio: 'pipe' })
    return true
  } catch (e) { log('say 失败: ' + e.message); return false }
}

/**
 * 把游戏里的一句话投进 DSH 对话，等于替用户打了一次字。
 *
 * 失败不是致命错误：待办已经落盘了，只是没人立刻接手。所以这里吞掉异常、
 * 返回 false，让调用方能在游戏框里如实说明「对话没连上」，而不是假装成功。
 */
async function wakeAgent(line) {
  try {
    const r = await wakeSession(line)
    if (!r?.accepted) { log('[WAKE] DSH 未确认接受'); return false }
    log('[WAKE] 已投进对话 ' + r.sessionId)
    return true
  } catch (e) {
    log('[WAKE-ERR] ' + e.message)
    return false
  }
}

// ---- 固定指令：不需要 agent 在场也能办的那几条 ----
// 只处理确定性动作；认不出的句子原样留在 inbox 里等上层处理。
const say = (kind, text) => { sendToGame(kind, text); log(`[DO:${kind}] ${text}`) }
const nap = (ms) => new Promise(r => setTimeout(r, ms))

async function runIntent(intent) {
  // 急停闸拦在入口：只有 cancel / resume / stop 三条能穿过它。
  // 这样即使上层 agent 卡住没收到取消消息，角色也不会被后续指令推着继续动。
  if (isStopped() && !['cancel', 'resume', 'stop'].includes(intent.kind)) {
    log(`[GATE] 急停闸生效，拒绝执行 ${intent.kind}`)
    return false
  }
  switch (intent.kind) {
    case 'where': {
      const d = await bridge('state', {})
      const p = d.player || {}
      const pos = p.pos || []
      say('think', '读一次当前状态')
      say('say', `位置 ${Number(pos[0] ?? 0).toFixed(1)}, ${Number(pos[1] ?? 0).toFixed(1)}`)
      return true
    }
    case 'status': {
      say('think', '读一次当前状态')
      say('say', describeStatus(await bridge('state', {})))
      return true
    }
    case 'inventory': {
      say('think', '读背包')
      say('say', describeInventory(await bridge('inventory', {})))
      return true
    }
    case 'stop': {
      await bridge('stop', {})
      say('say', '已松手停下')
      return true
    }
    case 'cancel': {
      // 三层一起停：①桥上松手（角色立刻不动）②立起急停闸（后面的指令一律拒）
      // ③把消息投进对话（上层 agent 收到后停掉它自己的任务）
      raiseStop('用户取消')
      await bridge('stop', {})
      say('say', '停了。后面的指令先不接，说「继续」才恢复')
      wakeAgent('【取消】玩家在游戏里说了取消，已松手并立起急停闸。请立刻停止手头正在做的事，'
        + '回报你停在哪一步，然后等玩家说「继续」。')
      return true
    }
    case 'resume': {
      clearStop()
      await bridge('stop', {})
      say('say', '好了，可以继续')
      return true
    }
    case 'mount': {
      const before = await bridge('state', {})
      if (before.player?.mounted) { say('say', '已经在坐骑上了'); return true }
      await bridge('mount', { action: 'quick', ticks: 16 })
      await nap(400)
      const after = await bridge('state', {})
      const ok = !!after.player?.mounted
      say(ok ? 'say' : 'warn', ok ? `已骑上（mountType ${after.player?.mountType}）` : '没骑上，坐骑槽可能是空的')
      return ok
    }
    case 'dismount': {
      const before = await bridge('state', {})
      if (!before.player?.mounted) { say('say', '现在没在坐骑上'); return true }
      await bridge('mount', { action: 'dismount', ticks: 16 })
      await nap(400)
      const after = await bridge('state', {})
      const ok = !after.player?.mounted
      say(ok ? 'say' : 'warn', ok ? '已下骑' : '没能下骑')
      return ok
    }
    case 'fly': {
      const st = await bridge('state', {})
      if (!st.player?.mounted && !st.player?.hasWings) {
        say('warn', '现在既没骑坐骑也没有翅膀，飞不起来')
        return false
      }
      const y0 = st.player.pos[1]
      say('think', `起飞前 y=${y0.toFixed(1)}，${st.player.mounted ? '在坐骑上' : '靠翅膀'}`)
      const r = await bridge('fly', { ms: 1300 })
      await nap(300)
      const up = await bridge('state', {})
      const y1 = up.player.pos[1]
      const climbed = y0 - y1
      say('say', `升了 ${climbed.toFixed(1)} 格（y ${y0.toFixed(1)} → ${y1.toFixed(1)}，按键 ${r?.key ?? '?'}）`)
      return climbed > 0.5
    }
    case 'walk': {
      // 只说「走五格」没给方向时，沿用上一次的方向；一次都没走过就按右。
      const dir = intent.dir ?? lastWalkDir ?? 'right'
      lastWalkDir = dir
      say('think', `往${dir === 'right' ? '右' : '左'}走 ${intent.tiles} 格`)
      const r = await walkTiles(bridge, nap, dir, intent.tiles, isStopped)
      if (!r.ok && r.reason === 'cancelled') { say('say', `听你的，停在 x=${r.to.toFixed(1)}`); return true }
      if (!r.ok) { say('warn', `走不动（${r.reason}），停在 x=${r.to.toFixed(1)}`); return false }
      say('say', `走完：x ${r.from.toFixed(1)} → ${r.to.toFixed(1)}，实走 ${Math.abs(r.moved).toFixed(1)} 格，偏差 ${r.error.toFixed(2)}`)
      return true
    }
    default:
      return false
  }
}

// ---- 主循环 ----
let since = loadCursor()
const handled = new Set()
let lastWalkDir = null
let bridgeUp = false
log(`chatd 启动 pid=${process.pid} 起始游标=${since}`)

while (true) {
  // 1) 尝试连桥；连不上就等着，别退出
  let polled = null
  try {
    polled = await bridge('chatpoll', { since, max: 500 })
    if (!bridgeUp) { bridgeUp = true; log('桥已连上') }
  } catch (e) {
    if (bridgeUp) { bridgeUp = false; log('桥断开: ' + e.message) }
    if (ONCE) { console.log(JSON.stringify({ ok: false, reason: 'bridge-down', since })); process.exit(3) }
    await sleep(RECONNECT_MS)
    continue
  }

  // 2) 新消息入 inbox
  since = polled.next ?? since
  saveCursor(since)
  for (const line of (polled.lines || [])) {
    const rec = { t: new Date().toISOString(), text: line, intent: intentKind(line) }
    try { appendFileSync(INBOX, JSON.stringify(rec) + '\n') } catch (e) { log('inbox 写入失败: ' + e.message) }
    log('[GAME] ' + line)
    const intent = parseIntent(line)
    if (intent) {
      handled.add(line)
      try { await runIntent(intent) } catch (e) { log('[INTENT-ERR] ' + e.message); say('warn', '这条没办成：' + e.message) }
    } else {
      // 认不出的句子：写进 pending 兜底，然后把原话直接投进对话。
      //
      // 游戏框里**不再**说「这条我认不出」——用户明确不要那句解释。
      // 说了也没用：他要的是事情被办掉，不是听工具报一句自己不会。
      // 所以这里保持安静，只在日志里留痕。
      appendPending(rec)
      const woke = await wakeAgent(line)
      log('[UNKNOWN] ' + line + ' → ' + (woke ? '已转给对话' : '对话没连上，留在待办'))
    }
  }

  if (ONCE) {
    for (const item of drainOutbox()) sendOne(item)
    console.log(JSON.stringify({ ok: true, since, lines: (polled.lines || []).length, bridgeUp }))
    process.exit(0)
  }

  // 3) 发 outbox。
  // 这一层必须比 chatpoll 快得多：用户要的是「边想边看到」，不是等我想完一次性刷屏。
  // 所以把整段等待切成 OUTBOX_TICK 的小片，每片都 drain 一次；
  // 上层写一行，最多 OUTBOX_TICK 毫秒后就出现在游戏框里。
  let waited = 0
  while (waited < POLL_MS) {
    for (const item of drainOutbox()) sendOne(item)
    const step = Math.min(OUTBOX_TICK, POLL_MS - waited)
    await sleep(step)
    waited += step
  }
  // 主循环最后再 drain 一次，避免刚好卡在边界上
  for (const item of drainOutbox()) sendOne(item)
}

function sendOne(item) {
  if (!item || !item.text) return
  const kind = item.kind || 'say'
  const ok = sendToGame(kind, item.text)
  log(`[SEND:${kind}] ${item.text} -> ${ok ? 'ok' : 'fail'}`)
}

function sleep(ms) { return new Promise(r => setTimeout(r, ms)) }
