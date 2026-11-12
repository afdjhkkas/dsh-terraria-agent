// escape：掉坑/卡死的闭环自救。
//
// 为什么不是"命令里循环到出来"：命令一次只跑一帧就返回，中间那几秒没人管；
// 而游戏要 60Hz 判断。所以命令只开局，动作在 BridgeSystem.ApplyEscape 里每 tick 做。
//
// 判据三条（都能算，不靠猜）：
//   ① 比起点高 2 格且站稳
//   ② 可达格数比起点多 10 格
//   ③ 连续 60 次评估没进展 → stuck，并说清左右上下各几格
import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const bridge = readFileSync(join(root, 'DshTerrariaBridge', 'BridgeSystem.cs'), 'utf8');
const commands = readFileSync(join(root, 'DshTerrariaBridge', 'Commands.cs'), 'utf8');

function slice(text, startMark, endMark) {
  const a = text.indexOf(startMark);
  assert.ok(a > 0, `找不到起点：${startMark}`);
  const b = text.indexOf(endMark, a);
  assert.ok(b > a, `找不到终点：${endMark}`);
  return text.slice(a, b);
}

const escapeBody = () => slice(bridge, 'private static void ApplyEscape(Player p, ulong now)', '// 手动动作开始时');
const cmdBody = () => slice(commands, 'private static string EscapeCmd(JsonElement p)', '// 钩爪槽');

// 与 C# 同判据的镜像：只判"这一步该结束了吗、结束成什么"
function decide(s) {
  if (s.dead) return 'stuck';
  if (s.timeout) return 'stuck';
  if ((s.ty <= s.startY - 2 && s.grounded) || s.reach >= s.startReach + 10) return 'escaped';
  if (s.stalls >= 60) return 'stuck';
  if (s.left === 0 && s.right === 0) return 'stuck';
  return 'running';
}

test('开了自救之后由 Update 每 tick 驱动，而且排在 guard 之后', () => {
  assert.match(bridge, /internal static bool EscapeActive;/);
  const iGuard = bridge.indexOf('ApplyGuard(p, now);');
  const iEscape = bridge.indexOf('ApplyEscape(p, now);');
  assert.ok(iGuard > 0 && iEscape > iGuard, 'ApplyEscape 必须在 ApplyGuard 之后调用');
});

test('judged：高 2 格且站稳就判 escaped', () => {
  assert.equal(decide({ ty: 100, startY: 103, grounded: true, reach: 5, startReach: 5, stalls: 3, left: 2, right: 2 }), 'escaped');
  // 高是高了但还在空中，不算
  assert.equal(decide({ ty: 100, startY: 103, grounded: false, reach: 5, startReach: 5, stalls: 0, left: 2, right: 2 }), 'running');
});

test('judged：位置没变但可达范围大了 10 格也算出来', () => {
  assert.equal(decide({ ty: 100, startY: 100, grounded: true, reach: 30, startReach: 20, stalls: 9, left: 3, right: 3 }), 'escaped');
  assert.equal(decide({ ty: 100, startY: 100, grounded: true, reach: 29, startReach: 20, stalls: 9, left: 3, right: 3 }), 'running');
});

test('judged：连续 60 次没进展就放弃，不再无限试', () => {
  assert.equal(decide({ ty: 100, startY: 100, grounded: true, reach: 6, startReach: 6, stalls: 60, left: 1, right: 1 }), 'stuck');
  assert.equal(decide({ ty: 100, startY: 100, grounded: true, reach: 6, startReach: 6, stalls: 59, left: 1, right: 1 }), 'running');
});

test('judged：左右都是 0 格 → 立刻 stuck，并说清需要挖', () => {
  assert.equal(decide({ ty: 100, startY: 100, grounded: true, reach: 4, startReach: 4, stalls: 0, left: 0, right: 0 }), 'stuck');
});

test('C# 里确实按这三条判，且 stuck 带地形诊断', () => {
  const body = escapeBody();
  assert.match(body, /ty <= EscapeStartY - 2 && grounded/);
  assert.match(body, /reach >= EscapeStartReach \+ 10/);
  assert.match(body, /EscapeStalls >= 60/);
  assert.match(body, /EscapeDiag\(p\)/);
  assert.match(body, /EscapeFinish\("escaped"/);
  assert.match(body, /EscapeFinish\("stuck"/);
});

test('命令层只开局和查询：有 report、already、dead 三种分支', () => {
  const body = cmdBody();
  assert.match(body, /Json\.Bool\(p, "report", false\)/);
  assert.match(body, /BridgeSystem\.EscapeActive/);
  assert.match(body, /已经在自救中/);
  assert.match(body, /角色已经死了/);
  assert.match(body, /BridgeSystem\.EscapeActive = true;/);
});

test('escape 是只读世界之外的：不改 tile、不直接摆速度、不写文件', () => {
  const body = escapeBody();
  assert.doesNotMatch(body, /Main\.tile\[[^\]]+\]\.type\s*=(?!=)/);
  assert.doesNotMatch(body, /WorldGen\./);
  assert.doesNotMatch(body, /File\.(Write|Append|Delete|Move|Copy)/);
  // 走和跳必须走已有的 until 通道，而不是自己写 velocity
  assert.match(body, /LeftUntil = now \+ Ticks\(240\)/);
  assert.match(body, /RightUntil = now \+ Ticks\(240\)/);
  assert.match(body, /JumpUntil = now \+ Ticks\(180\)/);
});

test('说明：真机验证不在本文件内', () => {
  assert.ok(true);
});
// 真机第一次调 escape 时踩到的：人在开阔地（半径 8 能走 258 格）也叫了 escape，
// 结果白跑 2 秒才回 stuck + "头顶 8 格空"，看着像被困住。加一道"要不要救"的预判。
test('开阔地不该开局自救，直接说清楚', () => {
  const body = cmdBody()
  assert.match(body, /EscapeProbeReach\(nx, ny, 8\)/, '开局要先算一次连通格数')
  assert.match(body, /openReach >= 120/, '连通 120 格以上判定为不在坑里')
  assert.match(body, /你现在不在坑里/, '拒绝时要说明为什么')
  assert.match(body, /要走路用 move[\s\S]{0,40}要上台阶用 jump/, '要给出正确的替代命令')
})

// 真机踩到的第二个坑：往左走 7 格掉进坑，escape 连跑两次都回 escaped，
// 但每次结束后人还在坑底（report 的 nowY 一直是 287）。
// 根因是"站稳"只判了 velocity.Y≈0——跳到顶点那一瞬速度正好是 0。
test('站稳要脚踩实心，且连续两次评估才算出来', () => {
  const body = escapeBody()
  assert.match(body, /TileSolid\(tx, ty \+ 1\)/, '站稳必须检查脚下一格是实心')
  assert.match(body, /Math\.Abs\(p\.velocity\.Y\) < 0\.6f && TileSolid/, '速度与脚下是 and 关系')
  assert.match(body, /EscapeHits\+\+/, '要有连续计数')
  assert.match(body, /EscapeHits >= 2/, '连续两次才判 escaped')
  assert.match(body, /else EscapeHits = 0/, '不满足要归零')
})
