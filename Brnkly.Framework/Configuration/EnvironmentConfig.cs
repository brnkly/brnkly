using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Framework.Configuration
{
    public class EnvironmentConfig
    {
        public static readonly string StorageId = "brnkly/config/environment";

        public string Id { get; private set; }
        public Collection<Application> Applications { get; private set; }
        public Collection<MachineGroup> MachineGroups { get; private set; }

        public EnvironmentConfig()
        {
            this.Id = StorageId;
            this.Applications = new Collection<Application>();
            this.MachineGroups = new Collection<MachineGroup>();
        }

        internal EnvironmentConfig ExpandMachineGroups()
        {
            var allLogicalInstances = this.Applications.SelectMany(app => app.LogicalInstances);
            foreach (var instance in allLogicalInstances)
            {
                instance.ExpandMachineGroups(this.MachineGroups);
            }

            return this;
        }

        public override string ToString()
        {
            return string.Format(
                "Id='{0}'\nApplications:\n{1}",
                this.Id,
                string.Join("\n", this.Applications.Select(m => m.ToString())));
        }
    }
}
