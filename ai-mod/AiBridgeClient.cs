using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DshAiMini
{
    /// <summary>
    /// AI Mini 本机角色使用的标准桥客户端。
    /// 即使调用方和桥在同一个 tModLoader 进程，也走同一套 TCP JSON 协议，
    /// 这样本机和 Bot 的能力、参数格式、回读结果保持一致。
    /// </summary>
    internal static class AiBridgeClient
    {
        public static async Task<string> RequestAsync(int port, string command, string paramsJson,
                                                       CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("bridge 需要 command", nameof(command));

            JsonElement parameters;
            using (var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(paramsJson) ? "{}" : paramsJson))
                parameters = doc.RootElement.Clone();

            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            await client.ConnectAsync("127.0.0.1", port, timeout.Token);
            client.NoDelay = true;
            using NetworkStream stream = client.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            string request = JsonSerializer.Serialize(new { cmd = command, @params = parameters });
            await writer.WriteLineAsync(request.AsMemory(), timeout.Token);
            string response = await reader.ReadLineAsync(timeout.Token);
            if (string.IsNullOrWhiteSpace(response)) throw new IOException("桥返回空响应");
            return response;
        }
    }
}
