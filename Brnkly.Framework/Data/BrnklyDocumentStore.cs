using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Brnkly.Framework.Configuration;
using Brnkly.Framework.Logging;
using Microsoft.Practices.Unity;
using Raven.Client;
using Raven.Client.Connection;
using Raven.Client.Connection.Async;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Client.Listeners;

namespace Brnkly.Framework.Data
{
    /// <summary>
    /// A platform-specific Raven document store, whose connection string can be updated at runtime.
    /// All store names must be declared in the Brnkly.Framework.StoreName class.
    /// Use this instead of Raven's DocumentStore type when creating and registering a store.
    /// </summary>
    /// <example>
    /// <para>To register the store at startup, do this in your PlatformAreaRegistration-derived class:</para>
    /// <code>
    /// BrnklyDocumentStore.Register(state.Container, StoreName.MyStore);
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
    public sealed class BrnklyDocumentStore : IDocumentStore
    {
        public static readonly string StoreUrlFormat = "http://{0}:8081/ravendb/databases/{1}";
        public static readonly string ServerOnlyUrlFormat = "http://{0}:8081/ravendb";

        private static readonly ConcurrentBag<BrnklyDocumentStore> AllStores =
            new ConcurrentBag<BrnklyDocumentStore>();
        private static RavenConfig RavenConfig = new RavenConfig();
        private static string DefaultRavenServer;

        public string Name { get; private set; }
        public StoreAccessMode AccessMode { get; private set; }

        private string nameWithAccessMode;
        private DocumentStore innerStore;
        private Assembly assemblyToScanForIndexingTasks;

        static BrnklyDocumentStore()
        {
            try
            {
                var server = ConfigurationManager.AppSettings["DefaultRavenServer"];
                if (string.IsNullOrWhiteSpace(server))
                {
                    throw new InvalidOperationException("The value of the DefaultRavenServer app setting is invalid. Use a valid machine name.");
                }

                DefaultRavenServer = server;
            }
            catch (Exception exception)
            {
                Log.Critical(
                    exception,
                    "Could not retrieve the DefaultRavenServer app setting from config file. " +
                    "The application will not function properly until a valid Raven config is received.",
                    LogPriority.Application);
            }
        }

        public bool IsInitialized { get; private set; }

        public BrnklyDocumentStore(string name, StoreAccessMode accessMode = StoreAccessMode.ReadOnly)
        {
            CodeContract.ArgumentNotNullOrWhitespace("name", name);

            StoreName.ValidateName(name);

            this.IsInitialized = false;
            this.Name = name;
            this.AccessMode = accessMode;
            this.nameWithAccessMode =
                (this.AccessMode == StoreAccessMode.ReadOnly) ?
                this.Name :
                this.Name + ".ReadWrite";

            this.innerStore = CreateDocumentStore(this.Name, this.AccessMode);
        }

        public static BrnklyDocumentStore Register(
            IUnityContainer container,
            string name,
            StoreAccessMode accessMode = StoreAccessMode.ReadOnly)
        {
            CodeContract.ArgumentNotNull("container", container);
            CodeContract.ArgumentNotNullOrWhitespace("name", name);

            var existingStore = AllStores.SingleOrDefault(
                store =>
                    store.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    store.AccessMode == accessMode);
            if (existingStore != null)
            {
                return existingStore;
            }

            var newStore = new BrnklyDocumentStore(name, accessMode);
            AllStores.Add(newStore);
            container.RegisterInstance<IDocumentStore>(newStore.nameWithAccessMode, newStore);

            newStore.UpdateInnerStore();

            return newStore;
        }

        public BrnklyDocumentStore CreateIndexes(Assembly assemblyToScanForIndexingTasks)
        {
            CodeContract.ArgumentNotNull("assemblyToScanForIndexingTasks", assemblyToScanForIndexingTasks);

            this.assemblyToScanForIndexingTasks = assemblyToScanForIndexingTasks;

            try
            {
                IndexCreation.CreateIndexes(assemblyToScanForIndexingTasks, this.innerStore);
            }
            catch (Exception exception)
            {
                Log.Error(
                    exception,
                    string.Format(
                        "Failed to create indexes for assembly {0}.",
                        assemblyToScanForIndexingTasks.FullName),
                    LogPriority.Application);
            }

            return this;
        }

