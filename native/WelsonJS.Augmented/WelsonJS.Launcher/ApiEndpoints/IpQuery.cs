// IpQuery.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
// PLEASE NOTE:
// Requires joining the IP Query API providers to provide IP information.
// WelsonJS has no affiliation with any IP Query API providers.
//
// Providers: the addresses come from the blob config (services/ipQuery/provider, by
// name), the API keys from the local settings:
//   criminalip -> IpQueryApiKey
//   abuseipdb  -> IpQueryApiKey2
//
// XML response structure:
// <result target="1.1.1.1">
//   <response provider="criminalip" status="200"><text>{"...json..."}</text></response>
//   <response provider="abuseipdb"  status="200"><text>{"...json..."}</text></response>
// </result>
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace WelsonJS.Launcher.ApiEndpoints
{
    public class IpQuery : IApiEndpoint
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private const string Prefix = "ip-query/";

        // The supported providers (BuildProviderRequest) with the settings of their API keys.
        private static readonly Dictionary<string, string> ApiKeySettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["criminalip"] = "IpQueryApiKey",
            ["abuseipdb"] = "IpQueryApiKey2"
        };

        public IpQuery(ResourceServer server, HttpClient httpClient, ILogger logger)
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
            try
            {
                string target = path.Substring(Prefix.Length).Trim();
                if (string.IsNullOrWhiteSpace(target))
                {
                    await Server.ServeResource(context, "<error>Missing IP target</error>", "application/xml", 400);
                    return;
                }

                List<IpQueryProvider> providers = (await Server.GetBlobConfigAsync())?.Services?.IpQueryProviders;
                if (providers == null || providers.Count == 0)
                {
                    _logger.Error("The IP query providers are not configured (blob config: services/ipQuery).");
                    await Server.ServeResource(context, "<error>The IP query providers are not configured. Check the network connection and try again.</error>", "application/xml", 503);
                    return;
                }

                var root = new XElement("result", new XAttribute("target", target));

                XElement[] results = await Task.WhenAll(providers
                    .Where(p => p.Name != null && ApiKeySettings.ContainsKey(p.Name))
                    .Select(p => QueryProviderAsync(context, target, p.Name, p.PrefixUrl, Program.GetAppConfig(ApiKeySettings[p.Name]))));

                foreach (XElement result in results)
                    root.Add(result);

                bool anySuccess = results.Any(r => int.TryParse((string)r.Attribute("status"), out int s) && s >= 200 && s < 300);

                int httpCode = anySuccess ? 200 : 502;

                context.Response.StatusCode = httpCode;
                await Server.ServeResource(context, root.ToString(), "application/xml", httpCode);
            }
            catch (Exception ex)
            {
                _logger.Error("Error processing IP query request: " + ex.Message);
                await Server.ServeResource(context, "<error>" + WebUtility.HtmlEncode(ex.Message) + "</error>", "application/xml", 500);
            }
        }

        private async Task<XElement> QueryProviderAsync(HttpListenerContext ctx, string ip, string provider, string prefix, string key)
        {
            var node = new XElement("response", new XAttribute("provider", provider));

            if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(key))
            {
                node.Add(new XAttribute("status", 503));
                node.Add(new XElement("error", "Missing configuration for " + provider));
                return node;
            }

            try
            {
                HttpRequestMessage req = BuildProviderRequest(ctx, ip, provider, prefix, key);
                try
                {
                    using (HttpResponseMessage resp = await _httpClient.SendAsync(req))
                    {
                        string body = JsonCodec.Format(await resp.Content.ReadAsStringAsync(), 4);
                        node.Add(new XAttribute("status", (int)resp.StatusCode));
                        node.Add(new XElement("text", body));
                        return node;
                    }
                }
                finally
                {
                    req.Dispose();
                }
            }
            catch (Exception ex)
            {
                node.Add(new XAttribute("status", 500));
                node.Add(new XElement("error", ex.Message));
                return node;
            }
        }

        private static HttpRequestMessage BuildProviderRequest(HttpListenerContext ctx, string ip, string provider, string prefix, string key)
        {
            HttpRequestMessage req;

            if (string.Equals(provider, "criminalip", StringComparison.OrdinalIgnoreCase))
            {
                string url = prefix.TrimEnd('/') + "/asset/ip/report?ip=" + Uri.EscapeDataString(ip) + "&full=true";
                req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.TryAddWithoutValidation("x-api-key", key);
            }
            else if (string.Equals(provider, "abuseipdb", StringComparison.OrdinalIgnoreCase))
            {
                var ub = new UriBuilder(prefix.TrimEnd('/') + "/check");
                var q = HttpUtility.ParseQueryString(ub.Query);
                q["ipAddress"] = ip;
                q["maxAgeInDays"] = "90";
                q["verbose"] = "";
                ub.Query = q.ToString();

                req = new HttpRequestMessage(HttpMethod.Get, ub.Uri);
                req.Headers.TryAddWithoutValidation("Accept", "application/json");
                req.Headers.TryAddWithoutValidation("Key", key);
            }
            else
            {
                throw new ArgumentException("Unsupported provider: " + provider);
            }

            if (!string.IsNullOrEmpty(ctx.Request.UserAgent))
            {
                req.Headers.TryAddWithoutValidation("User-Agent", ctx.Request.UserAgent);
            }

            return req;
        }
    }
}