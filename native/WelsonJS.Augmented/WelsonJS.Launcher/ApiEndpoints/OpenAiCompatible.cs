// OpenAiCompatible.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
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
    /// An OpenAI-compatible API for the launcher's models:
    ///
    ///   POST v1/chat/completions   {"model": "...", "messages": [...], "stream": false}
    ///   POST v1/embeddings         {"model": "...", "input": "..." or [...], "dimensions"?: n}
    ///   POST v1/decisions          {"model": "...", "state": {...}, "questions": {...}, "workflow"?: "..."}
    ///   GET  v1/models             the models below
    ///
    /// The model is either
    /// - a llama.cpp model (configured in LlamaCppModels or installed), answered by the
    ///   llama.cpp router (LlamaCppChat, with the prompt translation for
    ///   LlamaCppTranslatedModels and its optional "language" and "translate" fields), or
    /// - a web chat service in webchat.js (copilot, chatgpt, claude, ...), answered in the
    ///   launcher's browser (WebChatRunner). Generation parameters do not apply to them.
    ///
    /// Embeddings come from an installed llama.cpp embedding model (LlamaCppEmbeddingServers,
    /// for example embeddinggemma_2, the original google/embeddinggemma-2) or a CrispEmbed
    /// embedding model; typed decisions (a WelsonJS extension; OpenAI has no such API) from a
    /// CrispEmbed decision model such as embeddinggemma_2_typed_decision. The model's server
    /// answers the request body unchanged.
    ///
    /// Responses are not streamed: with "stream": true the finished answer is sent as one
    /// server-sent event chunk, then [DONE]. Errors use the OpenAI error object.
    ///
    /// Only local callers may use the API (it runs local processes and the user's signed-in
    /// chats): trusted local requests (ResourceServer.IsTrustedLocalRequest), and other
    /// software on this machine that sends the launcher's API key (ResourceServerApiKey) as
    /// "Authorization: Bearer ...", such as apps built on a browser engine, whose requests
    /// look like requests from another site.
    /// </summary>
    public sealed class OpenAiCompatible : IApiKeyEndpoint
    {
        private const string ChatPath = "v1/chat/completions";
        private const string ModelsPath = "v1/models";
        private const string EmbeddingsPath = "v1/embeddings";
        private const string DecisionsPath = "v1/decisions";
        private static readonly TimeSpan CrispEmbedTimeout = TimeSpan.FromMinutes(5);
        private const long MaxRequestBodyBytes = 1024 * 1024;
        private static readonly TimeSpan LlamaCppTimeout = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan WebChatTimeout = TimeSpan.FromMinutes(15);
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly LlamaCppChat _llamaCpp;
        private readonly CrispEmbedServers _crispEmbed;
        private readonly LlamaCppEmbeddingServers _llamaCppEmbeddings;
        private readonly WebChatRunner _webChat;
        private readonly ILogger _logger;

        public OpenAiCompatible(ResourceServer server, System.Net.Http.HttpClient httpClient, ILogger logger)
        {
            _llamaCpp = server.LlamaCppChat;
            _crispEmbed = server.CrispEmbedServers;
            _llamaCppEmbeddings = server.LlamaCppEmbeddingServers;
            _webChat = server.WebChatRunner;
            _logger = logger;
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            if (context == null || path == null) return false;
            string method = context.Request.HttpMethod;
            return ((string.Equals(path, ChatPath, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(path, EmbeddingsPath, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(path, DecisionsPath, StringComparison.OrdinalIgnoreCase)) &&
                    string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)) ||
                (string.Equals(path, ModelsPath, StringComparison.OrdinalIgnoreCase) && string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase));
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            if (!ResourceServer.IsTrustedLocalRequest(context) && !ResourceServer.HasValidApiKey(context))
            {
                await WriteError(context, 403, "This API is only available to local callers. Other local software can use it with the API key (ResourceServerApiKey).", "permission_error");
                return;
            }

            if (string.Equals(path, ModelsPath, StringComparison.OrdinalIgnoreCase))
            {
                await HandleModelsAsync(context);
                return;
            }
            if (string.Equals(path, EmbeddingsPath, StringComparison.OrdinalIgnoreCase))
            {
                await HandleCrispEmbedAsync(context, "embedding");
                return;
            }
            if (string.Equals(path, DecisionsPath, StringComparison.OrdinalIgnoreCase))
            {
                await HandleCrispEmbedAsync(context, "decision");
                return;
            }

            Dictionary<string, object> input;
            string model;
            bool stream;
            try
            {
                input = await LlamaCpp.ReadJsonObject(context, MaxRequestBodyBytes);
                object value;
                model = input.TryGetValue("model", out value) ? value as string : null;
                if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("\"model\" is required.");
                stream = input.TryGetValue("stream", out value) && true.Equals(value);
                NormalizeMessages(input);
            }
            catch (Exception ex)
            {
                await WriteError(context, 400, "Invalid request: " + ex.Message, "invalid_request_error");
                return;
            }

            try
            {
                KeyValuePair<int, string> result;
                if (LlamaCppChat.IsModel(model))
                {
                    using (var cts = new CancellationTokenSource(LlamaCppTimeout))
                        result = await _llamaCpp.CompleteAsync(input, cts.Token);
                }
                else if (_webChat.HasService(model))
                {
                    string answer;
                    using (var cts = new CancellationTokenSource(WebChatTimeout))
                        answer = await _webChat.ChatAsync(model, WebChatRunner.BuildMessage((object[])input["messages"]), cts.Token);
                    result = new KeyValuePair<int, string>(200, JsonCodec.Serialize(Completion(model, answer)));
                }
                else
                {
                    await WriteError(context, 404, "The model '" + model + "' does not exist. See GET /v1/models.", "invalid_request_error", "model_not_found");
                    return;
                }

                if (stream && result.Key >= 200 && result.Key <= 299)
                    await WriteEventStream(context, JsonCodec.DeserializeObject(result.Value));
                else
                    await LlamaCpp.WriteRaw(context, result.Value, result.Key);
            }
            catch (ArgumentException ex)
            {
                await WriteError(context, 400, ex.Message, "invalid_request_error");
            }
            catch (FileNotFoundException ex)
            {
                await WriteError(context, 404, ex.Message, "invalid_request_error", "model_not_found");
            }
            catch (Exception ex) when (ex is TimeoutException || ex is OperationCanceledException)
            {
                await WriteError(context, 504, ex is TimeoutException ? ex.Message : "The model did not answer in time.", "timeout");
            }
            catch (Exception ex)
            {
                _logger.Error("[OpenAI] Chat request failed (" + model + ")", ex);
                await WriteError(context, 503, ex.Message, "server_error");
            }
        }

        private async Task HandleModelsAsync(HttpListenerContext context)
        {
            var data = new List<object>();
            foreach (string name in LlamaCpp.ConfiguredModels.Where(m => LlamaCppRouter.FindModelFile(m) != null))
                data.Add(Model(name, name, "llamacpp"));
            foreach (string name in LlamaCppEmbeddingServers.InstalledModels)
            {
                Dictionary<string, object> model = Model(name, name, "llamacpp");
                model["type"] = "embedding";
                data.Add(model);
            }
            foreach (string name in CrispEmbedServers.InstalledModels)
            {
                Dictionary<string, object> model = Model(name, name, "crispembed");
                model["type"] = CrispEmbedServers.ModelKind(name);   // "embedding" (v1/embeddings) or "decision" (v1/decisions)
                data.Add(model);
            }
            try
            {
                foreach (KeyValuePair<string, object> service in _webChat.Services)
                {
                    var info = service.Value as Dictionary<string, object>;
                    data.Add(Model(service.Key, info != null ? Convert.ToString(info["name"]) : service.Key, "webchat"));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("[OpenAI] Could not read the web chat services", ex);
            }
            await LlamaCpp.WriteRaw(context, JsonCodec.Serialize(new Dictionary<string, object> { ["object"] = "list", ["data"] = data }), 200);
        }

        // Embeddings (kind "embedding") or typed decisions (kind "decision") with a CrispEmbed
        // model of that kind. The body is sent to the model's crispembed-server as it was
        // received: the server ignores "model", and a decision renders the state and questions
        // in their JSON key order, which parsing and serializing the body again could change.
        private async Task HandleCrispEmbedAsync(HttpListenerContext context, string kind)
        {
            bool decision = kind == "decision";
            string body;
            string model;
            try
            {
                body = await LlamaCpp.ReadBody(context, MaxRequestBodyBytes);
                Dictionary<string, object> input = string.IsNullOrWhiteSpace(body) ? null : JsonCodec.DeserializeObject(body);
                if (input == null) throw new ArgumentException("The request body must be a JSON object.");
                object value;
                model = input.TryGetValue("model", out value) ? value as string : null;
                if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("\"model\" is required.");
                if (decision)
                {
                    if (!input.TryGetValue("state", out value) || value == null) throw new ArgumentException("\"state\" is required.");
                    if (!input.TryGetValue("questions", out value) || !(value is Dictionary<string, object>)) throw new ArgumentException("\"questions\" must be an object.");
                }
                else if (!input.TryGetValue("input", out value) || value == null)
                {
                    throw new ArgumentException("\"input\" is required.");
                }
            }
            catch (Exception ex)
            {
                await WriteError(context, 400, "Invalid request: " + ex.Message, "invalid_request_error");
                return;
            }

            // A llama.cpp embedding model (the WelsonJS llama.cpp build in embedding mode).
            if (!decision && LlamaCppEmbeddingServers.IsModel(model))
            {
                try
                {
                    KeyValuePair<int, string> embedded;
                    using (var cts = new CancellationTokenSource(CrispEmbedTimeout))
                        embedded = await _llamaCppEmbeddings.EmbedAsync(model, body, cts.Token);
                    if (embedded.Key >= 200 && embedded.Key <= 299)
                        await LlamaCpp.WriteRaw(context, embedded.Value, embedded.Key);
                    else
                        await WriteError(context, embedded.Key, ServerError(embedded.Value), embedded.Key >= 500 ? "server_error" : "invalid_request_error");
                }
                catch (ArgumentException ex)
                {
                    await WriteError(context, 400, ex.Message, "invalid_request_error");
                }
                catch (FileNotFoundException ex)
                {
                    await WriteError(context, 404, ex.Message, "invalid_request_error", "model_not_found");
                }
                catch (Exception ex) when (ex is TimeoutException || ex is OperationCanceledException)
                {
                    await WriteError(context, 504, ex is TimeoutException ? ex.Message : "The model did not answer in time.", "timeout");
                }
                catch (Exception ex)
                {
                    _logger.Error("[OpenAI] Embeddings request failed (" + model + ")", ex);
                    await WriteError(context, 503, ex.Message, "server_error");
                }
                return;
            }

            if (!CrispEmbedServers.IsModel(model))
            {
                await WriteError(context, 404, "The " + kind + " model '" + model + "' is not installed. See GET /v1/models (type \"" + kind + "\").",
                    "invalid_request_error", "model_not_found");
                return;
            }

            try
            {
                CrispEmbedServers.EnsureKind(model, kind);
                KeyValuePair<int, string> result;
                using (var cts = new CancellationTokenSource(CrispEmbedTimeout))
                    result = await _crispEmbed.PostAsync(model, decision ? "/decide" : "/v1/embeddings", body, cts.Token);
                if (result.Key >= 200 && result.Key <= 299)
                    await LlamaCpp.WriteRaw(context, result.Value, result.Key);
                else
                    await WriteError(context, result.Key, ServerError(result.Value), result.Key >= 500 ? "server_error" : "invalid_request_error");
            }
            catch (ArgumentException ex)
            {
                await WriteError(context, 400, ex.Message, "invalid_request_error", "model_not_supported");
            }
            catch (FileNotFoundException ex)
            {
                await WriteError(context, 404, ex.Message, "invalid_request_error", "model_not_found");
            }
            catch (Exception ex) when (ex is TimeoutException || ex is OperationCanceledException)
            {
                await WriteError(context, 504, ex is TimeoutException ? ex.Message : "The model did not answer in time.", "timeout");
            }
            catch (Exception ex)
            {
                _logger.Error("[OpenAI] " + (decision ? "Decision" : "Embeddings") + " request failed (" + model + ")", ex);
                await WriteError(context, 503, ex.Message, "server_error");
            }
        }

        // The message of a crispembed-server error ({"error": "..."}) or a llama-server error
        // ({"error": {"message": "..."}}), or the body itself.
        private static string ServerError(string body)
        {
            try
            {
                Dictionary<string, object> error = JsonCodec.DeserializeObject(body);
                object message;
                if (error != null && error.TryGetValue("error", out message))
                {
                    if (message is string) return (string)message;
                    var detail = message as Dictionary<string, object>;
                    object text;
                    if (detail != null && detail.TryGetValue("message", out text) && text is string) return (string)text;
                }
            }
            catch (Exception)
            {
                // Not JSON.
            }
            return string.IsNullOrWhiteSpace(body) ? "CrispEmbed returned an error." : body;
        }

        // Accepts content given as an array of parts ([{"type": "text", "text": "..."}]),
        // as some clients send it, by joining the text parts.
        private static void NormalizeMessages(Dictionary<string, object> input)
        {
            object value;
            object[] messages = input.TryGetValue("messages", out value) ? value as object[] : null;
            if (messages == null || messages.Length == 0)
                throw new ArgumentException("\"messages\" must be a non-empty array.");
            foreach (object item in messages)
            {
                var message = item as Dictionary<string, object>;
                if (message == null || !(message.TryGetValue("role", out value) && value is string))
                    throw new ArgumentException("Each message must be an object with a \"role\" string.");
                if (!message.TryGetValue("content", out value) || value == null)
                {
                    message["content"] = "";
                    continue;
                }
                var parts = value as object[];
                if (parts == null) continue;
                message["content"] = string.Join("\n", parts.OfType<Dictionary<string, object>>()
                    .Where(p => p.TryGetValue("type", out object type) && "text".Equals(type))
                    .Select(p => p.TryGetValue("text", out object text) ? Convert.ToString(text) : ""));
            }
        }

        private static Dictionary<string, object> Completion(string model, string answer)
        {
            return new Dictionary<string, object>
            {
                ["id"] = "chatcmpl-" + Guid.NewGuid().ToString("N"),
                ["object"] = "chat.completion",
                ["created"] = Now(),
                ["model"] = model,
                ["choices"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["index"] = 0,
                        ["message"] = new Dictionary<string, object> { ["role"] = "assistant", ["content"] = answer },
                        ["finish_reason"] = "stop"
                    }
                },
                // A web chat page does not report token counts.
                ["usage"] = new Dictionary<string, object> { ["prompt_tokens"] = 0, ["completion_tokens"] = 0, ["total_tokens"] = 0 }
            };
        }

        private static Dictionary<string, object> Model(string id, string name, string ownedBy)
        {
            return new Dictionary<string, object> { ["id"] = id, ["object"] = "model", ["created"] = 0, ["owned_by"] = ownedBy, ["name"] = name };
        }

        // The finished completion as server-sent events: one chunk with the message, one
        // with the finish reason, then [DONE].
        private static async Task WriteEventStream(HttpListenerContext context, Dictionary<string, object> completion)
        {
            object id = completion.TryGetValue("id", out object i) ? i : "chatcmpl-" + Guid.NewGuid().ToString("N");
            object created = completion.TryGetValue("created", out object c) ? c : Now();
            object model = completion.TryGetValue("model", out object m) ? m : "";
            object[] choices = completion.TryGetValue("choices", out object ch) ? ch as object[] : null;
            var choice = choices != null && choices.Length > 0 ? choices[0] as Dictionary<string, object> : null;
            var message = choice != null && choice.TryGetValue("message", out object msg) ? msg as Dictionary<string, object> : null;
            object finish = choice != null && choice.TryGetValue("finish_reason", out object f) ? f : "stop";

            var delta = new Dictionary<string, object> { ["role"] = "assistant", ["content"] = message != null && message.TryGetValue("content", out object content) ? content : "" };
            if (message != null && message.TryGetValue("reasoning_content", out object reasoning) && reasoning != null)
                delta["reasoning_content"] = reasoning;

            Func<Dictionary<string, object>, object, string> chunk = (d, reason) => "data: " + JsonCodec.Serialize(new Dictionary<string, object>
            {
                ["id"] = id,
                ["object"] = "chat.completion.chunk",
                ["created"] = created,
                ["model"] = model,
                ["choices"] = new object[] { new Dictionary<string, object> { ["index"] = 0, ["delta"] = d, ["finish_reason"] = reason } }
            }) + "\n\n";

            string body = chunk(delta, null) + chunk(new Dictionary<string, object>(), finish) + "data: [DONE]\n\n";
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/event-stream; charset=utf-8";
            context.Response.Headers["Cache-Control"] = "no-cache";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }

        private static Task WriteError(HttpListenerContext context, int status, string message, string type, string code = null)
        {
            return LlamaCpp.WriteRaw(context, JsonCodec.Serialize(new Dictionary<string, object>
            {
                ["error"] = new Dictionary<string, object> { ["message"] = message, ["type"] = type, ["param"] = null, ["code"] = code }
            }), status);
        }

        private static long Now()
        {
            return (long)(DateTime.UtcNow - Epoch).TotalSeconds;
        }
    }
}
