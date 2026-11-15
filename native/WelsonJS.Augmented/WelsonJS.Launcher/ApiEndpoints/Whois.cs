// Whois.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    public class Whois : IApiEndpoint
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private const string Prefix = "whois/";

        public Whois(ResourceServer server, HttpClient httpClient, ILogger logger)
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
            string query = path.Substring(Prefix.Length);

            if (string.IsNullOrWhiteSpace(query) || query.Length > 255)
            {
                _logger.Error("Invalid WHOIS query parameter.");
                await Server.ServeResource(context, "<error>Invalid query parameter</error>", "application/xml", 400);
                return;
            }

            // The service's addresses come from the blob config (services/whois).
            WhoisService service = (await Server.GetBlobConfigAsync())?.Services?.Whois;
            if (service == null || string.IsNullOrWhiteSpace(service.ServerUrl))
            {
                _logger.Error("The WHOIS service is not configured (blob config: services/whois).");
                await Server.ServeResource(context, "<error>The WHOIS service is not configured. Check the network connection and try again.</error>", "application/xml", 503);
                return;
            }

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, service.ServerUrl)
                {
                    Content = new StringContent($"query={Uri.EscapeDataString(query)}&ip={Uri.EscapeDataString(service.ClientAddress ?? "")}", Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                request.Headers.Add("Accept", "*/*");
                if (!string.IsNullOrEmpty(context.Request.UserAgent))
                    request.Headers.TryAddWithoutValidation("User-Agent", context.Request.UserAgent);
                if (Uri.TryCreate(service.ReferrerUrl, UriKind.Absolute, out Uri referrer))
                    request.Headers.Referrer = referrer;

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
