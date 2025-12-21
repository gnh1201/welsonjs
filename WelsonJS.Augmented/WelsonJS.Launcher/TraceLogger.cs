// TraceLogger.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// File-based trace logger.
    /// Writes to %APPDATA%\WelsonJS\Logs\<Namespace>.<random-6>.<PID>.log
    /// Falls back to current directory if %APPDATA%\WelsonJS\Logs cannot be created.
    /// </summary>
    public class TraceLogger : ICompatibleLogger
    {
        private static readonly string _logFilePath;

        static TraceLogger()
        {
            try
            {
                string ns = typeof(TraceLogger).Namespace ?? "WelsonJS.Launcher";
                string suffix = GenerateRandomSuffix(6);
                int pid = Process.GetCurrentProcess().Id;

                // Try %APPDATA%\WelsonJS\Logs
                string baseDir;
                try
                {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    baseDir = Path.Combine(appData, "WelsonJS", "Logs");
                    Directory.CreateDirectory(baseDir);
                }
                catch
                {
                    // Fallback: current directory
                    baseDir = AppDomain.CurrentDomain.BaseDirectory;
                }

                _logFilePath = Path.Combine(baseDir, $"{ns}.{suffix}.{pid}.log");

                if (!Trace.Listeners.OfType<TextWriterTraceListener>().Any())
                {
                    var fs = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                    var writer = new StreamWriter(fs) { AutoFlush = true };

                    Trace.Listeners.Add(new TextWriterTraceListener(writer)
                    {
                        Name = "FileTraceListener",
                        TraceOutputOptions = TraceOptions.DateTime,
                        Filter = new EventTypeFilter(SourceLevels.Information)
                    });

                    Trace.Listeners.Add(new ConsoleTraceListener());
                }
            }
            catch (Exception ex)
            {
                Trace.Listeners.Add(new ConsoleTraceListener());
                Trace.TraceWarning($"TraceLogger: failed to open log file. Using console. Error: {ex.Message}");
            }

            Trace.AutoFlush = true;
        }

        public void Info(params object[] args) => Trace.TraceInformation(Format(args));
        public void Warn(params object[] args) => Trace.TraceWarning(Format(args));
        public void Error(params object[] args) => Trace.TraceError(Format(args));

        private static string Format(object[] args)
        {
            if (args == null || args.Length == 0)
                return string.Empty;

            if (args.Length == 1)
                return args[0]?.ToString() ?? string.Empty;

            string fmt = args[0]?.ToString() ?? string.Empty;
            try {
                return string.Format(fmt, args.Skip(1).ToArray());
            }
            catch {
                return string.Join(" ", args.Select(a => a?.ToString() ?? ""));
            }
        }

        private static string GenerateRandomSuffix(int length)
        {
            char[] buf = new char[length];
            byte[] rnd = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(rnd);
            }

            for (int i = 0; i < length; i++)
                buf[i] = (char)('a' + (rnd[i] % 26));

            return new string(buf);
        }
    }
}
