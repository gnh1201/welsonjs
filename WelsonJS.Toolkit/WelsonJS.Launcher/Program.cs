// Program.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Configuration;

namespace WelsonJS.Launcher
{
    internal static class Program
    {
        static Mutex mutex;
        public static ResourceServer _ResourceServer;

        [STAThread]
        static void Main()
        {
            // create the mutex
            mutex = new Mutex(true, "WelsonJS.Launcher", out bool isMutexNotExists);
            if (!isMutexNotExists)
            {
                MessageBox.Show("WelsonJS Launcher already running.");
                return;
            }

            // draw the main form
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

            // destory the mutex
            mutex.ReleaseMutex();
            mutex.Dispose();
        }

        public static void RunCommandPrompt(string workingDirectory, string entryFileName, string scriptName, bool isConsoleApplication = false, bool isInteractiveServiceAapplication = false)
        {
            if (!isConsoleApplication)
            {
                if (!File.Exists(Path.Combine(workingDirectory, entryFileName)))
                {
                    throw new Exception("Not Found: " + entryFileName);
                }
            }
            else
            {
                if (!Directory.EnumerateFiles(workingDirectory, scriptName + ".*").Any())
                {
                    throw new Exception("Not found matches file: " + scriptName);
                }
            }

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo("cmd")
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    Arguments = "/k",
                }
            };
            process.Start();

            process.StandardInput.WriteLine("pushd " + workingDirectory);
            process.StandardInput.WriteLine();
            process.StandardInput.Flush();
            process.StandardOutput.ReadLine();

            if (isInteractiveServiceAapplication)
            {
                process.StandardInput.WriteLine($"start cmd /c startInteractiveService.bat");
                process.StandardInput.WriteLine();
                process.StandardInput.Flush();
                process.StandardOutput.ReadLine();
            }
            else if (!isConsoleApplication)
            {
                process.StandardInput.WriteLine(entryFileName);
                process.StandardInput.WriteLine();
                process.StandardInput.Flush();
                process.StandardOutput.ReadLine();
            }
            else
            {
                process.StandardInput.WriteLine($"start cmd /c cscript app.js {scriptName}");
                process.StandardInput.WriteLine();
                process.StandardInput.Flush();
                process.StandardOutput.ReadLine();
            }
            process.StandardInput.Close();
            process.WaitForExit();
        }

        public static string GetFinalDirectory(string path)
        {
            string[] directories = Directory.GetDirectories(path);

            while (directories.Length == 1)
            {
                path = directories[0];
                directories = Directory.GetDirectories(path);
            }

            return path;
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

        public static string GetWorkingDirectory(string instanceId, bool followSubDirectory = false)
        {
            string workingDirectory = Path.Combine(GetAppDataPath(), instanceId);

            if (followSubDirectory)
            {
                if (!Directory.Exists(workingDirectory))
                {
                    workingDirectory = Path.Combine(Path.GetTempPath(), instanceId);
                }

                workingDirectory = GetFinalDirectory(workingDirectory);
            }

            return workingDirectory;
        }

        public static void InitResourceServer()
        {
            lock(typeof(Program))
            {
                if (_ResourceServer == null)
                {
                    _ResourceServer = new ResourceServer(GetAppConfig("ResourceServerPrefix"), "editor.html");
                }
            }
        }
        public static void OpenWebBrowser(string url)
        {
            Uri resourceServerUri = new Uri(GetAppConfig("ResourceServerPrefix"));
            Uri devToolsUri = new Uri(GetAppConfig("ChromiumDevToolsPrefix"));

            string userDataDir = Path.Combine(GetAppDataPath(), "EdgeUserProfile");
            string remoteAllowOrigins = $"{resourceServerUri.Scheme}://{resourceServerUri.Host}:{resourceServerUri.Port}";
            int remoteDebuggingPort = devToolsUri.Port;
            string[] arguments = {
                $"\"{url}\"",
                $"--remote-debugging-port={remoteDebuggingPort}",
                $"--remote-allow-origins={remoteAllowOrigins}",  // for security reason
                $"--user-data-dir=\"{userDataDir}\""
            };

            Process.Start(new ProcessStartInfo
            {
                FileName = Program.GetAppConfig("ChromiumFileName"),
                Arguments = string.Join(" ", arguments),
                UseShellExecute = true
            });
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
