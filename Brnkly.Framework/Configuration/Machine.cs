
namespace Brnkly.Framework.Configuration
{
    public class Machine
    {
        public string Name { get; private set; }

        public Machine(string name)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
