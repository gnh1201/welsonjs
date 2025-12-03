// TelemetryClient.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.Telemetry
{
    public sealed class TelemetryClient : IDisposable
    {
        private readonly ITelemetryProvider _provider;
        private readonly ICompatibleLogger _logger;

        public TelemetryClient(string providerName, TelemetryOptions options, ICompatibleLogger logger = null)
        {
            _provider = TelemetryProviderFactory.Create(providerName, options);
            _logger = logger;
        }

        public Task TrackEventAsync(
            string eventName,
            IDictionary<string, object> properties = null,
            CancellationToken cancellationToken = default)
        {
            return _provider.TrackEventAsync(eventName, properties, cancellationToken);
        }

        public Task TrackAppStartedAsync(string appName, string appVersion)
        {
            var props = new Dictionary<string, object>
            {
                { "app_name", appName },
                { "app_version", appVersion },
                { "os_platform", Environment.OSVersion.Platform.ToString() },
                { "os_version", Environment.OSVersion.Version.ToString() },
                { "timestamp_utc", DateTime.UtcNow.ToString("o") }
            };

            return TrackEventAsync("app_started", props);
        }

        public void Dispose()
        {
            (_provider as IDisposable)?.Dispose();
        }
    }
}
