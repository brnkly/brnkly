using System;
using System.Collections.ObjectModel;

namespace Brnkly.Raven.Admin.Models
{
    public class StoreInstanceModel
    {
        public Uri Uri { get; set; }
        public string DisplayName { get; set; }
        public bool AllowReads { get; set; }
        public bool AllowWrites { get; set; }
        public Collection<DestinationModel> Destinations { get; set; }
    }
}
