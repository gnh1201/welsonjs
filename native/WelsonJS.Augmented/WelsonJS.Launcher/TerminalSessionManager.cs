// TerminalSessionManager.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// An interactive pseudo console session shared by any number of browser terminals.
    /// Keeps a bounded output history so a reattached terminal can redraw the screen.
    /// </summary>
    public sealed class TerminalSession : IDisposable
    {
        private const int MaxHistoryBytes = 2 * 1024 * 1024;

        private readonly object _sync = new object();
        private readonly ConPtySession _pty;
        private readonly LinkedList<byte[]> _history = new LinkedList<byte[]>();
        private readonly List<Subscriber> _subscribers = new List<Subscriber>();
        private int _historyBytes;
        private int? _exitCode;

        public string Id { get; }
        public string Profile { get; }
        public string Title { get; }
        public string WorkingDirectory { get; }
        public string InstanceId { get; internal set; }
        public DateTime CreatedAt { get; }
        public DateTime? ExitedAt { get; private set; }
        public int ProcessId { get { return _pty.ProcessId; } }
        public bool IsRunning { get { lock (_sync) return !_exitCode.HasValue; } }

        internal TerminalSession(string profile, string title, string commandLine, string workingDirectory, IDictionary<string, string> environment, short cols, short rows)
        {
            Id = Guid.NewGuid().ToString("N");
            Profile = profile;
            Title = title;
            WorkingDirectory = workingDirectory;
            CreatedAt = DateTime.Now;

            _pty = ConPtySession.Start(commandLine, workingDirectory, environment, cols, rows);
            _pty.OutputReceived += OnOutput;
            _pty.Exited += OnExited;
        }

        private void OnOutput(byte[] chunk)
        {
            lock (_sync)
            {
                _history.AddLast(chunk);
                _historyBytes += chunk.Length;
                while (_historyBytes > MaxHistoryBytes && _history.Count > 1)
                {
                    _historyBytes -= _history.First.Value.Length;
                    _history.RemoveFirst();
                }

                // Subscribers only enqueue, so invoking under the lock keeps ordering
                // consistent with the history replayed to new subscribers.
                foreach (var subscriber in _subscribers) subscriber.OnOutput(chunk);
            }
        }

        private void OnExited(int exitCode)
        {
            lock (_sync)
            {
                _exitCode = exitCode;
                ExitedAt = DateTime.Now;
                foreach (var subscriber in _subscribers) subscriber.OnExit(exitCode);
                _subscribers.Clear();
            }
        }

        /// <summary>
        /// Replays the output history to <paramref name="onOutput"/>, then streams live output.
        /// Callbacks run on the pseudo console reader thread and must not block.
        /// </summary>
        public IDisposable Subscribe(Action<byte[]> onOutput, Action<int> onExit)
        {
            var subscriber = new Subscriber(this, onOutput, onExit);
            lock (_sync)
            {
                if (_historyBytes > 0)
                {
                    var replay = new byte[_historyBytes];
                    int offset = 0;
                    foreach (var chunk in _history)
                    {
                        Buffer.BlockCopy(chunk, 0, replay, offset, chunk.Length);
                        offset += chunk.Length;
                    }
                    onOutput(replay);
                }

                if (_exitCode.HasValue) onExit(_exitCode.Value);
                else _subscribers.Add(subscriber);
            }
            return subscriber;
        }

        private void Unsubscribe(Subscriber subscriber)
        {
            lock (_sync) _subscribers.Remove(subscriber);
        }

        /// <summary>Number of terminals currently streaming this session.</summary>
        public int AttachedCount { get { lock (_sync) return _subscribers.Count; } }

        public void Write(string text) { _pty.Write(text); }

        public void Resize(short cols, short rows) { _pty.Resize(cols, rows); }

        public void Kill() { _pty.Kill(); }

        public void Dispose() { _pty.Dispose(); }

        public Dictionary<string, object> ToDictionary()
        {
            lock (_sync)
            {
                return new Dictionary<string, object>
                {
                    ["sessionId"] = Id,
                    ["profile"] = Profile,
                    ["title"] = Title,
                    ["workingDirectory"] = WorkingDirectory,
                    ["instanceId"] = InstanceId,
                    ["pid"] = ProcessId,
                    ["running"] = !_exitCode.HasValue,
                    ["attached"] = _subscribers.Count,
                    ["exitCode"] = _exitCode,
                    ["createdAt"] = CreatedAt.ToString("o")
                };
            }
        }

        private sealed class Subscriber : IDisposable
        {
            private readonly TerminalSession _owner;
            public readonly Action<byte[]> OnOutput;
            public readonly Action<int> OnExit;

            public Subscriber(TerminalSession owner, Action<byte[]> onOutput, Action<int> onExit)
            {
                _owner = owner;
                OnOutput = onOutput;
                OnExit = onExit;
            }

            public void Dispose() { _owner.Unsubscribe(this); }
        }
    }

    /// <summary>
    /// Creates and tracks interactive terminal sessions for the resource server.
    /// Sessions run with the launcher's privileges and are reachable only through
    /// the loopback, same-origin endpoints in <see cref="ApiEndpoints.Terminal"/>.
    /// </summary>
    public sealed class TerminalSessionManager
    {
        private const int MaxRunningSessions = 16;
        private static readonly TimeSpan ExitedSessionRetention = TimeSpan.FromMinutes(10);

        // AI coding agent CLIs launched inside a shell: profile id, title, command name.
        // Only the ones found on this machine are offered as profiles.
        private static readonly string[][] AgentProfiles =
        {
            new[] { "claude", "Claude Code", "claude" },
            new[] { "codex", "Codex", "codex" },
            new[] { "copilot", "GitHub Copilot", "copilot" },
            new[] { "agy", "Antigravity CLI", "agy" },
            new[] { "grok", "Grok Build", "grok" },
            new[] { "gemini", "Gemini CLI", "gemini" },
            new[] { "qwen", "Qwen Code", "qwen" },
            new[] { "cursor-agent", "Cursor CLI", "cursor-agent" },
            new[] { "opencode", "opencode", "opencode" },
            new[] { "amp", "Amp", "amp" },
            new[] { "kiro-cli", "Kiro CLI", "kiro-cli" },
            new[] { "droid", "Factory Droid", "droid" },
            new[] { "crush", "Crush", "crush" },
            new[] { "goose", "Goose", "goose" },
            new[] { "aider", "Aider", "aider" }
        };

        private readonly ConcurrentDictionary<string, TerminalSession> _sessions = new ConcurrentDictionary<string, TerminalSession>();

        public IList<Dictionary<string, object>> Profiles()
        {
            var profiles = new List<Dictionary<string, object>>
            {
                Profile("powershell", "Windows PowerShell", true),
                Profile("pwsh", "PowerShell 7", FindExecutable("pwsh.exe") != null),
                Profile("cmd", "Command Prompt", true)
            };
            var searchDirectories = AgentSearchDirectories().ToList();
            foreach (var agent in AgentProfiles.Where(a => FindAgent(a[2], searchDirectories) != null))
                profiles.Add(Profile(agent[0], agent[1], true));
            return profiles;
        }

        private static Dictionary<string, object> Profile(string id, string title, bool available)
        {
            return new Dictionary<string, object> { ["id"] = id, ["title"] = title, ["available"] = available };
        }

        /// <param name="script">
        /// Editor content written to the new instance's bootstrap.js, as Run File does.
        /// Ignored when <paramref name="workingDirectory"/> is given or an instance is reused.
        /// </param>
        /// <param name="instanceId">
        /// An existing instance to share (all tabs of one terminal window use the same one).
        /// A new instance is deployed when it is omitted or its directory no longer exists.
        /// </param>
        public TerminalSession Create(string profile, short cols, short rows, string workingDirectory, string script = null, string instanceId = null)
        {
            string comspec = Environment.GetEnvironmentVariable("ComSpec");
            if (string.IsNullOrEmpty(comspec)) comspec = "cmd.exe";

            profile = string.IsNullOrWhiteSpace(profile) ? "powershell" : profile.Trim().ToLowerInvariant();
            var environment = CreateEnvironment();
            string commandLine;
            string title;

            switch (profile)
            {
                case "cmd":
                    commandLine = Quote(comspec);
                    title = "Command Prompt";
                    break;

                case "pwsh":
                    commandLine = "pwsh.exe -NoLogo";
                    title = "PowerShell 7";
                    break;

                case "powershell":
                    commandLine = "powershell.exe -NoLogo";
                    title = "Windows PowerShell";
                    break;

                default:
                    string[] agent = AgentProfiles.FirstOrDefault(a => a[0] == profile);
                    if (agent == null)
                        throw new ArgumentException($"Unknown terminal profile: {profile}");

                    // Keep a shell open after the agent exits so the session stays usable,
                    // and make the default install locations resolvable even when the
                    // launcher was started before the agent was installed.
                    environment["PATH"] = string.Join(";", AgentSearchDirectories()
                        .Concat(new[] { Environment.GetEnvironmentVariable("PATH") ?? "" }));
                    commandLine = Quote(comspec) + " /d /k " + agent[2];
                    title = agent[1];
                    break;
            }

            // Like Run File, a terminal opened without an explicit directory gets its own
            // isolated per-GUID instance and works inside it.
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                instanceId = null;
                workingDirectory = ResolveWorkingDirectory(workingDirectory);
            }
            else if (!string.IsNullOrWhiteSpace(instanceId) && TryGetInstanceWorkingDirectory(instanceId, out string sharedDirectory))
            {
                workingDirectory = sharedDirectory;
            }
            else
            {
                instanceId = Guid.NewGuid().ToString();
                workingDirectory = CreateInstanceWorkingDirectory(instanceId, script);
                Program.RecordFirstDeployTime(workingDirectory, instanceId);
            }

            TerminalSession session = Create(profile, title, commandLine, workingDirectory, environment, cols, rows);
            session.InstanceId = instanceId;
            return session;
        }

        private static bool TryGetInstanceWorkingDirectory(string instanceId, out string workingDirectory)
        {
            workingDirectory = null;

            // Only a GUID can name an instance; this also keeps the id from escaping the
            // instances directory.
            Guid parsed;
            if (!Guid.TryParse(instanceId, out parsed)) return false;

            string directory = Program.GetWorkingDirectory(parsed.ToString());
            if (!Directory.Exists(directory)) return false;

            workingDirectory = directory;
            return true;
        }

        /// <summary>
        /// Creates the isolated per-GUID instance directory used by deployed instances,
        /// deploys the application base files and optional data files into it, and
        /// writes <paramref name="script"/> (the editor content) as its bootstrap.js.
        /// </summary>
        public static string CreateInstanceWorkingDirectory(string instanceId, string script)
        {
            string workingDirectory = Program.CreateInstanceDirectory(instanceId);
            string appRoot = Program.GetAppRootDirectory();

            Program.DeployBaseFiles(appRoot, workingDirectory);
            Program.DeployOptionalDataFiles(appRoot, workingDirectory);
            if (!string.IsNullOrWhiteSpace(script))
            {
                File.WriteAllText(
                    Path.Combine(workingDirectory, "bootstrap.js"),
                    script,
                    new UTF8Encoding(false));
            }
            return workingDirectory;
        }

        public TerminalSession Create(string profile, string title, string commandLine, string workingDirectory, IDictionary<string, string> environment, short cols, short rows)
        {
            PurgeExited();
            if (_sessions.Values.Count(s => s.IsRunning) >= MaxRunningSessions)
                throw new InvalidOperationException($"Too many running terminal sessions (limit: {MaxRunningSessions}). Close one and try again.");

            var session = new TerminalSession(profile, title, commandLine, workingDirectory, environment ?? CreateEnvironment(), cols, rows);
            _sessions[session.Id] = session;
            return session;
        }

        public TerminalSession Get(string id)
        {
            TerminalSession session;
            return id != null && _sessions.TryGetValue(id, out session) ? session : null;
        }

        public IList<Dictionary<string, object>> List()
        {
            PurgeExited();
            return _sessions.Values.OrderBy(s => s.CreatedAt).Select(s => s.ToDictionary()).ToList();
        }

        public bool Close(string id)
        {
            TerminalSession session;
            if (id == null || !_sessions.TryRemove(id, out session)) return false;
            session.Dispose();
            return true;
        }

        public void Shutdown()
        {
            foreach (var id in _sessions.Keys.ToList()) Close(id);
        }

        private void PurgeExited()
        {
            DateTime threshold = DateTime.Now - ExitedSessionRetention;
            foreach (var session in _sessions.Values.Where(s => !s.IsRunning && s.ExitedAt < threshold).ToList())
                Close(session.Id);
        }

        public static Dictionary<string, string> CreateEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["COLORTERM"] = "truecolor",
                ["WELSONJS_TERMINAL"] = "1"
            };
        }

        private static string ResolveWorkingDirectory(string requested)
        {
            string expanded = Environment.ExpandEnvironmentVariables(requested.Trim());
            if (!Directory.Exists(expanded))
                throw new DirectoryNotFoundException($"Working directory not found: {expanded}");
            return Path.GetFullPath(expanded);
        }

        private static IEnumerable<string> AgentSearchDirectories()
        {
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return new[]
            {
                Path.Combine(profile, ".local", "bin"),                      // Claude Code native installer
                Path.Combine(appData, "npm"),                                // npm install -g
                Path.Combine(localAppData, "Programs", "OpenAI", "Codex", "bin"), // Codex installer
                Path.Combine(localAppData, "agy", "bin"),                    // Antigravity CLI installer
                Path.Combine(profile, ".grok", "bin"),                       // Grok Build installer
                Path.Combine(profile, ".opencode", "bin"),                   // opencode installer
                Path.Combine(profile, ".bun", "bin"),                        // bun install -g
                Path.Combine(profile, "scoop", "shims"),                     // scoop install
                Path.Combine(localAppData, "Microsoft", "WinGet", "Links")   // winget install
            }.Where(Directory.Exists);
        }

        private static string FindAgent(string command, IEnumerable<string> searchDirectories)
        {
            foreach (string extension in new[] { ".exe", ".cmd", ".bat" })
            {
                string found = FindExecutable(command + extension, searchDirectories);
                if (found != null) return found;
            }
            return null;
        }

        private static string FindExecutable(string fileName, IEnumerable<string> extraDirectories = null)
        {
            var directories = (Environment.GetEnvironmentVariable("PATH") ?? "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Concat(extraDirectories ?? Enumerable.Empty<string>());

            foreach (string directory in directories)
            {
                try
                {
                    string candidate = Path.Combine(directory.Trim().Trim('"'), fileName);
                    if (File.Exists(candidate)) return candidate;
                }
                catch (ArgumentException) { }
            }
            return null;
        }

        private static string Quote(string path)
        {
            return path.IndexOf(' ') >= 0 ? "\"" + path + "\"" : path;
        }
    }
}
