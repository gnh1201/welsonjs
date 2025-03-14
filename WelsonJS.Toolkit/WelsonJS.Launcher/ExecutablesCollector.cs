using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WelsonJS.Launcher
{
    public class ExecutablesCollector
    {
        private List<string> executables = new List<string>();

        public ExecutablesCollector()
        {
            executables.AddRange(GetInstalledSoftwareExecutables());
            executables.AddRange(GetExecutablesFromPath());
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
                catch (Exception) { }
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
            Match match = Regex.Match(s, @"(?<=""|^)([a-zA-Z]:\\[^""\s]+\.exe)");

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
            string pathEnv = Environment.GetEnvironmentVariable("PATH");

            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (string path in pathEnv.Split(';'))
                {
                    if (Directory.Exists(path))
                    {
                        try
                        {
                            executables.AddRange(Directory.GetFiles(path, "*.exe", SearchOption.TopDirectoryOnly));
                        }
                        catch (Exception) { }
                    }
                }
            }

            return executables;
        }
    }
}
