//#define REFRESH_TOKENS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Web;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;

namespace ISBoxerEVELauncher
{
    public enum DirectXVersion
    {
        Default,
        dx9,
        dx11,
    }

    /// <summary>
    /// An EVE Online account and related data
    /// </summary>
    public class EVEAccount : INotifyPropertyChanged, IDisposable, ISBoxerEVELauncher.Launchers.ILaunchTarget
    {
        /// <summary>
        /// An Outh2 Access Token
        /// </summary>
        public class Token
        {
            public Token()
            {

            }

            /// <summary>
            /// We usually just need to parse a Uri for the Access Token details. So here is the constructor that does it for us.
            /// </summary>
            /// <param name="fromUri"></param>
            public Token(Uri fromUri)
            {
                TokenString = HttpUtility.ParseQueryString(fromUri.Fragment).Get("#access_token");
                String expires_in = HttpUtility.ParseQueryString(fromUri.Fragment).Get("expires_in");
                Expiration = DateTime.Now.AddSeconds(int.Parse(expires_in));
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
            public string TokenString { get; set; }
            /// <summary>
            /// When the token is good until...
            /// </summary>
            public DateTime Expiration { get; set; }
        }

        CookieContainer _Cookies;

        /// <summary>
        /// The EVE login process requires cookies; this will ensure we maintain the same cookies for the account
        /// </summary>
        CookieContainer Cookies
        {
            get
            {
                if (_Cookies == null)
                {
                    if (!string.IsNullOrEmpty(CookieStorage))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();


                        using (Stream s = new MemoryStream(Convert.FromBase64String(CookieStorage)))
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
                CookieStorage = null;
                return;
            }
            
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, Cookies);
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                CookieStorage = Convert.ToBase64String(ms.ToArray());
            }
            
        }

        string _Username;
        /// <summary>
        /// EVE Account username
        /// </summary>
        public string Username { get { return _Username; } set { _Username = value; OnPropertyChanged("Username"); } }

        public string CookieStorage
        {
            get;set;
        }


        #region Password
        System.Security.SecureString _SecurePassword;
        /// <summary>
        /// A Secure (and non-plaintext) representation of the password. This will NOT be stored in XML.
        /// </summary>
        [XmlIgnore]
        public System.Security.SecureString SecurePassword { get { return _SecurePassword; } set { _SecurePassword = value; OnPropertyChanged("SecurePassword"); EncryptedPassword = null; EncryptedPasswordIV = null; } }

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
            set { _EncryptedPassword = value; OnPropertyChanged("EncryptedPassword"); } 
        }

        string _EncryptedPasswordIV;
        /// <summary>
        /// The Initialization Vector used to encrypt the password
        /// </summary>
        public string EncryptedPasswordIV { get { return _EncryptedPasswordIV; } set { _EncryptedPasswordIV = value; OnPropertyChanged("EncryptedPasswordIV"); } }
        
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

                using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey,true))
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
            if (SecurePassword!=null)
            {
                EncryptPassword();
            }
            if (SecureCharacterName!=null)
            {
                EncryptCharacterName();
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
        public System.Security.SecureString SecureCharacterName { get { return _SecureCharacterName; } set { _SecureCharacterName = value; OnPropertyChanged("SecureCharacterName"); EncryptedCharacterName = null; EncryptedCharacterNameIV = null; } }

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
            set { _EncryptedCharacterName = value; OnPropertyChanged("EncryptedCharacterName"); }
        }

        string _EncryptedCharacterNameIV;
        /// <summary>
        /// The Initialization Vector used to encrypt the CharacterName
        /// </summary>
        public string EncryptedCharacterNameIV { get { return _EncryptedCharacterNameIV; } set { _EncryptedCharacterNameIV = value; OnPropertyChanged("EncryptedCharacterNameIV"); } }

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


        Token _TranquilityToken;
        /// <summary>
        /// AccessToken for Tranquility. Lasts up to 11 hours?
        /// </summary>
        [XmlIgnore]
        public Token TranquilityToken { get { return _TranquilityToken; } set { _TranquilityToken = value; OnPropertyChanged("TranquilityToken"); } }
        
        Token _SisiToken;
        /// <summary>
        /// AccessToken for Singularity. Lasts up to 11 hours?
        /// </summary>
        [XmlIgnore]
        public Token SisiToken { get { return _SisiToken; } set { _SisiToken = value; OnPropertyChanged("SisiToken"); } }

        #region Refresh Tokens
        /* This section is for experimental implemtnation using Refresh Tokens, which are used by the official EVE Launcher and described as insecure.
         * They ultimately need the same encrypted storage care as a Password. May or may not be worth implementing. 
         * The code will not compile at this time if enabled.
         */
