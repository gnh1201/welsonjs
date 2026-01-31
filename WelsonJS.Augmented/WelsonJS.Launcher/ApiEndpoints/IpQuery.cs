// IpQuery.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
// PLEASE NOTE:
// Requires joining the IP Query API providers to provide IP information.
// WelsonJS has no affiliation with any IP Query API providers.
//
// Providers:
//   1) CriminalIP   -> IpQueryApiPrefix,  IpQueryApiKey
//   2) AbuseIPDB    -> IpQueryApiPrefix2, IpQueryApiKey2
//
// XML response structure:
// <result target="1.1.1.1">
//   <response provider="criminalip" status="200"><text>{"...json..."}</text></response>
//   <response provider="abuseipdb"  status="200"><text>{"...json..."}</text></response>
// </result>
//
using log4net;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace WelsonJS.Launcher.ResourceTools
{
    public class IpQuery : IApiEndpoint
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly ILog _logger;
        private const string Prefix = "ip-query/";

        public IpQuery(ResourceServer server, HttpClient httpClient, ILog logger)
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

                string crimPrefix = Program.GetAppConfig("IpQueryApiPrefix");
                string crimKey = Program.GetAppConfig("IpQueryApiKey");

                string abusePrefix = Program.GetAppConfig("IpQueryApiPrefix2");
                string abuseKey = Program.GetAppConfig("IpQueryApiKey2");

                var root = new XElement("result", new XAttribute("target", target));

                var p1 = QueryProviderAsync(context, target, "criminalip", crimPrefix, crimKey);
                var p2 = QueryProviderAsync(context, target, "abuseipdb", abusePrefix, abuseKey);

                await Task.WhenAll(p1, p2);

                root.Add(p1.Result);
                root.Add(p2.Result);

                bool anySuccess =
                    (int.TryParse((string)p1.Result.Attribute("status"), out var s1) && s1 >= 200 && s1 < 300) ||
                    (int.TryParse((string)p2.Result.Attribute("status"), out var s2) && s2 >= 200 && s2 < 300);

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
                    using (JsSerializer ser = new JsSerializer())
                    {
                        string body = ser.Pretty(await resp.Content.ReadAsStringAsync(), 4);
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