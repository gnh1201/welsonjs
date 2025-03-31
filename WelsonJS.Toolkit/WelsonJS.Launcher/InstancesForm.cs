using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public partial class InstancesForm : Form
    {
        private string entryFileName;
        private string scriptName;
        private const string timestampFormat = "yyyy-MM-dd HH:mm:ss";

        public InstancesForm()
        {
            InitializeComponent();

            entryFileName = "bootstrap.bat";
        }

        private void InstancesForm_Load(object sender, EventArgs e)
        {
            lvInstances.Items.Clear();
            LoadInstances(Program.GetAppDataPath());
            LoadInstances(Path.GetTempPath());
        }

        private void LoadInstances(string instancesRoot)
        {
            if (!Directory.Exists(instancesRoot))
                return;

            foreach (string dir in Directory.GetDirectories(instancesRoot))
            {
                string timestampFile = Path.Combine(dir, ".welsonjs_first_deploy_time");
                string entryScriptFile = Path.Combine(dir, "app.js");
                string firstDeployTime = null;

                if (File.Exists(timestampFile)
                    && DateTime.TryParse(File.ReadAllText(timestampFile).Trim(), out DateTime parsedTimestamp))
                {
                    firstDeployTime = parsedTimestamp.ToString(timestampFormat);
                }
                else if (File.Exists(entryScriptFile))
                {
                    firstDeployTime = File.GetCreationTime(entryScriptFile).ToString(timestampFormat);
                }

                if (firstDeployTime != null)
                {
                    lvInstances.Items.Add(new ListViewItem(new[]
                    {
                        Path.GetFileName(dir),
                        firstDeployTime
                    })
                    {
                        Tag = dir
                    });
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (lvInstances.SelectedItems.Count > 0)
            {
                scriptName = txtUseSpecificScript.Text;

                string instanceId = lvInstances.SelectedItems[0].Text;
                string workingDirectory = Program.GetWorkingDirectory(instanceId, true);

                Task.Run(() =>
                {
                    try
                    {
                        // Run the appliction
                        Program.RunCommandPrompt(workingDirectory, entryFileName, scriptName, cbUseSpecificScript.Checked, cbInteractiveServiceApp.Checked);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                });
            }
            else
            {
                MessageBox.Show("No selected an instance");
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lvInstances.SelectedItems.Count > 0)
            {
                string instanceId = lvInstances.SelectedItems[0].Text;
                string workingDirectory = Program.GetWorkingDirectory(instanceId, false);

                if (!Directory.Exists(workingDirectory))
                {
                    workingDirectory = Path.Combine(Path.GetTempPath(), instanceId);
                }

                if (Directory.Exists(workingDirectory))
                {
                    Directory.Delete(workingDirectory, true);

                    lvInstances.Items.Clear();
                    LoadInstances(Program.GetAppDataPath());
                    LoadInstances(Path.GetTempPath());
                }
            }
            else
            {
                MessageBox.Show("No selected an instance");
            }
        }

        private void btnOpenWithExplorer_Click(object sender, EventArgs e)
        {
            if (lvInstances.SelectedItems.Count > 0)
            {
                string instanceId = lvInstances.SelectedItems[0].Text;
                string workingDirectory = Program.GetWorkingDirectory(instanceId, true);

                if (Directory.Exists(workingDirectory))
                {
                    System.Diagnostics.Process.Start("explorer", workingDirectory);
                }
            }
            else
            {
                MessageBox.Show("No selected an instance");
            }
        }

        private void cbUseSpecificScript_CheckedChanged(object sender, EventArgs e)
        {
            txtUseSpecificScript.Enabled = cbUseSpecificScript.Checked;
        }
    }
}
