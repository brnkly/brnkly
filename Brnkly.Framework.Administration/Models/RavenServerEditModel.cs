using System.Collections.ObjectModel;

namespace Brnkly.Framework.Administration.Models
{
    public class RavenServerEditModel
    {
        public string Name { get; set; }
        public PendingChangeType PendingChange { get; set; }
        public TrackedChange<bool> AllowReads { get; set; }
        public TrackedChange<bool> AllowWrites { get; set; }
        public Collection<RavenReplicationDestinationEditModel> ReplicationDestinations { get; set; }

        public RavenServerEditModel()
        {
            this.AllowReads = new TrackedChange<bool>();
            this.AllowWrites = new TrackedChange<bool>();
            this.ReplicationDestinations = new Collection<RavenReplicationDestinationEditModel>();
        }

        internal void MarkPendingChanges(RavenServerEditModel compareTo)
        {
            if (compareTo == null)
            {
                this.PendingChange = PendingChangeType.Added;
                return;
            }

            this.AllowReads.MarkPendingChange(compareTo.AllowReads.Value);
            this.AllowWrites.MarkPendingChange(compareTo.AllowWrites.Value);

            foreach (var destination in this.ReplicationDestinations)
            {
                var oldValue =
                    compareTo.ReplicationDestinations.SelectByName(destination.Name);
                destination.MarkPendingChanges(oldValue);
            }
        }
    }
}
