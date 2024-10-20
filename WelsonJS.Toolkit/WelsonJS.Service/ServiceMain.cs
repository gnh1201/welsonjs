/*
 * WelsonJS.Service 
 * 
 *     filename:
 *         ServiceMain.cs
 * 
 *     description:
 *         WelsonJS - Build a Windows app on the Windows built-in JavaScript engine
 * 
 *     website:
 *         - https://github.com/gnh1201/welsonjs
 *         - https://catswords.social/@catswords_oss
 *         - https://teams.live.com/l/community/FEACHncAhq8ldnojAI
 * 
 *     author:
 *         Namhyeon Go <abuse@catswords.net>
 *
 *     license:
 *         GPLv3 or MS-RL(Microsoft Reciprocal License)
 * 
 *     references:
 *         - https://learn.microsoft.com/en-us/dotnet/framework/windows-services/how-to-debug-windows-service-applications
 *         - https://stackoverflow.com/questions/6490979/how-to-pass-parameters-to-windows-service
 *         - https://stackoverflow.com/questions/42812333/pass-an-argument-to-a-windows-service-at-automatic-startup
 *         - https://learn.microsoft.com/ko-kr/windows/win32/api/winuser/nf-winuser-getsystemmetrics
 */
using System;
using System.ServiceProcess;
using System.Timers;
using System.Runtime.InteropServices;
using MSScriptControl;
using System.IO;
using System.Collections.Generic;
using WelsonJS.TinyINIController;
using System.Collections;
using System.Threading.Tasks;

namespace WelsonJS.Service
{
    public partial class ServiceMain : ServiceBase
    {
        private readonly string appName = "WelsonJS";
        private static List<Timer> timers;
        private string workingDirectory;
        private string scriptName;
        private string scriptFilePath;
        private string scriptText;
        private ScriptControl scriptControl;
        private string logFilePath;
        private string[] args;
        private bool disabledHeartbeat = false;
        private bool disabledScreenTime = false;
        private bool disabledFileMonitor = false;
        private ScreenMatch screenMatcher;
        private FileEventMonitor fileEventMonitor;
        private IniFile settingsHandler;
        private UserVariables userVariablesHandler;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private static int SM_REMOTESESSION = 0x1000;

        public ServiceMain(string[] args)
        {
            InitializeComponent();

            // set service arguments
            this.args = args;

            // set the log file path
            logFilePath = Path.Combine(Path.GetTempPath(), "welsonjs_service.log");

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
                workingDirectory = Path.Combine(Path.GetTempPath(), appName);
                Log("Working directory not provided. Using default value: " + workingDirectory);

                if (!Directory.Exists(workingDirectory))
                {
                    Directory.CreateDirectory(workingDirectory);
                    Log("Directory created: " + workingDirectory);
                }
            }
            Directory.SetCurrentDirectory(workingDirectory);

            // read settings.ini
            string settingsFilePath = Path.Combine(workingDirectory, "settings.ini");
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    settingsHandler = new IniFile(settingsFilePath);
                }
                catch (Exception)
                {
                    settingsHandler = null;
                }
            }
            else
            {
                Log($"Configuration file not found: {settingsFilePath}");
            }

            // read configrations from settings.ini
            if (settingsHandler != null)
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
                        if ("true" == GetSettingsHandler().Read(configName, "Service"))
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
                        Log($"{configName} is ignored: {ex.Message}");
                    }
                }
            }

            // set script name
            if (string.IsNullOrEmpty(scriptName))
            {
                scriptName = "defaultService";
                Log($"Script name not provided. Using default value: {scriptName}");
            }

            // set path of the script
            scriptFilePath = Path.Combine(workingDirectory, "app.js");

            // start the heartbeat
            if (!disabledHeartbeat)
            {
                HeartbeatClient heartbeatClient = new HeartbeatClient(this);
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
                this.OnUserInteractiveEnvironment();
            }
            else
            {
                Log("Disabled the User Interactive Mode. (e.g., OnScreenTime)");
            }

            Log(appName + " Service Loaded");
        }

        public IniFile GetSettingsHandler()
        {
            return settingsHandler;
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
                Log($"Script file found: {scriptFilePath}");

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
                    Log(DispatchServiceEvent("start", startArguments));
                }
                catch (Exception ex)
                {
                    Log($"Failed to start because of {ex.Message}");
                }
            }
            else
            {
                Log($"Script file not found: {scriptFilePath}");
            }

            // Trace a Sysmon file events (If Sysinternals Sysmon installed)
            if (!disabledFileMonitor)
            {
                fileEventMonitor = new FileEventMonitor(this, workingDirectory);
                fileEventMonitor.Start();

                Log("File Event Monitor Started");
            }
            else
            {
                Log("File Event Monitor is Disabled");
            }

            // Start all the registered timers
            timers.ForEach(timer => timer?.Start()); 

            Log(appName + " Service Started");
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
                Log(DispatchServiceEvent("stop"));
                scriptControl?.Reset();
            }
            catch (Exception ex)
            {
                Log("Exception when stop: " + ex.Message);
            }
            scriptControl = null;

            Log(appName + " Service Stopped");
        }

        private void OnUserInteractiveEnvironment()
        {
            // check is it a remote desktop session
            if (GetSystemMetrics(SM_REMOTESESSION) > 0)
            {
                disabledScreenTime = true;
                Log("This application may not work correctly in a remote desktop session");
            }

            // set screen timer
            if (!disabledScreenTime)
            {
                screenMatcher = new ScreenMatch(this, workingDirectory);

                Timer screenTimer = new Timer
                {
                    Interval = 1000 // 1 seconds
                };
                screenTimer.Elapsed += OnScreenTime;
                timers.Add(screenTimer);

                Log("Screen Time Event Enabled");
            }
            else
            {
                disabledScreenTime = true;

                Log("Screen Time Event Disabled");
            }
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                Log(DispatchServiceEvent("elapsedTime"));
            }
            catch (Exception ex)
            {
                Log("Exception when elapsed time: " + ex.Message);
            }
        }

        private void OnScreenTime(object source, ElapsedEventArgs e)
        {
            try
            {
                List<ScreenMatchResult> matchedResults = screenMatcher.CaptureAndMatch();
                matchedResults.ForEach(result =>
                {
                    Log(DispatchServiceEvent("screenTemplateMatched", new string[]
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
                Log($"Waiting a next screen time... {ex.Message}");
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
                Log("InvokeScriptMethod Ignored: " + methodName);
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
                        var value = arg.Substring(index + 1);
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
                Log($"Use all templates because of {ex.Message}");
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

        public void Log(string message)
        {
            string _message = $"{DateTime.Now}: {message}";

            if (Environment.UserInteractive)
            {
                Console.WriteLine(_message);
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(_message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LOGGING FAILED: {ex.Message}");
            }
        }
    }
}
