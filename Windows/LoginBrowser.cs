using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using ISBoxerEVELauncher.Extensions;

namespace ISBoxerEVELauncher.Windows
{
    public partial class LoginBrowser : Form
    {
        public string strCurrentAddress = "";
        public string strHTML_RequestVerificationToken = "";
        public string strURL_RequestVerificationToken = "";
        public string strHTML_VerficationCode = "";
        public string strURL_VerficationCode = "";
        public string strHTML_Result = "";
        public string strURL_Result = "";

        public LoginBrowser()
        {
            InitializeComponent();
        }

        private void chromiumWebBrowser_AddressChanged(object sender, CefSharp.AddressChangedEventArgs e)
        {
            strCurrentAddress = e.Address;
        }

        private void chromiumWebBrowser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            //chromiumWebBrowser.ViewSource();

            chromiumWebBrowser.GetSourceAsync().ContinueWith(taskHtml =>
            {

                const string needle = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
                int hashStart = taskHtml.Result.IndexOf(needle, StringComparison.Ordinal);
                if (hashStart == -1)
                {

                }
                else
                {
                    strHTML_RequestVerificationToken = taskHtml.Result;
                    strURL_RequestVerificationToken = strCurrentAddress;
                }
                
                if (taskHtml.Result.Contains("Be sure to click the prompt above to login to the EVE Online launcher"))
                {
                    strHTML_Result = taskHtml.Result;
                    strURL_Result = strCurrentAddress;
                }
                else if (taskHtml.Result.Contains("Please enter the verification code"))
                {
                    strHTML_VerficationCode = taskHtml.Result;
                    strURL_VerficationCode = strCurrentAddress;
                }
                else if (taskHtml.Result.Equals("<html><head></head><body></body></html>"))
                {
                    this.InvokeOnUiThreadIfRequired(() => this.Close());
                }

            });
        }


    }


}
