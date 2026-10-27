using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Terraria;

namespace DshTerrariaBridge
{
    /// <summary>
    /// 导航决策层的**可热更参数**。
    ///
    /// 为什么要有这个：mod 桥接方案每改一次 C# 就要重启游戏，调试成本极高
    /// （用户 2026-10-27 明确点出）。所以把「决策阈值 / 优先级 / 动作参数」抽到
    /// 一个 json 里，改完写文件就生效，只有感知层和 A* 核心才需要重启。
    ///
    /// 约定：
    /// - 文件位置 `&lt;存档目录&gt;/ModConfigs/DshTerrariaBridge/nav-rules.json`
    /// - 文件不存在 → 用内置默认值，并在 `status` 里回报 `source:"default"`
    /// - 文件改了（mtime 变）→ 下一个 tick 自动重载，不需要重启
    /// - json 写错 → **保留上一份能用的值**并回报 `error`，绝不让坏配置把游戏搞崩
    /// </summary>
    internal static class NavRules
    {
        internal sealed class Thresholds
        {
            public int StallJumpsMax = 8;        // 同一位置连续自救跳几次后改成重规划
            public int CliffMaxDrop = 14;        // 前方落差超过这个数判悬崖（掉下去会受伤）
            public int WallMaxJump = 6;          // 墙高于这个数就不尝试跳
            public float JumpTilesMax = 6.0f;    // 满跳高度（格）
            public float StallJumpTiles = 6.0f;  // 停顿自救时按多高跳
            public float GapJumpTiles = 4.0f;    // 跨坑时按多高跳
            public float ClimbExtraTiles = 1.2f; // 翻墙时比墙高多跳多少
            public int DetourBudget = 3;         // 建材挡路最多绕几次
            public int RepathBudget = 3;         // 卡死重规划预算
        }

        internal sealed class Priorities
        {
            public int EscapeDanger = 100;
            public int Arrive = 95;
            public int UnstickBlocked = 90;
            public int StallJump = 80;
            public int AvoidCliff = 70;
            public int PassDoor = 60;
            public int DetourBuild = 50;
            public int JumpGap = 40;
            public int ClimbSlope = 30;
            public int MoveWaypoint = 10;
        }

        internal static Thresholds T = new Thresholds();
        internal static Priorities P = new Priorities();

        internal static string Source = "default";
        internal static string Error = "";
        internal static DateTime LastWrite = DateTime.MinValue;
        private static DateTime _lastCheck = DateTime.MinValue;
        private static string _path = "";

