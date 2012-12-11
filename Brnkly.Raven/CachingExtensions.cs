using System;
using System.Collections.Concurrent;
using Raven.Abstractions.Extensions;
using Raven.Client;

namespace Brnkly.Raven
{
    public static class CachingExtensions
    {
        private static TimeSpan minCacheTimeAllowedByRaven = TimeSpan.FromSeconds(1);

        public static IDisposable AggressivelyCacheFor(
            this IDocumentStore store,
            string cacheProfileName)
        {
            var cacheDuration = store.GetAggressiveCacheSettings().GetCacheDuration(cacheProfileName)
                ?? store.JsonRequestFactory.AggressiveCacheDuration;
            if (cacheDuration.HasValue &&
                minCacheTimeAllowedByRaven <= cacheDuration.Value )
            {
                return store.AggressivelyCacheFor(cacheDuration.Value);
            }
            else
            {
                return new DisposableAction(() => { });
            }
        }

        public static void SetAggressiveCacheSettings(
            this IDocumentStore store,
            AggressiveCacheSettings cacheSettings)
        {
            store.SetProperty(AggressiveCacheSettings.StorePropertyKey, cacheSettings);
        }

        public static AggressiveCacheSettings GetAggressiveCacheSettings(
            this IDocumentStore store)
        {
            AggressiveCacheSettings cacheSettings;
            if (!store.TryGetProperty(AggressiveCacheSettings.StorePropertyKey, out cacheSettings))
            {
                cacheSettings = new AggressiveCacheSettings();
                store.SetProperty(AggressiveCacheSettings.StorePropertyKey, cacheSettings);
            }

            return cacheSettings;
        }
    }
}
