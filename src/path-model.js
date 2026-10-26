// 寻路模型的离线镜像：与 DshTerrariaBridge/BridgeSystem.cs 的 ComputePath() 逐项对应。
//
// 为什么单独一份：寻路的两个核心决策（能不能走、能不能跳过去）最容易凭感觉改。
// 规范要求固定用例 + 改前后对比，所以必须能离线跑。字段与 C# 一致。
//
// 关键设计（垂直层）：
//   1) 走边：水平相邻 1 格，落点脚下 1~2 格内有可站立面 → 代价 1
//   2) 跨边：水平相邻但高差 -1（自动跨台阶）→ 代价 1.2
//   3) 跳边：水平 1~3 格、垂直上升 1~6 格（满跳 6.1），起跳上方净空、落点可站 → 代价 4
//   4) 落边：水平 1~4 格、垂直下降 1~14 格（自然掉落不受伤）→ 代价 2
// 禁止：挖任何"自己造的"方块；液体格不可通行。

import { isStandable, isBlockingSolid, isBuildTile } from './sense-model.js';

export const JUMP_UP_MAX = 6;
/** 跳跃/下落的水平跨距上限（格）：3 格宽的坑需要跨 4 格 */
export const JUMP_HORIZ_MAX = 4;      // 满跳约 6.1 格
export const DROP_DOWN_MAX = 14;   // 超过这个高度按摔伤处理，不当通路
export const JUMP_COST = 4;
export const DROP_COST = 2;
export const STEP_COST = 1;
export const CROSS_COST = 1.2;
export const AIR_COST = 20;        // 悬空但可通行（代价高，只有别无选择才用）

/** 脚能踩住：**紧贴**脚下一格（y+1）必须是可站立面。
 *  早先写成 k=1..2 会在身体中段就判"有地"，于是目标点被算到高 1 格的位置（用例抓到）。 */
function floorAt(world, x, y) {
  return isStandable(world.at(x, y + 1));
}

/** 起跳通道：从 y 往上到 ny 的竖直空间必须通 */
function clearColumn(world, x, y, ny) {
  for (let yy = y - 1; yy >= ny; yy--) if (isBlockingSolid(world.at(x, yy))) return false;
  return true;
}

/** 横跨通道：x0 到 x1（不含两端）之间、从 yTop 到 yBot 的格子里不能有阻挡 */
function horizontalClear(world, x0, x1, yTop, yBot) {
  const dir = Math.sign(x1 - x0);
  for (let x = x0 + dir; x !== x1; x += dir) {
    for (let y = yTop - 1; y <= yBot; y++) {
      if (isBlockingSolid(world.at(x, y))) return false;
    }
  }
  return true;
}

/** 目标格本身可以容纳人（身体 2 格：ny 与 ny-1） */
function bodyFree(world, x, y) {
  return !isBlockingSolid(world.at(x, y)) && !isBlockingSolid(world.at(x, y - 1));
}

/**
 * 邻居生成：走/跨/跳/落四种边，返回 [{x,y,cost,kind}]
 * 所有"跳/落"都要求落点身体两格为空且脚下有地。
 */
export function neighbors(world, x, y) {
  const out = [];
  for (const dx of [-1, 1]) {
    const nx = x + dx;
    // 1) 同高直走
    if (bodyFree(world, nx, y) && floorAt(world, nx, y)) {
      out.push({ x: nx, y, cost: STEP_COST, kind: 'walk' });
    }
    // 2) 上 1 格自动跨（原版能自己上台阶）
    if (bodyFree(world, nx, y - 1) && floorAt(world, nx, y - 1)) {
      out.push({ x: nx, y: y - 1, cost: CROSS_COST, kind: 'cross' });
    }
    // 3) 跳上：水平 1~3 格、上升 1~6（这是"上屋顶"的关键边）。
    //    水平必须允许超过 1 格，否则"1 格宽的坑"跳不过去：落点就是坑本身，
    //    它永远没有地面，真正的落脚点在对岸 2 格之外（用例 test/path.test.js 抓到的缺陷）。
    //    JUMP_HORIZ_MAX 是 4 而不是 3：3 格宽的坑要求"从坑左沿跳到右沿"，水平跨距正好 4 格，
    //    上限定死 3 时这类坑在图上就是死路（用例 gap3 抓到）。
    for (let step = 1; step <= JUMP_HORIZ_MAX; step++) {
      const jx = x + dx * step;
      // up 从 0 开始：横跨同高度的坑也是"跳"，落点在对岸同一高度上（用例抓到过漏边）。
      for (let up = 0; up <= JUMP_UP_MAX; up++) {
        // 同高横跳至少要跨 2 格：跨 1 格同高本来就能走过去，
        // 允许它等于给"1 格宽的墙"开了穿墙后门（天花板用例立刻变红）。
        if (up === 0 && step === 1) continue;
        const ny = y - up;
        if (!clearColumn(world, x, y, ny)) break;
        if (!bodyFree(world, jx, ny)) continue;
        if (!floorAt(world, jx, ny)) continue;
        // 中间格必须过得去：只查竖直列会允许“3 格横跳穿过一堵墙”的假路。
        if (!horizontalClear(world, x, jx, Math.min(y, ny), Math.max(y, ny))) continue;
        out.push({ x: jx, y: ny, cost: JUMP_COST + step + Math.abs(up) * 0.3, kind: 'jump' });
        break; // 同一列只留最低的那一跳，避免邻居爆炸
      }
    }
    // 4) 落下去：水平 1~JUMP_HORIZ_MAX、下降 1~14
    for (let step = 1; step <= JUMP_HORIZ_MAX; step++) {
      const jx = x + dx * step;
      for (let down = 1; down <= DROP_DOWN_MAX; down++) {
        const ny = y + down;
        if (!bodyFree(world, jx, ny)) break;
        if (!floorAt(world, jx, ny)) continue;
        out.push({ x: jx, y: ny, cost: DROP_COST + step * 0.5 + down * 0.2, kind: 'drop' });
        break;
      }
    }
  }
  return out;
}

