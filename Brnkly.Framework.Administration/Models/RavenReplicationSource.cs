using Newtonsoft.Json;
using Raven.Abstractions.Data;
using System;

namespace Brnkly.Framework.Administration.Models
{
    public class RavenReplicationSource
    {
        public Guid LastDocumentEtag { get; set; }
        public Guid LastAttachmentEtag { get; set; }
        public Guid ServerInstanceId { get; set; }

        [JsonProperty(Constants.Metadata)]
        public RavenDocumentMetadata Metadata { get; set; }
    }
}