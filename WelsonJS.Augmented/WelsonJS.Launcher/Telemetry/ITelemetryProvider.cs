// ITelemetryProvider.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.Telemetry
{
    public interface ITelemetryProvider
    {
        Task TrackEventAsync(
            string eventName,
            IDictionary<string, object> properties = null,
            CancellationToken cancellationToken = default
        );
    }
}
