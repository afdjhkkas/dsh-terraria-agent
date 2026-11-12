/**
 * 知识库路由：把玩家一句话判成 1~N 个标签，只取对应的库文件。
 *
 * 设计约束（来自用户要求）：
 *  - 收到指令 → 先判标签 → 只检索命中标签下的内容，不混无关资料；
 *  - safety_limit 永远加载，不参与判定；
 *  - 每个库只收"换张地图也成立"的通用机制；命令/参数/报错这类接口文档
 *    在 docs/kb/_generated-commands.md 里，由代码生成，不进库；
 *  - 判定必须可解释（返回命中的证据词），不能是黑盒。
 */

import { readFileSync } from 'node:fs'
import { readFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'
import { classifyIntent } from './v2/intent-classifier.js'

const HERE = dirname(fileURLToPath(import.meta.url))
export const KB_DIR = join(HERE, '..', 'docs', 'kb')

/** 标签 → 库文件。顺序即加载优先级。 */
export const LIBRARIES = {
  safety_limit: 'safety-limit.md',
  game_rule: 'game-rule.md',
  world_biome: 'world-biome.md',
  progression_boss: 'progression-boss.md',
  gear_route: 'gear-route.md',
  abilities: 'abilities.md',
  combat_math: 'combat-math.md',
  loot_source: 'loot-source.md',
  npc_town: 'npc-town.md',
  build_standard: 'build-standard.md',
  actor_behavior: 'actor-behavior.md',
  atomic_action: 'atomic-action.md',
  item_operation: 'item-operation.md',
  task_correct: 'task-correct.md',
  skills: 'skills.md',
}

/** 永远加载，不参与判定。 */
export const ALWAYS = ['safety_limit']

/** 合法标签（顺序即加载顺序）。 */
export const TAGS = Object.keys(LIBRARIES)

/** 前置分类器 Prompt 的位置：玩家发话先跑它，拿到标签再查库。 */
export const ROUTER_PROMPT_PATH = join(HERE, '..', 'prompts', 'intent-router.md')

/** 单轮自路由的 system prompt。 */
export const SELF_ROUTE_PROMPT_PATH = join(HERE, '..', 'prompts', 'self-route.md')

let selfRouteCache = null

/** 单轮自路由 system prompt 正文（惰性读盘）。 */
export function loadSelfRoutePrompt() {
  if (selfRouteCache == null) selfRouteCache = readFileSync(SELF_ROUTE_PROMPT_PATH, 'utf8')
  return selfRouteCache
}

/** 模型自己写的那行标记。 */
export const NEED_MARKER = 'NEED_KNOWLEDGE'

/**
 * 从模型输出里抓 `NEED_KNOWLEDGE:a, b, c`。
 *
 * 容忍：大小写、前后空格、全角冒号、CRLF；标签行尾部跟着的标点会被截掉。
 * 不宽容：标记必须整行独立（行内乱入不算），抓不到就 found:false 让上层去兜底。
 *
 * @param {string} text 模型这一回合的输出
 * @returns {{found: boolean, tags: string[], unknown: string[], draft: string, markerFirst: boolean}}
 */
export function parseNeedKnowledge(text) {
  const s = String(text ?? '')
  const re = /(?:^|\n)[ \t]*NEED_KNOWLEDGE[ \t]*[:：][ \t]*([^\n]*)/i
  const m = re.exec(s)
  if (!m) {
    // 标记写在了行中间（例如先说了一句废话）：不算数，但要报 nearMiss，
    // 好让上层决定"重问一次"而不是静默退到关键词兜底。
    const nearMiss = new RegExp(NEED_MARKER, 'i').test(s)
    return { found: false, tags: [], unknown: [], draft: s.trim(), markerFirst: false, nearMiss }
  }
  // 标签行只取合法字符段，后面跟的中文说明/标点一律丢掉
  const rawTags = m[1].split(/[^A-Za-z0-9_,，、 ]/)[0]
  const { tags, unknown } = normalizeTags(rawTags)
  const draft = (s.slice(0, m.index) + s.slice(m.index + m[0].length)).trim()
  return { found: true, tags, unknown, draft, markerFirst: m.index === 0, nearMiss: false }
}

/**
 * 把检索到的知识拼成"续传提示"：追加到同一个会话里，让模型接着出动作方案。
 * @param {{tag: string, file: string, text: string}[]} docs
 * @param {{task?: string, ask?: string}} [opts]
 */
export function buildContinuation(docs, opts = {}) {
  const ask = opts.ask ?? '参考上面的知识，输出完整可执行动作序列。'
  const parts = ['【已按 NEED_KNOWLEDGE 标记检索到以下知识】']
  for (const d of docs) {
    parts.push('', `=== ${d.tag} · ${d.file} ===`, d.text.trim())
  }
  parts.push('', ask, '每一步写清：命令与参数、怎么回读确认、失败怎么改。')
  if (opts.task) parts.push('', `原任务：${opts.task}`)
  return parts.join('\n')
}

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
    // 房屋合格性属于游戏判定（面积/尺寸/光源/背景墙），不是建造手艺
    '盖房', '建房', '造房', '房子', '多大', '多少格', '面积', '尺寸', '多宽', '多高',
    '光源', '门放', '门怎么', '背墙', '背景墙', '最小规模',
    '掉落', '概率', '硬度', '属性', '液体', '溺水', '岩浆', '昼夜', '事件', 'boss',
  ],
  world_biome: [
    '在哪一层', '哪一层', '什么群系', '生物群系', '群系', '地形群系', '环境',
    '分层', '洞穴层', '地下层', '地表', '太空', '地狱', '海洋', '沙漠', '雪原',
    '丛林', '蘑菇地', '地牢', '浮空岛', '金字塔', '蜂巢', '月亮领主之前的世界',
    '水源', '血月', '日食', '沙尘暴', '暴雨', '天气', '月相', '深渊', '微光',
  , '水下', '水底', '水里', '淹', '溺水', '窒息', '氧气', '憋气', '天黑了', '晚上', '夜里', '天黑', '刷怪'],
  progression_boss: [
    '打谁', '先打', '下一个', '下一步做什么', '召唤', '召唤物', '祭坛', '恶魔祭坛',
    '猩红祭坛', '巫毒娃娃', '血肉墙', '困难模式', '机械', '三王', '世纪之花',
    '石巨人', '猪龙鱼', '光之女皇', '克苏鲁', '之眼', '蜂王', '骷髅王',
    '世界吞噬者', '克苏鲁之脑', '史莱姆王', '四柱', '月之领主', '通关', '进度',
    // 「打boss」这一类是最常见的提问方式，原先一个词都没覆盖到（route.mjs "打boss" 只命中 game_rule）
    '打boss', '打BOSS', 'boss', 'BOSS', '杀boss', '刷boss', '打个boss', 'boss顺序',
    '该打哪个', '先打哪个', '打哪个boss', '哪个boss', 'boss怎么打', 'boss打不过',
  ],
  combat_math: [
    '伤害', '打不动', '多少血', '掉血', '防御', '护甲值', '暴击', '穿透', '无敌帧',
    '击退', '减益', '中毒', '着火', '幸运', '生命上限', '魔力', '召唤位', '输出',
    '为什么打不', '秒伤', '真伤', '掉不动', '不动血', '打它都', '不破防',
  , '打不过', '干不过', '打不赢', '打不过它', '被秒', '一招就死', '血掉太快', '被秒', '秒了', '秒人', '一下就没血',
    'boss打不动', '打boss打不动', 'boss血太厚', '打boss刮痧'],
  loot_source: [
    '去哪找', '怎么得', '怎么拿', '从哪来', '刷什么', '刷钱', '宝箱', '金箱', '箱子',
    '开箱', '钥匙', '地牢箱', '暗影箱', '掉落表', '钓', '钓鱼', '板条箱', '渔获',
    '买', '商店', '售价', '幸福度', '卖钱', '提取机', '泥沙', '回收', '重铸',
    '铁矿', '矿脉', '搞点', '路子', '不想挖', '有没有别的', '替代', '别的办法',
    // 召唤物材料的取得链（晶状体/腐化材料这类"要攒"的东西）
    '晶状体', '召唤物材料', '材料哪里', '怎么攒', '要多少', '凑齐',
  , '铁', '铅', '银', '金矿', '找矿', '挖点'],
  item_operation: [
    '镐', '斧', '锤', '物品', '东西', '道具', '工具', '装备', '武器', '弓', '箭',
    '弹药', '药水', '抓钩', '钩爪', '翅膀', '坐骑', '召唤', '背包', '切换', '手持',
    '分类', '怎么用', '用法', '能干什么', '合成材料',
    '固定属性', '装备槽', 'headSlot', 'bodySlot', 'legSlot', 'wingSlot', 'mountType',
    'useAmmo', 'consumable', 'idmap', 'props', 'NPC字段', 'tileMerge',
    // 合成召唤物本身是"在原版合成界面里做一件物品"
    '可疑眼球', '合成召唤', '做召唤', '制作召唤',
  , '砍树', '伐木', '挖矿', '采矿', '挖铁', '挖石头', '锯'],
  build_standard: [
    '建', '造', '盖', '搭', '房', '屋', '装修', '完善', '平台', '农场', '刷怪场',
    '竖井', '隧道', '通道', '桥', '楼梯', '布局', '墙', '屋顶', '地板', '改造', '地形',
    // 尺寸 / 面积下限 / 房间多大（60 格那条线）
    '面积', '尺寸', '多大', '几格', '格数', '加宽', '加高', '扩', '宽', '高',
    // 自建房的进出方式（不拆墙）：门 / 屋顶平台口
    '出门', '出去', '进出', '被困', '困在', '被关', '拆墙', '拆房子', '平台口', '出口',
    '翻出去', '屋顶留', '封顶', '先封墙', '封口',
  ],
  actor_behavior: [
    '走', '跑', '跳', '挖', '砍', '打', '攻击', '射', '放', '捡', '拾取', '喝',
    '说话', '对话', '买', '卖', '商店', '爬', '爬绳', '按上键', '上方向键', '移动', '撤退', '躲',
    // 掉坑/卡死自救（2026-11-12：AI 在坑里空转一小时的教训）
    '自救', '出不来', '出不去', '卡住', '卡死', '掉坑', '坑里', '困住', '困在坑', '爬不出',
    // 找坑 / 读地形（同日：把树干当墙、把树干顶当地表、看不见脚边 1 格的沟）
    '找坑', '坑在哪', '哪有坑', '深坑', '陷阱', '落差', '读地形', '看地形',
  ],
  skills: [
    // 成组的可复用能力（怎么把一件事完整做出来）
    '技能', '技能清单', '会什么', '都能干什么', '怎么做', '怎么弄', '怎么整',
    '造房子', '建房子', '盖房子', '造一间', '建一间',
    '斜向上', '斜向上挖', '爬坡', '爬山', '往上爬', '向上挖',
    '砍树', '砍倒', '伐木', '树根',
    '穿平台', '平台下去', '平台上', '平台口', '跳上去', '跳下来',
    '整平', '铺墙', '背景墙', '放家具', '工作台', '椅子', '门装', '开门',
    // 位移之前的地形判断（2026-11-03 掉坑事故后补：不许长 ms 直线冲）
    '悬崖', '落差', '掉下去', '走空', '先判断', '地形判断', '移动前', '看不清路',
    '探路', '能不能走', '跳得过', '跳不过', '走之前', '前方地形',
    '掉下去了', '摔下去', '没判断', '没做判断', '不做判断', '冲过头', '走过头', '跳下去',
    // 探索推进（技能 #14，2026-11-04 补）
    // 用户说"探索"时没有命中 skills，导致没有加载探图循环步骤
    '探索', '探图', '往右探索', '往左探索', '往东探索', '往西探索',
    '一路向右', '一路向左', '路过', '遇到坑', '遇到箱子', '路上',
    '看到什么', '路边', '发现东西', '卡住了原因', '位移不变',
  ],
  atomic_action: [
    '原子动作', '动作', '操作步骤', '先后顺序', '先做什么', '执行', '判定', '算不算成功',
    '重试', '失败原因', '为什么放不', '为什么挖不', '放不上', '挖不动', '没反应',
    '批量', '逐格', '一格一格', '锚点', '支撑', '选中', '切换物品', '卡住', '回读',
    '放歪', '掉不下来', '捡不到', '静默失败',
    // 命令回了 ok 但行为完全不对（多半是报文/参数层出问题，见 AGENTS.md 7.5）
    '返回 ok', '回了 ok', '失败但不报错', '为什么不走', '为什么不跳', '指令没生效',
    '参数没生效', '报文', '协议格式', '两级结构', '真实输入', '原生输入', '真操作',
    '按键输入', '跳跃键', '右键交互', '输入层', 'humaninput.control', 'control位', '当帧控制位',
    'flagLeft', 'flagRight', 'flagUp', 'flagDown', 'flagJump', '脚底绳', '真实绳格', '脱绳', '挂绳',
    'GameViewMatrix', 'MouseWorld', '鼠标世界坐标', '瞄准坐标', '缩放逆变换', '抓钩坐标',
    '方向键输入', '按键输入', '上方向键', '向上按键', 'controlUp', '爬绳',
    // 假操作零容忍（AGENTS.md §0.55）—— 用户原话就这么说，必须能命中
    '假操作', '不是真操作', '作弊', '绕过工具', '凭空', '直接改世界', '写速度', '写坐标',
    'velocity', 'position 赋值', 'ItemCheck 结算', '真实物品流', '没掏镐', '没掏方块',
    // 失焦可用（AGENTS.md §0.56）—— 用户原话"失焦""后台""不点窗口"都要命中
    '失焦', '没有焦点', '无焦点', '失去焦点', 'hasFocus', '后台跑', '后台运行',
    '不切窗口', '不点亮窗口', '失焦也能', '窗口在后台', '别的窗口打字',
    '预开闸', '开闸', 'MountPreOpenUntil', 'preOpenTicks', 'releaseMount',
    '边沿输入', '刚按下', '按住不放', 'Triggers.Current', 'QuickMount',
  ],
  task_correct: [
    '不是', '别', '不要', '错了', '又', '听不懂', '我说过', '上次', '纠正', '重新',
    '更正', '观测错误', '误报', '废弃', '模糊', '什么意思', '你是傻', '废话', '没做完', '不对',
  ],
  npc_town: [
    'npc', 'npc 房', 'npc房', 'npc房子', 'npc 房子', 'NPC房屋', '房子', '住进来', '住进去',
    '不来', '入住', '幸福度', '好感', '价格贵',
    '卖贵', '便宜', '优惠', '商店', '货架', '传送水晶', 'pylon', '水晶塔', '渔夫',
    '旅商', '哥布林工匠', '电工妹', '护士', '商人', '树妖', '蒸汽朋克', '松露人',
    '税收官', '派对女孩', '骷髅商人', '解绑', '救人',
  ],
  gear_route: [
    '装备', '升级', '镐力', '挖不动', '矿石等级', '护甲', '套装', '头盔', '胸甲',
    '护腿', '武器线', '近战', '远程', '魔法', '召唤', '饰品', '词条', '重铸',
    '前缀', '机动性', '翅膀', '靴子', '二段跳', '进阶', '该做什么装备', '下一步装备',
  , '做镐', '做把镐', '铁镐', '铜镐', '升级镐', '做武器', '做防具', '穿什么', '装备路线'],
  abilities: [
    '能力', '能力指纹', '能力画像', '现在能做什么', '能不能飞', '飞行能力', '二段跳',
    '跳跃次数', '冲刺', '冲刺能力', '水上行走', '水上走', '岩浆行走', '火走', '免摔伤',
    '摔伤免疫', '抓钩能力', '照明能力', '光源能力', '生物探测', '危险探测', '宝藏探测',
    '可达性', '能跳过', '跳得过', '跳不过', '跨坑', '跳墙', '能到达', '闭环动作', '动作闭环',
    '动作标定', '重新标定', '重标', '失效', '移速加成', '跑速', '跳跃高度', '跳跃 profile',
    '移动 profile', 'mobility profile', 'maxRunSpeed', 'jumpSpeedBoost', 'doubleJumpAvailable',
    'canDash', 'canWalkOnWater', 'immuneFallDamage',
    // 用户实际问法补充（m25396 实测漏判，按 AGENTS.md 0.6 补命中）
    '能飞', '飞多高', '飞多久', '飞吗', '能不能飞起来', '这身装备', '装备之后',
    '装备变了', '数值变了', '数值变', '走路会变', '变快', '变慢', '饰品有什么用',
    '这件有什么用', '装备效果', '装备加成', '属性加成', '速度加成',
    // 坐骑与骑乘（用户 m40094 指出的缺口：新对话问"召唤坐骑"路由不到 abilities，
    // 而"快捷召唤必须走输入层、不能写 controlMount"这条只在 abilities.md 里）
    '坐骑', '骑坐骑', '骑上', '骑乘', '骑不骑', '召唤坐骑', '坐骑召唤', '快捷召唤',
    '下坐骑', '上坐骑', '骑着', '骑吗', '能骑', '不能骑', '骑上去', '召唤物',
    '怎么骑', '咋骑', '如何骑', '骑不上', '骑下不来', '骑不了',
    '矿车', 'mountType', 'miscEquips', '装备槽', '饰品槽',
  ],
}

