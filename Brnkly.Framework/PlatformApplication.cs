using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using Brnkly.Framework.Caching;
using Brnkly.Framework.Configuration;
using Brnkly.Framework.Data;
using Brnkly.Framework.Logging;
using Brnkly.Framework.ServiceBus;
using Brnkly.Framework.ServiceBus.Core;
using Brnkly.Framework.ServiceBus.Wcf;
using Microsoft.Practices.Unity;
using Raven.Client;

namespace Brnkly.Framework
{
    public partial class PlatformApplication
    {
        static PlatformApplication()
        {
            var applications = new Collection<PlatformApplication>();
            ConfigureApplications(applications);
            AllApplications = applications.ToList().AsReadOnly();

            UnknownApplication = new PlatformApplication("[Unknown]");
            Current = GetCurrentApplication();
        }

        public static IEnumerable<PlatformApplication> AllApplications;
        public static readonly PlatformApplication UnknownApplication;
        public static readonly PlatformApplication Current;

        private object configurationLock = new object();
        private bool isConfigured;
        private bool isReadOnly;

        public string Name { get; private set; }
        public IEnumerable<Subscription> Subscriptions { get; set; }
        public IEnumerable<ServiceProvided> ServicesProvided { get; set; }
        public bool IsDebuggingEnabled { get; private set; }
        public EnvironmentType EnvironmentType { get; private set; }
        internal IUnityContainer Container { get; private set; }

        private static PlatformApplication GetCurrentApplication()
        {
            var currentName = GetNameFromIisApplication();
            var currentApp = AllApplications.SingleOrDefault(
                app => app.Name.Equals(currentName, StringComparison.OrdinalIgnoreCase));
            if (currentApp == null)
            {
                currentApp = UnknownApplication;
            }

            currentApp.IsDebuggingEnabled = GetIsDebuggingEnabled();
            return currentApp;
        }

        private static string GetNameFromIisApplication()
        {
            if (string.IsNullOrWhiteSpace(HttpRuntime.AppDomainAppVirtualPath) ||
                HttpRuntime.AppDomainAppVirtualPath.Length <= 1)
            {
                return string.Empty;
            }
            else
            {
                return HttpRuntime.AppDomainAppVirtualPath.Substring(1);
            }
        }

        private static bool GetIsDebuggingEnabled()
        {
            return
                HttpContext.Current != null &&
                HttpContext.Current.IsDebuggingEnabled;
        }

        public PlatformApplication(string name)
        {
            Name = name;
            this.Subscriptions = this.GetRequiredSubscriptions();
            this.ServicesProvided = Enumerable.Empty<ServiceProvided>().ToList().AsReadOnly();
            this.IsDebuggingEnabled = false;
            this.EnvironmentType = this.GetEnvironmentType(Environment.MachineName);
        }

        public PlatformApplication SubscribeTo(
            Type messageType,
            string fromApplication = "*",
            SubscriptionType subscriptionType = SubscriptionType.RoundRobin)
        {
            this.ThrowIfReadOnly();

            if (messageType != null)
            {
                this.Subscriptions = this.Subscriptions.Concat(
                    new[] { new Subscription(messageType, fromApplication, subscriptionType) })
                    .ToList()
                    .AsReadOnly();
            }

            return this;
        }

        public PlatformApplication ProvideService(Type requestMessageType)
        {
            this.ThrowIfReadOnly();

            if (requestMessageType != null)
            {
                this.ServicesProvided = this.ServicesProvided.Concat(
                    new[] { new ServiceProvided(requestMessageType) })
                    .ToList()
                    .AsReadOnly();
            }

            return this;
        }

        public PlatformApplication AsReadOnly()
        {
            this.isReadOnly = true;
            return this;
        }

        private ReadOnlyCollection<Subscription> GetRequiredSubscriptions()
        {
            return new[] 
                {
                    new Subscription(
                        typeof(RavenConfigChanged), 
                        "Administration", 
                        SubscriptionType.Broadcast),
                    new Subscription(
                        typeof(RavenDocumentChanged), 
                        "Administration", 
                        SubscriptionType.Broadcast),
                }
                .ToList()
                .AsReadOnly();
        }

        private void ThrowIfReadOnly()
        {
            if (isReadOnly)
            {
                throw new InvalidOperationException("The ApplicationConfiguration has been set to read-only.");
            }
        }

