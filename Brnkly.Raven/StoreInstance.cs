using System;
using System.Linq;
using System.Collections.ObjectModel;

namespace Brnkly.Raven
{
    public class StoreInstance
    {
        public Uri Uri { get; private set; }
        public string DatabaseName { get; private set; }
        public bool AllowReads { get; private set; }
        public bool AllowWrites { get; private set; }
        public Collection<Destination> Destinations { get; private set; }

        public StoreInstance(Uri uri, bool allowReads, bool allowWrites)
        {
            uri.Ensure("uri").IsNotNull();

            this.Uri = uri;
            this.DatabaseName = this.Uri.GetDatabaseName();
            this.AllowReads = allowReads;
            this.AllowWrites = allowWrites;
            this.Destinations = new Collection<Destination>();
        }
    }
}
