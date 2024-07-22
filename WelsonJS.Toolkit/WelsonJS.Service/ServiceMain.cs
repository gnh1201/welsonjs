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
                    scriptControl.AddCode(scriptText);

                    // initialize
                    InvokeScriptMethod("initializeService", scriptName, "start");
                }
                catch (Exception ex)
                {
                    Log("Exception. " + ex.Message);
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
                InvokeScriptMethod("initializeService", scriptName, "stop");
                scriptControl.Reset();
            }
            catch (Exception ex)
            {
                Log("Exception. " + ex.Message);
            }
            scriptControl = null;

            Log(appName + " Service Stopped");
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                InvokeScriptMethod("initializeService", scriptName, "elapsedTime");
            }
            catch (Exception ex)
            {
                Log("Exception. " + ex.Message);
            }
        }

        private string InvokeScriptMethod(string methodName, params object[] parameters)
        {
            return scriptControl.Run(methodName, parameters).ToString();
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
