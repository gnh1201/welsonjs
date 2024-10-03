using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public partial class EnvForm : Form
    {
        private Dictionary<string, string> userVariables = new Dictionary<string, string>();
        private string tempFilePath;

        public EnvForm()
        {
            InitializeComponent();
            InitializeListView();
            // Set the variable file path in the temporary folder
            tempFilePath = Path.Combine(Path.GetTempPath(), "WelsonJS.UserVariables.txt");
            LoadUserVariables();  // Load variables
        }

        // Initialize ListView
        private void InitializeListView()
        {
            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.Columns[0].Width = 150;
            listView1.Columns[1].Width = 220;
            listView1.SelectedIndexChanged += ListView1_SelectedIndexChanged;
        }

        // Load user-defined variables from the temporary folder (text format)
        private void LoadUserVariables()
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    string fileContent = File.ReadAllText(tempFilePath);
                    string[] keyValuePairs = fileContent.Split(new[] { " --" }, StringSplitOptions.RemoveEmptyEntries);

                    userVariables = new Dictionary<string, string>();

                    foreach (string pair in keyValuePairs)
                    {
                        string[] keyValue = pair.Split(new[] { '=' }, 2);
                        if (keyValue.Length == 2)
                        {
                            string key = keyValue[0].TrimStart('-');
                            string value = keyValue[1];
                            userVariables[key] = value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading variable file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    userVariables = new Dictionary<string, string>();
                }
            }
            else
            {
                userVariables = new Dictionary<string, string>();
            }
            UpdateListView();
        }

        // Update ListView with current variables
        private void UpdateListView()
        {
            listView1.Items.Clear();
            foreach (var variable in userVariables)
            {
                var item = new ListViewItem(variable.Key);
                item.SubItems.Add(variable.Value);
                listView1.Items.Add(item);
            }
        }

        // Handle ListView selection change
        private void ListView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var selectedItem = listView1.SelectedItems[0];
                textSetName.Text = selectedItem.Text;
                textSetValue.Text = selectedItem.SubItems[1].Text;
                checkDeleteVariable.Checked = false;
            }
        }

        // Handle OK button click
        private void btnOk_Click(object sender, EventArgs e)
        {
            var name = textSetName.Text.Trim();
            var value = textSetValue.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a variable name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (checkDeleteVariable.Checked)
            {
                if (userVariables.ContainsKey(name))
                {
                    userVariables.Remove(name);
                    MessageBox.Show("Variable deleted.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Variable not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                userVariables[name] = value;
                MessageBox.Show("Variable saved.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            UpdateListView();
            SaveUserVariables();  // Save variables
            ClearInputFields();
        }

        // Save user-defined variables to the temporary folder (text format)
        private void SaveUserVariables()
        {
            try
            {
                List<string> keyValuePairs = new List<string>();
                foreach (var variable in userVariables)
                {
                    keyValuePairs.Add($"--{variable.Key}={variable.Value}");
                }

                string content = string.Join(" ", keyValuePairs);
                File.WriteAllText(tempFilePath, content);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving variable file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Clear input fields
        private void ClearInputFields()
        {
            textSetName.Clear();
            textSetValue.Clear();
            checkDeleteVariable.Checked = false;
        }

        // Handle "Open Directory" button click
        private void btnOpenDirectory_Click(object sender, EventArgs e)
        {
            var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                textSetValue.Text = folderDialog.SelectedPath;
            }
        }

        // Handle "Open File" button click
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                textSetValue.Text = fileDialog.FileName;
            }
        }
    }
}