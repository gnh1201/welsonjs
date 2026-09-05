// ServiceMain.cs
// SPDX-License-Identifier: MS-RL
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.ServiceProcess;
using System.Timers;
using System.Runtime.InteropServices;
using MSScriptControl;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using WelsonJS.ManagedObject;

namespace WelsonJS.Service
{
    public partial class ServiceMain : ServiceBase
    {
        private readonly string applicationName = "WelsonJS";
        private static List<Timer> timers;
        private ILogger logger;
        private string workingDirectory;
        private string scriptName;
        private string scriptFilePath;
        private string scriptText;
        private ScriptControl scriptControl;
        private string[] args;
        private bool disabledScreenTime = false;
        private bool disabledFileMonitor = false;
        private ScreenMatch screenMatcher;
        private FileEventMonitor fileEventMonitor;
        private ProfileStringFile settingsFileHandler;
        private UserVariables userVariablesHandler;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private static int SM_REMOTESESSION = 0x1000;

        public ServiceMain(string[] _args, ILogger _logger)
        {
            InitializeComponent();

            // set arguments and logger
            args = _args;
            logger = _logger;

            // get the program files directory
            var programFiles = Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFiles);

            // mapping arguments to each variables
            var arguments = ParseArguments(this.args);
            foreach (KeyValuePair<string, string> entry in arguments)
            {
                switch (entry.Key)
                {
                    case "working-directory":
                        // Temporary mitigation for GHSA-9jmm-5v6v-gpq2.
                        // Services must use the trusted installation directory instead of a fallback path.
                        // Additional path and integrity validation will be added in a future release.
                        workingDirectory = Environment.UserInteractive
                            ? entry.Value
                            : Path.Combine(programFiles, applicationName);
                        break;

                    case "script-name":
                        scriptName = entry.Value;
                        break;

                    case "disable-screen-time":
                        disabledScreenTime = true;
                        break;

                    case "disable-file-monitor":
                        disabledFileMonitor = true;
                        break;
                }
            }

            // load the user variables
            userVariablesHandler = new UserVariables(this);
            userVariablesHandler.Load();

            // set timers
            timers = new List<Timer>();

            // set working directory
            if (string.IsNullOrEmpty(workingDirectory))
            {
                if (Environment.UserInteractive)
                {
                    // Temporary mitigation for GHSA-9jmm-5v6v-gpq2.
                    // Allow the temporary directory only for interactive execution.
                    workingDirectory = Path.Combine(Path.GetTempPath(), applicationName);
                }
                else
                {
                    // Temporary mitigation for GHSA-9jmm-5v6v-gpq2.
                    // Services must never fall back to a user-writable temporary directory.
                    workingDirectory = Path.Combine(programFiles, applicationName);
                }

                logger.Info("Working directory not provided. Using default value: " + workingDirectory);

                if (!Directory.Exists(workingDirectory))
                {
                    Directory.CreateDirectory(workingDirectory);
                    logger.Info("Directory created: " + workingDirectory);
                }
            }
            Directory.SetCurrentDirectory(workingDirectory);

