#!/usr/bin/env node
/**
 * 从代码生成 docs/kb/_generated-commands.md。
 * 目的：命令表永远不会和实现漂移（README 里写了"不要手改"）。
 *
 * 用法：node scripts/kb-sync.mjs [--check]
 *   --check  只校验生成物是否与代码一致，不写文件（CI / 测试用）
 */

import { readFile, writeFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')
const CS = join(ROOT, 'DshTerrariaBridge', 'Commands.cs')
const INDEX = join(ROOT, 'index.js')
const OUT = join(ROOT, 'docs', 'kb', '_generated-commands.md')

/**
 * 数一行代码里的花括号增减量。
 * 必须先把字符串字面量和注释抠掉，否则 `"{}"`、`"{{"` 这种会把配对算歪
 * （Mine 函数就是因为一个不平衡的字面量把整个类都吞进来了）。
 */
export function braceDelta(line) {
  let s = ''
  let i = 0
  while (i < line.length) {
    const c = line[i]
    if (c === '/' && line[i + 1] === '/') break
    if (c === '"' || c === "'") {
      const quote = c
      i++
      while (i < line.length) {
        if (line[i] === '\\') { i += 2; continue }
        if (line[i] === quote) { i++; break }
        i++
      }
      continue
    }
    s += c
    i++
  }
  let d = 0
  for (const ch of s) {
    if (ch === '{') d++
    else if (ch === '}') d--
  }
  return d
}

/**
 * 从 Commands.cs 抽命令表。
 * 参数通常不写在 switch 的 case 里（case 只是 `return Mine(p);`），而是在被调函数里，
 * 所以先记 case→函数名，再去函数体里抓 `Json.*(p, "key")`。
 */
export function extractCommands(cs) {
  const lines = cs.split('\n')

  // 函数起始行：private/public static string SomeCmd(JsonElement p)
  const funcStart = new Map()
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i].match(/(?:private|internal|public)\s+static\s+string\s+([A-Za-z_]\w*)\s*\(\s*JsonElement\s+\w+\s*\)/)
    if (m) funcStart.set(m[1], i)
  }
  const funcBody = (fn) => {
    const start = funcStart.get(fn)
    if (start == null) return ''
    // 从签名行往后找第一个 '{'，再从那里按花括号配对，遇到配平就停。
    // （签名可能是多行的；绝不能把后面的函数一起吞进来。）
    let i = start
    while (i < lines.length && !lines[i].includes('{') && i < start + 6) i++
    if (i >= lines.length) return ''
    let depth = 0, body = ''
    for (; i < lines.length; i++) {
      depth += braceDelta(lines[i])
      body += lines[i] + '\n'
      if (depth <= 0) break
    }
    return body
  }
  const paramsOf = (text) =>
    [...new Set([...text.matchAll(/Json\.\w+\(\s*p\s*,\s*"([^"]+)"/g)].map((x) => x[1]))]

  const out = []
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i].match(/case\s+"([a-z_]+)"\s*:/)
    if (!m) continue
    const name = m[1]
    let body = ''
    for (let j = i; j < lines.length; j++) {
      if (j > i && /case\s+"[a-z_]+"\s*:/.test(lines[j])) break
      if (j > i && /^\s*default\s*:/.test(lines[j])) break
      body += lines[j] + '\n'
    }
    let params = paramsOf(body)
    if (!params.length) {
      const fn = body.match(/return\s+([A-Za-z_]\w*)\s*\(/)
      if (fn) params = paramsOf(funcBody(fn[1]))
    }
    out.push({ name, params })
  }
  return out.sort((a, b) => a.name.localeCompare(b.name))
}

/** 从 index.js 抽工具名 + 描述。 */
export function extractTools(indexJs) {
  const out = []
  const re = /name:\s*'([a-z_]+)'\s*,\s*description:\s*\n?\s*'((?:[^'\\]|\\.)*)'/g
  for (const m of indexJs.matchAll(re)) {
    out.push({ name: m[1], description: m[2].replace(/\\'/g, "'") })
  }
  return out.sort((a, b) => a.name.localeCompare(b.name))
}

export function render(commands, tools) {
  const lines = []
  lines.push('# 生成物：mod 命令与插件工具清单')
  lines.push('')
  lines.push('> 本文件由 `node scripts/kb-sync.mjs` 从 `DshTerrariaBridge/Commands.cs` 与')
  lines.push('> `index.js` 生成。**不要手改**；要改就改代码再重新生成。')
  lines.push('')
  lines.push(`## mod 桥命令（${commands.length} 个）`)
  lines.push('')
  lines.push('| 命令 | 参数 |')
  lines.push('|---|---|')
  for (const c of commands) {
    lines.push(`| \`${c.name}\` | ${c.params.length ? c.params.map((p) => '`' + p + '`').join(' ') : '—'} |`)
  }
  lines.push('')
  lines.push(`## DSH 插件工具（${tools.length} 个）`)
  lines.push('')
  lines.push('| 工具 | 作用 |')
  lines.push('|---|---|')
  for (const t of tools) {
    lines.push(`| \`${t.name}\` | ${t.description.split('\n')[0].slice(0, 120)} |`)
  }
  lines.push('')
  return lines.join('\n')
}

async function main() {
  const [cs, index] = await Promise.all([readFile(CS, 'utf8'), readFile(INDEX, 'utf8')])
  const commands = extractCommands(cs)
  const tools = extractTools(index)
  const text = render(commands, tools)
  const check = process.argv.includes('--check')
  if (check) {
    const current = await readFile(OUT, 'utf8').catch(() => '')
    if (current !== text) {
      console.error('docs/kb/_generated-commands.md 与代码不一致，跑 node scripts/kb-sync.mjs')
      process.exit(1)
    }
    console.log(`ok: ${commands.length} commands / ${tools.length} tools`)
    return
  }
  await writeFile(OUT, text)
  console.log(`wrote ${OUT} (${commands.length} commands, ${tools.length} tools)`)
}

if (process.argv[1] && process.argv[1].replace(/\\/g, '/').endsWith('scripts/kb-sync.mjs')) {
  main().catch((e) => {
    console.error(e)
    process.exit(1)
  })
}