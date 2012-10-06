using System.Collections.Generic;

namespace Brnkly.Framework.Administration.Models
{
    public class RavenIndexStatusModel
    {
        public string IndexName { get; set; }
        public IDictionary<string, bool> IndexExistenceByServerName { get; set; }
    }
}