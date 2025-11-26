//#define REFRESH_TOKENS

using ISBoxerEVELauncher.Enums;
using ISBoxerEVELauncher.Extensions;
using ISBoxerEVELauncher.Interface;
using ISBoxerEVELauncher.Security;
using ISBoxerEVELauncher.Web;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using ISBoxerEVELauncher.Windows;


namespace ISBoxerEVELauncher.Games.EVE
{


    /// <summary>
    /// An EVE Online account and related data
    /// </summary>
    public class EVEAccount : INotifyPropertyChanged, IDisposable, ILaunchTarget
    {
        private const string LogCategory = "EVEAccount";

        [XmlIgnore]
        private Guid challengeCodeSource;
        [XmlIgnore]
        private byte[] challengeCode;
        [XmlIgnore]
        private string challengeHash;
        [XmlIgnore]
        private Guid state;
        [XmlIgnore]
        private string code;

        public string Profile
        {
            get; set;
        }


        public EVEAccount()
        {
            state = Guid.NewGuid();
            challengeCodeSource = Guid.NewGuid();
            challengeCode = Encoding.UTF8.GetBytes(challengeCodeSource.ToString().Replace("-", ""));
            challengeHash = Base64UrlEncoder.Encode(ISBoxerEVELauncher.Security.SHA256.GenerateHash(Base64UrlEncoder.Encode(challengeCode)));
            Profile = "Default";
        }



        /// <summary>
        /// An Outh2 Access Token
        /// </summary>
        public class Token
        {
            private authObj _authObj;
            public Token()
            {

            }

            /// <summary>
            /// We usually just need to parse a Uri for the Access Token details. So here is the constructor that does it for us.
            /// </summary>
            /// <param name="fromUri"></param>
            public Token(authObj resp)
            {
                _authObj = resp;
                TokenString = resp.access_token;
                RefreshToken = resp.refresh_token;
                Expiration = DateTime.Now.AddSeconds(resp.expires_in).AddMinutes(-1);
            }

            public override string ToString()
            {
                return TokenString;
            }

            /// <summary>
            /// Determine if the Access Token is expired. If it is, we know we can't use it...
            /// </summary>
            public bool IsExpired
            {
                get
                {
                    return DateTime.Now >= Expiration;
                }
            }

            /// <summary>
            /// The actual token data
            /// </summary>
            public string TokenString
            {
                get; set;
            }

            public string RefreshToken
            {
                get; set;
            }

            /// <summary>
            /// When the token is good until...
            /// </summary>
            public DateTime Expiration
            {
                get; set;
            }
        }

        CookieContainer _Cookies;

        /// <summary>
        /// The EVE login process requires cookies; this will ensure we maintain the same cookies for the account
        /// </summary>
        [XmlIgnore]
        CookieContainer Cookies
        {
            get
            {
                if (_Cookies == null)
                {
                    if (!string.IsNullOrEmpty(NewCookieStorage))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();


                        using (Stream s = new MemoryStream(Convert.FromBase64String(NewCookieStorage)))
                        {
                            _Cookies = (CookieContainer)formatter.Deserialize(s);
                        }
                    }
                    else
                        _Cookies = new CookieContainer();
                }
                return _Cookies;
            }
            set
            {
                _Cookies = value;
            }
        }

