using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Brnkly.Framework.Administration.Models;
using Brnkly.Framework.Data;
using Newtonsoft.Json;

namespace Brnkly.Framework.Administration.Controllers
{
    public class RavenIndexStatusController : OperationsSavePublishController<RavenConfig>
    {
        public ActionResult Index()
        {
            var graphs = this.GetIndexGraphsFromPublishedRavenConfig();

            return this.View(graphs);
        }

        public ActionResult Delete(RavenIndexDataModel indexModel)
        {
            try
            {
                RavenIndexHelper.Delete(indexModel);
            }
            catch (JsonSerializationException e)
            {
                // Even when the index is successfully deleted,
                // DatabaseCommands.DeleteIndex thows an error:
                // "Error reading RavenJToken from JsonReader."
            }

            return this.Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Reset(RavenIndexDataModel indexModel)
        {
            RavenIndexHelper.Reset(indexModel);

            return this.Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Copy(RavenIndexDataModel indexModel, string target)
        {
            RavenIndexHelper.Copy(indexModel, target);

            return this.Json(new { indexModel, target }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetHashCode(RavenIndexDataModel indexModel)
        {
            var hashCode = RavenIndexHelper.GetHashCode(indexModel);

            return this.Content(hashCode.ToString());
        }

        public ActionResult Stats(RavenIndexDataModel indexModel)
        {
            var queryResult = RavenIndexHelper.GetZeroPageQueryResult(indexModel);

            var stats = new
                            {
                                timestamp = queryResult.IndexTimestamp,
                                stale = queryResult.IsStale,
                                results = queryResult.TotalResults
                            };

            return this.Json(stats, JsonRequestBehavior.AllowGet);
        }

        private IEnumerable<RavenIndexGraphModel> GetIndexGraphsFromPublishedRavenConfig()
        {
            using (var session = this.Store.OpenSession())
            {
                var ravenConfig = session.Load<RavenConfig>(RavenConfig.StorageId);

                if (ravenConfig == null)
                {
                    return Enumerable.Empty<RavenIndexGraphModel>();
                }

                return RavenIndexHelper.GetIndexGraphs(ravenConfig);
            }
        }
    }
}