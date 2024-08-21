using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public partial class MainForm : Form
    {
        private string workingDirectory;
        private string appName;
        private bool waiting = false;

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
            waiting = true;

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
					
					waiting = false;
                }
                catch (Exception ex)
                {
                    ShowMessageBox(ex.Message);
                }
            });

            DisableUI();
            while (waiting)
            {
                Thread.Sleep(1000);
            }
            EnableUI();
        }

        private void RunCommandPrompt()
        {
            bool isConsoleApplication = checkBox1.Checked;

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo("cmd")
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true,
                    Arguments = "/k",
                }
            };
            process.Start();

            process.StandardInput.WriteLine("pushd " + workingDirectory);
            process.StandardInput.WriteLine();
            if (!isConsoleApplication)
            {
                process.StandardInput.WriteLine("bootstrap.bat");
                process.StandardInput.WriteLine();
            }
            else
            {
                process.StandardInput.WriteLine("start cmd /c cscript app.js " + textBox1.Text);
                process.StandardInput.WriteLine();
            }
            process.StandardInput.Flush();
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
    }
}
