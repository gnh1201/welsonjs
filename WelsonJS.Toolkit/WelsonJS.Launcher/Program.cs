// Program.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    internal static class Program
    {
        private static readonly ICompatibleLogger _logger;

        public static Mutex _mutex;
        public static ResourceServer _resourceServer;
        public static string _dateTimeFormat;

        static Program()
        {
            // get the date time format
            _dateTimeFormat = GetAppConfig("DateTimeFormat");

            // set up logger
            _logger = new TraceLogger();

            // load native libraries
            string appDataSubDirectory = "WelsonJS";
           bool requireSigned = string.Equals(
               GetAppConfig("NativeRequireSigned"),
               "true",
               StringComparison.OrdinalIgnoreCase);

            NativeBootstrap.Init(
                dllNames: new[] { "ChakraCore.dll" },
                appDataSubdirectory: appDataSubDirectory,
                logger: _logger,
                requireSigned: requireSigned
            );
        }

        [STAThread]
        static void Main(string[] args)
        {
            // if set the target file path
            string targetFilePath = GetTargetFilePath(args);
            if (!string.IsNullOrEmpty(targetFilePath))
            {
                try {
                    HandleTargetFilePath(targetFilePath);
                }
                catch (Exception e)
                {
                    _logger.Error($"Initialization failed: {e}");
                }
                return;
            }

            // create the mutex
            _mutex = new Mutex(true, "WelsonJS.Launcher", out bool createdNew);
            if (!createdNew)
            {
                _logger.Info("WelsonJS Launcher already running.");
                return;
            }

            // draw the main form
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(_logger));

            // release the mutex
            try
            {
                _mutex.ReleaseMutex();
            }
            catch { /* ignore if not owned */ }
            _mutex.Dispose();
        }

        public static void RecordFirstDeployTime(string directory, string instanceId)
        {
            // get current time
            DateTime now = DateTime.Now;

            // record to the metadata database
            InstancesForm instancesForm = new InstancesForm();
            try
            {
                instancesForm.GetDatabaseInstance().Insert(new Dictionary<string, object>
                {
                    ["InstanceId"] = instanceId,
                    ["FirstDeployTime"] = now
                }, out _);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to record first deploy time: {ex.Message}");
            }
            instancesForm.Dispose();

            // record to the instance directory
            try
            {
                string filePath = Path.Combine(directory, ".welsonjs_first_deploy_time");
                string text = now.ToString(_dateTimeFormat);
                File.WriteAllText(filePath, text);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to record first deploy time: {ex.Message}");
            }
        }

        private static string GetTargetFilePath(string[] args)
        {
            if (args == null || args.Length == 0) return null;

            for (int i = 0; i < args.Length; i++)
            {
                string token = args[i];
                if (string.Equals(token, "--file", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, "/file", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        return args[i + 1];
                    }
                }

                if (token.StartsWith("--file=", StringComparison.OrdinalIgnoreCase))
                {
                    return token.Substring("--file=".Length).Trim('"');
                }
            }

            return null;
        }

        private static void HandleTargetFilePath(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath);

            if (String.IsNullOrEmpty(fileExtension))
            {
                throw new ArgumentException("The file extension is null or empty");
            }

            if (fileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException("Not implemented yet.");
            }

            if (fileExtension.Equals(".js", StringComparison.OrdinalIgnoreCase))
            {
                string instanceId = Guid.NewGuid().ToString();
                string workingDirectory = CreateInstanceDirectory(instanceId);

                string appRoot = GetAppRootDirectory();
                string appBaseSource = Path.Combine(appRoot, "app.js");
                if (!File.Exists(appBaseSource))
                {
                    throw new FileNotFoundException("app.js not found in application root.", appBaseSource);
                }

                string appBaseDestination = Path.Combine(workingDirectory, "app.js");
                File.Copy(appBaseSource, appBaseDestination, overwrite: true);

                string assetsSource = Path.Combine(appRoot, "app", "assets", "js");
                string assetsDestination = Path.Combine(workingDirectory, "app", "assets", "js");
                CopyDirectoryRecursive(assetsSource, assetsDestination);

                string libSource = Path.Combine(appRoot, "lib");
                string libDestination = Path.Combine(workingDirectory, "lib");
                CopyDirectoryRecursive(libSource, libDestination);

                string entrypointDestination = Path.Combine(workingDirectory, "bootstrap.js");
                File.Copy(filePath, entrypointDestination, overwrite: true);

                RecordFirstDeployTime(workingDirectory, instanceId);

                RunCommandPrompt(
                    workingDirectory: workingDirectory,
                    entryFileName: "app.js",
                    scriptName: "bootstrap",
                    isConsoleApplication: true,
                    isInteractiveServiceApplication: false
                );
                return;
            }

            throw new NotSupportedException($"Unsupported file type: {fileExtension}");
        }

        private static string GetAppRootDirectory()
        {
            string[] candidates = new[]
            {
                GetAppConfig("AppRootDirectory"),
                AppDomain.CurrentDomain.BaseDirectory,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WelsonJS"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WelsonJS"),
            };

            foreach (string dir in candidates)
            {
                if (string.IsNullOrEmpty(dir))
                    continue;

                string appJs = Path.Combine(dir, "app.js");
                if (File.Exists(appJs))
                    return dir;
            }

            throw new FileNotFoundException("Could not locate app.js in any known application root directory.");
        }

        private static string CreateInstanceDirectory(string instanceId)
        {
            string workingDirectory = GetWorkingDirectory(instanceId);

            try
            {
                // check if the working directory exists
                if (Directory.Exists(workingDirectory))
                {
                    throw new InvalidOperationException("GUID validation failed. Directory already exists.");
                }

                Directory.CreateDirectory(workingDirectory);
            }
            catch
            {
                throw new Exception("Instance Initialization failed");
            }

            return workingDirectory;
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException("Source directory not found: " + sourceDir);
            }

            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(sourceDir.Length).TrimStart(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                );

                string targetPath = Path.Combine(destDir, relativePath);
                string targetDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                File.Copy(file, targetPath, overwrite: true);
            }
        }

        public static void RunCommandPrompt(string workingDirectory, string entryFileName, string scriptName, bool isConsoleApplication = false, bool isInteractiveServiceApplication = false)
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

            StreamWriter input = process.StandardInput;
            StreamReader output = process.StandardOutput;

            input.WriteLine("pushd " + workingDirectory);
            input.WriteLine();
            input.Flush();
            output.ReadLine();

            if (isInteractiveServiceApplication)
            {
                input.WriteLine($"start cmd /c startInteractiveService.bat");
                input.WriteLine();
                input.Flush();
                output.ReadLine();
            }
            else if (!isConsoleApplication)
            {
                input.WriteLine(entryFileName);
                input.WriteLine();
                input.Flush();
                output.ReadLine();
            }
            else
            {
                input.WriteLine($"start cmd /c cscript app.js {scriptName}");
                input.WriteLine();
                input.Flush();
                output.ReadLine();
            }
            input.Close();

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

        public static void InitializeResourceServer()
        {
            lock(typeof(Program))
            {
                if (_resourceServer == null)
                {
                    _resourceServer = new ResourceServer(GetAppConfig("ResourceServerPrefix"), "editor.html", _logger);
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
            bool isAppMode = string.Equals(
                GetAppConfig("ChromiumAppMode"),
                "true",
                StringComparison.OrdinalIgnoreCase);
            string[] arguments = {
                isAppMode ? $"\"--app={url}\"" : $"\"{url}\"",
                $"--remote-debugging-port={remoteDebuggingPort}",
                $"--remote-allow-origins={remoteAllowOrigins}",  // for security reason
                $"--user-data-dir=\"{userDataDir}\""
            };

            Process.Start(new ProcessStartInfo
            {
                FileName = Program.GetAppConfig("ChromiumExecutablePath"),
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
