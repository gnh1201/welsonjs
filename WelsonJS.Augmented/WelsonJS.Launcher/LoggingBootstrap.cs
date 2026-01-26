using log4net;
using log4net.Config;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace WelsonJS.Launcher
{
    public static class LoggingBootstrap
    {
        /// <summary>
        /// Call once at process start, before any logging.
        /// - Pulls as much info as possible from the current environment.
        /// - Populates log4net GlobalContext.Properties for WelsonJS-style logging.
        /// </summary>
        public static void Init(string profile)
        {
            ConfigureLog4Net();
            PopulateGlobalContext(profile);

            var log = LogManager.GetLogger(typeof(LoggingBootstrap));
            log.Info("WelsonJS Launcher logging initialized");
        }

        private static void ConfigureLog4Net()
        {
            // Prefer external config file if present. Falls back to default configure().
            // Matches common pattern: <log4net configSource="log4net.config" /> in App.config,
            // but also supports standalone config without App.config.
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var cfgPath = Path.Combine(baseDir, "log4net.config");
                if (File.Exists(cfgPath))
                {
                    XmlConfigurator.ConfigureAndWatch(new FileInfo(cfgPath));
                    return;
                }
            }
            catch { }

            try { XmlConfigurator.Configure(); } catch { }
        }

        private static void PopulateGlobalContext(string profile)
        {
            var p = Process.GetCurrentProcess();

            // Core keys used by the recommended welsonjs log4net.config
            Set("pid", Safe(() => p.Id.ToString(), "0"));
            Set("host", DetectHostType());
            Set("user", GetUserName());
            Set("ver", GetAppVersion());
            Set("profile", StringOr(profile, "default"));

            // Extra useful environment context (optional but recommended)
            Set("machine", Safe(() => Environment.MachineName, "unknown"));
            Set("domain", Safe(() => Environment.UserDomainName, "unknown"));
            Set("interactive", Safe(() => Environment.UserInteractive ? "1" : "0", "0"));
            Set("elevated", IsElevated() ? "1" : "0");

            Set("os", GetOsLabel());
            Set("osVer", Safe(() => Environment.OSVersion.VersionString, "unknown"));
            Set("archProc", Safe(() => RuntimeInformation.ProcessArchitecture.ToString(), "unknown"));
            Set("archOS", Safe(() => RuntimeInformation.OSArchitecture.ToString(), "unknown"));
            Set("is64Proc", Safe(() => Environment.Is64BitProcess ? "1" : "0", "0"));
            Set("is64OS", Safe(() => Environment.Is64BitOperatingSystem ? "1" : "0", "0"));

            Set("runtime", GetRuntimeLabel());
            Set("culture", Safe(() => CultureInfo.CurrentCulture.Name, "unknown"));
            Set("uiCulture", Safe(() => CultureInfo.CurrentUICulture.Name, "unknown"));
            Set("tz", GetTimeZoneLabel());

            Set("exe", GetProcessPath());
            Set("cwd", Safe(() => Environment.CurrentDirectory, "unknown"));
            Set("cmd", Safe(() => Environment.CommandLine, "unknown"));

            Set("procName", Safe(() => p.ProcessName, "unknown"));
            Set("startUtc", Safe(() => p.StartTime.ToUniversalTime().ToString("o"), ""));
            Set("ws", Safe(() => p.WorkingSet64.ToString(), "0"));
        }

        private static void Set(string key, string value)
        {
            if (key == null) return;
            if (value == null) value = "";
            GlobalContext.Properties[key] = value;
        }

        private static string DetectHostType()
        {
            // Best-effort: service vs interactive launcher
            // (WSH-hosted scripts should still be launched by your Launcher, so "Launcher" is fine)
            try
            {
                if (!Environment.UserInteractive)
                    return "Service";
            }
            catch { }

            return "Launcher";
        }

        private static string GetUserName()
        {
            // Prefer WindowsIdentity (more precise, includes domain)
            try
            {
                var name = WindowsIdentity.GetCurrent()?.Name;
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch { }

            // Fallbacks
            try
            {
                var domain = Environment.UserDomainName;
                var user = Environment.UserName;
                if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(user))
                    return domain + "\\" + user;
                if (!string.IsNullOrEmpty(user)) return user;
            }
            catch { }

            return "unknown";
        }

        private static bool IsElevated()
        {
            try
            {
                using (var id = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(id);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch { }
            return false;
        }

        private static string GetAppVersion()
        {
            // Prefer informational version -> file version -> assembly version
            try
            {
                var asm = EntryAssemblyOrExecuting();
                var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrEmpty(info)) return info;

                var fileVer = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                if (!string.IsNullOrEmpty(fileVer)) return fileVer;

                var ver = asm.GetName().Version?.ToString();
                if (!string.IsNullOrEmpty(ver)) return ver;
            }
            catch { }

            // Fallback to FileVersionInfo of process
            try
            {
                var path = GetProcessPath();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var fvi = FileVersionInfo.GetVersionInfo(path);
                    if (!string.IsNullOrEmpty(fvi.ProductVersion)) return fvi.ProductVersion;
                    if (!string.IsNullOrEmpty(fvi.FileVersion)) return fvi.FileVersion;
                }
            }
            catch { }

            return "unknown";
        }

        private static string GetOsLabel()
        {
            try
            {
                // Example: "Windows 10.0.19045.0"
                return RuntimeInformation.OSDescription?.Trim() ?? "unknown";
            }
            catch { }
            return "unknown";
        }

        private static string GetRuntimeLabel()
        {
            try
            {
                // Example: ".NET 8.0.1" or ".NET Framework 4.8.1" etc.
                return RuntimeInformation.FrameworkDescription?.Trim() ?? "unknown";
            }
            catch { }
            return "unknown";
        }

        private static string GetTimeZoneLabel()
        {
            try
            {
                var tz = TimeZoneInfo.Local;
                // Example: "Korea Standard Time (+09:00)"
                return tz.Id + " (" + tz.BaseUtcOffset.ToString() + ")";
            }
            catch { }
            return "unknown";
        }

        private static string GetProcessPath()
        {
            // .NET 6+ has Environment.ProcessPath, but keep compatibility.
            try
            {
                var p = Process.GetCurrentProcess();
                return p.MainModule?.FileName ?? "";
            }
            catch { }

            try
            {
                // AppContext.BaseDirectory is a directory, not exe, but better than nothing
                return Assembly.GetEntryAssembly()?.Location ?? "";
            }
            catch { }

            return "";
        }

        private static Assembly EntryAssemblyOrExecuting()
        {
            return Assembly.GetEntryAssembly()
                ?? Assembly.GetExecutingAssembly();
        }

        private static string Safe(Func<string> f, string fallback)
        {
            try
            {
                var v = f();
                return v ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static string StringOr(string v, string fallback)
        {
            if (string.IsNullOrEmpty(v)) return fallback;
            return v;
        }
    }
}
