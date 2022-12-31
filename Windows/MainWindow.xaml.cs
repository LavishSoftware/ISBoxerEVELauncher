using ISBoxerEVELauncher.Enums;
using ISBoxerEVELauncher.Games.EVE;
using ISBoxerEVELauncher.InnerSpace;
using ISBoxerEVELauncher.Security;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

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
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);
        [DllImport("User32.dll", SetLastError = true, EntryPoint = "SendMessage")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        //        [DllImport("User32.dll", SetLastError = true, EntryPoint = "PostMessage")]
        //        public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("User32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("user32")]
        public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, ChangeWindowMessageFilterExAction action, ref CHANGEFILTERSTRUCT changeInfo);

        public System.Windows.Forms.NotifyIcon NotifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            Style itemContainerStyle = new Style(typeof(ListBoxItem));
            itemContainerStyle.Setters.Add(new Setter(ListBoxItem.AllowDropProperty, true));
            itemContainerStyle.Setters.Add(new EventSetter(ListBoxItem.PreviewMouseMoveEvent, new MouseEventHandler(s_PreviewMouseMoveEvent)));
            itemContainerStyle.Setters.Add(new EventSetter(ListBoxItem.DropEvent, new DragEventHandler(listAccounts_Drop)));
            listAccounts.ItemContainerStyle = itemContainerStyle;

            checkSavePasswords.IsChecked = App.Settings.UseMasterKey;

            App.Settings.PropertyChanged += Settings_PropertyChanged;

            this.Title += " (v" + VersionString + ")";


            NotifyIcon = new System.Windows.Forms.NotifyIcon();
            var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/ISBoxerEVELauncher;component/ISBEL.ico")).Stream;
            NotifyIcon.Icon = new System.Drawing.Icon(iconStream);

            NotifyIcon.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    NotifyIcon.Visible = false;
                };
        }

        protected override void OnStateChanged(EventArgs e)
        {
            //if (WindowState == System.Windows.WindowState.Minimized)
            //{
            //    this.Hide();
            //    NotifyIcon.Visible = true;
            //}
            base.OnStateChanged(e);
        }

        #region Drag and drop for Accounts list
        void s_PreviewMouseMoveEvent(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            if (sender is ListBoxItem)
            {
                ListBoxItem draggedItem = sender as ListBoxItem;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        void listAccounts_Drop(object sender, DragEventArgs e)
        {
            EVEAccount droppedData = e.Data.GetData(typeof(EVEAccount)) as EVEAccount;
            EVEAccount target = ((ListBoxItem)(sender)).DataContext as EVEAccount;

            int removedIdx = listAccounts.Items.IndexOf(droppedData);
            int targetIdx = listAccounts.Items.IndexOf(target);

            if (removedIdx == targetIdx)
                return;

            if (removedIdx < targetIdx)
            {
                Accounts.Insert(targetIdx + 1, droppedData);
                Accounts.RemoveAt(removedIdx);
            }
            else
            {
                int remIdx = removedIdx + 1;
                if (Accounts.Count + 1 > remIdx)
                {
                    Accounts.Insert(targetIdx, droppedData);
                    Accounts.RemoveAt(remIdx);
                }
            }
            App.Settings.Store();
        }
        #endregion

        /// <summary>
        /// Enables a specified message to be sent to the window from non-Administrator processes
        /// </summary>
        /// <param name="source"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        static bool EnableWindowMessage(HwndSource source, uint msg)
        {
            CHANGEFILTERSTRUCT filterStatus = new CHANGEFILTERSTRUCT();
            filterStatus.size = (uint)Marshal.SizeOf(filterStatus);
            filterStatus.info = 0;
            return ChangeWindowMessageFilterEx(source.Handle, msg, ChangeWindowMessageFilterExAction.Allow, ref filterStatus);
        }

        /// <summary>
        /// Request Master Key from Master Instance
        /// </summary>
        /// <returns>True if a request was transmitted to the Master Instance</returns>
        public bool RequestMasterKey()
        {
            if (App.Settings.UseMasterKey && (App.Settings.PasswordMasterKey == null || !App.Settings.PasswordMasterKey.HasData))
            {

                System.Diagnostics.Process masterInstance = App.GetMasterInstance(true);
                if (masterInstance == null)
                    return false;

                if (masterInstance.MainWindowHandle == null)
                    return false;

                KeyTransmitter.RequestMasterKey(this, masterInstance.MainWindowHandle, masterInstance);
                App.Settings.MasterKeyRequested = DateTime.Now;
                return true;
            }

            return false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);


            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;

            // enable WM_COPYDATA from non-Administrator
            EnableWindowMessage(source, WM_COPYDATA);

            source.AddHook(WndProc);

            if (!RequestMasterKey())
                App.Settings.RequestMasterPassword();

        }


        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle messages...
            switch (msg)
            {
                case WM_COPYDATA:
                    handled = true;
                    COPYDATASTRUCT cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));
                    switch ((long)cds.dwData)
                    {
                        case 0:
                            byte[] buff = new byte[cds.cbData];
                            Marshal.Copy(cds.lpData, buff, 0, cds.cbData);
                            string receivedString = Encoding.Unicode.GetString(buff, 0, cds.cbData);

                            //MessageBox.Show("Processing " + receivedString);
                            App.ProcessCommandLine(receivedString);
                            break;
                        case 10:
                        case 11:
                        case 12:
                            {
                                int processId = 0;
                                GetWindowThreadProcessId(wParam, out processId);

                                if (processId == 0)
                                    return IntPtr.Zero;

                                System.Diagnostics.Process newInstance = System.Diagnostics.Process.GetProcessById(processId);
                                // ensure this is the same app, otherwise we might be leaking ...

                                try
                                {
                                    if (newInstance == null || newInstance.MainModule.FileName != System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
                                    {
                                        // different thing, sorry.
                                        return IntPtr.Zero;
                                    }
                                }
                                catch (System.ComponentModel.Win32Exception we)
                                {
                                    if (we.NativeErrorCode == 5)
                                    {
                                        // other instance is Administrator and we are not; cannot confirm main module
                                        return IntPtr.Zero;
                                    }
                                    MessageBox.Show("Error=" + we.NativeErrorCode);
                                    throw;
                                }
                                /**/

                                KeyTransmitter.ReceiveTransmission(this, wParam, newInstance, cds);
                            }
                            break;
                    }

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

        public ObservableCollection<EVECharacter> Characters
        {
            get
            {
                return App.Settings.Characters;
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
            get
            {
                return App.Settings.EVESharedCachePath;
            }
            set
            {
                App.Settings.EVESharedCachePath = value;
                App.Settings.Store();
            }
        }

        public Visibility InnerSpaceVisibility
        {
            get
            {
                if (App.HasInnerSpace)
                    return System.Windows.Visibility.Visible;
                return System.Windows.Visibility.Collapsed;
            }
            set
            {

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

        public bool? UseDirectX12
        {
            get
            {
                switch (App.Settings.UseDirectXVersion)
                {
                    case DirectXVersion.Default:
                        return false;
                    case DirectXVersion.dx11:
                        return false;
                    case DirectXVersion.dx12:
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
                        App.Settings.UseDirectXVersion = DirectXVersion.dx12;
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
            var dialog = new System.Windows.Forms.FolderBrowserDialog() { SelectedPath = EVESharedCachePath, ShowNewFolderButton = false, Description = "Please select the EVE SharedCache folder, typically C:\\ProgramData\\EVE\\CCP\\SharedCache" };
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            switch (result)
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
                //App.AddGame(cgpw.Game, cgpw.GameProfile, System.IO.Path.Combine(filepath, "bin"), "exefile.exe", "/noconsole");
                App.AddGame(cgpw.Game, cgpw.GameProfile + " x64", System.IO.Path.Combine(filepath, "bin64"), "exefile.exe", "/noconsole");
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
                //App.AddGame(cgpw.Game, cgpw.GameProfile, System.IO.Path.Combine(filepath, "bin"), "exefile.exe", "/noconsole /server:Singularity");
                App.AddGame(cgpw.Game, cgpw.GameProfile + " x64", System.IO.Path.Combine(filepath, "bin64"), "exefile.exe", "/noconsole");
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
                LoginResult lr = newAccount.GetAccessToken(false, out token);
                switch (lr)
                {
                    case LoginResult.Success:
                        break;
                    case LoginResult.InvalidUsernameOrPassword:
                        {
                            MessageBox.Show("Invalid Username or Password. Account NOT added.");
                            return;
                        }
                    case LoginResult.Timeout:
                        {
                            MessageBox.Show("Timed out attempting to log in. Account NOT added.");
                            return;
                        }
                    case LoginResult.InvalidCharacterChallenge:
                        {
                            MessageBox.Show("Invalid Character Name entered, or Invalid Username or Password. Account NOT added.");
                            return;
                        }
                    case LoginResult.EmailVerificationRequired:
                        // message already provided
                        return;
                    default:
                        {
                            MessageBox.Show("Failed to log in: " + lr.ToString() + ". Account NOT added.");
                            return;
                        }
                }



                EVEAccount existingAccount = App.Settings.Accounts.FirstOrDefault(q => q.Username.Equals(newAccount.Username, StringComparison.InvariantCultureIgnoreCase));

                if (existingAccount != null)
                {
                    // update existing account?
                    existingAccount.Username = newAccount.Username;
                    existingAccount.SecurePassword = newAccount.SecurePassword.Copy();
                    existingAccount.EncryptPassword();

                    if (newAccount.SecureCharacterName != null && newAccount.SecureCharacterName.Length > 0)
                    {
                        existingAccount.SecureCharacterName = newAccount.SecureCharacterName.Copy();
                        existingAccount.EncryptCharacterName();
                    }

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
                    switch (MessageBox.Show("By un-checking this box, this launcher will immediately clear out all *saved* passwords. Do you wish to continue?", "Wait! You are about to lose any saved passwords!", MessageBoxButton.YesNo))
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

            if (string.IsNullOrWhiteSpace(App.Settings.EVESharedCachePath))
            {
                MessageBox.Show("Please set the EVE SharedCache path first!");
                return;
            }

            Windows.LaunchProgressWindow lpw = new LaunchProgressWindow(launchAccounts, new Launchers.Direct(App.Settings.EVESharedCachePath, App.Settings.UseDirectXVersion, App.Settings.UseSingularity));
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

            if (gp == null || string.IsNullOrEmpty(gp.Game) || string.IsNullOrEmpty(gp.GameProfile))
            {
                MessageBox.Show("Please select a Game Profile first!");
                return;
            }

            Windows.LaunchProgressWindow lpw = new LaunchProgressWindow(launchAccounts, new Launchers.InnerSpace(gp, App.Settings.UseDirectXVersion, App.Settings.UseSingularity));
            lpw.ShowDialog();
        }

        private void buttonDeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            List<EVEAccount> deleteAccounts = new List<EVEAccount>();
            foreach (EVEAccount a in listAccounts.SelectedItems)
            {
                deleteAccounts.Add(a);
            }

            if (deleteAccounts.Count == 0)
                return;

            if (deleteAccounts.Count == 1)
            {
                switch (MessageBox.Show("Are you sure you want to delete '" + deleteAccounts[0].Username + "'?", "Wait! You are about to lose an account!", MessageBoxButton.YesNo))
                {
                    case MessageBoxResult.Yes:
                        break;
                    default:
                        return;
                }
            }
            else
            {
                switch (MessageBox.Show("Are you sure you want to delete " + deleteAccounts.Count + " accounts?", "Wait! You are about to lose some accounts!", MessageBoxButton.YesNo))
                {
                    case MessageBoxResult.Yes:
                        break;
                    default:
                        return;
                }
            }

            foreach (EVEAccount toDelete in deleteAccounts)
            {
                Accounts.Remove(toDelete);
                toDelete.Dispose();

            }

            App.Settings.Store();
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

            CreateAccountGameProfilesWindow cagpw = new CreateAccountGameProfilesWindow("ISBoxer EVE Launcher", "ISBEL - {0}");
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


                    if (!App.AddGame(cagpw.Game, string.Format(cagpw.GameProfile, acct.Username), App.BaseDirectory, "ISBoxerEVELauncher.exe", flags + "\"" + acct.Username + "\""))
                    {
                        App.ReloadGameConfiguration();
                        return;

                    }
                }

                App.ReloadGameConfiguration();

            }
        }

        private void TranquilityGameProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // only perform validation if the user is touching the control
            if (!comboTranquilityGameProfile.IsMouseOver && !comboTranquilityGameProfile.IsFocused && !comboTranquilityGameProfile.IsKeyboardFocused)
                return;

            InnerSpaceGameProfile gp = TranquilityGameProfile;
            if (gp == null)
                return;

            switch (gp.Executable)
            {
                case RelatedExecutable.EXEFile:
                    // good
                    break;
                case RelatedExecutable.EVELauncher:
                case RelatedExecutable.InnerSpace:
                case RelatedExecutable.InvalidGameProfile:
                case RelatedExecutable.ISBoxerEVELauncher:
                case RelatedExecutable.Other:
                    MessageBox.Show("This Game Profile does not appear to point to exefile.exe. Please select a Game Profile that points at exefile.exe, or use 'Create one now' to have one correctly set up for you.");
                    TranquilityGameProfile = null;
                    break;
            }

        }

        private void SingularityGameProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // only perform validation if the user is touching the control
            if (!comboSingularityGameProfile.IsMouseOver && !comboSingularityGameProfile.IsFocused && !comboSingularityGameProfile.IsKeyboardFocused)
                return;

            InnerSpaceGameProfile gp = SingularityGameProfile;
            if (gp == null)
                return;

            switch (gp.Executable)
            {
                case RelatedExecutable.EXEFile:
                    // good
                    break;
                case RelatedExecutable.EVELauncher:
                case RelatedExecutable.InnerSpace:
                case RelatedExecutable.InvalidGameProfile:
                case RelatedExecutable.ISBoxerEVELauncher:
                case RelatedExecutable.Other:
                    MessageBox.Show("This Game Profile does not appear to point to exefile.exe. Please select a Game Profile that points at exefile.exe, or use 'Create one now' to have one correctly set up for you.");
                    SingularityGameProfile = null;
                    break;
            }

        }

        private void buttonAddCharacter_Click(object sender, RoutedEventArgs e)
        {
            EVECharacter newCharacter = new EVECharacter();
            AddCharacterWindow acw = new AddCharacterWindow(newCharacter);
            acw.ShowDialog();

            if (acw.DialogResult.HasValue && acw.DialogResult.Value)
            {
                // user clicked Go
                EVECharacter existing = App.Settings.FindEVECharacter(acw.UseSingularity, acw.CharacterName);
                if (existing != null)
                {
                    existing.EVEAccount = acw.Account;
                }
                else
                {
                    // no existing.
                    App.Settings.Characters.Add(newCharacter);
                }

                App.Settings.Store();
            }
        }

        private void buttonLaunchCharacterIS_Click(object sender, RoutedEventArgs e)
        {
            List<EVECharacter> launchCharacters = new List<EVECharacter>();
            foreach (EVECharacter a in listCharacters.SelectedItems)
            {
                launchCharacters.Add(a);
            }

            if (launchCharacters.Count == 0)
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

            if (gp == null || string.IsNullOrEmpty(gp.Game) || string.IsNullOrEmpty(gp.GameProfile))
            {
                MessageBox.Show("Please select a Game Profile first!");
                return;
            }

            Windows.LaunchProgressWindow lpw = new LaunchProgressWindow(launchCharacters, new Launchers.InnerSpace(gp, App.Settings.UseDirectXVersion, App.Settings.UseSingularity));
            lpw.ShowDialog();
        }

        private void buttonLaunchCharacterNonIS_Click(object sender, RoutedEventArgs e)
        {

            List<EVECharacter> launchCharacters = new List<EVECharacter>();
            foreach (EVECharacter a in listCharacters.SelectedItems)
            {
                launchCharacters.Add(a);
            }

            if (launchCharacters.Count == 0)
                return;

            if (string.IsNullOrWhiteSpace(App.Settings.EVESharedCachePath))
            {
                MessageBox.Show("Please set the EVE SharedCache path first!");
                return;
            }

            Windows.LaunchProgressWindow lpw = new LaunchProgressWindow(launchCharacters, new Launchers.Direct(App.Settings.EVESharedCachePath, App.Settings.UseDirectXVersion, App.Settings.UseSingularity));
            lpw.ShowDialog();
        }

        private void buttonCreateCharacterLauncherProfiles_Click(object sender, RoutedEventArgs e)
        {
            List<EVECharacter> launchCharacters = new List<EVECharacter>();
            foreach (EVECharacter a in listCharacters.SelectedItems)
            {
                launchCharacters.Add(a);
            }

            if (launchCharacters.Count == 0)
                return;

            CreateAccountGameProfilesWindow cagpw = new CreateAccountGameProfilesWindow("ISBoxer EVE Launcher", "ISBEL - {0}");
            cagpw.ShowDialog();

            if (cagpw.DialogResult.HasValue && cagpw.DialogResult.Value)
            {

                foreach (EVECharacter acct in launchCharacters)
                {
                    string flags = string.Empty;
                    flags += "-c ";

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


                    if (!App.AddGame(cagpw.Game, string.Format(cagpw.GameProfile, acct.Name), App.BaseDirectory, "ISBoxerEVELauncher.exe", flags + "\"" + acct.Name + "\""))
                    {
                        App.ReloadGameConfiguration();
                        return;

                    }
                }

                App.ReloadGameConfiguration();

            }

        }

        private void buttonDeleteCharacter_Click(object sender, RoutedEventArgs e)
        {
            List<EVECharacter> deleteCharacters = new List<EVECharacter>();
            foreach (EVECharacter a in listCharacters.SelectedItems)
            {
                deleteCharacters.Add(a);
            }

            if (deleteCharacters.Count == 0)
                return;

            if (deleteCharacters.Count == 1)
            {
                switch (MessageBox.Show("Are you sure you want to delete '" + deleteCharacters[0].Name + "'?", "Wait! You are about to lose a character!", MessageBoxButton.YesNo))
                {
                    case MessageBoxResult.Yes:
                        break;
                    default:
                        return;
                }
            }
            else
            {
                switch (MessageBox.Show("Are you sure you want to delete " + deleteCharacters.Count + " characters?", "Wait! You are about to lose some characters!", MessageBoxButton.YesNo))
                {
                    case MessageBoxResult.Yes:
                        break;
                    default:
                        return;
                }
            }

            foreach (EVECharacter toDelete in deleteCharacters)
            {
                Characters.Remove(toDelete);
            }

        }


    }
}
