using System.Collections.ObjectModel;

namespace Brnkly.Raven.Admin.Models
{
    public class StoreModel
    {
        public string Name { get; set; }
        public Collection<InstanceModel> Instances { get; set; }
        
        public StoreModel()
        {
			this.Instances = new Collection<InstanceModel>();
        }
    }
}