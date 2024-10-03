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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EnvForm));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.checkDeleteVariable = new System.Windows.Forms.CheckBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnOpenFile = new System.Windows.Forms.Button();
            this.btnOpenDirectory = new System.Windows.Forms.Button();
            this.textSetValue = new System.Windows.Forms.TextBox();
            this.textSetName = new System.Windows.Forms.TextBox();
            this.labelSetValue = new System.Windows.Forms.Label();
            this.labelSetName = new System.Windows.Forms.Label();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.listView1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(419, 328);
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
            this.listView1.Location = new System.Drawing.Point(16, 26);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(386, 285);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.ListView1_SelectedIndexChanged);
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
            this.groupBox2.Location = new System.Drawing.Point(12, 346);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(419, 229);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Update the user-defined variable";
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
            this.btnOpenFile.Size = new System.Drawing.Size(201, 40);
            this.btnOpenFile.TabIndex = 5;
            this.btnOpenFile.Text = "Open the file...";
            this.btnOpenFile.UseVisualStyleBackColor = true;
            this.btnOpenFile.Click += new System.EventHandler(this.btnOpenFile_Click);
            // 
            // btnOpenDirectory
            // 
            this.btnOpenDirectory.Image = ((System.Drawing.Image)(resources.GetObject("btnOpenDirectory.Image")));
            this.btnOpenDirectory.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOpenDirectory.Location = new System.Drawing.Point(31, 123);
            this.btnOpenDirectory.Name = "btnOpenDirectory";
            this.btnOpenDirectory.Size = new System.Drawing.Size(201, 40);
            this.btnOpenDirectory.TabIndex = 4;
            this.btnOpenDirectory.Text = "Open the directory...";
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
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Value";
            // 
            // EnvForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(447, 596);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::WelsonJS.Launcher.Properties.Resources.favicon;
            this.MaximizeBox = false;
            this.Name = "EnvForm";
            this.Text = "User-defined variables editor";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
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
    }
}