using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for AddCharacterWindow.xaml
    /// </summary>
    public partial class AddCharacterWindow : Window
    {
        public AddCharacterWindow(EVECharacter editCharacter)
        {
            Character = editCharacter;
            InitializeComponent();
        }

        public string CharacterName
        {
            get { return Character.Name; }
            set { Character.Name = value; }
        }

        public EVECharacter Character { get; set; }

        public EVEAccount Account
        {
            get { return Character.EVEAccount; }
            set { Character.EVEAccount = value; }
        }

        public bool UseSingularity
        {
            get { return Character.UseSingularity; }
            set { Character.UseSingularity = value; }
        }

        public ObservableCollection<EVEAccount> Accounts
        { 
            get
            {
                return App.Settings.Accounts;
            }
        }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            // validate...
            if (string.IsNullOrEmpty(CharacterName))
            {
                MessageBox.Show("An EVE Character name is required!");
                return;
            }

            if (Account == null)
            {
                MessageBox.Show("An EVE Account is required!");
                return;
            }

            // grab character ID ...
            if (Character.GetCharacterID() == 0)
            {
                MessageBox.Show("Invalid EVE Character -- Failed to find Character ID via ESI");
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
