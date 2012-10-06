
namespace Brnkly.Framework.Caching
{
    public static class CacheSettings
    {
        private static CacheSettingsData Data = new CacheSettingsData();

        public static int OutputCacheDurationSeconds
        {
            get
            {
                return Data.CurrentCacheParameters.OutputCacheDurationSeconds;
            }
        }

        public static int RavenAggressiveCachingDurationSeconds
        {
            get
            {
                return Data.CurrentCacheParameters.RavenAggressiveDataCacheDurationSeconds;
            }
        }

        internal static void UpdateSettings(CacheSettingsData data)
        {
            Data = data;
        }
    }
}