const SCORE_STRONG = 3 // 明确的技术词
const SCORE_WEAK = 1 // 泛用动词（"打""走""建"这种，容易误判）

/**
 * 共现必带：某些库只要命中，就必须连带把配套的库一起加载。
 * 起因（用户 m20028 的"制作可疑眼球"）：分数闸门（≥最高分 50%）会把低分的配套库筛掉——
 * `progression_boss` 命中 9 分，`item_operation` 只有 3 分（"可疑眼球"是弱信号），
 * 于是"配方在库 A、材料来源在库 B、怎么合成在库 C"这件事只加载了 A，
 * 剩下的知识从头到尾不会出现。这和"检索上下文割裂"是同一类病。
 */
export const CO_LOAD = Object.freeze({
  // 要打某个首领 → 必然要凑召唤物材料（loot_source）+ 在原版合成界面把它做出来（item_operation）
  progression_boss: ['loot_source', 'item_operation'],
  // 要盖房/做建筑 → 必然要动身体（放方块、量尺寸）和知道原版放置规则
  build_standard: ['atomic_action'],
})

const STRONG = new Set([
  ...KEYWORDS.game_rule,
  // task_correct 里的'又'太泛（'又不想挖'也命中），只算弱信号
  ...KEYWORDS.task_correct.filter((w) => w !== '又'),
  ...KEYWORDS.atomic_action,
  ...KEYWORDS.world_biome,
  ...KEYWORDS.progression_boss,
  ...KEYWORDS.combat_math,
  // loot_source 里"买/卖/商店/幸福度"同时是 actor_behavior 的泛用词，只算弱信号
  ...KEYWORDS.loot_source.filter((w) => !['买', '卖', '商店', '幸福度'].includes(w)),
  ...KEYWORDS.npc_town.filter((w) => !['商店'].includes(w)),
  // gear_route 里"装备/武器/翅膀/靴子"与 item_operation、actor_behavior 重叠，只算弱信号
  ...KEYWORDS.gear_route.filter(
    (w) => !['装备', '武器线', '翅膀', '靴子', '召唤', '魔法', '远程', '近战'].includes(w),
  ),
  ...KEYWORDS.abilities,
  // 建造类明确词：房子/建/造/盖/搭 直接指向"怎么盖、要多大"，不是泛用动作
  '房', '房子', '建', '造', '盖', '搭', '屋',
  '镐', '斧', '锤', '物品', '道具', '工具', '装备', '武器', '弓', '箭', '弹药',
  '药水', '抓钩', '钩爪', '翅膀', '坐骑', '背包', '分类', '怎么用', '用法', '能干什么',
])

