using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ResourceTools
{
    public class AzureAi : IResourceTool
    {
        private ResourceServer Server;
        private const string Prefix = "azure-ai/";
        private const int ChunkSize = 4096;
        private readonly string AzureAiServiceUrl;
        private readonly string AzureAiServiceApiKey;

        public AzureAi(ResourceServer server)
        {
            Server = server;

            AzureAiServiceUrl = Program.GetAppConfig("AzureAiServiceUrl");
            AzureAiServiceApiKey = Program.GetAppConfig("AzureAiServiceApiKey");
        }

        public bool CanHandle(string path)
        {
            return path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            string apiKey = AzureAiServiceApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                WriteError(context, "Missing the API key.", HttpStatusCode.BadRequest);
                return;
            }

            string requestBody = ReadRequestBody(context);
            bool isStreaming = ContainsSequentialKeywords(requestBody, new[] { "\"stream\"", "true" }, 30);

            try
            {
                using (var response = await SendAzureRequestAsync(apiKey, requestBody, isStreaming))
                {
                    await ForwardResponseAsync(context, response, isStreaming);
                }
            }
            catch (HttpRequestException ex)
            {
                WriteError(context, "Request error: " + ex.Message, HttpStatusCode.BadGateway);
            }
        }

        private string ReadRequestBody(HttpListenerContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        private async Task<HttpResponseMessage> SendAzureRequestAsync(string apiKey, string requestBody, bool isStreaming)
        {
            using (var client = new HttpClient())
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, AzureAiServiceUrl);
                requestMessage.Headers.Add("api-key", apiKey);
                requestMessage.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var completionOption = isStreaming
                    ? HttpCompletionOption.ResponseHeadersRead
                    : HttpCompletionOption.ResponseContentRead;

                return await client.SendAsync(requestMessage, completionOption);
            }
        }

        private async Task ForwardResponseAsync(HttpListenerContext context, HttpResponseMessage response, bool isStreaming)
        {
            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

            using (var responseStream = await response.Content.ReadAsStreamAsync())
            {
                if (isStreaming)
                {
                    context.Response.SendChunked = true;
                    context.Response.Headers.Add("Transfer-Encoding", "chunked");
                    context.Response.Headers.Add("Cache-Control", "no-cache");

                    byte[] buffer = new byte[ChunkSize];
                    int bytesRead;
                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await context.Response.OutputStream.WriteAsync(buffer, 0, bytesRead);
                        context.Response.OutputStream.Flush();
                    }
                }
                else
                {
                    await responseStream.CopyToAsync(context.Response.OutputStream);
                }
            }
        }

        private void WriteError(HttpListenerContext context, string message, HttpStatusCode statusCode)
        {
            context.Response.StatusCode = (int)statusCode;
            using (var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8))
            {
                writer.Write(message);
            }
        }

        public static bool ContainsSequentialKeywords(string input, string[] keywords, int maxDistance)
        {
            if (input == null || keywords == null || keywords.Length < 2)
                return false;

            int position = 0;

            for (int i = 0; i < keywords.Length; i++)
            {
                int index = input.IndexOf(keywords[i], position, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    return false;

                if (i > 0 && (index - position) > maxDistance)
                    return false;

                position = index + keywords[i].Length;
            }

            return true;
        }
    }
}