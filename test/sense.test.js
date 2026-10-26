// 检测器固定用例集（规范 m17206 第 2 条）：
// 平台 / 门 / 实墙 / 悬崖 / 上坡 / 坑 / 卡方块 / 门格不误判。
//
// 用法纪律：
//  - 改 DshTerrariaBridge 的 SenseWorld() 或本仓 src/sense-model.js 后，必须跑本文件并在提交信息里贴前后差异。
//  - 一次只改一类检测逻辑；改动点与结果写进 docs/navigation.md 的“检测器变更记录”。
//  - 信号字段名（blockingSelf / onGround / stalled / cliffAhead / doorAhead / buildAhead / gapAhead / wallH）不得改名。
import test from 'node:test';
import assert from 'node:assert/strict';
import { T, makeWorld, sense, isBlockingSolid, isStandable, isBuildTile } from '../src/sense-model.js';

/** 地面：在 y=floor 铺一条从 x0 到 x1 的土块 */
function ground(tiles, x0, x1, floor, type = T.DIRT) {
  for (let x = x0; x <= x1; x++) tiles[`${x},${floor}`] = type;
  return tiles;
}

/* ── 1. 平台：站在平台上，前方平台继续 ─────────────────────────── */
test('平台：踩在平台上算 onGround，前方平台不算悬崖', () => {
  const tiles = {};
  for (let x = 100; x <= 120; x++) tiles[`${x},200`] = T.PLATFORM;
  const world = makeWorld(tiles);
  const s = sense({ world, player: { px: 105, py: 199, onGround: true }, target: { x: 118, y: 199 } });

  assert.equal(s.onGround, true);
  assert.equal(s.cliffAhead, false, '平台连续时应认得出脚下有地');
  assert.equal(s.wallH, 0);
  assert.equal(s.blockingSelf, false, '平台不能算把人卡住');
  assert.equal(isStandable(T.PLATFORM), true);
  assert.equal(isBlockingSolid(T.PLATFORM), false);
});

/* ── 2. 平台尽头：平台断掉就是悬崖 ─────────────────────────────── */
test('平台尽头：平台在 110 断掉 → cliffAhead', () => {
  const tiles = {};
  for (let x = 100; x <= 110; x++) tiles[`${x},200`] = T.PLATFORM;
  const world = makeWorld(tiles);
  const s = sense({ world, player: { px: 110, py: 199, onGround: true }, target: { x: 130, y: 199 } });
  assert.equal(s.cliffAhead, true, '站在平台最后一格，前方三列都无地');
});

/* ── 3. 门：关着的门要认成 doorAhead，且不算阻挡 ────────────────── */
test('门：前方关着的木门 → doorAhead 且不判阻挡', () => {
  const tiles = ground({}, 100, 120, 210);
  for (let y = 207; y <= 209; y++) tiles[`111,${y}`] = T.CLOSED_DOOR;
  const world = makeWorld(tiles);
  const s = sense({ world, player: { px: 110, py: 209, onGround: true }, target: { x: 120, y: 209 } });

  assert.equal(s.doorAhead, true);
  assert.equal(s.wallH, 0, '门不应被算成墙，否则会想去跳或挖');
  assert.equal(s.buildAhead, false, '门单独靠 doorAhead 表达');
  assert.equal(isBlockingSolid(T.CLOSED_DOOR), false);
});

/* ── 4. 实墙（天然）：墙高算得对，且不是建材 ──────────────────── */
test('实墙：3 格高的土墙 → wallH=3 且不是建材', () => {
  const tiles = ground({}, 100, 120, 210);
  for (let y = 207; y <= 209; y++) tiles[`111,${y}`] = T.DIRT;
  const world = makeWorld(tiles);
  const s = sense({ world, player: { px: 110, py: 209, onGround: true }, target: { x: 120, y: 209 } });

  assert.equal(s.wallH, 3);
  assert.equal(s.buildAhead, false);
});

/* ── 5. 实墙（自己造的）：同样高度但必须是建材 ─────────────────── */
test('自建墙：3 格高木块 → wallH=3 且 buildAhead=true', () => {
  const tiles = ground({}, 100, 120, 210);
  for (let y = 207; y <= 209; y++) tiles[`111,${y}`] = T.WOOD;
  const world = makeWorld(tiles);
  const s = sense({ world, player: { px: 110, py: 209, onGround: true }, target: { x: 120, y: 209 } });

  assert.equal(s.wallH, 3);
  assert.equal(s.buildAhead, true, '自己造的东西不能被挖，必须由感知层标出来');
  assert.equal(isBuildTile(T.WOOD), true);
});

