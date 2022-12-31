using System.Windows;

namespace ISBoxerEVELauncher.Windows
{
    /// <summary>
    /// Interaction logic for SecurityWarningWindow.xaml
    /// </summary>
    public partial class SecurityWarningWindow : Window
    {
        public string URI
        {
            get; set;
        }

        public SecurityWarningWindow(string responseBody)
        {
            InitializeComponent();

            responseBody = EmailChallengeWindow.RemoveScriptTags(responseBody);

            this.webBrowser.NavigateToString(responseBody);
            webBrowser.Navigating += WebBrowser_Navigating;
        }

        private void WebBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            URI = e.Uri.PathAndQuery;
            if (string.IsNullOrEmpty(e.Uri.Host))
            {
                // this is the Continue URL
                e.Cancel = true;
                this.Close();
            }
            else
            {
                // this is an external URL, we will open in browser
                e.Cancel = true;
                System.Diagnostics.Process.Start(e.Uri.ToString());
            }
        }
    }
}
