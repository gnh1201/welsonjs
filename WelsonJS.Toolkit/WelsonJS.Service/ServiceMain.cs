using System;
using System.ServiceProcess;
using System.Timers;
using MSScriptControl;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

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
            logFilePath = Path.Combine(Path.GetTempPath(), "WelsonJS.ServiceLog.txt");
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
                workingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), appName);
                Log("Working directory not provided. Using default value.");
            }
            /*
            Directory.SetCurrentDirectory(workingDirectory);

            // set script file path
            scriptFilePath = Path.Combine(workingDirectory, "app.js");

            // try load the script
            if (File.Exists(scriptFilePath))
            {
                scriptText = File.ReadAllText(scriptFilePath);
                scriptControl = new ScriptControl
                {
                    Language = "JScript",
                    AllowUI = false
                };
                scriptControl.AddCode(scriptText);
            }
            else
            {
                Log($"Script file not found: {scriptFilePath}");
            }

            // initialize
            InvokeScriptMethod("initializeService", scriptName, "start");
            */

            // set interval
            timer = new Timer();
            timer.Interval = 60000; // 1 minute
            timer.Elapsed += OnElapsedTime;
            timer.Start();
        }

        protected override void OnStop()
        {
            //InvokeScriptMethod("initializeService", scriptName, "stop");
            timer.Stop();
            //scriptControl.Reset();
            //scriptControl = null;
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            //InvokeScriptMethod("initializeService", scriptName, "elapsedTime");
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
