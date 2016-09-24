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
    /// Interaction logic for CreateGameProfileWindow.xaml
    /// </summary>
    public partial class CreateGameProfileWindow : Window
    {
        public CreateGameProfileWindow(bool sisi, string defaultGame, string defaultGameProfile)
        {
            Game = defaultGame;
            GameProfile = defaultGameProfile;
            InitializeComponent();

            if (sisi)
                Title += " (Singularity)";
            else
                Title += " (Tranquility)";
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

            if (string.IsNullOrWhiteSpace(Game) || string.IsNullOrWhiteSpace(GameProfile))
            {
                MessageBox.Show("Both 'Game' and 'Game Profile' names are required!");
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
