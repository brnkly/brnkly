using System.Security.Principal;
using System.Web.Mvc;
using Brnkly.Framework.Logging;
using Brnkly.Framework.Security;

namespace Brnkly.Framework.Web
{
    public class AuthorizeActivityAttribute : AuthorizeAttribute
    {
        public string Activity { get; set; }

        public AuthorizeActivityAttribute(string activity)
        {
            this.Activity = activity;
        }

        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            bool isAuthorized = IsAuthorized(httpContext.User);
            if (!isAuthorized)
            {
                this.LogAuthorizationDenied(httpContext.User.Identity.Name);
            }

            return isAuthorized;
        }

        private bool IsAuthorized(IPrincipal principal)
        {
            var authorizationService = new HardCodedAuthorizationService();
            var isAuthorized = principal.Identity.IsAuthenticated ?
                authorizationService.IsAuthorized(principal.Identity.Name, this.Activity) :
                false;
            if (isAuthorized)
            {
                return true;
            }

            var platformPrincipal = principal as PlatformPrincipal;
            if (platformPrincipal != null)
            {
                foreach (var role in platformPrincipal.Roles)
                {
                    if (authorizationService.IsAuthorized(role, this.Activity))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void LogAuthorizationDenied(string username)
        {
            LogBuffer.Current.LogPriority = LogPriority.Application;
            LogBuffer.Current.Warning(
                string.Format(
                    "User '{0}' was denied authorization to perform activity '{1}'.",
                    username,
                    this.Activity));
        }
    }
}
