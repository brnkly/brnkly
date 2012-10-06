
namespace Brnkly.Framework.Administration.Models
{
    public class RavenReplicationDestinationEditModel
    {
        public string Name { get; set; }
        public TrackedChange<bool> Enabled { get; set; }
        public TrackedChange<bool> IsTransitive { get; set; }

        public RavenReplicationDestinationEditModel()
        {
            this.Enabled = new TrackedChange<bool>(false);
            this.IsTransitive = new TrackedChange<bool>(false);
        }

        internal void MarkPendingChanges(RavenReplicationDestinationEditModel compareTo)
        {
            if (compareTo == null)
            {
                if (this.Enabled.Value)
                {
                    this.Enabled.PendingChange = PendingChangeType.Changed;
                }

                if (this.IsTransitive.Value)
                {
                    this.IsTransitive.PendingChange = PendingChangeType.Changed;
                }
            }
            else
            {
                this.Enabled.MarkPendingChange(compareTo.Enabled.Value);
                this.IsTransitive.MarkPendingChange(compareTo.IsTransitive.Value);
            }
        }
    }
}
