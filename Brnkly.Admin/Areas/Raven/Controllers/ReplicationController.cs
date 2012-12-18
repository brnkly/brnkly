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
    public class ReplicationController : BrnklyApiControllerBase
    {
        [HttpGet]
        public RavenConfigModel Pending()
        {
            var config = this.LoadConfig();
            var configModel = Mapper.Map<RavenConfigModel>(config.Pending);
            configModel.Etag = GetEtag(config.Pending);
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

        [HttpGet]
        public RavenConfigModel Live()
        {
            var config = this.LoadConfig();
            var configModel = Mapper.Map<RavenConfigModel>(config.Live);
            configModel.Etag = GetEtag(config.Pending);
            return configModel;
        }

        private Guid GetEtag(RavenConfig pending)
        {
            if (pending != null && !string.IsNullOrWhiteSpace(pending.Id))
            {
                return this.RavenSession.Advanced.GetEtagFor(pending).Value;
            }

            return Guid.Empty;
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

            var errors = new List<string>();
            foreach (var store in config.Live.Stores)
            {
                errors.AddRange(this.RavenHelper.UpdateReplicationDocuments(store));
            }

            // TODO: Add errors to response.

            var newEtag = this.RavenSession.Advanced.GetEtagFor(config.Live).Value;
            return GetCreatedResponse(newEtag);
        }

        [HttpPost]
        public async Task<StoreStats> Stats([FromBody]StoreModel storeModel)
        {
            var store = Mapper.Map<Store>(storeModel);
            var status = await this.RavenHelper.GetStats(store);
            return status;
        }

        [HttpPost]
        public void Tracers([FromBody]StoreModel storeModel)
        {
            var store = Mapper.Map<Store>(storeModel);
            this.RavenHelper.UpdateTracerDocuments(store);
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