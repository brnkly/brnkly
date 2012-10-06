using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    public class BusReceiverServiceHostFactory : ServiceHostFactory
    {
        private Uri[] baseAddresses;
        private BusReceiverServiceHost serviceHost;

        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            this.baseAddresses = baseAddresses;
            this.serviceHost = this.CreateServiceHostInternal();
            return this.serviceHost;
        }

        private BusReceiverServiceHost CreateServiceHostInternal()
        {
            var host = new BusReceiverServiceHost(this.baseAddresses);
            host.Faulted += new EventHandler(host_Faulted);
            return host;
        }

        private void host_Faulted(object sender, EventArgs e)
        {
            var logBuffer = new LogBuffer();

            try
            {
                logBuffer.Critical("The BusReceiverServiceHost faulted. Attempting to re-open...");

                this.serviceHost.Abort();
                this.serviceHost = this.CreateServiceHostInternal();
                this.serviceHost.Open();

                logBuffer.Information("BusReceiverServiceHost re-opened.");
            }
            finally
            {
                logBuffer.FlushToLog(LogPriority.Application, LogCategory.ServiceBus);
            }
        }
    }
}
