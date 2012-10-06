using System.Collections.ObjectModel;

namespace Brnkly.Framework
{
    public partial class PlatformApplication
    {
        private static void ConfigureApplications(Collection<PlatformApplication> applications)
        {
            applications.Add(
                new PlatformApplication("Administration")
                    .AsReadOnly());

            applications.Add(
                new PlatformApplication("Demo")
                    .AsReadOnly());
        }
    }
}
