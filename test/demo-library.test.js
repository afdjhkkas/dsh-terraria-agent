import test from 'node:test'
import assert from 'node:assert/strict'
import { mkdtempSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

import {
  TIMING_BOUNDS,
  DemoLibrary,
  actionDelta,
  sceneLabel,
  sceneOf,
  similarity,
  tuneTiming,
  validateSample,
} from '../src/demo-library.js'

const tmp = () => join(mkdtempSync(join(tmpdir(), 'dsh-demo-')), 'cases.jsonl')
const baseSnap = { life: 100, lifeMax: 100, grounded: true, wet: false, zone: 'surface', dx: 3, dy: 0, hostileDist: 20 }

test('sceneOf 把连续量分档，坐标不进键', () => {
  const a = sceneOf({ ...baseSnap, dx: 3 })
  const b = sceneOf({ ...baseSnap, dx: 4 })
  assert.deepEqual(a, b, '同一档位应是同一个场景键')
  assert.equal(a.hp, 'high')
  assert.equal(a.threat, 'clear')
  const low = sceneOf({ ...baseSnap, life: 30 })
  assert.equal(low.hp, 'low')
  const critical = sceneOf({ ...baseSnap, life: 10 })
  assert.equal(critical.hp, 'critical')
})

test('sceneLabel 稳定且可读', () => {
  assert.equal(sceneLabel(baseSnap), 'surface|dry|ground|hp-high|dx8|dy0|threat-clear|still')
})

test('similarity：同场景 1.0，换区明显下降', () => {
  assert.equal(similarity(baseSnap, baseSnap), 1)
  const other = similarity(baseSnap, { ...baseSnap, zone: 'underground', wet: 1, grounded: false })
  assert.ok(other < 0.6, `换区换介质应显著降低，实际 ${other}`)
  const near = similarity({ ...baseSnap, life: 60 }, baseSnap)
  assert.ok(near > 0.8 && near < 1, `相邻血量档应只掉一点，实际 ${near}`)
})

test('validateSample 丢弃不合法的示范', () => {
  const human = { kind: 'move', moveHoldMs: 600 }
  assert.equal(validateSample({ snap: baseSnap, human }).ok, true)
  assert.equal(validateSample({ snap: { ...baseSnap, dead: true }, human }).reason, 'player-dead')
  assert.equal(validateSample({ snap: { ...baseSnap, inWorld: false }, human }).reason, 'not-in-world')
  assert.equal(validateSample({ snap: { ...baseSnap, paused: true }, human }).reason, 'paused')
  // 安全线拦的是"低血还干活"（挖/打/放），不是逃生：低血往回跑是正道，不能连示范一起丢掉。
  assert.equal(validateSample({ snap: { ...baseSnap, life: 10 }, human: { kind: 'mine' } }).reason, 'below-safety-line')
  assert.equal(validateSample({ snap: { ...baseSnap, life: 10 }, human }).ok, true, '低血移动=撤退，不算违规')
  // 水不是禁区：真人演示过"在水袋里往上挖通道再跳出去"，那是唯一出来的办法。
  assert.equal(validateSample({ snap: { ...baseSnap, wet: true }, human: { kind: 'mine' } }).ok, true, '普通水可以挖')
  assert.equal(validateSample({ snap: { ...baseSnap, liquidName: 'lava' }, human: { kind: 'mine' } }).reason, 'liquid-mining')
  assert.equal(validateSample({ snap: { ...baseSnap, velocity: [0, 12] }, human }).reason, 'uncontrolled-fall')
  assert.equal(validateSample({ snap: baseSnap, human: {} }).reason, 'no-human-action')
})

test('actionDelta 只在动作类型不同或时长差 30% 以上时算显著', () => {
  assert.equal(actionDelta({ kind: 'move', moveHoldMs: 600 }, { kind: 'move', moveHoldMs: 700 }).significant, false)
  const timing = actionDelta({ kind: 'move', moveHoldMs: 600 }, { kind: 'move', moveHoldMs: 900 })
  assert.equal(timing.significant, true)
  assert.equal(timing.reason, 'timing')
  assert.equal(timing.diffs[0].key, 'moveHoldMs')
  assert.equal(actionDelta({ kind: 'jump' }, { kind: 'mine' }).reason, 'kind')
})

test('DemoLibrary 记样本、落盘、重载、检索', () => {
  const file = tmp()
  const lib = new DemoLibrary({ file })
  const rec = lib.record({ snap: baseSnap, human: { kind: 'jump', jumpHoldMs: 260 }, agent: { kind: 'jump', jumpHoldMs: 120 } })
  assert.equal(rec.ok, true)
  assert.equal(lib.stats().total, 1)

  const reloaded = new DemoLibrary({ file })
  assert.equal(reloaded.stats().total, 1, '重载应能从 jsonl 读回样本')
  const hit = reloaded.recall(baseSnap)
  assert.equal(hit.length, 1)
  assert.ok(hit[0].sim >= 0.72)
  assert.deepEqual(reloaded.recall({ ...baseSnap, zone: 'underworld', wet: 1 }), [], '不像的场景不应命中')
})

test('DemoLibrary 丢弃样本不会落盘', () => {
  const file = tmp()
  const lib = new DemoLibrary({ file })
  const bad = lib.record({ snap: { ...baseSnap, dead: true }, human: { kind: 'move' } })
  assert.equal(bad.ok, false)
  assert.equal(bad.reason, 'player-dead')
  assert.equal(new DemoLibrary({ file }).stats().total, 0)
  const same = lib.record({ snap: baseSnap, human: { kind: 'move', moveHoldMs: 610 }, agent: { kind: 'move', moveHoldMs: 600 } })
  assert.equal(same.reason, 'not-significant')
})

test('tuneTiming 用相似度加权中位数覆盖基线，并夹在硬边界内', () => {
  const cases = [
    { sim: 0.95, rec: { label: 'a', human: { jumpHoldMs: 240 } } },
    { sim: 0.93, rec: { label: 'd', human: { jumpHoldMs: 250 } } },
    { sim: 0.9, rec: { label: 'b', human: { jumpHoldMs: 260 } } },
    { sim: 0.5, rec: { label: 'c', human: { jumpHoldMs: 900 } } },
  ]
  const out = tuneTiming({ jumpHoldMs: 150 }, cases)
  assert.ok(out.timing.jumpHoldMs >= 240 && out.timing.jumpHoldMs <= 260, `应落在高相似样本之间，实际 ${out.timing.jumpHoldMs}`)
  assert.equal(out.applied[0].param, 'jumpHoldMs')

  const wild = tuneTiming({ jumpHoldMs: 150 }, [{ sim: 0.99, rec: { human: { jumpHoldMs: 5000 } } }], { minSamples: 1 })
  assert.equal(wild.timing.jumpHoldMs, TIMING_BOUNDS.jumpHoldMs[1], '超出硬边界必须被夹回')
  assert.equal(wild.clamped[0].wanted, 5000)
})

test('tuneTiming 少于一两条样本不改（手抖不算规律）', () => {
  const one = tuneTiming({ jumpHoldMs: 150 }, [{ sim: 0.99, rec: { human: { jumpHoldMs: 300 } } }])
  assert.equal(one.timing.jumpHoldMs, 150, '一条样本不足以改参数')
  assert.equal(one.applied.length, 0)
  const two = tuneTiming({ jumpHoldMs: 150 }, [
    { sim: 0.99, rec: { human: { jumpHoldMs: 300 } } },
    { sim: 0.98, rec: { human: { jumpHoldMs: 310 } } },
  ])
  assert.equal(two.timing.jumpHoldMs, 150, '两条同向也还不够')
})

test('tuneTiming 没有够像的样本时保持原值', () => {
  const out = tuneTiming({ moveHoldMs: 600 }, [{ sim: 0.3, rec: { human: { moveHoldMs: 900 } } }])
  assert.equal(out.timing.moveHoldMs, 600)
  assert.equal(out.applied.length, 0)
})