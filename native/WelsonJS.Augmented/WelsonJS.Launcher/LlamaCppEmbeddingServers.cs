// LlamaCppEmbeddingServers.cs
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
    /// Embedding models served by the WelsonJS llama.cpp build (llama-server.exe in
    /// embedding mode), for example the original google/embeddinggemma-2.
    ///
    /// The llama.cpp router (LlamaCppRouter) serves chat models, and every model instance
    /// it starts shares its options, so an embedding model runs in a llama-server of its
    /// own: llama-server --embedding with the model file and its pooling (from the GGUF),
    /// one process per model, started on first use (LocalServerPool, the same process
    /// handling as the router and the CrispEmbed servers) and at most
    /// LlamaCppEmbeddingModelsMax at a time.
    ///
    /// A model is a folder under %APPDATA%\WelsonJS\llamacpp_embedding_models named after
    /// the installer component, holding one .gguf file (mmproj files, the vision and audio
    /// encoders, are not used). The folder is not in %APPDATA%\WelsonJS itself, where the
    /// router would list it as a chat model.
    /// </summary>
    public sealed class LlamaCppEmbeddingServers
    {
        private static readonly Regex ModelNamePattern = new Regex(@"^[A-Za-z0-9][A-Za-z0-9_.\-]{0,127}$");

        // Embedding a batch on a CPU can take a while; each call is bounded by the caller's token.
        private static readonly HttpClient Http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        private readonly LocalServerPool _pool;

        public LlamaCppEmbeddingServers(LocalServerRegistry registry)
        {
            _pool = new LocalServerPool(CreateOptions, () => ModelsMax, registry);
        }

        /// <summary>The folder of the embedding models (%APPDATA%\WelsonJS\llamacpp_embedding_models).</summary>
        public static string ModelsDirectory
        {
            get { return Path.Combine(Program.GetAppDataPath(), "llamacpp_embedding_models"); }
        }

        /// <summary>Embedding servers running at once (LlamaCppEmbeddingModelsMax, default 1).</summary>
        public static int ModelsMax
        {
            get { return GetIntSetting("LlamaCppEmbeddingModelsMax", 1, 1); }
        }

        /// <summary>
        /// The context of an embedding server (LlamaCppEmbeddingContextSize, default 2048). An
        /// input is embedded in one batch, so it is also the longest input in tokens; the
        /// bidirectional attention over a batch of this size needs memory that grows with
        /// its square.
        /// </summary>
        public static int ContextSize
        {
            get { return GetIntSetting("LlamaCppEmbeddingContextSize", 2048, 256); }
        }

        /// <summary>The installed embedding models: folders under ModelsDirectory that hold a model .gguf file.</summary>
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

        public static string ValidateModelName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A model name is required (the installer component, for example embeddinggemma_2).");
            name = name.Trim();
            if (!ModelNamePattern.IsMatch(name))
                throw new ArgumentException("Invalid model name: " + name);
            return name;
        }

        /// <summary>The model's .gguf file (not an mmproj file), or null when it is not installed.</summary>
        public static string FindModelFile(string name)
        {
            string folder = Path.Combine(ModelsDirectory, ValidateModelName(name));
            if (!Directory.Exists(folder)) return null;
            return Directory.GetFiles(folder, "*.gguf")
                .Where(f => Path.GetFileName(f).IndexOf("mmproj", StringComparison.OrdinalIgnoreCase) < 0)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        /// <summary>True when the name is an installed llama.cpp embedding model.</summary>
        public static bool IsModel(string name)
        {
            try { return FindModelFile(name) != null; }
            catch (ArgumentException) { return false; }
        }

        public Dictionary<string, object> Status()
        {
            return new Dictionary<string, object>
            {
                ["executablePath"] = LlamaCppRouter.ExecutablePath,
                ["modelsDirectory"] = ModelsDirectory,
                ["modelsMax"] = ModelsMax,
                ["contextSize"] = ContextSize,
                ["models"] = InstalledModels.Select(name => new Dictionary<string, object>
                {
                    ["id"] = name,
                    ["running"] = _pool.IsRunning(name)
                }).ToList(),
                ["servers"] = _pool.Status()
            };
        }

        public bool Stop(string name)
        {
            return _pool.Stop(ValidateModelName(name));
        }

        public void StopAll()
        {
            _pool.StopAll();
        }

        /// <summary>
        /// Sends an OpenAI embeddings request body, unchanged, to the model's server (starting
        /// it if needed) and returns the status code and body. "dimensions" (Matryoshka
        /// truncation, which llama-server does not do) is applied to the response: each
        /// vector keeps its leading values and is normalized again.
        /// </summary>
        public async Task<KeyValuePair<int, string>> EmbedAsync(string name, string json, CancellationToken token)
        {
            name = ValidateModelName(name);
            if (FindModelFile(name) == null)
                throw new FileNotFoundException("The llama.cpp embedding model '" + name + "' is not installed: no .gguf file in " +
                    Path.Combine(ModelsDirectory, name) + " (install the setup component llamacpp\\" + name + ").");

            Dictionary<string, object> request = JsonCodec.DeserializeObject(json);
            int dimensions = 0;
            object value;
            if (request != null && request.TryGetValue("dimensions", out value) && value != null)
            {
                double requested;
                if (!double.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
                        System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out requested) ||
                    requested < 1 || requested != Math.Floor(requested))
                    throw new ArgumentException("\"dimensions\" must be a positive integer.");
                dimensions = (int)requested;
            }

            KeyValuePair<int, string> result;
            using (LocalServerLease lease = await _pool.AcquireAsync(name, token))
            using (var content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await Http.PostAsync(lease.BaseUrl + "/v1/embeddings", content, token))
            {
                result = new KeyValuePair<int, string>((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            if (dimensions == 0 || result.Key < 200 || result.Key > 299) return result;
            return new KeyValuePair<int, string>(result.Key, Truncate(result.Value, dimensions));
        }

        // Keeps the first n values of every embedding and normalizes it to unit length again.
        private static string Truncate(string json, int dimensions)
        {
            Dictionary<string, object> response = JsonCodec.DeserializeObject(json);
            object data;
            if (response == null || !response.TryGetValue("data", out data) || !(data is object[])) return json;
            foreach (object item in (object[])data)
            {
                var entry = item as Dictionary<string, object>;
                object embedding;
                if (entry == null || !entry.TryGetValue("embedding", out embedding) || !(embedding is object[])) continue;
                object[] values = (object[])embedding;
                if (dimensions > values.Length)
                    throw new ArgumentException("\"dimensions\" (" + dimensions + ") is larger than the model's " + values.Length + ".");
                double[] kept = values.Take(dimensions).Select(v => Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                double norm = Math.Sqrt(kept.Sum(v => v * v));
                entry["embedding"] = kept.Select(v => (object)(norm > 0 ? v / norm : v)).ToArray();
            }
            return JsonCodec.Serialize(response);
        }

        private static LocalServerOptions CreateOptions(string name)
        {
            return new LocalServerOptions
            {
                Name = "llama-server embeddings (" + name + ")",
                ExecutablePath = LlamaCppRouter.ExecutablePath,
                MissingExecutableMessage = "llama-server.exe was not found (install the setup component llamacpp).",
                BuildArguments = (host, port) =>
                {
                    string modelFile = FindModelFile(name);
                    if (modelFile == null)
                        throw new FileNotFoundException("The llama.cpp embedding model '" + name + "' is not installed.", Path.Combine(ModelsDirectory, name));
                    string context = ContextSize.ToString();
                    return new List<string>
                    {
                        "--host", host,
                        "--port", port.ToString(),
                        "-m", modelFile,
                        "--embedding",
                        // An input is embedded in one batch: the batch sizes match the context.
                        "--ctx-size", context,
                        "--batch-size", context,
                        "--ubatch-size", context
                    };
                },
                ReadyPath = "/health",
                ReadyTimeout = TimeSpan.FromSeconds(120)
            };
        }

        private static int GetIntSetting(string key, int fallback, int minimum)
        {
            int value;
            return int.TryParse(Program.GetAppConfig(key), out value) && value >= minimum ? value : fallback;
        }
    }
}
