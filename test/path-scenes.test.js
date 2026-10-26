// 寻路固定场景集：每个场景是一份固定地图快照 + 起点终点 + 预期结论。
//
// 纪律（用户 m17728 定的四条）：
//   1. 场景绑定固定快照、起终点、预期（连通 / 不通 / 是否允许开建材），覆盖房子、坑、斜坡、
//      水域、地牢、平台、门这些典型 case；
//   2. 以后调 A* 参数或改判定规则，必须先跑整套，改前改后对比，不能单场景试对就合并；
//   3. 场景集跟代码一起进仓库维护，每次变更记录哪个用例失效、是否退化；
//   4. 这套只校验寻路底层判定，不碰 Sense / Evaluate / Act 四层决策。
//
// 加场景的办法：往 test/fixtures/path-scenes.json 里加一条即可，不必改这个文件。
// 符号名到瓦片 ID 的映射在下面 SYM 里；要用新方块先在这里登记。

import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { T, makeWorld } from '../src/sense-model.js';
import { astar, pathKinds } from '../src/path-model.js';

const SYM = {
  dirt: T.DIRT,
  stone: T.STONE,
  grass: T.GRASS,
  door: T.CLOSED_DOOR,
  opendoor: T.OPEN_DOOR,
  platform: T.PLATFORM,
  wood: T.WOOD,
  // 地牢蓝砖（TileID.BlueDungeonBrick = 41）：能走、不可挖，用来验证"不可挖"不等于"不可通行"
  bluebrick: 41,
};

const fixtureUrl = new URL('./fixtures/path-scenes.json', import.meta.url);
const { scenes } = JSON.parse(fs.readFileSync(fixtureUrl, 'utf8'));

test('场景集不为空且字段完整', () => {
  assert.ok(scenes.length >= 10, `场景太少：${scenes.length}`);
  for (const s of scenes) {
    assert.ok(s.id && s.note, '每个场景都要有 id 和 note');
    assert.ok(Array.isArray(s.start) && s.start.length === 2, `${s.id} 缺 start`);
    assert.ok(Array.isArray(s.goal) && s.goal.length === 2, `${s.id} 缺 goal`);
    assert.equal(typeof s.expect?.ok, 'boolean', `${s.id} 缺 expect.ok`);
  }
  const ids = scenes.map((s) => s.id);
  assert.equal(new Set(ids).size, ids.length, '场景 id 不能重复');
});

for (const s of scenes) {
  test(`场景 ${s.id}：${s.note}`, () => {
    const tiles = {};
    for (const [k, v] of Object.entries(s.tiles)) {
      const id = SYM[v];
      assert.notEqual(id, undefined, `${s.id} 用了没登记的符号 ${v}`);
      tiles[k] = id;
    }
    const world = makeWorld(tiles);
    const [sx, sy] = s.start;
    const [tx, ty] = s.goal;
    const r = astar({ world, sx, sy, tx, ty });

    assert.equal(r.ok, s.expect.ok, `${s.id} 期望 ok=${s.expect.ok}，实际 ${r.ok}（${r.why ?? ''}）`);

    if (s.expect.ok && s.expect.kinds) {
      const kinds = pathKinds(world, r.path);
      for (const k of s.expect.kinds) {
        assert.ok(kinds.includes(k), `${s.id} 路径应包含 ${k}，实际 ${kinds.join(',') || '空'}`);
      }
    }
  });
}