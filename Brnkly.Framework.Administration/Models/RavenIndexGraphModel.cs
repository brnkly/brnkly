using System.Collections.Generic;

namespace Brnkly.Framework.Administration.Models
{
    public class RavenIndexGraphModel
    {
        public string StoreName { get; set; }
        public IEnumerable<RavenIndexStatusModel> IndexStatuses { get; set; }
        public IEnumerable<string> AllServerNames { get; set; }
    }
}