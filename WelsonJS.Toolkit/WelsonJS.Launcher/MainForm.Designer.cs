namespace WelsonJS.Launcher
{
    partial class MainForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnRunFromZipFile = new System.Windows.Forms.Button();
            this.btnRunFromExternalLink = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.cbUseSpecificScript = new System.Windows.Forms.CheckBox();
            this.txtUseSpecificScript = new System.Windows.Forms.TextBox();
            this.cbInteractiveServiceApp = new System.Windows.Forms.CheckBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.userdefinedVariablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.instancesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runAsAdministratorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.globalSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startCodeEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openCopilotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openLauncherToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openCodeEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnRunFromZipFile
            // 
            this.btnRunFromZipFile.Image = global::WelsonJS.Launcher.Properties.Resources.icon_zip_128;
            this.btnRunFromZipFile.Location = new System.Drawing.Point(24, 67);
            this.btnRunFromZipFile.Name = "btnRunFromZipFile";
            this.btnRunFromZipFile.Size = new System.Drawing.Size(200, 200);
            this.btnRunFromZipFile.TabIndex = 0;
            this.btnRunFromZipFile.Text = "From ZIP file...";
            this.btnRunFromZipFile.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnRunFromZipFile.UseVisualStyleBackColor = true;
            this.btnRunFromZipFile.Click += new System.EventHandler(this.btnRunFromZipFile_Click);
            // 
            // btnRunFromExternalLink
            // 
            this.btnRunFromExternalLink.Image = global::WelsonJS.Launcher.Properties.Resources.icon_link_128;
            this.btnRunFromExternalLink.Location = new System.Drawing.Point(230, 67);
            this.btnRunFromExternalLink.Name = "btnRunFromExternalLink";
            this.btnRunFromExternalLink.Size = new System.Drawing.Size(200, 200);
            this.btnRunFromExternalLink.TabIndex = 1;
            this.btnRunFromExternalLink.Text = "From external link...";
            this.btnRunFromExternalLink.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnRunFromExternalLink.UseVisualStyleBackColor = true;
            this.btnRunFromExternalLink.Click += new System.EventHandler(this.btnRunFromExternalLink_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 41);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(312, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "Choose the location of WelsonJS application package.";
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(24, 345);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(219, 12);
            this.linkLabel1.TabIndex = 3;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "https://github.com/gnh1201/welsonjs";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // cbUseSpecificScript
            // 
            this.cbUseSpecificScript.AutoSize = true;
            this.cbUseSpecificScript.Location = new System.Drawing.Point(26, 281);
            this.cbUseSpecificScript.Name = "cbUseSpecificScript";
            this.cbUseSpecificScript.Size = new System.Drawing.Size(184, 16);
            this.cbUseSpecificScript.TabIndex = 4;
            this.cbUseSpecificScript.Text = "I want to use specific script ";
            this.cbUseSpecificScript.UseVisualStyleBackColor = true;
            this.cbUseSpecificScript.CheckedChanged += new System.EventHandler(this.cbUseSpecificScript_CheckedChanged);
            // 
            // txtUseSpecificScript
            // 
            this.txtUseSpecificScript.Enabled = false;
            this.txtUseSpecificScript.Location = new System.Drawing.Point(214, 278);
            this.txtUseSpecificScript.Name = "txtUseSpecificScript";
            this.txtUseSpecificScript.Size = new System.Drawing.Size(110, 21);
            this.txtUseSpecificScript.TabIndex = 5;
            // 
            // cbInteractiveServiceApp
            // 
            this.cbInteractiveServiceApp.AutoSize = true;
            this.cbInteractiveServiceApp.Location = new System.Drawing.Point(26, 305);
            this.cbInteractiveServiceApp.Name = "cbInteractiveServiceApp";
            this.cbInteractiveServiceApp.Size = new System.Drawing.Size(254, 16);
            this.cbInteractiveServiceApp.TabIndex = 6;
            this.cbInteractiveServiceApp.Text = "This is an Interactive Service Application";
            this.cbInteractiveServiceApp.UseVisualStyleBackColor = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(461, 24);
            this.menuStrip1.TabIndex = 7;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.userdefinedVariablesToolStripMenuItem,
            this.instancesToolStripMenuItem,
            this.runAsAdministratorToolStripMenuItem,
            this.globalSettingsToolStripMenuItem,
            this.startCodeEditorToolStripMenuItem,
            this.openCopilotToolStripMenuItem});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // userdefinedVariablesToolStripMenuItem
            // 
            this.userdefinedVariablesToolStripMenuItem.Name = "userdefinedVariablesToolStripMenuItem";
            this.userdefinedVariablesToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.userdefinedVariablesToolStripMenuItem.Text = "User-defined variables";
            this.userdefinedVariablesToolStripMenuItem.Click += new System.EventHandler(this.userdefinedVariablesToolStripMenuItem_Click);
            // 
            // instancesToolStripMenuItem
            // 
            this.instancesToolStripMenuItem.Name = "instancesToolStripMenuItem";
            this.instancesToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.instancesToolStripMenuItem.Text = "Instances";
            this.instancesToolStripMenuItem.Click += new System.EventHandler(this.instancesToolStripMenuItem_Click);
            // 
            // runAsAdministratorToolStripMenuItem
            // 
            this.runAsAdministratorToolStripMenuItem.Name = "runAsAdministratorToolStripMenuItem";
            this.runAsAdministratorToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.runAsAdministratorToolStripMenuItem.Text = "Run as Administrator...";
            this.runAsAdministratorToolStripMenuItem.Click += new System.EventHandler(this.runAsAdministratorToolStripMenuItem_Click);
            // 
            // globalSettingsToolStripMenuItem
            // 
            this.globalSettingsToolStripMenuItem.Name = "globalSettingsToolStripMenuItem";
            this.globalSettingsToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.globalSettingsToolStripMenuItem.Text = "Global settings...";
            this.globalSettingsToolStripMenuItem.Click += new System.EventHandler(this.globalSettingsToolStripMenuItem_Click);
            // 
            // startCodeEditorToolStripMenuItem
            // 
            this.startCodeEditorToolStripMenuItem.Name = "startCodeEditorToolStripMenuItem";
            this.startCodeEditorToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.startCodeEditorToolStripMenuItem.Text = "Start the code editor...";
            this.startCodeEditorToolStripMenuItem.Click += new System.EventHandler(this.startCodeEditorToolStripMenuItem_Click);
            // 
            // openCopilotToolStripMenuItem
            // 
            this.openCopilotToolStripMenuItem.Name = "openCopilotToolStripMenuItem";
            this.openCopilotToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.openCopilotToolStripMenuItem.Text = "Open the Copilot...";
            this.openCopilotToolStripMenuItem.Click += new System.EventHandler(this.openCopilotToolStripMenuItem_Click);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = global::WelsonJS.Launcher.Properties.Resources.favicon;
            this.notifyIcon1.Text = "WelsonJS Launcher";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openLauncherToolStripMenuItem,
            this.openCodeEditorToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(199, 70);
            // 
            // openLauncherToolStripMenuItem
            // 
            this.openLauncherToolStripMenuItem.Name = "openLauncherToolStripMenuItem";
            this.openLauncherToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.openLauncherToolStripMenuItem.Text = "Open the launcher...";
            // 
            // openCodeEditorToolStripMenuItem
            // 
            this.openCodeEditorToolStripMenuItem.Name = "openCodeEditorToolStripMenuItem";
            this.openCodeEditorToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.openCodeEditorToolStripMenuItem.Text = "Open the code editor...";
            this.openCodeEditorToolStripMenuItem.Click += new System.EventHandler(this.openCodeEditorToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(461, 382);
            this.Controls.Add(this.cbInteractiveServiceApp);
            this.Controls.Add(this.txtUseSpecificScript);
            this.Controls.Add(this.cbUseSpecificScript);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnRunFromExternalLink);
            this.Controls.Add(this.btnRunFromZipFile);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::WelsonJS.Launcher.Properties.Resources.favicon;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "WelsonJS Application Launcher";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnRunFromZipFile;
        private System.Windows.Forms.Button btnRunFromExternalLink;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.CheckBox cbUseSpecificScript;
        private System.Windows.Forms.TextBox txtUseSpecificScript;
        private System.Windows.Forms.CheckBox cbInteractiveServiceApp;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem userdefinedVariablesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem instancesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runAsAdministratorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem globalSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startCodeEditorToolStripMenuItem;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openCodeEditorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openLauncherToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openCopilotToolStripMenuItem;
    }
}

