using System;

namespace Brnkly.Raven.Admin.Models
{
    public class DestinationModel
    {
        public Uri Uri { get; set; }
        public string DisplayName { get; set; }
        public bool Replicate { get; set; }
        public bool IsTransitive { get; set; }
    }
}