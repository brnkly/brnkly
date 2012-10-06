using System;

namespace Brnkly.Framework.ServiceBus.SelfTest
{
    public class PingMessage : RequestMessage
    {
        public static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(10);
    }
}
