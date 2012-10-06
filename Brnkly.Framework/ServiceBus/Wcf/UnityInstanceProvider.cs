using System;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using Microsoft.Practices.Unity;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    internal class UnityInstanceProvider : IInstanceProvider
    {
        private IUnityContainer container;
        private Type serviceType;

        public UnityInstanceProvider(IUnityContainer container, Type serviceType)
        {
            this.container = container;
            this.serviceType = serviceType;
        }

        public object GetInstance(InstanceContext instanceContext, System.ServiceModel.Channels.Message message)
        {
            return this.container.Resolve(this.serviceType);
        }

        public object GetInstance(InstanceContext instanceContext)
        {
            return this.GetInstance(instanceContext, null);
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            this.container.Teardown(instance);
        }
    }
}
