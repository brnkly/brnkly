using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Brnkly.Framework.Web
{
    public class AuthorizeActivityFilterProvider : IFilterProvider
    {
        private string areaRegistrationNamespace;
        private AuthorizeActivityAttribute filter;

        public AuthorizeActivityFilterProvider(Type areaRegistrationType, string activity)
        {
            this.areaRegistrationNamespace = areaRegistrationType.Namespace;
            this.filter = new AuthorizeActivityAttribute(activity);
        }

        public IEnumerable<Filter> GetFilters(
            ControllerContext controllerContext,
            ActionDescriptor actionDescriptor)
        {
            var controllerNamespace = actionDescriptor.ControllerDescriptor.ControllerType.Namespace;
            if (controllerNamespace.StartsWith(areaRegistrationNamespace))
            {
                return new[] { new Filter(this.filter, FilterScope.First, int.MinValue) };
            }

            return Enumerable.Empty<Filter>();
        }
    }
}
