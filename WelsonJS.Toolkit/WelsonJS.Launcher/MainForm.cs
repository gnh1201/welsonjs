using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace WelsonJS.Launcher
{
    public partial class MainForm : Form
    {
        private string workingDirectory;
        private string appName;
        private readonly string entrypointFileName = "bootstrap.bat";
        private string scriptName;

        public MainForm()
        {
            InitializeComponent();
        }

        private void EnableUI()
        {
            label1.Text = "Choose the location of WelsonJS application package.";
            button1.Enabled = true;
            button2.Enabled = true;
            checkBox1.Enabled = true;
            if (checkBox1.Checked)
            {
                textBox1.Enabled = true;
            }
        }

        private void DisableUI()
        {
            label1.Text = "Please wait...";
            button1.Enabled = false;
            button2.Enabled = false;
            checkBox1.Enabled = false;
            textBox1.Enabled = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ShowMessageBox("Comming soon...!");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string filePath = OpenFileDialog();
            if (filePath != null)
            {
                string fileExtension = Path.GetExtension(filePath);
                if (fileExtension != ".zip")
                {
                    ShowMessageBox("It doesn't seems to a ZIP file.");
                }
                else
                {
                    ExtractAndRun(filePath);
                }
            }
        }

        private void ExtractAndRun(string filePath)
        {
            appName = Path.GetFileNameWithoutExtension(filePath);
            workingDirectory = Path.Combine(Path.GetTempPath(), appName);
            scriptName = textBox1.Text;

            Task.Run(() =>
            {
                try
                {
                    // If exists, delete all
                    if (Directory.Exists(workingDirectory))
                    {
                        Directory.Delete(workingDirectory, true);
                    }

                    // try to extact ZIP file
                    ZipFile.ExtractToDirectory(filePath, workingDirectory);

                    // If it is created the sub-directory
                    workingDirectory = GetFinalDirectory(workingDirectory);

                    // Run the appliction
                    RunCommandPrompt();
                }
                catch (Exception ex)
                {
                    ShowMessageBox(ex.Message);
                }

                // Enable UI
                label1.Invoke((MethodInvoker)delegate {
                    EnableUI();
                });
            });

            DisableUI();
        }

        private void RunCommandPrompt()
        {
            bool isConsoleApplication = checkBox1.Checked;
            bool isInteractiveServiceAapplication = checkBox2.Checked;

            if (!isConsoleApplication)
            {
                if (!File.Exists(Path.Combine(workingDirectory, entrypointFileName)))
                {
                    throw new Exception("Not Found: " + entrypointFileName);
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
                process.StandardInput.WriteLine(entrypointFileName);
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

        private string OpenFileDialog()
        {
            string filePath = null;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                }
            }

            return filePath;
        }

        private void ShowMessageBox(string message)
        {
            MessageBox.Show(message);
        }

        private string GetFinalDirectory(string path)
        {
            string[] directories = Directory.GetDirectories(path);

            while (directories.Length == 1)
            {
                path = directories[0];
                directories = Directory.GetDirectories(path);
            }

            return path;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = checkBox1.Checked;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/gnh1201/welsonjs");
        }

        private void userdefinedVariablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var envForm = new EnvForm())
            {
                envForm.Show();
            }
        }
    }
}
