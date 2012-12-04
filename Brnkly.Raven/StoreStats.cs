using System.Collections.ObjectModel;
using Raven.Abstractions.Replication;

namespace Brnkly.Raven
{
    public class StoreStats
    {
        public string Name { get; set; }
        public Collection<ReplicationStatisticsTemp> Replication { get; set; }
        public Collection<IndexingStatistics> Indexing { get; set; }

        public StoreStats()
        {
            this.Replication = new Collection<ReplicationStatisticsTemp>();
            this.Indexing = new Collection<IndexingStatistics>();
        }
    }
}
