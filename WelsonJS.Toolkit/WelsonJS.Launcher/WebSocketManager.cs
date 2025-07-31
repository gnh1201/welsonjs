// WebSocketManager.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    public class WebSocketManager
    {
        private class WebSocketEntry
        {
            public ClientWebSocket Socket { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public string Path { get; set; }
        }

        private readonly ConcurrentDictionary<string, WebSocketEntry> _wsPool = new ConcurrentDictionary<string, WebSocketEntry>();

        private string MakeKey(string host, int port, string path)
        {
            // To create a unique key for the WebSocket connection
            string input = host + ":" + port + "/" + path;
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public async Task<ClientWebSocket> GetOrCreateAsync(string host, int port, string path)
        {
            string key = MakeKey(host, port, path);

            if (_wsPool.TryGetValue(key, out var entry) && entry.Socket?.State == WebSocketState.Open)
                return entry.Socket;

            // 재연결 필요
            if (entry != null)
            {
                _wsPool.TryRemove(key, out _);
                entry.Socket?.Dispose();
            }

            var ws = new ClientWebSocket();
            Uri uri = new Uri($"ws://{host}:{port}/{path}");

            try
            {
                await ws.ConnectAsync(uri, CancellationToken.None);
                _wsPool[key] = new WebSocketEntry
                {
                    Socket = ws,
                    Host = host,
                    Port = port,
                    Path = path
                };
                return ws;
            }
            catch
            {
                ws.Dispose();
                throw;
            }
        }

        public void Remove(string host, int port, string path)
        {
            string key = MakeKey(host, port, path);
            if (_wsPool.TryRemove(key, out var entry))
            {
                entry.Socket?.Abort();
                entry.Socket?.Dispose();
            }
        }

        public async Task<bool> SendWithReconnectAsync(string host, int port, string path, byte[] message, CancellationToken token)
        {
            ClientWebSocket ws;

            try
            {
                ws = await GetOrCreateAsync(host, port, path);
                await ws.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, token);
                return true;
            }
            catch
            {
                Remove(host, port, path);
                try
                {
                    ws = await GetOrCreateAsync(host, port, path);
                    await ws.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, token);
                    return true;
                }
                catch
                {
                    Remove(host, port, path);
                    return false;
                }
            }
        }

        public async Task<string> SendAndReceiveAsync(string host, int port, string path, string message, int timeoutSeconds, int bufferSize = 65536)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            CancellationTokenSource cts = timeoutSeconds > 0
                ? new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds))
                : new CancellationTokenSource();

            if (!await SendWithReconnectAsync(host, port, path, buffer, cts.Token))
                throw new IOException("Failed to send after reconnect");

            ClientWebSocket ws = await GetOrCreateAsync(host, port, path);

            byte[] recvBuffer = new byte[bufferSize];
            WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(recvBuffer), cts.Token);
            return Encoding.UTF8.GetString(recvBuffer, 0, result.Count);
        }
    }
}
