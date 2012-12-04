using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Brnkly.Raven.Admin.Models
{
    public class RavenConfigModel
    {
        public string Id { get; set; }
        public Collection<StoreModel> Stores { get; set; }

        public RavenConfigModel()
        {
            this.Id = RavenConfig.DocumentId;
            this.Stores = new Collection<StoreModel>();
        }
    }
}