/* ── 6. 上坡：2 格台阶，不是坑也不是悬崖 ───────────────────────── */
test('上坡：前方 2 格台阶 → wallH=2，不误判 gap/cliff', () => {
  const tiles = ground({}, 100, 120, 210);
  tiles['111,209'] = T.DIRT;
  tiles['111,208'] = T.DIRT;
  const world = makeWorld(tiles);
  const s = sense({ world, player: { px: 110, py: 209, onGround: true }, target: { x: 120, y: 208 } });

  assert.equal(s.wallH, 2);
  assert.equal(s.gapAhead, false);
  assert.equal(s.cliffAhead, false);
  assert.equal(s.buildAhead, false);
});

/* ── 7. 坑：脚下空、2~6 格深 ──────────────────────────────────── */
test('坑：脚下空且对岸 3 格深 → gapAhead，不是悬崖', () => {
  const tiles = ground({}, 100, 109, 210);
  tiles['110,210'] = T.AIR; // 坑
  ground(tiles, 111, 120, 213); // 对岸低 3 格
  const world = makeWorld(tiles);
  const s = sense({ world, player: { px: 109, py: 209, onGround: true }, target: { x: 118, y: 212 } });

  assert.equal(s.cliffAhead, false, '3 格深是坑不是悬崖');
  assert.equal(s.gapAhead, true);
});

/* ── 8. 悬崖：前方一路到底 ────────────────────────────────────── */
test('悬崖：前方 15 格内无地面 → cliffAhead 且不是坑', () => {
  const tiles = ground({}, 100, 109, 210);
  const world = makeWorld(tiles);
  const s = sense({ world, player: { px: 109, py: 209, onGround: true }, target: { x: 140, y: 209 } });

  assert.equal(s.cliffAhead, true);
  assert.equal(s.gapAhead, false);
});

/* ── 9. 卡方块：身体两格都被实心包住 ─────────────────────────── */
test('卡方块：身体两格都是石头 → blockingSelf=true', () => {
  const tiles = ground({}, 100, 120, 210);
  tiles['110,209'] = T.STONE;
  tiles['110,208'] = T.STONE;
  const world = makeWorld(tiles);
  const s = sense({ world, player: { px: 110, py: 209, onGround: true }, target: { x: 120, y: 209 } });
  assert.equal(s.blockingSelf, true);
});

/* ── 10. 站在门格里不算卡住（事故回归） ───────────────────────── */
test('门格：站在门里 blockingSelf=false（避免把门当卡死）', () => {
  const tiles = ground({}, 100, 120, 210);
  for (let y = 207; y <= 209; y++) tiles[`110,${y}`] = T.CLOSED_DOOR;
  const world = makeWorld(tiles);
  const s = sense({ world, player: { px: 110, py: 209, onGround: true }, target: { x: 120, y: 209 } });
  assert.equal(s.blockingSelf, false);
});

/* ── 11. 到达判定 ────────────────────────────────────────────── */
test('到达：tol 内 arrived=true', () => {
  const tiles = ground({}, 100, 120, 210);
  const world = makeWorld(tiles);
  const a = sense({ world, player: { px: 118, py: 209, onGround: true }, target: { x: 119, y: 209, tol: 2 } });
  const b = sense({ world, player: { px: 110, py: 209, onGround: true }, target: { x: 119, y: 209, tol: 2 } });
  assert.equal(a.arrived, true);
  assert.equal(b.arrived, false);
});

/* ── 12. 危险态透传 ──────────────────────────────────────────── */
test('危险态：dead / lava / drowning 原样透传', () => {
  const tiles = ground({}, 100, 120, 210);
  const world = makeWorld(tiles);
  const s = sense({
    world,
    player: { px: 110, py: 209, onGround: true, dead: true, lava: true, drowning: true },
    target: { x: 120, y: 209 },
  });
  assert.equal(s.dead, true);
  assert.equal(s.lava, true);
  assert.equal(s.drowning, true);
});

/* ── 13. 原地朝目标方向 ───────────────────────────────────────── */
test('方向：目标在左 dir=-1，重合时默认 1', () => {
  const tiles = ground({}, 100, 120, 210);
  const world = makeWorld(tiles);
  assert.equal(sense({ world, player: { px: 110, py: 209 }, target: { x: 105, y: 209 } }).dir, -1);
  assert.equal(sense({ world, player: { px: 110, py: 209 }, target: { x: 110, y: 209 } }).dir, 1);
});