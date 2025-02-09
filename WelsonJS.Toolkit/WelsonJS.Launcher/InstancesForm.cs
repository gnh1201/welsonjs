using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public partial class InstancesForm : Form
    {
        private readonly string instancesRoot = Path.GetTempPath();
        private string entryFileName;
        private string scriptName;

        public InstancesForm()
        {
            InitializeComponent();

            entryFileName = "bootstrap.bat";
        }

        private void InstancesForm_Load(object sender, EventArgs e)
        {
            LoadInstances();
        }

        private void LoadInstances()
        {
            listView1.Items.Clear();

            if (!Directory.Exists(instancesRoot))
                return;

            foreach (string dir in Directory.GetDirectories(instancesRoot))
            {
                string launcherFile = Path.Combine(dir, ".welsonjs_launcher");

                if (File.Exists(launcherFile))
                {
                    string firstDeployTime = File.ReadAllText(launcherFile).Trim();
                    ListViewItem item = new ListViewItem(new[] {
                        Path.GetFileName(dir),
                        firstDeployTime
                    });
                    item.Tag = dir;
                    listView1.Items.Add(item);
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                scriptName = textBox1.Text;

                string selectedInstance = listView1.SelectedItems[0].Text;
                string workingDirectory = Path.Combine(instancesRoot, selectedInstance);

                Task.Run(() =>
                {
                    try
                    {
                        // Run the appliction
                        Program.RunCommandPrompt(workingDirectory, entryFileName, scriptName, checkBox1.Checked, checkBox2.Checked);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                });
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string selectedInstance = listView1.SelectedItems[0].Text;
                string workingDirectory = Path.Combine(instancesRoot, selectedInstance);

                if (Directory.Exists(workingDirectory))
                {
                    Directory.Delete(workingDirectory, true);
                    LoadInstances();
                }
            }
        }

        private void btnOpenWithExplorer_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string selectedInstance = listView1.SelectedItems[0].Text;
                string workingDirectory = Path.Combine(instancesRoot, selectedInstance);

                if (Directory.Exists(workingDirectory))
                {
                    System.Diagnostics.Process.Start("explorer", workingDirectory);
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = checkBox1.Checked;
        }
    }
}
