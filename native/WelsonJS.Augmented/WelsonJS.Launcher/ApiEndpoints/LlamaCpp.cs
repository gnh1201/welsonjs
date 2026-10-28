// LlamaCpp.cs
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WelsonJS.Launcher.ApiEndpoints
{
    public sealed class LlamaCpp : IApiEndpoint
    {
        private const string Prefix = "llamacpp";
        private const long MaxRequestBodyBytes = 64 * 1024;
        private const int RequestBodyTimeoutMilliseconds = 5000;
        private readonly ResourceServer _server;
        private readonly ILogger _logger;
        private readonly LlamaCppProcessManager _manager;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public LlamaCpp(ResourceServer server, System.Net.Http.HttpClient httpClient, ILogger logger)
        {
            _server = server; _logger = logger; _manager = new LlamaCppProcessManager();
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            if (context == null || path == null) return false;
            bool read = path.Equals(Prefix + "/status", StringComparison.OrdinalIgnoreCase) || path.Equals(Prefix + "/list", StringComparison.OrdinalIgnoreCase);
            bool write = path.Equals(Prefix + "/start", StringComparison.OrdinalIgnoreCase) || path.Equals(Prefix + "/stop", StringComparison.OrdinalIgnoreCase);
            return (read && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)) ||
                (write && string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase));
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            try
            {
                if (path.EndsWith("/status", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/list", StringComparison.OrdinalIgnoreCase))
                {
                    await Write(context, new Dictionary<string, object> { ["processes"] = _manager.List() });
                    return;
                }
                var body = await ReadBody(context);
                var input = string.IsNullOrEmpty(body) ? new Dictionary<string, object>() : _serializer.DeserializeObject(body) as Dictionary<string, object>;
                if (input == null) throw new ArgumentException("Request body must be a JSON object.");
                if (path.EndsWith("/start", StringComparison.OrdinalIgnoreCase))
                {
                    var result = _manager.Start(GetString(input, "modelPath", true), GetString(input, "executablePath", false), GetString(input, "host", false), GetInt(input, "port", 8080), GetInt(input, "contextSize", 4096), GetInt(input, "threads", 0));
                    await Write(context, result, 201); return;
                }
                string id = GetString(input, "id", true);
                if (!_manager.Stop(id)) { await Write(context, new { error = "Managed llama.cpp process not found." }, 404); return; }
                await Write(context, new { stopped = true, id = id });
            }
            catch (Exception ex)
            {
                _logger.Error("[LlamaCpp] Request failed", ex);
                await Write(context, new { error = ex.Message }, 400);
            }
        }

        public void Shutdown()
        {
            _manager.Shutdown();
        }

        private static string GetString(Dictionary<string, object> input, string key, bool required)
        {
            object value; input.TryGetValue(key, out value); string text = value == null ? null : Convert.ToString(value);
            if (required && string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Missing required field: " + key);
            return text;
        }
        private static int GetInt(Dictionary<string, object> input, string key, int fallback)
        {
            object value; input.TryGetValue(key, out value); int result;
            return value == null || !int.TryParse(Convert.ToString(value), out result) ? fallback : result;
        }
        private static async Task<string> ReadBody(HttpListenerContext context)
        {
            if (context.Request.ContentLength64 > MaxRequestBodyBytes)
                throw new InvalidDataException("Request body is too large.");

            var buffer = new byte[8192];
            using (var body = new MemoryStream())
            {
                while (body.Length < MaxRequestBodyBytes)
                {
                    int remaining = (int)Math.Min(buffer.Length, MaxRequestBodyBytes - body.Length);
                    Task<int> readTask = context.Request.InputStream.ReadAsync(buffer, 0, remaining);
                    if (await Task.WhenAny(readTask, Task.Delay(RequestBodyTimeoutMilliseconds)) != readTask)
                        throw new TimeoutException("Request body read timed out.");
                    int read = await readTask;
                    if (read == 0) break;
                    body.Write(buffer, 0, read);
                }
                if (body.Length >= MaxRequestBodyBytes)
                    throw new InvalidDataException("Request body is too large.");
                return Encoding.UTF8.GetString(body.ToArray());
            }
        }
        private async Task Write(HttpListenerContext context, object value, int status = 200)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(_serializer.Serialize(value));
            context.Response.StatusCode = status; context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length); context.Response.Close();
        }
    }
}
