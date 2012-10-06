using Brnkly.Framework.Logging;
using Brnkly.Framework.ServiceBus;
using Microsoft.Practices.Unity;
using Raven.Client;
using Raven.Client.Connection;
using Raven.Json.Linq;

namespace Brnkly.Framework.Data
{
    public class RavenConfigChangedHandler : IMessageHandler<RavenConfigChanged>
    {
        private IDocumentStore store;

        public RavenConfigChangedHandler(
            [Dependency(StoreName.Operations)] IDocumentStore store)
        {
            this.store = store;
        }

        public void Handle(MessageHandlingContext<RavenConfigChanged> context)
        {
            context.Log.LogPriority = LogPriority.Application;

            var ravenJObject = RavenJObject.Parse(context.Message.NewRavenConfigJson);
            var config = ravenJObject.Deserialize<RavenConfig>(store.Conventions);
            BrnklyDocumentStore.UpdateAllStores(config);
        }
    }
}
