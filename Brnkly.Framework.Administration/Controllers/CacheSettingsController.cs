using System.Diagnostics;
using System.Web.Mvc;
using AutoMapper;
using Brnkly.Framework.Administration.Models;
using Brnkly.Framework.Caching;
using Brnkly.Framework.Logging;
using Brnkly.Framework.Web;

namespace Brnkly.Framework.Administration.Controllers
{
    public class CacheSettingsController : OperationsSavePublishController<CacheSettingsData>
    {
        [HttpGet]
        public ActionResult Index()
        {
            this.AddAppsAndMachineToViewBagWithWildcards();
            var model = this.GetModelForEditing();
            return this.View(model);
        }

        [HttpPost]
        //[AuthorizeActivity("framework/admin/cachesettings/change")]
        public ActionResult Save(CacheSettingsEditModel model)
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
        //[AuthorizeActivity("framework/admin/cachesettings/change")]
        public ActionResult DeleteCacheDuration(string applicationName, string machineName)
        {
            var model = this.GetModelForEditing();
            model.DeleteCacheDuration(applicationName, machineName);
            this.SavePendingChanges(model);

            return this.RedirectToAction("index");
        }

        [HttpPost]
        [SetLoggingLevel(LogPriority.Application, TraceEventType.Information)]
        //[AuthorizeActivity("framework/admin/cachesettings/change")]
        public ActionResult Publish()
        {
            this.PublishChanges(CacheSettingsData.StorageId);
            return this.RedirectToAction("index");
        }

        protected CacheSettingsEditModel GetModelForEditing()
        {
            return this.GetModel<CacheSettingsEditModel>(CacheSettingsData.StorageId)
                .ForEditing();
        }

        protected void SavePendingChanges(CacheSettingsEditModel model)
        {
            this.EnsurePendingSuffixOnId(model);
            var pendingItem = Mapper.Map<CacheSettingsData>(model);
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
