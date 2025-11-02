// WebSocketManagerForm.Designer.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
namespace WelsonJS.Launcher
{
    partial class WebSocketManagerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lvConnections = new System.Windows.Forms.ListView();
            this.chHost = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chPort = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chUpdated = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnRefresh = new System.Windows.Forms.Button();
            this.gbNewConnection = new System.Windows.Forms.GroupBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.lblPath = new System.Windows.Forms.Label();
            this.nudPort = new System.Windows.Forms.NumericUpDown();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtHost = new System.Windows.Forms.TextBox();
            this.lblHost = new System.Windows.Forms.Label();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.statusTimer = new System.Windows.Forms.Timer(this.components);
            this.gbNewConnection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            this.SuspendLayout();
            // 
            // lvConnections
            // 
            this.lvConnections.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chHost,
            this.chPort,
            this.chPath,
            this.chStatus,
            this.chUpdated});
            this.lvConnections.FullRowSelect = true;
            this.lvConnections.HideSelection = false;
            this.lvConnections.Location = new System.Drawing.Point(12, 12);
            this.lvConnections.MultiSelect = false;
            this.lvConnections.Name = "lvConnections";
            this.lvConnections.Size = new System.Drawing.Size(601, 188);
            this.lvConnections.TabIndex = 0;
            this.lvConnections.UseCompatibleStateImageBehavior = false;
            this.lvConnections.View = System.Windows.Forms.View.Details;
            this.lvConnections.SelectedIndexChanged += new System.EventHandler(this.lvConnections_SelectedIndexChanged);
            // 
            // chHost
            // 
            this.chHost.Text = "Host";
            this.chHost.Width = 150;
            // 
            // chPort
            // 
            this.chPort.Text = "Port";
            this.chPort.Width = 80;
            // 
            // chPath
            // 
            this.chPath.Text = "Path";
            this.chPath.Width = 150;
            // 
            // chStatus
            // 
            this.chStatus.Text = "Status";
            this.chStatus.Width = 120;
            // 
            // chUpdated
            // 
            this.chUpdated.Text = "Last Updated";
            this.chUpdated.Width = 180;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(518, 206);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(95, 23);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // gbNewConnection
            // 
            this.gbNewConnection.Controls.Add(this.btnConnect);
            this.gbNewConnection.Controls.Add(this.txtPath);
            this.gbNewConnection.Controls.Add(this.lblPath);
            this.gbNewConnection.Controls.Add(this.nudPort);
            this.gbNewConnection.Controls.Add(this.lblPort);
            this.gbNewConnection.Controls.Add(this.txtHost);
            this.gbNewConnection.Controls.Add(this.lblHost);
            this.gbNewConnection.Location = new System.Drawing.Point(12, 235);
            this.gbNewConnection.Name = "gbNewConnection";
            this.gbNewConnection.Size = new System.Drawing.Size(601, 100);
            this.gbNewConnection.TabIndex = 2;
            this.gbNewConnection.TabStop = false;
            this.gbNewConnection.Text = "New connection";
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(470, 56);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(112, 27);
            this.btnConnect.TabIndex = 6;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(70, 60);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(318, 21);
            this.txtPath.TabIndex = 5;
            this.txtPath.TextChanged += new System.EventHandler(this.ConnectionInputChanged);
            // 
            // lblPath
            // 
            this.lblPath.AutoSize = true;
            this.lblPath.Location = new System.Drawing.Point(15, 63);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(31, 12);
            this.lblPath.TabIndex = 4;
            this.lblPath.Text = "Path";
            // 
            // nudPort
            // 
            this.nudPort.Location = new System.Drawing.Point(308, 26);
            this.nudPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nudPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudPort.Name = "nudPort";
            this.nudPort.Size = new System.Drawing.Size(80, 21);
            this.nudPort.TabIndex = 3;
            this.nudPort.Value = new decimal(new int[] {
            80,
            0,
            0,
            0});
            this.nudPort.ValueChanged += new System.EventHandler(this.ConnectionInputChanged);
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(268, 29);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(29, 12);
            this.lblPort.TabIndex = 2;
            this.lblPort.Text = "Port";
            // 
            // txtHost
            // 
            this.txtHost.Location = new System.Drawing.Point(70, 26);
            this.txtHost.Name = "txtHost";
            this.txtHost.Size = new System.Drawing.Size(180, 21);
            this.txtHost.TabIndex = 1;
            this.txtHost.TextChanged += new System.EventHandler(this.ConnectionInputChanged);
            // 
            // lblHost
            // 
            this.lblHost.AutoSize = true;
            this.lblHost.Location = new System.Drawing.Point(15, 29);
            this.lblHost.Name = "lblHost";
            this.lblHost.Size = new System.Drawing.Size(31, 12);
            this.lblHost.TabIndex = 0;
            this.lblHost.Text = "Host";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Location = new System.Drawing.Point(12, 341);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(126, 27);
            this.btnDisconnect.TabIndex = 3;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(487, 341);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(126, 27);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // statusTimer
            // 
            this.statusTimer.Interval = 2000;
            this.statusTimer.Tick += new System.EventHandler(this.statusTimer_Tick);
            // 
            // WebSocketManagerForm
            // 
            this.AcceptButton = this.btnConnect;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(625, 380);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.gbNewConnection);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.lvConnections);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WebSocketManagerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "WebSocket Manager";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.WebSocketManagerForm_FormClosed);
            this.Load += new System.EventHandler(this.WebSocketManagerForm_Load);
            this.gbNewConnection.ResumeLayout(false);
            this.gbNewConnection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ListView lvConnections;
        private System.Windows.Forms.ColumnHeader chHost;
        private System.Windows.Forms.ColumnHeader chPort;
        private System.Windows.Forms.ColumnHeader chPath;
        private System.Windows.Forms.ColumnHeader chStatus;
        private System.Windows.Forms.ColumnHeader chUpdated;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.GroupBox gbNewConnection;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.NumericUpDown nudPort;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.Label lblHost;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Timer statusTimer;
    }
}
