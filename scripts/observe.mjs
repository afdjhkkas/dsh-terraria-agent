#!/usr/bin/env node
/**
 * 观摩模式 CLI —— 让 AI 先看人怎么打，再自己打。
 * ============================================================================
 *   node scripts/observe.mjs status               只看当前模式和案例库统计
 *   node scripts/observe.mjs observe [--ms 30000] 进观摩模式采样（不写世界）
 *   node scripts/observe.mjs auto                 切回自动模式
 *   node scripts/observe.mjs report [--limit 10]  看本场学到了什么
 *
 * 观摩期间脚本只发读命令（state/humaninput/npc/tile/inventory），写命令会被
 * guard 直接拦下并抛 ModeLockedError —— 这是"只看不做"的硬保证，不是自觉。
 *
 * 采到的东西分三类，全部基于逐帧对齐：
 *   1. 游戏状态   —— 位置、生命、地形介质、威胁距离（每帧一次）
 *   2. 人按键时序 —— 真人的控制键/鼠标/选槽（每帧一次，来自 humaninput）
 *   3. 脚本判断   —— 同一帧上，脚本按同一份状态会怎么做（planner 纯函数）
 * 只有"人做的和脚本推演的不一样"才入库；一致的不算学问。
 * ============================================================================
 */
import { createBridgeClient } from '../src/bridge-client.js'
import { DemoLibrary, tuneTiming } from '../src/demo-library.js'
import {
  MODES,
  createGuard,
  createModeStore,
  createObserver,
  defaultPlanner,
  formatLibraryReport,
  formatObserveReport,
} from '../src/observe-mode.js'

const args = process.argv.slice(2)
const cmd = args[0] || 'status'
const flag = (name, fallback) => {
  const i = args.indexOf(`--${name}`)
  return i >= 0 && args[i + 1] !== undefined ? Number(args[i + 1]) : fallback
}
const port = flag('port', 47777)

const modeStore = createModeStore()
const library = new DemoLibrary()
const client = createBridgeClient({ port, timeoutMs: 8000 })
const guard = createGuard((req) => client.request(req.cmd, req.params || {}), { getMode: () => modeStore.get() })
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms))
/** 桥的回包是 {req, ok, data} 信封；读状态一律取 data，别把信封当数据用。 */
const unwrap = r => (r && typeof r === 'object' && 'data' in r ? r.data : r)

/** 把桥的只读回包拼成 planner/观测器要的那份状态；dx 由相邻两帧格子差推出来。 */
function createLiveReader() {
  let last = null
  let frame = 0
  let ctx = { zone: 'surface', threat: 999, life: 0, lifeMax: 100 }

  return async function read() {
    const hi = unwrap(await guard({ cmd: 'humaninput' }))
    const p = hi?.player || {}
    // 每 10 帧补一次重信息（区域、威胁），帧内沿用——这些量变化慢，没必要每帧打。
    if (frame++ % 10 === 0) {
      try {
        const st = unwrap(await guard({ cmd: 'state' }))
        const npcs = unwrap(await guard({ cmd: 'npc', params: { r: 24 } }))
        ctx = {
          zone: st?.player?.zones?.[0] || st?.player?.zone || 'surface',
          life: st?.player?.life ?? p.life,
          lifeMax: st?.player?.lifeMax ?? p.lifeMax,
          threat: Array.isArray(npcs?.npcs)
            ? npcs.npcs.reduce((m, n) => Math.min(m, Number(n.dist ?? 999)), 999)
            : 999,
        }
      } catch {
        // 补信息失败不影响本帧采样：沿用上一轮的 zone/threat，不编造。
      }
    }

    const tile = p.tile || {}
    const dx = last ? Number(tile.x) - Number(last.x) : 0
    const dy = last ? Number(tile.y) - Number(last.y) : 0
    last = tile

    return {
      snap: {
        inWorld: !!hi?.inWorld && !hi?.paused,
        dead: !!p.dead,
        life: p.life ?? ctx.life,
        lifeMax: p.lifeMax ?? ctx.lifeMax,
        wet: !!p.wet,
        grounded: !!p.grounded,
        zone: ctx.zone,
        threatDist: ctx.threat,
        dx,
        dy,
        stuck: Math.abs(dx) === 0 && Math.abs(dy) === 0,
      },
      input: { control: hi?.control || {}, mouse: hi?.mouse || {}, player: p },
    }
  }
}

function printStatus() {
  const stats = library.stats()
  console.log(`模式          : ${modeStore.get()}`)
  console.log(`案例文件      : ${library.file}`)
  console.log(`示范案例      : ${stats.total} 条（可用 ${stats.usable}，已丢弃 ${stats.discarded}）`)
  if (stats.total) {
    console.log('场景分布      :')
    for (const [label, n] of Object.entries(stats.byLabel).slice(0, 8)) {
      console.log(`  ${label} :: ${n}`)
    }
  }
}

if (cmd === 'status') {
  printStatus()
} else if (cmd === 'auto') {
  modeStore.set(MODES.auto)
  console.log(`模式已切换为 ${MODES.auto}（脚本可以操作世界了）`)
} else if (cmd === 'report') {
  const limit = flag('limit', 8)
  const stats = library.stats()
  const corrections = library.corrections(limit)
  const tuned = tuneTiming({}, corrections, { minSim: 0, minSamples: 1 })
  console.log(formatLibraryReport({ stats, corrections }, { limit }))
  console.log('')
  console.log('## 时序基线（只用这 %d 条样本调过之后）', corrections.length)
  if (!tuned.applied.length && !tuned.clamped.length) {
    console.log('  （样本不足 3 条或相似度不够，本次没有参数的改动）')
  } else {
    for (const a of tuned.applied) console.log(`  ${a.param}: ${a.from} → ${a.to}（sim ${a.sim}，来自 ${a.from_case || 'unknown'}）`)
    for (const c of tuned.clamped) console.log(`  ${c.param}: 人想 ${c.wanted}，被知识库边界 ${c.bounds.join('..')} 夹回 ${c.allowed}`)
  }
} else if (cmd === 'observe') {
  const ms = flag('ms', 30000)
  const everyMs = flag('every', 100)
  modeStore.set(MODES.observe)
  console.log(`观察中（${ms} ms，每 ${everyMs} ms 一帧）。你可以正常操作，脚本只在旁边记录。`)

  const observer = createObserver({
    read: createLiveReader(),
    library,
    planner: defaultPlanner,
    everyMs,
    onSample: s => console.log(`  + ${s.label} :: ${JSON.stringify(s.delta).slice(0, 160)}`),
  })
  const result = await observer.run({ ms, maxSamples: 200 })
  console.log('')
  console.log(formatObserveReport({ stats: result.stats || observer.stats(), corrections: observer.corrections() }))
  library.flush()
  modeStore.set(MODES.auto)
  console.log(`\n采样结束，已切回 ${MODES.auto}。下次自动跑会按这些案例微调时序。`)
} else {
  console.error(`未知子命令: ${cmd}\n用法: status | observe [--ms N] | auto | report [--limit N]`)
  process.exit(2)
}