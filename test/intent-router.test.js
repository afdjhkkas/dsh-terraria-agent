import test from 'node:test'
import assert from 'node:assert/strict'
import {
  TAGS,
  LIBRARIES,
  ALWAYS,
  CO_LOAD,
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
test('命令回包 ok 但行为不对 → 命中 atomic_action（AGENTS.md 7.5 报文坑）', () => {
  // 真实踩过的坑：客户端把参数平铺在 root 上，Commands.Handle 只读 root.params，
  // p 变成 default，于是 move 往右走、guard 变成再开一次、walkto 坐标全丢。
  // 现象都是"回包 ok，行为完全不对"，这类原话必须能命中原语库。
  for (const q of [
    '命令返回 ok 但角色一动不动，为什么',
    '指令没生效，是不是报文写错了',
    '参数没生效，回了 ok 却没反应',
  ]) {
    const r = route(q)
    assert.ok(r.tags.includes('atomic_action'), `${q} 应该命中 atomic_action，实际 ${r.tags.join(',')}`)
  }
})

test('真实输入/按键层审计 → 命中 atomic_action', () => {
  for (const q of [
    '所有动作都必须是真实输入',
    '跳跃键要从原生输入层发出去',
    '右键交互后要回读确认',
  ]) {
    const r = route(q)
    assert.ok(r.tags.includes('atomic_action'), `${q} 应该命中 atomic_action，实际 ${r.tags.join(',')}`)
  }
})

test('一次任务的真实原话必须命中"动手要用的那几个库"（漏了就白干一轮）', () => {
  // 2026-10-27 实测十句真实任务原话，三句明显漏：挖铁没命中装备线、水下没命中世界规则、
  // 砍树没命中物品库。这些都是"知道有这条知识、但检索没到"，等于没写。
  // 每条都要给出**必须命中**的最小集合（只要求关键的那一两个，不要求全集）。
  const cases = [
    ['挖点铁做镐子', ['gear_route', 'loot_source']],
    ['铁镐要怎么升级', ['gear_route']],
    ['水下面怎么过去', ['world_biome']],
    ['快淹死了怎么办', ['world_biome']],
    ['帮我砍树', ['item_operation']],
    ['砍树要用什么', ['item_operation']],
    ['晚上能不能出去', ['world_biome']],
    ['天黑了外面危险吗', ['world_biome']],
    ['遇到僵尸打不过怎么办', ['combat_math']],
    ['被怪秒了怎么回事', ['combat_math']],
  ]
  const bad = []
  for (const [q, need] of cases) {
    const { tags } = route(q)
    const miss = need.filter((t) => !tags.includes(t))
    if (miss.length) bad.push(`「${q}」漏 ${miss.join(',')}（实际 ${tags.join(',')}）`)
  }
  assert.deepEqual(bad, [], '这些原话检索不到对应库：\n' + bad.join('\n'))
})

// ============================================================================
// 共现必带（用户 m20028 的"制作可疑眼球"暴露出分数闸门会把配套库筛掉）
// ============================================================================

test('要打首领时必须连带加载材料来源与合成操作', () => {
  const r = route('制作可疑眼球 召唤克苏鲁之眼')
  assert.ok(r.tags.includes('progression_boss'), '配方库必须有')
  assert.ok(r.tags.includes('loot_source'), '材料从哪来的库必须一起加载')
  assert.ok(r.tags.includes('item_operation'), '怎么把它做出来的库必须一起加载')
})

test('要盖房时必须连带加载肢体动作库', () => {
  const r = route('盖一间 npc 房子')
  assert.ok(r.tags.includes('build_standard'))
  assert.ok(r.tags.includes('atomic_action'), '放置/量尺寸属于动作原语，必须一起加载')
})

test('CO_LOAD 只引用真实存在的库', () => {
  for (const [base, extras] of Object.entries(CO_LOAD)) {
    assert.ok(LIBRARIES[base], `未知基础库 ${base}`)
    for (const e of extras) assert.ok(LIBRARIES[e], `${base} 引用了未知库 ${e}`)
  }
})
