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
