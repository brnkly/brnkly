using System;
using Brnkly.Raven.Admin.Controllers;
using Ninject.Modules;
using Raven.Client;

namespace Brnkly.Raven.Admin
{
    public class RavenAdminNinjectModule : NinjectModule
    {
        public override void Load()
        {
            var factory = new DocumentStoreFactory(
                new Uri("http://rav1:8081/databases/operations"))
				.Initialize();
            Bind<DocumentStoreFactory>().ToConstant(factory);

            var readWriteOpsStore = factory
                .GetOrCreate("Operations", AccessMode.ReadWrite)
                .Initialize();

            BindWithRavenSession<ReplicationController>(readWriteOpsStore);
            BindWithRavenSession<IndexingController>(readWriteOpsStore);
        }

        private void BindWithRavenSession<T>(IDocumentStore store)
        {
            Bind<T>().ToSelf().WithPropertyValue(
                "RavenSession",
                (context, target) => OpenSession(store));
        }

        private static IDocumentSession OpenSession(IDocumentStore docStore)
        {
            var session = docStore.OpenSession();
            session.Advanced.UseOptimisticConcurrency = true;
            return session;
        }
    }
}
