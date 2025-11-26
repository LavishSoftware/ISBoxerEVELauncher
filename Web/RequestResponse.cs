using ISBoxerEVELauncher.Enums;
using ISBoxerEVELauncher.Extensions;
using ISBoxerEVELauncher.Utils;
using ISBoxerEVELauncher.Windows;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32;
using System;
using System.Net;
using System.Text;
using System.Windows;

namespace ISBoxerEVELauncher.Web
{
    public static class RequestResponse
    {
        private const string LogCategory = "RequestResponse";

        //public const string logoff = "/account/logoff";
        private const string auth = "/v2/oauth/authorize";
        private const string eula = "/v2/oauth/eula";
        private const string logon = "/account/logon";
        private const string launcher = "launcher";
        public const string token = "/v2/oauth/token";
        private const string tqBaseUri = "https://login.eveonline.com";
        private const string sisiBaseUri = "https://sisilogin.testeveonline.com";
        private const string verifyTwoFactor = "/account/verifytwofactor";

        //public const string logonRetURI = "ReturnUrl=/v2/oauth/authorize?client_id=eveLauncherTQ&response_type=code&scope=eveClientLogin%20cisservice.customerRead.v1%20cisservice.customerWrite.v1";
        //public const string logonRedirectURI = "redirect_uri={0}/launcher?client_id=eveLauncherTQ&state={1}&code_challenge_method=S256&code_challenge={2}&ignoreClientStyle=true&showRemember=true";

        public const string originUri = "https://launcher.eveonline.com";
        public const string refererUri = "https://launcher.eveonline.com/6-0-x/6.4.15/";

        public static Uri FullUri(bool sisi, Uri uri)
        {
            if (!uri.IsAbsoluteUri)
                uri = new Uri(string.Concat(sisi ? sisiBaseUri : tqBaseUri, uri.ToString()));
            return uri;
        }
        
        public static Uri GetTokenUri(bool sisi)
        {
            return new Uri(token, UriKind.Relative);
        }

