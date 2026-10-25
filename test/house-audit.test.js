import { test } from 'node:test'
import assert from 'node:assert/strict'
import { auditHousing, obsFromDiag, HOUSING_CHECKS } from '../src/house-audit.js'

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
