using System;
using System.Collections.Concurrent;
using Raven.Client.Connection;
using Raven.Client.Document;

namespace Brnkly.Raven
{
    public class RavenHelper
    {
        internal const int RavenRequestTimeoutMilliseconds = 2000;

        internal static readonly ConcurrentDictionary<string, DocumentStore> DocumentStores =
            new ConcurrentDictionary<string, DocumentStore>(StringComparer.OrdinalIgnoreCase);

        internal DocumentStore GetDocumentStore(Uri serverRootUri)
        {
            var url = serverRootUri.ToString();
            DocumentStore store;
            if (DocumentStores.TryGetValue(url, out store))
            {
                return store;
            }

            store = new DocumentStore
            {
                Url = url,
                ResourceManagerId = Guid.NewGuid()
            };
            store.Initialize();

            store.Conventions.FailoverBehavior = FailoverBehavior.FailImmediately;
            store.JsonRequestFactory.ConfigureRequest +=
                new EventHandler<WebRequestEventArgs>(JsonRequestFactory_ConfigureRequest);

            DocumentStores.TryAdd(url, store);

            return store as DocumentStore;
        }

        private static void JsonRequestFactory_ConfigureRequest(object sender, WebRequestEventArgs e)
        {
            e.Request.Timeout = RavenRequestTimeoutMilliseconds;
        }
    }
}
