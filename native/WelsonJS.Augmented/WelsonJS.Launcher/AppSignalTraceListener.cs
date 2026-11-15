// AppSignalTraceListener.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace WelsonJS.Launcher
{
    public sealed class AppSignalTraceListener : TraceListener
    {
        private const string DefaultEndpoint =
            "https://appsignal-endpoint.net/logs/json";

        private readonly string _endpoint;
        private readonly string _hostname;
        private readonly string _group;
        private readonly Dictionary<string, object> _systemInfo;

        // Log lines written before ChakraCore.dll is loaded (JsonCodec needs it);
        // sent with the first line after it is.
        private const int MaxPendingLogs = 100;
        private readonly Queue<Dictionary<string, object>> _pending =
            new Queue<Dictionary<string, object>>();

        public AppSignalTraceListener(
            string apiKey,
            string hostname = null,
            string group = null,
            string endpoint = null)
        {
            _hostname = string.IsNullOrEmpty(hostname)
                ? Environment.MachineName
                : hostname;

            _group = string.IsNullOrEmpty(group)
                ? "welsonjs"
                : group;

            _endpoint = BuildEndpoint(
                string.IsNullOrEmpty(endpoint)
                    ? DefaultEndpoint
                    : endpoint,
                apiKey);

            _systemInfo = BuildSystemInfo();
        }

        private static string BuildEndpoint(
            string endpoint,
            string apiKey)
        {
            if (string.IsNullOrEmpty(endpoint))
                return null;

            if (string.IsNullOrEmpty(apiKey))
                return null;

            return endpoint
                + "?api_key="
                + Uri.EscapeDataString(apiKey);
        }

        private Dictionary<string, object> BuildSystemInfo()
        {
            var process = Process.GetCurrentProcess();

            string processName = "unknown";

            try
            {
                processName = process.ProcessName;
            }
            catch
            {
                // Ignore process information failures.
            }

            string applicationVersion = "unknown";

            try
            {
                Assembly assembly = Assembly.GetEntryAssembly();

                if (assembly != null)
                {
                    AssemblyName assemblyName = assembly.GetName();

                    if (assemblyName.Version != null)
                        applicationVersion =
                            assemblyName.Version.ToString();
                }
            }
            catch
            {
                // Ignore version detection failures.
            }

            string processArchitecture =
                Environment.Is64BitProcess
                    ? "x64"
                    : "x86";

            string osArchitecture =
                Environment.Is64BitOperatingSystem
                    ? "x64"
                    : "x86";

            return new Dictionary<string, object>
            {
                ["machine_name"] = Environment.MachineName,
                ["os_version"] = Environment.OSVersion.ToString(),
                ["os_architecture"] = osArchitecture,
                ["process_architecture"] = processArchitecture,
                ["process_name"] = processName,
                ["process_id"] = process.Id,
                ["runtime_version"] = Environment.Version.ToString(),
                ["application_version"] = applicationVersion
            };
        }

        public override void Write(string message)
        {
            Send("info", message);
        }

        public override void WriteLine(string message)
        {
            Send("info", message);
        }

        public override void TraceEvent(
            TraceEventCache eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string message)
        {
            Send(
                GetSeverity(eventType),
                message);
        }

        public override void TraceEvent(
            TraceEventCache eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string format,
            params object[] args)
        {
            string message;

            try
            {
                message = string.Format(format, args);
            }
            catch
            {
                message = format;
            }

            Send(
                GetSeverity(eventType),
                message);
        }

        private void Send(
            string severity,
            string message)
        {
            if (string.IsNullOrEmpty(_endpoint))
                return;

            if (message == null)
                message = string.Empty;

            try
            {
                Dictionary<string, object> entry = BuildLogMessage(
                    severity,
                    message);

                var entries = new List<Dictionary<string, object>>();

                // Serialize outside the lock: JsonCodec takes the engine lock, and
                // logging from inside an engine call must not wait on this one.
                lock (_pending)
                {
                    if (!JsonCodec.IsAvailable)
                    {
                        if (_pending.Count >= MaxPendingLogs)
                            _pending.Dequeue();

                        _pending.Enqueue(entry);
                        return;
                    }

                    entries.AddRange(_pending);
                    _pending.Clear();
                }

                entries.Add(entry);

                var lines = new StringBuilder();
                foreach (Dictionary<string, object> item in entries)
                    lines.Append(JsonCodec.Serialize(item)).Append('\n');

                _ = SendHttpAsync(lines.ToString());
            }
            catch
            {
                // Logging must never affect the application.
            }
        }

        // payload: one or more newline-terminated JSON lines (NDJSON)
        private async System.Threading.Tasks.Task SendHttpAsync(
            string payload)
        {
            try
            {
                byte[] data =
                    Encoding.UTF8.GetBytes(payload);

                var request =
                    (HttpWebRequest)
                    WebRequest.Create(_endpoint);

                request.Method = "POST";
                request.ContentType = "application/x-ndjson";
                request.Accept = "application/json";
                request.ContentLength = data.Length;
                request.Timeout = 5000;

                using (Stream stream =
                    await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(
                        data,
                        0,
                        data.Length);
                }

                _ = await request.GetResponseAsync();
            }
            catch
            {
                // Never allow logging failures to affect the application.
            }
        }

        private Dictionary<string, object> BuildLogMessage(
            string severity,
            string message)
        {
            string timestamp =
                DateTime.UtcNow.ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

            return new Dictionary<string, object>
            {
                ["timestamp"] = timestamp,
                ["group"] = _group,
                ["severity"] = severity,
                ["message"] = message,
                ["hostname"] = _hostname,
                ["attributes"] = _systemInfo
            };
        }

        private static string GetSeverity(
            TraceEventType eventType)
        {
            switch (eventType)
            {
                case TraceEventType.Critical:
                    return "fatal";

                case TraceEventType.Error:
                    return "error";

                case TraceEventType.Warning:
                    return "warn";

                case TraceEventType.Verbose:
                    return "debug";

                case TraceEventType.Information:
                default:
                    return "info";
            }
        }
    }
}