        internal static void UpdateAllStores(RavenConfig ravenConfig)
        {
            LogBuffer.Current.LogPriority = LogPriority.Application;

            lock (AllStores)
            {
                RavenConfig = ravenConfig ?? new RavenConfig();

                foreach (var store in AllStores)
                {
                    store.UpdateInnerStore();
                }
            }
        }

        private bool UpdateInnerStore()
        {
            DocumentStore existingInnerStore = this.innerStore;
            DocumentStore newInnerStore = null;
            bool updateSucceeded = false;

            try
            {
                newInnerStore = CreateDocumentStore(this.Name, this.AccessMode);
                var newUrl = newInnerStore.Url;
                newInnerStore.Initialize();

                if (this.assemblyToScanForIndexingTasks != null)
                {
                    IndexCreation.CreateIndexes(this.assemblyToScanForIndexingTasks, newInnerStore);
                }

                newInnerStore.JsonRequestFactory.ConfigureRequest +=
                    new EventHandler<WebRequestEventArgs>(JsonRequestFactory_ConfigureRequest);

                this.innerStore = newInnerStore;
                this.IsInitialized = true;
                updateSucceeded = true;
                LogBuffer.Current.LogPriority = LogPriority.Application;
                LogBuffer.Current.Information(
                    "The store '{0}' was updated to point to '{1}'.",
                    this.nameWithAccessMode,
                    newUrl);
            }
            catch (Exception exception)
            {
                LogException(exception, newInnerStore);
            }
            finally
            {
                if (updateSucceeded &&
                    existingInnerStore != null)
                {
                    existingInnerStore.Dispose();
                }
            }

            return updateSucceeded;
        }

        private static DocumentStore CreateDocumentStore(string storeName, StoreAccessMode accessMode)
        {
            string serverName = GetClosestServer(storeName, accessMode);
            var documentStore = CreateDocumentStore(storeName, serverName);

            if (accessMode == StoreAccessMode.ReadOnly)
            {
                documentStore.RegisterListener((IDocumentStoreListener)new ReadOnlyListener());
                documentStore.RegisterListener((IDocumentDeleteListener)new ReadOnlyListener());
            }

            return documentStore;
        }

        private static DocumentStore CreateDocumentStore(string storeName, string serverName)
        {
            var store = new DocumentStore
            {
                Identifier = storeName,
                DefaultDatabase = storeName,
                // Guid.NewGuid() is okay because we don't care about recovering a tx after an app restart.
                ResourceManagerId = Guid.NewGuid(),
                Conventions =
                {
                    FailoverBehavior = FailoverBehavior.AllowReadsFromSecondariesAndWritesToSecondaries
                }
            };

            if (string.IsNullOrWhiteSpace(serverName))
            {
                serverName = DefaultRavenServer;
            }

            store.Url = string.Format(StoreUrlFormat, serverName, storeName);

            return store;
        }

        private static void JsonRequestFactory_ConfigureRequest(object sender, WebRequestEventArgs e)
        {
            e.Request.Timeout = 10000;
        }

        private static string GetClosestServer(string storeName, StoreAccessMode accessMode)
        {
            var ravenStore = RavenConfig.Stores.SelectByName(storeName);
            if (ravenStore != null)
            {
                return ravenStore.GetClosestReplica(
                    Environment.MachineName,
                    accessMode == StoreAccessMode.ReadWrite);
            }

            return null;
        }

        private void LogException(Exception exception, DocumentStore newInnerStore)
        {
            string newUrl = (newInnerStore == null) ? null : newInnerStore.Url;
            string message = string.Format(
                "The store '{0}' could not be initialized with " +
                "the Url '{1}' due to the exception below. ",
                this.Identifier,
                newUrl);

            if (this.IsInitialized &&
                this.innerStore != null &&
                !this.innerStore.WasDisposed)
            {
                message += string.Format(
                    "The store will continue to use the RavenDB server at '{0}'.",
                    this.innerStore.Url);
            }

            LogBuffer.Current.LogPriority = LogPriority.Application;
            LogBuffer.Current.Critical(message);
            LogBuffer.Current.Critical(exception);
        }

