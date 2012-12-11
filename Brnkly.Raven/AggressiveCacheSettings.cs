using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Raven.Client;

namespace Brnkly.Raven
{
    public class AggressiveCacheSettings
    {
        public static readonly string LiveId = "brnkly/raven/aggressiveCacheSettings";
        public static readonly string PendingId = "brnkly/raven/aggressiveCacheSettings/pending";
        public static readonly string StorePropertyKey = "AggressiveCacheSettings";

        internal ConcurrentDictionary<string, TimeSpan> Profiles { get; private set; }

        public AggressiveCacheSettings(IDictionary<string, TimeSpan> profiles = null)
        {
            this.Profiles = profiles == null
                ? new ConcurrentDictionary<string, TimeSpan>()
                : new ConcurrentDictionary<string, TimeSpan>(profiles);
        }

        public TimeSpan? GetCacheDuration(string cacheProfileName)
        {
            TimeSpan cacheFor;
            if (this.Profiles.TryGetValue(cacheProfileName, out cacheFor) ||
                this.Profiles.TryGetValue("*", out cacheFor))
            {
                return cacheFor;
            }
            else
            {
                return null;
            }
        }
    }
}
