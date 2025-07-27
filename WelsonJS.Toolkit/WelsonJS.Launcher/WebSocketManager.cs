using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    public class WebSocketManager
    {
        private readonly ConcurrentDictionary<int, ClientWebSocket> _wsPool;

        public WebSocketManager() {
             _wsPool = new ConcurrentDictionary<int, ClientWebSocket>();
        }

        public async Task<ClientWebSocket> GetOrCreateAsync(int port)
        {
            if (_wsPool.TryGetValue(port, out var ws) && ws.State == WebSocketState.Open)
                return ws;

            if (ws != null)
            {
                _wsPool.TryRemove(port, out _);
                ws.Dispose();
            }

            var newWs = new ClientWebSocket();
            var uri = new Uri($"ws://localhost:{port}/ws");

            try
            {
                await newWs.ConnectAsync(uri, CancellationToken.None);
                _wsPool[port] = newWs;
                return newWs;
            }
            catch
            {
                newWs.Dispose();
                throw;
            }
        }

        public void Remove(int port)
        {
            if (_wsPool.TryRemove(port, out var ws))
            {
                ws.Abort();
                ws.Dispose();
            }
        }
    }
}