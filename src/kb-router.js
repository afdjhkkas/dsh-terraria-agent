/**
 * 知识库路由：把玩家一句话判成 1~N 个标签，只取对应的库文件。
 *
 * 设计约束（来自用户要求）：
 *  - 收到指令 → 先判标签 → 只检索命中标签下的内容，不混无关资料；
 *  - safety_limit 永远加载，不参与判定；
 *  - 判定必须可解释（返回命中的证据词），不能是黑盒。
 */

import { readFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const HERE = dirname(fileURLToPath(import.meta.url))
export const KB_DIR = join(HERE, '..', 'docs', 'kb')

/** 标签 → 库文件。顺序即加载优先级。 */
export const LIBRARIES = {
  safety_limit: 'safety-limit.md',
  game_rule: 'game-rule.md',
  build_plan: 'build-plan.md',
  actor_interact: 'actor-interact.md',
  tool_command: 'tool-command.md',
  chat_correct: 'chat-correct.md',
}

/** 永远加载，不参与判定。 */
export const ALWAYS = ['safety_limit']

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
  tool_command: [
    '命令', '指令', '工具', '参数', '坐标', '报错', '错误', '调用', '接口', 'api',
    'scan', 'tile', 'state', 'inventory', 'recipes', 'tiledata', 'find', 'bridge',
    '怎么填', '哪个工具', '语法',
  ],
  build_plan: [
    '建', '造', '盖', '搭', '房', '屋', '装修', '完善', '平台', '农场', '刷怪场',
    '竖井', '隧道', '通道', '桥', '楼梯', '布局', '墙', '屋顶', '地板', '改造',
  ],
  actor_interact: [
    '走', '跑', '跳', '挖', '砍', '打', '攻击', '射', '放', '捡', '拾取', '喝',
    '药', '说话', '对话', '买', '卖', '商店', '召唤', '坐骑', '飞行', '爬',
  ],
  chat_correct: [
    '不是', '别', '不要', '错了', '又', '听不懂', '我说过', '上次', '纠正', '重新',
    '模糊', '什么意思', '你是傻', '废话',
  ],
}

const SCORE_STRONG = 3 // 明确的技术词
const SCORE_WEAK = 1 // 泛用动词（"打""走"这种，容易误判）

const STRONG = new Set([
  ...KEYWORDS.game_rule.slice(0, 10), ...KEYWORDS.tool_command,
  ...KEYWORDS.chat_correct,
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
  // 有"纠正"语气时，纠正优先于其他一切（用户纠错必须被当成最高优先级）
  const tags = Object.keys(scores)
    .sort((a, b) => scores[b] - scores[a] || order(a) - order(b))
    .filter((tag, i) => i === 0 || scores[tag] >= scores[Object.keys(scores).sort((a, b) => scores[b] - scores[a])[0]] * 0.5)
  const ordered = ALWAYS.concat(tags.filter((x) => !ALWAYS.includes(x)))
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
  const { tags, scores, hits } = route(text)
  const docs = await load(tags, opts)
  return { tags, scores, hits, docs }
}