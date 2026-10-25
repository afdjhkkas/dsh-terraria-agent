#!/usr/bin/env node
/**
 * 路由小工具。
 *
 *   node scripts/route.mjs --prompt                  打印前置分类器 Prompt（整段发给模型）
 *   node scripts/route.mjs "帮我造一间护士的房子"      离线关键词兜底会怎么判
 *   node scripts/route.mjs --tags "game_rule, 瞎写"   校验/规整一份标签串（分类器输出格式对不对）
 *   node scripts/route.mjs --json --tags ...         机器可读输出
 *
 * 注意：真正判标签的是 prompts/intent-router.md（模型跑）；这里的 --tags 只做校验，
 * 而 "句子 → 标签" 那条路是没有模型时的兜底，不要拿它当分类器用。
 */

import { pathToFileURL } from 'node:url'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const HERE = dirname(fileURLToPath(import.meta.url))
const { route, normalizeTags, ROUTER_PROMPT } = await import(
  pathToFileURL(join(HERE, '..', 'src', 'kb-router.js')).href
)

const argv = process.argv.slice(2)
const json = argv.includes('--json')
const rest = argv.filter((a) => a !== '--json')

if (rest[0] === '--prompt') {
  process.stdout.write(ROUTER_PROMPT)
} else if (rest[0] === '--tags') {
  const raw = rest.slice(1).join(' ')
  const { tags, unknown } = normalizeTags(raw)
  if (json) console.log(JSON.stringify({ tags, unknown }))
  else {
    console.log(tags.join(', '))
    if (unknown.length) console.error('忽略的非法标签: ' + unknown.join(', '))
    process.exit(unknown.length ? 1 : 0)
  }
} else {
  const text = rest.join(' ')
  if (!text) {
    console.error('用法: node scripts/route.mjs "<玩家原话>" | --prompt | --tags "<标签串>"')
    process.exit(2)
  }
  const { tags, scores, hits } = route(text)
  if (json) console.log(JSON.stringify({ tags, scores, hits }))
  else {
    console.log(tags.join(', '))
    const detail = tags
      .map((t) => `${t}(${scores[t] ?? 0}${hits[t]?.length ? ': ' + hits[t].join('/') : ''})`)
      .join('  ')
    console.error('依据: ' + detail)
  }
}