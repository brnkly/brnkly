using MvcContrib.PortableAreas;

namespace Brnkly.Framework.Web.Menus
{
    public class AddMenuItem : IEventMessage
    {
        public MenuItem MenuItem { get; set; }
    }
}
