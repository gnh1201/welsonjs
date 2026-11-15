// LlamaCppRouter.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// The launcher's single llama-server, run in router mode (model hot swap).
    ///
    /// The router is started on first use on a launcher-allocated localhost port, with
    /// --models-dir %APPDATA%\WelsonJS: each folder there that holds a .gguf file is a
    /// model named after the folder, which is the installer component name (for example
    /// hy_mt2_1_8b_1_25bit). Every request names its model; the router loads it on
    /// demand and unloads the least recently used model when more than
    /// LlamaCppModelsMax models are loaded. The editor, the LLM guard and
    /// lib/llamacpp.js all share this one router.
    /// </summary>
    public sealed class LlamaCppRouter
    {
        private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan LoadTimeout = TimeSpan.FromSeconds(180);
        private static readonly Regex ModelNamePattern = new Regex(@"^[A-Za-z0-9][A-Za-z0-9_.\-]{0,127}$");

        // Generation on a CPU can take minutes; each call is bounded by the caller's token.
        private static readonly HttpClient Http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        private readonly object _sync = new object();
        private readonly SemaphoreSlim _startLock = new SemaphoreSlim(1, 1);
        private Process _process;
        private string _host;
        private int _port;
        private DateTime _startedAtUtc;

        /// <summary>The folder the router reads models from (%APPDATA%\WelsonJS).</summary>
        public static string ModelsDirectory
        {
            get { return Program.GetAppDataPath(); }
        }

        /// <summary>The installed llama-server.exe; the only executable the launcher runs.</summary>
        public static string ExecutablePath
        {
            get { return Path.Combine(Program.GetAppDataPath(), "llamacpp", "llama-server.exe"); }
        }

        /// <summary>Maximum number of models loaded at once (LlamaCppModelsMax, default 3).</summary>
        public static int ModelsMax
        {
            get { return GetIntSetting("LlamaCppModelsMax", 3, 0); }
        }

        /// <summary>Context size passed to every model instance (LlamaCppContextSize, default 8192).</summary>
        public static int ContextSize
        {
            get { return GetIntSetting("LlamaCppContextSize", 8192, 512); }
        }

        /// <summary>
        /// Checks a model name. A name selects a folder under ModelsDirectory, so only
        /// letters, digits, '_', '.' and '-' are accepted.
        /// </summary>
        public static string ValidateModelName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A model name is required (the installer component, for example hy_mt2_1_8b_1_25bit).");
            name = name.Trim();
            if (!ModelNamePattern.IsMatch(name))
                throw new ArgumentException("Invalid model name: " + name);
            return name;
        }

        /// <summary>
        /// The GGUF file the router uses for a model, found as the router finds it: a
        /// non-mmproj .gguf directly inside ModelsDirectory\name. Null when not installed.
        /// </summary>
        public static string FindModelFile(string name)
        {
            string folder = Path.Combine(ModelsDirectory, ValidateModelName(name));
            if (!Directory.Exists(folder)) return null;
            return Directory.GetFiles(folder, "*.gguf")
                .Where(f => Path.GetFileName(f).IndexOf("mmproj", StringComparison.OrdinalIgnoreCase) < 0)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        /// <summary>Throws a clear error when a model is not installed.</summary>
        public static void EnsureInstalled(string name)
        {
            if (FindModelFile(name) == null)
                throw new FileNotFoundException("The llama.cpp model '" + name + "' is not installed: no .gguf file in " +
                    Path.Combine(ModelsDirectory, name) + " (install the setup component llamacpp\\" + name + ").");
        }

        /// <summary>The router process state (null values when it is not running).</summary>
        public Dictionary<string, object> Status()
        {
            lock (_sync)
            {
                bool running = IsRunning();
                return new Dictionary<string, object>
                {
                    ["running"] = running,
                    ["pid"] = running ? (object)_process.Id : null,
                    ["host"] = running ? _host : null,
                    ["port"] = running ? (object)_port : null,
                    ["baseUrl"] = running ? BaseUrl : null,
                    ["startedAtUtc"] = running ? _startedAtUtc.ToString("o") : null,
                    ["executablePath"] = ExecutablePath,
                    ["modelsDirectory"] = ModelsDirectory,
                    ["modelsMax"] = ModelsMax,
                    ["contextSize"] = ContextSize
                };
            }
        }

        /// <summary>Starts the router if needed and returns its base URL.</summary>
        public async Task<string> EnsureStartedAsync(CancellationToken token)
        {
            await _startLock.WaitAsync(token);
            try
            {
                lock (_sync)
                {
                    if (IsRunning()) return BaseUrl;
                    StartProcess();
                }

                string baseUrl = BaseUrl;
                DateTime deadline = DateTime.UtcNow + ReadyTimeout;
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    lock (_sync)
                    {
                        if (!IsRunning())
                            throw new InvalidOperationException("The llama-server router exited during startup.");
                    }
                    try
                    {
                        using (HttpResponseMessage health = await Http.GetAsync(baseUrl + "/health", token))
                        {
                            if (health.IsSuccessStatusCode) return baseUrl;
                        }
                    }
                    catch (HttpRequestException)
                    {
                        // Not listening yet.
                    }
                    if (DateTime.UtcNow > deadline)
                    {
                        Stop();
                        throw new TimeoutException("The llama-server router did not become ready in " + ReadyTimeout.TotalSeconds + " seconds.");
                    }
                    await Task.Delay(300, token);
                }
            }
            finally
            {
                _startLock.Release();
            }
        }

        /// <summary>The router's model list (GET /models), as returned by llama-server.</summary>
        public async Task<object[]> ListModelsAsync(CancellationToken token)
        {
            string baseUrl = await EnsureStartedAsync(token);
            using (HttpResponseMessage response = await Http.GetAsync(baseUrl + "/models", token))
            {
                string text = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("llama-server /models failed with HTTP " + (int)response.StatusCode + ": " + text);
                Dictionary<string, object> result = JsonCodec.DeserializeObject(text);
                object data;
                return result.TryGetValue("data", out data) && data is object[] ? (object[])data : new object[0];
            }
        }

        /// <summary>
        /// Loads a model and waits until it can serve. Requests also load a model on
        /// demand; loading first gives a clear error (for example the instance's exit
        /// code) instead of a failed request.
        /// </summary>
        public async Task<Dictionary<string, object>> LoadAsync(string name, CancellationToken token)
        {
            name = ValidateModelName(name);
            EnsureInstalled(name);
            string baseUrl = await EnsureStartedAsync(token);

            Dictionary<string, object> model = await FindModelAsync(name, token);
            if (model == null)
                throw new InvalidOperationException("llama-server does not list the model '" + name + "'. Restart the launcher after installing a model.");

            if (StatusValue(model) == "loaded") return model;
            if (StatusValue(model) == "unloaded" || StatusValue(model) == "sleeping")
            {
                await PostAsync(baseUrl + "/models/load", new Dictionary<string, object> { ["model"] = name }, token, allowAlreadyRunning: true);
            }

            DateTime deadline = DateTime.UtcNow + LoadTimeout;
            while (true)
            {
                await Task.Delay(300, token);
                model = await FindModelAsync(name, token);
                string status = model == null ? null : StatusValue(model);
                if (status == "loaded") return model;
                if (status == "unloaded" && IsFailed(model))
                    throw new InvalidOperationException("llama-server could not load the model '" + name + "' (exit code " + ExitCode(model) + ").");
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("The model '" + name + "' was not loaded in " + LoadTimeout.TotalSeconds + " seconds.");
            }
        }

        /// <summary>Unloads a model. Returns false when it was not loaded.</summary>
        public async Task<bool> UnloadAsync(string name, CancellationToken token)
        {
            name = ValidateModelName(name);
            lock (_sync)
            {
                if (!IsRunning()) return false;
            }
            Dictionary<string, object> model = await FindModelAsync(name, token);
            if (model == null || StatusValue(model) == "unloaded") return false;
            await PostAsync(BaseUrl + "/models/unload", new Dictionary<string, object> { ["model"] = name }, token, allowAlreadyRunning: false);
            return true;
        }

        /// <summary>
        /// Sends an OpenAI-compatible chat completion (non-streaming) to the named model and
        /// returns the llama-server status code and JSON body unchanged.
        /// </summary>
        public async Task<KeyValuePair<int, string>> ChatAsync(string name, Dictionary<string, object> request, CancellationToken token)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            name = ValidateModelName(name);
            await LoadAsync(name, token);

            request["model"] = name;
            request["stream"] = false;
            using (var content = new StringContent(JsonCodec.Serialize(request), Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await Http.PostAsync(BaseUrl + "/v1/chat/completions", content, token))
            {
                return new KeyValuePair<int, string>((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }

        /// <summary>Stops the router and every model instance it started.</summary>
        public void Stop()
        {
            Process process;
            lock (_sync)
            {
                process = _process;
                _process = null;
            }
            if (process == null) return;
            try
            {
                if (!process.HasExited)
                {
                    // The model instances are child processes of the router.
                    KillTree(process.Id);
                    process.WaitForExit(5000);
                }
            }
            catch (InvalidOperationException)
            {
                // Already exited.
            }
            finally
            {
                process.Dispose();
            }
        }

        public void Shutdown()
        {
            Stop();
        }

        private string BaseUrl
        {
            get { return "http://" + _host + ":" + _port; }
        }

        private bool IsRunning()
        {
            if (_process == null) return false;
            try { if (!_process.HasExited) return true; }
            catch (InvalidOperationException) { }
            _process.Dispose();
            _process = null;
            return false;
        }

        private void StartProcess()
        {
            string executable = ExecutablePath;
            if (!File.Exists(executable))
                throw new FileNotFoundException("llama-server.exe was not found (install the setup component llamacpp).", executable);

            string modelsDirectory = ModelsDirectory;
            _host = LocalPorts.Host;
            _port = LocalPorts.FindAvailable(_host);

            var arguments = new List<string>
            {
                "--host", _host,
                "--port", _port.ToString(),
                "--models-dir", Quote(modelsDirectory),
                "--models-max", ModelsMax.ToString(),
                "--ctx-size", ContextSize.ToString()
            };

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = string.Join(" ", arguments),
                    WorkingDirectory = Path.GetDirectoryName(executable),
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            if (!process.Start()) throw new InvalidOperationException("Failed to start llama-server.exe.");
            // End the router and its model instances with the launcher, however it ends. The
            // router starts model instances only on request, after this.
            try { ChildProcessJob.Add(process); }
            catch (Exception ex) { Trace.TraceWarning("llama-server is not in the launcher's job: " + ex.Message); }
            _process = process;
            _startedAtUtc = DateTime.UtcNow;
        }

        private async Task<Dictionary<string, object>> FindModelAsync(string name, CancellationToken token)
        {
            foreach (object item in await ListModelsAsync(token))
            {
                var model = item as Dictionary<string, object>;
                object id;
                if (model != null && model.TryGetValue("id", out id) && string.Equals(Convert.ToString(id), name, StringComparison.Ordinal))
                    return model;
            }
            return null;
        }

        private static async Task PostAsync(string url, Dictionary<string, object> body, CancellationToken token, bool allowAlreadyRunning)
        {
            using (var content = new StringContent(JsonCodec.Serialize(body), Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await Http.PostAsync(url, content, token))
            {
                if (response.IsSuccessStatusCode) return;
                string text = await response.Content.ReadAsStringAsync();
                // A request may have started loading the model in the meantime.
                if (allowAlreadyRunning && text.IndexOf("already running", StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("llama-server " + new Uri(url).AbsolutePath + " failed with HTTP " + (int)response.StatusCode + ": " + text);
            }
        }

        private static Dictionary<string, object> StatusObject(Dictionary<string, object> model)
        {
            object status;
            return model.TryGetValue("status", out status) ? status as Dictionary<string, object> : null;
        }

        private static string StatusValue(Dictionary<string, object> model)
        {
            Dictionary<string, object> status = StatusObject(model);
            object value;
            return status != null && status.TryGetValue("value", out value) ? Convert.ToString(value) : null;
        }

        private static bool IsFailed(Dictionary<string, object> model)
        {
            Dictionary<string, object> status = StatusObject(model);
            object failed;
            return status != null && status.TryGetValue("failed", out failed) && true.Equals(failed);
        }

        private static string ExitCode(Dictionary<string, object> model)
        {
            Dictionary<string, object> status = StatusObject(model);
            object code;
            return status != null && status.TryGetValue("exit_code", out code) ? Convert.ToString(code) : "unknown";
        }

        private static int GetIntSetting(string key, int fallback, int minimum)
        {
            int value;
            return int.TryParse(Program.GetAppConfig(key), out value) && value >= minimum ? value : fallback;
        }

        private static void KillTree(int pid)
        {
            using (var taskkill = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "taskkill.exe"),
                Arguments = "/PID " + pid + " /T /F",
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                if (taskkill != null) taskkill.WaitForExit(5000);
            }
        }

        private static string Quote(string value)
        {
            // A trailing backslash would escape the closing quote.
            return "\"" + value.TrimEnd('\\') + "\"";
        }
    }
}
