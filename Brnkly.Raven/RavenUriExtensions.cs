using System;
using System.Linq;

namespace Brnkly.Raven
{
    public static class RavenUriExtensions
    {
		public static Uri GetServerRootUrl(this Uri url)
		{
			if (url.Segments.Contains("databases/", StringComparer.OrdinalIgnoreCase))
			{
				var serverUrl = new UriBuilder(url);
				var dbIndex = serverUrl.Path.IndexOf("/databases/", StringComparison.OrdinalIgnoreCase);
				serverUrl.Path = serverUrl.Path.Substring(0, dbIndex);
				return serverUrl.Uri;
			}

			return url;
		}

        public static string GetDatabaseName(this Uri url, bool throwIfNotFound = true)
        {
			if (!url.Segments.Contains("databases/", StringComparer.OrdinalIgnoreCase) ||
				url.Segments.Length < 3 ||
				!url.Segments[url.Segments.Length - 2].Equals("databases/", StringComparison.OrdinalIgnoreCase))
			{
				if (throwIfNotFound)
				{
					throw new ArgumentException("Uri path must end with '/databases/[dbname]'");
				}
				else
				{
					return null;
				}
			}

            return url.Segments[url.Segments.Length - 1];
        }

        public static Uri TrimTrailingSlash(this Uri url)
        {
            if (url.AbsolutePath.Length > 1 &&
                url.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
            {
                var originalUrl = url.ToString();
                return new Uri(originalUrl.Substring(0, originalUrl.Length - 2));
            }

            return url;
        }
    }
}
