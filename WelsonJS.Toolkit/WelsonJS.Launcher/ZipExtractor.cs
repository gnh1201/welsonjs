using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    public class ZipExtractor
    {
        class Extractor
        {
            public string Name { get; set; }
            public string FileName { get; set; }
            public string Path { get; set; }
            public Func<string, string, string> ExtractCommand { get; set; }
            public bool UseCmd { get; set; } = false;
        }

        private readonly List<Extractor> AvailableExtractors;

        public ZipExtractor()
        {
            AvailableExtractors = new List<Extractor>{
                new Extractor
                {
                    Name = "7z",
                    FileName = "7z.exe",
                    ExtractCommand = (src, dest) => $"x \"{src}\" -o\"{dest}\" -y"
                },
                new Extractor
                {
                    Name = "WinRAR",
                    FileName = "rar.exe",
                    ExtractCommand = (src, dest) => $"x -o+ \"{src}\" \"{dest}\\\""
                },
                new Extractor
                {
                    Name = "PeaZip",
                    FileName = "peazip.exe",
                    ExtractCommand = (src, dest) => $"-ext2simple \"{src}\" \"{dest}\""
                },
                new Extractor
                {
                    Name = "WinZip",
                    FileName = "wzunzip.exe",
                    ExtractCommand = (src, dest) => $"-d \"{dest}\" \"{src}\""
                },
                new Extractor
                {
                    Name = "ALZip",
                    FileName = "ALZipcon.exe",
                    ExtractCommand = (src, dest) => $"-x \"{src}\" \"{dest}\""
                },
                new Extractor
                {
                    Name = "Bandizip",
                    FileName = "Bandizip.exe",
                    ExtractCommand = (src, dest) => $"x -o:\"{dest}\" \"{src}\" -y"
                },
                new Extractor
                {
                    Name = "jar (Java SDK)",
                    FileName = "jar.exe",
                    ExtractCommand = (src, dest) => $"jar xf \"{src}\"",
                    UseCmd = true
                },
                new Extractor
                {
                    Name = "tar (Windows)",  // Windows 10 build 17063 or later
                    FileName = "tar.exe",
                    ExtractCommand = (src, dest) => $"-xf \"{src}\" -C \"{dest}\""
                },
                new Extractor
                {
                    Name = "unzip",  // Info-ZIP, Cygwin
                    FileName = "unzip.exe",
                    ExtractCommand = (src, dest) => $"\"{src}\" -d \"{dest}\" -o"
                }
            };

            Task.Run(() => CheckAvailableExtractors());
        }

        public bool Extract(string filePath, string workingDirectory)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            if (!filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The specified file is not a ZIP archive.");

            if (!IsValidFile(filePath))
                throw new InvalidDataException("The specified file is not a valid ZIP archive.");

            Directory.CreateDirectory(workingDirectory);

            foreach (var extractor in AvailableExtractors.Where(e => e.Path != null))
            {
                if (RunProcess(extractor.Path, extractor, workingDirectory))
                    return true;
            }

            if (ExtractUsingPowerShell(filePath, workingDirectory))
                return true;

            if (ExtractUsingShell(filePath, workingDirectory))
                return true;

            return false;
        }

        private bool IsValidFile(string filePath)
        {
            byte[] signature = new byte[4];

            using (var fs = File.OpenRead(filePath))
            {
                if (fs.Length < 4)
                    return false;

                int bytesRead = fs.Read(signature, 0, 4);
                if (bytesRead < 4)
                    return false;
            }

            return signature.SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 });
        }

        private void CheckAvailableExtractors()
        {
            var fileNames = AvailableExtractors.Select(e => e.FileName).ToList();

            // Check PATH environment variable
            var paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
            foreach (var dir in paths)
            {
                foreach (var fileName in fileNames)
                {
                    var path = Path.Combine(dir.Trim(), fileName);
                    if (File.Exists(path))
                    {
                        var index = fileNames.IndexOf(fileName);
                        var extractor = AvailableExtractors[index];
                        extractor.Path = path;
                    }
                }
            }

            // Check common install locations
            var programDirs = new[]
            {
                Path.Combine(Program.GetAppDataPath(), "bin"),  // find an extractor from APPDATA directory
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            foreach (var rootDir in programDirs)
            {
                if (!Directory.Exists(rootDir))
                    continue;

                try
                {
                    foreach (var file in Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories))
                    {
                        var fileName = Path.GetFileName(file);
                        if (fileNames.Contains(fileName))
                        {
                            var index = fileNames.IndexOf(fileName);
                            var extractor = AvailableExtractors[index];
                            extractor.Path = file;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation($"Ignored file or directory: {ex.Message}");
                }
            }
        }

        private bool RunProcess(string executableFilePath, string arguments, string workingDirectory, bool useCmd = false)
        {
            var fileName = executableFilePath;
            var adjustedArguments = arguments;

            if (useCmd && !String.IsNullOrEmpty(workingDirectory))
            {
                fileName = "cmd.exe";
                adjustedArguments = $"/c cd /d \"{workingDirectory}\" && {arguments}";
            }

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = adjustedArguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = psi, EnableRaisingEvents = true })
            {
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }

        private bool RunProcess(string executableFilePath, Extractor extractor, string workingDirectory)
        {
            return RunProcess(executableFilePath,
                extractor.ExtractCommand(extractor.FileName, workingDirectory),
                workingDirectory,
                extractor.UseCmd
            );
        }

        private bool ExtractUsingPowerShell(string filePath, string workingDirectory)
        {
            var escapedSrc = filePath.Replace("'", "''");
            var escapedDest = workingDirectory.Replace("'", "''");
            var script = $"Expand-Archive -LiteralPath '{escapedSrc}' -DestinationPath '{escapedDest}' -Force";

            return RunProcess("powershell.exe", $"-NoProfile -Command \"{script}\"", workingDirectory);
        }

        private bool ExtractUsingShell(string filePath, string workingDirectory)
        {
            var shellAppType = Type.GetTypeFromProgID("Shell.Application");
            if (shellAppType == null)
                return false;

            dynamic shell = Activator.CreateInstance(shellAppType);
            if (shell == null)
                return false;

            dynamic zip = shell.NameSpace(filePath);
            dynamic dest = shell.NameSpace(workingDirectory);
            if (zip == null || dest == null)
                return false;

            int expected = zip.Items().Count;
            dest.CopyHere(zip.Items(), 16);
            
            // wait (max 30 s) until all files appear
            var sw = Stopwatch.StartNew();
            while (dest.Items().Count < expected && sw.Elapsed < TimeSpan.FromSeconds(30))
                System.Threading.Thread.Sleep(200);
            
            Marshal.ReleaseComObject(zip);
            Marshal.ReleaseComObject(dest);
            Marshal.ReleaseComObject(shell);
            
            return dest.Items().Count == expected;
        }
    }
}
