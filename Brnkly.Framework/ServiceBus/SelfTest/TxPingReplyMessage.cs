using System;

namespace Brnkly.Framework.ServiceBus.SelfTest
{
    public class TxPingReplyMessage : Message
    {
        public static readonly bool IsTransactional = true;
        public static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(10);
    }
}
