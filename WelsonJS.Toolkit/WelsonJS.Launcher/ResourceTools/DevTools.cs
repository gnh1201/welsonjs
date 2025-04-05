using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ResourceTools
{
    public class DevTools : IResourceTool
    {
        private ResourceServer Server;
        private const string Prefix = "devtools/";
        private const double Timeout = 5000;

        public DevTools(ResourceServer server)
        {
            Server = server;
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
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(Timeout);

                    string url = Program.GetAppConfig("DevToolsPrefix") + endpoint;
                    string data = await client.GetStringAsync(url);

                    Server.ServeResource(context, data, "application/json");
                }
            }
            catch (Exception ex)
            {
                Server.ServeResource(context, $"<error>Failed to process DevTools request. {ex.Message}</error>", "application/xml", 500);
            }
        }
    }
}
