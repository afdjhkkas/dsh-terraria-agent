import test from 'node:test'
import assert from 'node:assert/strict'
import { mkdtemp, writeFile, readFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { execFileSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { dirname } from 'node:path'

import { route, load, buildContext, LIBRARIES, ALWAYS, KB_DIR } from '../src/kb-router.js'
import { extractCommands, extractTools, braceDelta } from '../scripts/kb-sync.mjs'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')

test('六个标签都有库文件，且文件真实存在', async () => {
  assert.deepEqual(Object.keys(LIBRARIES).sort(), [
    'actor_interact',
    'build_plan',
    'chat_correct',
    'game_rule',
    'safety_limit',
    'tool_command',
  ])
  const docs = await load(Object.keys(LIBRARIES))
  for (const d of docs) {
    assert.ok(d.text.length > 200, `${d.file} 内容太少`)
    assert.match(d.text, /^#/m, `${d.file} 缺少标题`)
  }
})

test('safety_limit 永远在最前面，且不参与关键词判定', () => {
  for (const text of ['把家完善一下', '为什么房间不合法', 'mine 怎么填参数', '你错了别在出生点挖']) {
    const { tags } = route(text)
    assert.equal(tags[0], 'safety_limit', `"${text}" 没有先带安全库`)
    assert.equal(tags.filter((t) => t === 'safety_limit').length, 1)
  }
  assert.deepEqual(ALWAYS, ['safety_limit'])
})

test('造东西的话命中 build_plan', () => {
  for (const text of ['把家完善一下', '帮我盖个房子', '搭个战斗平台', '挖一条竖井']) {
    const { tags } = route(text)
    assert.ok(tags.includes('build_plan'), `"${text}" 没命中 build_plan: ${tags}`)
  }
})

test('问机制的话命中 game_rule', () => {
  for (const text of ['为什么房屋不合法', '刷怪机制是怎样的', '水能不能被方块挤出']) {
    const { tags } = route(text)
    assert.ok(tags.includes('game_rule'), `"${text}" 没命中 game_rule: ${tags}`)
  }
})

test('问命令/参数的话命中 tool_command', () => {
  for (const text of ['mine 命令参数怎么填', '这个报错什么意思', '哪个工具能查方块属性']) {
    const { tags } = route(text)
    assert.ok(tags.includes('tool_command'), `"${text}" 没命中 tool_command: ${tags}`)
  }
})

test('纠正语气命中 chat_correct', () => {
  for (const text of ['你错了', '不是这样,别在出生点挖', '你又挖穿了']) {
    const { tags } = route(text)
    assert.ok(tags.includes('chat_correct'), `"${text}" 没命中 chat_correct: ${tags}`)
  }
})

test('普通过日子的话（走路/打怪）命中 actor_interact', () => {
  for (const text of ['走过去打那只史莱姆', '把地上的东西捡起来']) {
    const { tags } = route(text)
    assert.ok(tags.includes('actor_interact'), `"${text}" 没命中 actor_interact: ${tags}`)
  }
})

test('判定结果可解释：返回命中的词', () => {
  const { hits, scores } = route('为什么房屋不合法')
  assert.ok(hits.game_rule.includes('为什么'))
  assert.ok(scores.game_rule > 0)
})

test('load 只读命中的库，不读别的', async () => {
  const dir = await mkdtemp(join(tmpdir(), 'kb-'))
  for (const f of Object.values(LIBRARIES)) await writeFile(join(dir, f), `# ${f}\n内容内容内容`)
  const docs = await load(['safety_limit', 'build_plan'], { dir })
  assert.deepEqual(docs.map((d) => d.tag), ['safety_limit', 'build_plan'])
})

test('未知标签直接报错，不静默吞掉', async () => {
  await assert.rejects(() => load(['nope']), /未知知识库标签/)
})

test('buildContext：一句话拿到安全库 + 命中库', async () => {
  const ctx = await buildContext('把家完善一下')
  assert.equal(ctx.tags[0], 'safety_limit')
  assert.ok(ctx.tags.includes('build_plan'))
  assert.ok(ctx.docs.length === ctx.tags.length)
  assert.ok(ctx.docs.every((d) => d.text.length > 100))
})

test('braceDelta 不被字符串里的花括号骗到', () => {
  assert.equal(braceDelta('if (x) { return; }'), 0)
  assert.equal(braceDelta('var s = "{{";'), 0)
  assert.equal(braceDelta('Json.Error($"bounds: {x},{y}");'), 0)
  assert.equal(braceDelta('}'), -1)
  assert.equal(braceDelta('{'), 1)
})

test('从 Commands.cs 抽出的命令表包含关键命令且参数正确', async () => {
  const cs = await readFile(join(ROOT, 'DshTerrariaBridge', 'Commands.cs'), 'utf8')
  const cmds = extractCommands(cs)
  const byName = new Map(cmds.map((c) => [c.name, c.params]))
  for (const name of ['mine', 'place', 'state', 'scan', 'homeroom', 'roomcheck']) {
    assert.ok(byName.has(name), `命令表里缺 ${name}`)
  }
  assert.deepEqual(byName.get('place').sort(), ['ms', 'type', 'x', 'y'])
  assert.deepEqual(byName.get('homeroom').sort(), ['who', 'x', 'y'])
  assert.ok(cmds.length >= 50, `命令数量异常: ${cmds.length}`)
})

test('从 index.js 抽工具表', async () => {
  const idx = await readFile(join(ROOT, 'index.js'), 'utf8')
  const tools = extractTools(idx)
  assert.ok(tools.length >= 20, `工具数量异常: ${tools.length}`)
  assert.ok(tools.some((t) => t.name === 'terraria_bridge'))
})

test('生成物与代码一致（忘了跑 kb-sync 就会红）', () => {
  const out = execFileSync(process.execPath, [join(ROOT, 'scripts', 'kb-sync.mjs'), '--check'], {
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  })
  assert.match(out, /ok:/)
})

test('KB_DIR 指向 docs/kb', () => {
  assert.match(KB_DIR.replace(/\\/g, '/'), /docs\/kb$/)
})