// CrispEmbedServers.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// The launcher's CrispEmbed servers (crispembed-server.exe, https://github.com/CrispStrobe/CrispEmbed):
    /// text embeddings and typed decisions with GGUF encoder models, on the CPU.
    ///
    /// A model is either a decision model (listed in CrispEmbedDecisionModels, such as
    /// embeddinggemma_2_typed_decision), which answers typed decisions, or an embedding
    /// model, which answers embeddings. A decision model's encoder is fine-tuned for its
    /// decision head, so its vectors are not offered as embeddings: embeddings come from an
    /// embedding model.
    ///
    /// crispembed-server serves one embedding model per process, so the launcher runs one
    /// server per model, started on first use (LocalServerPool, the same process handling as
    /// the llama.cpp router) and at most CrispEmbedModelsMax at a time: starting another
    /// stops the least recently used server that is not answering a request.
    ///
    /// A model is a folder under %APPDATA%\WelsonJS\crispembed_models named after the
    /// installer component, holding one .gguf file (for example
    /// embeddinggemma_2_typed_decision). The models are not in %APPDATA%\WelsonJS itself,
    /// where the llama.cpp router would list every folder with a .gguf file as its model.
    ///
    /// Each server listens on a launcher-allocated port on 127.0.0.1, runs offline (no
    /// model download), and confines the file paths a request may name to the models folder
    /// (--model-root) and to an empty folder for images (--image-root): the launcher only
    /// forwards the embedding and decision routes, but the server's other routes read files
    /// by path, and other software on this machine could reach its port directly.
    /// </summary>
    public sealed class CrispEmbedServers
    {
        private static readonly Regex ModelNamePattern = new Regex(@"^[A-Za-z0-9][A-Za-z0-9_.\-]{0,127}$");

        // Inference on a CPU can take a while; each call is bounded by the caller's token.
        private static readonly HttpClient Http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        private readonly LocalServerPool _pool;

        public CrispEmbedServers(LocalServerRegistry registry)
        {
            _pool = new LocalServerPool(CreateOptions, () => ModelsMax, registry);
        }

        /// <summary>The installed crispembed-server.exe (setup component crispembed).</summary>
        public static string ExecutablePath
        {
            get { return Path.Combine(Program.GetAppDataPath(), "crispembed", "crispembed-server.exe"); }
        }

        /// <summary>The folder of the CrispEmbed models (%APPDATA%\WelsonJS\crispembed_models).</summary>
        public static string ModelsDirectory
        {
            get { return Path.Combine(Program.GetAppDataPath(), "crispembed_models"); }
        }

        /// <summary>Maximum number of CrispEmbed servers running at once (CrispEmbedModelsMax, default 2).</summary>
        public static int ModelsMax
        {
            get
            {
                int value;
                return int.TryParse(Program.GetAppConfig("CrispEmbedModelsMax"), out value) && value >= 1 ? value : 2;
            }
        }

        /// <summary>
        /// The models offered first, in order (CrispEmbedModels setting, a comma-separated list
        /// of model names). Invalid names and duplicates are skipped.
        /// </summary>
        public static List<string> ConfiguredModels
        {
            get
            {
                var models = new List<string>();
                foreach (string item in (Program.GetAppConfig("CrispEmbedModels") ?? "").Split(','))
                {
                    string name = item.Trim();
                    if (name.Length == 0 || !ModelNamePattern.IsMatch(name) || models.Contains(name)) continue;
                    models.Add(name);
                }
                return models;
            }
        }

        /// <summary>
        /// The typed-decision models (CrispEmbedDecisionModels setting, comma-separated). Every
        /// other model is an embedding model.
        /// </summary>
        public static List<string> DecisionModels
        {
            get
            {
                var models = new List<string>();
                foreach (string item in (Program.GetAppConfig("CrispEmbedDecisionModels") ?? "").Split(','))
                {
                    string name = item.Trim();
                    if (name.Length == 0 || !ModelNamePattern.IsMatch(name) || models.Contains(name)) continue;
                    models.Add(name);
                }
                return models;
            }
        }

        /// <summary>True when the model is a typed-decision model (CrispEmbedDecisionModels).</summary>
        public static bool IsDecisionModel(string name)
        {
            return name != null && DecisionModels.Contains(name.Trim());
        }

        /// <summary>"decision" or "embedding".</summary>
        public static string ModelKind(string name)
        {
            return IsDecisionModel(name) ? "decision" : "embedding";
        }

        /// <summary>True when the name is an installed decision model.</summary>
        public static bool IsInstalledDecisionModel(string name)
        {
            return IsModel(name) && IsDecisionModel(name);
        }

        /// <summary>True when the name is an installed embedding model.</summary>
        public static bool IsInstalledEmbeddingModel(string name)
        {
            return IsModel(name) && !IsDecisionModel(name);
        }

        /// <summary>
        /// Throws when a model cannot answer the request kind: embeddings need an embedding
        /// model, typed decisions a decision model.
        /// </summary>
        public static void EnsureKind(string name, string kind)
        {
            name = ValidateModelName(name);
            if (kind == "embedding" && IsDecisionModel(name))
                throw new ArgumentException("'" + name + "' is a typed-decision model: send it typed decisions (POST /v1/decisions or /crispembed/decide). " +
                    "Embeddings need an embedding model; its encoder is fine-tuned for its decision head.");
            if (kind == "decision" && !IsDecisionModel(name))
                throw new ArgumentException("'" + name + "' is not a typed-decision model (CrispEmbedDecisionModels).");
        }

        /// <summary>The installed models: folders under ModelsDirectory that hold a .gguf file.</summary>
        public static List<string> InstalledModels
        {
            get
            {
                if (!Directory.Exists(ModelsDirectory)) return new List<string>();
                return Directory.GetDirectories(ModelsDirectory)
                    .Select(Path.GetFileName)
                    .Where(name => ModelNamePattern.IsMatch(name) && FindModelFile(name) != null)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        /// <summary>Checks a model name; a name selects a folder under ModelsDirectory.</summary>
        public static string ValidateModelName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A model name is required (the installer component, for example embeddinggemma_2_typed_decision).");
            name = name.Trim();
            if (!ModelNamePattern.IsMatch(name))
                throw new ArgumentException("Invalid model name: " + name);
            return name;
        }

        /// <summary>The model's .gguf file (the first one by name in its folder), or null when it is not installed.</summary>
        public static string FindModelFile(string name)
        {
            string folder = Path.Combine(ModelsDirectory, ValidateModelName(name));
            if (!Directory.Exists(folder)) return null;
            return Directory.GetFiles(folder, "*.gguf")
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        /// <summary>True when the name is an installed CrispEmbed model.</summary>
        public static bool IsModel(string name)
        {
            try { return FindModelFile(name) != null; }
            catch (ArgumentException) { return false; }
        }

        /// <summary>The servers created so far, keyed by model, and the settings.</summary>
        public Dictionary<string, object> Status()
        {
            return new Dictionary<string, object>
            {
                ["executablePath"] = ExecutablePath,
                ["installed"] = File.Exists(ExecutablePath),
                ["modelsDirectory"] = ModelsDirectory,
                ["modelsMax"] = ModelsMax,
                ["servers"] = _pool.Status()
            };
        }

        /// <summary>The installed and configured models, with whether each is installed and running.</summary>
        public List<Dictionary<string, object>> Models()
        {
            return ConfiguredModels.Concat(InstalledModels).Distinct(StringComparer.Ordinal).Select(name => new Dictionary<string, object>
            {
                ["id"] = name,
                ["kind"] = ModelKind(name),
                ["configured"] = ConfiguredModels.Contains(name),
                ["installed"] = FindModelFile(name) != null,
                ["running"] = _pool.IsRunning(name)
            }).ToList();
        }

        /// <summary>Starts the server of a model and waits until it is ready; returns its health report.</summary>
        public async Task<Dictionary<string, object>> StartAsync(string name, CancellationToken token)
        {
            name = EnsureInstalled(name);
            using (LocalServerLease lease = await _pool.AcquireAsync(name, token))
            using (HttpResponseMessage response = await Http.GetAsync(lease.BaseUrl + "/health", token))
            {
                return JsonCodec.DeserializeObject(await response.Content.ReadAsStringAsync()) ?? new Dictionary<string, object>();
            }
        }

        /// <summary>Stops the server of a model. Returns false when it was not running.</summary>
        public bool Stop(string name)
        {
            return _pool.Stop(ValidateModelName(name));
        }

        public void StopAll()
        {
            _pool.StopAll();
        }

        /// <summary>
        /// Sends a JSON request body, unchanged, to a route of the model's server (starting it
        /// if needed) and returns the status code and body unchanged. Only the routes the
        /// launcher exposes are passed here.
        /// </summary>
        public async Task<KeyValuePair<int, string>> PostAsync(string name, string route, string json, CancellationToken token)
        {
            name = EnsureInstalled(name);
            using (LocalServerLease lease = await _pool.AcquireAsync(name, token))
            using (var content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await Http.PostAsync(lease.BaseUrl + route, content, token))
            {
                return new KeyValuePair<int, string>((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }

        private static string EnsureInstalled(string name)
        {
            name = ValidateModelName(name);
            if (FindModelFile(name) == null)
                throw new FileNotFoundException("The CrispEmbed model '" + name + "' is not installed: no .gguf file in " +
                    Path.Combine(ModelsDirectory, name) + " (install the setup component crispembed\\" + name + ").");
            return name;
        }

        private static LocalServerOptions CreateOptions(string name)
        {
            string imageRoot = Path.Combine(Program.GetAppDataPath(), "crispembed_images");
            return new LocalServerOptions
            {
                Name = "crispembed-server (" + name + ")",
                ExecutablePath = ExecutablePath,
                MissingExecutableMessage = "crispembed-server.exe was not found (install the setup component crispembed).",
                BuildArguments = (host, port) =>
                {
                    // An empty folder: no request can make the server read an image elsewhere.
                    Directory.CreateDirectory(imageRoot);
                    string modelFile = FindModelFile(name);
                    if (modelFile == null)
                        throw new FileNotFoundException("The CrispEmbed model '" + name + "' is not installed.", Path.Combine(ModelsDirectory, name));
                    return new List<string>
                    {
                        "-m", modelFile,
                        "--host", host,
                        "--port", port.ToString(),
                        "--offline",
                        "--model-root", ModelsDirectory,
                        "--image-root", imageRoot
                    };
                },
                Environment = new Dictionary<string, string>
                {
                    ["CRISPEMBED_OFFLINE"] = "1",
                    // The typed-decision backend of EmbeddingGemma 2 is opt-in, and its quantized
                    // weights reproduce the published results with FP32 activations. Neither
                    // setting changes other models.
                    ["CRISPEMBED_GEMMA_DECISION"] = "1",
                    ["CRISPEMBED_GEMMA_DECISION_F32_DOT"] = "1"
                },
                // The server answers /health/ready once its model is loaded.
                ReadyPath = "/health/ready",
                ReadyTimeout = TimeSpan.FromSeconds(120)
            };
        }
    }
}
