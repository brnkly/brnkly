using System;
using System.Linq;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.ServiceBus.Core
{
    public abstract class BusImplementation : IBus
    {
        private BusUriProvider busUriProvider;

        protected ApplicationEndpoints CurrentAppEndpoints { get; private set; }

        protected BusImplementation(BusUriProvider busUriProvider)
        {
            this.busUriProvider = busUriProvider;
            this.CurrentAppEndpoints =
                new ApplicationEndpoints(PlatformApplication.Current.Name, Environment.MachineName);
        }

        public Uri GetReplyTo<T>()
        {
            BusEndpointType endpointType = typeof(T).GetRecieverEndpointType();
            return this.CurrentAppEndpoints[endpointType].Uri;
        }

        public void Publish(object message)
        {
            CodeContract.ArgumentNotNull("message", message);

            var sendToUris = this.busUriProvider.GetSendToUris(message.GetType());
            if (sendToUris.Count() == 0)
            {
                this.LogNoSubscribersConfigured(message.GetType());
            }

            LogBuffer logBuffer = null;
            var busActivity = BusActivity.Current ?? new BusActivity();
            foreach (Uri sendToUri in sendToUris)
            {
                try
                {
                    this.Send(sendToUri, message, busActivity);
                }
                catch (Exception exception)
                {
                    this.HandlePublishException(ref logBuffer, sendToUri, exception);
                }

                if (logBuffer != null)
                {
                    logBuffer.Error(
                        "The message may have been successfully published to other endpoints. Message: {0}",
                        message);
                    logBuffer.FlushToLog("Service bus publishing failure", LogPriority.Message);
                }
            }
        }

        public void SendRequest(object message)
        {
            CodeContract.ArgumentNotNull("message", message);

            var providerUri = this.GetProviderUri(message);

            this.Send(providerUri, message);
        }

        public void SendToSelf(object message)
        {
            BusEndpointType endpointType = message.GetType().GetRecieverEndpointType();
            this.Send(this.CurrentAppEndpoints[endpointType].Uri, message);
        }

        public void Send(Uri destination, object message)
        {
            this.Send(destination, message, BusActivity.Current ?? new BusActivity());
        }

        protected abstract void SendCore(Uri destination, TransportMessage transportMessage);

        private void Send(Uri destination, object message, BusActivity busActivity)
        {
            CodeContract.ArgumentNotNull("destination", destination);
            CodeContract.ArgumentNotNull("message", message);

            var transportMessage = this.CreateTransportMessage(message, busActivity);

            this.SendCore(destination, transportMessage);

            ServiceBusCounters.IncrementSent(message.GetType().FullName);
        }

        private TransportMessage CreateTransportMessage(object message, BusActivity busActivity)
        {
            return new TransportMessage()
            {
                Id = Guid.NewGuid(),
                InnerMessage = message,
                BusActivity = busActivity ?? BusActivity.Current ?? new BusActivity(),
                SentAtUtc = DateTimeOffset.UtcNow,
                Originator = Environment.MachineName
            };
        }

        private void HandlePublishException(ref LogBuffer logBuffer, Uri sendToUri, Exception exception)
        {
            logBuffer = logBuffer ?? new LogBuffer();

            logBuffer.Error("Publishing to '{0}' failed.", sendToUri);
            logBuffer.Error(exception);
        }

        private Uri GetProviderUri(object message)
        {
            var providerUri = this.busUriProvider.GetServiceProviderUri(message.GetType());
            if (providerUri == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "No service provider is configured for the request message type: {0}.\n\n" +
                        "Add a service provider for this message type to the BusConfiguration.",
                        message.GetType().FullName));
            }

            return providerUri;
        }

        private void LogNoSubscribersConfigured(Type messageType)
        {
            Log.Warning(
                string.Format(
                    "No subscribers are configured for the message type: {0}. " +
                    "Add a subscriber for this message type to the BusConfiguration.",
                    messageType.FullName),
                LogPriority.Message);
        }
    }
}
