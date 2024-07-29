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
 */
using System;
using System.ServiceProcess;
using System.Timers;
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
        private ScreenMatching screenMatcher;

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

            // set screen timer
            if (!disabledScreenTime && Environment.UserInteractive) {
                screenMatcher = new ScreenMatching(workingDirectory);

                Timer screenTimer = new Timer
                {
                    Interval = 10000 // 10 seconds
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

            timers.ForEach(timer => timer.Start()); // start

            Log(appName + " Service Started");
        }

        protected override void OnStop()
        {
            timers.ForEach(timer => timer.Stop()); // stop

            try
            {
                Log(DispatchServiceEvent("stop"));
                if (scriptControl != null)
                {
                    scriptControl.Reset();
                }
            }
            catch (Exception ex)
            {
                Log("Exception when stop: " + ex.Message);
            }
            scriptControl = null;

            Log(appName + " Service Stopped");
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

        private string DispatchServiceEvent(string eventType, string[] args = null)
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


        private void Log(string message)
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
    }
}
