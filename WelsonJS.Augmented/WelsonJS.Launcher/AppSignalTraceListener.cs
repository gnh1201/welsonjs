// AppSignalTraceListener.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
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
        private readonly string _systemInfo;

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

        private string BuildSystemInfo()
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

            return
                "\"machine_name\":" +
                JsonString(Environment.MachineName) + "," +

                "\"os_version\":" +
                JsonString(Environment.OSVersion.ToString()) + "," +

                "\"os_architecture\":" +
                JsonString(osArchitecture) + "," +

                "\"process_architecture\":" +
                JsonString(processArchitecture) + "," +

                "\"process_name\":" +
                JsonString(processName) + "," +

                "\"process_id\":" +
                process.Id + "," +

                "\"runtime_version\":" +
                JsonString(Environment.Version.ToString()) + "," +

                "\"application_version\":" +
                JsonString(applicationVersion);
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
                string json = BuildLogMessage(
                    severity,
                    message);

                _ = SendHttpAsync(json);
            }
            catch
            {
                // Logging must never affect the application.
            }
        }

        private async System.Threading.Tasks.Task SendHttpAsync(
            string json)
        {
            try
            {
                string payload = json + "\n";

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

        private string BuildLogMessage(
            string severity,
            string message)
        {
            string timestamp =
                DateTime.UtcNow.ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

            return
                "{"
                + "\"timestamp\":"
                + JsonString(timestamp)
                + ",\"group\":"
                + JsonString(_group)
                + ",\"severity\":"
                + JsonString(severity)
                + ",\"message\":"
                + JsonString(message)
                + ",\"hostname\":"
                + JsonString(_hostname)
                + ",\"attributes\":{"
                + _systemInfo
                + "}"
                + "}";
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

        private static string JsonString(
            string value)
        {
            if (value == null)
                return "null";

            var builder =
                new StringBuilder();

            builder.Append('"');

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                switch (c)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;

                    case '"':
                        builder.Append("\\\"");
                        break;

                    case '\r':
                        builder.Append("\\r");
                        break;

                    case '\n':
                        builder.Append("\\n");
                        break;

                    case '\t':
                        builder.Append("\\t");
                        break;

                    case '\b':
                        builder.Append("\\b");
                        break;

                    case '\f':
                        builder.Append("\\f");
                        break;

                    default:
                        if (c < 0x20)
                        {
                            builder.Append("\\u");
                            builder.Append(
                                ((int)c).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            builder.Append('"');

            return builder.ToString();
        }
    }
}
