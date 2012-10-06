using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Framework
{
    internal class MessageType
    {
        public static ReadOnlyCollection<Type> AllKnownTypes;
        private static Collection<Type> knownTypes = new Collection<Type>();

        // Brnkly.Framework messages.
        public static Type RavenConfigChanged = Register("Brnkly.Framework.Data.RavenConfigChanged, Brnkly.Framework");
        public static Type RavenDocumentChanged = Register("Brnkly.Framework.Data.RavenDocumentChanged, Brnkly.Framework");
        public static Type Ping = Register("Brnkly.Framework.ServiceBus.SelfTest.PingMessage, Brnkly.Framework");
        public static Type PingReply = Register("Brnkly.Framework.ServiceBus.SelfTest.PingReplyMessage, Brnkly.Framework");
        public static Type TxPing = Register("Brnkly.Framework.ServiceBus.SelfTest.TxPingMessage, Brnkly.Framework");
        public static Type TxPingReply = Register("Brnkly.Framework.ServiceBus.SelfTest.TxPingReplyMessage, Brnkly.Framework");

        static MessageType()
        {
            AllKnownTypes = knownTypes.ToList().AsReadOnly();
        }

        private static Type Register(string assemblyQualifiedName)
        {
            var type = Type.GetType(assemblyQualifiedName, throwOnError: false, ignoreCase: true);
            if (type != null)
            {
                knownTypes.Add(type);
            }

            return type;
        }
    }
}
