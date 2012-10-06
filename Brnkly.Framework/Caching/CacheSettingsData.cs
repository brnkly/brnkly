using System.Collections.ObjectModel;
using Brnkly.Framework.Configuration;
using Newtonsoft.Json;

namespace Brnkly.Framework.Caching
{
    public sealed class CacheSettingsData
    {
        public static readonly string StorageId = "brnkly/config/cachesettings";

        private CacheParameters currentCacheParameters;

        public string Id { get; private set; }
        public Collection<Setting<CacheParameters>> CacheParametersPerInstance { get; private set; }

        [JsonIgnore]
        public CacheParameters CurrentCacheParameters
        {
            get
            {
                if (currentCacheParameters == null)
                {
                    currentCacheParameters = this.CacheParametersPerInstance.GetCurrentOrDefault(
                        GetDefaultCacheParameters());
                }

                return currentCacheParameters;
            }
        }

        public CacheSettingsData()
        {
            this.Id = StorageId;
            this.CacheParametersPerInstance = new Collection<Setting<CacheParameters>>();
        }

        private CacheParameters GetDefaultCacheParameters()
        {
            if (PlatformApplication.Current.EnvironmentType != EnvironmentType.Production)
            {
                return new CacheParameters()
                {
                    OutputCacheDurationSeconds = 0,
                    RavenAggressiveDataCacheDurationSeconds = 0
                };
            }

            return new CacheParameters()
            {
                OutputCacheDurationSeconds = 10,
                RavenAggressiveDataCacheDurationSeconds = 10
            };
        }
    }
}
