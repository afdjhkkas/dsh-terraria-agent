// move 的地形闸门。
//
// 病根：`move` 原先只按住方向键 ms 毫秒，回包永远 ok:true。
// 人掉进 14 格深的坑，回包还是 ok:true —— AI 看到 ok 就以为走成了。
// 现在 move 发之前先沿 dir 预走一遍（和 probe 同一套判据），
// 遇到 >2 格落差就拒绝，并把「jump / 绕 / 搭桥」三个选项写在错误里。
//
// C# 逻辑在游戏里跑，node 这里用「同判据镜像 + 源码锁」两件事来守：
// 镜像保证规则是 §0.57 那一条（>2 才拦，1~2 放行），
// 源码锁保证闸门真的接在 move 的写入之前、且默认开着。

import test from 'node:test'
import assert from 'node:assert/strict'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const REPO = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const CS = fs.readFileSync(path.join(REPO, 'DshTerrariaBridge', 'Commands.cs'), 'utf8')

// ---- 判据镜像：与 Commands.cs 的 TerrainAhead 逐行对应 ----
// depth[i] = 从当前位置往 dir 方向第 i+1 列，脚下要找几格才落到地板
//   0       脚下就是地板
//   1~2     小台阶，人自己会走下去（不拦）
//   >2      悬崖（AGENTS §0.57：不许走过去）
//   null    14 格内找不到地板
//   'wall'  身体过不去（只提示，不拦——撞墙人不会受伤）
function terrainAhead(depth, steps) {
  let reach = 0
  for (let i = 1; i <= steps; i++) {
    const d = depth[i - 1]
    if (d === 'wall') return { ok: false, reach, issue: 'blocked', drop: 0 }
    if (d === null) return { ok: false, reach, issue: 'pit_deep', drop: 14 }
    if (d > 2) return { ok: false, reach, issue: `pit_${d}_deep`, drop: d }
    reach = i
  }
  return { ok: true, reach, issue: 'none', drop: 0 }
}

test('落差 3 格：判悬崖，reach 停在坑前那格', () => {
  const r = terrainAhead([0, 0, 3, 0], 4)
  assert.equal(r.ok, false)
  assert.equal(r.issue, 'pit_3_deep')
  assert.equal(r.reach, 2, '前两格能走，第三格是坑')
  assert.equal(r.drop, 3)
})

test('落差 2 格：放行（人能自己走下去，和 §0.57 的阈值一致）', () => {
  const r = terrainAhead([0, 2, 0, 0], 4)
  assert.equal(r.ok, true)
  assert.equal(r.reach, 4)
})

test('第一列就是 14 格深坑：reach=0，一步都不该走', () => {
  const r = terrainAhead([14, 0], 2)
  assert.equal(r.ok, false)
  assert.equal(r.reach, 0)
  assert.equal(r.issue, 'pit_14_deep')
})

test('14 格内找不到地板：判 pit_deep，不假装有底', () => {
  const r = terrainAhead([0, null], 2)
  assert.equal(r.ok, false)
  assert.equal(r.issue, 'pit_deep')
  assert.equal(r.reach, 1)
})

test('撞墙：判 blocked（移动会停住，但这不是摔下去）', () => {
  const r = terrainAhead([0, 'wall', 0], 3)
  assert.equal(r.ok, false)
  assert.equal(r.issue, 'blocked')
  assert.equal(r.reach, 1)
})

test('全程平地：放行到 steps 尽头', () => {
  const r = terrainAhead([0, 0, 0, 0, 0], 5)
  assert.equal(r.ok, true)
  assert.equal(r.reach, 5)
})

// ---- 源码锁：闸门必须真的接在 move 里，而且默认开着 ----

function moveBody() {
  const start = CS.indexOf('private static string Move(JsonElement p)')
  assert.ok(start > 0, '找不到 Move 函数')
  // 下一个同级函数：Jump
  const end = CS.indexOf('private static string Jump(JsonElement p)', start)
  assert.ok(end > start, '找不到 Move 的结束位置')
  return CS.slice(start, end)
}

