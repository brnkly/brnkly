using Brnkly.Framework.Logging;
using Microsoft.Practices.Unity;

namespace Brnkly.Framework.Web
{
    public class PlatformAreaRegistrationState
    {
        public IUnityContainer Container { get; private set; }
        public LogBuffer Log { get; private set; }

        public PlatformAreaRegistrationState(IUnityContainer container, LogBuffer logBuffer)
        {
            this.Container = container;
            this.Log = logBuffer;
        }
    }
}
