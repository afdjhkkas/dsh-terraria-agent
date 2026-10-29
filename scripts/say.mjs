#!/usr/bin/env node
/**
 * 把话说进游戏聊天框（不是终端，是玩家真的能看到的那个框）。
 *
 * 三条通道各自有颜色，靠行首前缀区分，由桥的 `dialog` 命令落地：
 *   think  灰   —— 我在想什么（推理过程、排查思路、为什么这么判断）
 *   say    助手 —— 我要对玩家说的话（结论、提问、报告）
 *   warn   警告 —— 危险/失败/需要人介入
 *
 * 用法：
 *   node scripts/say.mjs think "先看这一格的回读，再决定要不要挖"
 *   node scripts/say.mjs say   "东边 12 格有个箱子，我去开一下"
 *   echo "多行文本" | node scripts/say.mjs think          # 从 stdin 读
 *   node scripts/say.mjs say --dry "..."                 # 只打印不发送
 *
 * 为什么单独做一个脚本：游戏聊天框单行会换行，一条消息太长会被切碎；
 * 这里负责按句子切段、加前缀、按顺序推送，调用方只管写字。
 */
import { createRequire } from 'node:module';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { readFileSync } from 'node:fs';

const ROOT = fileURLToPath(new URL('..', import.meta.url));

const PREFIX = { think: '~', say: '<', warn: '!', title: '#' };
const COLORS = { think: '灰', say: '助手色', warn: '告警色', title: '标题色' };

const argv = process.argv.slice(2);
const dry = argv.includes('--dry');
const rest = argv.filter((a) => a !== '--dry');
const kind = rest[0];
const inline = rest.slice(1).join(' ').trim();

if (!PREFIX[kind]) {
  console.error(`用法: node scripts/say.mjs <${Object.keys(PREFIX).join('|')}> "文本" [--dry]`);
  process.exit(2);
}

function readStdin() {
  try {
    return readFileSync(0, 'utf8');
  } catch {
    return '';
  }
}

let text = inline;
if (!text) text = readStdin();
text = String(text || '').trim();
if (!text) {
  console.error('没有内容可发（参数或 stdin 都是空的）');
  process.exit(2);
}

/** 按句子切段：游戏聊天框一行大约放得下 40 个汉字，超过就拆，避免被引擎硬切。 */
function chunk(input, limit = 40) {
  const out = [];
  for (const para of input.split(/\r?\n/)) {
    const line = para.trim();
    if (!line) continue;
    if (line.length <= limit) {
      out.push(line);
      continue;
    }
    // 优先在中文标点处断，其次硬断
    let rest = line;
    while (rest.length > limit) {
      let cut = -1;
      for (const p of ['。', '；', '，', '！', '？', '.', ';', ',', ' ']) {
        const i = rest.lastIndexOf(p, limit);
        if (i > cut) cut = i;
      }
      if (cut < limit * 0.5) cut = limit - 1;
      out.push(rest.slice(0, cut + 1).trim());
      rest = rest.slice(cut + 1).trim();
    }
    if (rest) out.push(rest);
  }
  return out;
}

const lines = chunk(text).map((l) => `${PREFIX[kind]} ${l}`);

if (dry) {
  for (const l of lines) console.log(l);
  process.exit(0);
}

const payload = JSON.stringify({ text: lines.join('\n'), max: Math.max(3, lines.length + 2) });
const r = spawnSync(process.execPath, ['scripts/bridge.mjs', 'dialog', '--params', payload], {
  cwd: ROOT,
  encoding: 'utf8',
});

if (r.status !== 0) {
  console.error('发送失败:', (r.stderr || r.stdout || '').trim().slice(0, 400));
  process.exit(r.status || 1);
}

let parsed = {};
try {
  parsed = JSON.parse(r.stdout);
} catch {
  parsed = { raw: r.stdout.trim() };
}
console.log(`已发 ${parsed.sent ?? lines.length} 行（${kind} / ${COLORS[kind]}）`);