/** 该格是否"可站立的地面层"（用于判断目标是否可达） */
export function isReachableGround(world, x, y) {
  return bodyFree(world, x, y) && floorAt(world, x, y);
}

/**
 * A*：四方向邻居 + 垂直层边。
 * @returns {{ok:boolean, path:{x:number,y:number}[], why?:string, expanded:number}}
 */
export function astar({ world, sx, sy, tx, ty, maxNodes = 20000 }) {
  if (!bodyFree(world, sx, sy)) return { ok: false, path: [], why: '起点身体被占', expanded: 0 };
  if (!isReachableGround(world, tx, ty)) {
    // 目标点没有地面：向下找最近的落脚点（和 C# 的 FloorWithin 行为一致）
    let found = null;
    for (let d = 0; d <= 3; d++) if (isReachableGround(world, tx, ty + d)) { found = ty + d; break; }
    if (found === null) return { ok: false, path: [], why: '目标点脚下没有可站立地面', expanded: 0 };
    ty = found;
  }

  const key = (x, y) => `${x},${y}`;
  const h = (x, y) => Math.abs(x - tx) + Math.abs(y - ty) * 1.5;

  const open = [{ x: sx, y: sy, g: 0, f: h(sx, sy) }];
  const best = new Map([[key(sx, sy), 0]]);
  const prev = new Map();
  const seen = new Set();
  let expanded = 0;

  while (open.length) {
    open.sort((a, b) => a.f - b.f);
    const cur = open.shift();
    const ck = key(cur.x, cur.y);
    if (seen.has(ck)) continue;
    seen.add(ck);
    expanded++;
    if (expanded > maxNodes) return { ok: false, path: [], why: '搜索节点超限', expanded };

    if (cur.x === tx && cur.y === ty) {
      const path = [];
      let k = ck;
      while (k) {
        const [px, py] = k.split(',').map(Number);
        path.unshift({ x: px, y: py });
        k = prev.get(k);
      }
      return { ok: true, path, expanded };
    }

    for (const n of neighbors(world, cur.x, cur.y)) {
      const nk = key(n.x, n.y);
      if (seen.has(nk)) continue;
      const g = cur.g + n.cost;
      if (g >= (best.get(nk) ?? Infinity)) continue;
      best.set(nk, g);
      prev.set(nk, ck);
      open.push({ x: n.x, y: n.y, g, f: g + h(n.x, n.y) });
    }
  }
  return { ok: false, path: [], why: '没有可达路径', expanded };
}

/** 路径里是否用过垂直层（跳/落），用于日志和用例断言 */
export function pathKinds(world, path) {
  const kinds = [];
  for (let i = 1; i < path.length; i++) {
    const dy = path[i].y - path[i - 1].y;
    const dx = path[i].x - path[i - 1].x;
    if (Math.abs(dx) === 1 && dy === 0) kinds.push('walk');
    else if (Math.abs(dx) === 1 && dy === -1) kinds.push('cross');
    else if (dy <= -1 || (dy <= 0 && Math.abs(dx) >= 2)) kinds.push('jump');
    else if (dy >= 1) kinds.push('drop');
  }
  return kinds;
}

export { isBuildTile };