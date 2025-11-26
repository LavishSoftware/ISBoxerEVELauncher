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
            Utils.Debug.Info($"GetSecurityWarningChallenge - Starting security warning challenge | Sisi: {sisi} | Referer: {referer}", LogCategory);
            var uri = RequestResponse.GetSecurityWarningChallenge(sisi, state.ToString(), challengeHash);
            Utils.Debug.Info($"GetSecurityWarningChallenge - URI: {uri}", LogCategory);
            var req = RequestResponse.CreateGetRequest(uri, sisi, true, referer.ToString(), Cookies);
            var result = GetAccessToken(sisi, req, out accessToken);
            Utils.Debug.Info($"GetSecurityWarningChallenge - Result: {result}", LogCategory);
            return result;

        }

        public LoginResult GetEmailChallenge(bool sisi, string responseBody, out Token accessToken)
        {
            Utils.Debug.Info($"GetEmailChallenge - Starting email challenge | Sisi: {sisi}", LogCategory);
            Windows.EmailChallengeWindow emailWindow = new Windows.EmailChallengeWindow(responseBody);
            emailWindow.ShowDialog();
            if (!emailWindow.DialogResult.HasValue || !emailWindow.DialogResult.Value)
            {
                Utils.Debug.Info($"GetEmailChallenge - Email challenge dialog cancelled or failed", LogCategory);
                SecurePassword = null;
                accessToken = null;
                return LoginResult.EmailVerificationRequired;
            }
            Utils.Debug.Info($"GetEmailChallenge - Email verification required", LogCategory);
            SecurePassword = null;
            accessToken = null;
            return LoginResult.EmailVerificationRequired;
        }


        public LoginResult GetEULAChallenge(bool sisi, string responseBody, Uri referer, out Token accessToken)
        {
            Utils.Debug.Info($"GetEULAChallenge - Starting EULA challenge | Sisi: {sisi} | Referer: {referer}", LogCategory);
            Windows.EVEEULAWindow eulaWindow = new Windows.EVEEULAWindow(responseBody);
            eulaWindow.ShowDialog();
            if (!eulaWindow.DialogResult.HasValue || !eulaWindow.DialogResult.Value)
            {
                Utils.Debug.Info($"GetEULAChallenge - EULA declined by user", LogCategory);
                SecurePassword = null;
                accessToken = null;
                return LoginResult.EULADeclined;
            }

            Utils.Debug.Info($"GetEULAChallenge - EULA accepted, proceeding with login", LogCategory);
            //string uri = "https://login.eveonline.com/OAuth/Eula";
            //if (sisi)
            //{
            //    uri = "https://sisilogin.testeveonline.com/OAuth/Eula";
            //}

            var uri = RequestResponse.GetEulaUri(sisi, state.ToString(), challengeHash);
            Utils.Debug.Info($"GetEULAChallenge - EULA URI: {uri}", LogCategory);
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
                Utils.Debug.Info($"GetEULAChallenge - Attempting to get access token", LogCategory);
                result = GetAccessToken(sisi, req, out accessToken);
            }
            catch (System.Net.WebException ex)
            {
                Utils.Debug.Warning($"GetEULAChallenge - WebException during first GetAccessToken attempt: {ex.Message}", LogCategory);
                result = GetAccessToken(sisi, out accessToken);
            }

            result = GetAccessToken(sisi, req, out accessToken);
            Utils.Debug.Info($"GetEULAChallenge - Final result: {result}", LogCategory);
            if (result == LoginResult.Success)
            {
                // successful verification code challenge, make sure we save the cookies.
                Utils.Debug.Info($"GetEULAChallenge - Success, storing settings", LogCategory);
                App.Settings.Store();
            }
            return result;
        }


        public LoginResult GetEmailCodeChallenge(bool sisi, string responseBody, out Token accessToken)
        {
            Utils.Debug.Info($"GetEmailCodeChallenge - Starting email code challenge | Sisi: {sisi}", LogCategory);

            Windows.VerificationCodeChallengeWindow acw = new Windows.VerificationCodeChallengeWindow(this);
            acw.ShowDialog();
            if (!acw.DialogResult.HasValue || !acw.DialogResult.Value)
            {
                Utils.Debug.Info($"GetEmailCodeChallenge - Dialog cancelled or failed", LogCategory);
                SecurePassword = null;
                accessToken = null;
                return LoginResult.InvalidEmailVerificationChallenge;
            }

            Utils.Debug.Info($"GetEmailCodeChallenge - Verification code entered, proceeding", LogCategory);
            var uri = RequestResponse.GetVerifyTwoFactorUri(sisi, state.ToString(), challengeHash);
            Utils.Debug.Info($"GetEmailCodeChallenge - Two-factor URI: {uri}", LogCategory);
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
            Utils.Debug.Info($"GetEmailCodeChallenge - Sending verification code request", LogCategory);
            LoginResult result = GetAccessToken(sisi, req, out accessToken);
            Utils.Debug.Info($"GetEmailCodeChallenge - Result: {result}", LogCategory);
            if (result == LoginResult.Success)
            {
                // successful verification code challenge, make sure we save the cookies.
                Utils.Debug.Info($"GetEmailCodeChallenge - Success, storing settings", LogCategory);
                App.Settings.Store();
            }
            return result;
        }

        public LoginResult GetAuthenticatorChallenge(bool sisi, out Token accessToken)
        {
            Utils.Debug.Info($"GetAuthenticatorChallenge - Starting authenticator challenge | Sisi: {sisi}", LogCategory);
            Windows.AuthenticatorChallengeWindow acw = new Windows.AuthenticatorChallengeWindow(this);
            acw.ShowDialog();
            if (!acw.DialogResult.HasValue || !acw.DialogResult.Value)
            {
                Utils.Debug.Info($"GetAuthenticatorChallenge - Dialog cancelled or failed", LogCategory);
                SecurePassword = null;
                accessToken = null;
                return LoginResult.InvalidAuthenticatorChallenge;
            }

            Utils.Debug.Info($"GetAuthenticatorChallenge - Authenticator code entered, proceeding", LogCategory);
            var uri = RequestResponse.GetAuthenticatorUri(sisi, state.ToString(), challengeHash);
            Utils.Debug.Info($"GetAuthenticatorChallenge - Authenticator URI: {uri}", LogCategory);
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
            Utils.Debug.Info($"GetAuthenticatorChallenge - Sending authenticator code request", LogCategory);
            LoginResult result = GetAccessToken(sisi, req, out accessToken);
            Utils.Debug.Info($"GetAuthenticatorChallenge - Result: {result}", LogCategory);
            if (result == LoginResult.Success)
            {
                // successful authenticator challenge, make sure we save the cookies.
                Utils.Debug.Info($"GetAuthenticatorChallenge - Success, storing settings", LogCategory);
                App.Settings.Store();
            }
            return result;
        }

        public LoginResult GetCharacterChallenge(bool sisi, out Token accessToken)
        {
            Utils.Debug.Info($"GetCharacterChallenge - Starting character challenge | Sisi: {sisi}", LogCategory);
            // need SecureCharacterName.
            if (SecureCharacterName == null || SecureCharacterName.Length == 0)
            {
                Utils.Debug.Info($"GetCharacterChallenge - No SecureCharacterName, attempting to decrypt", LogCategory);
                DecryptCharacterName(true);
                if (SecureCharacterName == null || SecureCharacterName.Length == 0)
                {
                    Utils.Debug.Info($"GetCharacterChallenge - Showing character challenge window", LogCategory);
                    Windows.CharacterChallengeWindow ccw = new Windows.CharacterChallengeWindow(this);
                    bool? result = ccw.ShowDialog();

                    if (string.IsNullOrWhiteSpace(ccw.CharacterName))
                    {
                        Utils.Debug.Info($"GetCharacterChallenge - Character name not provided", LogCategory);
                        // CharacterName is required, sorry dude
                        accessToken = null;
                        //  SecurePassword = null;
                        SecureCharacterName = null;
                        return LoginResult.InvalidCharacterChallenge;
                    }

                    Utils.Debug.Info($"GetCharacterChallenge - Character name provided, encrypting", LogCategory);
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
            Utils.Debug.Info($"GetCharacterChallenge - Character challenge URI: {uri}", LogCategory);
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
            Utils.Debug.Info($"GetCharacterChallenge - Sending character challenge request", LogCategory);
            var characterResult = GetAccessToken(sisi, req, out accessToken);
            Utils.Debug.Info($"GetCharacterChallenge - Result: {characterResult}", LogCategory);
            return characterResult;
        }

        public LoginResult GetAccessToken(bool sisi, HttpWebRequest req, out Token accessToken)
        {
            Utils.Debug.Info($"GetAccessToken(request) - Starting | Request URI: {req.RequestUri} | Method: {req.Method} | Sisi: {sisi}", LogCategory);

            accessToken = null;
            Response response = null;

            try
            {
                Utils.Debug.Info($"GetAccessToken(request) - RequestVerificationToken is {(string.IsNullOrEmpty(App.myLB.strHTML_RequestVerificationToken) ? "empty" : "present")}", LogCategory);
                if (App.myLB.strHTML_RequestVerificationToken == "")
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Creating Response from HttpWebRequest directly", LogCategory);
                    response = new Response(req);
                }
                else
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Creating Response with WebRequestType.Result (captcha flow)", LogCategory);
                    response = new Response(req, WebRequestType.Result);
                }


                string responseBody = response.Body;
                Utils.Debug.Info($"GetAccessToken(request) - Response body length: {responseBody?.Length ?? 0}", LogCategory);
                UpdateCookieStorage();

                Utils.Debug.Info($"GetAccessToken(request) - Full Response:{Environment.NewLine}{response.ToString()}", LogCategory);

                if (responseBody.Contains("Incorrect character name entered"))
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Detected: Incorrect character name entered", LogCategory);
                    accessToken = null;
                    SecurePassword = null;
                    SecureCharacterName = null;
                    return LoginResult.InvalidCharacterChallenge;
                }

                if (responseBody.Contains("Invalid username / password"))
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Detected: Invalid username / password", LogCategory);
                    accessToken = null;
                    SecurePassword = null;
                    return LoginResult.InvalidUsernameOrPassword;
                }

                // I'm just guessing on this one at the moment.
                if (responseBody.Contains("Invalid authenticat")
                    || (responseBody.Contains("Verification code mismatch") && responseBody.Contains("/account/authenticator"))
                    )
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Detected: Invalid authenticator challenge", LogCategory);
                    accessToken = null;
                    SecurePassword = null;
                    return LoginResult.InvalidAuthenticatorChallenge;
                }
                //The 2FA page now has "Character challenge" in the text but it is hidden. This should fix it from
                //Coming up during 2FA challenge
                if (responseBody.Contains("Character challenge") && !responseBody.Contains("visuallyhidden"))
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Detected: Character challenge required", LogCategory);
                    return GetCharacterChallenge(sisi, out accessToken);
                }

                if (responseBody.Contains("Email verification required"))
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Detected: Email verification required", LogCategory);
                    return GetEmailChallenge(sisi, responseBody, out accessToken);
                }

                if (responseBody.Contains("Authenticator is enabled"))
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Detected: Authenticator is enabled (2FA required)", LogCategory);
                    return GetAuthenticatorChallenge(sisi, out accessToken);
                }

                if (responseBody.Contains("Please enter the verification code "))
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Detected: Email verification code required", LogCategory);
                    return GetEmailCodeChallenge(sisi, responseBody, out accessToken);
                }

                if (responseBody.Contains("Security Warning"))
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Detected: Security Warning", LogCategory);
                    return GetSecurityWarningChallenge(sisi, responseBody, response.ResponseUri, out accessToken);
                }

                if (responseBody.ToLower().Contains("form action=\"/oauth/eula\""))
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Detected: EULA acceptance required", LogCategory);
                    return GetEULAChallenge(sisi, responseBody, response.ResponseUri, out accessToken);
                }

                if (response.ResponseUri.OriginalString.Contains("/v2/oauth/token"))
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Detected: Token response URI, parsing token directly", LogCategory);
                    accessToken = new Token(JsonConvert.DeserializeObject<authObj>(response.Body));
                    Utils.Debug.Info($"GetAccessToken(request) - Token parsed, expiration: {accessToken.Expiration}, has refresh token: {!string.IsNullOrEmpty(accessToken.RefreshToken)}", LogCategory);
                    if (!sisi)
                    {
                        TranquilityToken = accessToken;
                    }
                    else
                    {
                        SisiToken = accessToken;
                    }
                    Utils.Debug.Info($"GetAccessToken(request) - Success (direct token)", LogCategory);
                    return LoginResult.Success;
                }

                try
                {
                    Utils.Debug.Info($"GetAccessToken(request) - Attempting to extract auth code from response URI: {response.ResponseUri.OriginalString}", LogCategory);
                    code = HttpUtility.ParseQueryString(response.ResponseUri.OriginalString).Get("code");

                    if (code == null)
                    {
                        Utils.Debug.Warning($"GetAccessToken(request) - No auth code found in response URI", LogCategory);
                        return LoginResult.Error;
                    }
                    Utils.Debug.Info($"GetAccessToken(request) - Auth code extracted (length: {code.Length}), exchanging for token", LogCategory);
                    GetAccessToken(sisi, code, out response);
                    Utils.Debug.Info($"GetAccessToken(request) - Token exchange response received", LogCategory);
                    accessToken = new Token(JsonConvert.DeserializeObject<authObj>(response.Body));
                    Utils.Debug.Info($"GetAccessToken(request) - Token parsed, expiration: {accessToken.Expiration}, has refresh token: {!string.IsNullOrEmpty(accessToken.RefreshToken)}", LogCategory);
                }
                catch (Exception ex)
                {
                    Utils.Debug.Error($"GetAccessToken(request) - Exception during token parsing: {ex.Message}", LogCategory);
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

                Utils.Debug.Info($"GetAccessToken(request) - Success (code exchange)", LogCategory);
                return LoginResult.Success;
            }
            catch (System.Net.WebException we)
            {
                Utils.Debug.Error($"GetAccessToken(request) - WebException: Status={we.Status}, Message={we.Message}", LogCategory);
                switch (we.Status)
                {
                    case WebExceptionStatus.Timeout:
                        Utils.Debug.Error($"GetAccessToken(request) - Timeout occurred", LogCategory);
                        return LoginResult.Timeout;
                    default:
                        Utils.Debug.Error($"GetAccessToken(request) - Unhandled WebException, showing error window", LogCategory);
                        Windows.UnhandledResponseWindow urw = new Windows.UnhandledResponseWindow(response?.ToString() ?? we.Message);
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
            Utils.Debug.Info($"GetAccessToken(authCode) - Exchanging auth code for token | Sisi: {sisi} | Auth code length: {authCode?.Length ?? 0}", LogCategory);
            HttpWebRequest req2 = RequestResponse.CreatePostRequest(new Uri(RequestResponse.token, UriKind.Relative), sisi, true, RequestResponse.refererUri, Cookies);

            req2.SetBody(RequestResponse.GetSsoTokenRequestBody(sisi, authCode, challengeCode));

            var result = RequestResponse.GetHttpWebResponse(req2, UpdateCookieStorage, out response);
            Utils.Debug.Info($"GetAccessToken(authCode) - Token exchange result: {result}", LogCategory);
            return result;

        }

        public LoginResult GetRequestVerificationToken(Uri uri, bool sisi, out string verificationToken)
        {
            Utils.Debug.Info($"GetRequestVerificationToken - Starting | URI: {uri} | Sisi: {sisi}", LogCategory);
            Response response;
            verificationToken = null;

            var req = RequestResponse.CreateGetRequest(uri, sisi, true, "URL", Cookies);
            req.ContentLength = 0;

            var result = RequestResponse.GetHttpWebResponse(req, UpdateCookieStorage, out response);
            Utils.Debug.Info($"GetRequestVerificationToken - HTTP response result: {result}", LogCategory);

            if (result == LoginResult.Success)
            {
                verificationToken = RequestResponse.GetRequestVerificationTokenResponse(response);
                Utils.Debug.Info($"GetRequestVerificationToken - Token extracted: {(string.IsNullOrEmpty(verificationToken) ? "null/empty" : $"length {verificationToken.Length}")}", LogCategory);
            }

            return result;
        }
        
        private bool TryGetExistingAccessToken(bool sisi, out Token accessToken)
        {
            Utils.Debug.Info($"TryGetExistingAccessToken - Checking for existing token | Sisi: {sisi}", LogCategory);
            Token checkToken = sisi ? SisiToken : TranquilityToken;
            if (checkToken != null && !checkToken.IsExpired)
            {
                Utils.Debug.Info($"TryGetExistingAccessToken - Found valid existing token, expiration: {checkToken.Expiration}", LogCategory);
                accessToken = checkToken;
                return true;
            }

            Utils.Debug.Info($"TryGetExistingAccessToken - No valid existing token found (token null: {checkToken == null}, expired: {checkToken?.IsExpired})", LogCategory);
            accessToken = null;
            return false;
        }

        private bool TryGetFromRefreshToken(bool sisi, out Token accessToken)
        {
            Utils.Debug.Info($"TryGetFromRefreshToken - Starting | Sisi: {sisi}", LogCategory);
            try
            {
                if (!App.Settings.UseRefreshTokens)
                {
                    Utils.Debug.Info($"TryGetFromRefreshToken - Refresh tokens disabled in settings", LogCategory);
                    accessToken = null;
                    return false;
                }

                Utils.Debug.Info($"TryGetFromRefreshToken - Decrypting refresh tokens", LogCategory);
                DecryptTranquilityRefreshToken(true);
                DecryptSisiRefreshToken(true);
                SecureString refreshToken = sisi ? SecureSisiRefreshToken : SecureTranquilityRefreshToken;
                if (refreshToken == null || refreshToken.Length == 0)
                {
                    Utils.Debug.Info($"TryGetFromRefreshToken - No refresh token available (null: {refreshToken == null}, length: {refreshToken?.Length ?? 0})", LogCategory);
                    accessToken = null;
                    return false;
                }

                Utils.Debug.Info($"TryGetFromRefreshToken - Refresh token found (length: {refreshToken.Length}), exchanging for access token", LogCategory);
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
                Utils.Debug.Info($"TryGetFromRefreshToken - Sending refresh token request", LogCategory);
                LoginResult result = GetAccessToken(sisi, req, out accessToken);
                Utils.Debug.Info($"TryGetFromRefreshToken - Refresh token exchange result: {result}", LogCategory);
                if (result == LoginResult.Success)
                {
                    Utils.Debug.Info($"TryGetFromRefreshToken - Success, storing settings", LogCategory);
                    App.Settings.Store();
                    return true;
                }
            }
            catch (Exception e)
            {
                Utils.Debug.Error($"TryGetFromRefreshToken exception: {e}", LogCategory);
                throw;
            }
            Utils.Debug.Info($"TryGetFromRefreshToken - Failed to get token from refresh token", LogCategory);
            accessToken = null;
            return false;
        }

        public LoginResult GetAccessToken(bool sisi, out Token accessToken)
        {
            Utils.Debug.Info($"GetAccessToken - Starting login flow | Username: {Username} | Sisi: {sisi}", LogCategory);

            // first check for an existing, valid token
            Utils.Debug.Info($"GetAccessToken - Checking for existing valid token", LogCategory);
            if (TryGetExistingAccessToken(sisi, out accessToken))
            {
                Utils.Debug.Info($"GetAccessToken - Using existing valid token", LogCategory);
                return LoginResult.Success;
            }

            // need SecurePassword.
            Utils.Debug.Info($"GetAccessToken - Checking for SecurePassword", LogCategory);
            if (SecurePassword == null || SecurePassword.Length == 0)
            {
                Utils.Debug.Info($"GetAccessToken - No SecurePassword, attempting to decrypt", LogCategory);
                DecryptPassword(true);
                if (SecurePassword == null || SecurePassword.Length == 0)
                {
                    Utils.Debug.Info($"GetAccessToken - Showing login dialog", LogCategory);
                    Windows.EVELogin el = new Windows.EVELogin(this, true);
                    bool? dialogResult = el.ShowDialog();

                    if (SecurePassword == null || SecurePassword.Length == 0)
                    {
                        Utils.Debug.Info($"GetAccessToken - Password not provided, returning InvalidUsernameOrPassword", LogCategory);
                        // password is required, sorry dude
                        accessToken = null;
                        return LoginResult.InvalidUsernameOrPassword;
                    }

                    Utils.Debug.Info($"GetAccessToken - Password provided, storing settings", LogCategory);
                    App.Settings.Store();
                }
            }

            Utils.Debug.Info($"GetAccessToken - Attempting refresh token login", LogCategory);
            if (TryGetFromRefreshToken(sisi, out accessToken))
            {
                Utils.Debug.Info($"GetAccessToken - Refresh token login successful", LogCategory);
                return LoginResult.Success;
            }

            Utils.Debug.Info($"GetAccessToken - Setting App.strUserName and App.strPassword for legacy flow", LogCategory);
            App.strUserName = Username;
            App.strPassword = new System.Net.NetworkCredential(string.Empty, SecurePassword).Password;

            if (App.Settings.ManualLogin)
            {
                Utils.Debug.Info($"GetAccessToken - Manual login mode enabled, opening WebView2 login window", LogCategory);
                var manualLoginWindow = new EVEManualLogin(this, sisi);
                manualLoginWindow.ShowDialog();
                accessToken = manualLoginWindow.AccessToken;
                var manualResult = manualLoginWindow.LoginResult;
                Utils.Debug.Info($"GetAccessToken - Manual login result: {manualResult}", LogCategory);
                // save the token if we got one.
                if (manualResult == LoginResult.Success && accessToken != null)
                {
                    Utils.Debug.Info($"GetAccessToken - Manual login successful, storing token", LogCategory);
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

            Utils.Debug.Info($"GetAccessToken - Automatic login mode, getting login URI", LogCategory);
            var uri = RequestResponse.GetLoginUri(sisi, state.ToString(), challengeHash);
            Utils.Debug.Info($"GetAccessToken - Login URI: {uri}", LogCategory);

            string RequestVerificationToken = string.Empty;
            Utils.Debug.Info($"GetAccessToken - Getting request verification token", LogCategory);
            var result = GetRequestVerificationToken(uri, sisi, out RequestVerificationToken);

            if (result == LoginResult.Error)
            {
                Utils.Debug.Error($"GetAccessToken - Failed to get request verification token", LogCategory);
                accessToken = null;
                return result;
            }

            Utils.Debug.Info($"GetAccessToken - Got verification token, creating login POST request", LogCategory);
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

            Utils.Debug.Info($"GetAccessToken - Sending login request", LogCategory);
            var loginResult = GetAccessToken(sisi, req, out accessToken);
            Utils.Debug.Info($"GetAccessToken - Login result: {loginResult}", LogCategory);
            return loginResult;
        }

        public LoginResult GetSSOToken(bool sisi, out Token ssoToken)
        {
            Utils.Debug.Info($"GetSSOToken - Starting | Username: {Username} | Sisi: {sisi}", LogCategory);
            LoginResult lr = this.GetAccessToken(sisi, out ssoToken);
            Utils.Debug.Info($"GetSSOToken - Result: {lr} | Token obtained: {ssoToken != null}", LogCategory);
            return lr;
        }

        public LoginResult Launch(string sharedCachePath, bool sisi, DirectXVersion dxVersion, long characterID)
        {
            Utils.Debug.Info($"Launch(sharedCache) - Starting | Username: {Username} | SharedCachePath: {sharedCachePath} | Sisi: {sisi} | DX: {dxVersion} | CharacterID: {characterID}", LogCategory);
            Token ssoToken;
            LoginResult lr = GetSSOToken(sisi, out ssoToken);
            if (lr != LoginResult.Success)
            {
                Utils.Debug.Warning($"Launch(sharedCache) - GetSSOToken failed with result: {lr}", LogCategory);
                return lr;
            }
            Utils.Debug.Info($"Launch(sharedCache) - Got SSO token, launching game", LogCategory);
            if (!App.Launch(sharedCachePath, sisi, dxVersion, characterID, ssoToken))
            {
                Utils.Debug.Error($"Launch(sharedCache) - App.Launch returned false", LogCategory);
                return LoginResult.Error;
            }

            Utils.Debug.Info($"Launch(sharedCache) - Success", LogCategory);
            return LoginResult.Success;
        }

        public LoginResult Launch(string gameName, string gameProfileName, bool sisi, DirectXVersion dxVersion, long characterID)
        {
            Utils.Debug.Info($"Launch(gameProfile) - Starting | Username: {Username} | GameName: {gameName} | Profile: {gameProfileName} | Sisi: {sisi} | DX: {dxVersion} | CharacterID: {characterID}", LogCategory);
            Token ssoToken;
            LoginResult lr = GetSSOToken(sisi, out ssoToken);
            if (lr != LoginResult.Success)
            {
                Utils.Debug.Warning($"Launch(gameProfile) - GetSSOToken failed with result: {lr}", LogCategory);
                return lr;
            }
            Utils.Debug.Info($"Launch(gameProfile) - Got SSO token, launching game", LogCategory);
            if (!App.Launch(gameName, gameProfileName, sisi, dxVersion, characterID, ssoToken))
            {
                Utils.Debug.Error($"Launch(gameProfile) - App.Launch returned false", LogCategory);
                return LoginResult.Error;
            }

            Utils.Debug.Info($"Launch(gameProfile) - Success", LogCategory);
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
