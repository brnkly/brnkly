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
                new AuthorizationEntry("Administrators", "*", MatchResult.Allow),
            };
        }

        public bool IsAuthorized(string userId, string activityId)
        {
            // TODO: Remove this when we are loading authz data from the ops store.
            // This is a short-term hack while we're using WB groups for authz.
            if (PlatformApplication.Current.EnvironmentType == EnvironmentType.Development ||
                PlatformApplication.Current.EnvironmentType == EnvironmentType.Test)
            {
                return true;
            }

            var result = entries.Select(e => e.GetResult(userId, activityId)).FirstOrDefault();
            return (result == null) ? false : result.Value == MatchResult.Allow;
        }
    }
}
