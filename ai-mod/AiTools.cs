using System;
using System.Collections.Generic;
using System.Text.Json;
using Terraria;
using Terraria.ModLoader;

namespace DshAiMini
{
    /// <summary>
    /// 工具清单：把"这个 AI 能对游戏做什么"写成模型看得懂的格式。
    ///
    /// 为什么要有这一层：没有它，模型只会告诉你"在泰拉瑞亚按 R 召唤坐骑"——
    /// 那是玩家自己的手在动。有了它，模型才能说"我来召唤"，然后真的召唤出来。
    /// 描述里刻意写清三件事：什么时候用、参数含义、会花多久。
    /// </summary>
    internal static class AiTools
    {
        internal sealed class Def
        {
            public string Name;
            public string Desc;
            /// <summary>参数名 → (类型, 说明)。类型支持 JSON Schema 的基础类型。</summary>
            public (string Name, string Type, string Desc, bool Required)[] Args;
        }

        internal static readonly Def[] All = new Def[]
        {
            new Def {
                Name = "status",
                Desc = "读取玩家当前状态：位置、生命、魔力、手持物品、是否骑乘、深度分区、附近敌人数。回答任何判断前先调它。",
                Args = Array.Empty<(string, string, string, bool)>(),
            },
            new Def {
                Name = "players",
                Desc = "读取多人世界里的玩家列表、索引、位置、生命和是否本机。要跟随队友前先调用它。",
                Args = Array.Empty<(string, string, string, bool)>(),
            },
            new Def {
                Name = "follow",
                Desc = "让当前操控对象通过真实方向输入跟随另一名玩家；目标可由 players 实时识别或用 player 指定。寻路和越障复用主桥的 walkto 执行器。off=true 停止跟随，range 是保持距离，guard=true 时兼顾护卫。",
                Args = new (string, string, string, bool)[] {
                    ("player", "int", "players 返回的目标索引", false),
                    ("range", "number", "保持距离（格），默认 4.5", false),
                    ("guard", "boolean", "是否护卫", false),
                    ("off", "boolean", "是否停止跟随", false),
                },
            },
            new Def {
                Name = "summon_mount",
                Desc = "召唤并骑上坐骑（快捷召唤）。已经骑着时会先下来。需要背包或装备槽里有坐骑。",
                Args = Array.Empty<(string, string, string, bool)>(),
            },
            new Def {
                Name = "dismount",
                Desc = "从当前坐骑上下来。没骑乘时什么也不做。",
                Args = Array.Empty<(string, string, string, bool)>(),
            },
            new Def {
                Name = "fly",
                Desc = "向上飞。有翅膀时按住跳键上升；骑悬停类坐骑时按上键。ms 是按住多久，1000 大约升几十格。",
                Args = new (string, string, string, bool)[] {
                    ("ms", "int", "按住多少毫秒，建议 600-2000", true),
                },
            },
            new Def {
                Name = "walk",
                Desc = "向左或向右走指定格数。会自己按刹车距离提前松手，实际落点误差通常小于半格。",
                Args = new (string, string, string, bool)[] {
                    ("dir", "string", "left 或 right", true),
                    ("tiles", "int", "走几格，1-60", true),
                },
            },
            new Def {
                Name = "jump",
                Desc = "跳一次。按住 ms 毫秒；有翅膀时按住会持续上升。满跳约 6.1 格。",
                Args = new (string, string, string, bool)[] {
                    ("ms", "int", "按住多少毫秒，150 是短跳，400 是满跳", true),
                },
            },
            new Def {
                Name = "mine",
                Desc = "挖掉指定格子的方块。会自动选中镐子并挥到挖掉为止。距离超过 7 格会失败。",
                Args = new (string, string, string, bool)[] {
                    ("x", "int", "世界格 X", true),
                    ("y", "int", "世界格 Y", true),
                },
            },
            new Def {
                Name = "place",
                Desc = "在指定格子放一个方块。需要背包里有该方块、且这一格挨着已有的方块。距离超过 4 格会失败。",
                Args = new (string, string, string, bool)[] {
                    ("x", "int", "世界格 X", true),
                    ("y", "int", "世界格 Y", true),
                    ("tile", "int", "方块 ID，1=石块 30=木块", true),
                },
            },
            new Def {
                Name = "attack",
                Desc = "攻击附近的敌人若干毫秒。只在有敌意生物时才动手。",
                Args = new (string, string, string, bool)[] {
                    ("ms", "int", "持续多少毫秒，建议 800-3000", true),
                    ("radius", "int", "搜索半径（格），默认 12", false),
                },
            },
            new Def {
                Name = "use_item",
                Desc = "使用当前手持的物品一次（喝药、放家具、投掷物等）。",
                Args = Array.Empty<(string, string, string, bool)>(),
            },
            new Def {
                Name = "scan",
                Desc = "扫描玩家周围一圈格子，返回哪些格子是实心的。想挖矿或找落脚点前先扫。",
                Args = new (string, string, string, bool)[] {
                    ("radius", "int", "半径（格），1-20，默认 8", false),
                },
            },
            new Def {
                Name = "bridge",
                Desc = "完整主桥能力入口。本机和 Bot 都可调用 DshTerrariaBridge 的全部命令（state、tile、inventory、move/moveto/walkto/path、jump/climb/fly、mine/place/use/interact、select/shoot/hook/mount、attack/guard/minion、talk/shop/buy/sell、chest、craft、equip、chatpoll/chatpost、players/follow、map/props/idmap 等）。命令名放 command，参数必须放在嵌套 params 对象；必须读取回包和后续 state/tile/inventory 回读，命令返回 ok 不等于动作已完成。命令清单和参数以 docs/kb/_generated-commands.md 为准。",
                Args = new (string, string, string, bool)[] {
                    ("command", "string", "主桥命令名，例如 state、moveto、mount、hook、talk、shop、inventory", true),
                    ("params", "object", "主桥命令参数对象；没有参数时传 {}。不要把参数平铺到 bridge 顶层", false),
                },
            },
        };

