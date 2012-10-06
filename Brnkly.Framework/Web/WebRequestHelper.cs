using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Brnkly.Framework.Web
{
    public class WebRequestHelper
    {
        private const int BinaryBufferLength = 32768;

        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(500);

        public static WebResponseData Get(
            Uri uri,
            IEnumerable<Cookie> cookies = null,
            TimeSpan? timeout = null,
            bool downloadAsBinary = false)
        {
            return IssueRequest(uri, "GET", null, cookies, timeout, downloadAsBinary);
        }

        public static WebResponseData Post(
            Uri uri,
            string requestContent,
            IEnumerable<Cookie> cookies = null,
            TimeSpan? timeout = null,
            bool downloadAsBinary = false)
        {
            return IssueRequest(uri, "POST", requestContent, cookies, timeout, downloadAsBinary);
        }

        public static WebResponseData IssueRequest(
            Uri uri,
            string method = "GET",
            string requestContent = null,
            IEnumerable<Cookie> cookies = null,
            TimeSpan? timeout = null,
            bool downloadAsBinary = false)
        {
            cookies = cookies ?? Enumerable.Empty<Cookie>();
            timeout = timeout ?? DefaultTimeout;

            var responseData = new WebResponseData { RequestedUri = uri };

            try
            {
                var webRequest = GetRequest(uri, method, timeout.Value, requestContent);

                AttachOutgoingCookies(webRequest, cookies);

                using (var webResponse = (HttpWebResponse)webRequest.GetResponse())
                using (var responseStream = webResponse.GetResponseStream())
                {
                    responseData.StatusCode = webResponse.StatusCode;
                    responseData.ContentType = webResponse.GetResponseHeader("Content-Type");

                    if (downloadAsBinary)
                    {
                        ProcessResponseBinary(responseStream, responseData);
                    }
                    else
                    {
                        ProcessResponse(responseStream, responseData);
                    }
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    var webResponse = (HttpWebResponse)e.Response;
                    responseData.StatusCode = webResponse.StatusCode;
                }
            }

            return responseData;
        }

        private static void ProcessResponse(
            Stream responseStream,
            WebResponseData responseData)
        {
            using (var reader = new StreamReader(responseStream, Encoding.UTF8))
            {
                responseData.Content = reader.ReadToEnd();
            }
        }

        private static void ProcessResponseBinary(
            Stream responseStream,
            WebResponseData responseData)
        {
            var buffer = new byte[BinaryBufferLength];

            using (var memoryStream = new MemoryStream())
            {
                while (true)
                {
                    int read = responseStream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        responseData.Bytes = memoryStream.ToArray();
                        return;
                    }

                    memoryStream.Write(buffer, 0, read);
                }
            }
        }

        private static HttpWebRequest GetRequest(Uri uri, string method, TimeSpan timeout, string requestContent)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Timeout = (int)timeout.TotalMilliseconds;
            webRequest.AllowAutoRedirect = false;

            if (method == "POST")
            {
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = requestContent.Length;

                using (var requestStream = webRequest.GetRequestStream())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(requestContent);
                    requestStream.Write(bytes, 0, requestContent.Length);
                }
            }

            return webRequest;
        }

        private static void AttachOutgoingCookies(
            HttpWebRequest webRequest,
            IEnumerable<Cookie> cookies)
        {
            webRequest.CookieContainer = new CookieContainer();
            foreach (var cookie in cookies)
            {
                if (string.IsNullOrWhiteSpace(cookie.Domain))
                {
                    cookie.Domain = webRequest.RequestUri.Host;
                }
                webRequest.CookieContainer.Add(cookie);
            }
        }
    }
}
