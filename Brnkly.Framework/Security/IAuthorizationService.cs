
namespace Brnkly.Framework.Security
{
    public interface IAuthorizationService
    {
        bool IsAuthorized(string userId, string activityId);
    }
}
