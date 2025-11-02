// WebSocketManagerForm.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public partial class WebSocketManagerForm : Form
    {
        private sealed class ConnectionInfo
        {
            public string Host;
            public int Port;
            public string Path;
        }

        private sealed class ConnectionRecord
        {
            public ConnectionInfo Info;
            public ClientWebSocket Socket;
            public string LastStatus;
        }

        private readonly WebSocketManager _manager;
        private readonly ConcurrentDictionary<string, ConnectionRecord> _records;
        private readonly string _dateTimeFormat;

        public WebSocketManagerForm()
        {
            InitializeComponent();

            _manager = new WebSocketManager();
            _records = new ConcurrentDictionary<string, ConnectionRecord>();

            string format = Program.GetAppConfig("DateTimeFormat");
            if (string.IsNullOrWhiteSpace(format))
            {
                format = "yyyy-MM-dd HH:mm:ss";
            }

            _dateTimeFormat = format;
            btnConnect.Enabled = false;
        }

        private void WebSocketManagerForm_Load(object sender, EventArgs e)
        {
            statusTimer.Start();
            UpdateButtons();
        }

        private void WebSocketManagerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            statusTimer.Stop();
        }

        private void ConnectionInputChanged(object sender, EventArgs e)
        {
            btnConnect.Enabled = !string.IsNullOrWhiteSpace(txtHost.Text);
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            string host = txtHost.Text.Trim();
            string path = NormalizePath(txtPath.Text);
            int port = (int)nudPort.Value;

            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("Host is required.", "WebSocket Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtHost.Focus();
                return;
            }

            btnConnect.Enabled = false;

            try
            {
                ConnectionInfo info = new ConnectionInfo
                {
                    Host = host,
                    Port = port,
                    Path = path
                };

                ClientWebSocket socket = await _manager.GetOrCreateAsync(info.Host, info.Port, info.Path);

                string key = BuildKey(info.Host, info.Port, info.Path);
                ConnectionRecord record = _records.GetOrAdd(key, k => new ConnectionRecord { Info = info });
                record.Info = info;
                record.Socket = socket;
                record.LastStatus = socket.State.ToString();

                AddOrUpdateListViewItem(record, record.LastStatus);
                RefreshStatuses();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect: " + ex.Message, "WebSocket Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConnect.Enabled = !string.IsNullOrWhiteSpace(txtHost.Text);
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            ConnectionRecord record = GetSelectedRecord();
            if (record == null)
            {
                return;
            }

            try
            {
                _manager.Remove(record.Info.Host, record.Info.Port, record.Info.Path);

                if (record.Socket != null)
                {
                    try
                    {
                        record.Socket.Dispose();
                    }
                    catch
                    {
                        // Ignore dispose errors from socket
                    }
                    record.Socket = null;
                }

                record.LastStatus = "Closed";
                AddOrUpdateListViewItem(record, record.LastStatus);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to disconnect: " + ex.Message, "WebSocket Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshStatuses();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            RefreshStatuses();
        }

        private void lvConnections_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            btnDisconnect.Enabled = lvConnections.SelectedItems.Count > 0;
        }

        private void RefreshStatuses()
        {
            foreach (ListViewItem item in lvConnections.Items)
            {
                ConnectionRecord record = item.Tag as ConnectionRecord;
                if (record == null)
                {
                    continue;
                }

                string status = record.LastStatus ?? "Unknown";
                if (record.Socket != null)
                {
                    try
                    {
                        status = record.Socket.State.ToString();
                        record.LastStatus = status;
                    }
                    catch (ObjectDisposedException)
                    {
                        record.Socket = null;
                        status = "Disposed";
                        record.LastStatus = status;
                    }
                }

                if (item.SubItems.Count >= 5)
                {
                    item.SubItems[3].Text = status;
                    item.SubItems[4].Text = DateTime.Now.ToString(_dateTimeFormat);
                }
            }
        }

        private void AddOrUpdateListViewItem(ConnectionRecord record, string status)
        {
            string key = BuildKey(record.Info.Host, record.Info.Port, record.Info.Path);
            string formattedPath = FormatPath(record.Info.Path);
            string timestamp = DateTime.Now.ToString(_dateTimeFormat);

            ListViewItem item;
            if (lvConnections.Items.ContainsKey(key))
            {
                item = lvConnections.Items[key];
                item.SubItems[0].Text = record.Info.Host;
                item.SubItems[1].Text = record.Info.Port.ToString();
                item.SubItems[2].Text = formattedPath;
                item.SubItems[3].Text = status;
                item.SubItems[4].Text = timestamp;
                item.Tag = record;
            }
            else
            {
                item = new ListViewItem(new string[]
                {
                    record.Info.Host,
                    record.Info.Port.ToString(),
                    formattedPath,
                    status,
                    timestamp
                });
                item.Name = key;
                item.Tag = record;
                lvConnections.Items.Add(item);
            }
        }

        private ConnectionRecord GetSelectedRecord()
        {
            if (lvConnections.SelectedItems.Count == 0)
            {
                return null;
            }

            return lvConnections.SelectedItems[0].Tag as ConnectionRecord;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string trimmed = path.Trim();
            while (trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1);
            }

            return trimmed;
        }

        private static string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "/";
            }

            return path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path;
        }

        private static string BuildKey(string host, int port, string path)
        {
            if (host == null)
            {
                host = string.Empty;
            }

            if (path == null)
            {
                path = string.Empty;
            }

            return host.ToLowerInvariant() + ":" + port + "/" + path;
        }
    }
}
