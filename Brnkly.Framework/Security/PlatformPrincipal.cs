using System.Collections.Generic;
using System.Security.Principal;

namespace Brnkly.Framework.Security
{
    public class PlatformPrincipal : GenericPrincipal
    {
        internal IEnumerable<string> Roles { get; private set; }

        public PlatformPrincipal(IIdentity identity, string[] roles)
            : base(identity, roles)
        {
            this.Roles = roles;
        }
    }
}
