// GlobalSettingsForm.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public partial class GlobalSettingsForm : Form
    {
        private const string RegistryPath = "Software\\Microsoft\\Internet Explorer\\Styles";
        private const string RegistryKey = "MaxScriptStatements";

        public GlobalSettingsForm()
        {
            InitializeComponent();
            LoadRegistryValue();
        }

        private void LoadRegistryValue()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
            {
                if (key != null)
                {
                    string value = key.GetValue(RegistryKey)?.ToString();
                    if (value != null && int.TryParse(value, out int maxStatements))
                    {
                        txtMaxScriptStatements.Text = maxStatements.ToString();
                    }
                }
            }
        }

        private void btnOkMaxScriptStatements_Click(object sender, EventArgs e)
        {
            try
            {
                if (int.TryParse(txtMaxScriptStatements.Text, out int maxStatements))
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                    {
                        key.SetValue(RegistryKey, maxStatements, RegistryValueKind.DWord);
                    }
                    MessageBox.Show($"MaxScriptStatements setting has been changed to {maxStatements}.", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Please enter a valid number within the DWORD range.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } 
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while trying to change the MaxScriptStatements setting: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
