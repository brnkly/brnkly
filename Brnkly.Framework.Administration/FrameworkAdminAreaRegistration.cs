using System.Web.Mvc;
using Brnkly.Framework.Data;
using Brnkly.Framework.Logging;
using Brnkly.Framework.Web;
using Brnkly.Framework.Web.Menus;
using Microsoft.Practices.Unity;
using MvcContrib.PortableAreas;

namespace Brnkly.Framework.Administration
{
    public class FrameworkAdminAreaRegistration : PlatformAreaRegistration
    {
        public override string AreaName { get { return "framework"; } }

        protected override void ConfigureContainer(IUnityContainer container, LogBuffer log)
        {
            BrnklyDocumentStore.Register(
                container,
                StoreName.Operations,
                StoreAccessMode.ReadWrite);
        }

        protected override void RegisterArea(
            AreaRegistrationContext context,
            IApplicationBus bus,
            PlatformAreaRegistrationState state)
        {
            //this.RequireAuthorizationOnAllActions("framework/admin/view");
            this.RegisterRoutes(context);
            this.AddSettingsMenuItem(bus);
            new AutoMapperConfig().Initialize();
        }

        private void RegisterRoutes(AreaRegistrationContext context)
        {
            context.MapRoute(
                this.AreaName + "-Default",
                this.AreaRoutePrefix + "/{controller}/{action}/{*id}",
                new { controller = "applications", action = "index", id = UrlParameter.Optional });
        }

        private void AddSettingsMenuItem(IApplicationBus bus)
        {
            bus.Send(
                new AddMenuItem
                {
                    MenuItem = new MenuItem
                    {
                        MenuName = "Settings",
                        LinkText = "Logging",
                        ActionName = "Index",
                        ControllerName = "LoggingSettings",
                        AreaName = "Framework",
                        HtmlAttributes = new { data_role = "button" },
                        Position = int.MinValue
                    }
                });

            bus.Send(
                new AddMenuItem
                {
                    MenuItem = new MenuItem
                    {
                        MenuName = "Settings",
                        LinkText = "Caching",
                        ActionName = "Index",
                        ControllerName = "CacheSettings",
                        AreaName = "Framework",
                        HtmlAttributes = new { data_role = "button" },
                        Position = int.MinValue
                    }
                });
        }
    }
}
