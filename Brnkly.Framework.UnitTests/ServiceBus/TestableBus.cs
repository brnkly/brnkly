using System;
using System.Collections.ObjectModel;
using Brnkly.Framework.ServiceBus;
using Brnkly.Framework.ServiceBus.Core;

namespace Brnkly.Framework.UnitTests
{
    public class TestableBus : BusImplementation
    {
        public Collection<MessageSent> MessagesSent = new Collection<MessageSent>();

        public TestableBus(BusUriProvider busUriProvider)
            : base(busUriProvider)
        {
        }

        protected override void SendCore(Uri destination, TransportMessage transportMessage)
        {
            this.MessagesSent.Add(new MessageSent(destination, transportMessage.InnerMessage));
        }

        public class MessageSent
        {
            public Uri Destination { get; set; }
            public object Message { get; set; }
            public Type MessageType { get { return this.Message.GetType(); } }

            public MessageSent(Uri destination, object message)
            {
                this.Destination = destination;
                this.Message = message;
            }
        }
    }
}
