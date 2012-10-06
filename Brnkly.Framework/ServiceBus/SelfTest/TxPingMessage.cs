using System;

namespace Brnkly.Framework.ServiceBus.SelfTest
{
    public class TxPingMessage : PingMessage
    {
        public static readonly bool IsTransactional = true;
        new public static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(10);
    }
}
