using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace DshAiMini
{
    /// <summary>把 AI 工具翻译成 Bot Bridge 命令；结果只使用桥的真实回读。</summary>
    internal static class AiRemoteToolExec
    {
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
                        case "jump": response = await AiBotRuntime.RequestAsync(actor, "jump", new { ms = Int(a, "ms", 400) }); break;
                        case "fly": response = await AiBotRuntime.RequestAsync(actor, "fly", new { ms = Int(a, "ms", 1200) }); break;
                        case "mine": response = await AiBotRuntime.RequestAsync(actor, "mine", new { x = Int(a, "x", 0), y = Int(a, "y", 0) }); break;
                        case "place": response = await AiBotRuntime.RequestAsync(actor, "place", new { x = Int(a, "x", 0), y = Int(a, "y", 0), tile = Int(a, "tile", 1) }); break;
                        case "attack": response = await AiBotRuntime.RequestAsync(actor, "attack", new { ms = Int(a, "ms", 1500), r = Int(a, "radius", 12) }); break;
                        case "summon_mount": response = await AiBotRuntime.RequestAsync(actor, "mount", new { action = "quick" }); break;
                        case "dismount": response = await AiBotRuntime.RequestAsync(actor, "mount", new { action = "down" }); break;
                        case "use_item": response = await AiBotRuntime.RequestAsync(actor, "use", new { ms = 250 }); break;
                        default: response = "Bot 执行器暂不支持工具: " + name; break;
                    }
                    done?.Invoke(response);
                }
                catch (Exception ex) { done?.Invoke("Bot Bridge 请求失败: " + ex.Message); }
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