            // read settings.ini
            string settingsFilePath = Path.Combine(workingDirectory, "settings.ini");
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    settingsFileHandler = new ProfileStringFile(settingsFilePath);
                }
                catch (Exception ex)
                {
                    logger.Warn(ex.Message);
                }
            }
            else
            {
                logger.Info($"Configuration file not found: {settingsFilePath}");
            }

            // read configrations from settings.ini
            if (settingsFileHandler != null)
            {
                string[] configNames = new string[]
                {
                    "DISABLE_SCREEN_TIME",
                    "DISABLE_FILE_MONITOR"
                };
                foreach (string configName in configNames)
                {
                    try
                    {
                        if ("true" == ReadSettingsValue(configName))
                        {
                            switch (configName)
                            {
                                case "DISABLE_SCREEN_TIME":
                                    disabledScreenTime = true;
                                    break;

                                case "DISABLE_FILE_MONITOR":
                                    disabledFileMonitor = true;
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Info($"{configName} is ignored: {ex.Message}");
                    }
                }
            }

            // set script name
            if (string.IsNullOrEmpty(scriptName))
            {
                scriptName = "defaultService";
                logger.Info($"Script name not provided. Using default value: {scriptName}");
            }

            // set path of the script
            scriptFilePath = Path.Combine(workingDirectory, "app.js");

            // set default timer
            Timer defaultTimer = new Timer
            {
                Interval = 60000 // 1 minute
            };
            defaultTimer.Elapsed += OnElapsedTime;
            timers.Add(defaultTimer);

            // check this session is the user interactive mode
            if (Environment.UserInteractive) {
                OnUserInteractiveEnvironment();
            }
            else
            {
                logger.Info("Disabled the User Interactive Mode. (e.g., OnScreenTime)");
            }

            logger.Info(applicationName + " Service Loaded");
        }

        public string ReadSettingsValue(string key, string defaultValue = null)
        {
            if (settingsFileHandler != null)
            {
                return settingsFileHandler.Read(key, "Service") ?? defaultValue;
            }
            else
            {
                logger.Warn("Unable to read the value. It seems that settings.ini is not configured correctly.");
                return defaultValue;
            }
        }

        public UserVariables GetUserVariablesHandler()
        {
            return userVariablesHandler;
        }

        internal void TestStartupAndStop()
        {
            this.OnStart(this.args);
            Console.ReadLine();
            this.OnStop();
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            // Check exists the entry script file
            if (File.Exists(scriptFilePath))
            {
                logger.Info($"Script file found: {scriptFilePath}");

                try
                {
                    // load the script
                    scriptText = File.ReadAllText(scriptFilePath);
                    scriptControl = new ScriptControl
                    {
                        Language = "JScript",
                        AllowUI = false
                    };
                    scriptControl.Reset();
                    scriptControl.AddCode(scriptText);

                    // make the start arguments
                    string[] startArguments;
                    string[] _args;
                    if (Environment.UserInteractive)
                    {
                        _args = new string[]
                        {
                            $"--env-file={userVariablesHandler.GetEnvFilePath()}",
                            "--user-interactive"
                        };
                    }
                    else
                    {
                        _args = new string[]
                        {
                            $"--env-file={userVariablesHandler.GetEnvFilePath()}"
                        };
                    }
                    startArguments = new string[args.Length + _args.Length];
                    args.CopyTo(startArguments, 0);
                    for (int i = 0; i < _args.Length; i++)
                    {
                        startArguments[args.Length + i] = _args[i];
                    }

                    // initialize
                    logger.Info(DispatchServiceEvent("start", startArguments));
                }
                catch (Exception ex)
                {
                    logger.Info($"Failed to start because of {ex.Message}");
                }
            }
            else
            {
                logger.Info($"Script file not found: {scriptFilePath}");
            }

            // Trace a Sysmon file events (If Sysinternals Sysmon installed)
            if (!disabledFileMonitor)
            {
                fileEventMonitor = new FileEventMonitor(this, workingDirectory, logger);
                fileEventMonitor.Start();

                logger.Info("File Event Monitor Started");
            }
            else
            {
                logger.Info("File Event Monitor is Disabled");
            }

            // Start all the registered timers
            timers.ForEach(timer => timer?.Start());

            logger.Info(applicationName + " Service Started");
        }

        protected override void OnStop()
        {
            // stop timers
            timers.ForEach(timer => timer?.Stop());

            // stop the File Event Monitor
            fileEventMonitor?.Stop();

            // dispatch stop callback
            try
            {
                logger.Info(DispatchServiceEvent("stop"));
                scriptControl?.Reset();
            }
            catch (Exception ex)
            {
                logger.Info("Exception when stop: " + ex.Message);
            }
            scriptControl = null;

            logger.Info(applicationName + " Service Stopped");
        }

        private void OnUserInteractiveEnvironment()
        {
            // check is it a remote desktop session
            if (GetSystemMetrics(SM_REMOTESESSION) > 0)
            {
                disabledScreenTime = true;
                logger.Info("This application may not work correctly in a remote desktop session");
            }

            // set screen timer
            if (!disabledScreenTime)
            {
                screenMatcher = new ScreenMatch(this, workingDirectory, logger);

                Timer screenTimer = new Timer
                {
                    Interval = 1000 // 1 seconds
                };
                screenTimer.Elapsed += OnScreenTime;
                timers.Add(screenTimer);

                logger.Info("Screen Time Event Enabled");
            }
            else
            {
                disabledScreenTime = true;

                logger.Info("Screen Time Event Disabled");
            }
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                logger.Info(DispatchServiceEvent("elapsedTime"));
            }
            catch (Exception ex)
            {
                logger.Info("Exception when elapsed time: " + ex.Message);
            }
        }

        private void OnScreenTime(object source, ElapsedEventArgs e)
        {
            try
            {
                List<ScreenMatchResult> matchedResults = screenMatcher.CaptureAndMatch();
                matchedResults.ForEach(result =>
                {
                    logger.Info(DispatchServiceEvent("screenTemplateMatched", new string[]
                    {
                        result.FileName,
                        result.ScreenNumber.ToString(),
                        result.Position.X.ToString(),
                        result.Position.Y.ToString()
                    }));
                });
            }
            catch (Exception ex)
            {
                logger.Info($"Waiting a next screen time... {ex.Message}");
            }
        }

        private string InvokeScriptMethod(string methodName, string scriptName, string eventType, string[] args)
        {
            if (scriptControl != null)
            {
                object[] parameters = new object[] {
                    scriptName,
                    eventType,
                    new ArrayList(args),
                    args.Length
                };
                //scriptControl.AddObject("extern_arguments", new ArrayList(args), true);

                return scriptControl.Run(methodName, parameters)?.ToString() ?? "void";
            }
            else
            {
                logger.Info("InvokeScriptMethod Ignored: " + methodName);
            }

            return "void";
        }

        private Dictionary<string, string> ParseArguments(string[] args)
        {
            var arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string arg in args)
            {
                if (arg.StartsWith("--"))
                {
                    var index = arg.IndexOf('=');
                    if (index > 2)
                    {
                        var key = arg.Substring(2, index - 2);
                        var value = arg.Substring(index + 1).Trim('"');
                        arguments[key] = value;
                    }
                    else
                    {
                        var key = arg.Substring(2, index - 2);
                        arguments[key] = "";
                    }
                }
            }

            return arguments;
        }

        public ScreenMatch.TemplateInfo GetNextTemplateInfo()
        {
            string templateName = string.Empty;
            int index = 0;

            try
            {
                templateName = DispatchServiceEvent("screenNextTemplate");

                // Check if the received value contains an index
                string[] parts = templateName.Split(':');
                if (parts.Length > 1)
                {
                    templateName = parts[0];
                    int.TryParse(parts[1], out index);
                }
            }
            catch (Exception ex)
            {
                logger.Info($"Use all templates because of {ex.Message}");
            }

            return new ScreenMatch.TemplateInfo(templateName, index);
        }

        public string DispatchServiceEvent(string eventType, string[] args = null)
        {
            if (args == null)
            {
                return InvokeScriptMethod("dispatchServiceEvent", scriptName, eventType, new string[] { });
            }
            else
            {
                return InvokeScriptMethod("dispatchServiceEvent", scriptName, eventType, args);
            }

        }
    }
}