test('TerrainAhead 存在，且用的是 PathBodyFree / PathFloorAt（和 probe 同一套读地形）', () => {
  const i = CS.indexOf('private static bool TerrainAhead(')
  assert.ok(i > 0, '缺少 TerrainAhead')
  const body = CS.slice(i, i + 1600)
  assert.match(body, /BridgeSystem\.PathBodyFree\(/)
  assert.match(body, /BridgeSystem\.PathFloorAt\(/)
})

test('move 里调用了 TerrainAhead，且闸门在写方向键之前', () => {
  const body = moveBody()
  const gate = body.indexOf('TerrainAhead(')
  const write = body.indexOf('BridgeSystem.LeftUntil = until')
  assert.ok(gate > 0, 'move 没有调用 TerrainAhead，闸门没接上')
  assert.ok(write > 0, '找不到写 LeftUntil 的那行')
  assert.ok(gate < write, '闸门必须排在写方向键之前，否则拦不住')
})

test('闸门默认开启：terrain 缺省为 true，force 缺省为 false', () => {
  const body = moveBody()
  assert.match(body, /Json\.Bool\(p, "terrain", true\)/, 'terrain 必须默认 true')
  assert.match(body, /Json\.Bool\(p, "force", false\)/, 'force 必须默认 false')
})

test('拒绝时给出三个可选动作，并说明怎么硬走', () => {
  const body = moveBody()
  assert.match(body, /jump/)
  assert.match(body, /绕/)
  assert.match(body, /place/)
  assert.match(body, /force:true/)
  assert.match(body, /pit_/)
})

test('move 仍然只写输入标志，没有任何改世界的语句', () => {
  const body = moveBody()
  assert.doesNotMatch(body, /Main\.tile\[[^\]]*\]\.(type|wall|active)\s*=(?!=)/)
  assert.doesNotMatch(body, /WorldGen\./)
  assert.doesNotMatch(body, /\.velocity\.[XY]\s*=(?!=)/)
})

// ---- 第二道判据：深度不够也拦 ----
// 实测 (1796,309)：深 2 格、宽 3 格、头顶是半砖。闸门的 d>2 完全不拦，
// 但落进去 regions 回 verdict=trapped / cells=0，escape 只能报 stuck（跳 19 次、gainY 0）。
// 所以判据换成「落进去还能连通多少格」：landed < 40 且不到当前的一半，就判 trap。
function trapped(hereReach, landedReach) {
  return landedReach < 40 && landedReach * 2 < hereReach
}

test('镜像：开放缓坡不拦，封闭小井拦', () => {
  assert.equal(trapped(258, 0), true, '开阔地旁边一个 0 连通的死角，必须拦')
  assert.equal(trapped(258, 39), true, '连 39 格也不到 40，且远小于 258，拦')
  assert.equal(trapped(79, 0), true, '自己就在小空间里，落点更小，拦')
  assert.equal(trapped(258, 79), false, '79 已经够走开，放行')
  assert.equal(trapped(30, 20), false, '两处都小的时候不拦，宁可放过')
  assert.equal(trapped(258, 40), false, '40 是下限，等于 40 放行')
})

test('源码锁：TerrainAhead 用 EscapeProbeReach 比落点与起点的连通数', () => {
  const i = CS.indexOf('private static bool TerrainAhead')
  assert.ok(i > 0, '缺少 TerrainAhead')
  const body = CS.slice(i, i + 2200)
  assert.match(body, /BridgeSystem\.EscapeProbeReach\(px, py, 8\)/, '起点连通数要在循环外算一次')
  assert.match(body, /BridgeSystem\.EscapeProbeReach\(nx, ny \+ d, 8\)/, '落点连通数要按落点算')
  assert.match(body, /landedReach < 40 && landedReach \* 2 < hereReach/, '判据必须与镜像一致')
  assert.match(body, /issue = "trap"/, '要给出独立的 trap 状态，不能混进 pit_')
})

test('源码锁：move 对 trap 单独回包，且仍给 jump / place / 绕 / force 四条路', () => {
  const body = moveBody()
  assert.match(body, /tIssue == "trap"/, 'move 没有处理 trap 分支')
  assert.match(body, /落进去就走不开了/, '错误文本要说明这是"进得去出不来"，不是"深"')
  assert.match(body, /只连通 \d|只连通 \{/, '要报出落点能连通多少格')
})

test('说明：真机验证不在本文件内（需要游戏开着看角色真的停在坑边）', () => {
  assert.ok(true)
})