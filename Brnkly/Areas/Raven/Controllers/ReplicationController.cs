using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper;
using Brnkly.Raven.Admin.Models;

namespace Brnkly.Raven.Admin.Controllers
{
    public class ReplicationController : RavenConfigApiControllerBase
    {
        private RavenHelper ravenHelper;

        public ReplicationController(RavenHelper ravenHelper)
        {
            this.ravenHelper = ravenHelper;
        }

        [HttpGet]
        public RavenConfigModel Pending()
        {
            var config = this.LoadConfig();
            var configModel = GetModel(config.Pending);
            return configModel;
        }

        [HttpPut]
        public HttpResponseMessage Pending([FromBody]RavenConfigModel configModel)
        {
            var config = this.LoadConfig();
            ThrowIfEtagDoesNotMatch(config.Pending, configModel.Etag);

            Mapper.Map<RavenConfigModel, RavenConfig>(configModel, config.Pending);

            this.RavenSession.Store(config.Pending, RavenConfig.PendingDocumentId);
            this.RavenSession.SaveChanges();

            var newEtag = this.RavenSession.Advanced.GetEtagFor(config.Pending).Value;
            return this.GetCreatedResponse(newEtag);
        }

        [HttpPut]
        public HttpResponseMessage Live([FromBody]Guid etag)
        {
            var config = this.LoadConfig();
            ThrowIfEtagDoesNotMatch(config.Pending, etag);

            config.Live.Stores.Clear();
            foreach (var store in config.Pending.Stores)
            {
                config.Live.Stores.Add(store);
            }

            this.RavenSession.Store(config.Live, RavenConfig.LiveDocumentId);
            this.RavenSession.SaveChanges();

            foreach (var store in config.Live.Stores)
            {
                this.ravenHelper.UpdateReplicationDocuments(store);
            }

            var newEtag = this.RavenSession.Advanced.GetEtagFor(config.Live).Value;
            return GetCreatedResponse(newEtag);
        }

        [HttpPost]
        public async Task<StoreStats> Stats([FromBody]StoreModel storeModel)
        {
            var store = Mapper.Map<Store>(storeModel);
            var status = await this.ravenHelper.GetStats(store);
            return status;
        }

        [HttpPost]
        public void Tracers([FromBody]StoreModel storeModel)
        {
            var store = Mapper.Map<Store>(storeModel);
            this.ravenHelper.UpdateTracerDocuments(store);
        }

        private HttpResponseMessage GetCreatedResponse(Guid etag)
        {
            var response = Request.CreateResponse(
                HttpStatusCode.Created,
                new { Etag = etag },
                "application/json");
            return response;
        }
    }
}