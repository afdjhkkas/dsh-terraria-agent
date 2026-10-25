import { test } from 'node:test'
import assert from 'node:assert/strict'
import { needsServerSave } from '../src/bridge-client.js'

// Host&Play 下世界归属于另一个进程：客户端（netMode 1）存盘必然失败。
// 这条判据决定要不要把 save 转发给服务端端口，判错就会"看起来存档成功、文件其实没动"。
test('客户端存盘失败（saved:false + netMode:1）→ 必须转服务端', () => {
  assert.equal(needsServerSave({ ok: true, data: { saved: false, netMode: 1 } }), true)
})

test('服务端存盘成功 → 不再转发', () => {
  assert.equal(needsServerSave({ ok: true, data: { saved: true, netMode: 2, via: 'SaveWorld()' } }), false)
})

test('服务端存盘失败（netMode:2）→ 不转发（换端口也没用）', () => {
  assert.equal(needsServerSave({ data: { saved: false, netMode: 2 } }), false)
})

test('裸对象与异常输入都不炸', () => {
  assert.equal(needsServerSave({ saved: false, netMode: 1 }), true)
  assert.equal(needsServerSave(null), false)
  assert.equal(needsServerSave(undefined), false)
  assert.equal(needsServerSave('nope'), false)
  assert.equal(needsServerSave({ data: null }), false)
})
