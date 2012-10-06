using System.Collections.ObjectModel;

namespace Brnkly.Framework.Configuration
{
    public class MachineGroup
    {
        public string Name { get; private set; }
        public Collection<string> MachineNames { get; private set; }

        public MachineGroup(string name)
        {
            this.Name = name;
            this.MachineNames = new Collection<string>();
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
