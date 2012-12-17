using System.Web.Mvc;

namespace Brnkly.Raven.Admin
{
    public class BrnklyAreaRegistration : AreaRegistration
    {
        public override string AreaName { get { return "Brnkly"; } }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                name: AreaName + "-Default",
                url: "brnkly/{action}/{id}",
                defaults: new { controller = "Raven", action = "Index", id = UrlParameter.Optional });
        }
    }
}
