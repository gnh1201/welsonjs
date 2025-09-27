// TraceLogger.cs (WelsonJS.Launcher)
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
// We use the ICompatibleLogger interface to maintain a BCL-first style.
// This allows for later replacement with logging libraries such as ILogger or Log4Net.
// 
using System;
using System.Diagnostics;
using System.Linq;

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
            catch (Exception ex)
            {
                // Fallback when the process cannot write to the working directory
                Trace.Listeners.Add(new ConsoleTraceListener());
                Trace.TraceWarning($"TraceLogger: failed to initialize file listener '{_logFileName}'. Falling back to ConsoleTraceListener. Error: {ex.Message}");
            }
            Trace.AutoFlush = true;
        }

        public void Info(params object[] args) => Trace.TraceInformation(Format(args));
        public void Warn(params object[] args) => Trace.TraceWarning(Format(args));
        public void Error(params object[] args) => Trace.TraceError(Format(args));

        private static string Format(object[] args)
        {
            if (args == null || args.Length == 0) return string.Empty;

            if (args.Length == 1)
                return args[0]?.ToString() ?? string.Empty;

            string format = args[0]?.ToString() ?? string.Empty;
            try
            {
                return string.Format(format, args.Skip(1).ToArray());
            }
            catch
            {
                // In case of mismatched format placeholders
                return string.Join(" ", args);
            }
        }
    }
}
