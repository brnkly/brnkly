﻿using System.Linq;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Brnkly.Admin;

namespace Brnkly
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
