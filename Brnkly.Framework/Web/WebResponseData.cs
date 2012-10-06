using System;
using System.Net;

namespace Brnkly.Framework.Web
{
    public class WebResponseData
    {
        public Uri RequestedUri { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
        public byte[] Bytes { get; set; }
    }
}
