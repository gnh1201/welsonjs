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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Catswords.Phantomizer;

namespace WelsonJS.Launcher
{
    internal static class Program
    {
        private static readonly TraceLogger _logger;

        public static Mutex _mutex;
        public static ResourceServer _resourceServer;
        public static string _dateTimeFormat;

        static Program()
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
                    appSignalApiKey, null, typeof(Program).Namespace ?? "WelsonJS.Launcher", appSignalApiPrefix));
            }

            // get the date time format
            _dateTimeFormat = GetAppConfig("DateTimeFormat");

            // set up logger
            _logger = new TraceLogger(typeof(Program));

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
                try
                {
                    HandleTargetFilePath(targetFilePath);
                }
                catch (Exception e)
                {
                    _logger.Error($"Initialization failed: {e}");
                }
                return;
            }

            // if use the stdio-jsonrpc2 forwarder
            if (HasArg(args, "--stdio-jsonrpc2"))
            {
                _logger.Info("Starting in the stdio-jsonrpc2 forwarder...");
                ProcessStdioJsonRpc2().GetAwaiter().GetResult();
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

        private static bool HasArg(string[] args, string key)
        {
            if (args == null)
                return false;

            for (int i = 0; i < args.Length; i++)
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return true;
            
            return false;
        }

        private async static Task ProcessStdioJsonRpc2()
        {
            string serverPrefix = GetAppConfig("ResourceServerPrefix");
            string endpoint = $"{serverPrefix}jsonrpc2";
            int timeout = int.TryParse(GetAppConfig("HttpClientTimeout"), out timeout) ? timeout : 300;

            var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };

            using (var stdin = Console.OpenStandardInput())
            using (var stdout = Console.OpenStandardOutput())
            {
                var buffer = new byte[8192];

                while (true)
                {
                    int read;
                    try
                    {
                        read = await stdin.ReadAsync(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("[stdio] stdin read failed", ex);
                        break;
                    }

                    if (read <= 0)
                    {
                        _logger.Info("[stdio] EOF received, exiting loop");
                        break;
                    }

                    byte[] payload = new byte[read];
                    Buffer.BlockCopy(buffer, 0, payload, 0, read);

                    _logger.Debug($"[stdio] recv {payload.Length} bytes");
                    _logger.Debug($"[stdio] payload preview: {SafePreviewUtf8(payload, 512)}");

                    try
                    {
                        using (var content = new ByteArrayContent(payload))
                        {
                            // Content-Type: application/json
                            content.Headers.ContentType =
                                new MediaTypeHeaderValue("application/json")
                                {
                                    CharSet = "utf-8"
                                };

                            _logger.Debug($"[http] POST {endpoint}");

                            using (var response = await http.PostAsync(endpoint, content))
                            {
                                _logger.Info(
                                    $"[http] status={(int)response.StatusCode} {response.ReasonPhrase}");

                                foreach (var h in response.Headers)
                                    _logger.Debug($"[http] H {h.Key}: {string.Join(", ", h.Value)}");

                                foreach (var h in response.Content.Headers)
                                    _logger.Debug($"[http] HC {h.Key}: {string.Join(", ", h.Value)}");

                                byte[] responseBytes =
                                    await response.Content.ReadAsByteArrayAsync();

                                _logger.Debug($"[http] body {responseBytes.Length} bytes");

                                _logger.Debug(
                                    $"[http] body preview: {SafePreviewUtf8(responseBytes, 2048)}");

                                await stdout.WriteAsync(responseBytes, 0, responseBytes.Length);
                                await stdout.FlushAsync();
                            }
                        }
                    }
                    catch (TaskCanceledException ex)
                    {
                        _logger.Error("[http] request timed out or canceled", ex);
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.Error("[http] request failed", ex);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("[http] unexpected error", ex);
                    }
                }
            }
        }

        private static string SafePreviewUtf8(byte[] bytes, int maxBytes)
        {
            if (bytes == null || bytes.Length == 0)
                return "(empty)";

            try
            {
                int len = Math.Min(bytes.Length, maxBytes);
                string s = Encoding.UTF8.GetString(bytes, 0, len);

                s = s.Replace("\r", "\\r").Replace("\n", "\\n");

                if (bytes.Length > maxBytes)
                    s += $"...(truncated, total {bytes.Length} bytes)";

                return s;
            }
            catch
            {
                return "(binary / non-utf8 response)";
            }
        }

        private static void InitializeAssemblyLoader()
        {
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
            if (args == null || args.Length == 0)
                return null;

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

        internal static void DeployBaseFiles(string appRoot, string workingDirectory)
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

        internal static void DeployOptionalDataFiles(string appRoot, string workingDirectory)
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

        internal static void DeployEntrypoint(string sourceJsPath, string workingDirectory)
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

        public static string GetAppRootDirectory()
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

        internal static string CreateInstanceDirectory(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                throw new ArgumentException("Instance ID is required.", nameof(instanceId));

            string workingDirectory = GetWorkingDirectory(instanceId);

            if (Directory.Exists(workingDirectory))
                throw new InvalidOperationException($"Instance directory already exists: {workingDirectory}");

            Directory.CreateDirectory(workingDirectory);

            if (!Directory.Exists(workingDirectory))
                throw new IOException($"Instance directory could not be created: {workingDirectory}");

            return workingDirectory;
        }

        public static void RunCommandPrompt(string workingDirectory, string entryFileName, string scriptName, bool isConsoleApplication = false, bool isInteractiveServiceApplication = false)
        {
            if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
                throw new DirectoryNotFoundException($"Working directory not found: {workingDirectory}");

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

            ProcessStartInfo startInfo;

            if (isInteractiveServiceApplication)
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c call " + QuoteCommandArgument(Path.Combine(workingDirectory, "startInteractiveService.bat")),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };
            }
            else if (!isConsoleApplication)
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c call " + QuoteCommandArgument(Path.Combine(workingDirectory, entryFileName)),
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = workingDirectory
                };
            }
            else
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = GetCscriptPath(),
                    Arguments = "//nologo app.js " + QuoteCommandArgument(scriptName),
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = workingDirectory
                };
            }

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new InvalidOperationException("Failed to start the instance process.");

                if (!isInteractiveServiceApplication)
                    process.WaitForExit();
            }
        }

        internal static string GetCscriptPath()
        {
            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows";
            string folder = Environment.Is64BitOperatingSystem ? "SysWOW64" : "System32";
            string path = Path.Combine(systemRoot, folder, "cscript.exe");
            if (!File.Exists(path))
                throw new FileNotFoundException("Windows Script Host was not found.", path);
            return path;
        }

        private static string QuoteCommandArgument(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return "\"" + value.Replace("\"", "\\\"") + "\"";
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
            string instanceDirectoryName = ".welsonjs_" + instanceId;
            string workingDirectory = Path.Combine(GetAppDataPath(), instanceDirectoryName);
            string legacyWorkingDirectory = Path.Combine(GetAppDataPath(), instanceId);

            if (followSubDirectory)
            {
                if (!Directory.Exists(workingDirectory))
                {
                    if (Directory.Exists(legacyWorkingDirectory))
                    {
                        workingDirectory = legacyWorkingDirectory;
                    }
                    else
                    {
                        workingDirectory = Path.Combine(Path.GetTempPath(), instanceDirectoryName);
                        if (!Directory.Exists(workingDirectory))
                            workingDirectory = Path.Combine(Path.GetTempPath(), instanceId);
                    }
                }

                workingDirectory = GetFinalDirectory(workingDirectory);
            }

            return workingDirectory;
        }

        public static void InitializeResourceServer()
        {
            lock (typeof(Program))
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
            string browserPath = ResolveChromiumExecutable();

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
                $"--remote-allow-origins={remoteAllowOrigins}",
                $"--user-data-dir=\"{userDataDir}\""
            };

            Process.Start(new ProcessStartInfo
            {
                FileName = browserPath,
                Arguments = string.Join(" ", arguments),
                UseShellExecute = true
            });
        }

        private static string ResolveChromiumExecutable()
        {
            var candidates = new List<string>
            {
                GetAppConfig("ChromiumExecutablePath"),
                "msedge.exe",
                "chrome.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome Beta", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome SxS", "Application", "chrome.exe")
            };

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                string resolved = FindExecutable(candidate);
                if (resolved != null)
                    return resolved;
            }

            throw new FileNotFoundException("Microsoft Edge or Google Chrome was not found. Install one of them to open the WelsonJS editor.");
        }

        private static string FindExecutable(string candidate)
        {
            if (Path.IsPathRooted(candidate))
                return File.Exists(candidate) ? candidate : null;

            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string directory in path.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory))
                    continue;

                string fullPath = Path.Combine(directory.Trim(), candidate);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            return null;
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
