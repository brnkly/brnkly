using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace Brnkly.Framework.Administration.Models
{
    [CustomValidation(typeof(LoggingSettingsEditModel), "Validate")]
    public class LoggingSettingsEditModel
    {
        public string Id { get; set; }
        public Collection<SettingEditModel<SourceLevels>> LoggingLevels { get; private set; }

        public LoggingSettingsEditModel()
        {
            this.LoggingLevels = new Collection<SettingEditModel<SourceLevels>>();
        }

        public static ValidationResult Validate(object value)
        {
            var model = value as LoggingSettingsEditModel;
            model.RemoveEmptyLoggingLevels();
            model.SortLoggingLevels();

            return
                CheckForEmptyApplicationsOrMachines(model) ??
                CheckForDuplicates(model) ??
                ValidationResult.Success;
        }

        private static ValidationResult CheckForEmptyApplicationsOrMachines(
            LoggingSettingsEditModel model)
        {
            if (model.LoggingLevels.Any(item =>
                string.IsNullOrWhiteSpace(item.ApplicationName) ||
                string.IsNullOrWhiteSpace(item.MachineName)))
            {
                return new ValidationResult(
                    "One or more Application or Machine values is empty.");
            }

            return null;
        }

        private static ValidationResult CheckForDuplicates(
            LoggingSettingsEditModel model)
        {
            string previousAppName = Guid.NewGuid().ToString();
            string previousMachineName = Guid.NewGuid().ToString();

            foreach (var item in model.LoggingLevels)
            {
                if (string.Equals(item.ApplicationName, previousAppName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.MachineName, previousMachineName, StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult(
                        string.Format(
                            "Multiple settings are specified for application '{0}' and machine '{1}'.",
                            item.ApplicationName,
                            item.MachineName));
                }

                previousAppName = item.ApplicationName;
                previousMachineName = item.MachineName;
            }

            return null;
        }

        internal LoggingSettingsEditModel ForEditing()
        {
            this.EnsureEmptyLoggingLevelExists();

            return this;
        }

        private void EnsureEmptyLoggingLevelExists()
        {
            var lastItem = this.LoggingLevels.LastOrDefault();
            if (lastItem == null || !lastItem.IsEmpty())
            {
                this.LoggingLevels.Add(new SettingEditModel<SourceLevels>());
            }
        }

        private void RemoveEmptyLoggingLevels()
        {
            var emptyItems = this.LoggingLevels.Where(x => x.IsEmpty()).ToArray();
            foreach (var item in emptyItems)
            {
                this.LoggingLevels.Remove(item);
            }
        }

        private void SortLoggingLevels()
        {
            this.LoggingLevels = new Collection<SettingEditModel<SourceLevels>>(
                this.LoggingLevels
                    .OrderBy(x => x.ApplicationName)
                    .ThenBy(x => x.MachineName)
                    .ToList());
        }

        internal LoggingSettingsEditModel DeleteLoggingLevel(string applicationName, string machineName)
        {
            var level = this.LoggingLevels
                .Where(x =>
                    string.Equals(x.ApplicationName, applicationName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.MachineName, machineName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var item in level)
            {
                this.LoggingLevels.Remove(item);
            }

            return this;
        }
    }
}
