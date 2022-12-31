using ISBoxerEVELauncher.Games.EVE;
using System.Windows;

namespace ISBoxerEVELauncher.Windows
{
    /// <summary>
    /// Interaction logic for VerificationCodeChallengeWindow.xaml
    /// </summary>
    public partial class VerificationCodeChallengeWindow : Window
    {
        EVEAccount Account;
        public VerificationCodeChallengeWindow(EVEAccount account)
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

        string _VerificationCode;
        public string VerificationCode
        {
            get
            {
                return _VerificationCode;
            }
            set
            {
                _VerificationCode = value;
            }
        }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            VerificationCode = textVerificationCode.Text;
            if (string.IsNullOrEmpty(VerificationCode))
            {
                MessageBox.Show("Please enter a valid Verification Code to continue logging into this EVE Account!");
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
