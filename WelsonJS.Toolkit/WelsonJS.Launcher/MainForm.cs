using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
            InitializeComponent();

            entryFileName = "bootstrap.bat";
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
            workingDirectory = Path.Combine(Path.GetTempPath(), instanceId);
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

                    // If it is created the sub-directory
                    workingDirectory = GetFinalDirectory(workingDirectory);

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
                string filePath = Path.Combine(directory, ".welsonjs_launcher");
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
    }
}
