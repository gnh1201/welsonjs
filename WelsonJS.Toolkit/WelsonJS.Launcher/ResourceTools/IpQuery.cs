// CitiQuery.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ResourceTools
{
    public class IpQuery : IResourceTool
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly ICompatibleLogger _logger;
        private const string Prefix = "ip-query/";

        public IpQuery(ResourceServer server, HttpClient httpClient, ICompatibleLogger logger)
        {
            Server = server;

            _httpClient = httpClient;
            _logger = logger;
        }

        public bool CanHandle(string path)
        {
            return path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            try
            {
                string target = path.Substring(Prefix.Length).Trim();
                string apiKey = Program.GetAppConfig("CriminalIpApiKey");
                if (string.IsNullOrEmpty(apiKey))
                {
                    await Server.ServeResource(context, "<error>Missing API key</error>", "application/xml", 500);
                    return;
                }

                string encoded = Uri.EscapeDataString(target);
                string apiPrefix = Program.GetAppConfig("CriminalIpApiPrefix");
                string url = $"{apiPrefix}asset/ip/report?ip={encoded}&full=true";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("User-Agent", context.Request.UserAgent);

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();

                context.Response.StatusCode = (int)response.StatusCode;
                await Server.ServeResource(context, content, "application/json", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                await Server.ServeResource(context, $"<error>{ex.Message}</error>", "application/xml", 500);
            }
        }
    }
}
