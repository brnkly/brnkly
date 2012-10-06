using System.Diagnostics;

namespace Brnkly.Framework.ServiceBus.Core
{
    internal static partial class ServiceBusCounters
    {
        private const string CategoryName = "Brnkly Service Bus";
        private const string CategoryHelp = "Performance counters for the service bus. This is a multi-instance category, with one instance per message type.";
        private static readonly PerformanceCounterCategoryType CategoryType = PerformanceCounterCategoryType.MultiInstance;
        private const string TotalInstanceName = "_Total_";

        private static readonly IServiceBusCountersImplementation CounterImplementation;

        static ServiceBusCounters()
        {
            if (PlatformApplication.Current == PlatformApplication.UnknownApplication)
            {
                CounterImplementation = new TestImplementation();
            }
            else
            {
                CounterImplementation = new Implementation();
            }
        }

        internal static void IncrementHandled(string instanceName)
        {
            CounterImplementation.IncrementHandled(instanceName);
        }

        internal static void IncrementFailed(string instanceName)
        {
            CounterImplementation.IncrementFailed(instanceName);
        }

        internal static void IncrementDeadLettered(string instanceName)
        {
            CounterImplementation.IncrementDeadLettered(instanceName);
        }

        internal static void IncrementSent(string instanceName)
        {
            CounterImplementation.IncrementSent(instanceName);
        }

        internal static void IncrementReceived(
            string instanceName,
            long millisecondsSinceMessageSent,
            long millisecondsSinceActivityStarted)
        {
            CounterImplementation.IncrementReceived(
                instanceName,
                millisecondsSinceMessageSent,
                millisecondsSinceActivityStarted);
        }

        private interface IServiceBusCountersImplementation
        {
            void IncrementHandled(string instanceName);
            void IncrementFailed(string instanceName);
            void IncrementDeadLettered(string instanceName);
            void IncrementSent(string instanceName);
            void IncrementReceived(
                string instanceName,
                long millisecondsSinceMessageSent,
                long millisecondsSinceActivityStarted);
        }

        private class Implementation : IServiceBusCountersImplementation
        {
            void IServiceBusCountersImplementation.IncrementHandled(string instanceName)
            {
                Counters.Handled.GetInstance(instanceName).Increment();
                Counters.Handled.GetInstance(TotalInstanceName).Increment();

                Counters.HandledPerSecond.GetInstance(instanceName).Increment();
                Counters.HandledPerSecond.GetInstance(TotalInstanceName).Increment();
            }

            void IServiceBusCountersImplementation.IncrementFailed(string instanceName)
            {
                Counters.Failed.GetInstance(instanceName).Increment();
                Counters.Failed.GetInstance(TotalInstanceName).Increment();

                Counters.FailedPerSecond.GetInstance(instanceName).Increment();
                Counters.FailedPerSecond.GetInstance(TotalInstanceName).Increment();
            }

            void IServiceBusCountersImplementation.IncrementDeadLettered(string instanceName)
            {
                Counters.DeadLetters.GetInstance(instanceName).Increment();
                Counters.DeadLetters.GetInstance(TotalInstanceName).Increment();

                Counters.DeadLettersPerSecond.GetInstance(instanceName).Increment();
                Counters.DeadLettersPerSecond.GetInstance(TotalInstanceName).Increment();
            }

            void IServiceBusCountersImplementation.IncrementSent(string instanceName)
            {
                Counters.Sent.GetInstance(instanceName).Increment();
                Counters.Sent.GetInstance(TotalInstanceName).Increment();

                Counters.SentPerSecond.GetInstance(instanceName).Increment();
                Counters.SentPerSecond.GetInstance(TotalInstanceName).Increment();
            }

            void IServiceBusCountersImplementation.IncrementReceived(
                string instanceName,
                long millisecondsSinceMessageSent,
                long millisecondsSinceActivityStarted)
            {
                Counters.MillisecondsSinceMessageSent.GetInstance(instanceName).RawValue = millisecondsSinceMessageSent;
                Counters.MillisecondsSinceMessageSent.GetInstance(TotalInstanceName).RawValue = millisecondsSinceMessageSent;

                Counters.MillisecondsSinceActivityStarted.GetInstance(instanceName).RawValue = millisecondsSinceActivityStarted;
                Counters.MillisecondsSinceActivityStarted.GetInstance(TotalInstanceName).RawValue = millisecondsSinceActivityStarted;
            }
        }

        private class TestImplementation : IServiceBusCountersImplementation
        {
            void IServiceBusCountersImplementation.IncrementHandled(string instanceName)
            {
            }

            void IServiceBusCountersImplementation.IncrementFailed(string instanceName)
            {
            }

            void IServiceBusCountersImplementation.IncrementDeadLettered(string instanceName)
            {
            }

            void IServiceBusCountersImplementation.IncrementSent(string instanceName)
            {
            }

            void IServiceBusCountersImplementation.IncrementReceived(
                string instanceName,
                long millisecondsSinceMessageSent,
                long millisecondsSinceActivityStarted)
            {
            }
        }
    }
}