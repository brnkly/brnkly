using System;
using System.Web.Mvc;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.Caching
{
    public class ConfiguredOutputCacheAttribute : OutputCacheAttribute
    {
        public new int Duration
        {
            get { return base.Duration; }
            set { throw new NotSupportedException("Duration cannot be set directly. Set from runtime config."); }
        }

        public new string VaryByParam
        {
            get { return base.VaryByParam; }
            set { throw new NotSupportedException(); }
        }

        public new string VaryByHeader
        {
            get { return base.VaryByHeader; }
            set { throw new NotSupportedException(); }
        }

        public ConfiguredOutputCacheAttribute()
        {
            base.Duration = CacheSettings.OutputCacheDurationSeconds;
            base.VaryByParam = "*";
            base.VaryByHeader = "host";
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.Duration = CacheSettings.OutputCacheDurationSeconds;

            if (base.Duration > 0)
            {
                LogBuffer.Current.Information("Processing using output cache duration TTL of {0} seconds.", base.Duration);

                //note: you cannot put base.OnActionExecuting inside this if block. it will cause 
                //an exception to be thrown (see: Application log) because of the way the underlying 
                //plumbing deals with cached heirarchies.
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
