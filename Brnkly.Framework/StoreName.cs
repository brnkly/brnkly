using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Brnkly.Framework.Logging;

namespace Brnkly.Framework
{
    public sealed class StoreName
    {
        public const string Operations = "Operations";
        public const string Content = "Content";

        public static readonly IEnumerable<string> AllStoreNames = new ReadOnlyCollection<string>(new List<string>
            {
                Operations,
                Content,
            });

        internal static void ValidateName(string name)
        {
            if (PlatformApplication.Current == PlatformApplication.UnknownApplication)
            {
                return;
            }

            foreach (var character in name)
            {
                if (!char.IsLetterOrDigit(character))
                {
                    throw new ArgumentException("Name may only contain alphanumeric characters.");
                }
            }

            var knownStoreNames = StoreName.AllStoreNames;
            if (!knownStoreNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                Log.Warning(
                    string.Format(
                        "The store name '{0}' is not known. " +
                        "Stores must be configured in the Brnkly.Framework.StoreName name.",
                        name),
                    LogPriority.Application);
            }
        }
    }
}
