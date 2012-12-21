using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using AutoMapper;
using Brnkly.Raven.Admin.Models;

namespace Brnkly.Raven.Admin
{
    public class BrnklyRavenAreaRegistration : AreaRegistration
    {
        public override string AreaName { get { return "Brnkly"; } }

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
                new ScriptBundle("~/scripts/brnkly-js")
                .Include(
                    "~/Areas/Brnkly/Scripts/jquery-{version}.min.js",
                    "~/Areas/Brnkly/Scripts/bootstrap.js.min",
                    "~/Areas/Brnkly/Scripts/knockout-{version}.js",
                    "~/Areas/Brnkly/Scripts/knockout.mapping-latest.js",
                    "~/Areas/Brnkly/Scripts/linq.min.js",
                    "~/Areas/Brnkly/Scripts/KnockoutBootstrapCheckboxBinding.js",
                    "~/Areas/Brnkly/Scripts/KnockoutBootstrapRadioBinding.js",
                    "~/Areas/Brnkly/Scripts/KnockoutDirtyFlag.js",
                    "~/Areas/Brnkly/Scripts/BrnklyRaven.js"));

            bundles.Add(
                new StyleBundle("~/content/brnkly-css")
                .Include(
                    "~/Areas/Brnkly/Content/bootstrap.min.css",
                    "~/Areas/Brnkly/Content/Brnkly.css"));

#if DEBUG
            BundleTable.EnableOptimizations = true;
#endif
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
