// WelsonJsInstance.cs
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WelsonJS.Launcher.ApiEndpoints
{
    public sealed class WelsonJsInstance : IApiEndpoint
    {
        private const string Prefix = "welsonjs-instance";
        private const long MaxRequestBodyBytes = 1024 * 1024;
        private readonly WelsonJsInstanceProcessManager _manager;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private readonly ILogger _logger;

        public WelsonJsInstance(ResourceServer server, HttpClient httpClient, ILogger logger)
        {
            _manager = new WelsonJsInstanceProcessManager(server.TerminalSessions);
            _logger = logger;
        }

        public bool CanHandle(System.Net.HttpListenerContext context, string path)
        {
            if (context == null || path == null || !path.StartsWith(Prefix + "/", StringComparison.OrdinalIgnoreCase)) return false;
            bool status = path.Equals(Prefix + "/status", StringComparison.OrdinalIgnoreCase);
            bool write = path.Equals(Prefix + "/run", StringComparison.OrdinalIgnoreCase);
            return (status && context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                || (write && context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase));
        }

        public async Task HandleAsync(System.Net.HttpListenerContext context, string path)
        {
            try
            {
                // Running a script is equivalent to running code as the launcher user,
                // so only accept requests from this server's own origin on localhost.
                if (!ResourceServer.IsTrustedLocalRequest(context))
                {
                    await Write(context, new { error = "Script execution is only allowed from the launcher's own origin on localhost." }, 403);
                    return;
                }

                if (path.EndsWith("/status", StringComparison.OrdinalIgnoreCase))
                {
                    await Write(context, _manager.Status());
                    return;
                }

                var input = _serializer.DeserializeObject(await ReadBody(context)) as Dictionary<string, object>;
                if (input == null || !input.ContainsKey("script"))
                    throw new ArgumentException("Request body must contain a script.");

                await Write(context, _manager.Start(
                    Convert.ToString(input["script"]),
                    GetShort(input, "cols"),
                    GetShort(input, "rows")));
            }
            catch (Exception ex)
            {
                _logger.Error("[WelsonJsInstance] Request failed", ex);
                await Write(context, new { error = ex.Message }, 400);
            }
        }

        public void Shutdown()
        {
            _manager.Stop();
        }

        private static short GetShort(Dictionary<string, object> input, string key)
        {
            object value;
            if (!input.TryGetValue(key, out value) || value == null) return 0;
            try { return (short)Math.Max(0, Math.Min(1000, Convert.ToInt32(value))); }
            catch (FormatException) { return 0; }
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
