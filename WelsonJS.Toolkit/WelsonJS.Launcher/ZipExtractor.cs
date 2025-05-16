using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace WelsonJS.Launcher
{
    public class ZipExtractor
    {
        class Extractor
        {
            public string Name;
            public string Path;
            public Func<string, string, string> ExtractCommand;
        }

        private readonly List<Extractor> AvailableExtractors;

        public ZipExtractor()
        {
            AvailableExtractors = new List<Extractor>{
                new Extractor
                {
                    Name = "7z",
                    Path = FindExecutable("7z.exe"),
                    ExtractCommand = (src, dest) => $"x \"{src}\" -o\"{dest}\" -y"
                },
                new Extractor
                {
                    Name = "WinRAR",
                    Path = FindExecutable("rar.exe"),
                    ExtractCommand = (src, dest) => $"x -o+ \"{src}\" \"{dest}\\\""
                },
                new Extractor
                {
                    Name = "PeaZip",
                    Path = FindExecutable("peazip.exe"),
                    ExtractCommand = (src, dest) => $"-ext2simple \"{src}\" \"{dest}\""
                },
                new Extractor
                {
                    Name = "tar (Windows)",
                    Path = FindExecutable("tar.exe"),
                    ExtractCommand = (src, dest) => $"-xf \"{src}\" -C \"{dest}\""
                },
                new Extractor
                {
                    Name = "WinZip",
                    Path = FindExecutable("wzunzip.exe"),
                    ExtractCommand = (src, dest) => $"-d \"{dest}\" \"{src}\""
                },
                new Extractor
                {
                    Name = "ALZip",
                    Path = FindExecutable("ALZipcon.exe"),
                    ExtractCommand = (src, dest) => $"-x \"{src}\" \"{dest}\""
                },
                new Extractor
                {
                    Name = "Bandizip",
                    Path = FindExecutable("Bandizip.exe"),
                    ExtractCommand = (src, dest) => $"x -o:\"{dest}\" \"{src}\" -y"
                }
            };
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
                if (RunProcess(extractor.Path, extractor.ExtractCommand(filePath, workingDirectory)))
                {
                    return true;
                }
            }

            return ExtractUsingShell(filePath, workingDirectory);
        }

        private bool IsValidFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File does not exist.", filePath);

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

        private string FindExecutable(string executableFileName)
        {
            var paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
            foreach (var dir in paths)
            {
                var fullpath = Path.Combine(dir.Trim(), executableFileName);
                if (File.Exists(fullpath))
                    return fullpath;
            }

            // Check common install locations
            var programDirs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Path.Combine(Program.GetAppDataPath(), "bin")  // find an extractor from APPDATA directory
            };

            foreach (var dir in programDirs)
            {
                try
                {
                    var found = Directory.EnumerateFiles(dir, executableFileName, SearchOption.AllDirectories).FirstOrDefault();
                    if (found != null)
                        return found;
                }
                catch { }
            }

            return null;
        }

        private bool RunProcess(string executableFilePath, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = executableFilePath,
                Arguments = arguments,
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

        private bool ExtractUsingShell(string filePath, string workingDirectory)
        {
            var shellAppType = Type.GetTypeFromProgID("Shell.Application");

            if (shellAppType == null)
                return false;

            dynamic shell = Activator.CreateInstance(shellAppType);
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
