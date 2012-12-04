using System.Web.Mvc;

namespace Brnkly.Raven.Admin.Controllers
{
    public class RavenAdminController : Controller
    {
        public ActionResult Replication()
        {
            return View();
        }

        public ActionResult Indexes()
        {
            return View();
        }
    }
}
