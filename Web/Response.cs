using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ISBoxerEVELauncher.Extensions;

namespace ISBoxerEVELauncher.Web
{
    public class Response
    {
        Uri _requestUri = null;
        string _origin;
        string _referer;
        HttpStatusCode _response = HttpStatusCode.Unused;
        string _responseLocation;
        string _responseBody;
        Uri _responseUri = null;

        private string _body;

        public Response(HttpWebRequest request)
        {
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                _requestUri = request.RequestUri;
                _origin = request.Headers["Origin"];
                _referer = request.Referer;
                _response = response.StatusCode;
                _responseLocation = response.Headers["Location"];
                _responseBody = response.GetResponseBody();
                _responseUri = response.ResponseUri;
            }
        }

        public string Body
        {
            get { return _responseBody; }
        }

        public Uri ResponseUri
        {
            get { return _responseUri; }
        }

        public bool IsHtml()
        {
            try
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(_body);
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public bool IsJson()
        {
            try
            {
                dynamic json = JsonConvert.DeserializeObject(_body);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return
                "RequestUri: " + _requestUri.ToString() + Environment.NewLine
                + "Origin: " + _origin + Environment.NewLine
                + "Referer: " + _referer + Environment.NewLine + Environment.NewLine
                + "ResponseCode: " + _response.ToString() + Environment.NewLine
                + "ResponseUri: " + _responseUri + Environment.NewLine
                + "Location: " + _responseLocation + Environment.NewLine + Environment.NewLine
                + "Body: " + Environment.NewLine + _responseBody + Environment.NewLine;



        }
    }
}
