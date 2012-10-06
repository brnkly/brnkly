using System.Diagnostics;
using System.Web.Mvc;
using AutoMapper;
using Brnkly.Framework.Administration.Models;
using Brnkly.Framework.Logging;
using Brnkly.Framework.Web;

namespace Brnkly.Framework.Administration.Controllers
{
    public class LoggingSettingsController : OperationsSavePublishController<LoggingSettings>
    {
        [HttpGet]
        public ActionResult Index()
        {
            this.AddAppsAndMachineToViewBagWithWildcards();
            var model = this.GetModelForEditing();
            return this.View(model);
        }

        [HttpPost]
        //[AuthorizeActivity("framework/admin/loggingsettings/change")]
        public ActionResult Save(LoggingSettingsEditModel model)
        {
            if (!this.ModelState.IsValid)
            {
                this.AddAppsAndMachineToViewBagWithWildcards();
                return this.View("Index", model);
            }

            this.SavePendingChanges(model);

            return this.RedirectToAction("index");
        }

        [HttpGet]
        //[AuthorizeActivity("framework/admin/loggingsettings/change")]
        public ActionResult DeleteLoggingLevel(string applicationName, string machineName)
        {
            var model = this.GetModelForEditing();
            model.DeleteLoggingLevel(applicationName, machineName);
            this.SavePendingChanges(model);

            return this.RedirectToAction("index");
        }

        [HttpPost]
        [SetLoggingLevel(LogPriority.Application, TraceEventType.Information)]
        //[AuthorizeActivity("framework/admin/loggingsettings/change")]
        public ActionResult Publish()
        {
            this.PublishChanges(LoggingSettings.StorageId);
            return this.RedirectToAction("index");
        }

        protected LoggingSettingsEditModel GetModelForEditing()
        {
            return this.GetModel<LoggingSettingsEditModel>(LoggingSettings.StorageId)
                .ForEditing();
        }

        protected void SavePendingChanges(LoggingSettingsEditModel model)
        {
            this.EnsurePendingSuffixOnId(model);
            var pendingItem = Mapper.Map<LoggingSettings>(model);
            this.SavePendingItem(pendingItem);
        }

        private void AddAppsAndMachineToViewBagWithWildcards()
        {
            this.AddApplicationsAndMachinesToViewBag();
            this.ViewBag.Applications.Add("[All applications]", "*");
            this.ViewBag.Machines.Add("[All machines]", "*");
        }
    }
}
