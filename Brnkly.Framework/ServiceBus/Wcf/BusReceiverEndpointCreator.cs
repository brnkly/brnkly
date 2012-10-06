using System;
using System.Linq;
using System.Messaging;
using System.ServiceModel;
using Brnkly.Framework.Logging;
using Brnkly.Framework.ServiceBus.Core;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    internal class BusReceiverEndpointCreator
    {
        internal static readonly string NetMsmqScheme = new NetMsmqBinding().Scheme;

        public virtual void CreateEndpoints(ServiceHost serviceHost, LogBuffer logBuffer)
        {
            string applicationName = this.GetApplicationName(serviceHost);

            var applicationEndpoints = new ApplicationEndpoints(applicationName, Environment.MachineName);
            foreach (var endpointInfo in applicationEndpoints)
            {
                this.LogCreatingEndpoint(endpointInfo, logBuffer);
                this.ValidateThatQueueExists(endpointInfo.QueueName);
                this.AddServiceEndpoint(serviceHost, endpointInfo);
            }
        }

        private string GetApplicationName(ServiceHost serviceHost)
        {
            var baseUri = serviceHost.BaseAddresses.First(uri => uri.Scheme == NetMsmqScheme);

            // First segment is "/".  Remaining segments have a trailing "/".
            string applicationName = baseUri.Segments.ElementAt(1);
            if (applicationName == "private/")
            {
                applicationName = baseUri.Segments.ElementAt(2);
            }

            applicationName = applicationName.Substring(0, applicationName.Length - 1);
            return applicationName;
        }

        private void LogCreatingEndpoint(BusEndpointInfo endpointInfo, LogBuffer logBuffer)
        {
            logBuffer.Information(
                "Creating service endpoint:\n\tAddress: {0}\n\tQueue name:{1}",
                endpointInfo.LocalhostUri,
                endpointInfo.QueueName);
        }

        private void ValidateThatQueueExists(string queueName)
        {
            if (!MessageQueue.Exists(queueName))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "The service endpoint could not be created because the message queue does not exist.\n\tQueue name: {0}",
                        queueName));
            }
        }

        private void AddServiceEndpoint(ServiceHost serviceHost, BusEndpointInfo endpointInfo)
        {
            var busBinding = new BusReceiverBinding(endpointInfo.Type.IsTransactional());
            serviceHost.AddServiceEndpoint(typeof(IBusReceiver), busBinding, endpointInfo.LocalhostUri);
        }
    }
}
