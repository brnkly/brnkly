using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Framework.Configuration
{
    public class LogicalInstance
    {
        public string Name { get; private set; }
        public Collection<Machine> Machines { get; private set; }

        public LogicalInstance(string name)
        {
            this.Name = name;
            this.Machines = new Collection<Machine>();
        }

        public void ExpandMachineGroups(IEnumerable<MachineGroup> groups)
        {
            var itemsToExpand =
                from machine in this.Machines
                join g in groups on machine.Name.ToLowerInvariant() equals g.Name.ToLowerInvariant()
                select new { Group = machine, ReplaceWith = g.MachineNames };

            foreach (var item in itemsToExpand.ToArray())
            {
                this.Machines.Remove(item.Group);
                foreach (var name in item.ReplaceWith)
                {
                    if (!this.Machines.Any
                        (m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        this.Machines.Add(new Machine(name));
                    }
                }
            }
        }

        public override string ToString()
        {
            return string.Format(
                "Name='{0}', Machines={1}.",
                this.Name,
                string.Join(", ", this.Machines.Select(i => i.ToString())));
        }
    }
}
