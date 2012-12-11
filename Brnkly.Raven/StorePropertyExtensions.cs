using System.Collections.Concurrent;
using Raven.Client;

namespace Brnkly.Raven
{
    public static class StorePropertyExtensions
    {
        private static ConcurrentDictionary<string, ConcurrentDictionary<string, object>> properties =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();

        public static void SetProperty<T>(this IDocumentStore store, string key, T value)
        {
            var storeProperties = properties.GetOrAdd(
                store.Identifier,
                x => new ConcurrentDictionary<string, object>());

            storeProperties.AddOrUpdate(key, value, (k, v) => value);
        }

        public static bool TryGetProperty<T>(this IDocumentStore store, string key, out T value)
        {
            var storeProperties = properties.GetOrAdd(
                store.Identifier,
                x => new ConcurrentDictionary<string, object>());

            object obj;
            if (storeProperties.TryGetValue(key, out obj) &&
                obj is T)
            {
                value = (T)obj;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }
    }
}
