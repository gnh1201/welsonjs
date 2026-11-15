// LlamaCpp.cs
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    /// <summary>
    /// The launcher's llama.cpp router (llama-server in router mode, see LlamaCppRouter).
    /// Every request that uses a model names it; the name is the installer component
    /// (the folder under %APPDATA%\WelsonJS), for example hy_mt2_1_8b_1_25bit.
    ///
    ///   GET  llamacpp/status    router state and the editor's models (LlamaCppModels)
    ///   GET  llamacpp/models    installed models and their state (starts the router)
    ///   POST llamacpp/start     start the router                  {}
    ///   POST llamacpp/stop      stop the router and all models     {}
    ///   POST llamacpp/load      load a model and wait until ready {"model": "..."}
    ///   POST llamacpp/unload    unload a model                     {"model": "..."}
    ///   POST llamacpp/chat      OpenAI-compatible chat completion  {"model": "...", "messages": [...]}
    ///                           For LlamaCppTranslatedModels the user messages are first
    ///                           translated into English (LlamaCppPromptTranslator); optional
    ///                           "language" (the system language code) and "translate": false.
    ///
    /// The endpoints start and stop local processes, so only local callers may use them.
    /// </summary>
    public sealed class LlamaCpp : IApiEndpoint
    {
        private const string Prefix = "llamacpp";
        private const long MaxRequestBodyBytes = 64 * 1024;
        private const long MaxChatRequestBodyBytes = 1024 * 1024;
        private const int RequestBodyTimeoutMilliseconds = 5000;
        private static readonly TimeSpan ControlRequestTimeout = TimeSpan.FromMinutes(4);
        private static readonly TimeSpan ChatRequestTimeout = TimeSpan.FromMinutes(10);
        private static readonly string[] ReadPaths = { "status", "models" };
        private static readonly string[] WritePaths = { "start", "stop", "load", "unload", "chat" };

        // Chat request fields passed through to llama-server. chat_template_kwargs lets
        // callers set template options such as {"enable_thinking": false} for reasoning models.
        private static readonly string[] ChatParameters = { "temperature", "top_p", "top_k", "max_tokens", "presence_penalty", "frequency_penalty", "stop", "seed", "chat_template_kwargs" };

        private readonly LlamaCppRouter _router;
        private readonly LlamaCppPromptTranslator _translator;
        private readonly ILogger _logger;

        public LlamaCpp(ResourceServer server, System.Net.Http.HttpClient httpClient, ILogger logger)
        {
            _router = server.LlamaCppRouter;
            _translator = new LlamaCppPromptTranslator(_router);
            _logger = logger;
        }

        /// <summary>
        /// The models the editor offers as Local AI, in order (LlamaCppModels setting, a
        /// comma-separated list of model names). Invalid names and duplicates are skipped.
        /// </summary>
        public static List<string> ConfiguredModels
        {
            get
            {
                var models = new List<string>();
                string configured = Program.GetAppConfig("LlamaCppModels") ?? "";
                foreach (string item in configured.Split(','))
                {
                    string name = item.Trim();
                    if (name.Length == 0) continue;
                    try { name = LlamaCppRouter.ValidateModelName(name); }
                    catch (ArgumentException) { continue; }
                    if (!models.Contains(name)) models.Add(name);
                }
                return models;
            }
        }

        // The configured models with whether each is installed.
        private static List<Dictionary<string, object>> ConfiguredModelInfo()
        {
            return ConfiguredModels.Select(name => new Dictionary<string, object>
            {
                ["id"] = name,
                ["installed"] = LlamaCppRouter.FindModelFile(name) != null
            }).ToList();
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            if (context == null || path == null) return false;
            string method = context.Request.HttpMethod;
            string action = Action(path);
            return (ReadPaths.Contains(action) && string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)) ||
                (WritePaths.Contains(action) && string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase));
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            if (!ResourceServer.IsTrustedLocalRequest(context))
            {
                await Write(context, new { error = "The llama.cpp API is only available to local callers." }, 403);
                return;
            }

            string action = Action(path);
            if (action == "chat")
            {
                await HandleChatAsync(context);
                return;
            }

            try
            {
                using (var cts = new CancellationTokenSource(ControlRequestTimeout))
                {
                    switch (action)
                    {
                        case "status":
                            await Write(context, new Dictionary<string, object>
                            {
                                ["server"] = _router.Status(),
                                ["models"] = ConfiguredModelInfo(),
                                ["translationModel"] = LlamaCppPromptTranslator.TranslationModel,
                                ["translatedModels"] = LlamaCppPromptTranslator.TranslatedModels
                            });
                            return;
                        case "models":
                            await Write(context, new Dictionary<string, object> { ["models"] = await ListModelsAsync(cts.Token) });
                            return;
                        case "start":
                            await _router.EnsureStartedAsync(cts.Token);
                            await Write(context, new Dictionary<string, object> { ["server"] = _router.Status() });
                            return;
                        case "stop":
                            _router.Stop();
                            await Write(context, new Dictionary<string, object> { ["stopped"] = true });
                            return;
                    }

                    Dictionary<string, object> input = await ReadJsonObject(context, MaxRequestBodyBytes);
                    string model = LlamaCppRouter.ValidateModelName(GetString(input, "model"));
                    if (action == "load")
                    {
                        Dictionary<string, object> loaded = await _router.LoadAsync(model, cts.Token);
                        await Write(context, new Dictionary<string, object> { ["model"] = loaded, ["server"] = _router.Status() });
                    }
                    else
                    {
                        bool unloaded = await _router.UnloadAsync(model, cts.Token);
                        await Write(context, new Dictionary<string, object> { ["model"] = model, ["unloaded"] = unloaded });
                    }
                }
            }
            catch (ArgumentException ex)
            {
                await Write(context, new { error = ex.Message }, 400);
            }
            catch (FileNotFoundException ex)
            {
                await Write(context, new { error = ex.Message }, 404);
            }
            catch (Exception ex)
            {
                _logger.Error("[LlamaCpp] Request failed", ex);
                await Write(context, new { error = "llama.cpp is unavailable: " + ex.Message }, 503);
            }
        }

        // The router's models, without the LLM guard's classifier, which is not a chat model.
        private async Task<List<object>> ListModelsAsync(CancellationToken token)
        {
            string guardModel = LlmGuardService.ModelName;
            var models = new List<object>();
            foreach (object item in await _router.ListModelsAsync(token))
            {
                var model = item as Dictionary<string, object>;
                object id;
                if (model != null && model.TryGetValue("id", out id) && string.Equals(Convert.ToString(id), guardModel, StringComparison.Ordinal))
                    continue;
                models.Add(item);
            }
            return models;
        }

        private async Task HandleChatAsync(HttpListenerContext context)
        {
            string model;
            string language;
            bool translate;
            Dictionary<string, object> request;
            try
            {
                Dictionary<string, object> input = await ReadJsonObject(context, MaxChatRequestBodyBytes);
                model = LlamaCppRouter.ValidateModelName(GetString(input, "model"));
                request = BuildChatRequest(input);
                language = LlamaCppPromptTranslator.ResolveLanguage(GetString(input, "language"));
                object value;
                translate = !(input.TryGetValue("translate", out value) && false.Equals(value));
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
                    Dictionary<string, object> translation = null;
                    if (translate && LlamaCppPromptTranslator.AppliesTo(model))
                    {
                        try
                        {
                            translation = await _translator.PrepareAsync(request, language, cts.Token);
                        }
                        catch (FileNotFoundException ex)
                        {
                            // The translation model is not installed: send the prompt as it is.
                            _logger.Warn("[LlamaCpp] Prompt not translated: " + ex.Message);
                        }
                    }

                    KeyValuePair<int, string> result = await _router.ChatAsync(model, request, cts.Token);
                    string body = result.Value;
                    if (translation != null && result.Key >= 200 && result.Key <= 299)
                    {
                        // Report the English prompt that the model answered.
                        Dictionary<string, object> response = JsonCodec.DeserializeObject(body);
                        if (response != null)
                        {
                            response["welsonjs_translation"] = translation;
                            body = JsonCodec.Serialize(response);
                        }
                    }
                    await WriteRaw(context, body, result.Key);
                }
            }
            catch (FileNotFoundException ex)
            {
                await Write(context, new { error = ex.Message }, 404);
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

            var request = new Dictionary<string, object> { ["messages"] = messages };
            foreach (string key in ChatParameters)
            {
                if (input.TryGetValue(key, out value) && value != null)
                    request[key] = value;
            }
            return request;
        }

        private static string Action(string path)
        {
            if (!path.StartsWith(Prefix + "/", StringComparison.OrdinalIgnoreCase)) return null;
            return path.Substring(Prefix.Length + 1).ToLowerInvariant();
        }

        private static string GetString(Dictionary<string, object> input, string key)
        {
            object value;
            return input.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : null;
        }

        private static async Task<Dictionary<string, object>> ReadJsonObject(HttpListenerContext context, long maxBytes)
        {
            string body = await ReadBody(context, maxBytes);
            if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("The request body must be a JSON object.");
            Dictionary<string, object> input = JsonCodec.DeserializeObject(body);
            if (input == null) throw new ArgumentException("The request body must be a JSON object.");
            return input;
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

        private static Task Write(HttpListenerContext context, object value, int status = 200)
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
