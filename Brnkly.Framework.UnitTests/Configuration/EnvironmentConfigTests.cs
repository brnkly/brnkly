using System.Linq;
using Brnkly.Framework.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Brnkly.Framework.UnitTests.Configuration
{
    [TestClass]
    public class EnvironmentConfigTests
    {
        [TestMethod]
        public void ExpandMachineGroups_should_replace_groups_with_machines()
        {
            var instance =
                new LogicalInstance("MyInstance")
                {
                    Machines = 
                    { 
                        new Machine("MyGroup"), 
                        new Machine("MachineC"), 
                    }
                };

            var groups = new MachineGroup("MyGroup")
            {
                MachineNames = { "MachineA", "MachineB" }
            };

            instance.ExpandMachineGroups(new[] { groups });

            Assert.AreEqual(3, instance.Machines.Count);
            Assert.AreEqual(1, instance.Machines.Count(m => m.Name == "MachineA"));
            Assert.AreEqual(1, instance.Machines.Count(m => m.Name == "MachineB"));
            Assert.AreEqual(1, instance.Machines.Count(m => m.Name == "MachineC"));
        }

        [TestMethod]
        public void ExpandMachineGroups_should_remove_duplicate_machine_names()
        {
            var instance =
                new LogicalInstance("MyInstance")
                {
                    Machines = 
                    { 
                        new Machine("MyGroup"), 
                        new Machine("MachineA"), 
                        new Machine("MachineC"), 
                    }
                };

            var groups = new MachineGroup("MyGroup")
            {
                MachineNames = { "MachineA", "MachineB" }
            };

            instance.ExpandMachineGroups(new[] { groups });

            Assert.AreEqual(3, instance.Machines.Count);
            Assert.AreEqual(1, instance.Machines.Count(m => m.Name == "MachineA"));
            Assert.AreEqual(1, instance.Machines.Count(m => m.Name == "MachineB"));
            Assert.AreEqual(1, instance.Machines.Count(m => m.Name == "MachineC"));
        }
    }
}
