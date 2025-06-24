// InstancesForm.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WelsonJS.Esent;

namespace WelsonJS.Launcher
{
    public partial class InstancesForm : Form
    {
        private const string _timestampFormat = "yyyy-MM-dd HH:mm:ss";

        private string _entryFileName;
        private string _scriptName;
        private readonly DataStore _dataStore;

        public InstancesForm()
        {
            InitializeComponent();

            // set the entry file name to run the instance
            _entryFileName = "bootstrap.bat";

            // connect the database to manage an instances
            Schema schema = new Schema("Instances", new List<Column>
            {
                new Column("InstanceId", typeof(string), 255),
                new Column("FirstDeployTime", typeof(DateTime), 1)
            });
            schema.SetPrimaryKey("InstanceId");
            _dataStore = new DataStore(schema, Program.GetAppDataPath());
        }

        public DataStore GetDataStore()
        {
            return _dataStore;
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

            /*
            foreach (string dir in Directory.GetDirectories(instancesRoot))
            {
                string timestampFile = Path.Combine(dir, ".welsonjs_first_deploy_time");
                string entryScriptFile = Path.Combine(dir, "app.js");
                string firstDeployTime = null;

                if (File.Exists(timestampFile)
                    && DateTime.TryParse(File.ReadAllText(timestampFile).Trim(), out DateTime parsedTimestamp))
                {
                    firstDeployTime = parsedTimestamp.ToString(_timestampFormat);
                }
                else if (File.Exists(entryScriptFile))
                {
                    firstDeployTime = File.GetCreationTime(entryScriptFile).ToString(_timestampFormat);
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
            */

            var instances = _dataStore.FindAll();
            foreach (var instance in instances)
            {
                string instanceId = instance["InstanceId"].ToString();
                string firstDeployTime = instance.ContainsKey("FirstDeployTime") 
                    ? ((DateTime)instance["FirstDeployTime"]).ToString(_timestampFormat) 
                    : "Unknown";

                lvInstances.Items.Add(new ListViewItem(new[]
                {
                    instanceId,
                    firstDeployTime
                })
                {
                    Tag = Path.Combine(instancesRoot, instanceId)
                });
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (lvInstances.SelectedItems.Count > 0)
            {
                _scriptName = txtUseSpecificScript.Text;

                string instanceId = lvInstances.SelectedItems[0].Text;
                string workingDirectory = Program.GetWorkingDirectory(instanceId, true);

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
