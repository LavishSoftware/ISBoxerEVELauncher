﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ISBoxerEVELauncher.Windows
{
    /// <summary>
    /// Interaction logic for CreateAccountGameProfilesWindow.xaml
    /// </summary>
    public partial class CreateAccountGameProfilesWindow : Window, INotifyPropertyChanged
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
                if (UseISBoxerSettings)
                    return true;
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
                if (UseISBoxerSettings)
                    return false;
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
                if (UseISBoxerSettings)
                    return true;
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
                OnPropertyChanged("SelectedItem");
            }
        }

        bool _UseISBoxerSettings = true;
        public bool UseISBoxerSettings
        {
            get
            {
                return _UseISBoxerSettings;
            }
            set
            {
                _UseISBoxerSettings = value;
                OnPropertyChanged("UseISBoxerSettings");
                OnPropertyChanged("UseAdvancedSettings");
                OnPropertyChanged("AdvancedVisibility");

                OnPropertyChanged("SelectedItem");
                OnPropertyChanged("UseEVEDirect");
                OnPropertyChanged("LeaveLauncherOpen");
                OnPropertyChanged("UseNewLauncher");
            }
        }

        public bool UseAdvancedSettings
        {
            get
            {
                return !UseISBoxerSettings;
            }
            set
            {
                UseISBoxerSettings = !value;
            }
        }

        public Visibility AdvancedVisibility
        {
            get
            {
                if (UseISBoxerSettings)
                    return System.Windows.Visibility.Collapsed;
                return System.Windows.Visibility.Visible;
            }
            set
            {

            }
        }


        string _Game;
        public string Game
        {
            get
            {
                return _Game;
            }
            set
            {
                _Game = value;
            }
        }

        string _GameProfile;
        public string GameProfile
        {
            get
            {
                return _GameProfile;
            }
            set
            {
                _GameProfile = value;
            }
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

            if (SelectedItem == null)
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

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void FirePropertyChanged(string value)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(value));
            }
        }
        public void OnPropertyChanged(string value)
        {
            FirePropertyChanged(value);
        }
        #endregion
    }
}
