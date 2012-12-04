using System.Web.Http;
using Raven.Client;

namespace Brnkly.Raven.Web
{
    public class RavenApiController : ApiController
    {
        public IDocumentSession RavenSession { get; set; }

        protected override void Dispose(bool disposing)
        {
            if(RavenSession != null)
            {
                RavenSession.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}