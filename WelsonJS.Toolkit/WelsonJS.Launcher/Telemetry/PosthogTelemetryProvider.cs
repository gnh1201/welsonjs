// PosthogTelemetryProvider.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.Telemetry
{
    public sealed class PosthogTelemetryProvider : ITelemetryProvider, IDisposable
    {
        private readonly TelemetryOptions _options;
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public PosthogTelemetryProvider(TelemetryOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new ArgumentException("PostHog API key is missing.");

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new ArgumentException("PostHog BaseUrl is missing.");

            if (string.IsNullOrWhiteSpace(_options.DistinctId))
                _options.DistinctId = $"anon-{Guid.NewGuid():N}";

            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        public async Task TrackEventAsync(
            string eventName,
            IDictionary<string, object> properties = null,
            CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PosthogTelemetryProvider));
            if (_options.Disabled) return;

            if (string.IsNullOrWhiteSpace(eventName))
                return;

            string json;
            using (var ser = new JsSerializer())
            {
                var payload = new Dictionary<string, object>
                {
                    ["api_key"] = _options.ApiKey,
                    ["distinct_id"] = _options.DistinctId,
                    ["event"] = eventName,
                    ["properties"] = properties ?? new Dictionary<string, object>()
                };

                json = ser.Serialize(payload, 0);
            }

            var url = $"{_options.BaseUrl.TrimEnd('/')}/i/v0/e";

            try
            {
                await _httpClient.PostAsync(
                    url,
                    new StringContent(json, Encoding.UTF8, "application/json"),
                    cancellationToken
                );
            }
            catch
            {
                // swallow for fire-and-forget telemetry
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}
