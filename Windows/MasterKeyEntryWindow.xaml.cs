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
