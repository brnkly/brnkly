using System.Collections.Generic;
using System.Collections.ObjectModel;
using Brnkly.Framework.Configuration;
using Brnkly.Framework.ServiceBus;
using Brnkly.Framework.ServiceBus.Core;
using Raven.Client;
using Raven.Client.Embedded;

namespace Brnkly.Framework.UnitTests
{
    public static class TestHelpers
    {
        public static IDocumentStore CreateAndInitializeInMemoryStore()
        {
            return new EmbeddableDocumentStore { Configuration = { RunInMemory = true } }
                .Initialize();
        }

        public static IBus CreateTestableBus()
        {
            return CreateTestableBus(
                PlatformApplication.AllApplications,
                DefaultTestEnvironment.EnvironmentApps);
        }

        public static TestableBus CreateTestableBus(
            IEnumerable<PlatformApplication> platformApps,
            IEnumerable<Application> environmentApps)
        {
            var busUriProvider = new BusUriProvider(platformApps, environmentApps);
            return new TestableBus(busUriProvider);
        }

        public static class DefaultTestEnvironment
        {
            public static Collection<Application> EnvironmentApps
            {
                get
                {
                    var apps = new Collection<Application>();
                    foreach (var app in PlatformApplication.AllApplications)
                    {
                        apps.Add(CreateApp(app.Name, "UnitTestLogicalInstance", "UnitTestMachine"));
                    }

                    return apps;
                }
            }

            private static Application CreateApp(
                string name,
                string instanceName,
                params string[] machineNames)
            {
                var app = new Application(name);
                var instance = new LogicalInstance(instanceName);
                foreach (var machineName in machineNames)
                {
                    instance.Machines.Add(new Machine(machineName));
                }

                app.LogicalInstances.Add(instance);

                return app;
            }
        }
    }
}
