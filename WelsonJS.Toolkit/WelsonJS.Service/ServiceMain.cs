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
        private bool disabledScreenTimer = false;
        private string scriptFilePath;
        private string scriptText;
        private ScriptControl scriptControl;
        private readonly string logFilePath = Path.Combine(Path.GetTempPath(), "WelsonJS.Service.Log.txt");
        private readonly string appName = "WelsonJS";

        public ServiceMain()
        {
            InitializeComponent();

            // set the log file path
            Log(appName + " Service Loaded");

            // set timers
            timers = new List<Timer>();

            // set default timer
            Timer defaultTimer = new Timer
            {
                Interval = 60000 // 1 minute
            };
            defaultTimer.Elapsed += OnElapsedTime;
            timers.Add(defaultTimer);

            // set screen timer
            if (!disabledScreenTimer)
            {
                timers.Add(new ScreenTimer
                {
                    Parent = this,
                    WorkingDirectory = workingDirectory,
                    Interval = 1000 // 1 second
                });
            }
        }

        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }

        protected override void OnStart(string[] args)
        {
            // mapping arguments to each variables
            var arguments = ParseArguments(args);
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

                    case "--disable-screen-timer":
                        disabledScreenTimer = true;
                        break;
                }
            }

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
            
            // Start timers
            timers.ForEach(timer => timer.Start());

            Log(appName + " Service Started");
        }

        protected override void OnStop()
        {
            // Stop timers
            timers.ForEach(timer => timer.Stop());

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
                        var key = arg.Substring(2);
                        arguments[key] = "";
                    }
                }
            }

            return arguments;
        }

        public void Log(string message)
        {
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }

        public string DispatchServiceEvent(string eventType, object[] args = null)
        {
            return InvokeScriptMethod("dispatchServiceEvent", scriptName, eventType, args);
        }

        public string GetWorkingDirectory()
        {
            return workingDirectory;
        }
    }
}
