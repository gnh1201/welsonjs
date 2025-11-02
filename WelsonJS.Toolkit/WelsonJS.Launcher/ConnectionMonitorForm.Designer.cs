namespace WelsonJS.Launcher
{
    partial class ConnectionMonitorForm
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

        private void InitializeComponent()
        {
            this.lvConnections = new System.Windows.Forms.ListView();
            this.chType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chKey = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chDetails = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chHealth = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnCloseSelected = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lvConnections
            // 
            this.lvConnections.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chType,
            this.chKey,
            this.chState,
            this.chDetails,
            this.chHealth});
            this.lvConnections.FullRowSelect = true;
            this.lvConnections.HideSelection = false;
            this.lvConnections.Location = new System.Drawing.Point(12, 12);
            this.lvConnections.MultiSelect = true;
            this.lvConnections.Name = "lvConnections";
            this.lvConnections.Size = new System.Drawing.Size(640, 260);
            this.lvConnections.TabIndex = 0;
            this.lvConnections.UseCompatibleStateImageBehavior = false;
            this.lvConnections.View = System.Windows.Forms.View.Details;
            this.lvConnections.SelectedIndexChanged += new System.EventHandler(this.lvConnections_SelectedIndexChanged);
            // 
            // chType
            // 
            this.chType.Text = "Type";
            this.chType.Width = 100;
            // 
            // chKey
            // 
            this.chKey.Text = "Key";
            this.chKey.Width = 140;
            // 
            // chState
            // 
            this.chState.Text = "State";
            this.chState.Width = 100;
            // 
            // chDetails
            // 
            this.chDetails.Text = "Details";
            this.chDetails.Width = 220;
            // 
            // chHealth
            // 
            this.chHealth.Text = "Health";
            this.chHealth.Width = 80;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(12, 280);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(95, 30);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnCloseSelected
            // 
            this.btnCloseSelected.Enabled = false;
            this.btnCloseSelected.Location = new System.Drawing.Point(557, 280);
            this.btnCloseSelected.Name = "btnCloseSelected";
            this.btnCloseSelected.Size = new System.Drawing.Size(95, 30);
            this.btnCloseSelected.TabIndex = 2;
            this.btnCloseSelected.Text = "Close";
            this.btnCloseSelected.UseVisualStyleBackColor = true;
            this.btnCloseSelected.Click += new System.EventHandler(this.btnCloseSelected_Click);
            // 
            // ConnectionMonitorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 322);
            this.Controls.Add(this.btnCloseSelected);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.lvConnections);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = global::WelsonJS.Launcher.Properties.Resources.favicon;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConnectionMonitorForm";
            this.Text = "Connection Monitor";
            this.Load += new System.EventHandler(this.ConnectionMonitorForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView lvConnections;
        private System.Windows.Forms.ColumnHeader chType;
        private System.Windows.Forms.ColumnHeader chKey;
        private System.Windows.Forms.ColumnHeader chState;
        private System.Windows.Forms.ColumnHeader chDetails;
        private System.Windows.Forms.ColumnHeader chHealth;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnCloseSelected;
    }
}
