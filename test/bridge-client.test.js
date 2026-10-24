import assert from 'node:assert/strict'
import net from 'node:net'
import test from 'node:test'
import { createBridgeClient, stopBestEffort } from '../src/bridge-client.js'

/**
 * Start a loopback server that records the lines it receives, and tear it down
 * (including any half-open client socket) when the test finishes.
 */
async function withServer(handler, run) {
  const connections = new Set()
  const server = net.createServer(socket => {
    connections.add(socket)
    socket.on('close', () => connections.delete(socket))
    socket.on('error', () => {})
    handler(socket)
  })
  await new Promise((resolve, reject) => {
    server.once('error', reject)
    server.listen(0, '127.0.0.1', resolve)
  })
  const { port } = server.address()
  try {
    await run(port)
  } finally {
    for (const socket of connections) socket.destroy()
    await new Promise(resolve => server.close(resolve))
  }
}

/** A port nobody is listening on. */
async function closedPort() {
  const server = net.createServer()
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve))
  const { port } = server.address()
  await new Promise(resolve => server.close(resolve))
  return port
}

test('sends cmd/params/req as one JSON line and returns the data envelope', async () => {
  let seen = null
  await withServer(socket => {
    let input = ''
    socket.setEncoding('utf8')
    socket.on('data', chunk => {
      input += chunk
      const end = input.indexOf('\n')
      if (end < 0) return
      seen = JSON.parse(input.slice(0, end))
      socket.write(`${JSON.stringify({ ok: true, data: { echoed: seen }, req: seen.req })}\n`)
    })
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 1000 })
    const result = await client.request('tile', { x: 12, y: 34 })
    assert.deepEqual(result.data.echoed.cmd, 'tile')
    assert.deepEqual(result.data.echoed.params, { x: 12, y: 34 })
    assert.equal(typeof seen.req, 'number')
    client.close()
  })
})

test('a valid {"ok":false} reply surfaces as its own message, never as a protocol fault', async () => {
  await withServer(socket => {
    socket.once('data', () => socket.write('{"ok":false,"error":"not in a world"}\n'))
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    await assert.rejects(client.request('state'), error => {
      assert.equal(error.message, 'not in a world')
      assert.doesNotMatch(error.message, /Invalid Terraria bridge response/)
      return true
    })
    client.close()
  })
})

test('a reply without an error string still surfaces as a failure, not as a protocol fault', async () => {
  await withServer(socket => {
    socket.once('data', () => socket.write('{"ok":false}\n'))
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    await assert.rejects(client.request('state'), /^Error: Terraria bridge rejected the command\.$/)
    client.close()
  })
})

test('connected-but-silent times out with the waiting wording, not the connecting wording', async () => {
  await withServer(() => {}, async port => {
    const client = createBridgeClient({ port, timeoutMs: 120 })
    await assert.rejects(client.request('wait'), error => {
      assert.equal(error.code, 'ETIMEDOUT')
      assert.equal(error.message, 'Timed out waiting for "wait" after 120 ms (bridge connected, no reply)')
      return true
    })
    client.close()
  })
})

test('a socket that never connects times out with the connecting wording', async () => {
  const client = createBridgeClient({
    port: 7777,
    timeoutMs: 80,
    // test seam: a socket that never connects and never answers
    createConnection: () => new net.Socket(),
  })
  await assert.rejects(client.request('state'), error => {
    assert.equal(error.code, 'ETIMEDOUT')
    assert.equal(error.message, 'Timed out connecting to the Terraria bridge at 127.0.0.1:7777.')
    return true
  })
  client.close()
})

test('an unsolicited banner line is skipped and the real reply still arrives', async () => {
  await withServer(socket => {
    socket.once('data', () => socket.write('{"hello":"bridge ready","version":"0.2.0"}\n{"ok":true,"data":{"late":true}}\n'))
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    const result = await client.request('state')
    assert.deepEqual(result.data, { late: true })
    client.close()
  })
})

test('free text that cannot be a payload is skipped too', async () => {
  await withServer(socket => {
    socket.once('data', () => socket.write('bridge ready\n{"ok":true,"data":{"n":1}}\n'))
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    const result = await client.request('state')
    assert.deepEqual(result.data, { n: 1 })
    client.close()
  })
})

test('a reply carrying another call\'s req is ignored', async () => {
  await withServer(socket => {
    let input = ''
    socket.setEncoding('utf8')
    socket.on('data', chunk => {
      input += chunk
      const end = input.indexOf('\n')
      if (end < 0) return
      const req = JSON.parse(input.slice(0, end)).req
      socket.write(`${JSON.stringify({ ok: true, data: { stale: true }, req: req + 1000 })}\n`)
      socket.write(`${JSON.stringify({ ok: true, data: { fresh: true }, req })}\n`)
    })
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    const result = await client.request('state')
    assert.deepEqual(result.data, { fresh: true })
    client.close()
  })
})

