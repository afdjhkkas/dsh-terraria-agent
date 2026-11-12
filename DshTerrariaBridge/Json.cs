using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        /// <summary>失败但带结构化诊断：ok:false 时模型还需要 data 里的数字（卡在哪、差几格、试过什么）。</summary>
        public static string Error(string message, object data) => Envelope(false, data, message);

        private static string Envelope(bool ok, object data, string error)
        {
            var obj = new Dictionary<string, object>();
            if (CurrentReq != null) obj["req"] = CurrentReq;
            obj["ok"] = ok;
            if (ok) obj["data"] = data;
            else obj["error"] = error;
            return Ser(obj);
        }

        /* ── 参数读取跟踪 ────────────────────────────────────────────────
         * 取不到就走默认值，意味着拼错的参数名会被静默吞掉：
         * move 传 direction（应为 dir）永远向右走、trash 不带 count 吃掉整栈，都是这么出的事。
         * 这里记下一次命令里真正被读过的参数名，命令结束后把"传了但没人读"的名字
         * 写进回包的 data.ignoredParams，调用方当场就能看见自己哪个参数没生效。
         * 不维护白名单：读没读过由代码自己说了算，不会和实现分叉。
         */
        [System.ThreadStatic] private static HashSet<string> readNames;

        internal static void BeginParamTracking() => readNames = new HashSet<string>(System.StringComparer.Ordinal);

        internal static HashSet<string> EndParamTracking()
        {
            var read = readNames;
            readNames = null;
            return read;
        }

        private static void Touch(string name) => readNames?.Add(name);

        /// <summary>
        /// 命令绕过下面几个读取函数、直接用 TryGetProperty 读参数时，在这里登记名字，
        /// 否则它们会被误报成"没人读"。
        /// </summary>
        internal static void Seen(params string[] names)
        {
            if (readNames == null || names == null) return;
            foreach (string n in names) readNames.Add(n);
        }

        /// <summary>
        /// 把"传了但这次没被读取"的参数名附到成功回包的 data 里。
        /// 失败回包不附：命令提前返回时后面的参数本来就还没轮到读，附上去全是误报。
        /// </summary>
        internal static string WithIgnoredParams(string response, JsonElement p, HashSet<string> read)
        {
            if (response == null || read == null || p.ValueKind != JsonValueKind.Object) return response;
            List<string> ignored = null;
            foreach (JsonProperty prop in p.EnumerateObject())
            {
                if (read.Contains(prop.Name)) continue;
                ignored ??= new List<string>();
                ignored.Add(prop.Name);
            }
            if (ignored == null) return response;
            try
            {
                if (JsonNode.Parse(response) is not JsonObject env) return response;
                if (env["ok"] is not JsonValue okValue || !okValue.TryGetValue(out bool ok) || !ok) return response;
                var names = new JsonArray();
                foreach (string n in ignored) names.Add(JsonValue.Create(n));
                const string note = "这些参数这次调用没有被读取，等于没传：检查参数名拼写（参数表见 docs/kb/_generated-commands.md）";
                // data 是对象就放进 data（插件和 npm run bridge 都只展示 data）；否则放信封上。
                JsonObject target = env["data"] as JsonObject ?? env;
                target["ignoredParams"] = names;
                target["ignoredParamsNote"] = note;
                return env.ToJsonString();
            }
            catch
            {
                // 附加信息失败不能把一条已经成功的命令变成失败。
                return response;
            }
        }

        public static bool Has(JsonElement p, string name)
        {
            Touch(name);
            return p.ValueKind == JsonValueKind.Object && p.TryGetProperty(name, out _);
        }

        public static int Int(JsonElement p, string name, int fallback)
        {
            Touch(name);
            if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty(name, out var e)) return fallback;
            if (e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var v)) return v;
            if (e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s)) return s;
            return fallback;
        }

        public static float Float(JsonElement p, string name, float fallback)
        {
            Touch(name);
            if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty(name, out var e)) return fallback;
            if (e.ValueKind == JsonValueKind.Number && e.TryGetSingle(out var v)) return v;
            if (e.ValueKind == JsonValueKind.String && float.TryParse(e.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s)) return s;
            return fallback;
        }

        public static bool Bool(JsonElement p, string name, bool fallback)
        {
            Touch(name);
            if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty(name, out var e)) return fallback;
            if (e.ValueKind is JsonValueKind.True or JsonValueKind.False) return e.GetBoolean();
            return fallback;
        }

        public static string Str(JsonElement p, string name, string fallback = null)
        {
            Touch(name);
            if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty(name, out var e)) return fallback;
            return e.ValueKind == JsonValueKind.String ? e.GetString() : fallback;
        }
    }
}