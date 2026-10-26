#!/usr/bin/env node
// 把旧的真人录像重新切成原子模板。
//
// 早期录的是"整段轨迹"（帧序列），不符合示范库的入库规则——那里面混着循环轮数、
// 总时长、固定坐标。这个脚本用 src/atom-model.js 重新过一遍，只把原子动作抽出来：
// 一次按下到松开 = 一个模板；同类连续只留第一条；超过单次上限的长按直接丢。
//
// 用法：
//   node scripts/reatomize.mjs <录像.jsonl> [更多.jsonl...]
//   node scripts/reatomize.mjs --out atoms.json a.jsonl b.jsonl
//
// 录像每帧至少要有 { t, keys?, mouse?{left,right} }，t 单位为毫秒。

import fs from 'node:fs';
import { basename } from 'node:path';
import { ATOMIC_KINDS, atomize } from '../src/atom-model.js';

function parseArgs(argv) {
  const files = [];
  let out = null;
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--out') { out = argv[++i]; continue; }
    files.push(argv[i]);
  }
  return { files, out };
}

function readFrames(file) {
  const text = fs.readFileSync(file, 'utf8')
  return text.split('\n').map(l => { try { return JSON.parse(l) } catch { return null } }).filter(Boolean)
}

function quantile(sorted, q) {
  if (!sorted.length) return null
  return sorted[Math.min(sorted.length - 1, Math.floor(sorted.length * q))]
}

function summarize(byKind) {
  const out = {}
  for (const [kind, arr] of Object.entries(byKind)) {
    const sorted = [...arr].sort((a, b) => a - b)
    out[kind] = {
      label: ATOMIC_KINDS[kind]?.label || kind,
      n: sorted.length,
      median: quantile(sorted, 0.5),
      p25: quantile(sorted, 0.25),
      p75: quantile(sorted, 0.75),
      min: sorted[0],
      max: sorted[sorted.length - 1],
    }
  }
  return out
}

const { files, out } = parseArgs(process.argv.slice(2))
if (!files.length) {
  console.error('用法: node scripts/reatomize.mjs [--out atoms.json] <录像.jsonl> ...')
  process.exit(2)
}

const byKind = {}
let totalFrames = 0
let totalAtoms = 0
for (const f of files) {
  if (!fs.existsSync(f)) { console.log(`跳过（文件不存在） ${f}`); continue }
  const frames = readFrames(f)
  totalFrames += frames.length
  const { atoms, dropped } = atomize(frames)
  totalAtoms += atoms.length
  const perFile = {}
  for (const a of atoms) {
    ;(byKind[a.kind] ||= []).push(a.holdMs)
    perFile[a.kind] = (perFile[a.kind] || 0) + 1
  }
  console.log(`${basename(f).padEnd(24)} 帧 ${String(frames.length).padStart(5)} 原子 ${String(atoms.length).padStart(4)} 丢弃 ${JSON.stringify(dropped)}`)
  if (Object.keys(perFile).length) console.log(`${''.padEnd(24)} ${JSON.stringify(perFile)}`)
}

const summary = summarize(byKind)
console.log('\n=== 原子模板统计（跨全部录像）')
for (const [kind, s] of Object.entries(summary)) {
  console.log(`${kind.padEnd(14)} ${s.label}  n=${String(s.n).padStart(3)}  中位 ${s.median}ms  p25-p75 ${s.p25}-${s.p75}ms  范围 ${s.min}-${s.max}ms`)
}
console.log(`\n合计 帧 ${totalFrames}  原子 ${totalAtoms}`)

if (out) {
  fs.writeFileSync(out, JSON.stringify({ generatedAt: new Date().toISOString(), files: files.map(f => basename(f)), frames: totalFrames, atoms: totalAtoms, byKind: summary }, null, 1), 'utf8')
  console.log(`已写入 ${out}`)
}