using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DshTerrariaBridge
{
    /// <summary>极小的 JSON 外壳：统一 {ok, data|error} 形状，和 win.ps1 的协议一致。</summary>
    internal static class Json
    {
        internal static readonly JsonSerializerOptions Options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Correlation id of the request currently being served on this thread.
        /// The client sends an optional "req" and gets it echoed back, so it can
        /// ignore any unrelated line instead of mistaking it for its own reply.
        /// </summary>
        [System.ThreadStatic] internal static string CurrentReq;

        public static string Ser(object value) => JsonSerializer.Serialize(value, Options);

        public static string Ok(object data) => Envelope(true, data, null);

        public static string Error(string message) => Envelope(false, null, message);

        private static string Envelope(bool ok, object data, string error)
        {
            var obj = new Dictionary<string, object>();
            if (CurrentReq != null) obj["req"] = CurrentReq;
            obj["ok"] = ok;
            if (ok) obj["data"] = data;
            else obj["error"] = error;
            return Ser(obj);
        }

        public static bool Has(JsonElement p, string name) =>
            p.ValueKind == JsonValueKind.Object && p.TryGetProperty(name, out _);

        public static int Int(JsonElement p, string name, int fallback)
        {
            if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty(name, out var e)) return fallback;
            if (e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var v)) return v;
            if (e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s)) return s;
            return fallback;
        }

        public static float Float(JsonElement p, string name, float fallback)
        {
            if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty(name, out var e)) return fallback;
            if (e.ValueKind == JsonValueKind.Number && e.TryGetSingle(out var v)) return v;
            if (e.ValueKind == JsonValueKind.String && float.TryParse(e.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s)) return s;
            return fallback;
        }

        public static bool Bool(JsonElement p, string name, bool fallback)
        {
            if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty(name, out var e)) return fallback;
            if (e.ValueKind is JsonValueKind.True or JsonValueKind.False) return e.GetBoolean();
            return fallback;
        }

        public static string Str(JsonElement p, string name, string fallback = null)
        {
            if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty(name, out var e)) return fallback;
            return e.ValueKind == JsonValueKind.String ? e.GetString() : fallback;
        }
    }
}