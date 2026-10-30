using System;
using System.Collections.Generic;
using System.Text;

namespace DshAiMini
{
    /// <summary>
    /// Agent 循环：让它能"先动手再回答"。
    ///
    /// 一轮的流程：
    ///   发请求（带工具清单） → 它说"我要先 walk(左,5)" →
    ///   真的走 → 把结果回填 → 再问它 → 它说"走了 4.8 格，现在 x=..." →
    ///   它接着判断 → 直到它不再要工具，直接给答案。
    ///
    /// 上限 6 层，防止它陷入"再挖一格/再挖一格"停不下来。
    /// 每一层都把过程推给 UI（stream），玩家能看见它此刻在做什么，
    /// 而不是盯着一个静止的"思考中"。
    /// </summary>
    internal static class AiAgent
    {
        const int MaxDepth = 6;

        public static void Ask(AiConfig cfg, string userText,
                               Action<AiLog.Kind, string> stream,
                               Action<string> done)
        {
            AiChat.EnsureHistoryLoaded();
            // system 和普通对话一样：人格 + 知识 + 实时状态
            string sys = BuildSystem(cfg, userText);

            var turns = new List<ChatTurn> { new ChatTurn("system", sys) };
            turns.AddRange(AiChat.History);
            turns.Add(new ChatTurn("user", userText));

            Step(cfg, turns, 0, stream, done);
        }

        static string BuildSystem(AiConfig cfg, string userText)
        {
            var sb = new StringBuilder();
            sb.Append(SystemPrompt.Text);

            // 让它知道现在手上有什么工具 —— 这是"配置完就能读到所有操作方法"的那一半
            sb.Append("\n\n## 你能对游戏做的事（用工具调用，不要只用嘴说）\n");
            sb.Append("你现在可以真的操作这个角色。想做事就调用工具，不要告诉玩家「你去按某个键」。\n");
            sb.Append("当前执行对象：").Append(cfg.ActiveActorDescription).Append("。\n");
            if (cfg.ActiveActor != null && cfg.ActiveActor.IsBot)
            {
                sb.Append("这是独立队友客户端；本机玩家的坐标、背包和状态不能代表 Bot。"
                    + "Bot 未显示‘已进世界’时，不要假装已经能操作；先说明等待入服。\n");
            }
            sb.Append("可用工具：").Append(AiTools.Brief()).Append("\n");
            sb.Append("bridge 是完整主桥入口：command 使用主桥命令名，params 必须是嵌套对象；本机和 Bot 共用同一套命令、知识库和游戏侧执行器。先读取当前操控对象的实际状态，再按玩家目标选择 players、follow 或其他命令；跟随目标由实时玩家列表识别，寻路和越障交给主桥现有执行器。动作完成必须依据 state、tile、inventory、shop 或相应回读字段确认；只给输入让游戏引擎计算，不用直接写位置、速度、pulley、生命、物品栏或 tile 来制造结果。\n");
            sb.Append("规则：做判断前先读当前对象的实际状态；一次只调一个动作并等待回读；");
            sb.Append("③ 干完活要用玩家听得懂的话说结果（走了几格、挖掉没有、掉血没有）；④ 做不到就说做不到，别编。");

            if (cfg.UseKnowledge)
            {
                bool full = string.Equals(cfg.KnowledgeMode, "full", StringComparison.OrdinalIgnoreCase);
                string kb = full ? Knowledge.All() : Knowledge.ForQuery(userText);
                if (!string.IsNullOrWhiteSpace(kb))
                    sb.Append("\n\n").Append(full
                        ? "## 知识库全文（内部资料，回答时优先采信；与你的记忆冲突时以此为准）\n"
                        : "## 相关知识库\n").Append(kb);
            }
            if (cfg.IncludeGameState)
            {
                string st = AiGameState.Describe(cfg);
                if (!string.IsNullOrWhiteSpace(st)) sb.Append("\n\n## 玩家当前状态（实时）\n").Append(st);
            }
            return sb.ToString();
        }

        static void Step(AiConfig cfg, List<ChatTurn> turns, int depth,
                         Action<AiLog.Kind, string> stream, Action<string> done)
        {
            if (depth >= MaxDepth)
            {
                done("（已经连续做了 " + MaxDepth + " 步，先停在这里。要我继续就说一声。）");
                return;
            }

            var body = AiChat.BuildBody(cfg, turns, stream: false, withTools: true);
            AiHttp.Post(cfg.RuntimeBaseUrlResolved, body, cfg.RuntimeApiKeyResolved, cfg.TimeoutMs,
                (ok, status, text, error) =>
                {
                    if (!ok)
                    {
                        done("[连不上] " + (string.IsNullOrEmpty(error) ? ("HTTP " + status) : error));
                        return;
                    }

                    var calls = AiChat.ExtractToolCalls(text);
                    if (calls.Count == 0)
                    {
                        // 它不再要工具了 —— 这一轮的话就是最终回答
                        string reply = AiChat.ExtractText(text);
                        if (reply.StartsWith("[错误]")) { done(reply.Substring(4)); return; }
                        if (string.IsNullOrWhiteSpace(reply)) { done("[模型返回空回复] 请检查模型、协议和接口地址"); return; }
                        AiChat.History.Add(new ChatTurn("user", LastUser(turns)));
                        AiChat.History.Add(new ChatTurn("assistant", reply));
                        while (AiChat.History.Count > 40) AiChat.History.RemoveAt(0);
                        done(reply);
                        return;
                    }

                    // 它要动手：把这一轮的 assistant(tool_calls) 记进上下文
                    turns.Add(new ChatTurn("assistant", null) { Calls = calls });

                    RunCalls(cfg, turns, calls, 0, depth, stream, done);
                }, cfg.RuntimeProtocolResolved);
        }

        /// <summary>按顺序执行这一批工具，全部做完再回到 Step。</summary>
        static void RunCalls(AiConfig cfg, List<ChatTurn> turns, List<ToolCall> calls, int i, int depth,
                             Action<AiLog.Kind, string> stream, Action<string> done)
        {
            if (i >= calls.Count)
            {
                Step(cfg, turns, depth + 1, stream, done);
                return;
            }

            var c = calls[i];
            stream(AiLog.Kind.Tool, c.Name + " " + Short(c.Args));

            Action<string> onResult = result =>
            {
                stream(AiLog.Kind.Tool, "→ " + OneLine(result));
                turns.Add(new ChatTurn("tool", result) { ToolCallId = c.Id });
                RunCalls(cfg, turns, calls, i + 1, depth, stream, done);
            };

            // 当前执行对象决定工具落点：本机走游戏内状态机，队友 Bot 走独立客户端桥。
            if (cfg.ActiveActor != null && cfg.ActiveActor.IsBot)
                AiRemoteToolExec.Run(cfg.ActiveActor, c.Name, c.Args, onResult);
            else
                AiToolExec.Run(c.Name, c.Args, onResult);
        }

        static string LastUser(List<ChatTurn> turns)
        {
            for (int i = turns.Count - 1; i >= 0; i--)
                if (turns[i].Role == "user") return turns[i].Content ?? "";
            return "";
        }

        static string Short(string s)
        {
            if (string.IsNullOrEmpty(s) || s == "{}") return "";
            return s.Length > 80 ? s.Substring(0, 80) + "…" : s;
        }

        static string OneLine(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ").Trim();
            return s.Length > 100 ? s.Substring(0, 100) + "…" : s;
        }
    }
}
