// LlamaCpp.cs
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    /// <summary>
    /// Launcher-managed llama.cpp (llama-server) processes.
    ///
    ///   GET  llamacpp/status, llamacpp/list   managed processes
    ///   POST llamacpp/start, llamacpp/stop    start or stop a process
    ///   POST llamacpp/chat                    OpenAI-compatible chat completion with the
    ///                                         LlamaCppModelPath model (started on first use)
    /// </summary>
    public sealed class LlamaCpp : IApiEndpoint
    {
        private const string Prefix = "llamacpp";
        private const long MaxRequestBodyBytes = 64 * 1024;
        private const long MaxChatRequestBodyBytes = 1024 * 1024;
        private const int RequestBodyTimeoutMilliseconds = 5000;
        private static readonly TimeSpan ChatRequestTimeout = TimeSpan.FromMinutes(10);

        // Chat request fields passed through to llama-server. chat_template_kwargs lets
        // callers set template options such as {"enable_thinking": false} for reasoning models.
        private static readonly string[] ChatParameters = { "temperature", "top_p", "top_k", "max_tokens", "presence_penalty", "frequency_penalty", "stop", "seed", "chat_template_kwargs" };

        private readonly ResourceServer _server;
        private readonly ILogger _logger;
        private readonly LlamaCppProcessManager _manager;
        private readonly LlamaCppChatService _chat;

        public LlamaCpp(ResourceServer server, System.Net.Http.HttpClient httpClient, ILogger logger)
        {
            _server = server; _logger = logger; _manager = new LlamaCppProcessManager();
            _chat = new LlamaCppChatService(_manager);
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            if (context == null || path == null) return false;
            bool read = path.Equals(Prefix + "/status", StringComparison.OrdinalIgnoreCase) || path.Equals(Prefix + "/list", StringComparison.OrdinalIgnoreCase);
            bool write = path.Equals(Prefix + "/start", StringComparison.OrdinalIgnoreCase) || path.Equals(Prefix + "/stop", StringComparison.OrdinalIgnoreCase) ||
                path.Equals(Prefix + "/chat", StringComparison.OrdinalIgnoreCase);
            return (read && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)) ||
                (write && string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase));
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            if (path.EndsWith("/chat", StringComparison.OrdinalIgnoreCase))
            {
                await HandleChatAsync(context);
                return;
            }

            try
            {
                if (path.EndsWith("/status", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/list", StringComparison.OrdinalIgnoreCase))
                {
                    await Write(context, new Dictionary<string, object> { ["processes"] = _manager.List() });
                    return;
                }
                var body = await ReadBody(context, MaxRequestBodyBytes);
                var input = string.IsNullOrEmpty(body) ? new Dictionary<string, object>() : JsonCodec.Deserialize(body) as Dictionary<string, object>;
                if (input == null) throw new ArgumentException("Request body must be a JSON object.");
                if (path.EndsWith("/start", StringComparison.OrdinalIgnoreCase))
                {
                    var result = _manager.Start(GetString(input, "modelPath", true), GetString(input, "executablePath", false), GetString(input, "host", false), GetInt(input, "port", 0), GetInt(input, "contextSize", 4096), GetInt(input, "threads", 0));
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

        private async Task HandleChatAsync(HttpListenerContext context)
        {
            // Chat may start a local process, so only local callers may use it.
            if (!ResourceServer.IsTrustedLocalRequest(context))
            {
                await Write(context, new { error = "llama.cpp chat is only available to local callers." }, 403);
                return;
            }

            Dictionary<string, object> request;
            try
            {
                Dictionary<string, object> input = JsonCodec.DeserializeObject(await ReadBody(context, MaxChatRequestBodyBytes));
                request = BuildChatRequest(input);
            }
            catch (Exception ex)
            {
                await Write(context, new { error = "Invalid chat request: " + ex.Message }, 400);
                return;
            }

            try
            {
                using (var cts = new CancellationTokenSource(ChatRequestTimeout))
                {
                    KeyValuePair<int, string> result = await _chat.CompleteAsync(request, cts.Token);
                    await WriteRaw(context, result.Value, result.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("[LlamaCpp] Chat request failed", ex);
                await Write(context, new { error = "llama.cpp is unavailable: " + ex.Message }, 503);
            }
        }

        // Keeps "messages" (an array of {role, content} objects) and the known generation parameters.
        private static Dictionary<string, object> BuildChatRequest(Dictionary<string, object> input)
        {
            if (input == null) throw new ArgumentException("The request body must be a JSON object.");

            object value;
            object[] messages = input.TryGetValue("messages", out value) ? value as object[] : null;
            if (messages == null || messages.Length == 0)
                throw new ArgumentException("\"messages\" must be a non-empty array.");

            foreach (object item in messages)
            {
                var message = item as Dictionary<string, object>;
                if (message == null || !(message.TryGetValue("role", out value) && value is string) ||
                    !(message.TryGetValue("content", out value) && value is string))
                    throw new ArgumentException("Each message must be an object with \"role\" and \"content\" strings.");
            }

            var request = new Dictionary<string, object> { ["model"] = "welsonjs", ["messages"] = messages };
            foreach (string key in ChatParameters)
            {
                if (input.TryGetValue(key, out value) && value != null)
                    request[key] = value;
            }
            return request;
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
        private static async Task<string> ReadBody(HttpListenerContext context, long maxBytes)
        {
            if (context.Request.ContentLength64 > maxBytes)
                throw new InvalidDataException("Request body is too large.");

            var buffer = new byte[8192];
            using (var body = new MemoryStream())
            {
                while (body.Length < maxBytes)
                {
                    int remaining = (int)Math.Min(buffer.Length, maxBytes - body.Length);
                    Task<int> readTask = context.Request.InputStream.ReadAsync(buffer, 0, remaining);
                    if (await Task.WhenAny(readTask, Task.Delay(RequestBodyTimeoutMilliseconds)) != readTask)
                        throw new TimeoutException("Request body read timed out.");
                    int read = await readTask;
                    if (read == 0) break;
                    body.Write(buffer, 0, read);
                }
                if (body.Length >= maxBytes)
                    throw new InvalidDataException("Request body is too large.");
                return Encoding.UTF8.GetString(body.ToArray());
            }
        }
        private Task Write(HttpListenerContext context, object value, int status = 200)
        {
            return WriteRaw(context, JsonCodec.Serialize(value), status);
        }

        private static async Task WriteRaw(HttpListenerContext context, string json, int status)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json ?? "");
            context.Response.StatusCode = status; context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length); context.Response.Close();
        }
    }
}
