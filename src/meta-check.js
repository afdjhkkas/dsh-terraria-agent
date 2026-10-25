/**
 * 元自检（顶层固定流程）的机器部分。
 *
 * 规则正文在 `prompts/meta-check.md`；这里只做三件事：
 *   1. 解析模型写的 SELF_CHECK 块（五行，PASS/GAP）
 *   2. 把缺口整理成「缺口清单 + 分支A/B」，并且**判定能不能动手**
 *   3. 拿常驻能力清单校验计划里出现的命令名（防编造）
 *
 * 为什么要有机器部分：光靠提示词，模型可以写"PASS"然后照样编一个 `fish` 命令。
 * 解析 + 校验之后，"没写自检 / 少写一维 / 编造命令" 都会变成可报错的失败。
 */

import { readFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')

export const META_CHECK_PATH = join(ROOT, 'prompts', 'meta-check.md')
export const CAPS_JSON_PATH = join(ROOT, 'docs', 'capabilities.json')

export const SELF_CHECK_MARKER = 'SELF_CHECK'

/** 五个维度。编号与名称固定；`aliases` 只是容忍模型写成别的说法。 */
export const DIMENSIONS = [
  { no: 1, label: '规则核验', gap: '知识缺口', aliases: ['规则', '游戏规则'] },
  { no: 2, label: '道具核验', gap: '道具缺口', aliases: ['道具', '物品'] },
  { no: 3, label: '地形/路径核验', gap: '路径缺口', aliases: ['地形', '路径', '地形路径', '地形/路径'] },
  { no: 4, label: '动作原语核验', gap: '动作原语缺口', aliases: ['动作原语', '动作', '原语'] },
  { no: 5, label: '状态读取核验', gap: '状态读取缺口', aliases: ['状态读取', '状态', '读取'] },
]

const OK_WORDS = ['PASS', 'OK', '通过', '✅']
const GAP_WORDS = ['GAP', '缺口', 'FAIL', '不通过', '❌']
const SKIP_WORDS = ['SKIP', 'skip', '跳过']

let promptCache = null
/** 懒加载规则正文（缺文件时不要拖垮插件启动）。 */
export async function loadMetaCheckPrompt() {
  if (promptCache != null) return promptCache
  promptCache = await readFile(META_CHECK_PATH, 'utf8')
  return promptCache
}

let capsCache = null
/** 读生成的能力清单（docs/capabilities.json）。 */
export async function loadCapabilities() {
  if (capsCache != null) return capsCache
  const raw = JSON.parse(await readFile(CAPS_JSON_PATH, 'utf8'))
  const names = raw.commands.map((c) => c.name)
  const byName = new Map(raw.commands.map((c) => [c.name, c]))
  capsCache = { commands: raw.commands, names, byName, notSupported: raw.notSupported ?? [] }
  return capsCache
}

/** 测试用：清掉缓存，改文件后能重新读。 */
export function resetCapabilitiesCache() {
  capsCache = null
  promptCache = null
}

function normalizeStatus(word) {
  const w = String(word ?? '').trim()
  if (!w) return ''
  const upper = w.toUpperCase()
  if (SKIP_WORDS.some((s) => w === s || upper === s.toUpperCase())) return 'SKIP'
  // 先判 GAP：`不通过` 里也含 `通过`
  if (GAP_WORDS.some((g) => upper.includes(g.toUpperCase()))) return 'GAP'
  if (OK_WORDS.some((o) => upper.includes(o.toUpperCase()))) return 'PASS'
  return ''
}

/**
 * 解析模型输出里的 SELF_CHECK 块。
 *
 * 容忍：中英文冒号、`1.`/`1、`/`一`、`【维度】`、CRLF、PASS/GAP 前后带说明。
 * 返回 `{found, markerFirst, skip, items, missing, gaps, canAct}`。
 * `canAct` 只在「找到了块 + 五维齐全 + 没有 GAP」时为 true。
 */
export function parseSelfCheck(text) {
  const src = String(text ?? '').replace(/\r\n?/g, '\n')
  const lines = src.split('\n')

  // `SELF_CHECK: skip（只读）` 这种一行封顶的写法：只允许用于不涉及动作缺口的只读小事
  for (const line of lines) {
    if (new RegExp('^\\s*' + SELF_CHECK_MARKER + '\\s*[:：]\\s*(SKIP|跳过)', 'i').test(line)) {
      return { found: true, markerFirst: false, skip: true, items: [], missing: DIMENSIONS.map((d) => d.no), gaps: [], canAct: false }
    }
  }

  let blockStart = -1
  for (let i = 0; i < lines.length; i++) {
    if (new RegExp('^\\s*' + SELF_CHECK_MARKER + '\\s*[:：]?\\s*$', 'i').test(lines[i])) {
      blockStart = i
      break
    }
  }
  if (blockStart < 0) {
    const inlineAt = lines.findIndex((l) => new RegExp(SELF_CHECK_MARKER, 'i').test(l))
    return {
      found: false,
      markerFirst: false,
      skip: false,
      inlineAt,
      items: [],
      missing: DIMENSIONS.map((d) => d.no),
      gaps: [],
      canAct: false,
    }
  }

  const items = []
  for (const raw of lines.slice(blockStart + 1)) {
    const trimmed = raw.trim()
    if (!trimmed) continue
    if (new RegExp('^' + SELF_CHECK_MARKER, 'i').test(trimmed)) continue
    const numbered = /^(\d+)\s*[.、)．]?\s*(.*)$/.exec(trimmed)
    const no = numbered ? Number(numbered[1]) : 0
    let rest = numbered ? numbered[2] : trimmed
    rest = rest.replace(/[【】\[\]]/g, '')
    const colon = /^([^:：]{1,14})\s*[:：]\s*(.*)$/.exec(rest)
    if (!colon) continue
    const labelText = colon[1].trim()
    const tail = colon[2].trim()
    const norm = (x) => x.replace(/[\s/／·]/g, '')
    const dim = DIMENSIONS.find((d) => {
      if (items.some((it) => it.no === d.no)) return false
      if (no && no !== d.no) return false
      const want = [d.label, ...d.aliases].map(norm)
      const got = norm(labelText)
      // 带编号时允许简称；没编号时必须是完整标签，避免把正文里的"规则："当自检行
      return no ? want.includes(got) : got === norm(d.label)
    })
    if (!dim) continue
    const status = normalizeStatus(tail.split(/[\s（(,，]/)[0] || '')
    if (!status) continue
    items.push({
      no: dim.no,
      label: dim.label,
      gap: dim.gap,
      status,
      note: tail.replace(/^\S+/, '').trim(),
    })
  }

  const missing = DIMENSIONS.filter((d) => !items.some((it) => it.no === d.no)).map((d) => d.no)
  const gaps = items.filter((it) => it.status === 'GAP')
  const canAct = items.length === DIMENSIONS.length && gaps.length === 0 && missing.length === 0
  return {
    found: true,
    markerFirst: isMarkerFirst(lines, blockStart),
    skip: false,
    items,
    missing,
    gaps,
    canAct,
  }
}

/**
 * SELF_CHECK 块前面允许有什么？
 *
 * 严格说它该独占第一行，但实际流程里同一回合的第一行是 `NEED_KNOWLEDGE:...`
 * （先要资料，再核验），所以多容忍一条标记行，其他任何非空内容都算"不独占"。
 */
function isMarkerFirst(lines, blockStart) {
  const before = lines.slice(0, blockStart).filter((l) => l.trim())
  if (before.length === 0) return true
  if (before.length === 1 && /^\s*NEED_KNOWLEDGE\s*[:：]/i.test(before[0])) return true
  return false
}

function escapeRe(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

/**
 * 缺口报告：给玩家看的 A/B 出口。
 * 没有缺口时返回 null（正常路径不产生噪音）。
 */
export function formatGapReport(check) {
  if (!check || (check.canAct && !check.skip)) return null
  const problems = []
  for (const no of check.missing ?? []) {
    const dim = DIMENSIONS.find((d) => d.no === no)
    problems.push(`- 【自检没做完】第 ${no} 维${dim ? dim.label : ''}没写`)
  }
  for (const gap of check.gaps ?? []) {
    problems.push(`- 【${gap.gap}】${gap.note || '（没写缺什么）'}`)
  }
  const head = `【自检未通过】缺口 ${problems.length} 项，先不动手：`
  const tail = [
    '',
    '你可以选：',
    '分支A：先补齐条件再执行原目标（前置任务也要过五维）',
    '分支B：放弃这个任务',
  ].join('\n')
  return `${head}\n${problems.join('\n')}\n${tail}`
}

/**
 * 从计划文本里找出被当作命令用、但清单里没有的名字。
 * 只看反引号里的词，避免把普通中文和散文当成命令。
 */
export function checkPlanCommands(text, names) {
  const known = new Set(names)
  const unknown = []
  const used = []
  for (const m of String(text ?? '').matchAll(/`([^`\n]{1,60})`/g)) {
    const raw = m[1].trim()
    // `mine{x,y}` / `terraria_move dir=left` → 取第一个词
    const token = (raw.match(/^([A-Za-z][A-Za-z0-9_]*(?::[A-Za-z][A-Za-z0-9_]*)?)/) ?? [])[1]
    if (!token) continue
    const name = token.replace(/^terraria:/, '')
    if (!/^[a-z][a-z0-9_]*$/.test(name)) continue
    if (known.has(name)) {
      if (!used.includes(name)) used.push(name)
      continue
    }
    if (!unknown.includes(name)) unknown.push(name)
  }
  return { unknown, used }
}

/**
 * 一键自检：解析 + 命令校验，给工具层用。
 * `task` 只用于回显，不参与判定。
 */
export async function selfCheck(output, { task = '' } = {}) {
  const caps = await loadCapabilities()
  const check = parseSelfCheck(output)
  const plan = checkPlanCommands(output, caps.names)
  const report = formatGapReport(check)
  return {
    task,
    found: check.found,
    markerFirst: check.markerFirst,
    skip: check.skip,
    items: check.items,
    missing: check.missing,
    gaps: check.gaps.map((g) => ({ no: g.no, dimension: g.gap, detail: g.note })),
    unknownCommands: plan.unknown,
    usedCommands: plan.used,
    canAct: check.canAct && plan.unknown.length === 0,
    report,
    dimensions: DIMENSIONS.map((d) => ({ no: d.no, label: d.label })),
  }
}