#!/usr/bin/env node
/**
 * 生成「常驻能力清单」：docs/capabilities-manifest.md + AGENTS.md 里的能力块。
 *
 * 存在理由（用户 2026-10-26 的要求）：
 *   元自检第 4、5 维要拿**真实能力表**对照，不能靠我记忆里"我大概有吧"。
 *   所以清单必须从源码生成：新增一个命令，清单自己会变；忘了同步，测试会红。
 *
 * 用法：
 *   node scripts/kb-caps.mjs           写文件
 *   node scripts/kb-caps.mjs --check   只校验（CI / 测试用）
 *
 * 分类规则（尽量机械，避免我手写偏好）：
 *   - 函数体里有「动作标记」（见 ACTION_MARKERS）→ act
 *   - 但若同时有 `Json.Str(p, "action"` → both（同一命令按参数分只读/动作）
 *   - 都没有 → read
 *   - KIND_OVERRIDE 只用来纠机械规则判错的个例，每条都要写理由
 */

import { readFile, writeFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

import { braceDelta } from './kb-sync.mjs'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')
const CS = join(ROOT, 'DshTerrariaBridge', 'Commands.cs')
const BRIDGE = join(ROOT, 'DshTerrariaBridge', 'BridgeSystem.cs')
const AGENTS = join(ROOT, 'AGENTS.md')
const OUT = join(ROOT, 'docs', 'capabilities-manifest.md')
const OUT_JSON = join(ROOT, 'docs', 'capabilities.json')

const CAPS_BEGIN = '<!-- caps:begin 本节由 scripts/kb-caps.mjs 生成，不要手改 -->'
const CAPS_END = '<!-- caps:end -->'

/**
 * 动作标记：出现这些，说明这条命令会让角色动、改世界、改动背包或写盘。
 * 只收「真的动手」的符号；纯读的东西（Main.tile[x,y]、pl.statLife）不算。
 */
const ACTION_MARKERS = [
  'BeginItemUse(',
  'UseUntil =',
  'UseTileUntil =',
  'MoveUntil =',
  'JumpUntil =',
  'ClimbUpUntil',
  'ClimbDownUntil',
  'GuardUntil =',
  'FollowPlayer =',
  'selectedItem =',
  'controlUseItem = true',
  'controlLeft =',
  'controlRight =',
  'controlJump =',
  'SaveWorld(',
  'moveRoom(',
  'SpawnOnPlayer(',
  'StartInvasion(',
  'Main.NewText(',
  'Item.Clone(',
  '.Clone(',
  'pl.GetItem(',
  'pl.talkNPC =',
  'pl.armor[a] =',
  'pl.armor[armorSlot] =',
  'pl.statLife -=',
  'StartRain(',
  'StopRain(',
  'CraftItem(',
  'QuickHeal(',
  'SetTalkNPC(',
  'LeftUntil =',
  'RightUntil =',
  'UpUntil =',
  'DownUntil =',
  'pl.inventory[i] =',
  'pl.inventory[slot] =',
  'pl.armor[a] =',
  'SetDefaults(0)',
]

/** 机械规则判错的个例。每条必须写清为什么。 */
const KIND_OVERRIDE = {
  // 会写盘（导出 TSV），但不动世界、不动角色 → 对角色来说仍是只读
  idmap: 'read',
  // state 只是读；它把 guard.latched 里的 climbUp/climbDown 快照打出来，
  // 机械规则看到字段名 ClimbUpUntil/ClimbDownUntil 就误判成“动作”。
  state: 'read',
  // 会写世界存档文件，属于动作
  save: 'act',
  // 只算路径，不动角色
  path: 'read',
  // 读研究进度
  research: 'read',
  // 复制物品 = 改背包
  duplicate: 'act',
  // 探路/找方块都是只读
  find: 'read',
}

/**
 * 明确做不到的事（人工维护）。
 *
 * 这一块是元自检里最有用的部分：写清「读不到什么」，第 5 维才不会幻想接口。
 * 新增能力时要回来删掉对应行；删漏了不会报错，但写多了会被下面的断言拦住。
 */
const NOT_SUPPORTED = [
  {
    what: '通用弹幕状态',
    detail:
      '只能间接判断两类：自己名下的弹幕**数量**（远程攻击的凭证）、抓钩弹幕的**类型**（aiStyle 7）。' +
      '拿不到任意弹幕的位置/速度/生命周期，读不到"浮标有没有落水""咬钩没咬钩"这类状态。',
  },
  {
    what: '生物 AI 内部状态',
    detail:
      '可以列半径内 NPC 的类型/血量/坐标/是否敌对，但读不到目标、路径、攻击冷却、拾取意图。',
  },
  {
    what: '随机事件与时机窗口',
    detail:
      '没有任何「一直等到某件事发生」的**单条**原语：现有命令都是「发一个动作 → 到点返回」。' +
      '可行替代是模型侧循环——反复读状态（`bobber` 读浮标、`event` 读事件、`npc` 读敌人），' +
      '看到变化再发下一条动作。代价是判定粒度受采样间隔限制，' +
      '窄窗口（咬钩那一下、躲冲撞的帧）会漏，所以这类用例天然比确定性动作需要更多重试。',
  },
  {
    what: 'NPC 对话选项与任务进度',
    detail:
      '`talk` 能读到当前这一句正文（`chatText`）和右上角展示的物品（`cornerItem`），' +
      '`quest` 能读到渔夫的每日任务（要哪条鱼、今天交没交）；' +
      '但对话**选项按钮的文案读不到**（那是绘制时才算出来的），' +
      '其他 NPC 也没有可读的任务字段。',
  },
  {
    what: '容器以外的容器',
    detail: '只能读 6 格内的箱子（list/take）。梳妆台、展示框、武器架、展示柜都读不到。',
  },
  {
    what: '画面语义',
    detail:
      '截图只能看像素（本机没前台也能抓），但不能识别"这是什么怪""这里黑不黑"。' +
      '一切判定必须落到 state / tile / inventory 这类数值上。',
  },
  {
    what: '音效与事件字幕',
    detail: '读不到游戏内提示文本（"你感觉邪恶的东西在看着你"之类），只能靠 boss/event 状态反推。',
  },
  {
    what: '合成树自动规划',
    detail: '能查配方、能按 id 合成，但不会自己把"缺 3 个铁锭"翻译成"该去挖 9 个铁矿"。要我自己推。',
  },
]

/** 从函数体里拿 `<summary>` 的第一句，作为动作列表的人读说明。 */
function summaryOf(body) {
  const m = /<summary>([\s\S]*?)<\/summary>/.exec(body)
  if (!m) return ''
  const text = m[1]
    .split('\n')
    .map((l) => l.replace(/^\s*\/\/\/\s?/, '').trim())
    .filter((l) => l && !l.startsWith('<'))
    .join('')
    .trim()
  return text.replace(/。.*$/, '')
}

/** 抓 `Json.Ok(new {` 之后深度 1 的键名（返回值的顶层字段）。 */
function returnKeys(body) {
  const keys = []
  const re = /Json\.Ok\(\s*new\s*/g
  let m
  while ((m = re.exec(body))) {
    let i = body.indexOf('{', m.index + m[0].length)
    if (i < 0) continue
    // 单行形式 `new { a = 1, b = 2 }` 也要覆盖
    let depth = 0
    let buf = ''
    for (; i < body.length; i++) {
      const ch = body[i]
      if (ch === '{') {
        depth++
        if (depth === 1) continue
      }
      if (ch === '}') {
        depth--
        if (depth === 0) break
      }
      if (depth === 1) buf += ch
    }
    for (const part of splitTopLevel(buf)) {
      const k = /^\s*([A-Za-z_]\w*)\s*=/.exec(part)
      if (k && !keys.includes(k[1])) keys.push(k[1])
    }
  }
  return keys
}

/** `data["key"] = ...` 这种字典写法（state 用的就是这个）。 */
function dictKeys(body) {
  const out = []
  for (const m of body.matchAll(/\[\s*"([A-Za-z_]\w*)"\s*\]\s*=/g)) {
    if (!out.includes(m[1])) out.push(m[1])
  }
  return out
}

/** 按深度 0 的逗号切分（忽略括号/字符串里的逗号）。 */
function splitTopLevel(text) {
  const out = []
  let depth = 0
  let cur = ''
  for (let i = 0; i < text.length; i++) {
    const ch = text[i]
    if (ch === '"' || ch === "'") {
      const q = ch
      cur += ch
      i++
      while (i < text.length) {
        cur += text[i]
        if (text[i] === '\\') { i++; cur += text[i] ?? ''; i++; continue }
        if (text[i] === q) break
        i++
      }
      continue
    }
    if (ch === '(' || ch === '[' || ch === '{') depth++
    else if (ch === ')' || ch === ']' || ch === '}') depth--
    if (ch === ',' && depth === 0) {
      out.push(cur)
      cur = ''
      continue
    }
    cur += ch
  }
  if (cur.trim()) out.push(cur)
  return out
}

/** 解开 switch：case → 函数体；没走函数的（`case "x": return Ping();`）也跟上。 */
export function extractCapabilities(cs, bridgeSrc = '') {
  const lines = cs.split('\n')
  const funcStart = new Map()
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i].match(/(?:private|internal|public)\s+static\s+[\w<>\[\],\s]+?\s+([A-Za-z_]\w*)\s*\(\s*JsonElement\s+\w+\s*\)/)
    if (m) funcStart.set(m[1], i)
  }
  const funcBody = (fn) => {
    const start = funcStart.get(fn)
    if (start == null) return ''
    let i = start
    while (i < lines.length && !lines[i].includes('{') && i < start + 6) i++
    if (i >= lines.length) return ''
    let depth = 0
    let body = ''
    for (; i < lines.length; i++) {
      depth += braceDelta(lines[i])
      body += lines[i] + '\n'
      if (depth <= 0) break
    }
    return body
  }
  // 表达式体函数（`private static string Ping() => Json.Ok(...)`）
  const exprBody = (fn) => {
    const start = funcStart.get(fn)
    if (start == null) return ''
    let body = ''
    for (let i = start; i < Math.min(lines.length, start + 40); i++) {
      body += lines[i] + '\n'
      if (lines[i].includes(');')) break
    }
    return body
  }

  // 关键：命令本身往往只是一层壳（`Mine` → `BeginPlayerMine` → `StartSwing` → `BeginItemUse`）。
  // 只看命令自己的函数体会把 mine/move 判成"只读"，所以要把**本地辅助函数**也解出来做传递闭包。
  const allHelpers = new Map()
  for (const [fn, start] of funcStart) allHelpers.set(fn, funcBody(fn) || exprBody(fn))
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i].match(/(?:private|internal|public|protected)\s+static\s+[\w<>\[\],\.]+\s+([A-Za-z_]\w*)\s*\(/)
    if (!m || allHelpers.has(m[1])) continue
    let j = i
    while (j < lines.length && !lines[j].includes('{') && j < i + 6) j++
    if (j >= lines.length) continue
    let depth = 0
    let body = ''
    for (; j < lines.length; j++) {
      depth += braceDelta(lines[j])
      body += lines[j] + '\n'
      if (depth <= 0) break
    }
    allHelpers.set(m[1], body)
  }
  // 跨文件：真正干活的辅助函数大量在 BridgeSystem.cs 里（StartMove/StartSwing/...），
  // 只看 Commands.cs 会把 move / travel / equip 这些壳子判成只读。
  const bridgeLines = bridgeSrc.split('\n')
  for (let i = 0; i < bridgeLines.length; i++) {
    const m = bridgeLines[i].match(/(?:private|internal|public|protected)\s+static\s+[\w<>\[\],\.]+\s+([A-Za-z_]\w*)\s*\(/)
    if (!m || allHelpers.has(m[1])) continue
    let j = i
    while (j < bridgeLines.length && !bridgeLines[j].includes('{') && j < i + 6) j++
    if (j >= bridgeLines.length) continue
    let depth = 0
    let body = ''
    for (; j < bridgeLines.length; j++) {
      depth += braceDelta(bridgeLines[j])
      body += bridgeLines[j] + '\n'
      if (depth <= 0) break
    }
    allHelpers.set(m[1], body)
  }
  const actionHelpers = new Set(
    [...allHelpers].filter(([, b]) => ACTION_MARKERS.some((k) => b.includes(k))).map(([n]) => n),
  )
  // 传递闭包：调用了"动作函数"的函数，自己也是动作函数
  for (let pass = 0; pass < 4; pass++) {
    let grew = false
    for (const [name, body] of allHelpers) {
      if (actionHelpers.has(name)) continue
      for (const h of actionHelpers) {
        if (h !== name && new RegExp('(?:^|[^A-Za-z0-9_])' + h + '\\s*\\(').test(body)) {
          actionHelpers.add(name)
          grew = true
          break
        }
      }
    }
    if (!grew) break
  }
  const callsAction = (body) => {
    for (const h of actionHelpers) {
      if (new RegExp('(?:^|[^A-Za-z0-9_])' + h + '\\s*\\(').test(body)) return h
    }
    return ''
  }

  // 只扫顶层 Handle 的 switch：内层 switch 里的 case 是"参数取值"
  // （startevent 的 event=bloodmoon、move 的 dir=left），不是命令名。
  // 之前没限定范围，capabilities 里凭空多出 16 条根本调不通的命令。
  let handleStart = 0
  let handleEnd = lines.length - 1
  {
    const sw = lines.findIndex((l) => /switch\s*\(\s*cmd\s*\)/.test(l))
    if (sw >= 0) {
      handleStart = sw
      let depth = 0
      let opened = false
      for (let i = sw; i < lines.length; i++) {
        depth += braceDelta(lines[i])
        if (depth > 0) opened = true
        if (opened && depth <= 0) {
          handleEnd = i
          break
        }
      }
    }
  }
  const out = []
  for (let i = handleStart; i <= handleEnd; i++) {
    const m = lines[i].match(/case\s+"([a-z_]+)"\s*:/)
    if (!m) continue
    const name = m[1]
    let slice = ''
    for (let j = i; j <= handleEnd; j++) {
      if (j > i && (/case\s+"[a-z_]+"\s*:/.test(lines[j]) || /^\s*default\s*:/.test(lines[j]))) break
      slice += lines[j] + '\n'
    }
    const fn = slice.match(/return\s+([A-Za-z_]\w*)\s*\(/)
    let body = fn ? allHelpers.get(fn[1]) ?? funcBody(fn[1]) ?? '' : slice
    if (!body.trim()) body = slice
    // 有些命令只是转发到 BridgeSystem（chatpoll / chatpost），再往里看一层
    if (!returnKeys(body).length && !dictKeys(body).length) {
      const fwd = body.match(/Json\.Ok\(\s*(?:BridgeSystem\.)?([A-Za-z_]\w*)\s*\(/)
      if (fwd && allHelpers.has(fwd[1])) body = allHelpers.get(fwd[1])
    }
    const via = callsAction(body)
    const markers = ACTION_MARKERS.filter((k) => body.includes(k))
    const hasActionArg = /Json\.(?:Str|Bool)\(\s*p\s*,\s*"action"/.test(body)
    let kind = via || markers.length ? (hasActionArg ? 'both' : 'act') : 'read'
    if (KIND_OVERRIDE[name]) kind = KIND_OVERRIDE[name]
    out.push({
      name,
      fn: fn ? fn[1] : '',
      kind,
      via,
      markers,
      params: [...new Set([...body.matchAll(/Json\.\w+\(\s*p\s*,\s*"([^"]+)"/g)].map((x) => x[1]))],
      returns: [...new Set([...returnKeys(body), ...dictKeys(body)])],
      summary: summaryOf(body) || summaryOf(slice),
    })
  }
  return out.sort((a, b) => a.name.localeCompare(b.name))
}

const KIND_LABEL = { read: '只读', act: '动作', both: '只读/动作（看参数）' }

export function renderManifest(caps) {
  const read = caps.filter((c) => c.kind === 'read')
  const act = caps.filter((c) => c.kind === 'act')
  const both = caps.filter((c) => c.kind === 'both')
  const L = []
  L.push('# 常驻能力清单（生成物）')
  L.push('')
  L.push('> 由 `node scripts/kb-caps.mjs` 从 `DshTerrariaBridge/Commands.cs` 生成，**不要手改**。')
  L.push('> 元自检的第 4、5 维（动作原语 / 状态读取）就是拿这张表对照的：')
  L.push('> 表里没有的接口和命令，一律视为不存在，不许在计划里使用。')
  L.push('')
  L.push(`## 一、状态读取能力（${read.length + both.length} 个命令）`)
  L.push('')
  L.push('| 命令 | 能读到什么（返回值顶层字段） |')
  L.push('|---|---|')
  for (const c of [...read, ...both]) {
    const ret = c.returns.length ? c.returns.map((k) => '`' + k + '`').join(' ') : '—'
    L.push(`| \`${c.name}\` | ${ret} |`)
  }
  L.push('')
  L.push(`## 二、动作能力（${act.length + both.length} 个命令）`)
  L.push('')
  L.push('| 命令 | 参数 | 干什么 |')
  L.push('|---|---|---|')
  for (const c of [...act, ...both]) {
    const ps = c.params.length ? c.params.map((k) => '`' + k + '`').join(' ') : '—'
    L.push(`| \`${c.name}\` | ${ps} | ${c.summary || '—'} |`)
  }
  L.push('')
  L.push('## 三、明确做不到的（做计划前先看这里）')
  L.push('')
  L.push('> 这一节是人工维护的。它不是"暂时没写"，是"现在真的没有这个接口"。')
  L.push('> 任务卡在这里就是【动作原语缺口】或【状态读取缺口】，不许绕。')
  L.push('')
  for (const n of NOT_SUPPORTED) {
    L.push(`- **${n.what}**：${n.detail}`)
  }
  L.push('')
  return L.join('\n')
}

/** AGENTS.md 里的紧凑块：三个分组 + 不做清单的行首，够看但不太长。 */
export function renderAgentsBlock(caps) {
  const read = caps.filter((c) => c.kind === 'read').map((c) => c.name)
  const act = caps.filter((c) => c.kind === 'act').map((c) => c.name)
  const both = caps.filter((c) => c.kind === 'both').map((c) => c.name)
  const L = []
  L.push(CAPS_BEGIN)
  L.push('')
  L.push(`**只读（${read.length}）**：${read.join(' ')}`)
  L.push('')
  L.push(`**动作（${act.length}）**：${act.join(' ')}`)
  L.push('')
  L.push(`**看参数（${both.length}）**：${both.join(' ')}`)
  L.push('')
  L.push('**明确做不到**：')
  for (const n of NOT_SUPPORTED) L.push(`- ${n.what}`)
  L.push('')
  L.push('完整表（含每个只读命令返回哪些字段）见 `docs/capabilities-manifest.md`。')
  L.push('')
  L.push(CAPS_END)
  return L.join('\n')
}

async function main() {
  const [cs, bridge] = await Promise.all([readFile(CS, 'utf8'), readFile(BRIDGE, 'utf8')])
  const caps = extractCapabilities(cs, bridge)
  const manifest = renderManifest(caps)
  const block = renderAgentsBlock(caps)
  const agents = await readFile(AGENTS, 'utf8')
  const a = agents.indexOf(CAPS_BEGIN)
  const b = agents.indexOf(CAPS_END)
  let nextAgents
  if (a >= 0 && b > a) {
    nextAgents = agents.slice(0, a) + block + agents.slice(b + CAPS_END.length)
  } else {
    nextAgents = agents.replace(/\s*$/, '\n') + '\n' + block + '\n'
  }
  const json = JSON.stringify(
    {
      commands: caps.map((c) => ({
        name: c.name,
        kind: c.kind,
        params: c.params,
        returns: c.returns,
        summary: c.summary,
      })),
      notSupported: NOT_SUPPORTED,
    },
    null,
    2,
  ) + '\n'
  const check = process.argv.includes('--check')
  if (check) {
    const cur = await readFile(OUT, 'utf8').catch(() => '')
    const curJson = await readFile(OUT_JSON, 'utf8').catch(() => '')
    const problems = []
    if (cur !== manifest) problems.push('docs/capabilities-manifest.md 过期')
    if (curJson !== json) problems.push('docs/capabilities.json 过期')
    if (nextAgents !== agents) problems.push('AGENTS.md 的能力块过期')
    if (problems.length) {
      console.error(problems.join('；') + '\n跑 node scripts/kb-caps.mjs')
      process.exit(1)
    }
    const read = caps.filter((c) => c.kind !== 'act').length
    const act = caps.filter((c) => c.kind !== 'read').length
    console.log(`ok: ${caps.length} commands（读 ${read} / 动作 ${act}）`)
    return
  }
  await writeFile(OUT, manifest)
  await writeFile(OUT_JSON, json)
  if (nextAgents !== agents) await writeFile(AGENTS, nextAgents)
  console.log(`wrote ${OUT}（${caps.length} commands）`)
}

if (process.argv[1] && process.argv[1].replace(/\\/g, '/').endsWith('scripts/kb-caps.mjs')) {
  main().catch((e) => {
    console.error(e)
    process.exit(1)
  })
}