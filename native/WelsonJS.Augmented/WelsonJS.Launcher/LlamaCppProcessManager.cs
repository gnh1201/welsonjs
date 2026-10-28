// LlamaCppProcessManager.cs
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace WelsonJS.Launcher
{
    public sealed class LlamaCppProcessManager
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, ManagedProcess> _processes = new Dictionary<string, ManagedProcess>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, object> Start(string modelPath, string executablePath, string host, int port, int contextSize, int threads)
        {
            lock (_sync)
            {
                CleanupExited();
                host = NormalizeHost(host);
                port = ValidatePort(port);
                contextSize = contextSize <= 0 ? 4096 : contextSize;
                threads = threads < 0 ? 0 : threads;
                executablePath = string.IsNullOrEmpty(executablePath)
                    ? Path.Combine(Program.GetAppDataPath(), "llamacpp", "llama-server.exe")
                    : Path.GetFullPath(executablePath);
                modelPath = Path.GetFullPath(modelPath ?? string.Empty);

                if (!File.Exists(executablePath)) throw new FileNotFoundException("llama-server.exe was not found.", executablePath);
                if (!string.Equals(Path.GetFileName(executablePath), "llama-server.exe", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Only llama-server.exe may be launched.", nameof(executablePath));
                if (!File.Exists(modelPath)) throw new FileNotFoundException("GGUF model was not found.", modelPath);
                if (_processes.Values.Any(x => x.Port == port && !x.Process.HasExited))
                    throw new InvalidOperationException("The requested port is already managed by another llama.cpp process.");
                if (!IsPortAvailable(port))
                    throw new InvalidOperationException("The requested port is already in use.");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = BuildArguments(host, port, contextSize, threads, modelPath),
                        WorkingDirectory = Path.GetDirectoryName(executablePath),
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                if (!process.Start()) throw new InvalidOperationException("Failed to start llama-server.exe.");

                var managed = new ManagedProcess
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Process = process,
                    ExecutablePath = executablePath,
                    ModelPath = modelPath,
                    Host = host,
                    Port = port,
                    StartedAtUtc = DateTime.UtcNow
                };
                _processes.Add(managed.Id, managed);
                return ToInfo(managed);
            }
        }

        public List<Dictionary<string, object>> List()
        {
            lock (_sync)
            {
                CleanupExited();
                return _processes.Values.Select(ToInfo).ToList();
            }
        }

        public Dictionary<string, object> Get(string id)
        {
            lock (_sync)
            {
                ManagedProcess item;
                if (string.IsNullOrEmpty(id) || !_processes.TryGetValue(id, out item)) return null;
                return ToInfo(item);
            }
        }

        public bool Stop(string id)
        {
            lock (_sync)
            {
                ManagedProcess item;
                if (string.IsNullOrEmpty(id) || !_processes.TryGetValue(id, out item)) return false;
                if (!item.Process.HasExited)
                {
                    item.Process.Kill();
                    item.Process.WaitForExit(5000);
                }
                item.Process.Dispose();
                _processes.Remove(id);
                return true;
            }
        }

        private void CleanupExited()
        {
            foreach (var item in _processes.Values.Where(x => x.Process.HasExited).ToList())
            {
                item.Process.Dispose();
                _processes.Remove(item.Id);
            }
        }

        private static string NormalizeHost(string host)
        {
            if (string.IsNullOrEmpty(host) || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return "127.0.0.1";
            if (!string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) && !string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("llama.cpp may only bind to localhost.", nameof(host));
            return host;
        }

        private static int ValidatePort(int port)
        {
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            return port;
        }

        private static bool IsPortAvailable(int port)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return true;
            }
            catch (SocketException) { return false; }
            finally { if (listener != null) listener.Stop(); }
        }

        private static string BuildArguments(string host, int port, int contextSize, int threads, string modelPath)
        {
            var args = new List<string> { "--host", Quote(host), "--port", port.ToString(), "--ctx-size", contextSize.ToString() };
            if (threads > 0) { args.Add("--threads"); args.Add(threads.ToString()); }
            args.Add("-m"); args.Add(Quote(modelPath));
            return string.Join(" ", args);
        }

        private static string Quote(string value) { return "\"" + value.Replace("\\\"", "\\\\\"") + "\""; }

        private static Dictionary<string, object> ToInfo(ManagedProcess item)
        {
            bool exited = item.Process.HasExited;
            return new Dictionary<string, object>
            {
                ["id"] = item.Id, ["pid"] = item.Process.Id, ["running"] = !exited,
                ["host"] = item.Host, ["port"] = item.Port, ["portListening"] = !exited && IsPortListening(item.Host, item.Port),
                ["executablePath"] = item.ExecutablePath, ["modelPath"] = item.ModelPath,
                ["startedAtUtc"] = item.StartedAtUtc.ToString("o")
            };
        }

        private static bool IsPortListening(string host, int port)
        {
            try { using (var client = new TcpClient()) { return client.ConnectAsync(host, port).Wait(250); } }
            catch { return false; }
        }

        private sealed class ManagedProcess
        {
            public string Id; public Process Process; public string ExecutablePath; public string ModelPath;
            public string Host; public int Port; public DateTime StartedAtUtc;
        }
    }
}
