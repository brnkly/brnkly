using System;
using System.Collections;
using System.Linq;

namespace Brnkly.Framework
{
    public static class CodeContract
    {
        public static void ArgumentNotNull(string paramName, object argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void ArgumentNotNullOrWhitespace(string paramName, string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void ArgumentInRange(string paramName, int argument, int minValue, int maxValue)
        {
            if (argument < minValue || maxValue < argument)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }

        public static void ArgumentHasAtLeastOneItem(string paramName, IEnumerable enumerable)
        {
            if (enumerable == null || !enumerable.Cast<object>().Any())
            {
                throw new ArgumentException("The enumerable must have at least one item.", paramName);
            }
        }
    }
}
