using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    public sealed class BusReceiverServiceHost : ServiceHost
    {
        public BusReceiverServiceHost(params Uri[] baseAddresses)
            : base(
                typeof(BusReceiver),
                baseAddresses.Where(uri => uri.Scheme.Equals(new NetMsmqBinding().Scheme)).ToArray())
        {
        }

        protected override void ApplyConfiguration()
        {
            var logBuffer = LogBuffer.Current;

            try
            {
                logBuffer.Information("Configuring BusReceiverServiceHost...");

                base.ApplyConfiguration();
                PlatformApplication.Current.Initialize(logBuffer);
                this.AddUnityServiceBehavior();
                this.AddThrottlingBehavior();
                this.AddFaultHandler();
                this.CreateEndpoints(logBuffer);

                logBuffer.Information("BusReceiverServiceHost was successfully configured.");
            }
            catch (Exception exception)
            {
                logBuffer.Critical("BusReceiverServiceHost configuration failed.");
                logBuffer.Critical(exception);
                throw;
            }
            finally
            {
                logBuffer.FlushToLog(LogPriority.Application);
            }
        }

        private void AddUnityServiceBehavior()
        {
            if (this.Description.Behaviors.Find<UnityServiceBehavior>() == null)
            {
                this.Description.Behaviors.Add(
                    new UnityServiceBehavior(PlatformApplication.Current.Container));
            }
        }

        private void AddThrottlingBehavior()
        {
            var behavior = this.Description.Behaviors.Find<ServiceThrottlingBehavior>();
            if (behavior == null)
            {
                behavior = new ServiceThrottlingBehavior();
                this.Description.Behaviors.Add(behavior);
            }

            behavior.MaxConcurrentCalls = 50;
            behavior.MaxConcurrentInstances = 50;
            behavior.MaxConcurrentSessions = 50;
        }

        private void AddFaultHandler()
        {
            this.Faulted += new EventHandler(BusReceiverServiceHost_Faulted);
        }

        private void CreateEndpoints(LogBuffer logBuffer)
        {
            new BusReceiverEndpointCreator().CreateEndpoints(this, logBuffer);
            BusReceiverDataContractResolver.AddToEndpoints(this.Description.Endpoints);
        }

        private void BusReceiverServiceHost_Faulted(object sender, EventArgs e)
        {
            Log.Critical(
                "The BusReceiverServiceHost faulted.",
                LogPriority.Application);
        }
    }
}
