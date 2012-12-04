using System;
using System.Collections.ObjectModel;
using Raven.Abstractions.Replication;

namespace Brnkly.Raven
{
    public class Instance
    {
        public Uri Url { get; internal set; }
        public bool AllowReads { get; internal set; }
        public bool AllowWrites { get; internal set; }
        public Collection<ReplicationDestination> Destinations { get; private set; }

        public Instance()
        {
            this.Destinations = new Collection<ReplicationDestination>();
        }

        public override string ToString()
        {
            return string.Format(
                "{0}: AllowReads={1}, AllowWrites={2}, {3} destinations",
                Url,
                AllowReads,
                AllowWrites,
                Destinations.Count);
        }
    }
}
