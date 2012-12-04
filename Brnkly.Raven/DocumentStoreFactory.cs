using System;
using System.Collections.Concurrent;
using System.Linq;
using Raven.Client.Document;
using Raven.Client.Listeners;

namespace Brnkly.Raven
{
    public sealed class DocumentStoreFactory
    {
        private ConcurrentBag<DocumentStoreWrapper> AllStores =
            new ConcurrentBag<DocumentStoreWrapper>();
        private RavenConfig RavenConfig = new RavenConfig();

        public Uri OperationsUri { get; private set; }

        public DocumentStoreFactory(Uri operationsUri)
        {
            this.OperationsUri = operationsUri;
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

            var newStore = new DocumentStoreWrapper(this, name, accessMode);
            AllStores.Add(newStore);

            this.UpdateInnerStore(newStore);

            return newStore;
        }

        internal void UpdateAllStores(RavenConfig ravenConfig)
        {
            //LogBuffer.Current.LogPriority = LogPriority.Application;

            lock (this.AllStores)
            {
                this.RavenConfig = ravenConfig ?? new RavenConfig();

                foreach (var store in AllStores)
                {
                    this.UpdateInnerStore(store);
                }
            }
        }

        private void UpdateInnerStore(DocumentStoreWrapper wrapper)
        {
            DocumentStore existingInnerStore = wrapper.InnerStore;
            DocumentStore newInnerStore = null;

            try
            {
                newInnerStore = this.CreateInnerStore(wrapper);
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
                newInnerStore.Identifier,
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
            var storeInstance = GetClosestStoreInstanceOrDefault(wrapper);
            var innerStore = CreateDocumentStore(storeInstance);

            if (wrapper.AccessMode == AccessMode.ReadOnly)
            {
                innerStore.RegisterListener((IDocumentStoreListener)new ReadOnlyListener());
                innerStore.RegisterListener((IDocumentDeleteListener)new ReadOnlyListener());
            }

            return innerStore;
        }

        internal DocumentStore CreateDocumentStore(StoreInstance storeInstance)
        {
            var store = new DocumentStore
            {
                Identifier = storeInstance.DatabaseName,
                DefaultDatabase = storeInstance.DatabaseName,
                Url = storeInstance.Uri.ToString(),
                // Guid.NewGuid() is okay because we don't care about recovering a tx after an app restart.
                ResourceManagerId = Guid.NewGuid(),
                Conventions =
                {
                    FailoverBehavior = FailoverBehavior.AllowReadsFromSecondariesAndWritesToSecondaries
                },
            };

            return store;
        }

        private StoreInstance GetClosestStoreInstanceOrDefault(DocumentStoreWrapper wrapper)
        {
            var store = RavenConfig.Stores.SelectByName(wrapper.Name);
            if (store != null)
            {
                return store.GetClosestReplica(
                    Environment.MachineName,
                    wrapper.AccessMode == AccessMode.ReadWrite);
            }
            
            return new StoreInstance(
                new Uri(this.OperationsUri, wrapper.Name.ToLowerInvariant()),
                true,
                wrapper.AccessMode == AccessMode.ReadWrite);
        }
    }
}
