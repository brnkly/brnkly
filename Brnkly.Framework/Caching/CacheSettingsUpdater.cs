using System;
using Brnkly.Framework.Data;
using Microsoft.Practices.Unity;
using Raven.Client;

namespace Brnkly.Framework.Caching
{
    public class CacheSettingsUpdater : RavenDocumentChangedHandler<CacheSettingsData>
    {
        public CacheSettingsUpdater(
            [Dependency(StoreName.Operations)] IDocumentStore store)
            : base(store)
        {
        }

        protected override bool ShouldHandle(string id)
        {
            return string.Equals(id, CacheSettingsData.StorageId, StringComparison.OrdinalIgnoreCase);
        }

        protected override void Update(CacheSettingsData settingsData)
        {
            CacheSettings.UpdateSettings(settingsData);
        }
    }
}
