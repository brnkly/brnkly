using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Raven.Abstractions.Extensions;
using Raven.Client;

namespace Brnkly.Raven
{
    public static class AggressiveCachingExtensions
    {
        private static TimeSpan minCacheTimeAllowedByRaven = TimeSpan.FromSeconds(1);

        public static IDocumentStore SetUpConfigurableAggressiveCaching(
            this IDocumentStore store,
            IDictionary<string, TimeSpan> initialProfiles = null)
        {
            AggressiveCachingSettings.Subscribe(store, initialProfiles);
            return store;
        }

        public static IDisposable AggressivelyCacheFor(
            this IDocumentStore store,
            string cacheProfileName)
        {
            var cacheDuration = AggressiveCachingSettings.ForStore(store)
                .GetCacheDuration(cacheProfileName)
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
    }
}
