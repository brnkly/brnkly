using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Raven.Abstractions.Data;
using Raven.Abstractions.Logging;
using Raven.Client;
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
        private bool isInitialized;
        private IDocumentStore readOnlyOpsStore;
        private int updatingRavenConfig = 0;

        public DocumentStoreFactory(string connectionStringName = "Brnkly.Raven.DocumentStoreFactory")
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
                    store.Initialize();
                    store.Changes()
                        .ForDocument(RavenConfig.LiveDocumentId)
                        .Subscribe(OnRavenConfigChanged);
                };

            readOnlyOpsStore = this
                .GetOrCreate(this.operationsStoreUrl.GetDatabaseName(), AccessMode.ReadOnly, initializer)
                .Initialize();

            readOnlyOpsStore.DatabaseCommands.EnsureDatabaseExists(
                this.operationsStoreUrl.GetDatabaseName());

            LoadRavenConfig();

            // Force immediate update, in case the config says to use 
            // an instance different than what is specified in the config file.
            OnRavenConfigChanged(new DocumentChangeNotification { Id = RavenConfig.LiveDocumentId });

            this.isInitialized = true;

            return this;
        }

        private void LoadRavenConfig()
        {
            using (var session = this.readOnlyOpsStore.OpenSession())
            {
                var config = session.Load<RavenConfig>(RavenConfig.LiveDocumentId);
                if (config != null)
                {
                    ravenConfig = config;
                }
            }
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
                    logger.Info("{0} {1} store Url did not change. It remains {2}.", wrapper.AccessMode, wrapper.Name, wrapper.InnerStore.Url);
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
                        existingInnerStore.Dispose();
                    }
                }
                else
                {
                    if (newInnerStore != null)
                    {
                        newInnerStore.Dispose();
                    }
                }
            }
        }

        private void OnRavenConfigChanged(DocumentChangeNotification notification)
        {
            if (Interlocked.Exchange(ref updatingRavenConfig, 1) == 0)
            {
                try
                {
                    logger.Debug("RavenConfig change notification received.");

                    LoadRavenConfig();
                    
                    lock (this.allStoreWrappers)
                    {
                        foreach (var wrapper in allStoreWrappers)
                        {
                            wrapper.UpdateInnerStore(wrapper);
                        }
                    }

                    // When the Url of the readOnlyOpsStore changes, we may receive
                    // change notifications from both old and new inner stores.  
                    // Ignore one of them.
                    Thread.Sleep(2000);
                }
                catch (Exception exception)
                {
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
    }
}