/**
 * v2 意图分类器的接线阈值：置信度低于它就完全不采信，走关键词老路。
 * 实测（59 句玩家原话）：v2 在"能不能飞/装备够不够"这类问题上会把 abilities
 * 换成 atomic_action+skills，所以这里**只做并集补齐、绝不替换**关键词的结论。
 */
export const V2_MIN_CONFIDENCE = 0.5

/**
 * 只跑 v2 意图分类，失败就返回 null（分类器不能把路由搞挂）。
 * @param {string} text
 */
function tryClassify(text) {
  try {
    return classifyIntent(text)
  } catch {
    return null
  }
}

/**
 * 纯关键词判定（原文逻辑，一行没动）。`route(text, {classifier:'keyword'})` 走这条。
 * @param {string} text 玩家原话
 * @returns {{tags: string[], scores: Record<string, number>, hits: Record<string, string[]>}}
 */
export function routeKeyword(text) {
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
  // 共现必带：把配套库补进来（分数闸门之后再补，否则会被 50% 规则筛掉）
  const expanded = []
  for (const tag of corrected) {
    if (!expanded.includes(tag)) expanded.push(tag)
    for (const extra of CO_LOAD[tag] ?? []) {
      if (!expanded.includes(extra)) expanded.push(extra)
    }
  }
  const ordered = ALWAYS.concat(expanded.filter((x) => !ALWAYS.includes(x)))
  return { tags: ordered, scores, hits }
}

