// InstancesForm.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WelsonJS.Esent;

namespace WelsonJS.Launcher
{
    public partial class InstancesForm : Form
    {
        private string _entryFileName;
        private string _scriptName;
        private readonly string _dateTimeFormat;
        private readonly EsentDatabase _db;

        public InstancesForm()
        {
            InitializeComponent();

            // set the datetime format
            _dateTimeFormat = Program.GetAppConfig("DateTimeFormat");

            // set the entry file name to run the instance
            _entryFileName = "bootstrap.bat";

            // connect the database to manage an instances
            Schema schema = new Schema("Instances", new List<Column>
            {
                new Column("InstanceId", typeof(string), 255),
                new Column("FirstDeployTime", typeof(DateTime), 1)
            });
            schema.SetPrimaryKey("InstanceId");
            _db = new EsentDatabase(schema, Program.GetAppDataPath());
        }

        public EsentDatabase GetDatabaseInstance()
        {
            return _db;
        }

        private void InstancesForm_Load(object sender, EventArgs e)
        {
            lvInstances.Items.Clear();
            LoadInstances();
        }

        private void LoadInstances()
        {
            var instances = _db.FindAll();
            foreach (var instance in instances)
            {
                try
                {
                    string instanceId = instance["InstanceId"].ToString();
                    string firstDeployTime = instance.ContainsKey("FirstDeployTime")
                        ? ((DateTime)instance["FirstDeployTime"]).ToString(_dateTimeFormat)
                        : "Unknown";

                    lvInstances.Items.Add(new ListViewItem(new[]
                    {
                        instanceId,
                        firstDeployTime
                    })
                    {
                        Tag = ResolveWorkingDirectory(instanceId)
                    });
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning(ex.Message);
                }
            }
        }

        private string ResolveWorkingDirectory(string instanceId)
        {
            string workingDirectory = Program.GetWorkingDirectory(instanceId, true);

            if (!Directory.Exists(workingDirectory))
            {
                workingDirectory = Path.Combine(Path.GetTempPath(), instanceId);
            }

            if (!Directory.Exists(workingDirectory))
            {
                throw new DirectoryNotFoundException($"Working directory for instance '{instanceId}' does not exist: {workingDirectory}");
            }

            return workingDirectory;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (lvInstances.SelectedItems.Count > 0)
            {
                _scriptName = txtUseSpecificScript.Text;

                string workingDirectory = (string)lvInstances.SelectedItems[0].Tag;

                Task.Run(() =>
                {
                    try
                    {
                        // Run the appliction
                        Program.RunCommandPrompt(workingDirectory, _entryFileName, _scriptName, cbUseSpecificScript.Checked, cbInteractiveServiceApp.Checked);
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
            var selectedItems = lvInstances.SelectedItems;

            if (selectedItems.Count > 0)
            {
                string workingDirectory = (string)selectedItems[0].Tag;
                string instanceId = selectedItems[0].SubItems[0].Text;

                try
                {
                    Directory.Delete(workingDirectory, true);
                    _db.DeleteById(instanceId);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                }

                lvInstances.Items.Clear();
                LoadInstances();
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
                string workingDirectory = (string)lvInstances.SelectedItems[0].Tag;

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
