#!/usr/bin/env node
/**
 * 把一段话直接投进 DSH 的对话里 —— 让外部进程能叫醒对话里的 agent。
 *
 * 为什么需要它：`chatd` 是普通 node 进程，能写文件、能开 TCP，但 LLM 会话的启动权
 * 在 DSH 宿主手里；光把消息写进 `data/chat-pending.jsonl` 只是存着，不会有人来看。
 * 走 DSH 的 `session/prompt` 才等于「像用户打字一样」把话递进去。
 *
 * 鉴权是**离线铸造**的：读 `~/.dsh/.credentials.yaml` 里的浏览器会话密钥自己签 Cookie，
 * 不依赖 `dsh web` 启动时打印的一次性 token，所以进程重启、DSH 重启后都能自己重签。
 *
 * 用法：
 *   node scripts/wake.mjs "去挖矿吧"            投进当前活跃会话
 *   node scripts/wake.mjs --session <id> "文本"  投进指定会话
 *   node scripts/wake.mjs --list                列出会话（不投递）
 *   node scripts/wake.mjs --dry "文本"          只打印将要发的报文，不真的投
 *
 * 退出码：0 投递成功 / 1 DSH 拒绝 / 2 用法或环境错 / 3 连不上 DSH
 */
import fs from 'node:fs'
import os from 'node:os'
import path from 'node:path'
import { createHash, createHmac, randomUUID } from 'node:crypto'
import { execFileSync } from 'node:child_process'

/**
 * DSH 的 GUI 端口每次启动都可能变（它是动态分配的），所以不能把端口写死。
 * 之前写死 49701，DSH 重启后变成 64900，chatd 就一直是
 *   [WAKE-ERR] 连不上 DSH（http://127.0.0.1:49701）：fetch failed
 * 而消息其实已经收到了，只是投不进去。
 *
 * 探测顺序：环境变量 → 问 Windows 哪个进程叫 DeepSeek Harness 且在听 → 常见端口兜底。
 * 结果缓存，进程生命周期内只探一次。
 */
let _detectedBase = null

function listeningPortsOf(processNamePattern) {
  try {
    const ps = [
      "$p = Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match '" + processNamePattern + "' } | Select-Object -ExpandProperty ProcessId",
      "if ($p) { Get-NetTCPConnection -State Listen | Where-Object { $_.OwningProcess -in $p } | Select-Object -ExpandProperty LocalPort | Sort-Object -Unique }",
    ].join('; ')
    const out = execFileSync('powershell', ['-NoProfile', '-Command', ps], { encoding: 'utf8', timeout: 8000 })
    return out.split(/\r?\n/).map((v) => parseInt(v.trim(), 10)).filter((v) => Number.isFinite(v) && v > 0)
  } catch {
    return []
  }
}

export function detectBaseUrl() {
  if (process.env.DSH_BASE_URL) return process.env.DSH_BASE_URL
  if (_detectedBase) return _detectedBase
  const ports = listeningPortsOf('DeepSeek Harness[.]exe')
  const guess = ports.length ? ports : [49701, 64900, 49700, 64901]
  _detectedBase = 'http://127.0.0.1:' + guess[0]
  return _detectedBase
}

export function setBaseUrl(url) { _detectedBase = url }

function currentBaseUrl(opts) {
  return opts.baseUrl || detectBaseUrl()
}

const DEFAULT_BASE_URL = 'http://127.0.0.1:49701'   // 只作最后兜底，实际用 detectBaseUrl()
const COOKIE_TTL_MS = 60 * 60 * 1000
const COOKIE_REFRESH_MARGIN_MS = 5 * 60 * 1000

/** DSH home：凭据文件在这里。 */
export function resolveDshHome() {
  return process.env.DSH_HOME || path.join(os.homedir(), '.dsh')
}

const b64u = (buf) => Buffer.from(buf).toString('base64')
  .replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '')

/**
 * 从 `.credentials.yaml` 里取出浏览器会话密钥。
 *
 * 这里不引 yaml 依赖：文件结构固定（两空格缩进的 `records:` 下、`client-connection/
 * browser-session:` 为一个块），按块扫描比引一个 parser 更省事，也不会因为版本差异
 * 把整份文件解析坏。
 */
