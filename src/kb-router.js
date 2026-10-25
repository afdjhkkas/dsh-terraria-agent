/**
 * 知识库路由：把玩家一句话判成 1~N 个标签，只取对应的库文件。
 *
 * 设计约束（来自用户要求）：
 *  - 收到指令 → 先判标签 → 只检索命中标签下的内容，不混无关资料；
 *  - safety_limit 永远加载，不参与判定；
 *  - 六个库只收"换张地图也成立"的通用机制；命令/参数/报错这类接口文档
 *    在 docs/kb/_generated-commands.md 里，由代码生成，不进七个库；
 *  - 判定必须可解释（返回命中的证据词），不能是黑盒。
 */

import { readFileSync } from 'node:fs'
import { readFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const HERE = dirname(fileURLToPath(import.meta.url))
export const KB_DIR = join(HERE, '..', 'docs', 'kb')

/** 标签 → 库文件。顺序即加载优先级。 */
export const LIBRARIES = {
  safety_limit: 'safety-limit.md',
  game_rule: 'game-rule.md',
  build_standard: 'build-standard.md',
  actor_behavior: 'actor-behavior.md',
  atomic_action: 'atomic-action.md',
  item_operation: 'item-operation.md',
  task_correct: 'task-correct.md',
}

/** 永远加载，不参与判定。 */
export const ALWAYS = ['safety_limit']

/** 合法标签（顺序即加载顺序）。 */
export const TAGS = Object.keys(LIBRARIES)

/** 前置分类器 Prompt 的位置：玩家发话先跑它，拿到标签再查库。 */
export const ROUTER_PROMPT_PATH = join(HERE, '..', 'prompts', 'intent-router.md')

let promptCache = null

/**
 * Prompt 正文。惰性读盘：只有真正用到分类器时才碰这个文件，
 * 插件在没带 docs/prompts 的环境里加载也不会因为缺文件而崩。
 */
export function loadRouterPrompt() {
  if (promptCache == null) promptCache = readFileSync(ROUTER_PROMPT_PATH, 'utf8')
  return promptCache
}

/**
 * 把分类器（或人）给出的原始标签串规整成合法标签数组：
 * 容忍中英文逗号、顿号、空格、换行、多余引号；丢掉非法标签；去重；按加载顺序排序。
 * @param {string|string[]} raw
 * @returns {{tags: string[], unknown: string[]}}
 */
export function normalizeTags(raw) {
  const list = Array.isArray(raw) ? raw : String(raw ?? '').split(/[,，、\s]+/)
  const seen = new Set()
  const unknown = []
  for (const item of list) {
    const tag = String(item).trim().replace(/^[`'"“”]+|[`'"“”]+$/g, '')
    if (!tag) continue
    if (!LIBRARIES[tag]) {
      unknown.push(tag)
      continue
    }
    seen.add(tag)
  }
  const tags = TAGS.filter((t) => seen.has(t))
  return { tags, unknown }
}

/**
 * 关键词表。一个词命中即算该标签打一分；分数高者优先，同分按 LIBRARIES 顺序。
 * 刻意写得不像"自然语言分类器"——关键词就是可审计的规则。
 */
const KEYWORDS = {
  game_rule: [
    '为什么', '能不能', '可不可以', '规则', '机制', '判定', '算不算', '有效', '无效',
    '刷怪', '刷新', '生成', '扩散', '腐化', '猩红', '神圣', '房屋', '房间', '入住',
    '掉落', '概率', '硬度', '属性', '液体', '溺水', '岩浆', '昼夜', '事件', 'boss',
  ],
  item_operation: [
    '镐', '斧', '锤', '物品', '东西', '道具', '工具', '装备', '武器', '弓', '箭',
    '弹药', '药水', '抓钩', '钩爪', '翅膀', '坐骑', '召唤', '背包', '切换', '手持',
    '分类', '怎么用', '用法', '能干什么', '合成材料',
  ],
  build_standard: [
    '建', '造', '盖', '搭', '房', '屋', '装修', '完善', '平台', '农场', '刷怪场',
    '竖井', '隧道', '通道', '桥', '楼梯', '布局', '墙', '屋顶', '地板', '改造', '地形',
  ],
  actor_behavior: [
    '走', '跑', '跳', '挖', '砍', '打', '攻击', '射', '放', '捡', '拾取', '喝',
    '说话', '对话', '买', '卖', '商店', '爬', '移动', '撤退', '躲',
  ],
  atomic_action: [
    '原子动作', '动作', '操作步骤', '先后顺序', '先做什么', '执行', '判定', '算不算成功',
    '重试', '失败原因', '为什么放不', '为什么挖不', '放不上', '挖不动', '没反应',
    '批量', '逐格', '一格一格', '锚点', '支撑', '选中', '切换物品', '卡住', '回读',
    '放歪', '掉不下来', '捡不到', '静默失败',
  ],
  task_correct: [
    '不是', '别', '不要', '错了', '又', '听不懂', '我说过', '上次', '纠正', '重新',
    '模糊', '什么意思', '你是傻', '废话', '没做完', '不对',
  ],
}

const SCORE_STRONG = 3 // 明确的技术词
const SCORE_WEAK = 1 // 泛用动词（"打""走""建"这种，容易误判）

const STRONG = new Set([
  ...KEYWORDS.game_rule,
  ...KEYWORDS.task_correct,
  ...KEYWORDS.atomic_action,
  '镐', '斧', '锤', '物品', '道具', '工具', '装备', '武器', '弓', '箭', '弹药',
  '药水', '抓钩', '钩爪', '翅膀', '坐骑', '背包', '分类', '怎么用', '用法', '能干什么',
])

/**
 * 判标签。
 * @param {string} text 玩家原话
 * @returns {{tags: string[], scores: Record<string, number>, hits: Record<string, string[]>}}
 */
export function route(text) {
  const t = String(text ?? '')
  const scores = {}
  const hits = {}
  for (const [tag, words] of Object.entries(KEYWORDS)) {
    for (const w of words) {
      if (!t.includes(w)) continue
      scores[tag] = (scores[tag] ?? 0) + (STRONG.has(w) ? SCORE_STRONG : SCORE_WEAK)
      ;(hits[tag] ??= []).push(w)
    }
  }
  const ranked = Object.keys(scores).sort((a, b) => scores[b] - scores[a] || order(a) - order(b))
  // 保留与最高分同档次（≥50%）的标签；玩家纠错时 task_correct 一定进前列
  const top = ranked.length ? scores[ranked[0]] : 0
  const tags = ranked.filter((tag) => scores[tag] >= top * 0.5)
  // 有纠正语气时，纠正优先于其他一切（用户纠错必须被当成最高优先级）
  const corrected = tags.includes('task_correct')
    ? ['task_correct', ...tags.filter((x) => x !== 'task_correct')]
    : tags
  const ordered = ALWAYS.concat(corrected.filter((x) => !ALWAYS.includes(x)))
  return { tags: ordered, scores, hits }
}

function order(tag) {
  return Object.keys(LIBRARIES).indexOf(tag)
}

/**
 * 按标签读库文件内容。只读命中的那些，其余不读。
 * @param {string[]} tags
 * @param {{dir?: string}} [opts]
 * @returns {Promise<{tag: string, file: string, text: string}[]>}
 */
export async function load(tags, opts = {}) {
  const dir = opts.dir ?? KB_DIR
  const out = []
  for (const tag of tags) {
    const file = LIBRARIES[tag]
    if (!file) throw new Error(`未知知识库标签: ${tag}`)
    out.push({ tag, file, text: await readFile(join(dir, file), 'utf8') })
  }
  return out
}

/**
 * 给一句话准备上下文：永远带 safety_limit，再带命中的库。
 * @param {string} text
 * @param {{dir?: string}} [opts]
 */
export async function buildContext(text, opts = {}) {
  // 传了 tags 就用传进来的（前置分类器的结果），不再自己拿关键词猜。
  if (opts.tags) {
    const { tags, unknown } = normalizeTags(opts.tags)
    const docs = await load(tags, opts)
    return { tags, unknown, scores: {}, hits: {}, docs }
  }
  const { tags, scores, hits } = route(text)
  const docs = await load(tags, opts)
  return { tags, scores, hits, docs }
}