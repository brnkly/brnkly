using System.Linq;
using System.Web.Mvc;

namespace Brnkly.Framework.Web
{
    public static class ClientValidation
    {
        public static object GetModelStateForJson(this Controller controller)
        {
            return controller.ModelState.Select(
                kv => new
                {
                    fieldName = kv.Key,
                    error = kv.Value.Errors.FirstOrDefault()
                })
                .Where(obj => obj.error != null)
                .ToDictionary(obj => obj.fieldName, obj => obj.error.ErrorMessage);
        }
    }
}