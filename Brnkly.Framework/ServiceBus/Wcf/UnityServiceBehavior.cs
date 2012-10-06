using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Microsoft.Practices.Unity;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    public class UnityServiceBehavior : IServiceBehavior
    {
        private IUnityContainer container;

        public UnityServiceBehavior(IUnityContainer container)
        {
            this.container = container;
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            var endpointDispatchers = serviceHostBase.ChannelDispatchers
                 .Where(cd => cd as ChannelDispatcher != null)
                 .SelectMany(cd => (cd as ChannelDispatcher).Endpoints)
                 .Where(ed => ed.ContractName != "IMetadataExchange")
                 .Select(ed => ed);

            foreach (var endpointDispatcher in endpointDispatchers)
            {
                ServiceEndpoint serviceEndpoint = serviceDescription.Endpoints
                    .FirstOrDefault(e => e.Contract.Name == endpointDispatcher.ContractName);

                if (serviceEndpoint != null)
                {
                    endpointDispatcher.DispatchRuntime.InstanceProvider =
                        new UnityInstanceProvider(this.container, serviceEndpoint.Contract.ContractType);
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }

        public void AddBindingParameters(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints,
            BindingParameterCollection bindingParameters)
        {
        }
    }
}
