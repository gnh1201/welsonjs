using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public partial class InstancesForm : Form
    {
        private string instancesRoot;
        private string entryFileName;
        private string scriptName;

        public InstancesForm()
        {
            InitializeComponent();

            instancesRoot = Program.GetAppDataPath();
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
                string timestampFile = Path.Combine(dir, ".welsonjs_first_deploy_time");

                if (File.Exists(timestampFile))
                {
                    string firstDeployTime = File.ReadAllText(timestampFile).Trim();
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

                // If it is created the sub-directory
                workingDirectory = Program.GetFinalDirectory(workingDirectory);

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
            else
            {
                MessageBox.Show("No selected an instance");
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
            else
            {
                MessageBox.Show("No selected an instance");
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
            else
            {
                MessageBox.Show("No selected an instance");
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = checkBox1.Checked;
        }
    }
}
