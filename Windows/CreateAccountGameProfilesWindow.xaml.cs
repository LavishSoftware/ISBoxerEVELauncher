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
    /// Interaction logic for CreateAccountGameProfilesWindow.xaml
    /// </summary>
    public partial class CreateAccountGameProfilesWindow : Window
    {
        public CreateAccountGameProfilesWindow(string defaultGame, string defaultGameProfile)
        {
            Game = defaultGame;
            GameProfile = defaultGameProfile;
            UseNewLauncher = true;
            LeaveLauncherOpen = false;
            InitializeComponent();
            SelectedItem = cbiEVEDirect;
            
        }

        bool _UseNewLauncher;
        public bool UseNewLauncher
        {
            get
            {
                return _UseNewLauncher;
            }
            set
            {
                _UseNewLauncher = value;
            }
        }

        bool _LeaveLauncherOpen;
        public bool LeaveLauncherOpen
        {
            get
            {
                return _LeaveLauncherOpen;
            }
            set
            {
                _LeaveLauncherOpen = value;
            }
        }

        public bool UseEVEDirect
        {
            get
            {
                return SelectedItem == cbiEVEDirect;
            }            
        }

        ComboBoxItem _SelectedItem;
        public ComboBoxItem SelectedItem
        {
            get
            {
                return _SelectedItem;
            }
            set
            {
                _SelectedItem = value;
            }
        }

        string _Game;
        public string Game
        {
            get { return _Game; }
            set { _Game = value; }
        }

        string _GameProfile;
        public string GameProfile
        {
            get { return _GameProfile; }
            set { _GameProfile = value; }
        }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (string.IsNullOrWhiteSpace(Game))
            {
                MessageBox.Show("Please enter a Game name before continuing.");
                return;
            }
            if (string.IsNullOrWhiteSpace(GameProfile))
            {
                MessageBox.Show("Please enter a Game Profile scheme before continuing. We recommend \"ISBEL - {0}\"");
                return;
            }

            if (SelectedItem==null)
            {
                MessageBox.Show("Please select a Launch method via the drop-down box before continuing.");
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
