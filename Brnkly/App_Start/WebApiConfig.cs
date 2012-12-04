using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Dependencies;
using Newtonsoft.Json.Serialization;

namespace Brnkly
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.DependencyResolver = new WrappedMvcResolver();

            var jsonFormatter = config.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.ContractResolver = 
                new CamelCasePropertyNamesContractResolver();
            config.Formatters.Add(jsonFormatter);

            // api/raven/replication/pending
            config.Routes.MapHttpRoute(
                name: "Api-Raven-Default",
                routeTemplate: "api/raven/{controller}/{action}");
        }
    }

    public class WrappedMvcResolver : IDependencyResolver
    {
        public IDependencyScope BeginScope()
        {
            return this;
        }

        public object GetService(Type serviceType)
        {
            return System.Web.Mvc.DependencyResolver.Current.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return System.Web.Mvc.DependencyResolver.Current.GetServices(serviceType);
        }

        public void Dispose()
        {
        }
    }
}
