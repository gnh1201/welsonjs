// LlamaCppChat.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Chat completions with the llama.cpp router, shared by POST llamacpp/chat and
    /// POST v1/chat/completions: validates the request, translates the prompt for the
    /// models in LlamaCppTranslatedModels (LlamaCppPromptTranslator), and returns the
    /// llama-server response (an OpenAI chat.completion) with its status code.
    /// </summary>
    public sealed class LlamaCppChat
    {
        // Chat request fields passed through to llama-server. chat_template_kwargs lets
        // callers set template options such as {"enable_thinking": false} for reasoning models.
        private static readonly string[] ChatParameters = { "temperature", "top_p", "top_k", "max_tokens", "presence_penalty", "frequency_penalty", "stop", "seed", "chat_template_kwargs" };

        private readonly LlamaCppRouter _router;
        private readonly LlamaCppPromptTranslator _translator;
        private readonly ILogger _logger;

        public LlamaCppChat(LlamaCppRouter router, ILogger logger)
        {
            if (router == null) throw new ArgumentNullException(nameof(router));
            _router = router;
            _translator = new LlamaCppPromptTranslator(router);
            _logger = logger;
        }

        /// <summary>True when the name is a llama.cpp model: configured (LlamaCppModels) or installed.</summary>
        public static bool IsModel(string name)
        {
            try { LlamaCppRouter.ValidateModelName(name); }
            catch (ArgumentException) { return false; }
            return ApiEndpoints.LlamaCpp.ConfiguredModels.Contains(name) || LlamaCppRouter.FindModelFile(name) != null;
        }

        /// <summary>
        /// Runs a chat completion. input: {"model", "messages", generation parameters,
        /// optional "language" (system language code) and "translate": false}. Throws
        /// ArgumentException for an invalid request and FileNotFoundException when the
        /// model is not installed.
        /// </summary>
        public async Task<KeyValuePair<int, string>> CompleteAsync(Dictionary<string, object> input, CancellationToken token)
        {
            string model = LlamaCppRouter.ValidateModelName(GetString(input, "model"));
            Dictionary<string, object> request = BuildRequest(input);
            string language = LlamaCppPromptTranslator.ResolveLanguage(GetString(input, "language"));
            object value;
            bool translate = !(input.TryGetValue("translate", out value) && false.Equals(value));

            Dictionary<string, object> translation = null;
            if (translate && LlamaCppPromptTranslator.AppliesTo(model))
            {
                try
                {
                    translation = await _translator.PrepareAsync(request, language, token);
                }
                catch (FileNotFoundException ex)
                {
                    // The translation model is not installed: send the prompt as it is.
                    _logger?.Warn("[LlamaCpp] Prompt not translated: " + ex.Message);
                }
            }

            KeyValuePair<int, string> result = await _router.ChatAsync(model, request, token);
            if (translation == null || result.Key < 200 || result.Key > 299)
                return result;

            // Report the English prompt that the model answered.
            Dictionary<string, object> response = JsonCodec.DeserializeObject(result.Value);
            if (response == null) return result;
            response["welsonjs_translation"] = translation;
            return new KeyValuePair<int, string>(result.Key, JsonCodec.Serialize(response));
        }

        // Keeps "messages" (an array of {role, content} objects) and the known generation parameters.
        private static Dictionary<string, object> BuildRequest(Dictionary<string, object> input)
        {
            object value;
            object[] messages = input.TryGetValue("messages", out value) ? value as object[] : null;
            if (messages == null || messages.Length == 0)
                throw new ArgumentException("\"messages\" must be a non-empty array.");

            foreach (object item in messages)
            {
                var message = item as Dictionary<string, object>;
                if (message == null || !(message.TryGetValue("role", out value) && value is string) ||
                    !(message.TryGetValue("content", out value) && value is string))
                    throw new ArgumentException("Each message must be an object with \"role\" and \"content\" strings.");
            }

            var request = new Dictionary<string, object> { ["messages"] = messages };
            foreach (string key in ChatParameters)
            {
                if (input.TryGetValue(key, out value) && value != null)
                    request[key] = value;
            }
            return request;
        }

        private static string GetString(Dictionary<string, object> input, string key)
        {
            object value;
            return input.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : null;
        }
    }
}
