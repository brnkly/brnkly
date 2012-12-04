using System;
using System.Collections.ObjectModel;
using AutoMapper;

namespace Brnkly.Raven.Admin.Models
{
    public class RavenConfigModel
    {
        public string Id { get; set; }
        public Collection<StoreModel> Stores { get; set; }

        [IgnoreMap]
        public Guid Etag { get; set; }

        public RavenConfigModel()
        {
			this.Stores = new Collection<StoreModel>();
        }
    }
}