using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using Brnkly.Framework.Configuration;
using Brnkly.Framework.Data;
using Brnkly.Framework.Logging;
using Brnkly.Framework.ServiceBus;
using Microsoft.Practices.Unity;
using Raven.Client;

namespace Brnkly.Framework.Administration.Controllers
{
    public abstract class OperationsSavePublishController<T> : Controller
        where T : class, new()
    {
        protected static readonly string PendingStorageIdSuffix = "/pending";

        [Dependency("Operations.ReadWrite")]
        public IDocumentStore Store { get; set; }

        [Dependency]
        public IBus Bus { get; set; }

        protected TModel GetModel<TModel>(string publishedItemId)
        {
            var config =
                this.GetItem(publishedItemId, getPending: true) ??
                this.GetItem(publishedItemId, getPending: false) ??
                new T();

            var model = Mapper.Map<TModel>(config);
            this.EnsurePendingSuffixOnId(model);

            return model;
        }

        protected T GetItem(string publishedItemId, bool getPending)
        {
            var id = publishedItemId;
            if (getPending)
            {
                id += PendingStorageIdSuffix;
            }

            T item = null;

            try
            {
                using (var session = this.Store.OpenSession())
                {
                    item = session.Load<T>(id);
                }
            }
            catch (Exception exception)
            {
                Log.Warning(
                    exception,
                    string.Format(
                        "Unable to load the existing {0} with Id '{1}' due to the following exception. " +
                        "This is likely due to a version conflict between existing data and current code. ",
                        typeof(T).Name,
                        id),
                    LogPriority.Application);
            }

            return item;
        }

        protected void SavePendingItem(T pendingItem)
        {
            using (var session = this.Store.OpenSession())
            {
                session.Store(pendingItem);
                session.SaveChanges();
            }
        }

        protected void PublishChanges(string publishedItemId)
        {
            var pendingItem = this.GetItem(publishedItemId, getPending: true);
            var etag = this.SaveAsPublishedItem(publishedItemId, pendingItem);

            this.PublishUpdatedMessageOnServiceBus(publishedItemId, etag);
        }

        protected void DeletePendingChanges(string publishedItemId)
        {
            var pendingId = publishedItemId + PendingStorageIdSuffix;
            using (var session = this.Store.OpenSession())
            {
                var pendingItem = session.Load<T>(pendingId);
                if (pendingItem != null)
                {
                    session.Delete(pendingItem);
                    session.SaveChanges();
                }
            }
        }

        protected Guid? SaveAsPublishedItem(string publishedItemId, T pendingItem)
        {
            using (var session = this.Store.OpenSession())
            {
                session.Store(pendingItem, publishedItemId);
                session.SaveChanges();
                return session.Advanced.GetEtagFor(pendingItem);
            }
        }

        protected void PublishUpdatedMessageOnServiceBus(string id, Guid? etag)
        {
            var message = new RavenDocumentChanged { Id = id, Etag = etag };
            this.Bus.SendToSelf(message);
            this.Bus.Publish(message);
        }

        protected void AddApplicationsAndMachinesToViewBag(bool getPending = false)
        {
            this.ViewBag.Applications = PlatformApplication.AllApplications
                .ToDictionary(a => a.Name, a => a.Name, StringComparer.OrdinalIgnoreCase);

            this.ViewBag.Machines = new Dictionary<string, string>();

            EnvironmentConfig config = null;
            using (var session = this.Store.OpenSession())
            {
                string id = EnvironmentConfig.StorageId;
                if (getPending)
                {
                    id += PendingStorageIdSuffix;
                }

                config = session.Load<EnvironmentConfig>(id);
            }

            if (config == null)
            {
                return;
            }

            foreach (var group in config.MachineGroups)
            {
                this.ViewBag.Machines.Add(group.Name, group.Name);
                foreach (var machineName in group.MachineNames)
                {
                    this.ViewBag.Machines.Add(" - " + machineName, machineName);
                }
            }
        }

        protected void EnsurePendingSuffixOnId(object model)
        {
            dynamic dynamicModel = model;
            if (!dynamicModel.Id.EndsWith(PendingStorageIdSuffix, StringComparison.OrdinalIgnoreCase))
            {
                dynamicModel.Id += PendingStorageIdSuffix;
            }
        }
    }
}
