#!/usr/bin/env node
/**
 * 桥的直接调用入口 —— 不依赖编辑器插件。
 *
 *   node scripts/bridge.mjs state
 *   node scripts/bridge.mjs inventory
 *   node scripts/bridge.mjs tile --params '{"x":2100,"y":300}'
 *   node scripts/bridge.mjs move --params '{"dir":"right","ms":400}'
 *   node scripts/bridge.mjs help
 *
 * 为什么需要它：插件（index.js 注册的 terraria_* 工具）在某些宿主里会因为
 * 长连接状态异常而超时，但桥本身是好的。这个脚本每次请求都新建一条 TCP 连接，
 * 不存在连接陈旧的问题，因此既是插件的后备通道，也是"新环境能不能跑起来"的
 * 第一道自检。
 *
 * 协议是两级结构，发错格式会静默走默认值（见 AGENTS.md §7.5）：
 *   {"cmd":"move","params":{"dir":"right"}}      ← 正确
 *   {"cmd":"move","dir":"right"}                 ← 错误，dir 会被忽略
 *
 * 环境变量：TERRARIA_BRIDGE_HOST（默认 127.0.0.1）、TERRARIA_BRIDGE_PORT（默认 47777）、
 * TERRARIA_BRIDGE_TIMEOUT_MS（默认 15000）。
 */

import { pathToFileURL } from 'node:url'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const HERE = dirname(fileURLToPath(import.meta.url))
const { createBridgeClient } = await import(pathToFileURL(join(HERE, '..', 'src', 'bridge-client.js')).href)

const HOST = process.env.TERRARIA_BRIDGE_HOST || '127.0.0.1'
const PORT = Number(process.env.TERRARIA_BRIDGE_PORT || 47777)
const TIMEOUT_MS = Number(process.env.TERRARIA_BRIDGE_TIMEOUT_MS || 15000)

const USAGE = `用法:
  node scripts/bridge.mjs <命令> [--params '<JSON>'] [--raw] [--timeout <ms>]

例:
  node scripts/bridge.mjs state
  node scripts/bridge.mjs tile --params '{"x":2100,"y":300}'
  node scripts/bridge.mjs help          # 打印桥支持的全部命令

环境变量:
  TERRARIA_BRIDGE_HOST (默认 ${HOST})
  TERRARIA_BRIDGE_PORT (默认 ${PORT})
  TERRARIA_BRIDGE_TIMEOUT_MS (默认 ${TIMEOUT_MS})

退出码: 0 成功 / 1 桥报错 / 2 用法错误 / 3 连不上桥`

function fail(code, message) {
  console.error(message)
  process.exit(code)
}

const argv = process.argv.slice(2)
if (argv.length === 0 || argv[0] === '-h' || argv[0] === '--help') fail(2, USAGE)

const cmd = argv[0]
let params = {}
let raw = false
let timeoutMs = TIMEOUT_MS

for (let i = 1; i < argv.length; i++) {
  const a = argv[i]
  if (a === '--params') {
    const text = argv[++i]
    if (text === undefined) fail(2, '--params 后面要跟一个 JSON 对象')
    try {
      params = JSON.parse(text)
    } catch (err) {
      fail(2, `--params 不是合法 JSON: ${err.message}`)
    }
    if (params === null || typeof params !== 'object' || Array.isArray(params)) {
      fail(2, '--params 必须是一个 JSON 对象，例如 \'{"x":1,"y":2}\'')
    }
  } else if (a === '--raw') {
    raw = true
  } else if (a === '--timeout') {
    const v = Number(argv[++i])
    if (!Number.isFinite(v) || v <= 0) fail(2, '--timeout 要一个正整数毫秒数')
    timeoutMs = v
  } else {
    fail(2, `不认识的参数: ${a}\n\n${USAGE}`)
  }
}

const client = createBridgeClient({ host: HOST, port: PORT, timeoutMs })

let exitCode = 0
try {
  const res = await client.request(cmd, params)
  process.stdout.write(JSON.stringify(raw ? res : (res?.data ?? res), null, 2) + '\n')
  if (res && res.ok === false) exitCode = 1
} catch (err) {
  const msg = String(err?.message || err)
  // 连不上和"桥回了错误"是两件事，退出码分开，脚本才分得清该修环境还是修命令。
  const connish = /ECONNREFUSED|EHOSTUNREACH|ENOTFOUND|ECONNRESET|超时|timeout/i.test(msg)
  console.error(`${connish ? '连不上桥' : '桥报错'}: ${msg}`)
  if (connish) {
    console.error(`检查：游戏是否在运行、是否已进入世界、${HOST}:${PORT} 是否在监听。`)
    console.error('Windows 上可先跑: curl -s --max-time 2 http://127.0.0.1:1/ 无关；或 netstat -ano | findstr ' + PORT)
  }
  exitCode = connish ? 3 : 1
} finally {
  client.close()
}

process.exit(exitCode)