export function readBrowserSessionSecret(home = resolveDshHome()) {
  const file = path.join(home, '.credentials.yaml')
  if (!fs.existsSync(file)) {
    throw Object.assign(new Error(`找不到凭据文件：${file}`), { code: 'no-credentials' })
  }
  const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/)
  const record = {}
  let inside = false
  for (const line of lines) {
    if (/^\s{2}client-connection\/browser-session:\s*$/.test(line)) { inside = true; continue }
    if (!inside) continue
    if (/^\s{2}\S/.test(line)) break               // 下一个记录/键，块结束
    const m = line.match(/^\s+(\w+):\s*(.+)$/)
    if (m) record[m[1]] = m[2].trim()
  }
  if (!record.secret) {
    throw Object.assign(new Error('凭据里没有 browser-session 的 secret'), { code: 'no-secret' })
  }
  const secret = Buffer.from(record.secret.replaceAll('-', '+').replaceAll('_', '/'), 'base64')
  if (secret.length !== 32) {
    throw Object.assign(new Error(`secret 应为 32 字节，实际 ${secret.length}`), { code: 'bad-secret' })
  }
  return secret
}

/**
 * 按 DSH 的规则离线铸一个认证 Cookie。
 *
 * 格式：`dsh-auth-<b64u(sha256(authority))>=v1.<body>.<signature>`，
 * body 是 `{version,authority,issuedAt,expiresAt}` 的 base64url，signature 是它的 HMAC-SHA256。
 * authority 取 baseUrl 的 host（含端口），换端口就要重新签。
 */
export function mintCookie(secret, baseUrl = DEFAULT_BASE_URL, now = Date.now()) {
  const authority = new URL(baseUrl).host
  const body = b64u(JSON.stringify({
    version: 1,
    authority,
    issuedAt: now,
    expiresAt: now + COOKIE_TTL_MS,
  }))
  const signature = b64u(createHmac('sha256', secret).update(body).digest())
  const name = 'dsh-auth-' + b64u(createHash('sha256').update(authority).digest())
  return {
    name,
    value: `v1.${body}.${signature}`,
    header: `${name}=v1.${body}.${signature}`,
    authority,
    expiresAt: now + COOKIE_TTL_MS,
  }
}

/**
 * 调用一个 DSH API 端点。
 *
 * 报文形状是 DSH 的 RPC 信封：`{type:'client-request', rpcId, method, payload:{args}}`。
 * `args` 的包装方式**因端点而异**：`session/list` 包一层 `_request`，
 * `session/prompt` / `session/projections` 包一层 `request`。包错会得到
 * `gateway/arguments-invalid: args fields do not match the descriptor`。
 */
async function callApi(endpoint, args, { baseUrl, cookie, timeoutMs = 10000 } = {}) {
  let res
  try {
    res = await fetch(`${baseUrl}/api/${endpoint}`, {
      method: 'POST',
      headers: { 'content-type': 'application/json', cookie: cookie.header },
      body: JSON.stringify({
        type: 'client-request',
        rpcId: randomUUID(),
        method: endpoint,
        payload: { args },
      }),
      signal: AbortSignal.timeout(timeoutMs),
    })
  } catch (e) {
    throw Object.assign(new Error(`连不上 DSH（${baseUrl}）：${e.message}`), { code: 'unreachable' })
  }
  if (res.status === 401 || res.status === 403) {
    throw Object.assign(new Error(`鉴权被拒（HTTP ${res.status}）：Cookie 可能已过期或密钥变了`), { code: 'unauthorized' })
  }
  let body
  try {
    body = await res.json()
  } catch {
    throw Object.assign(new Error(`DSH 返回的不是 JSON（HTTP ${res.status}）`), { code: 'bad-response' })
  }
  const result = body?.result
  if (!result) throw Object.assign(new Error('DSH 响应缺少 result'), { code: 'bad-response' })
  if (!result.ok) {
    const err = result.error || {}
    throw Object.assign(
      new Error(`${endpoint} 被拒：${err.code || 'unknown'} ${err.message || ''}`.trim()),
      { code: 'rejected', detail: err },
    )
  }
  return result.value
}

/** 列出会话（按 updatedAt 从新到旧）。 */
export async function listSessions(opts = {}) {
  const baseUrl = currentBaseUrl(opts)
  const cookie = opts.cookie || mintCookie(opts.secret || readBrowserSessionSecret(), baseUrl)
  const value = await callApi('session/list', { _request: {} }, { baseUrl, cookie })
  const list = Array.isArray(value) ? value : (value?.sessions ?? value?.items ?? [])
  return list.slice().sort((a, b) => (b.updatedAt || 0) - (a.updatedAt || 0))
}

/**
 * 挑出「该投给谁」：优先正在运行的那个（那就是用户正看着的对话），
 * 其次最近更新过的。这比让调用方自己传 sessionId 稳——会话 id 每次开新对话都会变。
 *
 * 这里自己排一次序，不依赖调用方先排好：listSessions 会排，但 pickTargetSession
 * 是导出的纯函数，测试和别的调用方都可能直接喂原始数组。
 */
export function pickTargetSession(sessions) {
  const sorted = (sessions || []).slice().sort((a, b) => (b.updatedAt || 0) - (a.updatedAt || 0))
  const running = sorted.filter((s) => s.running)
  return (running.length ? running : sorted)[0] || null
}

