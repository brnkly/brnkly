using System;
using Raven.Client.Listeners;
using Raven.Json.Linq;

namespace Brnkly.Framework.Data
{
    public class ReadOnlyListener : IDocumentStoreListener, IDocumentDeleteListener
    {
        private const string ErrorMessage =
            "The store is read-only. To enable writes, pass StoreAccessMode.ReadWrite to its constructor.";

        public void AfterStore(string key, object entityInstance, RavenJObject metadata)
        {
            // Do nothing.
        }

        public bool BeforeStore(string key, object entityInstance, RavenJObject metadata, RavenJObject original)
        {
            throw new InvalidOperationException(ErrorMessage);
        }

        public void BeforeDelete(string key, object entityInstance, RavenJObject metadata)
        {
            throw new InvalidOperationException(ErrorMessage);
        }
    }
}
