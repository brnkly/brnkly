using System;
using System.ServiceModel;
using System.Xml;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    public class BusReceiverBinding : NetMsmqBinding
    {
        internal const int MaxAttempts = 3;

        public BusReceiverBinding()
            : this(false)
        {
        }

        public BusReceiverBinding(bool transactional)
            : base(NetMsmqSecurityMode.None)
        {
            this.ExactlyOnce = transactional;
            this.Durable = transactional;

            this.MaxReceivedMessageSize = 3 * 1024 * 1024;

            this.ReaderQuotas = XmlDictionaryReaderQuotas.Max;
            this.ReaderQuotas.MaxDepth = 128;
            this.ReaderQuotas.MaxStringContentLength = 3 * 1024 * 1024;

            this.ReceiveErrorHandling = ReceiveErrorHandling.Reject;
            this.ReceiveRetryCount = 0;
            this.MaxRetryCycles = MaxAttempts - 1;
            this.RetryCycleDelay = new TimeSpan(0, 0, 5);
        }
    }
}
