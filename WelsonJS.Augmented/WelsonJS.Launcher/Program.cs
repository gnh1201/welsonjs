// Program.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    internal static class Program
    {
        private static readonly ILog _logger;

        public static Mutex _mutex;
        public static ResourceServer _resourceServer;
        public static string _dateTimeFormat;

        static Program()
        {
            // get the date time format
            _dateTimeFormat = GetAppConfig("DateTimeFormat");

            // set up logger
            LoggingBootstrap.Init("dev");
            _logger = LogManager.GetLogger(typeof(Program));

            // load external assemblies
            InitializeAssemblyLoader();
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

        private static void InitializeAssemblyLoader()
        {
            byte[] gzBytes = Properties.Resources.Phantomizer;

            byte[] dllBytes;
            using (var input = new MemoryStream(gzBytes))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gz.CopyTo(output);
                dllBytes = output.ToArray();
            }

            Assembly phantomAsm = Assembly.Load(dllBytes);
            Type loaderType = phantomAsm.GetType("Catswords.Phantomizer.AssemblyLoader", true);

            loaderType.GetProperty("BaseUrl")?.SetValue(null, GetAppConfig("AssemblyBaseUrl"));
            loaderType.GetProperty("LoaderNamespace")?.SetValue(null, typeof(Program).Namespace);
            loaderType.GetProperty("AppName")?.SetValue(null, "WelsonJS");
            loaderType.GetProperty("IntegrityUrl")?.SetValue(null, GetAppConfig("AssemblyIntegrityUrl"));
            // curl.exe integrity hash can be added here if needed
            // e.g., 23b24c6a2dc39dbfd83522968d99096fc6076130a6de7a489bc0380cce89143d (curl-8.17.0-win-x86-full.2025-11-09, Muldersoft)
            loaderType.GetMethod("AddIntegrityHash")?.Invoke(null, new object[] { GetAppConfig("IntegrityHashCurl") });
            loaderType.GetMethod("Register")?.Invoke(null, null);

            var loadNativeModulesMethod = loaderType.GetMethod(
                "LoadNativeModules",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(Version), typeof(string[]) },
                modifiers: null
            );

            if (loadNativeModulesMethod == null)
            {
                throw new InvalidOperationException("LoadNativeModules(string, Version, string[]) method not found.");
            }

            loadNativeModulesMethod.Invoke(null, new object[]
            {
                "ChakraCore",
                new Version(1, 13, 0, 0),
                new[] { "ChakraCore.dll" }
            });

            /*
            // Alternative way using direct type reference
            AssemblyLoader.BaseUrl = GetAppConfig("AssemblyBaseUrl");   // Configure CDN base URL
            AssemblyLoader.IntegrityUrl = GetAppConfig("AssemblyIntegrityUrl");   // (Optional) Set the integrity URL
            AssemblyLoader.LoaderNamespace = typeof(Program).Namespace;
            AssemblyLoader.AppName = "WelsonJS";
            // curl.exe integrity hash can be added here if needed
            // e.g., 23b24c6a2dc39dbfd83522968d99096fc6076130a6de7a489bc0380cce89143d (curl-8.17.0-win-x86-full.2025-11-09, Muldersoft)
            AssemblyLoader.AddIntegrityHash(GetAppConfig("IntegrityHashCurl"));
            AssemblyLoader.Register();

            AssemblyLoader.LoadNativeModules(
                "ChakraCore",
                new Version(1, 13, 0, 0),
                new[] { "ChakraCore.dll" }
            );
            */
        }

        public static void RecordFirstDeployTime(string directory, string instanceId)
        {
            // get current time
            DateTime now = DateTime.Now;

            // record to the metadata database
            using (InstancesForm instancesForm = new InstancesForm())
            {
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
            }

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
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath is null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Target file not found.", filePath);

            string ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext))
                throw new ArgumentException("The file extension is null or empty.", nameof(filePath));

            if (ext.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                throw new NotImplementedException("Not implemented yet.");

            if (!ext.Equals(".js", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException($"Unsupported file type: {ext}");

            string instanceId = Guid.NewGuid().ToString();
            string workingDirectory = CreateInstanceDirectory(instanceId);

            string appRoot = GetAppRootDirectory();
            if (string.IsNullOrWhiteSpace(appRoot) || !Directory.Exists(appRoot))
                throw new DirectoryNotFoundException($"Application root not found: {appRoot}");

            DeployBaseFiles(appRoot, workingDirectory);
            DeployOptionalDataFiles(appRoot, workingDirectory);
            DeployEntrypoint(filePath, workingDirectory);

            RecordFirstDeployTime(workingDirectory, instanceId);

            RunCommandPrompt(
                workingDirectory: workingDirectory,
                entryFileName: "app.js",
                scriptName: "bootstrap",
                isConsoleApplication: true,
                isInteractiveServiceApplication: false
            );
        }

        private static void DeployBaseFiles(string appRoot, string workingDirectory)
        {
            CopyFile(
                Path.Combine(appRoot, "app.js"),
                Path.Combine(workingDirectory, "app.js"),
                isRequired: true
            );

            CopyDirectoryRecursive(
                Path.Combine(appRoot, "app", "assets", "js"),
                Path.Combine(workingDirectory, "app", "assets", "js"),
                isRequired: true
            );

            CopyDirectoryRecursive(
                Path.Combine(appRoot, "lib"),
                Path.Combine(workingDirectory, "lib"),
                isRequired: true
            );
        }

        private static void DeployOptionalDataFiles(string appRoot, string workingDirectory)
        {
            CopyFile(
                Path.Combine(appRoot, "data", "apikey.json"),
                Path.Combine(workingDirectory, "data", "apikey.json"),
                isRequired: false
            );

            CopyFile(
                Path.Combine(appRoot, "data", "available_proxies.json"),
                Path.Combine(workingDirectory, "data", "available_proxies.json"),
                isRequired: false
            );

            CopyFile(
                Path.Combine(appRoot, "data", "filetypes.json"),
                Path.Combine(workingDirectory, "data", "filetypes.json"),
                isRequired: false
            );
        }

        private static void DeployEntrypoint(string sourceJsPath, string workingDirectory)
        {
            CopyFile(
                sourceJsPath,
                Path.Combine(workingDirectory, "bootstrap.js"),
                isRequired: true
            );
        }

        private static void CopyDirectoryRecursive(
            string sourceDir,
            string destinationDir,
            bool isRequired = false
        )
        {
            if (!Directory.Exists(sourceDir))
            {
                if (isRequired)
                    throw new DirectoryNotFoundException($"Required directory not found: {sourceDir}");
                return;
            }

            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectoryRecursive(subDir, destSubDir, isRequired);
            }
        }

        private static void CopyFile(string sourcePath, string destinationPath, bool isRequired)
        {
            if (!File.Exists(sourcePath))
            {
                if (isRequired)
                    throw new FileNotFoundException("Required file not found.", sourcePath);
                return;
            }

            string parent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            File.Copy(sourcePath, destinationPath, overwrite: true);
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
