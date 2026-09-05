// Program.cs
// SPDX-License-Identifier: MS-RL
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace WelsonJS.Service
{
    internal static class Program
    {
        private static TraceLogger _logger;

        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        /// 
        static void Main(string[] args)
        {
            // Initialize file-based logging before external assembly resolution begins.
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Trace.Listeners.Add(new LogFileTraceListener(logDirectory));

            // Display a message box whenever Trace.TraceError() is called.
            Trace.Listeners.Add(new MessageBoxTraceListener());

            // AppSignal (Error Tracking & Performance Monitoring) integration
            // Free AppSingal plan for open-source projects: https://www.appsignal.com/open-source?utm_source=welsonjs
            string appSignalApiPrefix = GetAppConfig("AppSignalApiPrefix");
            string appSignalApiKey = GetAppConfig("AppSignalApiKey");
            if (!string.IsNullOrEmpty(appSignalApiKey))
            {
                Trace.Listeners.Add(new AppSignalTraceListener(
                    appSignalApiKey, null, typeof(Program).Namespace ?? "WelsonJS.Service", appSignalApiPrefix));
            }

            // set up logger
            _logger = new TraceLogger(typeof(Program));

            // create the service
            if (Environment.UserInteractive)
            {
                Console.WriteLine("WelsonJS Service Application (User Interactive Mode)");
                Console.WriteLine("https://github.com/gnh1201/welsonjs");
                Console.WriteLine();
                Console.WriteLine("Service is running...");

                ServiceMain svc = new ServiceMain(args, _logger);
                svc.TestStartupAndStop();
            }
            else
            {
                ServiceBase[] ServicesToRun = new ServiceBase[]
                {
                    new ServiceMain(args, _logger)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }

        public static string GetAppDataPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WelsonJS"
            );

            Directory.CreateDirectory(path);

            if (!Directory.Exists(path))
            {
                throw new IOException("Failed to create directory: " + path);
            }

            return path;
        }

        public static string GetAppConfig(string key)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            value = Properties.Resources.ResourceManager.GetString(key);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            return null;
        }
    }
}
