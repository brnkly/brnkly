using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace Brnkly.Framework.Web
{
    internal class PlatformControllerActivator : IControllerActivator
    {
        public IController Create(RequestContext requestContext, Type controllerType)
        {
            return DependencyResolver.Current.GetService(controllerType)
                as IController;
        }
    }
}
