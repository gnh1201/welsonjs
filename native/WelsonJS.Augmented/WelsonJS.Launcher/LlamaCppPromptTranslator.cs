// LlamaCppPromptTranslator.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Prepares chat requests for models that work best in English (LlamaCppTranslatedModels,
    /// for example FrogNano and BitNet): the user messages are translated into English with
    /// the translation model (LlamaCppTranslationModel, Hy-MT2), and the last one says what
    /// the user's system language is. There is no instruction about the reply language (the
    /// model decides), and the answer is not translated.
    ///
    /// Code is never translated: fenced code blocks stay as they are, and only the text
    /// between them is sent to the translation model, sentences with their inline code (so
    /// that a sentence is not split at it). A translation that does not keep every inline
    /// code span unchanged is discarded, and the text is sent as it is. Text without
    /// non-ASCII characters is taken to be English already. Nothing is changed when the
    /// system language is English.
    /// </summary>
    public sealed class LlamaCppPromptTranslator
    {
        private const int MaxCacheEntries = 256;
        private static readonly Regex CodeBlockPattern = new Regex(@"```[\s\S]*?(?:```|$)", RegexOptions.Compiled);
        private static readonly Regex InlineCodePattern = new Regex(@"`[^`\r\n]+`", RegexOptions.Compiled);
        private static readonly Regex LanguageCodePattern = new Regex(@"^[A-Za-z]{2,3}$");

        private readonly LlamaCppRouter _router;
        private readonly object _cacheLock = new object();
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Queue<string> _cacheOrder = new Queue<string>();

        public LlamaCppPromptTranslator(LlamaCppRouter router)
        {
            if (router == null) throw new ArgumentNullException(nameof(router));
            _router = router;
        }

        /// <summary>The translation model (LlamaCppTranslationModel), or null when not set.</summary>
        public static string TranslationModel
        {
            get
            {
                string configured = Program.GetAppConfig("LlamaCppTranslationModel");
                if (string.IsNullOrWhiteSpace(configured)) return null;
                try { return LlamaCppRouter.ValidateModelName(configured); }
                catch (ArgumentException) { return null; }
            }
        }

        /// <summary>The models whose prompts are translated (LlamaCppTranslatedModels, comma-separated).</summary>
        public static List<string> TranslatedModels
        {
            get
            {
                var models = new List<string>();
                foreach (string item in (Program.GetAppConfig("LlamaCppTranslatedModels") ?? "").Split(','))
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

        public static bool AppliesTo(string model)
        {
            string translationModel = TranslationModel;
            return translationModel != null && !string.Equals(model, translationModel, StringComparison.Ordinal) &&
                TranslatedModels.Contains(model);
        }

        /// <summary>
        /// The system language as a language code without the region: the requested one (the
        /// editor sends the browser's UI language, which follows the Windows display language),
        /// or the Windows display language of the launcher.
        /// </summary>
        public static string ResolveLanguage(string requested)
        {
            if (!string.IsNullOrWhiteSpace(requested))
            {
                string code = requested.Trim().Split('-', '_')[0];
                if (LanguageCodePattern.IsMatch(code)) return code.ToLowerInvariant();
            }
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        }

        /// <summary>The English name of a language code, for example "Korean" for "ko".</summary>
        public static string LanguageName(string code)
        {
            try
            {
                string name = CultureInfo.GetCultureInfo(code).EnglishName;
                int paren = name.IndexOf(" (", StringComparison.Ordinal);
                return paren > 0 ? name.Substring(0, paren) : name;
            }
            catch (CultureNotFoundException)
            {
                return code;
            }
        }

        /// <summary>
        /// Translates the user messages of the request into English and adds the user's system
        /// language to the last one. Returns what was done (for the response), or null when
        /// nothing was changed because the system language is English.
        /// </summary>
        public async Task<Dictionary<string, object>> PrepareAsync(Dictionary<string, object> request, string language, CancellationToken token)
        {
            if (language == "en") return null;
            string translationModel = TranslationModel;
            object value;
            object[] messages = request.TryGetValue("messages", out value) ? value as object[] : null;
            if (translationModel == null || messages == null) return null;

            int translated = 0;
            Dictionary<string, object> lastUser = null;
            foreach (object item in messages)
            {
                var message = item as Dictionary<string, object>;
                if (message == null || !"user".Equals(message["role"] as string)) continue;
                lastUser = message;
                string content = message["content"] as string ?? "";
                string english = await TranslateTextAsync(translationModel, content, token);
                if (!string.Equals(english, content, StringComparison.Ordinal))
                {
                    message["content"] = english;
                    translated++;
                }
            }

            string languageName = LanguageName(language);
            if (lastUser != null)
                lastUser["content"] = (lastUser["content"] as string ?? "").TrimEnd() + "\n\nThe user's system language is " + languageName + ".";

            return new Dictionary<string, object>
            {
                ["translationModel"] = translationModel,
                ["language"] = language,
                ["languageName"] = languageName,
                ["translatedMessages"] = translated,
                ["prompt"] = lastUser != null ? lastUser["content"] : null
            };
        }

        // Translates the text between fenced code blocks into English.
        private async Task<string> TranslateTextAsync(string model, string text, CancellationToken token)
        {
            var output = new StringBuilder();
            int position = 0;
            foreach (Match match in CodeBlockPattern.Matches(text))
            {
                output.Append(await TranslateSegmentAsync(model, text.Substring(position, match.Index - position), token));
                output.Append(match.Value);
                position = match.Index + match.Length;
            }
            output.Append(await TranslateSegmentAsync(model, text.Substring(position), token));
            return output.ToString();
        }

        private async Task<string> TranslateSegmentAsync(string model, string segment, CancellationToken token)
        {
            string trimmed = segment.Trim();
            if (trimmed.Length == 0 || !HasNonAscii(trimmed)) return segment;

            string english;
            lock (_cacheLock)
            {
                _cache.TryGetValue(trimmed, out english);
            }
            if (english == null)
            {
                english = await TranslateAsync(model, trimmed, token);
                // Discard a translation that changed or dropped inline code.
                foreach (Match code in InlineCodePattern.Matches(trimmed))
                {
                    if (english.IndexOf(code.Value, StringComparison.Ordinal) < 0)
                    {
                        english = trimmed;
                        break;
                    }
                }
                lock (_cacheLock)
                {
                    if (!_cache.ContainsKey(trimmed))
                    {
                        _cache[trimmed] = english;
                        _cacheOrder.Enqueue(trimmed);
                        while (_cacheOrder.Count > MaxCacheEntries) _cache.Remove(_cacheOrder.Dequeue());
                    }
                }
            }

            // Keep the whitespace around the segment, which separates it from the code.
            int lead = segment.Length - segment.TrimStart().Length;
            int trail = segment.Length - segment.TrimEnd().Length;
            return segment.Substring(0, lead) + english + segment.Substring(segment.Length - trail);
        }

        // The prompt format of the Hy-MT2 model card.
        private async Task<string> TranslateAsync(string model, string text, CancellationToken token)
        {
            var request = new Dictionary<string, object>
            {
                ["messages"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["role"] = "user",
                        ["content"] = "Translate the following text into English. Note that you should only output the translated result without any additional explanation:\n\n" + text
                    }
                },
                ["temperature"] = 0,
                ["max_tokens"] = Math.Min(4096, text.Length * 3 + 64)
            };
            KeyValuePair<int, string> response = await _router.ChatAsync(model, request, token);
            if (response.Key < 200 || response.Key > 299)
                throw new InvalidOperationException("The translation model failed with HTTP " + response.Key + ".");

            Dictionary<string, object> result = JsonCodec.DeserializeObject(response.Value);
            object[] choices = result.TryGetValue("choices", out object c) ? c as object[] : null;
            var first = choices != null && choices.Length > 0 ? choices[0] as Dictionary<string, object> : null;
            var message = first != null && first.TryGetValue("message", out object m) ? m as Dictionary<string, object> : null;
            string answer = message != null && message.TryGetValue("content", out object a) ? a as string : null;
            if (string.IsNullOrWhiteSpace(answer))
                throw new InvalidOperationException("The translation model returned an empty translation.");
            return answer.Trim();
        }

        private static bool HasNonAscii(string text)
        {
            foreach (char ch in text)
                if (ch > 0x7F) return true;
            return false;
        }
    }
}
