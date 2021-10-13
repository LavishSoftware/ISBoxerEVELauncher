using ISBoxerEVELauncher.Games.EVE;
using System.Windows;

namespace ISBoxerEVELauncher.Windows
{
    /// <summary>
    /// Interaction logic for AuthenticatorChallengeWindow.xaml
    /// </summary>
    public partial class AuthenticatorChallengeWindow : Window
    {
        EVEAccount Account;
        public AuthenticatorChallengeWindow(EVEAccount account)
        {
            Account = account;
            InitializeComponent();
        }

        public string AccountName
        {
            get
            {
                return Account.Username;
            }
            set
            {
            }
        }

        string _AuthenticatorCode;
        public string AuthenticatorCode
        {
            get
            {
                return _AuthenticatorCode;
            }
            set
            {
                _AuthenticatorCode = value;
            }
        }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            AuthenticatorCode = textAuthenticatorCode.Text;
            if (string.IsNullOrEmpty(AuthenticatorCode))
            {
                MessageBox.Show("Please enter a valid Authenticator Code to continue logging into this EVE Account!");
                return;
            }

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
