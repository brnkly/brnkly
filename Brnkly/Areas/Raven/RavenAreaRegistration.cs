using System.Web.Mvc;

namespace Brnkly.Raven.Admin
{
    public class RavenAdminAreaRegistration : AreaRegistration
    {
        public override string AreaName { get { return "Raven"; } }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                name: AreaName + "-Default",
                url: "admin/raven/{controller}/{action}/{id}",
                defaults: new { controller = "Replication", action = "Index", id = UrlParameter.Optional });
        }
    }
}