#if REFRESH_TOKENS
        string _SisiRefreshToken;
        public string SisiRefreshToken { get { return _SisiRefreshToken; } set { _SisiRefreshToken = value; OnPropertyChanged("SisiRefreshToken"); } }

        string _TranquilityRefreshToken;
        public string TranquilityRefreshToken { get { return _TranquilityRefreshToken; } set { _TranquilityRefreshToken = value; OnPropertyChanged("TranquilityRefreshToken"); } }

        public void GetTokensFromCode(bool sisi, string code)
        {
            string uri = "https://client.eveonline.com/launcher/en/SSOVerifyUser";
            if (sisi)
            {
                uri = "https://testclient.eveonline.com/launcher/en/SSOVerifyUser";
            }

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 5000;
            req.AllowAutoRedirect = true;
            /*
            if (!sisi)
            {
                req.Headers.Add("Origin", "https://login.eveonline.com");
            }
            else
            {
                req.Headers.Add("Origin", "https://sisilogin.testeveonline.com");
            }
            /**/
            //req.Referer = uri;
            //req.CookieContainer = Cookies;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            byte[] body = Encoding.ASCII.GetBytes(String.Format("authCode={0}", Uri.EscapeDataString(code)));
            req.ContentLength = body.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(body, 0, body.Length);
            }

            string refreshCode;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                // https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=...&token_type=Bearer&expires_in=43200
                string responseBody = null;
                using (Stream stream = resp.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        responseBody = sr.ReadToEnd();
                    }
                }

                /*
<span id="ValidationContainer"><div class="validation-summary-errors"><span>Login failed. Possible reasons can be:</span>
<ul><li>Invalid username / password</li>
</ul></div></span>
                 */

                //                https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=l4nGki1CTUI7pCQZoIdnARcCLqL6ZGJM1X1tPf1bGKSJxEwP8lk_shS19w3sjLzyCbecYAn05y-Vbs-Jm1d1cw2&token_type=Bearer&expires_in=43200
                //accessToken = new Token(resp.ResponseUri);
                //refreshCode = HttpUtility.ParseQueryString(resp.ResponseUri.Query).Get("code");

                // String expires_in = HttpUtility.ParseQueryString(fromUri.Fragment).Get("expires_in");

                throw new NotImplementedException();
                // responseBody should now be JSON containing the needed tokens.
            }

        }

        public LoginResult GetRefreshToken(bool sisi, out string refreshToken)
        {
            string checkToken = sisi ? SisiRefreshToken : TranquilityRefreshToken;
            if (!string.IsNullOrEmpty(checkToken))
            {
                refreshToken = checkToken;
                return LoginResult.Success;
            }

            // need PlaintextPassword.
            if (SecurePassword == null || SecurePassword.Length == 0)
            {
                Windows.EVELogin el = new Windows.EVELogin(this, false);
                bool? result = el.ShowDialog();

                if (SecurePassword == null || SecurePassword.Length == 0)
                {
                    // password is required, sorry dude
                    refreshToken = null;
                    return LoginResult.InvalidUsernameOrPassword;
                }
            }

            string uri = "https://login.eveonline.com/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dcode%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken%2520user";
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dcode%26redirect_uri%3Dhttps%3A%2F%2Fsisilogin.testeveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken%2520user";
            }

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 5000;
            req.AllowAutoRedirect = true;
            if (!sisi)
            {
                req.Headers.Add("Origin", "https://login.eveonline.com");
            }
            else
            {
                req.Headers.Add("Origin", "https://sisilogin.testeveonline.com");
            }
            req.Referer = uri;
            req.CookieContainer = Cookies;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            using (SecureBytesWrapper body = new SecureBytesWrapper())
            {
                byte[] body1 = Encoding.ASCII.GetBytes(String.Format("UserName={0}&Password=", Uri.EscapeDataString(Username)));
                using (SecureStringWrapper ssw = new SecureStringWrapper(SecurePassword, Encoding.ASCII))
                {
                    using (SecureBytesWrapper escapedPassword = new SecureBytesWrapper())
                    {
                        escapedPassword.Bytes = System.Web.HttpUtility.UrlEncodeToBytes(ssw.ToByteArray());

                        body.Bytes = new byte[body1.Length + escapedPassword.Bytes.Length];
                        System.Buffer.BlockCopy(body1, 0, body.Bytes, 0, body1.Length);
                        System.Buffer.BlockCopy(escapedPassword.Bytes, 0, body.Bytes, body1.Length, escapedPassword.Bytes.Length);
                    }
                }

                req.ContentLength = body.Bytes.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(body.Bytes, 0, body.Bytes.Length);
                }
            }

            string refreshCode;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                // https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=...&token_type=Bearer&expires_in=43200
                string responseBody = null;
                using (Stream stream = resp.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        responseBody = sr.ReadToEnd();
                    }
                }

                if (responseBody.Contains("Invalid username / password"))
                {
                    refreshToken = null;
                    return LoginResult.InvalidUsernameOrPassword;
                }


                /*
<span id="ValidationContainer"><div class="validation-summary-errors"><span>Login failed. Possible reasons can be:</span>
<ul><li>Invalid username / password</li>
</ul></div></span>
                 */

                //                https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=l4nGki1CTUI7pCQZoIdnARcCLqL6ZGJM1X1tPf1bGKSJxEwP8lk_shS19w3sjLzyCbecYAn05y-Vbs-Jm1d1cw2&token_type=Bearer&expires_in=43200
                //accessToken = new Token(resp.ResponseUri);
                refreshCode = HttpUtility.ParseQueryString(resp.ResponseUri.Query).Get("code");

                    // String expires_in = HttpUtility.ParseQueryString(fromUri.Fragment).Get("expires_in");
            }

            GetTokensFromCode(sisi,refreshCode);
            throw new NotImplementedException();

            if (!sisi)
            {
                TranquilityRefreshToken = refreshToken;
            }
            else
            {
                SisiRefreshToken = refreshToken;
            }

            return LoginResult.Success;
        }
