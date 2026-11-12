// scan {format:"regions"}：把 96×96 格原始 tile 压成十几行区域结论。
//
// 为什么不是“截屏”：桥里没有任何截屏代码（capture 是虫网抓小动物），
// 而 scan 原样吐 tile 是上万 token、还只是一帧。真正缺的是“我在哪、这地方通不通、谁在旁边”，
// 那是 BFS 一次就能算完的事，不该丢给模型去数格子。
import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const commands = readFileSync(join(root, 'DshTerrariaBridge', 'Commands.cs'), 'utf8');

function slice(text, startMark, endMark) {
  const a = text.indexOf(startMark);
  assert.ok(a > 0, `找不到起点：${startMark}`);
  const b = text.indexOf(endMark, a);
  assert.ok(b > a, `找不到终点：${endMark}`);
  return text.slice(a, b);
}

const scanBody = () => slice(commands, 'private static string Scan(JsonElement p)', 'private static string TileAt(JsonElement p)');
const regionBody = () => slice(commands, 'private static string RegionReport(', 'private static string Scan(JsonElement p)');

test('scan 的 format 白名单收了 regions', () => {
  const body = scanBody();
  assert.match(body, /format != "regions"/);
  assert.match(body, /format == "regions"/);
  assert.match(body, /return RegionReport\(cx, cy, r\);/);
});

test('regions 走的是 BFS 可达性，不是逐格打印', () => {
  const body = regionBody();
  assert.match(body, /PathBodyFree\(/, '连通条件必须是身体能过去');
  assert.match(body, /PathFloorAt\(/, '站立点按脚下有地算');
  assert.match(body, /Queue<int>/);
  // 不应该原样吐 96×96
  assert.doesNotMatch(body, /tiles\[j\]\[/);
});

test('verdict 只有四种，且都带得出下一步', () => {
  const body = regionBody();
  for (const v of ['open', 'closed_pit', 'trapped', 'closed_room']) {
    assert.match(body, new RegExp(`"${v}"`), `缺少 verdict: ${v}`);
  }
});

test('区域结论里带了威胁，且只算敌意的', () => {
  const body = regionBody();
  assert.match(body, /threats/);
  assert.match(body, /CanBeChasedBy\(\)|friendly|townNPC/);
  assert.match(body, /incoming/, '怪物朝向要能看出来，否则分不清路过还是冲我来');
});

test('RegionReport 是只读的：不改 tile、不挥、不写文件', () => {
  const body = regionBody();
  assert.doesNotMatch(body, /Main\.tile\[[^\]]+\]\.type\s*=(?!=)/);
  assert.doesNotMatch(body, /WorldGen\./);
  assert.doesNotMatch(body, /BeginItemUse\(/);
  assert.doesNotMatch(body, /BeginPlayerMine\(/);
  assert.doesNotMatch(body, /File\.(Write|Append|Delete|Move|Copy)/);
});