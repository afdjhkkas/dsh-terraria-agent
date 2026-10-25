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
  node scripts/testcases.mjs --dry [--phase N] [--category X] [--md 路径] [--json 路径] [--baseline 路径] [--allow-gap]
  node scripts/testcases.mjs --sim [--phase N] [--md 路径]   阶段①离线推演包（不连游戏）`)
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

  if (has('--sim')) {
    // 阶段①：离线模拟。只为每条用例产出一份"推演包"：
    // 路由标记 + 该读哪几个库（真正读出来）+ 相关命令是否在清单里 + 已知缺口。
    // 不连游戏、不发动作；推演本身由模型按 prompts/offline-sim.md 写。
    const { loadCapabilities } = await import('../src/meta-check.js')
    const { buildContext } = await import('../src/kb-router.js')
    const caps = await loadCapabilities()
    const phase = val('--phase')
    const category = val('--category')
    const doc = await loadCases()
    const rows = doc.cases.filter(
      (c) => (phase == null || Number(c.phase) === Number(phase)) && (category == null || c.category === category),
    )
    const out = []
    for (const c of rows) {
      const tags = ['safety_limit', ...(c.needs.kb ?? [])].filter((t, i, a) => a.indexOf(t) === i)
      const { docs } = await buildContext(c.task, { tags })
      const have = (c.needs.commands ?? []).filter((n) => caps.byName.has(n))
      const unknownDeclared = (c.needs.commands ?? []).filter((n) => !caps.byName.has(n))
      out.push(`### ${c.id}（阶段 ${c.phase} · ${c.category}）\n\n**任务**：${c.task}\n`)
      out.push(`**路由**：\n\n\`\`\`\nNEED_KNOWLEDGE:${tags.join(',')}\n\`\`\`\n`)
      out.push(`**要读的库**（${docs.length} 个）：` + docs.map((d) => `${d.tag}(${d.text.length}字)`).join(' / ') + '\n')
      out.push(`**声明需要的命令**：${(c.needs.commands ?? []).join(', ') || '—'}`)
      if (unknownDeclared.length) out.push(`　清单里没有：${unknownDeclared.join(', ')}`)
      else if (have.length) out.push('　全部在清单里')
      if (c.needs.missingCommands?.length) out.push(`\n**已知缺的命令**：${c.needs.missingCommands.join(', ')}`)
      if (c.needs.kbMissing?.length) out.push(`\n**已知缺的知识**：${c.needs.kbMissing.join('；')}`)
      if (c.needs.unsupported?.length) out.push(`\n**撞上明确做不到**：${c.needs.unsupported.join('；')}`)
      if (c.note) out.push(`\n**备注**：${c.note}`)
      out.push('\n**推演要点**：按 `prompts/offline-sim.md` 写 SELF_CHECK 五维 + FLOW + GAPS。\n')
    }
    const body = `# 离线推演包（阶段①）\n\n> 由 \`node scripts/testcases.mjs --sim\` 生成。每条用例的推演结论写回 \`docs/testcases/sim-gaps.md\`。\n> 这个模式不连游戏、不发动作，只走路由 + 检索 + 五维自检 + 动作流推演。\n\n共 ${rows.length} 条。\n\n` + out.join('\n')
    const mdPath = val('--md')
    if (mdPath) {
      await mkdir(dirname(mdPath), { recursive: true })
      await writeFile(mdPath, body, 'utf8')
      console.log(`推演包写入 ${mdPath}（${rows.length} 条）`)
    } else {
      console.log(body)
    }
    return 0
  }

  console.error('没有动作。看 --help')
  return 2
}

main().then((code) => process.exit(code)).catch((e) => {
  console.error(e?.stack ?? e)
  process.exit(1)
})