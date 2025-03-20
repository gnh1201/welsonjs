using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.Tools
{
    public class ExecutablesCollector
    {
        private List<string> executables = new List<string>();

        public ExecutablesCollector()
        {
            new Task(() =>
            {
                executables.AddRange(GetInstalledSoftwareExecutables());
                executables.AddRange(GetExecutablesFromPath());
                executables.AddRange(GetExecutablesFromNetFx());
            }).Start();
        }

        public List<string> GetExecutables()
        {
            return executables;
        }

        private List<string> GetInstalledSoftwareExecutables()
        {
            List<string> executables = new List<string>();
            string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey))
            {
                if (key != null)
                {
                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                        {
                            string installLocation = subKey?.GetValue("InstallLocation") as string;
                            string uninstallString = subKey?.GetValue("UninstallString") as string;

                            List<string> executablePaths = FindExecutables(installLocation, uninstallString);
                            executables.AddRange(executablePaths);
                        }
                    }
                }
            }

            return executables;
        }

        private List<string> FindExecutables(string installLocation, string uninstallString)
        {
            List<string> executables = new List<string>();

            if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
            {
                try
                {
                    List<string> executableFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.AllDirectories)
                                            .OrderByDescending(f => new FileInfo(f).Length)
                                            .ToList();
                    executables.AddRange(executableFiles);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error enumerating executables in '{installLocation}': {ex}");
                }
            }

            if (!string.IsNullOrEmpty(uninstallString))
            {
                if (TryParseExecutablePath(uninstallString, out string executablePath))
                {
                    executables.Add(executablePath);
                }
            }

            return executables;
        }

        private static bool TryParseExecutablePath(string s, out string path)
        {
            Match match = Regex.Match(s, @"(?<=""|^)([a-zA-Z]:\\[^""]+\.exe)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                path = match.Value;
                return true;
            }

            path = null;
            return false;
        }

        private List<string> GetExecutablesFromPath()
        {
            List<string> executables = new List<string>();
            string[] paths = Environment.GetEnvironmentVariable("PATH")?.Split(';');

            if (paths != null)
            {
                foreach (string path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        try
                        {
                            executables.AddRange(Directory.GetFiles(path, "*.exe", SearchOption.TopDirectoryOnly));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error enumerating executables in '{path}': {ex}");
                        }
                    }
                }
            }

            return executables;
        }

        private List<string> GetExecutablesFromNetFx()
        {
            List<string> executables = new List<string>();

            string windir = Environment.GetEnvironmentVariable("WINDIR");

            if (!string.IsNullOrEmpty(windir))
            {
                string[] paths = new string[]
                {
                    Path.Combine(windir, "Microsoft.NET", "Framework"),
                    Path.Combine(windir, "Microsoft.NET", "Framework64")
                };

                foreach (string path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        try
                        {
                            executables.AddRange(Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error enumerating executables in '{path}': {ex}");
                        }
                    }
                }
            }

            return executables;
        }
    }
}
