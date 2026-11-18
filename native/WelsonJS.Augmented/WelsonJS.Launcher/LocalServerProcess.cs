// LocalServerProcess.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>How to start one local HTTP server (see LocalServerProcess).</summary>
    public sealed class LocalServerOptions
    {
        /// <summary>The name used in status reports and error messages, for example "llama-server".</summary>
        public string Name { get; set; }

        /// <summary>The executable to run. A missing file fails with MissingExecutableMessage.</summary>
        public string ExecutablePath { get; set; }

        /// <summary>The error message when ExecutablePath does not exist (for example which setup component installs it).</summary>
        public string MissingExecutableMessage { get; set; }

        /// <summary>
        /// The command-line arguments, built when the process starts from the host and port
        /// the launcher allocated, so settings read here apply at every start. Each argument
        /// is passed as one argument (quoted when needed).
        /// </summary>
        public Func<string, int, IList<string>> BuildArguments { get; set; }

        /// <summary>Environment variables set for the process, in addition to the launcher's.</summary>
        public IDictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();

        /// <summary>The path that answers 2xx once the server can serve requests.</summary>
        public string ReadyPath { get; set; } = "/health";

        /// <summary>How long the server may take to answer ReadyPath after it starts.</summary>
        public TimeSpan ReadyTimeout { get; set; } = TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// One local HTTP server process that the launcher starts on demand and owns: the
    /// llama.cpp router, or a CrispEmbed server for one model. Shared by every server the
    /// launcher runs, so they start, report and stop the same way:
    ///
    /// - The process listens on a launcher-allocated port on 127.0.0.1 (LocalPorts) and is
    ///   started on first use (EnsureStartedAsync), then again after it exits. A start waits
    ///   until ReadyPath answers; a process that exits while starting is started once more on
    ///   a new port, because the port may have been taken between allocation and bind.
    /// - It joins the launcher's job object (ChildProcessJob), so it ends with the launcher
    ///   however the launcher ends, and Stop ends the process with its child processes.
    /// - Its standard output and error are read continuously, so a full pipe never blocks it,
    ///   and the last lines are kept: a start that fails reports the exit code and those
    ///   lines instead of only "exited".
    /// - Acquire marks the server as in use for the duration of a request, so a pool that
    ///   stops idle servers (LocalServerPool) never stops one that is answering.
    /// </summary>
    public sealed class LocalServerProcess
    {
        private const int KeptOutputLines = 40;
        private static readonly HttpClient ProbeClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        private readonly LocalServerOptions _options;
        private readonly object _sync = new object();
        private readonly SemaphoreSlim _startLock = new SemaphoreSlim(1, 1);
        private readonly Queue<string> _output = new Queue<string>();
        private Process _process;
        private string _host;
        private int _port;
        private DateTime _startedAtUtc;
        private DateTime _lastUsedUtc;
        private int _activeUses;
        private int? _lastExitCode;
        private DateTime? _lastExitedAtUtc;

        public LocalServerProcess(LocalServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.Name)) throw new ArgumentException("A server name is required.", nameof(options));
            if (string.IsNullOrEmpty(options.ExecutablePath)) throw new ArgumentException("An executable path is required.", nameof(options));
            if (options.BuildArguments == null) throw new ArgumentException("BuildArguments is required.", nameof(options));
            _options = options;
        }

        public string Name
        {
            get { return _options.Name; }
        }

        public bool IsRunning
        {
            get { lock (_sync) return IsRunningLocked(); }
        }

        /// <summary>The server's base URL (http://127.0.0.1:port), or null when it is not running.</summary>
        public string BaseUrl
        {
            get { lock (_sync) return IsRunningLocked() ? Url() : null; }
        }

        /// <summary>When the server was last started or acquired.</summary>
        public DateTime LastUsedUtc
        {
            get { lock (_sync) return _lastUsedUtc; }
        }

        /// <summary>The number of requests using the server now (see Acquire).</summary>
        public int ActiveUses
        {
            get { lock (_sync) return _activeUses; }
        }

        /// <summary>Starts the server if it is not running, waits until it is ready, and returns its base URL.</summary>
        public async Task<string> EnsureStartedAsync(CancellationToken token)
        {
            await _startLock.WaitAsync(token);
            try
            {
                for (int attempt = 1; ; attempt++)
                {
                    Process process;
                    string baseUrl;
                    lock (_sync)
                    {
                        _lastUsedUtc = DateTime.UtcNow;
                        if (IsRunningLocked()) return Url();
                        process = StartLocked();
                        baseUrl = Url();
                    }

                    try
                    {
                        await WaitUntilReadyAsync(process, baseUrl, token);
                        return baseUrl;
                    }
                    catch (LocalServerExitedException) when (attempt < 2)
                    {
                        // Most likely the allocated port was taken before the server bound it.
                        Trace.TraceWarning(Name + " exited while starting; starting it again on a new port.");
                    }
                }
            }
            finally
            {
                _startLock.Release();
            }
        }

        /// <summary>
        /// Starts the server if needed and marks it as in use until the returned lease is
        /// disposed. Use it around every request sent to the server.
        /// </summary>
        public async Task<LocalServerLease> AcquireAsync(CancellationToken token)
        {
            lock (_sync)
            {
                _activeUses++;
                _lastUsedUtc = DateTime.UtcNow;
            }
            try
            {
                string baseUrl = await EnsureStartedAsync(token);
                return new LocalServerLease(this, baseUrl);
            }
            catch
            {
                Release();
                throw;
            }
        }

        internal void Release()
        {
            lock (_sync)
            {
                if (_activeUses > 0) _activeUses--;
                _lastUsedUtc = DateTime.UtcNow;
            }
        }

        /// <summary>The process state, with the last lines it wrote (null values when it is not running).</summary>
        public Dictionary<string, object> Status()
        {
            lock (_sync)
            {
                bool running = IsRunningLocked();
                return new Dictionary<string, object>
                {
                    ["name"] = Name,
                    ["running"] = running,
                    ["pid"] = running ? (object)_process.Id : null,
                    ["host"] = running ? _host : null,
                    ["port"] = running ? (object)_port : null,
                    ["baseUrl"] = running ? Url() : null,
                    ["startedAtUtc"] = running ? _startedAtUtc.ToString("o") : null,
                    ["lastUsedUtc"] = _lastUsedUtc == default(DateTime) ? null : _lastUsedUtc.ToString("o"),
                    ["activeUses"] = _activeUses,
                    ["lastExitCode"] = _lastExitCode.HasValue ? (object)_lastExitCode.Value : null,
                    ["lastExitedAtUtc"] = _lastExitedAtUtc.HasValue ? _lastExitedAtUtc.Value.ToString("o") : null,
                    ["executablePath"] = _options.ExecutablePath,
                    ["recentOutput"] = _output.ToArray()
                };
            }
        }

        /// <summary>Stops the server and every process it started. Safe to call when it is not running.</summary>
        public void Stop()
        {
            Process process;
            lock (_sync)
            {
                process = _process;
                _process = null;
            }
            if (process == null) return;
            try
            {
                if (!process.HasExited)
                {
                    KillTree(process.Id);
                    process.WaitForExit(5000);
                }
            }
            catch (InvalidOperationException)
            {
                // Already exited.
            }
            finally
            {
                process.Dispose();
            }
        }

        private string Url()
        {
            return "http://" + _host + ":" + _port;
        }

        private bool IsRunningLocked()
        {
            if (_process == null) return false;
            try { if (!_process.HasExited) return true; }
            catch (InvalidOperationException) { }
            _process.Dispose();
            _process = null;
            return false;
        }

        private Process StartLocked()
        {
            string executable = _options.ExecutablePath;
            if (!File.Exists(executable))
                throw new FileNotFoundException(_options.MissingExecutableMessage ?? (Path.GetFileName(executable) + " was not found."), executable);

            _host = LocalPorts.Host;
            _port = LocalPorts.FindAvailable(_host);
            _output.Clear();

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = string.Join(" ", _options.BuildArguments(_host, _port).Select(QuoteArgument)),
                WorkingDirectory = Path.GetDirectoryName(executable),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (KeyValuePair<string, string> variable in _options.Environment)
                startInfo.EnvironmentVariables[variable.Key] = variable.Value;

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.OutputDataReceived += (sender, e) => KeepOutput(e.Data);
            process.ErrorDataReceived += (sender, e) => KeepOutput(e.Data);
            process.Exited += (sender, e) => RecordExit((Process)sender);
            if (!process.Start()) throw new InvalidOperationException("Failed to start " + Path.GetFileName(executable) + ".");

            // End the server and the processes it starts with the launcher, however it ends.
            try { ChildProcessJob.Add(process); }
            catch (Exception ex) { Trace.TraceWarning(Name + " is not in the launcher's job: " + ex.Message); }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _process = process;
            _startedAtUtc = DateTime.UtcNow;
            _lastUsedUtc = _startedAtUtc;
            return process;
        }

        private async Task WaitUntilReadyAsync(Process process, string baseUrl, CancellationToken token)
        {
            DateTime deadline = DateTime.UtcNow + _options.ReadyTimeout;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (HasExited(process))
                {
                    // Let the output readers finish so the message has the last lines, but not
                    // for long: a child process that inherited the pipes can keep them open.
                    Task drain = Task.Run(() => { try { process.WaitForExit(); } catch (Exception) { } });
                    await Task.WhenAny(drain, Task.Delay(2000));
                    throw new LocalServerExitedException(Name + " exited while starting" + ExitCodeText(process) + "." + RecentOutputText());
                }
                try
                {
                    using (HttpResponseMessage response = await ProbeClient.GetAsync(baseUrl + _options.ReadyPath, token))
                    {
                        if (response.IsSuccessStatusCode) return;
                    }
                }
                catch (HttpRequestException)
                {
                    // Not listening yet.
                }
                catch (TaskCanceledException) when (!token.IsCancellationRequested)
                {
                    // The probe timed out; the server may be busy loading.
                }
                if (DateTime.UtcNow > deadline)
                {
                    string output = RecentOutputText();
                    Stop();
                    throw new TimeoutException(Name + " did not become ready in " + _options.ReadyTimeout.TotalSeconds + " seconds." + output);
                }
                await Task.Delay(300, token);
            }
        }

        private void KeepOutput(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            lock (_sync)
            {
                _output.Enqueue(line.Length > 500 ? line.Substring(0, 500) : line);
                while (_output.Count > KeptOutputLines) _output.Dequeue();
            }
        }

        private void RecordExit(Process process)
        {
            int? code = null;
            try { code = process.ExitCode; } catch (InvalidOperationException) { }
            lock (_sync)
            {
                _lastExitCode = code;
                _lastExitedAtUtc = DateTime.UtcNow;
            }
        }

        private string RecentOutputText()
        {
            string[] lines;
            lock (_sync) lines = _output.Skip(Math.Max(0, _output.Count - 8)).ToArray();
            return lines.Length == 0 ? "" : " Last output: " + string.Join(" | ", lines);
        }

        private static bool HasExited(Process process)
        {
            try { return process.HasExited; }
            catch (InvalidOperationException) { return true; }
        }

        private static string ExitCodeText(Process process)
        {
            try { return " (exit code " + process.ExitCode + ")"; }
            catch (InvalidOperationException) { return ""; }
        }

        // Quotes an argument for the Windows command line (CommandLineToArgvW rules): a
        // value with spaces or quotes is wrapped in quotes, backslashes before a quote are
        // doubled, and a trailing backslash does not escape the closing quote.
        private static string QuoteArgument(string value)
        {
            if (value == null) return "\"\"";
            if (value.Length > 0 && value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return value;

            var builder = new StringBuilder("\"");
            int backslashes = 0;
            foreach (char c in value)
            {
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }
                if (c == '"')
                {
                    builder.Append('\\', backslashes * 2 + 1);
                }
                else
                {
                    builder.Append('\\', backslashes);
                }
                backslashes = 0;
                builder.Append(c);
            }
            builder.Append('\\', backslashes * 2);
            builder.Append('"');
            return builder.ToString();
        }

        private static void KillTree(int pid)
        {
            using (var taskkill = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(System.Environment.SystemDirectory, "taskkill.exe"),
                Arguments = "/PID " + pid + " /T /F",
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                if (taskkill != null) taskkill.WaitForExit(5000);
            }
        }
    }

    /// <summary>A server in use by one request (see LocalServerProcess.AcquireAsync).</summary>
    public sealed class LocalServerLease : IDisposable
    {
        private LocalServerProcess _server;

        internal LocalServerLease(LocalServerProcess server, string baseUrl)
        {
            _server = server;
            BaseUrl = baseUrl;
        }

        public string BaseUrl { get; private set; }

        public void Dispose()
        {
            LocalServerProcess server = Interlocked.Exchange(ref _server, null);
            if (server != null) server.Release();
        }
    }

    /// <summary>A local server exited before it became ready.</summary>
    public sealed class LocalServerExitedException : InvalidOperationException
    {
        public LocalServerExitedException(string message) : base(message) { }
    }

    /// <summary>
    /// The local servers the launcher runs, for one status report and one shutdown: the
    /// llama.cpp router and the CrispEmbed servers register here when they are created.
    /// </summary>
    public sealed class LocalServerRegistry
    {
        private readonly object _sync = new object();
        private readonly List<LocalServerProcess> _servers = new List<LocalServerProcess>();

        public void Register(LocalServerProcess server)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            lock (_sync)
            {
                if (!_servers.Contains(server)) _servers.Add(server);
            }
        }

        public void Unregister(LocalServerProcess server)
        {
            lock (_sync) _servers.Remove(server);
        }

        public List<Dictionary<string, object>> Status()
        {
            LocalServerProcess[] servers;
            lock (_sync) servers = _servers.ToArray();
            return servers.Select(s => s.Status()).ToList();
        }

        /// <summary>Stops every registered server (the launcher is exiting).</summary>
        public void StopAll()
        {
            LocalServerProcess[] servers;
            lock (_sync) servers = _servers.ToArray();
            foreach (LocalServerProcess server in servers)
            {
                try { server.Stop(); }
                catch (Exception ex) { Trace.TraceWarning("Could not stop " + server.Name + ": " + ex.Message); }
            }
        }
    }

    /// <summary>
    /// Local servers of one kind keyed by name (one CrispEmbed server per model), at most
    /// MaxRunning at a time: starting another stops the least recently used server that no
    /// request is using. A server is created on first use and registered in the registry.
    /// </summary>
    public sealed class LocalServerPool
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, LocalServerProcess> _servers = new Dictionary<string, LocalServerProcess>(StringComparer.Ordinal);
        private readonly Func<string, LocalServerOptions> _createOptions;
        private readonly Func<int> _maxRunning;
        private readonly LocalServerRegistry _registry;

        public LocalServerPool(Func<string, LocalServerOptions> createOptions, Func<int> maxRunning, LocalServerRegistry registry)
        {
            if (createOptions == null) throw new ArgumentNullException(nameof(createOptions));
            if (maxRunning == null) throw new ArgumentNullException(nameof(maxRunning));
            _createOptions = createOptions;
            _maxRunning = maxRunning;
            _registry = registry;
        }

        /// <summary>Starts the server for a key if needed (stopping idle servers over the limit) and leases it.</summary>
        public Task<LocalServerLease> AcquireAsync(string key, CancellationToken token)
        {
            return Get(key).AcquireAsync(token);
        }

        /// <summary>Starts the server for a key if needed (stopping idle servers over the limit).</summary>
        public Task<string> EnsureStartedAsync(string key, CancellationToken token)
        {
            return Get(key).EnsureStartedAsync(token);
        }

        /// <summary>Stops the server for a key. Returns false when it was not running.</summary>
        public bool Stop(string key)
        {
            LocalServerProcess server;
            lock (_sync)
            {
                if (!_servers.TryGetValue(key, out server)) return false;
            }
            bool running = server.IsRunning;
            server.Stop();
            return running;
        }

        public void StopAll()
        {
            LocalServerProcess[] servers;
            lock (_sync) servers = _servers.Values.ToArray();
            foreach (LocalServerProcess server in servers) server.Stop();
        }

        /// <summary>The servers created so far, keyed by name.</summary>
        public Dictionary<string, Dictionary<string, object>> Status()
        {
            lock (_sync) return _servers.ToDictionary(p => p.Key, p => p.Value.Status(), StringComparer.Ordinal);
        }

        public bool IsRunning(string key)
        {
            lock (_sync)
            {
                LocalServerProcess server;
                return _servers.TryGetValue(key, out server) && server.IsRunning;
            }
        }

        private LocalServerProcess Get(string key)
        {
            LocalServerProcess server;
            List<LocalServerProcess> idle;
            lock (_sync)
            {
                if (!_servers.TryGetValue(key, out server))
                {
                    server = new LocalServerProcess(_createOptions(key));
                    _servers[key] = server;
                    if (_registry != null) _registry.Register(server);
                }

                // Make room for this server: keep at most MaxRunning - 1 others running,
                // stopping the least recently used ones that are not answering a request.
                int keep = Math.Max(0, Math.Max(1, _maxRunning()) - 1);
                LocalServerProcess current = server;
                List<LocalServerProcess> others = _servers.Values
                    .Where(s => s != current && s.IsRunning)
                    .OrderByDescending(s => s.LastUsedUtc)
                    .ToList();
                idle = others.Skip(keep).Where(s => s.ActiveUses == 0).ToList();
            }
            foreach (LocalServerProcess stale in idle)
            {
                Trace.TraceInformation("Stopping " + stale.Name + " to stay within the running server limit.");
                stale.Stop();
            }
            return server;
        }
    }
}
