using System;

namespace Brnkly.Framework.ServiceBus.Core
{
    internal static class MessageTypeExtensions
    {
        public static TimeSpan TimeToLive(this Type messageType)
        {
            return messageType.GetStaticFieldValue<TimeSpan>("TimeToLive", TimeSpan.FromMinutes(60));
        }

        public static bool IsTransactional(this Type messageType)
        {
            return messageType.GetStaticFieldValue<bool>("IsTransactional", false);
        }

        public static BusEndpointType GetRecieverEndpointType(this Type messageType)
        {
            if (messageType != null && messageType.IsTransactional())
            {
                return BusEndpointType.Tx;
            }

            return BusEndpointType.NonTx;
        }

        private static T GetStaticFieldValue<T>(this Type messageType, string fieldName, T defaultValue)
        {
            var field = messageType.GetField(fieldName);
            if (field == null ||
                field.FieldType != typeof(T))
            {
                return defaultValue;
            }

            return (T)field.GetValue(null);
        }
    }
}
