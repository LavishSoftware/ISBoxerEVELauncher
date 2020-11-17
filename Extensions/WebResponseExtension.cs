using System.IO;
using System.Net;

namespace ISBoxerEVELauncher.Extensions
{
    public static class WebResponseExtension
    {

        public static string GetResponseBody(this WebResponse response)
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
    }
}