/**
 * 把一段文本投进会话。
 *
 * `mode: 'queue'` 表示「排在这一轮之后」，`steer` 表示「插进正在进行的这一轮」。
 * `content` 是内容块数组，文本块形状是 `{type:'text', text}`。这两个字段都是必填，
 * 少一个就只会得到一句 boundary validation，不看 schema 根本猜不到。
 */
export async function say(text, opts = {}) {
  const content = String(text ?? '').trim()
  if (!content) throw Object.assign(new Error('要说的话是空的'), { code: 'empty' })

  const baseUrl = currentBaseUrl(opts)
  const cookie = opts.cookie || mintCookie(opts.secret || readBrowserSessionSecret(), baseUrl)

  let sessionId = opts.sessionId
  if (!sessionId) {
    const target = pickTargetSession(await listSessions({ baseUrl, cookie }))
    if (!target) throw Object.assign(new Error('DSH 里没有可投递的会话'), { code: 'no-session' })
    sessionId = target.sessionId
  }

  const request = {
    requestId: randomUUID(),
    sessionId,
    mode: opts.mode === 'steer' ? 'steer' : 'queue',
    content: [{ type: 'text', text: content }],
    clientTimeZone: opts.clientTimeZone || 'Asia/Shanghai',
  }
  if (opts.dry) return { dry: true, sessionId, request }

  const value = await callApi('session/prompt', { request }, { baseUrl, cookie })
  return { accepted: value?.accepted === true, sessionId, requestId: request.requestId }
}

function readArgv(argv) {
  const out = { text: [], list: false, dry: false, session: null, json: false }
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i]
    if (a === '--list') out.list = true
    else if (a === '--dry') out.dry = true
    else if (a === '--json') out.json = true
    else if (a === '--session') out.session = argv[++i]
    else if (a === '-h' || a === '--help') out.help = true
    else out.text.push(a)
  }
  return out
}

const USAGE = `用法：
  node scripts/wake.mjs "要投进对话的话"        # 投给当前活跃会话
  node scripts/wake.mjs --session <id> "话"      # 投给指定会话
  node scripts/wake.mjs --list                   # 列出会话
  node scripts/wake.mjs --dry "话"               # 只打印报文
`

async function main() {
  const argv = readArgv(process.argv.slice(2))
  if (argv.help) { process.stdout.write(USAGE); return 0 }

  try {
    if (argv.list) {
      const sessions = await listSessions()
      const rows = sessions.slice(0, 20).map((s) => ({
        sessionId: s.sessionId,
        running: s.running === true,
        updatedAt: new Date(s.updatedAt || 0).toISOString(),
        title: s.projections?.values?.title ?? null,
        cwd: s.cwd ?? null,
      }))
      if (argv.json) { console.log(JSON.stringify({ total: sessions.length, rows }, null, 2)); return 0 }
      const target = pickTargetSession(sessions)
      console.log(`共 ${sessions.length} 个会话，当前投递目标：${target?.sessionId ?? '(无)'}`)
      for (const r of rows) {
        console.log(`${r.running ? '*' : ' '} ${r.sessionId}  ${r.updatedAt}  ${r.title ?? ''}`)
      }
      return 0
    }

    const text = argv.text.join(' ')
    if (!text.trim()) { process.stderr.write(USAGE); return 2 }

    const r = await say(text, { sessionId: argv.session, dry: argv.dry, mode: 'queue' })
    if (argv.json) { console.log(JSON.stringify(r, null, 2)); return 0 }
    if (r.dry) {
      console.log('（dry）将投给 ' + r.sessionId)
      console.log(JSON.stringify(r.request, null, 2))
      return 0
    }
    console.log((r.accepted ? '已投进对话' : 'DSH 未确认接受') + ' → ' + r.sessionId)
    return r.accepted ? 0 : 1
  } catch (e) {
    const code = e.code === 'rejected' ? 1 : e.code === 'unreachable' ? 3 : 2
    process.stderr.write(`wake 失败：${e.message}\n`)
    if (e.detail) process.stderr.write(JSON.stringify(e.detail) + '\n')
    return code
  }
}

if (import.meta.url === `file://${process.argv[1]?.replaceAll('\\', '/')}` || process.argv[1]?.endsWith('wake.mjs')) {
  // 用 exitCode 而不是 process.exit()：fetch 还留着 keep-alive 句柄，
  // 强行退出会让 libuv 在收尾时报 UV_HANDLE_CLOSING 断言（虽然退出码没坏，
  // 但每次调用都刷一行红字，看着像失败）。让它自然退出即可。
  main().then((code) => { process.exitCode = code })
}
