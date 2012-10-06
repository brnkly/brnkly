using System.Web;

namespace Brnkly.Framework.Web
{
    public static class HttpContextExtensions
    {
        public static T GetOrCreateItem<T>(this HttpContextBase httpContext, string key)
            where T : class, new()
        {
            T contextItem = httpContext.Items[key] as T;

            if (contextItem == null)
            {
                contextItem = new T();
                httpContext.Items[key] = contextItem;
            }

            return contextItem;
        }

        internal static T GetOrCreateItem<T>(this HttpContext httpContext, string key)
            where T : class, new()
        {
            return GetOrCreateItem<T>(new HttpContextWrapper(httpContext), key);
        }
    }
}
