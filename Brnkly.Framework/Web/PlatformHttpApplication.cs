using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.Routing;
using Brnkly.Framework.Logging;
using Brnkly.Framework.ServiceBus;
using Brnkly.Framework.ServiceBus.SelfTest;
using Microsoft.Practices.Unity;
using MvcContrib;
using MvcContrib.PortableAreas;

namespace Brnkly.Framework.Web
{
    public class PlatformHttpApplication : HttpApplication
    {
        private void Application_Start()
        {
            LogBuffer logBuffer = LogBuffer.Current;
            logBuffer.Information("Starting PlatformHttpApplication...");

            PlatformApplication.Current.Initialize(logBuffer);
            this.RunServiceBusSelfTest(logBuffer);

            var container = PlatformApplication.Current.Container;
            this.ConfigureApplicationBus(container);
            this.ConfigureMvc(container);
            this.RegisterPlatformAreas(container, logBuffer);
            ApplicationHeartbeat.Start();

            this.LogAssembliesLoaded(logBuffer);

            logBuffer.FlushToLog(LogPriority.Application);
        }

        private void RunServiceBusSelfTest(LogBuffer logBuffer)
        {
            var bus = PlatformApplication.Current.Container.Resolve<IBus>();
            BusSelfTest.Run(bus, logBuffer);
        }

        private void Application_End()
        {
            ApplicationHeartbeat.End();
        }

        private void ConfigureApplicationBus(IUnityContainer container)
        {
            var factory = new UnityMessageHandlerFactory(container);
            Bus.Instance.SetMessageHandlerFactory(factory);
            container.RegisterInstance<IApplicationBus>(Bus.Instance);
        }

        private void ConfigureMvc(IUnityContainer container)
        {
            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
            container.RegisterType<IControllerActivator, PlatformControllerActivator>();

            GlobalFilters.Filters.Add(new HandleErrorAttribute());
            GlobalFilters.Filters.Add(new LogActionFilter());
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            MvcHandler.DisableMvcResponseHeader = true;

            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine());
        }

        private void RegisterPlatformAreas(IUnityContainer container, LogBuffer logBuffer)
        {
            var state = new PlatformAreaRegistrationState(container, logBuffer);
            AreaRegistration.RegisterAllAreas(state);
        }

        private void LogAssembliesLoaded(LogBuffer logBuffer)
        {
            string assembliesLoaded =
                string.Join(
                    "\n",
                    BuildManager.GetReferencedAssemblies()
                        .Cast<Assembly>()
                        .OrderBy(a => a.FullName)
                        .Select(a => a.FullName));

            logBuffer.Information("Assemblies loaded:\n{0}", assembliesLoaded);
        }
    }
}
