﻿using System.Diagnostics;
using Brnkly.Framework.Instrumentation;

namespace Brnkly.Framework.ServiceBus.Core
{
    internal static partial class ServiceBusCounters
    {
        internal static void Create()
        {
            if (PerformanceCounterCategory.Exists(CategoryName))
            {
                PerformanceCounterCategory.Delete(CategoryName);
            }

            PerformanceCounterCategory.Create(
                CategoryName,
                CategoryHelp,
                CategoryType,
                GetCounterCreationDataCollection());
        }

        private static CounterCreationDataCollection GetCounterCreationDataCollection()
        {
            var dataCollection = new CounterCreationDataCollection()
            {
                Counters.Handled.Data,
                Counters.HandledPerSecond.Data,
                Counters.Failed.Data,
                Counters.FailedPerSecond.Data,
                Counters.DeadLetters.Data,
                Counters.DeadLettersPerSecond.Data,
                Counters.Sent.Data,
                Counters.SentPerSecond.Data,
                Counters.MillisecondsSinceMessageSent.Data,
                Counters.MillisecondsSinceActivityStarted.Data,
            };

            return dataCollection;
        }

        private static class Counters
        {
            public static MultiInstanceCounter Handled =
                new MultiInstanceCounter(
                    CategoryName,
                    new CounterCreationData(
                        "Handled",
                        "Number of messages successfully handled.",
                        PerformanceCounterType.NumberOfItems32));

            public static MultiInstanceCounter HandledPerSecond =
                new MultiInstanceCounter(
                    CategoryName,
                    new CounterCreationData(
                        "Handled/sec",
                        "Number of messages successfully handled in the past second.",
                        PerformanceCounterType.RateOfCountsPerSecond32));

            public static MultiInstanceCounter Failed =
                new MultiInstanceCounter(
                    CategoryName,
                     new CounterCreationData(
                        "Failed",
                        "Number of messages for which handling failed.",
                        PerformanceCounterType.NumberOfItems32));

            public static MultiInstanceCounter FailedPerSecond =
                new MultiInstanceCounter(
                    CategoryName,
                    new CounterCreationData(
                        "Failed/sec",
                        "Number of messages for which handling failed in the past second.",
                        PerformanceCounterType.RateOfCountsPerSecond32));

            public static MultiInstanceCounter DeadLetters =
                new MultiInstanceCounter(
                    CategoryName,
                    new CounterCreationData(
                        "Dead letters",
                        "Number of dead letters received.",
                        PerformanceCounterType.NumberOfItems32));

            public static MultiInstanceCounter DeadLettersPerSecond =
                new MultiInstanceCounter(
                    CategoryName,
                    new CounterCreationData(
                        "Dead letters received/sec",
                        "Number of dead letters received in the past second.",
                        PerformanceCounterType.RateOfCountsPerSecond32));

            public static MultiInstanceCounter Sent =
                new MultiInstanceCounter(
                    CategoryName,
                    new CounterCreationData(
                        "Sent",
                        "Number of messages sent.",
                        PerformanceCounterType.NumberOfItems32));

            public static MultiInstanceCounter SentPerSecond =
                new MultiInstanceCounter(
                    CategoryName,
                    new CounterCreationData(
                        "Sent/sec",
                        "Number of messages sent in the past second. (A single message publish can result in multiple messages sent, if there are multiple subscribers.)",
                        PerformanceCounterType.RateOfCountsPerSecond32));

            public static MultiInstanceCounter MillisecondsSinceMessageSent =
                new MultiInstanceCounter(
                    CategoryName,
                    new CounterCreationData(
                        "Milliseconds since message sent",
                        "Number of milliseconds from when the message was sent to when it was received (prior to handling). This indicates the total amount of time a message spent in transit and in the destination queue.",
                        PerformanceCounterType.NumberOfItems32));

            public static MultiInstanceCounter MillisecondsSinceActivityStarted =
                new MultiInstanceCounter(
                    CategoryName,
                    new CounterCreationData(
                        "Milliseconds since activity started",
                        "Number of milliseconds from when the first message in the activity was sent to when the current message was received (prior to handling). This indicates the total amount of time taken for all activity resulting from the original message.",
                        PerformanceCounterType.NumberOfItems32));
        }
    }
}
