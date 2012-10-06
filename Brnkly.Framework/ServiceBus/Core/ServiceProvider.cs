using System;
using System.Collections.Generic;

namespace Brnkly.Framework.ServiceBus.Core
{
    internal class ServiceProvider : SubscriptionBase
    {
        public ServiceProvider(
            Type requestMessageType,
            IEnumerable<ApplicationEndpoints> applicationEndpointsList)
            : base(requestMessageType, applicationEndpointsList)
        {
        }

        public override string ToString()
        {
            return string.Format(
                "MessageType='{0}'.",
                this.MessageType.AssemblyQualifiedName);
        }
    }
}
