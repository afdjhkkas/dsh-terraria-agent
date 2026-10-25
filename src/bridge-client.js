/**
 * Terraria bridge client.
 * ============================================================================
 * Transport: TCP 127.0.0.1:47777, one JSON object per line (protocol v0.2.0).
 *   request  {"cmd":"<name>","params":{...},"req":<n>}
 *   success  {"ok":true,"data":{...},"req":<n>}
 *   failure  {"ok":false,"error":"<actionable message>","req":<n>}
 *
 * A `{"ok":false}` reply is a **command failure** (the command did not do the
 * thing); it must reach the caller with its own message and no "Invalid ..."
 * prefix. Only a line that is genuinely not a protocol payload gets that
 * prefix. See classifyLine() for the exact rule.
 *
 * One socket per request: no FIFO queue to desync, no reconnect state to get
 * wrong, and a game restart simply fails the in-flight call. `req` is an
 * optional correlation echo so an unsolicited line on the same connection (a
 * banner, a log line) can never be mistaken for this call's answer.
 * ============================================================================
 */
import net from 'node:net'

const MAX_RESPONSE_BYTES = 2 * 1024 * 1024
/** `stop` is fire-and-forget: short deadline, never awaited, never reported. */
const STOP_TIMEOUT_MS = 300

/** Whether a value may be used as a `req` correlation token. */
const isReqToken = value => typeof value === 'number' || typeof value === 'string'

/**
 * Classify one line read from the bridge.
 *
 * - `skip`     not this call's answer, keep reading: a JSON object without a
 *              boolean `ok` (banner/event line), a reply carrying another
 *              call's `req`, or free text that cannot be a payload.
 * - `response` `{"ok":true,...}` for this call.
 * - `failure`  `{"ok":false,"error":"..."}` for this call; the message is the
 *              mod's own actionable text.
 * - `invalid`  a genuine protocol fault: unparseable JSON that looks like a
 *              payload, or JSON that is not an object.
 *
 * @param {string} line - one trimmed line.
 * @param {unknown} req - this call's correlation token.
 * @returns {{kind:'skip'}|{kind:'response',response:object}|{kind:'failure',message:string}|{kind:'invalid',reason:string}}
 */
