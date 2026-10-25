import { test } from 'node:test'
import assert from 'node:assert/strict'
import { auditHousing, obsFromDiag, HOUSING_CHECKS, planClearance, classifyObstacle, auditClearance, BUILD_FLOW } from '../src/house-audit.js'

const good = {
  enclosed: true, spaceCells: 44, interiorCells: 32, houseWallCells: 32,
  naturalWallCells: 0, doors: 1, light: 1, comfort: 1,
}

test('六条判定齐全，代码不重复', () => {
  assert.equal(HOUSING_CHECKS.length, 6)
  assert.equal(new Set(HOUSING_CHECKS.map(c => c.code)).size, 6)
})

test('全达标才算通过', () => {
  const r = auditHousing(good)
  assert.equal(r.ok, true)
  assert.deepEqual(r.defects, [])
})

test('自然墙不算：铺满但全是自然墙 → houseWalls 缺陷', () => {
  const r = auditHousing({ ...good, houseWallCells: 0, naturalWallCells: 32 })
  assert.equal(r.ok, false)
  assert.equal(r.defects[0].code, 'houseWalls')
  assert.match(r.defects[0].detail, /自然墙/)
})

test('不封闭/没门/没光/没桌椅/空间不对，各报一条且都有修法', () => {
  const r = auditHousing({ enclosed: false, spaceCells: 10, interiorCells: 32, houseWallCells: 4, doors: 0, light: 0, comfort: 0 })
  const codes = r.defects.map(d => d.code).sort()
  assert.deepEqual(codes, ['comfort', 'door', 'enclosed', 'houseWalls', 'light', 'space'])
  for (const d of r.defects) assert.ok(d.fix && d.fix.length > 5)
})

test('obsFromDiag 区分人工墙与自然墙', () => {
  const walls = [
    { wall: 4, wallHouse: true }, { wall: 4, wallHouse: true },
    { wall: 2, wallHouse: false }, { wall: 16, wallHouse: false },
  ]
  const obs = obsFromDiag({ enclosed: true, tiles: 40, doors: 1, light: 1, comfort: 1 }, walls)
  assert.equal(obs.houseWallCells, 2)
  assert.equal(obs.naturalWallCells, 2)
  const r = auditHousing(obs)
  assert.equal(r.ok, false)
  assert.equal(r.defects.some(d => d.code === 'houseWalls'), true)
})

test('清场范围含向外 1 格缓冲区', () => {
  const c = planClearance({ x0: 0, x1: 7, y0: 0, y1: 3 }, { buffer: 1 })
  assert.equal(c.inner.length, 8 * 4)
  assert.equal(c.cells.length, 10 * 6)
  assert.equal(c.buffer.length, 60 - 32)
  assert.ok(c.buffer.some(p => p.x === -1) && c.buffer.some(p => p.y === 4))
})

test('树干/植被/实心方块都算障碍，空气不算', () => {
  assert.equal(classifyObstacle({ type: 5, solid: false, name: 'Trees' }), 'tree')
  assert.equal(classifyObstacle({ type: 3, solid: false, name: 'Grass' }), 'plant')
  assert.equal(classifyObstacle({ type: 1, solid: true }), 'solid')
  assert.equal(classifyObstacle({ type: -1 }), null)
  assert.equal(classifyObstacle(null), null)
})

test('范围内有树 → 清场不通过，且单独统计树木数量', () => {
  const obs = [
    { x: 2, y: 1, type: 5, solid: false }, { x: 3, y: 1, type: 5, solid: false },
    { x: 4, y: 1, type: -1 },
  ]
  const r = auditClearance(obs)
  assert.equal(r.ok, false)
  assert.equal(r.treeCount, 2)
  assert.match(r.verdict, /树木 2 格/)
})

test('缓冲区里的树也算障碍（不许贴着墙）', () => {
  const obs = [{ x: 8, y: 1, type: 5, solid: false }] // 8 在 x1=7 的缓冲区里
  const r = auditClearance(obs, { rect: { x0: 0, x1: 7, y0: 0, y1: 3 }, buffer: 1 })
  assert.equal(r.ok, false)
  const clean = [{ x: 9, y: 1, type: 5, solid: false }] // 超出缓冲区 → 不算
  assert.equal(auditClearance(clean, { rect: { x0: 0, x1: 7, y0: 0, y1: 3 }, buffer: 1 }).ok, true)
})

test('建房流程固定六步，清场在搭框架之前', () => {
  assert.deepEqual(BUILD_FLOW.map(s => s.code), ['survey', 'clear', 'frame', 'walls', 'furniture', 'verify'])
})
