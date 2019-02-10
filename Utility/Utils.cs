using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ISBoxerEVELauncher.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ISBoxerEVELauncher.Utility
{
    public static class Utils
    {

        //public const string logoff = "/account/logoff";
        private const string auth = "/v2/oauth/authorize";
        private const string logon = "/account/logon";
        private const string launcher = "launcher";
        public const string token = "/v2/oauth/token";
        private const string tqBaseUri = "https://login.eveonline.com";
        private const string sisiBaseUri = "https://sisilogin.testeveonline.com";
        private const string verifyTwoFactor = "/account/verifytwofactor";

        //public const string logonRetURI = "ReturnUrl=/v2/oauth/authorize?client_id=eveLauncherTQ&response_type=code&scope=eveClientLogin%20cisservice.customerRead.v1%20cisservice.customerWrite.v1";
        //public const string logonRedirectURI = "redirect_uri={0}/launcher?client_id=eveLauncherTQ&state={1}&code_challenge_method=S256&code_challenge={2}&ignoreClientStyle=true&showRemember=true";

        public const string originUri = "https://launcher.eveonline.com";
        public const string refererUri = "https://launcher.eveonline.com/6-0-x/6.0.22/";


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
                        .AddQuery("ignoreClientStyle", "true")
                        .AddQuery("showRemember", "true").ToString());
        }


        public static HttpWebRequest CreateGetRequest(Uri uri, bool sisi, bool origin, string referer, CookieContainer cookies)
        {
            if (!uri.IsAbsoluteUri)
                uri = new Uri(string.Concat(sisi ? sisiBaseUri : tqBaseUri, uri.ToString()));
            return CreateHttpWebRequest(uri, "GET", sisi, origin, referer, cookies);
        }
        public static HttpWebRequest CreatePostRequest(Uri uri, bool sisi, bool origin, string referer, CookieContainer cookies)
        {
            if (!uri.IsAbsoluteUri)
                uri = new Uri(string.Concat(sisi ? sisiBaseUri : tqBaseUri, uri.ToString()));
            return CreateHttpWebRequest(uri, "POST", sisi, origin, referer, cookies);
        }


        public static byte[] GetSsoTokenRequestBody(bool sisi, string authCode, byte[] challengeCode)
        {

            return 
                Encoding.UTF8.GetBytes(new Uri("/", UriKind.Relative)
                .AddQuery("grant_type","authorization_code")
                .AddQuery("client_id", "eveLauncherTQ")
                .AddQuery("redirect_uri", new Uri(new Uri(sisi ? Utils.sisiBaseUri : Utils.tqBaseUri), launcher)
                    .AddQuery("client_id", "eveLauncherTQ").ToString())
                .AddQuery("code", authCode)
                .AddQuery("code_verifier", Base64UrlEncoder.Encode(challengeCode)).SafeQuery());

            //byte[] body =                Encoding.UTF8.GetBytes("grant_type=authorization_code&client_id=eveLauncherTQ&redirect_uri=" + HttpUtility.UrlEncode(sisi ? Utils.sisiBaseUri : Utils.tqBaseUri) + "%2Flauncher%3Fclient_id%3DeveLauncherTQ&code=" + authCode + "&code_verifier=" +
            //Base64UrlEncoder.Encode(challengeCode));
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
                        .AddQuery("ignoreClientStyle", "true")
                        .AddQuery("showRemember", "true").ToString());

            //byte[] body =                Encoding.UTF8.GetBytes("grant_type=authorization_code&client_id=eveLauncherTQ&redirect_uri=" + HttpUtility.UrlEncode(sisi ? Utils.sisiBaseUri : Utils.tqBaseUri) + "%2Flauncher%3Fclient_id%3DeveLauncherTQ&code=" + authCode + "&code_verifier=" +
            //Base64UrlEncoder.Encode(challengeCode));

            //"POST /account/verifytwofactor?ReturnUrl=%2Fv2%2Foauth%2Fauthorize%3Fclient_id%3DeveLauncherTQ%26response_type%3Dcode%26scope%3DeveClientLogin%2520cisservice.customerRead.v1%2520cisservice.customerWrite.v1%26redirect_uri%3Dhttps%253A%252F%252Fsisilogin.testeveonline.com%252Flauncher%253Fclient_id%253DeveLauncherTQ%26state%3D1043d900-ab13-42f3-a741-285cce0c8b47%26code_challenge_method%3DS256%26code_challenge%3DC0emnYPGUFfgXiyQx9d47zMM3uUXb6H9JB-PLptvtZ4%26ignoreClientStyle%3Dtrue%26showRemember%3Dtrue HTTP/1.1"
        }





        private static HttpWebRequest CreateHttpWebRequest(Uri uri, string methodType, bool sisi, bool origin, string referer, CookieContainer cookies)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Method = methodType;
            req.Timeout = 30000;
            req.AllowAutoRedirect = true;
            if (origin)
            {
                if (referer == Utils.refererUri)
                {
                    req.Headers.Add("Origin", Utils.originUri);
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
                req.Referer = referer;
            }
            if (cookies != null)
            {
                req.CookieContainer = cookies;
            }

            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = 0;
            return req;
        }

        public static LoginResult GetHttpWebResponse(HttpWebRequest webRequest, Action updateCookies, out string responseBody)
        {
            HttpWebResponse resp = null;
            string body = null;
            try
            {
                resp = (HttpWebResponse)webRequest.GetResponse();
                {
                    body = GetResponseBody(resp);
                    if (updateCookies != null)
                    {
                        updateCookies();
                    }
                }
            }
            catch (System.Net.WebException ex)
            {
                switch (ex.Status)
                {
                    case WebExceptionStatus.Timeout:
                        {
                            responseBody = body;
                            return LoginResult.Timeout;
                        }
                    case WebExceptionStatus.ProtocolError:
                        {
                            if (resp != null)
                            {
                                body = GetResponseBody(resp);
                            }
                            responseBody = body;
                            return LoginResult.Error;
                        }
                    default:
                        throw;
                }
            }
            finally
            {
                resp.Dispose();
            }

            responseBody = body;
            return LoginResult.Success;
        }



        public static string GetResponseBody(HttpWebResponse response)
        {
            string body;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    body = sr.ReadToEnd();
                }
            }
            return body;
        }

        public static string GetResponseBody(WebResponse response)
        {
            string body;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    body = sr.ReadToEnd();
                }
            }
            return body;
        }


        public static string GetRequestVerificationTokenFromBody(string body)
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
