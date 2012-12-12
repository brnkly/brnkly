using System;
using Brnkly.Raven.Admin.Controllers;
using Ninject.Modules;
using Raven.Client;
using Raven.Client.Document;

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
                store.SetUpConfigurableAggressiveCaching();
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
    }
}
