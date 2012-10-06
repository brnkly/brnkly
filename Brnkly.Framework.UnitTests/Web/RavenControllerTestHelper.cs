using System.Reflection;
using Brnkly.Framework.Web;
using Raven.Client;

namespace Brnkly.Framework.UnitTests.Web
{

    public static class RavenControllerTestHelper
    {
        public static void SetSessionOnController(
            RavenController controller,
            IDocumentSession session)
        {
            typeof(RavenController)
                .GetProperty("session", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(controller, session, null);
        }
    }

}
