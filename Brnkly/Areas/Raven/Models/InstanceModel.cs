using System.Collections.ObjectModel;
using Raven.Abstractions.Replication;

namespace Brnkly.Raven.Admin.Models
{
    public class InstanceModel
    {
        public string Url { get; set; }
        public bool AllowReads { get; set; }
        public bool AllowWrites { get; set; }
        public Collection<ReplicationDestination> Destinations { get; set; }

        public InstanceModel()
        {
			this.Destinations = new Collection<ReplicationDestination>();
        }

        public override string ToString()
        {
            return string.Format(
                "{0}, AllowReads={1}, AllowWrites={2}",
                Url,
                AllowReads,
                AllowWrites);
        }
    }
}
