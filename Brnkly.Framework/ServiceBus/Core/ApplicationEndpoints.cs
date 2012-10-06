using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Framework.ServiceBus.Core
{
    public class ApplicationEndpoints : KeyedCollection<BusEndpointType, BusEndpointInfo>
    {
        public string ApplicationName { get; private set; }
        public string MachineName { get; private set; }

        public ApplicationEndpoints(string applicationName)
            : this(applicationName, Environment.MachineName)
        {
        }

        public ApplicationEndpoints(string applicationName, string machineName)
        {
            this.ApplicationName = applicationName;
            this.MachineName = machineName;

            foreach (var value in Enum.GetValues(typeof(BusEndpointType)))
            {
                this.Add(new BusEndpointInfo((BusEndpointType)value, applicationName, machineName));
            }
        }

        public static string[] GetAllQueueNames(string applicationName)
        {
            var applicationEndpoints = new ApplicationEndpoints(applicationName, Environment.MachineName);

            return applicationEndpoints
                .Select(endpoint => endpoint.QueueName)
                .ToArray();
        }

        protected override BusEndpointType GetKeyForItem(BusEndpointInfo item)
        {
            return item.Type;
        }
    }
}
