
namespace Brnkly.Framework.Web.Menus
{
    public class MenuItem
    {
        public string MenuName { get; set; }
        public string LinkText { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public string AreaName { get; set; }
        public object HtmlAttributes { get; set; }
        public int Position { get; set; }
    }
}