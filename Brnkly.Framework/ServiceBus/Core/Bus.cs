using System;
using System.Collections.ObjectModel;
using Brnkly.Framework.Configuration;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.ServiceBus.Core
{
    public sealed class Bus : IBus
    {
        private static BusImplementation Implementation;

        public static Func<BusUriProvider, BusImplementation> FactoryMethod { get; set; }

        public void Publish(object message)
        {
            Implementation.Publish(message);
        }

        public void Send(Uri destination, object message)
        {
            Implementation.Send(destination, message);
        }

        public void SendRequest(object message)
        {
            Implementation.SendRequest(message);
        }

        public void SendToSelf(object message)
        {
            Implementation.SendToSelf(message);
        }

        public Uri GetReplyTo<T>()
        {
            return Implementation.GetReplyTo<T>();
        }

        internal static void UpdateSubscriptions(Collection<Application> environmentApplications)
        {
            LogBuffer.Current.LogPriority = LogPriority.Application;

            if (FactoryMethod == null)
            {
                throw new InvalidOperationException("FactoryMethod has not been set.");
            }

            try
            {
                var busUriProvider = new BusUriProvider(
                    PlatformApplication.AllApplications,
                    environmentApplications);
                var newImplementation = FactoryMethod(busUriProvider);

                if (newImplementation == null)
                {
                    throw new InvalidOperationException(
                        "The bus implementation factory method returned null.");
                }

                Implementation = newImplementation;

                LogBuffer.Current.Information(
                    "Service bus updated.\n\n" + busUriProvider.GetAllAsStringForLogging());
            }
            catch (Exception exception)
            {
                LogBuffer.Current.Critical("Failed to load service bus configuration due to the exception below.");
                LogBuffer.Current.Critical(exception);
            }
        }
    }
}

