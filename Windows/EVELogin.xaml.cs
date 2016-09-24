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
    /// Interaction logic for EVELogin.xaml
    /// </summary>
    public partial class EVELogin : Window
    {
        public EVEAccount Account { get; private set; }
        public EVELogin(EVEAccount account, bool readOnly)
        {
            Account = account;
            this.DataContext = account;
            InitializeComponent();

            textAccountName.IsReadOnly = readOnly;
            if (readOnly)
                this.textPassword.Focus();
            else
                this.textAccountName.Focus();

        }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            // validate...
            if (string.IsNullOrEmpty(Account.Username))
            {
                MessageBox.Show("An EVE Account Name is required!");
                return;
            }

            if (textPassword.SecurePassword.Length==0)
            {
                MessageBox.Show("A Password is required!");
                return;
            }

            Account.SecurePassword = textPassword.SecurePassword.Copy();
            Account.EncryptPassword();
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
