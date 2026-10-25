/**
 * 房屋入住判定清单（把"觉得盖好了"变成"逐条查过的六条硬判定"）。
 *
 * 为什么需要它：`place`/`wall` 返回 ok 只代表"挥了"，不代表"这间房能被 NPC 入住"。
 * 尤其背景墙——地图自带的泥土墙/岩石墙 `Main.wallHouse` 是 false，
 * 铺满了也一堵都不算（用户指出的坑）。
 *
 * 权威判据：`tile.wallHouse`（原版 `Main.wallHouse[wallType]`），不是 `wall != 0`。
 */

export const HOUSING_CHECKS = [
  { code: 'enclosed', title: '封闭实体边框', fix: '找出缺口那几格，砌实心方块补上（地板/侧墙/天花板都要连续）' },
  { code: 'houseWalls', title: '人工背墙铺满', fix: '用可住人的墙（wallHouse=true）逐格铺，自然墙不算' },
  { code: 'door', title: '有门', fix: '拆掉侧墙底部三格，装门（门要三格高、下方有支撑）' },
  { code: 'light', title: '有光源', fix: '屋内放火把（靠墙或地面，悬空放不下）' },
  { code: 'comfort', title: '桌椅等舒适物品', fix: '放一张桌子/工作台 + 一把椅子，光有椅子不算' },
  { code: 'space', title: '空间够且不过大', fix: '内部至少 30 格有效空间，且不要打通成大厅（超 7500 格判无效）' },
]

function cellCount(list) {
  return Array.isArray(list) ? list.length : Number(list || 0)
}

/**
 * @param {object} obs 观测值（来自 housediag / tile 回读）
 * @param {boolean} obs.enclosed
 * @param {number}  obs.spaceCells        封闭区域格数
 * @param {number}  obs.interiorCells     需要铺墙的内部格数
 * @param {number}  obs.houseWallCells    wallHouse=true 的格数
 * @param {number}  obs.doors
 * @param {number}  obs.light
 * @param {number}  obs.comfort
 */
export function auditHousing(obs = {}) {
  const space = Number(obs.spaceCells || 0)
  const interior = Number(obs.interiorCells || 0)
  const houseWalls = Number(obs.houseWallCells || 0)
  const naturalWalls = Number(obs.naturalWallCells || 0)
  const defects = []
  const push = (code, detail) => {
    const c = HOUSING_CHECKS.find(x => x.code === code)
    defects.push({ code, title: c.title, detail, fix: c.fix })
  }

  if (!obs.enclosed) push('enclosed', '洪水填充跑到区域外，说明有缺口')
  if (interior > 0 && houseWalls < interior) {
    push('houseWalls', `人工背墙 ${houseWalls}/${interior} 格${naturalWalls > 0 ? `（其中 ${naturalWalls} 格是自然墙，不算）` : ''}`)
  }
  if (!(Number(obs.doors) > 0)) push('door', '没有门')
  if (!(Number(obs.light) > 0)) push('light', '没有光源')
  if (!(Number(obs.comfort) > 0)) push('comfort', '没有桌椅/工作台')
  if (space > 0 && (space < 30 || space > 7500)) push('space', `封闭区域 ${space} 格，不在 30~7500 之间`)

  return {
    ok: defects.length === 0,
    checked: HOUSING_CHECKS.map(c => c.code),
    defects,
    verdict: defects.length === 0 ? '可以试着让 NPC 入住' : '先按 fix 修，别宣布完成',
  }
}

/** 从 housediag 的返回里抽观测值（字段名对齐 mod 的 HouseDiag）。 */
export function obsFromDiag(diag = {}, walls = []) {
  const counted = Array.isArray(walls) ? walls : []
  const natural = counted.filter(w => w && w.wall > 0 && w.wallHouse === false).length
  const house = counted.filter(w => w && w.wallHouse === true).length
  return {
    enclosed: !!diag.enclosed,
    spaceCells: Number(diag.tiles || 0),
    interiorCells: counted.length || cellCount(diag.walled),
    houseWallCells: counted.length ? house : Number(diag.walled || 0),
    naturalWallCells: natural,
    doors: Number(diag.doors || 0),
    light: Number(diag.light || 0),
    comfort: Number(diag.comfort || 0),
  }
}

