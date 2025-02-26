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
                    object value = key.GetValue(RegistryKey);
                    if (value != null && value is int maxStatements)
                    {
                        textBox1.Text = maxStatements.ToString();
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (uint.TryParse(textBox1.Text, out uint maxStatements))
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    key.SetValue(RegistryKey, (int)maxStatements, RegistryValueKind.DWord);
                }
                MessageBox.Show($"MaxScriptStatements setting has been changed to {maxStatements}.", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Please enter a valid number within the DWORD range.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
