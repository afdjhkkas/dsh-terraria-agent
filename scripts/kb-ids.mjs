#!/usr/bin/env node
/**
 * 从游戏导出的 TSV 生成「物品 / 物块 / NPC ID 对照」附录（引擎常量表）。
 *
 * 为什么这些数字可以进知识库：ItemID / TileID / WallID 是程序里的常量，
 * 同一个 id 在任何世界、任何存档都指向同一个对象（用户 m05433 定的界）。
 *
 * 数据来源：游戏内 `idmap` 命令（mod 的 Commands.IdMap）导出 TSV，不手抄。
 *   1. 进世界后：{"cmd":"idmap","params":{"path":"...\\ids.tsv"}}
 *   2. node scripts/kb-ids.mjs --tsv <那个 tsv>
 *
 * 生成的表只收「用得上的部分」：工具、可放置物块、家具、墙、弹药与材料，
 * 外加物块/墙/NPC 的完整枚举。完整物品属性按需临时导出，不进仓库（避免几十万行垃圾）。
 */

import { readFile, writeFile } from 'node:fs/promises'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')
const OUT = join(ROOT, 'docs', 'kb', '_generated-ids.md')

/** 常用条目：{ 分组标题, 说明, 名称列表（ItemID 枚举名） } */
export const GROUPS = [
  {
    title: '1. 挖掘与建造工具',
    note: '镐挖硬质物块、斧砍植被与木制物、锤改外形与拆墙；数字是工具力（越大能挖的越硬）。',
    kind: 'item',
    names: [
      'CopperPickaxe', 'TinPickaxe', 'IronPickaxe', 'LeadPickaxe', 'SilverPickaxe',
      'TungstenPickaxe', 'GoldPickaxe', 'PlatinumPickaxe', 'MoltenPickaxe',
      'CopperAxe', 'IronAxe', 'SilverAxe', 'GoldAxe',
      'WoodenHammer', 'CopperHammer', 'IronHammer', 'SilverHammer', 'GoldHammer',
      'MoltenHamaxe',
    ],
  },
  {
    title: '2. 可放置物块',
    note: '「放置后物块」就是放下之后该格变成的物块 id —— 判断"放成了没有"要读这个 id，不是读物品 id。',
    kind: 'item',
    names: [
      'DirtBlock', 'StoneBlock', 'ClayBlock', 'SandBlock', 'SnowBlock', 'IceBlock',
      'MudBlock', 'AshBlock', 'SiltBlock', 'SlushBlock', 'Obsidian', 'Glass', 'Wood',
    ],
  },
  {
    title: '3. 家具与制作站',
    note: '多格家具有固定占地与锚点，放下后占地每一格都应是同一个物块 id。',
    kind: 'item',
    names: [
      'WorkBench', 'Furnace', 'IronAnvil', 'LeadAnvil', 'MythrilAnvil',
      'Chest', 'GoldChest', 'TrashCan',
      'WoodenDoor', 'WoodenChair', 'WoodenTable', 'Bed',
    ],
  },
  {
    title: '4. 光源',
    note: '房屋判定要光源；照亮地下避免视野死角。',
    kind: 'item',
    names: ['Torch', 'Campfire', 'Candle', 'ChainLantern'],
  },
  {
    title: '5. 弹药',
    note: '远程武器必须配对应弹药；弹药耗尽即无法攻击。',
    kind: 'item',
    names: ['WoodenArrow', 'FlamingArrow', 'UnholyArrow', 'Shuriken'],
  },
  {
    title: '6. 矿石、锭与基础材料',
    note: '锭是多数装备的合成材料；矿石要先熔炼成锭。',
    kind: 'item',
    names: [
      'CopperOre', 'TinOre', 'IronOre', 'SilverOre', 'GoldOre',
      'CopperBar', 'TinBar', 'IronBar', 'SilverBar', 'GoldBar',
      'Gel', 'Silk', 'Lens', 'LifeCrystal', 'Bottle',
    ],
  },
  {
    title: '7. 背景墙',
    note: '背景墙物品放置后形成背景墙；房屋判定要求墙铺满。',
    kind: 'item',
    names: ['StoneWall', 'WoodWall'],
  },
]

