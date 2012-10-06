using System;

namespace Brnkly.Framework.ServiceBus
{
    public abstract class RequestMessage : Message
    {
        public Uri ReplyTo { get; set; }
    }
}
