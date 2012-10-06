using System;
using System.Collections.Generic;
using System.Linq;

namespace Brnkly.Framework.Configuration
{
    public class Setting<T>
    {
        private Func<string, string, bool> areEqual = (a, b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        public string ApplicationName { get; set; }
        public string MachineName { get; set; }
        public T Value { get; set; }

        internal int GetMatchQuality(string applicationName, string machineName)
        {
            if (areEqual(this.ApplicationName, applicationName) &&
                areEqual(this.MachineName, machineName))
            {
                return 4;
            }

            if (areEqual(this.ApplicationName, applicationName) &&
                areEqual(this.MachineName, "*"))
            {
                return 3;
            }

            if (areEqual(this.ApplicationName, "*") &&
                areEqual(this.MachineName, machineName))
            {
                return 2;
            }

            if (areEqual(this.ApplicationName, "*") &&
                areEqual(this.MachineName, "*"))
            {
                return 1;
            }

            return 0;
        }
    }

    public static class SettingExtensions
    {
        public static T GetCurrentOrDefault<T>(
            this IEnumerable<Setting<T>> settings,
            T defaultValue)
        {
            var appName = PlatformApplication.Current.Name;
            var machineName = Environment.MachineName;

            var bestMatch = settings
                .Select(s => new
                {
                    Value = s.Value,
                    MatchValue = s.GetMatchQuality(appName, machineName)
                })
                .Where(x => x.MatchValue > 0)
                .OrderByDescending(x => x.MatchValue)
                .FirstOrDefault();

            if (bestMatch == null)
            {
                return defaultValue;
            }

            return bestMatch.Value;
        }

    }
}
