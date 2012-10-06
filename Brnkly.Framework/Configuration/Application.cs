using System;
using System.Collections.ObjectModel;
using System.Linq;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.Configuration
{
    public class Application
    {
        public string Name { get; private set; }
        public Collection<LogicalInstance> LogicalInstances { get; private set; }

        public Application(string name)
        {
            this.ValidateName(name);

            this.Name = name;
            this.LogicalInstances = new Collection<LogicalInstance>();
        }

        public override string ToString()
        {
            return string.Format(
                "Name='{0}', Instances=\n\t{1}",
                this.Name,
                string.Join("\n\t", this.LogicalInstances.Select(i => i.ToString())));
        }

        private void ValidateName(string name)
        {
            if (PlatformApplication.Current == PlatformApplication.UnknownApplication)
            {
                return;
            }

            var knownPlatformApps = PlatformApplication.AllApplications;
            if (!knownPlatformApps.Any(app => app.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Log.Warning(
                    string.Format(
                        "The application name '{0}' is not known. " +
                        "Applications must be configured in the Brnkly.Framework.PlatformApplication class.",
                        name),
                    LogPriority.Application);
            }
        }
    }
}
