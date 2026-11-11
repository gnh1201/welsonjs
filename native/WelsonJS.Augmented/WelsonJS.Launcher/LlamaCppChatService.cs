// LlamaCppChatService.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Chat completions with the local llama.cpp generation model (the editor's AI tools).
    /// Uses a running launcher-managed llama-server for the model in the LlamaCppModelPath
    /// setting, or starts one on first use (on a launcher-allocated localhost port). The
    /// process is started in the shared manager, so it is listed by /llamacpp/list and
    /// can be reused by other clients (lib/llamacpp.js).
    /// </summary>
    public sealed class LlamaCppChatService
    {
        private const int ContextSize = 8192;
        private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(120);

        // Generation on a CPU can take minutes, longer than the shared client's timeout
        // (HttpClientTimeout); each request is bounded by the caller's cancellation token.
        private static readonly HttpClient Http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        private readonly LlamaCppProcessManager _processes;
        private readonly SemaphoreSlim _startLock = new SemaphoreSlim(1, 1);

        public LlamaCppChatService(LlamaCppProcessManager processes)
        {
            if (processes == null) throw new ArgumentNullException(nameof(processes));
            _processes = processes;
        }

        /// <summary>
        /// The GGUF model from the LlamaCppModelPath setting (app.config, falling back to
        /// the built-in resource). Environment variables are expanded, and a relative path
        /// is resolved under %APPDATA%\WelsonJS, where the installer puts the model.
        /// </summary>
        public static string ModelPath
        {
            get
            {
                string configured = Program.GetAppConfig("LlamaCppModelPath");
                if (string.IsNullOrWhiteSpace(configured))
                    throw new InvalidOperationException("LlamaCppModelPath is not configured.");

                string path = Environment.ExpandEnvironmentVariables(configured.Trim());
                if (!Path.IsPathRooted(path))
                    path = Path.Combine(Program.GetAppDataPath(), path);
                return Path.GetFullPath(path);
            }
        }

        /// <summary>
        /// Sends an OpenAI-compatible chat completion request (non-streaming) and returns
        /// the llama-server response status and JSON body unchanged.
        /// </summary>
        public async Task<KeyValuePair<int, string>> CompleteAsync(Dictionary<string, object> request, CancellationToken token)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string baseUrl = await EnsureServerAsync(token);

            request["stream"] = false;
            string body = JsonCodec.Serialize(request);

            using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await Http.PostAsync(baseUrl + "/v1/chat/completions", content, token))
            {
                string responseText = await response.Content.ReadAsStringAsync();
                return new KeyValuePair<int, string>((int)response.StatusCode, responseText);
            }
        }

        private async Task<string> EnsureServerAsync(CancellationToken token)
        {
            await _startLock.WaitAsync(token);
            try
            {
                string modelPath = ModelPath;

                // Reuse a running server for the same model, started here or by /llamacpp/start.
                foreach (Dictionary<string, object> info in _processes.List())
                {
                    if (true.Equals(info["running"]) && true.Equals(info["portListening"]) &&
                        string.Equals(Convert.ToString(info["modelPath"]), modelPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return "http://" + info["host"] + ":" + info["port"];
                    }
                }

                if (!File.Exists(modelPath))
                    throw new FileNotFoundException("The llama.cpp model was not found at " + modelPath + " (install the setup component llamacpp\\hy_mt2_1_8b or set LlamaCppModelPath).", modelPath);

                Dictionary<string, object> started = _processes.Start(modelPath, null, "127.0.0.1", 0, ContextSize, 0);
                string id = (string)started["id"];
                string baseUrl = "http://" + started["host"] + ":" + started["port"];

                // Model loading is asynchronous; /health answers 200 once it can serve.
                DateTime deadline = DateTime.UtcNow + ReadyTimeout;
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    Dictionary<string, object> info = _processes.Get(id);
                    if (info == null || !true.Equals(info["running"]))
                        throw new InvalidOperationException("The llama-server process exited during startup.");

                    try
                    {
                        using (HttpResponseMessage health = await Http.GetAsync(baseUrl + "/health", token))
                        {
                            if (health.IsSuccessStatusCode)
                                return baseUrl;
                        }
                    }
                    catch (HttpRequestException)
                    {
                        // Not listening yet.
                    }

                    if (DateTime.UtcNow > deadline)
                    {
                        _processes.Stop(id);
                        throw new TimeoutException("The llama-server did not become ready in " + ReadyTimeout.TotalSeconds + " seconds.");
                    }
                    await Task.Delay(500, token);
                }
            }
            finally
            {
                _startLock.Release();
            }
        }
    }
}
