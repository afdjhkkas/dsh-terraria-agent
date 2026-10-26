/**
 * 知识库闸门（rule gate）——静态规则层，优先级永远高于示范案例库。
 *
 * 这里放的是"哪种动作在哪种局面下不许做"，每条都挂 `kb` 出处（`docs/kb/*.md` 的库名）。
 * 两条铁律：
 *  1. 示范样本入库前必须过这一层；过不了就丢弃，不入库、不学习。
 *  2. 自主方案执行前必须再过这一层；过不了就直接拒绝执行。
 *
 * 闸门刻意**不提供** force/override/bypass 之类的参数——"示范优先于规则"这种能力
 * 不是漏了，是根本不该存在。示范只能调时序细节（见 observe-mode 的 resolveTiming），
 * 不能把一条被禁止的动作变成允许。
 *
 * 规则本身必须与地图无关（换一张世界种子仍然成立），所以判定只吃
 * `snap`（局面快照）与 `action`（动作），不读世界坐标、不认具体方块。
 */

/** 每条规则的 kb 出处，全部是 docs/kb 下的真实库名（由 kb-router 的 LIBRARIES 维护）。 */
export const KB_SAFETY = 'safety_limit';
export const KB_ATOMIC = 'atomic_action';
export const KB_ITEM = 'item_operation';

/** 血量安全线：低于它就停手回血，不开新工程、不主动近战。 */
export const MIN_HP_RATIO = 0.35;

/** 会直接改写世界的命令——不经手持物品流程的那几个，一律视为作弊/危险捷径。 */
export const SHORTCUT_COMMANDS = Object.freeze([
  'give',
  'teleport',
  'removeblock',
  'setlife',
  'setmana',
  'godmode',
]);

/** 需要"站到能碰到"才能做的动作，以及它们的作用距离（格）。 */
const REACH = Object.freeze({ mine: 7, dig: 7, use: 4, attack: 4, place: 4 });

/** 主动动作：只有在角色能自主行动时才允许。 */
const ACTIVE_KINDS = Object.freeze(['move', 'jump', 'mine', 'dig', 'place', 'use', 'attack']);

function ratioOf(snap = {}) {
  const life = Number(snap.life ?? snap.hp);
  const max = Number(snap.lifeMax ?? snap.hpMax ?? 100);
  if (!Number.isFinite(life)) return null;
  if (!Number.isFinite(max) || max <= 0) return null;
  return life / max;
}

function isActive(action = {}) {
  return ACTIVE_KINDS.includes(action.kind);
}

/**
 * 竖直速度：桥的 state 回的是 `velocity: [vx, vy]`，示范样本里可能已经摊平成 `vy`。
 * 两种形状都要认——只认一种的话，闸门在真机快照上会安静地不生效。
 */
function vyOf(snap = {}) {
  const direct = Number(snap.vy ?? snap.velY);
  if (Number.isFinite(direct)) return direct;
  const arr = snap.velocity;
  const fromArr = Array.isArray(arr) ? Number(arr[1]) : NaN;
  return Number.isFinite(fromArr) ? fromArr : NaN;
}

/**
 * 规则表。`check(action, ctx)` 返回 true 表示**违反**。
 * 每条都带 `reason`（稳定字符串，测试与日志都靠它）与 `kb`（出处）。
 */
