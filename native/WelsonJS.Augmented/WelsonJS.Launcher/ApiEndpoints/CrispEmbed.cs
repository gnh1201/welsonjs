// CrispEmbed.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    /// <summary>
    /// The launcher's CrispEmbed servers (see CrispEmbedServers). Every request that uses a
    /// model names it; the name is the installer component (the folder under
    /// %APPDATA%\WelsonJS\crispembed_models), for example embeddinggemma_2_typed_decision.
    ///
    ///   GET  crispembed/status   the servers and the settings
    ///   GET  crispembed/models   configured and installed models, their kind (decision or
    ///                            embedding), and which are running
    ///   POST crispembed/start    start a model's server and wait   {"model": "..."}
    ///   POST crispembed/stop     stop one model's server, or all   {"model": "..."} or {}
    ///   POST crispembed/embed    OpenAI-style embeddings           {"model": "...", "input": "..." or [...], "dimensions"?: n}
    ///                            (embedding models only; also POST v1/embeddings)
    ///   POST crispembed/decide   typed decisions                   {"model": "...", "state": {...}, "questions": {...}, "workflow"?: "..."}
    ///                            (decision models, such as embeddinggemma_2_typed_decision; also POST v1/decisions)
    ///
    /// Only these routes of crispembed-server are forwarded. The endpoints start and stop
    /// local processes, so only local callers may use them.
    /// </summary>
    public sealed class CrispEmbed : IApiEndpoint
    {
        private const string Prefix = "crispembed";
        private const long MaxControlBodyBytes = 64 * 1024;
        private const long MaxInferenceBodyBytes = 1024 * 1024;
        private static readonly TimeSpan ControlRequestTimeout = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan InferenceRequestTimeout = TimeSpan.FromMinutes(5);
        private static readonly string[] ReadPaths = { "status", "models" };
        private static readonly string[] WritePaths = { "start", "stop", "embed", "decide" };

        private readonly CrispEmbedServers _servers;
        private readonly ILogger _logger;

        public CrispEmbed(ResourceServer server, System.Net.Http.HttpClient httpClient, ILogger logger)
        {
            _servers = server.CrispEmbedServers;
            _logger = logger;
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
                await Write(context, new { error = "The CrispEmbed API is only available to local callers." }, 403);
                return;
            }

            string action = Action(path);
            try
            {
                switch (action)
                {
                    case "status":
                        await Write(context, new Dictionary<string, object> { ["server"] = _servers.Status(), ["models"] = _servers.Models() });
                        return;
                    case "models":
                        await Write(context, new Dictionary<string, object> { ["models"] = _servers.Models() });
                        return;
                }

                bool inference = action == "embed" || action == "decide";
                // The body is forwarded as it was sent: typed decisions render the state
                // and questions in their JSON key order, which parsing and serializing
                // again could change (a JavaScript object puts integer-like keys first).
                // crispembed-server ignores the launcher's "model" field.
                string body = await LlamaCpp.ReadBody(context, inference ? MaxInferenceBodyBytes : MaxControlBodyBytes);
                Dictionary<string, object> input = string.IsNullOrWhiteSpace(body) ? null : JsonCodec.DeserializeObject(body);
                if (input == null) throw new ArgumentException("The request body must be a JSON object.");
                object value;
                string model = input.TryGetValue("model", out value) && value != null ? Convert.ToString(value) : null;

                using (var cts = new CancellationTokenSource(inference ? InferenceRequestTimeout : ControlRequestTimeout))
                {
                    switch (action)
                    {
                        case "start":
                            await Write(context, new Dictionary<string, object>
                            {
                                ["model"] = CrispEmbedServers.ValidateModelName(model),
                                ["health"] = await _servers.StartAsync(model, cts.Token)
                            });
                            return;
                        case "stop":
                            if (string.IsNullOrWhiteSpace(model))
                            {
                                _servers.StopAll();
                                await Write(context, new Dictionary<string, object> { ["stopped"] = true });
                            }
                            else
                            {
                                await Write(context, new Dictionary<string, object> { ["model"] = model, ["stopped"] = _servers.Stop(model) });
                            }
                            return;
                        case "embed":
                            CrispEmbedServers.EnsureKind(model, "embedding");
                            await Forward(context, model, "/v1/embeddings", body, cts.Token);
                            return;
                        case "decide":
                            CrispEmbedServers.EnsureKind(model, "decision");
                            await Forward(context, model, "/decide", body, cts.Token);
                            return;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                await Write(context, new { error = ex.Message }, 400);
            }
            catch (InvalidDataException ex)
            {
                await Write(context, new { error = ex.Message }, 400);
            }
            catch (FileNotFoundException ex)
            {
                await Write(context, new { error = ex.Message }, 404);
            }
            catch (Exception ex) when (ex is TimeoutException || ex is OperationCanceledException)
            {
                await Write(context, new { error = ex is TimeoutException ? ex.Message : "CrispEmbed did not answer in time." }, 504);
            }
            catch (Exception ex)
            {
                _logger.Error("[CrispEmbed] Request failed", ex);
                await Write(context, new { error = "CrispEmbed is unavailable: " + ex.Message }, 503);
            }
        }

        private async Task Forward(HttpListenerContext context, string model, string route, string body, CancellationToken token)
        {
            KeyValuePair<int, string> result = await _servers.PostAsync(model, route, body, token);
            await LlamaCpp.WriteRaw(context, result.Value, result.Key);
        }

        private static string Action(string path)
        {
            if (!path.StartsWith(Prefix + "/", StringComparison.OrdinalIgnoreCase)) return null;
            return path.Substring(Prefix.Length + 1).ToLowerInvariant();
        }

        private static Task Write(HttpListenerContext context, object value, int status = 200)
        {
            return LlamaCpp.WriteRaw(context, JsonCodec.Serialize(value), status);
        }
    }
}
