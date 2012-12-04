using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using AutoMapper;
using Brnkly.Raven.Admin.Models;
using Brnkly.Raven.Web;

namespace Brnkly.Raven.Admin.Controllers
{
    public class StoresController : RavenApiController
    {
        public IEnumerable<StoreModel> Get()
        {
            var config =
                this.RavenSession.Load<RavenConfig>(RavenConfig.DocumentId) ??
                new RavenConfig();
            var configModel = Mapper.Map<RavenConfigModel>(config);

            return configModel.Stores;
        }

        public HttpResponseMessage Post(IEnumerable<StoreModel> stores)
        {
            var config =
                this.RavenSession.Load<RavenConfig>(RavenConfig.DocumentId) ??
                new RavenConfig();
            var configModel = Mapper.Map<RavenConfigModel>(config);

            configModel.Stores = new Collection<StoreModel>(stores.ToList().OrderByName());
            Mapper.Map<RavenConfigModel, RavenConfig>(configModel, config);

            this.RavenSession.Store(config);
            this.RavenSession.SaveChanges();

            return GetCreatedResponse();
        }

        private HttpResponseMessage GetCreatedResponse()
        {
            var response = Request.CreateResponse(HttpStatusCode.Created);
            string uri = Url.Link("DefaultApi", new { id = RavenConfig.DocumentId });
            response.Headers.Location = new Uri(uri);
            return response;
        }
    }
}