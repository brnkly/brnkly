using System.Web.Mvc;
using Brnkly.Framework.Data;
using Raven.Client;

namespace Brnkly.Framework.Web
{
    public class RavenController : Controller
    {
        private readonly IDocumentStore store;

        public IDocumentSession session { get; protected set; }

        public RavenController(IDocumentStore store)
        {
            this.store = store;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            this.session = this.store.OpenSession();
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);

            if (filterContext.IsChildAction)
            {
                return;
            }

            using (session)
            {
                if (filterContext.Exception != null)
                {
                    return;
                }

                if (session != null)
                {
                    var storeAsPlatformDocumentStore = this.store as BrnklyDocumentStore;

                    if (storeAsPlatformDocumentStore != null &&
                        storeAsPlatformDocumentStore.AccessMode == StoreAccessMode.ReadOnly)
                    {
                        return;
                    }

                    session.SaveChanges();
                }
            }
        }
    }
}
