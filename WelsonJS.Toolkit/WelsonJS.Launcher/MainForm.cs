using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public partial class MainForm : Form
    {
        private string workingDirectory;
        private string instanceId;
        private string entryFileName;
        private string scriptName;

        public MainForm()
        {
            entryFileName = "bootstrap.bat";

            InitializeComponent();

            if (IsInAdministrator())
            {
                Text = Text + " (Administrator)";
            }

            notifyIcon1.DoubleClick += OnShow;
            openLauncherToolStripMenuItem.Click += OnShow;
            exitToolStripMenuItem.Click += OnExit;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                notifyIcon1.Visible = true;
            }
            base.OnFormClosing(e);
        }

        private void OnShow(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Focus();
            notifyIcon1.Visible = false;
        }

        private void OnExit(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        private void EnableUI()
        {
            label1.Text = "Choose the location of WelsonJS application package.";
            button1.Enabled = true;
            button2.Enabled = true;
            checkBox1.Enabled = true;
            checkBox2.Enabled = true;
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
            checkBox2.Enabled = false;
            textBox1.Enabled = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Comming soon...!");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string filePath = OpenFileDialog();
            if (filePath != null)
            {
                string fileExtension = Path.GetExtension(filePath);
                if (fileExtension != ".zip")
                {
                    MessageBox.Show("It doesn't seems to a ZIP file.");
                }
                else
                {
                    ExtractAndRun(filePath);
                }
            }
        }

        private void ExtractAndRun(string filePath)
        {
            instanceId = Guid.NewGuid().ToString();
            workingDirectory = Program.GetWorkingDirectory(instanceId);
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

                    // record the first deploy time
                    RecordFirstDeployTime(workingDirectory);

                    // follow the sub-directory
                    workingDirectory = Program.GetWorkingDirectory(instanceId, true);

                    // Run the appliction
                    Program.RunCommandPrompt(workingDirectory, entryFileName, scriptName, checkBox1.Checked, checkBox2.Checked);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Enable UI
                label1.Invoke((MethodInvoker)delegate {
                    EnableUI();
                });
            });

            DisableUI();
        }

        private void RecordFirstDeployTime(string directory)
        {
            try
            {
                string filePath = Path.Combine(directory, ".welsonjs_first_deploy_time");
                string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                File.WriteAllText(filePath, text);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to record first deploy time: {ex.Message}");
            }
        }

        private string OpenFileDialog()
        {
            string filePath = null;

            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Get the path of specified file
                    filePath = fileDialog.FileName;
                }
            }

            return filePath;
        }

        private bool IsInAdministrator()
        {
            try
            {
                WindowsPrincipal wp = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                return wp.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = checkBox1.Checked;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/gnh1201/welsonjs");
        }

        private void userdefinedVariablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new EnvForm()).Show();
        }

        private void instancesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new InstancesForm()).Show();
        }

        private void runAsAdministratorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!IsInAdministrator())
            {
                ProcessStartInfo procInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = Application.ExecutablePath,
                    WorkingDirectory = Environment.CurrentDirectory,
                    Verb = "runas"
                };

                try
                {
                    Process.Start(procInfo);
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to run as administrator: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Already running as Administrator.");
            }
        }

        private void globalSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new GlobalSettingsForm()).Show();
        }

        private void startCodeEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.resourceServer == null)
            {
                Program.resourceServer = new ResourceServer("http://localhost:3000/", "editor.html");
            }

            if (!Program.resourceServer.IsRunning())
            {
                Program.resourceServer.Start();
                ((ToolStripMenuItem)sender).Text = "Open the code editor...";
            }
            else
            {
                Program.OpenWebBrowser(Program.resourceServer.GetPrefix());
            }
        }

        private void openCodeEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.resourceServer == null)
            {
                MessageBox.Show("A resource server is not running.");
            }
            else
            {
                Program.OpenWebBrowser(Program.resourceServer.GetPrefix());
            }
        }

        private void openMicrosoftCopilotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.OpenWebBrowser("https://copilot.microsoft.com/");
        }
    }
}
