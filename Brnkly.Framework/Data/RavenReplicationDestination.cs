
namespace Brnkly.Framework.Data
{
    public class RavenReplicationDestination
    {
        public string ServerName { get; private set; }
        public bool IsTransitive { get; private set; }

        public RavenReplicationDestination(string serverName, bool isTransitive)
        {
            this.ServerName = serverName;
            this.IsTransitive = isTransitive;
        }

        // To handle deserialization from old data, where destination was just a string.
        public static implicit operator RavenReplicationDestination(string destination)
        {
            return new RavenReplicationDestination(destination, false);
        }
    }
}