function classifyLine(line, req) {
  let parsed
  try {
    parsed = JSON.parse(line)
  } catch {
    // Free text (a log line the mod wrote straight to the socket) is not a
    // payload; skip it. A line that starts like a payload but does not parse is
    // a truncated/corrupt response and must fail loudly.
    if (!/^[[{]/.test(line)) return { kind: 'skip' }
    return { kind: 'invalid', reason: 'line is not valid JSON' }
  }
  if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
    return { kind: 'invalid', reason: 'response must be a JSON object' }
  }
  if (typeof parsed.ok !== 'boolean') return { kind: 'skip' }
  // Compare as text: the bridge may echo a numeric id as a string (or vice versa),
  // and a strict !== would treat every answer as somebody else's and time out.
  if (isReqToken(parsed.req) && String(parsed.req) !== String(req)) return { kind: 'skip' }
  if (parsed.ok !== true) {
    return { kind: 'failure', message: String(parsed.error ?? 'Terraria bridge rejected the command.') }
  }
  return { kind: 'response', response: parsed }
}

/**
 * Clear movement/jump/use/attack holds on a fresh short-lived connection.
 * Fire-and-forget by design: used on the abort path, where the caller must not
 * wait, must not throw, and must not turn a dead bridge into a second error.
 *
 * @param {{host?:string,port?:number,timeoutMs?:number}} [options]
 */
export function stopBestEffort({ host = '127.0.0.1', port = 47777, timeoutMs = STOP_TIMEOUT_MS } = {}) {
  try {
    const socket = net.createConnection({ host, port })
    // Never keep the process alive just for a courtesy stop.
    socket.unref?.()
    let done = false
    const timer = setTimeout(() => settle(), timeoutMs)
    timer.unref?.()
    function settle() {
      if (done) return
      done = true
      clearTimeout(timer)
      try { socket.destroy() } catch {}
    }
    socket.setNoDelay(true)
    socket.on('error', settle)
    socket.on('close', settle)
    socket.on('connect', () => {
      try {
        socket.write(`${JSON.stringify({ cmd: 'stop', params: {} })}\n`, () => {
          clearTimeout(timer)
          try { socket.end() } catch { settle() }
        })
      } catch {
        settle()
      }
    })
  } catch {
    // best effort: a throw here would mask the caller's original error
  }
}

/**
 * @param {{host?:string,port?:number,timeoutMs?:number,createConnection?:Function}} [options]
 *   `createConnection` is a test seam only; production always uses net.createConnection.
 */
/**
 * 这一条 `save` 的结果是不是"客户端存档、必须转给服务端进程"？
 *
 * Host&Play 会开两个 tModLoader 进程：无参客户端（连 47777）持有玩家但**不拥有世界**，
 * `-server` 进程（连 47778）才是世界的宿主。客户端上调 save 会失败（或返回 saved:false），
 * 毫无意义；真正能落盘的是服务端。所以看到 netMode:1 + saved:false 就换端口重发一次。
 */
export function needsServerSave(res) {
  const d = res && typeof res === 'object' && 'data' in res ? res.data : res
  if (!d || typeof d !== 'object') return false
  return d.saved === false && d.netMode === 1
}

export function createBridgeClient({ host = '127.0.0.1', port = 47777, timeoutMs = 5000, createConnection = net.createConnection } = {}) {
  const sockets = new Set()
  let nextReq = 1

  function request(command, params = {}, { signal } = {}) {
    if (signal?.aborted) {
      const error = new Error('Terraria bridge request was cancelled.')
      error.name = 'AbortError'
      return Promise.reject(error)
    }

    const req = nextReq++
    return new Promise((resolve, reject) => {
      const socket = createConnection({ host, port })
      sockets.add(socket)
      let buffer = Buffer.alloc(0)
      let connected = false
      let settled = false

      const cleanup = () => {
        clearTimeout(timer)
        signal?.removeEventListener('abort', abort)
        sockets.delete(socket)
      }
      const finish = (error, result) => {
        if (settled) return
        settled = true
        cleanup()
        socket.destroy()
        if (error) reject(error)
        else resolve(result)
      }
      const abort = () => {
        const error = new Error('Terraria bridge request was cancelled.')
        error.name = 'AbortError'
        finish(error)
      }
      const timer = setTimeout(() => {
        // Distinguish "cannot reach the bridge" from "the bridge took the call
        // and never answered": the two need completely different reactions.
        const error = connected
          ? new Error(`Timed out waiting for "${command}" after ${timeoutMs} ms (bridge connected, no reply)`)
          : new Error(`Timed out connecting to the Terraria bridge at ${host}:${port}.`)
        error.code = 'ETIMEDOUT'
        finish(error)
      }, timeoutMs)

      signal?.addEventListener('abort', abort, { once: true })
      socket.setNoDelay(true)
      socket.on('connect', () => {
        connected = true
        socket.write(`${JSON.stringify({ cmd: command, params, req })}\n`)
      })
      socket.on('data', chunk => {
        buffer = Buffer.concat([buffer, chunk])
        if (buffer.length > MAX_RESPONSE_BYTES) {
          finish(new Error(`Terraria bridge response exceeded ${MAX_RESPONSE_BYTES} bytes.`))
          return
        }
        while (!settled) {
          const newline = buffer.indexOf(0x0a)
          if (newline < 0) return
          const line = buffer.subarray(0, newline).toString('utf8').trim()
          buffer = buffer.subarray(newline + 1)
          if (!line) continue
          const verdict = classifyLine(line, req)
          if (verdict.kind === 'skip') continue
          if (verdict.kind === 'response') return finish(null, verdict.response)
          if (verdict.kind === 'failure') return finish(new Error(verdict.message))
          return finish(new Error(`Invalid Terraria bridge response: ${verdict.reason}`))
        }
      })
      socket.on('error', error => {
        if (error.code === 'ECONNREFUSED') {
          finish(new Error(`Terraria bridge is not running at ${host}:${port}. Load DSH Terraria Bridge in tModLoader first.`))
        } else {
          finish(error)
        }
      })
      socket.on('close', () => {
        if (settled) return
        // A writer that flushes and closes without the trailing newline still
        // delivered a complete payload; accept it instead of reporting a
        // truncated response.
        const tail = buffer.toString('utf8').trim()
        buffer = Buffer.alloc(0)
        if (tail) {
          const verdict = classifyLine(tail, req)
          if (verdict.kind === 'response') return finish(null, verdict.response)
          if (verdict.kind === 'failure') return finish(new Error(verdict.message))
        }
        finish(new Error('Terraria bridge closed the connection before returning a response.'))
      })
      if (signal?.aborted) abort()
    })
  }

  return {
    request,
    /** Short-timeout `stop` bound to this client's endpoint; see stopBestEffort(). */
    stopBestEffort: (options = {}) => stopBestEffort({ host, port, timeoutMs: Math.min(timeoutMs, STOP_TIMEOUT_MS), ...options }),
    close() {
      for (const socket of sockets) socket.destroy()
      sockets.clear()
    },
  }
}