export const RULES = Object.freeze([
  {
    id: 'no-out-of-world',
    reason: 'not-in-world',
    kb: KB_SAFETY,
    why: '还没进世界/已退出世界，此时任何动作都只是空挥',
    always: true,
    check: (action, { snap }) => isActive(action) && snap.inWorld === false,
  },
  {
    id: 'no-dead-action',
    reason: 'player-dead',
    kb: KB_SAFETY,
    why: '角色已死（血量自检：死亡状态下没有"示范"可言）',
    always: true,
    check: (action, { snap }) => isActive(action) && snap.dead === true,
  },
  {
    id: 'no-action-while-paused',
    reason: 'paused',
    kb: KB_SAFETY,
    why: '游戏暂停时按键不会作用于角色（写操作必须回读确认）',
    always: true,
    check: (action, { snap }) => isActive(action) && snap.paused === true,
  },
  {
    id: 'hp-floor',
    reason: 'below-safety-line',
    kb: KB_SAFETY,
    why: '血量低于安全线就停手回血，不开新工程、不主动近战、不贪挖',
    always: true,
    // 停的是"作业"，不是逃生：低血还继续挖/打/放是真会死，低血往回跑是该做的事。
    // 把 move/jump 也拦掉，规划器只能吐 hold，人就站在原地等怪——那不是安全，是等死。
    // 逃跑本身另有约束（no-uncontrolled-fall / no-liquid-mining），不靠这一条兜。
    check: (action, { snap, opts }) => {
      if (!['attack', 'mine', 'dig', 'place', 'use'].includes(action.kind)) return false;
      const r = ratioOf(snap);
      if (r === null) return false;
      return r < Number(opts.minHpRatio ?? MIN_HP_RATIO);
    },
  },
  {
    id: 'no-liquid-mining',
    reason: 'liquid-mining',
    kb: KB_SAFETY,
    // 这条原来是"凡在水里就不许挖"，被真人教学推翻：水袋里唯一出来的办法就是在水里往上挖通道，
    // 再跳出去（实测连续挖了十几秒）。会灌满通道烧死人的是岩浆/微光/蜂蜜，普通水不是。
    why: '岩浆、微光、蜂蜜灌进通道会立刻烧死或黏住，几乎没有救援窗口；普通水不在此列',
    always: true,
    check: (action, { snap }) => {
      if (!isActive(action) || !['mine', 'dig'].includes(action.kind)) return false
      const ln = String(snap.liquidName ?? snap.liquid ?? '').toLowerCase()
      return ln === 'lava' || ln === 'shimmer' || ln === 'honey'
    },
  },
  {
    id: 'no-uncontrolled-fall',
    reason: 'uncontrolled-fall',
    kb: KB_SAFETY,
    why: '未确认退路就往下走＝高危坠落；竖直速度已经压不住时必须先抓落地',
    always: true,
    check: (action, { snap }) => {
      if (!['move', 'jump'].includes(action.kind)) return false;
      const vy = vyOf(snap);
      if (!Number.isFinite(vy) || vy <= 8) return false;
      // 只看竖直速度：速度已经压不住就是坠落，落地后 vy 会归零。
      // grounded 与 vy>8 同时成立属于自相矛盾的快照，这种情况下宁可不学这一条。
      return true;
    },
  },
  {
    id: 'no-multi-task',
    reason: 'multi-task',
    kb: KB_SAFETY,
    why: '同时只做一件事——不边走边挖、不边挖边放',
    always: true,
    check: (action) => action.multi === true || (Array.isArray(action.concurrent) && action.concurrent.length > 1),
  },
  {
    id: 'no-command-shortcut',
    reason: 'command-shortcut',
    kb: KB_ATOMIC,
    why: '世界只能通过真实物品流改变；直写世界的命令等于作弊',
    always: true,
    check: (action) => {
      const cmd = String(action.command ?? action.cmd ?? '').toLowerCase();
      if (!cmd) return false;
      return SHORTCUT_COMMANDS.includes(cmd);
    },
  },
  {
    id: 'reach-check',
    reason: 'out-of-reach',
    kb: KB_SAFETY,
    why: '站到能碰到目标的距离再动手，不隔空操作',
    always: true,
    check: (action) => {
      const need = REACH[action.kind];
      if (!need) return false;
      const dist = Number(action.dist ?? action.distance);
      if (!Number.isFinite(dist)) return false;
      return dist > need;
    },
  },
]);

/** 规则 id 列表，方便日志与文档自检。 */
export function ruleIds() {
  return RULES.map((r) => r.id);
}

/** 某条规则的知识库出处；查不到返回 null（不猜）。 */
export function kbFor(ruleId) {
  const r = RULES.find((x) => x.id === ruleId);
  return r ? r.kb : null;
}

/**
 * 取本次要用的规则子集。`tags` 是 kb-router 路由出来的库名（如 ['safety_limit','combat_math']）；
 * 标了 `always` 的规则（安全兜底）无论 tags 是什么都带上——兜底库"每次请求都加载"。
 */
export function loadRuleSet({ tags = [] } = {}) {
  const wanted = new Set([KB_SAFETY, ...tags]);
  const rules = RULES.filter((r) => r.always || wanted.has(r.kb));
  const sources = [...new Set(rules.map((r) => r.kb))];
  return { rules, tags: [...wanted], sources };
}

/**
 * 校验一个动作。返回 `{ ok, violations }`，violations 每项 `{ id, reason, kb, why }`。
 * `rules` 不传就用全表——**默认更严**，不会因为忘了传而放行。
 */
export function checkAction(action = {}, { snap = {}, opts = {}, rules = RULES } = {}) {
  const list = Array.isArray(rules) ? rules : rules.rules ?? RULES;
  const violations = [];
  for (const rule of list) {
    let bad = false;
    try {
      bad = rule.check(action, { snap, opts }) === true;
    } catch {
      // 规则自己炸了就当"判不了"。宁可放过这一条，也不能因为规则写错而卡死整条链路；
      // 但这条会被记进 violations 之外的 unknown，由调用方决定要不要报警。
      continue;
    }
    if (bad) violations.push({ id: rule.id, reason: rule.reason, kb: rule.kb, why: rule.why });
  }
  return { ok: violations.length === 0, violations };
}

/**
 * 自主方案闸门：**执行前**把整份方案过一遍知识库。
 * 校验失败就是失败——没有 override、没有白名单、没有"示范要求的例外"。
 */
export function gatePlan(plan = {}, ctx = {}) {
  const verdict = checkAction(plan, ctx);
  if (verdict.ok) return { ok: true, plan, violations: [] };
  return { ok: false, plan: null, violations: verdict.violations, why: describeViolations(verdict.violations) };
}

/** 人读的一行行违规说明，带知识库出处。 */
export function describeViolations(violations = [], { limit = 4 } = {}) {
  return violations
    .slice(0, limit)
    .map((v) => `[${v.id}] ${v.why}（kb: ${v.kb}）`)
    .join('；');
}

/** 一段话交代"这次用了哪些规则"。日志里一眼能看出闸门有没有真在管事。 */
export function formatRuleSet({ rules = RULES, sources = [] } = {}) {
  const tags = sources.length ? sources.join(',') : [...new Set(rules.map((r) => r.kb))].join(',');
  return `[gate] rules=${rules.length} kb=${tags}`;
}