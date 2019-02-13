using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using ISBoxerEVELauncher.Launchers;
using ISBoxerEVELauncher.Enums;

namespace ISBoxerEVELauncher.Windows
{

    /// <summary>
    /// Interaction logic for LaunchProgressWindow.xaml
    /// </summary>
    public partial class LaunchProgressWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<ILaunchTarget> Accounts { get; set; }

        public ObservableCollection<ILaunchTarget> AccountsLaunched { get; set; }

        float _DelaySeconds;
        public float DelaySeconds
        {
            get
            {
                return _DelaySeconds;
            }
            set
            {
                _DelaySeconds = value;
                OnPropertyChanged("DelaySeconds");
            }
        }

        public bool AutoClose { get; set; }

        DateTime LastLaunch = DateTime.MinValue;

        public ILauncher Launcher { get; set; }

        public int NumErrors { get; set; }

        public LaunchProgressWindow(IEnumerable<ILaunchTarget> accounts, ILauncher launcher)
        {
            Accounts = new ObservableCollection<ILaunchTarget>(accounts);
            AccountsLaunched = new ObservableCollection<ILaunchTarget>();
            Launcher = launcher;
            AutoClose = true;
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Timer = new System.Windows.Threading.DispatcherTimer();
            Timer.Tick += Timer_Tick;
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 25); // 25ms
            Timer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (Timer != null)
            {
                Timer.Stop();
                Timer = null;
            }
        }

        public void Stop()
        {
            if (Timer != null)
            {
                Timer.Stop();
                Timer = null;
            }
            if (AutoClose && NumErrors==0)
                this.Close();
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            if (Accounts.Count==0)
            {
                Stop();
                return;
            }


            // determine if we're waiting on master key transfer...
            if (App.Settings.UseMasterKey && !App.Settings.HasPasswordMasterKey)
            {
                TimeSpan sinceKeyRequested = DateTime.Now - App.Settings.MasterKeyRequested;
                if (sinceKeyRequested.TotalSeconds < 2)
                {
                    // waiting ...
                    DelaySeconds = (float)Math.Truncate((2 - (float)sinceKeyRequested.TotalSeconds) * 100.0f) / 100.0f;
                    return;
                }
            }

            TimeSpan SinceLastLaunch = DateTime.Now - LastLaunch;
            if (SinceLastLaunch.TotalSeconds < App.Settings.LaunchDelay)
            {
                // waiting ...
                DelaySeconds = (float)Math.Truncate((App.Settings.LaunchDelay - (float)SinceLastLaunch.TotalSeconds) * 100.0f) / 100.0f;
                return;
            }
            else
            {
                DelaySeconds = 0;
            }


            ILaunchTarget a = Accounts[0];
            LoginResult lr = LoginResult.Error;
            try
            {            
                 lr = Launcher.Launch(a);
            }
            catch(ArgumentNullException ane)
            {
                switch(ane.ParamName)
                {
                    case "sharedCachePath":
                        {
                            AddDetailsLine("Missing EVE SharedCachePath. Aborting!");
                        }
                        break;
                    case "ssoToken":
                        {
                            AddDetailsLine("Failed to retrieve SSO Token from EVE servers. Aborting!");
                        }
                        break;
                    case "gameName":
                        {
                            AddDetailsLine("Missing appropriate Game Profile. Aborting!");
                        }
                        break;
                    case "gameProfileName":
                        {
                            AddDetailsLine("Missing appropriate Game Profile. Aborting!");
                        }
                        break;
                    default:
                        AddDetailsLine(ane.ToString());
                        break;
                }
            }
            catch(Exception ex)
            {
                AddDetailsLine(ex.ToString());
            }
                switch(lr)
                {
                    case LoginResult.Success:
                        AccountsLaunched.Add(a);
                        Accounts.Remove(a);
                        LastLaunch = DateTime.Now;
                        AddDetailsLine("Account '"+a.EVEAccount.Username+"' launched");
                        break;
                    default:
                        AddDetailsLine("Account '" + a.EVEAccount.Username + "' failed to launch: " + lr.ToString() + ". Aborting!");
                        NumErrors++;
                        Stop();
                        break;
                }
        }

        public void AddDetailsLine(string text)
        {
            textDetails.Text += text + System.Environment.NewLine;
            textDetails.ScrollToEnd();
        }

        System.Windows.Threading.DispatcherTimer Timer;

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            Accounts.Clear();
            //this.Close();
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
