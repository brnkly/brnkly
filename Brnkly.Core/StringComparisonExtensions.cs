using System;
using System.Collections.Generic;
using System.Linq;

namespace Brnkly
{
    public static class StringComparisonExtensions
    {
        public static bool StartsWithAny(this string input, IEnumerable<string> compareTo)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            foreach (var compare in compareTo)
            {
                if (input.StartsWith(compare, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<string> GetLongestMatches(this string input, IEnumerable<string> compareTo)
        {
            input = input.ToLowerInvariant();

            var longestMatches = compareTo
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(
                    s => new
                    {
                        Value = s,
                        MatchCount = input.GetMatchingCharCount(s.ToLowerInvariant()),
                    })
                .GroupBy(x => x.MatchCount)
                .OrderByDescending(grouping => grouping.Key)
                .FirstOrDefault();

            if (longestMatches == null)
            {
                return Enumerable.Empty<string>();
            }

            return longestMatches
                .Select(grp => grp.Value);
        }

        public static int GetMatchingCharCount(this string a, string b)
        {
            int shortestLength = Math.Min(a.Length, b.Length);
            int matchingChars = 0;

            for (int index = 0; index < shortestLength; index++)
            {
                if (!a[index].Equals(b[index]))
                {
                    break;
                }

                matchingChars++;
            }

            return matchingChars;
        }

    }
}
