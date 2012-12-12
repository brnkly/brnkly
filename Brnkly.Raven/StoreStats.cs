using System.Collections.ObjectModel;
using Raven.Abstractions.Replication;

namespace Brnkly.Raven
{
    public class StoreStats
    {
        public string Name { get; set; }
        public Collection<ReplicationStatistics> Replication { get; set; }
        public Collection<IndexingStatistics> Indexing { get; set; }

        public StoreStats()
        {
            this.Replication = new Collection<ReplicationStatistics>();
            this.Indexing = new Collection<IndexingStatistics>();
        }
    }
}
