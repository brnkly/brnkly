using AutoMapper;
using Brnkly.Raven;
using Brnkly.Raven.Admin.Models;

namespace Brnkly
{
    public static class AutoMapperConfig
    {
        public static void Register()
        {
            Mapper.CreateMap<RavenConfig, RavenConfigModel>();
            Mapper.CreateMap<RavenConfigModel, RavenConfig>();

            Mapper.CreateMap<Store, StoreModel>();
            Mapper.CreateMap<StoreModel, Store>();

            Mapper.CreateMap<StoreInstance, StoreInstanceModel>()
                .ForMember(
                    i => i.DisplayName, 
                    expr => expr.ResolveUsing(x => x.Uri.Authority));
            Mapper.CreateMap<StoreInstanceModel, StoreInstance>()
               .ForMember(i => i.DatabaseName, expr => expr.Ignore());

            Mapper.CreateMap<Destination, DestinationModel>()
                .ForMember(
                    i => i.DisplayName,
                    expr => expr.ResolveUsing(x => x.Uri.Authority));
            Mapper.CreateMap<DestinationModel, Destination>();

            Mapper.AssertConfigurationIsValid();
        }
    }
}