using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace Brnkly.Raven.Admin.Models
{
    public class StoreModel
    {
        public string Name { get; set; }
        public Collection<StoreInstanceModel> Instances { get; set; }
    }
}