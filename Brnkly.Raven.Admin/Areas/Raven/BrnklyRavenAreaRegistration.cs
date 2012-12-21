using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using AutoMapper;
using Brnkly.Raven.Admin.Models;

namespace Brnkly.Raven.Admin
{
    public class BrnklyRavenAreaRegistration : AreaRegistration
    {
        public override string AreaName { get { return "BrnklyRaven"; } }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            this.RegisterMvcRoutes(context);
            this.RegisterApiRoutes();
            this.RegisterBundles();
            this.ConfigureAutoMapper();
        }

        public void RegisterMvcRoutes(AreaRegistrationContext context)
        {
            context.MapRoute(
                name: "Brnkly-Raven-Default",
                url: "brnkly/raven/{action}/{id}",
                defaults: new { controller = "Raven", action = "Index", id = UrlParameter.Optional });
        }

        public void RegisterApiRoutes()
        {
            GlobalConfiguration.Configuration.Routes.MapHttpRoute(
                name: "Brnkly-Api-Raven-Default",
                routeTemplate: "brnkly/api/raven/{controller}/{action}");
        }

        public void RegisterBundles()
        {
            var bundles = BundleTable.Bundles;

            bundles.Add(
                new ScriptBundle("~/brnkly/scripts/frameworks")
                .Include(
                    "~/Scripts/jquery-{version}.js",
                    "~/Scripts/bootstrap.js",
                    "~/Scripts/knockout-{version}.js",
                    "~/Scripts/knockout.mapping-latest.js",
                    "~/Scripts/linq.js"));

            bundles.Add(
                new ScriptBundle("~/brnkly/scripts/brnkly")
                .Include(
                    "~/Areas/Brnkly/Scripts/KnockoutBootstrapCheckboxBinding.js",
                    "~/Areas/Brnkly/Scripts/KnockoutBootstrapRadioBinding.js",
                    "~/Areas/Brnkly/Scripts/KnockoutDirtyFlag.js",
                    "~/Areas/Brnkly/Scripts/BrnklyRaven.js"));

            bundles.Add(
                new StyleBundle("~/content/bootstrap")
                .Include(
                    "~/Content/bootstrap.css",
                    "~/Content/bootstrap-responsive.css"));

            bundles.Add(
                new StyleBundle("~/brnkly/css/brnkly")
                .Include("~/Areas/Brnkly/Content/Brnkly.css"));

            BundleTable.EnableOptimizations = true;
        }

        public void ConfigureAutoMapper()
        {
            Mapper.CreateMap<RavenConfig, RavenConfigModel>();
            Mapper.CreateMap<RavenConfigModel, RavenConfig>();

            Mapper.CreateMap<Store, StoreModel>();
            Mapper.CreateMap<StoreModel, Store>();

            Mapper.CreateMap<Instance, InstanceModel>();
            Mapper.CreateMap<InstanceModel, Instance>();

            Mapper.AssertConfigurationIsValid();
        }
    }
}
