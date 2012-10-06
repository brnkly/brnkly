using System;

namespace Brnkly.Framework.Security
{
    public class AuthorizationEntry
    {
        private bool userIdIsWildcard;
        private bool activityIdIsWildcard;

        public string UserId { get; private set; }
        public string ActivityId { get; private set; }
        public MatchResult Result { get; private set; }

        public AuthorizationEntry(string userId, string activityId, MatchResult result)
        {
            CodeContract.ArgumentNotNullOrWhitespace("userId", userId);
            CodeContract.ArgumentNotNullOrWhitespace("activityId", activityId);

            if (userId.EndsWith("*", StringComparison.OrdinalIgnoreCase))
            {
                this.userIdIsWildcard = true;
                this.UserId = userId.Substring(0, userId.Length - 1);
            }
            else
            {
                this.UserId = userId;
            }

            if (activityId.EndsWith("*", StringComparison.OrdinalIgnoreCase))
            {
                this.activityIdIsWildcard = true;
                this.ActivityId = activityId.Substring(0, activityId.Length - 1);
            }
            else
            {
                this.ActivityId = activityId;
            }

            this.Result = result;
        }

        public MatchResult? GetResult(string userId, string activityId)
        {
            if (this.UserIdIsMatch(userId) &&
                this.ActivityIdIsMatch(activityId))
            {
                return this.Result;
            }

            return null;
        }

        private bool UserIdIsMatch(string userIdToCheck)
        {
            return this.userIdIsWildcard ?
                userIdToCheck.StartsWith(this.UserId, StringComparison.OrdinalIgnoreCase) :
                this.UserId.Equals(userIdToCheck, StringComparison.OrdinalIgnoreCase);
        }

        private bool ActivityIdIsMatch(string activityIdToCheck)
        {
            return this.activityIdIsWildcard ?
                activityIdToCheck.StartsWith(this.ActivityId, StringComparison.OrdinalIgnoreCase) :
                this.ActivityId.Equals(activityIdToCheck, StringComparison.OrdinalIgnoreCase);
        }
    }
}
