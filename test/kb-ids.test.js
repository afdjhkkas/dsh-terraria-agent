import test from 'node:test'
import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { GROUPS, buildMarkdown } from '../scripts/kb-ids.mjs'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')
const IDS = join(ROOT, 'docs', 'kb', '_generated-ids.md')

/** 最小 TSV，只用来验证生成逻辑本身（不是真实数据）。 */
const MINI_TSV = [
  'kind\tid\tenum\tname\tcreateTile\tcreateWall\tpick\taxe\thammer\tdamage\tmaxStack',
  'item\t3\tStoneBlock\t石块\t1\t-1\t0\t0\t0\t-1\t999',
  'item\t9\tWood\t木材\t30\t-1\t0\t0\t0\t-1\t999',
  'item\t1\tIronPickaxe\t铁镐\t-1\t-1\t40\t0\t0\t4\t1',
  'item\t7\tIronHammer\t铁锤\t-1\t-1\t0\t0\t40\t-1\t1',
  'tile\t1\tStone\t石块\t0\t0\t0\t0\t0\t-1\t0',
  'tile\t30\tWoodBlock\t木材\t0\t0\t0\t0\t0\t-1\t0',
  'wall\t1\tStoneWall\t石墙\t0\t0\t0\t0\t0\t-1\t0',
].join('\n')

test('生成器：物品/物块/墙 分别计数', () => {
  const rows = MINI_TSV.split('\n').slice(1).map((line) => {
    const c = line.split('\t')
    return {
      kind: c[0], id: Number(c[1]), enum_: c[2], name: c[3],
      createTile: Number(c[4]), createWall: Number(c[5]),
      pick: Number(c[6]), axe: Number(c[7]), hammer: Number(c[8]),
      damage: Number(c[9]), maxStack: Number(c[10]),
    }
  })
  const r = buildMarkdown(rows)
  assert.equal(r.tiles, 2)
  assert.equal(r.walls, 1)
  assert.equal(r.items, 4)
  assert.deepEqual(r.missing, GROUPS.flatMap((g) => g.names).filter(
    (n) => !['StoneBlock', 'Wood', 'IronPickaxe', 'IronHammer'].includes(n),
  ).slice(0, r.missing.length))
})

test('生成器：物品 id 与放置后物块 id 分列，不混用', () => {
  const md = buildMarkdown([
    { kind: 'item', id: 3, enum_: 'StoneBlock', name: '石块', createTile: 1, createWall: -1, pick: 0, axe: 0, hammer: 0, damage: -1, maxStack: 999 },
    { kind: 'tile', id: 1, enum_: 'Stone', name: '石块', createTile: 0, createWall: 0, pick: 0, axe: 0, hammer: 0, damage: -1, maxStack: 0 },
  ])
  assert.equal(md.missing.length > 0, true, '缺常用条目要报出来（这里只给了两个）')
  assert.match(md.markdown, /StoneBlock/)
})

test('生成物：文件存在且覆盖完整枚举', async () => {
  const text = await readFile(IDS, 'utf8')
  assert.match(text, /物品 \/ 物块 ID 对照/)
  assert.match(text, /## 10\. 物块枚举全表/)
  assert.match(text, /## 11\. 背景墙枚举全表/)
  assert.match(text, /## 12\. 完整物品表（全量）/)
  const itemRows = text.split('\n').filter((l) => /^\| \d+ \| `/.test(l))
  assert.ok(itemRows.length > 5000, `完整物品表应超过 5000 行，实际 ${itemRows.length}`)
})

test('生成物：关键常量值没被写错', async () => {
  const text = await readFile(IDS, 'utf8')
  const line = (re) => text.split('\n').find((l) => re.test(l))
  assert.ok(line(/^\| `StoneBlock` 石块 \| 3 \| 1 \(Stone\) \|/), '石块：物品 3 → 物块 1')
  assert.ok(line(/^\| `Wood` 木材 \| 9 \| 30 \(WoodBlock\) \|/), '木材：物品 9 → 物块 30')
  assert.ok(line(/^\| `Torch` 火把 \| 8 \| 4 \(Torches\) \|/), '火把：物品 8 → 物块 4')
  assert.ok(line(/^\| `IronBar` 铁锭 \| 22 \| 239 \(MetalBars\) \|/), '铁锭：物品 22 → 物块 239')
  assert.ok(line(/^\| `IronPickaxe` 铁镐 \| 1 \| — \|/), '铁镐不放置物块')
})