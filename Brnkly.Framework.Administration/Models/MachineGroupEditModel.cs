using System.Collections.ObjectModel;

namespace Brnkly.Framework.Administration.Models
{
    public class MachineGroupEditModel
    {
        public string Name { get; set; }
        public Collection<string> MachineNames { get; set; }

        public MachineGroupEditModel()
        {
            this.MachineNames = new Collection<string>();
        }
    }
}
