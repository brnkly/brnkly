using System;
using System.Linq;

namespace Brnkly.Framework.Configuration
{
    public static class EnvironmentConfigBuilderExtensions
    {
        private const string DefaultLogicalInstanceName = "DefaultLogicalInstance";
        private const string DefaultMachineGroupName = "DefaultMachineGroup";

        public static EnvironmentConfig WithDefaultsForEnvironmentType(
            this EnvironmentConfig config,
            EnvironmentType environmentType)
        {
            switch (environmentType)
            {
                case EnvironmentType.Development:
                    config.ForDevelopment();
                    break;
                case EnvironmentType.Test:
                case EnvironmentType.Production:
                    config.Add(
                        "Administration",
                        DefaultLogicalInstanceName,
                        Environment.MachineName);
                    break;
            }

            return config;
        }

        private static void ForDevelopment(
            this EnvironmentConfig config)
        {
            var machineName = Environment.MachineName;

            var defaultMachineGroup =
                config.MachineGroups.SelectByName(DefaultMachineGroupName);
            if (defaultMachineGroup == null)
            {
                defaultMachineGroup = new MachineGroup(DefaultMachineGroupName);
                config.MachineGroups.Add(defaultMachineGroup);
            }

            if (!defaultMachineGroup.MachineNames.Any(
                name => name.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)))
            {
                defaultMachineGroup.MachineNames.Add(Environment.MachineName);
            }

            foreach (var app in PlatformApplication.AllApplications)
            {
                config.Add(app.Name, DefaultLogicalInstanceName, DefaultMachineGroupName);
            }
        }

        private static EnvironmentConfig Add(
            this EnvironmentConfig config,
            string applicationName,
            string logicalInstanceName,
            string machineName)
        {
            var app = config.Applications.SelectByName(applicationName);
            if (app == null)
            {
                app = new Application(applicationName);
                config.Applications.Add(app);
            }

            var logicalInstance = app.LogicalInstances.SelectByName(logicalInstanceName);
            if (logicalInstance == null)
            {
                logicalInstance = new LogicalInstance(logicalInstanceName);
                app.LogicalInstances.Add(logicalInstance);
            }

            var machine = logicalInstance.Machines.SelectByName(machineName);
            if (machine == null)
            {
                machine = new Machine(machineName);
                logicalInstance.Machines.Add(machine);
            }

            return config;
        }
    }
}