        public static Uri GetLoginUri(bool sisi, string state, string challengeHash)
        {

            return new Uri(logon, UriKind.Relative)
                .AddQuery("ReturnUrl",
                    new Uri(auth, UriKind.Relative)
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(new Uri(sisi ? sisiBaseUri : tqBaseUri), launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("showRemember", "true").ToString());
        }

        public static Uri GetSecurityWarningChallenge(bool sisi, string state, string challengeHash)
        {

            //https://login.eveonline.com/v2/oauth/authorize?
            //client_id =eveLauncherTQ
            //&amp;response_type=code
            //&amp;scope=eveClientLogin%20cisservice.customerRead.v1%20cisservice.customerWrite.v1
            //&amp;redirect_uri=https%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ
            //&amp;state=5617f90c-efdb-41a1-b00d-6f4f24bbeee4
            //&amp;code_challenge_method=S256
            //&amp;code_challenge=nC-B19HKX8ZZYfOEN_bg-YZSjVAMieqEB3nJXFyfQQc
            //&amp;showRemember=true

            return new Uri(auth, UriKind.Relative)
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(new Uri(sisi ? sisiBaseUri : tqBaseUri), launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("showRemember", "true");
        }


        public static HttpWebRequest CreateGetRequest(Uri uri, bool sisi, bool origin, string referer, CookieContainer cookies)
        {
            Debug.Info($"CreateGetRequest - Starting | URI: {uri} | Sisi: {sisi} | Origin: {origin} | Referer: {referer}", LogCategory);
            //.Replace("https:", "http:")
            if (!uri.IsAbsoluteUri)
            {
                var baseUri = sisi ? sisiBaseUri : tqBaseUri;
                uri = new Uri(string.Concat(baseUri, uri.ToString()));
                Debug.Info($"CreateGetRequest - Converted to absolute URI: {uri}", LogCategory);
            }
            var request = CreateHttpWebRequest(uri, "GET", sisi, origin, referer, cookies);
            Debug.Info($"CreateGetRequest - Created request for: {request.RequestUri}", LogCategory);
            return request;
        }
        public static HttpWebRequest CreatePostRequest(Uri uri, bool sisi, bool origin, string referer, CookieContainer cookies)
        {
            Debug.Info($"CreatePostRequest - Starting | URI: {uri} | Sisi: {sisi} | Origin: {origin} | Referer: {referer}", LogCategory);
            if (!uri.IsAbsoluteUri)
            {
                var baseUri = sisi ? sisiBaseUri : tqBaseUri;
                uri = new Uri(string.Concat(baseUri, uri.ToString()));
                Debug.Info($"CreatePostRequest - Converted to absolute URI: {uri}", LogCategory);
            }
            var request = CreateHttpWebRequest(uri, "POST", sisi, origin, referer, cookies);
            Debug.Info($"CreatePostRequest - Created request for: {request.RequestUri}", LogCategory);
            return request;
        }


        public static byte[] GetSsoTokenRequestBody(bool sisi, string authCode, byte[] challengeCode)
        {
            return
                Encoding.UTF8.GetBytes(new Uri("/", UriKind.Relative)
                .AddQuery("grant_type", "authorization_code")
                .AddQuery("client_id", "eveLauncherTQ")
                .AddQuery("redirect_uri", new Uri(new Uri(sisi ? RequestResponse.sisiBaseUri : RequestResponse.tqBaseUri), launcher)
                    .AddQuery("client_id", "eveLauncherTQ").ToString())
                .AddQuery("code", authCode)
                .AddQuery("code_verifier", Base64UrlEncoder.Encode(challengeCode)).SafeQuery());
        }

        public static Uri GetVerifyTwoFactorUri(bool sisi, string state, string challengeHash)
        {
            return new Uri(verifyTwoFactor, UriKind.Relative)
                .AddQuery("ReturnUrl",
                    new Uri(auth, UriKind.Relative)
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(new Uri(sisi ? sisiBaseUri : tqBaseUri), launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("showRemember", "true").ToString());

        }

        public static Uri GetAuthenticatorUri(bool sisi, string state, string challengeHash)
        {
            return new Uri("/account/authenticator", UriKind.Relative)
                .AddQuery("ReturnUrl",
                    new Uri(auth, UriKind.Relative)
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(new Uri(sisi ? sisiBaseUri : tqBaseUri), launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("showRemember", "true").ToString());

        }

        public static Uri GetEulaUri(bool sisi, string state, string challengeHash)
        {
            return new Uri("/account/eula", UriKind.Relative)
                .AddQuery("ReturnUrl",
                    new Uri(auth, UriKind.Relative)
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(new Uri(sisi ? sisiBaseUri : tqBaseUri), launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("showRemember", "true").ToString());

        }


        public static Uri GetCharacterChallengeUri(bool sisi, string state, string challengeHash)
        {
            return new Uri("/account/character", UriKind.Relative)
                .AddQuery("ReturnUrl",
                    new Uri(auth, UriKind.Relative)
                        .AddQuery("client_id", "eveLauncherTQ")
                        .AddQuery("response_type", "code")
                        .AddQuery("scope", "eveClientLogin cisservice.customerRead.v1 cisservice.customerWrite.v1")
                        .AddQuery("redirect_uri", new Uri(new Uri(sisi ? sisiBaseUri : tqBaseUri), launcher)
                            .AddQuery("client_id", "eveLauncherTQ").ToString())
                        .AddQuery("state", state)
                        .AddQuery("code_challenge_method", "S256")
                        .AddQuery("code_challenge", challengeHash)
                        .AddQuery("showRemember", "true").ToString());

        }



        private static HttpWebRequest CreateHttpWebRequest(Uri uri, string methodType, bool sisi, bool origin, string referer, CookieContainer cookies)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Method = methodType;
            req.Timeout = 30000;
            req.AllowAutoRedirect = true;

            var t = req.Headers.GetType();

            var customHeaders = new CustomWebHeaderCollection(new System.Collections.Generic.Dictionary<string, string>()
            {
                ["User-Agent"] = @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) QtWebEngine/5.12.3 Chrome/69.0.3497.128 Safari/537.36",
            });
            req.SetCustomheaders(customHeaders);

            if (origin)
            {
                if (referer == RequestResponse.refererUri)
                {
                    req.Headers.Add("Origin", RequestResponse.originUri);
                }
                else
                {
                    if (!sisi)
                    {
                        req.Headers.Add("Origin", tqBaseUri);
                    }
                    else
                    {
                        req.Headers.Add("Origin", sisiBaseUri);
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(referer))
            {
                if (referer == "URL")
                {
                    req.Referer = req.RequestUri.ToString();
                }
                else
                {
                    req.Referer = referer;
                }
            }
            if (cookies != null)
            {
                req.CookieContainer = cookies;
            }

            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = 0;
            return req;
        }

        public static LoginResult GetHttpWebResponse(HttpWebRequest webRequest, Action updateCookies, out Response response)
        {
            Debug.Info($"GetHttpWebResponse - Starting | URI: {webRequest.RequestUri} | Method: {webRequest.Method}", LogCategory);
            response = null;

            try
            {
                Debug.Info($"GetHttpWebResponse - tofCaptcha flag: {App.tofCaptcha}", LogCategory);
                if (!App.tofCaptcha)
                {
                    Debug.Info($"GetHttpWebResponse - Creating response from direct HTTP request", LogCategory);
                    response = new Response(webRequest);
                    Debug.Info($"GetHttpWebResponse - Direct response created successfully", LogCategory);
                }
            }
            catch (Exception e)
            {
                Debug.Warning($"GetHttpWebResponse - Exception during direct request, enabling captcha flow: {e.Message}", LogCategory);
                App.tofCaptcha = true;
            }

            try
            {
                if (App.tofCaptcha)
                {
                    Debug.Info($"GetHttpWebResponse - Captcha flow enabled, opening browser window", LogCategory);
                    App.myLB = new EVELoginBrowser();
                    App.myLB.Clearup();

                    App.myLB.Text = "EVE - " + App.strUserName;

                    if (webRequest.Method == "GET")
                    {
                        Debug.Info($"GetHttpWebResponse - Browser navigating to (GET): {webRequest.Address}", LogCategory);
                        App.myLB.webBrowser_EVE.Navigate(webRequest.Address.ToString());
                    }
                    else
                    {
                        Debug.Info($"GetHttpWebResponse - Browser navigating to (POST): {webRequest.Address}", LogCategory);
                        SetRegistery();
                        App.myLB.webBrowser_EVE.Navigate(webRequest.Address, string.Empty, App.requestBody, webRequest.Headers.ToString());

                    }

                    Debug.Info($"GetHttpWebResponse - Showing browser dialog", LogCategory);
                    App.myLB.ShowDialog();
                    Debug.Info($"GetHttpWebResponse - Browser dialog closed", LogCategory);
                }

                if (App.tofCaptcha)
                {
                    Debug.Info($"GetHttpWebResponse - Processing captcha flow result | HTML_Result empty: {string.IsNullOrEmpty(App.myLB.strHTML_Result)}", LogCategory);
                    if (App.myLB.strHTML_Result == "")
                    {
                        Debug.Error($"GetHttpWebResponse - Captcha flow returned empty result", LogCategory);
                        return LoginResult.Error;
                    }
                    if (webRequest.Method == "GET")
                    {
                        Debug.Info($"GetHttpWebResponse - Creating response from captcha flow (RequestVerificationToken)", LogCategory);
                        response = new Response(webRequest, WebRequestType.RequestVerificationToken);
                    }
                    else
                    {
                        Debug.Info($"GetHttpWebResponse - Creating response from captcha flow (Result)", LogCategory);
                        response = new Response(webRequest, WebRequestType.Result);
                    }
                }

                if (updateCookies != null)
                {
                    Debug.Info($"GetHttpWebResponse - Updating cookies", LogCategory);
                    updateCookies();
                }
            }
            catch (System.Net.WebException ex)
            {
                Debug.Error($"GetHttpWebResponse - WebException: Status={ex.Status}, Message={ex.Message}", LogCategory);
                switch (ex.Status)
                {
                    case WebExceptionStatus.Timeout:
                        {
                            Debug.Error($"GetHttpWebResponse - Request timed out", LogCategory);
                            return LoginResult.Timeout;
                        }
                    case WebExceptionStatus.ProtocolError:
                        {
                            Debug.Error($"GetHttpWebResponse - Protocol error", LogCategory);
                            return LoginResult.Error;
                        }
                    default:
                        throw;
                        break;
                }
            }

            Debug.Info($"GetHttpWebResponse - Success | Response URI: {response?.ResponseUri}", LogCategory);
            return LoginResult.Success;
        }

        private static bool SetRegistery()
        {
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64))
                {
                    if (hklm.OpenSubKey(@"MIME\Database\Content Type\application/json", true) == null)
                    {
                        hklm.CreateSubKey(@"MIME\Database\Content Type\application/json");
                    }
                    using (RegistryKey key = hklm.OpenSubKey(@"MIME\Database\Content Type\application/json", true))
                    {
                        if (key != null)
                        {
                            key.SetValue("CLSID", "{25336920-03F9-11cf-8FD0-00AA00686F13}");
                            key.SetValue("Encoding", new byte[] { 0x80, 0x00, 0x00, 0x00 });
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        public static string GetRequestVerificationTokenResponse(Response response)
        {
            // <input name="__RequestVerificationToken" type="hidden" value="rGFOR5OvmlpJ_6_Kabcx3JSrJ3v6EL0W6tuOuD-e8QvUuK2l1MX5jP7pztjxnm5k0qgHIv-mati2ctst9M8kD9jBg3E1" />
            const string needle = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
            int hashStart = response.Body.IndexOf(needle, StringComparison.Ordinal);
            if (hashStart == -1)
                return null;

            hashStart += needle.Length;

            // get hash end
            int hashEnd = response.Body.IndexOf('"', hashStart);
            if (hashEnd == -1)
                return null;

            return response.Body.Substring(hashStart, hashEnd - hashStart);
        }

        public static string GetEulaHashFromBody(string body)
        {
            const string needle = "name=\"eulaHash\" type=\"hidden\" value=\"";
            int hashStart = body.IndexOf(needle, StringComparison.Ordinal);
            if (hashStart == -1)
                return null;
            return body.Substring(hashStart + needle.Length, 32);
        }
        public static string GetEulaReturnUrlFromBody(string body)
        {
            const string needle = "input id=\"returnUrl\" name=\"returnUrl\" type=\"hidden\" value=\"";
            int fieldStart = body.IndexOf(needle, StringComparison.Ordinal);
            if (fieldStart == -1)
                return null;

            fieldStart += needle.Length;
            int fieldEnd = body.IndexOf('"', fieldStart);


            return body.Substring(fieldStart, fieldEnd - fieldStart);
        }


    }


}
