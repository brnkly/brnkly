using System.Web.Mvc;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.Web
{
    public class LogActionFilter : IActionFilter, IResultFilter
    {
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var logBuffer = LogBuffer.Current;

            if (!filterContext.IsChildAction)
            {
                logBuffer.Verbose("Request Url: {0}", filterContext.HttpContext.Request.Url);
                // Do not add additional values here. Use HttpContextInformationProvider instead.
            }

            logBuffer.Verbose(
                "Starting rendering of action {0} with route values:",
                filterContext.ActionDescriptor.ActionName);

            foreach (var key in filterContext.RouteData.Values.Keys)
            {
                logBuffer.Verbose("  {0}='{1}'", key, filterContext.RouteData.Values[key]);
            }
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (!filterContext.IsChildAction)
            {
                if (filterContext.Exception != null)
                {
                    var logBuffer = LogBuffer.Current;
                    logBuffer.Error(filterContext.Exception);
                    this.WriteToLog(logBuffer);
                }
            }
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // Do nothing.
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (!filterContext.IsChildAction)
            {
                var logBuffer = LogBuffer.Current;
                if (filterContext.Exception != null)
                {
                    logBuffer.Error(filterContext.Exception);
                }

                this.WriteToLog(logBuffer);
            }
        }

        private void WriteToLog(LogBuffer logBuffer)
        {
            logBuffer.FlushToLog("Request log", LogPriority.Request);
        }
    }
}
