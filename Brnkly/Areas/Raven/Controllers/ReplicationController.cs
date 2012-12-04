using System.Web.Mvc;
using Brnkly.Raven.Web;
using Raven.Client;

namespace Brnkly.Raven.Admin.Controllers
{
    public class ReplicationController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
