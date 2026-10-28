// WelsonJsMcpProcessManager.cs
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace WelsonJS.Launcher
{
    public sealed class WelsonJsMcpProcessManager
    {
        private readonly object _sync = new object();
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private Process _process;
        private int _requestId;

        public Dictionary<string, object> Start()
        {
            lock (_sync)
            {
                EnsureStarted();
                return Status();
            }
        }

        public Dictionary<string, object> Status()
        {
            lock (_sync)
            {
                bool running = _process != null && !_process.HasExited;
                return new Dictionary<string, object>
                {
                    ["running"] = running,
                    ["pid"] = running ? (object)_process.Id : null,
                    ["command"] = "cscript.exe app.js mcploader /quiet"
                };
            }
        }

        public object Evaluate(string script, int timeout)
        {
            if (string.IsNullOrEmpty(script)) throw new ArgumentException("Script is required.", nameof(script));
            lock (_sync)
            {
                EnsureStarted();
                var request = new Dictionary<string, object>
                {
                    ["jsonrpc"] = "2.0",
                    ["id"] = ++_requestId,
                    ["method"] = "tools/call",
                    ["params"] = new Dictionary<string, object>
                    {
                        ["name"] = "evaluate_js",
                        ["arguments"] = new Dictionary<string, object>
                        {
                            ["script"] = script,
                            ["allowUnsafeEval"] = true,
                            ["timeout"] = timeout <= 0 ? 900000 : timeout
                        }
                    }
                };

                _process.StandardInput.WriteLine(_serializer.Serialize(request));
                _process.StandardInput.Flush();
                string responseLine = _process.StandardOutput.ReadLine();
                if (string.IsNullOrEmpty(responseLine))
                    throw new InvalidOperationException("welsonjs-mcp exited without returning a response.");

                var response = _serializer.DeserializeObject(responseLine) as Dictionary<string, object>;
                if (response == null) throw new InvalidOperationException("Invalid JSON-RPC response from welsonjs-mcp.");
                object error;
                if (response.TryGetValue("error", out error) && error != null) return response;
                object result;
                return response.TryGetValue("result", out result) ? result : response;
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                if (_process == null) return;
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                        _process.WaitForExit(5000);
                    }
                }
                finally
                {
                    _process.Dispose();
                    _process = null;
                }
            }
        }

        private void EnsureStarted()
        {
            if (_process != null && !_process.HasExited) return;
            Stop();

            string root = Program.GetAppRootDirectory();
            string appJs = Path.Combine(root, "app.js");
            if (!File.Exists(appJs)) throw new FileNotFoundException("WelsonJS app.js was not found.", appJs);

            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(systemRoot, "System32", "cscript.exe"),
                    Arguments = "//nologo app.js mcploader /quiet",
                    WorkingDirectory = root,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };
            if (!process.Start()) throw new InvalidOperationException("Failed to start welsonjs-mcp.");
            process.ErrorDataReceived += (sender, args) => { /* Drain stderr so MCP output cannot block the child. */ };
            process.BeginErrorReadLine();
            _process = process;

            var initialize = new Dictionary<string, object>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = ++_requestId,
                ["method"] = "initialize",
                ["params"] = new Dictionary<string, object>
                {
                    ["protocolVersion"] = "2025-11-25",
                    ["capabilities"] = new Dictionary<string, object>(),
                    ["clientInfo"] = new Dictionary<string, object> { ["name"] = "WelsonJS Editor", ["version"] = "1.0" }
                }
            };
            _process.StandardInput.WriteLine(_serializer.Serialize(initialize));
            _process.StandardInput.Flush();
            if (string.IsNullOrEmpty(_process.StandardOutput.ReadLine()))
                throw new InvalidOperationException("welsonjs-mcp failed to initialize.");
            SendNotification("notifications/initialized");
        }

        private void SendNotification(string method)
        {
            var request = new Dictionary<string, object>
            {
                ["jsonrpc"] = "2.0",
                ["method"] = method,
                ["params"] = new Dictionary<string, object>()
            };
            _process.StandardInput.WriteLine(_serializer.Serialize(request));
            _process.StandardInput.Flush();
        }
    }
}
