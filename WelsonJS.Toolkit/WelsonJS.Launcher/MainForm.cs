// MainForm.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
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
        private const string _entryFileName = "bootstrap.bat";
        private readonly string _dateTimeFormat;
        private readonly ICompatibleLogger _logger;

        private string _workingDirectory;
        private string _instanceId;
        private string _scriptName;

        public MainForm(ICompatibleLogger logger = null)
        {
            // Set the logger
            _logger = logger ?? new TraceLogger();

            // Set the datetime format
            _dateTimeFormat = Program.GetAppConfig("DateTimeFormat");

            // Initialize UI
            InitializeComponent();

            // Check the user is an Administator
            if (IsInAdministrator())
            {
                Text += " (Administrator)";
            }

            // Send to the tray (to the background)
            notifyIcon1.DoubleClick += OnShow;
            openLauncherToolStripMenuItem.Click += OnShow;
            exitToolStripMenuItem.Click += OnExit;

            // Autostart the resource server
            if (Program.GetAppConfig("ResourceServerAutoStart").ToLower() == "true")
            {
                RunResourceServer();
            }
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

                    DisableUI();
                    Task.Run(() => RunAppPackageFile(filePath));
                }
            }
        }

        private void RunAppPackageFile(string filePath)
        {
            _instanceId = Guid.NewGuid().ToString();
            _workingDirectory = Program.GetWorkingDirectory(_instanceId);
            _scriptName = txtUseSpecificScript.Text;

            try
            {
                // check if the working directory exists
                if (Directory.Exists(_workingDirectory))
                {
                    throw new InvalidOperationException("GUID validation failed. Directory already exists.");
                }

                // try to extract ZIP file
                ZipFile.ExtractToDirectory(filePath, _workingDirectory);

                // record the first deploy time
                RecordFirstDeployTime(_workingDirectory, _instanceId);

                // follow the sub-directory
                _workingDirectory = Program.GetWorkingDirectory(_instanceId, true);

                // Run the application
                Program.RunCommandPrompt(_workingDirectory, _entryFileName, _scriptName, cbUseSpecificScript.Checked, cbInteractiveServiceApp.Checked);
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>  MessageBox.Show($"Extraction failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }

            // Enable UI
            SafeInvoke(() => EnableUI());
        }

        private bool RunResourceServer()
        {
            Program.InitializeResourceServer();

            if (Program._resourceServer == null)
            {
                _logger.Error("Resource server failed to initialize.");
                return false;
            }

            if (!Program._resourceServer.IsRunning())
            {
                Program._resourceServer.Start(false);

                string text = "Open the editor...";
                startTheEditorToolStripMenuItem.Text = text;
                btnStartTheEditor.Text = text;
            }

            return Program._resourceServer.IsRunning();
        }

        private void RecordFirstDeployTime(string directory, string instanceId)
        {
            // get current time
            DateTime now = DateTime.Now;

            // record to the metadata database
            InstancesForm instancesForm = new InstancesForm();
            try
            {
                instancesForm.GetDatabaseInstance().Insert(new Dictionary<string, object>
                {
                    ["InstanceId"] = instanceId,
                    ["FirstDeployTime"] = now
                }, out _);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to record first deploy time: {ex.Message}");
            }
            instancesForm.Dispose();

            // record to the instance directory
            try
            {
                string filePath = Path.Combine(directory, ".welsonjs_first_deploy_time");
                string text = now.ToString(_dateTimeFormat);
                File.WriteAllText(filePath, text);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to record first deploy time: {ex.Message}");
            }
        }

        private bool IsInAdministrator()
        {
            try
            {
                WindowsPrincipal wp = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                return wp.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                _logger.Info($"The current user is not an administrator, or the check failed: {ex.Message}");
                return false;
            }
        }

        private void LaunchEditor()
        {
            if (RunResourceServer())
            {
                Program.OpenWebBrowser(Program._resourceServer.GetPrefix());
            }
            else
            {
                _logger.Error("Failed to start the resource server.");
                MessageBox.Show(
                    "Failed to start the resource server. Please check your configuration or try again.",
                    "Resource Server Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void cbUseSpecificScript_CheckedChanged(object sender, EventArgs e)
        {
            txtUseSpecificScript.Enabled = cbUseSpecificScript.Checked;
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
                    MessageBox.Show($"Failed to run as Administrator: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void btnStartTheEditor_Click(object sender, EventArgs e)
        {
            LaunchEditor();
        }

        private void startCodeEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LaunchEditor();
        }

        private void openCodeEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program._resourceServer == null)
            {
                MessageBox.Show("A resource server is not running.");
                return;
            }

            Program.OpenWebBrowser(Program._resourceServer.GetPrefix());
        }

        private void openCopilotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.OpenWebBrowser(Program.GetAppConfig("CopilotUrl"));
        }

        private void webSocketManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new WebSocketManagerForm()).Show();
        }

        private void btnJoinTheCommunity_Click(object sender, EventArgs e)
        {
            Program.OpenWebBrowser(Program.GetAppConfig("RepositoryUrl"));
        }
    }
}
