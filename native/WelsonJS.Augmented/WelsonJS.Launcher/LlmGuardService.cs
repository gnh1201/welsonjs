// LlmGuardService.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Local Qwen3Guard policy gate. The guard model runs in the launcher's shared
    /// llama.cpp router (LlamaCppRouter), which loads it on first use, and classifies
    /// text as safe, controversial or unsafe. Every failure is reported as an exception
    /// so callers can fail closed.
    /// </summary>
    public sealed class LlmGuardService
    {
        public const string Safe = "safe";
        public const string Controversial = "controversial";
        public const string Unsafe = "unsafe";

        // The router runs models with LlamaCppContextSize (8K by default); keep each
        // classified chunk well inside it (CJK text can be about one token per character).
        private const int ChunkChars = 4000;

        private static readonly Regex SafetyLine = new Regex(@"^\s*Safety\s*:\s*(Safe|Unsafe|Controversial)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static readonly Regex CategoriesLine = new Regex(@"Categories?\s*:\s*([^\r\n]+)", RegexOptions.IgnoreCase);

        private readonly LlamaCppRouter _router;

        public LlmGuardService(LlamaCppRouter router)
        {
            if (router == null) throw new ArgumentNullException(nameof(router));
            _router = router;
        }

        /// <summary>
        /// The router model name of the guard (LlmGuardModel setting, app.config, falling
        /// back to the built-in resource): the installer component qwen3guard_06b.
        /// </summary>
        public static string ModelName
        {
            get
            {
                string configured = Program.GetAppConfig("LlmGuardModel");
                if (string.IsNullOrWhiteSpace(configured))
                    throw new InvalidOperationException("LlmGuardModel is not configured.");
                return LlamaCppRouter.ValidateModelName(configured);
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

            string model = ModelName;
            await _router.LoadAsync(model, token);

            string label = Safe;
            var categories = new List<string>();
            var raw = new StringBuilder();

            foreach (string chunk in Split(text))
            {
                string content = await ClassifyAsync(model, chunk, token);
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
            var status = new Dictionary<string, object> { ["server"] = _router.Status() };
            try
            {
                string model = ModelName;
                string file = LlamaCppRouter.FindModelFile(model);
                status["model"] = model;
                status["modelPath"] = file;
                status["modelInstalled"] = file != null;
            }
            catch (Exception ex)
            {
                // An invalid LlmGuardModel setting
                status["model"] = null;
                status["modelPath"] = null;
                status["modelInstalled"] = false;
                status["error"] = ex.Message;
            }
            return status;
        }

        private async Task<string> ClassifyAsync(string model, string text, CancellationToken token)
        {
            var request = new Dictionary<string, object>
            {
                ["messages"] = new[] { new Dictionary<string, object> { ["role"] = "user", ["content"] = text } },
                ["temperature"] = 0,
                ["max_tokens"] = 64
            };

            KeyValuePair<int, string> response = await _router.ChatAsync(model, request, token);
            if (response.Key < 200 || response.Key > 299)
                throw new InvalidOperationException("Qwen3Guard request failed with HTTP " + response.Key + ".");

            Dictionary<string, object> result = JsonCodec.DeserializeObject(response.Value);
            object[] choices = result.TryGetValue("choices", out object c) ? c as object[] : null;
            var first = choices != null && choices.Length > 0 ? choices[0] as Dictionary<string, object> : null;
            var message = first != null && first.TryGetValue("message", out object m) ? m as Dictionary<string, object> : null;
            string answer = message != null && message.TryGetValue("content", out object a) ? a as string : null;
            if (answer == null)
                throw new InvalidOperationException("Qwen3Guard returned an empty response.");
            return answer;
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
