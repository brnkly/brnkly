using System.Linq;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Brnkly
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AutoMapperConfig.Register();

            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(this.GetRazorViewEngine());
        }

        private RazorViewEngine GetRazorViewEngine()
        {
            var razor = new RazorViewEngine();
            razor.AreaMasterLocationFormats = AppendInstalledAreaPaths(razor.AreaMasterLocationFormats);
            razor.AreaPartialViewLocationFormats = AppendInstalledAreaPaths(razor.AreaPartialViewLocationFormats);
            razor.AreaViewLocationFormats = AppendInstalledAreaPaths(razor.AreaViewLocationFormats);
            razor.FileExtensions = new[] { "cshtml" };

            return razor;
        }

        private string[] AppendInstalledAreaPaths(string[] original)
        {
            var newPaths = original.Select(path => path.Replace("~/Areas", "~/InstalledAreas"));
            return original.Concat(newPaths).Where(path => path.EndsWith(".cshtml")).ToArray();
        }
    }
}