#endif
        #endregion

        public enum LoginResult
        {
            Success,
            Error,
            Timeout,
            InvalidUsernameOrPassword,
            InvalidCharacterChallenge,
            InvalidAuthenticatorChallenge,
            EULADeclined,
            EmailVerificationRequired,
            SecurityWarningClosed,
            TokenFailure,
        }

        private static string GetRequestVerificationToken(string body)
        {
            // <input name="__RequestVerificationToken" type="hidden" value="rGFOR5OvmlpJ_6_Kabcx3JSrJ3v6EL0W6tuOuD-e8QvUuK2l1MX5jP7pztjxnm5k0qgHIv-mati2ctst9M8kD9jBg3E1" />
            const string needle = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
            int hashStart = body.IndexOf(needle, StringComparison.Ordinal);
            if (hashStart == -1)
                return null;

            hashStart += needle.Length;

            // get hash end
            int hashEnd = body.IndexOf('"', hashStart);
            if (hashEnd == -1)
                return null;

            return body.Substring(hashStart, hashEnd - hashStart);
        }

        private static string GetEulaHash(string body)
        {
            const string needle = "name=\"eulaHash\" type=\"hidden\" value=\"";
            int hashStart = body.IndexOf(needle, StringComparison.Ordinal);
            if (hashStart == -1)
                return null;
            return body.Substring(hashStart + needle.Length, 32);
        }
        private static string GetEulaReturnUrl(string body)
        {
            const string needle = "input id=\"returnUrl\" name=\"returnUrl\" type=\"hidden\" value=\"";
            int fieldStart = body.IndexOf(needle, StringComparison.Ordinal);
            if (fieldStart == -1)
                return null;

            fieldStart += needle.Length;
            int fieldEnd = body.IndexOf('"', fieldStart);


            return body.Substring(fieldStart, fieldEnd-fieldStart);
        }

        public LoginResult GetSecurityWarningChallenge(bool sisi, string responseBody, out Token accessToken)
        {
            /*
            Windows.SecurityWarningWindow swWindow = new Windows.SecurityWarningWindow(responseBody);
            swWindow.ShowDialog();

            // /oauth/authorize/?client_id=eveLauncherTQ&lang=en&response_type=token&redirect_uri=https://login.eveonline.com/launcher?client_id=eveLauncherTQ&scope=eveClientToken
      
            if (string.IsNullOrEmpty( swWindow.URI))
            {
                SecurePassword = null;
                accessToken = null;
                return LoginResult.SecurityWarningClosed;
            }
            */

            string uri = "https://login.eveonline.com/oauth/authorize/?client_id=eveLauncherTQ&lang=en&response_type=token&redirect_uri=https://login.eveonline.com/launcher?client_id=eveLauncherTQ&scope=eveClientToken";
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/oauth/authorize/?client_id=eveLauncherTQ&lang=en&response_type=token&redirect_uri=https://sisilogin.testeveonline.com/launcher?client_id=eveLauncherTQ&scope=eveClientToken";
            }

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 30000;
            req.AllowAutoRedirect = true;
            if (!sisi)
            {
                req.Headers.Add("Origin", "https://login.eveonline.com");
            }
            else
            {
                req.Headers.Add("Origin", "https://sisilogin.testeveonline.com");
            }
            req.Referer = uri;
            req.CookieContainer = Cookies;
            req.Method = "GET";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = 0;
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

        public LoginResult GetEULAChallenge(bool sisi, string responseBody,  out Token accessToken)
        {
            Windows.EVEEULAWindow eulaWindow = new Windows.EVEEULAWindow(responseBody);
            eulaWindow.ShowDialog();
            if (!eulaWindow.DialogResult.HasValue || !eulaWindow.DialogResult.Value)
            {
                SecurePassword = null;
                accessToken = null;
                return LoginResult.EULADeclined;
            }

            string uri = "https://login.eveonline.com/OAuth/Eula";
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/OAuth/Eula";
            }


            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 30000;
            req.AllowAutoRedirect = true;
            if (!sisi)
            {
                req.Headers.Add("Origin", "https://login.eveonline.com");
            }
            else
            {
                req.Headers.Add("Origin", "https://sisilogin.testeveonline.com");
            }
            req.Referer = uri;
            req.CookieContainer = Cookies;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            using (SecureBytesWrapper body = new SecureBytesWrapper())
            {
                string eulaHash = GetEulaHash(responseBody);
                string returnUrl = GetEulaReturnUrl(responseBody);

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
            try
            {
                return GetAccessToken(sisi, req, out accessToken);
            }
            catch (System.Net.WebException we)
            {
                return GetAccessToken(sisi, out accessToken);
            }
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

            string uri = "https://login.eveonline.com/account/authenticator?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/account/authenticator?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Fsisilogin.testeveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            }

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 30000;
            req.AllowAutoRedirect = true;
            if (!sisi)
            {
                req.Headers.Add("Origin", "https://login.eveonline.com");
            }
            else
            {
                req.Headers.Add("Origin", "https://sisilogin.testeveonline.com");
            }
            req.Referer = uri;
            req.CookieContainer = Cookies;            
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
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
            
            string uri = "https://login.eveonline.com/Account/Challenge?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/Account/Challenge?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Fsisilogin.testeveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            }

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 30000;
            req.AllowAutoRedirect = true;
            if (!sisi)
            {
                req.Headers.Add("Origin", "https://login.eveonline.com");
            }
            else
            {
                req.Headers.Add("Origin", "https://sisilogin.testeveonline.com");
            }
            req.Referer = uri;
            req.CookieContainer = Cookies;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
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
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    // https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=...&token_type=Bearer&expires_in=43200
                    string responseBody = null;
                    using (Stream stream = resp.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            responseBody = sr.ReadToEnd();
                        }
                    }
                    UpdateCookieStorage();
                    /*
<span id="ValidationContainer"><div class="validation-summary-errors"><span>Login failed. Possible reasons can be:</span>
<ul><li>Invalid username / password</li>
<li>Incorrect character name entered</li>
</ul></div></span>
                     */
                    if (responseBody.Contains("Incorrect character name entered"))
                    {
                        accessToken = null;
                        SecurePassword = null;
                        SecureCharacterName = null;
                        return LoginResult.InvalidCharacterChallenge;
                    }

                    /*
    <span id="ValidationContainer"><div class="validation-summary-errors"><span>Login failed. Possible reasons can be:</span>
    <ul><li>Invalid username / password</li>
    </ul></div></span>
                     */

                    if (responseBody.Contains("Invalid username / password"))
                    {
                        accessToken = null;
                        SecurePassword = null;
                        return LoginResult.InvalidUsernameOrPassword;
                    }

                    // I'm just guessing on this one at the moment.
                    if (responseBody.Contains("Invalid authenticat"))
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

                    if (responseBody.Contains("Security Warning"))
                    {
                        return GetSecurityWarningChallenge(sisi, responseBody, out accessToken);
                    }

                    if (responseBody.ToLower().Contains("form action=\"/oauth/eula\"")) 
                    {
                        return GetEULAChallenge(sisi, responseBody, out accessToken);
                    }

                    try
                    {
                        //                https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=l4nGki1CTUI7pCQZoIdnARcCLqL6ZGJM1X1tPf1bGKSJxEwP8lk_shS19w3sjLzyCbecYAn05y-Vbs-Jm1d1cw2&token_type=Bearer&expires_in=43200
                        accessToken = new Token(resp.ResponseUri);
                    }
                    catch (Exception e)
                    {
                        Windows.UnhandledResponseWindow urw = new Windows.UnhandledResponseWindow(responseBody);
                        urw.ShowDialog();

                        // can't get the token
                        accessToken = null;
                        SecurePassword = null;
                        return LoginResult.TokenFailure;
                    }

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
                        accessToken = null;
                        return LoginResult.Timeout;
                    default:
                        string responseBody = null;
                        using (Stream stream = we.Response.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                responseBody = sr.ReadToEnd();
                            }
                        }

                        Windows.UnhandledResponseWindow urw = new Windows.UnhandledResponseWindow(responseBody);
                        urw.ShowDialog();
                        accessToken = null;
                        return LoginResult.Error;
                }
            }
        }

        public LoginResult GetRequestVerificationToken(bool sisi, out string verificationToken)
        {
            string uri = "https://login.eveonline.com/Account/LogOn";
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/Account/LogOn";
            }

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 30000;
            req.AllowAutoRedirect = true;
            req.Referer = uri;
            req.CookieContainer = Cookies;
            req.Method = "GET";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = 0;
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {                   
                    string responseBody = null;
                    using (Stream stream = resp.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            responseBody = sr.ReadToEnd();
                        }
                    }
                    UpdateCookieStorage();

                    verificationToken = GetRequestVerificationToken(responseBody);
                    return LoginResult.Success;
                }
            }
            catch (System.Net.WebException ex)
            {
                switch (ex.Status)
                {
                    case WebExceptionStatus.Timeout:
                        {
                            verificationToken = string.Empty;
                            return LoginResult.Timeout;
                        }
                    default:
                        throw;
                }
            }
        }
    

        public LoginResult GetAccessToken(bool sisi, out Token accessToken)
        {
            Token checkToken = sisi ? SisiToken : TranquilityToken;
            if (checkToken!=null && !checkToken.IsExpired)
            {
                accessToken = checkToken;
                return LoginResult.Success;
            }

            // need SecurePassword.
            if (SecurePassword==null || SecurePassword.Length == 0)
            {             
                DecryptPassword(true);
                if (SecurePassword == null || SecurePassword.Length == 0)
                {

                    Windows.EVELogin el = new Windows.EVELogin(this, true);
                    bool? result = el.ShowDialog();

                    if (SecurePassword == null || SecurePassword.Length == 0)
                    {
                        // password is required, sorry dude
                        accessToken = null;
                        return LoginResult.InvalidUsernameOrPassword;
                    }

                    App.Settings.Store();
                }
            }

            string uri = "https://login.eveonline.com/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Fsisilogin.testeveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            }

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 30000;
            req.AllowAutoRedirect = true;
            if (!sisi)
            {
                req.Headers.Add("Origin", "https://login.eveonline.com");
            }
            else
            {
                req.Headers.Add("Origin", "https://sisilogin.testeveonline.com");
            }
            req.Referer = uri;
            req.CookieContainer = Cookies;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            string RequestVerificationToken = string.Empty;
            GetRequestVerificationToken(sisi, out RequestVerificationToken);
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
                catch(System.Net.WebException e)
                {
                    switch(e.Status)
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

        public LoginResult GetSSOToken(bool sisi, out string ssotoken)
        {
            Token accessToken;
            LoginResult lr = this.GetAccessToken(sisi, out accessToken);
            if (accessToken == null)
            {
                ssotoken = null;
                return lr;
            }
            string uri = "https://login.eveonline.com/launcher/token?accesstoken=" + accessToken;
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/launcher/token?accesstoken=" + accessToken;
            }
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Timeout = 30000;
            req.AllowAutoRedirect = false;

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                Uri responseUri = new Uri(resp.GetResponseHeader("Location"));
                ssotoken = HttpUtility.ParseQueryString(responseUri.Fragment).Get("#access_token");
                
            }
            UpdateCookieStorage();
            return LoginResult.Success;
        }

        public LoginResult Launch(string sharedCachePath, bool sisi, DirectXVersion dxVersion, long characterID)
        {
            string ssoToken;
            LoginResult lr = GetSSOToken(sisi, out ssoToken);
            if (lr != LoginResult.Success)
                return lr;
            if (!App.Launch(ssoToken, sharedCachePath, sisi, dxVersion, characterID))
                return LoginResult.Error;

            return LoginResult.Success;
        }

        public LoginResult Launch(string gameName, string gameProfileName, bool sisi, DirectXVersion dxVersion, long characterID)
        {
            string ssoToken;
            LoginResult lr = GetSSOToken(sisi, out ssoToken);
            if (lr != LoginResult.Success)
                return lr;
            if (!App.Launch(ssoToken, gameName, gameProfileName, sisi, dxVersion, characterID))
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
            if (this.SecureCharacterName!=null)
            {
                this.SecureCharacterName.Dispose();
                this.SecureCharacterName = null;
            }
            this.EncryptedCharacterName = null;
            this.EncryptedCharacterNameIV = null;
            this.Username = null;
            this.Cookies = null;
            this.CookieStorage = null;
        }

        public override string ToString()
        {
            return Username;
        }

        EVEAccount Launchers.ILaunchTarget.EVEAccount
        {
            get { return this; }
        }

        public long CharacterID
        {
            get { return 0; }
        }
    }
}
