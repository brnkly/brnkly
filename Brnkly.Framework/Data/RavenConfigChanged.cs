using System;
using Brnkly.Framework.ServiceBus;

namespace Brnkly.Framework.Data
{
    public class RavenConfigChanged : Message
    {
        public static bool IsTransactional = false;
        public static TimeSpan TimeToLive = TimeSpan.FromSeconds(30);

        public string NewRavenConfigJson { get; set; }

        public override string ToString()
        {
            return this.NewRavenConfigJson;
        }
    }
}
