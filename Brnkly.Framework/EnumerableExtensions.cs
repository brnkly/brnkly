using System.Collections.Generic;
using System.Linq;

namespace Brnkly.Framework
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<string> WhereIsNotNullOrWhiteSpace(this IEnumerable<string> items)
        {
            if (items == null)
            {
                return Enumerable.Empty<string>();
            }

            return items
                .Where(id => !string.IsNullOrWhiteSpace(id));
        }
    }
}
