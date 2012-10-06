using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Framework.Administration.Models
{
    public class EnvironmentConfigEditModel
    {
        public string ErrorMessage { get; set; }
        public string Id { get; set; }
        public Collection<ApplicationEditModel> Applications { get; private set; }
        public Collection<MachineGroupEditModel> MachineGroups { get; private set; }

        public EnvironmentConfigEditModel()
        {
            this.Applications = new Collection<ApplicationEditModel>();
            this.MachineGroups = new Collection<MachineGroupEditModel>();
        }

        #region Applications

        public LogicalInstanceEditModel GetLogicalInstance(
            string applicationName,
            string logicalInstanceName)
        {
            var application = this.Applications.SelectByName(applicationName);
            if (application == null)
            {
                return null;
            }

            return application.LogicalInstances.SelectByName(logicalInstanceName);
        }

        public EnvironmentConfigEditModel AddLogicalInstance(
            string applicationName,
            string logicalInstanceName)
        {
            var logicalInstance = this.GetLogicalInstance(applicationName, logicalInstanceName);
            if (logicalInstance == null)
            {
                var application = this.Applications.SelectByName(applicationName);
                application.LogicalInstances.Add(
                    new LogicalInstanceEditModel { Name = logicalInstanceName });
            }

            return this;
        }

        public EnvironmentConfigEditModel DeleteLogicalInstance(
            string applicationName,
            string logicalInstanceName)
        {
            var logicalInstance = this.GetLogicalInstance(applicationName, logicalInstanceName);
            if (logicalInstance != null)
            {
                var application = this.Applications.SelectByName(applicationName);
                application.LogicalInstances.Remove(logicalInstance);
            }

            return this;
        }

        public MachineEditModel GetMachineFromLogicalInstance(
            string applicationName,
            string logicalInstanceName,
            string machineName)
        {
            var logicalInstance = this.GetLogicalInstance(applicationName, logicalInstanceName);
            if (logicalInstance == null)
            {
                return null;
            }

            return logicalInstance.Machines.SelectByName(machineName);
        }

        public EnvironmentConfigEditModel AddMachineToLogicalInstance(
            string applicationName,
            string logicalInstanceName,
            string machineName)
        {
            var machine = this.GetMachineFromLogicalInstance(applicationName, logicalInstanceName, machineName);
            if (machine == null)
            {
                var logicalInstance = this.GetLogicalInstance(applicationName, logicalInstanceName);
                logicalInstance.Machines.Add(new MachineEditModel { Name = machineName });
            }

            return this;
        }

        public EnvironmentConfigEditModel DeleteMachineFromLogicalInstance(
            string applicationName,
            string logicalInstanceName,
            string machineName)
        {
            var machine = this.GetMachineFromLogicalInstance(applicationName, logicalInstanceName, machineName);
            if (machine != null)
            {
                var logicalInstance = this.GetLogicalInstance(applicationName, logicalInstanceName);
                logicalInstance.Machines.Remove(machine);
            }

            return this;
        }

        #endregion

        #region MachineGroups

        public EnvironmentConfigEditModel AddMachineGroup(string groupName)
        {
            var group = this.MachineGroups.SelectByName(groupName);
            if (group == null)
            {
                this.MachineGroups.Add(new MachineGroupEditModel { Name = groupName });
            }

            return this;
        }

        public EnvironmentConfigEditModel DeleteMachineGroup(string groupName)
        {
            var group = this.MachineGroups.SelectByName(groupName);
            if (group != null)
            {
                this.MachineGroups.Remove(group);
            }

            return this;
        }

        public EnvironmentConfigEditModel AddMachineToGroup(string groupName, string machineName)
        {
            var group = this.MachineGroups.SelectByName(groupName);
            if (group != null)
            {
                group.MachineNames.Add(machineName);
            }

            return this;
        }

        public EnvironmentConfigEditModel DeleteMachineFromGroup(string groupName, string machineName)
        {
            var group = this.MachineGroups.SelectByName(groupName);
            if (group != null &&
                group.MachineNames.Contains(machineName))
            {
                group.MachineNames.Remove(machineName);
            }

            return this;
        }

        #endregion

        public EnvironmentConfigEditModel Normalize()
        {
            var knownPlatformApps = PlatformApplication.AllApplications;
            this.EnsureKnownAppsExist(knownPlatformApps);
            this.RemoveUnknownApps(knownPlatformApps);

            var knownStores = StoreName.AllStoreNames;

            this.SortCollections();

            return this;
        }

        private void EnsureKnownAppsExist(IEnumerable<PlatformApplication> knownPlatformApps)
        {
            var missingApps = knownPlatformApps
                .Where(app => this.Applications.SelectByName(app.Name) == null);
            foreach (var missingApp in missingApps)
            {
                this.Applications.Add(new ApplicationEditModel { Name = missingApp.Name });
            }
        }

        private void RemoveUnknownApps(IEnumerable<PlatformApplication> knownPlatformApps)
        {
            var unknownApps = this.Applications
                .Where(app => knownPlatformApps.SelectByName(app.Name) == null)
                .ToArray();
            foreach (var unknownApp in unknownApps)
            {
                this.Applications.Remove(unknownApp);
            }
        }

        private void SortCollections()
        {
            this.Applications = this.Applications.OrderByName();
            foreach (var application in this.Applications)
            {
                application.LogicalInstances.OrderByName();
                foreach (var instance in application.LogicalInstances)
                {
                    instance.Machines.OrderByName();
                }
            }

            this.MachineGroups = this.MachineGroups.OrderByName();
            foreach (var group in this.MachineGroups)
            {
                group.MachineNames = new Collection<string>(
                    group.MachineNames.OrderBy(m => m).ToList());
            }
        }
    }
}
