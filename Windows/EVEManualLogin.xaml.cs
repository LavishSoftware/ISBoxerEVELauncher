using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;

namespace ISBoxerEVELauncher.Windows
{
    public partial class EVEManualLogin : Window, INotifyPropertyChanged
    {
        private const string LogCategory = "EVEManualLogin";

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
            Debug.Info($"EVEManualLogin - Constructor starting | Username: {account.Username} | Sisi: {sisi}", LogCategory);
            _account = account;
            _sisi = sisi;
            LoginResult = LoginResult.Error;
            _state = Guid.NewGuid();
            var challengeCodeSource = Guid.NewGuid();
            _challengeCode = Encoding.UTF8.GetBytes(challengeCodeSource.ToString().Replace("-", ""));
            _challengeHash = Base64UrlEncoder.Encode(ISBoxerEVELauncher.Security.SHA256.GenerateHash(Base64UrlEncoder.Encode(_challengeCode)));
            Debug.Info($"EVEManualLogin - Challenge hash generated: {_challengeHash.Substring(0, Math.Min(10, _challengeHash.Length))}...", LogCategory);

            InitializeComponent();
            Instructions = "Please log in to your EVE Online account in the browser below.";
            Closing += OnWindowClosing;
            InitializeWebView();
            Debug.Info($"EVEManualLogin - Constructor completed", LogCategory);
        }