test('a reply without req is accepted (the mod may not echo it)', async () => {
  await withServer(socket => {
    socket.once('data', () => socket.write('{"ok":true,"data":{"noReq":true}}\n'))
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    const result = await client.request('state')
    assert.deepEqual(result.data, { noReq: true })
    client.close()
  })
})

test('a corrupt payload object fails fast with the Invalid prefix', async () => {
  await withServer(socket => {
    socket.once('data', () => socket.write('{"ok":tru\n{"ok":true,"data":{"never":true}}\n'))
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    await assert.rejects(client.request('state'), /^Error: Invalid Terraria bridge response: line is not valid JSON$/)
    client.close()
  })
})

test('a non-object JSON payload fails with the Invalid prefix', async () => {
  await withServer(socket => {
    socket.once('data', () => socket.write('[1,2,3]\n'))
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    await assert.rejects(client.request('state'), /^Error: Invalid Terraria bridge response: response must be a JSON object$/)
    client.close()
  })
})

test('a complete payload without a trailing newline is accepted when the socket closes', async () => {
  await withServer(socket => {
    socket.once('data', () => {
      socket.write('{"ok":true,"data":{"tail":true}}')
      setTimeout(() => socket.destroy(), 20)
    })
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    const result = await client.request('state')
    assert.deepEqual(result.data, { tail: true })
    client.close()
  })
})

test('a truncated payload without a newline still reports a closed connection', async () => {
  await withServer(socket => {
    socket.once('data', () => {
      socket.write('{"ok":true,"data":{"tail"')
      setTimeout(() => socket.destroy(), 20)
    })
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    await assert.rejects(client.request('state'), /closed the connection before returning a response/)
    client.close()
  })
})

test('a multibyte character split across TCP chunks decodes correctly', async () => {
  await withServer(socket => {
    socket.once('data', () => {
      const payload = Buffer.from('{"ok":true,"data":{"name":"世界"}}\n', 'utf8')
      const cut = payload.indexOf(Buffer.from('世')) + 1
      socket.write(payload.subarray(0, cut))
      setTimeout(() => socket.write(payload.subarray(cut)), 20)
    })
  }, async port => {
    const client = createBridgeClient({ port, timeoutMs: 1000 })
    const result = await client.request('state')
    assert.deepEqual(result.data, { name: '世界' })
    client.close()
  })
})

test('an ECONNREFUSED port reports the bridge as not running', async () => {
  const port = await closedPort()
  const client = createBridgeClient({ port, timeoutMs: 500 })
  await assert.rejects(client.request('state'), /Terraria bridge is not running at 127\.0\.0\.1:\d+/)
  client.close()
})

test('cancels a pending request through the DSH execution signal', async () => {
  await withServer(socket => socket.on('data', () => {}), async port => {
    const client = createBridgeClient({ port, timeoutMs: 1000 })
    const controller = new AbortController()
    const request = client.request('wait', {}, { signal: controller.signal })
    controller.abort()
    await assert.rejects(request, error => error.name === 'AbortError' && /cancelled/.test(error.message))
    client.close()
  })
})

test('a request is never written when the signal is already aborted', async () => {
  let received = 0
  await withServer(socket => socket.on('data', () => { received += 1 }), async port => {
    const client = createBridgeClient({ port, timeoutMs: 500 })
    const controller = new AbortController()
    controller.abort()
    await assert.rejects(client.request('state', {}, { signal: controller.signal }), error => error.name === 'AbortError')
    assert.equal(received, 0)
    client.close()
  })
})

test('stopBestEffort does not throw when the bridge is down', async () => {
  const port = await closedPort()
  assert.doesNotThrow(() => stopBestEffort({ port, timeoutMs: 100 }))
  assert.doesNotThrow(() => createBridgeClient({ port }).stopBestEffort())
  // let the refused connections settle so nothing leaks into the next test
  await new Promise(resolve => setTimeout(resolve, 60))
})

test('stopBestEffort sends {"cmd":"stop"} to a live bridge', async () => {
  let line = null
  await withServer(socket => {
    let input = ''
    socket.setEncoding('utf8')
    socket.on('data', chunk => {
      input += chunk
      const end = input.indexOf('\n')
      if (end < 0) return
      line = JSON.parse(input.slice(0, end))
      socket.write('{"ok":true,"data":{"stopped":true}}\n')
    })
  }, async port => {
    stopBestEffort({ port, timeoutMs: 300 })
    for (let i = 0; i < 50 && line === null; i++) await new Promise(resolve => setTimeout(resolve, 10))
    assert.deepEqual(line, { cmd: 'stop', params: {} })
  })
})