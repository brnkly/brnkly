using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using System.Xml;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    public sealed class BusReceiverDataContractResolver : DataContractResolver
    {
        private static string[] NamespacesToLeaveAlone = new[] 
        { 
            "http://schemas.datacontract.org/2004/07/System",
        };

        public override bool TryResolveType(
            Type type,
            Type declaredType,
            DataContractResolver knownTypeResolver,
            out XmlDictionaryString typeName,
            out XmlDictionaryString typeNamespace)
        {
            if (!MessageType.AllKnownTypes.Contains(type))
            {
                typeName = null;
                typeNamespace = null;
                return false;
            }

            knownTypeResolver.TryResolveType(
                type, declaredType, knownTypeResolver, out typeName, out typeNamespace);

            if (this.NamespaceShouldBeReplaced(typeNamespace))
            {
                typeName = new XmlDictionaryString(XmlDictionary.Empty, type.Name, 0);
                typeNamespace = new XmlDictionaryString(XmlDictionary.Empty, type.Namespace, 0);
            }

            return true;
        }

        private bool NamespaceShouldBeReplaced(XmlDictionaryString typeNamespace)
        {
            if (NamespacesToLeaveAlone.Any(
                ns => typeNamespace.Value.StartsWith(ns, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        }

        public override Type ResolveName(
            string typeName,
            string typeNamespace,
            Type declaredType,
            DataContractResolver knownTypeResolver)
        {
            Type type =
                GetTypeMatchingNameAndNamespace(typeName, typeNamespace) ??
                GetTypeMatchingName(typeName) ??
                knownTypeResolver.ResolveName(
                    typeName,
                    typeNamespace,
                    declaredType,
                    knownTypeResolver);

            return type;
        }

        private static Type GetTypeMatchingNameAndNamespace(string typeName, string typeNamespace)
        {
            var type = MessageType.AllKnownTypes.Where(
                t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                     t.Namespace.Equals(typeNamespace, StringComparison.OrdinalIgnoreCase))
                .SingleOrDefault();

            return type;
        }

        private static Type GetTypeMatchingName(string typeName)
        {
            var type = MessageType.AllKnownTypes.Where(
                t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                .SingleOrDefault();

            return type;
        }

        internal static void AddToEndpoints(IEnumerable<ServiceEndpoint> endpoints)
        {
            var operations = endpoints
                .Select(endpoint => endpoint.Contract)
                .SelectMany(contract => contract.Operations);

            foreach (var operation in operations)
            {
                var behavior = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();
                if (behavior == null)
                {
                    behavior = new DataContractSerializerOperationBehavior(operation);
                    operation.Behaviors.Add(behavior);
                }

                behavior.DataContractResolver = new BusReceiverDataContractResolver();
            }
        }
    }
}
