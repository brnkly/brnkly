using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Microsoft.Practices.EnterpriseLibrary.Logging.ExtraInformation;

namespace Brnkly.Framework.Logging
{
    public class HttpContextInformationProvider : IExtraInformationProvider
    {
        public void PopulateDictionary(IDictionary<string, object> dict)
        {
            if (HttpContext.Current == null ||
                HttpContext.Current.Handler == null)
            {
                return;
            }

            var request = HttpContext.Current.Request;

            dict["HttpContext.Request.HttpMethod"] = request.HttpMethod;
            dict["HttpContext.Request.RawUrl"] = request.RawUrl;
            dict["HttpContext.Request.Url.Port"] = request.Url.Port;
            dict["HttpContext.Request.UserAgent"] = request.UserAgent;
            dict["HttpContext.Request.UserHostAddress"] = request.UserHostAddress;
            dict["Referer"] = request.Headers["Referer"] ?? string.Empty;
            dict["X-SkyPadInterop-OriginalUrl"] = request.Headers["X-SkyPadInterop-OriginalUrl"] ?? string.Empty;
            dict["HttpContext.Request.Cookies"] = GetCookies();
        }

        private static string GetCookies()
        {
            var request = HttpContext.Current.Request;
            StringBuilder cookies = new StringBuilder();

            foreach (string key in request.Cookies.AllKeys)
            {
                if (key.EndsWith("FormsAuth", StringComparison.OrdinalIgnoreCase))
                {
                    cookies.AppendFormat("{0}=[value suppressed];", key);
                }
                else
                {
                    cookies.AppendFormat("{0}={1};", key, request.Cookies[key].Value);
                }
            }

            return cookies.ToString();
        }
    }
}