        private void ThrowIfNotInitialized()
        {
            if (!this.IsInitialized)
            {
                var message = string.Format("The '{0}' store has not been initialized. ", this.Name);
                if (string.Equals(this.Name, StoreName.Operations))
                {
                    message += "Check the connection string in the web.config file.";
                }
                else
                {
                    message += "Check the Operations connection string in the web.config file, ";
                    message += "and the RavenDB servers listed in the document '" + EnvironmentConfig.StorageId + "' ";
                    message += "in the Operations store.";
                }

                throw new InvalidOperationException(message);
            }
        }

        #region Partially supported or unsupported IDocumentStore members

        IDocumentStore IDocumentStore.Initialize()
        {
            throw new NotImplementedException("Use BrnklyDocumentStore.InitializeAndRegister() instead.");
        }

        public IAsyncDocumentSession OpenAsyncSession(string database)
        {
            throw new NotImplementedException("BrnklyDocumentStore does not support opening a session with a database name. The database name must be specified in the constructor.");
        }

        public IDocumentSession OpenSession(string database)
        {
            throw new NotImplementedException("BrnklyDocumentStore does not support opening a session with a database name. The database name must be specified in the constructor.");
        }

        public IDocumentSession OpenSession(OpenSessionOptions sessionOptions)
        {
            if (!string.IsNullOrWhiteSpace(sessionOptions.Database))
            {
                throw new NotImplementedException("BrnklyDocumentStore does not support opening a session with a database name. The database name must be specified in the constructor.");
            }

            return this.innerStore.OpenSession(sessionOptions);
        }

        public string Identifier
        {
            get
            {
                return this.innerStore.Identifier;
            }
            set
            {
                throw new InvalidOperationException(
                    "Setting the identifier is not supported. " +
                    "Instead, use the storeName parameter when constructing the object.");
            }
        }

        #endregion

        #region IDocumentStore members delegated to innerStore

        public IDisposable AggressivelyCacheFor(TimeSpan cacheDuration)
        {
            this.ThrowIfNotInitialized();
            return this.innerStore.AggressivelyCacheFor(cacheDuration);
        }

        public IAsyncDatabaseCommands AsyncDatabaseCommands
        {
            get
            {
                this.ThrowIfNotInitialized();
                return this.innerStore.AsyncDatabaseCommands;
            }
        }

        public DocumentConvention Conventions
        {
            get
            {
                this.ThrowIfNotInitialized();
                return this.innerStore.Conventions;
            }
        }

        public IDatabaseCommands DatabaseCommands
        {
            get
            {
                this.ThrowIfNotInitialized();
                return this.innerStore.DatabaseCommands;
            }
        }

        public IDisposable DisableAggressiveCaching()
        {
            this.ThrowIfNotInitialized();
            return this.innerStore.DisableAggressiveCaching();
        }

        public void ExecuteIndex(AbstractIndexCreationTask indexCreationTask)
        {
            this.ThrowIfNotInitialized();
            this.innerStore.ExecuteIndex(indexCreationTask);
        }

        public Guid? GetLastWrittenEtag()
        {
            this.ThrowIfNotInitialized();
            return this.innerStore.GetLastWrittenEtag();
        }

        public HttpJsonRequestFactory JsonRequestFactory
        {
            get
            {
                this.ThrowIfNotInitialized();
                return this.innerStore.JsonRequestFactory;
            }
        }

        public IAsyncDocumentSession OpenAsyncSession()
        {
            this.ThrowIfNotInitialized();
            return this.innerStore.OpenAsyncSession();
        }

        public IDocumentSession OpenSession()
        {
            this.ThrowIfNotInitialized();
            return this.innerStore.OpenSession();
        }

        public NameValueCollection SharedOperationsHeaders
        {
            get
            {
                this.ThrowIfNotInitialized();
                return this.innerStore.SharedOperationsHeaders;
            }
        }

        public string Url
        {
            get { return this.innerStore.Url; }
        }

        public event EventHandler AfterDispose
        {
            add { this.innerStore.AfterDispose += value; }
            remove { this.innerStore.AfterDispose -= value; }
        }

        public bool WasDisposed
        {
            get { return this.innerStore.WasDisposed; }
        }

        public void Dispose()
        {
            this.innerStore.Dispose();
        }

        #endregion
    }
}
