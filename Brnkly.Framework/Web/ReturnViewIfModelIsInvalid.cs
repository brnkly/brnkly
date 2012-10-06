using System.Web.Mvc;

namespace Brnkly.Framework.Web
{
    public class ReturnViewIfModelIsInvalidAttribute : ActionFilterAttribute
    {
        private string viewName;

        public ReturnViewIfModelIsInvalidAttribute()
        {
        }

        public ReturnViewIfModelIsInvalidAttribute(string viewName)
        {
            this.viewName = viewName;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.Controller.ViewData.ModelState.IsValid)
            {
                return;
            }

            viewName = viewName ?? filterContext.ActionDescriptor.ActionName;
            filterContext.Controller.ViewData.Model = filterContext.ActionParameters["model"];

            var viewResult = new ViewResult();
            viewResult.ViewName = viewName;
            viewResult.MasterName = null;
            viewResult.ViewData = filterContext.Controller.ViewData;
            viewResult.TempData = filterContext.Controller.TempData;
            filterContext.Result = viewResult;
        }
    }
}