function parseTsv(text) {
  const rows = []
  const lines = text.split(/\r?\n/)
  const header = lines[0].split('\t')
  const idx = Object.fromEntries(header.map((h, i) => [h.trim(), i]))
  for (let i = 1; i < lines.length; i++) {
    const line = lines[i]
    if (!line.trim()) continue
    const c = line.split('\t')
    const num = (key, fallback = 0) => {
      const raw = c[idx[key]]
      if (raw == null || raw === '') return fallback
      const n = Number(raw)
      return Number.isFinite(n) ? n : fallback
    }
    const bool = (key) => num(key, 0) !== 0
    rows.push({
      kind: c[idx.kind],
      id: num('id', -1),
      enum_ : c[idx.enum],
      name: c[idx.name],
      createTile: num('createTile', -1),
      createWall: num('createWall', -1),
      pick: num('pick'),
      axe: num('axe'),
      hammer: num('hammer'),
      damage: num('damage'),
      maxStack: num('maxStack'),
      headSlot: num('headSlot', -1), bodySlot: num('bodySlot', -1), legSlot: num('legSlot', -1),
      waistSlot: num('waistSlot', -1), accessory: bool('accessory'), wingSlot: num('wingSlot', -1),
      mountType: num('mountType', -1), hook: bool('hook'), pet: bool('pet'), lightPet: bool('lightPet'),
      healLife: num('healLife'), healMana: num('healMana'), potion: bool('potion'),
      ammo: num('ammo'), useAmmo: num('useAmmo'), consumable: bool('consumable'), useStyle: num('useStyle'),
      shoot: num('shoot'), buffType: num('buffType'),
      solid: bool('solid'), solidTop: bool('solidTop'), tileMerge: bool('tileMerge'),
      townNPC: bool('townNPC'), boss: bool('boss'), lifeMax: num('lifeMax'), aiStyle: num('aiStyle'),
      catchItem: num('catchItem'), critter: bool('critter'),
    })
  }
  return rows
}

function cell(v) {
  return String(v).replace(/\|/g, '\\|')
}

function toolPower(r) {
  const parts = []
  if (r.pick > 0) parts.push(`镐 ${r.pick}`)
  if (r.axe > 0) parts.push(`斧 ${r.axe}`)
  if (r.hammer > 0) parts.push(`锤 ${r.hammer}`)
  return parts.join(' · ') || '—'
}

