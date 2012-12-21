using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Dispatcher;
using Raven.Client;

namespace Brnkly.Raven.Admin.Controllers
{
    internal class BrnklyApiControllerActivator : IHttpControllerActivator
    {
        private static IDocumentStore readWriteOpsStore = 
            DocumentStoreFactory.Instance
                .GetOrCreate("Operations", AccessMode.ReadWrite)
                .Initialize();

        public IHttpController Create(
            HttpRequestMessage request, 
            HttpControllerDescriptor controllerDescriptor, 
            Type controllerType)
        {
            if (!typeof(BrnklyApiControllerBase).IsAssignableFrom(controllerType))
            {
                throw new InvalidOperationException(
                    "BrnklyApiControllerActivator can only be used with BrnklyApiControllerBase.");
            }

            var controller = Activator.CreateInstance(controllerType) as BrnklyApiControllerBase;
            InjectDependencies(controller);
            return controller;
        }

        private static void InjectDependencies(BrnklyApiControllerBase controller)
        {
            if (controller != null)
            {
                controller.RavenHelper = new RavenHelper();
                controller.RavenSession = readWriteOpsStore.OpenSession();
                controller.RavenSession.Advanced.UseOptimisticConcurrency = true;
            }
        }
    }
}
