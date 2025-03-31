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
            this.gbUserDefinedVariables = new System.Windows.Forms.GroupBox();
            this.lvUserDefinedVariables = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.gbUpdateUserDefinedVariable = new System.Windows.Forms.GroupBox();
            this.checkDeleteVariable = new System.Windows.Forms.CheckBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnOpenFile = new System.Windows.Forms.Button();
            this.btnOpenDirectory = new System.Windows.Forms.Button();
            this.textSetValue = new System.Windows.Forms.TextBox();
            this.textSetName = new System.Windows.Forms.TextBox();
            this.labelSetValue = new System.Windows.Forms.Label();
            this.labelSetName = new System.Windows.Forms.Label();
            this.gbImportAndExport = new System.Windows.Forms.GroupBox();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();
            this.gbUserDefinedVariables.SuspendLayout();
            this.gbUpdateUserDefinedVariable.SuspendLayout();
            this.gbImportAndExport.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbUserDefinedVariables
            // 
            this.gbUserDefinedVariables.Controls.Add(this.lvUserDefinedVariables);
            this.gbUserDefinedVariables.Location = new System.Drawing.Point(12, 12);
            this.gbUserDefinedVariables.Name = "gbUserDefinedVariables";
            this.gbUserDefinedVariables.Size = new System.Drawing.Size(419, 170);
            this.gbUserDefinedVariables.TabIndex = 0;
            this.gbUserDefinedVariables.TabStop = false;
            this.gbUserDefinedVariables.Text = "User-defined variables";
            // 
            // lvUserDefinedVariables
            // 
            this.lvUserDefinedVariables.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.lvUserDefinedVariables.HideSelection = false;
            this.lvUserDefinedVariables.Location = new System.Drawing.Point(16, 26);
            this.lvUserDefinedVariables.Name = "lvUserDefinedVariables";
            this.lvUserDefinedVariables.Size = new System.Drawing.Size(386, 129);
            this.lvUserDefinedVariables.TabIndex = 0;
            this.lvUserDefinedVariables.UseCompatibleStateImageBehavior = false;
            this.lvUserDefinedVariables.SelectedIndexChanged += new System.EventHandler(this.lvUserDefinedVariables_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Value";
            // 
            // gbUpdateUserDefinedVariable
            // 
            this.gbUpdateUserDefinedVariable.Controls.Add(this.checkDeleteVariable);
            this.gbUpdateUserDefinedVariable.Controls.Add(this.btnOk);
            this.gbUpdateUserDefinedVariable.Controls.Add(this.btnOpenFile);
            this.gbUpdateUserDefinedVariable.Controls.Add(this.btnOpenDirectory);
            this.gbUpdateUserDefinedVariable.Controls.Add(this.textSetValue);
            this.gbUpdateUserDefinedVariable.Controls.Add(this.textSetName);
            this.gbUpdateUserDefinedVariable.Controls.Add(this.labelSetValue);
            this.gbUpdateUserDefinedVariable.Controls.Add(this.labelSetName);
            this.gbUpdateUserDefinedVariable.Location = new System.Drawing.Point(12, 188);
            this.gbUpdateUserDefinedVariable.Name = "gbUpdateUserDefinedVariable";
            this.gbUpdateUserDefinedVariable.Size = new System.Drawing.Size(419, 229);
            this.gbUpdateUserDefinedVariable.TabIndex = 1;
            this.gbUpdateUserDefinedVariable.TabStop = false;
            this.gbUpdateUserDefinedVariable.Text = "Update the user-defined variable";
            // 
            // checkDeleteVariable
            // 
            this.checkDeleteVariable.AutoSize = true;
            this.checkDeleteVariable.Location = new System.Drawing.Point(31, 88);
            this.checkDeleteVariable.Name = "checkDeleteVariable";
            this.checkDeleteVariable.Size = new System.Drawing.Size(131, 16);
            this.checkDeleteVariable.TabIndex = 7;
            this.checkDeleteVariable.Text = "Delete this variable";
            this.checkDeleteVariable.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Image = global::WelsonJS.Launcher.Properties.Resources.icon_check_32;
            this.btnOk.Location = new System.Drawing.Point(303, 123);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(86, 86);
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
            this.btnOpenFile.Location = new System.Drawing.Point(31, 169);
            this.btnOpenFile.Name = "btnOpenFile";
            this.btnOpenFile.Padding = new System.Windows.Forms.Padding(11, 0, 0, 0);
            this.btnOpenFile.Size = new System.Drawing.Size(201, 40);
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
            this.btnOpenDirectory.Location = new System.Drawing.Point(31, 123);
            this.btnOpenDirectory.Name = "btnOpenDirectory";
            this.btnOpenDirectory.Padding = new System.Windows.Forms.Padding(11, 0, 0, 0);
            this.btnOpenDirectory.Size = new System.Drawing.Size(201, 40);
            this.btnOpenDirectory.TabIndex = 4;
            this.btnOpenDirectory.Text = "Open the directory...";
            this.btnOpenDirectory.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOpenDirectory.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOpenDirectory.UseVisualStyleBackColor = true;
            this.btnOpenDirectory.Click += new System.EventHandler(this.btnOpenDirectory_Click);
            // 
            // textSetValue
            // 
            this.textSetValue.Location = new System.Drawing.Point(98, 55);
            this.textSetValue.Name = "textSetValue";
            this.textSetValue.Size = new System.Drawing.Size(291, 21);
            this.textSetValue.TabIndex = 3;
            // 
            // textSetName
            // 
            this.textSetName.Location = new System.Drawing.Point(98, 28);
            this.textSetName.Name = "textSetName";
            this.textSetName.Size = new System.Drawing.Size(291, 21);
            this.textSetName.TabIndex = 2;
            // 
            // labelSetValue
            // 
            this.labelSetValue.AutoSize = true;
            this.labelSetValue.Location = new System.Drawing.Point(29, 59);
            this.labelSetValue.Name = "labelSetValue";
            this.labelSetValue.Size = new System.Drawing.Size(61, 12);
            this.labelSetValue.TabIndex = 1;
            this.labelSetValue.Text = "Set value:";
            // 
            // labelSetName
            // 
            this.labelSetName.AutoSize = true;
            this.labelSetName.Location = new System.Drawing.Point(29, 31);
            this.labelSetName.Name = "labelSetName";
            this.labelSetName.Size = new System.Drawing.Size(63, 12);
            this.labelSetName.TabIndex = 0;
            this.labelSetName.Text = "Set name:";
            // 
            // gbImportAndExport
            // 
            this.gbImportAndExport.Controls.Add(this.btnExport);
            this.gbImportAndExport.Controls.Add(this.btnImport);
            this.gbImportAndExport.Location = new System.Drawing.Point(12, 423);
            this.gbImportAndExport.Name = "gbImportAndExport";
            this.gbImportAndExport.Size = new System.Drawing.Size(419, 89);
            this.gbImportAndExport.TabIndex = 2;
            this.gbImportAndExport.TabStop = false;
            this.gbImportAndExport.Text = "Import and export";
            // 
            // btnExport
            // 
            this.btnExport.Image = global::WelsonJS.Launcher.Properties.Resources.icon_export_32;
            this.btnExport.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnExport.Location = new System.Drawing.Point(213, 29);
            this.btnExport.Name = "btnExport";
            this.btnExport.Padding = new System.Windows.Forms.Padding(11, 0, 0, 0);
            this.btnExport.Size = new System.Drawing.Size(176, 40);
            this.btnExport.TabIndex = 6;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnImport
            // 
            this.btnImport.Image = global::WelsonJS.Launcher.Properties.Resources.icon_import_32;
            this.btnImport.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnImport.Location = new System.Drawing.Point(31, 29);
            this.btnImport.Name = "btnImport";
            this.btnImport.Padding = new System.Windows.Forms.Padding(11, 0, 0, 0);
            this.btnImport.Size = new System.Drawing.Size(176, 40);
            this.btnImport.TabIndex = 5;
            this.btnImport.Text = "Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // EnvForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(447, 529);
            this.Controls.Add(this.gbImportAndExport);
            this.Controls.Add(this.gbUpdateUserDefinedVariable);
            this.Controls.Add(this.gbUserDefinedVariables);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::WelsonJS.Launcher.Properties.Resources.favicon;
            this.MaximizeBox = false;
            this.Name = "EnvForm";
            this.Text = "User-defined variables editor";
            this.gbUserDefinedVariables.ResumeLayout(false);
            this.gbUpdateUserDefinedVariable.ResumeLayout(false);
            this.gbUpdateUserDefinedVariable.PerformLayout();
            this.gbImportAndExport.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbUserDefinedVariables;
        private System.Windows.Forms.GroupBox gbUpdateUserDefinedVariable;
        private System.Windows.Forms.TextBox textSetValue;
        private System.Windows.Forms.TextBox textSetName;
        private System.Windows.Forms.Label labelSetValue;
        private System.Windows.Forms.Label labelSetName;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnOpenFile;
        private System.Windows.Forms.Button btnOpenDirectory;
        private System.Windows.Forms.CheckBox checkDeleteVariable;
        private System.Windows.Forms.ListView lvUserDefinedVariables;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.GroupBox gbImportAndExport;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Button btnExport;
    }
}