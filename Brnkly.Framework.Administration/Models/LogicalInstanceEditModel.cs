using System.Collections.ObjectModel;

namespace Brnkly.Framework.Administration.Models
{
    public class LogicalInstanceEditModel
    {
        public string Name { get; set; }
        public Collection<MachineEditModel> Machines { get; set; }

        public LogicalInstanceEditModel()
        {
            this.Machines = new Collection<MachineEditModel>();
        }
    }
}
