using System.Diagnostics;
using AutoMapper;
using Brnkly.Framework.Administration.Models;
using Brnkly.Framework.Caching;
using Brnkly.Framework.Configuration;
using Brnkly.Framework.Data;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework.Administration
{
    internal class AutoMapperConfig
    {
        public void Initialize()
        {
            Mapper.CreateMap<EnvironmentConfigEditModel, EnvironmentConfig>();
            Mapper.CreateMap<EnvironmentConfig, EnvironmentConfigEditModel>()
                .ForMember(m => m.ErrorMessage, opt => opt.Ignore());

            Mapper.CreateMap<ApplicationEditModel, Application>();
            Mapper.CreateMap<Application, ApplicationEditModel>();

            Mapper.CreateMap<LogicalInstanceEditModel, LogicalInstance>();
            Mapper.CreateMap<LogicalInstance, LogicalInstanceEditModel>();

            Mapper.CreateMap<MachineEditModel, Machine>();
            Mapper.CreateMap<Machine, MachineEditModel>();

            Mapper.CreateMap<MachineGroupEditModel, MachineGroup>();
            Mapper.CreateMap<MachineGroup, MachineGroupEditModel>();

            Mapper.CreateMap<RavenConfig, RavenConfigEditModel>()
                .ForMember(m => m.ErrorMessage, opt => opt.Ignore())
                .ForMember(m => m.OriginalEtag, opt => opt.Ignore());
            Mapper.CreateMap<RavenConfigEditModel, RavenConfig>();

            Mapper.CreateMap<RavenServer, RavenServerEditModel>()
                .ConvertUsing(new RavenServerConverter());
            Mapper.CreateMap<RavenServerEditModel, RavenServer>()
                .ConvertUsing(new RavenServerConverter());

            Mapper.CreateMap<RavenStore, RavenStoreEditModel>()
                .ForMember(m => m.PendingChange, opt => opt.Ignore());
            Mapper.CreateMap<RavenStoreEditModel, RavenStore>();

            Mapper.CreateMap<LoggingSettingsEditModel, LoggingSettings>();
            Mapper.CreateMap<LoggingSettings, LoggingSettingsEditModel>();

            Mapper.CreateMap<SettingEditModel<SourceLevels>, Setting<SourceLevels>>();
            Mapper.CreateMap<Setting<SourceLevels>, SettingEditModel<SourceLevels>>();

            Mapper.CreateMap<CacheSettingsEditModel, CacheSettingsData>();
            Mapper.CreateMap<CacheSettingsData, CacheSettingsEditModel>();

            Mapper.CreateMap<SettingEditModel<CacheParameters>, Setting<CacheParameters>>();
            Mapper.CreateMap<Setting<CacheParameters>, SettingEditModel<CacheParameters>>();

            Mapper.AssertConfigurationIsValid();
        }
    }
}
