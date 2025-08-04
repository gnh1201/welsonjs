// WebSocketManager.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    public class WebSocketManager
    {
        private class Entry
        {
            public ClientWebSocket Socket;
            public string Host;
            public int Port;
            public string Path;
        }

        private readonly ConcurrentDictionary<string, Entry> _pool = new ConcurrentDictionary<string, Entry>();

        // Create a unique cache key using MD5 hash
        private string MakeKey(string host, int port, string path)
        {
            string raw = host + ":" + port + "/" + path;
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        // Get an open WebSocket or connect a new one
        public async Task<ClientWebSocket> GetOrCreateAsync(string host, int port, string path)
        {
            string key = MakeKey(host, port, path);

            if (_pool.TryGetValue(key, out var entry))
            {
                var sock = entry.Socket;

                if (sock == null || sock.State != WebSocketState.Open)
                {
                    Remove(host, port, path);
                }
                else
                {
                    return sock;
                }
            }

            var newSock = new ClientWebSocket();
            var uri = new Uri($"ws://{host}:{port}/{path}");

            try
            {
                await newSock.ConnectAsync(uri, CancellationToken.None);

                _pool[key] = new Entry
                {
                    Socket = newSock,
                    Host = host,
                    Port = port,
                    Path = path
                };

                return newSock;
            }
            catch (Exception ex)
            {
                newSock.Dispose();
                Remove(host, port, path);
                throw new WebSocketException("WebSocket connection failed", ex);
            }
        }

        // Remove a socket from the pool and dispose it
        public void Remove(string host, int port, string path)
        {
            string key = MakeKey(host, port, path);
            if (_pool.TryRemove(key, out var entry))
            {
                try
                {
                    entry.Socket?.Abort();
                    entry.Socket?.Dispose();
                }
                catch { /* Ignore dispose exceptions */ }
            }
        }

        // Send and receive with automatic retry on first failure
        public async Task<string> SendAndReceiveAsync(string host, int port, string path, string message, int timeoutSec)
        {
            byte[] buf = Encoding.UTF8.GetBytes(message);
            var cts = timeoutSec > 0
                ? new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec))
                : new CancellationTokenSource();

            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    return await TrySendAndReceiveAsync(host, port, path, buf, cts.Token);
                }
                catch
                {
                    Remove(host, port, path);
                    if (attempt == 1) throw;
                }
            }

            throw new InvalidOperationException("Unreachable");
        }

        // Actual send and receive implementation
        private async Task<string> TrySendAndReceiveAsync(string host, int port, string path, byte[] buf, CancellationToken token)
        {
            try
            {
                var sock = await GetOrCreateAsync(host, port, path);

                if (sock.State != WebSocketState.Open)
                    throw new WebSocketException("WebSocket is not in an open state");

                await sock.SendAsync(new ArraySegment<byte>(buf), WebSocketMessageType.Text, true, token);

                byte[] recv = new byte[4096];
                var result = await sock.ReceiveAsync(new ArraySegment<byte>(recv), token);

                return Encoding.UTF8.GetString(recv, 0, result.Count);
            }
            catch (WebSocketException ex)
            {
                throw new InvalidOperationException("WebSocket communication error", ex);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("WebSocket operation timed out");
            }
        }
    }
}
