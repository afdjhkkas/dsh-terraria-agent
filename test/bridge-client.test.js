import assert from 'node:assert/strict'
import net from 'node:net'
import test from 'node:test'
import { createBridgeClient } from '../src/bridge-client.js'

async function withServer(handler, run) {
  const server = net.createServer(handler)
  await new Promise((resolve, reject) => {
    server.once('error', reject)
    server.listen(0, '127.0.0.1', resolve)
  })
  const { port } = server.address()
  try {
    await run(port)
  } finally {
    await new Promise(resolve => server.close(resolve))
  }
}

test('sends a JSON-line command and returns the bridge data envelope', async () => {
  await withServer(socket => {
    let input = ''
    socket.setEncoding('utf8')
    socket.on('data', chunk => {
      input += chunk
      const end = input.indexOf('\n')
      if (end < 0) return
      const request = JSON.parse(input.slice(0, end))
      socket.write(`${JSON.stringify({ ok: true, data: { echoed: request } })}\n`)
    })
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 1000 })
    const result = await client.request('tile', { x: 12, y: 34 })
    assert.deepEqual(result.data.echoed, { cmd: 'tile', params: { x: 12, y: 34 } })
    client.close()
  })
})

test('reports bridge command errors and times out when the bridge does not reply', async () => {
  await withServer(socket => {
    socket.setEncoding('utf8')
    socket.on('data', chunk => {
      if (chunk.includes('"cmd":"bad"')) socket.write('{"ok":false,"error":"unknown command"}\n')
    })
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 100 })
    await assert.rejects(client.request('bad'), /unknown command/)
    await assert.rejects(client.request('wait'), error => error.code === 'ETIMEDOUT')
    client.close()
  })
})

test('cancels a pending request through the DSH execution signal', async () => {
  await withServer(socket => socket.on('data', () => {}), async port => {
    const client = createBridgeClient({ port, timeoutMs: 1000 })
    const controller = new AbortController()
    const request = client.request('wait', {}, { signal: controller.signal })
    controller.abort()
    await assert.rejects(request, error => error.name === 'AbortError')
    client.close()
  })
})
