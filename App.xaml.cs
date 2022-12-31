using ISBoxerEVELauncher.Enums;
using ISBoxerEVELauncher.Extensions;
using ISBoxerEVELauncher.Games.EVE;
using ISBoxerEVELauncher.InnerSpace;
using ISBoxerEVELauncher.Interface;
using ISBoxerEVELauncher.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;


namespace ISBoxerEVELauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool HasInnerSpace
        {
            get; set;
        }
        public static EVELoginBrowser myLB = new EVELoginBrowser();
        public static byte[] requestBody;
        public static bool tofCaptcha;
        public static string strUserName
        {
            get; set;
        }
        public static string strPassword
        {
            get; set;
        }


        public static string AppVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }


        public static void ReloadGameConfiguration()
        {
            try
            {
                GameConfiguration = InnerSpaceSettings.Load(ISPath + @"\GameConfiguration.XML");
            }
            catch
            {
                GameConfiguration = null;
            }
        }

        public static string DetectedEVESharedCachePath
        {
            get
            {
                RegistryKey hkcu = Registry.CurrentUser;
                RegistryKey PathKey = hkcu.OpenSubKey(@"SOFTWARE\CCP\EVEONLINE");
                if (PathKey != null)
                {
                    string value = PathKey.GetValue("CACHEFOLDER", Environment.SpecialFolder.CommonApplicationData + @"\CCP\EVE\SharedCache\") as string;
                    return value;
                }
                return null;
            }

        }

        public static string[] CommandLine;
        public static bool ExitAfterLaunch;
        public static bool searchCharactersOnly;

        static Settings _Settings;
        public static Settings Settings
        {
            get
            {
                if (_Settings == null)
                {
                    try
                    {
                        _Settings = Settings.Load();
                        if (EVEAccount.ShouldUgradeCookieStorage)
                        {
                            _Settings.Store();
                        }
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        _Settings = new Settings();
                        _Settings.Store();
                    }
                }
                return _Settings;
            }
        }

        public static string ISBoxerEVELauncherPath
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static string ISExecutable
        {
            get
            {
                return ISPath + "\\InnerSpace.exe";
            }
        }

        public static string BaseDirectory
        {
            get
            {
                return System.AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        static string _ISPath = string.Empty;
        /// <summary>
        /// Path to Inner Space, which should either be in the registry, or the current folder. Otherwise, the user likely doesn't care about Inner Space integration.
        /// </summary>
        public static string ISPath
        {
            get
            {
                // already detected?
                if (!string.IsNullOrEmpty(_ISPath))
                    return _ISPath;

                // nope.

                // we're expected to be installed into the IS/ISBoxer folder....
                if (System.IO.File.Exists(BaseDirectory + @"\InnerSpace.exe"))
                {
                    _ISPath = BaseDirectory;
                    return _ISPath;
                }

                // oh well... the path SHOULD be in the registry...
                {
                    RegistryKey hklm = Registry.LocalMachine;
                    RegistryKey ISPathKey = hklm.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\InnerSpace.exe");
                    if (ISPathKey != null)
                    {
                        string reg_path = ISPathKey.GetValue("Path", Environment.SpecialFolder.ProgramFiles + @"\InnerSpace") as string;
                        if (System.IO.File.Exists(reg_path + "\\InnerSpace.exe"))
                        {
                            _ISPath = reg_path;
                            return _ISPath;
                        }
                    }
                }

                // didn't find it. maybe not even installed.
                return BaseDirectory;
            }
            set
            {
                _ISPath = value;
            }
        }

        static void ReloadGameProfiles()
        {
            InnerSpaceGameProfile gpSingularity = Settings.SingularityGameProfile;
            InnerSpaceGameProfile gpTranquility = Settings.TranquilityGameProfile;

            if (_GameProfiles == null)
            {
                _GameProfiles = new ObservableCollection<InnerSpaceGameProfile>();
            }
            else
                _GameProfiles.Clear();

            if (GameConfiguration != null)
            {
                if (GameConfiguration.Sets != null)
                {
                    foreach (Set gameSet in GameConfiguration.Sets)
                    {
                        Set profilesSet = gameSet.FindSet("Profiles");
                        if (profilesSet == null || profilesSet.Sets == null)
                            continue;

                        foreach (Set gameProfileSet in profilesSet.Sets)
                        {
                            InnerSpaceGameProfile gp = new InnerSpaceGameProfile() { Game = gameSet.Name, GameProfile = gameProfileSet.Name };
                            _GameProfiles.Add(gp);
                        }
                    }
                }
            }

            Settings.SingularityGameProfile = App.FindGlobalGameProfile(gpSingularity);
            Settings.TranquilityGameProfile = App.FindGlobalGameProfile(gpTranquility);
        }

        public static Set FindGameProfileSet(string gameName, string gameProfileName)
        {
            if (string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(gameProfileName))
                return null;

            if (GameConfiguration == null || GameConfiguration.Sets == null)
                return null;

            Set gameSet = GameConfiguration.FindSet(gameName);
            if (gameSet == null)
                return null;

            Set profilesSet = gameSet.FindSet("Profiles");
            if (profilesSet == null || profilesSet.Sets == null)
                return null;

            return profilesSet.FindSet(gameProfileName);
        }

        static Set _GameConfiguration;
        public static Set GameConfiguration
        {
            get
            {
                return _GameConfiguration;
            }
            private set
            {
                _GameConfiguration = value;
                ReloadGameProfiles();
            }
        }
        static ObservableCollection<InnerSpaceGameProfile> _GameProfiles;
        public static ObservableCollection<InnerSpaceGameProfile> GameProfiles
        {
            get
            {
                if (_GameProfiles == null)
                {
                    _GameProfiles = new ObservableCollection<InnerSpaceGameProfile>();
                    ReloadGameProfiles();
                }
                return _GameProfiles;
            }
            private set
            {
                _GameProfiles = value;
            }
        }

        public static InnerSpaceGameProfile FindGlobalGameProfile(InnerSpaceGameProfile likeThis)
        {
            if (GameProfiles == null || likeThis == null)
                return likeThis;

            InnerSpaceGameProfile found = GameProfiles.FirstOrDefault(q => q.Game.Equals(likeThis.Game, StringComparison.InvariantCultureIgnoreCase) && q.GameProfile.Equals(likeThis.GameProfile, StringComparison.InvariantCultureIgnoreCase));
            if (found == null)
                return likeThis;

            return found;
        }

        static bool AddGameToXML(string gameName, string gameProfileName, string executablePath, string executableName, string parameters)
        {
            // not running, just edit the XML
            if (GameConfiguration == null)
            {
                // no existing XML.
                return false;
            }

            Set gameSet = GameConfiguration.FindSet(gameName);
            if (gameSet == null)
            {
                gameSet = new Set(gameName);
                GameConfiguration.Add(gameSet);
                gameSet.Add(new Setting("OpenGL", "1"));
                gameSet.Add(new Setting("Direct3D8", "1"));
                gameSet.Add(new Setting("Direct3D9", "1"));
                gameSet.Add(new Setting("Win32I Keyboard", "1"));
                gameSet.Add(new Setting("Win32I Mouse", "1"));
                gameSet.Add(new Setting("DirectInput8 Keyboard", "1"));
                gameSet.Add(new Setting("DirectInput8 Mouse", "1"));
                gameSet.Add(new Setting("modules", "auto"));
                gameSet.Add(new Setting("Background Mouse", "1"));
                gameSet.Add(new Setting("Keystroke Delay", "1"));
            }

            Set gameProfilesSet = gameSet.FindSet("Profiles");
            if (gameProfilesSet == null)
            {
                gameProfilesSet = new Set("Profiles");
                gameSet.Add(gameProfilesSet);
            }

            Set gameProfileSet = gameSet.FindSet(gameProfileName);
            if (gameProfileSet == null)
            {
                gameProfileSet = new Set(gameProfileName);
                gameProfilesSet.Add(gameProfileSet);
            }

            Setting setting = gameProfileSet.FindSetting("Executable");
            if (setting == null)
                gameProfileSet.Add(new Setting("Executable", executableName));
            else
                setting.Value = executableName;

            setting = gameProfileSet.FindSetting("Path");
            if (setting == null)
                gameProfileSet.Add(new Setting("Path", executablePath));
            else
                setting.Value = executablePath;

            setting = gameProfileSet.FindSetting("Parameters");
            if (setting == null)
            {
                if (!string.IsNullOrEmpty(parameters))
                    gameProfileSet.Add(new Setting("Parameters", parameters));
            }
            else
            {
                if (string.IsNullOrEmpty(parameters))
                    gameProfileSet.Settings.Remove(setting);
                else
                    setting.Value = parameters;
            }

            if (GameProfiles.FirstOrDefault(q => q.GameProfile.Equals(gameProfileName)) == null)
            {
                GameProfiles.Add(new InnerSpaceGameProfile() { Game = gameName, GameProfile = gameProfileName });
            }

            GameConfiguration.Store(ISPath + @"\GameConfiguration.XML");
            return true;
        }
        /// <summary>
        /// Add a Game/Game Profile to Inner Space
        /// </summary>
        /// <param name="gameName"></param>
        /// <param name="gameProfileName"></param>
        /// <param name="executablePath"></param>
        /// <param name="executableName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static bool AddGame(string gameName, string gameProfileName, string executablePath, string executableName, string parameters)
        {
            string isboxerFilename = System.IO.Path.Combine(ISPath, "ISBoxer Toolkit.exe");
            while (true)
            {
                System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName("InnerSpace");
                if (processes.Length == 0)
                {
                    if (!AddGameToXML(gameName, gameProfileName, executablePath, executableName, parameters))
                    {
                        MessageBox.Show("In order to Add Game for you, ISBoxer EVE Launcher requires that GameConfiguration.XML exist in the Inner Space folder -- looking for: " + isboxerFilename);
                        return false;
                    }
                    return true;
                }

                if (!System.IO.File.Exists(isboxerFilename))
                {
                    switch (MessageBox.Show("ISBoxer EVE Launcher has determined that Inner Space is running. Please Exit Inner Space and click OK to try again, otherwise click Cancel.", "Adding a Game this way requires Inner Space to be closed", MessageBoxButton.OKCancel))
                    {
                        case MessageBoxResult.OK:
                            continue;
                        case MessageBoxResult.Cancel:
                            return false;
                    }
                }
                else
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(isboxerFilename);
                    if (fvi.ProductMajorPart < 42)
                    {
                        switch (MessageBox.Show("ISBoxer EVE Launcher has determined that Inner Space is running. Please Exit Inner Space and click OK to try again, otherwise click Cancel.", "Adding a Game this way requires Inner Space to be closed", MessageBoxButton.OKCancel))
                        {
                            case MessageBoxResult.OK:
                                continue;
                            case MessageBoxResult.Cancel:
                                return false;
                        }
                    }
                }


                string cmdLine = "run isboxer -inituplink;isboxeraddgame \"" + gameName.Replace("\"", "\\\"") + "\" \"" + gameProfileName.Replace("\"", "\\\"") + "\" \"" + executablePath.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\" \"" + executableName.Replace("\"", "\\\"") + "\" \"" + parameters.Replace("\"", "\\\"") + "\"";
                System.Diagnostics.Process.Start(ISExecutable, cmdLine);
                return true;
            }
        }

        /// <summary>
        /// Have Inner Space launch EVE via a specified Game and Game Profile
        /// </summary>
        /// <param name="ssoToken"></param>
        /// <param name="gameName"></param>
        /// <param name="gameProfileName"></param>
        /// <param name="sisi"></param>
        /// <param name="dxVersion"></param>
        /// <returns></returns>
        static public bool Launch(string gameName, string gameProfileName, bool sisi, DirectXVersion dxVersion, long characterID, EVEAccount.Token token)
        {
            //if (ssoToken == null)
            //    throw new ArgumentNullException("ssoToken");
            if (gameName == null)
                throw new ArgumentNullException("gameName");
            if (gameProfileName == null)
                throw new ArgumentNullException("gameProfileName");

            string cmdLine = "open \"" + gameName + "\" \"" + gameProfileName + "\" -addparam \"/noconsole\" -addparam \"/ssoToken=" + token.TokenString + "\" -addparam \"/refreshToken=" + token.RefreshToken + "\"";
            if (dxVersion != DirectXVersion.Default)
            {
                cmdLine += " -addparam \"/triPlatform=" + dxVersion.ToString() + "\"";
            }

            if (characterID != 0)
            {
                cmdLine += " -addparam \"/character=" + characterID + "\"";
            }

            if (sisi)
            {
                cmdLine += " -addparam \"/server:Singularity\"";
            }
            else
            {
                cmdLine += " -addparam \"/server:tranquility\"";
            }

            cmdLine += " -addparam \"settingsprofile=ISBEL\" -addparam \"/machineHash=" + App.Settings.MachineHash + "\" \"\"";

            try
            {
                System.Diagnostics.Process.Start(App.ISExecutable, cmdLine);
            }
            catch (Exception e)
            {
                MessageBox.Show("Launch failed. executable=" + App.ISExecutable + "; args=" + cmdLine + System.Environment.NewLine + e.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// Launch EVE directly
        /// </summary>
        /// <param name="ssoToken"></param>
        /// <param name="sharedCachePath"></param>
        /// <param name="sisi"></param>
        /// <param name="dxVersion"></param>
        /// <returns></returns>
        static public bool Launch(string sharedCachePath, bool sisi, DirectXVersion dxVersion, long characterID, EVEAccount.Token token)
        {
            //if (ssoToken == null)
            //    throw new ArgumentNullException("ssoToken");
            if (sharedCachePath == null)
                throw new ArgumentNullException("sharedCachePath");

            string args = "/noconsole /ssoToken=" + token.TokenString + " /refreshToken=" + token.RefreshToken;
            if (dxVersion != DirectXVersion.Default)
            {
                args += " /triPlatform=" + dxVersion.ToString();
            }

            if (sisi)
            {
                args += " /server:Singularity";
            }
            else
            {
                args += " /server:tranquility";
            }

            if (characterID != 0)
            {
                args += " /character=" + characterID;
            }

            args += " /settingsprofile=ISBEL /machineHash=" + App.Settings.MachineHash + " \"\"";

            string executable;
            if (sisi)
                executable = App.Settings.GetSingularityEXE();
            else
                executable = App.Settings.GetTranquilityEXE();

            if (!System.IO.File.Exists(executable))
            {
                MessageBox.Show("Cannot find exefile.exe for launch -- looking at: " + executable);
                return false;
            }

            try
            {
                System.Diagnostics.Process.Start(executable, args);
            }
            catch (Exception e)
            {
                MessageBox.Show("Launch failed. executable=" + executable + "; args=" + args + System.Environment.NewLine + e.ToString());
                return false;
            }
            return true;
        }

        public static void ProcessCommandLine(string Args)
        {
            ProcessCommandLine(Args.SplitCommandLine());
        }

        public static void ProcessCommandLine(IEnumerable<string> Args)
        {

            if (Args == null || Args.Count() == 0)
            {
                ProfileManager.MigrateSettingsToISBEL();
                return;
            }

            List<string> LaunchAccountNames = new List<string>();

            bool useInnerSpace = false;
            foreach (string s in Args)
            {
                switch (s.ToLowerInvariant())
                {
                    case "-dx11":
                        Settings.UseDirectXVersion = DirectXVersion.dx11;
                        break;
                    case "-dx12":
                        Settings.UseDirectXVersion = DirectXVersion.dx12;
                        break;
                    case "-singularity":
                        Settings.UseSingularity = true;
                        break;
                    case "-tranquility":
                        Settings.UseSingularity = false;
                        break;
                    case "-innerspace":
                        useInnerSpace = true;
                        break;
                    case "-eve":
                        useInnerSpace = false;
                        break;
                    case "-multiinstance":
                        break;
                    case "-exit":
                        ExitAfterLaunch = true;
                        break;
                    case "-c":
                        searchCharactersOnly = true;
                        break;
                    case "null":
                        // ignore
                        break;
                    default:
                        LaunchAccountNames.Add(s);
                        break;
                }
            }

            if (LaunchAccountNames.Count == 0)
            {

                return;
            }

            List<ILaunchTarget> LaunchAccounts = new List<ILaunchTarget>();

            foreach (string name in LaunchAccountNames)
            {

                if (!searchCharactersOnly)
                {
                    EVEAccount acct = Settings.Accounts.FirstOrDefault(q => q.Username.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                    if (acct != null)
                    {
                        LaunchAccounts.Add(acct);
                        continue;
                    }
                }
                EVECharacter ec = Settings.Characters.FirstOrDefault(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                if (ec != null)
                {
                    LaunchAccounts.Add(ec);
                    continue;
                }

                MessageBox.Show("Unrecognized EVE Account or Character name '" + name + "' -- if this is correct, please use Add Account/Character to enable it before launching.");
                return;
            }

            ILauncher launcher;
            if (useInnerSpace)
            {
                InnerSpaceGameProfile gp;
                if (Settings.UseSingularity)
                {
                    gp = Settings.SingularityGameProfile;
                }
                else
                {
                    gp = Settings.TranquilityGameProfile;
                }

                if (gp == null || string.IsNullOrEmpty(gp.Game) || string.IsNullOrEmpty(gp.GameProfile))
                {
                    MessageBox.Show("Please select a Game Profile first!");
                    return;
                }

                launcher = new Launchers.InnerSpace(gp, Settings.UseDirectXVersion, Settings.UseSingularity);
            }
            else
            {
                launcher = new Launchers.Direct(Settings.EVESharedCachePath, Settings.UseDirectXVersion, Settings.UseSingularity);
            }
            Windows.LaunchProgressWindow lpw = new Windows.LaunchProgressWindow(LaunchAccounts, launcher);
            lpw.ShowDialog();

            if (ExitAfterLaunch)
            {
                App.Current.Shutdown();
            }
        }

        /// <summary>
        /// Gets what we think is the "master" ISBoxer EVE Launcher instance
        /// </summary>
        /// <param name="ensureMainModule">true if we should be more secure about it (e.g. Master Key transfer), false if we're just passing command-line around</param>
        /// <returns></returns>
        static public Process GetMasterInstance(bool ensureMainModule)
        {
            Process currentProcess = Process.GetCurrentProcess();

            //Note:  https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.mainwindowhandle?view=netframework-4.7
            //When the Main window is hidden, it will return a MainWindowHandle of 0.   This makes the following code fail when trying to find the master window that is minimized to the system tray:
            IEnumerable<Process> processList = Process.GetProcesses().Where(q => q.NameMatches(currentProcess) && (q.MainWindowHandle != IntPtr.Zero || q.Id == currentProcess.Id));

            if (processList == null)
                return null;

            Process[] processes = processList.ToArray();
            Array.Sort(processes, (a, b) => a.StartTime > b.StartTime ? 1 : -1);
            if (processes.Length > 1)
            {
                for (int i = 0; i < processes.Length; i++)
                {
                    if (processes[i].Id == currentProcess.Id)
                        continue;

                    // ensure that the Master Instance is indeed this app by checking the module list? (note: if the other process is Administrator, this one also needs to be Administrator)
                    if (!ensureMainModule)
                        return processes[i];

                    try
                    {

                        if (processes[i].MainModuleNameMatches(currentProcess))
                            return processes[i];
                    }
                    catch (System.ComponentModel.Win32Exception we)
                    {
                        if (we.NativeErrorCode == 5)
                        {
                            // this might be the right instance, but since it's Administrator and we're not, we can't use it.
                            continue;
                        }
                        throw;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Transmit the command-line to an already-running Master instance, if one is available
        /// </summary>
        /// <returns></returns>
        public bool TransmitCommandLine()
        {
            // check if it's already running.

            System.Diagnostics.Process masterInstance = GetMasterInstance(false);

            if (masterInstance != null)
            {
                string joinedCommandLine = string.Empty;
                foreach (string s in CommandLine)
                {
                    // if it's another process, don't exit
                    switch (s.ToLowerInvariant())
                    {
                        case "null":
                            // ignore...
                            break;
                        case "-exit":
                            // the -exit is intended for THIS process, not the other one.
                            break;
                        case "-multiinstance":
                            return false;
                    }

                    if (!s.Equals("-exit", StringComparison.InvariantCultureIgnoreCase))
                        joinedCommandLine += "\"" + s.Replace("\"", "\\\"") + "\" ";
                }
                joinedCommandLine.TrimEnd();

                byte[] buff = Encoding.Unicode.GetBytes(joinedCommandLine);

                Windows.COPYDATASTRUCT cds = new Windows.COPYDATASTRUCT();
                cds.cbData = buff.Length;
                cds.lpData = Marshal.AllocHGlobal(buff.Length);
                Marshal.Copy(buff, 0, cds.lpData, buff.Length);
                cds.dwData = IntPtr.Zero;
                cds.cbData = buff.Length;
                var ret = ISBoxerEVELauncher.Windows.MainWindow.SendMessage(masterInstance.MainWindowHandle, ISBoxerEVELauncher.Windows.MainWindow.WM_COPYDATA, IntPtr.Zero, ref cds);
                Marshal.FreeHGlobal(cds.lpData);

                Shutdown();
                return true;
            }
            return false;
        }

        private void ApplicationStart(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            // Allows the operating system to choose the best protocol to use, and to block protocols that are not secure. Unless your app has a specific reason not to, you should use this value.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;


            CommandLine = e.Args;

            if (!TransmitCommandLine())
            {
                ReloadGameConfiguration();

                if (GameConfiguration != null || System.IO.File.Exists(ISExecutable))
                {
                    HasInnerSpace = true;
                }

                tofCaptcha = false;

                var mainWindow = new Windows.MainWindow();
                //Re-enable normal shutdown mode.
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Current.MainWindow = mainWindow;
                mainWindow.Show();

                ProcessCommandLine(CommandLine);
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Exception unhandled by ISBoxer EVE Launcher: " + e.ExceptionObject.ToString());
        }

    }
}
