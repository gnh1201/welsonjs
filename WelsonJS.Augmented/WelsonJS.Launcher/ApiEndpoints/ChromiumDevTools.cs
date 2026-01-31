// ChromiumDevTools.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using log4net;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    public class ChromiumDevTools : IApiEndpoint
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly ILog _logger;
        private readonly WebSocketManager _wsManager = new WebSocketManager();
        private const string Prefix = "devtools/";

        public ChromiumDevTools(ResourceServer server, HttpClient httpClient, ILog logger)
        {
            Server = server;

            _httpClient = httpClient;
            _logger = logger;
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            return path != null && path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
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
                    await Server.ServeResource(context, data, "application/json");
                }
                catch (Exception ex)
                {
                    await Server.ServeResource(context, $"<error>Failed to process DevTools request. {EscapeXml(ex.Message)}</error>", "application/xml", 500);
                }
                return;
            }

            if (endpoint.StartsWith("page/", StringComparison.OrdinalIgnoreCase))
            {
                // read the variable
                string baseUrl = Program.GetAppConfig("ChromiumDevToolsPrefix");
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri))
                {
                    await Server.ServeResource(context, "<error>Invalid ChromiumDevToolsPrefix</error>", "application/xml", 500);
                    return;
                }

                string hostname = uri.Host;
                int port = uri.Port;

                // override the port number: ?port=1234
                string portQuery = context.Request.QueryString["port"];
                if (!string.IsNullOrEmpty(portQuery))
                {
                    int.TryParse(portQuery, out int parsedPort);
                    if (parsedPort > 0) port = parsedPort;
                }

                // set timeout
                int timeout = 5;
                string timeoutConfig = Program.GetAppConfig("ChromiumDevToolsTimeout");
                if (!string.IsNullOrEmpty(timeoutConfig))
                    int.TryParse(timeoutConfig, out timeout);

                // targeted WebSocket path
                string wsPath = "devtools/" + endpoint;

                // read the body messsage
                string postBody;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    postBody = await reader.ReadToEndAsync();
                }

                // try to communicate
                try
                {
                    string response = await _wsManager.SendAndReceiveAsync(hostname, port, wsPath, postBody, timeout);
                    await Server.ServeResource(context, response, "application/json", 200);
                }
                catch (Exception ex)
                {
                    _wsManager.Remove(hostname, port, wsPath);
                    await Server.ServeResource(context, $"<error>WebSocket communication error: {EscapeXml(ex.Message)}</error>", "application/xml", 500);
                }
                return;
            }

            await Server.ServeResource(context, "<error>Invalid DevTools endpoint</error>", "application/xml", 404);
        }

        private string EscapeXml(string text)
        {
            return SecurityElement.Escape(text);
        }
    }
}
