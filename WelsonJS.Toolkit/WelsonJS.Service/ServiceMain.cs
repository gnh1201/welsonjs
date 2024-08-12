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

namespace WelsonJS.Service
{
    public partial class ServiceMain : ServiceBase
    {
        private static List<Timer> timers;
        private string workingDirectory;
        private string scriptName;
        private string scriptFilePath;
        private string scriptText;
        private ScriptControl scriptControl;
        private readonly string logFilePath = Path.Combine(Path.GetTempPath(), "WelsonJS.Service.Log.txt");
        private readonly string appName = "WelsonJS";
        private string[] _args;
        private bool disabledScreenTime = false;
        private bool disabledFileMonitor = false;
        private ScreenMatching screenMatcher;
        private FileEventMonitor fileEventMonitor;

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);
        public static int SM_REMOTESESSION = 0x1000;

        public ServiceMain(string[] args)
        {
            InitializeComponent();

            // set service arguments
            _args = args;

            // mapping arguments to each variables
            var arguments = ParseArguments(_args);
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

                    case "disable-screen-time":
                        disabledScreenTime = true;
                        break;

                    case "disable-file-monitor":
                        disabledFileMonitor = true;
                        break;
                }
            }

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

            // set script name
            if (string.IsNullOrEmpty(scriptName))
            {
                scriptName = "defaultService";
                Log("Script name not provided. Using default value: " + scriptName);
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

            // Trace an event of file creation
            if (!disabledFileMonitor)
            {
                fileEventMonitor = new FileEventMonitor(this, workingDirectory);
                fileEventMonitor.Start();

                Log("File Event Monitor started.");
            }
            else
            {
                Log("Disabled the File Event Monitor (Sysinternals Sysmon based file event monitor)");
            }

            // check this session is the user interactive mode
            if (Environment.UserInteractive) {
                this.OnUserInteractiveEnvironment();
            }
            else
            {
                Log("Disabled the User Interactive Mode. (e.g., OnScreenTime)");
            }

            // set the log file path
            logFilePath = Path.Combine(Path.GetTempPath(), "WelsonJS.Service.Log.txt");
            Log(appName + " Service Loaded");
        }

        internal void TestStartupAndStop()
        {
            this.OnStart(_args);
            Console.ReadLine();
            this.OnStop();
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            // check the script file exists
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

                    // initialize
                    Log(DispatchServiceEvent("start"));
                }
                catch (Exception ex)
                {
                    Log("Exception when start: " + ex.Message);
                }
            }
            else
            {
                Log($"Script file not found: {scriptFilePath}");
            }

            timers.ForEach(timer => timer?.Start()); // start

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
                screenMatcher = new ScreenMatching(this, workingDirectory);

                Timer screenTimer = new Timer
                {
                    Interval = 5000 // 5 seconds
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
                List<ScreenMatchResult> matchedResults = screenMatcher.CaptureAndMatchAllScreens();
                matchedResults.ForEach(result =>
                {
                    if (result.MaxCorrelation > 0.0) {
                        Log(DispatchServiceEvent("screenTime", new string[]
                        {
                            result.FileName,
                            result.ScreenNumber.ToString(),
                            result.Location.X.ToString(),
                            result.Location.Y.ToString(),
                            result.MaxCorrelation.ToString()
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                Log("Exception when screen time: " + ex.ToString());
            }
        }

        private string InvokeScriptMethod(string methodName, params object[] parameters)
        {
            if (scriptControl != null)
            {
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

        public string DispatchServiceEvent(string eventType, string[] args = null)
        {
            if (args == null)
            {
                return InvokeScriptMethod("dispatchServiceEvent", scriptName, eventType, "");
            }
            else
            {
                return InvokeScriptMethod("dispatchServiceEvent", scriptName, eventType, String.Join("; ", args));
            }

        }

        public void Log(string message)
        {
            string _message = $"{DateTime.Now}: {message}";

            if (Environment.UserInteractive)
            {
                Console.WriteLine(_message);
            }

            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine(_message);
            }
        }
    }
}