        public void UpdateCookieStorage()
        {
            if (Cookies == null)
            {
                NewCookieStorage = null;
                return;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, Cookies);
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                NewCookieStorage = Convert.ToBase64String(ms.ToArray());
            }

        }

        string _Username;
        /// <summary>
        /// EVE Account username
        /// </summary>
        public string Username
        {
            get
            {
                return _Username;
            }
            set
            {
                _Username = value;
                OnPropertyChanged("Username");
            }
        }

        /// <summary>
        /// Old cookie storage. If found in the XML, it will automatically be split into separate storage
        /// </summary>
        public string CookieStorage
        {
            get
            {
                return null;// return ISBoxerEVELauncher.CookieStorage.GetCookies(this);
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ISBoxerEVELauncher.Web.CookieStorage.SetCookies(this, value);
                    EVEAccount.ShouldUgradeCookieStorage = true;
                }
            }
        }

        public static bool ShouldUgradeCookieStorage
        {
            get; private set;
        }
        /// <summary>
        /// New method of storing cookies
        /// </summary>
        [XmlIgnore]
        public string NewCookieStorage
        {
            get
            {
                return ISBoxerEVELauncher.Web.CookieStorage.GetCookies(this);
            }
            set
            {
                ISBoxerEVELauncher.Web.CookieStorage.SetCookies(this, value);
            }
        }

        /// <summary>
        /// WebView2 cookie storage (JSON format) - separate from HttpWebRequest cookies
        /// </summary>
        [XmlIgnore]
        public string WebView2CookieStorage
        {
            get
            {
                return ISBoxerEVELauncher.Web.CookieStorage.GetWebViewCookies(this);
            }
            set
            {
                ISBoxerEVELauncher.Web.CookieStorage.SetWebViewCookies(this, value);
            }
        }


        #region Password
        System.Security.SecureString _SecurePassword;
        /// <summary>
        /// A Secure (and non-plaintext) representation of the password. This will NOT be stored in XML.
        /// </summary>
        [XmlIgnore]
        public System.Security.SecureString SecurePassword
        {
            get
            {
                return _SecurePassword;
            }
            set
            {
                _SecurePassword = value;
                OnPropertyChanged("SecurePassword");
                EncryptedPassword = null;
                EncryptedPasswordIV = null;
            }
        }

        string _EncryptedPassword;
        /// <summary>
        /// An encrypted version of the password for the account. It is protected by the Password Master Key. Changing the Password Master Key will wipe this.
        /// </summary>
        public string EncryptedPassword
        {
            get
            {
                return _EncryptedPassword;
            }
            set
            {
                _EncryptedPassword = value;
                OnPropertyChanged("EncryptedPassword");
            }
        }

        string _EncryptedPasswordIV;
        /// <summary>
        /// The Initialization Vector used to encrypt the password
        /// </summary>
        public string EncryptedPasswordIV
        {
            get
            {
                return _EncryptedPasswordIV;
            }
            set
            {
                _EncryptedPasswordIV = value;
                OnPropertyChanged("EncryptedPasswordIV");
            }
        }

        /// <summary>
        /// Sets the encrypted password to the given SecureString, if possible
        /// </summary>
        /// <param name="password"></param>
        void SetEncryptedPassword(System.Security.SecureString password)
        {
            if (!App.Settings.UseMasterKey || password == null)
            {
                ClearEncryptedPassword();
                return;
            }

            if (!App.Settings.RequestMasterPassword())
            {
                System.Windows.MessageBox.Show("Your configured Master Password is required in order to save EVE Account passwords. It can be reset or disabled by un-checking 'Save passwords (securely)', and then all currently saved EVE Account passwords will be lost.");
                return;
            }

            using (RijndaelManaged rjm = new RijndaelManaged())
            {
                if (string.IsNullOrEmpty(EncryptedPasswordIV))
                {
                    rjm.GenerateIV();
                    EncryptedPasswordIV = Convert.ToBase64String(rjm.IV);
                }
                else
                    rjm.IV = Convert.FromBase64String(EncryptedPasswordIV);

                using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey, true))
                {
                    rjm.Key = sbwKey.Bytes;

                    using (ICryptoTransform encryptor = rjm.CreateEncryptor())
                    {
                        using (SecureStringWrapper ssw2 = new SecureStringWrapper(password, Encoding.Unicode))
                        {
                            byte[] inblock = ssw2.ToByteArray();
                            byte[] encrypted = encryptor.TransformFinalBlock(inblock, 0, inblock.Length);
                            EncryptedPassword = Convert.ToBase64String(encrypted);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Attempts to prepare the encrypted verison of the currently active SecurePassword
        /// </summary>
        public void EncryptPassword()
        {
            SetEncryptedPassword(SecurePassword);
        }

        /// <summary>
        /// Prepares the EVEAccount for storage by ensuring that the Encrypted fields are set, if available
        /// </summary>
        public void PrepareStorage()
        {
            if (SecurePassword != null)
            {
                EncryptPassword();
            }
            if (SecureCharacterName != null)
            {
                EncryptCharacterName();
            }
            if (SecureTranquilityRefreshToken != null)
            {
                EncryptTranquilityRefreshToken();
            }
            if (SecureSisiRefreshToken != null)
            {
                EncryptSisiRefreshToken();
            }
        }

        /// <summary>
        /// Removes the encrypted password and IV
        /// </summary>
        public void ClearEncryptedPassword()
        {
            EncryptedPassword = null;
            EncryptedPasswordIV = null;
        }

        /// <summary>
        /// Decrypts the currently EncryptedPassword if possible, populating SecurePassword (which can then be used to log in...)
        /// </summary>
        public void DecryptPassword(bool allowPopup)
        {
            if (string.IsNullOrEmpty(EncryptedPassword) || string.IsNullOrEmpty(EncryptedPasswordIV))
            {
                // no password stored to decrypt.
                return;
            }
            // password is indeed encrypted

            if (!App.Settings.HasPasswordMasterKey)
            {
                // Master Password not yet entered
                if (!allowPopup)
                {
                    // can't ask for it right now
                    return;
                }

                // ok, ask for it
                if (!App.Settings.RequestMasterPassword())
                {
                    // not entered. can't decrypt.
                    return;
                }
            }
            using (RijndaelManaged rjm = new RijndaelManaged())
            {
                rjm.IV = Convert.FromBase64String(EncryptedPasswordIV);

                using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey, true))
                {
                    rjm.Key = sbwKey.Bytes;
                    using (ICryptoTransform decryptor = rjm.CreateDecryptor())
                    {
                        byte[] pass = Convert.FromBase64String(EncryptedPassword);

                        using (SecureBytesWrapper sbw = new SecureBytesWrapper())
                        {
                            sbw.Bytes = decryptor.TransformFinalBlock(pass, 0, pass.Length);

                            SecurePassword = new System.Security.SecureString();
                            foreach (char c in Encoding.Unicode.GetChars(sbw.Bytes))
                            {
                                SecurePassword.AppendChar(c);
                            }
                            SecurePassword.MakeReadOnly();
                        }
                    }
                }
            }
        }
        #endregion

        #region CharacterName
        System.Security.SecureString _SecureCharacterName;
        /// <summary>
        /// A Secure (and non-plaintext) representation of the CharacterName. This will NOT be stored in XML.
        /// </summary>
        [XmlIgnore]
        public System.Security.SecureString SecureCharacterName
        {
            get
            {
                return _SecureCharacterName;
            }
            set
            {
                _SecureCharacterName = value;
                OnPropertyChanged("SecureCharacterName");
                EncryptedCharacterName = null;
                EncryptedCharacterNameIV = null;
            }
        }

        string _EncryptedCharacterName;
        /// <summary>
        /// An encrypted version of the CharacterName for the account. It is protected by the CharacterName Master Key. Changing the CharacterName Master Key will wipe this.
        /// </summary>
        public string EncryptedCharacterName
        {
            get
            {
                return _EncryptedCharacterName;
            }
            set
            {
                _EncryptedCharacterName = value;
                OnPropertyChanged("EncryptedCharacterName");
            }
        }

        string _EncryptedCharacterNameIV;
        /// <summary>
        /// The Initialization Vector used to encrypt the CharacterName
        /// </summary>
        public string EncryptedCharacterNameIV
        {
            get
            {
                return _EncryptedCharacterNameIV;
            }
            set
            {
                _EncryptedCharacterNameIV = value;
                OnPropertyChanged("EncryptedCharacterNameIV");
            }
        }

        /// <summary>
        /// Attempts to prepare the encrypted verison of the currently active SecureCharacterName
        /// </summary>
        public void EncryptCharacterName()
        {
            SetEncryptedCharacterName(SecureCharacterName);
        }

        /// <summary>
        /// Removes the encrypted CharacterName and IV
        /// </summary>
        public void ClearEncryptedCharacterName()
        {
            EncryptedCharacterName = null;
            EncryptedCharacterNameIV = null;
        }

        /// <summary>
        /// Sets the encrypted CharacterName to the given SecureString, if possible
        /// </summary>
        /// <param name="CharacterName"></param>
        void SetEncryptedCharacterName(System.Security.SecureString CharacterName)
        {
            if (!App.Settings.UseMasterKey || CharacterName == null)
            {
                ClearEncryptedCharacterName();
                return;
            }

            if (!App.Settings.RequestMasterPassword())
            {
                System.Windows.MessageBox.Show("Your configured Master Password is required in order to save EVE Account Character Names and passwords. It can be reset or disabled by un-checking 'Save passwords (securely)', and then all currently saved EVE Account Character Names will be lost.");
                return;
            }

            using (RijndaelManaged rjm = new RijndaelManaged())
            {
                if (string.IsNullOrEmpty(EncryptedCharacterNameIV))
                {
                    rjm.GenerateIV();
                    EncryptedCharacterNameIV = Convert.ToBase64String(rjm.IV);
                }
                else
                    rjm.IV = Convert.FromBase64String(EncryptedCharacterNameIV);

                using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey, true))
                {
                    rjm.Key = sbwKey.Bytes;

                    using (ICryptoTransform encryptor = rjm.CreateEncryptor())
                    {
                        using (SecureStringWrapper ssw2 = new SecureStringWrapper(CharacterName, Encoding.Unicode))
                        {
                            byte[] inblock = ssw2.ToByteArray();
                            byte[] encrypted = encryptor.TransformFinalBlock(inblock, 0, inblock.Length);
                            EncryptedCharacterName = Convert.ToBase64String(encrypted);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts the currently EncryptedCharacterName if possible, populating SecureCharacterName (which can then be used to log in...)
        /// </summary>
        public void DecryptCharacterName(bool allowPopup)
        {
            if (string.IsNullOrEmpty(EncryptedCharacterName) || string.IsNullOrEmpty(EncryptedCharacterNameIV))
            {
                // no CharacterName stored to decrypt.
                return;
            }
            // CharacterName is indeed encrypted

            if (!App.Settings.HasPasswordMasterKey)
            {
                // Master CharacterName not yet entered
                if (!allowPopup)
                {
                    // can't ask for it right now
                    return;
                }

                // ok, ask for it
                if (!App.Settings.RequestMasterPassword())
                {
                    // not entered. can't decrypt.
                    return;
                }
            }
            using (RijndaelManaged rjm = new RijndaelManaged())
            {
                rjm.IV = Convert.FromBase64String(EncryptedCharacterNameIV);

                using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey, true))
                {
                    rjm.Key = sbwKey.Bytes;
                    using (ICryptoTransform decryptor = rjm.CreateDecryptor())
                    {
                        byte[] pass = Convert.FromBase64String(EncryptedCharacterName);

                        using (SecureBytesWrapper sbw = new SecureBytesWrapper())
                        {
                            sbw.Bytes = decryptor.TransformFinalBlock(pass, 0, pass.Length);

                            SecureCharacterName = new System.Security.SecureString();
                            foreach (char c in Encoding.Unicode.GetChars(sbw.Bytes))
                            {
                                SecureCharacterName.AppendChar(c);
                            }
                            SecureCharacterName.MakeReadOnly();
                        }
                    }
                }
            }
        }
        #endregion


        #region tokens
        private Token _TranquilityToken;
        /// <summary>
        /// AccessToken for Tranquility. Lasts up to 11 hours?
        /// </summary>
        [XmlIgnore]
        public Token TranquilityToken
        {
            get
            {
                return _TranquilityToken;
            }
            set
            {
                _TranquilityToken = value;
                if (value != null) 
                    SetEncryptedTranquilityRefreshToken(value.RefreshToken.ToSecureString());
                OnPropertyChanged("TranquilityToken");
            }
        }

        private Token _SisiToken;
        /// <summary>
        /// AccessToken for Singularity. Lasts up to 11 hours?
        /// </summary>
        [XmlIgnore]
        public Token SisiToken
        {
            get
            {
                return _SisiToken;
            }
            set
            {
                _SisiToken = value;
                if (value != null)
                    SetEncryptedSisiRefreshToken(value.RefreshToken.ToSecureString());
                OnPropertyChanged("SisiToken");
            }
        }
        
        private System.Security.SecureString _secureTranquilityRefreshToken; 
        
        /// <summary>
        /// Secure Refresh Token for Tranquility
        /// </summary>
        [XmlIgnore]
        public System.Security.SecureString SecureTranquilityRefreshToken
        {
            get
            {
                return _secureTranquilityRefreshToken;
            }
            set
            {
                _secureTranquilityRefreshToken = value;
                OnPropertyChanged("SecureTranquilityRefreshToken");
            }
        }
        

        private string _encryptedTranquilityRefreshToken;
        
        /// <summary>
        /// Encrypted Refresh Token for Tranquility
        /// </summary>
        public string EncryptedTranquilityRefreshToken
        {
            get
            {
                return _encryptedTranquilityRefreshToken;
            }
            set
            {
                _encryptedTranquilityRefreshToken = value;
                OnPropertyChanged("EncryptedTranquilityRefreshToken");
            }
        }
        
        private string _encryptedTranquilityRefreshTokenIV;

        /// <summary>
        /// Encrypted Refresh Token IV for Tranquility
        /// </summary>
        public string EncryptedTranquilityRefreshTokenIV
        {
            get
            {
                return _encryptedTranquilityRefreshTokenIV;
            }
            set
            {
                _encryptedTranquilityRefreshTokenIV = value;
                OnPropertyChanged("EncryptedTranquilityRefreshTokenIV");
            }
        }
        
        private DateTime? _tranquilityRefreshTokenCreatedAt;
        
        /// <summary>
        /// When the Tranquility Refresh Token was created
        /// </summary>
        public DateTime? TranquilityRefreshTokenCreatedAt
        {
            get
            {
                return _tranquilityRefreshTokenCreatedAt;
            }
            set
            {
                _tranquilityRefreshTokenCreatedAt = value;
                OnPropertyChanged("TranquilityRefreshTokenCreatedAt");
            }
        }
        

        private System.Security.SecureString _secureSisiRefreshToken;

        /// <summary>
        /// Secure Refresh Token for Sisi (Singularity)
        /// </summary>
        [XmlIgnore]
        public System.Security.SecureString SecureSisiRefreshToken
        {
            get
            {
                return _secureSisiRefreshToken;
            }
            set
            {
                _secureSisiRefreshToken = value;
                OnPropertyChanged("SecureSisiRefreshToken");
            }
        }

        private string _encryptedSisiRefreshToken;

        /// <summary>
        /// Encrypted Refresh Token for Sisi (Singularity)
        /// </summary>
        public string EncryptedSisiRefreshToken
        {
            get
            {
                return _encryptedSisiRefreshToken;
            }
            set
            {
                _encryptedSisiRefreshToken = value;
                OnPropertyChanged("EncryptedSisiRefreshToken");
            }
        }

        private string _encryptedSisiRefreshTokenIV;

        /// <summary>
        /// Encrypted Refresh Token IV for Sisi (Singularity)
        /// </summary>
        public string EncryptedSisiRefreshTokenIV
        {
            get
            {
                return _encryptedSisiRefreshTokenIV;
            }
            set
            {
                _encryptedSisiRefreshTokenIV = value;
                OnPropertyChanged("EncryptedSisiRefreshTokenIV");
            }
        }
        
        private DateTime? _sisiRefreshTokenCreatedAt;
        
        /// <summary>
        /// When the Sisi (Singularity) Refresh Token was created
        /// </summary>
        public DateTime? SisiRefreshTokenCreatedAt
        {
            get
            {
                return _sisiRefreshTokenCreatedAt;
            }
            set
            {
                _sisiRefreshTokenCreatedAt = value;
                OnPropertyChanged("SisiRefreshTokenCreatedAt");
            }
        }

        /// <summary>
        /// Attempts to prepare the encrypted version of the currently active SecureTranquilityRefreshToken
        /// </summary>
        public void EncryptTranquilityRefreshToken()
        {
            SetEncryptedTranquilityRefreshToken(SecureTranquilityRefreshToken);
        }

        /// <summary>
        /// Removes the encrypted Tranquility refresh token and IV
        /// </summary>
        public void ClearEncryptedTranquilityRefreshToken()
        {
            EncryptedTranquilityRefreshToken = null;
            EncryptedTranquilityRefreshTokenIV = null;
            TranquilityRefreshTokenCreatedAt = null;
        }

        /// <summary>
        /// Sets the encrypted Tranquility refresh token to the given SecureString, if possible
        /// </summary>
        /// <param name="refreshToken"></param>
        void SetEncryptedTranquilityRefreshToken(System.Security.SecureString refreshToken)
        {
            if (!App.Settings.UseMasterKey || !App.Settings.UseRefreshTokens || refreshToken == null)
            {
                ClearEncryptedTranquilityRefreshToken();
                return;
            }

            if (!App.Settings.RequestMasterPassword())
            {
                System.Windows.MessageBox.Show("Your configured Master Password is required in order to save refresh tokens. It can be reset or disabled by un-checking 'Save passwords (securely)', and then all currently saved refresh tokens will be lost.");
                return;
            }

            using (RijndaelManaged rjm = new RijndaelManaged())
            {
                if (string.IsNullOrEmpty(EncryptedTranquilityRefreshTokenIV))
                {
                    rjm.GenerateIV();
                    EncryptedTranquilityRefreshTokenIV = Convert.ToBase64String(rjm.IV);
                }
                else
                    rjm.IV = Convert.FromBase64String(EncryptedTranquilityRefreshTokenIV);

                using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey, true))
                {
                    rjm.Key = sbwKey.Bytes;

                    using (ICryptoTransform encryptor = rjm.CreateEncryptor())
                    {
                        using (SecureStringWrapper ssw2 = new SecureStringWrapper(refreshToken, Encoding.Unicode))
                        {
                            byte[] inblock = ssw2.ToByteArray();
                            byte[] encrypted = encryptor.TransformFinalBlock(inblock, 0, inblock.Length);
                            EncryptedTranquilityRefreshToken = Convert.ToBase64String(encrypted);
                            SecureTranquilityRefreshToken = refreshToken;
                            TranquilityRefreshTokenCreatedAt = DateTime.Now;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts the currently EncryptedTranquilityRefreshToken if possible, populating SecureTranquilityRefreshToken
        /// </summary>
        public void DecryptTranquilityRefreshToken(bool allowPopup)
        {
            if (string.IsNullOrEmpty(EncryptedTranquilityRefreshToken) || string.IsNullOrEmpty(EncryptedTranquilityRefreshTokenIV))
            {
                // no refresh token stored to decrypt.
                return;
            }

            if (!App.Settings.HasPasswordMasterKey)
            {
                // Master Password not yet entered
                if (!allowPopup)
                {
                    // can't ask for it right now
                    return;
                }

                // ok, ask for it
                if (!App.Settings.RequestMasterPassword())
                {
                    // not entered. can't decrypt.
                    return;
                }
            }
            using (RijndaelManaged rjm = new RijndaelManaged())
            {
                rjm.IV = Convert.FromBase64String(EncryptedTranquilityRefreshTokenIV);

                using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey, true))
                {
                    rjm.Key = sbwKey.Bytes;
                    using (ICryptoTransform decryptor = rjm.CreateDecryptor())
                    {
                        byte[] pass = Convert.FromBase64String(EncryptedTranquilityRefreshToken);

                        using (SecureBytesWrapper sbw = new SecureBytesWrapper())
                        {
                            sbw.Bytes = decryptor.TransformFinalBlock(pass, 0, pass.Length);

                            SecureTranquilityRefreshToken = new System.Security.SecureString();
                            foreach (char c in Encoding.Unicode.GetChars(sbw.Bytes))
                            {
                                SecureTranquilityRefreshToken.AppendChar(c);
                            }
                            SecureTranquilityRefreshToken.MakeReadOnly();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to prepare the encrypted version of the currently active SecureSisiRefreshToken
        /// </summary>
        public void EncryptSisiRefreshToken()
        {
            SetEncryptedSisiRefreshToken(SecureSisiRefreshToken);
        }

        /// <summary>
        /// Removes the encrypted Sisi refresh token and IV
        /// </summary>
        public void ClearEncryptedSisiRefreshToken()
        {
            EncryptedSisiRefreshToken = null;
            EncryptedSisiRefreshTokenIV = null;
            SisiRefreshTokenCreatedAt = null;
        }

        /// <summary>
        /// Sets the encrypted Sisi refresh token to the given SecureString, if possible
        /// </summary>
        /// <param name="refreshToken"></param>
        void SetEncryptedSisiRefreshToken(System.Security.SecureString refreshToken)
        {
            if (!App.Settings.UseMasterKey || !App.Settings.UseRefreshTokens || refreshToken == null)
            {
                ClearEncryptedSisiRefreshToken();
                return;
            }

            if (!App.Settings.RequestMasterPassword())
            {
                System.Windows.MessageBox.Show("Your configured Master Password is required in order to save refresh tokens. It can be reset or disabled by un-checking 'Save passwords (securely)', and then all currently saved refresh tokens will be lost.");
                return;
            }

            using (RijndaelManaged rjm = new RijndaelManaged())
            {
                if (string.IsNullOrEmpty(EncryptedSisiRefreshTokenIV))
                {
                    rjm.GenerateIV();
                    EncryptedSisiRefreshTokenIV = Convert.ToBase64String(rjm.IV);
                }
                else
                    rjm.IV = Convert.FromBase64String(EncryptedSisiRefreshTokenIV);

                using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey, true))
                {
                    rjm.Key = sbwKey.Bytes;

                    using (ICryptoTransform encryptor = rjm.CreateEncryptor())
                    {
                        using (SecureStringWrapper ssw2 = new SecureStringWrapper(refreshToken, Encoding.Unicode))
                        {
                            byte[] inblock = ssw2.ToByteArray();
                            byte[] encrypted = encryptor.TransformFinalBlock(inblock, 0, inblock.Length);
                            EncryptedSisiRefreshToken = Convert.ToBase64String(encrypted);
                            SecureSisiRefreshToken = refreshToken;
                            SisiRefreshTokenCreatedAt = DateTime.Now;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts the currently EncryptedSisiRefreshToken if possible, populating SecureSisiRefreshToken
        /// </summary>
        public void DecryptSisiRefreshToken(bool allowPopup)
        {
            if (string.IsNullOrEmpty(EncryptedSisiRefreshToken) || string.IsNullOrEmpty(EncryptedSisiRefreshTokenIV))
            {
                // no refresh token stored to decrypt.
                return;
            }

            if (!App.Settings.HasPasswordMasterKey)
            {
                // Master Password not yet entered
                if (!allowPopup)
                {
                    // can't ask for it right now
                    return;
                }

                // ok, ask for it
                if (!App.Settings.RequestMasterPassword())
                {
                    // not entered. can't decrypt.
                    return;
                }
            }
            using (RijndaelManaged rjm = new RijndaelManaged())
            {
                rjm.IV = Convert.FromBase64String(EncryptedSisiRefreshTokenIV);

                using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey, true))
                {
                    rjm.Key = sbwKey.Bytes;
                    using (ICryptoTransform decryptor = rjm.CreateDecryptor())
                    {
                        byte[] pass = Convert.FromBase64String(EncryptedSisiRefreshToken);

                        using (SecureBytesWrapper sbw = new SecureBytesWrapper())
                        {
                            sbw.Bytes = decryptor.TransformFinalBlock(pass, 0, pass.Length);

                            SecureSisiRefreshToken = new System.Security.SecureString();
                            foreach (char c in Encoding.Unicode.GetChars(sbw.Bytes))
                            {
                                SecureSisiRefreshToken.AppendChar(c);
                            }
                            SecureSisiRefreshToken.MakeReadOnly();
                        }
                    }
                }
            }
        }


        #endregion

        public LoginResult GetSecurityWarningChallenge(bool sisi, string responseBody, Uri referer, out Token accessToken)
        {
            var uri = RequestResponse.GetSecurityWarningChallenge(sisi, state.ToString(), challengeHash);
            var req = RequestResponse.CreateGetRequest(uri, sisi, true, referer.ToString(), Cookies);
            return GetAccessToken(sisi, req, out accessToken);

        }

        public LoginResult GetEmailChallenge(bool sisi, string responseBody, out Token accessToken)
        {
            Windows.EmailChallengeWindow emailWindow = new Windows.EmailChallengeWindow(responseBody);
            emailWindow.ShowDialog();
            if (!emailWindow.DialogResult.HasValue || !emailWindow.DialogResult.Value)
            {
                SecurePassword = null;
                accessToken = null;
                return LoginResult.EmailVerificationRequired;
            }
            SecurePassword = null;
            accessToken = null;
            return LoginResult.EmailVerificationRequired;
        }


        public LoginResult GetEULAChallenge(bool sisi, string responseBody, Uri referer, out Token accessToken)
        {
            Windows.EVEEULAWindow eulaWindow = new Windows.EVEEULAWindow(responseBody);
            eulaWindow.ShowDialog();
            if (!eulaWindow.DialogResult.HasValue || !eulaWindow.DialogResult.Value)
            {
                SecurePassword = null;
                accessToken = null;
                return LoginResult.EULADeclined;
            }

            //string uri = "https://login.eveonline.com/OAuth/Eula";
            //if (sisi)
            //{
            //    uri = "https://sisilogin.testeveonline.com/OAuth/Eula";
            //}

            var uri = RequestResponse.GetEulaUri(sisi, state.ToString(), challengeHash);
            HttpWebRequest req = RequestResponse.CreatePostRequest(uri, sisi, true, referer.ToString(), Cookies);


            using (SecureBytesWrapper body = new SecureBytesWrapper())
            {
                string eulaHash = RequestResponse.GetEulaHashFromBody(responseBody);
                string returnUrl = RequestResponse.GetEulaReturnUrlFromBody(responseBody);

                string formattedString = String.Format("eulaHash={0}&returnUrl={1}&action={2}", Uri.EscapeDataString(eulaHash), Uri.EscapeDataString(returnUrl), "Accept");
                body.Bytes = Encoding.ASCII.GetBytes(formattedString);

                req.ContentLength = body.Bytes.Length;
                try
                {
                    using (Stream reqStream = req.GetRequestStream())
                    {
                        reqStream.Write(body.Bytes, 0, body.Bytes.Length);
                    }
                }
                catch (System.Net.WebException e)
                {
                    switch (e.Status)
                    {
                        case WebExceptionStatus.Timeout:
                            {
                                accessToken = null;
                                return LoginResult.Timeout;
                            }
                        default:
                            throw;
                    }
                }
            }
            LoginResult result;
            try
            {
                result = GetAccessToken(sisi, req, out accessToken);
            }
            catch (System.Net.WebException)
            {
                result = GetAccessToken(sisi, out accessToken);
            }

            result = GetAccessToken(sisi, req, out accessToken);
            if (result == LoginResult.Success)
            {
                // successful verification code challenge, make sure we save the cookies.
                App.Settings.Store();
            }
            return result;
        }


        public LoginResult GetEmailCodeChallenge(bool sisi, string responseBody, out Token accessToken)
        {

            Windows.VerificationCodeChallengeWindow acw = new Windows.VerificationCodeChallengeWindow(this);
            acw.ShowDialog();
            if (!acw.DialogResult.HasValue || !acw.DialogResult.Value)
            {
                SecurePassword = null;
                accessToken = null;
                return LoginResult.InvalidEmailVerificationChallenge;
            }

            var uri = RequestResponse.GetVerifyTwoFactorUri(sisi, state.ToString(), challengeHash);
            var req = RequestResponse.CreatePostRequest(uri, sisi, true, null, Cookies);

            using (SecureBytesWrapper body = new SecureBytesWrapper())
            {
                //                body.Bytes = Encoding.ASCII.GetBytes(String.Format("Challenge={0}&IsPasswordBreached={1}&NumPasswordBreaches={2}&command={3}", Uri.EscapeDataString(acw.VerificationCode), IsPasswordBreached, NumPasswordBreaches, "Continue"));
                body.Bytes = Encoding.ASCII.GetBytes(String.Format("Challenge={0}&command={1}", Uri.EscapeDataString(acw.VerificationCode), "Continue"));

                req.ContentLength = body.Bytes.Length;
                try
                {
                    using (Stream reqStream = req.GetRequestStream())
                    {
                        reqStream.Write(body.Bytes, 0, body.Bytes.Length);
                    }
                }
                catch (System.Net.WebException e)
                {
                    switch (e.Status)
                    {
                        case WebExceptionStatus.Timeout:
                            {
                                accessToken = null;
                                return LoginResult.Timeout;
                            }
                        default:
                            throw;
                    }
                }
            }
            LoginResult result = GetAccessToken(sisi, req, out accessToken);
            if (result == LoginResult.Success)
            {
                // successful verification code challenge, make sure we save the cookies.
                App.Settings.Store();
            }
            return result;
        }

        public LoginResult GetAuthenticatorChallenge(bool sisi, out Token accessToken)
        {
            Windows.AuthenticatorChallengeWindow acw = new Windows.AuthenticatorChallengeWindow(this);
            acw.ShowDialog();
            if (!acw.DialogResult.HasValue || !acw.DialogResult.Value)
            {
                SecurePassword = null;
                accessToken = null;
                return LoginResult.InvalidAuthenticatorChallenge;
            }


            var uri = RequestResponse.GetAuthenticatorUri(sisi, state.ToString(), challengeHash);
            var req = RequestResponse.CreatePostRequest(uri, sisi, true, uri.ToString(), Cookies);

            using (SecureBytesWrapper body = new SecureBytesWrapper())
            {
                body.Bytes = Encoding.ASCII.GetBytes(String.Format("Challenge={0}&RememberTwoFactor={1}&command={2}", Uri.EscapeDataString(acw.AuthenticatorCode), "true", "Continue"));

                req.ContentLength = body.Bytes.Length;
                try
                {
                    using (Stream reqStream = req.GetRequestStream())
                    {
                        reqStream.Write(body.Bytes, 0, body.Bytes.Length);
                    }
                }
                catch (System.Net.WebException e)
                {
                    switch (e.Status)
                    {
                        case WebExceptionStatus.Timeout:
                            {
                                accessToken = null;
                                return LoginResult.Timeout;
                            }
                        default:
                            throw;
                    }
                }
            }
            LoginResult result = GetAccessToken(sisi, req, out accessToken);
            if (result == LoginResult.Success)
            {
                // successful authenticator challenge, make sure we save the cookies.
                App.Settings.Store();
            }
            return result;
        }

        public LoginResult GetCharacterChallenge(bool sisi, out Token accessToken)
        {
            // need SecureCharacterName.
            if (SecureCharacterName == null || SecureCharacterName.Length == 0)
            {
                DecryptCharacterName(true);
                if (SecureCharacterName == null || SecureCharacterName.Length == 0)
                {

                    Windows.CharacterChallengeWindow ccw = new Windows.CharacterChallengeWindow(this);
                    bool? result = ccw.ShowDialog();

                    if (string.IsNullOrWhiteSpace(ccw.CharacterName))
                    {
                        // CharacterName is required, sorry dude
                        accessToken = null;
                        //  SecurePassword = null;
                        SecureCharacterName = null;
                        return LoginResult.InvalidCharacterChallenge;
                    }

                    SecureCharacterName = new System.Security.SecureString();
                    foreach (char c in ccw.CharacterName)
                    {
                        SecureCharacterName.AppendChar(c);
                    }
                    SecureCharacterName.MakeReadOnly();
                    EncryptCharacterName();
                    App.Settings.Store();
                }
            }

            var uri = RequestResponse.GetCharacterChallengeUri(sisi, state.ToString(), challengeHash);
            var req = RequestResponse.CreatePostRequest(uri, sisi, true, uri.ToString(), Cookies);
            using (SecureBytesWrapper body = new SecureBytesWrapper())
            {
                byte[] body1 = Encoding.ASCII.GetBytes(String.Format("RememberCharacterChallenge={0}&Challenge=", "true"));
                using (SecureStringWrapper ssw = new SecureStringWrapper(SecureCharacterName, Encoding.ASCII))
                {
                    using (SecureBytesWrapper escapedCharacterName = new SecureBytesWrapper())
                    {
                        escapedCharacterName.Bytes = System.Web.HttpUtility.UrlEncodeToBytes(ssw.ToByteArray());

                        body.Bytes = new byte[body1.Length + escapedCharacterName.Bytes.Length];
                        System.Buffer.BlockCopy(body1, 0, body.Bytes, 0, body1.Length);
                        System.Buffer.BlockCopy(escapedCharacterName.Bytes, 0, body.Bytes, body1.Length, escapedCharacterName.Bytes.Length);
                    }
                }

                req.ContentLength = body.Bytes.Length;
                try
                {
                    using (Stream reqStream = req.GetRequestStream())
                    {
                        reqStream.Write(body.Bytes, 0, body.Bytes.Length);
                    }
                }
                catch (System.Net.WebException e)
                {
                    switch (e.Status)
                    {
                        case WebExceptionStatus.Timeout:
                            {
                                accessToken = null;
                                return LoginResult.Timeout;
                            }
                        default:
                            throw;
                    }
                }
            }
            return GetAccessToken(sisi, req, out accessToken);
        }

        public LoginResult GetAccessToken(bool sisi, HttpWebRequest req, out Token accessToken)
        {
            Utils.Debug.Info($"GetAccessToken - Request URI: {req.RequestUri} | Sisi: {sisi}", LogCategory);

            accessToken = null;
            Response response = null;

            try
            {
                if (App.myLB.strHTML_RequestVerificationToken == "")
                {
                    response = new Response(req);
                }
                else
                {
                    response = new Response(req, WebRequestType.Result);
                }


                string responseBody = response.Body;
                UpdateCookieStorage();

                Utils.Debug.Info($"GetAccessToken - Response:{Environment.NewLine}{response.ToString()}", LogCategory);

                if (responseBody.Contains("Incorrect character name entered"))
                {
                    accessToken = null;
                    SecurePassword = null;
                    SecureCharacterName = null;
                    return LoginResult.InvalidCharacterChallenge;
                }

                if (responseBody.Contains("Invalid username / password"))
                {
                    accessToken = null;
                    SecurePassword = null;
                    return LoginResult.InvalidUsernameOrPassword;
                }

                // I'm just guessing on this one at the moment.
                if (responseBody.Contains("Invalid authenticat")
                    || (responseBody.Contains("Verification code mismatch") && responseBody.Contains("/account/authenticator"))
                    )
                {
                    accessToken = null;
                    SecurePassword = null;
                    return LoginResult.InvalidAuthenticatorChallenge;
                }
                //The 2FA page now has "Character challenge" in the text but it is hidden. This should fix it from
                //Coming up during 2FA challenge
                if (responseBody.Contains("Character challenge") && !responseBody.Contains("visuallyhidden"))
                {
                    return GetCharacterChallenge(sisi, out accessToken);
                }

                if (responseBody.Contains("Email verification required"))
                {
                    return GetEmailChallenge(sisi, responseBody, out accessToken);
                }

                if (responseBody.Contains("Authenticator is enabled"))
                {
                    return GetAuthenticatorChallenge(sisi, out accessToken);
                }

                if (responseBody.Contains("Please enter the verification code "))
                {
                    return GetEmailCodeChallenge(sisi, responseBody, out accessToken);
                }

                if (responseBody.Contains("Security Warning"))
                {
                    return GetSecurityWarningChallenge(sisi, responseBody, response.ResponseUri, out accessToken);
                }

                if (responseBody.ToLower().Contains("form action=\"/oauth/eula\""))
                {
                    return GetEULAChallenge(sisi, responseBody, response.ResponseUri, out accessToken);
                }

                if (response.ResponseUri.OriginalString.Contains("/v2/oauth/token"))
                {
                    accessToken = new Token(JsonConvert.DeserializeObject<authObj>(response.Body));
                    if (!sisi)
                    {
                        TranquilityToken = accessToken;
                    }
                    else
                    {
                        SisiToken = accessToken;
                    }
                    return LoginResult.Success;
                }
                
                try
                {

                    code = HttpUtility.ParseQueryString(response.ResponseUri.OriginalString).Get("code");
                    
                    if (code == null)
                    {

                        return LoginResult.Error;
                    }
                    GetAccessToken(sisi, code, out response);
                    accessToken = new Token(JsonConvert.DeserializeObject<authObj>(response.Body));
                }
                catch (Exception)
                {
                    Windows.UnhandledResponseWindow urw = new Windows.UnhandledResponseWindow(responseBody);
                    urw.ShowDialog();

                    // can't get the token
                    accessToken = null;
                    SecurePassword = null;
                    return LoginResult.TokenFailure;
                }

                if (!sisi)
                {
                    TranquilityToken = accessToken;
                }
                else
                {
                    SisiToken = accessToken;
                }

                return LoginResult.Success;
            }
            catch (System.Net.WebException we)
            {
                switch (we.Status)
                {
                    case WebExceptionStatus.Timeout:
                        return LoginResult.Timeout;
                    default:

                        Windows.UnhandledResponseWindow urw = new Windows.UnhandledResponseWindow(response.ToString());
                        urw.ShowDialog();
                        return LoginResult.Error;
                }
            }
        }


        public class authObj
        {
            private int _expiresIn;
            public string access_token
            {
                get; set;
            }
            public int expires_in
            {
                get
                {
                    return _expiresIn;
                }
                set
                {
                    _expiresIn = value;
                    Expiration = DateTime.Now.AddMinutes(_expiresIn);
                }
            }
            public string token_type
            {
                get; set;
            }
            public string refresh_token
            {
                get; set;
            }

            public DateTime Expiration
            {
                get; private set;
            }

        }


        private LoginResult GetAccessToken(bool sisi, string authCode, out Response response)
        {
            HttpWebRequest req2 = RequestResponse.CreatePostRequest(new Uri(RequestResponse.token, UriKind.Relative), sisi, true, RequestResponse.refererUri, Cookies);

            req2.SetBody(RequestResponse.GetSsoTokenRequestBody(sisi, authCode, challengeCode));

            return RequestResponse.GetHttpWebResponse(req2, UpdateCookieStorage, out response);

        }

        public LoginResult GetRequestVerificationToken(Uri uri, bool sisi, out string verificationToken)
        {
            Response response;
            verificationToken = null;

            var req = RequestResponse.CreateGetRequest(uri, sisi, true, "URL", Cookies);
            req.ContentLength = 0;

            var result = RequestResponse.GetHttpWebResponse(req, UpdateCookieStorage, out response);

            if (result == LoginResult.Success)
            {
                verificationToken = RequestResponse.GetRequestVerificationTokenResponse(response);
            }

            return result;
        }
        
        private bool TryGetExistingAccessToken(bool sisi, out Token accessToken)
        {
            Token checkToken = sisi ? SisiToken : TranquilityToken;
            if (checkToken != null && !checkToken.IsExpired)
            {
                accessToken = checkToken;
                return true;
            }

            accessToken = null;
            return false;
        }
        
        private bool TryGetFromRefreshToken(bool sisi, out Token accessToken)
        {
            try
            {
                if (!App.Settings.UseRefreshTokens)
                {
                    accessToken = null;
                    return false;
                }

                DecryptTranquilityRefreshToken(true);
                DecryptSisiRefreshToken(true);
                SecureString refreshToken = sisi ? SecureSisiRefreshToken : SecureTranquilityRefreshToken;
                if (refreshToken == null || refreshToken.Length == 0)
                {
                    accessToken = null;
                    return false;
                }
                
                var uri = RequestResponse.GetTokenUri(sisi);
                var req = RequestResponse.CreatePostRequest(uri, sisi, true, "URL", Cookies);

                using (SecureBytesWrapper body = new SecureBytesWrapper())
                {
                    byte[] body1 = Encoding.ASCII.GetBytes(String.Format("client_id=eveLauncherTQ&grant_type=refresh_token&refresh_token="));
                    using (SecureStringWrapper ssw = new SecureStringWrapper(refreshToken, Encoding.ASCII))
                    {
                        using (SecureBytesWrapper escapedRefreshToken = new SecureBytesWrapper())
                        {
                            escapedRefreshToken.Bytes = System.Web.HttpUtility.UrlEncodeToBytes(ssw.ToByteArray());

                            body.Bytes = new byte[body1.Length + escapedRefreshToken.Bytes.Length];
                            System.Buffer.BlockCopy(body1, 0, body.Bytes, 0, body1.Length);
                            System.Buffer.BlockCopy(escapedRefreshToken.Bytes, 0, body.Bytes, body1.Length, escapedRefreshToken.Bytes.Length);
                            req.SetBody(body);
                        }
                    }
                }
                LoginResult result = GetAccessToken(sisi, req, out accessToken);
                if (result == LoginResult.Success)
                {
                    App.Settings.Store();
                    return true;
                }
            }
            catch (Exception e)
            {
                Utils.Debug.Error($"TryGetFromRefreshToken exception: {e}", LogCategory);
                throw;
            }
            // Refresh token implementation is disabled for now.
            accessToken = null;
            return false;
        }

        public LoginResult GetAccessToken(bool sisi, out Token accessToken)
        {
            // first check for an existing, valid token
            if (TryGetExistingAccessToken(sisi, out accessToken))
            {
                return LoginResult.Success;
            }
            
            // need SecurePassword.
            if (SecurePassword == null || SecurePassword.Length == 0)
            {
                DecryptPassword(true);
                if (SecurePassword == null || SecurePassword.Length == 0)
                {

                    Windows.EVELogin el = new Windows.EVELogin(this, true);
                    bool? dialogResult = el.ShowDialog();

                    if (SecurePassword == null || SecurePassword.Length == 0)
                    {
                        // password is required, sorry dude
                        accessToken = null;
                        return LoginResult.InvalidUsernameOrPassword;
                    }

                    App.Settings.Store();
                }
            }
            
            if (TryGetFromRefreshToken(sisi, out accessToken))
            {
                return LoginResult.Success;
            }

            App.strUserName = Username;
            App.strPassword = new System.Net.NetworkCredential(string.Empty, SecurePassword).Password;

            if (App.Settings.ManualLogin)
            {
                var manualLoginWindow = new EVEManualLogin(this, sisi);
                manualLoginWindow.ShowDialog();
                accessToken = manualLoginWindow.AccessToken;
                var manualResult = manualLoginWindow.LoginResult;
                // save the token if we got one.
                if (manualResult == LoginResult.Success && accessToken != null)
                {
                    if (!sisi)
                    {
                        TranquilityToken = accessToken;
                    }
                    else
                    {
                        SisiToken = accessToken;
                    }
                    App.Settings.Store();
                }
                return manualResult;
            }
            
            var uri = RequestResponse.GetLoginUri(sisi, state.ToString(), challengeHash);

            string RequestVerificationToken = string.Empty;
            var result = GetRequestVerificationToken(uri, sisi, out RequestVerificationToken);

            if (result == LoginResult.Error)
            {
                accessToken = null;
                return result;
            }

            var req = RequestResponse.CreatePostRequest(uri, sisi, true, "URL", Cookies);

            using (SecureBytesWrapper body = new SecureBytesWrapper())
            {
                byte[] body1 = Encoding.ASCII.GetBytes(String.Format("__RequestVerificationToken={1}&UserName={0}&Password=", Uri.EscapeDataString(Username), Uri.EscapeDataString(RequestVerificationToken)));
                //                byte[] body1 = Encoding.ASCII.GetBytes(String.Format("UserName={0}&Password=", Uri.EscapeDataString(Username)));
                using (SecureStringWrapper ssw = new SecureStringWrapper(SecurePassword, Encoding.ASCII))
                {
                    using (SecureBytesWrapper escapedPassword = new SecureBytesWrapper())
                    {
                        escapedPassword.Bytes = System.Web.HttpUtility.UrlEncodeToBytes(ssw.ToByteArray());

                        body.Bytes = new byte[body1.Length + escapedPassword.Bytes.Length];
                        System.Buffer.BlockCopy(body1, 0, body.Bytes, 0, body1.Length);
                        System.Buffer.BlockCopy(escapedPassword.Bytes, 0, body.Bytes, body1.Length, escapedPassword.Bytes.Length);
                        req.SetBody(body);
                    }
                }
            }

            return GetAccessToken(sisi, req, out accessToken);
        }

        public LoginResult GetSSOToken(bool sisi, out Token ssoToken)
        {
            LoginResult lr = this.GetAccessToken(sisi, out ssoToken);

            return lr;
        }

        public LoginResult Launch(string sharedCachePath, bool sisi, DirectXVersion dxVersion, long characterID)
        {
            Token ssoToken;
            LoginResult lr = GetSSOToken(sisi, out ssoToken);
            if (lr != LoginResult.Success)
                return lr;
            if (!App.Launch(sharedCachePath, sisi, dxVersion, characterID, ssoToken))
                return LoginResult.Error;

            return LoginResult.Success;
        }

        public LoginResult Launch(string gameName, string gameProfileName, bool sisi, DirectXVersion dxVersion, long characterID)
        {
            Token ssoToken;
            LoginResult lr = GetSSOToken(sisi, out ssoToken);
            if (lr != LoginResult.Success)
                return lr;
            if (!App.Launch(gameName, gameProfileName, sisi, dxVersion, characterID, ssoToken))
                return LoginResult.Error;

            return LoginResult.Success;
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


        public void Dispose()
        {
            if (this.SecurePassword != null)
            {
                this.SecurePassword.Dispose();
                this.SecurePassword = null;
            }
            this.EncryptedPassword = null;
            this.EncryptedPasswordIV = null;
            if (this.SecureCharacterName != null)
            {
                this.SecureCharacterName.Dispose();
                this.SecureCharacterName = null;
            }
            this.EncryptedCharacterName = null;
            this.EncryptedCharacterNameIV = null;
            
            this.EncryptedTranquilityRefreshToken = null;
            this.EncryptedTranquilityRefreshTokenIV = null;
            this.EncryptedSisiRefreshToken = null;
            this.EncryptedSisiRefreshTokenIV = null;

            ISBoxerEVELauncher.Web.CookieStorage.DeleteCookies(this);
            ISBoxerEVELauncher.Web.CookieStorage.DeleteWebViewCookies(this);
            this.Username = null;
            this.Cookies = null;
            //this.NewCookieStorage = null;
        }

        public override string ToString()
        {
            return Username;
        }

        EVEAccount ILaunchTarget.EVEAccount
        {
            get
            {
                return this;
            }
        }

        public long CharacterID
        {
            get
            {
                return 0;
            }
        }

        public void ClearRefreshToken()
        {
            SetEncryptedTranquilityRefreshToken(null);
            SetEncryptedSisiRefreshToken(null);
        }
    }
}
