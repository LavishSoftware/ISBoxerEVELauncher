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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace ISBoxerEVELauncher.Windows
{
    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;    // Any value the sender chooses.  Perhaps its main window handle?
        public int cbData;       // The count of bytes in the message.
        public IntPtr lpData;    // The address of the message.
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CHANGEFILTERSTRUCT
    {
        public uint size;
        public MessageFilterInfo info;
    }
    public enum ChangeWindowMessageFilterExAction : uint
    {
        Reset = 0, Allow = 1, DisAllow = 2
    };
    public enum MessageFilterInfo : uint
    {
        None = 0, AlreadyAllowed = 1, AlreadyDisAllowed = 2, AllowedHigher = 3
    };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public const int WM_COPYDATA = 0x004a;
        [DllImport("User32.dll", SetLastError = true, EntryPoint = "SendMessage")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32")]
        public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, ChangeWindowMessageFilterExAction action, ref CHANGEFILTERSTRUCT changeInfo);

        public MainWindow()
        {
            InitializeComponent();
            checkSavePasswords.IsChecked = App.Settings.UseMasterKey;
            App.Settings.RequestMasterPassword();
            App.Settings.PropertyChanged += Settings_PropertyChanged;

            this.Title += " (v"+VersionString+")";
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;

            CHANGEFILTERSTRUCT filterStatus = new CHANGEFILTERSTRUCT();
            filterStatus.size = (uint)Marshal.SizeOf(filterStatus);
            filterStatus.info = 0;
            ChangeWindowMessageFilterEx(source.Handle, WM_COPYDATA, ChangeWindowMessageFilterExAction.Allow, ref filterStatus);

            source.AddHook(WndProc);
        }


        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle messages...
            switch(msg)
            {
                case WM_COPYDATA:
                    
                    COPYDATASTRUCT cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));
                    byte[] buff = new byte[cds.cbData];
                    Marshal.Copy(cds.lpData, buff, 0, cds.cbData);
                    string receivedString = Encoding.Unicode.GetString(buff, 0, cds.cbData);

                    //MessageBox.Show("Processing " + receivedString);
                    App.ProcessCommandLine(receivedString);

                    break;
            }

            return IntPtr.Zero;
        }

        public string VersionString
        {
            get
            {
                return App.AppVersion;
            }
        }

        void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        public ObservableCollection<InnerSpaceGameProfile> GameProfiles 
        { 
            get
            {
                return App.GameProfiles;
            }
        }

        public ObservableCollection<EVEAccount> Accounts
        {
            get
            {
                return App.Settings.Accounts;
            }
        }

        public InnerSpaceGameProfile TranquilityGameProfile
        {
            get
            {
                return App.Settings.TranquilityGameProfile;
            }
            set
            {
                App.Settings.TranquilityGameProfile = value;
                App.Settings.Store();
            }
        }
        public InnerSpaceGameProfile SingularityGameProfile
        {
            get
            {
                return App.Settings.SingularityGameProfile;
            }
            set
            {
                App.Settings.SingularityGameProfile = value;
                App.Settings.Store();
            }
        }
        public string EVESharedCachePath 
        { 
            get { return App.Settings.EVESharedCachePath; }
            set
            {
                App.Settings.EVESharedCachePath = value;
                App.Settings.Store();
            }            
        }


        public bool UseSingularity
        {
            get
            {
                return App.Settings.UseSingularity;
            }
            set
            {
                App.Settings.UseSingularity = value;
                App.Settings.Store();
            }
        }

        public bool? UseDirectX9
        {
            get
            {
                switch(App.Settings.UseDirectXVersion)
                {
                    case DirectXVersion.Default:
                        return null;
                    case DirectXVersion.dx11:
                        return false;
                    case DirectXVersion.dx9:
                        return true;
                }
                return null;
            }
            set
            {
                if (!value.HasValue)
                {
                    App.Settings.UseDirectXVersion = DirectXVersion.Default;
                }
                else
                {
                    if (value.Value)
                        App.Settings.UseDirectXVersion = DirectXVersion.dx9;
                    else
                        App.Settings.UseDirectXVersion = DirectXVersion.dx11;
                }
                App.Settings.Store();
            }
        }

        public float LaunchDelay
        {
            get
            {
                return App.Settings.LaunchDelay;
            }
            set
            {
                App.Settings.LaunchDelay = value;
                App.Settings.Store();
            }
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
//            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog() { InitialDirectory = EVESharedCachePath, CheckPathExists=true, RestoreDirectory=true,  };
            var dialog = new System.Windows.Forms.FolderBrowserDialog() { SelectedPath = EVESharedCachePath, ShowNewFolderButton=false, Description="Please select the EVE SharedCache folder, typically C:\\ProgramData\\EVE\\CCP\\SharedCache" };
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            switch(result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    EVESharedCachePath = dialog.SelectedPath;
                    break;
            }
        }

        private void buttonCreateTranquility_Click(object sender, RoutedEventArgs e)
        {
            string filepath = App.Settings.GetTranquilityPath();
            if (string.IsNullOrEmpty(filepath))
            {
                MessageBox.Show("Please configure Path to EVE's SharedCache first!");
                return;
            }

            CreateGameProfileWindow cgpw = new CreateGameProfileWindow(false, "EVE Online", "Tranquility (Skip Launcher)");
            cgpw.ShowDialog();
            if (cgpw.DialogResult.HasValue && cgpw.DialogResult.Value)
            {
                App.AddGame(cgpw.Game, cgpw.GameProfile, filepath + "\\bin", "exefile.exe", "/noconsole");
                App.ReloadGameConfiguration();
                App.Settings.TranquilityGameProfile = App.FindGlobalGameProfile(new InnerSpaceGameProfile() { Game = cgpw.Game, GameProfile = cgpw.GameProfile });
            }
        }

        private void buttonCreateSingularity_Click(object sender, RoutedEventArgs e)
        {
            string filepath = App.Settings.GetSingularityPath();
            if (string.IsNullOrEmpty(filepath))
            {
                MessageBox.Show("Please configure Path to EVE's SharedCache first!");
                return;
            }

            CreateGameProfileWindow cgpw = new CreateGameProfileWindow(true, "EVE Online", "Singularity (Skip Launcher)");
            cgpw.ShowDialog();
            if (cgpw.DialogResult.HasValue && cgpw.DialogResult.Value)
            {
                App.AddGame(cgpw.Game, cgpw.GameProfile, filepath + "\\bin", "exefile.exe", "/noconsole /server:Singularity");
                App.ReloadGameConfiguration();
                App.Settings.SingularityGameProfile = App.FindGlobalGameProfile(new InnerSpaceGameProfile() { Game = cgpw.Game, GameProfile = cgpw.GameProfile });
            }
        }


        private void buttonAddAccount_Click(object sender, RoutedEventArgs e)
        {
            EVEAccount newAccount = new EVEAccount();
            EVELogin el = new EVELogin(newAccount, false);
            el.ShowDialog();

            if (el.DialogResult.HasValue && el.DialogResult.Value)
            {
                // user clicked Go

                // check password...
//                string refreshToken;
//                switch (newAccount.GetRefreshToken(false, out refreshToken))
                EVEAccount.Token token;
                EVEAccount.LoginResult lr = newAccount.GetAccessToken(false, out token);
                switch(lr)
                {
                    case EVEAccount.LoginResult.Success:
                        break;
                    case EVEAccount.LoginResult.InvalidUsernameOrPassword:
                        {
                            MessageBox.Show("Invalid Username or Password. Account NOT added.");
                            return;
                        }
                    case EVEAccount.LoginResult.Timeout:
                        {
                            MessageBox.Show("Timed out attempting to log in. Account NOT added.");
                            return;
                        }
                    case EVEAccount.LoginResult.InvalidCharacterChallenge:
                        {
                            MessageBox.Show("Invalid Character Name entered, or Invalid Username or Password. Account NOT added.");
                            return;
                        }
                    default:
                        {
                            MessageBox.Show("Failed to log in: "+lr.ToString()+". Account NOT added.");
                            return;
                        }
                        break;
                }
                


                EVEAccount existingAccount = App.Settings.Accounts.FirstOrDefault(q => q.Username.Equals(newAccount.Username, StringComparison.InvariantCultureIgnoreCase));

                if (existingAccount!=null)
                {
                    // update existing account?
                    existingAccount.Username = newAccount.Username;
                    existingAccount.SecurePassword = newAccount.SecurePassword.Copy();
                    existingAccount.EncryptPassword();
                    newAccount.Dispose();
                    newAccount = existingAccount;
                }
                else
                {
                    // new account
                    App.Settings.Accounts.Add(newAccount);
                }

                App.Settings.Store();
            }

        }

        private void checkSavePasswords_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (checkSavePasswords.IsChecked.HasValue && checkSavePasswords.IsChecked.Value)
            {
                SetMasterKeyWindow smkw = new SetMasterKeyWindow();
                smkw.ShowDialog();

                checkSavePasswords.IsChecked = App.Settings.HasPasswordMasterKey;
            }
            else
            {
                // clear master key
                if (App.Settings.UseMasterKey)
                {
                    switch(MessageBox.Show("By un-checking this box, this launcher will immediately clear out all *saved* passwords. Do you wish to continue?", "Wait! You are about to lose any saved passwords!", MessageBoxButton.YesNo))
                    {
                        case MessageBoxResult.Yes:
                            App.Settings.ClearPasswordMasterKey();
                            break;
                        default:
                            checkSavePasswords.IsChecked = true;
                            return;
                    }



                }
            }
        }

        private void buttonLaunchNonIS_Click(object sender, RoutedEventArgs e)
        {
            List<EVEAccount> launchAccounts = new List<EVEAccount>();
            foreach (EVEAccount a in listAccounts.SelectedItems)
            {
                launchAccounts.Add(a);
            }

            if (launchAccounts.Count == 0)
                return;

            InnerSpaceGameProfile gp;
            if (string.IsNullOrWhiteSpace(App.Settings.EVESharedCachePath))
            {
                MessageBox.Show("Please set the EVE SharedCache path first!");
                return;
            }

            Windows.LaunchProgressWindow lpw = new LaunchProgressWindow(launchAccounts, new Launchers.DirectLauncher(App.Settings.EVESharedCachePath, App.Settings.UseDirectXVersion, App.Settings.UseSingularity));
            lpw.ShowDialog();
        }

        private void buttonLaunchIS_Click(object sender, RoutedEventArgs e)
        {
            List<EVEAccount> launchAccounts = new List<EVEAccount>();
            foreach (EVEAccount a in listAccounts.SelectedItems)
            {
                launchAccounts.Add(a);
            }

            if (launchAccounts.Count == 0)
                return;

            InnerSpaceGameProfile gp;
            if (App.Settings.UseSingularity)
            {
                gp = App.Settings.SingularityGameProfile;
            }
            else
            {
                gp = App.Settings.TranquilityGameProfile;
            }

            if (gp==null || string.IsNullOrEmpty(gp.Game) || string.IsNullOrEmpty(gp.GameProfile))
            {
                MessageBox.Show("Please select a Game Profile first!");
                return;
            }

            Windows.LaunchProgressWindow lpw = new LaunchProgressWindow(launchAccounts, new Launchers.InnerSpaceLauncher(gp, App.Settings.UseDirectXVersion, App.Settings.UseSingularity));
            lpw.ShowDialog();
            /*
            foreach(EVEAccount a in launchAccounts)
            {
                EVEAccount.LoginResult lr = a.Launch(gp.Game, gp.GameProfile, App.Settings.UseSingularity, App.Settings.UseDirectXVersion);
                switch(lr)
                {
                    case EVEAccount.LoginResult.Success:
                        listAccounts.SelectedItems.Remove(a);
                        break;
                    case EVEAccount.LoginResult.InvalidUsernameOrPassword:
                        {
                            MessageBox.Show("Invalid Username or Password. Account NOT launched.");
                            return;
                        }
                    case EVEAccount.LoginResult.Timeout:
                        {
                            MessageBox.Show("Timed out attempting to log in. Account NOT launched.");
                            return;
                        }
                    case EVEAccount.LoginResult.InvalidCharacterChallenge:
                        {
                            MessageBox.Show("Invalid Character Name entered, or Invalid Username or Password. Account NOT launched.");
                            return;
                        }
                    default:
                        {
                            MessageBox.Show("Failed to log in: " + lr.ToString() + ". Account NOT launched.");
                            return;
                        }

                }
            }
             */
        }

        private void buttonDeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            List<EVEAccount> deleteAccounts = new List<EVEAccount>();
            foreach(EVEAccount a in listAccounts.SelectedItems)
            {
                deleteAccounts.Add(a);
            }

            if (deleteAccounts.Count == 0)
                return;

            if (deleteAccounts.Count==1)
            {
                switch(MessageBox.Show("Are you sure you want to delete '"+deleteAccounts[0].Username+"'?","Wait! You are about to lose an account!", MessageBoxButton.YesNo))
                {
                    case MessageBoxResult.Yes:
                        break;
                    default:
                        return;
                }
            }
            else
            {
                switch (MessageBox.Show("Are you sure you want to delete "+deleteAccounts.Count+" accounts?", "Wait! You are about to lose some accounts!", MessageBoxButton.YesNo))
                {
                    case MessageBoxResult.Yes:
                        break;
                    default:
                        return;
                }
            }

            foreach(EVEAccount toDelete in deleteAccounts)
            {
                Accounts.Remove(toDelete);
                toDelete.Dispose();
            }
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

        private void buttonCreateLauncherProfiles_Click(object sender, RoutedEventArgs e)
        {
            List<EVEAccount> launchAccounts = new List<EVEAccount>();
            foreach (EVEAccount a in listAccounts.SelectedItems)
            {
                launchAccounts.Add(a);
            }

            if (launchAccounts.Count == 0)
                return;

            CreateAccountGameProfilesWindow cagpw = new CreateAccountGameProfilesWindow("ISBoxer EVE Launcher","ISBEL - {0}");
            cagpw.ShowDialog();

            if (cagpw.DialogResult.HasValue && cagpw.DialogResult.Value)
            {

                foreach (EVEAccount acct in launchAccounts)
                {
                    string flags = string.Empty;
                    if (cagpw.UseEVEDirect)
                        flags += "-eve ";
                    else
                        flags += "-innerspace ";
                    
                    if (cagpw.UseNewLauncher)
                    {
                        flags += "-multiinstance ";

                        if (!cagpw.LeaveLauncherOpen)
                            flags += "-exit ";
                    }

                    
                    if (!App.AddGame(cagpw.Game,string.Format(cagpw.GameProfile, acct.Username), App.BaseDirectory, "ISBoxerEVELauncher.exe", flags +"\""+ acct.Username + "\""))
                    {
                        App.ReloadGameConfiguration();
                        return;

                    }
                }

                App.ReloadGameConfiguration();

            }
        }



    }
}
