using ISBoxerEVELauncher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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

            try
            {
                using (Stream reqStream = webRequest.GetRequestStream())
                {
                    reqStream.Write(body, 0, body.Length);
                }

            }
            catch (Exception e)
            { }
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
            catch (Exception e)
            { }
        }
    }

}

