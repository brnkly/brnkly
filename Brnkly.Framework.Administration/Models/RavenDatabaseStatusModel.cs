using System;
using System.Collections.Generic;
using Raven.Abstractions.Data;

namespace Brnkly.Framework.Administration.Models
{
    public class RavenDatabaseStatusModel
    {
        public Exception Error { get; set; }
        public bool DatabaseCreated { get; set; }
        public DatabaseStatistics Statistics { get; set; }
        public SortedDictionary<string, bool> ReplicatingTo { get; set; }

        public RavenDatabaseStatusModel()
        {
            this.ReplicatingTo = new SortedDictionary<string, bool>();
        }
    }
}
