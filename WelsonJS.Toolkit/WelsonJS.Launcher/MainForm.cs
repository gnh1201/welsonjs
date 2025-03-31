using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using WelsonJS.Launcher.Tools;

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
                this.ShowInTaskbar = false;
                notifyIcon1.Visible = true;
            }
            base.OnFormClosing(e);
        }

        private void OnShow(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
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
            btnRunFromZipFile.Enabled = true;
            btnRunFromExternalLink.Enabled = true;
            cbUseSpecificScript.Enabled = true;
            cbInteractiveServiceApp.Enabled = true;
            if (cbUseSpecificScript.Checked)
            {
                txtUseSpecificScript.Enabled = true;
            }
        }

        private void DisableUI()
        {
            label1.Text = "Please wait...";
            btnRunFromZipFile.Enabled = false;
            btnRunFromExternalLink.Enabled = false;
            cbUseSpecificScript.Enabled = false;
            cbInteractiveServiceApp.Enabled = false;
            txtUseSpecificScript.Enabled = false;
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }

        private void btnRunFromExternalLink_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Coming soon...!");
        }

        private void btnRunFromZipFile_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "zip files (*.zip)|*.zip|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    ExtractAndRun(filePath);
                }
            }
        }

        private void ExtractAndRun(string filePath)
        {
            instanceId = Guid.NewGuid().ToString();
            workingDirectory = Program.GetWorkingDirectory(instanceId);
            scriptName = txtUseSpecificScript.Text;

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
                    Program.RunCommandPrompt(workingDirectory, entryFileName, scriptName, cbUseSpecificScript.Checked, cbInteractiveServiceApp.Checked);
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    });
                }

                // Enable UI
                SafeInvoke(() => {
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

        private void cbUseSpecificScript_CheckedChanged(object sender, EventArgs e)
        {
            txtUseSpecificScript.Enabled = cbUseSpecificScript.Checked;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.OpenWebBrowser(Properties.Resources.RepositoryUrl);
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
                Program.resourceServer = new ResourceServer(Properties.Resources.ResourceServerPrefix, "editor.html");
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

        private void openCopilotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.OpenWebBrowser(Properties.Resources.CopilotUrl);
        }
    }
}
