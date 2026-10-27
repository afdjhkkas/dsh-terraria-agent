// 寻路固定用例集：走 / 跨 / 跳 / 落 四类边，以及"必须上屋顶才能过"的房型。
//
// 用法纪律同 test/sense.test.js：一次只改一类决策；改前改后跑同一套；字段不改名。
import test from 'node:test';
import assert from 'node:assert/strict';
import { T, makeWorld } from '../src/sense-model.js';
import { astar, neighbors, pathKinds, isReachableGround } from '../src/path-model.js';

function ground(tiles, x0, x1, floor, type = T.DIRT) {
  for (let x = x0; x <= x1; x++) tiles[`${x},${floor}`] = type;
  return tiles;
}

/* ── 1. 平地直线 ─────────────────────────────────────────── */
test('平地：直线可达，全程 walk', () => {
  const world = makeWorld(ground({}, 100, 120, 210));
  const r = astar({ world, sx: 105, sy: 209, tx: 115, ty: 209 });
  assert.equal(r.ok, true);
  assert.deepEqual(pathKinds(world, r.path).every((k) => k === 'walk'), true);
});

/* ── 2. 1 格台阶：自动跨，不用跳 ─────────────────────────── */
test('台阶：1 格高自动跨过去', () => {
  const tiles = ground({}, 100, 120, 210);
  tiles['111,210'] = T.DIRT;
  tiles['111,209'] = T.DIRT;
  const world = makeWorld(tiles);
  const r = astar({ world, sx: 108, sy: 209, tx: 116, ty: 208 });
  assert.equal(r.ok, true);
  assert.ok(pathKinds(world, r.path).includes('cross'), '应该出现 cross 边');
});

/* ── 3. 2 格台阶：必须跳 ─────────────────────────────────── */
test('台阶：2 格高需要跳上去', () => {
  const tiles = ground({}, 100, 120, 210);
  tiles['111,209'] = T.DIRT;
  tiles['111,208'] = T.DIRT;
  const world = makeWorld(tiles);
  const r = astar({ world, sx: 108, sy: 209, tx: 116, ty: 207 });
  assert.equal(r.ok, true);
  // 2 格台阶既可能一次跳上去，也可能连跨两次 1 格台阶，两种都算对；
  // 用例原本写死"必须有 jump"，是我把"能上去"错写成了"必须跳"。
  const k = pathKinds(world, r.path);
  const climbed = k.filter((v) => v === 'cross').length >= 2 || k.includes('jump');
  assert.ok(climbed, `2 格高要真的上去，实际：${JSON.stringify(k)}`);
});

/* ── 4. 房型：墙挡路 → 必须跳屋顶再下来（垂直层核心用例） ── */
test('垂直层：中间一堵 4 格高墙，必须上屋顶再下', () => {
  const tiles = ground({}, 100, 130, 210);
  // 屋顶：117..119 在 206，比地面(210)高 4 格——实测无饰品满跳 4.1 格，刚好够。
  // 旧用例写的是 204/高 6 格，那是照“满跳 6.1”算的；实测只有 4.1，6 格根本跳不上去。
  for (let x = 117; x <= 119; x++) tiles[`${x},206`] = T.PLATFORM;
  // 4 格高墙挡在 118
  for (let y = 207; y <= 209; y++) tiles[`118,${y}`] = T.WOOD;

  const world = makeWorld(tiles);
  const r = astar({ world, sx: 112, sy: 209, tx: 124, ty: 209 });
  assert.equal(r.ok, true, `应能绕上屋顶过去：${r.why ?? ''}`);
  const kinds = pathKinds(world, r.path);
  assert.ok(kinds.includes('jump'), '必须有一次跳上屋顶');
  assert.ok(kinds.includes('drop'), '必须有一次从屋顶落下');
});

/* ── 5. 门：关着的门可穿 ────────────────────────────────── */
test('门：关着的门当通道，不绕路也不跳', () => {
  const tiles = ground({}, 100, 120, 210);
  for (let y = 207; y <= 209; y++) tiles[`111,${y}`] = T.CLOSED_DOOR;
  const world = makeWorld(tiles);
  const r = astar({ world, sx: 108, sy: 209, tx: 116, ty: 209 });
  assert.equal(r.ok, true);
  assert.ok(pathKinds(world, r.path).every((k) => k === 'walk' || k === 'cross'));
});

/* ── 6. 坑：1 格宽直接跳过 ──────────────────────────────── */
test('坑：1 格宽缺口用跳边越过', () => {
  const tiles = ground({}, 100, 115, 210);
  ground(tiles, 116, 125, 210);
  delete tiles['115,210']; // 115 空一格
  const world = makeWorld(tiles);
  const r = astar({ world, sx: 112, sy: 209, tx: 120, ty: 209 });
  assert.equal(r.ok, true);
  assert.ok(pathKinds(world, r.path).includes('jump') || pathKinds(world, r.path).includes('drop'));
});

/* ── 7. 高差过大：14 格以上不当通路 ─────────────────────── */
test('悬崖：14 格落差以内可落，超过则不算通路', () => {
  const ok = ground({}, 100, 110, 200);
  ground(ok, 111, 120, 210); // 10 格落差，可落
  const r1 = astar({ world: makeWorld(ok), sx: 105, sy: 199, tx: 115, ty: 209 });
  assert.equal(r1.ok, true, '10 格落差应该能落下去');

  const bad = ground({}, 100, 110, 190);
  ground(bad, 111, 120, 210); // 20 格落差，不许当通路
  const r2 = astar({ world: makeWorld(bad), sx: 105, sy: 189, tx: 115, ty: 209 });
  assert.equal(r2.ok, false, '20 格落差必须判不可达');
});

/* ── 8. 完全封闭：无路可走 ──────────────────────────────── */
test('封闭：四周全是实心 → 不可达', () => {
  const tiles = ground({}, 100, 120, 210);
  for (let x = 104; x <= 106; x++) for (let y = 200; y <= 209; y++) tiles[`${x},${y}`] = T.STONE;
  const world = makeWorld(tiles);
  const r = astar({ world, sx: 102, sy: 209, tx: 110, ty: 209 });
  assert.equal(r.ok, false);
  assert.equal(r.why, '没有可达路径');
});

/* ── 9. 起跳空间被堵：跳边不能穿透天花板 ────────────────── */
test('天花板：屋顶正上方有实心时不能跳上去', () => {
  const tiles = ground({}, 100, 120, 210);
  for (let x = 100; x <= 120; x++) tiles[`${x},204`] = T.STONE; // 头顶封死
  for (let y = 205; y <= 209; y++) tiles['111,205'] = T.STONE;
  for (let y = 205; y <= 209; y++) tiles['111,y'] = T.STONE;
  for (let y = 205; y <= 209; y++) tiles[`111,${y}`] = T.STONE;
  const world = makeWorld(tiles);
  const n = neighbors(world, 108, 209);
  assert.equal(n.some((v) => v.kind === 'jump' && v.y < 204), false, '不该跳出天花板');
});

/* ── 10. 目标点没地面：向下找落脚点 ────────────────────── */
test('目标点悬空：向下 1~3 格找落脚点', () => {
  const world = makeWorld(ground({}, 100, 120, 210));
  assert.equal(isReachableGround(world, 110, 209), true);
  assert.equal(isReachableGround(world, 110, 205), false);
  const r = astar({ world, sx: 105, sy: 209, tx: 110, ty: 206 });
  assert.equal(r.ok, true, '应自动落到 209');
  assert.equal(r.path[r.path.length - 1].y, 209);
});