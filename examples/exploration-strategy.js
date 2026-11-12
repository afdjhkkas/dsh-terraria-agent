/**
 * 探索策略示例：结合 scan、probe、path 实现智能探索
 *
 * 演示如何让 AI 在移动前先分析地形、预判风险、选择策略。
 */

import { request } from '../src/index.js'

/**
 * 智能向右探索一段距离
 * @param {number} distance - 目标距离（格数）
 */
async function exploreRight(distance = 20) {
  console.log(`\n=== 探索目标：向右 ${distance} 格 ===\n`)

  // 第 1 步：扫描前方地形
  console.log('1. 扫描前方地形...')
  const scan = await request('scan', { r: distance })
  const { x0, y0, w, h, tiles } = scan.data

  // 简单分析：检查前方有没有大坑（连续 5+ 格空）
  console.log(`   地形范围：${w}×${h} (中心 ${scan.data.center})`)

  // 第 2 步：试探性移动预判
  console.log('\n2. 试探向右移动...')
  const probe = await request('probe', { dir: 'right', steps: distance })

  if (probe.data.safe) {
    console.log(`   ✓ 安全，可以直接走 ${probe.data.reachedSteps} 格`)
    console.log('\n3. 执行：直接 walkto')
    const state = await request('state')
    const { x, y } = state.data.player.tile
    await request('walkto', { x: x + distance, y })
    console.log('   → 移动完成')
  } else {
    console.log(`   ✗ 不安全：${probe.data.issue}`)
    console.log(`   只能走到第 ${probe.data.reachedSteps} 格`)

    // 第 3 步：根据障碍类型选择策略
    if (probe.data.issue === 'blocked') {
      console.log('\n3. 策略：遇墙，尝试爬高绕过')
      // 这里可以调用 jump + path 组合
      console.log('   → 跳跃 + 重新规划路径（待实现）')
    } else if (probe.data.issue.startsWith('pit_')) {
      const depth = parseInt(probe.data.issue.split('_')[1])
      if (depth <= 3) {
        console.log('\n3. 策略：浅坑，跳过去')
        await request('jump', { dir: 'right' })
        console.log('   → 跳跃完成')
      } else {
        console.log('\n3. 策略：深坑，搭桥')
        // 这里需要：选平台 → craft → 瞄准 → place
        console.log('   → 搭桥（待实现：需要 craft + place 组合）')
      }
    } else if (probe.data.issue === 'pit_deep') {
      console.log('\n3. 策略：极深坑（>14格），绕路或下探')
      console.log('   → 寻找其他路径')
    }
  }
}

/**
 * 持续探索模式：每次走一段，遇障碍就处理
 */
async function continuousExplore() {
  console.log('\n=== 持续探索模式 ===')
  console.log('每次向右探索 10 格，遇障碍就停下\n')

  for (let i = 0; i < 5; i++) {
    console.log(`\n--- 第 ${i + 1} 轮 ---`)

    const probe = await request('probe', { dir: 'right', steps: 10 })

    if (probe.data.safe) {
      console.log('前方安全，继续前进 10 格')
      const state = await request('state')
      const { x, y } = state.data.player.tile
      await request('walkto', { x: x + 10, y })
    } else {
      console.log(`遇到障碍：${probe.data.issue}，停止探索`)
      console.log(`本次探索共前进 ${i * 10 + probe.data.reachedSteps} 格`)
      break
    }

    // 间隔 2 秒
    await new Promise((resolve) => setTimeout(resolve, 2000))
  }
}

// 运行演示
async function main() {
  const mode = process.argv[2] || 'single'

  if (mode === 'continuous') {
    await continuousExplore()
  } else {
    await exploreRight(20)
  }
}

main().catch(console.error)
