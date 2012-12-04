using System;
using System.Collections.Concurrent;
using System.Linq;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Client.Listeners;

namespace Brnkly.Raven
{
    public sealed class DocumentStoreFactory
    {
        private ConcurrentBag<DocumentStoreWrapper> AllStores =
            new ConcurrentBag<DocumentStoreWrapper>();
        private RavenConfig RavenConfig = new RavenConfig();

        public Uri OperationsUrl { get; private set; }

        public DocumentStoreFactory(Uri operationsUrl)
        {
            var trimmedUrl = operationsUrl.TrimTrailingSlash();
            EnsureUriPathIncludesDatabase(trimmedUrl);
            this.OperationsUrl = trimmedUrl;
        }

        public DocumentStoreFactory Initialize()
        {
            var readOnlyOpsStore = new DocumentStoreWrapper(
                this.OperationsUrl.GetDatabaseName(), 
                AccessMode.ReadOnly);
            AllStores.Add(readOnlyOpsStore);
            this.UpdateInnerStore(readOnlyOpsStore);

            readOnlyOpsStore.DatabaseCommands.EnsureDatabaseExists(
                this.OperationsUrl.GetDatabaseName());

            UpdateRavenConfig(readOnlyOpsStore);
            UpdateAllStores();
            return this;
        }

        private void UpdateRavenConfig(IDocumentStore opsStore)
        {
            using (var session = opsStore.OpenSession())
            {
                var config = session.Load<RavenConfig>(RavenConfig.LiveDocumentId);
                if (config != null)
                {
                    RavenConfig = config;
                }
            }
        }

        private void UpdateAllStores()
        {
            //LogBuffer.Current.LogPriority = LogPriority.Application;

            lock (this.AllStores)
            {
                foreach (var store in AllStores)
                {
                    this.UpdateInnerStore(store);
                }
            }
        }

        public DocumentStoreWrapper GetOrCreate(
            string name,
            AccessMode accessMode = AccessMode.ReadOnly)
        {
            name.Ensure("name").IsNotNullOrWhiteSpace();

            var existingStore = AllStores.SingleOrDefault(
                store =>
                    store.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    store.AccessMode == accessMode);
            if (existingStore != null)
            {
                return existingStore;
            }

            var newStore = new DocumentStoreWrapper(name, accessMode);
            AllStores.Add(newStore);
            this.UpdateInnerStore(newStore);

            return newStore;
        }

        private void UpdateInnerStore(DocumentStoreWrapper wrapper)
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

                newInnerStore.Initialize();
                wrapper.InnerStore = newInnerStore;
                wrapper.IsInitialized = true;
                //LogBuffer.Current.LogPriority = LogPriority.Application;
                //LogBuffer.Current.Information(
                //    "The store '{0}' was updated to point to '{1}'.",
                //    this.nameWithAccessMode,
                //    newUrl);
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

        private void LogException(
            Exception exception,
            DocumentStoreWrapper wrapper,
            DocumentStore newInnerStore)
        {
            string newUrl = (newInnerStore == null) ? null : newInnerStore.Url;
            string message = string.Format(
                "The store '{0}' could not be initialized with " +
                "the Url '{1}' due to the exception below. ",
                newInnerStore == null ? null : newInnerStore.Identifier,
                newUrl);

            if (wrapper.IsInitialized &&
                newInnerStore != null &&
                !wrapper.InnerStore.WasDisposed)
            {
                message += string.Format(
                    "The store will continue to use the Raven instance at '{0}'.",
                    wrapper.InnerStore.Url);
            }

            //LogBuffer.Current.LogPriority = LogPriority.Application;
            //LogBuffer.Current.Critical(message);
            //LogBuffer.Current.Critical(exception);
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
            var databaseName = storeInstance.Url.GetDatabaseName();
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
            var store = RavenConfig.Stores.SelectByName(wrapper.Name);
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
                    Url = new Uri(this.OperationsUrl, wrapper.Name.ToLowerInvariant()),
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
