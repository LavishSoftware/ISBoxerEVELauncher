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
    public class EVEAccount : INotifyPropertyChanged, IDisposable
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

        /// <summary>
        /// The EVE login process requires cookies, this will ensure we maintain the same cookies for the session
        /// </summary>
        CookieContainer Cookies = new CookieContainer();

        string _Username;
        /// <summary>
        /// EVE Account username
        /// </summary>
        public string Username { get { return _Username; } set { _Username = value; OnPropertyChanged("Username"); } }

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
        /// Attempts to prepare the encrypted verison of the currently active SecurePassword
        /// </summary>
        public void EncryptPassword()
        {
            SetEncryptedPassword(SecurePassword);
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

                rjm.Key = App.Settings.PasswordMasterKey.Bytes;

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

                rjm.Key = App.Settings.PasswordMasterKey.Bytes;
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
            return GetAccessToken(sisi, req, out accessToken);
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


            string uri = "https://login.eveonline.com/Account/Authenticator?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/Account/Authenticator?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Fsisilogin.testeveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
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
            return GetAccessToken(sisi, req, out accessToken);
        }

        public LoginResult GetCharacterChallenge(bool sisi, out Token accessToken)
        {
            Windows.CharacterChallengeWindow ccw = new Windows.CharacterChallengeWindow(this);
            ccw.ShowDialog();
            if (!ccw.DialogResult.HasValue || !ccw.DialogResult.Value)
            {
                SecurePassword = null;
                accessToken = null;
                return LoginResult.InvalidCharacterChallenge;
            }


            string uri = "https://login.eveonline.com/Account/Challenge?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
            if (sisi)
            {
                uri = "https://sisilogin.testeveonline.com/Account/Challenge?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Fsisilogin.testeveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
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
                body.Bytes = Encoding.ASCII.GetBytes(String.Format("Challenge={0}&RememberCharacterChallenge={1}", Uri.EscapeDataString(ccw.CharacterName), "true"));
                
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

                    if (responseBody.Contains("Character challenge"))
                    {
                        return GetCharacterChallenge(sisi, out accessToken);
                    }

                    if (responseBody.Contains("form action=\"/Account/Authenticator\""))
                    {
                        return GetAuthenticatorChallenge(sisi, out accessToken);
                    }

                    if (responseBody.Contains("form action=\"/OAuth/Eula\""))
                    {
                        return GetEULAChallenge(sisi, responseBody, out accessToken);
                    }

                    //                https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=l4nGki1CTUI7pCQZoIdnARcCLqL6ZGJM1X1tPf1bGKSJxEwP8lk_shS19w3sjLzyCbecYAn05y-Vbs-Jm1d1cw2&token_type=Bearer&expires_in=43200
                    accessToken = new Token(resp.ResponseUri);

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
            catch(System.Net.WebException we)
            {
                switch(we.Status)
                {
                    case WebExceptionStatus.Timeout:
                        accessToken = null;
                        return LoginResult.Timeout;
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
            req.Timeout = 5000;
            req.AllowAutoRedirect = false;

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                Uri responseUri = new Uri(resp.GetResponseHeader("Location"));
                ssotoken = HttpUtility.ParseQueryString(responseUri.Fragment).Get("#access_token");
                
            }
            return LoginResult.Success;
        }

        public LoginResult Launch(string sharedCachePath, bool sisi, DirectXVersion dxVersion)
        {
            string ssoToken;
            LoginResult lr = GetSSOToken(sisi, out ssoToken);
            if (lr != LoginResult.Success)
                return lr;
            if (!App.Launch(ssoToken, sharedCachePath, sisi, dxVersion))
                return LoginResult.Error;

            return LoginResult.Success;
        }

        public LoginResult Launch(string gameName, string gameProfileName, bool sisi, DirectXVersion dxVersion)
        {
            string ssoToken;
            LoginResult lr = GetSSOToken(sisi, out ssoToken);
            if (lr != LoginResult.Success)
                return lr;
            if (!App.Launch(ssoToken, gameName, gameProfileName, sisi, dxVersion))
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
            this.Username = null;
        }

        public override string ToString()
        {
            return Username;
        }
    }
}
