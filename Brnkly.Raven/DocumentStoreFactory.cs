using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Raven.Abstractions.Data;
using Raven.Abstractions.Logging;
using Raven.Client;
using Raven.Client.Connection;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Client.Listeners;

namespace Brnkly.Raven
{
    public sealed class DocumentStoreFactory
    {
        private static ILog logger = LogManager.GetCurrentClassLogger();

        private ConcurrentBag<DocumentStoreWrapper> allStoreWrappers =
            new ConcurrentBag<DocumentStoreWrapper>();
        private RavenConfig ravenConfig = new RavenConfig();
        private Uri operationsStoreUrl;
        private bool isInitialized = false;
        private IDocumentStore readOnlyOpsStore;

        private int updatingRavenConfig = 0;
        private Timer updateTimer;
        private TimeSpan updateInterval = TimeSpan.FromMinutes(1);
        private DateTimeOffset lastUpdatedRavenConfig = DateTimeOffset.MinValue;
        private bool disposed = false;

        public DocumentStoreFactory(string connectionStringName = "Brnkly.Raven.Config")
        {
            var parser = ConnectionStringParser<RavenConnectionStringOptions>
                .FromConnectionStringName(connectionStringName);
            parser.Parse();
            var connection = parser.ConnectionStringOptions;

            var url = new Uri(connection.Url);
            EnsureUriPathIncludesDatabase(url);
            this.operationsStoreUrl = url;
        }

        public DocumentStoreFactory Initialize()
        {
            if (this.isInitialized)
            {
                return this;
            }

            Action<DocumentStore> initializer = store =>
                {
                    store.Conventions.FailoverBehavior =
                        FailoverBehavior.AllowReadsFromSecondariesAndWritesToSecondaries;
                    store.Initialize();
                    store.Changes().ConnectionStatusCahnged += ReadOnlyOpsStore_ConnectionStatusCahnged;
                    store.Changes()
                        .ForDocument(RavenConfig.LiveDocumentId)
                        .Subscribe(notification => this.ApplyRavenConfig());
                };

            readOnlyOpsStore = this
                .GetOrCreate(this.operationsStoreUrl.GetDatabaseName(), AccessMode.ReadOnly, initializer)
                .Initialize();

            ApplyRavenConfig();
            updateTimer = new Timer(
                _ => ApplyRavenConfig(fromTimer: true),
                null,
                this.updateInterval,
                this.updateInterval);
            this.isInitialized = true;

            return this;
        }

        public IDocumentStore GetOrCreate(
            string name,
            AccessMode accessMode = AccessMode.ReadOnly,
            Action<DocumentStore> initializer = null)
        {
            name.Ensure("name").IsNotNullOrWhiteSpace();

            var existingWrapper = allStoreWrappers.SingleOrDefault(
                store =>
                    store.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    store.AccessMode == accessMode);
            if (existingWrapper != null)
            {
                return existingWrapper;
            }

            initializer = initializer ?? (store => store.Initialize());
            Action<DocumentStoreWrapper> updateInnerStore = 
                wrapper => this.UpdateInnerStore(wrapper, initializer);
            var newStore = new DocumentStoreWrapper(name, accessMode, updateInnerStore);

            allStoreWrappers.Add(newStore);
            return newStore;
        }

