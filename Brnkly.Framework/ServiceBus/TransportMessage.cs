using System;
using System.Runtime.Serialization;

namespace Brnkly.Framework.ServiceBus
{
    [DataContract(Namespace = "http://Brnkly/ServiceBus/2009/11")]
    public class TransportMessage
    {
        [DataMember]
        public Guid Id { get; set; }
        [DataMember]
        public BusActivity BusActivity { get; set; }
        [DataMember]
        public DateTimeOffset SentAtUtc { get; set; }
        [DataMember]
        public object InnerMessage { get; set; }
        [DataMember]
        public string Originator { get; set; }
    }
}
