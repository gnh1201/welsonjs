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
        private static readonly string _logFileName;

        static TraceLogger()
        {
            try
            {
                _logFileName = (typeof(TraceLogger).Namespace ?? "WelsonJS.Launcher") + ".log";
                Trace.Listeners.Add(new TextWriterTraceListener(_logFileName));
            }
            catch (System.Exception ex)
            {
                // Fallback when the process cannot write to the working directory
                Trace.Listeners.Add(new ConsoleTraceListener());
                Trace.TraceWarning($"TraceLogger: failed to initialize file listener '{_logFileName}'. Falling back to ConsoleTraceListener. Error: {ex.Message}");
            }
            Trace.AutoFlush = true;
        }

        public void Info(string message) => Trace.TraceInformation(message);
        public void Warn(string message) => Trace.TraceWarning(message);
        public void Error(string message) => Trace.TraceError(message);
    }
}
