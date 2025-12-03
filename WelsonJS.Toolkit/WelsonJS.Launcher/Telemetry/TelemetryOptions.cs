// TelemetryOptions.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
namespace WelsonJS.Launcher.Telemetry
{
    public class TelemetryOptions
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public string DistinctId { get; set; }
        public bool Disabled { get; set; } = false;
    }
}
