using Brnkly.Framework.Administration.Models;
using Brnkly.Framework.Data;
using Microsoft.Practices.Unity;
using Raven.Abstractions.Data;
using Raven.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Brnkly.Framework.Administration.Controllers
{
    public class RavenReplicationStatusController : AsyncController
    {
        [Dependency("Operations")]
        public IDocumentStore Store { get; set; }

        public void IndexAsync(string city)
        {
            RavenConfig ravenConfig;

            using (var session = this.Store.OpenSession())
            {
                ravenConfig = session.Load<RavenConfig>(RavenConfig.StorageId);
            }

            var ravenReplicationInformation = this.StartGatheringRavenReplicationInformation(ravenConfig);
            AsyncManager.Parameters["ravenReplicationInformation"] = ravenReplicationInformation;
        }

        private List<ReplicationInfo> StartGatheringRavenReplicationInformation(RavenConfig ravenConfig)
        {
            var ravenReplicationInformation = new List<ReplicationInfo>();

            foreach (var store in ravenConfig.Stores)
            {
                foreach (var server in store.Servers)
                {
                    var replicationSourceDocumentsUrl = RavenHelper.GetStoreUrl(server.Name, store.Name, "/docs?startsWith=Raven/Replication/Sources/");
                    var serverStatsDocumentUrl = RavenHelper.GetStoreUrl(server.Name, store.Name, "/stats");
                    var serverUrl = RavenHelper.GetStoreUrl(server.Name);
                    var replicationInfo = new ReplicationInfo(server.Name, store.Name, serverUrl);

                    ravenReplicationInformation.Add(replicationInfo);

                    this.SendRequests(replicationInfo, serverStatsDocumentUrl, replicationSourceDocumentsUrl);
                }
            }

            return ravenReplicationInformation;
        }

        private void SendRequests(ReplicationInfo replicationInfo, string serverStatsDocumentUrl, string replicationSourceDocumentsUrl)
        {
            this.AsyncManager.SendWebRequest(serverStatsDocumentUrl, result =>
            {
                var stats = RavenHelper.Deserialize<DatabaseStatistics>(result, this.Store.Conventions);
                replicationInfo.LastDocumentEtag = stats.LastDocEtag;
            });

            this.AsyncManager.SendWebRequest(replicationSourceDocumentsUrl, result =>
            {
                var sources = RavenHelper.DeserializeMany<RavenReplicationSource>(result, this.Store.Conventions);
                replicationInfo.Sources = sources.Select(source =>
                    new ReplicationSourceInfo
                    {
                        LastDocumentEtag = source.LastDocumentEtag,
                        LastModifiedDateUtc = source.Metadata.LastModified,
                        ServerUrl = source.Metadata.Id.Replace("Raven/Replication/Sources/", string.Empty)
                    })
                    .ToArray();
            });
        }

        public JsonResult IndexCompleted(List<ReplicationInfo> ravenReplicationInformation)
        {
            var result =
                from replicationInfo in ravenReplicationInformation
                group replicationInfo by replicationInfo.StoreName into stores
                let lastDocEtags = stores.ToDictionary(s => s.ServerName, s => s.LastDocumentEtag)
                select new
                {
                    StoreName = stores.Key,
                    LastDocEtags = lastDocEtags,
                    Servers = stores.SelectMany(store =>
                        from source in store.Sources
                        let sourceHost = new Uri(source.ServerUrl).Host
                        select new
                        {
                            Destination = store.ServerName,
                            UpToDate = lastDocEtags.ContainsKey(sourceHost)
                                && lastDocEtags[sourceHost] == source.LastDocumentEtag
                                && DateTimeOffset.Now.Subtract(source.LastModifiedDateUtc) < TimeSpan.FromMinutes(1),
                            SourceLastDocEtag = source.LastDocumentEtag,
                            SourceLastModifiedDateUtc = source.LastModifiedDateUtc.ToString("o"),
                            Source = sourceHost
                        })
                        .GroupBy(g => g.Source)
                };

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}