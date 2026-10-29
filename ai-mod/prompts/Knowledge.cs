using System.Collections.Generic;
using System.Text;

namespace DshAiMini
{
    /// <summary>
    /// 精简知识库。编译进 dll，单 mod 自包含，不需要外部文件或联网。
    ///
    /// 和 docs/kb/ 的关系：那边是"写给人看的完整版"（一万两千行，带证据与反例），
    /// 这里是"塞进上下文的一页速查"（只留会改变决策的结论）。
    /// 两边冲突时以 docs/kb/ 为准，这里是它的摘要而不是替代。
    ///
    /// 检索方式故意做得很笨：按关键词命中主题，命中就整条塞进去。
    /// 不做向量检索，因为总量小到全塞也不会爆上下文，而错配一次关键词比多塞两段话糟。
    /// </summary>
    internal static class Knowledge
    {
        internal sealed class Entry
        {
            public string Topic;
            public string[] Keywords;
            public string Text;
        }

        public static readonly Entry[] Entries =
        {
            new Entry
            {
                Topic = "安全与操作纪律",
                Keywords = new[] { "安全", "作弊", "改存档", "假操作", "脚本", "自动", "后台", "失焦", "挂机" },
                Text = @"## 安全与操作纪律
- 改世界只能走""选物品 → 瞄准 → 使用 → 等结算 → 回读""这条链，直接写坐标/速度/方块都是假的。
- 窗口没焦点不等于操作不了：本项目的桥在失焦下也能控制角色，别把""没反应""归因于焦点。
- 撤退线：血量掉到最大值 60% 就该退，不要硬扛；没药不要走远。
- 不确定的事说不知道，不要拿攻略站的数字冒充实测。"
            },
            new Entry
            {
                Topic = "房屋与 NPC 入住",
                Keywords = new[] { "房子", "房屋", "入住", "NPC", "住", "房间", "家具", "门", "幸福" },
                Text = @"## 房屋与 NPC 入住
- 有效房屋要同时满足：封闭外框、铺满背景墙、四类家具各至少一件（光源/舒适/桌子/门或平台）、尺寸别太小。
- **开着的门会让房间""漏气""**，判定直接失败；要判定前先关门。
- 平台可以当屋顶或地板，但空洞太多不算封闭。
- NPC 幸福度看三件事：邻居（喜欢的群系/喜欢的人）、房屋质量、是否拥挤。想卖好东西就先让他住舒服。
- 搬 NPC 的房间要动真实房间归属，只改字段他不会走过来。"
            },
            new Entry
            {
                Topic = "世界分层与群系",
                Keywords = new[] { "世界", "分层", "深度", "群系", "生物群系", "腐化", "猩红", "神圣", "沙漠", "雪地", "丛林", "地牢", "地狱", "海洋" },
                Text = @"## 世界分层与群系
- 分层按**深度百分比**算，不按固定高度：地表 → 地下 → 洞穴 → 地狱。
- 群系由背景墙、物块类型、音乐共同判定，站在边界会来回跳。
- 液体有三种：水、岩浆、蜂蜜。水里会减速并可能溺水，岩浆碰到就重伤。
- 腐化/猩红/神圣会随时间扩散，隔离带要挖够宽。"
            },
            new Entry
            {
                Topic = "进度与首领",
                Keywords = new[] { "boss", "首领", "月之领主", "世纪之花", "石巨人", "机械", "召唤", "进度", "困难模式", "打不过", "刮痧" },
                Text = @"## 进度与首领
- 进度是骨牌：打掉一个才解锁下一个。
- 前困难模式：史莱姆王 → 克苏鲁之眼 → 世界吞噬者/克苏鲁之脑 → 蜂王 → 骷髅王 → 血肉墙。
- 困难模式：机械三王（双子/毁灭者/机械骷髅）→ 世纪之花 → 石巨人 → 拜月教徒 → 四柱 → **月之领主**。
- 打之前先备齐：血量上限、场地（平整+多层平台）、药水、退路。打不动多半是护甲/武器没跟上，不是操作问题。"
            },
            new Entry
            {
                Topic = "装备路线",
                Keywords = new[] { "镐", "矿", "矿石", "护甲", "盔甲", "武器", "饰品", "重铸", "词条", "装备", "升级" },
                Text = @"## 装备路线
- 镐力决定能挖什么：低级矿用低级镐，挖不动就是镐力不够，不是位置不对。
- 矿石阶梯大致是 铜/锡 → 铁/铅 → 银/钨 → 金/铂金 → 陨石/魔矿 → 地狱石（困难模式后才有狱炎锭以上）。
- 护甲走三件套，别混职业；早期混穿不如穿齐一套低级的。
- 真正决定生死的是**机动性饰品**（二段跳、速度、翅膀），比多几点防御重要。"
            },
            new Entry
            {
                Topic = "战斗结算",
                Keywords = new[] { "伤害", "暴击", "无敌帧", "穿透", "击退", "减益", "debuff", "为什么打不动", "防御" },
                Text = @"## 战斗结算
- 一次攻击要过：命中判定 → 暴击 → 减伤（对方防御）→ 无敌帧。
- **无敌帧**是""为什么打不动""最常见的原因：同一目标短时间内只吃一次伤害，连点没用。
- 穿透类武器能同时打多个目标，但单体伤害往往更低。
- 减益（燃烧/中毒/破甲）对高防御目标特别值。"
            },
            new Entry
            {
                Topic = "掉落与采集",
                Keywords = new[] { "掉落", "箱子", "容器", "钓", "鱼", "虫", "材料", "哪里拿", "怎么获得" },
                Text = @"## 掉落与采集
- 最早最稳的装备来源是**自然容器**（地下木箱/金箱），不用打怪。
- 钓鱼要有钓竿 + 鱼饵，抛竿 → 等咬钩（浮标下沉）→ 收线；板条箱里出装备和材料。
- 捉虫要用虫网对准目标挥一下，不是走过去捡。
- 召唤物的材料通常有一条前置链（打小怪 → 拿材料 → 合成召唤物），别想着直接买。"
            },
            new Entry
            {
                Topic = "移动与越障",
                Keywords = new[] { "走", "跳", "爬", "绳", "越", "上不去", "卡住", "怎么上去", "路线", "寻路", "挖" },
                Text = @"## 移动与越障
- 自动只能跨 1 格台阶；再高就要跳。
- 爬绳的前提是**脚底那一格就是绳**，站在平台上按上下不会爬。
- 往上挖时不要往下挖穿自己的立足点；隧道要保持能走回来的高度。
- 高处够不到就先找楼梯或斜坡，硬跳撞天花板时人会卡在半空。"
            },
            new Entry
            {
                Topic = "抓钩与飞行",
                Keywords = new[] { "抓钩", "钩爪", "翅膀", "飞", "坐骑", "骑", "二段跳" },
                Text = @"## 抓钩与飞行
- 抓钩要装在**专用饰品槽**，装在背包里不生效。
- 不是所有方块都能挂：木块一类能挂，平台/草/土常常挂不住。
- 翅膀上升靠**按住跳键**，不是按上键；一次按住能持续爬升。
- 悬停类坐骑（UFO）反过来，按**上键**升空。搞混就完全不动。"
            },
        };

