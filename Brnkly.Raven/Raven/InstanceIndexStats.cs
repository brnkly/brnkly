using System;
using Raven.Abstractions.Data;

namespace Brnkly.Raven
{
    public class InstanceIndexStats : IndexStats
    {
        public Uri Url { get; set; }
        public bool Exists { get; set; }
        public bool IsStale { get; set; }
        public int HashCode { get; set; }

        public void CopyFrom(IndexStats indexStats)
        {
            // TODO: Use automapper.
            this.IndexingAttempts = indexStats.IndexingAttempts;
            this.IndexingErrors = indexStats.IndexingErrors;
            this.IndexingSuccesses = indexStats.IndexingSuccesses;
            this.LastIndexedEtag = indexStats.LastIndexedEtag;
            this.LastIndexedTimestamp = indexStats.LastIndexedTimestamp;
            this.LastQueryTimestamp = indexStats.LastQueryTimestamp;
            this.LastReducedEtag = indexStats.LastReducedEtag;
            this.LastReducedTimestamp = indexStats.LastReducedTimestamp;
            this.Name = indexStats.Name;
            //this.Performance = indexStats.Performance;
            this.ReduceIndexingAttempts = indexStats.ReduceIndexingAttempts;
            this.ReduceIndexingErrors = indexStats.ReduceIndexingErrors;
            this.ReduceIndexingSuccesses = indexStats.ReduceIndexingSuccesses;
            this.TouchCount = indexStats.TouchCount;
        }
    }
}
