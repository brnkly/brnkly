using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Demo.Areas.Content.Models
{
    public class Post
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public bool IsPublished { get; set; }
    }
}