using Brnkly.Framework.Administration.Models;
using Brnkly.Framework.Data;
using Brnkly.Framework.Logging;
using Newtonsoft.Json;
using Raven.Abstractions.Data;
using Raven.Abstractions.Replication;
using Raven.Client.Connection;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;

namespace Brnkly.Framework.Administration.Controllers
{
    internal class RavenHelper
    {
        private const string DestinationsDocId = "Raven/Replication/Destinations";
        private const int RavenRequestTimeoutMilliseconds = 5000;

        private static readonly ConcurrentDictionary<string, DocumentStore> stores =
            new ConcurrentDictionary<string, DocumentStore>(StringComparer.OrdinalIgnoreCase);

        public static string GetJson(string serverName, string storeName, string path)
        {
            var token = GetRavenJToken(serverName, storeName, path);
            return token.ToString(Formatting.Indented);
        }

        public static RavenDatabaseStatusModel GetStatus(string serverName, string storeName)
        {
            RavenDatabaseStatusModel status = null;

            try
            {
                status = GetDatabaseStatus(serverName, storeName);
            }
            catch (WebException webException)
            {
                if (!TryHandleBadRequest(webException, serverName, storeName, out status))
                {
                    throw;
                }
            }
            catch (Exception exception)
            {
                status = new RavenDatabaseStatusModel { Error = exception, };
            }

            return status;
        }

        private static RavenDatabaseStatusModel GetDatabaseStatus(string serverName, string storeName)
        {
            var stats = GetSingleObject<DatabaseStatistics>(
                serverName, storeName, "stats");
            var replicationDoc = GetSingleObject<ReplicationDocument>(
                serverName, storeName, "docs/" + DestinationsDocId);

            var replicatingTo = new SortedDictionary<string, bool>();
            foreach (var destination in replicationDoc.Destinations)
            {
                replicatingTo.Add(
                    new Uri(destination.Url).Host,
                    destination.TransitiveReplicationBehavior ==
                        TransitiveReplicationOptions.Replicate);
            }

            return new RavenDatabaseStatusModel
            {
                Statistics = stats,
                ReplicatingTo = replicatingTo,
            };
        }

        private static bool TryHandleBadRequest(
            WebException webException,
            string serverName,
            string storeName,
            out RavenDatabaseStatusModel status)
        {
            status = null;

            if (webException.Status == WebExceptionStatus.ProtocolError)
            {
                var webResponse = (HttpWebResponse)webException.Response;
                if (webResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    var store = GetDocumentStore(serverName);
                    store.DatabaseCommands.EnsureDatabaseExists(storeName);
                    status = new RavenDatabaseStatusModel { DatabaseCreated = true };
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<string> UpdateReplicationDocuments(RavenConfig newConfig)
        {
            var results = new Collection<string>();
            foreach (var store in newConfig.Stores)
            {
                foreach (var server in store.Servers)
                {
                    if (!TryUpdateReplicationDocument(store, server))
                    {
                        results.Add(
                            string.Format(
                            "Update for store {0} on {1} failed.",
                            store.Name,
                            server.Name));
                    }
                }
            }

            return results;
        }

        private static bool TryUpdateReplicationDocument(RavenStore store, RavenServer server)
        {
            try
            {
                UpdateReplicationDocument(store, server);
                return true;
            }
            catch (Exception exception)
            {
                LogBuffer.Current.Error(
                    "Failed to update replication for Raven database {0} on {1} due to the following exception:",
                    store.Name,
                    server.Name);
                LogBuffer.Current.Error(exception);
                return false;
            }
        }

        private static void UpdateReplicationDocument(RavenStore store, RavenServer server)
        {
            var documentStore = GetDocumentStore(server.Name);
            documentStore.DatabaseCommands.EnsureDatabaseExists(store.Name);
            using (var session = documentStore.OpenSession(store.Name))
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var replicationDoc =
                    session.Load<ReplicationDocument>(DestinationsDocId) ??
                    new ReplicationDocument();
                ReplaceDestinations(replicationDoc, store.Name, server.ReplicationDestinations);
                session.Store(replicationDoc);
                session.SaveChanges();
                LogBuffer.Current.Information(
                    "Raven database {0} on {1} now replicating to: {2}",
                    store.Name,
                    server.Name,
                    string.Join(
                        ", ",
                        server.ReplicationDestinations.Select(d => d.ServerName)));
            }
        }

        private static void ReplaceDestinations(
            ReplicationDocument replicationDoc,
            string storeName,
            IEnumerable<RavenReplicationDestination> destinations)
        {
            replicationDoc.Destinations.Clear();

            foreach (var destination in destinations)
            {
                replicationDoc.Destinations.Add(
                    new ReplicationDestination
                    {
                        Url = GetDestinationUrl(destination.ServerName, storeName),
                        TransitiveReplicationBehavior = destination.IsTransitive ?
                            TransitiveReplicationOptions.Replicate :
                            TransitiveReplicationOptions.None
                    });
            }
        }

        private static string GetDestinationUrl(string serverName, string storeName)
        {
            return string.Format(BrnklyDocumentStore.StoreUrlFormat, serverName, storeName);
        }

        private static DocumentStore GetDocumentStore(string serverName)
        {
            DocumentStore store;
            if (stores.TryGetValue(serverName, out store))
            {
                return store;
            }

            var url = string.Format(BrnklyDocumentStore.ServerOnlyUrlFormat, serverName);
            store = new DocumentStore { Url = url, ResourceManagerId = Guid.NewGuid() };

            store.Initialize();

            store.JsonRequestFactory.ConfigureRequest +=
                new EventHandler<WebRequestEventArgs>(JsonRequestFactory_ConfigureRequest);

            stores.TryAdd(serverName, store);

            return store as DocumentStore;
        }

        private static void JsonRequestFactory_ConfigureRequest(object sender, WebRequestEventArgs e)
        {
            e.Request.Timeout = RavenRequestTimeoutMilliseconds;
        }

        private static T GetSingleObject<T>(string serverName, string storeName, string path)
        {
            var store = GetDocumentStore(serverName);
            var token = GetRavenJToken(serverName, storeName, path);
            return ((RavenJObject)token).Deserialize<T>(store.Conventions);
        }

        private static RavenJToken GetRavenJToken(string serverName, string storeName, string path)
        {
            var url = GetStoreUrl(serverName, storeName, path);

            using (var webClient = new WebClient())
            {
                webClient.UseDefaultCredentials = true;

                var response = webClient.DownloadString(url);
                return RavenJToken.Parse(response);
            }
        }

        public static TModel Deserialize<TModel>(string input, DocumentConvention convention)
        {
            return RavenJObject.Parse(input).Deserialize<TModel>(convention);
        }

        public static IEnumerable<TModel> DeserializeMany<TModel>(string input, DocumentConvention convention)
        {
            var objects = RavenJToken.Parse(input).Values();

            return objects.Select(obj => ((RavenJObject)obj).Deserialize<TModel>(convention));
        }

        public static string GetStoreUrl(string serverName, string storeName = "", string path = "")
        {
            var url = string.IsNullOrWhiteSpace(storeName) ?
                string.Format(BrnklyDocumentStore.ServerOnlyUrlFormat, serverName) :
                string.Format(BrnklyDocumentStore.StoreUrlFormat, serverName, storeName);

            if (!string.IsNullOrEmpty(path))
            {
                url += path.EnsurePrefix("/");
            }

            return url;
        }
    }
}
