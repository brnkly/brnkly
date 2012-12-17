using System;
using System.Web.Http;
using Brnkly.Areas.Raven.Models;

namespace Brnkly.Raven.Admin.Controllers
{
    public class IndexingController : RavenConfigApiControllerBase
    {
        private RavenHelper ravenHelper;

        public IndexingController(RavenHelper ravenHelper)
        {
            this.ravenHelper = ravenHelper;
        }

        [HttpDelete]
        public void Delete([FromBody]IndexModel model)
        {
            ravenHelper.DeleteIndex(model.InstanceUrl, model.IndexName);
        }

        [HttpPost]
        public void Reset([FromBody]IndexModel model)
        {
            ravenHelper.ResetIndex(model.InstanceUrl, model.IndexName);
        }

        [HttpPost]
        public void Copy([FromBody]CopyIndexModel model)
        {
            ravenHelper.Copy(model.FromInstanceUrl, model.ToInstanceUrl, model.IndexName);
            // TODO: return new hash code.
        }
    }
}