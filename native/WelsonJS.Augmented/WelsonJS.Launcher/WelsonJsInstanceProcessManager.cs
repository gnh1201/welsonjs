// WelsonJsInstanceProcessManager.cs
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Runs editor scripts in the same isolated per-GUID layout used by deployed instances.
    /// This deliberately does not use a resident stdio or JSON-RPC process.
    /// </summary>
    public sealed class WelsonJsInstanceProcessManager
    {
        private readonly object _sync = new object();
        private Process _process;
        private string _instanceId;
        private string _workingDirectory;
        private readonly ConcurrentQueue<Dictionary<string, object>> _output = new ConcurrentQueue<Dictionary<string, object>>();
        private int _outputSequence;

        public Dictionary<string, object> Start(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
                throw new ArgumentException("Script is required.", nameof(script));

            lock (_sync)
            {
                Stop();
                while (_output.TryDequeue(out _)) { }
                _outputSequence = 0;

                string instanceId = Guid.NewGuid().ToString();
                string workingDirectory = Program.CreateInstanceDirectory(instanceId);
                string appRoot = Program.GetAppRootDirectory();
                Encoding outputEncoding = GetConsoleOutputEncoding();

                Program.DeployBaseFiles(appRoot, workingDirectory);
                Program.DeployOptionalDataFiles(appRoot, workingDirectory);
                File.WriteAllText(
                    Path.Combine(workingDirectory, "bootstrap.js"),
                    script,
                    new UTF8Encoding(false));
                Program.RecordFirstDeployTime(workingDirectory, instanceId);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Program.GetCscriptPath(),
                        Arguments = "//nologo app.js bootstrap",
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        // cscript writes redirected WSH output using the Windows
                        // console code page, not UTF-8. Decode it before returning
                        // UTF-8 JSON to the browser terminal.
                        StandardOutputEncoding = outputEncoding,
                        StandardErrorEncoding = outputEncoding
                    },
                    EnableRaisingEvents = false
                };

                process.OutputDataReceived += (sender, args) => AddOutput("stdout", args.Data);
                process.ErrorDataReceived += (sender, args) => AddOutput("stderr", args.Data);

                if (!process.Start())
                    throw new InvalidOperationException("Failed to start the WelsonJS instance.");

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                _process = process;
                _instanceId = instanceId;
                _workingDirectory = workingDirectory;

                return Status();
            }
        }

        public Dictionary<string, object> Output(int since)
        {
            var lines = new List<Dictionary<string, object>>();
            foreach (var item in _output)
            {
                int sequence = Convert.ToInt32(item["sequence"]);
                if (sequence > since) lines.Add(item);
            }

            return new Dictionary<string, object>
            {
                ["lines"] = lines,
                ["next"] = _outputSequence
            };
        }

        private void AddOutput(string stream, string data)
        {
            if (data == null) return;
            int sequence = System.Threading.Interlocked.Increment(ref _outputSequence);
            _output.Enqueue(new Dictionary<string, object>
            {
                ["sequence"] = sequence,
                ["stream"] = stream,
                ["text"] = data
            });
        }

        private static Encoding GetConsoleOutputEncoding()
        {
            try
            {
                return Encoding.GetEncoding(checked((int)GetOEMCP()));
            }
            catch
            {
                return Encoding.Default;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetOEMCP();

        public Dictionary<string, object> Status()
        {
            lock (_sync)
            {
                bool running = _process != null && !_process.HasExited;
                return new Dictionary<string, object>
                {
                    ["running"] = running,
                    ["pid"] = running ? (object)_process.Id : null,
                    ["instanceId"] = _instanceId,
                    ["workingDirectory"] = _workingDirectory
                };
            }
        }

        public void Stop()
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
                _instanceId = null;
                _workingDirectory = null;
            }
        }
    }
}
