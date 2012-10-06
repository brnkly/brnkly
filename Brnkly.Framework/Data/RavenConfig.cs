using System.Collections.ObjectModel;

namespace Brnkly.Framework.Data
{
    public class RavenConfig
    {
        public static readonly string StorageId = "brnkly/config/raven";

        public string Id { get; private set; }
        public Collection<RavenStore> Stores { get; private set; }

        public RavenConfig()
        {
            this.Id = StorageId;
            this.Stores = new Collection<RavenStore>();
        }
    }
}