        internal PlatformApplication Initialize(LogBuffer logBuffer)
        {
            lock (this.configurationLock)
            {
                if (this.isConfigured)
                {
                    return this;
                }

                try
                {
                    logBuffer.Information("Configuring PlatformApplication...");

                    this.Container = new UnityContainer();
                    this.InitializeServiceBus(logBuffer);
                    this.InitializeOperationsStoreAndDocs(logBuffer);
                    this.AllowAreasToConfigureContainer(logBuffer);
                    isConfigured = true;

                    logBuffer.Information("PlatformApplication was successfully configured.");
                }
                catch (Exception exception)
                {
                    logBuffer.Critical("PlatformApplication configuration failed due to the exception below.");
                    logBuffer.Critical(exception);
                    throw;
                }

                return this;
            }
        }

        private void InitializeServiceBus(LogBuffer logBuffer)
        {
            logBuffer.Information("Configuring service bus.");
            Bus.FactoryMethod = provider => new WcfBusImplementation(provider, true);
            Bus.UpdateSubscriptions(
                new EnvironmentConfig()
                    .WithDefaultsForEnvironmentType(this.EnvironmentType)
                    .ExpandMachineGroups()
                    .Applications);
            this.Container.RegisterInstance<IBus>(new Bus());
            MessageHandlerRegistry.RegisterAllHandlerTypes();
            this.Container.RegisterInstance<MessageHandlerRegistry>(MessageHandlerRegistry.Instance);
            this.Container.RegisterType<IBusReceiver, BusReceiver>();
        }

        private void InitializeOperationsStoreAndDocs(LogBuffer logBuffer)
        {
            logBuffer.Information("Registering Operations store.");
            var operationsStore =
                BrnklyDocumentStore.Register(this.Container, StoreName.Operations);

            LoadRavenConfig(operationsStore, logBuffer);

            this.Container.Resolve<EnvironmentConfigUpdater>()
                .Initialize(EnvironmentConfig.StorageId, logBuffer);

            this.Container.Resolve<LoggingSettingsUpdater>()
                .Initialize(LoggingSettings.StorageId, logBuffer);

            this.Container.Resolve<CacheSettingsUpdater>()
                .Initialize(CacheSettingsData.StorageId, logBuffer);
        }

        public static void LoadRavenConfig(IDocumentStore operationsStore, LogBuffer logBuffer)
        {
            try
            {
                using (var session = operationsStore.OpenSession())
                {
                    var config = session.Load<RavenConfig>(RavenConfig.StorageId);
                    BrnklyDocumentStore.UpdateAllStores(config);
                }
            }
            catch (Exception exception)
            {
                logBuffer.Critical(
                    "Failed to load the Raven configuration due to the exception below. " +
                    "The application may not function correctly until a valid Raven configuration is received.");
                logBuffer.Critical(exception);
            }
        }

        private void AllowAreasToConfigureContainer(LogBuffer logBuffer)
        {
            var areaTypes = AssemblyHelper
                .GetConstructableTypes<IServiceBusAreaRegistration>();

            foreach (var areaType in areaTypes)
            {
                var areaInstance =
                    (IServiceBusAreaRegistration)Activator.CreateInstance(areaType);
                areaInstance.ConfigureContainer(this.Container, logBuffer);
            }
        }

        private EnvironmentType GetEnvironmentType(string machineName)
        {
            Func<string, string[], bool> startsWithAny = (machine, prefixes) =>
                prefixes.Any(p => machine.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (startsWithAny(machineName, new[] { "dev-" }))
            {
                return EnvironmentType.Development;
            }

            if (startsWithAny(machineName, new[] { "test-" }))
            {
                return EnvironmentType.Test;
            }

            return EnvironmentType.Production;
        }

        public class Subscription
        {
            public Type MessageType { get; private set; }
            public string FromApplication { get; private set; }
            public SubscriptionType SubscriptionType { get; private set; }

            public Subscription(
                Type messageType,
                string fromApplication,
                SubscriptionType subscriptionType)
            {
                CodeContract.ArgumentNotNull("messageType", messageType);
                CodeContract.ArgumentNotNullOrWhitespace("fromApplication", fromApplication);

                this.MessageType = messageType;
                this.FromApplication = fromApplication;
                this.SubscriptionType = subscriptionType;
            }
        }

        public class ServiceProvided
        {
            public Type RequestMessageType { get; private set; }

            public ServiceProvided(Type requestMessageType)
            {
                CodeContract.ArgumentNotNull("requestMessageType", requestMessageType);

                this.RequestMessageType = requestMessageType;
            }
        }
    }
}
