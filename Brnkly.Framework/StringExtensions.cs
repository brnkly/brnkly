using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Brnkly.Framework
{
    public static class StringExtensions
    {
        private static readonly Regex SentenceRegex = new Regex(
            "[^.!?\\s][^.!?]*(?:[.!?](?!['\"]?\\s|$)[^.!?]*)*[.!?]?['\"]?(?=\\s|$)",
            RegexOptions.Compiled);

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

        public static string Truncate(this string original, int maxLength, bool addElipsis = true)
        {
            if (!string.IsNullOrEmpty(original) && original.Length > maxLength)
            {
                string truncated = original.Substring(0, maxLength);
                if (addElipsis)
                {
                    truncated += "...";
                }
                return truncated;
            }

            return original;
        }

        public static string TruncateToNearestSentence(this string original, int maxLength)
        {
            if (!string.IsNullOrEmpty(original) && original.Length > maxLength)
            {
                string truncated = original.Substring(0, maxLength);

                var endOfSentencePunctuation = new char[] { '.', '?', '!' };
                var lastIndex = truncated.LastIndexOfAny(endOfSentencePunctuation);
                return truncated.Substring(0, lastIndex + 1);
            }
            return original;
        }

        public static IEnumerable<string> ParseSentences(this string input)
        {
            var sentenceMatches = SentenceRegex.Matches(input);

            return from Match sentenceMatch in sentenceMatches
                   where !string.IsNullOrWhiteSpace(sentenceMatch.Value)
                   select sentenceMatch.Value;
        }
    }
}
