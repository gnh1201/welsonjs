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
        private class Entry
        {
            public ClientWebSocket Socket;
            public string Host;
            public int Port;
            public string Path;
            // Ensures that send/receive is serialized per socket
            public readonly SemaphoreSlim IoLock = new SemaphoreSlim(1, 1);
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

        // Get an existing open WebSocket entry or create a new one
        private async Task<Entry> GetOrCreateAsync(string host, int port, string path)
        {
            string key = MakeKey(host, port, path);

            if (_pool.TryGetValue(key, out var entry))
            {
                var sock = entry.Socket;
                if (sock != null && sock.State == WebSocketState.Open)
                    return entry;

                Remove(host, port, path);
            }

            var newSock = new ClientWebSocket();

            // Build the WebSocket URI safely
            var ub = new UriBuilder
            {
                Scheme = "ws",
                Host = host,
                Port = port,
                Path = string.IsNullOrEmpty(path) ? "/" : (path.StartsWith("/") ? path : "/" + path)
            };

            try
            {
                await newSock.ConnectAsync(ub.Uri, CancellationToken.None);

                var newEntry = new Entry
                {
                    Socket = newSock,
                    Host = host,
                    Port = port,
                    Path = path
                };

                _pool[key] = newEntry;
                return newEntry;
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
                finally
                {
                    try { entry.IoLock?.Dispose(); } catch { }
                }
            }
        }

        // Send a message and receive a response, with automatic retry on first failure
        public async Task<string> SendAndReceiveAsync(string host, int port, string path, string message, int timeoutSec, int maxMessageBytes = 8 * 1024 * 1024)
        {
            var cts = timeoutSec > 0
                ? new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec))
                : new CancellationTokenSource();

            byte[] buf = Encoding.UTF8.GetBytes(message);

            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    return await TrySendAndReceiveAsync(host, port, path, buf, cts.Token, maxMessageBytes);
                }
                catch
                {
                    Remove(host, port, path);
                    if (attempt == 1) throw;
                }
            }

            throw new InvalidOperationException("Unreachable");
        }

        // Actual send/receive logic with full-frame accumulation until EndOfMessage
        private async Task<string> TrySendAndReceiveAsync(string host, int port, string path, byte[] sendBuf, CancellationToken token, int maxMessageBytes)
        {
            try
            {
                var entry = await GetOrCreateAsync(host, port, path);
                var sock = entry.Socket;

                if (sock.State != WebSocketState.Open)
                    throw new WebSocketException("WebSocket is not in an open state");

                await entry.IoLock.WaitAsync(token);
                try
                {
                    // Send message
                    await sock.SendAsync(new ArraySegment<byte>(sendBuf), WebSocketMessageType.Text, true, token);

                    // Receive message until EndOfMessage
                    byte[] buffer = new byte[8192];
                    using (var ms = new MemoryStream())
                    {
                        while (true)
                        {
                            var res = await sock.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                            if (res.MessageType == WebSocketMessageType.Close)
                            {
                                // Server requested closure
                                try { await sock.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing as requested by server", token); } catch { }
                                throw new WebSocketException($"WebSocket closed by server: {sock.CloseStatus} {sock.CloseStatusDescription}");
                            }

                            if (res.Count > 0)
                            {
                                ms.Write(buffer, 0, res.Count);

                                if (ms.Length > maxMessageBytes)
                                    throw new InvalidOperationException($"Received message exceeds limit ({maxMessageBytes} bytes).");
                            }

                            if (res.EndOfMessage)
                                break;
                        }

                        // Convert UTF-8 encoded text message to string
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
                finally
                {
                    entry.IoLock.Release();
                }
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
