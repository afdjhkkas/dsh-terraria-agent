/**
 * 任务用例库：把「能不能干」从"跑一遍游戏碰运气"变成"逐条用例过一遍"。
 *
 * 为什么要有这个东西：自由通关一遍效率低、盲区大——事件和生物是随机的，
 * 碰不到的场景自检永远不会触发，缺口就继续藏着。用例库是我主动去撞。
 *
 * 一个用例包含三层信息：
 *   1. `task`         —— 直接就能下发的自然语言指令（玩家口吻，不是技术描述）
 *   2. `needs`        —— 这条指令要成立，需要哪些命令 / 哪些库 / 哪些状态
 *   3. `missingCommands` / `unsupported` / `kbMissing` —— 我已知现在**没有**的东西
 *
 * 第 3 层是这套东西能不能自我进化的关键：一旦命令补上，
 * `validate()` 会报"已实现，删掉声明"，强制我回来更新用例，不会烂在库里。
 *
 * 这里只做**离线预检**（不动游戏）：命令在不在、库里有没有这个词、
 * 声明为缺的是不是真的还缺。真机执行是另一回事——执行时按五维自检判。
 */

import { readFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { LIBRARIES, load as loadKb } from './kb-router.js'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')

export const CASES_PATH = join(ROOT, 'testcases', 'cases.json')
export const CAPS_PATH = join(ROOT, 'docs', 'capabilities.json')
export const SCHEMA = 'terraria-testcases/1'

let casesCache = null
let capsCache = null
const kbCache = new Map()

/** 读用例库。缓存，测试里要重读就 `resetCasesCache()`。 */
export async function loadCases() {
  if (casesCache) return casesCache
  const raw = await readFile(CASES_PATH, 'utf8')
  casesCache = JSON.parse(raw)
  return casesCache
}

export function resetCasesCache() {
  casesCache = null
}

/** 读能力清单（`scripts/kb-caps.mjs` 生成的机器版）。 */
export async function loadCaps() {
  if (capsCache) return capsCache
  capsCache = JSON.parse(await readFile(CAPS_PATH, 'utf8'))
  return capsCache
}

/** 取某个知识库的正文（按标签），带缓存。 */
export async function kbText(tag) {
  if (!kbCache.has(tag)) {
    const docs = await loadKb([tag])
    kbCache.set(tag, docs[0]?.text ?? '')
  }
  return kbCache.get(tag)
}

/** 用例声明的所有命令名（含 missingCommands），去重。 */
export function caseCommands(c) {
  const n = c?.needs ?? {}
  return [...new Set([...(n.commands ?? []), ...(n.missingCommands ?? [])])]
}

/**
 * 结构校验：用例库本身有没有写错。
 *
 * 这里的 `missingCommands` 反向检查是有意为之——命令补上之后不回来改用例，
 * 用例库就会开始骗人，所以让它在测试里直接变红。
 */
export async function validate(data) {
  const doc = data ?? (await loadCases())
  const caps = await loadCaps()
  const names = new Set(caps.commands.map((c) => c.name))
  const unsupportedNames = new Set(caps.notSupported.map((x) => x.what))
  const errors = []
  const warnings = []
  const seen = new Set()

  if (doc.schema !== SCHEMA) errors.push(`schema 不是 ${SCHEMA}：${doc.schema}`)
  const cats = new Set(Object.keys(doc.categories ?? {}))
  const cases = Array.isArray(doc.cases) ? doc.cases : []
  if (cases.length === 0) errors.push('用例库是空的')

  for (const c of cases) {
    const where = c?.id ?? '(没有 id)'
    if (!c?.id) errors.push('有用例没有 id')
    else if (seen.has(c.id)) errors.push(`${where}: id 重复`)
    else seen.add(c.id)

    if (![1, 2, 3, 4].includes(c?.phase)) errors.push(`${where}: phase 必须是 1~4，现在是 ${c?.phase}`)
    if (!cats.has(c?.category)) errors.push(`${where}: category 不在 categories 里：${c?.category}`)
    if (!c?.task || typeof c.task !== 'string' || !c.task.trim()) errors.push(`${where}: task 空`)

    const n = c?.needs ?? {}
    if (!Array.isArray(n.commands)) errors.push(`${where}: needs.commands 必须是数组`)
    if (!Array.isArray(n.kb)) errors.push(`${where}: needs.kb 必须是数组`)
    if (!Array.isArray(n.kbAnchors)) errors.push(`${where}: needs.kbAnchors 必须是数组`)
    if (!Array.isArray(n.kbMissing)) errors.push(`${where}: needs.kbMissing 必须是数组`)

    for (const cmd of n.commands ?? []) {
      if (!names.has(cmd)) errors.push(`${where}: 需要用到的命令 "${cmd}" 在能力清单里不存在（是不是该写进 missingCommands？）`)
    }
    for (const cmd of n.missingCommands ?? []) {
      if (names.has(cmd)) warnings.push(`${where}: "${cmd}" 已经实现了，把 missingCommands 里的声明删掉`)
    }
    for (const tag of n.kb ?? []) {
      if (!(tag in LIBRARIES)) errors.push(`${where}: 知识库标签不存在：${tag}`)
    }
    for (const what of n.unsupported ?? []) {
      if (!unsupportedNames.has(what)) errors.push(`${where}: unsupported 里的 "${what}" 不在能力清单的 notSupported 里（别自己编理由）`)
    }
  }

  return { ok: errors.length === 0, errors, warnings, total: cases.length }
}

/**
 * 单条用例的离线预检。
 *
 * 只判三件事，都是能落到「有 / 没有」的：
 *   动作原语：命令在不在
 *   能力：是不是撞上了清单里"明确做不到"的那几样
 *   知识：库里有没有这几个词
 */
export async function precheck(c, caps) {
  const cap = caps ?? (await loadCaps())
  const names = new Set(cap.commands.map((x) => x.name))
  const n = c.needs ?? {}
  const gaps = []

  for (const cmd of n.commands ?? []) {
    if (!names.has(cmd)) gaps.push({ dimension: '动作原语缺口', detail: `命令 ${cmd} 不存在` })
  }
  for (const cmd of n.missingCommands ?? []) {
    gaps.push({ dimension: '动作原语缺口', detail: `没有 ${cmd}` })
  }
  for (const what of n.unsupported ?? []) {
    gaps.push({ dimension: '能力缺口', detail: `明确做不到：${what}` })
  }

  // 知识缺口：只认我读过之后**确认**缺的（kbMissing）。
  // 关键词匹配（kbAnchors）不判失败——同一个概念库里可能换个说法，
  // 用字面匹配会把"坑"没写出来当成知识缺口，报告立刻变成噪音。
  for (const what of n.kbMissing ?? []) {
    gaps.push({ dimension: '知识缺口', detail: what })
  }

  const warns = []
  const anchors = n.kbAnchors ?? []
  if (anchors.length) {
    const texts = []
    for (const tag of n.kb ?? []) texts.push(await kbText(tag))
    const blob = texts.join('\n')
    const absent = anchors.filter((t) => !blob.includes(t))
    if (absent.length) warns.push(`库里没直接出现：${absent.join('、')}（确认是不是换了说法）`)
  }

  return { id: c.id, phase: c.phase, category: c.category, task: c.task, status: gaps.length ? 'GAP' : 'PASS', gaps, warnings: warns }
}

/** 跑一批用例（可只跑某个阶段 / 某个分类）。 */
export async function runSuite({ phase, category } = {}) {
  const doc = await loadCases()
  const caps = await loadCaps()
  const picked = (doc.cases ?? []).filter(
    (c) => (phase == null || Number(c.phase) === Number(phase)) && (category == null || c.category === category),
  )
  const results = []
  for (const c of picked) results.push(await precheck(c, caps))
  return {
    schema: SCHEMA,
    ranAt: new Date().toISOString(),
    phase: phase ?? null,
    category: category ?? null,
    total: results.length,
    pass: results.filter((r) => r.status === 'PASS').length,
    gap: results.filter((r) => r.status === 'GAP').length,
    results,
    aggregate: aggregate(results),
  }
}

/**
 * 把所有 GAP 汇总成「该补什么」。
 *
 * 这是整个流程的产出物：不按用例列，而按**要修的东西**列，
 * 这样补一次能消掉多条用例的缺口。
 */
export function aggregate(results) {
  const commands = new Map()
  const knowledge = new Map()
  const unsupported = new Map()
  for (const r of results) {
    for (const g of r.gaps) {
      if (g.dimension === '动作原语缺口') {
        const key = g.detail
        if (!commands.has(key)) commands.set(key, [])
        commands.get(key).push(r.id)
      } else if (g.dimension === '知识缺口') {
        const key = g.detail
        if (!knowledge.has(key)) knowledge.set(key, [])
        knowledge.get(key).push(r.id)
      } else {
        const key = g.detail
        if (!unsupported.has(key)) unsupported.set(key, [])
        unsupported.get(key).push(r.id)
      }
    }
  }
  const toRows = (m) => [...m.entries()].map(([what, ids]) => ({ what, cases: ids, count: ids.length })).sort((a, b) => b.count - a.count)
  return { commands: toRows(commands), knowledge: toRows(knowledge), unsupported: toRows(unsupported) }
}

/** 渲染成 markdown（用于写进 docs/testcases/gap-report.md）。 */
export function formatReport(report) {
  const catName = { move: '移动探索', gather: '采集挖掘', build: '建造放置', craft: '合成背包', npc: 'NPC 交互', fish: '钓鱼捉虫', combat: '战斗', event: '事件机制', gear: '装备增益' }
  const lines = []
  lines.push(`# 用例预检报告`, '')
  lines.push(`范围：${report.phase ? `阶段 ${report.phase}` : '全部阶段'}${report.category ? ` / ${catName[report.category] ?? report.category}` : ''}　`)
  lines.push(`结果：**${report.pass}/${report.total} 通过**，${report.gap} 条有缺口`, '')
  lines.push('> 离线预检只判三件事：命令在不在、是不是撞上"明确做不到"、库里有没有这个词。')
  lines.push('> 它不代替真机执行——真机那步还要按五维自检判规则与时序。', '')

  lines.push('## 一、该补的东西（按影响面排序）', '')
  if (!report.aggregate.commands.length && !report.aggregate.knowledge.length && !report.aggregate.unsupported.length) {
    lines.push('没有缺口。', '')
  } else {
    if (report.aggregate.commands.length) {
      lines.push('### 缺动作原语（要加命令的）', '')
      lines.push('| 缺什么 | 影响用例 | 条数 |', '|---|---|---|')
      for (const r of report.aggregate.commands) lines.push(`| ${r.what} | ${r.cases.join(' ')} | ${r.count} |`)
      lines.push('')
    }
    if (report.aggregate.unsupported.length) {
      lines.push('### 撞上"明确做不到"（要加读取/时序能力）', '')
      lines.push('| 限制 | 影响用例 | 条数 |', '|---|---|---|')
      for (const r of report.aggregate.unsupported) lines.push(`| ${r.what} | ${r.cases.join(' ')} | ${r.count} |`)
      lines.push('')
    }
    if (report.aggregate.knowledge.length) {
      lines.push('### 缺知识（要补库的）', '')
      lines.push('| 缺什么 | 影响用例 | 条数 |', '|---|---|---|')
      for (const r of report.aggregate.knowledge) lines.push(`| ${r.what} | ${r.cases.join(' ')} | ${r.count} |`)
      lines.push('')
    }
  }

  lines.push('## 二、逐条结果', '')
  lines.push('| 用例 | 阶段 | 分类 | 指令 | 结果 | 缺口 |', '|---|---|---|---|---|---|')
  for (const r of report.results) {
    const g = r.gaps.map((x) => `${x.dimension}：${x.detail}`).join('；') || '—'
    lines.push(`| ${r.id} | ${r.phase} | ${catName[r.category] ?? r.category} | ${r.task} | ${r.status} | ${g} |`)
  }
  lines.push('')
  return lines.join('\n')
}

/** 和上一轮基线比，谁修好了、谁新坏了（回归用）。 */
export function diffReports(before, after) {
  const index = (rep) => new Map(rep.results.map((r) => [r.id, r]))
  const a = index(before)
  const b = index(after)
  const fixed = []
  const broken = []
  const still = []
  for (const [id, r] of b) {
    const old = a.get(id)
    if (!old) continue
    if (old.status === 'GAP' && r.status === 'PASS') fixed.push(id)
    else if (old.status === 'PASS' && r.status === 'GAP') broken.push(id)
    else if (r.status === 'GAP') still.push(id)
  }
  return { fixed, broken, still }
}