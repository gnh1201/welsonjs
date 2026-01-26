// Whois.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using log4net;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ResourceTools
{
    public class Whois : IApiEndpoint
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly ILog _logger;
        private const string Prefix = "whois/";

        public Whois(ResourceServer server, HttpClient httpClient, ILog logger)
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
            string query = path.Substring(Prefix.Length);

            if (string.IsNullOrWhiteSpace(query) || query.Length > 255)
            {
                _logger.Error("Invalid WHOIS query parameter.");
                await Server.ServeResource(context, "<error>Invalid query parameter</error>", "application/xml", 400);
                return;
            }

            string clientAddress = Program.GetAppConfig("WhoisClientAddress");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Program.GetAppConfig("WhoisServerUrl"))
            {
                Content = new StringContent($"query={Uri.EscapeDataString(query)}&ip={clientAddress}", Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("User-Agent", context.Request.UserAgent);
            _httpClient.DefaultRequestHeaders.Referrer = new Uri(Program.GetAppConfig("WhoisReferrerUrl"));

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                await Server.ServeResource(context, responseBody, "text/plain", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.Error("Error processing WHOIS request: " + ex.Message);
                await Server.ServeResource(context, $"<error>Failed to process WHOIS request. {ex.Message}</error>", "application/xml", 500);
            }
        }
    }
}
