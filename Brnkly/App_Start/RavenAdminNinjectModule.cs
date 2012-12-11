using System;
using System.Linq;
using Brnkly.Raven.Admin.Controllers;
using Ninject.Modules;
using Raven.Client;
using Raven.Abstractions.Data;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Json.Linq;
using Raven.Abstractions.Indexing;

namespace Brnkly.Raven.Admin
{
    public class RavenAdminNinjectModule : NinjectModule
    {
        public override void Load()
        {
            var factory = new DocumentStoreFactory().Initialize();
            Bind<DocumentStoreFactory>().ToConstant(factory);

            Action<DocumentStore> initializer = store =>
            {
                store.Initialize();
                CreateOpsStoreSubscriptions(store);
            };
            var readWriteOpsStore = factory
                .GetOrCreate("Operations", AccessMode.ReadWrite, initializer)
                .Initialize();

            BindToSelfWithRavenSession<ReplicationController>(readWriteOpsStore);
            BindToSelfWithRavenSession<IndexingController>(readWriteOpsStore);
        }

        private void BindToSelfWithRavenSession<T>(IDocumentStore docStore)
        {
            Bind<T>().ToSelf().WithPropertyValue(
                "RavenSession",
                (context, target) =>
                {
                    var session = docStore.OpenSession();
                    session.Advanced.UseOptimisticConcurrency = true;
                    return session;
                });
        }
        
        private static void CreateOpsStoreSubscriptions(IDocumentStore store)
        {
            store.Changes()
                .ForDocument(AggressiveCacheSettings.LiveId)
                .Subscribe(n => 
                {
                    using (var session = store.OpenSession())
                    {
                        var settings = session.Load<AggressiveCacheSettings>(
                            AggressiveCacheSettings.LiveId);
                        if (settings != null)
                        {
                            store.SetProperty(
                                AggressiveCacheSettings.StorePropertyKey,
                                settings);
                        }
                    }
                });
        }
    }
}
