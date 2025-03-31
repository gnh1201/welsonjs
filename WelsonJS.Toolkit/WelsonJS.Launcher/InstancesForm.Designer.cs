namespace WelsonJS.Launcher
{
    partial class InstancesForm
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
            this.lvInstances = new System.Windows.Forms.ListView();
            this.chInstanceId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chFirstDeployTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnOpenWithExplorer = new System.Windows.Forms.Button();
            this.cbInteractiveServiceApp = new System.Windows.Forms.CheckBox();
            this.txtUseSpecificScript = new System.Windows.Forms.TextBox();
            this.cbUseSpecificScript = new System.Windows.Forms.CheckBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lvInstances
            // 
            this.lvInstances.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chInstanceId,
            this.chFirstDeployTime});
            this.lvInstances.HideSelection = false;
            this.lvInstances.Location = new System.Drawing.Point(8, 8);
            this.lvInstances.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.lvInstances.Name = "lvInstances";
            this.lvInstances.Size = new System.Drawing.Size(384, 170);
            this.lvInstances.TabIndex = 0;
            this.lvInstances.UseCompatibleStateImageBehavior = false;
            this.lvInstances.View = System.Windows.Forms.View.Details;
            // 
            // chInstanceId
            // 
            this.chInstanceId.Text = "Instance ID";
            this.chInstanceId.Width = 220;
            // 
            // chFirstDeployTime
            // 
            this.chFirstDeployTime.Text = "First Deploy Time";
            this.chFirstDeployTime.Width = 160;
            // 
            // btnDelete
            // 
            this.btnDelete.Image = global::WelsonJS.Launcher.Properties.Resources.icon_delete_32;
            this.btnDelete.Location = new System.Drawing.Point(120, 243);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(105, 40);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text = "Delete";
            this.btnDelete.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnOpenWithExplorer
            // 
            this.btnOpenWithExplorer.Image = global::WelsonJS.Launcher.Properties.Resources.icon_directory_32;
            this.btnOpenWithExplorer.Location = new System.Drawing.Point(229, 243);
            this.btnOpenWithExplorer.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnOpenWithExplorer.Name = "btnOpenWithExplorer";
            this.btnOpenWithExplorer.Size = new System.Drawing.Size(162, 40);
            this.btnOpenWithExplorer.TabIndex = 3;
            this.btnOpenWithExplorer.Text = "Open with Explorer";
            this.btnOpenWithExplorer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOpenWithExplorer.UseVisualStyleBackColor = true;
            this.btnOpenWithExplorer.Click += new System.EventHandler(this.btnOpenWithExplorer_Click);
            // 
            // cbInteractiveServiceApp
            // 
            this.cbInteractiveServiceApp.AutoSize = true;
            this.cbInteractiveServiceApp.Location = new System.Drawing.Point(10, 215);
            this.cbInteractiveServiceApp.Name = "cbInteractiveServiceApp";
            this.cbInteractiveServiceApp.Size = new System.Drawing.Size(254, 16);
            this.cbInteractiveServiceApp.TabIndex = 9;
            this.cbInteractiveServiceApp.Text = "This is an Interactive Service Application";
            this.cbInteractiveServiceApp.UseVisualStyleBackColor = true;
            // 
            // txtUseSpecificScript
            // 
            this.txtUseSpecificScript.Enabled = false;
            this.txtUseSpecificScript.Location = new System.Drawing.Point(199, 189);
            this.txtUseSpecificScript.Name = "txtUseSpecificScript";
            this.txtUseSpecificScript.Size = new System.Drawing.Size(110, 21);
            this.txtUseSpecificScript.TabIndex = 8;
            // 
            // cbUseSpecificScript
            // 
            this.cbUseSpecificScript.AutoSize = true;
            this.cbUseSpecificScript.Location = new System.Drawing.Point(10, 192);
            this.cbUseSpecificScript.Name = "cbUseSpecificScript";
            this.cbUseSpecificScript.Size = new System.Drawing.Size(184, 16);
            this.cbUseSpecificScript.TabIndex = 7;
            this.cbUseSpecificScript.Text = "I want to use specific script ";
            this.cbUseSpecificScript.UseVisualStyleBackColor = true;
            this.cbUseSpecificScript.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // btnStart
            // 
            this.btnStart.Image = global::WelsonJS.Launcher.Properties.Resources.icon_start_32;
            this.btnStart.Location = new System.Drawing.Point(10, 243);
            this.btnStart.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(105, 40);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start";
            this.btnStart.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // InstancesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(401, 296);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.lvInstances);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.cbUseSpecificScript);
            this.Controls.Add(this.cbInteractiveServiceApp);
            this.Controls.Add(this.btnOpenWithExplorer);
            this.Controls.Add(this.txtUseSpecificScript);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::WelsonJS.Launcher.Properties.Resources.favicon;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.MaximizeBox = false;
            this.Name = "InstancesForm";
            this.Text = "Instances";
            this.Load += new System.EventHandler(this.InstancesForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView lvInstances;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnOpenWithExplorer;
        private System.Windows.Forms.CheckBox cbInteractiveServiceApp;
        private System.Windows.Forms.TextBox txtUseSpecificScript;
        private System.Windows.Forms.CheckBox cbUseSpecificScript;
        private System.Windows.Forms.ColumnHeader chInstanceId;
        private System.Windows.Forms.ColumnHeader chFirstDeployTime;
        private System.Windows.Forms.Button btnStart;
    }
}