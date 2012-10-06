using Brnkly.Framework;
using Brnkly.Framework.Data;
using Brnkly.Framework.Logging;
using Brnkly.Framework.Web;
using Microsoft.Practices.Unity;
using MvcContrib.PortableAreas;
using System.Web.Mvc;

namespace Demo.Areas.Content
{
    public class ContentAreaRegistration : PlatformAreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Content";
            }
        }

        protected override void ConfigureContainer(IUnityContainer container, LogBuffer log)
        {
            BrnklyDocumentStore.Register(
                container,
                StoreName.Content,
                StoreAccessMode.ReadWrite);
        }

        protected override void RegisterArea(
            AreaRegistrationContext context, 
            IApplicationBus bus, 
            PlatformAreaRegistrationState state)
        {
            context.MapRoute(
                "Content_default",
                "Content/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
