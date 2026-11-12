using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Terraria;

namespace DshTerrariaBridge
{
    /// <summary>
    /// WebSocket 服务端：实时推送玩家状态变化、游戏事件。
    /// 监听 ws://127.0.0.1:47778
    /// </summary>
    internal static class WebSocketServer
    {
        private static HttpListener listener;
        private static readonly List<WebSocket> clients = new List<WebSocket>();
        private static readonly object clientsLock = new object();
        private static bool running = false;

        // 上一帧的玩家状态（用于检测变化）
        private static int lastLife = -1;
        private static int lastMana = -1;
        private static float lastPosX = -1;
        private static float lastPosY = -1;
        private static int lastBuffCount = -1;

        internal static void Start()
        {
            if (running) return;
            running = true;

            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:47778/");
            listener.Start();

            Task.Run(async () =>
            {
                while (running)
                {
                    try
                    {
                        HttpListenerContext context = await listener.GetContextAsync();
                        if (context.Request.IsWebSocketRequest)
                        {
                            HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                            WebSocket ws = wsContext.WebSocket;
                            lock (clientsLock) clients.Add(ws);

                            // 发送连接成功消息
                            await SendToClient(ws, new { type = "connected", message = "WebSocket connected" });

                            // 启动接收循环（保持连接）
                            _ = Task.Run(async () =>
                            {
                                var buffer = new byte[1024];
                                try
                                {
                                    while (ws.State == WebSocketState.Open)
                                    {
                                        await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                                    }
                                }
                                catch { }
                                finally
                                {
                                    lock (clientsLock) clients.Remove(ws);
                                    ws.Dispose();
                                }
                            });
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                        }
                    }
                    catch { }
                }
            });
        }

        internal static void Stop()
        {
            if (!running) return;
            running = false;

            lock (clientsLock)
            {
                foreach (var ws in clients)
                {
                    try
                    {
                        if (ws.State == WebSocketState.Open)
                            ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutdown", CancellationToken.None).Wait(1000);
                        ws.Dispose();
                    }
                    catch { }
                }
                clients.Clear();
            }

            try
            {
                listener?.Stop();
                listener?.Close();
            }
            catch { }
        }

        /// <summary>
        /// 每帧调用：检测玩家状态变化并推送
        /// </summary>
        internal static void Update()
        {
            if (!running || clients.Count == 0) return;
            if (!Main.gameMenu && Main.LocalPlayer != null && Main.LocalPlayer.active)
            {
                Player p = Main.LocalPlayer;
                bool changed = false;
                var delta = new Dictionary<string, object> { ["type"] = "player_state" };

                if (p.statLife != lastLife)
                {
                    delta["life"] = p.statLife;
                    delta["maxLife"] = p.statLifeMax2;
                    lastLife = p.statLife;
                    changed = true;
                }

                if (p.statMana != lastMana)
                {
                    delta["mana"] = p.statMana;
                    delta["maxMana"] = p.statManaMax2;
                    lastMana = p.statMana;
                    changed = true;
                }

                float px = p.Center.X / 16f, py = p.Center.Y / 16f;
                if (Math.Abs(px - lastPosX) > 0.5f || Math.Abs(py - lastPosY) > 0.5f)
                {
                    delta["x"] = (int)px;
                    delta["y"] = (int)py;
                    lastPosX = px;
                    lastPosY = py;
                    changed = true;
                }

                int buffCount = 0;
                for (int i = 0; i < p.buffType.Length; i++)
                    if (p.buffType[i] > 0) buffCount++;

                if (buffCount != lastBuffCount)
                {
                    var buffs = new List<object>();
                    for (int i = 0; i < p.buffType.Length; i++)
                    {
                        if (p.buffType[i] > 0)
                            buffs.Add(new { type = p.buffType[i], time = p.buffTime[i] });
                    }
                    delta["buffs"] = buffs;
                    lastBuffCount = buffCount;
                    changed = true;
                }

                if (changed)
                {
                    Broadcast(delta);
                }
            }
        }

        private static void Broadcast(object data)
        {
            lock (clientsLock)
            {
                var json = JsonSerializer.Serialize(data, Json.Options);
                var bytes = Encoding.UTF8.GetBytes(json);
                var deadClients = new List<WebSocket>();

                foreach (var ws in clients)
                {
                    try
                    {
                        if (ws.State == WebSocketState.Open)
                        {
                            ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                        }
                        else
                        {
                            deadClients.Add(ws);
                        }
                    }
                    catch
                    {
                        deadClients.Add(ws);
                    }
                }

                foreach (var ws in deadClients)
                {
                    clients.Remove(ws);
                    try { ws.Dispose(); } catch { }
                }
            }
        }

        private static async Task SendToClient(WebSocket ws, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, Json.Options);
                var bytes = Encoding.UTF8.GetBytes(json);
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch { }
        }
    }
}