        /// <summary>拼成 OpenAI 兼容的 tools 数组。</summary>
        public static string Json()
        {
            using var ms = new System.IO.MemoryStream();
            using (var w = new Utf8JsonWriter(ms))
            {
                w.WriteStartArray();
                foreach (var d in All)
                {
                    w.WriteStartObject();
                    w.WriteString("type", "function");
                    w.WriteStartObject("function");
                    w.WriteString("name", d.Name);
                    w.WriteString("description", d.Desc);
                    w.WriteStartObject("parameters");
                    w.WriteString("type", "object");
                    if (d.Args.Length == 0)
                    {
                        w.WriteStartObject("properties");
                        w.WriteEndObject();
                    }
                    else
                    {
                        w.WriteStartObject("properties");
                        foreach (var a in d.Args)
                        {
                            w.WriteStartObject(a.Name);
                            // JSON Schema 的类型只有 string/number/integer/boolean/array/object。
                            // 写 "int" 会被 DeepSeek 直接判成非法 schema，整条请求 400 —— 这里必须转一次。
                            w.WriteString("type", a.Type == "int" ? "integer" : a.Type);
                            w.WriteString("description", a.Desc);
                            w.WriteEndObject();
                        }
                        w.WriteEndObject();
                        w.WriteStartArray("required");
                        foreach (var a in d.Args) if (a.Required) w.WriteStringValue(a.Name);
                        w.WriteEndArray();
                    }
                    w.WriteEndObject();
                    w.WriteEndObject();
                    w.WriteEndObject();
                }
                w.WriteEndArray();
            }
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }

        public static bool Exists(string name)
        {
            foreach (var d in All) if (d.Name == name) return true;
            return false;
        }

        /// <summary>一行摘要，给面板/prompt 用。</summary>
        public static string Brief()
        {
            var names = new List<string>();
            foreach (var d in All) names.Add(d.Name);
            return string.Join(" / ", names);
        }
    }
}
