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
    /// Interaction logic for UnhandledResponseWindow.xaml
    /// </summary>
    public partial class UnhandledResponseWindow : Window
    {
        public UnhandledResponseWindow(string responseBody)
        {
            ResponseBody = responseBody;
            InitializeComponent();
        }

        public string ResponseBody { get; set; }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ResponseBody);
        }
    }
}
