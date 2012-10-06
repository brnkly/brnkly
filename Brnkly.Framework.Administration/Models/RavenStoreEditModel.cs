using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Framework.Administration.Models
{
    public class RavenStoreEditModel
    {
        public string Name { get; set; }
        public PendingChangeType PendingChange { get; set; }
        public Collection<RavenServerEditModel> Servers { get; set; }

        public RavenStoreEditModel()
        {
            this.Servers = new Collection<RavenServerEditModel>();
        }

        internal void MarkPendingChanges(RavenStoreEditModel compareTo)
        {
            if (compareTo == null)
            {
                this.PendingChange = PendingChangeType.Added;
            }
            else
            {
                foreach (var server in this.Servers)
                {
                    server.MarkPendingChanges(
                        compareTo.Servers.SelectByName(server.Name));
                }

                foreach (var server in compareTo.Servers)
                {
                    if (this.Servers.SelectByName(server.Name) == null)
                    {
                        server.PendingChange = PendingChangeType.Deleted;
                        this.Servers.Add(server);
                    }
                }
            }
        }

        internal void RemovePendingDeletes()
        {
            var toRemove = new List<RavenServerEditModel>();
            toRemove.AddRange(this.Servers.Where(s => s.PendingChange == PendingChangeType.Deleted));
            foreach (var server in toRemove)
            {
                this.Servers.Remove(server);
            }
        }
    }
}
