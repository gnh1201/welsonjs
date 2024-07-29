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
        private Timer timer;
        private string workingDirectory;
        private string scriptFilePath;
        private string scriptText;
        private string scriptName;
        private ScriptControl scriptControl;
        private string logFilePath;
        private readonly string appName = "WelsonJS";

        public ServiceMain()
        {
            InitializeComponent();

            // set the log file path
            logFilePath = Path.Combine(Path.GetTempPath(), "WelsonJS.Service.Log.txt");
            Log(appName + " Service Loaded");
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
                    Log(DispatchServiceEvent(scriptName, "start"));
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

            // set interval
            timer = new Timer();
            timer.Interval = 60000; // 1 minute
            timer.Elapsed += OnElapsedTime;
            timer.Start();

            Log(appName + " Service Started");
        }

        protected override void OnStop()
        {
            timer.Stop();

            try
            {
                Log(DispatchServiceEvent(scriptName, "stop"));
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
                Log(DispatchServiceEvent(scriptName, "elapsedTime"));
            }
            catch (Exception ex)
            {
                Log("Exception when elapsed time: " + ex.Message);
            }
        }

        private string DispatchServiceEvent(string name, string eventType)
        {
            return InvokeScriptMethod("dispatchServiceEvent", name, eventType);
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
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
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
                }
            }

            return arguments;
        }
    }
}
