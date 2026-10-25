#!/usr/bin/env node
/**
 * 知识库防污染检查（kb-lint）。
 *
 * 七个库只收「换张地图也成立」的通用机制。这个脚本扫 docs/kb 下的七个库，
 * 一旦发现单局测试痕迹就报错：
 *   - 成对坐标        2103,301 / ( 2103 , 301 )
 *   - 坐标赋值        x=2103 / y = 466
 *   - 世界名          当前世界名
 *   - 单局措辞        本档 / 本次 / 这局 / 实测 / 当时 / 上次测试
 *   - 计时数值        900ms / 1.5 秒/tick / 11 格/秒
 *
 * 用法：node scripts/kb-lint.mjs            → 有违规 exit 1
 *       node scripts/kb-lint.mjs --json     → 机器可读
 *
 * 单局数据不是不能记录，而是不能记在库里：请放 docs/terraria-mechanics.md
 * 或 docs/debug/。
 */

import { readFile } from 'node:fs/promises'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { LIBRARIES, KB_DIR } from '../src/kb-router.js'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')

/** 每条规则：id + 说明 + 正则。加规则时同时更新 README 的「不能进库的东西」表。 */
export const RULES = [
  {
    id: 'coordinate-pair',
    why: '成对坐标（单局地形数据）',
    re: /(?<![\d.])\d{2,4}\s*[,，]\s*\d{2,4}(?![\d])/g,
  },
  {
    id: 'coord-assign',
    why: '坐标赋值',
    re: /\b[xyXY]\s*[=＝]\s*-?\d+/g,
  },
  {
    id: 'paren-coord',
    why: '括号坐标',
    re: /\(\s*-?\d{1,4}\s*[,，]\s*-?\d{1,4}\s*\)/g,
  },
  {
    id: 'world-name',
    why: '本档世界名',
    re: /可耻的港湾/g,
  },
  {
    id: 'single-run-word',
    why: '单局措辞（本档/本次/实测/这局…）',
    re: /本档|本次|这局|本轮实测|当次实测|上次测试|实测过/g,
  },
  {
    id: 'timing-number',
    why: '计时/速率数值（属调试参数）',
    re: /\d+\s*(ms|毫秒|tick|tps|格\/秒|格每秒)/gi,
  },
]

/**
 * 扫一份文本。
 * @param {string} text
 * @returns {{rule: string, why: string, line: number, match: string, text: string}[]}
 */
export function lintText(text) {
  const out = []
  const lines = String(text ?? '').split(/\r?\n/)
  lines.forEach((line, i) => {
    for (const rule of RULES) {
      rule.re.lastIndex = 0
      let m
      while ((m = rule.re.exec(line)) !== null) {
        out.push({ rule: rule.id, why: rule.why, line: i + 1, match: m[0], text: line.trim().slice(0, 160) })
        if (m[0].length === 0) rule.re.lastIndex++
      }
    }
  })
  return out
}

/** 扫整个 docs/kb 下的六个库。返回 {file: violations[]}。 */
export async function lintTree(dir = KB_DIR) {
  const result = {}
  for (const [tag, file] of Object.entries(LIBRARIES)) {
    const text = await readFile(join(dir, file), 'utf8')
    const v = lintText(text)
    if (v.length) result[`${tag} (${file})`] = v
  }
  return result
}

if (process.argv[1] && fileURLToPath(import.meta.url) === join(ROOT, 'scripts', 'kb-lint.mjs')) {
  const json = process.argv.includes('--json')
  const bad = await lintTree()
  const files = Object.keys(bad)
  if (json) {
    console.log(JSON.stringify({ ok: files.length === 0, violations: bad }, null, 2))
  } else if (files.length === 0) {
    console.log(`ok: 七个库都很干净（${Object.keys(LIBRARIES).length} 个文件）`)
  } else {
    console.error('知识库混进了单局数据：')
    for (const [file, vs] of Object.entries(bad)) {
      for (const v of vs) {
        console.error(`  ${file}:${v.line}  [${v.why}]  "${v.match}"  ← ${v.text}`)
      }
    }
    console.error('\n修法：删掉/改写成通用表述，或把这条数据移到 docs/terraria-mechanics.md / docs/debug/。')
  }
  process.exit(files.length === 0 ? 0 : 1)
}