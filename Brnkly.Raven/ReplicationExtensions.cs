using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Raven.Abstractions.Data;
using Raven.Abstractions.Replication;
using Raven.Client.Extensions;
using Raven.Json.Linq;

namespace Brnkly.Raven
{
    public static class ReplicationExtensions
    {
        public static void UpdateTracerDocuments(this RavenHelper raven, Store store)
        {
            var tasks = new List<Task>();
            foreach (var instance in store.Instances)
            {
                var task = new Task(() => raven.UpdateTracerDocument(store, instance));
                tasks.Add(task);
                task.Start();
            }

            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
        }

        private static void UpdateTracerDocument(this RavenHelper raven, Store store, Instance instance)
        {
            var tracerId = "brnkly/raven/tracers/" + instance.Url.Authority.Replace(":", "_");
            var docStore = raven.GetDocumentStore(instance.Url.GetServerRootUrl());
            using (var session = docStore.OpenSession(store.Name))
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var tracer = session.Load<Tracer>(tracerId) 
                    ?? new Tracer { UpdatedAtUtc = DateTimeOffset.UtcNow };
                tracer.UpdatedAtUtc = DateTimeOffset.UtcNow;
                session.Store(tracer, tracerId);
                session.SaveChanges();
            }
        }

        public static IEnumerable<string> UpdateReplicationDocuments(this RavenHelper raven, Store store)
        {
            var results = new Collection<string>();
            foreach (var instance in store.Instances)
            {
                if (!raven.TryUpdateReplicationDocument(store, instance))
                {
                    results.Add(
                        string.Format(
                        "Failed to update replication for store {0}, instance: {1}",
                        store.Name,
                        instance.Url));
                }
            }

            return results;
        }

        private static bool TryUpdateReplicationDocument(this RavenHelper raven, Store store, Instance instance)
        {
            try
            {
                raven.UpdateReplicationDocument(store, instance);
                raven.EnsureReplicationBundleIsActive(store, instance);
                return true;
            }
            catch (Exception exception)
            {
                //LogBuffer.Current.Error(
                //    "Failed to update replication for Raven database {0} on {1} due to the following exception:",
                //    store.Name,
                //    instance.Name);
                //LogBuffer.Current.Error(exception);
                return false;
            }
        }

        private static void UpdateReplicationDocument(this RavenHelper raven, Store store, Instance instance)
        {
            var docStore = raven.GetDocumentStore(instance.Url.GetServerRootUrl());
            docStore.DatabaseCommands.EnsureDatabaseExists(store.Name);
            using (var session = docStore.OpenSession(store.Name))
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var replicationDoc =
                    session.Load<ReplicationDocument>(Constants.RavenReplicationDestinations) ??
                    new ReplicationDocument();
                replicationDoc.Destinations = instance.Destinations
                    .Where(d => !d.Url.Equals(instance.Url.ToString(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
                session.Store(replicationDoc);
                session.SaveChanges();
                //LogBuffer.Current.Information(
                //    "Raven database {0} on {1} now replicating to: {2}",
                //    store.Name,
                //    instance.Name,
                //    string.Join(
                //        ", ",
                //        instance.ReplicationDestinations.Select(d => d.ServerName)));
            }
        }

        private static void EnsureReplicationBundleIsActive(this RavenHelper raven, Store store, Instance instance)
        {
            var docStore = raven.GetDocumentStore(instance.Url.GetServerRootUrl());
            docStore.DatabaseCommands.EnsureDatabaseExists(store.Name);
            using (var session = docStore.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var dbDoc = session.Load<RavenJObject>("Raven/Databases/" + store.Name);
                var settings = dbDoc["Settings"].Value<RavenJObject>();
                var activeBundles = settings[Constants.ActiveBundles] ?? "";
                var bundles = activeBundles.Value<string>()
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToArray();
                if (!bundles.Contains("Replication"))
                {
                    var newActiveBundles = string.Join(
                        ";",
                        bundles.Concat(new[] { "Replication" }).ToArray());
                    settings[Constants.ActiveBundles] = newActiveBundles;
                    session.Store(dbDoc);
                    session.SaveChanges();
                }
            }
        }
    }
}