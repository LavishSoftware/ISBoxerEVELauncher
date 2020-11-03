using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ISBoxerEVELauncher.Enums;
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

        public Response(HttpWebRequest request, string responseBody)
        {
            _requestUri = request.RequestUri;
            _origin = request.Headers["Origin"];
            _referer = request.Referer;
            _response = HttpStatusCode.OK;
            _responseLocation = null;
            _responseBody = responseBody;
            _responseUri = _requestUri;
        }

        public Response(HttpWebRequest request, WebRequestType requestType)
        {
            _requestUri = request.RequestUri;
            _origin = request.Headers["Origin"];
            _referer = request.Referer;
            _response = HttpStatusCode.OK;
            _responseLocation = null;

            switch (requestType)
            {
                case WebRequestType.RequestVerificationToken:
                    _responseBody = App.myLB.strHTML_RequestVerificationToken;
                    _responseUri = new Uri(App.myLB.strURL_RequestVerificationToken, UriKind.Absolute);
                    break;
                case WebRequestType.VerficationCode:
                    _responseBody = App.myLB.strHTML_VerficationCode;
                    _responseUri = new Uri(App.myLB.strURL_VerficationCode, UriKind.Absolute);
                    break;
                case WebRequestType.Result:
                    _responseBody = App.myLB.strHTML_Result;
                    _responseUri = new Uri(App.myLB.strURL_Result, UriKind.Absolute);
                    break;
            }

        }

        public string Body
        {
            get { return _responseBody; }
            set { value = _responseBody; }
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
                doc.LoadHtml(_responseBody);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public bool IsJson()
        {
            try
            {
                dynamic json = JsonConvert.DeserializeObject(_responseBody);
            }
            catch (Exception)
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
