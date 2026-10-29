using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DshAiMini
{
    /// <summary>
    /// 一步一个脚印：先把「能不能在 mod 里发 HTTP、且不卡游戏」这件事证了。
    ///
    /// 三条硬要求：
    ///   1. 请求必须在后台线程跑，主线程一帧都不能等网络；
    ///   2. 回包必须在主线程消费（Terraria 的 UI/世界数据只允许主线程碰）；
    ///   3. 超时、异常、连续失败都要有界，不能让一个挂住的请求堆住队列。
    ///
    /// 做法是「投递 → 后台 → 结果队列 → 主线程 drain」。
    /// </summary>
    internal static class AiHttp
    {
        private static readonly HttpClient Client = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(10),
        })
        {
            Timeout = Timeout.InfiniteTimeSpan, // 超时由每次请求自己控，避免全局设死
        };

        private sealed class Job
        {
            public string Url;
            public string Body;
            public string ApiKey;
            public int TimeoutMs;
            public long StartedTick;
            public Action<bool, int, string, string> Callback;
        }

        private sealed class Result
        {
            public bool Ok;
            public int Status;
            public string Text;
            public string Error;
            public long ElapsedMs;
            public Action<bool, int, string, string> Callback;
        }

        private static readonly ConcurrentQueue<Job> Pending = new ConcurrentQueue<Job>();
        private static readonly ConcurrentQueue<Result> Done = new ConcurrentQueue<Result>();
        private static int _inFlight;

        /// <summary>主线程调用。把请求丢进队列，立刻返回，不等网络。</summary>
        public static void Post(string url, string body, string apiKey, int timeoutMs = 30000,
                                Action<bool, int, string, string> callback = null)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            Pending.Enqueue(new Job { Url = url.Trim(), Body = body, ApiKey = apiKey,
                                      TimeoutMs = timeoutMs, Callback = callback });
            PumpIfIdle();
        }

        /// <summary>主线程调用。取回已完成的结果；没有就返回 null。</summary>
        public static (bool ok, int status, string text, string error, long elapsedMs)? TryTake()
        {
            // 顺手再启一个 worker：上一个处理完、队列里又攒了新的，这里把它接上。
            PumpIfIdle();
            if (Done.TryDequeue(out var r))
            {
                // 回调永远在主线程执行（TryTake 只在 PostUpdateEverything 里调）
                r.Callback?.Invoke(r.Ok, r.Status, r.Text, r.Error);
                return (r.Ok, r.Status, r.Text, r.Error, r.ElapsedMs);
            }
            return null;
        }

        /// <summary>还有多少没回来（含正在跑的）。用来在 UI 上显示"思考中"。</summary>
        public static int Busy => Volatile.Read(ref _inFlight);

        private static void PumpIfIdle()
        {
            if (Interlocked.CompareExchange(ref _inFlight, 1, 0) != 0) return;
            Task.Run(Worker);
        }

        private static async Task Worker()
        {
            try
            {
                while (Pending.TryDequeue(out var job))
                {
                    var sw = Stopwatch.StartNew();
                    var res = new Result();
                    try
                    {
                        using var cts = new CancellationTokenSource(job.TimeoutMs);
                        using var req = new HttpRequestMessage(HttpMethod.Post, job.Url);
                        if (!string.IsNullOrEmpty(job.ApiKey))
                            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + job.ApiKey);
                        if (job.Body != null)
                            req.Content = new StringContent(job.Body, Encoding.UTF8, "application/json");

                        using var resp = await Client.SendAsync(req, cts.Token).ConfigureAwait(false);
                        res.Status = (int)resp.StatusCode;
                        res.Text = await resp.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                        res.Ok = resp.IsSuccessStatusCode;
                    }
                    catch (OperationCanceledException)
                    {
                        res.Error = "请求超时 " + job.TimeoutMs + "ms";
                    }
                    catch (Exception ex)
                    {
                        res.Error = ex.GetType().Name + ": " + ex.Message;
                    }
                    sw.Stop();
                    res.ElapsedMs = sw.ElapsedMilliseconds;
                    res.Callback = job.Callback;
                    Done.Enqueue(res);
                }
            }
            finally
            {
                Volatile.Write(ref _inFlight, 0);
                // 收尾竞态：worker 退出前可能又有人入队，再叫它一次。
                if (!Pending.IsEmpty) PumpIfIdle();
            }
        }
    }
}
