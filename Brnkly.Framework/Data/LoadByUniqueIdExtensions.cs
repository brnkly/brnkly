using System.Globalization;
using Raven.Client;

namespace Brnkly.Framework.Data
{
    public static class LoadByUniqueIdPartExtensions
    {
        public static TModel LoadByUniqueIdPart<TModel>(this IDocumentSession session, string uniqueId)
        {
            var id = IdHelper.For<TModel>(uniqueId);
            return session.Load<TModel>(id);
        }

        public static TModel LoadByUniqueIdPart<TModel>(this IDocumentSession session, int uniqueId)
        {
            return session.LoadByUniqueIdPart<TModel>(uniqueId.ToString(CultureInfo.InvariantCulture));
        }
    }
}