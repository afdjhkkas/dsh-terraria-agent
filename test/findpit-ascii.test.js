// findpit / ascii 类型符号 / walkto 目标校验 —— 三处都是"读地形读错"的修复，锁住形状。
//
// 背景：ascii 以前把所有非空格画成 '#'，树、平台、门与石墙无法区分；
// 据此得结论时连续错三次（砍错树、把树干顶当地面高度、找不到脚边的坑）。
// 这里既锁"符号映射还在"，也锁"找坑判据"和"目标格先验"。

import test from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const root = join(dirname(fileURLToPath(import.meta.url)), '..')
const src = readFileSync(join(root, 'DshTerrariaBridge/Commands.cs'), 'utf8')

function pick(start, end) {
  const a = src.indexOf(start)
  assert.ok(a > 0, `源码里找不到起点：${start}`)
  const b = src.indexOf(end, a + start.length)
  assert.ok(b > a, `源码里找不到终点：${end}`)
  return src.slice(a, b)
}

// ── 判据镜像：与 C# FindPit 同一套规则 ───────────────────────────────
function findPit(grid, { minDepth = 6 } = {}) {
  const H = grid.length
  const W = grid[0].length
  const solid = (x, y) => (x < 0 || y < 0 || x >= W || y >= H ? false : grid[y][x] === 1)
  const hits = []
  for (let x = 0; x < W; x++) {
    for (let y = 0; y < H; y++) {
      if (solid(x, y)) continue
      let d = 0
      while (y + d < H && !solid(x, y + d)) d++
      const bottom = y + d
      if (d >= minDepth && bottom < H && solid(x, bottom)) {
        const mid = y + Math.floor(d / 2)
        let lw = 0, rw = 0
        for (let k = mid; k < bottom; k++) { if (solid(x - 1, k)) lw++; else break }
        for (let k = mid; k < bottom; k++) { if (solid(x + 1, k)) rw++; else break }
        if (Math.max(lw, rw) >= Math.max(2, Math.floor(d / 2))) hits.push({ x, top: y, depth: d, floorY: bottom })
      }
      y += Math.max(d, 1) - 1
    }
  }
  return hits
}

const map = rows => rows.map(r => [...r].map(c => (c === '#' ? 1 : 0)))

test('findpit 判据：8 格深、两侧有壁、有底 → 命中', () => {
  const g = map([
    '###',
    '#.#',
    '#.#',
    '#.#',
    '#.#',
    '#.#',
    '#.#',
    '#.#',
    '###'
  ])
  const hits = findPit(g)
  assert.equal(hits.length, 1)
  assert.deepEqual({ x: hits[0].x, depth: hits[0].depth }, { x: 1, depth: 7 })
})

test('findpit 判据：只有 4 格深 → 不算坑（裸跳能出来）', () => {
  const g = map([
    '###',
    '#.#',
    '#.#',
    '#.#',
    '#.#',
    '###'
  ])
  assert.equal(findPit(g).length, 0)
})

test('findpit 判据：够深但两侧都没壁（开阔地/斜坡）→ 不算坑', () => {
  const g = map([
    '.....',
    '.....',
    '.....',
    '.....',
    '.....',
    '.....',
    '.....',
    '#####'
  ])
  assert.equal(findPit(g).length, 0)
})

test('findpit 判据：够深、有壁，但下面是空的（通到图外）→ 不算坑', () => {
  const g = map([
    '###',
    '#.#',
    '#.#',
    '#.#',
    '#.#',
    '#.#',
    '#.#',
    '#.#'
  ])
  assert.equal(findPit(g).length, 0)
})

test('findpit 判据：minDepth 可调，5 格深的井在 minDepth=5 时命中', () => {
  const g = map([
    '###',
    '#.#',
    '#.#',
    '#.#',
    '#.#',
    '#.#',
    '###'
  ])
  assert.equal(findPit(g, { minDepth: 6 }).length, 0)
  assert.equal(findPit(g, { minDepth: 5 }).length, 1)
})

// ── 源码锁 ────────────────────────────────────────────────────────────
test('findpit 已登记为命令', () => {
  assert.match(src, /case "findpit":\s*return FindPit\(p\);/)
  assert.match(src, /"escape",\s*"event",\s*"findpit",/)
})

test('findpit 的实心判定走 tileSolid，不是"有 tile 就算墙"', () => {
  const body = pick('private static bool SolidAt(int x, int y)', 'private static string FindPit')
  assert.match(body, /return t\.HasTile && Main\.tileSolid\[t\.TileType\];/)
})

test('findpit 只读：不写 tile、不挥物品', () => {
  const body = pick('private static string FindPit(JsonElement p)', 'private static string count_note')
  assert.doesNotMatch(body, /Main\.tile\[[^\]]*\]\.[A-Za-z]+\s*=(?!=)/)
  assert.doesNotMatch(body, /BeginItemUse\(|BeginPlayerMine\(|WorldGen\./)
})

test('ascii 不再把树画成 #：Trees 走 T，非实心走独立符号', () => {
  const body = pick('else if (format == "ascii")', 'data["legend"]')
  assert.match(body, /if \(!Main\.tileSolid\[t\.TileType\]\)/)
  assert.match(body, /TileID\.Trees\) ch = 'T'/)
  assert.match(body, /TileID\.Platforms\) ch = '-'/)
  assert.match(body, /TileID\.ClosedDoor \|\| tt == TileID\.OpenDoor\) ch = '\+'/)
  // legend 必须把 T 树写出来，否则读图的人还是不知道 T 是什么
  assert.match(src, /legend"\] = "[^"]*T 树/)
})

test('walkto 出发前验目标格，并给出最近可站点 / force 跳过', () => {
  const body = pick('private static string WalkTo(JsonElement p)', 'private static string Climb(JsonElement p)')
  assert.match(body, /BridgeSystem\.PathBodyFree\(x, y\)/)
  assert.match(body, /BridgeSystem\.PathFloorAt\(x, y\)/)
  assert.match(body, /bool walkForce = Json\.Bool\(p, "force", false\);/)
  assert.match(body, /最近能站的是/)
  assert.match(body, /nearest = new \{ x = bx, y = by, dist = best \}/)
})