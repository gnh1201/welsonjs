// LlmGuard.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    /// <summary>
    /// Local LLM guard (Qwen3Guard) used before data leaves the machine for an LLM.
    ///
    ///   POST llm-guard/inspect   {"text": "..."} -> {"label": "safe|controversial|unsafe", "categories": [...], "raw": "..."}
    ///   GET  llm-guard/status    model installation and guard process state
    ///
    /// Any failure answers 503 with {"error": "..."}; clients must treat that as blocked.
    /// </summary>
    public sealed class LlmGuard : IApiEndpoint
    {
        private const string Prefix = "llm-guard";
        private const long MaxRequestBodyBytes = 1024 * 1024;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(5);

        private readonly LlmGuardService _guard;
        private readonly ILogger _logger;

        public LlmGuard(ResourceServer server, HttpClient httpClient, ILogger logger)
        {
            _guard = new LlmGuardService(httpClient);
            _logger = logger;
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            if (context == null || path == null) return false;
            string method = context.Request.HttpMethod;
            return (path.Equals(Prefix + "/inspect", StringComparison.OrdinalIgnoreCase) && method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                || (path.Equals(Prefix + "/status", StringComparison.OrdinalIgnoreCase) && method.Equals("GET", StringComparison.OrdinalIgnoreCase));
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            // Starting the guard runs a local process, so only local callers may use it.
            if (!ResourceServer.IsTrustedLocalRequest(context))
            {
                await Write(context, new { error = "The LLM guard is only available to local callers." }, 403);
                return;
            }

            if (path.EndsWith("/status", StringComparison.OrdinalIgnoreCase))
            {
                await Write(context, _guard.Status());
                return;
            }

            Dictionary<string, object> input;
            try
            {
                input = JsonCodec.DeserializeObject(await ReadBody(context));
            }
            catch (Exception ex)
            {
                await Write(context, new { error = "Request body must be a JSON object with a \"text\" string: " + ex.Message }, 400);
                return;
            }

            object value;
            string text = input.TryGetValue("text", out value) ? value as string : null;
            if (text == null)
            {
                await Write(context, new { error = "Missing required string field: text" }, 400);
                return;
            }

            try
            {
                using (var cts = new CancellationTokenSource(RequestTimeout))
                {
                    await Write(context, await _guard.InspectAsync(text, cts.Token));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("[LlmGuard] Inspection failed", ex);
                await Write(context, new { error = "LLM guard unavailable: " + ex.Message }, 503);
            }
        }

        public void Shutdown()
        {
            _guard.Shutdown();
        }

        private static async Task<string> ReadBody(HttpListenerContext context)
        {
            if (context.Request.ContentLength64 > MaxRequestBodyBytes) throw new InvalidDataException("Request body is too large.");
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                string body = await reader.ReadToEndAsync();
                if (body.Length > MaxRequestBodyBytes) throw new InvalidDataException("Request body is too large.");
                return body;
            }
        }

        private static async Task Write(HttpListenerContext context, object value, int status = 200)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(JsonCodec.Serialize(value));
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }
    }
}