        /// <summary>
        /// 按问题挑相关条目。没命中任何关键词就返回空串（不硬塞，省上下文）。
        /// </summary>
        public static string ForQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return string.Empty;

            var sb = new StringBuilder();
            foreach (var e in Entries)
            {
                foreach (var k in e.Keywords)
                {
                    if (query.Contains(k, System.StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine(e.Text);
                        break;
                    }
                }
            }
            return sb.ToString().TrimEnd();
        }

        public static int Count => Entries.Length;

        // ────────────────────────────────────────────────────────────────
        // 完整知识库（编进 dll 的那 16 个 .md，共约 200 KB）
        //
        // 上面那 9 条 Entries 是"一页速查"；这里是把 docs/kb/ 的原文整篇塞进来。
        // 冲突时以完整版为准 —— 里面写了大量"为什么"和"曾经错在哪"，
        // 摘要掉就等于把纠错能力也一起摘要掉了。
        // ────────────────────────────────────────────────────────────────

        static string _fullCache;
        static bool _fullLoaded;

        /// <summary>按固定顺序拼，保证每次生成的文本一致（便于命中上游缓存）。</summary>
        static readonly string[] Order =
        {
            "safety-limit.md",
            "atomic-action.md",
            "task-correct.md",
            "game-rule.md",
            "world-biome.md",
            "navigation.md",
            "actor-behavior.md",
            "actor-terrain-rules.md",
            "abilities.md",
            "item-operation.md",
            "combat-math.md",
            "loot-source.md",
            "progression-boss.md",
            "gear-route.md",
            "build-standard.md",
            "npc-town.md",
            "_generated-commands.md",
            "README.md",
        };

        /// <summary>
        /// 全部知识库原文。首次调用时从嵌入资源读一次并缓存。
        /// 读不到就返回空串（单 mod 版本要能容忍打包时漏了资源）。
        /// </summary>
        public static string All()
        {
            if (_fullLoaded) return _fullCache ?? string.Empty;
            _fullLoaded = true;

            var asm = typeof(Knowledge).Assembly;
            var sb = new StringBuilder();

            foreach (var name in Order)
            {
                var res = FindResource(asm, name);
                if (res == null) continue;

                using var s = asm.GetManifestResourceStream(res);
                if (s == null) continue;
                using var r = new System.IO.StreamReader(s, Encoding.UTF8);
                var text = r.ReadToEnd();
                if (text.Length == 0) continue;

                sb.Append("\n\n===== 知识库：").Append(name).Append(" =====\n");
                sb.Append(text);
            }

            _fullCache = sb.ToString();
            return _fullCache;
        }

        static string FindResource(System.Reflection.Assembly asm, string fileName)
        {
            foreach (var r in asm.GetManifestResourceNames())
            {
                if (r.EndsWith("." + fileName, System.StringComparison.OrdinalIgnoreCase))
                    return r;
            }
            return null;
        }

        /// <summary>完整知识库有多少字符（日志里报一下，确认资源真打进去了）。</summary>
        public static int FullLength => All().Length;
    }
}
