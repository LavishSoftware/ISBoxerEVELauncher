using System;
using System.Text;
using System.Web;

namespace ISBoxerEVELauncher.Extensions
{
    public static class UriExtension

    {
        public static Uri AddQuery(this Uri uri, string name, string value)
        {
            var httpValueCollection = HttpUtility.ParseQueryString(uri.SafeQuery());

            httpValueCollection.Remove(name);
            httpValueCollection.Add(name, value);

            var ub = new UriBuilder(uri.IsAbsoluteUri ? uri.ToString() : "https://localhost" + uri.ToString());

            // this code block is taken from httpValueCollection.ToString() method
            // and modified so it encodes strings with HttpUtility.UrlEncode
            if (httpValueCollection.Count == 0)
                ub.Query = String.Empty;
            else
            {
                var sb = new StringBuilder();

                for (int i = 0; i < httpValueCollection.Count; i++)
                {
                    string text = httpValueCollection.GetKey(i);
                    {
                        text = HttpUtility.UrlEncode(text);

                        string val = (text != null) ? (text + "=") : string.Empty;
                        string[] vals = httpValueCollection.GetValues(i);

                        if (sb.Length > 0)
                            sb.Append('&');

                        if (vals == null || vals.Length == 0)
                            sb.Append(val);
                        else
                        {
                            if (vals.Length == 1)
                            {
                                sb.Append(val);

                                //You can encode a URL using with the UrlEncode method or the UrlPathEncode method. However, the methods return different results. 
                                //The UrlEncode method converts each space character to a plus character (+). 
                                //The UrlPathEncode method converts each space character into the string "%20", which represents a space in hexadecimal notation. 
                                //Use the UrlPathEncode method when you encode the path portion of a URL in order to guarantee a consistent decoded URL, regardless of which platform or browser performs the decoding.
                                //The problem with UrlPathEncode though is it wont double encode, so we need to use UrlEncode, and if we want spaces to be %20, we need to handle them ourselves

                                if (vals[0].Contains(" "))
                                {
                                    vals = vals[0].Split(' ');
                                }

                                for (int j = 0; j < vals.Length; j++)
                                {
                                    if (j > 0)
                                        sb.Append("%20");
                                    sb.Append(HttpUtility.UrlEncode(vals[j]));
                                }
                            }
                            else
                            {
                                for (int j = 0; j < vals.Length; j++)
                                {
                                    if (j > 0)
                                        sb.Append('&');

                                    sb.Append(val);
                                    sb.Append(HttpUtility.UrlEncode(vals[j]));
                                }
                            }
                        }
                    }
                }

                ub.Query = sb.ToString();
            }
            if (uri.IsAbsoluteUri)
                return ub.Uri;
            return new Uri(ub.Uri.PathAndQuery, UriKind.Relative);
        }

        public static string SafeQuery(this Uri uri)
        {
            if (uri.IsAbsoluteUri)
                return uri.Query;
            if (uri.ToString().IndexOf('?') > 0)
                return uri.ToString().Substring(uri.ToString().IndexOf('?')+1);
            return string.Empty;
        }
    }
}