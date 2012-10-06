using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Brnkly.Framework.Caching;

namespace Brnkly.Framework.Administration.Models
{
    [CustomValidation(typeof(CacheSettingsEditModel), "Validate")]
    public class CacheSettingsEditModel
    {
        public string Id { get; set; }
        public Collection<SettingEditModel<CacheParameters>> CacheParametersPerInstance { get; private set; }

        public CacheSettingsEditModel()
        {
            this.CacheParametersPerInstance = new Collection<SettingEditModel<CacheParameters>>();
        }

        public static ValidationResult Validate(object value)
        {
            var model = value as CacheSettingsEditModel;
            if (model == null)
            {
                return null;
            }

            model.RemoveEmptyCacheParameterInstances();
            model.SortCacheParameterInstances();

            return
                CheckForDuplicateInstanceConfiguration(model) ??
                ValidationResult.Success;
        }

        private static ValidationResult CheckForDuplicateInstanceConfiguration(
            CacheSettingsEditModel model)
        {
            string previousAppName = Guid.NewGuid().ToString();
            string previousMachineName = Guid.NewGuid().ToString();

            foreach (var item in model.CacheParametersPerInstance)
            {
                if (string.Equals(item.ApplicationName, previousAppName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.MachineName, previousMachineName, StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult(
                        string.Format(
                            "Multiple cache durations are specified for application '{0}' and machine '{1}'.",
                            item.ApplicationName,
                            item.MachineName));
                }

                previousAppName = item.ApplicationName;
                previousMachineName = item.MachineName;
            }

            return null;
        }

        internal CacheSettingsEditModel ForEditing()
        {
            this.EnsureEmptyCacheDurationExists();

            return this;
        }

        private void EnsureEmptyCacheDurationExists()
        {
            var lastItem = this.CacheParametersPerInstance.LastOrDefault();
            if (lastItem == null || !(lastItem.ApplicationName == null && lastItem.MachineName == "*"))
            {
                this.CacheParametersPerInstance.Add(
                    new SettingEditModel<CacheParameters>()
                    {
                        Value = new CacheParameters()
                    });
            }
        }

        private void RemoveEmptyCacheParameterInstances()
        {
            var emptyItems = this.CacheParametersPerInstance.Where(x => x.ApplicationName == null && x.MachineName == "*").ToArray();
            foreach (var item in emptyItems)
            {
                this.CacheParametersPerInstance.Remove(item);
            }
        }

        private void SortCacheParameterInstances()
        {
            this.CacheParametersPerInstance = new Collection<SettingEditModel<CacheParameters>>(
                this.CacheParametersPerInstance
                    .OrderBy(x => x.ApplicationName)
                    .ToList());
        }

        internal CacheSettingsEditModel DeleteCacheDuration(string applicationName, string machineName)
        {
            var level = this.CacheParametersPerInstance
                .Where(x =>
                    string.Equals(x.ApplicationName, applicationName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.MachineName, machineName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var item in level)
            {
                this.CacheParametersPerInstance.Remove(item);
            }

            return this;
        }
    }
}
