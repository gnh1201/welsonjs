// DevTools.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX - FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ResourceTools
{
    public class DevTools : IResourceTool
    {
        private ResourceServer Server;
        private readonly HttpClient _httpClient;
        private const string Prefix = "devtools/";

        public DevTools(ResourceServer server, HttpClient httpClient)
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

            try
            {
                string url = Program.GetAppConfig("DevToolsPrefix") + endpoint;
                string data = await _httpClient.GetStringAsync(url);

                Server.ServeResource(context, data, "application/json");
            }
            catch (Exception ex)
            {
                Server.ServeResource(context, $"<error>Failed to process DevTools request. {ex.Message}</error>", "application/xml", 500);
            }
        }
    }
}
