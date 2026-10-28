// WelsonJsMcp.cs
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WelsonJS.Launcher.ApiEndpoints
{
    public sealed class WelsonJsMcp : IApiEndpoint
    {
        private const string Prefix = "welsonjs-mcp";
        private const long MaxRequestBodyBytes = 1024 * 1024;
        private readonly WelsonJsMcpProcessManager _manager = new WelsonJsMcpProcessManager();
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private readonly ILogger _logger;

        public WelsonJsMcp(ResourceServer server, System.Net.Http.HttpClient httpClient, ILogger logger)
        {
            _logger = logger;
        }

        public bool CanHandle(System.Net.HttpListenerContext context, string path)
        {
            if (context == null || path == null || !path.StartsWith(Prefix + "/", StringComparison.OrdinalIgnoreCase)) return false;
            bool read = path.Equals(Prefix + "/status", StringComparison.OrdinalIgnoreCase);
            bool write = path.Equals(Prefix + "/start", StringComparison.OrdinalIgnoreCase)
                || path.Equals(Prefix + "/stop", StringComparison.OrdinalIgnoreCase)
                || path.Equals(Prefix + "/evaluate", StringComparison.OrdinalIgnoreCase);
            return (read && context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                || (write && context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase));
        }

        public async Task HandleAsync(System.Net.HttpListenerContext context, string path)
        {
            try
            {
                if (path.EndsWith("/status", StringComparison.OrdinalIgnoreCase))
                {
                    await Write(context, _manager.Status());
                    return;
                }
                if (path.EndsWith("/start", StringComparison.OrdinalIgnoreCase))
                {
                    await Write(context, _manager.Start());
                    return;
                }
                if (path.EndsWith("/stop", StringComparison.OrdinalIgnoreCase))
                {
                    _manager.Stop();
                    await Write(context, new { stopped = true });
                    return;
                }

                var input = Parse(await ReadBody(context));
                string script = input.ContainsKey("script") ? Convert.ToString(input["script"]) : null;
                int timeout = input.ContainsKey("timeout") ? Convert.ToInt32(input["timeout"]) : 900000;
                await Write(context, _manager.Evaluate(script, timeout));
            }
            catch (Exception ex)
            {
                _logger.Error("[WelsonJsMcp] Request failed", ex);
                await Write(context, new { error = ex.Message }, 400);
            }
        }

        public void Shutdown()
        {
            _manager.Stop();
        }

        private Dictionary<string, object> Parse(string body)
        {
            if (string.IsNullOrEmpty(body)) throw new ArgumentException("Request body must be a JSON object.");
            var result = _serializer.DeserializeObject(body) as Dictionary<string, object>;
            if (result == null) throw new ArgumentException("Request body must be a JSON object.");
            return result;
        }

        private static async Task<string> ReadBody(System.Net.HttpListenerContext context)
        {
            if (context.Request.ContentLength64 > MaxRequestBodyBytes) throw new InvalidDataException("Request body is too large.");
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                string body = await reader.ReadToEndAsync();
                if (body.Length > MaxRequestBodyBytes) throw new InvalidDataException("Request body is too large.");
                return body;
            }
        }

        private async Task Write(System.Net.HttpListenerContext context, object value, int status = 200)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(_serializer.Serialize(value));
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }
    }
}
