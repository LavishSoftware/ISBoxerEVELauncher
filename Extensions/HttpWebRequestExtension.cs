using ISBoxerEVELauncher.Security;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace ISBoxerEVELauncher.Extensions
{
    public static class HttpWebRequestExtension
    {
        public static void SetBody(this HttpWebRequest webRequest, string bodyText)
        {
            webRequest.SetBody(Encoding.UTF8.GetBytes(bodyText));
        }


        public static void SetBody(this HttpWebRequest webRequest, byte[] body)
        {

            webRequest.ContentLength = body.Length;
            App.requestBody = body;
            try
            {
                if (!App.tofCaptcha)
                {
                    using (Stream reqStream = webRequest.GetRequestStream())
                    {
                        reqStream.Write(body, 0, body.Length);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static void SetBody(this HttpWebRequest webRequest, SecureBytesWrapper body)
        {

            webRequest.ContentLength = body.Bytes.Length;

            try
            {
                using (Stream reqStream = webRequest.GetRequestStream())
                {
                    reqStream.Write(body.Bytes, 0, body.Bytes.Length);
                }

            }
            catch (Exception)
            {
            }
        }

        public static void SetCustomheaders(this HttpWebRequest webRequest, WebHeaderCollection webHeaderCollection)
        {
            var field = typeof(HttpWebRequest).GetField("_HttpRequestHeaders", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(webRequest, webHeaderCollection);
        }
    }

}

