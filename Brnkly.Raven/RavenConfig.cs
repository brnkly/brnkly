using System.Collections.ObjectModel;

namespace Brnkly.Raven
{
    public class RavenConfig
    {
        public static readonly string PendingDocumentId = "brnkly/ravenconfig/pending";
        public static readonly string LiveDocumentId = "brnkly/ravenconfig";

        public string Id { get; private set; }
        public Collection<Store> Stores { get; private set; }

        public RavenConfig()
        {
            this.Stores = new Collection<Store>();
        }
    }
}
