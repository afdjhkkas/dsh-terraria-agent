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

const ROOT = dirname(dirname(fileURLToPath(import.meta.url)))
const DATA = join(ROOT, 'data')
const INBOX = join(DATA, 'chat-inbox.jsonl')
const OUTBOX = join(DATA, 'chat-outbox.jsonl')
const CURSOR = join(DATA, 'chatd-cursor.json')
const PIDFILE = join(DATA, 'chatd.pid')
const LOG = join(DATA, 'chatd.log')

const ONCE = process.argv.includes('--once')
const POLL_MS = 1200      // chatpoll 节奏：游戏里打字不需要更快
const OUTBOX_TICK = 150   // outbox 节奏：上层写一行，最多这么久就进游戏框
const RECONNECT_MS = 5000

mkdirSync(DATA, { recursive: true })

function log(msg) {
  const line = `[${new Date().toISOString()}] ${msg}`
  console.log(line)
  try { appendFileSync(LOG, line + '\n') } catch {}
}

function bridge(cmd, params) {
  const out = execFileSync(process.execPath,
    [join(ROOT, 'scripts/bridge.mjs'), cmd, '--raw', '--params', JSON.stringify(params || {})],
    { encoding: 'utf8', cwd: ROOT, timeout: 20000 })
  const j = JSON.parse(out)
  if (!j.ok) throw new Error(j.error || `${cmd} 失败`)
  return j.data
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

function sendToGame(kind, text) {
  try {
    execFileSync(process.execPath, [join(ROOT, 'scripts/say.mjs'), kind, text],
      { encoding: 'utf8', cwd: ROOT, timeout: 20000, stdio: 'pipe' })
    return true
  } catch (e) { log('say 失败: ' + e.message); return false }
}

// ---- 主循环 ----
let since = loadCursor()
let bridgeUp = false
let announced = false
log(`chatd 启动 pid=${process.pid} 起始游标=${since}`)

while (true) {
  // 1) 尝试连桥；连不上就等着，别退出
  let polled = null
  try {
    polled = bridge('chatpoll', { since, max: 500 })
    if (!bridgeUp) { bridgeUp = true; announced = false; log('桥已连上') }
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
    const rec = { t: new Date().toISOString(), text: line }
    try { appendFileSync(INBOX, JSON.stringify(rec) + '\n') } catch (e) { log('inbox 写入失败: ' + e.message) }
    log('[GAME] ' + line)
    if (!announced) { announced = true; sendToGame('think', '收到了，正在处理') }
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
