using System;
using Brnkly.Raven.Admin.Controllers;
using Ninject.Modules;

namespace Brnkly.Raven.Admin
{
    public class RavenAdminNinjectModule : NinjectModule
    {
        public override void Load()
        {
            var factory = new DocumentStoreFactory(
                new Uri("http://jtbdev1:8081/databases/operations"));
            Bind<DocumentStoreFactory>().ToConstant(factory);

            var readWriteOpsStore = factory.GetOrCreate("Operations", AccessMode.ReadWrite);

            Bind<StoresController>().ToSelf().WithPropertyValue(
                "RavenSession", 
                (context,target) => readWriteOpsStore.OpenSession());
        }
    }
}
