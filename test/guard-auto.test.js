// guard {auto:true}：自动防守的续期开关。
//
// 为什么需要它：ApplyGuard 每 tick 都在跑，但它第一件事就是判 GuardUntil 有没有过期。
// 默认不续期（早期为“不要自动打怪”删掉的），所以 AI 发完 move、在想下一步的那几秒里，
// 贴到脸上的怪是没人还手的 —— 表现出来就是“每帧都看得见怪，却站着不动”。
// auto:true 让它每 30 秒自己续一次，一直续到 guard {off:true}。
//
// 这个文件锁的是“开关还在、默认还是关、off 真的能关掉”，不重复测 ApplyGuard 的战斗逻辑。
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

test('BridgeSystem 有 GuardAuto 字段', () => {
  assert.match(bridge, /internal static volatile bool GuardAuto;/);
});

test('ApplyGuard：没过期就不管，过期了只有 auto 才续期', () => {
  const body = slice(bridge, 'private static void ApplyGuard(Player p, ulong now)', '手动动作开始时，取消仍在后台运行的寻路');
  assert.match(body, /if \(GuardUntil <= now\)/);
  assert.match(body, /if \(!GuardAuto\)/);
  // 续期那一行必须在 !GuardAuto 分支之后，否则等于默认一直打
  const noAuto = body.indexOf('if (!GuardAuto)');
  const renew = body.indexOf('GuardUntil = now + Ticks(30000);');
  assert.ok(renew > noAuto, '续期代码必须在 !GuardAuto 判断之后');
  // 续期之前要先 return 掉关闭路径，保证“默认关”这个语义还在
  const untilRenew = body.slice(noAuto, renew);
  assert.match(untilRenew, /return;/, '关闭路径必须先 return');
});

test('GuardCmd 读 auto，默认 false', () => {
  const body = slice(commands, 'private static string GuardCmd(JsonElement p)', '// 钩爪槽');
  assert.match(body, /BridgeSystem\.GuardAuto = Json\.Bool\(p, "auto", false\);/);
});

test('guard off 同时清掉 auto', () => {
  const body = slice(commands, 'private static string GuardCmd(JsonElement p)', '// 钩爪槽');
  const offMark = body.indexOf('if (Json.Bool(p, "off", false)');
  assert.ok(offMark > 0, '找不到 off 分支');
  const offBlock = body.slice(offMark, body.indexOf('BridgeSystem.GuardDefault = true;', offMark));
  assert.match(offBlock, /GuardAuto = false;/, 'off 必须清 GuardAuto，否则关了还会自己续');
  assert.match(offBlock, /GuardUntil = 0;/);
});

test('回包和 state 都能看见 auto 状态', () => {
  const body = slice(commands, 'private static string GuardCmd(JsonElement p)', '// 钩爪槽');
  assert.match(body, /auto = BridgeSystem\.GuardAuto,/);
  const state = slice(commands, 'data["guard"] = new', 'data["attack"]');
  assert.match(state, /auto = BridgeSystem\.GuardAuto,/);
});