using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net.Http;

namespace WelsonJS.Launcher.ResourceTools
{
    public class Completion : IResourceTool
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private const string Prefix = "completion/";
        private List<string> DiscoverdExecutables = new List<string>();

        public Completion(ResourceServer server, HttpClient httpClient)
        {
            Server = server;
            _httpClient = httpClient;

            Task.Run(() => DiscoverFromInstalledSoftware());
            Task.Run(() => DiscoverFromPathVariable());
            Task.Run(() => DiscoverFromProgramDirectories());
        }

        public bool CanHandle(string path)
        {
            return path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            await Task.Delay(0);

            string word = path.Substring(Prefix.Length);

            try
            {
                CompletionItem[] completionItems = DiscoverdExecutables
                    .Where(exec => exec.IndexOf(word, 0, StringComparison.OrdinalIgnoreCase) > -1)
                    .Take(100) // Limit the number of results
                    .Select(exec => new CompletionItem
                    {
                        Label = Path.GetFileName(exec),
                        Kind = "Text",
                        Documentation = $"An executable file: {exec}",
                        InsertText = exec
                    })
                    .ToArray();

                XElement response = new XElement("suggestions",
                    completionItems.Select(item => new XElement("item",
                        new XElement("label", item.Label),
                        new XElement("kind", item.Kind),
                        new XElement("documentation", item.Documentation),
                        new XElement("insertText", item.InsertText)
                    ))
                );

                Server.ServeResource(context, response.ToString(), "application/xml");
            }
            catch (Exception ex)
            {
                Server.ServeResource(context, $"<error>Failed to try completion. {ex.Message}</error>", "application/xml", 500);
            }
        }

        private void DiscoverFromInstalledSoftware()
        {
            const string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                if (key == null) return;

                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    RegistryKey subKey = key.OpenSubKey(subKeyName);
                    if (subKey == null) continue;

                    using (subKey)
                    {
                        string installLocation = subKey.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(installLocation))
                        {
                            SearchAllExecutables(installLocation);
                        }

                        string uninstallString = subKey.GetValue("UninstallString") as string;
                        if (!string.IsNullOrEmpty(uninstallString))
                        {
                            var match = Regex.Match(uninstallString, @"(?<=""|^)([a-zA-Z]:\\[^""]+\.exe)", RegexOptions.IgnoreCase);
                            if (match.Success && File.Exists(match.Value))
                            {
                                DiscoverdExecutables.Add(match.Value);
                            }
                        }
                    }
                }
            }
        }

        private void DiscoverFromPathVariable() {
            var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(';')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p));

            foreach (string path in paths)
            {
                SearchAllExecutables(path, SearchOption.TopDirectoryOnly);
            }
        }

        private void DiscoverFromProgramDirectories()
        {
            string windir = Environment.GetEnvironmentVariable("WINDIR");
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var paths = new[]
            {
                // Default directory
                Path.Combine(Program.GetAppDataPath(), "bin"),

                // Standard program installation directories
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),

                // .NET Framework directories
                Path.Combine(windir, "Microsoft.NET", "Framework"),
                Path.Combine(windir, "Microsoft.NET", "Framework64"),

                // Chocolatey package directory
                Path.Combine(programData, "chocolatey", "lib"),

                // Scoop apps directory
                Path.Combine(userProfile, "scoop", "apps")
            };

            foreach (string path in paths)
            {
                SearchAllExecutables(path);
            }
        }

        private void SearchAllExecutables(string path, SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (!Directory.Exists(path))
            {
                Trace.TraceWarning("Directory does not exist: {0}", path);
                return;
            }

            try
            {
                var executableFiles = Directory.GetFiles(path, "*.exe", searchOption)
                                               .OrderByDescending(f => new FileInfo(f).Length)
                                               .ToList();

                DiscoverdExecutables.AddRange(executableFiles);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error enumerating executables in '{0}': {1}", path, ex.Message);
            }
        }
    }

    public class CompletionItem
    {
        public string Label { get; set; }
        public string Kind { get; set; }
        public string Documentation { get; set; }
        public string InsertText { get; set; }
    }
}
