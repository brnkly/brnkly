using System;
using Brnkly.Framework.Data;
using Brnkly.Framework.ServiceBus.Core;
using Microsoft.Practices.Unity;
using Raven.Client;

namespace Brnkly.Framework.Configuration
{
    public sealed class EnvironmentConfigUpdater : RavenDocumentChangedHandler<EnvironmentConfig>
    {
        public EnvironmentConfigUpdater(
            [Dependency(StoreName.Operations)] IDocumentStore store)
            : base(store)
        {
        }

        protected override bool ShouldHandle(string id)
        {
            return id.Equals(EnvironmentConfig.StorageId, StringComparison.OrdinalIgnoreCase);
        }

        protected override void Update(EnvironmentConfig config)
        {
            Bus.UpdateSubscriptions(config.ExpandMachineGroups().Applications);
        }
    }
}
