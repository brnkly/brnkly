using System;
using Microsoft.Practices.Unity;
using MvcContrib.PortableAreas;

namespace Brnkly.Framework.Web
{
    internal class UnityMessageHandlerFactory : IMessageHandlerFactory
    {
        private IUnityContainer container;

        public UnityMessageHandlerFactory(IUnityContainer container)
        {
            this.container = container;
        }

        public IMessageHandler Create(Type type)
        {
            return (IMessageHandler)this.container.Resolve(type);
        }
    }
}
