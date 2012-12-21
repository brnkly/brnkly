using System;
using System.Web.Http;
using Brnkly.Areas.Raven.Models;

namespace Brnkly.Raven.Admin.Controllers
{
    public class IndexingController : BrnklyApiControllerBase
    {
        [HttpDelete]
        public void Delete([FromBody]IndexModel model)
        {
            this.RavenHelper.DeleteIndex(model.InstanceUrl, model.IndexName);
        }

        [HttpPost]
        public void Reset([FromBody]IndexModel model)
        {
            this.RavenHelper.ResetIndex(model.InstanceUrl, model.IndexName);
        }

        [HttpPost]
        public void Copy([FromBody]CopyIndexModel model)
        {
            this.RavenHelper.Copy(model.FromInstanceUrl, model.ToInstanceUrl, model.IndexName);
            // TODO: return new hash code.
        }
    }
}