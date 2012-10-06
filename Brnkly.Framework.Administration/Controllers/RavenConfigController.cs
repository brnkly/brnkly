using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;
using AutoMapper;
using Brnkly.Framework.Administration.Models;
using Brnkly.Framework.Data;
using Brnkly.Framework.Logging;
using Brnkly.Framework.Web;
using Newtonsoft.Json;

namespace Brnkly.Framework.Administration.Controllers
{
    public class RavenConfigController : OperationsSavePublishController<RavenConfig>
    {
        [HttpGet]
        public ActionResult Index(string error = null)
        {
            var publishedModel = this.GetPublishedModel();
            var model = this.GetModel()
                .Normalize()
                .MarkPendingChanges(publishedModel);
            model.ErrorMessage = error;

            this.ViewBag.ServerNames = model.GetAllServerNames();

            return this.View(model);
        }

        [HttpPost]
        [SetLoggingLevel(LogPriority.Application, TraceEventType.Information)]
        public ActionResult Publish()
        {
            var errorMessages = new List<string>();
            try
            {
                var id = RavenConfig.StorageId;
                var newConfig = this.GetItem(id, getPending: true);
                Guid? etag = this.SaveAsPublishedItem(id, newConfig);

                var errors = RavenHelper.UpdateReplicationDocuments(newConfig);
                errorMessages.AddRange(errors);

                var json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
                var message = new RavenConfigChanged { NewRavenConfigJson = json };
                this.Bus.SendToSelf(message);
                this.Bus.Publish(message);
            }
            catch (Exception exception)
            {
                LogBuffer.Current.Error(exception);
                errorMessages.Add(exception.Message);
            }

            var errorMessage = string.Join(" ", errorMessages);
            if (errorMessage.Length > 600)
            {
                errorMessage = errorMessage.Substring(0, 600);
            }

            return this.RedirectToAction(
                "index",
                "ravenconfig",
                new
                {
                    error = errorMessage
                });
        }

        [HttpPost]
        public ActionResult Save(RavenConfigEditModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View("Index", model);
            }

            var publishedModel = this.GetPublishedModel();
            model.RemovePendingDeletes();
            this.SavePendingChanges(model);

            return this.RedirectToAction("index");
        }

        [HttpPost]
        public ActionResult AddServer(string serverName)
        {
            var model = this.GetModel().AddServer(serverName);
            this.SavePendingChanges(model);

            return this.RedirectToAction("index");
        }

        [HttpPost]
        public ActionResult DeleteServer(string serverName)
        {
            var model = this.GetModel().DeleteServer(serverName);
            this.SavePendingChanges(model);

            return this.RedirectToAction("index");
        }

        [HttpGet]
        public ActionResult StoreStatus(string serverName, string storeName)
        {
            var status = RavenHelper.GetStatus(serverName, storeName);
            return this.View(status);
        }

        [HttpGet]
        public ActionResult GetFromStore(string storeName, string serverName, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return this.View();
            }

            var rawResult = RavenHelper.GetJson(serverName, storeName, path);

            this.ViewBag.StoreName = storeName;
            this.ViewBag.ServerName = serverName;
            this.ViewBag.path = path;
            this.ViewData.Model = rawResult;
            return this.View();
        }

        private RavenConfigEditModel GetModel()
        {
            return this.GetModel<RavenConfigEditModel>(RavenConfig.StorageId)
                .Normalize();
        }

        private RavenConfigEditModel GetPublishedModel()
        {
            var publishedConfig =
                this.GetItem(RavenConfig.StorageId, getPending: false)
                ??
                new RavenConfig();
            return Mapper.Map<RavenConfigEditModel>(publishedConfig);
        }

        private void SavePendingChanges(RavenConfigEditModel model)
        {
            model.Normalize();
            this.EnsurePendingSuffixOnId(model);
            var pendingItem = Mapper.Map<RavenConfig>(model);
            this.SavePendingItem(pendingItem);
        }
    }
}
