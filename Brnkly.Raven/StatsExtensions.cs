using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Raven.Abstractions.Data;
using Raven.Abstractions.Indexing;
using Raven.Abstractions.Replication;
using Raven.Client.Connection.Async;
using Raven.Json.Linq;
using RavenImports = Raven.Imports.Newtonsoft.Json;

namespace Brnkly.Raven
{
    public static class StatsExtensions
    {
        private class InstanceStatsHolder
        {
            public Uri Url { get; set; }
            public DatabaseStatistics DatabaseStats { get; set; }
            public ReplicationStatistics ReplicationStats { get; set; }
            public Dictionary<string, int> IndexHashCodes { get; set; }

            public InstanceStatsHolder()
            {
                this.IndexHashCodes = new Dictionary<string, int>();
            }
        }

        public static async Task<StoreStats> GetStats(this RavenHelper raven, Store store)
        {
            var dbStats = new Dictionary<Uri, Task<DatabaseStatistics>>();
            var replicationStats = new Dictionary<Uri, Task<RavenJToken>>();
            var indexes = new Dictionary<Uri, Task<IndexDefinition[]>>();
            raven.RequestData(store, dbStats, replicationStats, indexes);
            var responses = await GatherResponses(store, dbStats, replicationStats, indexes);
            var storeStats = CreateStats(store, responses);
            return storeStats;
        }

        private static void RequestData(
            this RavenHelper raven,
            Store store,
            Dictionary<Uri, Task<DatabaseStatistics>> dbStats,
            Dictionary<Uri, Task<RavenJToken>> replicationStats,
            Dictionary<Uri, Task<IndexDefinition[]>> indexes)
        {
            foreach (var instance in store.Instances)
            {
                var docStore = raven.GetDocumentStore(instance.Url.GetServerRootUrl());

                using (var session = docStore.OpenAsyncSession())
                {
                    dbStats.Add(
                        instance.Url,
                        session.Advanced.DocumentStore.AsyncDatabaseCommands.ForDatabase(store.Name)
                            .GetStatisticsAsync());

                    indexes.Add(
                        instance.Url,
                        session.Advanced.DocumentStore.AsyncDatabaseCommands.ForDatabase(store.Name)
                            .GetIndexesAsync(0, 255));

                    var asyncServerClient = session.Advanced.DocumentStore.AsyncDatabaseCommands
                        .ForDatabase(store.Name) as AsyncServerClient;
                    replicationStats.Add(
                        instance.Url,
                        asyncServerClient
                            .CreateRequest("/replication/info?noCache=" + Guid.NewGuid(), "GET")
                            .ReadResponseJsonAsync());
                }
            }
        }

        private static async Task<List<InstanceStatsHolder>> GatherResponses(
            Store store,
            Dictionary<Uri, Task<DatabaseStatistics>> dbStats,
            Dictionary<Uri, Task<RavenJToken>> replicationStats,
            Dictionary<Uri, Task<IndexDefinition[]>> indexes)
        {
            var responses = new List<InstanceStatsHolder>();

            foreach (var instance in store.Instances)
            {
                var response = new InstanceStatsHolder { Url = instance.Url };
                try
                {
                    response.DatabaseStats = await dbStats[instance.Url];

                    var indexData = await indexes[instance.Url];
                    response.IndexHashCodes = indexData.ToDictionary(
                        i => i.Name,
                        i => i.GetHashCode());

                    var data = await replicationStats[instance.Url];
                    response.ReplicationStats = new RavenImports.JsonSerializer()
                        .Deserialize<ReplicationStatistics>(new RavenJTokenReader(data));
                }
                catch (Exception exception)
                {
                    if (exception.IsFatal()) { throw; }

                    // TODO: Log exception.

                    response.DatabaseStats = response.DatabaseStats
                        ?? new DatabaseStatistics();
                    response.ReplicationStats = response.ReplicationStats
                        ?? new ReplicationStatistics { Self = response.Url.ToString() };
                }

                responses.Add(response);
            }

            return responses;
        }

        private static StoreStats CreateStats(Store store, IEnumerable<InstanceStatsHolder> responses)
        {
            var storeStats = new StoreStats { Name = store.Name };

            AddStoreIndexStats(storeStats, responses);

            foreach (var instance in store.Instances)
            {
                var response = responses.Where(r => r.Url == instance.Url).FirstOrDefault();
                AddInstanceIndexStats(storeStats, response);
                storeStats.Replication.Add(response.ReplicationStats);
            }

            return storeStats;
        }

        private static void AddStoreIndexStats(StoreStats stats, IEnumerable<InstanceStatsHolder> responses)
        {
            var allIndexNames = responses
                .Where(r => r.DatabaseStats != null && r.DatabaseStats.Indexes != null)
                .SelectMany(r => r.DatabaseStats.Indexes)
                .Select(idx => idx.Name)
                .Distinct()
                .OrderBy(name => name);
            foreach (var indexName in allIndexNames)
            {
                stats.Indexing.Add(new IndexingStatistics { Name = indexName });
            }
        }

        private static void AddInstanceIndexStats(StoreStats stats, InstanceStatsHolder response)
        {
            foreach (var index in stats.Indexing)
            {
                IndexStats indexStats = null;

                if (response.DatabaseStats != null && 
                    response.DatabaseStats.Indexes != null)
                {
                    indexStats = response.DatabaseStats.Indexes
                        .Where(idx => idx.Name == index.Name)
                        .FirstOrDefault();
                }

                var instanceIdxStatus = new InstanceIndexStats() { Url = response.Url };
                if (indexStats == null)
                {
                    instanceIdxStatus.Exists = false;
                    instanceIdxStatus.IsStale = true;
                }
                else
                {
                    instanceIdxStatus.Exists = true;
                    if (response.DatabaseStats != null &&
                        response.DatabaseStats.Indexes != null)
                    {
                        instanceIdxStatus.IsStale = response.DatabaseStats.StaleIndexes.Contains(index.Name);
                    }
                    else
                    {
                        instanceIdxStatus.IsStale = true;
                    }

                    if (response.IndexHashCodes != null)
                    {
                        int hashCode;
                        if (response.IndexHashCodes.TryGetValue(indexStats.Name, out hashCode))
                        {
                            instanceIdxStatus.HashCode = hashCode;
                        }
                    }

                    instanceIdxStatus.CopyFrom(indexStats);
                }

                index.Instances.Add(instanceIdxStatus);
            }
        }
    }
}
