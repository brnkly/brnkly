using System;
using System.Linq;
using AutoMapper;
using Brnkly.Raven.Admin.Models;
using Brnkly.Raven.Web;

namespace Brnkly.Raven.Admin.Controllers
{
    public abstract class RavenConfigApiControllerBase : RavenApiController
    {
        protected RavenConfigApiControllerBase()
            : base()
        {
        }

        protected Configs LoadConfig()
        {
            var docs = this.RavenSession.Advanced
                .LoadStartingWith<RavenConfig>(RavenConfig.LiveDocumentId);

            var configs = new Configs()
            {
                Live = docs
                    .FirstOrDefault(doc => doc.Id == RavenConfig.LiveDocumentId) ??
                    new RavenConfig(),
                Pending = docs
                    .FirstOrDefault(doc => doc.Id == RavenConfig.PendingDocumentId) ??
                    new RavenConfig()
            };

            return configs;
        }

        protected RavenConfigModel GetModel(RavenConfig config)
        {
            var configModel = Mapper.Map<RavenConfigModel>(config);
            if (configModel.Id != null)
            {
                configModel.Etag = this.RavenSession.Advanced.GetEtagFor(config).Value;
            }

            return configModel;
        }

        protected void ThrowIfEtagDoesNotMatch(RavenConfig config, Guid originalEtag)
        {
            var currentEtag = (config.Id == null) ?
                Guid.Empty :
                this.RavenSession.Advanced.GetEtagFor(config).Value;
            if (originalEtag != currentEtag)
            {
                throw new InvalidOperationException(
                    "The Raven config was modified by someone else or in a different browser window. " +
                    "Refresh the page to load the current version.");
            }
        }

        protected class Configs
        {
            public RavenConfig Live { get; set; }
            public RavenConfig Pending { get; set; }
        }
    }
}