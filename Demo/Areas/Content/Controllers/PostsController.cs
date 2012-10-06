using Brnkly.Framework;
using Brnkly.Framework.Web;
using Demo.Areas.Content.Models;
using Microsoft.Practices.Unity;
using Raven.Client;
using System.Web.Mvc;
using System.Linq;

namespace Demo.Areas.Content.Controllers
{
    public class PostsController : RavenController
    {
        public PostsController(
            [Dependency(StoreName.Content + ".ReadWrite")] IDocumentStore store)
            : base(store)
        {
        }

        public ActionResult Index(bool? drafts)
        {
            var isPublished = drafts.HasValue ? !drafts : true;
            var images = session.Query<Post>()
                .Where(p => p.IsPublished == isPublished)
                .ToArray();

            return Json(images, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Create(string title, bool isPublished)
        {
            var post = new Post
            {
                Title = title,
                IsPublished = isPublished,
            };

            session.Store(post);

            return Json(post, JsonRequestBehavior.AllowGet);
        }
    }
}
