using ISBoxerEVELauncher.Games.EVE;
using System.Windows;

namespace ISBoxerEVELauncher.Windows
{
    /// <summary>
    /// Interaction logic for CharacterChallengeWindow.xaml
    /// </summary>
    public partial class CharacterChallengeWindow : Window
    {
        EVEAccount Account;
        public CharacterChallengeWindow(EVEAccount account)
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

        string _CharacterName;
        public string CharacterName
        {
            get
            {
                return _CharacterName;
            }
            set
            {
                _CharacterName = value;
            }
        }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (string.IsNullOrEmpty(CharacterName))
            {
                MessageBox.Show("Please enter a valid Character Name to continue logging into this EVE Account!");
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
