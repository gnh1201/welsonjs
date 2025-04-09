using System;
using System.Net;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ResourceTools
{
    public class AzureAi : IResourceTool
    {
        private ResourceServer Server;
        private const string Prefix = "azure-ai/";

        public AzureAi(ResourceServer server)
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

            // TODO: Pass Azure AI request to Azure AI Foundry
        }
    }
}