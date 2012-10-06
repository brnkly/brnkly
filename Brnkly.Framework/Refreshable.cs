using System;
using System.Threading;
using System.Web;
using System.Web.Caching;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework
{
    /// <summary>
    /// A wrapper that will refresh its wrapped value
    /// either at an interval, when a cache dependency changes, or on demand.
    /// </summary>
    /// <typeparam name="T">The Type of object</typeparam>
    public class Refreshable<T> : IDisposable where T : new()
    {
        private static readonly string RefreshableCacheKeyPrefix = "Refreshable.";

        public event EventHandler<RefreshedEventArgs<T>> Refreshed;

        private Timer refreshTimer;
        private bool disposed;

        public TimeSpan RefreshInterval { get; private set; }
        public string DependentOnCacheKey { get; private set; }
        public Func<T> LoadMethod { get; private set; }
        public T Value { get; private set; }

        /// <summary>
        /// Initializes a new Refreshable instance that will refresh a value at a specified interval.
        /// </summary>
        /// <param name="loadMethod">The method used to load a new value.</param>
        /// <param name="refreshInterval">The amount of time between refreshes.</param>
        public Refreshable(
            Func<T> loadMethod,
            TimeSpan refreshInterval,
            EventHandler<RefreshedEventArgs<T>> refreshedHandler = null)
        {
            CodeContract.ArgumentNotNull("loadMethod", loadMethod);

            if (refreshInterval < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    "refreshInterval",
                    "RefreshInterval must be greater than or equal to TimeSpan.Zero. " +
                    "Use TimeSpan.Zero to disable automatic refreshes.");
            }

            this.Value = new T();
            this.LoadMethod = loadMethod;
            this.RefreshInterval = refreshInterval;
            if (refreshedHandler != null)
            {
                this.Refreshed += refreshedHandler;
            }

            this.Refresh();
            this.InitializeTimer();
        }

        /// <summary>
        /// Initializes a new Refreshable instance that will refresh a value whenever
        /// the specified cache key is updated or removed.
        /// </summary>
        /// <param name="loadMethod">The method used to load a new value.</param>
        /// <param name="dependentOnCacheKey">The cache key on which the Refreshable is dependent.</param>
        public Refreshable(
            Func<T> loadMethod,
            string dependentOnCacheKey,
            EventHandler<RefreshedEventArgs<T>> refreshedHandler = null)
        {
            CodeContract.ArgumentNotNull("loadMethod", loadMethod);
            CodeContract.ArgumentNotNull("dependentOnCacheKey", dependentOnCacheKey);

            this.Value = new T();
            this.LoadMethod = loadMethod;
            this.DependentOnCacheKey = dependentOnCacheKey;
            if (refreshedHandler != null)
            {
                this.Refreshed += refreshedHandler;
            }

            this.Refresh();
            this.AddRefreshableToCacheWithDependency();
        }

        /// <summary>
        /// Initiates an immediate refresh on the current thread.
        /// </summary>
        public void Refresh()
        {
            bool wasRefreshed = false;
            try
            {
                T newValue = this.LoadMethod();
                this.Value = newValue;
                wasRefreshed = true;
                this.OnRefreshed(new RefreshedEventArgs<T>(this.Value));
            }
            catch (ThreadAbortException)
            {
                // Do nothing, since there's nothing we can do.
            }
            catch (Exception exception)
            {
                WriteWarningToLog(wasRefreshed, exception);
            }
        }

        protected virtual void OnRefreshed(RefreshedEventArgs<T> e)
        {
            var handler = this.Refreshed;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void InitializeTimer()
        {
            if (!this.RefreshInterval.Equals(TimeSpan.Zero))
            {
                this.refreshTimer = new Timer(
                    new TimerCallback(TimerCallback),
                    this,
                    this.RefreshInterval,
                    this.RefreshInterval);
            }
        }

        private static void TimerCallback(object state)
        {
            var refreshable = state as Refreshable<T>;
            if (refreshable != null)
            {
                refreshable.Refresh();
            }
        }

        private void AddRefreshableToCacheWithDependency()
        {
            this.EnsureDependentOnCacheKeyExists();

            // Add the current instance to cache with the dependency.
            HttpRuntime.Cache.Insert(
                RefreshableCacheKeyPrefix + this.DependentOnCacheKey,
                this,
                new CacheDependency(null, new string[] { this.DependentOnCacheKey }),
                Cache.NoAbsoluteExpiration,
                Cache.NoSlidingExpiration,
                CacheItemPriority.NotRemovable,
                CacheRemovedCallback);
        }

        private void EnsureDependentOnCacheKeyExists()
        {
            if (HttpRuntime.Cache[this.DependentOnCacheKey] == null)
            {
                try
                {
                    HttpRuntime.Cache.Add(
                        this.DependentOnCacheKey,
                        new object(),
                        null,
                        Cache.NoAbsoluteExpiration,
                        Cache.NoSlidingExpiration,
                        CacheItemPriority.NotRemovable,
                        null);
                }
                catch
                {
                    // Add() will throw if the item exists.  
                    // We don't care, so we swallow the exception.
                }
            }
        }

        private static void CacheRemovedCallback(string key, object value, CacheItemRemovedReason removedReason)
        {
            var refreshable = value as Refreshable<T>;
            if (refreshable != null)
            {
                if (removedReason == CacheItemRemovedReason.DependencyChanged)
                {
                    refreshable.Refresh();
                }

                refreshable.AddRefreshableToCacheWithDependency();
            }
        }

        private static void WriteWarningToLog(bool wasRefreshed, Exception exception)
        {
            string message = null;
            if (wasRefreshed)
            {
                message = string.Format(
                    "Refresh succeeded for an instance of type {0}, " +
                    "however a handler of the Refreshed event threw an exception.",
                    typeof(T).FullName);
            }
            else
            {
                message = string.Format(
                    "Refresh failed for instance of type {0}. The previous value will remain in effect.",
                    typeof(T).FullName);
            }

            Log.Warning(exception, message, LogPriority.Operation, LogCategory.Framework);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.refreshTimer != null)
                    {
                        this.refreshTimer.Dispose();
                    }
                }

                this.disposed = true;
            }
        }
    }
}