using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