        internal static string FilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_path)) return _path;
                try
                {
                    string dir = Path.Combine(Main.SavePath, "ModConfigs", "DshTerrariaBridge");
                    Directory.CreateDirectory(dir);
                    _path = Path.Combine(dir, "nav-rules.json");
                }
                catch { _path = ""; }
                return _path;
            }
        }

        /// <summary>每帧调一次：最多每秒查一次 mtime，变了就重载。</summary>
        internal static void EnsureFresh()
        {
            string f = FilePath;
            if (string.IsNullOrEmpty(f)) return;
            if ((DateTime.UtcNow - _lastCheck).TotalMilliseconds < 1000) return;
            _lastCheck = DateTime.UtcNow;
            try
            {
                if (!File.Exists(f))
                {
                    // 首次运行：把当前默认值写出去，方便用户改
                    if (Source == "default" && LastWrite == DateTime.MinValue)
                    {
                        File.WriteAllText(f, Template());
                        LastWrite = File.GetLastWriteTimeUtc(f);
                        Source = "written";
                    }
                    return;
                }
                var mt = File.GetLastWriteTimeUtc(f);
                if (mt == LastWrite) return;
                Load(f, mt);
            }
            catch (Exception e) { Error = e.Message; }
        }

        /// <summary>显式重载（命令层 reload 用）。</summary>
        internal static string Reload()
        {
            _lastCheck = DateTime.MinValue;
            string f = FilePath;
            if (string.IsNullOrEmpty(f)) return "没有可写目录";
            if (!File.Exists(f)) { File.WriteAllText(f, Template()); }
            try { Load(f, File.GetLastWriteTimeUtc(f)); return "ok"; }
            catch (Exception e) { Error = e.Message; return "失败：" + e.Message; }
        }

        private static void Load(string f, DateTime mtime)
        {
            var doc = JsonDocument.Parse(File.ReadAllText(f));
            var root = doc.RootElement;
            var t = new Thresholds();
            var p = new Priorities();
            if (root.TryGetProperty("thresholds", out var te) && te.ValueKind == JsonValueKind.Object)
            {
                t.StallJumpsMax = GetInt(te, "stallJumpsMax", t.StallJumpsMax);
                t.CliffMaxDrop = GetInt(te, "cliffMaxDrop", t.CliffMaxDrop);
                t.WallMaxJump = GetInt(te, "wallMaxJump", t.WallMaxJump);
                t.JumpTilesMax = GetFloat(te, "jumpTilesMax", t.JumpTilesMax);
                t.StallJumpTiles = GetFloat(te, "stallJumpTiles", t.StallJumpTiles);
                t.GapJumpTiles = GetFloat(te, "gapJumpTiles", t.GapJumpTiles);
                t.ClimbExtraTiles = GetFloat(te, "climbExtraTiles", t.ClimbExtraTiles);
                t.DetourBudget = GetInt(te, "detourBudget", t.DetourBudget);
                t.RepathBudget = GetInt(te, "repathBudget", t.RepathBudget);
            }
            if (root.TryGetProperty("priorities", out var pe) && pe.ValueKind == JsonValueKind.Object)
            {
                p.EscapeDanger = GetInt(pe, "escapeDanger", p.EscapeDanger);
                p.Arrive = GetInt(pe, "arrive", p.Arrive);
                p.UnstickBlocked = GetInt(pe, "unstickBlocked", p.UnstickBlocked);
                p.StallJump = GetInt(pe, "stallJump", p.StallJump);
                p.AvoidCliff = GetInt(pe, "avoidCliff", p.AvoidCliff);
                p.PassDoor = GetInt(pe, "passDoor", p.PassDoor);
                p.DetourBuild = GetInt(pe, "detourBuild", p.DetourBuild);
                p.JumpGap = GetInt(pe, "jumpGap", p.JumpGap);
                p.ClimbSlope = GetInt(pe, "climbSlope", p.ClimbSlope);
                p.MoveWaypoint = GetInt(pe, "moveWaypoint", p.MoveWaypoint);
            }
            // 只在成功解析完之后才提交，坏配置不会把当前值弄坏
            T = t; P = p; LastWrite = mtime; Source = f; Error = "";
        }

        private static int GetInt(JsonElement e, string k, int def)
        {
            if (!e.TryGetProperty(k, out var v)) return def;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int i)) return i;
            return def;
        }

        private static float GetFloat(JsonElement e, string k, float def)
        {
            if (!e.TryGetProperty(k, out var v)) return def;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out double d)) return (float)d;
            return def;
        }

        internal static string Template()
        {
            return "{\n" +
                   "  \"_说明\": \"导航决策层热更参数：改完存盘即生效，不用重启游戏。感知层/A*核心仍要重启。\",\n" +
                   "  \"_注意\": \"删掉某个键就等于用内置默认值；写成 0 或负数会被当成非法值忽略。\",\n" +
                   "  \"thresholds\": {\n" +
                   "    \"stallJumpsMax\": 8,\n" +
                   "    \"cliffMaxDrop\": 14,\n" +
                   "    \"wallMaxJump\": 6,\n" +
                   "    \"jumpTilesMax\": 6.0,\n" +
                   "    \"stallJumpTiles\": 6.0,\n" +
                   "    \"gapJumpTiles\": 4.0,\n" +
                   "    \"climbExtraTiles\": 1.2,\n" +
                   "    \"detourBudget\": 3,\n" +
                   "    \"repathBudget\": 3\n" +
                   "  },\n" +
                   "  \"priorities\": {\n" +
                   "    \"escapeDanger\": 100,\n" +
                   "    \"arrive\": 95,\n" +
                   "    \"unstickBlocked\": 90,\n" +
                   "    \"stallJump\": 80,\n" +
                   "    \"avoidCliff\": 70,\n" +
                   "    \"passDoor\": 60,\n" +
                   "    \"detourBuild\": 50,\n" +
                   "    \"jumpGap\": 40,\n" +
                   "    \"climbSlope\": 30,\n" +
                   "    \"moveWaypoint\": 10\n" +
                   "  }\n" +
                   "}\n";
        }

        internal static object Status()
        {
            return new
            {
                file = FilePath,
                source = string.IsNullOrEmpty(Source) ? "default" : Source,
                error = Error,
                thresholds = T,
                priorities = P,
            };
        }
    }
}