export function buildMarkdown(rows) {
  const byEnum = new Map()
  const byId = new Map()
  for (const r of rows) {
    if (r.kind !== 'item') continue
    if (!byId.has(r.id)) byId.set(r.id, r)
    if (!byEnum.has(r.enum_)) byEnum.set(r.enum_, byId.get(r.id))
  }
  const tiles = rows.filter((r) => r.kind === 'tile').sort((a, b) => a.id - b.id)
  const walls = rows.filter((r) => r.kind === 'wall').sort((a, b) => a.id - b.id)
  const npcs = rows.filter((r) => r.kind === 'npc').sort((a, b) => a.id - b.id)
  const items = [...byId.values()].sort((a, b) => a.id - b.id)
  const tileName = new Map(tiles.map((t) => [t.id, t.enum_]))

  const missing = []
  const out = []
  out.push('# 物品 / 物块 ID 对照（含 NPC 固定属性 · 引擎常量 · 生成物）')
  out.push('')
  out.push('> **不要手改这个文件。** 它由 `node scripts/kb-ids.mjs --tsv <tsv>` 从游戏导出的常量表生成。')
  out.push('>')
  out.push('> 这些数字是程序常量：同一个 id 在任何世界、任何存档都指向同一个对象，')
  out.push('> 所以属于知识库，不属于"本局数据"。相反，本世界的坐标、矿脉位置、')
  out.push('> 某次动作的耗时参数都不在这里。')
  out.push('')
  out.push('重新生成（需要游戏在运行、已进世界）：')
  out.push('')
  out.push('```')
  out.push('# 1) 导出常量表（mod 命令，只写文件，不碰世界）')
  out.push('{"cmd":"idmap","params":{"path":"<临时目录>/ids.tsv"}}')
  out.push('# 2) 生成这份附录')
  out.push('node scripts/kb-ids.mjs --tsv "<临时目录>/ids.tsv"')
  out.push('```')
  out.push('')
  out.push(
    `覆盖：物块 ${tiles.length} 条 · 背景墙 ${walls.length} 条 · 物品 ${items.length} 条 · NPC ${npcs.length} 条。`,
  )
  out.push('')
  out.push('---')
  out.push('')

  for (const g of GROUPS) {
    out.push(`## ${g.title}`)
    out.push('')
    out.push(`> ${g.note}`)
    out.push('')
    if (g.kind === 'item') {
      out.push('| 名称 | 物品 ID | 放置后物块 ID | 工具力 / 伤害 |')
      out.push('|---|---|---|---|')
      for (const nm of g.names) {
        const r = byEnum.get(nm)
        if (!r) {
          missing.push(nm)
          continue
        }
        const placed = r.createTile >= 0 ? `${r.createTile} (${tileName.get(r.createTile) ?? '?'})` : '—'
        const power = toolPower(r) === '—' && r.damage > 0 ? `伤害 ${r.damage}` : toolPower(r)
        out.push(`| \`${nm}\` ${cell(r.name)} | ${r.id} | ${placed} | ${power} |`)
      }
    }
    out.push('')
  }

  const tools = items.filter((r) => r.pick > 0 || r.axe > 0 || r.hammer > 0)
  out.push('## 8. 挖掘 / 砍伐 / 锤击类物品全表')
  out.push('')
  out.push(`> 共 ${tools.length} 件。工具力越大，能处理的物块越硬；类别决定能做什么（镐/斧/锤不能互相替代）。`)
  out.push('')
  out.push('| 名称 | 物品 ID | 镐 | 斧 | 锤 |')
  out.push('|---|---|---|---|---|')
  for (const r of tools.sort((a, b) => a.id - b.id)) {
    out.push(`| \`${r.enum_}\` ${cell(r.name)} | ${r.id} | ${r.pick} | ${r.axe} | ${r.hammer} |`)
  }
  out.push('')

  const placeable = items.filter((r) => r.createTile > 0 || r.createWall > 0)
  out.push('## 9. 完整物品 → 物块 / 墙 对照（可放置物品）')
  out.push('')
  out.push(
    `> 共 ${placeable.length} 件。这是"放下之后世界会变成什么"的权威映射，` +
      '判断放置是否成功时按这里的物块 id 回读。',
  )
  out.push('')
  out.push('| 名称 | 物品 ID | 物块 ID | 物块名 | 墙 ID |')
  out.push('|---|---|---|---|---|')
  for (const r of placeable.sort((a, b) => a.id - b.id)) {
    const t = r.createTile > 0 ? r.createTile : '—'
    const tn = r.createTile > 0 ? tileName.get(r.createTile) ?? '?' : '—'
    out.push(`| \`${r.enum_}\` ${cell(r.name)} | ${r.id} | ${t} | ${tn} | ${r.createWall > 0 ? r.createWall : '—'} |`)
  }
  out.push('')

  out.push('## 10. 物块枚举全表')
  out.push('')
  out.push(`> 共 ${tiles.length} 条，按 id 升序。左是 id，右是程序里的枚举名。`)
  out.push('')
  for (const t of tiles) out.push(`- ${t.id} \`${t.enum_}\``)
  out.push('')

  out.push('## 11. 背景墙枚举全表')
  out.push('')
  out.push(`> 共 ${walls.length} 条。`)
  out.push('')
  for (const w of walls) out.push(`- ${w.id} \`${w.enum_}\``)
  out.push('')
  out.push('## 12. 完整物品表（全量）')
  out.push('')
  out.push(
    `> 共 ${items.length} 条，按物品 id 升序。\`放置物块\` 非空表示放下后该格会变成对应物块 id。` +
      '这张表就是"游戏里一共有哪些 item id"的权威答案，离线可查，不必进游戏试。',
  )
  out.push('')
  out.push('| 物品 ID | 枚举名 | 中文名 | 放置物块 | 墙 | 镐/斧/锤 | 伤害 |')
  out.push('|---|---|---|---|---|---|---|')
  for (const r of items) {
    const t = r.createTile > 0 ? r.createTile : '—'
    const w = r.createWall > 0 ? r.createWall : '—'
    const tp = [r.pick, r.axe, r.hammer].map((v) => (v > 0 ? v : '·')).join('/')
    out.push(
      `| ${r.id} | \`${r.enum_}\` | ${cell(r.name)} | ${t} | ${w} | ${tp} | ${r.damage > 0 ? r.damage : '—'} |`,
    )
  }
  out.push('')
  out.push('## 13. NPC 固定属性表')
  out.push('')
  out.push('> 这些字段用于静态可达性判断；附近 NPC 的实时位置、血量和是否激活仍以 `npc` 命令回读为准。')
  out.push('')
  out.push('| NPC ID | 枚举名 | 名称 | 城镇 NPC | Boss | 最大生命 | AI 风格 | 可捕捉物品 | 小动物 |')
  out.push('|---|---|---|---:|---:|---:|---:|---:|---:|')
  for (const r of npcs) {
    out.push(`| ${r.id} | \`${r.enum_}\` | ${cell(r.name)} | ${r.townNPC ? '是' : '否'} | ${r.boss ? '是' : '否'} | ${r.lifeMax || '—'} | ${r.aiStyle} | ${r.catchItem > 0 ? r.catchItem : '—'} | ${r.critter ? '是' : '否'} |`)
  }
  out.push('')
  out.push('## 14. 属性字段判定速查')
  out.push('')
  out.push('| 对象 | 关键字段 | 判定 |')
  out.push('|---|---|---|')
  out.push('| 物品 | `headSlot/bodySlot/legSlot` | 对应值 ≥ 0 才是护甲部位 |')
  out.push('| 物品 | `accessory`, `wingSlot` | 饰品或翅膀装备分类 |')
  out.push('| 物品 | `mountType`, `hook`, `pet`, `lightPet` | 坐骑、抓钩、宠物、照明宠物能力 |')
  out.push('| 物品 | `healLife/healMana/potion` | 治疗、回魔、药水分支 |')
  out.push('| 物品 | `ammo/useAmmo` | 弹药自身类别与武器消耗类别匹配 |')
  out.push('| 物品 | `consumable/useStyle` | 是否消耗以及使用分支；不能只看名称 |')
  out.push('| 物块 | `solid/solidTop/tileMerge` | 实心、平台型、合并规则 |')
  out.push('| NPC | `townNPC/boss/lifeMax/aiStyle/catchItem/critter` | 城镇、Boss、生命、AI、捕捉和小动物判定 |')
  out.push('')
  return { markdown: out.join('\n'), missing, tiles: tiles.length, walls: walls.length, items: items.length, npcs: npcs.length }
}

async function main() {
  const argv = process.argv.slice(2)
  const i = argv.indexOf('--tsv')
  const tsvPath = i >= 0 ? argv[i + 1] : process.env.KB_IDS_TSV
  if (!tsvPath) {
    console.error('用法：node scripts/kb-ids.mjs --tsv <ids.tsv>（先跑游戏内 idmap 命令导出）')
    process.exit(2)
  }
  const text = await readFile(resolve(tsvPath), 'utf8')
  const rows = parseTsv(text)
  const { markdown, missing, tiles, walls, items, npcs } = buildMarkdown(rows)
  if (missing.length) {
    console.error('这些名字在导出的常量表里找不到，先确认枚举名：' + missing.join(', '))
    process.exit(1)
  }
  await writeFile(OUT, markdown, 'utf8')
  console.log(`ok: ${OUT}（物块 ${tiles} / 墙 ${walls} / 物品 ${items} / NPC ${npcs}）`)
}

if (process.argv[1] && process.argv[1].endsWith('kb-ids.mjs')) await main()
