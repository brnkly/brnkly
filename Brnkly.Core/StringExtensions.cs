using System;

namespace Brnkly
{
    public static class StringExtensions
    {
        public static string TrimPrefix(
            this string input,
            string prefix,
            StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return input.StartsWith(prefix, comparisonType) && input.Length > prefix.Length
                ? input.Substring(prefix.Length)
                : input;
        }

        public static string TrimSuffix(
            this string input,
            string suffix,
            StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return input.EndsWith(suffix, comparisonType) && input.Length >= suffix.Length
                ? input.Substring(0, input.Length - suffix.Length)
                : input;
        }

        public static string EnsurePrefix(
            this string input,
            string prefix,
            StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return input.StartsWith(prefix, comparisonType)
                       ? input
                       : prefix + input;
        }

        public static string EnsureSuffix(
            this string input,
            string suffix,
            StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return input.EndsWith(suffix, comparisonType)
                       ? input
                       : input + suffix;
        }
    }
}
