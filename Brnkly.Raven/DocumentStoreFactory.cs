using System;
using System.Collections.Concurrent;
using System.Linq;
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

        private bool isInitialized;
        private ConcurrentBag<DocumentStoreWrapper> allStoreWrappers =
            new ConcurrentBag<DocumentStoreWrapper>();
        private RavenConfig ravenConfig = new RavenConfig();

        public Uri OperationsStoreUrl { get; private set; }

        public DocumentStoreFactory(Uri operationsStoreUrl)
        {
            var trimmedUrl = operationsStoreUrl.TrimTrailingSlash();
            EnsureUriPathIncludesDatabase(trimmedUrl);
            this.OperationsStoreUrl = trimmedUrl;
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
                        .Subscribe(OnConfigChanged);
                };
            var readOnlyOpsStore = this
                .GetOrCreate(this.OperationsStoreUrl.GetDatabaseName(), AccessMode.ReadOnly, initializer)
                .Initialize();

            readOnlyOpsStore.DatabaseCommands.EnsureDatabaseExists(
                this.OperationsStoreUrl.GetDatabaseName());

            LoadRavenConfig(readOnlyOpsStore);
            this.isInitialized = true;
            return this;
        }

        private void LoadRavenConfig(IDocumentStore opsStore)
        {
            using (var session = opsStore.OpenSession())
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

            try
            {
                newInnerStore = this.CreateInnerStore(wrapper);

                if (wrapper.IsInitialized &&
                    wrapper.InnerStore.Url.Equals(newInnerStore.Url, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                innerStoreInitializer(newInnerStore);
                wrapper.InnerStore = newInnerStore;
                wrapper.IsInitialized = true;

                logger.Info("{0} {1} store Url set to {2}.",
                    wrapper.AccessMode,
                    wrapper.Name,
                    newInnerStore.Url);
            }
            catch (Exception exception)
            {
                LogException(exception, wrapper, newInnerStore);

                if (exception.IsFatal())
                {
                    throw;
                }
            }
        }

        private void OnConfigChanged(DocumentChangeNotification notification)
        {
            logger.Info("RavenConfig change notification received.");
            lock (this.allStoreWrappers)
            {
                foreach (var wrapper in allStoreWrappers)
                {
                    wrapper.UpdateInnerStore(wrapper);
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

        internal DocumentStore CreateInnerStore(DocumentStoreWrapper wrapper)
        {
            var storeInstance = GetClosestInstanceOrDefault(wrapper);
            var innerStore = CreateDocumentStore(storeInstance);

            if (wrapper.AccessMode == AccessMode.ReadOnly)
            {
                innerStore.RegisterListener((IDocumentStoreListener)new ReadOnlyListener());
                innerStore.RegisterListener((IDocumentDeleteListener)new ReadOnlyListener());
            }

            return innerStore;
        }

        internal DocumentStore CreateDocumentStore(Instance storeInstance)
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
                    Url = new Uri(this.OperationsStoreUrl, wrapper.Name.ToLowerInvariant()),
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
