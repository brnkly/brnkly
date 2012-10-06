using System;

namespace Brnkly.Framework.Administration.Models
{
    public class ReplicationInfo
    {
        public string ServerName { get; set; }
        public string StoreName { get; set; }
        public string ServerUrl { get; set; }
        public Guid LastDocumentEtag { get; set; }
        public ReplicationSourceInfo[] Sources { get; set; }

        public ReplicationInfo(string serverName, string storeName, string serverUrl)
        {
            this.ServerName = serverName;
            this.StoreName = storeName;
            this.ServerUrl = serverUrl;
        }
    }
}