using System;
using System.Collections.Specialized;
using System.Reflection;
using Raven.Client;
using Raven.Client.Changes;
using Raven.Client.Connection;
using Raven.Client.Connection.Async;
using Raven.Client.Document;
using Raven.Client.Indexes;

namespace Brnkly.Raven
{
    /// <summary>
    /// A Raven document store whose connection string can be updated at runtime.
    /// Use this instead of Raven's DocumentStore type when creating and registering a store.
    /// </summary>
    /// <example>
    /// <para>To register the store at startup, do this in your PlatformAreaRegistration-derived class:</para>
    /// <code>
    /// DocumentStoreWrapper.Register(state.Container, StoreName.MyStore);
    /// </code>
    /// <para>
    /// To inject and use the store into a class, add a constructor parameter typed as 
    /// IDocumentStore, with a Unity Dependency attribute containing the store name.
    /// If injecting a writable store (e.g., in an editorial area), append ".ReadWrite" to
    /// the store name in the Dependency attribute (but not when registering the store).
    /// </para>
    /// <code>
    /// using Microsoft.Practices.Unity;
    /// 
    /// public class MyController
    /// {
    ///     private IDocumentStore store;
    ///     
    ///     public MyController([Dependency(StoreName.MyStore)] IDocumentStore store)
    ///     {
    ///         this.store = store;
    ///     }
    ///     
    ///     public ActionResult MyAction(string id)
    ///     {
    ///         MyDomainType domainObject = null;
    ///         using (var session = store.OpenSession())
    ///         {
    ///             domainObject = session.Load&lt;MyDomainType&gt;(id);
    ///         }
    ///         
    ///         var model = Mapper.Map&lt;MyModelType&gt;(domainObject);
    ///         return this.View(model);
    ///     }
    /// }
    /// </code>
    /// </example>
    public sealed class DocumentStoreWrapper : IDocumentStore
    {
        private Assembly assemblyToScanForIndexingTasks;

        public string Name { get; private set; }
        public AccessMode AccessMode { get; private set; }
        public bool IsInitialized { get; internal set; }
        internal DocumentStore InnerStore { get; set; }

        internal DocumentStoreWrapper(
            string name, 
            AccessMode accessMode = AccessMode.ReadOnly)
        {
            name.Ensure("name").IsNotNullOrWhiteSpace();

            this.IsInitialized = false;
            this.Name = name;
            this.AccessMode = accessMode;
        }

        public DocumentStoreWrapper CreateIndexes(Assembly assemblyToScanForIndexingTasks)
        {

            // TODO: Need to allow multiple of these to be added.
            assemblyToScanForIndexingTasks.Ensure("assemblyToScanForIndexingTasks").IsNotNull();

            this.assemblyToScanForIndexingTasks = assemblyToScanForIndexingTasks;

            try
            {
                IndexCreation.CreateIndexes(assemblyToScanForIndexingTasks, this.InnerStore);
            }
            catch (Exception exception)
            {
                //Log.Error(
                //    exception,
                //    string.Format(
                //        "Failed to create indexes for assembly {0}.",
                //        assemblyToScanForIndexingTasks.FullName),
                //    LogPriority.Application);
            }

            return this;
        }

        private void ThrowIfNotInitialized()
        {
            if (!this.IsInitialized)
            {
                var message = string.Format("The '{0}' store has not been initialized. ", this.Name);
                if (string.Equals(this.Name, "Operations"))
                {
                    message += "Check the connection string in the web.config file.";
                }
                else
                {
                    message += "Check the Operations connection string in the web.config file, ";
                    //message += "and the RavenDB servers listed in the document '" + EnvironmentConfig.StorageId + "' ";
                    message += "in the Operations store.";
                }

                throw new InvalidOperationException(message);
            }
        }

        public IDisposable AggressivelyCacheFor(TimeSpan cacheDuration)
        {
            this.ThrowIfNotInitialized();
            return this.InnerStore.AggressivelyCacheFor(cacheDuration);
        }

