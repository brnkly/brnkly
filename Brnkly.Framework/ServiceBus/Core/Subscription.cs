using System;
using System.Collections.Generic;

namespace Brnkly.Framework.ServiceBus.Core
{
    internal class Subscription : SubscriptionBase
    {
        public SubscriptionType Type { get; private set; }

        public Subscription(
            Type messageType,
            SubscriptionType subscriptionType,
            IEnumerable<ApplicationEndpoints> applicationEndpointsList)
            : base(messageType, applicationEndpointsList)
        {
            this.Type = subscriptionType;
        }

        public IEnumerable<Uri> GetSendToUris()
        {
            if (this.PhysicalUris.Count == 1 ||
                this.Type == SubscriptionType.Broadcast)
            {
                return this.PhysicalUris;
            }

            return new Uri[] { this.GetSendToUri() };
        }

        public override string ToString()
        {
            return string.Format(
                "MessageType='{0}', Type='{1}'.",
                this.MessageType.AssemblyQualifiedName,
                this.Type);
        }
    }
}
