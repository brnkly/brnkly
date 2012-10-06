using System.Security.Principal;

namespace Brnkly.Framework.Security
{
    public class PlatformIdentity : GenericIdentity
    {
        public PlatformIdentity(string name, string type)
            : base(name, type)
        {
        }
    }
}
