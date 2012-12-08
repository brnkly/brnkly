using System;
using Raven.Abstractions.Logging;
using Raven.Client.Connection;

namespace Brnkly.Raven
{
    public static class IndexingExtensions
    {
        private static ILog logger = LogManager.GetCurrentClassLogger();

        public static void DeleteIndex(this RavenHelper raven, Uri instanceUrl, string indexName)
        {
            try
            {
                raven.GetDatabaseCommands(instanceUrl).DeleteIndex(indexName);
            }
            catch (Exception exception)
            {
            }
        }

        public static void ResetIndex(this RavenHelper raven, Uri instanceUrl, string indexName)
        {
            raven.GetDatabaseCommands(instanceUrl).ResetIndex(indexName);
        }

        public static void Copy(this RavenHelper raven, Uri fromInstanceUrl, Uri toInstanceUrl, string indexName)
        {
            var source = raven.GetDatabaseCommands(fromInstanceUrl);
            var destination = raven.GetDatabaseCommands(toInstanceUrl);

            var sourceIndexDefinition = source.GetIndex(indexName);
            if (sourceIndexDefinition == null)
            {
                throw new InvalidOperationException("Index definition is null.");
            }

            destination.PutIndex(indexName, sourceIndexDefinition, overwrite: true);
        }

        private static IDatabaseCommands GetDatabaseCommands(this RavenHelper helper, Uri instanceUrl)
        {
            var docStore = helper.GetDocumentStore(instanceUrl.GetServerRootUrl());
            return docStore.DatabaseCommands.ForDatabase(instanceUrl.GetDatabaseName());
        }
    }
}
