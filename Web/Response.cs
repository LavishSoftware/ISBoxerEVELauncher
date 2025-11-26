using HtmlAgilityPack;
using ISBoxerEVELauncher.Enums;
using ISBoxerEVELauncher.Extensions;
using ISBoxerEVELauncher.Utils;
using Newtonsoft.Json;
using System;
using System.Net;

namespace ISBoxerEVELauncher.Web
{
    public class Response
    {
        private const string LogCategory = "Response";

        Uri _requestUri = null;
        string _origin;
        string _referer;
        HttpStatusCode _response = HttpStatusCode.Unused;
        string _responseLocation;
        string _responseBody;
        Uri _responseUri = null;

        public Response(HttpWebRequest request)
        {
            Debug.Info($"Response(HttpWebRequest) - Creating response from request to: {request.RequestUri}", LogCategory);
            try
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
                    Debug.Info($"Response(HttpWebRequest) - Status: {_response} | ResponseUri: {_responseUri} | Body length: {_responseBody?.Length ?? 0}", LogCategory);
                }
            }
            catch (WebException ex)
            {
                Debug.Error($"Response(HttpWebRequest) - WebException: {ex.Status} - {ex.Message}", LogCategory);
                throw;
            }
            catch (Exception ex)
            {
                Debug.Error($"Response(HttpWebRequest) - Exception: {ex.Message}", LogCategory);
                throw;
            }
        }

        public Response(HttpWebRequest request, string responseBody)
        {
            Debug.Info($"Response(HttpWebRequest, string) - Creating response with provided body for: {request.RequestUri}", LogCategory);
            _requestUri = request.RequestUri;
            _origin = request.Headers["Origin"];
            _referer = request.Referer;
            _response = HttpStatusCode.OK;
            _responseLocation = null;
            _responseBody = responseBody;
            _responseUri = _requestUri;
            Debug.Info($"Response(HttpWebRequest, string) - Body length: {_responseBody?.Length ?? 0}", LogCategory);
        }

        public Response(HttpWebRequest request, WebRequestType requestType)
        {
            Debug.Info($"Response(HttpWebRequest, WebRequestType) - Creating response with type: {requestType} for: {request.RequestUri}", LogCategory);
            _requestUri = request.RequestUri;
            _origin = request.Headers["Origin"];
            _referer = request.Referer;
            _response = HttpStatusCode.OK;
            _responseLocation = null;

            switch (requestType)
            {
                case WebRequestType.RequestVerificationToken:
                    Debug.Info($"Response(HttpWebRequest, WebRequestType) - Using RequestVerificationToken from browser | URL: {App.myLB.strURL_RequestVerificationToken}", LogCategory);
                    _responseBody = App.myLB.strHTML_RequestVerificationToken;
                    _responseUri = new Uri(App.myLB.strURL_RequestVerificationToken, UriKind.Absolute);
                    break;
                case WebRequestType.VerficationCode:
                    Debug.Info($"Response(HttpWebRequest, WebRequestType) - Using VerficationCode from browser | URL: {App.myLB.strURL_VerficationCode}", LogCategory);
                    _responseBody = App.myLB.strHTML_VerficationCode;
                    _responseUri = new Uri(App.myLB.strURL_VerficationCode, UriKind.Absolute);
                    break;
                case WebRequestType.Result:
                    Debug.Info($"Response(HttpWebRequest, WebRequestType) - Using Result from browser | URL: {App.myLB.strURL_Result}", LogCategory);
                    _responseBody = App.myLB.strHTML_Result;
                    _responseUri = new Uri(App.myLB.strURL_Result, UriKind.Absolute);
                    break;
            }
            Debug.Info($"Response(HttpWebRequest, WebRequestType) - Body length: {_responseBody?.Length ?? 0} | ResponseUri: {_responseUri}", LogCategory);

        }

        public string Body
        {
            get
            {
                return _responseBody;
            }
            set
            {
                value = _responseBody;
            }
        }

        public Uri ResponseUri
        {
            get
            {
                return _responseUri;
            }
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
