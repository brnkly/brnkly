using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Brnkly.Framework.ServiceBus.Core
{
    public sealed class MessageHandlerRegistry
    {
        private const string MessageHandlerInterface = "Brnkly.Framework.ServiceBus.IMessageHandler`1";

        internal static MessageHandlerRegistry Instance { get; private set; }
        private static readonly IEnumerable<Type> NoTypes = Enumerable.Empty<Type>();
        private ConcurrentDictionary<Type, ConcurrentBag<Type>> registry =
            new ConcurrentDictionary<Type, ConcurrentBag<Type>>();

        static MessageHandlerRegistry()
        {
            Instance = new MessageHandlerRegistry();
        }

        private MessageHandlerRegistry() { }

        public static void RegisterAllHandlerTypes()
        {
            var handlerTypes = Instance.FindHandlerTypesInAssemblies();
            Instance.Register(handlerTypes);
        }

        public IEnumerable<Type> GetHandlerTypes(object message)
        {
            ConcurrentBag<Type> handlerTypes;
            this.registry.TryGetValue(message.GetType(), out handlerTypes);
            return handlerTypes ?? NoTypes;
        }

        private List<Type> FindHandlerTypesInAssemblies()
        {
            return AssemblyHelper.GetAssemblies()
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => this.IsHandlerType(type))
                .ToList();
        }

        private bool IsHandlerType(Type type)
        {
            if (type.IsAbstract)
            {
                return false;
            }

            var interfaces = type.GetInterfaces();

            return interfaces.Any(
                i => i.FullName != null && i.FullName.StartsWith(MessageHandlerInterface, StringComparison.Ordinal));
        }

        private void Register(IEnumerable<Type> handlerTypes)
        {
            lock (this.registry)
            {
                foreach (var handlerType in handlerTypes)
                {
                    var interfaces = handlerType.GetInterfaces()
                        .Where(i => i.FullName.StartsWith(MessageHandlerInterface, StringComparison.Ordinal));

                    foreach (var handlerInterface in interfaces)
                    {
                        var messageType = handlerInterface.GetGenericArguments().First();

                        ConcurrentBag<Type> existingHandlerTypes;
                        if (this.registry.TryGetValue(messageType, out existingHandlerTypes))
                        {
                            if (!existingHandlerTypes.Contains(handlerType))
                            {
                                existingHandlerTypes.Add(handlerType);
                            }
                        }
                        else
                        {
                            this.registry[messageType] =
                                new ConcurrentBag<Type>(new Type[] { handlerType });
                        }
                    }
                }
            }
        }
    }
}
