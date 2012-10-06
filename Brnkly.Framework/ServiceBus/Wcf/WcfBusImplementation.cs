using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Transactions;
using Brnkly.Framework.Logging;
using Brnkly.Framework.ServiceBus.Core;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    public sealed class WcfBusImplementation : BusImplementation
    {
        private static readonly TransactionOptions BusSendTransactionOptions =
            new TransactionOptions()
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromSeconds(10)
            };

        private readonly bool useCustomDeadLetterQueue;
        private ConcurrentDictionary<Type, ChannelFactory<IBusReceiver>> channelFactories =
            new ConcurrentDictionary<Type, ChannelFactory<IBusReceiver>>();
        private Func<Type, ChannelFactory<IBusReceiver>> createChannelFactoryDelegate;

        public WcfBusImplementation(BusUriProvider busUriProvider, bool useCustomDeadLetterQueue)
            : base(busUriProvider)
        {
            this.useCustomDeadLetterQueue = useCustomDeadLetterQueue;
            this.createChannelFactoryDelegate =
                messageType => this.CreateChannelFactory(messageType);
        }

        protected override void SendCore(Uri destination, TransportMessage transportMessage)
        {
            var messageType = transportMessage.InnerMessage.GetType();
            var channelFactory = this.GetOrCreateChannelFactory(messageType);
            var busReceiver = channelFactory.CreateChannel(new EndpointAddress(destination));

            try
            {
                if (messageType.IsTransactional())
                {
                    SendInAmbientOrNewTransaction(transportMessage, busReceiver);
                }
                else
                {
                    SendWithoutTransaction(transportMessage, busReceiver);
                }
            }
            catch (SerializationException serializationException)
            {
                this.ThrowWrappedSerializationException(messageType, serializationException);
            }
        }

        private ChannelFactory<IBusReceiver> GetOrCreateChannelFactory(Type messageType)
        {
            ChannelFactory<IBusReceiver> channelFactory;

            if (this.channelFactories.TryGetValue(messageType, out channelFactory) &&
                channelFactory.State != CommunicationState.Faulted)
            {
                return channelFactory;
            }

            channelFactory = this.channelFactories.GetOrAdd(
                messageType,
                this.createChannelFactoryDelegate(messageType));

            BusReceiverDataContractResolver.AddToEndpoints(new[] { channelFactory.Endpoint });

            return channelFactory;
        }

        private ChannelFactory<IBusReceiver> CreateChannelFactory(Type messageType)
        {
            NetMsmqBinding binding = CreateBinding(messageType);

            try
            {
                return new ChannelFactory<IBusReceiver>(binding);
            }
            catch (ReflectionTypeLoadException exception)
            {
                // This exception is typically caused by an invalid or unknown message type.
                // We do this to expose the message type name in the log entry.
                Exception[] loaderExceptions = exception.LoaderExceptions;
                foreach (Exception loaderException in loaderExceptions)
                {
                    Log.Error(loaderException, null, LogPriority.Application);
                }

                throw;
            }
        }

        private NetMsmqBinding CreateBinding(Type messageType)
        {
            var binding = new NetMsmqBinding(NetMsmqSecurityMode.None);
            binding.TimeToLive = messageType.TimeToLive();

            if (messageType.IsTransactional())
            {
                if (this.useCustomDeadLetterQueue)
                {
                    binding.DeadLetterQueue = DeadLetterQueue.Custom;
                    binding.CustomDeadLetterQueue = this.CurrentAppEndpoints[BusEndpointType.TxDeadLetter].Uri;
                }
            }
            else
            {
                binding.ExactlyOnce = false;
                binding.Durable = false;
                binding.DeadLetterQueue = DeadLetterQueue.None;
            }

            return binding;
        }

        private static void SendWithoutTransaction(TransportMessage transportMessage, IBusReceiver busReceiver)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Suppress))
            {
                ChannelWrapper<IBusReceiver>.Execute(busReceiver, r => r.Receive(transportMessage));
                transaction.Complete();
            }
        }

        private static void SendInAmbientOrNewTransaction(TransportMessage transportMessage, IBusReceiver busReceiver)
        {
            if (Transaction.Current == null)
            {
                using (var transaction = new TransactionScope(TransactionScopeOption.Required, BusSendTransactionOptions))
                {
                    ChannelWrapper<IBusReceiver>.Execute(busReceiver, r => r.ReceiveInTransaction(transportMessage));
                    transaction.Complete();
                }
            }
            else
            {
                // Use the existing ambient transaction.
                ChannelWrapper<IBusReceiver>.Execute(busReceiver, r => r.ReceiveInTransaction(transportMessage));
            }
        }

        private void ThrowWrappedSerializationException(
            Type messageType,
            SerializationException serializationException)
        {
            string logMessage = string.Format(
                "A serialization exception occured while sending or publishing a message: {0}. " +
                "Ensure that all classes used in the message meet the criteria defined in " +
                "Brnkly.Platform.Framework.ServiceBus.Core.MessageTypeLoader. " +
                "See the inner exception for details.",
                messageType.FullName);

            throw new InvalidOperationException(logMessage, serializationException);
        }
    }
}
