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
        private const int Timeout = 5000;

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

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMilliseconds(Timeout);

                string clientAddress = Program.GetAppConfig("WhoisClientAddress");

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Program.GetAppConfig("WhoisServerUrl"))
                {
                    Content = new StringContent($"query={Uri.EscapeDataString(query)}&ip={clientAddress}", Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("User-Agent", Program.GetAppConfig("WhoisUserAgent"));
                client.DefaultRequestHeaders.Referrer = new Uri(Program.GetAppConfig("WhoisReferrerUrl"));

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
