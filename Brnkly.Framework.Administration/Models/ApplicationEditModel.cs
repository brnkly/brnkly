using System.Collections.ObjectModel;

namespace Brnkly.Framework.Administration.Models
{
    public class ApplicationEditModel
    {
        public string Name { get; set; }
        public Collection<LogicalInstanceEditModel> LogicalInstances { get; set; }

        public ApplicationEditModel()
        {
            this.LogicalInstances = new Collection<LogicalInstanceEditModel>();
        }
    }
}
