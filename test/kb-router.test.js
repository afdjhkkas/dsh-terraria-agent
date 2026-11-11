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

test('十五个标签都有库文件，且文件真实存在', async () => {
  assert.deepEqual(Object.keys(LIBRARIES).sort(), [
    'abilities',
    'actor_behavior',
    'atomic_action',
    'build_standard',
    'combat_math',
    'game_rule',
    'gear_route',
    'item_operation',
    'loot_source',
    'npc_town',
    'progression_boss',
    'safety_limit',
    'skills',
    'task_correct',
    'world_biome',
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

test('造东西的话命中 build_standard', () => {
  for (const text of ['把家完善一下', '帮我盖个房子', '搭个战斗平台', '挖一条竖井']) {
    const { tags } = route(text)
    assert.ok(tags.includes('build_standard'), `"${text}" 没命中 build_standard: ${tags}`)
  }
})

test('问机制的话命中 game_rule', () => {
  for (const text of ['为什么房屋不合法', '刷怪机制是怎样的', '水能不能被方块挤出']) {
    const { tags } = route(text)
    assert.ok(tags.includes('game_rule'), `"${text}" 没命中 game_rule: ${tags}`)
  }
})

test('问物品用法的话命中 item_operation', () => {
  for (const text of ['镐能不能放方块', '抓钩怎么用', '药水有冷却吗', '锤子能改什么']) {
    const { tags } = route(text)
    assert.ok(tags.includes('item_operation'), `"${text}" 没命中 item_operation: ${tags}`)
  }
})

test('纠正语气命中 task_correct，且排在安全库之后第一位', () => {
  for (const text of ['你错了', '不是这样,别在出生点挖', '你又挖穿了']) {
    const { tags } = route(text)
    assert.ok(tags.includes('task_correct'), `"${text}" 没命中 task_correct: ${tags}`)
    assert.equal(tags[0], 'safety_limit')
    assert.equal(tags[1], 'task_correct', `"${text}" 纠错没有被优先: ${tags}`)
  }
})

test('普通过日子的话（走路/打怪）命中 actor_behavior', () => {
  for (const text of ['走过去打那只史莱姆', '把地上的东西捡起来']) {
    const { tags } = route(text)
    assert.ok(tags.includes('actor_behavior'), `"${text}" 没命中 actor_behavior: ${tags}`)
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
  const docs = await load(['safety_limit', 'build_standard'], { dir })
  assert.deepEqual(docs.map((d) => d.tag), ['safety_limit', 'build_standard'])
})

test('未知标签直接报错，不静默吞掉', async () => {
  await assert.rejects(() => load(['nope']), /未知知识库标签/)
})

test('buildContext：一句话拿到安全库 + 命中库', async () => {
  const ctx = await buildContext('把家完善一下')
  assert.equal(ctx.tags[0], 'safety_limit')
  assert.ok(ctx.tags.includes('build_standard'))
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
  // guard 是 mine/place/craft 共有的可选闸门参数（默认 false，不传就当没开），所以 place 的参数表里必须看得见它。
  assert.deepEqual(byName.get('place').sort(), ['guard', 'ms', 'type', 'x', 'y'])
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

// 用户 m05144 的硬规格：4 大主库 + 2 辅助库，每个库必须带指定子分类小节。
// 这条测试的作用：改库时不许把结构删掉（删了子分类 = 知识库退化成随笔）。
const REQUIRED_SECTIONS = {
  'game-rule.md': ['房屋判定规则', 'NPC 入住与生成规则', '怪物刷新规则', '物块与地形规则', '环境扩散规则', '世界全局机制'],
  'item-operation.md': ['物品分类与功能边界', '物品使用硬性约束', '背包与切换逻辑', '交互站位规则'],
  'build-standard.md': ['通用建造规范', 'NPC 房屋通用建造规范', '各类平台建造规范', '方块搭配通用规范', '地形改造规范'],
  'actor-behavior.md': ['移动规则', '战斗行为规则', '拾取规则', 'NPC 交互规则', '增益与召唤规则'],
  'atomic-action.md': [
    '动作执行基本链条', '动作判定条件', '动作先后顺序',
    '失败原因分类', '重试策略', '操作限制', '批量操作规范',
  ],
  'task-correct.md': ['模糊指令处理规则', '歧义处理规则', '任务中断规则', '修改迭代规则', '无法完成时怎么回'],
  'safety-limit.md': ['禁止行为清单', '操作范围限制', '自检修正规则'],
  'world-biome.md': ['世界分层', '生物群系怎么判定', '液体、窒息、坠落', '环境扩散', '昼夜、月相、天气', '地貌结构'],
  'progression-boss.md': ['进程是骨牌', '前困难模式首领', '困难模式首领', '祭坛', '打首领前的通用准备'],
  'combat-math.md': ['一次攻击的结算顺序', '暴击、击退、无敌帧', '伤害类型与穿透', '生命、魔力、召唤位', '减益', '幸运'],
  'loot-source.md': ['自然容器', '掉落规则', '钓鱼与板条箱', '首领与事件掉落', 'NPC 商店', '转化与回收'],
  'npc-town.md': ['NPC 入住的两个必要条件', '常见 NPC 的出现条件', '幸福度', '传送水晶', '交易与商店'],
  'gear-route.md': ['镐力门槛', '矿石阶梯', '护甲', '武器', '饰品与机动性', '词条', '增益药水'],
  'abilities.md': ['能力、当前状态与连续 profile', '能力门槛', '动作执行分工', '失效与重标', '运行时资料分层'],
}

test('每个库保留规格要求的子分类小节', async () => {
  for (const [file, sections] of Object.entries(REQUIRED_SECTIONS)) {
    const text = await readFile(join(KB_DIR, file), 'utf8')
    for (const s of sections) {
      assert.match(text, new RegExp(`^#{1,3}.*${s}`, 'm'), `${file} 缺少「${s}」小节`)
    }
  }
})

test('全部库不含单局测试数据（坐标/世界名/单局措辞/计时数值）', () => {
  const out = execFileSync(process.execPath, [join(ROOT, 'scripts', 'kb-lint.mjs')], {
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  })
  assert.match(out, /^ok:/m)
})

test('README 列出了全部标签且与 LIBRARIES 一致', async () => {
  const readme = await readFile(join(KB_DIR, 'README.md'), 'utf8')
  for (const tag of Object.keys(LIBRARIES)) {
    assert.ok(readme.includes(tag), `README 没提到标签 ${tag}`)
  }
  for (const file of Object.values(LIBRARIES)) {
    assert.ok(readme.includes(file), `README 没链接 ${file}`)
  }
})

test('库文件里没有"应该没问题/一般来说"这类空话', async () => {
  const docs = await load(Object.keys(LIBRARIES))
  for (const d of docs) {
    for (const bad of ['应该没问题', '一般来说', '大概率可以']) {
      assert.ok(!d.text.includes(bad), `${d.file} 含空话「${bad}」`)
    }
  }
})
