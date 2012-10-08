using System.Collections.Generic;
using System.Linq;

namespace Brnkly.Framework.Security
{
    public class HardCodedAuthorizationService : IAuthorizationService
    {
        private static readonly List<AuthorizationEntry> entries;

        static HardCodedAuthorizationService()
        {
            entries = new List<AuthorizationEntry>()
            {
                new AuthorizationEntry("admin", "*", MatchResult.Allow),

                //Examples
                //new AuthorizationEntry("jdoe", "foo/bar/*", MatchResult.Allow),
                //new AuthorizationEntry("jdoe", "foo/baz", MatchResult.Deny),
            };
        }

        public bool IsAuthorized(string userId, string activityId)
        {
            var result = entries.Select(e => e.GetResult(userId, activityId)).FirstOrDefault();
            return (result == null) ? false : result.Value == MatchResult.Allow;
        }
    }
}
