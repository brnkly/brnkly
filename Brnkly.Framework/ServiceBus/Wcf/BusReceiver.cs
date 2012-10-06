using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Transactions;
using Brnkly.Framework.Logging;
using Brnkly.Framework.ServiceBus.Core;
using Microsoft.Practices.Unity;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerCall,
        ConcurrencyMode = ConcurrencyMode.Single,
        ReleaseServiceInstanceOnTransactionComplete = true,
        TransactionTimeout = "00:00:15",
        TransactionIsolationLevel = IsolationLevel.ReadCommitted,
        AddressFilterMode = AddressFilterMode.Any)]
    public sealed class BusReceiver : IBusReceiver
    {
        private const string HandleMethodName = "Handle";

        private static readonly ApplicationEndpoints applicationEndpoints =
            new ApplicationEndpoints(PlatformApplication.Current.Name, Environment.MachineName);

        private IUnityContainer container;
        private MessageHandlerRegistry handlerRegistry;
        private LogBuffer logBuffer;

        public BusReceiver(IUnityContainer container, MessageHandlerRegistry handlerRegistry)
        {
            this.container = container;
            this.handlerRegistry = handlerRegistry;
            this.logBuffer = LogBuffer.Current;
        }

        [OperationBehavior(TransactionScopeRequired = true, TransactionAutoComplete = true)]
        public void ReceiveInTransaction(TransportMessage transportMessage)
        {
            this.Receive(transportMessage);
        }

        // TODO: JB: Set timeout on this operation, since Tx timeout no longer applies.
        public void Receive(TransportMessage transportMessage)
        {
            this.logBuffer.Verbose("BusActivity: {0}", transportMessage.BusActivity);

            if (this.IsDeadLetterMessage())
            {
                this.HandleDeadLetterMessage(transportMessage);
                return;
            }

            if (transportMessage == null ||
                transportMessage.InnerMessage == null)
            {
                this.LogEmptyMessageWarning();
                this.WriteLog();
                return;
            }

            try
            {
                BusActivity.Current = transportMessage.BusActivity;

                this.LogMessageReceived(transportMessage);
                this.IncrementReceiveCounters(transportMessage);
                this.HandleMessage(transportMessage.InnerMessage);
                this.IncrementSuccessCounters(transportMessage.InnerMessage);
            }
            catch (Exception exception)
            {
                this.LogHandlingFailure(transportMessage.InnerMessage, exception);
                this.IncrementFailedCounters(transportMessage.InnerMessage);
                throw;
            }
            finally
            {
                BusActivity.Current = null;
                this.WriteLog();
            }
        }

        private bool IsDeadLetterMessage()
        {
            if (OperationContext.Current == null)
            {
                return false;
            }

            var currentPath = OperationContext.Current
                    .EndpointDispatcher.EndpointAddress.Uri.AbsolutePath;
            bool isDeadLetterEndpoint =
                currentPath.Equals(applicationEndpoints[BusEndpointType.TxDeadLetter].Uri.AbsolutePath, StringComparison.OrdinalIgnoreCase);

            return isDeadLetterEndpoint;
        }

        private void HandleDeadLetterMessage(TransportMessage transportMessage)
        {
            this.LogDeadLetterMessage(transportMessage.InnerMessage);
            this.IncrementDeadLetterCounters(transportMessage.InnerMessage);
            this.WriteLog();
        }

        private void HandleMessage(object message)
        {
            IEnumerable<Type> handlerTypes = this.handlerRegistry.GetHandlerTypes(message);
            if (handlerTypes.Any())
            {
                this.ExecuteHandlers(message, handlerTypes);
            }
            else
            {
                this.logBuffer.Warning(
                    "No handlers are registered for message type {0}. ",
                    message.GetType().FullName);
            }
        }

        private void ExecuteHandlers(object message, IEnumerable<Type> handlerTypes)
        {
            Type contextType = typeof(MessageHandlingContext<>).MakeGenericType(message.GetType());
            IMessageHandlingContext messageHandlingContext =
                (IMessageHandlingContext)Activator.CreateInstance(contextType, message, this.logBuffer);

            Type[] handleMethodParameterTypes = new Type[] { contextType };

            foreach (Type handlerType in handlerTypes)
            {
                this.logBuffer.Verbose("Invoking handler {0}.", handlerType.FullName);

                object handler = this.container.Resolve(handlerType);
                var handleMethod = handlerType.GetMethod(
                    HandleMethodName,
                    handleMethodParameterTypes);

                handleMethod.Invoke(handler, new object[] { messageHandlingContext });

                if (messageHandlingContext.StopProcessing)
                {
                    this.logBuffer.Verbose("Message handling was stopped by handler {0}.", handlerType.FullName);
                    break;
                }
            }
        }

        private void LogDeadLetterMessage(object message)
        {
            try
            {
                var properties =
                    OperationContext.Current.IncomingMessageProperties[MsmqMessageProperty.Name]
                    as MsmqMessageProperty;

                this.logBuffer.Warning(
                    "A message originally sent to '{0}' was recieved on the dead letter queue.\n" +
                    "DeliveryStatus: '{1}'\n" +
                    "DeliveryFailure: '{2}'\n" +
                    "{3}: '{4}'",
                    OperationContext.Current.IncomingMessageHeaders.To,
                    (properties == null) ? null : properties.DeliveryStatus,
                    (properties == null) ? null : properties.DeliveryFailure,
                    message.GetType().FullName,
                    message);
            }
            catch
            {
                // Swallow this exception, since we don't care what happens to this message.
            }
        }

        private void LogMessageReceived(TransportMessage transportMessage)
        {
            this.logBuffer.Verbose(
                "{0} was received at '{1}' from '{2}'.\nMessage: {3}",
                transportMessage.InnerMessage.GetType().FullName,
                (OperationContext.Current == null) ? null : OperationContext.Current.IncomingMessageHeaders.To,
                transportMessage.Originator,
                transportMessage.InnerMessage);
        }

        private void LogEmptyMessageWarning()
        {
            this.logBuffer.Warning(
                "An empty transport message was received at '{0}' and discarded.",
                (OperationContext.Current == null) ? null : OperationContext.Current.IncomingMessageHeaders.To);
        }

        private void LogHandlingFailure(object message, Exception exception)
        {
            if (message.GetType().IsTransactional())
            {
                this.LogTransactionalHandlingFailure(exception);
            }
            else
            {
                this.logBuffer.Warning(
                    "Message handling failed for a non-transactional message.  The message will be discarded.");
                this.logBuffer.Warning(exception);
            }
        }

        private void LogTransactionalHandlingFailure(Exception exception)
        {
            int currentAttemptNumber;
            if (!this.TryGetCurrentAttemptNumber(out currentAttemptNumber))
            {
                this.logBuffer.Warning("Could not retrieve handling attempt count from the operation context.");
            }

            this.logBuffer.Warning(
                "Message handling failed for a transactional message on attempt {0}. The transaction will be aborted.",
                currentAttemptNumber);

            if (currentAttemptNumber >= BusReceiverBinding.MaxAttempts)
            {
                this.logBuffer.Error("The message will be returned to the sender's dead letter queue.");
                this.logBuffer.Error(exception);
            }
            else
            {
                this.logBuffer.Warning("The message will be retried.");
                this.logBuffer.Warning(exception);
            }
        }

        private bool TryGetCurrentAttemptNumber(out int currentAttemptNumber)
        {
            if (OperationContext.Current == null)
            {
                currentAttemptNumber = -1;
                return false;
            }

            var messageProperty = OperationContext.Current
                .IncomingMessageProperties[MsmqMessageProperty.Name]
                as MsmqMessageProperty;
            if (messageProperty != null)
            {
                // MoveCount is incremented on each move to/from a subqueue (e.g., to retry and back).
                currentAttemptNumber = (messageProperty.MoveCount % 2 == 0) ?
                    messageProperty.MoveCount / 2 + 1 :
                    (messageProperty.MoveCount + 1) / 2;

                return true;
            }

            currentAttemptNumber = -1;
            return false;
        }

        private void WriteLog()
        {
            this.logBuffer.FlushToLog(
                "Service bus message handling log",
                LogPriority.Message);
        }

        private void IncrementReceiveCounters(TransportMessage transportMessage)
        {
            try
            {
                long millisecondsSinceMessageSent = 0;
                if (transportMessage.SentAtUtc != DateTimeOffset.MinValue)
                {
                    millisecondsSinceMessageSent = this.GetMillisecondsSince(
                        transportMessage.SentAtUtc);
                }

                long millisecondsSinceActivityStarted = 0;
                if (transportMessage.BusActivity != null &&
                    transportMessage.BusActivity.StartedAtUtc != DateTimeOffset.MinValue)
                {
                    millisecondsSinceActivityStarted = this.GetMillisecondsSince(
                        transportMessage.BusActivity.StartedAtUtc);
                }

                var instance = transportMessage.InnerMessage.GetType().FullName;

                ServiceBusCounters.IncrementReceived(
                    instance,
                    millisecondsSinceMessageSent,
                    millisecondsSinceActivityStarted);
            }
            catch (Exception exception)
            {
                this.logBuffer.Verbose(exception);
            }
        }

        private long GetMillisecondsSince(DateTimeOffset utcValue)
        {
            // Max accounts for the fact that utcValue may be in the future 
            // due to clock differences across machines.
            return Math.Max(
                0,
                Convert.ToInt64(DateTimeOffset.UtcNow.Subtract(utcValue).TotalMilliseconds));
        }

        private void IncrementSuccessCounters(object message)
        {
            try
            {
                ServiceBusCounters.IncrementHandled(message.GetType().FullName);
            }
            catch (Exception exception)
            {
                this.logBuffer.Verbose(exception);
            }
        }

        private void IncrementFailedCounters(object message)
        {
            try
            {
                ServiceBusCounters.IncrementFailed(message.GetType().FullName);
            }
            catch (Exception exception)
            {
                this.logBuffer.Verbose(exception);
            }
        }

        private void IncrementDeadLetterCounters(object message)
        {
            try
            {
                ServiceBusCounters.IncrementDeadLettered(message.GetType().FullName);
            }
            catch (Exception exception)
            {
                this.logBuffer.Verbose(exception);
            }
        }
    }
}
