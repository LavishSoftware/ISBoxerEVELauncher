using System.Windows;

namespace ISBoxerEVELauncher.Windows
{
    /// <summary>
    /// Interaction logic for MasterKeyEntryWindow.xaml
    /// </summary>
    public partial class MasterKeyEntryWindow : Window
    {
        public MasterKeyEntryWindow()
        {
            InitializeComponent();
        }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (App.Settings.TryMasterPassword(passwordMasterKey.SecurePassword))
            {
                DialogResult = true;
                this.Close();
                return;
            }

            MessageBox.Show("Invalid Master Password! To use saved EVE Account passwords, you MUST enter the correct Master Password!");
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Master Password entry cancelled. If you wish to reset the Master Password and clear saved EVE Account passwords, un-check 'Save passwords (secure)'");
            e.Handled = true;
            DialogResult = false;
            this.Close();
        }
    }
}
