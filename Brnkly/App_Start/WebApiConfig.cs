using System;
using System.Collections.Generic;
using System.Linq;
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

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
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
