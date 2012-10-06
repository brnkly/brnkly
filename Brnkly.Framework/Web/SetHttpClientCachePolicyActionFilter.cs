using System;
using System.Web;
using System.Web.Mvc;

namespace Brnkly.Framework.Web
{
    public class SetHttpClientCachePolicyActionFilter : ActionFilterAttribute
    {
        private static readonly TimeSpan NormalExpiration = new TimeSpan(0, 0, 3, 0);
        private static readonly TimeSpan ErrorExpiration = new TimeSpan(0, 0, 0, 10);

        private static readonly TimeSpan NormalMaxAge = new TimeSpan(0, 0, 3, 0);
        private static readonly TimeSpan ErrorMaxAge = new TimeSpan(0, 0, 0, 10);

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var httpCachePolicy = filterContext.HttpContext.Response.Cache;

            httpCachePolicy.SetCacheability(HttpCacheability.Public);

            if (filterContext.ActionDescriptor.ControllerDescriptor.ControllerName
                .Equals("error", StringComparison.OrdinalIgnoreCase))
            {
                httpCachePolicy.SetExpires(DateTime.Now.Add(ErrorExpiration));
                httpCachePolicy.SetMaxAge(ErrorMaxAge);
            }
            else
            {
                httpCachePolicy.SetExpires(DateTime.Now.Add(NormalExpiration));
                httpCachePolicy.SetMaxAge(NormalMaxAge);
            }
        }
    }
}
