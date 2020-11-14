namespace ISBoxerEVELauncher.Windows
{
    partial class EVELoginBrowser
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EVELoginBrowser));
            this.webBrowser_EVE = new System.Windows.Forms.WebBrowser();
            this.toolStrip_Main = new System.Windows.Forms.ToolStrip();
            this.toolStripTextBox_Addressbar = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripButton_Refresh = new System.Windows.Forms.ToolStripButton();
            this.toolStrip_Main.SuspendLayout();
            this.SuspendLayout();
            // 
            // webBrowser_EVE
            // 
            this.webBrowser_EVE.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser_EVE.Location = new System.Drawing.Point(0, 39);
            this.webBrowser_EVE.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser_EVE.Name = "webBrowser_EVE";
            this.webBrowser_EVE.Size = new System.Drawing.Size(767, 556);
            this.webBrowser_EVE.TabIndex = 0;
            this.webBrowser_EVE.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowser_EVE_DocumentCompleted);
            this.webBrowser_EVE.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(this.webBrowser_EVE_Navigated);
            // 
            // toolStrip_Main
            // 
            this.toolStrip_Main.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBox_Addressbar,
            this.toolStripButton_Refresh});
            this.toolStrip_Main.Location = new System.Drawing.Point(0, 0);
            this.toolStrip_Main.Name = "toolStrip_Main";
            this.toolStrip_Main.Size = new System.Drawing.Size(767, 39);
            this.toolStrip_Main.TabIndex = 2;
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
            // EVELoginBrowser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(767, 595);
            this.Controls.Add(this.webBrowser_EVE);
            this.Controls.Add(this.toolStrip_Main);
            this.Name = "EVELoginBrowser";
            this.Text = "EVELoginBrowser";
            this.Resize += new System.EventHandler(this.EVELoginBrowser_Resize);
            this.toolStrip_Main.ResumeLayout(false);
            this.toolStrip_Main.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.WebBrowser webBrowser_EVE;
        private System.Windows.Forms.ToolStrip toolStrip_Main;
        public System.Windows.Forms.ToolStripTextBox toolStripTextBox_Addressbar;
        private System.Windows.Forms.ToolStripButton toolStripButton_Refresh;
    }
}