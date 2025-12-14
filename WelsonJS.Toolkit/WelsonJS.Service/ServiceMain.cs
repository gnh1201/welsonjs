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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WelsonJS.Service
{
    public partial class ServiceMain : ServiceBase
    {
        private readonly static string applicationName = "WelsonJS";
        private static List<Timer> timers;
        private ILogger logger;
        private string workingDirectory;
        private string scriptName;
        private string scriptFilePath;
        private string scriptText;
        private ScriptControl scriptControl;
        private string[] args;
        private bool disabledHeartbeat = false;
        private bool disabledScreenTime = false;
        private bool disabledFileMonitor = false;
        private ScreenMatch screenMatcher;
        private FileEventMonitor fileEventMonitor;
        private IniFile settingsFileHandler;
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

            // mapping arguments to each variables
            var arguments = ParseArguments(this.args);
            foreach (KeyValuePair<string, string> entry in arguments)
            {
                switch (entry.Key)
                {
                    case "working-directory":
                        workingDirectory = entry.Value;
                        break;

                    case "script-name":
                        scriptName = entry.Value;
                        break;

                    case "disable-heartbeat":
                        disabledHeartbeat = true;
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
                workingDirectory = Path.Combine(Path.GetTempPath(), applicationName);
                logger.LogInformation("Working directory not provided. Using default value: " + workingDirectory);

                if (!Directory.Exists(workingDirectory))
                {
                    Directory.CreateDirectory(workingDirectory);
                    logger.LogInformation("Directory created: " + workingDirectory);
                }
            }
            Directory.SetCurrentDirectory(workingDirectory);

            // read settings.ini
            string settingsFilePath = Path.Combine(workingDirectory, "settings.ini");
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    settingsFileHandler = new IniFile(settingsFilePath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex.Message);
                }
            }
            else
            {
                logger.LogInformation($"Configuration file not found: {settingsFilePath}");
            }

            // read configrations from settings.ini
            if (settingsFileHandler != null)
            {
                string[] configNames = new string[]
                {
                    "DISABLE_HEARTBEAT",
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
                                case "DISABLE_HEARTBEAT":
                                    disabledHeartbeat = true;
                                    break;

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
                        logger.LogInformation($"{configName} is ignored: {ex.Message}");
                    }
                }
            }

            // set script name
            if (string.IsNullOrEmpty(scriptName))
            {
                scriptName = "defaultService";
                logger.LogInformation($"Script name not provided. Using default value: {scriptName}");
            }

            // set path of the script
            scriptFilePath = Path.Combine(workingDirectory, "app.js");

            // start the heartbeat
            if (!disabledHeartbeat)
            {
                HeartbeatClient heartbeatClient = new HeartbeatClient(this, logger);
                Task.Run(heartbeatClient.StartHeartbeatAsync);
                Task.Run(heartbeatClient.StartEventListenerAsync);
            }

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
                logger.LogInformation("Disabled the User Interactive Mode. (e.g., OnScreenTime)");
            }

            logger.LogInformation(applicationName + " Service Loaded");
        }

        public string ReadSettingsValue(string key, string defaultValue = null)
        {
            if (settingsFileHandler != null)
            {
                return settingsFileHandler.Read(key, "Service") ?? defaultValue;
            }
            else
            {
                logger.LogWarning("Unable to read the value. It seems that settings.ini is not configured correctly.");
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
                logger.LogInformation($"Script file found: {scriptFilePath}");

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
                    logger.LogInformation(DispatchServiceEvent("start", startArguments));
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"Failed to start because of {ex.Message}");
                }
            }
            else
            {
                logger.LogInformation($"Script file not found: {scriptFilePath}");
            }

            // Trace a Sysmon file events (If Sysinternals Sysmon installed)
            if (!disabledFileMonitor)
            {
                fileEventMonitor = new FileEventMonitor(this, workingDirectory, logger);
                fileEventMonitor.Start();

                logger.LogInformation("File Event Monitor Started");
            }
            else
            {
                logger.LogInformation("File Event Monitor is Disabled");
            }

            // Start all the registered timers
            timers.ForEach(timer => timer?.Start());

            logger.LogInformation(applicationName + " Service Started");
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
                logger.LogInformation(DispatchServiceEvent("stop"));
                scriptControl?.Reset();
            }
            catch (Exception ex)
            {
                logger.LogInformation("Exception when stop: " + ex.Message);
            }
            scriptControl = null;

            logger.LogInformation(applicationName + " Service Stopped");
        }

        private void OnUserInteractiveEnvironment()
        {
            // check is it a remote desktop session
            if (GetSystemMetrics(SM_REMOTESESSION) > 0)
            {
                disabledScreenTime = true;
                logger.LogInformation("This application may not work correctly in a remote desktop session");
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

                logger.LogInformation("Screen Time Event Enabled");
            }
            else
            {
                disabledScreenTime = true;

                logger.LogInformation("Screen Time Event Disabled");
            }
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                logger.LogInformation(DispatchServiceEvent("elapsedTime"));
            }
            catch (Exception ex)
            {
                logger.LogInformation("Exception when elapsed time: " + ex.Message);
            }
        }

        private void OnScreenTime(object source, ElapsedEventArgs e)
        {
            try
            {
                List<ScreenMatchResult> matchedResults = screenMatcher.CaptureAndMatch();
                matchedResults.ForEach(result =>
                {
                    logger.LogInformation(DispatchServiceEvent("screenTemplateMatched", new string[]
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
                logger.LogInformation($"Waiting a next screen time... {ex.Message}");
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
                logger.LogInformation("InvokeScriptMethod Ignored: " + methodName);
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
                logger.LogInformation($"Use all templates because of {ex.Message}");
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

/* References:
 * [1] MSDN - How to: Debug Windows Service Applications
 *     https://learn.microsoft.com/en-us/dotnet/framework/windows-services/how-to-debug-windows-service-applications
 * [2] StackOverflow - How to pass parameters to Windows Service?
 *     https://stackoverflow.com/questions/6490979/how-to-pass-parameters-to-windows-service
 * [3] StackOverflow - Pass an argument to a Windows Service at automatic startup
 *     https://stackoverflow.com/questions/42812333/pass-an-argument-to-a-windows-service-at-automatic-startup
 * [4] MSDN - GetSystemMetrics function (winuser.h)
 *     https://learn.microsoft.com/ko-kr/windows/win32/api/winuser/nf-winuser-getsystemmetrics
 */
