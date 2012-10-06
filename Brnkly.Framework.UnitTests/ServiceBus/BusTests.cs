using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Brnkly.Framework.Configuration;
using Brnkly.Framework.ServiceBus;
using Brnkly.Framework.ServiceBus.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Brnkly.Framework.UnitTests.ServiceBus
{
    public class MessageA : Message { }
    public class MessageB : Message { }
    public class MessageC : Message { public static bool IsTransactional = true; }
    public class MessageD : Message { public static bool IsTransactional = true; }
    public class SomeRequest : RequestMessage { }
    public class RequestWithoutServiceProvider : RequestMessage { }

    [TestClass]
    public class BusTests
    {
        [TestMethod]
        public void RoundRobin_publish_should_send_to_one_instance_of_each_subscribing_app()
        {
            var bus = GetBus(TestEnvironment.PlatformApps, TestEnvironment.EnvironmentApps);

            bus.MessagesSent.Clear();
            bus.Publish(new MessageA());
            Assert.AreEqual(3, bus.MessagesSent.Count);
            Assert.AreEqual(
                new Uri("net.msmq://machinex/private/appx/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(0).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexy/private/appy/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(1).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexyz/private/appz/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(2).Destination);

            bus.MessagesSent.Clear();
            bus.Publish(new MessageB());
            Assert.AreEqual(2, bus.MessagesSent.Count);
            Assert.AreEqual(
                new Uri("net.msmq://machinexy/private/appy/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(0).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexyz/private/appz/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(1).Destination);
        }

        [TestMethod]
        public void Broadcast_publish_should_send_to_all_instances_of_each_subscribing_app()
        {
            var bus = GetBus(TestEnvironment.PlatformApps, TestEnvironment.EnvironmentApps);

            bus.MessagesSent.Clear();
            bus.Publish(new MessageC());
            Assert.AreEqual(4, bus.MessagesSent.Count);
            Assert.AreEqual(
                new Uri("net.msmq://machinex/private/appx/bus.svc/tx"),
                bus.MessagesSent.ElementAt(0).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexy/private/appx/bus.svc/tx"),
                bus.MessagesSent.ElementAt(1).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexyz/private/appx/bus.svc/tx"),
                bus.MessagesSent.ElementAt(2).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexyz/private/appz/bus.svc/tx"),
                bus.MessagesSent.ElementAt(3).Destination);
        }

        [TestMethod]
        public void Publish_should_send_only_to_apps_with_matching_FromApplication()
        {
            var bus = GetBus(TestEnvironment.PlatformApps, TestEnvironment.EnvironmentApps);

            bus.MessagesSent.Clear();
            bus.Publish(new MessageD());
            Assert.AreEqual(2, bus.MessagesSent.Count);
            Assert.AreEqual(
                new Uri("net.msmq://machinex/private/appx/bus.svc/tx"),
                bus.MessagesSent.ElementAt(0).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexyz/private/appz/bus.svc/tx"),
                bus.MessagesSent.ElementAt(1).Destination);
        }

        [TestMethod]
        public void SendToSelf_should_send_to_self()
        {
            var bus = GetBus(TestEnvironment.PlatformApps, TestEnvironment.EnvironmentApps);

            bus.MessagesSent.Clear();
            bus.SendToSelf(new MessageA());
            var testUri = new Uri(
                string.Format(
                "net.msmq://{0}/private/[unknown]/bus.svc/nontx",
                System.Environment.MachineName));
            Assert.AreEqual(1, bus.MessagesSent.Count);
            Assert.AreEqual(testUri, bus.MessagesSent.ElementAt(0).Destination);
        }

        [TestMethod]
        public void SendRequest_should_send_to_one_service_providers_in_round_robin()
        {
            var bus = GetBus(TestEnvironment.PlatformApps, TestEnvironment.EnvironmentApps);

            bus.MessagesSent.Clear();
            bus.SendRequest(new SomeRequest());
            bus.SendRequest(new SomeRequest());
            bus.SendRequest(new SomeRequest());
            bus.SendRequest(new SomeRequest());
            bus.SendRequest(new SomeRequest());
            bus.SendRequest(new SomeRequest());
            bus.SendRequest(new SomeRequest());
            bus.SendRequest(new SomeRequest());

            Assert.AreEqual(8, bus.MessagesSent.Count);
            Assert.AreEqual(
                new Uri("net.msmq://machinex/private/appx/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(0).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexy/private/appx/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(1).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexyz/private/appx/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(2).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexyz/private/appz/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(3).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinex/private/appnosubs/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(4).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexy/private/appnosubs/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(5).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinexyz/private/appnosubs/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(6).Destination);
            Assert.AreEqual(
                new Uri("net.msmq://machinex/private/appx/bus.svc/nontx"),
                bus.MessagesSent.ElementAt(7).Destination);
        }

        public static TestableBus GetBus(
            IEnumerable<PlatformApplication> platformApps,
            IEnumerable<Application> environmentApps)
        {
            var busUriProvider = new BusUriProvider(platformApps, environmentApps);
            return new TestableBus(busUriProvider);
        }

        public static class TestEnvironment
        {
            public static Collection<Application> EnvironmentApps
            {
                get
                {
                    var apps = new Collection<Application>();
                    apps.Add(CreateApp("AppX", "InstanceFoo", "MachineX", "MachineXY", "MachineXYZ"));
                    apps.Add(CreateApp("AppY", "InstanceFoo", "MachineXY", "MachineXYZ"));
                    apps.Add(CreateApp("AppZ", "InstanceFoo", "MachineXYZ"));
                    apps.Add(CreateApp("AppNoMachines", null));
                    apps.Add(CreateApp("AppNoSubs", "InstanceFoo", "MachineX", "MachineXY", "MachineXYZ"));

                    return apps;
                }
            }

            private static Application CreateApp(
                string name,
                string instanceName,
                params string[] machineNames)
            {
                var app = new Application(name);
                if (instanceName != null)
                {
                    var instance = new LogicalInstance(instanceName);
                    foreach (var machineName in machineNames)
                    {
                        instance.Machines.Add(new Machine(machineName));
                    }

                    app.LogicalInstances.Add(instance);
                }

                return app;
            }

            public static IEnumerable<PlatformApplication> PlatformApps
            {
                get
                {
                    var applications = new[]
                        {
                            new PlatformApplication("AppX")
                                .SubscribeTo(typeof(MessageA))
                                .SubscribeTo(typeof(MessageC), "*", SubscriptionType.Broadcast)
                                .SubscribeTo(typeof(MessageD))
                                .ProvideService(typeof(SomeRequest))
                                .AsReadOnly(),

                            new PlatformApplication("AppY")
                                .SubscribeTo(typeof(MessageA), PlatformApplication.Current.Name, SubscriptionType.RoundRobin)
                                .SubscribeTo(typeof(MessageB))
                                .SubscribeTo(typeof(MessageD), "NonExistentApp", SubscriptionType.RoundRobin)
                                .AsReadOnly(),

                            new PlatformApplication("AppZ")
                                .SubscribeTo(typeof(MessageA))
                                .SubscribeTo(typeof(MessageB))
                                .SubscribeTo(typeof(MessageC), "*", SubscriptionType.Broadcast)
                                .SubscribeTo(typeof(MessageD), PlatformApplication.Current.Name, SubscriptionType.RoundRobin)
                                .ProvideService(typeof(SomeRequest))
                                .AsReadOnly(),

                            new PlatformApplication("AppNoMachines")
                                .SubscribeTo(typeof(MessageA))
                                .SubscribeTo(typeof(MessageB))
                                .SubscribeTo(typeof(MessageC))
                                .SubscribeTo(typeof(MessageD))
                                .ProvideService(typeof(SomeRequest))
                                .ProvideService(typeof(RequestWithoutServiceProvider))
                                .AsReadOnly(),

                            new PlatformApplication("AppNoSubs")
                                .ProvideService(typeof(SomeRequest))
                                .AsReadOnly(),
                        }
                        .ToList()
                        .AsReadOnly();

                    return applications;
                }
            }
        }
    }
}
