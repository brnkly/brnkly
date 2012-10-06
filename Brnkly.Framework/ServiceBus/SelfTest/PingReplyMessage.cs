using System;

namespace Brnkly.Framework.ServiceBus.SelfTest
{
    public class PingReplyMessage : Message
    {
        public static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(10);
    }
}
