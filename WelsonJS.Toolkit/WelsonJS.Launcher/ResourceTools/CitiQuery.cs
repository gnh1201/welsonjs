using System;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ResourceTools
{
    public class CitiQuery : IResourceTool
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private const string Prefix = "citi-query/";

        public CitiQuery(ResourceServer server, HttpClient httpClient)
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
            try
            {
                string target = path.Substring(Prefix.Length).Trim();
                string apiKey = Program.GetAppConfig("CitiApiKey");
                if (string.IsNullOrEmpty(apiKey))
                {
                    Server.ServeResource(context, "<error>Missing API key<error>", "application/xml", 500);
                    return;
                }

                string encoded = Uri.EscapeDataString(target);
                string apiPrefix = Program.GetAppConfig("CitiApiPrefix");
                string url = $"{apiPrefix}asset/ip/report?ip={encoded}&full=true";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("User-Agent", context.Request.UserAgent);

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();

                context.Response.StatusCode = (int)response.StatusCode;
                Server.ServeResource(context, content, "application/json", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                Server.ServeResource(context, $"<error>{ex.Message}</error>", "application/xml", 500);
            }
        }
    }
}
