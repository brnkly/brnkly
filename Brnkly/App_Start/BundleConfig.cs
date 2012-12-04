using System.Web;
using System.Web.Optimization;

namespace Brnkly 
{
    public class BundleConfig 
    {
        public static void RegisterBundles(BundleCollection bundles) 
        {
            bundles.Add(
                new ScriptBundle("~/scripts/frameworks")
                .Include(
                    "~/Scripts/jquery-{version}.js",
                    "~/Scripts/bootstrap-{version}.js",
                    "~/Scripts/knockout-{version}.js", 
                    "~/Scripts/knockout.mapping-latest.js"));

            bundles.Add(new ScriptBundle("~/scripts/ravenconfig")
                .Include(
                    "~/Areas/Raven/Scripts/KnockoutBootstrapCheckboxBinding.js",
                    "~/Areas/Raven/Scripts/KnockoutBootstrapRadioBinding.js",
                    "~/Areas/Raven/Scripts/KnockoutDirtyFlag.js",
                    "~/Areas/Raven/Scripts/RavenConfig.js"));

            bundles.Add(new StyleBundle("~/content/bootstrap")
                .Include(
                    "~/Content/bootstrap-{version}.css", 
                    "~/Content/bootstrap-responsive-{version}.css"));
            
            bundles.Add(new StyleBundle("~/content/site")
                .Include("~/Content/Site.css"));
            
            BundleTable.EnableOptimizations = true;
        }
    }
}