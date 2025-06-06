﻿// EnvForm.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public partial class EnvForm : Form
    {
        private Dictionary<string, string> userVariables = new Dictionary<string, string>();
        private string tempFilePath;
        private readonly Encoding defaultEncoding = Encoding.UTF8;

        public EnvForm()
        {
            InitializeComponent();
            InitializeListView();

            // Set the variable file path
            tempFilePath = Path.Combine(Program.GetAppDataPath(), "welsonjs_default.env");

            LoadUserVariables();  // Load variables
        }

        // Initialize ListView
        private void InitializeListView()
        {
            lvUserDefinedVariables.View = View.Details;
            lvUserDefinedVariables.FullRowSelect = true;
            lvUserDefinedVariables.Columns[0].Width = 150;
            lvUserDefinedVariables.Columns[1].Width = 220;
            lvUserDefinedVariables.SelectedIndexChanged += lvUserDefinedVariables_SelectedIndexChanged;
        }

        // Load user-defined variables from the temporary folder in .env format
        private void LoadUserVariables()
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    string fileContent = File.ReadAllText(tempFilePath);
                    // Split based on new line characters
                    string[] keyValuePairs = fileContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    userVariables = new Dictionary<string, string>();

                    foreach (string pair in keyValuePairs)
                    {
                        // Split by the first occurrence of '='
                        int indexOfEquals = pair.IndexOf('=');
                        if (indexOfEquals != -1)
                        {
                            string key = pair.Substring(0, indexOfEquals).Trim();
                            string value = pair.Substring(indexOfEquals + 1).Trim();

                            // Remove surrounding quotes if present
                            if (value.StartsWith("\"") && value.EndsWith("\""))
                            {
                                value = value.Substring(1, value.Length - 2); // Remove the first and last character
                            }

                            // Unescape double quotes in the value
                            value = value.Replace("\\\"", "\"");

                            userVariables[key] = value;
                        }
                        else
                        {
                            MessageBox.Show($"Error parsing line: '{pair}'.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            lvUserDefinedVariables.Items.Clear();
            foreach (var variable in userVariables)
            {
                var item = new ListViewItem(variable.Key);
                item.SubItems.Add(variable.Value);
                lvUserDefinedVariables.Items.Add(item);
            }
        }

        // Handle ListView selection change
        private void lvUserDefinedVariables_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvUserDefinedVariables.SelectedItems.Count > 0)
            {
                var selectedItem = lvUserDefinedVariables.SelectedItems[0];
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

        // Save user-defined variables to the temporary folder in .env format
        private void SaveUserVariables()
        {
            try
            {
                List<string> lines = new List<string>();

                foreach (var variable in userVariables)
                {
                    // Escape double quotes in the value
                    string value = variable.Value.Replace("\"", "\\\"");

                    // Enclose the value in double quotes if it contains spaces
                    if (value.Contains(" "))
                    {
                        value = $"\"{value}\"";
                    }

                    // Create the line in the format: KEY=VALUE
                    string line = $"{variable.Key}={value}";
                    lines.Add(line);
                }

                // Write lines to the file
                File.WriteAllLines(tempFilePath, lines, defaultEncoding);
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
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    textSetValue.Text = folderDialog.SelectedPath;
                }
            }
        }

        // Handle "Open File" button click
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            using (var fileDialog = new OpenFileDialog())
            {
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    textSetName.Text = Path.GetFileName(fileDialog.FileName);
                    textSetValue.Text = fileDialog.FileName;
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Env files (*.env)|*.env|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Load variables from the selected file
                        string filePath = openFileDialog.FileName;
                        string[] lines = File.ReadAllLines(filePath, defaultEncoding);

                        foreach (string line in lines)
                        {
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            int indexOfEquals = line.IndexOf('=');
                            if (indexOfEquals != -1)
                            {
                                string key = line.Substring(0, indexOfEquals).Trim();
                                string value = line.Substring(indexOfEquals + 1).Trim();

                                // Remove surrounding quotes if present
                                if (value.StartsWith("\"") && value.EndsWith("\""))
                                {
                                    value = value.Substring(1, value.Length - 2);
                                }

                                // Unescape double quotes in the value
                                value = value.Replace("\\\"", "\"");

                                // Update or add the key-value pair
                                userVariables[key] = value;
                            }
                        }

                        // Save the updated variables to the file
                        SaveUserVariables();
                        UpdateListView(); // Refresh the display
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error importing variable file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Env files (*.env)|*.env|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Save the current variables to the selected file
                    string filePath = saveFileDialog.FileName;
                    List<string> lines = new List<string>();
                    foreach (var variable in userVariables)
                    {
                        // Escape double quotes in the value
                        string value = variable.Value.Replace("\"", "\\\"");

                        // Enclose the value in double quotes if it contains spaces
                        if (value.Contains(" "))
                        {
                            value = $"\"{value}\"";
                        }

                        lines.Add($"{variable.Key}={value}");
                    }
                    File.WriteAllLines(filePath, lines, defaultEncoding);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting variable file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}