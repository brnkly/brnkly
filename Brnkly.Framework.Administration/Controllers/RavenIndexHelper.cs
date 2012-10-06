using System;
using System.Collections.Generic;
using System.Linq;
using Brnkly.Framework.Administration.Models;
using Brnkly.Framework.Data;
using Raven.Abstractions.Data;
using Raven.Client.Document;

namespace Brnkly.Framework.Administration.Controllers
{
    public class RavenIndexHelper
    {
        private static readonly IEnumerable<Func<string, bool>> IndexNameFilters =
            new Func<string, bool>[]
                {
                    //x => !x.StartsWith("Raven/"),
                    //x => !x.StartsWith("Dynamic/"),
                    //x => !x.StartsWith("Temp/"),
                };

        public static void Delete(RavenIndexDataModel indexModel)
        {
            var documentStore = GetTenantDocumentStore(indexModel.ServerName, indexModel.StoreName);

            documentStore.DatabaseCommands.DeleteIndex(indexModel.IndexName);
        }

        public static void Reset(RavenIndexDataModel indexModel)
        {
            var documentStore = GetTenantDocumentStore(indexModel.ServerName, indexModel.StoreName);

            documentStore.DatabaseCommands.ResetIndex(indexModel.IndexName);
        }

        public static void Copy(RavenIndexDataModel indexModel, string targetServerName)
        {
            var sourceStore = GetTenantDocumentStore(indexModel.ServerName, indexModel.StoreName);

            var targetStore = GetTenantDocumentStore(targetServerName, indexModel.StoreName);

            var sourceIndexDefinition = sourceStore.DatabaseCommands.GetIndex(indexModel.IndexName);

            if (sourceIndexDefinition == null)
            {
                throw new InvalidOperationException("Index definition is null.");
            }

            targetStore.DatabaseCommands
                .PutIndex(indexModel.IndexName, sourceIndexDefinition, overwrite: true);
        }

        public static int GetHashCode(RavenIndexDataModel indexModel)
        {
            var documentStore = GetTenantDocumentStore(indexModel.ServerName, indexModel.StoreName);

            var definition = documentStore.DatabaseCommands.GetIndex(indexModel.IndexName);

            if (documentStore == null)
            {
                throw new InvalidOperationException("Index definition is null.");
            }

            return definition.GetHashCode();
        }

        public static QueryResult GetZeroPageQueryResult(RavenIndexDataModel indexModel)
        {
            var documentStore = GetTenantDocumentStore(indexModel.ServerName, indexModel.StoreName);

            var queryResult = documentStore.DatabaseCommands
                .Query(indexModel.IndexName, new IndexQuery { PageSize = 0 }, null);

            return queryResult;
        }

        public static IEnumerable<RavenIndexGraphModel> GetIndexGraphs(RavenConfig ravenConfig)
        {
            var indexDataModels =
                ravenConfig.Stores
                    .SelectMany(store =>
                                store.Servers.Select(
                                    server =>
                                    new
                                        {
                                            StoreName = store.Name,
                                            ServerName = server.Name,
                                            IndexNames = GetIndexNames(server, store)
                                        }))
                    .SelectMany(flattenedByServer =>
                                flattenedByServer.IndexNames.Select(
                                    indexName =>
                                    new RavenIndexDataModel
                                        {
                                            StoreName = flattenedByServer.StoreName,
                                            ServerName = flattenedByServer.ServerName,
                                            IndexName = indexName,
                                        }))
                    .Where(indexData => IndexNameFilters.All(f => f(indexData.IndexName)))
                    .ToArray();

            var graphs = ReduceFlattenedDataIntoGraphs(indexDataModels);

            return graphs;
        }

        private static IEnumerable<RavenIndexGraphModel> ReduceFlattenedDataIntoGraphs(
            IEnumerable<RavenIndexDataModel> indexDataModels)
        {
            var allServerNames = indexDataModels.Select(set => set.ServerName).Distinct().ToArray();

            var graphs = indexDataModels
                .GroupBy(set => set.StoreName)
                .Select(setByStoreName =>
                        new RavenIndexGraphModel
                            {
                                StoreName = setByStoreName.Key,
                                AllServerNames = allServerNames,
                                IndexStatuses = setByStoreName
                                    .GroupBy(byStoreName => byStoreName.IndexName)
                                    .OrderBy(indexDataByIndexName => indexDataByIndexName.Key)
                                    .Select(indexDataByIndexName =>
                                        MapStatusModel(allServerNames, indexDataByIndexName))
                            })
                .OrderBy(graph => graph.StoreName);

            return graphs;
        }

        private static RavenIndexStatusModel MapStatusModel(
            IEnumerable<string> allServerNames,
            IGrouping<string, RavenIndexDataModel> indexDataByIndexName)
        {
            var serversWithIndex = indexDataByIndexName.Select(g => g.ServerName);

            return new RavenIndexStatusModel
                       {
                           IndexName = indexDataByIndexName.Key,
                           IndexExistenceByServerName = allServerNames
                               .ToDictionary(
                                   name => name,
                                   name => serversWithIndex.Contains(name))
                       };
        }

        private static IEnumerable<string> GetIndexNames(RavenServer server, RavenStore store)
        {
            try
            {
                var documentStore = GetTenantDocumentStore(server.Name, store.Name);

                var indexNames = documentStore.DatabaseCommands.GetIndexNames(0, 255);

                return indexNames;
            }
            catch (Exception)
            {
                // Currently we provide no feedback in this context if the GetIndexNames command fails
                // e.g. when the server is not available
                return Enumerable.Empty<string>();
            }
        }

        private static DocumentStore GetTenantDocumentStore(string serverName, string storeName)
        {
            var url = string.Format(BrnklyDocumentStore.StoreUrlFormat, serverName, storeName);

            var store = new DocumentStore { Url = url, ResourceManagerId = Guid.NewGuid() };

            store.Initialize();

            return store;
        }
    }
}