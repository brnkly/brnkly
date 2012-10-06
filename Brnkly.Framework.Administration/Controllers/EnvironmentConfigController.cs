using System;
using System.Diagnostics;
using System.Web.Mvc;
using AutoMapper;
using Brnkly.Framework.Administration.Models;
using Brnkly.Framework.Configuration;
using Brnkly.Framework.Logging;
using Brnkly.Framework.Web;

namespace Brnkly.Framework.Administration.Controllers
{
    public class EnvironmentConfigController : OperationsSavePublishController<EnvironmentConfig>
    {
        [HttpGet]
        public ActionResult Home(string error = null)
        {
            var activeModel = this.GetActiveModel();
            var pendingModel = this.GetPendingModel();
            pendingModel.ErrorMessage = error;
            var model = new Tuple<EnvironmentConfigEditModel, EnvironmentConfigEditModel>(
                activeModel,
                pendingModel);

            return this.View(model);
        }

        [HttpPost]
        [SetLoggingLevel(LogPriority.Application, TraceEventType.Information)]
        public ActionResult Publish()
        {
            string errorMessage = null;
            try
            {
                var id = EnvironmentConfig.StorageId;
                Guid? etag = null;
                var newConfig = this.GetItem(id, getPending: true);
                etag = this.SaveAsPublishedItem(id, newConfig);
                this.PublishUpdatedMessageOnServiceBus(id, etag);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
            }

            return this.RedirectToAction("home", "environmentconfig", new { error = errorMessage });
        }

        [HttpPost]
        public ActionResult DeletePending()
        {
            this.DeletePendingChanges(EnvironmentConfig.StorageId);
            return this.RedirectToAction("home");
        }

        protected void SavePendingChanges(EnvironmentConfigEditModel model)
        {
            model.Normalize();
            this.EnsurePendingSuffixOnId(model);
            var pendingItem = Mapper.Map<EnvironmentConfig>(model);
            this.SavePendingItem(pendingItem);
        }

        protected EnvironmentConfigEditModel GetActiveModel()
        {
            var active = this.GetItem(EnvironmentConfig.StorageId, getPending: false);
            return this.GetModel(active);
        }

        protected EnvironmentConfigEditModel GetPendingModel()
        {
            var pending =
                this.GetItem(EnvironmentConfig.StorageId, getPending: true) ??
                this.GetItem(EnvironmentConfig.StorageId, getPending: false);

            return this.GetModel(pending);
        }

        private EnvironmentConfigEditModel GetModel(EnvironmentConfig environmentConfig)
        {
            if (environmentConfig == null)
            {
                environmentConfig =
                    new EnvironmentConfig()
                        .WithDefaultsForEnvironmentType(
                            PlatformApplication.Current.EnvironmentType);
            }

            return Mapper.Map<EnvironmentConfigEditModel>(environmentConfig)
                .Normalize();
        }
    }
}
