using System.Net;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Text;

namespace WelsonJS.Launcher.ResourceTools
{
    public class Whois : IResourceTool
    {
        private ResourceServer Server;
        private const string Prefix = "whois/";

        public Whois(ResourceServer server)
        {
            Server = server;
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
                Server.ServeResource(context, "<error>Invalid query parameter</error>", "application/xml", 400);
                return;
            }

            string whoisServerUrl = "https://xn--c79as89aj0e29b77z.xn--3e0b707e";

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{whoisServerUrl}/kor/whois.jsc")
                {
                    Content = new StringContent($"query={Uri.EscapeDataString(query)}&ip=141.101.82.1", Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 Edg/134.0.3124.77");
                client.DefaultRequestHeaders.Referrer = new Uri($"{whoisServerUrl}/kor/whois/whois.jsp");

                try
                {
                    HttpResponseMessage response = await client.SendAsync(request);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    Server.ServeResource(context, responseBody, "text/plain", (int)response.StatusCode);
                }
                catch (Exception ex)
                {
                    Server.ServeResource(context, $"<error>Failed to process WHOIS request. {ex.Message}</error>", "application/xml", 500);
                }
            }
        }
    }
}
