namespace ISBoxerEVELauncher.Windows
{
    partial class LoginBrowser
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginBrowser));
            this.chromiumWebBrowser = new CefSharp.WinForms.ChromiumWebBrowser();
            this.toolStrip_Main = new System.Windows.Forms.ToolStrip();
            this.toolStripTextBox_Addressbar = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripButton_Refresh = new System.Windows.Forms.ToolStripButton();
            this.toolStrip_Main.SuspendLayout();
            this.SuspendLayout();
            // 
            // chromiumWebBrowser
            // 
            this.chromiumWebBrowser.ActivateBrowserOnCreation = false;
            this.chromiumWebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chromiumWebBrowser.Location = new System.Drawing.Point(0, 39);
            this.chromiumWebBrowser.Name = "chromiumWebBrowser";
            this.chromiumWebBrowser.Size = new System.Drawing.Size(804, 592);
            this.chromiumWebBrowser.TabIndex = 0;
            this.chromiumWebBrowser.FrameLoadEnd += new System.EventHandler<CefSharp.FrameLoadEndEventArgs>(this.chromiumWebBrowser_FrameLoadEnd);
            this.chromiumWebBrowser.AddressChanged += new System.EventHandler<CefSharp.AddressChangedEventArgs>(this.chromiumWebBrowser_AddressChanged);
            // 
            // toolStrip_Main
            // 
            this.toolStrip_Main.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBox_Addressbar,
            this.toolStripButton_Refresh});
            this.toolStrip_Main.Location = new System.Drawing.Point(0, 0);
            this.toolStrip_Main.Name = "toolStrip_Main";
            this.toolStrip_Main.Size = new System.Drawing.Size(804, 39);
            this.toolStrip_Main.TabIndex = 1;
            this.toolStrip_Main.Text = "toolStrip1";
            // 
            // toolStripTextBox_Addressbar
            // 
            this.toolStripTextBox_Addressbar.Enabled = false;
            this.toolStripTextBox_Addressbar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.toolStripTextBox_Addressbar.Name = "toolStripTextBox_Addressbar";
            this.toolStripTextBox_Addressbar.Size = new System.Drawing.Size(700, 39);
            // 
            // toolStripButton_Refresh
            // 
            this.toolStripButton_Refresh.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButton_Refresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton_Refresh.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton_Refresh.Image")));
            this.toolStripButton_Refresh.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButton_Refresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_Refresh.Name = "toolStripButton_Refresh";
            this.toolStripButton_Refresh.Size = new System.Drawing.Size(36, 36);
            this.toolStripButton_Refresh.Text = "Refresh";
            this.toolStripButton_Refresh.Click += new System.EventHandler(this.toolStripButton_Refresh_Click);
            // 
            // LoginBrowser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(804, 631);
            this.Controls.Add(this.chromiumWebBrowser);
            this.Controls.Add(this.toolStrip_Main);
            this.Name = "LoginBrowser";
            this.Text = "EVE Online Login";
            this.Resize += new System.EventHandler(this.LoginBrowser_Resize);
            this.toolStrip_Main.ResumeLayout(false);
            this.toolStrip_Main.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public CefSharp.WinForms.ChromiumWebBrowser chromiumWebBrowser;
        private System.Windows.Forms.ToolStrip toolStrip_Main;
        public System.Windows.Forms.ToolStripTextBox toolStripTextBox_Addressbar;
        private System.Windows.Forms.ToolStripButton toolStripButton_Refresh;
    }
}