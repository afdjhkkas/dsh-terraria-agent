#!/usr/bin/env node
/**
 * 任务用例库的命令行入口。
 *
 *   node scripts/testcases.mjs --list                 列出全部用例
 *   node scripts/testcases.mjs --list --phase 1       只看第一阶段
 *   node scripts/testcases.mjs --validate             校验用例库自身（会红在测试里）
 *   node scripts/testcases.mjs --dry                  离线预检，打印汇总
 *   node scripts/testcases.mjs --dry --phase 1 --md docs/testcases/gap-report.md
 *   node scripts/testcases.mjs --dry --json /tmp/base.json
 *   node scripts/testcases.mjs --dry --baseline docs/testcases/baseline.json
 *
 * 退出码：`--validate` 或 `--dry` 发现缺口时 exit 1，方便挂进 CI / npm test。
 * 想只看不算失败，加 `--allow-gap`。
 */

import { writeFile, readFile, mkdir } from 'node:fs/promises'
import { dirname } from 'node:path'
import { loadCases, validate, runSuite, formatReport, diffReports } from '../src/testcases.js'

const argv = process.argv.slice(2)
const has = (flag) => argv.includes(flag)
const val = (flag) => {
  const i = argv.indexOf(flag)
  return i >= 0 ? argv[i + 1] : undefined
}

async function main() {
  if (has('--help') || argv.length === 0) {
    console.log(`用法：
  node scripts/testcases.mjs --list [--phase N] [--category X]
  node scripts/testcases.mjs --validate
  node scripts/testcases.mjs --dry [--phase N] [--category X] [--md 路径] [--json 路径] [--baseline 路径] [--allow-gap]`)
    return 0
  }

  if (has('--list')) {
    const doc = await loadCases()
    const phase = val('--phase')
    const cat = val('--category')
    const rows = doc.cases.filter(
      (c) => (phase == null || Number(c.phase) === Number(phase)) && (cat == null || c.category === cat),
    )
    for (const c of rows) {
      const miss = c.needs.missingCommands?.length ? ` [缺 ${c.needs.missingCommands.join(',')}]` : ''
      console.log(`${c.id}  P${c.phase}  ${c.category.padEnd(7)} ${c.task}${miss}`)
    }
    console.log(`\n共 ${rows.length} 条`)
    return 0
  }

  if (has('--validate')) {
    const v = await validate()
    for (const e of v.errors) console.error('ERROR ' + e)
    for (const w of v.warnings) console.warn('WARN  ' + w)
    console.log(v.ok ? `ok: ${v.total} 条用例，结构无问题（${v.warnings.length} 条提示）` : `失败：${v.errors.length} 处错误`)
    return v.ok ? 0 : 1
  }

  if (has('--dry')) {
    const phase = val('--phase')
    const category = val('--category')
    const report = await runSuite({ phase, category })

    if (has('--baseline')) {
      const path = val('--baseline')
      try {
        const before = JSON.parse(await readFile(path, 'utf8'))
        const d = diffReports(before, report)
        console.log(`对比基线 ${path}`)
        console.log(`  修好了 ${d.fixed.length}：${d.fixed.join(' ') || '—'}`)
        console.log(`  新坏了 ${d.broken.length}：${d.broken.join(' ') || '—'}`)
        console.log(`  仍缺   ${d.still.length}：${d.still.join(' ') || '—'}`)
        if (d.broken.length) {
          console.error('回归：原来通过的用例现在不过了')
          return 1
        }
      } catch (e) {
        console.warn(`基线读不了（跳过对比）：${e.message}`)
      }
    }

    const md = formatReport(report)
    const mdPath = val('--md')
    if (mdPath) {
      await mkdir(dirname(mdPath), { recursive: true })
      await writeFile(mdPath, md, 'utf8')
      console.log(`报告写入 ${mdPath}`)
    }
    const jsonPath = val('--json')
    if (jsonPath) {
      await mkdir(dirname(jsonPath), { recursive: true })
      await writeFile(jsonPath, JSON.stringify(report, null, 2) + '\n', 'utf8')
      console.log(`基线写入 ${jsonPath}`)
    }
    if (!mdPath && !jsonPath) console.log(md)

    console.log(`\n结果：${report.pass}/${report.total} 通过，${report.gap} 条有缺口`)
    return report.gap === 0 || has('--allow-gap') ? 0 : 1
  }

  console.error('没有动作。看 --help')
  return 2
}

main().then((code) => process.exit(code)).catch((e) => {
  console.error(e?.stack ?? e)
  process.exit(1)
})