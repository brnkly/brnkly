using System;
using System.Net.Http.Formatting;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Dispatcher;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Raven.Client;

namespace Brnkly.Raven.Admin.Controllers
{
    public class BrnklyApiControllerConfigAttribute : Attribute, IControllerConfiguration
    {
        public static Func<IDocumentSession> GetSession { get; set; }

        public void Initialize(
            HttpControllerSettings controllerSettings, 
            HttpControllerDescriptor controllerDescriptor)
        {
            var jsonFormatter = new JsonMediaTypeFormatter();
            jsonFormatter.SerializerSettings.ContractResolver = 
                new CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());

            controllerSettings.Formatters.Clear();
            controllerSettings.Formatters.Add(jsonFormatter);

            controllerSettings.Services.Replace(
                typeof(IHttpControllerActivator),
                new BrnklyApiControllerActivator());
        }
    }
}
