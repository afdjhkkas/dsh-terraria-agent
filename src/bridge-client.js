import net from 'node:net'

const MAX_RESPONSE_BYTES = 2 * 1024 * 1024

export function createBridgeClient({ host = '127.0.0.1', port = 7777, timeoutMs = 5000 } = {}) {
  const sockets = new Set()

  function request(command, params = {}, { signal } = {}) {
    if (signal?.aborted) {
      const error = new Error('Terraria bridge request was cancelled.')
      error.name = 'AbortError'
      return Promise.reject(error)
    }

    return new Promise((resolve, reject) => {
      const socket = net.createConnection({ host, port })
      sockets.add(socket)
      let buffer = Buffer.alloc(0)
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
        const error = new Error(`Timed out connecting to the Terraria bridge at ${host}:${port}.`)
        error.code = 'ETIMEDOUT'
        finish(error)
      }, timeoutMs)

      signal?.addEventListener('abort', abort, { once: true })
      socket.setNoDelay(true)
      socket.on('connect', () => {
        socket.write(`${JSON.stringify({ cmd: command, params })}\n`)
      })
      socket.on('data', chunk => {
        buffer = Buffer.concat([buffer, chunk])
        if (buffer.length > MAX_RESPONSE_BYTES) {
          finish(new Error(`Terraria bridge response exceeded ${MAX_RESPONSE_BYTES} bytes.`))
          return
        }
        const newline = buffer.indexOf(0x0a)
        if (newline < 0) return
        const line = buffer.subarray(0, newline).toString('utf8').trim()
        try {
          const response = JSON.parse(line)
          if (!response || typeof response !== 'object' || Array.isArray(response)) {
            throw new Error('Bridge response must be a JSON object.')
          }
          if (response.ok !== true) {
            throw new Error(String(response.error ?? 'Terraria bridge rejected the command.'))
          }
          finish(null, response)
        } catch (error) {
          finish(new Error(`Invalid Terraria bridge response: ${error.message}`))
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
        if (!settled) finish(new Error('Terraria bridge closed the connection before returning a response.'))
      })
      if (signal?.aborted) abort()
    })
  }

  return {
    request,
    close() {
      for (const socket of sockets) socket.destroy()
      sockets.clear()
    },
  }
}
