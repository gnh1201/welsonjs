// TelemetryProviderFactory.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;

namespace WelsonJS.Launcher.Telemetry
{
    public static class TelemetryProviderFactory
    {
        public static ITelemetryProvider Create(string provider, TelemetryOptions options, ICompatibleLogger logger = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            provider = provider.ToLowerInvariant();

            switch (provider)
            {
                case "posthog":
                    return new PosthogTelemetryProvider(options, logger);

                default:
                    throw new NotSupportedException(
                        "Unknown telemetry provider: " + provider
                    );
            }
        }
    }
}
