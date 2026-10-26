// 感知层模型的离线镜像：与 DshTerrariaBridge/BridgeSystem.cs 的 SenseWorld() 逐字段对应。
//
// 为什么要有这一份：检测器的阈值（落差上限、扫描宽度、墙高上限）是最容易被"凭感觉微调"的地方。
// 规范要求改检测器必须先跑固定用例、前后对比，所以这些用例必须能离线跑（不依赖游戏进程）。
// 铁律：这里只镜像"读地形得信号"这一步，**不做任何决策**，字段名与 C# 的 ActorSense 一致。
// 每次改 C# 的 SenseWorld()，必须同步改这里并重跑 test/sense.test.js。

/* 瓦片类型常量，取自 Terraria TileID（与 idmap 一致） */
export const T = {
  AIR: -1,
  DIRT: 0,
  STONE: 1,
  GRASS: 2,
  IRON: 6,
  CLOSED_DOOR: 10,
  OPEN_DOOR: 11,
  PLATFORM: 19,
  WOOD: 30,
  PLATFORM_ALT: 19,
};

/* 底座标：只描述瓦片原始数据，不掺杂业务判断 */
export function isSolidTile(type) {
  if (type === undefined || type === null || type < 0) return false;
  return !isPassThrough(type);
}

/* 业务判定：门、平台可以穿过去，不算阻挡 */
export function isBlockingSolid(type) {
  if (type === undefined || type === null || type < 0) return false;
  if (type === T.CLOSED_DOOR || type === T.OPEN_DOOR) return false;
  if (type === T.PLATFORM) return false;
  return isSolidTile(type);
}

/* 可站立：实心或平台（平台能踩） */
export function isStandable(type) {
  if (type === undefined || type === null || type < 0) return false;
  if (type === T.PLATFORM) return true;
  return isBlockingSolid(type);
}

function isPassThrough(type) {
  return type === T.CLOSED_DOOR || type === T.OPEN_DOOR || type === T.PLATFORM;
}

/* 自己造的东西：建材、门、平台、家具。命中后只允许绕行，不允许挖 */
const BUILD_TILES = new Set([4, 10, 11, 15, 16, 17, 18, 19, 21, 30]);
export function isBuildTile(type) {
  return BUILD_TILES.has(type);
}

/**
 * 世界访问器：tiles 用 Map<"x,y", type> 表示，未登记的格按空气处理。
 */
export function makeWorld(tiles) {
  const m = new Map();
  for (const [k, v] of Object.entries(tiles)) m.set(k, v);
  const at = (x, y) => (m.has(`${x},${y}`) ? m.get(`${x},${y}`) : T.AIR);
  return { at, tiles: m };
}

/* 前方 w 列每列第一块可站立面的 y；找不到返回 null */
export function scanGround(world, px, py, sign, w) {
  const out = [];
  for (let i = 1; i <= w; i++) {
    let found = null;
    for (let k = -3; k <= 16; k++) {
      if (isStandable(world.at(px + sign * i, py + k))) { found = py + k; break; }
    }
    out.push(found);
  }
  return out;
}

/**
 * 感知：只读地形与玩家状态，输出 ActorSense 同名字段，不做决策。
 * @param {object} o
 * @param {ReturnType<typeof makeWorld>} o.world
 * @param {{px:number,py:number,onGround:boolean,dead?:boolean,lava?:boolean,drowning?:boolean}} o.player
 * @param {{x:number,y:number,tol?:number}} o.target
 */
export function sense({ world, player, target }) {
  const { px, py } = player;
  const tol = target.tol ?? 1;
  let dir = Math.sign(target.x - px);
  if (dir === 0) dir = 1;

  const s = {
    px, py, dir,
    dead: !!player.dead,
    lava: !!player.lava,
    drowning: !!player.drowning,
    arrived: Math.abs(px - target.x) <= tol && Math.abs(py - target.y) <= tol,
    blockingSelf: isBlockingSolid(world.at(px, py)) && isBlockingSolid(world.at(px, py - 1)),
    onGround: !!player.onGround,
    stalled: false,           // 由运行时计数提供，模型层默认 false
    cliffAhead: false,
    doorAhead: false,
    buildAhead: false,
    gapAhead: false,
    wallH: 0,
  };

  // 前方地面扫描（宽 8 列，向下 16 格）
  const scan = scanGround(world, px, py, dir, 8);
  const first = scan[0];
  const depth = first === null ? 99 : Math.max(0, first - py);
  // 悬崖 vs 坑：只看第 1 列会把“一格宽的坑”误判成悬崖（m17206 用例抓到的第一个真缺陷）。
  // 判定改成：前 3 列全都够不着地，才算悬崖；第 1 列空但后面有地，是坑。
  const near = scan.slice(0, 3);
  s.cliffAhead = near.every((v) => v === null || v - py > 14);

  // 门：前方 1~3 格内（含身体高度的 3 行）
  for (let k = 0; k <= 2; k++) {
    const t = world.at(px + dir, py + k);
    if (t === T.CLOSED_DOOR || t === T.OPEN_DOOR) { s.doorAhead = true; break; }
  }

  // 墙高：从脚下往上数连续阻挡格
  let h = 0;
  for (let k = 0; k < 12; k++) {
    if (isBlockingSolid(world.at(px + dir, py - k))) h = k + 1; else break;
  }
  s.wallH = h;
  if (h > 0) s.buildAhead = isBuildTile(world.at(px + dir, py - (h - 1)));

  // 坑：必须站在地上，且眼前这一格脚下是空的，对岸 2~6 格深
  const landAfter = scan.slice(1, 4).find((v) => v !== null);
  s.gapAhead =
    s.onGround && !s.cliffAhead && !s.doorAhead && h === 0 &&
    !isStandable(world.at(px + dir, py)) && !isStandable(world.at(px + dir, py + 1)) &&
    landAfter !== undefined && Math.abs(landAfter - py) >= 2 && Math.abs(landAfter - py) <= 6;

  return s;
}