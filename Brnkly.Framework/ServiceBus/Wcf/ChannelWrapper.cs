using System;
using System.ServiceModel;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    internal static class ChannelWrapper<T>
    {
        public static void Execute(T proxy, Action<T> actionToExecute)
        {
            var channel = (ICommunicationObject)proxy;
            bool success = false;
            try
            {
                actionToExecute(proxy);
                channel.Close();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    channel.Abort();
                }
            }
        }
    }
}
