using System;
using Brnkly.Framework.Data;
using Microsoft.Practices.Unity;
using Raven.Client;

namespace Brnkly.Framework.Logging
{
    public class LoggingSettingsUpdater : RavenDocumentChangedHandler<LoggingSettings>
    {
        public LoggingSettingsUpdater(
            [Dependency(StoreName.Operations)] IDocumentStore store)
            : base(store)
        {
        }

        protected override bool ShouldHandle(string id)
        {
            return string.Equals(id, LoggingSettings.StorageId, StringComparison.OrdinalIgnoreCase);
        }

        protected override void Update(LoggingSettings settingsData)
        {
            var currentLevel = settingsData.CurrentLoggingLevel;
            Log.UpdateLoggingLevel(currentLevel);
        }
    }
}
