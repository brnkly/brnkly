using System;
using System.Linq;
using System.Web.Http;
using Raven.Client;

namespace Brnkly.Raven.Admin.Controllers
{
    [BrnklyApiControllerConfig]
    public abstract class BrnklyApiControllerBase : ApiController
    {
        public IDocumentSession RavenSession { get; set; }
        public RavenHelper RavenHelper { get; set; }
        
        protected BrnklyApiControllerBase()
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

        protected override void Dispose(bool disposing)
        {
            if (RavenSession != null)
            {
                RavenSession.Dispose();
            }

            base.Dispose(disposing);
        }

        protected class Configs
        {
            public RavenConfig Live { get; set; }
            public RavenConfig Pending { get; set; }
        }
    }
}