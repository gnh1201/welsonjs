// WelsonJsInstanceProcessManager.cs
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Runs editor scripts in the same isolated per-GUID layout used by deployed instances.
    /// The script runs inside an interactive pseudo console session, so the browser
    /// terminal can both display its output and send keyboard input (e.g. WScript.StdIn).
    /// This deliberately does not use a resident stdio or JSON-RPC process.
    /// </summary>
    public sealed class WelsonJsInstanceProcessManager
    {
        private readonly object _sync = new object();
        private readonly TerminalSessionManager _sessions;
        private string _sessionId;
        private string _instanceId;
        private string _workingDirectory;

        public WelsonJsInstanceProcessManager(TerminalSessionManager sessions)
        {
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        }

        public Dictionary<string, object> Start(string script, short cols, short rows)
        {
            if (string.IsNullOrWhiteSpace(script))
                throw new ArgumentException("Script is required.", nameof(script));

            lock (_sync)
            {
                Stop();

                string instanceId = Guid.NewGuid().ToString();
                string workingDirectory = TerminalSessionManager.CreateInstanceWorkingDirectory(instanceId, script);
                Program.RecordFirstDeployTime(workingDirectory, instanceId);

                string cscript = Program.GetCscriptPath();
                string commandLine = (cscript.IndexOf(' ') >= 0 ? "\"" + cscript + "\"" : cscript) + " //nologo app.js bootstrap";

                TerminalSession session = _sessions.Create(
                    "welsonjs",
                    "WelsonJS instance " + instanceId,
                    commandLine,
                    workingDirectory,
                    TerminalSessionManager.CreateEnvironment(),
                    cols,
                    rows);

                session.InstanceId = instanceId;
                _sessionId = session.Id;
                _instanceId = instanceId;
                _workingDirectory = workingDirectory;

                return Status();
            }
        }

        public Dictionary<string, object> Status()
        {
            lock (_sync)
            {
                TerminalSession session = _sessions.Get(_sessionId);
                bool running = session != null && session.IsRunning;
                return new Dictionary<string, object>
                {
                    ["running"] = running,
                    ["pid"] = running ? (object)session.ProcessId : null,
                    ["sessionId"] = session?.Id,
                    ["instanceId"] = _instanceId,
                    ["workingDirectory"] = _workingDirectory
                };
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                if (_sessionId != null) _sessions.Close(_sessionId);
                _sessionId = null;
                _instanceId = null;
                _workingDirectory = null;
            }
        }
    }
}
