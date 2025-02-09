namespace WelsonJS.Launcher
{
    partial class EnvForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.checkDeleteVariable = new System.Windows.Forms.CheckBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnOpenFile = new System.Windows.Forms.Button();
            this.btnOpenDirectory = new System.Windows.Forms.Button();
            this.textSetValue = new System.Windows.Forms.TextBox();
            this.textSetName = new System.Windows.Forms.TextBox();
            this.labelSetValue = new System.Windows.Forms.Label();
            this.labelSetName = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.listView1);
            this.groupBox1.Location = new System.Drawing.Point(17, 18);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Size = new System.Drawing.Size(599, 255);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "User-defined variables";
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(23, 39);
            this.listView1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(550, 192);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.ListView1_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Value";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.checkDeleteVariable);
            this.groupBox2.Controls.Add(this.btnOk);
            this.groupBox2.Controls.Add(this.btnOpenFile);
            this.groupBox2.Controls.Add(this.btnOpenDirectory);
            this.groupBox2.Controls.Add(this.textSetValue);
            this.groupBox2.Controls.Add(this.textSetName);
            this.groupBox2.Controls.Add(this.labelSetValue);
            this.groupBox2.Controls.Add(this.labelSetName);
            this.groupBox2.Location = new System.Drawing.Point(17, 282);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox2.Size = new System.Drawing.Size(599, 344);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Update the user-defined variable";
            // 
            // checkDeleteVariable
            // 
            this.checkDeleteVariable.AutoSize = true;
            this.checkDeleteVariable.Location = new System.Drawing.Point(44, 132);
            this.checkDeleteVariable.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkDeleteVariable.Name = "checkDeleteVariable";
            this.checkDeleteVariable.Size = new System.Drawing.Size(178, 22);
            this.checkDeleteVariable.TabIndex = 7;
            this.checkDeleteVariable.Text = "Delete this variable";
            this.checkDeleteVariable.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Image = global::WelsonJS.Launcher.Properties.Resources.icon_check_32;
            this.btnOk.Location = new System.Drawing.Point(433, 184);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(123, 129);
            this.btnOk.TabIndex = 6;
            this.btnOk.Text = "Ok";
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnOpenFile
            // 
            this.btnOpenFile.Image = global::WelsonJS.Launcher.Properties.Resources.icon_file_32;
            this.btnOpenFile.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOpenFile.Location = new System.Drawing.Point(44, 254);
            this.btnOpenFile.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnOpenFile.Name = "btnOpenFile";
            this.btnOpenFile.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnOpenFile.Size = new System.Drawing.Size(287, 60);
            this.btnOpenFile.TabIndex = 5;
            this.btnOpenFile.Text = "Open the file...";
            this.btnOpenFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOpenFile.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOpenFile.UseVisualStyleBackColor = true;
            this.btnOpenFile.Click += new System.EventHandler(this.btnOpenFile_Click);
            // 
            // btnOpenDirectory
            // 
            this.btnOpenDirectory.Image = global::WelsonJS.Launcher.Properties.Resources.icon_directory_32;
            this.btnOpenDirectory.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOpenDirectory.Location = new System.Drawing.Point(44, 184);
            this.btnOpenDirectory.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnOpenDirectory.Name = "btnOpenDirectory";
            this.btnOpenDirectory.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnOpenDirectory.Size = new System.Drawing.Size(287, 60);
            this.btnOpenDirectory.TabIndex = 4;
            this.btnOpenDirectory.Text = "Open the directory...";
            this.btnOpenDirectory.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOpenDirectory.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOpenDirectory.UseVisualStyleBackColor = true;
            this.btnOpenDirectory.Click += new System.EventHandler(this.btnOpenDirectory_Click);
            // 
            // textSetValue
            // 
            this.textSetValue.Location = new System.Drawing.Point(140, 82);
            this.textSetValue.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textSetValue.Name = "textSetValue";
            this.textSetValue.Size = new System.Drawing.Size(414, 28);
            this.textSetValue.TabIndex = 3;
            // 
            // textSetName
            // 
            this.textSetName.Location = new System.Drawing.Point(140, 42);
            this.textSetName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textSetName.Name = "textSetName";
            this.textSetName.Size = new System.Drawing.Size(414, 28);
            this.textSetName.TabIndex = 2;
            // 
            // labelSetValue
            // 
            this.labelSetValue.AutoSize = true;
            this.labelSetValue.Location = new System.Drawing.Point(41, 88);
            this.labelSetValue.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSetValue.Name = "labelSetValue";
            this.labelSetValue.Size = new System.Drawing.Size(89, 18);
            this.labelSetValue.TabIndex = 1;
            this.labelSetValue.Text = "Set value:";
            // 
            // labelSetName
            // 
            this.labelSetName.AutoSize = true;
            this.labelSetName.Location = new System.Drawing.Point(41, 46);
            this.labelSetName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSetName.Name = "labelSetName";
            this.labelSetName.Size = new System.Drawing.Size(91, 18);
            this.labelSetName.TabIndex = 0;
            this.labelSetName.Text = "Set name:";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnExport);
            this.groupBox3.Controls.Add(this.btnImport);
            this.groupBox3.Location = new System.Drawing.Point(17, 634);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox3.Size = new System.Drawing.Size(599, 134);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Import and export";
            // 
            // btnExport
            // 
            this.btnExport.Image = global::WelsonJS.Launcher.Properties.Resources.icon_export_32;
            this.btnExport.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnExport.Location = new System.Drawing.Point(304, 44);
            this.btnExport.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnExport.Name = "btnExport";
            this.btnExport.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnExport.Size = new System.Drawing.Size(251, 60);
            this.btnExport.TabIndex = 6;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnImport
            // 
            this.btnImport.Image = global::WelsonJS.Launcher.Properties.Resources.icon_import_32;
            this.btnImport.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnImport.Location = new System.Drawing.Point(44, 44);
            this.btnImport.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnImport.Name = "btnImport";
            this.btnImport.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.btnImport.Size = new System.Drawing.Size(251, 60);
            this.btnImport.TabIndex = 5;
            this.btnImport.Text = "Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // EnvForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(639, 794);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::WelsonJS.Launcher.Properties.Resources.favicon;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.Name = "EnvForm";
            this.Text = "User-defined variables editor";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textSetValue;
        private System.Windows.Forms.TextBox textSetName;
        private System.Windows.Forms.Label labelSetValue;
        private System.Windows.Forms.Label labelSetName;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnOpenFile;
        private System.Windows.Forms.Button btnOpenDirectory;
        private System.Windows.Forms.CheckBox checkDeleteVariable;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Button btnExport;
    }
}