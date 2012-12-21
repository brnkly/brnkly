using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Raven.Abstractions.Logging;
using Raven.Client;

namespace Brnkly.Raven
{
    public class AggressiveCachingSettings
    {
        public static readonly string LiveId = "brnkly/raven/aggressiveCachingSettings";
        public static readonly string PendingId = "brnkly/raven/aggressiveCachingSettings/pending";

        private const string StorePropertyKey = "AggressiveCachingSettings";
        private static AggressiveCachingSettings Default = new AggressiveCachingSettings();
        private static ILog logger = LogManager.GetCurrentClassLogger();
        private IDocumentStore store;
        private int updating = 0;

        internal ConcurrentDictionary<string, TimeSpan> Profiles { get; private set; }

        public AggressiveCachingSettings(IDictionary<string, TimeSpan> defaults = null)
        {
            this.Profiles = defaults == null
                ? new ConcurrentDictionary<string, TimeSpan>()
                : new ConcurrentDictionary<string, TimeSpan>(defaults);
        }

        internal static AggressiveCachingSettings ForStore(IDocumentStore store)
        {
            AggressiveCachingSettings cacheSettings;
            store.TryGetProperty(AggressiveCachingSettings.StorePropertyKey, out cacheSettings);
            return cacheSettings ?? AggressiveCachingSettings.Default;
        }

        internal static void Subscribe(IDocumentStore store, IDictionary<string, TimeSpan> defaults)
        {
            store.Ensure("store").IsNotNull();

            var settings = new AggressiveCachingSettings(defaults);
            settings.store = store;
            store.SetProperty(AggressiveCachingSettings.StorePropertyKey, settings);

            store.Changes()
                .ForDocument(LiveId)
                .Subscribe(new DocumentChangeObserver(_ => settings.LoadFromStore()));
        }

        public void LoadFromStore()
        {
            if (this.store == null || this.store.WasDisposed)
            {
                return;
            }

            try
            {
                if (Interlocked.Exchange(ref this.updating, 1) == 0)
                {
                    using (var session = this.store.OpenSession())
                    {
                        var settings = session.Load<AggressiveCachingSettings>(LiveId);
                        if (settings != null)
                        {
                            this.Profiles = settings.Profiles
                                ?? new ConcurrentDictionary<string, TimeSpan>();
                            logger.Info("New aggressive caching settings loaded.");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                logger.ErrorException(
                    "Failed to load aggressive caching settings. The previous settings remain in effect.",
                    exception);
            }
            finally
            {
                Interlocked.Exchange(ref this.updating, 0);
            }
        }

        public TimeSpan? GetCacheDuration(string cacheProfileName)
        {
            TimeSpan cacheFor;
            if (this != Default &&
                this.Profiles.TryGetValue(cacheProfileName, out cacheFor) ||
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
