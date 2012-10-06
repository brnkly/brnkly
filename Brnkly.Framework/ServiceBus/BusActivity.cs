using System;
using System.Runtime.Serialization;

namespace Brnkly.Framework.ServiceBus
{
    [DataContract(Namespace = "http://Brnkly/ServiceBus/2009/11")]
    public class BusActivity
    {
        [ThreadStatic]
        private static BusActivity current;

        public static BusActivity Current
        {
            get { return current; }
            set { current = value; }
        }

        [DataMember]
        public Guid Id { get; private set; }
        [DataMember]
        public DateTimeOffset StartedAtUtc { get; private set; }

        public BusActivity()
        {
            this.Id = Guid.NewGuid();
            this.StartedAtUtc = DateTimeOffset.UtcNow;
        }

        public override string ToString()
        {
            return string.Format(
                "Id='{0}', StartedAtUtc='{1:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'fffffff}'",
                this.Id,
                this.StartedAtUtc);
        }
    }
}
