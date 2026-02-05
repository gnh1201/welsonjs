using log4net;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    public class JsonRpc2 : IApiEndpoint
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly ILog _logger;
        private const string Prefix = "jsonrpc2";

        public JsonRpc2(ResourceServer server, HttpClient httpClient, ILog logger)
        {
            Server = server;

            _httpClient = httpClient;
            _logger = logger;
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            if (context == null)
                return false;

            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                return false;

            if (path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) == false)
                return false;

            string contentType = context.Request.ContentType?.ToLowerInvariant() ?? string.Empty;
            if (!(contentType.StartsWith("application/json")
                  || contentType.StartsWith("application/json-rpc")
                  || contentType.StartsWith("application/jsonrpc")
                  || contentType.StartsWith("text/json")))
                return false;

            if (!context.Request.HasEntityBody)
                return false;

            return true;
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            string body;
            try
            {
                using (var input = context.Request.InputStream)
                using (var reader = new System.IO.StreamReader(input, System.Text.Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync();
                }

                _logger.Debug($"[JsonRpc2] Request body received ({body.Length} bytes)");
            }
            catch (Exception ex)
            {
                _logger.Error("[JsonRpc2] Failed to read request body", ex);

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
                return;
            }

            var dispatcher = new JsonRpc2Dispatcher(_logger);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300)))
            {
                await dispatcher.HandleAsync(
                    body,
                    async (method, ser, ct) =>
                    {
                        switch (method)
                        {
                            case "tools/list":
                                await Server.ServeResource(context, ResourceServer.GetResource("McpToolsList.json"), "application/json");
                                break;

                            case "tools/call":
                                // TODO: implement tool calling
                                break;
                        }

                        return string.Empty;
                    },
                    cts.Token);
            }
        }
    }
}
