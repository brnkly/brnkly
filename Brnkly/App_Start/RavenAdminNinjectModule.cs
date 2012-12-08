using System;
using Brnkly.Raven.Admin.Controllers;
using Ninject.Modules;
using Raven.Client;
using Raven.Abstractions.Data;

namespace Brnkly.Raven.Admin
{
    public class RavenAdminNinjectModule : NinjectModule
    {
        public override void Load()
        {
            var factory = new DocumentStoreFactory().Initialize();
            Bind<DocumentStoreFactory>().ToConstant(factory);

            var readWriteOpsStore = factory
                .GetOrCreate("Operations", AccessMode.ReadWrite)
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
