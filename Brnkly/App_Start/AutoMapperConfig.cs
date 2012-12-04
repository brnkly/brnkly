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

			Mapper.CreateMap<Instance, InstanceModel>();
            Mapper.CreateMap<InstanceModel, Instance>();

            Mapper.AssertConfigurationIsValid();
        }
    }
}