/* ── 范围清场（建房第一步，先于任何砌墙动作） ────────────────────────── */

/** 会挡住房屋判定的实体障碍分类。树干（Trees）虽然 solid=false，但它占格，必须清。 */
export const OBSTACLE_KINDS = {
  tree: '树木（树干/树冠）',
  plant: '植被（杂草/花/藤蔓）',
  solid: '实心方块',
}

const TREE_TILES = new Set([5, 583, 584, 585, 586, 587, 588, 589, 590, 591, 592, 593])
const PLANT_TILES = new Set([3, 24, 32, 61, 62, 69, 73, 74, 82, 83, 84, 85, 110, 113, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260])

export function classifyObstacle(tile) {
  if (!tile) return null
  const type = Number(tile.type)
  if (!(type > 0)) return null
  if (TREE_TILES.has(type)) return 'tree'
  if (PLANT_TILES.has(type)) return 'plant'
  return tile.solid === false && /Trees|Plant|Herb|Vine|Flower|Weed/i.test(tile.name || '') ? 'plant' : 'solid'
}

/** 划定范围（含向外 buffer 格的缓冲区）里所有需要清的格子。 */
export function planClearance({ x0, x1, y0, y1 }, { buffer = 1 } = {}) {
  const inner = []
  const ring = []
  for (let x = x0 - buffer; x <= x1 + buffer; x++) {
    for (let y = y0 - buffer; y <= y1 + buffer; y++) {
      const inInner = x >= x0 && x <= x1 && y >= y0 && y <= y1
      ;(inInner ? inner : ring).push({ x, y })
    }
  }
  return { inner, buffer: ring, cells: [...inner, ...ring], bufferSize: buffer }
}

/**
 * 清场验收：范围内（含缓冲区）不许有树、植被、多余方块。
 * @param {Array<{x:number,y:number,type:number,solid?:boolean,name?:string}>} observations
 */
export function auditClearance(observations = [], { buffer = 1, rect } = {}) {
  let cells = observations
  if (rect) {
    const allowed = new Set(planClearance(rect, { buffer }).cells.map(c => `${c.x},${c.y}`))
    cells = observations.filter(o => allowed.has(`${o.x},${o.y}`))
  }
  const obstacles = []
  for (const o of cells) {
    const kind = classifyObstacle(o)
    if (kind) obstacles.push({ x: o.x, y: o.y, kind, title: OBSTACLE_KINDS[kind], type: o.type })
  }
  const trees = obstacles.filter(o => o.kind === 'tree')
  return {
    ok: obstacles.length === 0,
    obstacleCount: obstacles.length,
    treeCount: trees.length,
    obstacles,
    verdict: obstacles.length === 0
      ? '范围内干净，可以开始搭框架'
      : `先清 ${obstacles.length} 格（其中树木 ${trees.length} 格），清完回读确认 type === -1 再继续`,
  }
}

/** 建房全流程（顺序固定，不许跳步）。 */
export const BUILD_FLOW = [
  { step: 1, code: 'survey', title: '划定建房范围（外扩 1 格缓冲区）' },
  { step: 2, code: 'clear', title: '清理范围内所有实体障碍（树木/植被/石块），回读 type === -1' },
  { step: 3, code: 'frame', title: '搭框架（人先进内部）' },
  { step: 4, code: 'walls', title: '铺合规人工背景墙（wallHouse === true）' },
  { step: 5, code: 'furniture', title: '摆家具：桌椅 + 光源 + 门' },
  { step: 6, code: 'verify', title: 'NPC 入住校验（六条全过才算完成）' },
]
