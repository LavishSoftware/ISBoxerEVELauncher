using System.Windows;

namespace ISBoxerEVELauncher.Windows
{
    /*

<!DOCTYPE html>
<!--[if lt IE 7]> <html class="no-js lt-ie9 lt-ie8 lt-ie7"> <![endif]-->
<!--[if IE 7]>    <html class="no-js lt-ie9 lt-ie8"> <![endif]-->
<!--[if IE 8]>    <html class="no-js lt-ie9"> <![endif]-->
<!--[if gt IE 8]><!--> <html class="no-js"> <!--<![endif]-->
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="initial-scale=1.0, width=device-width, maximum-scale=1.0"/>
    <title>Email verification required</title>
    <link rel="stylesheet" href="//web.ccpgamescdn.com/shared/webfonts/proxima/webfont.css">
    <link rel="stylesheet" href="//web.ccpgamescdn.com/shared/webfonts/fontawesome/fontawesome.css">
    <link href="/Content/v-636155053260000000/Site.css" rel="stylesheet" type="text/css" />
    <link href="/Content/themes/eveLauncherTQ/v-636155053260000000/style.css" rel="stylesheet" type="text/css" />
    <link rel="shortcut icon" href="/Images/favicons/v-636155053260000000/favicon.ico" />
    <script src="/Scripts/v-636155053280000000/jquery-2.1.0.min.js" type="text/javascript"></script>
    <script src="/Scripts/v-636155053280000000/jquery-ui-1.10.4.min.js" type="text/javascript"></script>
    <script src="/Scripts/v-636155053280000000/modernizr-2.5.3.js" type="text/javascript"></script> 
    <script type="text/javascript">document.domain = "eveonline.com";</script>
</head>
    <body id="loginbody" class="en">
        <div id="container">
            <header>
                <a href="#" class="logo">
                        <img src="/Images/eve.png" />
                </a>
            </header>
            <section id="main">
                

<div class="difficulties">
    <h3>Email verification required</h3>
    <p>A confirmation email has been sent to lax@lavishsoft.com. If you do not receive the email within 30 minutes, please contact Customer Support.</p>
</div>
            </section>
            <footer>
            </footer>
        </div>
        
    </body>
</html>
     * */
    /// <summary>
    /// Interaction logic for EmailChallengeWindow.xaml
    /// </summary>
    public partial class EmailChallengeWindow : Window
    {
        public static string RemoveScriptTags(string html)
        {
            do
            {
                if (string.IsNullOrEmpty(html))
                    return html;

                int startIndex = html.IndexOf("<script");
                if (startIndex < 0)
                    return html;

                int endIndex = html.IndexOf("</script>");
                if (endIndex < 0)
                    return html;

                html = html.Remove(startIndex, endIndex - startIndex + "</script>".Length);
            }
            while (true);
        }

        public EmailChallengeWindow(string body)
        {
            InitializeComponent();
            // A confirmation email has been sent to lax@lavishsoft.com. If you do not receive the email within 30 minutes, please contact Customer Support.

            /*
            body = RemoveScriptTags(body);
            body = body.Replace("href=\"//", "href=\"https://");
            body = body.Replace("href=\"/", "href=\"https://login.eveonline.com/");
            body = body.Replace("src=\"/", "src=\"https://login.eveonline.com/");
            //body = body.Replace("\"/Images/eve.png\"", "\"https://login.eveonline.com/Images/eve.png\"");
            this.webBrowser.NavigateToString(body);
            /**/

            string header = @"<html><head>
    <meta charset=""utf-8"" />
    <title>Email verification required</title>
</head>
";
            int startIndex = body.IndexOf("<section");
            int endIndex = body.IndexOf("</section>");



            string sectionOnly = body.Substring(startIndex, (endIndex - startIndex) + "</section>".Length);
            this.webBrowser.NavigateToString(header + sectionOnly);
            /**/
        }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            DialogResult = true;
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            DialogResult = false;
            this.Close();
        }
    }
}
