using ISBoxerEVELauncher.Extensions;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Web;
using System.Windows.Forms;
using ISBoxerEVELauncher.Web;

namespace ISBoxerEVELauncher.Windows
{
    public partial class EVELoginBrowser : Form
    {
        public string strCurrentAddress
        {
            get; set;
        }
        public string strHTML_RequestVerificationToken
        {
            get; set;
        }
        public string strURL_RequestVerificationToken
        {
            get; set;
        }
        public string strHTML_VerficationCode
        {
            get; set;
        }
        public string strURL_VerficationCode
        {
            get; set;
        }
        public string strHTML_Result
        {
            get; set;
        }
        public string strURL_Result
        {
            get; set;
        }

        public EVELoginBrowser()
        {
            int BrowserVer, RegVal;

            // get the installed IE version
            using (WebBrowser Wb = new WebBrowser())
                BrowserVer = Wb.Version.Major;

            // set the appropriate IE version
            if (BrowserVer >= 11)
                RegVal = 11001;
            else if (BrowserVer == 10)
                RegVal = 10001;
            else if (BrowserVer == 9)
                RegVal = 9999;
            else if (BrowserVer == 8)
                RegVal = 8888;
            else
                RegVal = 7000;

            // set the actual key
            using (RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", RegistryKeyPermissionCheck.ReadWriteSubTree))
                if (Key.GetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe") == null)
                    Key.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe", RegVal, RegistryValueKind.DWord);

            InitializeComponent();

            webBrowser_EVE.ScriptErrorsSuppressed = true;
            Clearup();
            toolStripTextBox_Addressbar.Size = new Size(toolStrip_Main.Size.Width - toolStripButton_Refresh.Size.Width - 20, toolStripTextBox_Addressbar.Size.Height);
        }

        public void Clearup()
        {
            strCurrentAddress = "";
            strHTML_RequestVerificationToken = "";
            strURL_RequestVerificationToken = "";
            strHTML_VerficationCode = "";
            strURL_VerficationCode = "";
            strHTML_Result = "";
            strURL_Result = "";
        }

        private void webBrowser_EVE_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            ContentAnalyse();
        }

        private void webBrowser_EVE_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            strCurrentAddress = e.Url.ToString();
            this.InvokeOnUiThreadIfRequired(() => toolStripTextBox_Addressbar.Text = e.Url.ToString());
            ContentAnalyse();
        }

        private void ContentAnalyse()
        {
            if (webBrowser_EVE.ReadyState == WebBrowserReadyState.Complete)
            {
                const string needle = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
                int hashStart = webBrowser_EVE.DocumentText.IndexOf(needle, StringComparison.Ordinal);

                if (hashStart != -1)
                {
                    strHTML_RequestVerificationToken = webBrowser_EVE.DocumentText;
                    strURL_RequestVerificationToken = strCurrentAddress;
                }

                if (webBrowser_EVE.DocumentText.Contains("Log in to your account") && !webBrowser_EVE.DocumentText.Contains("Invalid username / password"))
                {
                    webBrowser_EVE.Document.GetElementById("UserName").SetAttribute("value", App.strUserName);
                    webBrowser_EVE.Document.GetElementById("Password").SetAttribute("value", App.strPassword);
                    webBrowser_EVE.Document.GetElementById("RememberMe").InvokeMember("click");

                    var cookies = BrowserCookie.GetCookieInternal(webBrowser_EVE.Url, false);

                    string[] strCookies = HttpContext.Current.Response.Cookies.AllKeys;

                    webBrowser_EVE.Document.Forms["loginForm"].InvokeMember("submit");
                }
                else if (webBrowser_EVE.DocumentText.Contains("Be sure to click the prompt above to login to the EVE Online launcher") || webBrowser_EVE.DocumentText.Contains("{\"access_token\":\""))
                {
                    strHTML_Result = webBrowser_EVE.DocumentText;
                    strURL_Result = strCurrentAddress;
                    this.InvokeOnUiThreadIfRequired(() => this.Close());
                }
                else if (webBrowser_EVE.DocumentText.Contains("Please enter the verification code"))
                {
                    strHTML_VerficationCode = webBrowser_EVE.DocumentText;
                    strURL_VerficationCode = strCurrentAddress;
                }
                else if (webBrowser_EVE.DocumentText.Equals("<html><head></head><body></body></html>"))
                {
                    this.InvokeOnUiThreadIfRequired(() => this.Close());
                }
                else if (webBrowser_EVE.DocumentText.Contains("Please stand by, while we are checking your browser..."))
                {
                    webBrowser_EVE.Navigate(toolStripTextBox_Addressbar.Text);
                }
            }
        }

        private void EVELoginBrowser_Resize(object sender, EventArgs e)
        {
            toolStripTextBox_Addressbar.Size = new Size(toolStrip_Main.Size.Width - toolStripButton_Refresh.Size.Width - 20, toolStripTextBox_Addressbar.Size.Height);
        }

        private void toolStripButton_Refresh_Click(object sender, EventArgs e)
        {
            webBrowser_EVE.Navigate(toolStripTextBox_Addressbar.Text);
        }

        private void webBrowser_EVE_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.ToString().Contains("eveonline://callback"))
            {
                e.Cancel = true;
            }
        }
    }
}
