using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Framework
{
    public static class NameExtensions
    {
        public static Collection<T> OrderByName<T>(this Collection<T> collection)
        {
            Func<T, string> getName = (T item) => { dynamic d = item; return d.Name; };
            var sorted = collection.OrderBy(item => getName(item));
            return new Collection<T>(sorted.ToList());
        }

        public static T SelectByName<T>(this IEnumerable<T> collection, string name)
        {
            Func<T, string> getName = (T item) => { dynamic d = item; return d.Name; };
            var selectedItem = collection.SingleOrDefault(
                item => string.Equals(name, getName(item), StringComparison.OrdinalIgnoreCase));

            return selectedItem;
        }
    }
}
