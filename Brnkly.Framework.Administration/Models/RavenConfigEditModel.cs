using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Framework.Administration.Models
{
    public class RavenConfigEditModel
    {
        public string ErrorMessage { get; set; }
        public string Id { get; set; }
        public Guid? OriginalEtag { get; set; }
        public Collection<RavenStoreEditModel> Stores { get; set; }

        public RavenConfigEditModel()
        {
            this.Stores = new Collection<RavenStoreEditModel>();
        }

        internal RavenConfigEditModel AddServer(string name)
        {
            foreach (var store in this.Stores)
            {
                if (store.Servers.SelectByName(name) == null)
                {
                    store.Servers.Add(new RavenServerEditModel { Name = name, });
                }
            }

            return this;
        }

        internal RavenConfigEditModel DeleteServer(string name)
        {
            foreach (var store in this.Stores)
            {
                var server = store.Servers.SelectByName(name);
                if (server != null)
                {
                    store.Servers.Remove(server);
                }
            }

            return this;
        }

        internal RavenConfigEditModel Normalize()
        {
            var toRemove = new List<RavenStoreEditModel>();
            foreach (var store in this.Stores)
            {
                if (!StoreName.AllStoreNames.Contains(store.Name))
                {
                    toRemove.Add(store);
                }
            }

            foreach (var store in toRemove)
            {
                this.Stores.Remove(store);
            }

            foreach (var storeName in StoreName.AllStoreNames)
            {
                var store = this.GetOrAddStore(storeName);
            }

            this.Stores = this.Stores.OrderByName();

            return this;
        }

        internal IEnumerable<string> GetAllServerNames()
        {
            return this.Stores
                .SelectMany(store => store.Servers)
                .Select(server => server.Name)
                .Distinct()
                .OrderBy(name => name);
        }

        internal RavenConfigEditModel MarkPendingChanges(RavenConfigEditModel publishedConfig)
        {
            foreach (var store in this.Stores)
            {
                store.MarkPendingChanges(publishedConfig.Stores.SelectByName(store.Name));
            }

            foreach (var store in publishedConfig.Stores)
            {
                if (this.Stores.SelectByName(store.Name) == null)
                {
                    store.PendingChange = PendingChangeType.Deleted;
                    this.Stores.Add(store);
                }
            }

            this.Stores = this.Stores.OrderByName();
            return this;
        }

        internal RavenConfigEditModel RemovePendingDeletes()
        {
            var toRemove = new List<RavenStoreEditModel>();
            toRemove.AddRange(this.Stores.Where(s => s.PendingChange == PendingChangeType.Deleted));
            foreach (var store in toRemove)
            {
                this.Stores.Remove(store);
            }

            foreach (var store in this.Stores)
            {
                store.RemovePendingDeletes();
            }

            return this;
        }

        private RavenStoreEditModel GetOrAddStore(string storeName)
        {
            var store = this.Stores.SelectByName(storeName);
            if (store == null)
            {
                store = new RavenStoreEditModel { Name = storeName };
                this.Stores.Add(store);
            }

            foreach (var serverName in this.GetAllServerNames())
            {
                var server = this.GetOrAddServer(store, serverName);
            }

            store.Servers = store.Servers.OrderByName();
            return store;
        }

        private RavenServerEditModel GetOrAddServer(RavenStoreEditModel store, string serverName)
        {
            var server = store.Servers.SelectByName(serverName);
            if (server == null)
            {
                server = new RavenServerEditModel { Name = serverName };
                store.Servers.Add(server);
            }

            this.EnsureAllReplicationDestinationsExist(server);

            return server;
        }

        private void EnsureAllReplicationDestinationsExist(RavenServerEditModel server)
        {
            foreach (var serverName in this.GetAllServerNames())
            {
                if (server.ReplicationDestinations.SelectByName(serverName) == null)
                {
                    server.ReplicationDestinations.Add(
                        new RavenReplicationDestinationEditModel
                        {
                            Name = serverName
                        });
                }
            }
            server.ReplicationDestinations = server.ReplicationDestinations.OrderByName();
        }
    }
}
