// LlmGuardService.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Local Qwen3Guard policy gate. The launcher owns the guard's llama-server process
    /// (started on first use, on a launcher-allocated localhost port) and classifies text
    /// as safe, controversial or unsafe. Every failure is reported as an exception so
    /// callers can fail closed.
    /// </summary>
    public sealed class LlmGuardService
    {
        public const string Safe = "safe";
        public const string Controversial = "controversial";
        public const string Unsafe = "unsafe";

        // Qwen3Guard-Gen-0.6B is run with an 8K context; keep each classified chunk
        // well inside it (CJK text can be about one token per character).
        private const int ContextSize = 8192;
        private const int ChunkChars = 4000;
        private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);

        private static readonly Regex SafetyLine = new Regex(@"^\s*Safety\s*:\s*(Safe|Unsafe|Controversial)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static readonly Regex CategoriesLine = new Regex(@"Categories?\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);

        private readonly HttpClient _httpClient;
        // A separate manager keeps the guard model out of /llamacpp/list, where clients
        // look for a generation model to send prompts to.
        private readonly LlamaCppProcessManager _processes = new LlamaCppProcessManager();
        private readonly SemaphoreSlim _startLock = new SemaphoreSlim(1, 1);
        private string _processId;
        private string _baseUrl;

        public LlmGuardService(HttpClient httpClient)
        {
            if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
            _httpClient = httpClient;
        }

        /// <summary>
        /// The GGUF model from the LlmGuardModelPath setting (app.config, falling back to
        /// the built-in resource). Environment variables are expanded, and a relative path
        /// is resolved under %APPDATA%\WelsonJS, where the installer puts the model.
        /// </summary>
        public static string ModelPath
        {
            get
            {
                string configured = Program.GetAppConfig("LlmGuardModelPath");
                if (string.IsNullOrWhiteSpace(configured))
                    throw new InvalidOperationException("LlmGuardModelPath is not configured.");

                string path = Environment.ExpandEnvironmentVariables(configured.Trim());
                if (!Path.IsPathRooted(path))
                    path = Path.Combine(Program.GetAppDataPath(), path);
                return Path.GetFullPath(path);
            }
        }

        /// <summary>
        /// Classifies text. Long text is classified in chunks and the most severe label
        /// wins, so nothing past the model's context window goes unchecked.
        /// </summary>
        public async Task<Dictionary<string, object>> InspectAsync(string text, CancellationToken token)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            if (text.Trim().Length == 0)
            {
                return new Dictionary<string, object>
                {
                    ["label"] = Safe,
                    ["categories"] = new string[0],
                    ["raw"] = ""
                };
            }

            string baseUrl = await EnsureServerAsync(token);

            string label = Safe;
            var categories = new List<string>();
            var raw = new StringBuilder();

            foreach (string chunk in Split(text))
            {
                string content = await ClassifyAsync(baseUrl, chunk, token);
                string chunkLabel = ParseLabel(content);

                if (Severity(chunkLabel) > Severity(label))
                    label = chunkLabel;

                Match match = CategoriesLine.Match(content);
                if (match.Success)
                {
                    foreach (string category in match.Groups[1].Value.Split(','))
                    {
                        // "None" is what a safe chunk reports; it is not a category.
                        string name = category.Trim();
                        if (name.Length > 0 && !name.Equals("None", StringComparison.OrdinalIgnoreCase) && !categories.Contains(name))
                            categories.Add(name);
                    }
                }

                if (raw.Length > 0) raw.Append('\n');
                raw.Append(content);
            }

            return new Dictionary<string, object>
            {
                ["label"] = label,
                ["categories"] = categories,
                ["raw"] = raw.ToString()
            };
        }

        public Dictionary<string, object> Status()
        {
            Dictionary<string, object> process = _processId == null ? null : _processes.Get(_processId);
            var status = new Dictionary<string, object> { ["process"] = process };
            try
            {
                string modelPath = ModelPath;
                status["modelPath"] = modelPath;
                status["modelInstalled"] = File.Exists(modelPath);
            }
            catch (Exception ex)
            {
                // An invalid LlmGuardModelPath setting
                status["modelPath"] = null;
                status["modelInstalled"] = false;
                status["error"] = ex.Message;
            }
            return status;
        }

        public void Shutdown()
        {
            _processes.Shutdown();
        }

        private async Task<string> EnsureServerAsync(CancellationToken token)
        {
            await _startLock.WaitAsync(token);
            try
            {
                if (_processId != null)
                {
                    Dictionary<string, object> info = _processes.Get(_processId);
                    if (info != null && true.Equals(info["running"]))
                        return _baseUrl;
                    _processId = null;
                    _baseUrl = null;
                }

                string modelPath = ModelPath;
                if (!File.Exists(modelPath))
                    throw new FileNotFoundException("The Qwen3Guard model was not found at " + modelPath + " (install the setup component llamacpp\\qwen3guard_06b or set LlmGuardModelPath).", modelPath);

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
                        throw new InvalidOperationException("The Qwen3Guard llama-server process exited during startup.");

                    try
                    {
                        using (HttpResponseMessage health = await _httpClient.GetAsync(baseUrl + "/health", token))
                        {
                            if (health.IsSuccessStatusCode)
                                break;
                        }
                    }
                    catch (HttpRequestException)
                    {
                        // Not listening yet.
                    }

                    if (DateTime.UtcNow > deadline)
                    {
                        _processes.Stop(id);
                        throw new TimeoutException("The Qwen3Guard llama-server did not become ready in " + ReadyTimeout.TotalSeconds + " seconds.");
                    }
                    await Task.Delay(500, token);
                }

                _processId = id;
                _baseUrl = baseUrl;
                return baseUrl;
            }
            finally
            {
                _startLock.Release();
            }
        }

        private async Task<string> ClassifyAsync(string baseUrl, string text, CancellationToken token)
        {
            string body = JsonCodec.Serialize(new Dictionary<string, object>
            {
                ["model"] = "qwen3guard",
                ["messages"] = new[] { new Dictionary<string, object> { ["role"] = "user", ["content"] = text } },
                ["temperature"] = 0,
                ["max_tokens"] = 64,
                ["stream"] = false
            });

            using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await _httpClient.PostAsync(baseUrl + "/v1/chat/completions", content, token))
            {
                string responseText = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("Qwen3Guard request failed with HTTP " + (int)response.StatusCode + ".");

                Dictionary<string, object> result = JsonCodec.DeserializeObject(responseText);
                object[] choices = result.TryGetValue("choices", out object c) ? c as object[] : null;
                var first = choices != null && choices.Length > 0 ? choices[0] as Dictionary<string, object> : null;
                var message = first != null && first.TryGetValue("message", out object m) ? m as Dictionary<string, object> : null;
                string answer = message != null && message.TryGetValue("content", out object a) ? a as string : null;
                if (answer == null)
                    throw new InvalidOperationException("Qwen3Guard returned an empty response.");
                return answer;
            }
        }

        private static string ParseLabel(string content)
        {
            MatchCollection matches = SafetyLine.Matches(content);
            if (matches.Count != 1)
                throw new InvalidOperationException("Qwen3Guard returned an ambiguous or invalid Safety label.");
            return matches[0].Groups[1].Value.ToLowerInvariant();
        }

        private static int Severity(string label)
        {
            switch (label)
            {
                case Unsafe: return 2;
                case Controversial: return 1;
                default: return 0;
            }
        }

        private static IEnumerable<string> Split(string text)
        {
            if (text.Length <= ChunkChars)
            {
                yield return text;
                yield break;
            }

            for (int start = 0; start < text.Length; )
            {
                int length = Math.Min(ChunkChars, text.Length - start);
                // Do not cut a surrogate pair in half.
                if (start + length < text.Length && char.IsHighSurrogate(text[start + length - 1]))
                    length--;
                yield return text.Substring(start, length);
                start += length;
            }
        }
    }
}
