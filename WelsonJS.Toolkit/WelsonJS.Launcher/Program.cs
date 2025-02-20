using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
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
    }
}
