using System.IO;
using System.Net;

namespace ISBoxerEVELauncher.Extensions
{
    public static class HttpWebResponseExtension
    {
        public static string GetResponseBody(this HttpWebResponse response)
        {
            string body;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    body = sr.ReadToEnd();
                }
            }
            return body.Trim();
        }

    }
}
