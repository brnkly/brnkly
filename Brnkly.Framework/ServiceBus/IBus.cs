using System;

namespace Brnkly.Framework.ServiceBus
{
    public interface IBus
    {
        void Publish(object message);
        void Send(Uri destination, object message);
        void SendRequest(object message);
        void SendToSelf(object message);
        Uri GetReplyTo<T>();
    }
}
