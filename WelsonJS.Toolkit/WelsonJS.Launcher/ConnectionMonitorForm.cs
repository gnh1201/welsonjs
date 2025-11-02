// ConnectionMonitorForm.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public partial class ConnectionMonitorForm : Form
    {
        public ConnectionMonitorForm()
        {
            InitializeComponent();
        }

        private void ConnectionMonitorForm_Load(object sender, EventArgs e)
        {
            RefreshConnections();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshConnections();
        }

        private void lvConnections_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnCloseSelected.Enabled = lvConnections.SelectedItems.Count > 0;
        }

        private void btnCloseSelected_Click(object sender, EventArgs e)
        {
            if (lvConnections.SelectedItems.Count == 0)
            {
                return;
            }

            var errors = new List<string>();
            bool anyClosed = false;

            foreach (ListViewItem item in lvConnections.SelectedItems)
            {
                if (item.Tag is ConnectionItemTag tag)
                {
                    try
                    {
                        if (tag.Provider.TryClose(tag.Key))
                        {
                            anyClosed = true;
                        }
                        else
                        {
                            errors.Add($"Unable to close {tag.Provider.ConnectionType} connection {tag.Key}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{tag.Provider.ConnectionType} {tag.Key}: {ex.Message}");
                    }
                }
            }

            if (anyClosed)
            {
                RefreshConnections();
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join(Environment.NewLine, errors), "Connection Monitor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshConnections()
        {
            IReadOnlyList<IManagedConnectionProvider> providers = ConnectionMonitorRegistry.GetProviders();

            lvConnections.BeginUpdate();
            lvConnections.Items.Clear();

            foreach (var provider in providers)
            {
                IReadOnlyCollection<ManagedConnectionStatus> statuses;
                try
                {
                    statuses = provider.GetStatuses();
                }
                catch (Exception ex)
                {
                    var errorItem = new ListViewItem(provider.ConnectionType)
                    {
                        Tag = null
                    };
                    errorItem.SubItems.Add(string.Empty);
                    errorItem.SubItems.Add("Error");
                    errorItem.SubItems.Add(ex.Message);
                    errorItem.SubItems.Add("Unknown");
                    lvConnections.Items.Add(errorItem);
                    continue;
                }

                foreach (var status in statuses)
                {
                    var item = new ListViewItem(status.ConnectionType)
                    {
                        Tag = new ConnectionItemTag(provider, status.Key)
                    };
                    item.SubItems.Add(status.Key);
                    item.SubItems.Add(status.State);
                    item.SubItems.Add(status.Description);
                    item.SubItems.Add(status.IsValid ? "Healthy" : "Stale");
                    lvConnections.Items.Add(item);
                }
            }

            lvConnections.EndUpdate();

            btnCloseSelected.Enabled = lvConnections.SelectedItems.Count > 0;
        }

        private sealed class ConnectionItemTag
        {
            public ConnectionItemTag(IManagedConnectionProvider provider, string key)
            {
                Provider = provider;
                Key = key ?? string.Empty;
            }

            public IManagedConnectionProvider Provider { get; }

            public string Key { get; }
        }
    }
}
