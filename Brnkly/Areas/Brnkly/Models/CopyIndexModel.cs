using System;

namespace Brnkly.Areas.Raven.Models
{
    public class CopyIndexModel
    {
        public Uri FromInstanceUrl { get; set; }
        public Uri ToInstanceUrl { get; set; }
        public string IndexName { get; set; }
    }
}