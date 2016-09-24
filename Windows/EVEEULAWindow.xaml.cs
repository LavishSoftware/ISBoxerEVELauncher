using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ISBoxerEVELauncher.Windows
{
    /// <summary>
    /// Interaction logic for EVEEULAWindow.xaml
    /// </summary>
    public partial class EVEEULAWindow : Window
    {
        public EVEEULAWindow(string EULABody)
        {
            InitializeComponent();

            int startIndex = EULABody.IndexOf("<div class=\"eula\">");
            int endIndex = EULABody.IndexOf("<div class=\"submit\">");

            string header = @"<head>
    <meta charset=""utf-8"" />
    <title>License Agreement Update</title>
</head>
";
            string eulaOnly = EULABody.Substring(startIndex, endIndex - startIndex);

            this.webBrowser.NavigateToString(header + eulaOnly);
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
