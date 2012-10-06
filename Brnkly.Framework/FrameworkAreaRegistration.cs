using System.Web.Mvc;
using Brnkly.Framework.Logging;
using Brnkly.Framework.Web;
using Microsoft.Practices.Unity;
using MvcContrib.PortableAreas;

namespace Brnkly.Framework
{
    public class FrameworkAreaRegistration : PlatformAreaRegistration
    {
        public override string AreaName
        {
            get { return "Brnkly.Framework"; }
        }

        protected override void ConfigureContainer(IUnityContainer container, LogBuffer log)
        {
        }

        protected override void RegisterArea(
            AreaRegistrationContext context,
            IApplicationBus bus,
            PlatformAreaRegistrationState state)
        {
        }
    }
}
