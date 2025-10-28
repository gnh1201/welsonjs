// WebSocketManager.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    public sealed class WebSocketManager : ConnectionManagerBase<WebSocketManager.Endpoint, ClientWebSocket>, IManagedConnectionProvider
    {
        private const string ConnectionTypeName = "WebSocket";

        public struct Endpoint
        {
            public Endpoint(string host, int port, string path)
            {
                Host = host ?? throw new ArgumentNullException(nameof(host));
                Port = port;
                Path = path ?? string.Empty;
            }

            public string Host { get; }
            public int Port { get; }
            public string Path { get; }
        }

        public WebSocketManager()
        {
            ConnectionMonitorRegistry.RegisterProvider(this);
        }

        public string ConnectionType => ConnectionTypeName;

        protected override string CreateKey(Endpoint parameters)
        {
            string raw = parameters.Host + ":" + parameters.Port + "/" + parameters.Path;
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        protected override async Task<ClientWebSocket> OpenConnectionAsync(Endpoint parameters, CancellationToken token)
        {
            var socket = new ClientWebSocket();
            var uri = new Uri($"ws://{parameters.Host}:{parameters.Port}/{parameters.Path}");

            try
            {
                await socket.ConnectAsync(uri, token).ConfigureAwait(false);
                return socket;
            }
            catch (Exception ex)
            {
                socket.Dispose();
                throw new WebSocketException("WebSocket connection failed", ex);
            }
        }

        protected override bool IsConnectionValid(ClientWebSocket connection)
        {
            return connection != null && connection.State == WebSocketState.Open;
        }

        protected override void CloseConnection(ClientWebSocket connection)
        {
            try
            {
                connection?.Abort();
            }
            catch
            {
                // Ignore abort exceptions.
            }
            finally
            {
                connection?.Dispose();
            }
        }

        public void Remove(string host, int port, string path)
        {
            Remove(new Endpoint(host, port, path));
        }

        // Send and receive with automatic retry on first failure
        public async Task<string> SendAndReceiveAsync(string host, int port, string path, string message, int timeoutSec)
        {
            byte[] buf = Encoding.UTF8.GetBytes(message);
            var cts = timeoutSec > 0
                ? new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec))
                : new CancellationTokenSource();

            try
            {
                return await ExecuteWithRetryAsync(
                    new Endpoint(host, port, path),
                    (socket, token) => TrySendAndReceiveAsync(socket, buf, token),
                    2,
                    cts.Token).ConfigureAwait(false);
            }
            finally
            {
                cts.Dispose();
            }
        }

        public IReadOnlyCollection<ManagedConnectionStatus> GetStatuses()
        {
            var snapshots = SnapshotConnections();
            var result = new List<ManagedConnectionStatus>(snapshots.Count);

            foreach (var snapshot in snapshots)
            {
                string state;
                try
                {
                    state = snapshot.Connection?.State.ToString() ?? "Unknown";
                }
                catch
                {
                    state = "Unknown";
                }

                var endpoint = snapshot.Parameters;
                var description = $"ws://{endpoint.Host}:{endpoint.Port}/{endpoint.Path}";

                result.Add(new ManagedConnectionStatus(
                    ConnectionTypeName,
                    snapshot.Key,
                    state,
                    description,
                    snapshot.IsValid));
            }

            return result;
        }

        public bool TryClose(string key)
        {
            return TryRemoveByKey(key);
        }

        // Actual send and receive implementation that never truncates the accumulated data.
        // - Uses a fixed-size read buffer ONLY for I/O
        // - Accumulates dynamically into a List<byte[]> until EndOfMessage
        private async Task<string> TrySendAndReceiveAsync(ClientWebSocket socket, byte[] buf, CancellationToken token)
        {
            try
            {
                if (socket.State != WebSocketState.Open)
                    throw new WebSocketException("WebSocket is not in an open state");

                // Send request as a single text frame
                await socket.SendAsync(new ArraySegment<byte>(buf), WebSocketMessageType.Text, true, token).ConfigureAwait(false);

                // Fixed-size read buffer for I/O (does NOT cap total message size)
                byte[] readBuffer = new byte[8192];

                // Dynamic accumulator for all received chunks
                var parts = new System.Collections.Generic.List<byte[]>(8);
                int total = 0;

                while (true)
                {
                    var res = await socket.ReceiveAsync(new ArraySegment<byte>(readBuffer), token).ConfigureAwait(false);

                    if (res.MessageType == WebSocketMessageType.Close)
                    {
                        try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing as requested by server", token).ConfigureAwait(false); } catch { }
                        throw new WebSocketException($"WebSocket closed by server: {socket.CloseStatus} {socket.CloseStatusDescription}");
                    }

                    if (res.Count > 0)
                    {
                        // Copy out exactly the bytes read in this frame fragment
                        var copy = new byte[res.Count];
                        Buffer.BlockCopy(readBuffer, 0, copy, 0, res.Count);
                        parts.Add(copy);
                        total += res.Count;
                    }

                    if (res.EndOfMessage)
                        break; // Full logical message received
                }

                // Concatenate all parts into a single byte[] sized to the exact total
                var payload = new byte[total];
                int offset = 0;
                for (int i = 0; i < parts.Count; i++)
                {
                    var p = parts[i];
                    Buffer.BlockCopy(p, 0, payload, offset, p.Length);
                    offset += p.Length;
                }

                // Decode as UTF-8 text (adjust if Binary messages are expected)
                return Encoding.UTF8.GetString(payload);
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