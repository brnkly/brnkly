using System.Diagnostics;
using System.Web.Mvc;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.Web
{
    public class SetLoggingLevelAttribute : ActionFilterAttribute
    {
        public LogPriority LogPriority { get; set; }
        public TraceEventType Severity { get; set; }

        public SetLoggingLevelAttribute(LogPriority logPriority, TraceEventType severity)
        {
            this.LogPriority = logPriority;
            this.Severity = severity;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            LogBuffer.Current.LogPriority = this.LogPriority;
            LogBuffer.Current.Severity = this.Severity;
        }
    }
}
