using System;
using Brnkly.Framework.Caching;
using Raven.Client;

namespace Brnkly.Framework.Data
{
    public static class CachingExtensions
    {
        public class NonCachingContext : IDisposable
        {
            public void Dispose() { }
        }

        public static IDisposable GetCachingContext(this IDocumentSession session)
        {
            int aggressiveCacheDuration = CacheSettings.RavenAggressiveCachingDurationSeconds;
            if (aggressiveCacheDuration == 0)
            {
                return new NonCachingContext();
            }

            return session.Advanced.DocumentStore.AggressivelyCacheFor(
                TimeSpan.FromSeconds(aggressiveCacheDuration));
        }
    }
}