        public IAsyncDatabaseCommands AsyncDatabaseCommands
        {
            get
            {
                this.ThrowIfNotInitialized();
                return this.InnerStore.AsyncDatabaseCommands;
            }
        }

        public IDatabaseChanges Changes(string database = null)
        {
            if (database != null)
            {
                throw new NotImplementedException("DocumentStoreWrapper does not support specifying a database name in this method. The database name must be specified in the constructor.");
            }

            // TODO: Need to track these, so we can re-add to a new inner store.
            this.ThrowIfNotInitialized();
            return this.InnerStore.Changes();
        }

        public DocumentConvention Conventions
        {
            get
            {
                this.ThrowIfNotInitialized();
                return this.InnerStore.Conventions;
            }
        }

        public IDatabaseCommands DatabaseCommands
        {
            get
            {
                this.ThrowIfNotInitialized();
                return this.InnerStore.DatabaseCommands;
            }
        }

        public IDisposable DisableAggressiveCaching()
        {
            this.ThrowIfNotInitialized();
            return this.InnerStore.DisableAggressiveCaching();
        }

        public void ExecuteIndex(AbstractIndexCreationTask indexCreationTask)
        {
            // TODO: Need to track these, so we can re-execute on a new inner store.
            this.ThrowIfNotInitialized();
            this.InnerStore.ExecuteIndex(indexCreationTask);
        }

        public Guid? GetLastWrittenEtag()
        {
            this.ThrowIfNotInitialized();
            return this.InnerStore.GetLastWrittenEtag();
        }

        public string Identifier
        {
            get
            {
                return this.InnerStore.Identifier;
            }
            set
            {
                throw new InvalidOperationException(
                    "Setting the identifier is not supported. " +
                    "Instead, use the storeName parameter when constructing the object.");
            }
        }

        public IDocumentStore Initialize()
        {
            return this.InnerStore.Initialize();
        }
        
        public HttpJsonRequestFactory JsonRequestFactory
        {
            get
            {
                this.ThrowIfNotInitialized();
                return this.InnerStore.JsonRequestFactory;
            }
        }

        public IAsyncDocumentSession OpenAsyncSession(string database)
        {
            throw new NotImplementedException("DocumentStoreWrapper does not support opening a session with a database name. The database name must be specified in the constructor.");
        }

        public IAsyncDocumentSession OpenAsyncSession()
        {
            this.ThrowIfNotInitialized();
            return this.InnerStore.OpenAsyncSession();
        }

        public IDocumentSession OpenSession(OpenSessionOptions sessionOptions)
        {
            if (!string.IsNullOrWhiteSpace(sessionOptions.Database))
            {
                throw new NotImplementedException("DocumentStoreWrapper does not support opening a session with a database name. The database name must be specified in the constructor.");
            }

            return this.InnerStore.OpenSession(sessionOptions);
        }

        public IDocumentSession OpenSession(string database)
        {
            throw new NotImplementedException("DocumentStoreWrapper does not support opening a session with a database name. The database name must be specified in the constructor.");
        }

        public IDocumentSession OpenSession()
        {
            this.ThrowIfNotInitialized();
            return this.InnerStore.OpenSession();
        }

        public NameValueCollection SharedOperationsHeaders
        {
            get
            {
                this.ThrowIfNotInitialized();
                return this.InnerStore.SharedOperationsHeaders;
            }
        }

        public string Url
        {
            get { return this.InnerStore.Url; }
        }

        public event EventHandler AfterDispose
        {
            add { this.InnerStore.AfterDispose += value; }
            remove { this.InnerStore.AfterDispose -= value; }
        }

        public bool WasDisposed
        {
            get { return this.InnerStore.WasDisposed; }
        }

        public void Dispose()
        {
            this.InnerStore.Dispose();
        }
    }
}
