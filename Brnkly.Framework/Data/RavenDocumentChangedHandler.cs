using System;
using System.Threading;
using System.Web;
using System.Web.Caching;
using Brnkly.Framework.Logging;
using Brnkly.Framework.ServiceBus;
using Raven.Client;

namespace Brnkly.Framework.Data
{
    public abstract class RavenDocumentChangedHandler<T> : IMessageHandler<RavenDocumentChanged>
        where T : class, new()
    {
        private const string CacheKeyFormat = "RavenDocumentChanged-{0}";
        private IDocumentStore store;

        protected RavenDocumentChangedHandler(IDocumentStore store)
        {
            this.store = store;
        }

        protected abstract bool ShouldHandle(string id);
        protected abstract void Update(T item);

        public void Handle(MessageHandlingContext<RavenDocumentChanged> context)
        {
            this.HandleInternal(context.Message.Id, context.Log);
        }

        public void Initialize(string id, LogBuffer logBuffer)
        {
            try
            {
                logBuffer.Information("Initializing " + id + ".");
                this.HandleInternal(id, logBuffer);
            }
            catch (Exception exception)
            {
                logBuffer.Critical(
                    "Failed to initialize the document '" + id + "' due to the exception below. " +
                    "The application may not function correctly until a valid document is received.");
                logBuffer.Critical(exception);
            }
        }

        private void HandleInternal(string id, LogBuffer logBuffer)
        {
            CodeContract.ArgumentNotNullOrWhitespace("id", id);
            CodeContract.ArgumentNotNull("logBuffer", logBuffer);

            if (!this.ShouldHandle(id))
            {
                logBuffer.Verbose("ShouldHandle returned false for id '{0}'. No action taken.", id);
                return;
            }

            var metadata = this.GetMetadata(id);
            if (Interlocked.Exchange(ref metadata.Updating, 1) != 0)
            {
                logBuffer.Verbose("Another thread is performing an update for id '{0}'. No action taken.", id);
                return;
            }

            try
            {
                if (this.UpdateIfEtagIsDifferent(metadata))
                {
                    logBuffer.Verbose("Update performed for id '{0}'.", id);
                }
                else
                {
                    logBuffer.Verbose(
                        "The etag '{0}' has already been received for id '{1}'. No action taken.",
                        metadata.Etag,
                        id);
                }
            }
            finally
            {
                Interlocked.Exchange(ref metadata.Updating, 0);
            }
        }

        private ItemMetadata GetMetadata(string id)
        {
            var cacheKey = string.Format(CacheKeyFormat, id);
            var itemMetadata = HttpRuntime.Cache[cacheKey] as ItemMetadata;
            if (itemMetadata == null)
            {
                this.EnsureMetadataIsInCache(cacheKey, id);
                itemMetadata = HttpRuntime.Cache[cacheKey] as ItemMetadata;
            }

            return itemMetadata;
        }

        private void EnsureMetadataIsInCache(string cacheKey, string id)
        {
            var itemMetadata = new ItemMetadata { Id = id, Etag = null, Updating = 0, };
            try
            {
                HttpRuntime.Cache.Add(
                    cacheKey,
                    itemMetadata,
                    null,
                    Cache.NoAbsoluteExpiration,
                    TimeSpan.FromMinutes(10),
                    CacheItemPriority.AboveNormal,
                    null);
            }
            catch
            {
                // Do nothing.  Add() will throw if the key exists.
            }
        }

        private bool UpdateIfEtagIsDifferent(ItemMetadata metadata)
        {
            bool updated = false;

            Guid? etag;
            var item = this.GetFromStore(metadata.Id, out etag);
            if (etag != metadata.Etag)
            {
                this.Update(item);
                metadata.Etag = etag;
                updated = true;
            }

            return updated;
        }

        private T GetFromStore(string id, out Guid? etag)
        {
            etag = null;
            T item;

            using (var session = this.store.OpenSession())
            {
                item = session.Load<T>(id);

                if (item != null)
                {
                    etag = session.Advanced.GetEtagFor(item);
                }
            }

            return item;
        }

        private class ItemMetadata
        {
            public string Id;
            public Guid? Etag;
            public int Updating;
        }
    }
}
