// TraceLogger.cs (WelsonJS.Esent)
// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Diagnostics;

namespace WelsonJS.Esent
{
    public sealed class TraceLogger : ILogger
    {
        private readonly string _name;

        public TraceLogger(Type type)
        {
            _name = type == null ? "Unknown" : type.Name;
        }

        public void Debug(string message)
        {
            Write("DEBUG", message);
        }

        public void Info(string message)
        {
            Write("INFO", message);
        }

        public void Warn(string message)
        {
            Write("WARN", message);
        }

        public void Error(string message)
        {
            Write("ERROR", message);
        }

        public void Error(string message, Exception exception)
        {
            Write("ERROR", message + Environment.NewLine + exception);
        }

        public void Fatal(string message)
        {
            Write("FATAL", message);
        }

        public void Fatal(string message, Exception exception)
        {
            Write("FATAL", message + Environment.NewLine + exception);
        }

        private void Write(string level, string message)
        {
            Trace.WriteLine(string.Format(
                "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}: {3}",
                DateTime.Now,
                level,
                _name,
                message));
        }
    }
}
