using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Framework.ServiceBus.Core
{
    internal abstract class SubscriptionBase
    {
        private int requestCount;

        public Type MessageType { get; private set; }
        public ReadOnlyCollection<Uri> PhysicalUris { get; private set; }

        protected SubscriptionBase(
            Type messageType,
            IEnumerable<ApplicationEndpoints> applicationEndpointsList)
        {
            CodeContract.ArgumentNotNull("messageType", messageType);
            CodeContract.ArgumentHasAtLeastOneItem("applicationEndpointsList", applicationEndpointsList);

            this.MessageType = messageType;

            BusEndpointType endpointType = messageType.GetRecieverEndpointType();
            this.PhysicalUris = (from appEndpoints in applicationEndpointsList
                                 select appEndpoints[endpointType].Uri)
                                .ToList()
                                .AsReadOnly();
        }

        public Uri GetSendToUri()
        {
            if (this.PhysicalUris.Count == 1)
            {
                return this.PhysicalUris[0];
            }

            int nextUriIndex = requestCount++ % this.PhysicalUris.Count;

            return this.PhysicalUris[nextUriIndex];
        }
    }
}