        private void UpdateInnerStore(
            DocumentStoreWrapper wrapper, 
            Action<DocumentStore> innerStoreInitializer)
        {
            DocumentStore existingInnerStore = wrapper.InnerStore;
            DocumentStore newInnerStore = null;
            bool newInnerStoreApplied = false;

            try
            {
                var storeInstance = GetClosestInstanceOrDefault(wrapper);

                if (wrapper.IsInitialized &&
                    wrapper.InnerStore.Url.Equals(storeInstance.Url.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    !wrapper.InnerStore.WasDisposed)
                {
                    logger.Debug("{0} {1} store Url did not change. It remains {2}.", wrapper.AccessMode, wrapper.Name, wrapper.InnerStore.Url);
                    return;
                }

                newInnerStore = this.CreateDocumentStore(storeInstance, wrapper.AccessMode);

                innerStoreInitializer(newInnerStore);

                wrapper.InnerStore = newInnerStore;
                wrapper.IsInitialized = true;
                newInnerStoreApplied = true;

                logger.Info("{0} {1} store Url set to {2}.", wrapper.AccessMode, wrapper.Name, newInnerStore.Url);
            }
            catch (Exception exception)
            {
                LogException(exception, wrapper, newInnerStore);

                if (exception.IsFatal())
                {
                    throw;
                }
            }
            finally
            {
                if (newInnerStoreApplied)
                {
                    if (existingInnerStore != null)
                    {
                        // Wait for current operations to complete.
                        // This is on a background thread.
                        Thread.Sleep(5000);
                        logger.Debug("Disposing store for {0}", existingInnerStore.Url);
                        existingInnerStore.Dispose();
                    }
                }
                else
                {
                    if (newInnerStore != null)
                    {
                        logger.Debug("Disposing store for {0}", newInnerStore.Url);
                        newInnerStore.Dispose();
                    }
                }
            }
        }

        private void ReadOnlyOpsStore_FailoverStatusChanged(object sender, FailoverStatusChangedEventArgs e)
        {
            if (!this.isInitialized)
            {
                return;
            }

            if (e.Failing)
            {
                logger.Error("ReadOnly operations store instance failed: {0}.", e.Url);
            }
            else
            {
                logger.Info("ReadOnly operations store instance recovered: {0}.", e.Url);
            }

            this.ApplyRavenConfig();
        }

        private void ReadOnlyOpsStore_ConnectionStatusCahnged(object sender, EventArgs e)
        {
            if (!this.isInitialized)
            {
                return;
            }

            if (this.readOnlyOpsStore.Changes().Connected)
            {
                logger.Info("Connected to ReadOnly operations store at {0}.", this.readOnlyOpsStore.Url);
            }
            else
            {
                logger.Warn("Lost connection to ReadOnly operations store at {0}.", this.readOnlyOpsStore.Url);
            }

            this.ApplyRavenConfig();
        }

        private void ApplyRavenConfig(bool fromTimer = false)
        {
            if (Interlocked.Exchange(ref updatingRavenConfig, 1) == 0)
            {
                try
                {
                    if (fromTimer &&
                        DateTimeOffset.UtcNow < this.lastUpdatedRavenConfig.Add(updateInterval))
                    {
                        logger.Debug("Raven config timer doing nothing, since config was updated recently.");
                        return;
                    }

                    logger.Debug("Loading Raven config...");
                    LoadRavenConfig();
                    
                    lock (this.allStoreWrappers)
                    {
                        foreach (var wrapper in allStoreWrappers)
                        {
                            wrapper.UpdateInnerStore(wrapper);
                        }
                    }

                    this.lastUpdatedRavenConfig = DateTimeOffset.UtcNow;
                    // When the Url of the readOnlyOpsStore changes, we may receive
                    // change notifications from both old and new inner stores.  
                    // A small delay should cause one to be ignored.
                    Thread.Sleep(1000);
                }
                catch (Exception exception)
                {
                    logger.ErrorException("Error updating Raven config", exception);

                    if (exception.IsFatal())
                    {
                        throw;
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref updatingRavenConfig, 0);
                }
            }
        }

        private void LoadRavenConfig()
        {
            using (var session = this.readOnlyOpsStore.OpenSession())
            {
                var config = session.Load<RavenConfig>(RavenConfig.LiveDocumentId);
                if (config != null)
                {
                    this.ravenConfig = config;
                }
            }
        }

        private void LogException(
            Exception exception,
            DocumentStoreWrapper wrapper,
            DocumentStore newInnerStore)
        {
            string newUrl = (newInnerStore == null) ? null : newInnerStore.Url;
            string message = string.Format(
                "{0} {1} store Url could not be set to '{2}'. {3}",
                wrapper.AccessMode, 
                wrapper.Name, 
                newUrl,
                (wrapper.IsInitialized && !wrapper.InnerStore.WasDisposed) 
                    ? string.Format("Active Url remains {0}.", wrapper.Url) 
                    : "Store has not been initialized.");
            logger.ErrorException(message, exception);
        }

        internal DocumentStore CreateDocumentStore(Instance storeInstance, AccessMode accessMode)
        {
            var databaseName = storeInstance.Url.GetDatabaseName(throwIfNotFound: true);
            var store = new DocumentStore
            {
                Identifier = databaseName,
                DefaultDatabase = databaseName,
                Url = storeInstance.Url.ToString(),
                Conventions =
                {
                    FailoverBehavior = FailoverBehavior.AllowReadsFromSecondariesAndWritesToSecondaries
                }
            };

            if (accessMode == AccessMode.ReadOnly)
            {
                store.RegisterListener((IDocumentStoreListener)new ReadOnlyListener());
                store.RegisterListener((IDocumentDeleteListener)new ReadOnlyListener());
            }

            return store;
        }

        private Instance GetClosestInstanceOrDefault(DocumentStoreWrapper wrapper)
        {
            var store = ravenConfig.Stores.SelectByName(wrapper.Name);
            Instance instance = null;
            if (store != null)
            {
                instance = store.GetClosestReplica(
                    Environment.MachineName,
                    wrapper.AccessMode == AccessMode.ReadWrite);
            }

            instance = instance ?? 
                new Instance
                {
                    Url = new Uri(this.operationsStoreUrl, wrapper.Name.ToLowerInvariant()),
                    AllowReads = true,
                    AllowWrites = wrapper.AccessMode == AccessMode.ReadWrite
                };

            return instance;
        }

        private void EnsureUriPathIncludesDatabase(Uri uri)
        {
            uri.GetDatabaseName(throwIfNotFound: true);
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
                    if (this.updateTimer != null)
                    {
                        this.updateTimer.Dispose();
                    }
                }

                this.disposed = true;
            }
        }
    }
}
