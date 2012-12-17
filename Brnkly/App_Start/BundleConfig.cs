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
                    "~/Scripts/knockout.mapping-latest.js",
                    "~/Scripts/linq.js"));

            bundles.Add(new ScriptBundle("~/scripts/brnkly-raven")
                .Include(
                    "~/Areas/Brnkly/Scripts/KnockoutBootstrapCheckboxBinding.js",
                    "~/Areas/Brnkly/Scripts/KnockoutBootstrapRadioBinding.js",
                    "~/Areas/Brnkly/Scripts/KnockoutDirtyFlag.js",
                    "~/Areas/Brnkly/Scripts/BrnklyRaven.js"));

            bundles.Add(new StyleBundle("~/content/bootstrap")
                .Include(
                    "~/Content/bootstrap-{version}.css", 
                    "~/Content/bootstrap-responsive-{version}.css"));
            
            bundles.Add(new StyleBundle("~/content/brnkly")
                .Include("~/Content/Brnkly.css"));
            
            BundleTable.EnableOptimizations = true;
        }
    }
}