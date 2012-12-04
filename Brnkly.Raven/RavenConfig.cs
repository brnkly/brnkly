using System.Collections.ObjectModel;

namespace Brnkly.Raven
{
    public class RavenConfig
    {
        public static readonly string DocumentId = "brnkly/config/raven";

        public string Id { get; private set; }
        public Collection<Store> Stores { get; private set; }

        public RavenConfig()
        {
            this.Id = DocumentId;
            this.Stores = new Collection<Store>();
        }
    }
}
