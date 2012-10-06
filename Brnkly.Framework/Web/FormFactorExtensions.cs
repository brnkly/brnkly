using System.Web.Mvc;

namespace Brnkly.Framework.Web
{
    public static class FormFactorExtensions
    {
        public static FormFactor GetFormFactor(this ControllerContext controllerContext)
        {
            var formFactorName = controllerContext.RouteData.GetRequiredString(RouteDataTokens.FormFactor);

            if (formFactorName == RouteDataValues.Devices)
            {
                return FormFactor.Devices;
            }

            return FormFactor.Desktop;
        }
    }
}
