using System;
using System.Collections.Concurrent;
using System.Web.Mvc;
using Brnkly.Framework.Logging;
using Brnkly.Framework.ServiceBus;
using Microsoft.Practices.Unity;
using MvcContrib.PortableAreas;

namespace Brnkly.Framework.Web
{
    /// <summary>
    /// Derive from this class to create an area for handling web requests or service bus messages.
    /// </summary>
    /// <example>
    /// <code>
    ///public class MyAreaRegistration : PlatformAreaRegistration
    ///{
    ///    // Defaults to this area's namespace if not overridden.
    ///    public override string AreaName
    ///    {
    ///        get { return "MyAreaName"; }
    ///    }
    ///
    ///    // Defaults to the AreaName value if not overridden.
    ///    public override string AreaRoutePrefix
    ///    {
    ///        get { return "my/area/"; }
    ///    }
    ///
    ///    // Put all container registrations here, including BrnklyDocumentStore.
    ///    // This method executes exactly once, before either the first http request or 
    ///    // the first service bus message is handled.
    ///    protected override void ConfigureContainer(IUnityContainer container)
    ///    {
    ///        BrnklyDocumentStore.Register(state.Container, StoreName.MyStore);
    ///        
    ///        container.RegisterType&lt;IMyInterface, MyImplementation&gt;();
    ///
    ///        // See Brnkly.Content.Streams.Configuration for an example of implementing settings
    ///        // backed by the Operations store.
    ///        container.Resolve&lt;MySettingsUpdater&gt;()
    ///            .Initialize(MySettingsData.StorageId, state.Log);
    ///    }
    ///
    ///    // Put all route registrations and other web-related startup code here.
    ///    // This method executes once, before the first http request,
    ///    // but is not guaranteed to execute before the first message is handled.
    ///    protected override void RegisterArea(
    ///        AreaRegistrationContext context,
    ///        IApplicationBus bus,
    ///        PlatformAreaRegistrationState state)
    ///    {
    ///        context.MapRoute(
    ///            this.AreaName + "-Default",
    ///            this.AreaRoutePrefix + "{controller}/{action}");
    /// 
    ///        state.Log.Information("A message that will be included in the app startup event log entry.");
    ///    }
    ///}
    /// </code>
    /// </example>
    public abstract class PlatformAreaRegistration : PortableAreaRegistration, IServiceBusAreaRegistration
    {
        private static ConcurrentDictionary<Type, bool> areasWithContainerConfigured =
            new ConcurrentDictionary<Type, bool>();

        public override string AreaName { get { return this.GetType().Namespace; } }

        public override void RegisterArea(AreaRegistrationContext context, IApplicationBus bus)
        {
            CodeContract.ArgumentNotNull("context", context);
            CodeContract.ArgumentNotNull("bus", bus);

            var state = this.GetState(context);

            try
            {
                this.LogAreaRegistering(state);
                this.ConfigureContainerIfNecessary(state.Container, state.Log);
                this.RegisterArea(context, bus, state);
                this.CreateAssetsRoute(context, "css");
                this.CreateAssetsRoute(context, "img");
                this.CreateAssetsRoute(context, "js");
                this.RegisterAreaEmbeddedResources();
            }
            catch (Exception exception)
            {
                this.LogAreaRegistrationException(state, exception);
                throw;
            }
        }

        protected void CreateAssetsRoute(AreaRegistrationContext context, string subfolderName)
        {
            context.MapRoute(
                this.AreaName + "-" + subfolderName,
                this.AreaRoutePrefix.EnsureSuffix("/") + subfolderName + "/{resourceName}",
                new
                {
                    controller = "EmbeddedResource",
                    action = "Index",
                    resourcePath = ("Content." + subfolderName)
                },
                null,
                new string[1] { "MvcContrib.PortableAreas" });
        }

        protected void RequireAuthorizationOnAllActions(string activity)
        {
            var filterProvider = new AuthorizeActivityFilterProvider(this.GetType(), activity);
            FilterProviders.Providers.Add(filterProvider);
        }

        /// <summary>
        /// Configures the dependency injection container and performs other one-time startup actions.
        /// Place calls to BrnklyDocumentStore.Register() in this method.
        /// Guaranteed to execute exactly once, before the first web request or service bus message is handled.
        /// </summary>
        /// <param name="container">An IUnityContainer instance.</param>
        protected abstract void ConfigureContainer(IUnityContainer container, LogBuffer log);

        /// <summary>
        /// Registers routes and performs other one-time startup actions for handling web requests.
        /// Guaranteed to execute exactly once, before the first web request is handled.
        /// However, service bus messages may be received and handled before this method is called.
        /// </summary>
        /// <param name="context">An AreaRegistrationContext.</param>
        /// <param name="bus">An MvcContrib in-process IApplicationBus. (Not the service bus.)</param>
        /// <param name="state">A PlatformAreaRegistrationState.</param>
        protected abstract void RegisterArea(
            AreaRegistrationContext context,
            IApplicationBus bus,
            PlatformAreaRegistrationState state);

        void IServiceBusAreaRegistration.ConfigureContainer(
            IUnityContainer container,
            LogBuffer logBuffer)
        {
            this.ConfigureContainerIfNecessary(container, logBuffer);
        }

        private void ConfigureContainerIfNecessary(
            IUnityContainer container,
            LogBuffer logBuffer)
        {
            if (!areasWithContainerConfigured.TryAdd(this.GetType(), false))
            {
                logBuffer.Information(
                    "Container has already been configured for area {0}.",
                    this.AreaName);
                return;
            }

            logBuffer.Information("Configuring container for area {0}.", this.AreaName);
            this.ConfigureContainer(container, logBuffer);
            areasWithContainerConfigured[this.GetType()] = true;
        }

        private void LogAreaRegistering(PlatformAreaRegistrationState state)
        {
            state.Log.Information(
                "Registering area: AreaName='{0}', AreaRoutePrefix='{1}', Type='{2}'.",
                this.AreaName,
                this.AreaRoutePrefix,
                this.GetType().FullName);
        }

        private void LogAreaRegistrationException(PlatformAreaRegistrationState state, Exception exception)
        {
            state.Log.Critical(
                "Area registration failed: Name='{0}', Type='{1}'.",
                this.AreaName,
                this.GetType().FullName);
            state.Log.Critical(exception);
        }

        private PlatformAreaRegistrationState GetState(AreaRegistrationContext context)
        {
            var state = context.State as PlatformAreaRegistrationState;
            if (state == null)
            {
                throw new InvalidOperationException(
                    "State not found. " +
                    "Pass an instance of PlatformAreaRegistrationState " +
                    "to the RegisterAllAreas() method.");
            }

            return state;
        }
    }
}
