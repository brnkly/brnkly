using System;
using System.Collections.Generic;

// Temp override of raven types, until these changes are released to NuGet.

namespace Raven.Abstractions.Replication
{
    public class ReplicationStatisticsTemp
    {
        public string Self { get; set; }
        public Guid MostRecentDocumentEtag { get; set; }
        public Guid MostRecentAttachmentEtag { get; set; }
        public List<DestinationStatsTemp> Stats { get; set; }

        public ReplicationStatisticsTemp()
        {
            Stats = new List<DestinationStatsTemp>();
        }
    }

    public class DestinationStatsTemp
    {
        public int FailureCountInternal = 0;
        public string Url { get; set; }
        public DateTime? LastHeartbeatReceived { get; set; }
        public Guid? LastEtagCheckedForReplication { get; set; }
        public Guid? LastReplicatedEtag { get; set; }
        public DateTime? LastReplicatedLastModified { get; set; }
        public DateTime? LastSuccessTimestamp { get; set; }
        public DateTime? LastFailureTimestamp { get; set; }
        public int FailureCount { get { return FailureCountInternal; } }
        public string LastError { get; set; }
    }
}
