using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brnkly.Raven
{
    public static class DatabaseNameExtensions
    {
        public static string GetDatabaseName(this Uri uri)
        {
            int numberOfSegments = uri.Segments.Length;
            if (numberOfSegments < 3 ||
                !uri.Segments[numberOfSegments - 2].Equals("databases/", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Uri path must end with '/databases/[dbname]'");
            }

            return uri.Segments[numberOfSegments - 1];
        }
    }
}
