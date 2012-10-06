using System;

namespace Brnkly.Framework.ServiceBus.Core
{
    /// <summary>
    /// Describes a single service bus endpoint for an application.
    /// </summary>
    public class BusEndpointInfo
    {
        private const string BusEndpointUriFormat = "net.msmq://{0}/private/{1}/bus.svc{2}";
        private const string PrivateUriSegment = "/private/";
        private const string PrivateQueuePrefix = @".\private$\";
        private const string SuffixDelimiter = "/";

        public BusEndpointType Type { get; private set; }
        public string QueueName { get; private set; }
        public Uri Uri { get; private set; }
        public Uri LocalhostUri { get; private set; }

        public BusEndpointInfo(BusEndpointType type, string applicationName)
            : this(type, applicationName, Environment.MachineName)
        {
        }

        public BusEndpointInfo(BusEndpointType type, string applicationName, string machineName)
        {
            this.Type = type;
            this.Uri = this.GetBusUri(type, applicationName, machineName);
            this.LocalhostUri = this.GetBusUri(type, applicationName, "localhost");
            this.QueueName = this.Uri.AbsolutePath.Replace(PrivateUriSegment, PrivateQueuePrefix);
        }

        private Uri GetBusUri(BusEndpointType type, string applicationName, string machineName)
        {
            string endpointTypeSuffix = (type == BusEndpointType.Control) ?
                string.Empty :
                SuffixDelimiter + type.ToString();

            return new Uri(
                string.Format(
                    BusEndpointUriFormat,
                    machineName,
                    applicationName,
                    endpointTypeSuffix)
                .ToLowerInvariant());
        }
    }
}
