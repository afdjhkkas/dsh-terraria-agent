using System;
using System.Collections.Generic;
using System.Text.Json;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DshAiMini
{
    /// <summary>
    /// 工具执行层：模型说"做什么"，这里负责真的做到。
    ///
    /// 三条纪律（和主桥一样，不能破）：
    /// 1. 只写控制位，不写位置/速度。走路就按方向键，跳跃就按跳键，
    ///    让引擎自己算位移；写 p.position 那是瞬移，不是走路。
    /// 2. 一切都是多帧状态机。走路要几秒、挖矿要挥好几下，不能在一个 Tick 里
    ///    假装做完了——那样报的"完成"是假的。
    /// 3. 完成与否看回读。挖矿看那格是否变成空，走路看坐标是否真的变了。
    /// </summary>
    internal static class AiToolExec
    {
        // ── 当前任务 ─────────────────────────────────────────
        static bool _running;
        static string _name = "";
        static JsonElement _args;
        static Action<string> _done;
        static int _frame;
        static string _note = "";

        // 走路
        static float _walkStartX;
        static int _walkTiles;
        static bool _walkRight;
        static int _walkStuck;

        // 挖矿 / 放置
        static int _tx, _ty, _tileId, _tries;

        // 攻击
        static int _attackMs;

        /// <summary>对外：正在跑一个工具时为真，面板据此显示"执行中"。</summary>
        public static bool Busy => _running;

        public static string CurrentName => _running ? _name : "";

        /// <summary>给面板显示的一行进度。</summary>
        public static string Note => _note;

        // ── 供 AiPlayer 读的控制意图（只写这些，不碰物理） ──
        internal static bool WantLeft;
        internal static bool WantRight;
        internal static bool WantJump;
        internal static bool WantUp;
        internal static bool WantDown;
        internal static bool WantUseItem;

        static void ClearIntent()
        {
            WantLeft = WantRight = WantJump = WantUp = WantDown = WantUseItem = false;
        }

        public static void Run(string name, string argsJson, Action<string> done)
        {
            if (_running) { done?.Invoke("已经有一个操作在跑，等它结束"); return; }
            if (!AiTools.Exists(name)) { done?.Invoke("没有这个工具: " + name); return; }

            _running = true;
            _name = name;
            _done = done;
            _frame = 0;
            _note = "";
            _walkStuck = 0;
            _tries = 0;
            ClearIntent();

            try
            {
                _args = string.IsNullOrWhiteSpace(argsJson)
                    ? default
                    : JsonDocument.Parse(argsJson).RootElement.Clone();
            }
            catch (Exception ex)
            {
                Finish("参数不是合法 JSON: " + ex.Message);
                return;
            }

            Begin();
        }

        static void Finish(string result)
        {
            ClearIntent();
            _running = false;
            string n = _name;
            _name = "";
            _note = "";
            var cb = _done;
            _done = null;
            cb?.Invoke(result);
        }

        // ── 参数小工具 ───────────────────────────────────────
        static int I(string key, int def)
        {
            if (_args.ValueKind != JsonValueKind.Object) return def;
            if (!_args.TryGetProperty(key, out var v)) return def;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n)) return n;
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out int m)) return m;
            return def;
        }

        static string S(string key, string def)
        {
            if (_args.ValueKind != JsonValueKind.Object) return def;
            if (!_args.TryGetProperty(key, out var v)) return def;
            return v.ValueKind == JsonValueKind.String ? v.GetString() : def;
        }

        static Player P => Main.player[Main.myPlayer];

        // ── 起点：立刻能做完的当场做完，要花时间的进状态机 ──
        static void Begin()
        {
            var p = P;
            switch (_name)
            {
                case "status":
                    Finish(AiGameState.Describe());
                    return;

                case "scan":
                    Finish(ScanText(Math.Clamp(I("radius", 8), 1, 20)));
                    return;

                case "summon_mount":
                {
                    // 快捷召唤是边沿触发：第一次按下去骑上，再按一次下来。
                    // 走输入层而不是直接调 QuickMount，这样和玩家手感一致。
                    AiPlayer.RequestQuickMount = true;
                    _note = "按快捷召唤键";
                    return;
                }

                case "walk":
                {
                    string dir = (S("dir", "right") ?? "right").ToLowerInvariant();
                    _walkRight = dir != "left";
                    _walkTiles = Math.Clamp(I("tiles", 1), 1, 60);
                    _walkStartX = p.position.X / 16f;
                    _note = "走 " + _walkTiles + " 格";
                    return;
                }

                case "jump":
                {
                    int ms = Math.Clamp(I("ms", 400), 1, 3000);
                    WantJump = true;
                    _frame = -ms;          // 负数帧当"按住倒计时"用
                    _note = "按住跳 " + ms + "ms";
                    return;
                }

                case "fly":
                {
                    int ms = Math.Clamp(I("ms", 1200), 1, 6000);
                    // 有翅膀按跳键，骑悬停坐骑按上键；两个都按住不会冲突。
                    WantJump = true;
                    WantUp = true;
                    _frame = -ms;
                    _note = "上升 " + ms + "ms";
                    return;
                }

                case "mine":
                {
                    _tx = I("x", 0);
                    _ty = I("y", 0);
                    _note = "挖 (" + _tx + "," + _ty + ")";
                    return;
                }

                case "place":
                {
                    _tx = I("x", 0);
                    _ty = I("y", 0);
                    _tileId = I("tile", 1);
                    _note = "放 (" + _tx + "," + _ty + ")";
                    return;
                }

                case "attack":
                {
                    _attackMs = Math.Clamp(I("ms", 1500), 100, 8000);
                    WantUseItem = true;
                    _frame = -_attackMs;
                    _note = "攻击附近";
                    return;
                }

                case "use_item":
                    WantUseItem = true;
                    _frame = -250;
                    _note = "使用手持物品";
                    return;

                default:
                    Finish("工具没有实现: " + _name);
                    return;
            }
        }

        // ── 每帧推进 ─────────────────────────────────────────
        public static void Tick()
        {
            if (!_running) return;
            var p = P;
            _frame++;

            // 倒计时类（jump / fly / attack / use_item）：负数走到 0 就该松手
            if (_frame <= 0) return;

            switch (_name)
            {
                case "summon_mount":
                {
                    // 按一下就要松开，等引擎把边沿吃掉
                    if (_frame > 2) AiPlayer.RequestQuickMount = false;
                    if (_frame > 8)
                    {
                        bool on = p.mount.Active;
                        Finish(on ? "已骑上坐骑（mountType " + p.mount.Type + "）"
                                 : "没有骑上：可能背包和装备槽里都没有坐骑");
                    }
                    return;
                }

                case "walk":
                {
                    float x = p.position.X / 16f;
                    float moved = Math.Abs(x - _walkStartX);
                    if (moved >= _walkTiles - 0.12f)
                    {
                        ClearIntent();
                        Finish("走了 " + moved.ToString("0.0") + " 格（目标 " + _walkTiles + "），现在 x=" + x.ToString("0.0"));
                        return;
                    }
                    if (Math.Abs(p.velocity.X) < 0.02f && _frame > 12) _walkStuck++; else _walkStuck = 0;
                    if (_walkStuck > 45)
                    {
                        ClearIntent();
                        Finish("被挡住了，只走了 " + moved.ToString("0.0") + " 格，现在 x=" + x.ToString("0.0"));
                        return;
                    }
                    if (_frame > 60 * 12)
                    {
                        ClearIntent();
                        Finish("超时，走了 " + moved.ToString("0.0") + " 格");
                        return;
                    }
                    if (_walkRight) { WantRight = true; WantLeft = false; }
                    else { WantLeft = true; WantRight = false; }
                    return;
                }

                case "jump":
                case "fly":
                case "attack":
                case "use_item":
                    // 倒计时到点就松开并报结果
                    ClearIntent();
                    if (_name == "attack")
                        Finish("攻击结束，当前生命 " + p.statLife + "/" + p.statLifeMax2);
                    else if (_name == "fly")
                        Finish("上升结束，现在 y=" + ((p.position.Y + p.height) / 16f).ToString("0.0"));
                    else if (_name == "jump")
                        Finish("跳跃结束，现在 y=" + ((p.position.Y + p.height) / 16f).ToString("0.0"));
                    else
                        Finish("已使用手持物品");
                    return;

                case "mine":
                {
                    var t = Main.tile[_tx, _ty];
                    if (t == null || !t.HasTile)
                    {
                        ClearIntent();
                        Finish("(" + _tx + "," + _ty + ") 已经是空的");
                        return;
                    }
                    // 镐子够不到就别白挥
                    int cx = (int)((p.position.X + p.width * 0.5f) / 16f);
                    int cy = (int)((p.position.Y + p.height * 0.5f) / 16f);
                    if (Math.Abs(cx - _tx) > 7 || Math.Abs(cy - _ty) > 7)
                    {
                        ClearIntent();
                        Finish("够不到：玩家在 (" + cx + "," + cy + ")，目标 (" + _tx + "," + _ty + ") 超过 7 格");
                        return;
                    }
                    if (_frame > 60 * 10)
                    {
                        ClearIntent();
                        Finish("挖了 10 秒还没掉，可能这方块镐子挖不动");
                        return;
                    }
                    Aim(t, _tx, _ty);
                    WantUseItem = true;
                    return;
                }

                case "place":
                {
                    var t = Main.tile[_tx, _ty];
                    if (t != null && t.HasTile)
                    {
                        ClearIntent();
                        Finish("(" + _tx + "," + _ty + ") 已经有方块了");
                        return;
                    }
                    int cx = (int)((p.position.X + p.width * 0.5f) / 16f);
                    int cy = (int)((p.position.Y + p.height * 0.5f) / 16f);
                    if (Math.Abs(cx - _tx) > 5 || Math.Abs(cy - _ty) > 5)
                    {
                        ClearIntent();
                        Finish("够不到：目标离玩家超过 5 格");
                        return;
                    }
                    if (_frame > 60 * 4)
                    {
                        ClearIntent();
                        Finish("放了 4 秒没放上，可能背包里没有这个方块，或这一格没有依托");
                        return;
                    }
                    Aim(t, _tx, _ty);
                    WantUseItem = true;
                    return;
                }
            }
        }

        /// <summary>把瞄准点设到目标格（原版会自己处理玩家能不能够到）。</summary>
        static void Aim(object _, int x, int y)
        {
            // tileTargetX/Y 是 Player 的静态字段（全体玩家共用的"这一帧在点哪一格"）
            Player.tileTargetX = x;
            Player.tileTargetY = y;
            Main.mouseX = (int)(x * 16f - Main.screenPosition.X);
            Main.mouseY = (int)(y * 16f - Main.screenPosition.Y);
        }

        /// <summary>扫一圈，返回实心格的相对坐标，最多列 60 个免得刷屏。</summary>
        static string ScanText(int r)
        {
            var p = P;
            int cx = (int)((p.position.X + p.width * 0.5f) / 16f);
            int cy = (int)((p.position.Y + p.height * 0.5f) / 16f);
            var sb = new System.Text.StringBuilder();
            sb.Append("玩家在 (").Append(cx).Append(',').Append(cy).Append(") 半径 ").Append(r).Append("：");
            int solid = 0, listed = 0;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) continue;
                    var t = Main.tile[x, y];
                    if (t == null || !t.HasTile) continue;
                    solid++;
                    if (listed < 60)
                    {
                        if (listed == 0) sb.Append("\n实心格：");
                        sb.Append("(").Append(x).Append(',').Append(y).Append(")");
                        listed++;
                    }
                }
            }
            sb.Append("\n共 ").Append(solid).Append(" 格实心");
            if (solid > listed) sb.Append("（只列了前 ").Append(listed).Append(" 个）");
            return sb.ToString();
        }
    }
}
