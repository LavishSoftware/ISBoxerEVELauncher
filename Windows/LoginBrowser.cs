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
        public string strCurrentAddress { get; set; }
        public string strHTML_RequestVerificationToken { get; set; }
        public string strURL_RequestVerificationToken { get; set; }
        public string strHTML_VerficationCode { get; set; }
        public string strURL_VerficationCode { get; set; }
        public string strHTML_Result { get; set; }
        public string strURL_Result { get; set; }

        public LoginBrowser()
        {
            InitializeComponent();
            Clearup();
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
