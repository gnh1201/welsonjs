// TraceLogger.cs (WelsonJS.Launcher)
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
// We use the ICompatibleLogger interface to maintain a BCL-first style.
// This allows for later replacement with logging libraries such as ILogger or Log4Net.
// 
using System.Diagnostics;

namespace WelsonJS.Launcher
{
    public class TraceLogger : ICompatibleLogger
    {
        public void Info(string message) => Trace.TraceInformation(message);
        public void Warn(string message) => Trace.TraceWarning(message);
        public void Error(string message) => Trace.TraceError(message);
    }
}