        private bool VerifyWebView2LoaderExists()
        {
            string appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (string.IsNullOrEmpty(appDirectory))
                return false;

            string x86Path = Path.Combine(appDirectory, @"runtimes\win-x86\native\WebView2Loader.dll");
            string x64Path = Path.Combine(appDirectory, @"runtimes\win-x64\native\WebView2Loader.dll");

            Debug.Info($"Checking for WebView2Loader.dll in runtimes directories:");
            Debug.Info($"  x86: {x86Path} - Exists: {File.Exists(x86Path)}");
            Debug.Info($"  x64: {x64Path} - Exists: {File.Exists(x64Path)}");

            bool hasWebView2Loader = File.Exists(x86Path) || File.Exists(x64Path);

            if (!hasWebView2Loader)
            {
                MessageBox.Show(
                    "WebView2Loader.dll is missing from the runtimes directory.\n\n" +
                    "Please copy the 'runtimes' folder from the downloaded zip into the same folder as the launcher.\n\n" +
                    "Expected location:\n" +
                    "  runtimes\\win-x86\\native\\WebView2Loader.dll\n" +
                    "  runtimes\\win-x64\\native\\WebView2Loader.dll",
                    "WebView2 Missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return hasWebView2Loader;
        }

        private async void InitializeWebView()
        {
            Debug.Info($"InitializeWebView - Starting", LogCategory);
            try
            {
                // Verify WebView2Loader.dll exists in the runtimes directory
                if (!VerifyWebView2LoaderExists())
                {
                    Debug.Error($"InitializeWebView - WebView2Loader.dll not found", LogCategory);
                    CloseWithResult(LoginResult.Error);
                    return;
                }

                // We set this into userdata folder as Innerspace folder is write protected
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ISBoxerEVELauncher",
                    "WebViews",
                    _account.Username);
                Debug.Info($"InitializeWebView - Setting up WebView2 with UserDataFolder: {userDataFolder}", LogCategory);

                WebView2.CreationProperties = new CoreWebView2CreationProperties()
                {
                    ProfileName = _account.Username,
                    UserDataFolder = userDataFolder
                };

                Debug.Info($"InitializeWebView - Ensuring CoreWebView2 is initialized", LogCategory);
                await WebView2.EnsureCoreWebView2Async(null);
                Debug.Info($"InitializeWebView - CoreWebView2 initialized successfully", LogCategory);

                // Load saved cookies if available
                Debug.Info($"InitializeWebView - Loading saved cookies", LogCategory);
                await LoadCookiesAsync();

                // Setup navigation completed handler
                WebView2.NavigationCompleted += WebView2_NavigationCompleted;

                var uri = RequestResponse.FullUri(_sisi, RequestResponse.GetLoginUri(_sisi, _state.ToString() , _challengeHash));
                Debug.Info($"InitializeWebView - Navigating to login URI: {uri}", LogCategory);

                Navigate(uri.ToString());
            }
            catch (Exception ex)
            {
                Debug.Error($"InitializeWebView - Exception: {ex.Message}", LogCategory);
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Navigate(string url)
        {
            Debug.Info($"Navigate - Navigating to: {url}", LogCategory);
            if (WebView2?.CoreWebView2 != null)
            {
                WebView2.CoreWebView2.Navigate(url);
            }
            else
            {
                Debug.Warning($"Navigate - WebView2 or CoreWebView2 is null, cannot navigate", LogCategory);
            }
        }

        private async void WebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Debug.Info($"WebView2_NavigationCompleted - Navigation completed | IsSuccess: {e.IsSuccess} | WebErrorStatus: {e.WebErrorStatus}", LogCategory);
            try
            {
                LoginUrl = WebView2.CoreWebView2.Source;
                Debug.Info($"WebView2_NavigationCompleted - Current URL: {LoginUrl}", LogCategory);

                if (e.IsSuccess)
                {
                    await ProcessNavigationAsync();
                }
                else
                {
                    Debug.Warning($"WebView2_NavigationCompleted - Navigation was not successful: {e.WebErrorStatus}", LogCategory);
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"WebView2_NavigationCompleted - Exception: {ex.Message}", LogCategory);
                MessageBox.Show($"Navigation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ProcessNavigationAsync()
        {
            Debug.Info($"ProcessNavigationAsync - Starting", LogCategory);
            try
            {
                if (WebView2 == null || WebView2.CoreWebView2 == null)
                {
                    Debug.Warning($"ProcessNavigationAsync - WebView2 or CoreWebView2 is null, skipping", LogCategory);
                    return;
                }

                var url = WebView2.CoreWebView2.Source;
                Debug.Info($"ProcessNavigationAsync - Current URL: {url}", LogCategory);
                var html = await WebView2.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML;");

                Debug.Info($"ProcessNavigationAsync - HTML length: {html?.Length ?? 0}", LogCategory);
                Debug.Info($"ProcessNavigationAsync - HTML contains loginForm: {html?.Contains("loginForm") ?? false}", LogCategory);
                Debug.Info($"ProcessNavigationAsync - Full HTML:{Environment.NewLine}{html}", LogCategory);

                if (html.Contains("loginForm") && App.Settings.ManualLoginAutofill)
                {
                    Debug.Info($"ProcessNavigationAsync - Login form detected, autofill enabled", LogCategory);
                    FillAndCheckState(LoginState.LoginForm);
                    // Fill in username and password
                    // Sadly we cannot get around the SecureString to string conversion here as we have to
                    // inject it into JavaScript
                    var password = GetEscapedPasswordString();
                    Debug.Info($"ProcessNavigationAsync - Filling in username: {_account.Username} and password (hidden)", LogCategory);
                    string script = $@"
                        document.getElementById('UserName').value = '{_account.Username}';
                        document.getElementById('Password').value = '{password}';
                        document.forms['loginForm'].submit();
                    ";
                    Debug.Info($"ProcessNavigationAsync - Submitting login form", LogCategory);
                    await WebView2.CoreWebView2.ExecuteScriptAsync(script);
                    return;
                }

                var codeUri = RequestResponse.FullUri(_sisi, new Uri("/launcher", UriKind.Relative));
                Debug.Info($"ProcessNavigationAsync - Checking if URL matches launcher URI: {codeUri}", LogCategory);
                if (url.StartsWith(codeUri.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Info($"ProcessNavigationAsync - URL matches launcher URI, extracting auth code", LogCategory);

                    var authCode = HttpUtility.ParseQueryString(url).Get("code");
                    Debug.Info($"ProcessNavigationAsync - Auth code extracted: {(string.IsNullOrEmpty(authCode) ? "null/empty" : $"length {authCode.Length}")}", LogCategory);
                    if (string.IsNullOrEmpty(authCode))
                    {
                        Debug.Error($"ProcessNavigationAsync - No auth code found in URL", LogCategory);
                        MessageBox.Show("Launcher page did not retun a valid code.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        CloseWithResult(LoginResult.Error);
                        return;
                    }

                    Debug.Info($"ProcessNavigationAsync - Exchanging auth code for token", LogCategory);
                    var tokenUri = RequestResponse.FullUri(_sisi, RequestResponse.GetTokenUri(_sisi));

                    var req2 = RequestResponse.CreatePostRequest(new Uri(RequestResponse.token, UriKind.Relative), _sisi, true, RequestResponse.refererUri, null);
                    req2.SetBody(RequestResponse.GetSsoTokenRequestBody(_sisi, authCode, _challengeCode));
                    Debug.Info($"ProcessNavigationAsync - Sending token request to: {req2.RequestUri}", LogCategory);
                    var respone = new Response(req2);
                    Debug.Info($"ProcessNavigationAsync - Token response received, body length: {respone.Body?.Length ?? 0}", LogCategory);

                    var accessToken = new EVEAccount.Token(JsonConvert.DeserializeObject<EVEAccount.authObj>(respone.Body));
                    Debug.Info($"ProcessNavigationAsync - Token parsed | Has refresh token: {!string.IsNullOrEmpty(accessToken.RefreshToken)} | Expiration: {accessToken.Expiration}", LogCategory);
                    if (string.IsNullOrEmpty(accessToken.RefreshToken))
                    {
                        Debug.Error($"ProcessNavigationAsync - No refresh token in response", LogCategory);
                        MessageBox.Show("Failed to retrieve access token.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        CloseWithResult(LoginResult.Error);
                        return;
                    }

                    Debug.Info($"ProcessNavigationAsync - Storing token for {(_sisi ? "Sisi" : "Tranquility")}", LogCategory);
                    if (_sisi)
                    {
                        _account.SisiToken = accessToken;
                    }
                    else
                    {
                        _account.TranquilityToken = accessToken;
                    }

                    AccessToken = accessToken;
                    Debug.Info($"ProcessNavigationAsync - Login successful", LogCategory);
                    CloseWithResult(LoginResult.Success);
                    return;
                }

                Debug.Info($"ProcessNavigationAsync - URL did not match known patterns, waiting for user interaction", LogCategory);
            }
            catch (Exception ex)
            {
                Debug.Error($"ProcessNavigationAsync - Exception: {ex.Message}", ex, LogCategory);
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
            Debug.Info($"FillAndCheckState - Checking state: {state} | Already visited: {_visitedStates.Contains(state)}", LogCategory);
            if (_visitedStates.Contains(state))
            {
                Debug.Error($"FillAndCheckState - Loop detected! State {state} has already been visited", LogCategory);
                MessageBox.Show("Looping detected during login process.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                CloseWithResult(LoginResult.Error);
                return;
            }
            _visitedStates.Add(state);
            Debug.Info($"FillAndCheckState - Added state {state} to visited states", LogCategory);
        }

        private void CloseWithResult(LoginResult result)
        {
            Debug.Info($"CloseWithResult - Closing with result: {result}", LogCategory);
            LoginResult = result;
            WebView2.NavigationCompleted -= WebView2_NavigationCompleted;
            this.Close();
        }

        private async Task LoadCookiesAsync()
        {
            Debug.Info($"LoadCookiesAsync - Starting | Has stored cookies: {!string.IsNullOrEmpty(_account.WebView2CookieStorage)}", LogCategory);
            if (string.IsNullOrEmpty(_account.WebView2CookieStorage))
            {
                Debug.Info($"LoadCookiesAsync - No cookies to load", LogCategory);
                return;
            }

            try
            {
                // Decode from base64
                byte[] data = Convert.FromBase64String(_account.WebView2CookieStorage);
                string json = System.Text.Encoding.UTF8.GetString(data);

                var cookies = JsonConvert.DeserializeObject<List<CookieData>>(json);
                if (cookies == null)
                {
                    Debug.Warning($"LoadCookiesAsync - Cookie deserialization returned null", LogCategory);
                    return;
                }

                Debug.Info($"LoadCookiesAsync - Loaded {cookies.Count} cookies from storage", LogCategory);
                var cookieManager = WebView2.CoreWebView2.CookieManager;
                foreach (var cookieData in cookies)
                {
                    var cookie = cookieManager.CreateCookie(cookieData.Name, cookieData.Value, cookieData.Domain, cookieData.Path);
                    cookie.IsHttpOnly = cookieData.IsHttpOnly;
                    cookie.IsSecure = cookieData.IsSecure;
                    cookie.Expires = cookieData.Expires;

                    cookieManager.AddOrUpdateCookie(cookie);
                }
                Debug.Info($"LoadCookiesAsync - All cookies loaded successfully", LogCategory);
            }
            catch (Exception ex)
            {
                Debug.Error($"LoadCookiesAsync - Exception: {ex.Message}", LogCategory);
                MessageBox.Show($"Failed to load cookies: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task SaveCookiesAsync()
        {
            Debug.Info($"SaveCookiesAsync - Starting", LogCategory);
            try
            {
                var cookieManager = WebView2?.CoreWebView2?.CookieManager;
                if (cookieManager == null)
                {
                    Debug.Warning($"SaveCookiesAsync - CookieManager is null, cannot save cookies", LogCategory);
                    return;
                }
                var cookies = await cookieManager.GetCookiesAsync(null);
                if (cookies == null || cookies.Count == 0)
                {
                    Debug.Info($"SaveCookiesAsync - No cookies to save", LogCategory);
                    return;
                }

                Debug.Info($"SaveCookiesAsync - Saving {cookies.Count} cookies", LogCategory);
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
                Debug.Info($"SaveCookiesAsync - Cookies saved successfully", LogCategory);
            }
            catch (Exception ex)
            {
                Debug.Error($"SaveCookiesAsync - Exception: {ex.Message}", LogCategory);
                MessageBox.Show($"Failed to save cookies: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Debug.Info($"OnWindowClosing - Window closing, saving cookies", LogCategory);
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