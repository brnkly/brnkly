using Newtonsoft.Json;
using Raven.Abstractions.Data;
using System;

namespace Brnkly.Framework.Administration.Models
{
    public class RavenDocumentMetadata
    {
        [JsonProperty(Constants.LastModified)]
        public DateTimeOffset LastModified { get; set; }
        [JsonProperty("Non-Authoritative-Information")]
        public bool NonAuthoratativeInformation { get; set; }
        [JsonProperty("@id")]
        public string Id { get; set; }
        [JsonProperty("@etag")]
        public Guid Etag { get; set; }
    }
}