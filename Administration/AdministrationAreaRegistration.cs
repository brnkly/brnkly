using System.Web.Mvc;
using Brnkly.Framework.Logging;
using Brnkly.Framework.Web;
using Microsoft.Practices.Unity;
using MvcContrib.PortableAreas;

namespace Administration
{
    public class AdministrationAreaRegistration : PlatformAreaRegistration
    {
        public override string AreaName { get { return "AdminHome"; } }

        protected override void ConfigureContainer(IUnityContainer container, LogBuffer log)
        {
            // Do nothing.
        }

        protected override void RegisterArea(
            AreaRegistrationContext context,
            IApplicationBus bus,
            PlatformAreaRegistrationState state)
        {
            this.RegisterApplicationRoutes(context);
        }

        protected void RegisterApplicationRoutes(AreaRegistrationContext context)
        {
            context.MapRoute(
                "AdminHome-Default",
                "",
                new { controller = "Admin", action = "Index" });
        }
    }
}
