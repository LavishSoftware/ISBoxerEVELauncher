using System.Windows;

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

        public string ResponseBody
        {
            get; set;
        }

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
