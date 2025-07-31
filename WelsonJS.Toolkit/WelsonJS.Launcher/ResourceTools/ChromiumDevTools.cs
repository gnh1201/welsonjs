// ChromiumDevTools.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ResourceTools
{
    public class ChromiumDevTools : IResourceTool
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly WebSocketManager _wsManager = new WebSocketManager();
        private const string Prefix = "devtools/";

        public ChromiumDevTools(ResourceServer server, HttpClient httpClient)
        {
            Server = server;
            _httpClient = httpClient;
        }

        public bool CanHandle(string path)
        {
            return path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            string endpoint = path.Substring(Prefix.Length);

            if (endpoint.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string baseUrl = Program.GetAppConfig("ChromiumDevToolsPrefix"); // e.g., http://localhost:9222/
                    string url = baseUrl.TrimEnd('/') + "/" + endpoint;
                    string data = await _httpClient.GetStringAsync(url);
                    Server.ServeResource(context, data, "application/json");
                }
                catch (Exception ex)
                {
                    Server.ServeResource(context, $"<error>Failed to process DevTools request. {EscapeXml(ex.Message)}</error>", "application/xml", 500);
                }
                return;
            }

            if (endpoint.StartsWith("page/", StringComparison.OrdinalIgnoreCase))
            {
                // 기본 구성
                string baseUrl = Program.GetAppConfig("ChromiumDevToolsPrefix");
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri))
                {
                    Server.ServeResource(context, "<error>Invalid ChromiumDevToolsPrefix</error>", "application/xml", 500);
                    return;
                }

                string hostname = uri.Host;
                int port = uri.Port;

                // 포트 덮어쓰기: ?port=1234
                string portQuery = context.Request.QueryString["port"];
                if (!string.IsNullOrEmpty(portQuery))
                {
                    int.TryParse(portQuery, out int parsedPort);
                    if (parsedPort > 0) port = parsedPort;
                }

                // 타임아웃 처리
                int timeout = 5;
                string timeoutConfig = Program.GetAppConfig("ChromiumDevToolsTimeout");
                if (!string.IsNullOrEmpty(timeoutConfig))
                    int.TryParse(timeoutConfig, out timeout);

                // 경로
                string wsPath = "devtools/" + endpoint;

                // 본문 읽기
                string postBody;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    postBody = await reader.ReadToEndAsync();

                ClientWebSocket ws;
                try
                {
                    ws = await _wsManager.GetOrCreateAsync(hostname, port, wsPath);
                }
                catch (Exception ex)
                {
                    Server.ServeResource(context, $"<error>WebSocket connection failed: {EscapeXml(ex.Message)}</error>", "application/xml", 502);
                    return;
                }

                try
                {
                    var sendBuffer = Encoding.UTF8.GetBytes(postBody);
                    var sendToken = timeout == 0 ? CancellationToken.None : new CancellationTokenSource(TimeSpan.FromSeconds(timeout)).Token;
                    await ws.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, sendToken);

                    var recvBuffer = new byte[4096];
                    var recvToken = timeout == 0 ? CancellationToken.None : new CancellationTokenSource(TimeSpan.FromSeconds(timeout)).Token;
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(recvBuffer), recvToken);

                    string response = Encoding.UTF8.GetString(recvBuffer, 0, result.Count);
                    Server.ServeResource(context, response, "application/json", 200);
                }
                catch (OperationCanceledException)
                {
                    Server.ServeResource(context, "<error>Timeout occurred</error>", "application/xml", 504);
                }
                catch (Exception ex)
                {
                    _wsManager.Remove(hostname, port, wsPath);
                    Server.ServeResource(context, $"<error>WebSocket communication error: {EscapeXml(ex.Message)}</error>", "application/xml", 500);
                }
                return;
            }

            Server.ServeResource(context, "<error>Invalid DevTools endpoint</error>", "application/xml", 404);
        }

        private string EscapeXml(string text)
        {
            return WebUtility.HtmlEncode(text);
        }
    }
}
