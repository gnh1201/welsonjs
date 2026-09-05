// LogFileTraceListener.cs
// SPDX-License-Identifier: MS-RL
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WelsonJS.Service
{
    internal sealed class LogFileTraceListener : TraceListener
    {
        private readonly object _sync = new object();

        private readonly string _logFile;
        private readonly string _errorLogFile;

        public LogFileTraceListener(string directory)
        {
            Directory.CreateDirectory(directory);

            _logFile = Path.Combine(
                directory,
                "welsonjs.log");

            _errorLogFile = Path.Combine(
                directory,
                "welsonjs.error.log");
        }

        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            lock (_sync)
            {
                File.AppendAllText(
                    _logFile,
                    message + Environment.NewLine,
                    Encoding.UTF8);

                if (IsError(message))
                {
                    File.AppendAllText(
                        _errorLogFile,
                        message + Environment.NewLine,
                        Encoding.UTF8);
                }
            }
        }

        private static bool IsError(string message)
        {
            return message.IndexOf(
                "[ERROR]",
                StringComparison.OrdinalIgnoreCase) >= 0
                ||
                message.IndexOf(
                    "[FATAL]",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}