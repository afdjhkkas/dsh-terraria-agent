/**
 * probe 命令使用示例：试探性移动预判
 *
 * probe 可以模拟向某方向移动 N 格，预判会不会掉坑/撞墙，但不实际移动。
 * 适合 AI 在移动前做安全检查，决定要不要真走或搭桥。
 */

import { request } from '../src/index.js'

async function main() {
  // 1. 试探向右走 10 格
  console.log('Testing: 向右走 10 格安全吗？')
  const rightProbe = await request('probe', { dir: 'right', steps: 10 })

  if (rightProbe.data.safe) {
    console.log(`✓ 安全，可以走 ${rightProbe.data.reachedSteps} 格`)
  } else {
    console.log(`✗ 不安全：${rightProbe.data.issue}`)
    console.log(`  只能走到第 ${rightProbe.data.reachedSteps} 格`)
  }

  // 2. 试探向左走 5 格
  console.log('\nTesting: 向左走 5 格安全吗？')
  const leftProbe = await request('probe', { dir: 'left', steps: 5 })

  if (leftProbe.data.safe) {
    console.log(`✓ 安全，可以走 ${leftProbe.data.reachedSteps} 格`)
  } else {
    console.log(`✗ 不安全：${leftProbe.data.issue}`)

    // 根据问题类型决定策略
    if (leftProbe.data.issue.startsWith('pit_')) {
      const depth = parseInt(leftProbe.data.issue.split('_')[1])
      if (depth <= 3) {
        console.log('  → 建议：跳过去')
      } else {
        console.log('  → 建议：搭桥或绕路')
      }
    } else if (leftProbe.data.issue === 'blocked') {
      console.log('  → 建议：挖墙或爬高绕过')
    }
  }

  // 3. 实际应用：探索循环
  console.log('\n--- 探索演示 ---')
  for (const dir of ['left', 'right']) {
    const probe = await request('probe', { dir, steps: 20 })
    console.log(`${dir}: 可走 ${probe.data.reachedSteps}/20 格`, probe.data.safe ? '✓' : `✗ ${probe.data.issue}`)
  }
}

main().catch(console.error)
