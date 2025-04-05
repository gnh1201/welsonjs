using System;
using System.Net;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ResourceTools
{
    public class Config : IResourceTool
    {
        private ResourceServer Server;
        private const string Prefix = "config/";

        public Config(ResourceServer server)
        {
            Server = server;
        }

        public bool CanHandle(string path)
        {
            return path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            await Task.Delay(0);

            string configName = path.Substring(Prefix.Length);

            try
            {
                string configValue = Program.GetAppConfig(configName);
                Server.ServeResource(context, configValue, "text/plain");
            }
            catch (Exception ex)
            {
                Server.ServeResource(context, $"<error>Failed to process Config request. {ex.Message}</error>", "application/xml", 500);
            }
        }
    }
}
