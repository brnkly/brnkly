using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Brnkly.Framework.Configuration;

namespace Brnkly.Framework.ServiceBus.Core
{
    public class BusUriProvider
    {
        private readonly ReadOnlyCollection<Subscription> subscriptions;
        private readonly ReadOnlyCollection<ServiceProvider> serviceProviders;

        public BusUriProvider(
            IEnumerable<PlatformApplication> platformApps,
            IEnumerable<Application> environmentApps)
        {
            CodeContract.ArgumentNotNull("busConfig", platformApps);
            CodeContract.ArgumentNotNull("applications", environmentApps);

            this.subscriptions = this.LoadSubscriptions(platformApps, environmentApps);
            this.serviceProviders = this.LoadServiceProviders(platformApps, environmentApps);
        }

        public IEnumerable<Uri> GetSendToUris(Type messageType)
        {
            return
                (this.subscriptions
                 .Where(sub => messageType == sub.MessageType)
                 .SelectMany(sub => sub.GetSendToUris()))
                .ToArray();
        }

        public Uri GetServiceProviderUri(Type messageType)
        {
            return
                (this.serviceProviders
                 .Where(p => messageType == p.MessageType)
                 .Select(p => p.GetSendToUri()))
                .FirstOrDefault();
        }

        internal string GetAllAsStringForLogging()
        {
            var message = new StringBuilder();
            message.Append("\nSubscribers to which this application instance may publish:");
            foreach (var subscription in this.subscriptions.OrderBy(s => s.MessageType.AssemblyQualifiedName))
            {
                message.AppendFormat("\n{0}", subscription);
                foreach (var uri in subscription.PhysicalUris.OrderBy(u => u.ToString()))
                {
                    message.AppendFormat("\n\t{0}", uri);
                }
            }

            message.Append("\n\nService providers to which this application instance may send requests:");
            foreach (var serviceProvider in this.serviceProviders.OrderBy(sp => sp.MessageType.AssemblyQualifiedName))
            {
                message.AppendFormat("\n{0}", serviceProvider);
                foreach (var uri in serviceProvider.PhysicalUris.OrderBy(u => u.ToString()))
                {
                    message.AppendFormat("\n\t{0}", uri);
                }
            }

            message.Append("\n");
            return message.ToString();
        }

        private ReadOnlyCollection<Subscription> LoadSubscriptions(
            IEnumerable<PlatformApplication> platformApps,
            IEnumerable<Application> environmentApps)
        {
            var subscribingApps =
                (from app in platformApps
                 from sub in app.Subscriptions
                 where sub.FromApplication.Equals("*") ||
                       sub.FromApplication.Equals(PlatformApplication.Current.Name, StringComparison.OrdinalIgnoreCase)
                 select new
                 {
                     AppName = app.Name,
                     MessageType = sub.MessageType,
                     SubscriptionType = sub.SubscriptionType,
                 }).ToList();

            var instances = this.GetInstancesWithMachines(environmentApps);

            var activeSubscriptions =
                from subscribingInstance in subscribingApps.Join(
                    instances,
                    sub => sub.AppName,
                    instance => instance.AppName,
                    (sub, instance) => new { sub.AppName, sub.MessageType, sub.SubscriptionType, instance.MachineNames },
                    StringComparer.OrdinalIgnoreCase)
                select new Subscription(
                    subscribingInstance.MessageType,
                    subscribingInstance.SubscriptionType,
                    from machineName in subscribingInstance.MachineNames
                    select new ApplicationEndpoints(subscribingInstance.AppName, machineName));

            return activeSubscriptions.ToList().AsReadOnly();
        }

        private ReadOnlyCollection<ServiceProvider> LoadServiceProviders(
            IEnumerable<PlatformApplication> platformApps,
            IEnumerable<Application> environmentApps)
        {
            var requestTypes = platformApps
                .SelectMany(app => app.ServicesProvided)
                .Select(svc => svc.RequestMessageType)
                .Distinct();

            var appsWithMachines = this.GetAppsWithMachines(environmentApps);

            var flattenedData =
                from requestType in requestTypes
                from platformApp in platformApps
                where platformApp.ServicesProvided.Any(
                    svc => svc.RequestMessageType == requestType)
                from machineName in appsWithMachines.Where(
                    app => app.AppName == platformApp.Name)
                    .SelectMany(app => app.MachineNames)
                select new
                {
                    RequestMessageType = requestType,
                    AppName = platformApp.Name,
                    MachineName = machineName
                };

            var activeProviders =
                from row in flattenedData
                group row by row.RequestMessageType into g
                select new ServiceProvider(
                    g.Key,
                    from row in g
                    select new ApplicationEndpoints(row.AppName, row.MachineName));


            return activeProviders.ToList().AsReadOnly();
        }

        private IEnumerable<string> GetAppNamesForRequestType(
            IEnumerable<PlatformApplication> platformApps,
            Type requestType)
        {
            return (from app in platformApps
                    from service in app.ServicesProvided
                    where service.RequestMessageType == requestType
                    select app.Name)
                   .Distinct();
        }

        private IEnumerable<AppInstanceMachineData> GetInstancesWithMachines(
            IEnumerable<Application> environmentApps)
        {
            var appInstances =
                from app in environmentApps
                from instance in app.LogicalInstances
                where instance.Machines.Any()
                select new AppInstanceMachineData
                {
                    AppName = app.Name,
                    InstanceName = instance.Name,
                    MachineNames = instance.Machines.Select(m => m.Name).ToArray()
                };

            return appInstances.ToList();
        }

        private IEnumerable<AppInstanceMachineData> GetAppsWithMachines(
            IEnumerable<Application> environmentApps)
        {
            var appInstances =
                from app in environmentApps
                let machines = app.LogicalInstances.SelectMany(i => i.Machines)
                where machines.Any()
                select new AppInstanceMachineData
                {
                    AppName = app.Name,
                    MachineNames = machines.Select(m => m.Name).ToArray()
                };

            return appInstances.ToList();
        }

        private class AppInstanceMachineData
        {
            public string AppName { get; set; }
            public string InstanceName { get; set; }
            public IEnumerable<string> MachineNames { get; set; }
        }
    }
}
