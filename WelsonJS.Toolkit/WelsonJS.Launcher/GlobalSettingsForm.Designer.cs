namespace WelsonJS.Launcher
{
    partial class GlobalSettingsForm
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
            this.gbMaxScriptStatements = new System.Windows.Forms.GroupBox();
            this.btnOkMaxScriptStatements = new System.Windows.Forms.Button();
            this.txtMaxScriptStatements = new System.Windows.Forms.TextBox();
            this.gbMaxScriptStatements.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbMaxScriptStatements
            // 
            this.gbMaxScriptStatements.Controls.Add(this.btnOkMaxScriptStatements);
            this.gbMaxScriptStatements.Controls.Add(this.txtMaxScriptStatements);
            this.gbMaxScriptStatements.Location = new System.Drawing.Point(12, 12);
            this.gbMaxScriptStatements.Name = "gbMaxScriptStatements";
            this.gbMaxScriptStatements.Size = new System.Drawing.Size(290, 67);
            this.gbMaxScriptStatements.TabIndex = 0;
            this.gbMaxScriptStatements.TabStop = false;
            this.gbMaxScriptStatements.Text = "MaxScriptStatements (GUI only)";
            // 
            // btnOkMaxScriptStatements
            // 
            this.btnOkMaxScriptStatements.Location = new System.Drawing.Point(218, 30);
            this.btnOkMaxScriptStatements.Name = "btnOkMaxScriptStatements";
            this.btnOkMaxScriptStatements.Size = new System.Drawing.Size(57, 21);
            this.btnOkMaxScriptStatements.TabIndex = 1;
            this.btnOkMaxScriptStatements.Text = "Ok";
            this.btnOkMaxScriptStatements.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOkMaxScriptStatements.UseVisualStyleBackColor = true;
            this.btnOkMaxScriptStatements.Click += new System.EventHandler(this.btnOkMaxScriptStatements_Click);
            // 
            // txtMaxScriptStatements
            // 
            this.txtMaxScriptStatements.Location = new System.Drawing.Point(15, 30);
            this.txtMaxScriptStatements.Name = "txtMaxScriptStatements";
            this.txtMaxScriptStatements.Size = new System.Drawing.Size(197, 21);
            this.txtMaxScriptStatements.TabIndex = 1;
            // 
            // GlobalSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(315, 92);
            this.Controls.Add(this.gbMaxScriptStatements);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::WelsonJS.Launcher.Properties.Resources.favicon;
            this.MaximizeBox = false;
            this.Name = "GlobalSettingsForm";
            this.Text = "Global settings...";
            this.gbMaxScriptStatements.ResumeLayout(false);
            this.gbMaxScriptStatements.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbMaxScriptStatements;
        private System.Windows.Forms.TextBox txtMaxScriptStatements;
        private System.Windows.Forms.Button btnOkMaxScriptStatements;
    }
}