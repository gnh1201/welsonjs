// WebSocketManager.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

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
            string raw = host + ":" + port + "/" + path;
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return BitConverter.ToString(hash).Replace("-", "").ToLower(); // 32자
            }
        }

        public async Task<ClientWebSocket> GetOrCreateAsync(string host, int port, string path)
        {
            string key = MakeKey(host, port, path);

            if (_wsPool.TryGetValue(key, out var entry))
            {
                var socket = entry.Socket;

                if (socket != null)
                {
                    if (socket.State == WebSocketState.Open)
                        return socket;

                    Remove(host, port, path);
                }
            }

            var newSocket = new ClientWebSocket();
            var uri = new Uri($"ws://{host}:{port}/{path}");

            try
            {
                await newSocket.ConnectAsync(uri, CancellationToken.None);

                _wsPool[key] = new WebSocketEntry
                {
                    Socket = newSocket,
                    Host = host,
                    Port = port,
                    Path = path
                };

                return newSocket;
            }
            catch
            {
                newSocket.Dispose();
                throw;
            }
        }

        public void Remove(string host, int port, string path)
        {
            string key = MakeKey(host, port, path);
            if (_wsPool.TryRemove(key, out var entry))
            {
                try
                {
                    entry.Socket?.Abort();
                    entry.Socket?.Dispose();
                }
                catch { /* ignore */ }
            }
        }

        public async Task<string> SendAndReceiveAsync(string host, int port, string path, string message, int timeoutSeconds)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var cts = timeoutSeconds > 0 ? new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)) : new CancellationTokenSource();

            try
            {
                var socket = await GetOrCreateAsync(host, port, path);
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);

                var recvBuffer = new byte[4096];
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(recvBuffer), cts.Token);
                return Encoding.UTF8.GetString(recvBuffer, 0, result.Count);
            }
            catch
            {
                Remove(host, port, path);

                try
                {
                    var socket = await GetOrCreateAsync(host, port, path);
                    await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);

                    var recvBuffer = new byte[4096];
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(recvBuffer), cts.Token);
                    return Encoding.UTF8.GetString(recvBuffer, 0, result.Count);
                }
                catch
                {
                    Remove(host, port, path);
                    throw;
                }
            }
        }
    }
}
