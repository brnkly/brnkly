using System;
using System.Threading;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.ServiceBus.SelfTest
{
    public class BusSelfTest :
        IMessageHandler<PingMessage>,
        IMessageHandler<TxPingMessage>,
        IMessageHandler<PingReplyMessage>,
        IMessageHandler<TxPingReplyMessage>
    {
        private static bool replyReceived;
        private static bool txReplyReceived;
        private IBus bus;

        public BusSelfTest(IBus bus)
        {
            this.bus = bus;
        }

        internal static void Run(IBus bus, LogBuffer logBuffer)
        {
            logBuffer.Information("Running bus self-test.");
            bus.SendToSelf(new PingMessage { ReplyTo = bus.GetReplyTo<PingReplyMessage>() });
            bus.SendToSelf(new TxPingMessage { ReplyTo = bus.GetReplyTo<TxPingReplyMessage>() });

            for (int count = 0; count <= 20; count++)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(250));
                if (replyReceived && txReplyReceived)
                {
                    break;
                }
            }

            Conclude(logBuffer);
        }

        private static void Conclude(LogBuffer logBuffer)
        {
            if (replyReceived)
            {
                logBuffer.Information("Received non-transactional ping reply.");
            }
            else
            {
                logBuffer.Warning("Non-transactional ping reply was not received before the timeout.");
            }

            if (txReplyReceived)
            {
                logBuffer.Information("Received transactional ping reply.");
            }
            else
            {
                logBuffer.Warning("Transactional ping reply was not received before the timeout.");
            }
        }

        public void Handle(MessageHandlingContext<PingMessage> context)
        {
            this.bus.Send(context.Message.ReplyTo, new PingReplyMessage());
        }

        public void Handle(MessageHandlingContext<TxPingMessage> context)
        {
            this.bus.Send(context.Message.ReplyTo, new TxPingReplyMessage());
        }

        public void Handle(MessageHandlingContext<PingReplyMessage> context)
        {
            replyReceived = true;
        }

        public void Handle(MessageHandlingContext<TxPingReplyMessage> context)
        {
            txReplyReceived = true;
        }
    }
}
