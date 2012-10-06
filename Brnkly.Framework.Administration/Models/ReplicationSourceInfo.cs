using System;

namespace Brnkly.Framework.Administration.Models
{
    public class ReplicationSourceInfo
    {
        public Guid LastDocumentEtag { get; set; }
        public DateTimeOffset LastModifiedDateUtc { get; set; }
        public string ServerUrl { get; set; }
    }
}