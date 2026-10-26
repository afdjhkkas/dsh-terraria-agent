import test from 'node:test'
import assert from 'node:assert/strict'
import {
  TAGS,
  LIBRARIES,
  ALWAYS,
  loadRouterPrompt,
  ROUTER_PROMPT_PATH,
  normalizeTags,
  route,
} from '../src/kb-router.js'

const ROUTER_PROMPT = loadRouterPrompt()

/** 从 Prompt 的「示例」段抽出 [玩家原话, 标签串] 对。 */
function promptExamples() {
  const pairs = []
  const re = /\*\*玩家\*\*：(.+)\r?\n\*\*输出\*\*：(.+)/g
  let m
  while ((m = re.exec(ROUTER_PROMPT))) pairs.push([m[1].trim(), m[2].trim()])
  return pairs
}

/** 从 Prompt 末尾的「输出示例（供校验）」表抽出 [玩家原话, 标签数组] 对。 */
function expectedRows() {
  const rows = []
  const section = ROUTER_PROMPT.split('## 输出示例（供校验）')[1] ?? ''
  for (const line of section.split('\n')) {
    const m = /^\| ([^|]+) \| (.+) \|$/.exec(line.trim())
    if (!m) continue
    const text = m[1].trim()
    if (text === '玩家原话' || /^-+$/.test(text)) continue
    const tags = [...m[2].matchAll(/`([a-z_]+)`/g)].map((x) => x[1])
    rows.push([text, tags])
  }
  return rows
}

test('分类器 Prompt 存在且覆盖全部十三个标签', () => {
  assert.ok(ROUTER_PROMPT_PATH.endsWith('intent-router.md'))
  for (const tag of Object.keys(LIBRARIES)) {
    assert.match(ROUTER_PROMPT, new RegExp('`' + tag + '`'), `Prompt 里缺标签 ${tag}`)
  }
  assert.equal(TAGS.length, 13)
})

test('Prompt 咬死“只输出标签”和固定顺序', () => {
  assert.match(ROUTER_PROMPT, /只输出标签，不要解释/)
  const order = TAGS.join('` → `')
  assert.ok(
    ROUTER_PROMPT.includes(order),
    `Prompt 里的输出顺序必须与加载顺序一致：${TAGS.join(' → ')}`,
  )
})

test('Prompt 里的每个示例标签都合法', () => {
  const pairs = promptExamples()
  assert.ok(pairs.length >= 8, `示例太少：${pairs.length}`)
  for (const [text, raw] of pairs) {
    const { tags, unknown } = normalizeTags(raw)
    assert.deepEqual(unknown, [], `示例「${text}」里有非法标签：${unknown.join(',')}`)
    assert.ok(tags.length >= 1, `示例「${text}」没给出标签`)
  }
})

test('Prompt 的示例由问题到标签一致可读（不含解释性文字）', () => {
  for (const [text, raw] of promptExamples()) {
    const { tags } = normalizeTags(raw)
    assert.equal(
      raw,
      tags.join(', '),
      `示例「${text}」的输出必须是规范格式 "a, b, c"，实际是 "${raw}"`,
    )
  }
})

test('用户给的样板例子：造护士的房子 → 五个标签', () => {
  const rows = expectedRows()
  const row = rows.find(([t]) => t.includes('护士'))
  assert.ok(row, '期望表里缺“护士的房子”这条样板')
  const [, tags] = row
  assert.deepEqual(new Set(tags), new Set([
    'game_rule',
    'item_operation',
    'build_standard',
    'atomic_action',
    'safety_limit',
  ]))
})

test('期望表里的每句话必须真的出现在 Prompt 示例段里', () => {
  const exampleTexts = promptExamples().map(([t]) => t)
  for (const [text] of expectedRows()) {
    assert.ok(
      exampleTexts.includes(text),
      `期望表里的「${text}」在示例段找不到（文档与校验表脱节）`,
    )
  }
})

test('离线兜底：每句话至少命中它的一个核心标签，且永远带 safety_limit', () => {
  const core = [
    ['帮我造一间护士的房子', 'build_standard'],
    ['为什么我建的房子 NPC 不住进去', 'game_rule'],
    ['把这把铁镐换成银镐，然后继续挖', 'item_operation'],
    ['往东走，路上有怪就打', 'actor_behavior'],
    ['你上次说我听不懂人话吗，我说的是铺背景墙不是挖', 'task_correct'],
    ['这个方块放上去没反应，怎么回事', 'atomic_action'],
    // m18346：房子类指令必须同时拿到"合格性判定"和"NPC 入住"，不能只剩建造手艺
    ['盖一间NPC房子', 'npc_town'],
    ['盖一间NPC房子', 'game_rule'],
    // m18346：自建房的进出方式（门/屋顶平台口），不许靠拆墙
    ['我被困在自己房子里出不去，要拆墙吗', 'build_standard'],
    ['屋顶留两格平台当出口', 'build_standard'],
  ]
  for (const [text, want] of core) {
    const { tags } = route(text)
    assert.ok(tags.includes(want), `「${text}」应命中 ${want}，实际 ${tags.join(',')}`)
    for (const a of ALWAYS) assert.ok(tags.includes(a), `「${text}」漏了 ${a}`)
  }
})

test('normalizeTags：去重、排序、丢非法、容忍中英文标点', () => {
  const r = normalizeTags('task_correct， atomic_action、game_rule  atomic_action `safety_limit`')
  assert.deepEqual(r.tags, [
    'safety_limit',
    'game_rule',
    'atomic_action',
    'task_correct',
  ])
  assert.deepEqual(r.unknown, [])

  const bad = normalizeTags('game_rule, 不存在的库, build_standard')
  assert.deepEqual(bad.tags, ['game_rule', 'build_standard'].sort((a, b) => TAGS.indexOf(a) - TAGS.indexOf(b)))
  assert.deepEqual(bad.unknown, ['不存在的库'])

  assert.deepEqual(normalizeTags(['actor_behavior', 'safety_limit']).tags, [
    'safety_limit',
    'actor_behavior',
  ])
  assert.deepEqual(normalizeTags('').tags, [])
})