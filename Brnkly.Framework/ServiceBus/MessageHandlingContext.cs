using System.Collections.Generic;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.ServiceBus
{
    public class MessageHandlingContext<TMessage> : IMessageHandlingContext
    {
        public TMessage Message { get; private set; }
        object IMessageHandlingContext.Message { get { return this.Message; } }
        public IDictionary<string, object> Items { get; private set; }
        public LogBuffer Log { get; private set; }
        public bool StopProcessing { get; set; }

        public MessageHandlingContext(TMessage message)
            : this(message, new LogBuffer())
        {
        }

        public MessageHandlingContext(TMessage message, LogBuffer logBuffer)
        {
            CodeContract.ArgumentNotNull("message", message);
            CodeContract.ArgumentNotNull("logBuffer", logBuffer);

            this.Message = message;
            this.Log = logBuffer;
            this.Items = new Dictionary<string, object>();
            this.StopProcessing = false;
        }
    }
}
