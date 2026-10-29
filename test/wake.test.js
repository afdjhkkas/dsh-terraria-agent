import test from 'node:test'
import assert from 'node:assert/strict'
import { mintCookie, pickTargetSession, readBrowserSessionSecret, resolveDshHome } from '../scripts/wake.mjs'

// 这些用例只碰纯函数：不连 DSH、不投递。
// 真正的投递在 scripts/wake.mjs 的 CLI 上验，测不了也不该在 npm test 里依赖本机 DSH 在跑。

test('mintCookie：同一个密钥同一毫秒，签出来的 Cookie 完全一致', () => {
  const secret = Buffer.alloc(32, 7)
  const now = 1_700_000_000_000
  const a = mintCookie(secret, 'http://127.0.0.1:49701', now)
  const b = mintCookie(secret, 'http://127.0.0.1:49701', now)
  assert.equal(a.header, b.header)
  assert.equal(a.authority, '127.0.0.1:49701')
})

test('mintCookie：Cookie 名由 authority 决定，换端口就换名', () => {
  const secret = Buffer.alloc(32, 1)
  const a = mintCookie(secret, 'http://127.0.0.1:49701')
  const a2 = mintCookie(Buffer.alloc(32, 9), 'http://127.0.0.1:49701')
  const b = mintCookie(secret, 'http://127.0.0.1:50777')
  assert.notEqual(a.name, b.name, '换端口必须换名')
  assert.equal(a.name, a2.name, '换密钥不该影响名字（名字只由 authority 派生）')
  assert.ok(a.name.startsWith('dsh-auth-'))
})

test('mintCookie：值是三段 v1.<body>.<sig>，且没有 base64 的 +/= 字符', () => {
  const c = mintCookie(Buffer.alloc(32, 3), 'http://127.0.0.1:49701')
  assert.match(c.value, /^v1\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$/)
  assert.ok(!c.value.includes('='))
})

test('mintCookie：密钥不同则签名不同', () => {
  const now = 1_700_000_000_000
  const a = mintCookie(Buffer.alloc(32, 1), 'http://127.0.0.1:49701', now)
  const b = mintCookie(Buffer.alloc(32, 2), 'http://127.0.0.1:49701', now)
  assert.notEqual(a.header, b.header)
})

test('mintCookie：有效期是签发后一小时', () => {
  const now = 1_700_000_000_000
  const c = mintCookie(Buffer.alloc(32, 5), 'http://127.0.0.1:49701', now)
  assert.equal(c.expiresAt - now, 60 * 60 * 1000)
})

test('pickTargetSession：优先挑正在运行的会话', () => {
  const sessions = [
    { sessionId: 'a', running: false, updatedAt: 300 },
    { sessionId: 'b', running: true, updatedAt: 100 },
    { sessionId: 'c', running: false, updatedAt: 200 },
  ]
  assert.equal(pickTargetSession(sessions).sessionId, 'b')
})

test('pickTargetSession：都没有 running 就退到最近更新的那个', () => {
  const sessions = [
    { sessionId: 'old', running: false, updatedAt: 100 },
    { sessionId: 'new', running: false, updatedAt: 900 },
  ]
  assert.equal(pickTargetSession(sessions).sessionId, 'new')
})

test('pickTargetSession：空列表返回 null，不抛', () => {
  assert.equal(pickTargetSession([]), null)
})

test('resolveDshHome：默认是用户目录下的 .dsh', () => {
  const home = resolveDshHome()
  assert.ok(home.endsWith('.dsh'), '应为 ~/.dsh，实际 ' + home)
})

test('readBrowserSessionSecret：本机凭据里能取到 32 字节密钥', (t) => {
  let secret
  try {
    secret = readBrowserSessionSecret()
  } catch (e) {
    t.skip('本机没有 DSH 凭据，跳过：' + e.message)
    return
  }
  assert.equal(secret.length, 32)
  assert.ok(Buffer.isBuffer(secret))
})
