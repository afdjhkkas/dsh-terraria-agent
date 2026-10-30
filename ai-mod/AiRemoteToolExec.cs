using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace DshAiMini
{
    /// <summary>把 AI 工具翻译成 Bot Bridge 命令；结果只使用桥的真实回读。</summary>
    internal static class AiRemoteToolExec
    {
        static readonly ConcurrentQueue<Action> MainThread = new();

        /// <summary>把后台 Bot Bridge 的结果投递回 Terraria 主线程。</summary>
        public static void Tick(int max = 32)
        {
            if (max < 1) max = 1;
            int count = 0;
            while (count < max && MainThread.TryDequeue(out var action))
            {
                try { action(); } catch { }
                count++;
            }
        }

        public static void Run(AiActorBinding actor, string name, string argsJson, Action<string> done)
        {
            if (!AiBotRuntime.IsReady(actor)) { done?.Invoke("Bot 尚未确认已进入世界"); return; }
            Task.Run(async () =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argsJson) ? "{}" : argsJson);
                    var a = doc.RootElement;
                    string response;
                    switch (name)
                    {
                        case "status": response = await AiBotRuntime.RequestAsync(actor, "state", new { }); break;
                        case "players": response = await AiBotRuntime.RequestAsync(actor, "players", new { }); break;
                        case "follow": response = await AiBotRuntime.RequestAsync(actor, "follow", new {
                            player = Int(a, "player", -1),
                            range = Number(a, "range", 4.5),
                            guard = Bool(a, "guard", false),
                            off = Bool(a, "off", false),
                        }); break;
                        case "scan": response = await AiBotRuntime.RequestAsync(actor, "scan", new { radius = Int(a, "radius", 8) }); break;
                        case "walk":
                        {
                            string state = await AiBotRuntime.RequestAsync(actor, "state", new { });
                            double x = StateX(state);
                            int sign = string.Equals(Str(a, "dir", "right"), "left", StringComparison.OrdinalIgnoreCase) ? -1 : 1;
                            double target = x + sign * Math.Clamp(Int(a, "tiles", 1), 1, 60);
                            response = await AiBotRuntime.RequestAsync(actor, "moveto", new { x = target, ms = 12000 });
                            break;
                        }
                        case "jump":
                        {
                            // Do not send tiles=0: the bridge treats an explicit tiles
                            // value as a request to convert height back to press duration,
                            // so an absent optional field must stay absent.
                            var jump = new Dictionary<string, object> { ["ms"] = Int(a, "ms", 400) };
                            if (a.TryGetProperty("tiles", out var jt) && jt.ValueKind == JsonValueKind.Number)
                                jump["tiles"] = jt.GetDouble();
                            if (a.TryGetProperty("dir", out var jd) && jd.ValueKind == JsonValueKind.String)
                                jump["dir"] = jd.GetString();
                            if (a.TryGetProperty("moveMs", out var jm) && jm.ValueKind == JsonValueKind.Number)
                                jump["moveMs"] = jm.TryGetInt32(out var mm) ? mm : Int(a, "ms", 400);
                            response = await AiBotRuntime.RequestAsync(actor, "jump", jump);
                            break;
                        }
                        case "fly": response = await AiBotRuntime.RequestAsync(actor, "fly", new { ms = Int(a, "ms", 1200) }); break;
                        case "mine": response = await AiBotRuntime.RequestAsync(actor, "mine", new {
                            x = Int(a, "x", 0), y = Int(a, "y", 0), ms = Int(a, "ms", 8000),
                        }); break;
                        case "place": response = await AiBotRuntime.RequestAsync(actor, "place", new {
                            x = Int(a, "x", 0), y = Int(a, "y", 0),
                            type = Int(a, "type", Int(a, "tile", 1)),
                            ms = Int(a, "ms", 3000),
                        }); break;
                        case "attack": response = await AiBotRuntime.RequestAsync(actor, "attack", new {
                            ms = Int(a, "ms", 1500), r = Int(a, "r", Int(a, "radius", 12)),
                        }); break;
                        case "summon_mount": response = await AiBotRuntime.RequestAsync(actor, "mount", new { action = "quick" }); break;
                        case "dismount": response = await AiBotRuntime.RequestAsync(actor, "mount", new { action = "down" }); break;
                        case "use_item": response = await AiBotRuntime.RequestAsync(actor, "use", new {
                            slot = Int(a, "slot", -1),
                            x = Int(a, "x", int.MinValue), y = Int(a, "y", int.MinValue),
                            right = Bool(a, "right", false), ms = Int(a, "ms", 250),
                        }); break;
                        case "bridge":
                        {
                            string command = Str(a, "command", "");
                            if (string.IsNullOrWhiteSpace(command))
                            {
                                response = "bridge 缺少 command";
                                break;
                            }
                            JsonElement parameters = a.TryGetProperty("params", out var pe)
                                ? pe.Clone() : JsonDocument.Parse("{}").RootElement.Clone();
                            response = await AiBotRuntime.RequestAsync(actor, command, parameters);
                            break;
                        }
                        default:
                            // Keep the remote executor forward-compatible with a newly added
                            // convenience tool: if its name is also a bridge command, pass the
                            // nested object through instead of silently pretending it worked.
                            response = await AiBotRuntime.RequestAsync(actor, name, a.Clone());
                            break;
                    }
                    if (done != null) MainThread.Enqueue(() => done(response));
                }
                catch (Exception ex)
                {
                    string message = "Bot Bridge 请求失败: " + ex.Message;
                    if (done != null) MainThread.Enqueue(() => done(message));
                }
            });
        }

        static int Int(JsonElement e, string key, int fallback)
        {
            return e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : fallback;
        }
        static string Str(JsonElement e, string key, string fallback)
        {
            return e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : fallback;
        }

        static double Number(JsonElement e, string key, double fallback)
        {
            return e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out var v)
                && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var n) ? n : fallback;
        }

        static bool Bool(JsonElement e, string key, bool fallback)
        {
            return e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out var v)
                && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : fallback;
        }

        static double StateX(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var d)) root = d;
            if (root.TryGetProperty("player", out var p) && p.TryGetProperty("pos", out var pos) && pos.GetArrayLength() > 0)
                return pos[0].GetDouble();
            throw new InvalidOperationException("Bot state 没有 player.pos");
        }
    }
}
