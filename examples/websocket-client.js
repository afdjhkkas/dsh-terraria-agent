/**
 * WebSocket 客户端示例：实时接收玩家状态变化推送
 *
 * 用法：
 * 1. 启动游戏并进入世界（DshTerrariaBridge mod 会自动启动 WebSocket 服务）
 * 2. 运行：node examples/websocket-client.js
 *
 * 推送事件类型：
 * - connected: 连接成功
 * - player_state: 玩家状态变化（血量/蓝量/位置/buff）
 */

const WebSocket = require('ws')

const ws = new WebSocket('ws://127.0.0.1:47778')

ws.on('open', () => {
  console.log('Connected to Terraria WebSocket server')
})

ws.on('message', (data) => {
  try {
    const msg = JSON.parse(data.toString())
    console.log('[Event]', msg.type)

    switch (msg.type) {
      case 'connected':
        console.log('  →', msg.message)
        break

      case 'player_state':
        const changes = []
        if (msg.life !== undefined) changes.push(`Life: ${msg.life}/${msg.maxLife}`)
        if (msg.mana !== undefined) changes.push(`Mana: ${msg.mana}/${msg.maxMana}`)
        if (msg.x !== undefined) changes.push(`Pos: (${msg.x}, ${msg.y})`)
        if (msg.buffs !== undefined) changes.push(`Buffs: ${msg.buffs.length}`)
        console.log('  →', changes.join(' | '))
        break

      default:
        console.log('  → Unknown event:', msg)
    }
  } catch (err) {
    console.error('Parse error:', err.message)
  }
})

ws.on('close', () => {
  console.log('Disconnected from server')
})

ws.on('error', (err) => {
  console.error('WebSocket error:', err.message)
})