/**
 * 判标签（对外入口）。
 *
 * v2 意图分类器只做**并集补齐**：关键词的结论原样保留，v2 的 requiredKB 只往上加。
 * 理由（实测 59 句玩家原话）：v2 会在「能飞吗 / 装备够了没」这类问题上把 abilities
 * 换成 atomic_action+skills，直接替换会丢正确库；而补齐不会让任何已有结论变差。
 *
 * @param {string} text 玩家原话
 * @param {{classifier?: 'keyword'|'merged', v2?: boolean}} [opts]
 *   - `classifier:'keyword'` 强制走老路（A/B 对比、排障用）
 *   - 默认 'merged'：v2 置信度 ≥ V2_MIN_CONFIDENCE 时补库，否则等同 keyword
 * @returns {{tags: string[], scores: Record<string, number>, hits: Record<string, string[]>,
 *            classifier: 'keyword'|'merged', classify?: object}}
 */
export function route(text, opts = {}) {
  const base = routeKeyword(text)
  const mode = opts.classifier ?? (opts.v2 === false ? 'keyword' : 'merged')
  if (mode === 'keyword') return { ...base, classifier: 'keyword' }

  const c = tryClassify(text)
  if (!c || typeof c.confidence !== 'number' || c.confidence < V2_MIN_CONFIDENCE) {
    return { ...base, classifier: 'keyword', classify: c ?? undefined }
  }

  // 只取 requiredKB（details[].required），optionalKB 一律不补 —— 控制加载量。
  const required = [...new Set((c.details ?? []).flatMap((d) => d.required ?? []))]
  const added = required.filter((kb) => LIBRARIES[kb] && !base.tags.includes(kb))
  if (!added.length) return { ...base, classifier: 'merged', classify: c }

  // 插在 safety_limit 之后：意图驱动的库排前面，但绝不动 ALWAYS；
  // 玩家纠错时 task_correct 必须仍是安全库之后第一位（硬规则），v2 的库只能排在它后面。
  const head = base.tags.filter((x) => ALWAYS.includes(x))
  const rest = base.tags.filter((x) => !ALWAYS.includes(x))
  const corrected = rest[0] === 'task_correct'
  const tags = corrected
    ? [...head, 'task_correct', ...added, ...rest.slice(1)]
    : [...head, ...added, ...rest]
  return {
    tags,
    scores: base.scores,
    hits: base.hits,
    classifier: 'merged',
    classify: c,
  }
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
