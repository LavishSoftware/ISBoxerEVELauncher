using ISBoxerEVELauncher.Enums;
using ISBoxerEVELauncher.Games.EVE;
using ISBoxerEVELauncher.InnerSpace;
using ISBoxerEVELauncher.Security;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace ISBoxerEVELauncher
{
    /// <summary>
    /// Any data we want to store, and a little that we don't.
    /// </summary>
    public class Settings : IDisposable, INotifyPropertyChanged
    {
        public Settings()
        {
            Accounts = new ObservableCollection<EVEAccount>();
            Characters = new ObservableCollection<EVECharacter>();
            EULAAccepted = DateTime.MinValue;
            MasterKeyRequested = DateTime.MinValue;
            EVESharedCachePath = App.DetectedEVESharedCachePath;
            _LaunchDelay = 2;
        }

        /// <summary>
        /// List of EVE Accounts we've presumably verified access to. (A user may have manually edited the Settings file.)
        /// </summary>
        public ObservableCollection<EVEAccount> Accounts
        {
            get; set;
        }

        /// <summary>
        /// List of known EVE Characters, for use with auto-login
        /// </summary>
        public ObservableCollection<EVECharacter> Characters
        {
            get; set;
        }


        public EVEAccount FindEVEAccount(string name)
        {
            return App.Settings.Accounts.FirstOrDefault(q => q.Username.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
        public EVECharacter FindEVECharacter(bool sisi, string name)
        {
            return App.Settings.Characters.FirstOrDefault(q => q.UseSingularity = sisi && q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        InnerSpaceGameProfile _TranquilityGameProfile;
        /// <summary>
        /// An Inner Space Game Profile to use for launching Tranquility clients
        /// </summary>
        public InnerSpaceGameProfile TranquilityGameProfile
        {
            get
            {
                return _TranquilityGameProfile;
            }
            set
            {
                _TranquilityGameProfile = value;
                OnPropertyChanged("TranquilityGameProfile");
            }
        }

        InnerSpaceGameProfile _SingularityGameProfile;
        /// <summary>
        /// An Inner Space Game Profile to use for launching Singularity clients
        /// </summary>
        public InnerSpaceGameProfile SingularityGameProfile
        {
            get
            {
                return _SingularityGameProfile;
            }
            set
            {
                _SingularityGameProfile = value;
                OnPropertyChanged("SingularityGameProfile");
            }
        }

        bool _UseSingularity;
        /// <summary>
        /// If Singularity is to be used for in-app Launch button clicks
        /// </summary>
        public bool UseSingularity
        {
            get
            {
                return _UseSingularity;
            }
            set
            {
                _UseSingularity = value;
                OnPropertyChanged("UseSingularity");
            }
        }

        float _LaunchDelay;
        /// <summary>
        /// Delay between game launches, in seconds
        /// </summary>
        public float LaunchDelay
        {
            get
            {
                return _LaunchDelay;
            }
            set
            {
                _LaunchDelay = value;
                OnPropertyChanged("LaunchDelay");
            }
        }

        DirectXVersion _UseDirectXVersion;
        /// <summary>
        /// DirectX version to be used for in-app Launch button clicks
        /// </summary>
        public DirectXVersion UseDirectXVersion
        {
            get
            {
                return _UseDirectXVersion;
            }
            set
            {
                _UseDirectXVersion = value;
                OnPropertyChanged("UseDirectXVersion");
            }
        }

        string _EVESharedCachePath;
        /// <summary>
        /// Path to SharedCache
        /// </summary>
        public string EVESharedCachePath
        {
            get
            {
                return _EVESharedCachePath;
            }
            set
            {
                _EVESharedCachePath = value;
                OnPropertyChanged("EVESharedCachePath");
            }
        }


        string _machineHash;
        public string MachineHash
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_machineHash))
                {
                    MachineHash = Guid.NewGuid().ToString().Replace("-", "");
                }
                return _machineHash;
            }
            set
            {
                _machineHash = value;
                OnPropertyChanged("MachineHash");
            }
        }



        public string GetTranquilityPath()
        {
            if (string.IsNullOrEmpty(EVESharedCachePath))
                return null;
            return Path.Combine(EVESharedCachePath, "tq");
        }

        public string GetTranquilityEXE()
        {
            if (string.IsNullOrEmpty(EVESharedCachePath))
                return null;
            return Path.Combine(GetTranquilityPath(), "bin64\\exefile.exe");
        }
        public string GetSingularityPath()
        {
            if (string.IsNullOrEmpty(EVESharedCachePath))
                return null;
            return Path.Combine(EVESharedCachePath, "sisi");
        }
        public string GetSingularityEXE()
        {
            if (string.IsNullOrEmpty(EVESharedCachePath))
                return null;
            return Path.Combine(GetSingularityPath(), "bin64\\exefile.exe");
        }

        string _MasterKeyCheck;
        /// <summary>
        /// A SHA256 hash encoded as Base64, used to check whether the user entered the correct Master Password
        /// </summary>
        public string MasterKeyCheck
        {
            get
            {
                return _MasterKeyCheck;
            }
            set
            {
                _MasterKeyCheck = value;
                OnPropertyChanged("MasterKeyCheck");
                OnPropertyChanged("UseMasterKey");
            }
        }

        string _MasterKeyCheckIV;
        /// <summary>
        /// A Base64 Initialization Vector used to ensure the MasterKeyCheck is not the same twice for the same Master Password
        /// </summary>
        public string MasterKeyCheckIV
        {
            get
            {
                return _MasterKeyCheckIV;
            }
            set
            {
                _MasterKeyCheckIV = value;
                OnPropertyChanged("MasterKeyCheckIV");
            }
        }

        DateTime _EULAAccepted;
        /// <summary>
        /// We'll show the EULA because CCP will prefer it and we're all friends here. But only when it has actually changed since our user has been shown the EULA, if we can help it...
        /// </summary>
        public DateTime EULAAccepted
        {
            get
            {
                return _EULAAccepted;
            }
            set
            {
                _EULAAccepted = value;
                OnPropertyChanged("EULAAccepted");
            }
        }

        /// <summary>
        /// Has a Master Key been configured?
        /// </summary>
        [XmlIgnore]
        public bool UseMasterKey
        {
            get
            {
                return !string.IsNullOrEmpty(MasterKeyCheck);
            }
        }

        /// <summary>
        /// Have we *entered* the Password Master Key since launching this app? ...
        /// </summary>
        /// <returns></returns>
        [XmlIgnore]
        public bool HasPasswordMasterKey
        {
            get
            {
                return PasswordMasterKey != null && PasswordMasterKey.HasData;
            }
        }

        SecureStringWrapper _PasswordMasterKey;
        /// <summary>
        /// The already-encrypted Master Password. The Password itself is discarded and wiped.
        /// </summary>
        [XmlIgnore]
        public SecureStringWrapper PasswordMasterKey
        {
            get
            {
                return _PasswordMasterKey;
            }
            set
            {
                _PasswordMasterKey = value;
                OnPropertyChanged("PasswordMasterKey");
                OnPropertyChanged("HasPasswordMasterKey");
            }
        }

        DateTime _MasterKeyRequested;
        /// <summary>
        /// The time Master Key was requested from the master ISBoxer EVE Launcher instance
        /// </summary>
        [XmlIgnore]
        public DateTime MasterKeyRequested
        {
            get
            {
                return _MasterKeyRequested;
            }
            set
            {
                _MasterKeyRequested = value;
                OnPropertyChanged("MasterKeyRequested");
            }
        }


        /// <summary>
        /// This is used to generate the Master Key Check, along with an IV and the Master Key
        /// </summary>
        const string MasterKeyCheckPlaintext = "ISBoxerEVELauncher";

        /// <summary>
        /// This is used as Salt for building a Master Key from the Master Password, intending to compensate for YOUR shitty Master Password. That's right, it's YOUR fault.
        /// </summary>
        const string MasterKeySaltString = "Salt and pepper the steak before grilling";
        /// <summary>
        /// The Salt String stored as a Unicode Byte array, since that's what we'll be using later.
        /// </summary>
        static byte[] MasterKeySalt = Encoding.Unicode.GetBytes(MasterKeySaltString);

        /// <summary>
        /// If a Master Key has been configured, but not entered this session, request it from the user
        /// </summary>
        public bool RequestMasterPassword()
        {
            if (UseMasterKey && (PasswordMasterKey == null || !PasswordMasterKey.HasData))
            {
                Windows.MasterKeyEntryWindow mkew = new Windows.MasterKeyEntryWindow();
                mkew.ShowDialog();
            }
            return HasPasswordMasterKey;
        }

        /// <summary>
        /// Using the Password Master Key, generate a hash that can be tested to check if we enter the correct Master Password
        /// </summary>
        void GenerateMasterKeyCheck()
        {
            using (RijndaelManaged rjmIVGenerator = new RijndaelManaged())
            {
                rjmIVGenerator.GenerateIV();
                MasterKeyCheckIV = Convert.ToBase64String(rjmIVGenerator.IV);

                using (SecureBytesWrapper sbwPreHash = new SecureBytesWrapper())
                {
                    byte[] plaintextBytes = Encoding.Unicode.GetBytes(MasterKeyCheckPlaintext);
                    using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey, true))
                    {
                        sbwPreHash.Bytes = new byte[rjmIVGenerator.IV.Length + plaintextBytes.Length + sbwKey.Bytes.Length];

                        System.Buffer.BlockCopy(rjmIVGenerator.IV, 0, sbwPreHash.Bytes, 0, rjmIVGenerator.IV.Length);
                        System.Buffer.BlockCopy(plaintextBytes, 0, sbwPreHash.Bytes, rjmIVGenerator.IV.Length, plaintextBytes.Length);
                        System.Buffer.BlockCopy(sbwKey.Bytes, 0, sbwPreHash.Bytes, rjmIVGenerator.IV.Length + plaintextBytes.Length, sbwKey.Bytes.Length);
                    }
                    using (SHA256Managed sha = new SHA256Managed())
                    {
                        // convert to Base64 and this is our check
                        MasterKeyCheck = Convert.ToBase64String(sha.ComputeHash(sbwPreHash.Bytes));
                    }
                }
            }
        }

        /// <summary>
        /// Determine if the user has entered the correct Master Password, by testing with MasterKeyCheck and friends. If it is, keep the Master Key.
        /// </summary>
        /// <param name="masterPassword"></param>
        /// <returns></returns>
        public bool TryMasterPassword(System.Security.SecureString masterPassword)
        {
            if (masterPassword == null || masterPassword.Length < 1)
                return false;
            if (string.IsNullOrEmpty(MasterKeyCheck) || string.IsNullOrEmpty(MasterKeyCheckIV))
                return false;

            using (SecureBytesWrapper sbwKey = new SecureBytesWrapper())
            {
                // first we need to create a key out of the password.
                using (SHA256Managed sha = new SHA256Managed())
                {
                    using (SecureStringWrapper ssw = new SecureStringWrapper(masterPassword))
                    {
                        byte[] passwordBytes = ssw.ToByteArray();

                        using (SecureBytesWrapper sbw = new SecureBytesWrapper())
                        {
                            sbw.Bytes = new byte[MasterKeySalt.Length + passwordBytes.Length];

                            System.Buffer.BlockCopy(MasterKeySalt, 0, sbw.Bytes, 0, MasterKeySalt.Length);
                            System.Buffer.BlockCopy(passwordBytes, 0, sbw.Bytes, MasterKeySalt.Length, passwordBytes.Length);


                            sbwKey.Bytes = sha.ComputeHash(sbw.Bytes);

                        }
                    }
                }

                return TryPasswordMasterKey(sbwKey.Bytes);
            }
        }

        public void SetPasswordMasterKeyBytes(byte[] bytes)
        {
            PasswordMasterKey = SecureStringWrapper.ConvertToHex(bytes);
        }

        /// <summary>
        /// Using the provided Master Password, set the *new* Master Key. Populates Master Key Check, and encrypts+stores already-entered EVE Account passwords
        /// </summary>
        /// <param name="masterPassword"></param>
        public void SetPasswordMasterKey(System.Security.SecureString masterPassword)
        {
            ClearPasswordMasterKey();
            if (masterPassword == null || masterPassword.Length < 1)
            {
                return;
            }
            using (SHA256Managed sha = new SHA256Managed())
            {
                using (SecureStringWrapper ssw = new SecureStringWrapper(masterPassword))
                {
                    byte[] passwordBytes = ssw.ToByteArray();

                    using (SecureBytesWrapper sbw = new SecureBytesWrapper())
                    {
                        sbw.Bytes = new byte[MasterKeySalt.Length + passwordBytes.Length];

                        System.Buffer.BlockCopy(MasterKeySalt, 0, sbw.Bytes, 0, MasterKeySalt.Length);
                        System.Buffer.BlockCopy(passwordBytes, 0, sbw.Bytes, MasterKeySalt.Length, passwordBytes.Length);

                        sbw.Bytes = sha.ComputeHash(sbw.Bytes);

                        SetPasswordMasterKeyBytes(sbw.Bytes);
                    }
                }
            }
            GenerateMasterKeyCheck();
            foreach (EVEAccount account in Accounts)
            {
                account.EncryptPassword();
                account.EncryptCharacterName();
            }
            Store();
        }

        /// <summary>
        /// Determine if this is the correct Master Key, by testing with MasterKeyCheck and friends. If it is, keep the Master Key.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public bool TryPasswordMasterKey(byte[] bytes)
        {
            if (string.IsNullOrEmpty(MasterKeyCheck) || string.IsNullOrEmpty(MasterKeyCheckIV))
                return false;

            byte[] masterKeyCheckIV = Convert.FromBase64String(MasterKeyCheckIV);

            using (SecureBytesWrapper sbwPreHash = new SecureBytesWrapper())
            {
                byte[] plaintextBytes = Encoding.Unicode.GetBytes(MasterKeyCheckPlaintext);
                sbwPreHash.Bytes = new byte[masterKeyCheckIV.Length + plaintextBytes.Length + bytes.Length];

                System.Buffer.BlockCopy(masterKeyCheckIV, 0, sbwPreHash.Bytes, 0, masterKeyCheckIV.Length);
                System.Buffer.BlockCopy(plaintextBytes, 0, sbwPreHash.Bytes, masterKeyCheckIV.Length, plaintextBytes.Length);
                System.Buffer.BlockCopy(bytes, 0, sbwPreHash.Bytes, masterKeyCheckIV.Length + plaintextBytes.Length, bytes.Length);

                using (SHA256Managed sha = new SHA256Managed())
                {
                    // convert to Base64 and this is our check
                    if (!MasterKeyCheck.Equals(Convert.ToBase64String(sha.ComputeHash(sbwPreHash.Bytes))))
                    {
                        return false;
                    }
                }
            }

            SetPasswordMasterKeyBytes(bytes);
            return true;
        }

        /// <summary>
        /// Clear out our Password Master Key (if we've even entered it...), and remove all encrypted+stored passwords
        /// </summary>
        public void ClearPasswordMasterKey()
        {
            foreach (EVEAccount account in Accounts)
            {
                account.ClearEncryptedPassword();
                account.ClearEncryptedCharacterName();
            }
            MasterKeyCheck = null;
            MasterKeyCheckIV = null;
            if (PasswordMasterKey != null)
            {
                PasswordMasterKey.Dispose();
            }
            PasswordMasterKey = null;
            Store();
        }

        /// <summary>
        /// Load XML Settings from the DefaultFilename
        /// </summary>
        /// <returns></returns>
        public static Settings Load()
        {
            return Load(DefaultFilename);
        }

        /// <summary>
        /// Load XML Settings! (I am good at comments)
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Settings Load(string filename)
        {
            try
            {
                XmlSerializer s = new XmlSerializer(typeof(Settings));
                using (TextReader r = new StreamReader(filename, System.Text.Encoding.UTF8))
                {
                    Settings settings = (Settings)s.Deserialize(r);

                    settings.SingularityGameProfile = App.FindGlobalGameProfile(settings.SingularityGameProfile);
                    settings.TranquilityGameProfile = App.FindGlobalGameProfile(settings.TranquilityGameProfile);

                    return settings;
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                throw;
            }
            catch (Exception)
            {
                //                MessageBox.Show("Error loading file " + filename + "... " + Environment.NewLine + e.ToString());
                //                return null;
                throw;
            }
        }

        /// <summary>
        /// Where we find the Settings file.
        /// </summary>
        public static string DefaultFilename
        {
            get
            {
                return App.ISBoxerEVELauncherPath + @"\ISBoxerEVELauncher.Settings.XML";
            }
        }

        /// <summary>
        /// Stores in XML to the DefaultFilename
        /// </summary>
        public void Store()
        {
            Store(DefaultFilename);
        }


        bool cannotSave = false;

        /// <summary>
        /// Stores Settings in XML!
        /// </summary>
        /// <param name="filename"></param>
        public void Store(string filename)
        {
            foreach (EVEAccount a in Accounts)
            {
                a.PrepareStorage();
            }
            try
            {
                using (TextWriter w = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
                {
                    XmlSerializer s = new XmlSerializer(typeof(Settings));
                    s.Serialize(w, this);
                }
            }
            catch (UnauthorizedAccessException)
            {
                if (cannotSave)
                    return;
                cannotSave = true;

                System.Windows.MessageBox.Show("ISBoxer EVE Launcher cannot save its Settings to " + filename + ". You may need to Run as Administrator!");
                return;
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// Get rid of the evidence
        /// </summary>
        public void Dispose()
        {
            if (PasswordMasterKey != null)
            {
                PasswordMasterKey.Dispose();
                PasswordMasterKey = null;
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

    }
}
