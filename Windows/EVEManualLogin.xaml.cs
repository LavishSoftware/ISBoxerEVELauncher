using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using ISBoxerEVELauncher.Enums;
using ISBoxerEVELauncher.Extensions;
using ISBoxerEVELauncher.Games.EVE;
using ISBoxerEVELauncher.Security;
using ISBoxerEVELauncher.Utils;
using ISBoxerEVELauncher.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;

namespace ISBoxerEVELauncher.Windows
{
    public partial class EVEManualLogin : Window, INotifyPropertyChanged
    {
        private readonly EVEAccount _account;
        private readonly bool _sisi;
        private string _loginUrl;
        private string _instructions;
        private HashSet<LoginState> _visitedStates = new HashSet<LoginState>();
        private readonly byte[] _challengeCode;
        private readonly string _challengeHash;
        private readonly Guid _state;

        public LoginResult LoginResult { get; set; }
        public EVEAccount.Token AccessToken { get; set; }

        public EVEManualLogin(EVEAccount account, bool sisi)
        {
            _account = account;
            _sisi = sisi;
            LoginResult = LoginResult.Error;
            _state = Guid.NewGuid();
            var challengeCodeSource = Guid.NewGuid();
            _challengeCode = Encoding.UTF8.GetBytes(challengeCodeSource.ToString().Replace("-", ""));
            _challengeHash = Base64UrlEncoder.Encode(ISBoxerEVELauncher.Security.SHA256.GenerateHash(Base64UrlEncoder.Encode(_challengeCode)));
            
            InitializeComponent();
            Instructions = "Please log in to your EVE Online account in the browser below.";
            Closing += OnWindowClosing;
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await WebView2.EnsureCoreWebView2Async(null);

                // Load saved cookies if available
                await LoadCookiesAsync();

                // Setup navigation completed handler
                WebView2.NavigationCompleted += WebView2_NavigationCompleted;
                
                var uri = RequestResponse.FullUri(_sisi, RequestResponse.GetLoginUri(_sisi, _state.ToString() , _challengeHash));

                Navigate(uri.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Navigate(string url)
        {
            if (WebView2?.CoreWebView2 != null)
            {
                WebView2.CoreWebView2.Navigate(url);
            }
        }

        private async void WebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                LoginUrl = WebView2.CoreWebView2.Source;

                if (e.IsSuccess)
                {
                    await ProcessNavigationAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Navigation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ProcessNavigationAsync()
        {
            try
            {
                if (WebView2 == null || WebView2.CoreWebView2 == null)
                    return;
                
                var url = WebView2.CoreWebView2.Source;
                var html = await WebView2.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML;");
                
                Debug.Info($"Navigated to URL: {url}");
                Debug.Info($"HTML: {html}");
                
                if (html.Contains("loginForm") && App.Settings.ManualLoginAutofill)
                {
                    FillAndCheckState(LoginState.LoginForm);
                    // Fill in username and password
                    // Sadly we cannot get around the SecureString to string conversion here as we have to 
                    // inject it into JavaScript
                    var password = GetEscapedPasswordString();
                    string script = $@"
                        document.getElementById('UserName').value = '{_account.Username}';
                        document.getElementById('Password').value = '{password}';
                        document.forms['loginForm'].submit();
                    ";
                    await WebView2.CoreWebView2.ExecuteScriptAsync(script);
                    return;
                }

                var codeUri = RequestResponse.FullUri(_sisi, new Uri("/launcher", UriKind.Relative));
                if (url.StartsWith(codeUri.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    
                    var authCode = HttpUtility.ParseQueryString(url).Get("code");
                    if (string.IsNullOrEmpty(authCode))
                    {
                        MessageBox.Show("Launcher page did not retun a valid code.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        CloseWithResult(LoginResult.Error);
                        return;
                    }

                    var tokenUri = RequestResponse.FullUri(_sisi, RequestResponse.GetTokenUri(_sisi));
                    
                    var req2 = RequestResponse.CreatePostRequest(new Uri(RequestResponse.token, UriKind.Relative), _sisi, true, RequestResponse.refererUri, null);
                    req2.SetBody(RequestResponse.GetSsoTokenRequestBody(_sisi, authCode, _challengeCode));
                    var respone = new Response(req2);
                    
                    var accessToken = new EVEAccount.Token(JsonConvert.DeserializeObject<EVEAccount.authObj>(respone.Body));
                    if (string.IsNullOrEmpty(accessToken.RefreshToken))
                    {
                        MessageBox.Show("Failed to retrieve access token.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        CloseWithResult(LoginResult.Error);
                        return;
                    }
                    
                    if (_sisi)
                    {
                        _account.SisiToken = accessToken;
                    }
                    else
                    {
                        _account.TranquilityToken = accessToken;
                    }

                    AccessToken = accessToken;
                    CloseWithResult(LoginResult.Success);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Process error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CloseWithResult(LoginResult.Error);
            }
        }
        
        private string GetEscapedPasswordString()
        {
            // Gets the password in a way where we can use it in JavaScript
            using (var ssw = new SecureStringWrapper(_account.SecurePassword, Encoding.ASCII))
            {
                var raw = Encoding.UTF8.GetString(ssw.ToByteArray());
                return raw.Replace("\\", "\\\\").Replace("'", "\\'");
            }
        }

        private void FillAndCheckState(LoginState state)
        {
            if (_visitedStates.Contains(state))
            {
                MessageBox.Show("Looping detected during login process.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CloseWithResult(LoginResult.Error);
                return;
            }
            _visitedStates.Add(state);
        }
        
        private void CloseWithResult(LoginResult result)
        {
            LoginResult = result;
            WebView2.NavigationCompleted -= WebView2_NavigationCompleted;
            this.Close();
        }

        private async Task LoadCookiesAsync()
        {
            if (string.IsNullOrEmpty(_account.WebView2CookieStorage))
                return;

            try
            {
                // Decode from base64
                byte[] data = Convert.FromBase64String(_account.WebView2CookieStorage);
                string json = System.Text.Encoding.UTF8.GetString(data);

                var cookies = JsonConvert.DeserializeObject<List<CookieData>>(json);
                if (cookies == null)
                    return;

                var cookieManager = WebView2.CoreWebView2.CookieManager;
                foreach (var cookieData in cookies)
                {
                    var cookie = cookieManager.CreateCookie(cookieData.Name, cookieData.Value, cookieData.Domain, cookieData.Path);
                    cookie.IsHttpOnly = cookieData.IsHttpOnly;
                    cookie.IsSecure = cookieData.IsSecure;
                    cookie.Expires = cookieData.Expires;

                    cookieManager.AddOrUpdateCookie(cookie);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load cookies: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task SaveCookiesAsync()
        {
            try
            {
                var cookieManager = WebView2.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync(null);

                var cookieDataList = cookies.Select(c => new CookieData
                {
                    Name = c.Name,
                    Value = c.Value,
                    Domain = c.Domain,
                    Path = c.Path,
                    IsHttpOnly = c.IsHttpOnly,
                    IsSecure = c.IsSecure,
                    Expires = c.Expires,
                }).ToList();

                // Serialize to JSON then encode as base64
                string json = JsonConvert.SerializeObject(cookieDataList);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                _account.WebView2CookieStorage = Convert.ToBase64String(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save cookies: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            await SaveCookiesAsync();
        }

        public string LoginUrl
        {
            get => _loginUrl;
            set
            {
                _loginUrl = value;
                OnPropertyChanged(nameof(LoginUrl));
            }
        }

        public string Instructions
        {
            get => _instructions;
            set
            {
                _instructions = value;
                OnPropertyChanged(nameof(Instructions));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class CookieData
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Domain { get; set; }
            public string Path { get; set; }
            public bool IsHttpOnly { get; set; }
            public bool IsSecure { get; set; }
            public DateTime Expires { get; set; }
        }

        private enum LoginState
        {
            LoginForm,
        }
    }
}