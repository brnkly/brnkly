using System.Web.Mvc;
using Raven.Client;

namespace Brnkly.Raven.Web
{
    public abstract class RavenMvcController : Controller
    {
        public IDocumentSession RavenSession { get; set; }

        protected override void OnException(ExceptionContext filterContext)
        {
            DisposeSession();
            base.OnException(filterContext);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            DisposeSession();
            base.OnActionExecuted(filterContext);
        }

        private void DisposeSession()
        {
            if (RavenSession != null)
            {
                RavenSession.Dispose();
            }
        }
    }
}
