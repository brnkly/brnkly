using System.Collections.Generic;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.ServiceBus
{
    public interface IMessageHandlingContext
    {
        IDictionary<string, object> Items { get; }
        LogBuffer Log { get; }
        object Message { get; }
        bool StopProcessing { get; set; }
    }
}
