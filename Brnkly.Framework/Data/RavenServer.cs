using System.Collections.ObjectModel;

namespace Brnkly.Framework.Data
{
    public class RavenServer
    {
        public string Name { get; private set; }
        public bool AllowReads { get; private set; }
        public bool AllowWrites { get; private set; }
        public Collection<RavenReplicationDestination> ReplicationDestinations { get; private set; }

        public RavenServer(string name, bool allowReads, bool allowWrites)
        {
            this.Name = name;
            this.AllowReads = allowReads;
            this.AllowWrites = allowWrites;
            this.ReplicationDestinations = new Collection<RavenReplicationDestination>();
        }
    }
}
