using Brnkly.Framework.Logging;
using Microsoft.Practices.Unity;

namespace Brnkly.Framework.ServiceBus
{
    public interface IServiceBusAreaRegistration
    {
        void ConfigureContainer(IUnityContainer container, LogBuffer logBuffer);
    }
}
